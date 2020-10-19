#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static NINA.Model.CaptureSequence;

namespace NINA.ViewModel.Imaging {

    internal class AnchorableSnapshotVM : DockableVM, ICameraConsumer, IAnchorableSnapshotVM {
        private CancellationTokenSource _captureImageToken;
        private CancellationTokenSource _liveViewCts;
        private bool _liveViewEnabled;
        private BinningMode _snapBin;
        private bool _snapSubSample;

        private ApplicationStatus _status;

        private IApplicationStatusMediator applicationStatusMediator;
        private CameraInfo cameraInfo;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IProgress<ApplicationStatus> progress;

        public AnchorableSnapshotVM(
                IProfileService profileService,
                IImagingMediator imagingMediator,
                ICameraMediator cameraMediator,
                IApplicationStatusMediator applicationStatusMediator,
                IImageSaveMediator imageSaveMediator) : base(profileService) {
            Title = "LblImaging";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];
            this.applicationStatusMediator = applicationStatusMediator;
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            progress = new Progress<ApplicationStatus>(p => Status = p);
            SnapCommand = new AsyncCommand<bool>(async () => {
                cameraMediator.RegisterCaptureBlock(this);
                try {
                    var result = await SnapImage(progress);
                    return result;
                } finally {
                    cameraMediator.ReleaseCaptureBlock(this);
                }
            }, (o) => cameraMediator.IsFreeToCapture(this) && !LiveViewEnabled);
            CancelSnapCommand = new RelayCommand(CancelSnapImage);
            StartLiveViewCommand = new AsyncCommand<bool>(async () => {
                cameraMediator.RegisterCaptureBlock(this);
                try {
                    var result = await StartLiveView();
                    return result;
                } finally {
                    cameraMediator.ReleaseCaptureBlock(this);
                }
            }, (o) => cameraMediator.IsFreeToCapture(this) && !IsLooping);
            StopLiveViewCommand = new RelayCommand(StopLiveView);
            SnapFilter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Name == profileService.ActiveProfile.SnapShotControlSettings.Filter?.Name);

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                SnapFilter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Name == profileService.ActiveProfile.SnapShotControlSettings.Filter?.Name);
            };
        }

        /// <summary>
        /// Backwards compatible ContentId due to refactoring
        /// </summary>
        public new string ContentId {
            get => typeof(ImagingVM).Name;
        }

        public CameraInfo CameraInfo {
            get {
                return cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            }
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public ICommand CancelSnapCommand { get; private set; }

        private bool isLooping;

        public bool IsLooping {
            get => isLooping;
            set {
                isLooping = value;
                RaisePropertyChanged();
            }
        }

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool Loop {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.Loop;
            }
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Loop = value;
                RaisePropertyChanged();
            }
        }

        public BinningMode SnapBin {
            get {
                if (_snapBin == null) {
                    _snapBin = new BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand SnapCommand { get; private set; }

        public double SnapExposureDuration {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.ExposureDuration;
            }

            set {
                profileService.ActiveProfile.SnapShotControlSettings.ExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private Model.MyFilterWheel.FilterInfo snapFilter;

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return snapFilter;
            }
            set {
                snapFilter = value;
                profileService.ActiveProfile.SnapShotControlSettings.Filter = value;
                RaisePropertyChanged();
            }
        }

        public int SnapGain {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.Gain;
            }
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Gain = value;
                RaisePropertyChanged();
            }
        }

        public bool SnapSave {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.Save;
            }
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Save = value;
                RaisePropertyChanged();
            }
        }

        public bool SnapSubSample {
            get {
                return _snapSubSample;
            }
            set {
                _snapSubSample = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand StartLiveViewCommand { get; private set; }

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public ICommand StopLiveViewCommand { get; private set; }

        private void CancelSnapImage(object o) {
            _captureImageToken?.Cancel();
        }

        private async Task<bool> StartLiveView() {
            _liveViewCts?.Dispose();
            LiveViewEnabled = true;
            _liveViewCts = new CancellationTokenSource();
            try {
                await this.imagingMediator.StartLiveView(_liveViewCts.Token);
            } catch (OperationCanceledException) {
            } finally {
                LiveViewEnabled = false;
            }

            return true;
        }

        private void StopLiveView(object o) {
            _liveViewCts?.Cancel();
            LiveViewEnabled = false;
        }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
        }

        public async Task<bool> SnapImage(IProgress<ApplicationStatus> progress) {
            _captureImageToken?.Dispose();
            _captureImageToken = new CancellationTokenSource();

            try {
                var success = true;
                if (Loop) IsLooping = true;
                Task<IRenderedImage> prepareTask = null;
                do {
                    var seq = new CaptureSequence(SnapExposureDuration, ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
                    seq.EnableSubSample = SnapSubSample;
                    seq.Gain = SnapGain;

                    var exposureData = await imagingMediator.CaptureImage(seq, _captureImageToken.Token, progress);
                    var imageData = await exposureData.ToImageData(_captureImageToken.Token);

                    if (prepareTask?.Status < TaskStatus.RanToCompletion) {
                        await prepareTask;
                    }
                    prepareTask = imagingMediator.PrepareImage(exposureData, new PrepareImageParameters(), _captureImageToken.Token);
                    if (SnapSave) {
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSavingImage"] });

                        await imageSaveMediator.Enqueue(imageData, prepareTask, progress, _captureImageToken.Token);
                    }

                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop && success);
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            } finally {
                //if (_imageProcessingTask != null) {
                //    await _imageProcessingTask;
                //}
                IsLooping = false;
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }

            return true;
        }

        public void UpdateDeviceInfo(CameraInfo cameraStatus) {
            CameraInfo = cameraStatus;
        }
    }
}