#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Accord.Math.Geometry;

namespace NINA.ViewModel.Equipment.Focuser {

    internal class FocuserVM : DockableVM, IFocuserVM {

        public FocuserVM(IProfileService profileService, IFocuserMediator focuserMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFocuser";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;

            ChooseFocuserCommand = new AsyncCommand<bool>(() => ChooseFocuser());
            CancelChooseFocuserCommand = new RelayCommand(CancelChooseFocuser);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectDiag());
            RefreshFocuserListCommand = new RelayCommand(RefreshFocuserList, o => !(Focuser?.Connected == true));
            MoveFocuserInSmallCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal((int)Math.Round(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize / -2d)), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserInLargeCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize * -5), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserOutSmallCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal((int)Math.Round(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize / 2d)), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserOutLargeCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize * 5), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            MoveFocuserCommand = new AsyncCommand<int>(() => MoveFocuserInternal(TargetPosition), (p) => FocuserInfo.Connected && !FocuserInfo.IsMoving);
            HaltFocuserCommand = new RelayCommand((object o) => _cancelMove?.Cancel());
            ToggleTempCompCommand = new RelayCommand(ToggleTempComp);

            updateTimer = new DeviceUpdateTimer(
                GetFocuserValues,
                UpdateFocuserValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshFocuserList(null);
            };

            progress = new Progress<ApplicationStatus>(p => {
                p.Source = this.Title;
                this.applicationStatusMediator.StatusUpdate(p);
            });
        }

        private void ToggleTempComp(object obj) {
            if (FocuserInfo.Connected) {
                Focuser.TempComp = (bool)obj;
            }
        }

        public void ToggleTempComp(bool tempComp) {
            if (FocuserInfo.Connected) {
                Focuser.TempComp = tempComp;
            }
        }

        private void HaltFocuser() {
            Logger.Info("Halting Focuser");
            if (Focuser?.Connected == true) {
                try {
                    Focuser.Halt();
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        private CancellationTokenSource _cancelMove;

        private Task<int> MoveFocuserInternal(int position) {
            _cancelMove?.Dispose();
            _cancelMove = new CancellationTokenSource();
            return MoveFocuser(position, _cancelMove.Token);
        }

        private Task<int> MoveFocuserRelativeInternal(int position) {
            _cancelMove?.Dispose();
            _cancelMove = new CancellationTokenSource();
            return MoveFocuserRelative(position, _cancelMove.Token);
        }

        public async Task<int> MoveFocuser(int position, CancellationToken ct) {
            int pos = -1;

            await Task.Run(async () => {
                try {
                    using (ct.Register(() => HaltFocuser())) {
                        var tempComp = false;
                        if (Focuser.TempCompAvailable && Focuser.TempComp) {
                            tempComp = true;
                            ToggleTempComp(false);
                        }

                        Logger.Info($"Moving Focuser to position {position}");
                        progress.Report(new ApplicationStatus() { Status = string.Format(Locale.Loc.Instance["LblFocuserMoveToPosition"], position) });

                        while (Focuser.Position != position) {
                            FocuserInfo.IsMoving = true;
                            ct.ThrowIfCancellationRequested();
                            await Focuser.Move(position, ct);
                        }

                        FocuserInfo.Position = this.Position;
                        pos = this.Position;
                        ToggleTempComp(tempComp);
                        BroadcastFocuserInfo();

                        //Wait for focuser to settle
                        if (profileService.ActiveProfile.FocuserSettings.FocuserSettleTime > 0) {
                            FocuserInfo.IsSettling = true;
                            await Utility.Utility.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.FocuserSettings.FocuserSettleTime), ct, progress, Locale.Loc.Instance["LblSettle"]);
                        }
                    }
                } catch (OperationCanceledException) {
                } finally {
                    FocuserInfo.IsSettling = false;
                    FocuserInfo.IsMoving = false;
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                }
            });
            return pos;
        }

        public async Task<int> MoveFocuserRelative(int offset, CancellationToken ct) {
            int pos = -1;
            if (Focuser?.Connected == true) {
                pos = this.Position + offset;
                pos = await MoveFocuser(pos, ct);
            }
            return pos;
        }

        private CancellationTokenSource _cancelChooseFocuserSource;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private IFocuser GetBacklashCompensationFocuser(IProfileService profileService, IFocuser focuser) {
            switch (profileService.ActiveProfile.FocuserSettings.BacklashCompensationModel) {
                case Utility.Enum.BacklashCompensationModel.ABSOLUTE:
                    return new AbsoluteBacklashCompensationDecorator(profileService, focuser);

                case Utility.Enum.BacklashCompensationModel.OVERSHOOT:
                    return new OvershootBacklashCompensationDecorator(profileService, focuser);

                default:
                    return focuser;
            }
        }

        private async Task<bool> ChooseFocuser() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (FocuserChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.FocuserSettings.Id = FocuserChooserVM.SelectedDevice.Id;
                    return false;
                }

                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblConnecting"] });

                var focuser = GetBacklashCompensationFocuser(profileService, (IFocuser)FocuserChooserVM.SelectedDevice);
                _cancelChooseFocuserSource?.Dispose();
                _cancelChooseFocuserSource = new CancellationTokenSource();
                if (focuser != null) {
                    try {
                        var connected = await focuser?.Connect(_cancelChooseFocuserSource.Token);
                        _cancelChooseFocuserSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.Focuser = focuser;

                            FocuserInfo = new FocuserInfo {
                                Connected = true,
                                IsMoving = Focuser.IsMoving,
                                Name = Focuser.Name,
                                Position = this.Position,
                                StepSize = Focuser.StepSize,
                                TempCompAvailable = Focuser.TempCompAvailable,
                                TempComp = Focuser.TempComp,
                                Temperature = Focuser.Temperature
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblFocuserConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            TargetPosition = this.Position;
                            profileService.ActiveProfile.FocuserSettings.Id = Focuser.Id;

                            Logger.Info($"Successfully connected Focuser. Id: {Focuser.Id} Name: {Focuser.Name} Driver Version: {Focuser.DriverVersion}");

                            return true;
                        } else {
                            FocuserInfo.Connected = false;
                            this.Focuser = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (FocuserInfo.Connected) { await Disconnect(); }
                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
        }

        private void CancelChooseFocuser(object o) {
            _cancelChooseFocuserSource?.Cancel();
        }

        private Dictionary<string, object> GetFocuserValues() {
            Dictionary<string, object> focuserValues = new Dictionary<string, object>();
            focuserValues.Add(nameof(FocuserInfo.Connected), _focuser?.Connected ?? false);
            focuserValues.Add(nameof(FocuserInfo.Position), this?.Position ?? 0);
            focuserValues.Add(nameof(FocuserInfo.Temperature), _focuser?.Temperature ?? double.NaN);
            focuserValues.Add(nameof(FocuserInfo.IsMoving), _focuser?.IsMoving ?? false);
            focuserValues.Add(nameof(FocuserInfo.TempComp), _focuser?.TempComp ?? false);
            return focuserValues;
        }

        private void UpdateFocuserValues(Dictionary<string, object> focuserValues) {
            object o = null;
            focuserValues.TryGetValue(nameof(FocuserInfo.Connected), out o);
            FocuserInfo.Connected = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(FocuserInfo.Position), out o);
            FocuserInfo.Position = (int)(o ?? 0);

            focuserValues.TryGetValue(nameof(FocuserInfo.Temperature), out o);
            FocuserInfo.Temperature = (double)(o ?? double.NaN);

            focuserValues.TryGetValue(nameof(FocuserInfo.IsMoving), out o);
            FocuserInfo.IsMoving = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(FocuserInfo.TempComp), out o);
            FocuserInfo.TempComp = (bool)(o ?? false);

            BroadcastFocuserInfo();
        }

        private FocuserInfo focuserInfo;

        public FocuserInfo FocuserInfo {
            get {
                if (focuserInfo == null) {
                    focuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
                }
                return focuserInfo;
            }
            set {
                focuserInfo = value;
                RaisePropertyChanged();
            }
        }

        private void BroadcastFocuserInfo() {
            this.focuserMediator.Broadcast(FocuserInfo);
        }

        private int _targetPosition;

        public int TargetPosition {
            get {
                return _targetPosition;
            }
            set {
                _targetPosition = value;
                RaisePropertyChanged();
            }
        }

        public int Position {
            get {
                if (Focuser != null) {
                    return Focuser.Position;
                } else {
                    return 0;
                }
            }
        }

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblDisconnectFocuser"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public async Task Disconnect() {
            if (updateTimer != null) {
                await updateTimer.Stop();
            }
            Focuser?.Disconnect();
            Focuser = null;
            FocuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
            BroadcastFocuserInfo();
            RaisePropertyChanged(nameof(Focuser));
            Logger.Info("Disconnected Focuser");
        }

        public void RefreshFocuserList(object obj) {
            FocuserChooserVM.GetEquipment();
        }

        public Task<bool> Connect() {
            return ChooseFocuser();
        }

        public FocuserInfo GetDeviceInfo() {
            return FocuserInfo;
        }

        private IFocuser _focuser;

        public IFocuser Focuser {
            get {
                return _focuser;
            }
            private set {
                _focuser = value;
                RaisePropertyChanged();
            }
        }

        private FocuserChooserVM _focuserChooserVM;

        public FocuserChooserVM FocuserChooserVM {
            get {
                if (_focuserChooserVM == null) {
                    _focuserChooserVM = new FocuserChooserVM(profileService);
                    _focuserChooserVM.GetEquipment();
                }
                return _focuserChooserVM;
            }
            set {
                _focuserChooserVM = value;
            }
        }

        private DeviceUpdateTimer updateTimer;
        private IFocuserMediator focuserMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private IProgress<ApplicationStatus> progress;

        public ICommand RefreshFocuserListCommand { get; private set; }

        public IAsyncCommand ChooseFocuserCommand { get; private set; }
        public ICommand CancelChooseFocuserCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand MoveFocuserCommand { get; private set; }
        public ICommand MoveFocuserInSmallCommand { get; private set; }
        public ICommand MoveFocuserInLargeCommand { get; private set; }
        public ICommand MoveFocuserOutSmallCommand { get; private set; }
        public ICommand MoveFocuserOutLargeCommand { get; private set; }
        public ICommand HaltFocuserCommand { get; private set; }
        public ICommand ToggleTempCompCommand { get; private set; }
    }
}