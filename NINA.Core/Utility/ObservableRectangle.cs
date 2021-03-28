#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using System;

namespace NINA.Utility {

    public class ObservableRectangle : BaseINPC {

        public ObservableRectangle(double x, double y, double width, double height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public ObservableRectangle(double rotationOffset) {
            _rotationOffset = rotationOffset;
        }

        private double _x;

        public double X {
            get {
                return _x;
            }
            set {
                _x = value;
                RaisePropertyChanged();
            }
        }

        private double _y;

        public double Y {
            get {
                return _y;
            }
            set {
                _y = value;
                RaisePropertyChanged();
            }
        }

        private double _width;

        public double Width {
            get {
                return _width;
            }
            set {
                _width = value;
                RaisePropertyChanged();
            }
        }

        private double _height;

        public double Height {
            get {
                return _height;
            }
            set {
                _height = value;
                RaisePropertyChanged();
            }
        }

        private double _rotation;

        public double Rotation {
            get {
                return _rotation;
            }
            set {
                _rotation = AstroUtil.MathMod(value, 360);
                if (_rotation < 0) { _rotation += 360; }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TotalRotation));
            }
        }

        private readonly double _rotationOffset;

        public double TotalRotation {
            get {
                var rotation = _rotationOffset + Rotation;
                rotation = AstroUtil.MathMod(rotation, 360);
                return Math.Round(rotation, 2);
            }
            set {
                //This will rise property changed for TotalRotation
                Rotation = value - _rotationOffset;
            }
        }
    }
}