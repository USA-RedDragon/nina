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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment.Switch {

    internal class SwitchTemplateSelector : DataTemplateSelector {
        public DataTemplate Writable { get; set; }
        public DataTemplate WritableBoolean { get; set; }
        public DataTemplate ReadOnly { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item is Model.MySwitch.IWritableSwitch) {
                var s = (Model.MySwitch.IWritableSwitch)item;
                if (s.Minimum == 0 && s.Maximum == 1) {
                    return WritableBoolean;
                } else {
                    return Writable;
                }
            } else {
                return ReadOnly;
            }
        }
    }
}