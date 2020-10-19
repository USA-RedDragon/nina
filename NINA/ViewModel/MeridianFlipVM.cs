#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.PlateSolving;
using NINA.Profile;
using NINA.Utility.WindowService;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.ViewModel.AutoFocus;
using NINA.Model.MyFilterWheel;
using System.Linq;

namespace NINA.ViewModel {

    internal class MeridianFlipVM : BaseVM {

        public MeridianFlipVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IGuiderMediator guiderMediator,
                IFocuserMediator focuserMediator,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator,
                IFilterWheelMediator filterWheelMediator) : base(profileService) {
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.filterWheelMediator = filterWheelMediator;
            AutoFocusVMFactory = new AutoFocusVMFactory(profileService, cameraMediator, filterWheelMediator, focuserMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            CancelCommand = new RelayCommand(Cancel);
        }

        private ICommand _cancelCommand;

        private IProgress<ApplicationStatus> _progress;

        private TimeSpan _remainingTime;

        private ApplicationStatus _status;

        private AutomatedWorkflow _steps;

        private Coordinates _targetCoordinates;

        private CancellationTokenSource internalCancellationToken;
        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private IFilterWheelMediator filterWheelMediator;

        public ICommand CancelCommand {
            get {
                return _cancelCommand;
            }
            set {
                _cancelCommand = value;
            }
        }

        public TimeSpan RemainingTime {
            get {
                return _remainingTime;
            }
            set {
                _remainingTime = value;
                RaisePropertyChanged();
            }
        }

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = "MeridianFlip";
                RaisePropertyChanged();

                this.applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public AutomatedWorkflow Steps {
            get {
                return _steps;
            }
            set {
                _steps = value;
                RaisePropertyChanged();
            }
        }

        private void Cancel(object obj) {
            internalCancellationToken?.Cancel();
        }

        private async Task<bool> DoFlip(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblFlippingScope"] });
            Logger.Trace($"Meridian Flip - Scope will flip to coordinates RA: {_targetCoordinates.RAString} Dec: {_targetCoordinates.DecString} Epoch: {_targetCoordinates.Epoch}");
            var flipsuccess = await telescopeMediator.MeridianFlip(_targetCoordinates);
            Logger.Trace($"Meridian Flip - Successful flip: {flipsuccess}");

            await Settle(token, progress);

            return flipsuccess;
        }

        private async Task<bool> DoMeridianFlip(Coordinates targetCoordinates, TimeSpan timeToFlip, CancellationToken requestCancellationToken) {
            try {
                var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken, this.internalCancellationToken.Token).Token;
                _targetCoordinates = targetCoordinates;
                RemainingTime = timeToFlip;

                Logger.Trace("Meridian Flip - Initializing Meridian Flip");
                Logger.Trace($"Meridian Flip - Current target coordinates RA: {_targetCoordinates.RAString} Dec: {_targetCoordinates.DecString} Epoch: {_targetCoordinates.Epoch}");

                Steps = new AutomatedWorkflow();

                Steps.Add(new WorkflowStep("StopAutoguider", Locale.Loc.Instance["LblStopAutoguider"], () => StopAutoguider(cancellationToken, _progress)));

                Steps.Add(new WorkflowStep("PassMeridian", Locale.Loc.Instance["LblPassMeridian"], () => PassMeridian(cancellationToken, _progress)));
                Steps.Add(new WorkflowStep("Flip", Locale.Loc.Instance["LblFlip"], () => DoFlip(cancellationToken, _progress)));
                if (profileService.ActiveProfile.MeridianFlipSettings.AutoFocusAfterFlip) {
                    Steps.Add(new WorkflowStep("Autofocus", Locale.Loc.Instance["LblAutoFocus"], () => AutoFocus(cancellationToken, _progress)));
                }
                if (profileService.ActiveProfile.MeridianFlipSettings.Recenter) {
                    Steps.Add(new WorkflowStep("Recenter", Locale.Loc.Instance["LblRecenter"], () => Recenter(cancellationToken, _progress)));
                }

                Steps.Add(new WorkflowStep("SelectNewGuideStar", Locale.Loc.Instance["LblSelectNewGuideStar"], () => SelectNewGuideStar(cancellationToken, _progress)));
                Steps.Add(new WorkflowStep("ResumeAutoguider", Locale.Loc.Instance["LblResumeAutoguider"], () => ResumeAutoguider(cancellationToken, _progress)));

                Steps.Add(new WorkflowStep("Settle", Locale.Loc.Instance["LblSettle"], () => Settle(cancellationToken, _progress)));

                await Steps.Process();
            } catch (Exception ex) {
                Logger.Error(ex);

                try {
                    Logger.Trace("Meridian Flip - Resuming Autoguider after meridian flip error");
                    await ResumeAutoguider(new CancellationToken(), _progress);
                } catch (Exception ex2) {
                    Logger.Error(ex2);
                    Notification.ShowError(Locale.Loc.Instance["GuiderResumeFailed"]);
                }

                Logger.Trace("Meridian Flip - Re-enable Tracking after meridian flip error");
                telescopeMediator.SetTrackingEnabled(true);
                return false;
            } finally {
                _progress.Report(new ApplicationStatus() { Status = "" });
            }
            Logger.Trace("Meridian Flip - Exiting meridian flip");
            return true;
        }

        public IAutoFocusVMFactory AutoFocusVMFactory { get; set; }

        private async Task<bool> AutoFocus(CancellationToken token, IProgress<ApplicationStatus> progress) {
            using (var autoFocus = AutoFocusVMFactory.Create()) {
                progress.Report(new ApplicationStatus { Status = Locale.Loc.Instance["LblAutoFocus"] });
                var service = WindowServiceFactory.Create();
                service.Show(autoFocus, autoFocus.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
                try {
                    FilterInfo filter = null;
                    var selectedFilter = filterWheelMediator.GetInfo()?.SelectedFilter;
                    if (selectedFilter != null) {
                        filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(x => x.Position == selectedFilter.Position).FirstOrDefault();
                    }

                    var report = await autoFocus.StartAutoFocus(filter, token, progress);
                    //history.AppendAutoFocusPoint(report);
                } finally {
                    service.DelayedClose(TimeSpan.FromSeconds(10));
                }
            }
            return true;
        }

        private async Task<bool> PassMeridian(CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Meridian Flip - Passing meridian");

            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStopTracking"] });

            Logger.Trace("Meridian Flip - Stopping tracking to pass meridian");
            telescopeMediator.SetTrackingEnabled(false);
            do {
                progress.Report(new ApplicationStatus() { Status = RemainingTime.ToString(@"hh\:mm\:ss") });

                //progress.Report(string.Format("Next exposure paused until passing meridian. Remaining time: {0} seconds", RemainingTime));
                var delta = await Utility.Utility.Delay(1000, token);

                RemainingTime -= delta;
            } while (RemainingTime.TotalSeconds >= 1);
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblResumeTracking"] });

            Logger.Trace("Meridian Flip - Resuming tracking after passing meridian");
            telescopeMediator.SetTrackingEnabled(true);

            Logger.Trace("Meridian Flip - Meridian passed");
            return true;
        }

        private async Task<bool> Recenter(CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (profileService.ActiveProfile.MeridianFlipSettings.Recenter) {
                Logger.Trace("Meridian Flip - Recenter after meridian flip");

                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblInitiatePlatesolve"] });

                var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
                var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);
                var seq = new CaptureSequence(
                    profileService.ActiveProfile.PlateSolveSettings.ExposureTime,
                    CaptureSequence.ImageTypes.SNAPSHOT,
                    profileService.ActiveProfile.PlateSolveSettings.Filter,
                    new Model.MyCamera.BinningMode(profileService.ActiveProfile.PlateSolveSettings.Binning, profileService.ActiveProfile.PlateSolveSettings.Binning),
                    1
                );
                seq.Gain = profileService.ActiveProfile.PlateSolveSettings.Gain;

                var solver = new CenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator, filterWheelMediator);
                var parameter = new CenterSolveParameter() {
                    Attempts = profileService.ActiveProfile.PlateSolveSettings.NumberOfAttempts,
                    Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                    Coordinates = _targetCoordinates,
                    DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                    FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                    MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                    PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                    ReattemptDelay = TimeSpan.FromMinutes(profileService.ActiveProfile.PlateSolveSettings.ReattemptDelay),
                    Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                    SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius,
                    Threshold = profileService.ActiveProfile.PlateSolveSettings.Threshold,
                    NoSync = profileService.ActiveProfile.TelescopeSettings.NoSync
                };
                var result = await solver.Center(seq, parameter, default, progress, token);
                if (!result.Success) {
                    Logger.Error("Center after meridian flip failed. Continuing without it");
                    Notification.ShowError(Locale.Loc.Instance["LblMeridianFlipCenterFailed"]);
                    // Recenter is best effort, so always return true
                }
            }
            return true;
        }

        private async Task<bool> ResumeAutoguider(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblResumeGuiding"] });
            Logger.Trace("Meridian Flip - Resuming Autoguider");
            var result = await this.guiderMediator.StartGuiding(false, progress, token);

            return result;
        }

        private async Task<bool> SelectNewGuideStar(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSelectGuidestar"] });
            Logger.Trace("Meridian Flip - Selecting new guide star");
            return await this.guiderMediator.AutoSelectGuideStar(token);
        }

        private async Task<bool> Settle(CancellationToken token, IProgress<ApplicationStatus> progress) {
            RemainingTime = TimeSpan.FromSeconds(profileService.ActiveProfile.MeridianFlipSettings.SettleTime);
            Logger.Trace($"Meridian Flip - Settling scope for {profileService.ActiveProfile.MeridianFlipSettings.SettleTime}");
            do {
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSettle"] + " " + RemainingTime.ToString(@"hh\:mm\:ss") });

                var delta = await Utility.Utility.Delay(1000, token);

                RemainingTime = TimeSpan.FromSeconds(RemainingTime.TotalSeconds - delta.TotalSeconds);
            } while (RemainingTime.TotalSeconds >= 1);
            return true;
        }

        public static TimeSpan GetRemainingTime(IProfileService profileService, TelescopeInfo telescopeInfo) {
            if (telescopeInfo.Connected && !double.IsNaN(telescopeInfo.TimeToMeridianFlip)) {
                var tolerance = TimeSpan.FromMinutes(1);
                var remainingExposureTime = TimeSpan.FromHours(telescopeInfo.TimeToMeridianFlip) - tolerance;
                if (profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian != 0) {
                    remainingExposureTime = remainingExposureTime - TimeSpan.FromMinutes(profileService.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian) - TimeSpan.FromMinutes(profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian);
                }
                return remainingExposureTime;
            } else {
                return TimeSpan.Zero;
            }
        }

        public static bool ShouldFlip(IProfileService profileService, double exposureTime, TelescopeInfo telescopeInfo) {
            if (telescopeInfo.Connected && !double.IsNaN(telescopeInfo.TimeToMeridianFlip)) {
                var remainingExposureTime = GetRemainingTime(profileService, telescopeInfo);

                if (profileService.ActiveProfile.MeridianFlipSettings.Enabled && !profileService.ActiveProfile.MeridianFlipSettings.UseSideOfPier) {
                    if (telescopeInfo.Connected == true) {
                        if (remainingExposureTime < TimeSpan.FromSeconds(exposureTime)) {
                            return true;
                        }
                    }
                } else if (profileService.ActiveProfile.MeridianFlipSettings.Enabled && profileService.ActiveProfile.MeridianFlipSettings.UseSideOfPier) {
                    var pierside = telescopeInfo.SideOfPier;

                    // Logging if reported side of pier is East, as users may be wondering why Meridian Flip didn't occur
                    if (pierside == PierSide.pierEast) {
                        Logger.Trace("Meridian Flip - Telescope reports East Side of Pier, Automated Flip will not be performed.");
                    }

                    if (telescopeInfo.Connected == true && pierside != PierSide.pierEast) {
                        if (remainingExposureTime < TimeSpan.FromSeconds(exposureTime)) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private async Task<bool> StopAutoguider(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStopGuiding"] });
            Logger.Trace("Meridian Flip - Stopping Autoguider");
            var result = await this.guiderMediator.StopGuiding(token);
            return result;
        }

        private IWindowServiceFactory windowServiceFactory;

        public IWindowServiceFactory WindowServiceFactory {
            get {
                if (windowServiceFactory == null) {
                    windowServiceFactory = new WindowServiceFactory();
                }
                return windowServiceFactory;
            }
            set {
                windowServiceFactory = value;
            }
        }

        /// <summary>
        /// Checks if auto meridian flip should be considered and executes it
        /// 1) Compare next exposure length with time to meridian - If exposure length is greater
        ///    than time to flip the system will wait
        /// 2) Stop Guider
        /// 3) Execute the flip
        /// 4) If recentering is enabled, platesolve current position, sync and recenter to old
        ///    target position
        /// 5) Resume Guider
        /// </summary>
        /// <param name="targetCoordinates">Reference Coordinates to slew to after flip</param>
        /// <param name="timeToFlip">Remaining time to actually flip the scope</param>
        /// <returns></returns>
        public async Task<bool> MeridianFlip(Coordinates targetCoordinates, TimeSpan timeToFlip, CancellationToken cancellationToken = default) {
            var service = WindowServiceFactory.Create();
            this.internalCancellationToken?.Dispose();
            this.internalCancellationToken = new CancellationTokenSource();
            this._progress = new Progress<ApplicationStatus>(p => Status = p);
            var flip = DoMeridianFlip(targetCoordinates, timeToFlip, cancellationToken);

            var serviceTask = service.ShowDialog(this, Locale.Loc.Instance["LblAutoMeridianFlip"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.None, CancelCommand);
            var flipResult = await flip;

            await service.Close();
            await serviceTask;
            return flipResult;
        }
    }

    public class AutomatedWorkflow : BaseINPC, ICollection<WorkflowStep> {
        private WorkflowStep _activeStep;
        private AsyncObservableLimitedSizedStack<WorkflowStep> _internalStack;

        public AutomatedWorkflow() {
            _internalStack = new AsyncObservableLimitedSizedStack<WorkflowStep>(int.MaxValue);
        }

        public WorkflowStep ActiveStep {
            get {
                return _activeStep;
            }
            set {
                _activeStep = value;
                RaisePropertyChanged();
            }
        }

        public int Count {
            get {
                return _internalStack.Count;
            }
        }

        public bool IsReadOnly {
            get {
                return _internalStack.IsReadOnly;
            }
        }

        public void Add(WorkflowStep item) {
            _internalStack.Add(item);
        }

        public void Clear() {
            _internalStack.Clear();
        }

        public bool Contains(WorkflowStep item) {
            return _internalStack.Contains(item);
        }

        public void CopyTo(WorkflowStep[] array, int arrayIndex) {
            _internalStack.CopyTo(array, arrayIndex);
        }

        public IEnumerator<WorkflowStep> GetEnumerator() {
            return _internalStack.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _internalStack.GetEnumerator();
        }

        public async Task<bool> Process() {
            var success = true;

            var item = _internalStack.First();
            do {
                ActiveStep = item.Value;
                success = await ActiveStep.Process() && success;
                item = item.Next;
            }
            while (item != null);

            return success;
        }

        public bool Remove(WorkflowStep item) {
            return _internalStack.Remove(item);
        }
    }

    public class WorkflowStep : BaseINPC {
        private Func<Task<bool>> _action;

        private bool _finished;

        private string _id;

        private string _title;

        public WorkflowStep(string id, string title, Func<Task<bool>> action) {
            Id = id;
            Title = title;
            Action = action;
        }

        public Func<Task<bool>> Action {
            get {
                return _action;
            }
            set {
                _action = value;
                RaisePropertyChanged();
            }
        }

        public bool Finished {
            get {
                return _finished;
            }
            set {
                _finished = value;
                RaisePropertyChanged();
            }
        }

        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        public string Title {
            get {
                return _title;
            }
            set {
                _title = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> Process() {
            var success = await Action();
            Finished = success;
            return success;
        }
    }
}