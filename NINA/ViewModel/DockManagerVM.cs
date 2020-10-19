#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Profile;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using NINA.ViewModel.Equipment.FilterWheel;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Imaging;
using NINA.ViewModel.Equipment.Dome;
using NINA.ViewModel.Equipment.Switch;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Equipment.WeatherData;
using NINA.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.Sequencer;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Equipment.SafetyMonitor;

namespace NINA.ViewModel {

    internal class DockManagerVM : BaseVM, IDockManagerVM {

        public DockManagerVM(IProfileService profileService, ICameraVM cameraVM, ISequence2VM sequence2VM,
            IThumbnailVM thumbnailVM, ISwitchVM switchVM, IFilterWheelVM filterWheelVM, IFocuserVM focuserVM, IRotatorVM rotatorVM,
            IWeatherDataVM weatherDataVM, IDomeVM domeVM, IAnchorableSnapshotVM snapshotVM,
            IPolarAlignmentVM polarAlignmentVM, IAnchorablePlateSolverVM plateSolverVM, ITelescopeVM telescopeVM, IGuiderVM guiderVM,
            IFocusTargetsVM focusTargetsVM, IAutoFocusVM autoFocusVM, IExposureCalculatorVM exposureCalculatorVM, IImageHistoryVM imageHistoryVM,
            IImageControlVM imageControlVM, IImageStatisticsVM imageStatisticsVM, IFlatDeviceVM flatDeviceVM, ISafetyMonitorVM safetyMonitorVM) : base(profileService) {
            LoadAvalonDockLayoutCommand = new RelayCommand(LoadAvalonDockLayout);
            ResetDockLayoutCommand = new RelayCommand(ResetDockLayout, (object o) => _dockmanager != null);

            Anchorables.Add(imageControlVM);
            Anchorables.Add(cameraVM);
            Anchorables.Add(filterWheelVM);
            Anchorables.Add(focuserVM);
            Anchorables.Add(rotatorVM);
            Anchorables.Add(telescopeVM);
            Anchorables.Add(guiderVM);
            Anchorables.Add(switchVM);
            Anchorables.Add(weatherDataVM);
            Anchorables.Add(domeVM);

            Anchorables.Add(sequence2VM);
            Anchorables.Add(imageStatisticsVM);
            Anchorables.Add(imageHistoryVM);

            Anchorables.Add(snapshotVM);
            Anchorables.Add(thumbnailVM);
            Anchorables.Add(plateSolverVM);
            Anchorables.Add(polarAlignmentVM);
            Anchorables.Add(autoFocusVM);
            Anchorables.Add(focusTargetsVM);
            Anchorables.Add(exposureCalculatorVM);
            Anchorables.Add(flatDeviceVM);
            Anchorables.Add(safetyMonitorVM);

            AnchorableInfoPanels.Add(imageControlVM);
            AnchorableInfoPanels.Add(cameraVM);
            AnchorableInfoPanels.Add(filterWheelVM);
            AnchorableInfoPanels.Add(focuserVM);
            AnchorableInfoPanels.Add(rotatorVM);
            AnchorableInfoPanels.Add(telescopeVM);
            AnchorableInfoPanels.Add(guiderVM);
            AnchorableInfoPanels.Add(sequence2VM);
            AnchorableInfoPanels.Add(switchVM);
            AnchorableInfoPanels.Add(weatherDataVM);
            AnchorableInfoPanels.Add(domeVM);
            AnchorableInfoPanels.Add(imageStatisticsVM);
            AnchorableInfoPanels.Add(imageHistoryVM);
            AnchorableInfoPanels.Add(flatDeviceVM);
            AnchorableInfoPanels.Add(safetyMonitorVM);

            AnchorableTools.Add(snapshotVM);
            AnchorableTools.Add(thumbnailVM);
            AnchorableTools.Add(plateSolverVM);
            AnchorableTools.Add(polarAlignmentVM);
            AnchorableTools.Add(autoFocusVM);
            AnchorableTools.Add(focusTargetsVM);
            AnchorableTools.Add(exposureCalculatorVM);

            ClosingCommand = new RelayCommand(ClosingApplication);
        }

        private void ResetDockLayout(object arg) {
            if (MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblResetDockLayoutConfirmation"], Locale.Loc.Instance["LblResetDockLayout"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                lock (lockObj) {
                    _dockloaded = false;

                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                    serializer.LayoutSerializationCallback += (s, args) => {
                        var d = (DockableVM)args.Content;
                        d.IsVisible = true;
                        args.Content = d;
                    };

                    LoadDefaultLayout(serializer);
                }
            }
        }

        private ObservableCollection<IDockableVM> _anchorables;

        public ObservableCollection<IDockableVM> Anchorables {
            get {
                if (_anchorables == null) {
                    _anchorables = new ObservableCollection<IDockableVM>();
                }
                return _anchorables;
            }
            private set {
                _anchorables = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<IDockableVM> _anchorableTools;

        public ObservableCollection<IDockableVM> AnchorableTools {
            get {
                if (_anchorableTools == null) {
                    _anchorableTools = new ObservableCollection<IDockableVM>();
                }
                return _anchorableTools;
            }
            private set {
                _anchorableTools = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<IDockableVM> _anchorableInfoPanels;

        public ObservableCollection<IDockableVM> AnchorableInfoPanels {
            get {
                if (_anchorableInfoPanels == null) {
                    _anchorableInfoPanels = new ObservableCollection<IDockableVM>();
                }
                return _anchorableInfoPanels;
            }
            private set {
                _anchorableInfoPanels = value;
                RaisePropertyChanged();
            }
        }

        private Xceed.Wpf.AvalonDock.DockingManager _dockmanager;
        private bool _dockloaded = false;
        private object lockObj = new object();

        public void LoadAvalonDockLayout(object o) {
            lock (lockObj) {
                if (!_dockloaded) {
                    _dockmanager = (Xceed.Wpf.AvalonDock.DockingManager)o;

                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                    serializer.LayoutSerializationCallback += (s, args) => {
                        args.Content = args.Content;
                    };

                    if (System.IO.File.Exists(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH)) {
                        try {
                            serializer.Deserialize(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH);
                            _dockloaded = true;
                        } catch (Exception ex) {
                            Logger.Error("Failed to load AvalonDock Layout. Loading default Layout!", ex);
                            using (var stream = new StringReader(Properties.Resources.avalondock)) {
                                serializer.Deserialize(stream);
                            }
                        }
                    } else {
                        LoadDefaultLayout(serializer);
                    }
                }
            }
        }

        private void LoadDefaultLayout(Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer) {
            lock (lockObj) {
                using (var stream = new StringReader(Properties.Resources.avalondock)) {
                    serializer.Deserialize(stream);
                    _dockloaded = true;
                }
            }
        }

        public void SaveAvalonDockLayout() {
            lock (lockObj) {
                if (_dockloaded) {
                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                    serializer.Serialize(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH);
                }
            }
        }

        private void ClosingApplication(object o) {
            try {
                SaveAvalonDockLayout();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public ICommand LoadAvalonDockLayoutCommand { get; private set; }
        public ICommand ResetDockLayoutCommand { get; }

        public ICommand ClosingCommand { get; private set; }
    }
}