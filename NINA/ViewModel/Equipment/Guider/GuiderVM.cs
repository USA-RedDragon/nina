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
using NINA.Model.MyGuider;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using NINA.Utility.Notification;

namespace NINA.ViewModel.Equipment.Guider {

    internal class GuiderVM : DockableVM, IGuiderVM {

        public GuiderVM(IProfileService profileService, IGuiderMediator guiderMediator, ICameraMediator cameraMediator, IApplicationStatusMediator applicationStatusMediator, ITelescopeMediator telescopeMediator) : base(profileService) {
            Title = "LblGuider";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["GuiderSVG"];

            this.guiderMediator = guiderMediator;
            this.guiderMediator.RegisterHandler(this);

            this.applicationStatusMediator = applicationStatusMediator;

            ConnectGuiderCommand = new AsyncCommand<bool>(Connect);

            CancelConnectGuiderCommand = new RelayCommand(CancelConnectGuider);

            GuiderChooserVM = new GuiderChooserVM(profileService, cameraMediator, telescopeMediator);

            DisconnectGuiderCommand = new RelayCommand((object o) => Disconnect(), (object o) => Guider?.Connected == true);
            ClearGraphCommand = new RelayCommand((object o) => ResetGraphValues());

            GuideStepsHistory = new GuideStepsHistory(HistorySize, GuiderScale, GuiderMaxY);

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                GuiderChooserVM.GetEquipment();
            };
        }

        public IGuiderChooserVM GuiderChooserVM { get; set; }

        /// <summary>
        /// Starts recording RMS until StopRMSRecording is called
        /// </summary>
        /// <returns>Handle for the recording rms session</returns>
        public Guid StartRMSRecording() {
            var handle = Guid.NewGuid();
            var rms = new RMS();
            rms.SetScale(GuideStepsHistory.PixelScale);
            recordedRMS.Add(handle, rms);
            return handle;
        }

        /// <summary>
        /// Stops and returns RMS for the given rms session handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>recorded RMS</returns>
        public RMS StopRMSRecording(Guid handle) {
            if (recordedRMS.ContainsKey(handle)) {
                var rms = recordedRMS[handle];
                recordedRMS.Remove(handle);
                return rms;
            } else {
                return null;
            }
        }

        public bool GuiderIsSynchronized => Guider is SynchronizedPHD2Guider && Guider.Connected;

        public async Task<bool> AutoSelectGuideStar(CancellationToken token) {
            if (Guider?.Connected == true) {
                var result = await Guider?.AutoSelectGuideStar();
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                return result;
            } else {
                return false;
            }
        }

        public int HistorySize {
            get {
                return profileService.ActiveProfile.GuiderSettings.PHD2HistorySize;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.PHD2HistorySize = value;
                GuideStepsHistory.HistorySize = value;
                RaisePropertyChanged();
            }
        }

        private static Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

        private void ResetGraphValues() {
            GuideStepsHistory.Clear();
        }

        public async Task<bool> Connect() {
            ResetGraphValues();

            bool connected = false;

            try {
                if (Guider != null) {
                    Guider.PropertyChanged -= Guider_PropertyChanged;
                    Guider.GuideEvent -= Guider_GuideEvent;
                }
                _cancelConnectGuiderSource?.Dispose();
                _cancelConnectGuiderSource = new CancellationTokenSource();
                Guider = GuiderChooserVM.SelectedGuider;
                Guider.PropertyChanged += Guider_PropertyChanged;
                connected = await Guider.Connect();
                _cancelConnectGuiderSource.Token.ThrowIfCancellationRequested();

                if (connected) {
                    Guider.GuideEvent += Guider_GuideEvent;

                    GuiderInfo = new GuiderInfo {
                        Connected = connected
                    };
                    BroadcastGuiderInfo();
                    Notification.ShowSuccess(Locale.Loc.Instance["LblGuiderConnected"]);
                    RaisePropertyChanged(nameof(Guider));
                    profileService.ActiveProfile.GuiderSettings.GuiderName = Guider.Name;
                }
            } catch (OperationCanceledException) {
                Guider.PropertyChanged -= Guider_PropertyChanged;
                Guider?.Disconnect();
                GuiderInfo = new GuiderInfo {
                    Connected = false
                };
                BroadcastGuiderInfo();
            }

            return connected;
        }

        private void Guider_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "PixelScale") {
                GuideStepsHistory.PixelScale = Guider.PixelScale;
            }

            if (e.PropertyName == nameof(IGuider.Connected)) {
                GuiderInfo.Connected = Guider.Connected;
                BroadcastGuiderInfo();
            }
        }

        private void Guider_GuideEvent(object sender, IGuideStep e) {
            try {
                var step = e;

                GuideStepsHistory.AddGuideStep(step);

                foreach (RMS rms in recordedRMS.Values) {
                    rms.AddDataPoint(step.RADistanceRaw, step.DECDistanceRaw);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public Task Disconnect() {
            if (Guider != null) {
                Guider.PropertyChanged -= Guider_PropertyChanged;
                Guider.GuideEvent -= Guider_GuideEvent;
            }
            Guider?.Disconnect();
            GuiderInfo = DeviceInfo.CreateDefaultInstance<GuiderInfo>();
            BroadcastGuiderInfo();
            return Task.CompletedTask;
        }

        private GuiderInfo guiderInfo;

        public GuiderInfo GuiderInfo {
            get {
                if (guiderInfo == null) {
                    guiderInfo = DeviceInfo.CreateDefaultInstance<GuiderInfo>();
                }
                return guiderInfo;
            }
            set {
                guiderInfo = value;
                RaisePropertyChanged();
            }
        }

        private void BroadcastGuiderInfo() {
            this.guiderMediator.Broadcast(GuiderInfo);
        }

        public GuiderScaleEnum GuiderScale {
            get {
                return profileService.ActiveProfile.GuiderSettings.PHD2GuiderScale;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.PHD2GuiderScale = value;
                GuideStepsHistory.Scale = value;
            }
        }

        public double GuiderMaxY {
            get {
                return profileService.ActiveProfile.GuiderSettings.MaxY;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.MaxY = value;
                GuideStepsHistory.MaxY = value;
            }
        }

        public async Task<bool> StartGuiding(CancellationToken token) {
            if (Guider?.Connected == true) {
                return await Guider.StartGuiding(token);
            } else {
                return false;
            }
        }

        public async Task<bool> StopGuiding(CancellationToken token) {
            if (Guider?.Connected == true) {
                return await Guider.StopGuiding(token);
            } else {
                return false;
            }
        }

        public async Task<bool> Dither(CancellationToken token) {
            if (Guider?.Connected == true) {
                try {
                    Guider.GuideEvent -= Guider_GuideEvent;
                    applicationStatusMediator.StatusUpdate(new Model.ApplicationStatus() { Status = Locale.Loc.Instance["LblDither"], Source = Title });
                    GuideStepsHistory.AddDitherIndicator();
                    await Guider.Dither(token);
                } finally {
                    Guider.GuideEvent += Guider_GuideEvent;
                    applicationStatusMediator.StatusUpdate(new Model.ApplicationStatus() { Status = string.Empty, Source = Title });
                }

                return true;
            } else {
                await Disconnect();
                return false;
            }
        }

        public GuiderInfo GetDeviceInfo() {
            return GuiderInfo;
        }

        private GuideStepsHistory guideStepsHistory;

        public GuideStepsHistory GuideStepsHistory {
            get {
                return guideStepsHistory;
            }
            private set {
                guideStepsHistory = value;
                RaisePropertyChanged();
            }
        }

        private Dictionary<Guid, RMS> recordedRMS = new Dictionary<Guid, RMS>();

        private IGuider guider;

        public IGuider Guider {
            get => guider;
            set {
                guider = value;
                RaisePropertyChanged();
            }
        }

        private void CancelConnectGuider(object o) {
            _cancelConnectGuiderSource?.Cancel();
        }

        private IGuiderMediator guiderMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private CancellationTokenSource _cancelConnectGuiderSource;

        public ICommand ConnectGuiderCommand { get; private set; }

        public ICommand DisconnectGuiderCommand { get; private set; }

        public ICommand ClearGraphCommand { get; private set; }

        public ICommand CancelConnectGuiderCommand { get; }
    }
}