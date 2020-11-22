#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.WindowService;
using NINA.ViewModel;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Model.MyGuider.PHD2 {

    public class PHD2Guider : BaseINPC, IGuider {

        public PHD2Guider(IProfileService profileService, IWindowServiceFactory windowServiceFactory) {
            this.profileService = profileService;
            this.windowServiceFactory = windowServiceFactory;

            OpenPHD2DiagCommand = new RelayCommand(OpenPHD2FileDiag);
        }

        private readonly IProfileService profileService;
        private readonly IWindowServiceFactory windowServiceFactory;

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private PhdEventVersion _version;

        public string Name => "PHD2";

        public string Id => "PHD2_Single";

        public PhdEventVersion Version {
            get {
                return _version;
            }
            set {
                _version = value;
                RaisePropertyChanged();
            }
        }

        private ImageSource _image;

        public ImageSource Image {
            get {
                return _image;
            }
            set {
                _image = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventAppState _appState;

        public PhdEventAppState AppState {
            get {
                return _appState;
            }
            set {
                _appState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(State));
            }
        }

        private PhdEventSettling _settling;

        public PhdEventSettling Settling {
            get {
                return _settling;
            }
            set {
                _settling = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventSettleDone _settleDone;

        public PhdEventSettleDone SettleDone {
            get {
                return _settleDone;
            }
            set {
                _settleDone = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventGuidingDithered _guidingDithered;

        public PhdEventGuidingDithered GuidingDithered {
            get {
                return _guidingDithered;
            }
            set {
                _guidingDithered = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource _clientCTS;

        private static object lockobj = new object();

        private bool _connected;

        public bool Connected {
            get {
                return _connected;
            }
            private set {
                lock (lockobj) {
                    _connected = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _isDithering;

        private double _pixelScale;

        public double PixelScale {
            get {
                return _pixelScale;
            }
            set {
                _pixelScale = value;
                RaisePropertyChanged();
            }
        }

        public string State {
            get {
                return AppState?.State ?? string.Empty;
            }
        }

        public bool HasSetupDialog => !Connected;

        public string Category => "Guiders";

        public string Description => "PHD2 Guider";

        public string DriverInfo => "PHD2 Guider";

        public string DriverVersion => "1.0";

        /*private async Task<TcpClient> ConnectClient() {
            var client = new TcpClient();
            await client.ConnectAsync(Settings.PHD2ServerUrl, Settings.PHD2ServerPort);
            return client;
        }*/
        private TaskCompletionSource<bool> _tcs;

        public async Task<bool> Connect(CancellationToken token) {
            _tcs = new TaskCompletionSource<bool>();
            var startedPHD2 = await StartPHD2Process();
#pragma warning disable 4014
            Task.Run(RunListener);
#pragma warning restore 4014
            bool connected = await _tcs.Task;

            try {
                if (startedPHD2 && connected) {
                    await Task.Run(ConnectPHD2Equipment);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    var loopMsg = new Phd2Loop();
                    await SendMessage(loopMsg);
                }

                var msg = new Phd2GetPixelScale();
                var resp = await SendMessage(msg);
                if (resp.result != null)
                    PixelScale = double.Parse(resp.result.ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }

            return connected;
        }

        public async Task<bool> Dither(CancellationToken ct) {
            if (Connected) {
                _isDithering = true;

                var ditherMsg = new Phd2Dither() {
                    Parameters = new Phd2DitherParameter() {
                        Amount = profileService.ActiveProfile.GuiderSettings.DitherPixels,
                        RaOnly = profileService.ActiveProfile.GuiderSettings.DitherRAOnly,
                        Settle = new Phd2Settle() {
                            Pixels = profileService.ActiveProfile.GuiderSettings.SettlePixels,
                            Time = profileService.ActiveProfile.GuiderSettings.SettleTime,
                            Timeout = profileService.ActiveProfile.GuiderSettings.SettleTimeout
                        }
                    }
                };

                var ditherMsgResponse = await SendMessage(ditherMsg);
                if (ditherMsgResponse.error != null) {
                    /* Dither failed */
                    _isDithering = false;
                    return false;
                }
                await Task.Run<bool>(async () => {
                    var elapsed = new TimeSpan();
                    while (_isDithering == true) {
                        elapsed += await Utility.Utility.Delay(500, ct);

                        if (elapsed.TotalSeconds > 120) {
                            //Failsafe when phd is not sending settlingdone message
                            Notification.ShowWarning(Locale.Loc.Instance["LblGuiderNoSettleDone"]);
                            _isDithering = false;
                        }
                    }
                    return true;
                });
            }
            return true;
        }

        public async Task<bool> Pause(bool pause, CancellationToken ct) {
            if (Connected) {
                var msg = new Phd2Pause() { Parameters = new bool[] { true } };
                await SendMessage(msg);

                if (pause) {
                    var elapsed = new TimeSpan();
                    while (!(AppState.State == PhdAppState.PAUSED)) {
                        elapsed += await Utility.Utility.Delay(500, ct);
                    }
                } else {
                    var elapsed = new TimeSpan();
                    while ((AppState.State == PhdAppState.PAUSED)) {
                        elapsed += await Utility.Utility.Delay(500, ct);
                        if (elapsed.TotalSeconds > 60) {
                            //Failsafe when phd is not sending resume message
                            Notification.ShowWarning(Locale.Loc.Instance["LblGuiderNoResume"]/*, ToastNotifications.NotificationsSource.NeverEndingNotification*/);
                            break;
                        }
                    }
                }
            }
            return true;
        }

        private void CheckPhdError(PhdMethodResponse m) {
            if (m.error != null) {
                Notification.ShowError("PHDError: " + m.error.message + "\n CODE: " + m.error.code);
                Logger.Warning("PHDError: " + m.error.message + " CODE: " + m.error.code);
            }
        }

        public async Task<bool> AutoSelectGuideStar() {
            if (Connected) {
                var state = await GetAppState();
                if (state != PhdAppState.LOOPING) {
                    var loopMsg = new Phd2Loop();
                    await SendMessage(loopMsg);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                var findStarMsg = new Phd2FindStar();

                await SendMessage(findStarMsg);

                return true;
            }
            return false;
        }

        private async Task<string> GetAppState(
            int receiveTimeout = 0) {
            var msg = new Phd2GetAppState();
            var appStateResponse = await SendMessage(
                msg,
                receiveTimeout);
            return appStateResponse?.result?.ToString();
        }

        private Task<bool> WaitForAppState(
            string targetState,
            CancellationToken ct,
            int receiveTimeout = 0) {
            return Task.Run(async () => {
                var state = await GetAppState();
                while (state != targetState) {
                    await Task.Delay(1000, ct);
                    state = await GetAppState();
                }
                return true;
            });
        }

        public async Task<bool> StartGuiding(bool forceCalibration, CancellationToken ct) {
            var autoRetry = profileService.ActiveProfile.GuiderSettings.AutoRetryStartGuiding;
            var retryAfterSeconds = profileService.ActiveProfile.GuiderSettings.AutoRetryStartGuidingTimeoutSeconds * 1000;

            if (!Connected)
                return false;

            string state = await GetAppState();
            if (state == PhdAppState.GUIDING)
                return true;

            if (state == PhdAppState.CALIBRATING)
                return await WaitForAppState(PhdAppState.GUIDING, ct);

            async Task<bool> TryStartGuideCommand() {
                var guideMsg = new Phd2Guide() {
                    Parameters = new Phd2GuideParameter() {
                        Settle = new Phd2Settle() {
                            Pixels = profileService.ActiveProfile.GuiderSettings.SettlePixels,
                            Time = profileService.ActiveProfile.GuiderSettings.SettleTime,
                            Timeout = profileService.ActiveProfile.GuiderSettings.SettleTimeout
                        },
                        Recalibrate = forceCalibration
                    }
                };

                var guideMsgResponse = await SendMessage(guideMsg);
                return guideMsgResponse.error == null;
            }

            if (!autoRetry) {
                return await TryStartGuideCommand()
                    && await WaitForAppState(PhdAppState.GUIDING, ct);
            }

            while (!ct.IsCancellationRequested) {
                if (!await TryStartGuideCommand())
                    return false;

                using (var cancelOnTimeoutOrParent = CancellationTokenSource.CreateLinkedTokenSource(ct)) {
                    var timeout = Task.Delay(
                        retryAfterSeconds,
                        cancelOnTimeoutOrParent.Token);
                    var guidingHasBegun = WaitForAppState(
                        PhdAppState.GUIDING,
                        cancelOnTimeoutOrParent.Token);

                    if ((await Task.WhenAny(timeout, guidingHasBegun)) == guidingHasBegun) {
                        return await guidingHasBegun;
                    }
                    cancelOnTimeoutOrParent.Cancel();
                    await Task.Delay(100, ct); // 100ms sleep between retries

                    await StopGuiding(ct); // used to visual inspect that the guider is in the stopped state before retrying.
                }
            }
            return false;
        }

        public async Task<bool> StopGuiding(CancellationToken token) {
            if (!Connected) {
                return false;
            }
            try {
                string state = await GetAppState(3000);
                if (state == PhdAppState.STOPPED) {
                    return true;
                }
                var msg = new Phd2StopCapture();
                var stopCapture = await SendMessage(
                    msg,
                    10000); // triage: reported deadlock hanging of phd2+nina - 10s timeout

                if (stopCapture == null || stopCapture.error != null) {
                    /*stop capture failed */
                    return false;
                }

                return await WaitForAppState(
                    PhdAppState.STOPPED,
                    token,
                    10000);  // triage: reported deadlock hanging of phd2+nina - 10s timeout
            } catch (IOException ee) // communication error with phd2
              {
                Logger.Error(ee);
                return false;
            }
        }

        public bool CanClearCalibration {
            get => true;
        }

        public async Task<bool> ClearCalibration(CancellationToken ct) {
            if (Connected) {
                var clearMessage = new Phd2ClearCalibration() {
                    Parameters = new string[] { "Both" }
                };
                var clearGuidance = await SendMessage(clearMessage, 10000);

                if (clearGuidance == null || clearGuidance.error != null) {
                    return false;
                }

                await Task.Delay(100, ct); // give time for PHD2 to clear the guidance
            }
            return true;
        }

        private async Task<PhdMethodResponse> SendMessage(Phd2Method msg, int receiveTimeout = 0) {
            using (var client = new TcpClient()) {
                client.ReceiveTimeout = receiveTimeout;
                await client.ConnectAsync(
                    profileService.ActiveProfile.GuiderSettings.PHD2ServerUrl,
                    profileService.ActiveProfile.GuiderSettings.PHD2ServerPort);
                var stream = client.GetStream();
                var data = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }) + Environment.NewLine);

                await stream.WriteAsync(data, 0, data.Length);

                using (var reader = new StreamReader(stream, Encoding.UTF8)) {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null) {
                        var o = JObject.Parse(line);
                        string phdevent = "";
                        var t = o.GetValue("id");
                        if (t != null) {
                            phdevent = t.ToString();
                        }
                        if (phdevent == msg.Id) {
                            var response = o.ToObject<PhdMethodResponse>();
                            CheckPhdError(response);
                            return response;
                        }
                    }
                }
            }

            return null;
        }

        public void Disconnect() {
            _clientCTS?.Cancel();
        }

        private void ProcessEvent(string phdevent, JObject message) {
            switch (phdevent) {
                case "Resumed": {
                        break;
                    }
                case "Version": {
                        Version = message.ToObject<PhdEventVersion>();
                        break;
                    }
                case "AppState": {
                        AppState = message.ToObject<PhdEventAppState>();
                        break;
                    }
                case "GuideStep": {
                        AppState = new PhdEventAppState() { State = "Guiding" };
                        var step = message.ToObject<PhdEventGuideStep>();
                        GuideEvent?.Invoke(this, step);
                        break;
                    }
                case "GuidingDithered": {
                        SettleDone = null;
                        GuidingDithered = message.ToObject<PhdEventGuidingDithered>();
                        break;
                    }
                case "Settling": {
                        SettleDone = null;
                        Settling = message.ToObject<PhdEventSettling>();
                        break;
                    }
                case "SettleDone": {
                        GuidingDithered = null;
                        Settling = null;
                        _isDithering = false;
                        SettleDone = message.ToObject<PhdEventSettleDone>();
                        if (SettleDone.Error != null) {
                            Notification.ShowError("PHD2 Error: " + SettleDone.Error);
                        }
                        break;
                    }
                case "Paused": {
                        AppState = new PhdEventAppState() { State = "Paused" };
                        break;
                    }
                case "StartCalibration": {
                        AppState = new PhdEventAppState() { State = "Calibrating" };
                        break;
                    }
                case "LoopingExposures": {
                        AppState = new PhdEventAppState() { State = "Looping" };
                        break;
                    }
                case "LoopingExposuresStopped": {
                        AppState = new PhdEventAppState() { State = "Stopped" };
                        break;
                    }
                case "StarLost": {
                        AppState = new PhdEventAppState() { State = "LostLock" };
                        break;
                    }
                case "LockPositionLost": {
                        break;
                    }
                default: {
                        break;
                    }
            }
        }

        private static TcpState GetState(TcpClient tcpClient) {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
            return foo != null ? foo.State : TcpState.Unknown;
        }

        private async Task ConnectPHD2Equipment() {
            var msg = new Phd2SetConnected() {
                Parameters = new bool[] { true }
            };
            var connectMsg = await SendMessage(msg);
            if (connectMsg.error != null) {
                Notification.ShowWarning(Locale.Loc.Instance["LblPhd2FailedEquipmentConnection"]);
            }
        }

        private async Task<bool> StartPHD2Process() {
            // if phd2 is not running start it
            try {
                if (Process.GetProcessesByName("phd2").Length == 0) {
                    if (!File.Exists(profileService.ActiveProfile.GuiderSettings.PHD2Path)) {
                        throw new FileNotFoundException();
                    }

                    var process = Process.Start(profileService.ActiveProfile.GuiderSettings.PHD2Path);
                    process?.WaitForInputIdle();

                    await Task.Delay(2000);

                    //Try to read the appstate and retry for 5 times. On slow systems the startup of phd can take a couple of seconds.
                    string appState = string.Empty;
                    int retries = 5;
                    do {
                        try {
                            retries--;
                            appState = await GetAppState(2000);
                        } catch (Exception) {
                        }
                    } while (string.IsNullOrEmpty(appState) && retries > 0);

                    return !string.IsNullOrEmpty(appState);
                }
            } catch (FileNotFoundException ex) {
                Logger.Error(Locale.Loc.Instance["LblPhd2PathNotFound"], ex);
                Notification.ShowError(Locale.Loc.Instance["LblPhd2PathNotFound"]);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblPhd2StartProcessError"]);
            }

            return false;
        }

        private async Task RunListener() {
            JsonLoadSettings jls = new JsonLoadSettings() { LineInfoHandling = LineInfoHandling.Ignore, CommentHandling = CommentHandling.Ignore };
            _clientCTS?.Dispose();
            _clientCTS = new CancellationTokenSource();
            using (var client = new TcpClient()) {
                try {
                    await client.ConnectAsync(profileService.ActiveProfile.GuiderSettings.PHD2ServerUrl,
                        profileService.ActiveProfile.GuiderSettings.PHD2ServerPort);
                    Connected = true;
                    _tcs.TrySetResult(true);

                    using (NetworkStream s = client.GetStream()) {
                        while (true) {
                            var state = GetState(client);
                            if (state == TcpState.CloseWait) {
                                throw new Exception(Locale.Loc.Instance["LblPhd2ServerConnectionLost"]);
                            }

                            var message = string.Empty;
                            while (s.DataAvailable) {
                                byte[] response = new byte[1024];
                                await s.ReadAsync(response, 0, response.Length, _clientCTS.Token);
                                message += System.Text.Encoding.ASCII.GetString(response);
                            }

                            foreach (string line in message.Split(new[] { Environment.NewLine },
                                StringSplitOptions.None)) {
                                if (!string.IsNullOrEmpty(line) && !line.StartsWith("\0")) {
                                    JObject o = JObject.Parse(line, jls);
                                    JToken t = o.GetValue("Event");
                                    string phdevent = "";
                                    if (t != null) {
                                        phdevent = t.ToString();
                                        ProcessEvent(phdevent, o);
                                    }
                                }
                            }

                            await Task.Delay(TimeSpan.FromMilliseconds(500), _clientCTS.Token);
                        }
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("PHD2 Error: " + ex.Message);
                    throw;
                } finally {
                    _isDithering = false;
                    AppState = new PhdEventAppState() { State = "" };
                    PixelScale = 0.0d;
                    Connected = false;
                    _tcs.TrySetResult(false);
                    PHD2ConnectionLost?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void SetupDialog() {
            var windowService = windowServiceFactory.Create();
            windowService.ShowDialog(this, Locale.Loc.Instance["LblPHD2Setup"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public RelayCommand OpenPHD2DiagCommand { get; set; }

        private void OpenPHD2FileDiag(object o) {
            var dialog = OptionsVM.GetFilteredFileDialog(profileService.ActiveProfile.GuiderSettings.PHD2Path, "phd2.exe", "PHD2|phd2.exe");
            if (dialog.ShowDialog() == true) {
                this.profileService.ActiveProfile.GuiderSettings.PHD2Path = dialog.FileName;
            }
        }

        public event EventHandler PHD2ConnectionLost;

        public event EventHandler<IGuideStep> GuideEvent;

        public class PhdMethodResponse {
            public string jsonrpc;
            public object result;
            public PhdError error;
            public int id;
        }

        public class PhdImageResult {
            public int frame;
            public int width;
            public int height;
            public double[] star_pos;
            public string pixels;
        }

        public class PhdError {
            public int code;
            public string message;
        }

        [DataContract]
        public class PhdEvent : BaseINPC, IGuideEvent {

            [DataMember]
            public string Event { get; set; }

            [DataMember]
            public string TimeStamp { get; set; }

            [DataMember]
            public string Host { get; set; }

            [DataMember]
            public int Inst { get; set; }
        }

        public class PhdEventVersion : PhdEvent {
            public string PHDVersion;
            public string PHDSubver;
            public int MsgVersion;
        }

        public class PhdEventLockPositionSet : PhdEvent {
            public int X;
            public int Y;
        }

        public class PhdEventCalibrationComplete : PhdEvent {
            public string Mount;
        }

        public class PhdEventStarSelected : PhdEvent {
            public int X;
            public int Y;
        }

        public class PhdEventStartGuiding : PhdEvent {
        }

        public class PhdEventPaused : PhdEvent {
        }

        public class PhdEventStartCalibration : PhdEvent {
            public string Mount;
        }

        public class PhdEventAppState : PhdEvent, IGuiderAppState {
            private string state;

            public string State {
                get {
                    return state;
                }

                set {
                    state = value;
                    RaisePropertyChanged();
                }
            }
        }

        public sealed class PhdAppState {
            public static readonly string STOPPED = "Stopped";
            public static readonly string SELECTED = "Selected";
            public static readonly string CALIBRATING = "Calibrating";
            public static readonly string GUIDING = "Guiding";
            public static readonly string LOSTLOCK = "LostLock";
            public static readonly string PAUSED = "Paused";
            public static readonly string LOOPING = "Looping";
        }

        public class PhdEventCalibrationFailed : PhdEvent {
            public string Reason;
        }

        public class PhdEventCalibrationDataFlipped : PhdEvent {
            public string Mount;
        }

        public class PhdEventLoopingExposures : PhdEvent {
            public int Frame;
        }

        public class PhdEventLoopingExposuresStopped : PhdEvent {
        }

        public class PhdEventSettling : PhdEvent {
            public int Distance;
            public int Time;
            public int SettleTime;
        }

        public class PhdEventSettleDone : PhdEvent {
            public int Status;
            public string Error;
        }

        public class PhdEventStarLost : PhdEvent {
            public int Frame;
            public int Time;
            public int StarMass;
            public int SNR;
            public int AvgDist;
            public int ErrorCode;
            public int Status;
        }

        public class PhdEventGuidingStopped : PhdEvent {
        }

        public class PhdEventResumed : PhdEvent {
        }

        [DataContract]
        public class PhdEventGuideStep : PhdEvent, IGuideStep {

            [DataMember]
            private double frame;

            [DataMember]
            private double time;

            [DataMember]
            private string mount;

            [DataMember]
            private double dx;

            [DataMember]
            private double dy;

            [DataMember]
            private double rADistanceRaw;

            [DataMember]
            private double decDistanceRaw;

            [DataMember]
            private double raDistanceDisplay;

            [DataMember]
            private double decDistanceDisplay;

            [DataMember]
            private double rADistanceGuide;

            [DataMember]
            private double decDistanceGuide;

            [DataMember]
            private double raDistanceGuideDisplay;

            [DataMember]
            private double decDistanceGuideDisplay;

            [DataMember]
            private double rADuration;

            [DataMember]
            private string rADirection;

            [DataMember]
            private double dECDuration;

            [DataMember]
            private string decDirection;

            [DataMember]
            private double starMass;

            [DataMember]
            private double sNR;

            [DataMember]
            private double avgDist;

            [DataMember]
            private bool rALimited;

            [DataMember]
            private bool decLimited;

            [DataMember]
            private double errorCode;

            public PhdEventGuideStep() {
            }

            [DataMember]
            public double RADistanceGuideDisplay {
                get {
                    return raDistanceGuideDisplay;
                }
                set {
                    raDistanceGuideDisplay = value;
                }
            }

            [DataMember]
            public double DecDistanceGuideDisplay {
                get {
                    return decDistanceGuideDisplay;
                }
                set {
                    decDistanceGuideDisplay = value;
                }
            }

            [DataMember]
            public double Frame {
                get {
                    return frame;
                }

                set {
                    frame = value;
                }
            }

            [DataMember]
            public double Time {
                get {
                    return time;
                }

                set {
                    time = DateTime.UtcNow
                   .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                   .TotalSeconds;
                }
            }

            [DataMember]
            public string Mount {
                get {
                    return mount;
                }

                set {
                    mount = value;
                }
            }

            [DataMember]
            public double Dx {
                get {
                    return dx;
                }

                set {
                    dx = value;
                }
            }

            [DataMember]
            public double Dy {
                get {
                    return dy;
                }

                set {
                    dy = value;
                }
            }

            [DataMember]
            public double RADistanceRaw {
                get {
                    return -rADistanceRaw;
                }

                set {
                    rADistanceRaw = value;
                }
            }

            [DataMember]
            public double DECDistanceRaw {
                get {
                    return decDistanceRaw;
                }

                set {
                    decDistanceRaw = value;
                }
            }

            [DataMember]
            public double RADistanceGuide {
                get {
                    return rADistanceGuide;
                }

                set {
                    rADistanceGuide = value;
                    RADistanceGuideDisplay = RADistanceGuide;
                }
            }

            [DataMember]
            public double DECDistanceGuide {
                get {
                    return decDistanceGuide;
                }

                set {
                    decDistanceGuide = value;
                    DecDistanceGuideDisplay = DECDistanceRaw;
                }
            }

            [DataMember]
            public double RADuration {
                get {
                    if (RADirection == "East") {
                        return -rADuration;
                    } else {
                        return rADuration;
                    }
                }

                set {
                    rADuration = value;
                }
            }

            [DataMember]
            public string RADirection {
                get {
                    return rADirection;
                }

                set {
                    rADirection = value;
                }
            }

            [DataMember]
            public double DECDuration {
                get {
                    if (DECDirection == "South") {
                        return -dECDuration;
                    } else {
                        return dECDuration;
                    }
                }

                set {
                    dECDuration = value;
                }
            }

            [DataMember]
            public string DECDirection {
                get {
                    return decDirection;
                }

                set {
                    decDirection = value;
                }
            }

            [DataMember]
            public double StarMass {
                get {
                    return starMass;
                }

                set {
                    starMass = value;
                }
            }

            [DataMember]
            public double SNR {
                get {
                    return sNR;
                }

                set {
                    sNR = value;
                }
            }

            [DataMember]
            public double AvgDist {
                get {
                    return avgDist;
                }

                set {
                    avgDist = value;
                }
            }

            [DataMember]
            public bool RALimited {
                get {
                    return rALimited;
                }

                set {
                    rALimited = value;
                }
            }

            [DataMember]
            public bool DecLimited {
                get {
                    return decLimited;
                }

                set {
                    decLimited = value;
                }
            }

            [DataMember]
            public double ErrorCode {
                get {
                    return errorCode;
                }

                set {
                    errorCode = value;
                }
            }

            public IGuideStep Clone() {
                return (PhdEventGuideStep)this.MemberwiseClone();
            }
        }

        public class PhdEventGuidingDithered : PhdEvent {
            public int dx;
            public int dy;
        }

        public class PhdEventLockPositionLost : PhdEvent {
        }

        public class PhdEventAlert : PhdEvent {
            public string Msg;
            public string Type;
        }
    }
}