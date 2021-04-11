#region "copyright"

/*
    Copyright ? 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Core.Utility.Converters {

    public class WeatherRainRateConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            string output;
            string hour = Locale.Loc.Instance["LblHour"];

            if ((bool)values[1]) {
                output = string.Format("{0:0.00} in/{1}", (double)values[0] / 25.4, hour);
            } else {
                output = string.Format("{0:0.00} mm/{1}", values[0], hour);
            }

            return output;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}