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
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.Utility.WindowService;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.Telescope {

    internal class TelescopeVM : DockableVM, ITelescopeVM {

        public TelescopeVM(IProfileService profileService, ITelescopeMediator telescopeMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;
            Title = "LblTelescope";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["TelescopeSVG"];

            ChooseTelescopeCommand = new AsyncCommand<bool>(() => ChooseTelescope());
            CancelChooseTelescopeCommand = new RelayCommand(CancelChooseTelescope);
            DisconnectCommand = new RelayCommand(DisconnectTelescope);
            ParkCommand = new AsyncCommand<bool>(ParkTelescope);
            UnparkCommand = new RelayCommand(UnparkTelescope);
            SlewToCoordinatesCommand = new RelayCommand(SlewToCoordinates);
            RefreshTelescopeListCommand = new RelayCommand(RefreshTelescopeList);

            MoveCommand = new RelayCommand(Move);
            StopMoveCommand = new RelayCommand(StopMove);
            StopSlewCommand = new RelayCommand(StopSlew);

            updateTimer = new DeviceUpdateTimer(
                GetTelescopeValues,
                UpdateTelescopeValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshTelescopeList(null);
            };
        }

        public bool SendToSnapPort(bool start) {
            if (TelescopeInfo.Connected) {
                string command = string.Empty;
                if (start) {
                    command = profileService.ActiveProfile.TelescopeSettings.SnapPortStart;
                } else {
                    command = profileService.ActiveProfile.TelescopeSettings.SnapPortStop;
                }
                _telescope?.SendCommandString(command);
                return true;
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblTelescopeNotConnectedForCommand"]);
                return false;
            }
        }

        private void RefreshTelescopeList(object obj) {
            TelescopeChooserVM.GetEquipment();
        }

        private async Task<bool> ParkTelescope() {
            return await Task.Run<bool>(() => { Telescope.Park(); return true; });
        }

        private void UnparkTelescope(object o) {
            Telescope.Unpark();
        }

        //private DispatcherTimer _updateTelescope;

        private ITelescope _telescope;

        public ITelescope Telescope {
            get {
                return _telescope;
            }
            private set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private TelescopeChooserVM _telescopeChooserVM;

        public TelescopeChooserVM TelescopeChooserVM {
            get {
                if (_telescopeChooserVM == null) {
                    _telescopeChooserVM = new TelescopeChooserVM(profileService);
                }
                return _telescopeChooserVM;
            }
            set {
                _telescopeChooserVM = value;
            }
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseTelescope() {
            await ss.WaitAsync();
            try {
                Disconnect();
                updateTimer?.Stop();

                if (TelescopeChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.TelescopeSettings.Id = TelescopeChooserVM.SelectedDevice.Id;
                    return false;
                }

                this.applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );

                var telescope = (ITelescope)TelescopeChooserVM.SelectedDevice;
                _cancelChooseTelescopeSource?.Dispose();
                _cancelChooseTelescopeSource = new CancellationTokenSource();
                if (telescope != null) {
                    try {
                        var connected = await telescope?.Connect(_cancelChooseTelescopeSource.Token);
                        _cancelChooseTelescopeSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            Telescope = telescope;

                            if (Telescope.SiteLatitude != profileService.ActiveProfile.AstrometrySettings.Latitude || Telescope.SiteLongitude != profileService.ActiveProfile.AstrometrySettings.Longitude) {
                                var syncVM = new TelescopeLatLongSyncVM(
                                    Telescope.CanSetSiteLatLong,
                                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                                    profileService.ActiveProfile.AstrometrySettings.Longitude,
                                    Telescope.SiteLatitude,
                                    Telescope.SiteLongitude
                                );
                                await WindowService.ShowDialog(syncVM, Locale.Loc.Instance["LblSyncLatLong"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

                                if (syncVM.Mode == TelescopeLatLongSyncVM.LatLongSyncMode.NINA) {
                                    profileService.ChangeLatitude(Telescope.SiteLatitude);
                                    profileService.ChangeLongitude(Telescope.SiteLongitude);
                                } else if (syncVM.Mode == TelescopeLatLongSyncVM.LatLongSyncMode.TELESCOPE) {
                                    Telescope.SiteLatitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
                                    Telescope.SiteLongitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
                                }
                            }

                            TelescopeInfo = new TelescopeInfo {
                                AltitudeString = Telescope.AltitudeString,
                                AtPark = Telescope.AtPark,
                                AzimuthString = Telescope.AzimuthString,
                                Connected = true,
                                Coordinates = Telescope.Coordinates,
                                Declination = Telescope.Declination,
                                DeclinationString = Telescope.DeclinationString,
                                HoursToMeridianString = Telescope.HoursToMeridianString,
                                Name = Telescope.Name,
                                RightAscension = Telescope.RightAscension,
                                RightAscensionString = Telescope.RightAscensionString,
                                SiderealTime = Telescope.SiderealTime,
                                SiderealTimeString = Telescope.SiderealTimeString,
                                SiteElevation = Telescope.SiteElevation,
                                SiteLatitude = Telescope.SiteLatitude,
                                SiteLongitude = Telescope.SiteLongitude,
                                TimeToMeridianFlip = Telescope.TimeToMeridianFlip,
                                SideOfPier = Telescope.SideOfPier,
                                Tracking = Telescope.Tracking
                            };

                            BroadcastTelescopeInfo();

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            Notification.ShowSuccess(Locale.Loc.Instance["LblTelescopeConnected"]);
                            profileService.ActiveProfile.TelescopeSettings.Id = Telescope.Id;
                            return true;
                        } else {
                            Telescope = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (telescope?.Connected == true) { Disconnect(); }
                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                this.applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private void BroadcastTelescopeInfo() {
            telescopeMediator.Broadcast(TelescopeInfo);
        }

        private TelescopeInfo telescopeInfo;

        public TelescopeInfo TelescopeInfo {
            get {
                if (telescopeInfo == null) {
                    telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
                }
                return telescopeInfo;
            }
            set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        private DeviceUpdateTimer updateTimer;

        private void UpdateTelescopeValues(Dictionary<string, object> telescopeValues) {
            object o = null;
            telescopeValues.TryGetValue(nameof(TelescopeInfo.Connected), out o);
            TelescopeInfo.Connected = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.AltitudeString), out o);
            TelescopeInfo.AltitudeString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.AzimuthString), out o);
            TelescopeInfo.AzimuthString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.DeclinationString), out o);
            TelescopeInfo.DeclinationString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.RightAscensionString), out o);
            TelescopeInfo.RightAscensionString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiderealTimeString), out o);
            TelescopeInfo.SiderealTimeString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiderealTime), out o);
            TelescopeInfo.SiderealTime = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.Declination), out o);
            TelescopeInfo.Declination = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.RightAscension), out o);
            TelescopeInfo.RightAscension = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.HoursToMeridianString), out o);
            TelescopeInfo.HoursToMeridianString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.AtPark), out o);
            TelescopeInfo.AtPark = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiteLatitude), out o);
            TelescopeInfo.SiteLatitude = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiteLongitude), out o);
            TelescopeInfo.SiteLongitude = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SiteElevation), out o);
            TelescopeInfo.SiteElevation = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.Tracking), out o);
            TelescopeInfo.Tracking = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(Coordinates), out o);
            TelescopeInfo.Coordinates = (Coordinates)(o ?? null);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.TimeToMeridianFlip), out o);
            TelescopeInfo.TimeToMeridianFlip = (double)(o ?? double.NaN);

            telescopeValues.TryGetValue(nameof(TelescopeInfo.SideOfPier), out o);
            TelescopeInfo.SideOfPier = (PierSide)(o ?? new PierSide());

            BroadcastTelescopeInfo();
        }

        private Dictionary<string, object> GetTelescopeValues() {
            Dictionary<string, object> telescopeValues = new Dictionary<string, object>();

            telescopeValues.Add(nameof(TelescopeInfo.Connected), _telescope?.Connected ?? false);
            telescopeValues.Add(nameof(TelescopeInfo.AtPark), _telescope?.AtPark ?? false);
            telescopeValues.Add(nameof(TelescopeInfo.Tracking), _telescope?.Tracking ?? false);

            telescopeValues.Add(nameof(TelescopeInfo.AltitudeString), _telescope?.AltitudeString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.AzimuthString), _telescope?.AzimuthString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.DeclinationString), _telescope?.DeclinationString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.RightAscensionString), _telescope?.RightAscensionString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.SiderealTimeString), _telescope?.SiderealTimeString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.RightAscension), _telescope?.RightAscension ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.Declination), _telescope?.Declination ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.SiderealTime), _telescope?.SiderealTime ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.HoursToMeridianString), _telescope?.HoursToMeridianString ?? string.Empty);
            telescopeValues.Add(nameof(TelescopeInfo.SiteLongitude), _telescope?.SiteLongitude ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.SiteLatitude), _telescope?.SiteLatitude ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.SiteElevation), _telescope?.SiteElevation ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.Coordinates), _telescope?.Coordinates ?? null);
            telescopeValues.Add(nameof(TelescopeInfo.TimeToMeridianFlip), _telescope?.TimeToMeridianFlip ?? double.NaN);
            telescopeValues.Add(nameof(TelescopeInfo.SideOfPier), _telescope?.SideOfPier ?? new PierSide());

            return telescopeValues;
        }

        private void CancelChooseTelescope(object o) {
            _cancelChooseTelescopeSource?.Cancel();
        }

        private CancellationTokenSource _cancelChooseTelescopeSource;

        private void DisconnectTelescope(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Telescope?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {
            updateTimer?.Stop();
            Telescope?.Disconnect();
            Telescope = null;
            TelescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
            BroadcastTelescopeInfo();
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            if (TelescopeInfo.Connected) {
                Telescope.MoveAxis(axis, rate);
            }
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            if (TelescopeInfo.Connected) {
                Telescope.PulseGuide(direction, duration);
            }
        }

        public bool Sync(double ra, double dec) {
            if (TelescopeInfo.Connected) {
                return Telescope.Sync(ra, dec);
            } else {
                return false;
            }
        }

        private void Move(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                MoveAxis(TelescopeAxes.Primary, -Telescope.MovingRate);
            }
            if (cmd == "O") {
                MoveAxis(TelescopeAxes.Primary, Telescope.MovingRate);
            }
            if (cmd == "N") {
                MoveAxis(TelescopeAxes.Secondary, Telescope.MovingRate);
            }
            if (cmd == "S") {
                MoveAxis(TelescopeAxes.Secondary, -Telescope.MovingRate);
            }
        }

        private void StopMove(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                MoveAxis(TelescopeAxes.Primary, 0);
            }
            if (cmd == "O") {
                MoveAxis(TelescopeAxes.Primary, 0);
            }
            if (cmd == "N") {
                MoveAxis(TelescopeAxes.Secondary, 0);
            }
            if (cmd == "S") {
                MoveAxis(TelescopeAxes.Secondary, 0);
            }
        }

        private void StopSlew(object obj) {
            Telescope.StopSlew();
        }

        private int _targetDeclinationDegrees;

        public int TargetDeclinationDegrees {
            get {
                return _targetDeclinationDegrees;
            }

            set {
                _targetDeclinationDegrees = value;
                RaisePropertyChanged();
            }
        }

        private int _targetDeclinationMinutes;

        public int TargetDeclinationMinutes {
            get {
                return _targetDeclinationMinutes;
            }

            set {
                _targetDeclinationMinutes = value;
                RaisePropertyChanged();
            }
        }

        private double _targetDeclinationSeconds;

        public double TargetDeclinationSeconds {
            get {
                return _targetDeclinationSeconds;
            }

            set {
                _targetDeclinationSeconds = value;
                RaisePropertyChanged();
            }
        }

        private int _targetRightAscencionHours;

        public int TargetRightAscencionHours {
            get {
                return _targetRightAscencionHours;
            }

            set {
                _targetRightAscencionHours = value;
                RaisePropertyChanged();
            }
        }

        private int _targetRightAscencionMinutes;

        public int TargetRightAscencionMinutes {
            get {
                return _targetRightAscencionMinutes;
            }

            set {
                _targetRightAscencionMinutes = value;
                RaisePropertyChanged();
            }
        }

        private double _targetRightAscencionSeconds;
        private ITelescopeMediator telescopeMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public double TargetRightAscencionSeconds {
            get {
                return _targetRightAscencionSeconds;
            }

            set {
                _targetRightAscencionSeconds = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
            coords = coords.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);
            if (Telescope?.Connected == true) {
                await Task.Run(() => {
                    Telescope.SlewToCoordinates(coords.RA, coords.Dec);
                });
                await Utility.Utility.Delay(TimeSpan.FromSeconds(profileService.ActiveProfile.TelescopeSettings.SettleTime), new CancellationToken());
                return true;
            } else {
                return false;
            }
        }

        private void SlewToCoordinates(Coordinates coords) {
            coords = coords.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);
            if (Telescope?.Connected == true) {
                Telescope.SlewToCoordinatesAsync(coords.RA, coords.Dec);
            }
        }

        private void SlewToCoordinates(object obj) {
            var targetRightAscencion = TargetRightAscencionHours + Astrometry.ArcminToDegree(TargetRightAscencionMinutes) + Astrometry.ArcsecToDegree(TargetRightAscencionSeconds);
            var targetDeclination = TargetDeclinationDegrees + Astrometry.ArcminToDegree(TargetDeclinationMinutes) + Astrometry.ArcsecToDegree(TargetDeclinationSeconds);

            var coords = new Coordinates(targetRightAscencion, targetDeclination, Epoch.J2000, Coordinates.RAType.Hours);
            SlewToCoordinates(coords);
        }

        public bool MeridianFlip(Coordinates targetCoordinates) {
            if (TelescopeInfo.Connected) {
                return Telescope.MeridianFlip(targetCoordinates);
            } else {
                return false;
            }
        }

        public bool SetTracking(bool tracking) {
            if (TelescopeInfo.Connected) {
                Telescope.Tracking = tracking;
                return Telescope.Tracking;
            } else {
                return false;
            }
        }

        public Task<bool> Connect() {
            return ChooseTelescope();
        }

        public TelescopeInfo GetDeviceInfo() {
            return TelescopeInfo;
        }

        public Task<bool> SlewToCoordinatesAsync(TopocentricCoordinates coordinates) {
            var transformed = coordinates.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);
            return this.SlewToCoordinatesAsync(transformed);
        }

        public ICommand SlewToCoordinatesCommand { get; private set; }

        public IAsyncCommand ChooseTelescopeCommand { get; private set; }
        public ICommand CancelChooseTelescopeCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand MoveCommand { get; private set; }

        public ICommand StopMoveCommand { get; private set; }

        public IAsyncCommand ParkCommand { get; private set; }

        public ICommand UnparkCommand { get; private set; }

        public ICommand StopSlewCommand { get; private set; }

        public ICommand RefreshTelescopeListCommand { get; private set; }
    }
}