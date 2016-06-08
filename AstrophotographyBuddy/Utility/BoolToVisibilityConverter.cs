﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AstrophotographyBuddy.Utility {
    class BoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            System.Windows.Visibility result;
            if ((bool)value == true) {
                result = System.Windows.Visibility.Visible;
            } else {
                result = System.Windows.Visibility.Hidden;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            bool result;
            if((System.Windows.Visibility) value == System.Windows.Visibility.Visible) {
                result = true;
            } else {
                result = false;
            }
            return result;
        }
    }
}
