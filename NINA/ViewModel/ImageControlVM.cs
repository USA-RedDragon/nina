﻿using Microsoft.Win32;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.ViewModel;
using nom.tam.fits;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace NINA.ViewModel {
    public class ImageControlVM : DockableVM {

        public ImageControlVM() {
            Title = "LblImageArea";

            ContentId = nameof(ImageControlVM);
            CanClose = false;
            AutoStretch = false;
            DetectStars = false;
            ShowCrossHair = false;

            PrepareImageCommand = new AsyncCommand<bool>(() => PrepareImageHelper());
            PlateSolveImageCommand = new AsyncCommand<bool>(() => PlateSolveImage());
            CancelPlateSolveImageCommand = new RelayCommand(CancelPlateSolveImage);

            RegisterMediatorMessages();
        }

        private async Task<bool> PlateSolveImage() {
            if (Image != null) {


                _plateSolveToken = new CancellationTokenSource();
                if (!AutoStretch) {
                    AutoStretch = true;
                }
                await Mediator.Instance.RequestAsync(new PlateSolveMessage() { Progress = new Progress<string>(p => Status = p), Token = _plateSolveToken.Token });
                return true;
            } else {
                return false;
            }
        }

        private void CancelPlateSolveImage(object o) {
            _plateSolveToken?.Cancel();
        }

        private CancellationTokenSource _plateSolveToken;

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                AutoStretch = (bool)o;
            }, MediatorMessages.ChangeAutoStretch);
            Mediator.Instance.Register((object o) => {
                DetectStars = (bool)o;
            }, MediatorMessages.ChangeDetectStars);

            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);
            Mediator.Instance.Register((object o) => {
                Telescope = (ITelescope)o;
            }, MediatorMessages.TelescopeChanged);
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private ImageArray _imgArr;
        public ImageArray ImgArr {
            get {
                return _imgArr;
            }
            private set {
                _imgArr = value;
                RaisePropertyChanged();
            }
        }

        private ImageHistoryVM _imgHistoryVM;
        public ImageHistoryVM ImgHistoryVM {
            get {
                if (_imgHistoryVM == null) {
                    _imgHistoryVM = new ImageHistoryVM();
                }
                return _imgHistoryVM;
            }
            set {
                _imgHistoryVM = value;
                RaisePropertyChanged();
            }
        }

        private ImageStatisticsVM _imgStatisticsVM;
        public ImageStatisticsVM ImgStatisticsVM {
            get {
                if (_imgStatisticsVM == null) {
                    _imgStatisticsVM = new ImageStatisticsVM();
                }
                return _imgStatisticsVM;
            }
            set {
                _imgStatisticsVM = value;
                RaisePropertyChanged();
            }
        }

        private BitmapSource _image;
        public BitmapSource Image {
            get {
                return _image;
            }
            private set {
                _image = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoStretch;
        public bool AutoStretch {
            get {
                return _autoStretch;
            }
            set {
                _autoStretch = value;
                if (!_autoStretch && _detectStars) { _detectStars = false; RaisePropertyChanged(nameof(DetectStars)); }
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.AutoStrechChanged, _autoStretch);
            }
        }

        private async Task<bool> PrepareImageHelper() {
            _prepImageCancellationSource?.Cancel();
            try {
                _prepImageTask?.Wait(_prepImageCancellationSource.Token);
            } catch (OperationCanceledException) {

            }
            _prepImageCancellationSource = new CancellationTokenSource();
            _prepImageTask = PrepareImage(ImgArr, null, _prepImageCancellationSource.Token);
            await _prepImageTask;
            return true;
        }

        public AsyncCommand<bool> PrepareImageCommand { get; private set; }

        private Task _prepImageTask;
        private CancellationTokenSource _prepImageCancellationSource;

        private bool _showCrossHair;
        public bool ShowCrossHair {
            get {
                return _showCrossHair;
            }
            set {
                _showCrossHair = value;
                RaisePropertyChanged();
            }
        }

        private bool _detectStars;
        public bool DetectStars {
            get {
                return _detectStars;
            }
            set {
                _detectStars = value;
                if (_detectStars) { _autoStretch = true; RaisePropertyChanged(nameof(AutoStretch)); }
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.DetectStarsChanged, _detectStars);
            }
        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();
                Mediator.Instance.Request(new StatusUpdateMessage() { Status = new ApplicationStatus() { Status = _status, Source = Title } });
            }
        }

        private ICamera Cam { get; set; }
        private ITelescope Telescope { get; set; }

        public IAsyncCommand PlateSolveImageCommand { get; private set; }

        public ICommand CancelPlateSolveImageCommand { get; private set; }

        public static SemaphoreSlim ss = new SemaphoreSlim(1, 1);
        
        public async Task<BitmapSource> PrepareImage(
                ImageArray iarr, 
                IProgress<string> progress, 
                CancellationToken token, 
                bool bSave = false,
                CaptureSequence sequence = null,
                string targetname = "") {
            BitmapSource source = null;
            try {
                await ss.WaitAsync(token);
            
                if (iarr != null) {
                
                    source = ImageAnalysis.CreateSourceFromArray(iarr, System.Windows.Media.PixelFormats.Gray16);


                    if (AutoStretch) {
                        source = await StretchAsync(iarr, source);
                    }

                    if (DetectStars) {
                        var analysis = new ImageAnalysis(source, iarr);
                        await analysis.DetectStarsAsync(progress, token);

                        if (Settings.AnnotateImage) {
                            source = analysis.GetAnnotatedImage();
                        }

                        iarr.Statistics.HFR = analysis.AverageHFR;
                        iarr.Statistics.DetectedStars = analysis.DetectedStars;
                    }

                    if (iarr.IsBayered) {
                        source = ImageAnalysis.Debayer(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
                    }

                    await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                        Image = null;
                        ImgArr = null;
                        GC.Collect();
                        ImgArr = iarr;
                        Image = source;
                        ImgStatisticsVM.Add(ImgArr.Statistics);
                        ImgHistoryVM.Add(iarr.Statistics);
                    }));

                    if(bSave) {
                        await SaveToDisk(sequence, token, progress, targetname);
                    }
                }
            } finally {
                ss.Release();
            }
            return source;
        }

        private async Task<BitmapSource> StretchAsync(ImageArray iarr, BitmapSource source) {
            return await Task<BitmapSource>.Run(() => Stretch(iarr, source));
        }

        public static BitmapSource Stretch(ImageArray iarr, BitmapSource source) {
            var img = ImageAnalysis.BitmapFromSource(source);

            var filter = ImageAnalysis.GetColorRemappingFilter(iarr.Statistics.Mean, Settings.AutoStretchFactor);
            filter.ApplyInPlace(img);

            source = null;

            source = ImageAnalysis.ConvertBitmap(img, System.Windows.Media.PixelFormats.Gray16);
            source.Freeze();
            return source;
        }

        public async Task<bool> SaveToDisk(CaptureSequence sequence, CancellationToken token, IProgress<string> progress, string targetname = "") {

            var filter = sequence.FilterType?.Name ?? string.Empty;
            var framenr = sequence.ProgressExposureCount;
            return await SaveToDisk(sequence.ExposureTime, filter, sequence.ImageType, sequence.Binning.Name, Cam.CCDTemperature, framenr, token, progress, targetname);

        }


        public async Task<bool> SaveToDisk(double exposuretime, string filter, string imageType, string binning, double ccdtemp, int framenr, CancellationToken token, IProgress<string> progress, string targetname = "") {
            progress.Report("Saving...");
            await Task.Run(() => {

                List<OptionsVM.ImagePattern> p = new List<OptionsVM.ImagePattern>();

                p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", filter));

                p.Add(new OptionsVM.ImagePattern("$$EXPOSURETIME$$", "Exposure Time in seconds", string.Format("{0:0.00}", exposuretime)));
                p.Add(new OptionsVM.ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd")));
                p.Add(new OptionsVM.ImagePattern("$$DATETIME$$", "Date with format YYYY-MM-DD_HH-mm-ss", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
                p.Add(new OptionsVM.ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", string.Format("{0:0000}", framenr)));
                p.Add(new OptionsVM.ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", imageType));

                if (binning == string.Empty) {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
                } else {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", binning));
                }

                p.Add(new OptionsVM.ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", string.Format("{0:00}", ccdtemp)));

                p.Add(new OptionsVM.ImagePattern("$$TARGETNAME$$", "Target Name if available", targetname));

                p.Add(new OptionsVM.ImagePattern("$$GAIN$$", "Camera Gain", Cam?.Gain.ToString() ?? string.Empty));

                string filename = Utility.Utility.GetImageFileString(p);
                string completefilename = Settings.ImageFilePath + filename;

                Stopwatch sw = Stopwatch.StartNew();
                if (Settings.FileType == FileTypeEnum.FITS) {
                    if (imageType == "SNAP") imageType = "LIGHT";
                    SaveFits(completefilename, imageType, exposuretime, filter);
                } else if (Settings.FileType == FileTypeEnum.TIFF) {
                    SaveTiff(completefilename);
                } else if (Settings.FileType == FileTypeEnum.XISF) {
                    if (imageType == "SNAP") imageType = "LIGHT";
                    SaveXisf(completefilename, imageType, exposuretime, filter);
                } else {
                    SaveTiff(completefilename);
                }
                sw.Stop();
                Debug.Print("Time to save: " + sw.Elapsed);
                sw = null;


            });

            token.ThrowIfCancellationRequested();
            return true;
        }

        private void SaveFits(string path, string imagetype, double duration, string filter) {
            try {
                Header h = new Header();
                h.AddValue("SIMPLE", "T", "C# FITS");
                h.AddValue("BITPIX", 16, "");
                h.AddValue("NAXIS", 2, "Dimensionality");
                h.AddValue("NAXIS1", this.ImgArr.Statistics.Width, "");
                h.AddValue("NAXIS2", this.ImgArr.Statistics.Height, "");
                h.AddValue("BZERO", 32768, "");
                h.AddValue("EXTEND", "T", "Extensions are permitted");
                                
                if (!string.IsNullOrEmpty(filter)) {
                    h.AddValue("FILTER", filter, "");
                }

                if (Cam != null) {
                    if (Cam.BinX > 0) {
                        h.AddValue("XBINNING", Cam.BinX, "");
                    }
                    if (Cam.BinY > 0) {
                        h.AddValue("YBINNING", Cam.BinY, "");
                    }
                }

                var temp = Cam.CCDTemperature;
                if (!double.IsNaN(temp)) {
                    h.AddValue("TEMPERAT", temp, "");
                }

                h.AddValue("IMAGETYP", imagetype, "");
                h.AddValue("EXPOSURE", duration, "");


                short[][] curl = new short[this.ImgArr.Statistics.Height][];
                int idx = 0;
                for (int i = 0; i < this.ImgArr.Statistics.Height; i++) {
                    curl[i] = new short[this.ImgArr.Statistics.Width];
                    for (int j = 0; j < this.ImgArr.Statistics.Width; j++) {
                        curl[i][j] = (short)(short.MinValue + this.ImgArr.FlatArray[idx]);
                        idx++;
                    }
                }
                ImageData d = new ImageData(curl);

                Fits fits = new Fits();
                BasicHDU hdu = FitsFactory.HDUFactory(h, d);
                fits.AddHDU(hdu);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = GetUniqueFilePath(path + ".fits");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    fits.Write(fs);
                }

            } catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);

            }
        }

        private string GetUniqueFilePath(string fullPath) {
            int count = 1;

            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string path = Path.GetDirectoryName(fullPath);
            string newFullPath = fullPath;

            while (File.Exists(newFullPath)) {
                string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                newFullPath = Path.Combine(path, tempFileName + extension);
            }
            return newFullPath;
        }

        private void SaveTiff(String path) {

            try {
                BitmapSource bmpSource = ImageAnalysis.CreateSourceFromArray(ImgArr, System.Windows.Media.PixelFormats.Gray16);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = GetUniqueFilePath(path + ".tif");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                    encoder.Compression = TiffCompressOption.None;
                    encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                    encoder.Save(fs);
                }
            } catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);

            }
        }

        private void SaveXisf(String path, string imagetype, double duration, string filter) {
            try {


                var header = new XISFHeader();

                header.AddEmbeddedImage(ImgArr, imagetype);

                header.AddImageProperty(XISFImageProperty.Observation.Time.Start, DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture));

                if (Telescope != null) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, Telescope.Name);

                    /* Location */
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Latitude, Telescope.SiteLatitude.ToString(CultureInfo.InvariantCulture));
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Longitude, Telescope.SiteLongitude.ToString(CultureInfo.InvariantCulture));
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Elevation, Telescope.SiteElevation.ToString(CultureInfo.InvariantCulture));
                    /* convert to degrees */
                    var RA = Telescope.RightAscension * 360 / 24;
                    header.AddImageProperty(XISFImageProperty.Observation.Center.RA, RA.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.RA[2], Telescope.RightAscensionString);

                    header.AddImageProperty(XISFImageProperty.Observation.Center.Dec, Telescope.Declination.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.Dec[2], Telescope.DeclinationString);
                }

                if (Cam != null) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.Name, Cam.Name);

                    if (Cam.Gain > 0) {
                        /* Add offset as a comment. There is no dedicated keyword for this */
                        string offset = string.Empty;
                        if (Cam.Offset > 0) {
                            offset = Cam.Offset.ToString();
                        }
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, Cam.Gain.ToString(), offset);
                    }

                    if (Cam.BinX > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, Cam.BinX.ToString());
                    }
                    if (Cam.BinY > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, Cam.BinY.ToString());
                    }

                    var temp = Cam.CCDTemperature;
                    if (!double.IsNaN(temp)) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, temp.ToString(CultureInfo.InvariantCulture));
                    }

                    if (Cam.PixelSizeX > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, Cam.PixelSizeX.ToString(CultureInfo.InvariantCulture));
                    }

                    if (Cam.PixelSizeY > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, Cam.PixelSizeY.ToString(CultureInfo.InvariantCulture));
                    }
                }

                
                if (!string.IsNullOrEmpty(filter)) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Filter.Name, filter);
                }

                header.AddImageProperty(XISFImageProperty.Instrument.ExposureTime, duration.ToString(System.Globalization.CultureInfo.InvariantCulture));

                XISF img = new XISF(header);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = GetUniqueFilePath(path + ".xisf");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    img.Save(fs);
                }

            } catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);

            }
        }

    }
}
