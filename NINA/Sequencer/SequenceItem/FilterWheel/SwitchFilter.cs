﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord;
using Newtonsoft.Json;
using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Profile;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.FilterWheel {

    [ExportMetadata("Name", "Lbl_SequenceItem_FilterWheel_SwitchFilter_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FilterWheel_SwitchFilter_Description")]
    [ExportMetadata("Icon", "FW_NoFill_SVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_FilterWheel")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SwitchFilter : SequenceItem, IValidatable {

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            this.Filter = this.profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Name == this.Filter?.Name);
        }

        [ImportingConstructor]
        public SwitchFilter(IProfileService profileservice, IFilterWheelMediator filterWheelMediator) {
            this.profileService = profileservice;
            this.filterWheelMediator = filterWheelMediator;
        }

        private IProfileService profileService;
        private IFilterWheelMediator filterWheelMediator;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private FilterInfo filter;

        [JsonProperty]
        public FilterInfo Filter {
            get => filter;
            set {
                filter = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Filter == null) {
                throw new SequenceItemSkippedException("Skipping SwitchFilter - No Filter was selected");
            }
            if (Validate()) {
                return this.filterWheelMediator.ChangeFilter(Filter, token, progress);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public bool Validate() {
            var i = new List<string>();
            if (filter != null && !filterWheelMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblFilterWheelNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override object Clone() {
            return new SwitchFilter(profileService, filterWheelMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Filter = Filter
            };
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SwitchFilter)}, Filter: {Filter?.Name}";
        }
    }
}