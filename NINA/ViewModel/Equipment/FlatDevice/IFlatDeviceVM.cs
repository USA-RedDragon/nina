#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyFlatDevice;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.FlatDevice {

    public interface IFlatDeviceVM : IDeviceVM<FlatDeviceInfo>, IDockableVM {

        Task<bool> OpenCover();

        Task<bool> CloseCover();

        double Brightness { get; set; }
        bool LightOn { get; set; }
        FlatDeviceInfo FlatDeviceInfo { get; set; }

        void ToggleLight(object o);

        void SetBrightness(double value);

        void SetBrightness(object o);
    }
}