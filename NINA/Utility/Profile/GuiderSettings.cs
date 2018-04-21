﻿using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(ApplicationSettings))]
    public class GuiderSettings {

        private double ditherPixels = 5;
        [XmlElement(nameof(DitherPixels))]
        public double DitherPixels {
            get {
                return ditherPixels;
            }
            set {
                ditherPixels = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private bool ditherRAOnly = false;
        [XmlElement(nameof(DitherRAOnly))]
        public bool DitherRAOnly {
            get {
                return ditherRAOnly;
            }
            set {
                ditherRAOnly = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private int settleTime = 10;
        [XmlElement(nameof(SettleTime))]
        public int SettleTime {
            get {
                return settleTime;
            }
            set {
                settleTime = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private string pHD2ServerUrl = "localhost";
        [XmlElement(nameof(PHD2ServerUrl))]
        public string PHD2ServerUrl {
            get {
                return pHD2ServerUrl;
            }
            set {
                pHD2ServerUrl = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private int pHD2ServerPort = 4400;
        [XmlElement(nameof(PHD2ServerPort))]
        public int PHD2ServerPort {
            get {
                return pHD2ServerPort;
            }
            set {
                pHD2ServerPort = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}
