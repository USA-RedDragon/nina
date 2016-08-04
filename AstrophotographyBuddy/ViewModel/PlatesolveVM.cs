﻿using AstrophotographyBuddy.Model;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AstrophotographyBuddy.ViewModel {
    class PlatesolveVM : BaseVM {

        public PlatesolveVM() {
            Name = "Plate Solving";
            Progress = "Idle...";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];
            Platesolver = new AstrometryPlateSolver();
            BlindSolveCommand = new AsyncCommand<bool>(() => blindSolve());
            CancelBlindSolveCommand = new RelayCommand(cancelBlindSolve);
            SyncCommand = new RelayCommand(syncTelescope);

            
                    
        }

        private void imageChanged(Object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Image") {
                this.PlateSolveResult = null;
            }
        }

        private void syncTelescope(object obj) {
            if(PlateSolveResult != null) {
                Telescope.sync(PlateSolveResult.RaString, PlateSolveResult.DecString);
            }
        }

        private string _progress;
        public string Progress {
            get {
                return _progress;
            }
            set {
                _progress = value;
                RaisePropertyChanged();
            }
        }

        

        private async Task<bool> blindSolve() {
            BitmapSource source = ImagingVM.Image;
            BitmapFrame image = null;
            /* Resize Image */
            if (!Settings.UseFullResolutionPlateSolve && source.Width > 1400) {
                var factor = 1400 / source.Width;
                int width = (int)(source.Width * factor);
                int height = (int)(source.Height * factor);
                var margin = 0;
                var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

                var group = new DrawingGroup();
                RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
                group.Children.Add(new ImageDrawing(source, rect));

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                    drawingContext.DrawDrawing(group);

                var resizedImage = new RenderTargetBitmap(
                    width, height,         // Resized dimensions
                    96, 96,                // Default DPI values
                    PixelFormats.Default); // Default pixel format
                resizedImage.Render(drawingVisual);

                image = BitmapFrame.Create(resizedImage);
            }
            else {
                image = BitmapFrame.Create(source);
            }



            /* Read image into memorystream */
            var ms = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(image);
            encoder.QualityLevel = 100;
            encoder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return await Task<bool>.Run(async () => {

                _blindeSolveCancelToken = new CancellationTokenSource();
                PlateSolveResult = await Platesolver.blindSolve(ms, new Progress<string>(p => Progress = p), _blindeSolveCancelToken);
                return true;
            });           
            
        }

        private CancellationTokenSource _blindeSolveCancelToken;
        private void cancelBlindSolve(object o) {
            if (_blindeSolveCancelToken != null) {
                _blindeSolveCancelToken.Cancel();
            }
        }

        IPlateSolver _platesolver;
        IPlateSolver Platesolver {
            get {
                return _platesolver;
            }
            set {
                _platesolver = value;
                RaisePropertyChanged();
            }

        }

        private PlateSolveResult _plateSolveResult;

        private ImagingVM _imagingVM;
        public ImagingVM ImagingVM {
            get {
                return _imagingVM;
            }
            set {
                _imagingVM = value;
                RaisePropertyChanged();
                ImagingVM.PropertyChanged += new PropertyChangedEventHandler(imageChanged);
            }
        }

        private TelescopeModel _telescope;
        public TelescopeModel Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private IAsyncCommand _blindSolveCommand;
        public IAsyncCommand BlindSolveCommand {
            get {
                return _blindSolveCommand;
            }
            set {
                _blindSolveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _cancelBlindSolveCommand;
        public ICommand CancelBlindSolveCommand {
            get {
                return _cancelBlindSolveCommand;
            }
            set {
                _cancelBlindSolveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _syncCommand;
        public ICommand SyncCommand {
            get {
                return _syncCommand;
            }
            set {
                _syncCommand = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolveResult PlateSolveResult {
            get {
                return _plateSolveResult;
            }

            set {
                _plateSolveResult = value;
                RaisePropertyChanged();
            }
        }
    }
}
