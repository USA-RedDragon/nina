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
using NINA.Model.MyRotator;
using NINA.Utility;
using NINA.Profile;
using System;

namespace NINA.ViewModel.Equipment.Rotator {

    internal class RotatorChooserVM : EquipmentChooserVM {

        public RotatorChooserVM(IProfileService profileService) : base(typeof(RotatorChooserVM), profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoRotator"]));

            try {
                foreach (IRotator rotator in ASCOMInteraction.GetRotators(profileService)) {
                    Devices.Add(rotator);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            Devices.Add(new ManualRotator(profileService));

            DetermineSelectedDevice(profileService.ActiveProfile.RotatorSettings.Id);
        }
    }
}