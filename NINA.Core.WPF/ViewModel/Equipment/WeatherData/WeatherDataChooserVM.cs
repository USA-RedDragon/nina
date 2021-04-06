#region "copyright"

/*
    Copyright � 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyWeatherData;
using NINA.Utility;
using NINA.Profile;
using System;
using System.Collections.Generic;

namespace NINA.ViewModel.Equipment.WeatherData {

    public class WeatherDataChooserVM : DeviceChooserVM {

        public WeatherDataChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            lock (lockObj) {
                var devices = new List<Model.IDevice>();

                devices.Add(new DummyDevice(Locale.Loc.Instance["LblWeatherNoSource"]));

                try {
                    foreach (IWeatherData obsdev in ASCOMInteraction.GetWeatherDataSources(profileService)) {
                        devices.Add(obsdev);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.Add(new OpenWeatherMap(this.profileService));
                devices.Add(new UltimatePowerboxV2(profileService));
                devices.Add(new TheWeatherCompany(this.profileService));
                devices.Add(new WeatherUnderground(this.profileService));

                Devices = devices;
                DetermineSelectedDevice(profileService.ActiveProfile.WeatherDataSettings.Id);
            }
        }
    }
}