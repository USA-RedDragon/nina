﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Models.Regression.Linear;
using NINA.Utility;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.AutoFocus {

    public class QuadraticFitting : BaseINPC {

        public QuadraticFitting() {
        }

        private Func<double, double> _fitting;

        public Func<double, double> Fitting {
            get {
                return _fitting;
            }
            set {
                _fitting = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _minimum;

        public DataPoint Minimum {
            get {
                return _minimum;
            }
            set {
                _minimum = value;
                RaisePropertyChanged();
            }
        }

        public QuadraticFitting Calculate(ICollection<ScatterErrorPoint> points) {
            var fitting = new PolynomialLeastSquares() { Degree = 2 };
            PolynomialRegression poly = fitting.Learn(points.Select((dp) => dp.X).ToArray(), points.Select((dp) => dp.Y).ToArray(), points.Select((dp) => 1 / (dp.ErrorY * dp.ErrorY)).ToArray());
            Fitting = (x) => (poly.Weights[0] * x * x + poly.Weights[1] * x + poly.Intercept);
            int minimumX = (int)Math.Round(poly.Weights[1] / (2 * poly.Weights[0]) * -1);
            double minimumY = Fitting(minimumX);
            Minimum = new DataPoint(minimumX, minimumY);
            return this;
        }
    }
}