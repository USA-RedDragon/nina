﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Locale;
using NINA.Model.ImageData;
using NINA.Utility.WindowService;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.ViewModel.FlatWizard {

    public class FlatWizardExposureTimeFinderService : IFlatWizardExposureTimeFinderService {
        private List<ScatterErrorPoint> dataPoints = new List<ScatterErrorPoint>();

        public IWindowService WindowService { get; set; } = new WindowService();

        public ILoc Locale { get; set; } = Loc.Instance;

        public void ClearDataPoints() {
            dataPoints = new List<ScatterErrorPoint>();
        }

        public async Task<FlatWizardUserPromptVMResponse> EvaluateUserPromptResultAsync(IImageData imageData, double exposureTime, string message, FlatWizardFilterSettingsWrapper wrapper) {
            var imageStatistics = await imageData.Statistics.Task;
            var flatsWizardUserPrompt = new FlatWizardUserPromptVM(
                message,
                imageStatistics.Mean,
                CameraBitDepthToAdu(wrapper.BitDepth),
                wrapper,
                exposureTime);
            await WindowService.ShowDialog(flatsWizardUserPrompt, Locale["LblFlatUserPromptFailure"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

            if (flatsWizardUserPrompt.Reset) {
                ClearDataPoints();
            }

            return new FlatWizardUserPromptVMResponse() {
                Continue = flatsWizardUserPrompt.Continue,
                NextExposureTime = flatsWizardUserPrompt.Reset ? wrapper.Settings.MinFlatExposureTime : exposureTime
            };
        }

        public double GetExpectedExposureTime(FlatWizardFilterSettingsWrapper wrapper) {
            var trendLine = new TrendLine(dataPoints);

            return (wrapper.Settings.HistogramMeanTarget * CameraBitDepthToAdu(wrapper.BitDepth) - trendLine.Offset) / trendLine.Slope;
        }

        public async Task<FlatWizardExposureAduState> GetFlatExposureState(IImageData imageData, double exposureTime, FlatWizardFilterSettingsWrapper wrapper) {
            var histogramMeanAdu = HistogramMeanAndCameraBitDepthToAdu(wrapper.Settings.HistogramMeanTarget, wrapper.BitDepth);
            var histogramToleranceUpperBound = GetUpperToleranceAduFromAdu(histogramMeanAdu, wrapper.Settings.HistogramTolerance);
            var histogramToleranceLowerBound = GetLowerToleranceAduFromAdu(histogramMeanAdu, wrapper.Settings.HistogramTolerance);
            var imageStatistics = await imageData.Statistics.Task;
            var currentMean = imageStatistics.Mean;

            if (histogramToleranceLowerBound <= currentMean && histogramToleranceUpperBound >= currentMean) {
                return FlatWizardExposureAduState.ExposureFinished;
            }

            if (currentMean > histogramToleranceUpperBound) {
                return FlatWizardExposureAduState.ExposureAduAboveMean;
            }

            return FlatWizardExposureAduState.ExposureAduBelowMean;
        }

        public FlatWizardExposureTimeState GetNextFlatExposureState(double exposureTime, FlatWizardFilterSettingsWrapper wrapper) {
            if (exposureTime > wrapper.Settings.MaxFlatExposureTime) {
                return FlatWizardExposureTimeState.ExposureTimeAboveMaxTime;
            }

            if (exposureTime < wrapper.Settings.MinFlatExposureTime) {
                return FlatWizardExposureTimeState.ExposureTimeBelowMinTime;
            }

            return FlatWizardExposureTimeState.ExposureTimeWithinBounds;
        }

        public double GetNextExposureTime(double exposureTime, FlatWizardFilterSettingsWrapper wrapper) {
            if (dataPoints.Count >= 2) {
                return GetExpectedExposureTime(wrapper);
            }

            return exposureTime + wrapper.Settings.StepSize;
        }

        public static double CameraBitDepthToAdu(double cameraBitDepth) {
            return Math.Pow(2, cameraBitDepth);
        }

        public static double HistogramMeanAndCameraBitDepthToAdu(double histogramMean, double cameraBitDepth) {
            return histogramMean * CameraBitDepthToAdu(cameraBitDepth);
        }

        public static double GetLowerToleranceAduFromAdu(double histogramMeanAdu, double tolerance) {
            return histogramMeanAdu - histogramMeanAdu * tolerance;
        }

        public static double GetUpperToleranceAduFromAdu(double histogramMeanAdu, double tolerance) {
            return histogramMeanAdu + histogramMeanAdu * tolerance;
        }

        public static double GetLowerToleranceBoundInAdu(double histogramMean, double cameraBitDepth, double tolerance) {
            return GetLowerToleranceAduFromAdu(HistogramMeanAndCameraBitDepthToAdu(histogramMean, cameraBitDepth), tolerance);
        }

        public static double GetUpperToleranceBoundInAdu(double histogramMean, double cameraBitDepth, double tolerance) {
            return GetUpperToleranceAduFromAdu(HistogramMeanAndCameraBitDepthToAdu(histogramMean, cameraBitDepth), tolerance);
        }

        public void AddDataPoint(double exposureTime, double mean) {
            dataPoints.Add(new ScatterErrorPoint(exposureTime, mean, 1, 1));
        }
    }

    public struct FlatWizardUserPromptVMResponse {
        public bool Continue;
        public double NextExposureTime;
    }
}