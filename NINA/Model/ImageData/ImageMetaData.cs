﻿using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.ImageData {

    public class ImageMetaData {
        public ImageParameter Image { get; set; } = new ImageParameter();
        public CameraParameter Camera { get; set; } = new CameraParameter();
        public TelescopeParameter Telescope { get; set; } = new TelescopeParameter();
        public FocuserParameter Focuser { get; set; } = new FocuserParameter();
        public RotatorParameter Rotator { get; set; } = new RotatorParameter();
        public FilterWheelParameter FilterWheel { get; set; } = new FilterWheelParameter();
        public TargetParameter Target { get; set; } = new TargetParameter();
        public ObserverParameter Observer { get; set; } = new ObserverParameter();

        /// <summary>
        /// Fill relevant info from a Profile
        /// </summary>
        /// <param name="profile"></param>
        public void FromProfile(IProfile profile) {
            Camera.PixelSize = profile.CameraSettings.PixelSize;

            Telescope.Name = profile.TelescopeSettings.Name;
            Telescope.FocalLength = profile.TelescopeSettings.FocalLength;
            Telescope.FocalRatio = profile.TelescopeSettings.FocalRatio;

            Observer.Latitude = profile.AstrometrySettings.Latitude;
            Observer.Longitude = profile.AstrometrySettings.Longitude;
        }

        public void FromCameraInfo(CameraInfo info) {
            if (info.Connected) {
                Camera.Temperature = info.Temperature;
                Camera.Gain = info.Gain;
                Camera.Offset = info.Offset;
                Camera.SetPoint = info.TemperatureSetPoint;
                Camera.BinX = info.BinX;
                Camera.BinY = info.BinY;
                Camera.ElectronsPerADU = info.ElectronsPerADU;
                Camera.PixelSize = info.PixelSize;
            }
        }

        public void FromTelescopeInfo(TelescopeInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(Telescope.Name)) {
                    Telescope.Name = info.Name;
                }
                Observer.Elevation = info.SiteElevation;
                Telescope.Coordinates = info.Coordinates;
            }
        }

        public void FromFilterWheelInfo(FilterWheelInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(FilterWheel.Name)) {
                    FilterWheel.Name = info.Name;
                }
                FilterWheel.Filter = info.SelectedFilter?.Name ?? string.Empty;
            }
        }

        public void FromFocuserInfo(FocuserInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(Focuser.Name)) {
                    Focuser.Name = info.Name;
                }
                Focuser.Position = info.Position;
                Focuser.StepSize = info.StepSize;
                Focuser.Temperature = info.Temperature;
            }
        }

        public void FromRotatorInfo(RotatorInfo info) {
            if (info.Connected) {
                if (string.IsNullOrWhiteSpace(Rotator.Name)) {
                    Rotator.Name = info.Name;
                }
                Rotator.Position = info.Position;
                Rotator.StepSize = info.StepSize;
            }
        }
    }

    public class ImageParameter {
        public DateTime ExposureStart { get; set; } = DateTime.MinValue;
        public int ExposureNumber { get; set; } = -1;
        public string ImageType { get; set; } = string.Empty;
        public string Binning { get; set; } = string.Empty;
        public double ExposureTime { get; set; } = double.NaN;
        public RMS RecordedRMS { get; set; } = null;
    }

    public class CameraParameter {
        public string Name { get; set; } = string.Empty;
        public string Binning { get => $"{BinX}x{BinY}"; }
        public int BinX { get; set; } = 1;
        public int BinY { get; set; } = 1;
        public double PixelSize { get; set; } = double.NaN;
        public double Temperature { get; set; } = double.NaN;
        public double Gain { get; set; } = double.NaN;
        public double Offset { get; set; } = double.NaN;
        public double ElectronsPerADU { get; set; } = double.NaN;
        public double SetPoint { get; set; } = double.NaN;
    }

    public class TelescopeParameter {
        public string Name { get; set; } = string.Empty;
        public double FocalLength { get; set; } = double.NaN;
        public double FocalRatio { get; set; } = double.NaN;
        public Coordinates Coordinates { get; set; } = null;
    }

    public class FocuserParameter {
        public string Name { get; set; } = string.Empty;
        public double Position { get; set; } = double.NaN;
        public double StepSize { get; set; } = double.NaN;
        public double Temperature { get; set; } = double.NaN;
    }

    public class RotatorParameter {
        public string Name { get; set; } = string.Empty;
        public double Position { get; set; } = double.NaN;
        public double StepSize { get; set; } = double.NaN;
    }

    public class FilterWheelParameter {
        public string Name { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
    }

    public class TargetParameter {
        public string Name { get; set; } = string.Empty;
        public Coordinates Coordinates { get; set; } = null;
    }

    public class ObserverParameter {
        public double Latitude { get; set; } = double.NaN;
        public double Longitude { get; set; } = double.NaN;
        public double Elevation { get; set; } = double.NaN;
    }
}