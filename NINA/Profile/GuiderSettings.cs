#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Enum;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class GuiderSettings : Settings, IGuiderSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            ditherPixels = 5;
            ditherRAOnly = false;
            settleTime = 10;
            pHD2ServerUrl = "localhost";
            pHD2ServerPort = 4400;
            pHD2LargeHistorySize = 100;
            pHD2GuiderScale = GuiderScaleEnum.PIXELS;
            settlePixels = 1.5;
            settleTimeout = 40;
            directGuideDuration = 2;
            autoRetryStartGuiding = false;
            autoRetryStartGuidingTimeoutSeconds = 60;
            maxY = 4;

            var defaultPHD2Path = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\PHDGuiding2\phd2.exe");

            phd2Path =
                File.Exists(defaultPHD2Path)
                ? defaultPHD2Path
                : string.Empty;
            guiderName = "PHD2";
            mgenFocalLength = 1000;
            mgenPixelMargin = 10;
        }

        private double ditherPixels;

        [DataMember]
        public double DitherPixels {
            get => ditherPixels;
            set {
                if (ditherPixels != value) {
                    ditherPixels = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool ditherRAOnly;

        [DataMember]
        public bool DitherRAOnly {
            get => ditherRAOnly;
            set {
                if (ditherRAOnly != value) {
                    ditherRAOnly = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int settleTime;

        [DataMember]
        public int SettleTime {
            get => settleTime;
            set {
                if (settleTime != value) {
                    settleTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string pHD2ServerUrl;

        [DataMember]
        public string PHD2ServerUrl {
            get => pHD2ServerUrl;
            set {
                if (pHD2ServerUrl != value) {
                    pHD2ServerUrl = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int pHD2ServerPort;

        [DataMember]
        public int PHD2ServerPort {
            get => pHD2ServerPort;
            set {
                if (pHD2ServerPort != value) {
                    pHD2ServerPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int pHD2LargeHistorySize;

        [DataMember]
        public int PHD2HistorySize {
            get => pHD2LargeHistorySize;
            set {
                if (pHD2LargeHistorySize != value) {
                    pHD2LargeHistorySize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string phd2Path;

        [DataMember]
        public string PHD2Path {
            get => phd2Path;
            set {
                if (phd2Path != value) {
                    phd2Path = value;
                    RaisePropertyChanged();
                }
            }
        }

        private GuiderScaleEnum pHD2GuiderScale;

        [DataMember]
        public GuiderScaleEnum PHD2GuiderScale {
            get => pHD2GuiderScale;
            set {
                if (pHD2GuiderScale != value) {
                    pHD2GuiderScale = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double settlePixels;

        [DataMember]
        public double SettlePixels {
            get => settlePixels;

            set {
                if (settlePixels != value) {
                    settlePixels = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int settleTimeout;

        [DataMember]
        public int SettleTimeout {
            get => settleTimeout;

            set {
                if (settleTimeout != value) {
                    settleTimeout = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double directGuideDuration;

        [DataMember]
        public double DirectGuideDuration {
            get => directGuideDuration;

            set {
                if (directGuideDuration != value) {
                    directGuideDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string guiderName;

        [DataMember]
        public string GuiderName {
            get => guiderName;
            set {
                if (guiderName != value) {
                    guiderName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool autoRetryStartGuiding;

        [DataMember]
        public bool AutoRetryStartGuiding {
            get => autoRetryStartGuiding;
            set {
                if (autoRetryStartGuiding == value) return;
                autoRetryStartGuiding = value;
                RaisePropertyChanged();
            }
        }

        private int autoRetryStartGuidingTimeoutSeconds;

        [DataMember]
        public int AutoRetryStartGuidingTimeoutSeconds {
            get => autoRetryStartGuidingTimeoutSeconds;
            set {
                if (autoRetryStartGuidingTimeoutSeconds == value) return;
                autoRetryStartGuidingTimeoutSeconds = value;
                RaisePropertyChanged();
            }
        }

        private double maxY;

        [DataMember]
        public double MaxY {
            get => maxY;
            set {
                if (maxY != value) {
                    maxY = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int mgenFocalLength;

        [DataMember]
        public int MGENFocalLength {
            get => mgenFocalLength;
            set {
                if (mgenFocalLength != value) {
                    mgenFocalLength = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int mgenPixelMargin;
        [DataMember]
        public int MGENPixelMargin {
            get => mgenPixelMargin;
            set {
                if (mgenPixelMargin != value) {
                    mgenPixelMargin = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}