﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Core.Model.Equipment;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Utility.AutoFocus;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IAutoFocusVM : IDockableVM, IDisposable {
        double AverageContrast { get; }
        ICommand CancelAutoFocusCommand { get; }
        double ContrastStdev { get; }
        DataPoint FinalFocusPoint { get; set; }
        AsyncObservableCollection<ScatterErrorPoint> FocusPoints { get; set; }
        GaussianFitting GaussianFitting { get; set; }
        HyperbolicFitting HyperbolicFitting { get; set; }
        ReportAutoFocusPoint LastAutoFocusPoint { get; set; }
        AsyncObservableCollection<DataPoint> PlotFocusPoints { get; set; }
        QuadraticFitting QuadraticFitting { get; set; }
        ICommand StartAutoFocusCommand { get; }
        ApplicationStatus Status { get; set; }
        TrendlineFitting TrendlineFitting { get; set; }
        Boolean ChartListSelectable { get; }
        ICommand LoadChartCommand { get; }

        Task<AutoFocusReport> StartAutoFocus(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress);

        void UpdateDeviceInfo(CameraInfo cameraInfo);

        void UpdateDeviceInfo(FilterWheelInfo deviceInfo);

        void UpdateDeviceInfo(FocuserInfo focuserInfo);
    }
}