﻿using NINA.EquipmentChooser;
using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class TelescopeVM : DockableVM {
        public TelescopeVM() : base() {
            Title = Locale.Loc.Instance["LblTelescope"];
            ContentId = nameof(TelescopeVM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["TelescopeSVG"];
            
            ChooseTelescopeCommand = new RelayCommand(ChooseTelescope);
            DisconnectCommand = new RelayCommand(DisconnectTelescope);
            StepperMoveRateCommand = new RelayCommand(StepMoveRate);
            ParkCommand = new AsyncCommand<bool>(ParkTelescope);
            UnparkCommand = new RelayCommand(UnparkTelescope);
            SlewToCoordinatesCommand = new RelayCommand(SlewToCoordinates);
            RefreshTelescopeListCommand = new RelayCommand(RefreshTelescopeList);

            MoveCommand = new RelayCommand(Move);
            StopMoveCommand = new RelayCommand(StopMove);
            StopSlewCommand = new RelayCommand(StopSlew);

            _updateTelescope = new DispatcherTimer();
            _updateTelescope.Interval = TimeSpan.FromMilliseconds(300);
            _updateTelescope.Tick += UpdateTelescope_Tick;
        }

        private void RefreshTelescopeList(object obj) {
            TelescopeChooserVM.GetEquipment();
        }

        private void UpdateTelescope_Tick(object sender, EventArgs e) {            
            if (Telescope?.Connected == true) {
                Telescope.UpdateValues();
            }            
        }

        private async Task<bool> ParkTelescope() {
            return await Task.Run<bool>(() => { Telescope.Park(); return true; }); 

        }

        private void UnparkTelescope(object o) {
            Telescope.Unpark();
        }

        private DispatcherTimer _updateTelescope;

       private ITelescope _telescope;
        public ITelescope Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.TelescopeChanged, _telescope);
            }
        }

        private TelescopeChooserVM _telescopeChooserVM;
        public TelescopeChooserVM TelescopeChooserVM {
            get {
                if(_telescopeChooserVM == null) {
                    _telescopeChooserVM = new TelescopeChooserVM();
                }
                return _telescopeChooserVM;
            }
            set {
                _telescopeChooserVM = value;
            }
        }


        private void ChooseTelescope(object obj) {
            _updateTelescope.Stop();
            Telescope = (ITelescope)TelescopeChooserVM.SelectedDevice; 
            if (Telescope?.Connect() == true) {
                _updateTelescope.Start();
                Settings.TelescopeId = Telescope.Id;                
            } else {
                Telescope = null;
            }
        }

        private void DisconnectTelescope(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Telescope?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);            
            if (diag == System.Windows.MessageBoxResult.OK) {
                _updateTelescope.Stop();
                Telescope.Disconnect();
                Telescope = null;
            }
        }

        private void StepMoveRate(object obj) {
            string cmd = obj.ToString();
            if(cmd == "+") {
                Telescope.MovingRate++;
            } else {
                Telescope.MovingRate--;
            }
        }

        private void Move(object obj) {
            string cmd = obj.ToString();
            if(cmd == "W") {                                
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, -Telescope.MovingRate);
            }
            if (cmd == "O") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, Telescope.MovingRate);
            }
            if (cmd == "N") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, Telescope.MovingRate);
            }
            if (cmd == "S") {                
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, -Telescope.MovingRate);
            }
        }

        private void StopMove(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);
            }
            if (cmd == "O") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);
            }
            if (cmd == "N") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0);
            }
            if (cmd == "S") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0);
            }
        }

        private void StopSlew(object obj) {
            Telescope.StopSlew();
        }

        private int _targetDeclinationDegrees;        
        public int TargetDeclinationDegrees {
            get {
                return _targetDeclinationDegrees;
            }

            set {
                _targetDeclinationDegrees = value;
                RaisePropertyChanged();
            }
        }
        private int _targetDeclinationMinutes;
        public int TargetDeclinationMinutes {
            get {
                return _targetDeclinationMinutes;
            }

            set {
                _targetDeclinationMinutes = value;
                RaisePropertyChanged();
            }
        }
        private double _targetDeclinationSeconds;
        public double TargetDeclinationSeconds {
            get {
                return _targetDeclinationSeconds;
            }

            set {
                _targetDeclinationSeconds = value;
                RaisePropertyChanged();
            }
        }

        private int _targetRightAscencionHours;
        public int TargetRightAscencionHours {
            get {
                return _targetRightAscencionHours;
            }

            set {
                _targetRightAscencionHours = value;
                RaisePropertyChanged();
            }
        }
        private int _targetRightAscencionMinutes;
        public int TargetRightAscencionMinutes {
            get {
                return _targetRightAscencionMinutes;
            }

            set {
                _targetRightAscencionMinutes = value;
                RaisePropertyChanged();
            }
        }
        private double _targetRightAscencionSeconds;
        public double TargetRightAscencionSeconds {
            get {
                return _targetRightAscencionSeconds;
            }

            set {
                _targetRightAscencionSeconds = value;
                RaisePropertyChanged();
            }
        }

        private void SlewToCoordinates(object obj) {
            var targetRightAscencion = Utility.Utility.AscomUtil.HMSToHours(TargetRightAscencionHours + ":" + TargetRightAscencionMinutes + ":" + TargetRightAscencionSeconds);
            var targetDeclination = Utility.Utility.AscomUtil.HMSToHours(TargetDeclinationDegrees + ":" + TargetDeclinationMinutes + ":" + TargetDeclinationSeconds);
            Telescope.SlewToCoordinatesAsync(targetRightAscencion, targetDeclination);
        }
        
        public ICommand SlewToCoordinatesCommand { get; private set; }

        public ICommand ChooseTelescopeCommand { get; private set; }
        
        public ICommand DisconnectCommand { get; private set; }
        
        public ICommand MoveCommand { get; private set; }
        
        public ICommand StopMoveCommand { get; private set; }
        
        public IAsyncCommand ParkCommand { get; private set; }
        
        public ICommand UnparkCommand { get; private set; }
        
        public ICommand StopSlewCommand { get; private set; }
        
        public ICommand StepperMoveRateCommand { get; private set; }
        
        public ICommand RefreshTelescopeListCommand { get; private set; }
    }

    class TelescopeChooserVM : EquipmentChooserVM {
        public override void GetEquipment() {
            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Telescope")) {

                try {
                    AscomTelescope cam = new AscomTelescope(device.Key, device.Value);
                    Devices.Add(cam);
                } catch (Exception) {
                    //only add telescopes which are supported. e.g. x86 drivers will not work in x64
                }
            }

            if (Devices.Count > 0) {
                var selected = (from device in Devices where device.Id == Settings.TelescopeId select device).First();
                SelectedDevice = selected;
            }
        }
    }
}
