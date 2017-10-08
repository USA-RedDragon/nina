﻿using ASCOM.DriverAccess;
using EDSDKLib;
using NINA.EquipmentChooser;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.ViewModel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using ZWOptical.ASISDK;

namespace NINA.ViewModel {
    class CameraVM : DockableVM {

        public CameraVM() : base() {
            Title = "LblCamera";
            ContentId = nameof(CameraVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CameraSVG"];

            //ConnectCameraCommand = new RelayCommand(connectCamera);
            ChooseCameraCommand = new RelayCommand(ChooseCamera);
            DisconnectCommand = new RelayCommand(DisconnectCamera);
            CoolCamCommand = new AsyncCommand<bool>(() => CoolCamera(new Progress<double>(p => CoolingProgress = p)));
            CancelCoolCamCommand = new RelayCommand(CancelCoolCamera);
            RefreshCameraListCommand = new RelayCommand(RefreshCameraList);

            _updateCamera = new DispatcherTimer();
            _updateCamera.Interval = TimeSpan.FromMilliseconds(1000);
            _updateCamera.Tick += UpdateCamera_Tick;

            CoolingRunning = false;
            CoolerPowerHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);
            CCDTemperatureHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);

        }

        private void RefreshCameraList(object obj) {
            CameraChooserVM.GetEquipment();
        }

        private void CoolCamera_Tick(IProgress<double> progress) {

            double currentTemp = Cam.CCDTemperature;
            double deltaTemp = currentTemp - TargetTemp;


            DateTime now = DateTime.Now;
            TimeSpan delta = now.Subtract(_deltaT);

            Duration = Duration - ((double)delta.TotalMilliseconds / (1000 * 60));

            if(Duration < 0) { Duration = 0; }
                        
            double newTemp = GetY(_startPoint, _endPoint, new Vector2(-_startPoint.X, _startPoint.Y), Duration);
            Cam.SetCCDTemperature = newTemp;

            progress.Report(1 - (Duration / _initalDuration));

            _deltaT = DateTime.Now;


        }

        private CameraChooserVM _cameraChooserVM;
        public CameraChooserVM CameraChooserVM {
            get {
                if (_cameraChooserVM == null) {
                    _cameraChooserVM = new CameraChooserVM();
                }
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


        private DateTime _deltaT;

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

        private async Task<bool> CoolCamera(IProgress<double> progress) {
            _cancelCoolCameraSource = new CancellationTokenSource();
            Cam.CoolerOn = true;
            if (Duration == 0) {
                Cam.SetCCDTemperature = TargetTemp;
                progress.Report(1);
            } else {
                try {


                    _deltaT = DateTime.Now;
                    double currentTemp = Cam.CCDTemperature;
                    _startPoint = new Vector2(Duration, currentTemp);
                    _endPoint = new Vector2(0, TargetTemp);
                    Cam.SetCCDTemperature = currentTemp;
                    _initalDuration = Duration;
                    
                    CoolingRunning = true;
                    do {
                        CoolCamera_Tick(progress);
                        await Task.Delay(TimeSpan.FromMilliseconds(300), _cancelCoolCameraSource.Token);
                        _cancelCoolCameraSource.Token.ThrowIfCancellationRequested();
                    } while (Duration > 0);


                } catch (OperationCanceledException ex) {
                    Cam.SetCCDTemperature = Cam.CCDTemperature;
                    Logger.Trace(ex.Message);

                } finally {
                    progress.Report(1);
                    Duration = 0;
                    CoolingRunning = false;
                }
            }
            return true;

        }

        private void CancelCoolCamera(object o) {
            _cancelCoolCameraSource?.Cancel();
        }


        DispatcherTimer _updateCamera;


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
            set {
                _cam = value;
                Mediator.Instance.Notify(MediatorMessages.CameraChanged, _cam);
            }
        }

        private void ChooseCamera(object obj) {
            Cam = (ICamera)CameraChooserVM.SelectedDevice;
            if (Cam?.Connect() == true) {
                RaisePropertyChanged(nameof(Cam));
                _updateCamera.Start();
                Settings.CameraId = Cam.Id;
            } else {
                Cam = null;
            }
        }

        private void DisconnectCamera(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Camera?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                _updateCamera.Stop();
                _cancelCoolCameraSource?.Cancel();
                CoolingRunning = false;
                Cam.Disconnect();
                Cam = null;
            }
        }

        void UpdateCamera_Tick(object sender, EventArgs e) {
            if (Cam.Connected) {
                Cam.UpdateValues();

                DateTime x = DateTime.Now;
                CoolerPowerHistory.Add(new KeyValuePair<DateTime, double>(x, Cam.CoolerPower));
                CCDTemperatureHistory.Add(new KeyValuePair<DateTime, double>(x, Cam.CCDTemperature));

            }

        }


        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CoolerPowerHistory { get; private set; }
        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CCDTemperatureHistory { get; private set; }

        public ICommand CoolCamCommand { get; private set; }

        public ICommand ChooseCameraCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public ICommand CancelCoolCamCommand { get; private set; }

        public ICommand RefreshCameraListCommand { get; private set; }
    }

    class CameraChooserVM : EquipmentChooserVM {
        public override void GetEquipment() {
            Devices.Clear();

            var ascomDevices = new ASCOM.Utilities.Profile();

            for (int i = 0; i < ASICameras.Count; i++) {
                var cam = ASICameras.GetCamera(i);
                if (cam.Name != "") {
                    Devices.Add(cam);
                }
            }

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Camera")) {

                try {
                    AscomCamera cam = new AscomCamera(device.Key, device.Value + " (ASCOM)");
                    Devices.Add(cam);
                } catch (Exception) {
                    //only add cameras which are supported. e.g. x86 drivers will not work in x64
                }
            }



            IntPtr cameraList;
            uint err = EDSDK.EdsGetCameraList(out cameraList);
            if (err == (uint)EDSDK.EDS_ERR.OK) {
                int count;
                err = EDSDK.EdsGetChildCount(cameraList, out count);

                for (int i = 0; i < count; i++) {
                    IntPtr cam;
                    err = EDSDK.EdsGetChildAtIndex(cameraList, i, out cam);

                    EDSDK.EdsDeviceInfo info;
                    err = EDSDK.EdsGetDeviceInfo(cam, out info);


                    Devices.Add(new EDCamera(cam, info));
                }


            }

            if (Devices.Count > 0) {
                var items = (from device in Devices where device.Id == Settings.CameraId select device);
                if (items.Count() > 0) {
                    SelectedDevice = items.First();

                } else {
                    SelectedDevice = Devices.First();
                }
            }
        }


    }
}
