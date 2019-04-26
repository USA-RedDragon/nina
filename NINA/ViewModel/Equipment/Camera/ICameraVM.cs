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

using NINA.Model;
using NINA.Model.MyCamera;
using System;
using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.Camera {

    public interface ICameraVM : IDeviceVM<CameraInfo> {

        void SetBinning(short x, short y);

        void SetGain(short gain);

        void SetSubSample(bool subSample);

        void SetSubSampleArea(int x, int y, int width, int height);

        void AbortExposure();
        
        bool AtTargetTemp { get; }

        double TargetTemp { get; }

        Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress);

        IAsyncEnumerable<ImageArray> LiveView(CancellationToken token);

        Task<ImageArray> Download(CancellationToken token, bool calculateStatistics);
    }
}