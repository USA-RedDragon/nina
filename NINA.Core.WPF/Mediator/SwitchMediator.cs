#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MySwitch;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Switch;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    public class SwitchMediator : DeviceMediator<ISwitchVM, ISwitchConsumer, SwitchInfo>, ISwitchMediator {

        public Task SetSwitchValue(short switchIndex, double value, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return handler.SetSwitchValue(switchIndex, value, progress, ct);
        }
    }
}