﻿using ASCOM.DriverAccess;
using AstrophotographyBuddy.Utility;
using AstrophotographyBuddy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace AstrophotographyBuddy {
    class CameraVM : BaseVM {

        public CameraVM() {
            Name = "Camera";
            ImageURI = @"/AstrophotographyBuddy;component/Resources/Camera.png";
            //ConnectCameraCommand = new RelayCommand(connectCamera);
            ChooseCameraCommand = new RelayCommand(chooseCamera);
            DisconnectCommand = new RelayCommand(disconnectCamera);
            CoolCamCommand = new RelayCommand(coolCamera);
            CancelCoolCamCommand = new RelayCommand(cancelCoolCamera);
            Cam = new CameraModel();
            updateCamera = new DispatcherTimer();
            updateCamera.Interval = TimeSpan.FromMilliseconds(1000);
            updateCamera.Tick += updateCamera_Tick;

            CoolCameraTimer = new DispatcherTimer();
            CoolCameraTimer.Tick += coolCamera_Tick;
            CoolCameraTimer.Interval = TimeSpan.FromMilliseconds(300);
            CoolingRunning = false;
        }

        private void coolCamera_Tick(object sender, EventArgs e) {           

            double currentTemp = Cam.CCDTemperature;
            double deltaTemp = currentTemp - TargetTemp;

            
            DateTime now = DateTime.Now;
            TimeSpan delta = now.Subtract(deltaT);

            Duration = Duration - ((double)delta.TotalMilliseconds / (1000 * 60));

            //double newTemp = GetY(_startPoint, _endPoint, Duration);
            double newTemp = GetY(_startPoint, _endPoint, new Vector2(-_startPoint.X, _startPoint.Y), Duration);
            Cam.SetCCDTemperature = newTemp;
           
            CoolingProgress = 1 - (Duration / _initalDuration);

            deltaT = DateTime.Now;

            if (Duration <= 0) {
                Duration = 0;
                Cam.SetCCDTemperature = TargetTemp;
                CoolingRunning = false;
                CoolCameraTimer.Stop();
                
            }
        }

        private class Vector2 {
            public double X;
            public double Y;

            public Vector2(double x, double y) {
                X = x;
                Y = y;
            }
        }

        private double GetY(Vector2 point1, Vector2 point2, double x) {
            var m = (point2.Y - point1.Y) / (point2.X - point1.X);
            var b = point1.Y - (m * point1.X);

            return m * x + b;
        }

        private FilterWheelVM _filterWheelVM;
        public FilterWheelVM FilterWheelVM {
            get {
                if (_filterWheelVM == null) {
                    _filterWheelVM = new FilterWheelVM();                    
                }
                return _filterWheelVM;
            }
            set {
                _filterWheelVM = value;
                RaisePropertyChanged();
            }
        }

        private double GetY(Vector2 point1, Vector2 point2, Vector2 point3, double x) {            
            double denom = (point1.X - point2.X) * (point1.X - point3.X) * (point2.X - point3.X);
            double A = (point3.X * (point2.Y - point1.Y) + point2.X * (point1.Y - point3.Y) + point1.X * (point3.Y - point2.Y)) / denom;
            double B = (point3.X * point3.X * (point1.Y - point2.Y) + point2.X * point2.X * (point3.Y - point1.Y) + point1.X * point1.X * (point2.Y - point3.Y)) / denom;
            double C = (point2.X * point3.X * (point2.X - point3.X) * point1.Y + point3.X * point1.X * (point3.X - point1.X) * point2.Y + point1.X * point2.X * (point1.X - point2.X) * point3.Y) / denom;
            
            return (A * Math.Pow(x, 2) + B * x + C);
        }

        private Vector2 _startPoint;
        private Vector2 _endPoint;

        private double _initalDuration;
        private double _coolingProgress;
        public double CoolingProgress {
            get {
                return _coolingProgress;
            }

            set {
                _coolingProgress = value;
                RaisePropertyChanged();
            }
        }


        private DateTime deltaT;

        private bool _coolingRunning;
        public bool CoolingRunning {
            get {
                return _coolingRunning;
            } set {
                _coolingRunning = value;
                RaisePropertyChanged();
            }
        }

        private void coolCamera(object o) {
            Cam.CoolerOn = true;
            if (Duration == 0) {                            
                Cam.SetCCDTemperature = TargetTemp;
            } else {
                deltaT = DateTime.Now;
                double currentTemp = Cam.CCDTemperature;
                _startPoint = new Vector2(Duration, currentTemp);
                _endPoint = new Vector2(0, TargetTemp);
                Cam.SetCCDTemperature = currentTemp;
                _initalDuration = Duration;
                CoolCameraTimer.Start();
                CoolingRunning = true;
            }

        }

        private void cancelCoolCamera(object o) {
            CoolCameraTimer.Stop();
            CoolingRunning = false;
            Cam.SetCCDTemperature = Cam.CCDTemperature;
        }
            

        DispatcherTimer updateCamera;

        private DispatcherTimer _coolCameraTimer;
        public DispatcherTimer CoolCameraTimer {
            get {
                return _coolCameraTimer;
            }
            private set {
                _coolCameraTimer = value;
                RaisePropertyChanged();
            }
        }



        private double _targetTemp;
        public double TargetTemp {
            get {
                return _targetTemp;
            }
            set {
                _targetTemp = value;
                RaisePropertyChanged();
            }
        }

        private double _duration;
        public double Duration {
            get {
                return _duration;
            }
            set {
                _duration = value;
                RaisePropertyChanged();
            }
        }


        private CameraModel _cam;
        public CameraModel Cam {
            get {
                return _cam;
            }
            set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        /*private void connectCamera(object obj) {
            Cam.AscomCamera = new Camera(Cam.ProgId);
        }*/

        private void chooseCamera(object obj) {
            updateCamera.Stop();
            if (Cam.connect()) {                
                updateCamera.Start();
            }
            
        }

        private void disconnectCamera(object obj) {
            updateCamera.Stop();
            CoolCameraTimer.Stop();   
            Cam.disconnect();            
        }

        void updateCamera_Tick(object sender, EventArgs e) {
           if(Cam.Connected) {
                Cam.updateValues();
            }
            
        }
        
        /*private ICommand _connectCameraCommand;
        public ICommand ConnectCameraCommand {
            get {
                return _connectCameraCommand;
            }
            set {
                _connectCameraCommand = value;
                RaisePropertyChanged();
            }
        } */

        private ICommand _coolCamCommand;
        public ICommand CoolCamCommand {
            get {
                return _coolCamCommand;
            }
            set {
                _coolCamCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _chooseCameraCommand;
        public ICommand ChooseCameraCommand {
            get {
                return _chooseCameraCommand; 
            } set {
                _chooseCameraCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _disconnectCommand;
        public ICommand DisconnectCommand {
            get {
                return _disconnectCommand;
            }
            set {
                _disconnectCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _cancelCoolCommand;
        public ICommand CancelCoolCamCommand {
            get { return _cancelCoolCommand; }
            private set { _cancelCoolCommand = value; RaisePropertyChanged(); } }

        
    }
}
