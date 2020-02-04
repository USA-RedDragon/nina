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

namespace NINA.Model.MyFocuser {

    public class FocuserInfo : DeviceInfo {
        private int position;

        public int Position {
            get { return position; }
            set { position = value; RaisePropertyChanged(); }
        }

        private double stepsize;

        public double StepSize {
            get { return stepsize; }
            set { stepsize = value; RaisePropertyChanged(); }
        }

        private double temperature;

        public double Temperature {
            get { return temperature; }
            set { temperature = value; RaisePropertyChanged(); }
        }

        private bool isMoving;

        public bool IsMoving {
            get { return isMoving; }
            set { isMoving = value; RaisePropertyChanged(); }
        }

        private bool isSettling;

        public bool IsSettling {
            get { return isSettling; }
            set { isSettling = value; RaisePropertyChanged(); }
        }

        private bool tempComp;

        public bool TempComp {
            get { return tempComp; }
            set { tempComp = value; RaisePropertyChanged(); }
        }

        private bool tempCompAvailable;

        public bool TempCompAvailable {
            get { return tempCompAvailable; }
            set { tempCompAvailable = value; RaisePropertyChanged(); }
        }
    }
}