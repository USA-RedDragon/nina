﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Model.MyGuider.MetaGuide {

    [Flags]
    public enum CalibrationState {
        Unknown = 0,
        WestPierSide = 1,
        Rotate180OnFlip = 2,
        NorthSouthReverseOnFlip = 4,
        EastWestReverseOnFlip = 8,
        NorthSouthReversed = 16,
        EastWestReversed = 32,
        CalibratedFresh = 64,
        Calibrated = 128,
        Calibrating = 256
    }
}