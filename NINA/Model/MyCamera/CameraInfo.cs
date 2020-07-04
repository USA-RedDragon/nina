#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NINA.Model.MyCamera {
    public class CameraInfo : DeviceInfo {
        private bool canSetTemperature;

        public bool CanSetTemperature {
            get { return canSetTemperature; }
            set { canSetTemperature = value; RaisePropertyChanged(); }
        }

        private bool hasShutter;

        public bool HasShutter {
            get { return hasShutter; }
            set { hasShutter = value; RaisePropertyChanged(); }
        }

        private double temperature;

        public double Temperature {
            get { return temperature; }
            set { temperature = value; RaisePropertyChanged(); }
        }

        private int gain = -1;

        public int Gain {
            get { return gain; }
            set { gain = value; RaisePropertyChanged(); }
        }

        private int defaultGain = -1;

        public int DefaultGain {
            get { return defaultGain; }
            set { defaultGain = value; RaisePropertyChanged(); }
        }

        private double electronsPerADU;

        public double ElectronsPerADU {
            get { return electronsPerADU; }
            set { electronsPerADU = value; RaisePropertyChanged(); }
        }

        private short binxX;

        public short BinX {
            get { return binxX; }
            set { binxX = value; RaisePropertyChanged(); }
        }

        private int bitDepth;

        public int BitDepth {
            get {
                return bitDepth;
            }
            set {
                bitDepth = value;
                RaisePropertyChanged();
            }
        }

        private short binY;

        public short BinY {
            get { return binY; }
            set { binY = value; RaisePropertyChanged(); }
        }

        private bool canSetOffset;

        public bool CanSetOffset {
            get { return canSetOffset; }
            set { canSetOffset = value; RaisePropertyChanged(); }
        }

        private bool canGetGain;

        public bool CanGetGain {
            get { return canGetGain; }
            set { canGetGain = value; RaisePropertyChanged(); }
        }

        public int offsetMin;

        public int OffsetMin {
            get { return offsetMin; }
            set { offsetMin = value; RaisePropertyChanged(); }
        }

        public int offsetMax;

        public int OffsetMax {
            get { return offsetMax; }
            set { offsetMax = value; RaisePropertyChanged(); }
        }

        private int offset;

        public int Offset {
            get { return offset; }
            set { offset = value; RaisePropertyChanged(); }
        }

        private int defaultOffset = -1;

        public int DefaultOffset {
            get { return defaultOffset; }
            set { defaultOffset = value; RaisePropertyChanged(); }
        }

        private int usbLimit;

        public int USBLimit {
            get { return usbLimit; }
            set { usbLimit = value; RaisePropertyChanged(); }
        }

        private bool isSubSampleEnabled;

        public bool IsSubSampleEnabled {
            get { return isSubSampleEnabled; }
            set { isSubSampleEnabled = value; RaisePropertyChanged(); }
        }

        private string cameraState;

        public string CameraState {
            get { return cameraState; }
            set { cameraState = value; RaisePropertyChanged(); }
        }

        private int xSize;

        public int XSize {
            get { return xSize; }
            set { xSize = value; RaisePropertyChanged(); }
        }

        private int ySize;

        public int YSize {
            get { return ySize; }
            set { ySize = value; RaisePropertyChanged(); }
        }

        private double pixelSize;

        public double PixelSize {
            get { return pixelSize; }
            set { pixelSize = value; RaisePropertyChanged(); }
        }

        private int battery;

        public int Battery {
            get { return battery; }
            set { battery = value; RaisePropertyChanged(); }
        }

        private int gainMin;

        public int GainMin {
            get {
                return gainMin;
            }
            set {
                gainMin = value;
                RaisePropertyChanged();
            }
        }

        private int gainMax;

        public int GainMax {
            get {
                return gainMax;
            }
            set {
                gainMax = value;
                RaisePropertyChanged();
            }
        }

        private bool canSetGain;

        public bool CanSetGain {
            get {
                return canSetGain;
            }
            set {
                canSetGain = value;
                RaisePropertyChanged();
            }
        }

        private IList<int> gains = new List<int>();

        public IList<int> Gains {
            get {
                return gains;
            }
            set {
                gains = value;
                RaisePropertyChanged();
            }
        }

        private bool coolerOn;

        public bool CoolerOn {
            get {
                return coolerOn;
            }
            set {
                coolerOn = value;
                RaisePropertyChanged();
            }
        }

        private double _coolerPower;

        public double CoolerPower {
            get {
                return _coolerPower;
            }
            set {
                _coolerPower = value;
                RaisePropertyChanged();
            }
        }

        private bool dewHeaterOn;

        public bool DewHeaterOn {
            get {
                return dewHeaterOn;
            }
            set {
                dewHeaterOn = value;
                RaisePropertyChanged();
            }
        }

        private bool canSubSample;

        public bool CanSubSample {
            get { return canSubSample; }
            set { canSubSample = value; RaisePropertyChanged(); }
        }

        private int subSampleX;

        public int SubSampleX {
            get { return subSampleX; }
            set { subSampleX = value; RaisePropertyChanged(); }
        }

        private int subSampleY;

        public int SubSampleY {
            get { return subSampleY; }
            set { subSampleY = value; RaisePropertyChanged(); }
        }

        private int subSampleWidth;

        public int SubSampleWidth {
            get { return subSampleWidth; }
            set { subSampleWidth = value; RaisePropertyChanged(); }
        }

        private int subSampleHeight;

        public int SubSampleHeight {
            get { return subSampleHeight; }
            set { subSampleHeight = value; RaisePropertyChanged(); }
        }

        private double temperatureSetPoint;

        public double TemperatureSetPoint {
            get { return temperatureSetPoint; }
            set { temperatureSetPoint = value; RaisePropertyChanged(); }
        }

        private IEnumerable<string> readoutModes;

        public IEnumerable<string> ReadoutModes {
            get => readoutModes;
            set {
                readoutModes = value;
                RaisePropertyChanged();
            }
        }

        private short readoutMode;

        public short ReadoutMode {
            get => readoutMode;
            set {
                readoutMode = value;
                RaisePropertyChanged();
            }
        }

        private short snapReadoutMode;

        public short ReadoutModeForSnapImages {
            get => snapReadoutMode;
            set {
                snapReadoutMode = value;
                RaisePropertyChanged();
            }
        }

        private short normalReadoutMode;

        public short ReadoutModeForNormalImages {
            get => normalReadoutMode;
            set {
                normalReadoutMode = value;
                RaisePropertyChanged();
            }
        }

        private bool isExposing;

        public bool IsExposing {
            get => isExposing;
            set {
                isExposing = value;
                RaisePropertyChanged();
            }
        }

        private DateTime exposureEndTime = DateTime.Now;

        public DateTime ExposureEndTime {
            get => exposureEndTime;
            set {
                exposureEndTime = value;
                RaisePropertyChanged();
            }
        }

        private double nextExposureLength = -1;

        public double NextExposureLength {
            get => nextExposureLength;
            set {
                nextExposureLength = value;
                RaisePropertyChanged();
            }
        }

        private double lastDownloadTime = -1;

        public double LastDownloadTime {
            get => lastDownloadTime;
            set {
                lastDownloadTime = value;
                RaisePropertyChanged();
            }
        }

        private SensorType sensorType = SensorType.Monochrome;

        public SensorType SensorType {
            get => sensorType;
            set {
                sensorType = value;
                RaisePropertyChanged();
            }
        }

        private short bayerOffsetX = 0;

        public short BayerOffsetX {
            get => bayerOffsetX;
            set {
                bayerOffsetX = value;
                RaisePropertyChanged();
            }
        }

        private short bayerOffsetY = 0;

        public short BayerOffsetY {
            get => bayerOffsetY;
            set {
                bayerOffsetY = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<BinningMode> binningModes = new AsyncObservableCollection<BinningMode>();

        public AsyncObservableCollection<BinningMode> BinningModes {
            get => binningModes;
            set {
                binningModes = value;
                RaisePropertyChanged();
            }
        }

        private double exposureMax = 0;

        public double ExposureMax {
            get => exposureMax;
            set {
                exposureMax = value;
                RaisePropertyChanged();
            }
        }

        private double exposureMin = 0;

        public double ExposureMin {
            get => exposureMin;
            set {
                exposureMin = value;
                RaisePropertyChanged();
            }
        }

        private bool liveViewEnabled = false;

        public bool LiveViewEnabled {
            get => liveViewEnabled;
            set {
                liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool canShowLiveView = false;

        public bool CanShowLiveView {
            get => canShowLiveView;
            set {
                canShowLiveView = value;
                RaisePropertyChanged();
            }
        }

        private double exposureMin = 0;

        public double ExposureMin {
            get => exposureMin;
            set {
                exposureMin = value;
                RaisePropertyChanged();
            }
        }
    }
}