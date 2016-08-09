﻿using AstrophotographyBuddy.Model;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace AstrophotographyBuddy.ViewModel {
    public class OptionsVM :BaseVM {
        public OptionsVM() {
            Name = "Options";            
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SettingsSVG"];
            PreviewFileCommand = new RelayCommand(previewFile);
            OpenFileDiagCommand = new RelayCommand(openFileDiag);
            ToggleColorsCommand = new RelayCommand(toggleColors);

            ImageFilePath = Settings.ImageFilePath;
            ImageFilePattern = Settings.ImageFilePattern;
            PHD2ServerUrl = Settings.PHD2ServerUrl;
            PHD2ServerPort = Settings.PHD2ServerPort;

            HashSet<ImagePattern> p = new HashSet<ImagePattern>();
            p.Add(new ImagePattern("$$FILTER$$", "Filtername", "L"));
            p.Add(new ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", "2016-01-01-12-00-00"));
            p.Add(new ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", "0001"));
            p.Add(new ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", "Light"));
            p.Add(new ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
            p.Add(new ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", "-15"));
            p.Add(new ImagePattern("$$EXPOSURETIME$$", "Exposure Time in seconds", string.Format("{0:0.00}", 10.21234)));
            ImagePatterns = p;
        }

        private void openFileDiag(object o) {
            var diag = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = diag.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK) {
                ImageFilePath = diag.SelectedPath + "\\";
            }
        }

        private ICommand _openFileDiagCommand;
        public ICommand OpenFileDiagCommand {
            get {
                return _openFileDiagCommand;
            }
            set {
                _openFileDiagCommand = value;
                RaisePropertyChanged();
            }
        }

        private void previewFile(object o) {
            System.Windows.MessageBox.Show(Utility.Utility.getImageFileString(ImagePatterns), "Example File Name", System.Windows.MessageBoxButton.OK);
        }

                private ICommand _previewFileCommand;
        public ICommand PreviewFileCommand {
            get {
                return _previewFileCommand;
            }
            set {
                _previewFileCommand = value;
                RaisePropertyChanged();
            }
        }

     public string ImageFilePath {
            get {
                return Settings.ImageFilePath;
            }
            set {
                Settings.ImageFilePath = value;
                RaisePropertyChanged();
            }
        }

        
        public string ImageFilePattern {
            get {
                return Settings.ImageFilePattern;                
            }
            set {
                Settings.ImageFilePattern  = value;
                RaisePropertyChanged();
            }
        }
        
        public string PHD2ServerUrl {
            get {
                return Settings.PHD2ServerUrl;
            }
            set {
                Settings.PHD2ServerUrl = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolverEnum PlateSolverType {
            get {
                return Settings.PlateSolverType;
            }
            set {
                Settings.PlateSolverType = value;
                RaisePropertyChanged();
            }
        }

        public int AnsvrFocalLength {
            get {
                return Settings.AnsvrFocalLength;
            }
            set {
                Settings.AnsvrFocalLength = value;
                RaisePropertyChanged();
            }
        }

        public double AnsvrPixelSize {
            get {
                return Settings.AnsvrPixelSize;
            }
            set {
                Settings.AnsvrPixelSize = value;
                RaisePropertyChanged();
            }
        }


        
        public string AstrometryAPIKey {
            get {
                return Settings.AstrometryAPIKey;
            }
            set {
                Settings.AstrometryAPIKey = value;
                RaisePropertyChanged();
            }
        }

        public bool UseFullResolutionPlateSolve {
            get {
                return Settings.UseFullResolutionPlateSolve;
            }
            set {
                Settings.UseFullResolutionPlateSolve = value;
                RaisePropertyChanged();
            }
        }

        public int PHD2ServerPort {
            get {
                return Settings.PHD2ServerPort;
            }
            set {
                Settings.PHD2ServerPort = value;
                RaisePropertyChanged();
            }
        }
    
        private void toggleColors(object o) {
            var a = PrimaryColor;
            var b = SecondaryColor;
            var c = BorderColor;
            var d = BackgroundColor;
            var e = ButtonBackgroundColor;
            var f = ButtonBackgroundSelectedColor;
            var g = ButtonForegroundColor;
            var h = ButtonForegroundDisabledColor;

            PrimaryColor = AltPrimaryColor;
            SecondaryColor = AltSecondaryColor;
            BorderColor = AltBorderColor;
            BackgroundColor = AltBackgroundColor;
            ButtonBackgroundColor = AltButtonBackgroundColor;
            ButtonBackgroundSelectedColor = AltButtonBackgroundSelectedColor;
            ButtonForegroundColor = AltButtonForegroundColor;
            ButtonForegroundDisabledColor = AltButtonForegroundDisabledColor;

            AltPrimaryColor = a;
            AltSecondaryColor = b;
            AltBorderColor = c;
            AltBackgroundColor = d;
            AltButtonBackgroundColor = e;
            AltButtonBackgroundSelectedColor = f;
            AltButtonForegroundColor = g;
            AltButtonForegroundDisabledColor = h;
        }

        public Color PrimaryColor {
            get {
                return Settings.PrimaryColor;
            }
            set {
                Settings.PrimaryColor = value;
                RaisePropertyChanged();
            }
            
        }
        public Color SecondaryColor {
            get {
                return Settings.SecondaryColor;
            }
            set {
                Settings.SecondaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color BorderColor {
            get {
                return Settings.BorderColor;
            }
            set {
                Settings.BorderColor = value;
                RaisePropertyChanged();
            }

        }
        public Color BackgroundColor {
            get {
                return Settings.BackgroundColor;
            }
            set {
                Settings.BackgroundColor = value;
                RaisePropertyChanged();
            }

        }

        public FileTypeEnum FileType {
            get {
                return Settings.FileType;
            } set {
                Settings.FileType = value;
                RaisePropertyChanged();
            }
        }

        public Color ButtonBackgroundColor {
            get {
                return Settings.ButtonBackgroundColor;
            }
            set {
                Settings.ButtonBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color ButtonBackgroundSelectedColor {
            get {
                return Settings.ButtonBackgroundSelectedColor;
            }
            set {
                Settings.ButtonBackgroundSelectedColor = value;
                RaisePropertyChanged();
            }

        }

        public Color ButtonForegroundColor {
            get {
                return Settings.ButtonForegroundColor;
            }
            set {
                Settings.ButtonForegroundColor = value;
                RaisePropertyChanged();
            }

        }

        public Color ButtonForegroundDisabledColor {
            get {
                return Settings.ButtonForegroundDisabledColor;
            }
            set {
                Settings.ButtonForegroundDisabledColor = value;
                RaisePropertyChanged();
            }

        }

        public double DitherPixels {
            get {
                return Settings.DitherPixels;
            }
            set {
                Settings.DitherPixels = value;
                RaisePropertyChanged();
            }

        }

        public bool DitherRAOnly {
            get {
                return Settings.DitherRAOnly;
            }
            set {
                Settings.DitherRAOnly = value;
                RaisePropertyChanged();
            }

        }

        private HashSet<ImagePattern> _imagePatterns;
        public HashSet<ImagePattern> ImagePatterns {
            get {
                return _imagePatterns;
            }
            set {
                _imagePatterns = value;
                RaisePropertyChanged();
            }
        }


        private RelayCommand _toggleColorsCommand;
        public RelayCommand ToggleColorsCommand {
            get {
                return _toggleColorsCommand;
            }

            set {
                _toggleColorsCommand = value;
                RaisePropertyChanged();
            }
        }

        public class ImagePattern {
            public ImagePattern(string k, string d, string v) {
                Key = k;
                Description = d;
                Value = v;
            }
            private string _key;
            private string _description;
            private string _value;
            public string Value {
                get {
                    return _value;
                }
                set {
                    _value = value;
                }
            }
            public string Key {
                get {
                    return _key;
                }

                set {
                    _key = value;
                }
            }

            public string Description {
                get {
                    return _description;
                }

                set {
                    _description = value;
                }
            }
        }




        public Color AltPrimaryColor {
            get {
                return Settings.AltPrimaryColor;
            }
            set {
                Settings.AltPrimaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltSecondaryColor {
            get {
                return Settings.AltSecondaryColor;
            }
            set {
                Settings.AltSecondaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltBorderColor {
            get {
                return Settings.AltBorderColor;
            }
            set {
                Settings.AltBorderColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltBackgroundColor {
            get {
                return Settings.AltBackgroundColor;
            }
            set {
                Settings.AltBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltButtonBackgroundColor {
            get {
                return Settings.AltButtonBackgroundColor;
            }
            set {
                Settings.AltButtonBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltButtonBackgroundSelectedColor {
            get {
                return Settings.AltButtonBackgroundSelectedColor;
            }
            set {
                Settings.AltButtonBackgroundSelectedColor = value;
                RaisePropertyChanged();
            }

        }

        public Color AltButtonForegroundColor {
            get {
                return Settings.AltButtonForegroundColor;
            }
            set {
                Settings.AltButtonForegroundColor = value;
                RaisePropertyChanged();
            }

        }

        public Color AltButtonForegroundDisabledColor {
            get {
                return Settings.AltButtonForegroundDisabledColor;
            }
            set {
                Settings.AltButtonForegroundDisabledColor = value;
                RaisePropertyChanged();
            }

        }

        
    }
}
