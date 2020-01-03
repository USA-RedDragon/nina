﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class TelescopeLatLongSyncVM {

        public TelescopeLatLongSyncVM(
                bool canTelescopeSync,
                double nINALatitude,
                double nINALongitude,
                double telescopeLatitude,
                double telescopeLongitude) {
            SyncTelescopeCommand = new RelayCommand(SyncTelescope, (object o) => canTelescopeSync);
            SyncNINACommand = new RelayCommand(SyncNINA);
            SyncNoneCommand = new RelayCommand(SyncNone);
            this.NINALatitude = nINALatitude;
            this.NINALongitude = nINALongitude;
            this.TelescopeLatitude = telescopeLatitude;
            this.TelescopeLongitude = telescopeLongitude;
        }

        public double NINALatitude { get; private set; }
        public double NINALongitude { get; private set; }
        public double TelescopeLatitude { get; private set; }
        public double TelescopeLongitude { get; private set; }

        public enum LatLongSyncMode {
            NONE,
            TELESCOPE,
            NINA
        }

        public LatLongSyncMode Mode { get; set; }

        private void SyncNone(object obj) {
            Mode = LatLongSyncMode.NONE;
        }

        private void SyncNINA(object obj) {
            Mode = LatLongSyncMode.NINA;
        }

        private void SyncTelescope(object obj) {
            Mode = LatLongSyncMode.TELESCOPE;
        }

        public ICommand SyncTelescopeCommand { get; set; }
        public ICommand SyncNINACommand { get; set; }
        public ICommand SyncNoneCommand { get; set; }
    }
}