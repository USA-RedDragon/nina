﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyPlanetarium;
using NINA.Profile;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Switch;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Data;

namespace NINA.Sequencer {

    public class SequencerFactory {
        private CompositionContainer container;

        public IList<IDateTimeProvider> DateTimeProviders { get; private set; }

        public IList<ISequenceItem> Items { get; private set; }

        public IList<ISequenceCondition> Conditions { get; private set; }
        public IList<ISequenceTrigger> Triggers { get; private set; }
        public IList<ISequenceContainer> Container { get; private set; }

        [ImportMany(typeof(ISequenceItem))]
        public IEnumerable<Lazy<ISequenceItem, Dictionary<string, object>>> ItemImports { get; private set; }

        [ImportMany(typeof(ISequenceCondition))]
        public IEnumerable<Lazy<ISequenceCondition, Dictionary<string, object>>> ConditionImports { get; private set; }

        [ImportMany(typeof(ISequenceTrigger))]
        public IEnumerable<Lazy<ISequenceTrigger, Dictionary<string, object>>> TriggerImports { get; private set; }

        [ImportMany(typeof(ISequenceContainer))]
        public IEnumerable<Lazy<ISequenceContainer, Dictionary<string, object>>> ContainerImports { get; private set; }

        [ImportMany(typeof(ResourceDictionary))]
        public IEnumerable<ResourceDictionary> DataTemplateImports { get; private set; }

        public SequencerFactory(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IFocuserMediator focuserMediator,
                IFilterWheelMediator filterWheelMediator,
                IGuiderMediator guiderMediator,
                IRotatorMediator rotatorMediator,
                IFlatDeviceMediator flatDeviceMediator,
                IWeatherDataMediator weatherDataMediator,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator,
                INighttimeCalculator nighttimeCalculator,
                IPlanetariumFactory planetariumFactory,
                IImageHistoryVM imageHistoryVM,
                IDeepSkyObjectSearchVM deepSkyObjectSearchVM,
                IDomeMediator domeMediator,
                IImageSaveMediator imageSaveMediator,
                ISwitchMediator switchMediator,
                ISafetyMonitorMediator safetyMonitorMediator,
                IApplicationResourceDictionary resourceDictionary,
                IApplicationMediator applicationMediator,
                IFramingAssistantVM framingAssistantVM
        ) {
            this.DateTimeProviders = new ObservableCollection<IDateTimeProvider>() {
                new TimeProvider(),
                new SunsetProvider(nighttimeCalculator),
                new DuskProvider(nighttimeCalculator),
                new DawnProvider(nighttimeCalculator),
                new SunriseProvider(nighttimeCalculator)
            };

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(ISequenceEntity).Assembly));

            var extensionsFolder = Path.Combine(NINA.Utility.Utility.APPLICATIONDIRECTORY, "Plugins");
            if (Directory.Exists(extensionsFolder)) {
                catalog.Catalogs.Add(new DirectoryCatalog(extensionsFolder));
            }

            container = new CompositionContainer(catalog);
            container.ComposeExportedValue(profileService);
            container.ComposeExportedValue(cameraMediator);
            container.ComposeExportedValue(telescopeMediator);
            container.ComposeExportedValue(focuserMediator);
            container.ComposeExportedValue(filterWheelMediator);
            container.ComposeExportedValue(guiderMediator);
            container.ComposeExportedValue(rotatorMediator);
            container.ComposeExportedValue(flatDeviceMediator);
            container.ComposeExportedValue(weatherDataMediator);
            container.ComposeExportedValue(imagingMediator);
            container.ComposeExportedValue(applicationStatusMediator);
            container.ComposeExportedValue(nighttimeCalculator);
            container.ComposeExportedValue(planetariumFactory);
            container.ComposeExportedValue(imageHistoryVM);
            container.ComposeExportedValue(deepSkyObjectSearchVM);
            container.ComposeExportedValue(domeMediator);
            container.ComposeExportedValue(imageSaveMediator);
            container.ComposeExportedValue(switchMediator);
            container.ComposeExportedValue(resourceDictionary);
            container.ComposeExportedValue(DateTimeProviders);
            container.ComposeExportedValue(safetyMonitorMediator);
            container.ComposeExportedValue(applicationMediator);
            container.ComposeExportedValue(framingAssistantVM);

            container.ComposeParts(this);

            foreach (var template in DataTemplateImports) {
                Application.Current?.Resources.MergedDictionaries.Add(template);
            }

            Items = new ObservableCollection<ISequenceItem>(Assign(ItemImports, resourceDictionary));
            Conditions = new ObservableCollection<ISequenceCondition>(Assign(ConditionImports, resourceDictionary));
            Triggers = new ObservableCollection<ISequenceTrigger>(Assign(TriggerImports, resourceDictionary));
            Container = new ObservableCollection<ISequenceContainer>(Assign(ContainerImports, resourceDictionary));

            var instructions = new List<ISequenceEntity>();
            foreach (var item in Items) {
                instructions.Add(item);
            }
            foreach (var condition in Conditions) {
                instructions.Add(condition);
            }
            foreach (var trigger in Triggers) {
                instructions.Add(trigger);
            }

            ItemsView = CollectionViewSource.GetDefaultView(instructions);
            ItemsView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            ItemsView.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
            ItemsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            ItemsView.Filter += new Predicate<object>(ApplyViewFilter);
        }

        private bool ApplyViewFilter(object obj) {
            return (obj as ISequenceEntity).Name.IndexOf(ViewFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string viewFilter = string.Empty;

        public string ViewFilter {
            get => viewFilter;
            set {
                viewFilter = value;
                ItemsView.Refresh();
            }
        }

        public ICollectionView ItemsView { get; set; }

        private string GrabLabel(string label) {
            if (label.StartsWith("Lbl_")) {
                return Locale.Loc.Instance[label];
            } else {
                return label;
            }
        }

        private IOrderedEnumerable<T> Assign<T>(IEnumerable<Lazy<T, Dictionary<string, object>>> imports, IApplicationResourceDictionary resourceDictionary) where T : ISequenceEntity {
            var items = new List<T>();
            foreach (var importItem in imports) {
                var item = importItem.Value;
                if (importItem.Metadata.TryGetValue("Name", out var nameObj)) {
                    string name = nameObj.ToString();
                    item.Name = GrabLabel(name);
                }
                if (importItem.Metadata.TryGetValue("Description", out var descriptionObj)) {
                    string description = descriptionObj.ToString();
                    item.Description = GrabLabel(description);
                }
                if (importItem.Metadata.TryGetValue("Icon", out var iconObj)) {
                    string icon = iconObj.ToString();
                    item.Icon = (System.Windows.Media.GeometryGroup)resourceDictionary[icon];
                }
                if (importItem.Metadata.TryGetValue("Category", out var categoryObj)) {
                    string category = categoryObj.ToString();
                    item.Category = GrabLabel(category);
                }
                items.Add(item);
            }
            return items.OrderBy(item => item.Category + item.Name);
        }

        public T GetContainer<T>() where T : ISequenceContainer {
            return (T)Container.FirstOrDefault(x => x.GetType() == typeof(T)).Clone();
        }

        public T GetItem<T>() where T : ISequenceItem {
            return (T)Items.FirstOrDefault(x => x.GetType() == typeof(T)).Clone();
        }

        public T GetCondition<T>() where T : ISequenceCondition {
            return (T)Conditions.FirstOrDefault(x => x.GetType() == typeof(T)).Clone();
        }

        public T GetTrigger<T>() where T : ISequenceTrigger {
            return (T)Triggers.FirstOrDefault(x => x.GetType() == typeof(T)).Clone();
        }
    }
}