#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Threading.Tasks;
using NINA.Model.MyFlatDevice;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.FlatDevice;

namespace NINA.Utility.Mediator {

    internal class FlatDeviceMediator : DeviceMediator<IFlatDeviceVM, IFlatDeviceConsumer, FlatDeviceInfo>, IFlatDeviceMediator {

        public void SetBrightness(double brightness) {
            handler.SetBrightness(brightness);
        }

        public Task CloseCover() {
            return handler.CloseCover();
        }

        public void ToggleLight(object o) {
            handler.ToggleLight(o);
        }

        public Task OpenCover() {
            return handler.OpenCover();
        }
    }
}