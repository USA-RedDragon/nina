﻿#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.Camera {

    internal class CameraVM : DockableVM, ICameraVM {

        public CameraVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblCamera";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CameraSVG"];

            _cameraChooserVM = new CameraChooserVM(profileService, telescopeMediator);

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;

            ChooseCameraCommand = new AsyncCommand<bool>(ChooseCamera);
            CancelConnectCameraCommand = new RelayCommand(CancelConnectCamera);
            DisconnectCommand = new RelayCommand(DisconnectDiag);
            CoolCamCommand = new AsyncCommand<bool>(() => StartCoolCamera(new Progress<double>(p => CoolingProgress = p)));
            CancelCoolCamCommand = new RelayCommand(CancelCoolCamera);
            RefreshCameraListCommand = new RelayCommand(RefreshCameraList);

            CoolingRunning = false;
            CoolerPowerHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);
            CCDTemperatureHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);
            ToggleCoolerOnCommand = new RelayCommand(ToggleCoolerOn);
            ToggleDewHeaterOnCommand = new RelayCommand(ToggleDewHeaterOn);

            updateTimer = new DeviceUpdateTimer(
                GetCameraValues,
                UpdateCameraValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshCameraList(null);
            };
        }

        private void RefreshCameraList(object obj) {
            CameraChooserVM.GetEquipment();
        }

        private async Task WaitForTargetTemperatureStep(double targetTemperatureStep, double percentage, CancellationToken token) {
            var interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
            var threshold = 1;
            double temperature = 0.0;
            while (Math.Abs((temperature = Cam.Temperature) - targetTemperatureStep) > threshold) {
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblCooling"],
                        Progress = percentage,
                        Status2 = Locale.Loc.Instance["LblWaitForTemperatureStep"]
                    }
                );
                await Utility.Utility.Wait(TimeSpan.FromSeconds(interval), token);
            }
        }

        private async Task<double> GetNextTemperatureStep(CancellationToken token) {
            var interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
            double currentTemp = Cam.Temperature;
            double deltaTemp = currentTemp - TargetTemp;

            var delta = await Utility.Utility.Delay(TimeSpan.FromSeconds(interval), token);

            _remainingDuration = _remainingDuration - ((double)delta.TotalMilliseconds / (1000 * 60));
            if (_remainingDuration < 0) { _remainingDuration = 0; }

            return GetY(_startPoint, _endPoint, _remainingDuration);
        }

        private async Task SetNextTemperatureStep(IProgress<double> progress, CancellationToken token) {
            var targetTemperatureStep = await GetNextTemperatureStep(token);

            Cam.TemperatureSetPoint = targetTemperatureStep;

            targetTemperatureStep = Cam.TemperatureSetPoint;

            var percentage = 1 - (_remainingDuration / _initalDuration);

            //Use the camera set point here (some cameras like the ASI cameras can only set integer values and lose precision)
            await WaitForTargetTemperatureStep(Cam.TemperatureSetPoint, percentage, token);

            progress.Report(percentage);

            applicationStatusMediator.StatusUpdate(
                new ApplicationStatus() {
                    Source = Title,
                    Status = Locale.Loc.Instance["LblCooling"],
                    Progress = percentage
                }
            );
        }

        private ICameraMediator cameraMediator;

        private CameraChooserVM _cameraChooserVM;

        public CameraChooserVM CameraChooserVM {
            get {
                return _cameraChooserVM;
            }
            set {
                _cameraChooserVM = value;
            }
        }

        private class Vector2 {
            public double X { get; private set; }
            public double Y { get; private set; }

            public Vector2(double x, double y) {
                X = x;
                Y = y;
            }
        }

        private double GetY(Vector2 point1, Vector2 point2, double x) {
            var m = (point2.Y - point1.Y) / (point2.X - point1.X);
            var b = point1.Y - (m * point1.X);

            return m * x + b;
        }

        private double GetY(Vector2 point1, Vector2 point2, Vector2 point3, double x) {
            double denom = (point1.X - point2.X) * (point1.X - point3.X) * (point2.X - point3.X);
            double A = (point3.X * (point2.Y - point1.Y) + point2.X * (point1.Y - point3.Y) + point1.X * (point3.Y - point2.Y)) / denom;
            double B = (point3.X * point3.X * (point1.Y - point2.Y) + point2.X * point2.X * (point3.Y - point1.Y) + point1.X * point1.X * (point2.Y - point3.Y)) / denom;
            double C = (point2.X * point3.X * (point2.X - point3.X) * point1.Y + point3.X * point1.X * (point3.X - point1.X) * point2.Y + point1.X * point2.X * (point1.X - point2.X) * point3.Y) / denom;

            return (A * Math.Pow(x, 2) + B * x + C);
        }

        private Vector2 _startPoint;
        private Vector2 _endPoint;

        private double _initalDuration;
        private double _remainingDuration;
        private double _coolingProgress;

        public double CoolingProgress {
            get {
                return _coolingProgress;
            }

            set {
                _coolingProgress = value;
                RaisePropertyChanged();
            }
        }

        private bool _coolingRunning;

        public bool CoolingRunning {
            get {
                return _coolingRunning;
            }
            set {
                _coolingRunning = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource _cancelCoolCameraSource;

        private async Task<bool> StartCoolCamera(IProgress<double> progress) {
            _cancelCoolCameraSource?.Dispose();
            _cancelCoolCameraSource = new CancellationTokenSource();
            _remainingDuration = Duration;
            return await Task<bool>.Run(async () => {
                if (_remainingDuration == 0) {
                    Cam.TemperatureSetPoint = TargetTemp;
                    Cam.CoolerOn = true;
                    progress.Report(1);
                } else {
                    try {
                        double currentTemp = Cam.Temperature;
                        _startPoint = new Vector2(_remainingDuration, currentTemp);
                        _endPoint = new Vector2(0, TargetTemp);
                        Cam.TemperatureSetPoint = currentTemp;
                        _initalDuration = _remainingDuration;

                        Cam.CoolerOn = true;
                        CoolingRunning = true;
                        do {
                            await SetNextTemperatureStep(progress, _cancelCoolCameraSource.Token);

                            _cancelCoolCameraSource.Token.ThrowIfCancellationRequested();
                        } while (_remainingDuration > 0);
                    } catch (OperationCanceledException ex) {
                        Cam.TemperatureSetPoint = Cam.Temperature;
                        Logger.Trace(ex.Message);
                    } finally {
                        progress.Report(1);
                        _remainingDuration = 0;
                        CoolingRunning = false;
                        applicationStatusMediator.StatusUpdate(
                            new ApplicationStatus() {
                                Source = Title,
                                Status = string.Empty
                            }
                        );
                    }
                }
                return true;
            });
        }

        private void CancelCoolCamera(object o) {
            _cancelCoolCameraSource?.Cancel();
        }

        private double _targetTemp;

        public double TargetTemp {
            get {
                return _targetTemp;
            }
            set {
                _targetTemp = value;
                RaisePropertyChanged();
            }
        }

        private double _duration;

        public double Duration {
            get {
                return _duration;
            }
            set {
                _duration = value;
                RaisePropertyChanged();
            }
        }

        private Model.MyCamera.ICamera _cam;

        public Model.MyCamera.ICamera Cam {
            get {
                return _cam;
            }
            private set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseCamera() {
            await ss.WaitAsync();
            try {
                Disconnect();
                updateTimer?.Stop();

                if (CameraChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.CameraSettings.Id = CameraChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );

                var cam = (ICamera)CameraChooserVM.SelectedDevice;
                _cancelConnectCameraSource?.Dispose();
                _cancelConnectCameraSource = new CancellationTokenSource();
                if (cam != null) {
                    try {
                        var connected = await cam.Connect(_cancelConnectCameraSource.Token);
                        _cancelConnectCameraSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.Cam = cam;

                            CameraInfo = new CameraInfo {
                                BinX = Cam.BinX,
                                BinY = Cam.BinY,
                                CameraState = Cam.CameraState,
                                CanSubSample = Cam.CanSubSample,
                                Connected = true,
                                CoolerOn = Cam.CoolerOn,
                                CoolerPower = Cam.CoolerPower,
                                DewHeaterOn = Cam.DewHeaterOn,
                                Gain = Cam.Gain,
                                HasShutter = Cam.HasShutter,
                                CanSetTemperature = Cam.CanSetTemperature,
                                IsSubSampleEnabled = Cam.EnableSubSample,
                                Name = Cam.Name,
                                Offset = Cam.Offset,
                                PixelSize = Cam.PixelSizeX,
                                Temperature = Cam.Temperature,
                                TemperatureSetPoint = Cam.TemperatureSetPoint,
                                XSize = Cam.CameraXSize,
                                YSize = Cam.CameraYSize,
                                Battery = Cam.BatteryLevel,
                                BitDepth = Cam.BitDepth,
                                ElectronsPerADU = Cam.ElectronsPerADU
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblCameraConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            profileService.ActiveProfile.CameraSettings.Id = this.Cam.Id;
                            if (Cam.PixelSizeX > 0) {
                                profileService.ActiveProfile.CameraSettings.PixelSize = Cam.PixelSizeX;
                            }

                            BroadcastCameraInfo();

                            if (Cam.CanSetTemperature) {
                                TargetTemp = Cam.TemperatureSetPoint;
                            }

                            return true;
                        } else {
                            this.Cam = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (CameraInfo.Connected) { Disconnect(); }
                        CameraInfo.Connected = false;
                        return false;
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        if (CameraInfo.Connected) { Disconnect(); }
                        CameraInfo.Connected = false;
                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get {
                if (cameraInfo == null) {
                    cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
                }
                return cameraInfo;
            }
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private void ToggleCoolerOn(object o) {
            if (CameraInfo.Connected) {
                Cam.CoolerOn = (bool)o;
            }
        }

        private void ToggleDewHeaterOn(object o) {
            if (CameraInfo.Connected) {
                Cam.DewHeaterOn = (bool)o;
            }
        }

        private void BroadcastCameraInfo() {
            cameraMediator.Broadcast(CameraInfo);
        }

        private void CancelConnectCamera(object o) {
            _cancelConnectCameraSource?.Cancel();
        }

        private void UpdateCameraValues(Dictionary<string, object> cameraValues) {
            object o = null;
            cameraValues.TryGetValue(nameof(CameraInfo.Connected), out o);
            CameraInfo.Connected = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CameraInfo.CoolerOn), out o);
            CameraInfo.CoolerOn = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CameraInfo.Temperature), out o);
            CameraInfo.Temperature = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraInfo.CoolerPower), out o);
            CameraInfo.CoolerPower = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraInfo.DewHeaterOn), out o);
            CameraInfo.DewHeaterOn = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CameraInfo.CameraState), out o);
            CameraInfo.CameraState = (string)(o ?? string.Empty);

            cameraValues.TryGetValue(nameof(CameraInfo.Battery), out o);
            CameraInfo.Battery = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.Offset), out o);
            CameraInfo.Offset = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.TemperatureSetPoint), out o);
            CameraInfo.TemperatureSetPoint = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraInfo.ElectronsPerADU), out o);
            CameraInfo.ElectronsPerADU = (double)(o ?? double.NaN);

            DateTime x = DateTime.Now;
            CoolerPowerHistory.Add(new KeyValuePair<DateTime, double>(x, CameraInfo.CoolerPower));
            CCDTemperatureHistory.Add(new KeyValuePair<DateTime, double>(x, CameraInfo.Temperature));

            BroadcastCameraInfo();
        }

        private Dictionary<string, object> GetCameraValues() {
            Dictionary<string, object> cameraValues = new Dictionary<string, object>();
            cameraValues.Add(nameof(CameraInfo.Connected), _cam?.Connected ?? false);
            cameraValues.Add(nameof(CameraInfo.CoolerOn), _cam?.CoolerOn ?? false);
            cameraValues.Add(nameof(CameraInfo.Temperature), _cam?.Temperature ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.CoolerPower), _cam?.CoolerPower ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.DewHeaterOn), _cam?.DewHeaterOn ?? false);
            cameraValues.Add(nameof(CameraInfo.CameraState), _cam?.CameraState ?? string.Empty);
            cameraValues.Add(nameof(CameraInfo.TemperatureSetPoint), _cam?.TemperatureSetPoint ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.ElectronsPerADU), _cam?.ElectronsPerADU ?? double.NaN);

            if (_cam != null && _cam.CanSetOffset) {
                cameraValues.Add(nameof(CameraInfo.Offset), _cam?.Offset ?? -1);
            }

            if (_cam != null && _cam.HasBattery) {
                cameraValues.Add(nameof(CameraInfo.Battery), _cam?.BatteryLevel ?? -1);
            }

            return cameraValues;
        }

        private DeviceUpdateTimer updateTimer;

        private CancellationTokenSource _cancelConnectCameraSource;

        private void DisconnectDiag(object o) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Camera?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {
            updateTimer?.Stop();
            _cancelCoolCameraSource?.Cancel();
            CoolingRunning = false;
            Cam?.Disconnect();
            Cam = null;
            CameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
            BroadcastCameraInfo();
        }

        public IAsyncEnumerable<int> GetValues() {
            return new AsyncEnumerable<int>(async yield => {
                await _cam.DownloadLiveView(new CancellationToken()).ConfigureAwait(false);

                // Yes, it's even needed for 'yield.ReturnAsync'
                await yield.ReturnAsync(123).ConfigureAwait(false);
            });
        }

        public IAsyncEnumerable<ImageArray> LiveView(CancellationToken ct) {
            return new AsyncEnumerable<ImageArray>(async yield => {
                if (CameraInfo.Connected && _cam.CanShowLiveView) {
                    try {
                        _cam.StartLiveView();

                        while (true) {
                            var iarr = await _cam.DownloadLiveView(ct);

                            await yield.ReturnAsync(iarr);

                            ct.ThrowIfCancellationRequested();
                        }
                    } catch (OperationCanceledException) {
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    } finally {
                        _cam.StopLiveView();
                    }
                }
            });
        }

        public async Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress) {
            double exposureTime = sequence.ExposureTime;
            if (CameraInfo.Connected == true) {
                CameraInfo.IsExposing = true;
                CameraInfo.ExposureEndTime = DateTime.Now.AddSeconds(sequence.ExposureTime);
                CameraInfo.NextExposureLength = sequence.NextSequence?.ExposureTime ?? -1;
                BroadcastCameraInfo();
                Cam.StartExposure(sequence);

                var start = DateTime.Now;
                var elapsed = 0.0d;
                var exposureSeconds = 0;
                progress.Report(new ApplicationStatus() {
                    Status = Locale.Loc.Instance["LblExposing"],
                    Progress = exposureSeconds,
                    MaxProgress = (int)exposureTime,
                    ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue
                });
                /* Wait for Capture */

                if (exposureTime >= 1) {
                    await Task.Run(async () => {
                        do {
                            var delta = await Utility.Utility.Delay(500, token);
                            elapsed += delta.TotalSeconds;
                            exposureSeconds = (int)elapsed;
                            token.ThrowIfCancellationRequested();

                            progress.Report(new ApplicationStatus() {
                                Status = Locale.Loc.Instance["LblExposing"],
                                Progress = exposureSeconds,
                                MaxProgress = (int)exposureTime,
                                ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue
                            });
                        } while ((elapsed < exposureTime) && CameraInfo.Connected == true);
                    });
                }
                token.ThrowIfCancellationRequested();
                CameraInfo.IsExposing = false;
                BroadcastCameraInfo();
            }
        }

        public void SetBinning(short x, short y) {
            Cam.SetBinning(x, y);
            CameraInfo.BinX = Cam.BinX;
            CameraInfo.BinY = Cam.BinY;
            BroadcastCameraInfo();
        }

        public void AbortExposure() {
            if (CameraInfo.Connected == true) {
                Cam?.AbortExposure();
                BroadcastCameraInfo();
            }

            CameraInfo.IsExposing = false;
            CameraInfo.ExposureEndTime = DateTime.Now;
        }

        public void SetGain(short gain) {
            if (CameraInfo.Connected == true) {
                Cam.Gain = gain;
                CameraInfo.Gain = Cam.Gain;
                BroadcastCameraInfo();
            }
        }

        public void SetSubSample(bool subSample) {
            if (CameraInfo.Connected == true) {
                Cam.EnableSubSample = subSample;
                BroadcastCameraInfo();
            }
        }

        public async Task<ImageArray> Download(CancellationToken token, bool calculateStatistics) {
            CameraInfo.IsExposing = false;
            CameraInfo.ExposureEndTime = DateTime.Now;
            BroadcastCameraInfo();
            if (CameraInfo.Connected == true) {
                Stopwatch seqDuration = Stopwatch.StartNew();
                var output = await Cam.DownloadExposure(token, calculateStatistics);
                seqDuration.Stop();
                CameraInfo.LastDownloadTime = seqDuration.Elapsed.TotalSeconds;
                BroadcastCameraInfo();
                return output;
            } else {
                return null;
            }
        }

        public void SetSubSampleArea(int x, int y, int width, int height) {
            if (CameraInfo.Connected == true && CameraInfo.CanSubSample) {
                Cam.SubSampleX = x;
                Cam.SubSampleY = y;
                Cam.SubSampleWidth = width;
                Cam.SubSampleHeight = height;
            }
        }

        public bool AtTargetTemp {
            get {
                return Math.Abs(cameraInfo.Temperature - TargetTemp) <= 2;
            }
        }

        public Task<bool> Connect() {
            return ChooseCamera();
        }

        public CameraInfo GetDeviceInfo() {
            return CameraInfo;
        }

        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CoolerPowerHistory { get; private set; }
        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CCDTemperatureHistory { get; private set; }
        public ICommand ToggleCoolerOnCommand { get; private set; }
        public ICommand CoolCamCommand { get; private set; }
        public ICommand ToggleDewHeaterOnCommand { get; private set; }

        private IApplicationStatusMediator applicationStatusMediator;

        public IAsyncCommand ChooseCameraCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public ICommand CancelCoolCamCommand { get; private set; }

        public ICommand RefreshCameraListCommand { get; private set; }
        public ICommand CancelConnectCameraCommand { get; private set; }
    }
}