﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Camera {

    [ExportMetadata("Name", "Lbl_SequenceItem_Camera_CoolCamera_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Camera_CoolCamera_Description")]
    [ExportMetadata("Icon", "SnowflakeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class CoolCamera : SequenceItem, IValidatable {

        [ImportingConstructor]
        public CoolCamera(ICameraMediator cameraMediator) {
            this.cameraMediator = cameraMediator;
        }

        private ICameraMediator cameraMediator;

        private double temperature = 0;

        [JsonProperty]
        public double Temperature {
            get => temperature;
            set {
                temperature = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double Duration { get; set; } = 0;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                return cameraMediator.CoolCamera(Temperature, TimeSpan.FromMinutes(Duration), progress, token);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public bool Validate() {
            var i = new List<string>();
            var info = cameraMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Locale.Loc.Instance["LblCameraNotConnected"]);
            } else if (!info.CanSetTemperature) {
                i.Add(Locale.Loc.Instance["Lbl_SequenceItem_Validation_CameraCannotSetTemperature"]);
            }

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override TimeSpan GetEstimatedDuration() {
            return Duration > 0 ? TimeSpan.FromMinutes(Duration) : TimeSpan.FromMinutes(1);
        }

        public override object Clone() {
            return new CoolCamera(cameraMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Temperature = Temperature,
                Duration = Duration
            };
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(CoolCamera)}, Temperature: {Temperature}, Duration: {Duration}";
        }
    }
}