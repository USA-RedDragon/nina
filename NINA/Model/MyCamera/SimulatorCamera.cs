﻿#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NINA.Model.MyCamera {

    public class SimulatorCamera : BaseINPC, ICamera {

        public SimulatorCamera(IProfileService profileService) {
            this.profileService = profileService;
            RandomImageWidth = 640;
            RandomImageHeight = 480;
            RandomImageMean = 5000;
            RandomImageStdDev = 100;
            LoadImageCommand = new AsyncCommand<bool>(() => LoadImage());
            UnloadImageCommand = new RelayCommand((object o) => Image = null);
        }

        private object lockObj = new object();

        public bool HasShutter {
            get {
                return false;
            }
        }

        public bool Connected { get; private set; }

        public double CCDTemperature {
            get {
                return double.NaN;
            }
        }

        public double SetCCDTemperature {
            get {
                return double.NaN;
            }

            set {
            }
        }

        public short BinX {
            get {
                return -1;
            }

            set {
            }
        }

        public short BinY {
            get {
                return -1;
            }

            set {
            }
        }

        public string Description {
            get {
                return "NINA_SIM_Simulator";
            }
        }

        public string DriverInfo {
            get {
                return "NINA_SIM_DriverInfo";
            }
        }

        public string DriverVersion {
            get {
                return Utility.Utility.Version;
            }
        }

        public string SensorName {
            get {
                return "NINA_SIM_Sensor";
            }
        }

        public SensorType SensorType {
            get {
                return SensorType.Monochrome;
            }
        }

        public int CameraXSize {
            get {
                return Image?.Statistics?.Width ?? RandomImageWidth;
            }
        }

        public int CameraYSize {
            get {
                return Image?.Statistics?.Height ?? RandomImageHeight;
            }
        }

        public double ExposureMin {
            get {
                return 0;
            }
        }

        public double ExposureMax {
            get {
                return double.MaxValue;
            }
        }

        public short MaxBinX {
            get {
                return 1;
            }
        }

        public short MaxBinY {
            get {
                return 1;
            }
        }

        public double PixelSizeX {
            get {
                return 3.8;
            }
        }

        public double PixelSizeY {
            get {
                return 3.8;
            }
        }

        public bool CanSetCCDTemperature {
            get {
                return false;
            }
        }

        public bool CoolerOn {
            get {
                return false;
            }

            set {
            }
        }

        public double CoolerPower {
            get {
                return double.NaN;
            }
        }

        public string CameraState {
            get {
                return "NINA_SIM_State";
            }
        }

        public int Offset {
            get {
                return -1;
            }

            set {
            }
        }

        public int USBLimit {
            get {
                return -1;
            }

            set {
            }
        }

        public bool CanSetOffset {
            get {
                return false;
            }
        }

        public bool CanSetUSBLimit {
            get {
                return false;
            }
        }

        public bool CanGetGain {
            get {
                return false;
            }
        }

        public bool CanSetGain {
            get {
                return false;
            }
        }

        public short GainMax {
            get {
                return -1;
            }
        }

        public short GainMin {
            get {
                return -1;
            }
        }

        public short Gain {
            get {
                return -1;
            }

            set {
            }
        }

        public ArrayList Gains {
            get {
                return null;
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                return null;
            }
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Id {
            get {
                return "NINA_SIM_Id";
            }
        }

        public string Name {
            get {
                return "NINA_SIM";
            }
        }

        public double Temperature {
            get {
                return double.NaN;
            }
        }

        public double TemperatureSetPoint {
            get {
                return double.NaN;
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanSetTemperature {
            get {
                return false;
            }
        }

        public bool CanSubSample {
            get {
                return false;
            }
        }

        public bool EnableSubSample {
            get {
                return false;
            }

            set {
            }
        }

        public int SubSampleX {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleY {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleWidth {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleHeight {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanShowLiveView {
            get {
                return false;
            }
        }

        public bool LiveViewEnabled {
            get {
                return false;
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool HasDewHeater {
            get {
                return false;
            }
        }

        public bool DewHeaterOn {
            get {
                return false;
            }

            set {
            }
        }

        public bool HasBattery {
            get {
                return false;
            }
        }

        public int BatteryLevel {
            get {
                return -1;
            }
        }

        public int BitDepth {
            get {
                return (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        public ICollection ReadoutModes {
            get {
                return new List<string>() { "Default" };
            }
        }

        public short ReadoutModeForSnapImages {
            get {
                return 0;
            }

            set {
            }
        }

        public short ReadoutModeForNormalImages {
            get {
                return 0;
            }

            set {
            }
        }

        public void AbortExposure() {
            throw new NotImplementedException();
        }

        public async Task<bool> Connect(CancellationToken token) {
            Connected = true;
            return true;
        }

        public void Disconnect() {
            Connected = false;
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token, bool calculateStatistics) {
            int width, height, mean, stdev;
            lock (lockObj) {
                if (Image != null) {
                    return Image;
                }

                width = RandomImageWidth;
                height = RandomImageHeight;
                mean = RandomImageMean;
                stdev = RandomImageStdDev;
            }

            ushort[] input = new ushort[width * height];

            Random rand = new Random();
            for (int i = 0; i < width * height; i++) {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal = mean + stdev * randStdNormal; //random normal(mean,stdDev^2)
                input[i] = (ushort)randNormal;
            }

            return await ImageArray.CreateInstance(input, width, height, BitDepth, false, true, profileService.ActiveProfile.ImageSettings.HistogramResolution);
        }

        private int randomImageWidth;
        private IProfileService profileService;

        public int RandomImageWidth {
            get => randomImageWidth;
            set {
                lock (lockObj) {
                    randomImageWidth = value;
                }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CameraXSize));
            }
        }

        private int randomImageHeight;

        public int RandomImageHeight {
            get => randomImageHeight;
            set {
                lock (lockObj) {
                    randomImageHeight = value;
                }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CameraYSize));
            }
        }

        private int randomImageMean;

        public int RandomImageMean {
            get => randomImageMean;
            set {
                lock (lockObj) {
                    randomImageMean = value;
                }
                RaisePropertyChanged();
            }
        }

        private int randomImageStdDev;

        public int RandomImageStdDev {
            get => randomImageStdDev;
            set {
                lock (lockObj) {
                    randomImageStdDev = value;
                }
                RaisePropertyChanged();
            }
        }

        public void SetBinning(short x, short y) {
        }

        private IWindowService windowService;

        public IWindowService WindowService {
            get {
                if (windowService == null) {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set {
                windowService = value;
            }
        }

        public void SetupDialog() {
            WindowService.Show(this, "Simulator Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }

        private async Task<bool> LoadImage() {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Load Image";
            dialog.FileName = "Image";
            dialog.DefaultExt = ".tiff";

            if (dialog.ShowDialog() == true) {
                TiffBitmapDecoder TifDec = new TiffBitmapDecoder(new Uri(dialog.FileName), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                BitmapFrame bmp = TifDec.Frames[0];
                int stride = (bmp.PixelWidth * bmp.Format.BitsPerPixel + 7) / 8;
                int arraySize = stride * bmp.PixelHeight;
                ushort[] pixels = new ushort[(int)(bmp.Width * bmp.Height)];
                bmp.CopyPixels(pixels, stride, 0);
                Image = await ImageArray.CreateInstance(pixels, (int)bmp.Width, (int)bmp.Height, 16, false, true, profileService.ActiveProfile.ImageSettings.HistogramResolution);
                return true;
            }
            return false;
        }

        public IAsyncCommand LoadImageCommand { get; private set; }
        public ICommand UnloadImageCommand { get; private set; }

        private ImageArray _image;

        public ImageArray Image {
            get => _image;
            set {
                lock (lockObj) {
                    _image = value;
                }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CameraXSize));
                RaisePropertyChanged(nameof(CameraYSize));
            }
        }

        public void StartExposure(CaptureSequence captureSequence) {
        }

        public void StopExposure() {
        }

        public void UpdateValues() {
        }

        public void StartLiveView() {
            throw new NotImplementedException();
        }

        public Task<ImageArray> DownloadLiveView(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }
    }
}