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
using System.Runtime.Serialization;

#pragma warning disable 1998

namespace NINA.Model.MyGuider.PHD2 {
    /// <summary>
    /// This class is used to send over camera data from the guider client to this service.
    /// </summary>
    [DataContract]
    public class ProfileCameraState {

        [DataMember]
        public DateTime ExposureEndTime { get; set; }

        [DataMember]
        public Guid InstanceId { get; set; }

        [DataMember]
        public bool IsExposing { get; set; }

        [DataMember]
        public double LastDownloadTime { get; set; }

        [DataMember]
        public double NextExposureTime { get; set; }
    }
}