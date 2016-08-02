﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AstrophotographyBuddy.Model {
    interface IPlateSolver {
        Task<PlateSolveResult> blindSolve(BitmapSource source);
    }
    public class PlateSolveResult {
        BitmapSource _solvedImage;
        double _orientation;
        double _pixscale;
        double _radius;
        double _ra;
        double _dec;

        public double Orientation {
            get {
                return _orientation;
            }

            set {
                _orientation = value;
            }
        }

        public double Pixscale {
            get {
                return _pixscale;
            }

            set {
                _pixscale = value;
            }
        }

        public double Radius {
            get {
                return _radius;
            }

            set {
                _radius = value;
            }
        }

        public double Ra {
            get {
                return _ra;
            }

            set {
                _ra = value;
            }
        }

        public double Dec {
            get {
                return _dec;
            }

            set {
                _dec = value;
            }
        }

        public string RaString {
            get {
                return Utility.Utility.AscomUtil.DegreesToHMS(Ra);
            }
        }

        public string DecString {
            get {
                return Utility.Utility.AscomUtil.DegreesToDMS(Dec);
            }
        }

        public BitmapSource SolvedImage {
            get {
                return _solvedImage;
            }

            set {
                _solvedImage = value;
            }
        }
    }
}
