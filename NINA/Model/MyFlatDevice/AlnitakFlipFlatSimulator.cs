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

using NINA.Profile;
using NINA.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    internal class AlnitakFlipFlatSimulator : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;

        public AlnitakFlipFlatSimulator(IProfileService profileService) {
            _profileService = profileService;
            CoverState = CoverState.NeitherOpenNorClosed;
        }

        public bool HasSetupDialog => false;

        public string Id => "flip_flat_simulator";
        public string Name => "Flip-Flat Simulator";
        public string Category => "Alnitak Astrosystems";

        public bool Connected { get; private set; }

        public string Description => $"{Name} on port {PortName}. Firmware version: 200";
        public string DriverInfo => "Simulates an Alnitak FlipFlat.";
        public string DriverVersion => "1.0";

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            RaiseAllPropertiesChanged();
            return Task.Run(() => Connected, token);
        }

        public void Disconnect() {
            Connected = false;
        }

        public void SetupDialog() {
        }

        private CoverState _coverState;

        public CoverState CoverState {
            get => _coverState;
            private set {
                _coverState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LocalizedCoverState));
            }
        }

        public string LocalizedCoverState => Locale.Loc.Instance[$"LblFlatDevice{_coverState}"];

        public int MaxBrightness => 255;
        public int MinBrightness => 0;

        public async Task<bool> Open(CancellationToken ct) {
            if (!Connected) await Task.Run(() => false, ct);
            return await Task.Run(() => {
                _lightOn = false;
                CoverState = CoverState.NeitherOpenNorClosed;
                Thread.Sleep(2000);
                CoverState = CoverState.Open;
                return true;
            }, ct);
        }

        public async Task<bool> Close(CancellationToken ct) {
            if (!Connected) await Task.Run(() => false, ct);
            return await Task.Run(() => {
                CoverState = CoverState.NeitherOpenNorClosed;
                Thread.Sleep(2000);
                CoverState = CoverState.Closed;
                return true;
            }, ct);
        }

        private bool _lightOn;

        public bool LightOn {
            get {
                if (!Connected) {
                    return false;
                }

                return CoverState == CoverState.Closed && _lightOn;
            }
            set {
                if (!Connected) return;
                if (CoverState != CoverState.Closed) return;
                _lightOn = value;
                RaisePropertyChanged();
            }
        }

        private double _brightness;

        public double Brightness {
            get => !Connected ? 0 : _brightness;
            set {
                if (Connected) {
                    if (value < 0) {
                        value = 0;
                    }
                    if (value > 1) {
                        value = 1;
                    }
                    _brightness = value;
                }
                RaisePropertyChanged();
            }
        }

        public string PortName {
            get => "NO_PORT";
            set {
            }
        }

        public bool SupportsOpenClose => true;
    }
}