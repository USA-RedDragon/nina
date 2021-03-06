#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyRotator;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Interfaces;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class RotatorMediator : DeviceMediator<IRotatorVM, IRotatorConsumer, RotatorInfo>, IRotatorMediator {

        public void Sync(float skyAngle) {
            handler.Sync(skyAngle);
        }

        public Task<float> Move(float position) {
            return handler.Move(position);
        }

        public Task<float> MoveRelative(float position) {
            return handler.MoveRelative(position);
        }
    }
}