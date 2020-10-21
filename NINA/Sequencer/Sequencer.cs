﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Trigger;
using NINA.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.Serialization;
using Nito.Mvvm;
using NINA.Sequencer.Validations;

namespace NINA.Sequencer {

    public class Sequencer : BaseINPC {

        public Sequencer(
            ISequenceRootContainer sequenceRootContainer
        ) {
            MainContainer = sequenceRootContainer;
        }

        private ISequenceRootContainer mainContainer;

        public ISequenceRootContainer MainContainer {
            get => mainContainer;
            set {
                mainContainer = value;
                RaisePropertyChanged();
            }
        }

        public Task Start(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Task.Run(async () => {
                if (!PromptForIssues()) {
                    return false;
                }

                await MainContainer.Run(progress, token);
                return true;
            });
        }

        private bool PromptForIssues() {
            var issues = Validate(MainContainer).Distinct();

            if (issues.Count() > 0) {
                var builder = new StringBuilder();
                builder.AppendLine(Locale.Loc.Instance["LblPreSequenceChecklist"]).AppendLine();

                foreach (var issue in issues) {
                    builder.Append("  - ");
                    builder.AppendLine(issue);
                }

                builder.AppendLine();
                builder.Append(Locale.Loc.Instance["LblStartSequenceAnyway"]);

                var diag = MyMessageBox.MyMessageBox.Show(
                    builder.ToString(),
                    Locale.Loc.Instance["LblPreSequenceChecklistHeader"],
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxResult.Cancel
                );
                if (diag == System.Windows.MessageBoxResult.Cancel) {
                    return false;
                }
            }
            return true;
        }

        private IList<string> Validate(ISequenceContainer container) {
            List<string> issues = new List<string>();
            foreach (var item in container.Items) {
                if (item is IValidatable) {
                    var v = item as IValidatable;
                    v.Validate();
                    issues.AddRange(v.Issues);
                }

                if (item is ISequenceContainer) {
                    if (item is IConditionable) {
                        foreach (var condition in (item as IConditionable).Conditions) {
                            if (condition is IValidatable) {
                                var v = condition as IValidatable;
                                v.Validate();
                                issues.AddRange(v.Issues);
                            }
                        }
                    }

                    if (item is ITriggerable) {
                        foreach (var trigger in (item as ITriggerable).Triggers) {
                            if (trigger is IValidatable) {
                                var v = trigger as IValidatable;
                                v.Validate();
                                issues.AddRange(v.Issues);
                            }
                        }
                    }

                    issues.AddRange(Validate(item as ISequenceContainer));
                }
            }
            return issues;
        }
    }
}