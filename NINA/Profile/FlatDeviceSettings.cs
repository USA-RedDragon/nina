#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NINA.Model.MyFilterWheel;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    internal class FlatDeviceSettings : Settings, IFlatDeviceSettings {

        public FlatDeviceSettings() {
            FilterSettings = new Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (FilterSettings == null) {
                FilterSettings =
                    new Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue>();
            }
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
        }

        private string _id;

        [DataMember]
        public string Id {
            get => _id;
            set {
                if (_id == value) return;
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        [DataMember]
        public string Name {
            get => _name;
            set {
                if (_name == value) return;
                _name = value;
                RaisePropertyChanged();
            }
        }

        private string _portName;

        [DataMember]
        public string PortName {
            get => _portName;
            set {
                if (_portName == value) return;
                _portName = value;
                RaisePropertyChanged();
            }
        }

        private bool _closeAtSequenceEnd;

        [DataMember]
        public bool CloseAtSequenceEnd {
            get => _closeAtSequenceEnd;
            set {
                if (_closeAtSequenceEnd == value) return;
                _closeAtSequenceEnd = value;
                RaisePropertyChanged();
            }
        }

        private bool _openForDarkFlats;

        [DataMember]
        public bool OpenForDarkFlats {
            get => _openForDarkFlats;
            set {
                if (_openForDarkFlats == value) return;
                _openForDarkFlats = value;
                RaisePropertyChanged();
            }
        }

        private bool _useWizardTrainedValues;

        [DataMember]
        public bool UseWizardTrainedValues {
            get => _useWizardTrainedValues;
            set {
                if (_useWizardTrainedValues == value) return;
                _useWizardTrainedValues = value;
                RaisePropertyChanged();
            }
        }

        private Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> _filterSettings;

        [DataMember]
        public Dictionary<FlatDeviceFilterSettingsKey, FlatDeviceFilterSettingsValue> FilterSettings {
            get => _filterSettings;
            set {
                _filterSettings = value;
                RaisePropertyChanged();
            }
        }

        public void AddBrightnessInfo(FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) {
            if (FilterSettings.ContainsKey(key)) {
                FilterSettings[key] = value;
            } else {
                FilterSettings.Add(key, value);
            }

            RaisePropertyChanged(nameof(FilterSettings));
        }

        public FlatDeviceFilterSettingsValue GetBrightnessInfo(FlatDeviceFilterSettingsKey key) {
            return FilterSettings.ContainsKey(key) ? FilterSettings[key] : null;
        }

        public IEnumerable<BinningMode> GetBrightnessInfoBinnings() {
            var result = FilterSettings.Keys.Select(key => key.Binning).ToList();

            return result.Distinct();
        }

        public IEnumerable<int> GetBrightnessInfoGains() {
            var result = FilterSettings.Keys.Select(key => key.Gain).ToList();

            return result.Distinct();
        }

        public void RemoveGain(int gain, ICollection<FilterInfo> filters) {
            var filterNames = filters?.Select(filter => filter.Name) ?? new List<string> { null };
            var keysToRemove = FilterSettings.Keys
                .Where(key => key.Gain == gain && filterNames.Contains(key.FilterName)).ToList();

            foreach (var key in keysToRemove) {
                FilterSettings.Remove(key);
            }
            RaisePropertyChanged(nameof(FilterSettings));
        }

        public void RemoveBinning(BinningMode binning, ICollection<FilterInfo> filters) {
            var filterNames = filters?.Select(filter => filter.Name).ToList() ?? new List<string> { null };
            var keysToRemove = new List<FlatDeviceFilterSettingsKey>();
            foreach (var key in FilterSettings.Keys) {
                switch (key.Binning) {
                    case null when binning is null && filterNames.Contains(key.FilterName):
                        keysToRemove.Add(key);
                        break;

                    case BinningMode mode when mode.Equals(binning) && filterNames.Contains(key.FilterName):
                        keysToRemove.Add(key);
                        break;
                }
            }

            foreach (var key in keysToRemove) {
                FilterSettings.Remove(key);
            }
            RaisePropertyChanged(nameof(FilterSettings));
        }
    }

    [Serializable()]
    [DataContract]
    public class FlatDeviceFilterSettingsKey {

        [DataMember]
        public short? Position { get; }

        [DataMember]
        public BinningMode Binning { get; }

        [DataMember]
        public int Gain { get; }

        public FlatDeviceFilterSettingsKey(short? position, BinningMode binning, int gain) {
            Position = position;
            Binning = binning;
            Gain = gain;
        }

        public override bool Equals(object obj) {
            if (obj == null || this.GetType() != obj.GetType()) {
                return false;
            }

            var other = (FlatDeviceFilterSettingsKey)obj;
            switch (Binning) {
                case null when other.Binning == null:
                    return Position == other.Position && Gain == other.Gain;

                case null:
                    return false;

                default:
                    return Position == other.Position && Binning.Equals(other.Binning) && Gain == other.Gain;
            }
        }

        public override int GetHashCode() {
            //see https://en.wikipedia.org/wiki/Hash_function
            const int primeNumber = 397;
            unchecked {
                var hashCode = Position != null ? Position.GetHashCode() : 0;
                hashCode = (hashCode * primeNumber) ^ (Binning != null ? Binning.GetHashCode() : 0);
                hashCode = (hashCode * primeNumber) ^ Gain.GetHashCode();
                return hashCode;
            }
        }
    }

    [Serializable()]
    [DataContract]
    public class FlatDeviceFilterSettingsValue {

        [DataMember]
        public double Brightness { get; set; }

        [DataMember]
        public double Time { get; set; }

        public FlatDeviceFilterSettingsValue(double brightness, double time) {
            Brightness = brightness;
            Time = time;
        }
    }
}