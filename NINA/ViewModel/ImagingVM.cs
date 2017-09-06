﻿using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using nom.tam.fits;
using nom.tam.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static NINA.Model.CaptureSequence;
using System.ComponentModel;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyTelescope;
using NINA.Utility.Notification;

namespace NINA.ViewModel {
    class ImagingVM : DockableVM {

        public ImagingVM() : base() {

            Title = "LblImaging";
            ContentId = nameof(ImagingVM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

            SnapExposureDuration = 1;
            SnapCommand = new AsyncCommand<bool>(() => CaptureImage(new Progress<string>(p => Status = p)));
            CancelSnapCommand = new RelayCommand(CancelCaptureImage);
            
            ImageControl = new ImageControlVM();
            
            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsync(async (object o) => {
                var args = (object[])o;                
                ICollection<CaptureSequence> seq = (ICollection<CaptureSequence>)args[0];
                bool save = (bool)args[1];
                CancellationTokenSource token = (CancellationTokenSource)args[2];
                IProgress<string> progress = (IProgress<string>)args[3];
                await StartSequence(seq, save, token, progress);
            }, AsyncMediatorMessages.StartSequence);

            Mediator.Instance.RegisterAsync(async (object o) => {
                var args = (object[])o;
                CaptureSequence seq = (CaptureSequence)args[0];
                bool save = (bool)args[1];
                IProgress<string> progress = (IProgress<string>)args[2];
                CancellationTokenSource token = (CancellationTokenSource)args[3];
                
                await CaptureImage(seq, save, progress, token);
            }, AsyncMediatorMessages.CaptureImage);

            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);

            Mediator.Instance.Register((object o) => {
                Telescope = (ITelescope)o;
            }, MediatorMessages.TelescopeChanged);
            
        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();

                Mediator.Instance.Notify(MediatorMessages.StatusUpdate, _status);
            }
        }

        private PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private bool _loop;
        public bool Loop {
            get {
                return _loop;
            }
            set {
                _loop = value;
                RaisePropertyChanged();
            }

        }

        private bool _snapSave;
        public bool SnapSave {
            get {
                return _snapSave;
            }
            set {
                _snapSave = value;
                RaisePropertyChanged();
            }

        }

        private ICamera _cam;
        public ICamera Cam {
            get {
                return _cam;
            } set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        private ITelescope _telescope;
        public ITelescope Telescope {
            get {
                return _telescope;
            } set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private double _snapExposureDuration;
        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private int _exposureSeconds; 
        public int ExposureSeconds {
            get {
                return _exposureSeconds;
            }
            set {
                _exposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        private String _expStatus;
        public String ExpStatus {
            get {
                return _expStatus;
            }

            set {
                _expStatus = value;                
                RaisePropertyChanged();
            }
        }

        private bool _isExposing; 
        public bool IsExposing {
            get {
                return _isExposing;
            } set {
                _isExposing = value;
                RaisePropertyChanged();

                Mediator.Instance.Notify(MediatorMessages.IsExposingUpdate, _isExposing);
            }
        }        
        
        public IAsyncCommand SnapCommand { get; private set; }

        public ICommand CancelSnapCommand { get; private set; }

        private void CancelCaptureImage(object o) {
            _captureImageToken?.Cancel();
        }

        CancellationTokenSource _captureImageToken;

        private async Task ChangeFilter(CaptureSequence seq, CancellationTokenSource tokenSource, IProgress<string> progress) {

            progress.Report(ExposureStatus.FILTERCHANGE);
            await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.ChangeFilterWheelPosition, new object[] { seq.FilterType?.Position, tokenSource });            
        }

        private void SetBinning(CaptureSequence seq) {
            if (seq.Binning == null) {
                Cam.SetBinning(1, 1);
            }
            else {
                Cam.SetBinning(seq.Binning.X, seq.Binning.Y);
            }
        }

        private async Task Capture(CaptureSequence seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            IsExposing = true;
            try {
                double duration = seq.ExposureTime;
                progress.Report(string.Format(ExposureStatus.EXPOSING, 0, duration));
                bool isLight = false;
                if (Cam.HasShutter) {
                    isLight = true;
                }
                Cam.StartExposure(duration, isLight);
                ExposureSeconds = 1;
                progress.Report(string.Format(ExposureStatus.EXPOSING, 1, duration));
                /* Wait for Capture */
                if (duration >= 1) {
                    await Task.Run(async () => {
                        do {
                            await Task.Delay(1000, tokenSource.Token);
                            tokenSource.Token.ThrowIfCancellationRequested();
                            ExposureSeconds += 1;
                            progress.Report(string.Format(ExposureStatus.EXPOSING, ExposureSeconds, duration));
                        } while ((ExposureSeconds < duration) && Cam.Connected);
                    });
                }
                tokenSource.Token.ThrowIfCancellationRequested();
            } catch (System.OperationCanceledException ex) {
                Logger.Trace(ex.Message);
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
            } finally {
                IsExposing = false;
            }
            
            
        }

        private async Task<ImageArray> Download(CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.DOWNLOADING);
            return await Cam.DownloadExposure(tokenSource);
        }

        



        private async Task<bool> Save(CaptureSequence seq, ushort framenr,  CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.SAVING);           

            await ImageControl.SaveToDisk(seq, framenr, tokenSource, progress);            
                        
            return true;
        }

        private async Task<bool> Dither(CaptureSequence seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (seq.Dither && ((seq.ExposureCount % seq.DitherAmount) == 0)) {
                progress.Report(ExposureStatus.DITHERING);
                await PHD2Client.Dither();

                progress.Report(ExposureStatus.SETTLING);
                var time = 0;
                await Task.Run<bool>(async () => {
                    while (PHD2Client.IsDithering) {                        
                        await Task.Delay(100, tokenSource.Token);
                        time += 100;

                        if(time > 20000) {
                            //Failsafe when phd is not sending settlingdone message
                            Notification.ShowWarning(Locale.Loc.Instance["LblPHD2NoSettleDone"]);
                            PHD2Client.IsDithering = false;
                        }
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }
                    return true;
                });
            }
            tokenSource.Token.ThrowIfCancellationRequested();
            return true;
        }
                
        public  async Task<bool> StartSequence(ICollection<CaptureSequence> sequence, bool bSave, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (Cam?.Connected != true) {
                Notification.ShowWarning(Locale.Loc.Instance["LblNoCameraConnected"]);
                return false;
            }
            if (IsExposing) {
                Notification.ShowWarning(Locale.Loc.Instance["LblCameraBusy"]);
                return false;
            }

            return await Task.Run<bool>(async () => {


                try {


                    ushort framenr = 1;
                    foreach (CaptureSequence seq in sequence) {

                        Mediator.Instance.Notify(MediatorMessages.ActiveSequenceChanged, seq);                        

                        if (seq.Dither && !PHD2Client.Connected) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblPHD2DitherButNotConnected"]);
                        }

                        while (seq.ExposureCount > 0) {


                            /*Change Filter*/
                            await ChangeFilter(seq, tokenSource, progress);

                            if (Cam?.Connected != true) {
                                tokenSource.Cancel();
                                throw new OperationCanceledException();
                            }

                            /*Set Camera Binning*/
                            SetBinning(seq);

                            if (Cam?.Connected != true) {
                                tokenSource.Cancel();
                                throw new OperationCanceledException();
                            }



                            await CheckMeridianFlip(seq, tokenSource, progress);


                            /*Capture*/
                            await Capture(seq, tokenSource, progress);

                            if (Cam?.Connected != true) {
                                tokenSource.Cancel();
                                throw new OperationCanceledException();
                            }

                            /*Download Image */
                            ImageArray arr = await Download(tokenSource, progress);
                            if (arr == null) {
                                tokenSource.Cancel();
                                throw new OperationCanceledException();
                            }

                            ImageControl.ImgArr = arr;

                            /*Prepare Image for UI*/
                            progress.Report(ImagingVM.ExposureStatus.PREPARING);

                            await ImageControl.PrepareImage(progress, tokenSource);


                            if (Cam?.Connected != true) {
                                tokenSource.Cancel();
                                throw new OperationCanceledException();
                            }

                            /*Save to disk*/
                            if (bSave) {
                                await Save(seq, framenr, tokenSource, progress);
                            }

                            /*Dither*/
                            await Dither(seq, tokenSource, progress);

                            if (Cam?.Connected != true) {
                                tokenSource.Cancel();
                                throw new OperationCanceledException();
                            }

                            seq.ExposureCount -= 1;
                            framenr++;
                        }
                    }
                } catch (System.OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                    if (Cam?.Connected == true) {
                        Cam.AbortExposure();
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                    if (Cam?.Connected == true) {
                        Cam.AbortExposure();
                    }
                } finally {
                    progress.Report(ExposureStatus.IDLE);
                }
                return true;
            });
                     
        }

        /// <summary>
        /// Checks if auto meridian flip should be considered and executes it
        /// 1) Compare next exposure length with time to meridian - If exposure length is greater than time to flip the system will wait
        /// 2) Pause PHD2
        /// 3) Execute the flip
        /// 4) If recentering is enabled, platesolve current position, sync and recenter to old target position
        /// 5) Resume PHD2
        /// </summary>
        /// <param name="seq">Current Sequence row</param>
        /// <param name="tokenSource">cancel token</param>
        /// <param name="progress">progress reporter</param>
        /// <returns></returns>
        private async Task CheckMeridianFlip(CaptureSequence seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if(Settings.AutoMeridianFlip) {
                if(Telescope?.Connected == true) {

                    if(Telescope.TimeToMeridianFlip < (seq.ExposureTime / 60 / 60)) {
                        int remainingtime = (int)(Telescope.TimeToMeridianFlip * 60 * 60);
                        Notification.ShowInformation(Locale.Loc.Instance["LblMeridianFlipInit"], TimeSpan.FromSeconds(remainingtime));
                        do {
                            progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", remainingtime));
                            await Task.Delay(1000, tokenSource.Token);
                            remainingtime = remainingtime - 1;
                        } while (remainingtime > 0);
                        
                    
                        progress.Report("Pausing PHD2");
                        await PHD2Client.Pause(true);

                        var coords = Telescope.Coordinates;

                        progress.Report("Executing Meridian Flip");
                        var flipsuccess = Telescope.MeridianFlip();

                        //Let scope settle 
                        await Task.Delay(TimeSpan.FromSeconds(Settings.MeridianFlipSettleTime), tokenSource.Token);

                        if (flipsuccess) {
                            if(Settings.RecenterAfterFlip) { 
                                progress.Report("Initializing Platesolve");

                                //todo needs to be solve until error < x                                

                                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.SolveWithCapture, new object[] { null , progress, tokenSource });

                                
                                progress.Report("Sync and Reslew");
                                Mediator.Instance.Notify(MediatorMessages.SyncronizeTelescope, null);
                                Telescope.SlewToCoordinates(coords.RA, coords.Dec);                                
                            }

                            progress.Report("Resuming PHD2");
                            await PHD2Client.AutoSelectStar();
                            await Task.Delay(TimeSpan.FromSeconds(5), tokenSource.Token);
                            await PHD2Client.Pause(false);

                            var time = 0;
                            while (PHD2Client.Paused) {
                                await Task.Delay(500, tokenSource.Token);
                                time += 500;
                                if (time > 20000) {
                                    //Failsafe when phd is not sending resume message
                                    Notification.ShowWarning(Locale.Loc.Instance["LblPHD2NoResume"]/*, ToastNotifications.NotificationsSource.NeverEndingNotification*/);                                                                
                                    break;
                                }
                                tokenSource.Token.ThrowIfCancellationRequested();
                            }

                            await Task.Delay(TimeSpan.FromSeconds(Settings.MeridianFlipSettleTime), tokenSource.Token);
                        }
                    }
                }
            }
            
        }

        


        ImageControlVM _imageControl;
        public ImageControlVM ImageControl {
            get { return _imageControl; }
            set { _imageControl = value; RaisePropertyChanged(); }
        }

        

        private Model.MyFilterWheel.FilterInfo _snapFilter;
        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }
            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        private BinningMode _snapBin;
        public BinningMode SnapBin {
            get {
                if(_snapBin == null) {
                    _snapBin = new BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

       

        public async Task<bool> CaptureImage(IProgress<string> progress) {
            _captureImageToken = new CancellationTokenSource();
            if (IsExposing) {
                Notification.ShowWarning(Locale.Loc.Instance["LblCameraBusy"]);
                return false;
            } else {
                do {
                    List<CaptureSequence> seq = new List<CaptureSequence>();
                    seq.Add(new CaptureSequence(SnapExposureDuration, ImageTypes.SNAP, SnapFilter, SnapBin, 1));
                    await StartSequence(seq,  SnapSave, _captureImageToken, progress);
                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop);
                return true;
            }
        }

        public async Task<bool> CaptureImage(CaptureSequence seq, bool bsave, IProgress<string> progress, CancellationTokenSource token) {
            if (IsExposing) {
                Notification.ShowWarning(Locale.Loc.Instance["LblCameraBusy"]);
                return false;
            }
            else {
                var list = new List<CaptureSequence>() { seq };
                return await StartSequence(list, bsave, token, progress);
            }
        }

        public static class ExposureStatus {
            public const string EXPOSING = "Exposing {0}/{1}...";
            public const string DOWNLOADING = "Downloading...";
            public const string FILTERCHANGE = "Switching Filter...";
            public const string PREPARING = "Preparing...";
            public const string CALCHFR = "Calculating HFR...";
            public const string SAVING = "Saving...";
            public const string IDLE = "";
            public const string DITHERING = "Dithering...";
            public const string SETTLING = "Settling...";
        }
    }
}
