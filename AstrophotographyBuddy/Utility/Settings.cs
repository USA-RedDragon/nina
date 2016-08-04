﻿using System.Windows.Media;

namespace AstrophotographyBuddy.Utility {
    static class Settings  {
               

        public static FileTypeEnum FileType {
            get {
                return (FileTypeEnum)Properties.Settings.Default.FileType;
            } set {
                Properties.Settings.Default.FileType = (int)value;
                Properties.Settings.Default.Save();
            }

        }

        public static string AstrometryAPIKey {
            get {
                return Properties.Settings.Default.AstrometryAPIKey;
            }
            set {
                Properties.Settings.Default.AstrometryAPIKey = value;
                Properties.Settings.Default.Save();
            }

        }

        public static bool UseFullResolutionPlateSolve {
            get {
                return Properties.Settings.Default.UseFullResolutionPlateSolve;
            }
            set {
                Properties.Settings.Default.UseFullResolutionPlateSolve = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string CameraId {
            get {
                return Properties.Settings.Default.CameraId;
            }
            set {
                Properties.Settings.Default.CameraId = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string TelescopeId {
            get {
                return Properties.Settings.Default.TelescopeId;
            }
            set {
                Properties.Settings.Default.TelescopeId = value;
                Properties.Settings.Default.Save();
            }
        }

        public static int LogLevel {
            get {
                return Properties.Settings.Default.LogLevel;
            }
            set {
                Properties.Settings.Default.LogLevel = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string FilterWheelId {
            get {
                return Properties.Settings.Default.FilterWheelId;
            }
            set {
                Properties.Settings.Default.FilterWheelId = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string ImageFilePath {
            get {
                return Properties.Settings.Default.ImageFilePath;
            }
            set {
                Properties.Settings.Default.ImageFilePath = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string ImageFilePattern {
            get {
                return Properties.Settings.Default.ImageFilePattern;
            }
            set {
                Properties.Settings.Default.ImageFilePattern = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string PHD2ServerUrl {
            get {
                return Properties.Settings.Default.PHD2ServerUrl;
            }
            set {
                Properties.Settings.Default.PHD2ServerUrl = value;
                Properties.Settings.Default.Save();
            }
        }
        public static int PHD2ServerPort {
            get {
                return Properties.Settings.Default.PHD2ServerPort;
            }
            set {
                Properties.Settings.Default.PHD2ServerPort = value;
                Properties.Settings.Default.Save();
            }
        }

        public static double DitherPixels {
            get {
                return Properties.Settings.Default.DitherPixels;
            }
            set {
                Properties.Settings.Default.DitherPixels = value;
                Properties.Settings.Default.Save();
            }
        }

        public static bool DitherRAOnly {
            get {
                return Properties.Settings.Default.DitherRAOnly;
            }
            set {
                Properties.Settings.Default.DitherRAOnly = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color PrimaryColor {
            get {
                return Properties.Settings.Default.PrimaryColor;
            }
            set {
                Properties.Settings.Default.PrimaryColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color SecondaryColor {
            get {
                return Properties.Settings.Default.SecondaryColor;
            }
            set {
                Properties.Settings.Default.SecondaryColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color BorderColor {
            get {
                return Properties.Settings.Default.BorderColor;
            }
            set {
                Properties.Settings.Default.BorderColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color BackgroundColor {
            get {
                return Properties.Settings.Default.BackgroundColor;
            }
            set {
                Properties.Settings.Default.BackgroundColor = value;
                Properties.Settings.Default.Save();

            }
        }

        public static Color ButtonBackgroundColor {
            get {
                return Properties.Settings.Default.ButtonBackgroundColor;
            }
            set {
                Properties.Settings.Default.ButtonBackgroundColor = value;
                Properties.Settings.Default.Save();

            }
        }

        public static Color ButtonBackgroundSelectedColor {
            get {
                return Properties.Settings.Default.ButtonBackgroundSelectedColor;
            }
            set {
                Properties.Settings.Default.ButtonBackgroundSelectedColor = value;
                Properties.Settings.Default.Save();

            }
        }

        public static Color ButtonForegroundDisabledColor {
            get {
                return Properties.Settings.Default.ButtonForegroundDisabledColor;
            }
            set {
                Properties.Settings.Default.ButtonForegroundDisabledColor = value;
                Properties.Settings.Default.Save();

            }
        }

        public static Color ButtonForegroundColor {
            get {
                return Properties.Settings.Default.ButtonForegroundColor;
            }
            set {
                Properties.Settings.Default.ButtonForegroundColor = value;
                Properties.Settings.Default.Save();

            }
        }


        public static Color AltPrimaryColor {
            get {
                return Properties.Settings.Default.AltPrimaryColor;
            }
            set {
                Properties.Settings.Default.AltPrimaryColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color AltSecondaryColor {
            get {
                return Properties.Settings.Default.AltSecondaryColor;
            }
            set {
                Properties.Settings.Default.AltSecondaryColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color AltBorderColor {
            get {
                return Properties.Settings.Default.AltBorderColor;
            }
            set {
                Properties.Settings.Default.AltBorderColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color AltBackgroundColor {
            get {
                return Properties.Settings.Default.AltBackgroundColor;
            }
            set {
                Properties.Settings.Default.AltBackgroundColor = value;
                Properties.Settings.Default.Save();

            }
        }

        public static Color AltButtonBackgroundColor {
            get {
                return Properties.Settings.Default.AltButtonBackgroundColor;
            }
            set {
                Properties.Settings.Default.AltButtonBackgroundColor = value;
                Properties.Settings.Default.Save();

            }
        }

        public static Color AltButtonBackgroundSelectedColor {
            get {
                return Properties.Settings.Default.AltButtonBackgroundSelectedColor;
            }
            set {
                Properties.Settings.Default.AltButtonBackgroundSelectedColor = value;
                Properties.Settings.Default.Save();

            }
        }

        public static Color AltButtonForegroundDisabledColor {
            get {
                return Properties.Settings.Default.AltButtonForegroundDisabledColor;
            }
            set {
                Properties.Settings.Default.AltButtonForegroundDisabledColor = value;
                Properties.Settings.Default.Save();

            }
        }

        public static Color AltButtonForegroundColor {
            get {
                return Properties.Settings.Default.AltButtonForegroundColor;
            }
            set {
                Properties.Settings.Default.AltButtonForegroundColor = value;
                Properties.Settings.Default.Save();

            }
        }


    }

}
