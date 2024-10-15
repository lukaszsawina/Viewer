using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Stylet;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;


namespace Viewer.ViewModel
{
    public class ShellViewModel : Screen
    {
        private VideoCapture _videoCapture;
        private VideoWriter videoWriter;
        private CancellationTokenSource _cancellationTokenSource;
        public ObservableCollection<string> CameraList { get; set; } 
        private int _selectedCameraIndex; 

        public int SelectedCameraIndex
        {
            get => _selectedCameraIndex;
            set
            {
                _selectedCameraIndex = value;
                NotifyOfPropertyChange(() => SelectedCameraIndex);
            }
        }

        private BitmapSource _cameraImage;

        public BitmapSource CameraImage
        {
            get
            {
                if (!IsStarted)
                {
                    return LoadDefaultImage();
                }
                return _cameraImage ?? LoadDefaultImage();
            }
            set
            {
                _cameraImage = value;
                NotifyOfPropertyChange(() => CameraImage);
            }
        }

        private bool _isStarted;

        public bool IsStarted
        {
            get => _isStarted;
            set
            {
                SetAndNotify(ref _isStarted, value);
                NotifyOfPropertyChange(() => StartButtonVisible);
                NotifyOfPropertyChange(() => StopButtonVisible);
                NotifyOfPropertyChange(() => StartRecButtonVisible);
                NotifyOfPropertyChange(() => StopRecButtonVisible);
                NotifyOfPropertyChange(() => CameraImage);
            }
        }

        public bool StartButtonVisible => !IsStarted;
        public bool StopButtonVisible => IsStarted;

        private bool _isRecording;

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                SetAndNotify(ref _isRecording, value);
                NotifyOfPropertyChange(() => StartRecButtonVisible);
                NotifyOfPropertyChange(() => StopRecButtonVisible);
            }
        }

        public bool StartRecButtonVisible => (IsStarted && !IsRecording);
        public bool StopRecButtonVisible => (IsStarted && IsRecording);

        public ShellViewModel()
        {
            CameraList = new ObservableCollection<string>();
            LoadAvailableCameras();
        }

        private void LoadAvailableCameras()
        {
            for (int i = 0; i < 5; i++)
            {
                using (var cap = new VideoCapture(i))
                {
                    if (cap.IsOpened())
                    {
                        CameraList.Add($"Source {i}");
                    }
                }
            }

            if (CameraList.Count > 0)
            {
                SelectedCameraIndex = 0;
            }
            else
            {
                MessageBox.Show("No avaliable cameras");
            }
        }

        private BitmapImage LoadDefaultImage()
        {
            string solutionDir = AppDomain.CurrentDomain.BaseDirectory;
            string imagePath = Path.Combine(solutionDir, "Images/default.jpg");
            return new BitmapImage(new Uri(imagePath, UriKind.Absolute));
        }
        public void StartCamera()
        {
            _videoCapture = new VideoCapture(SelectedCameraIndex); 
            if (!_videoCapture.IsOpened())
            {
                MessageBox.Show("Nie można otworzyć kamery.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => CaptureFrames(_cancellationTokenSource.Token));

            IsStarted = true;
        }

        private async Task CaptureFrames(CancellationToken token)
        {
            using (var mat = new Mat())
            {
                while (!token.IsCancellationRequested)
                {
                    _videoCapture.Read(mat); 
                    if (!mat.Empty())
                    {
                        var bitmapImage = mat.ToBitmapSource();
                        bitmapImage.Freeze();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CameraImage = bitmapImage;
                        });

                        if (IsRecording && videoWriter != null)
                        {
                            videoWriter.Write(mat);
                        }
                    }

                    await Task.Delay(30); 
                }
            }
        }

        public void StopCamera()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_videoCapture != null)
            {
                _videoCapture.Release();
                _videoCapture.Dispose();
                _videoCapture = null;
            }

            if(IsRecording)
            {
                StopRecording();
            }


            IsStarted = false;
        }

        public void StartRecording()
        {
            if (!IsRecording)
            {
                string solutionDir = AppDomain.CurrentDomain.BaseDirectory;


                string fileName = Path.Combine(solutionDir, $"Recordings/video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
                videoWriter = new VideoWriter(fileName, FourCC.H264, 30, new OpenCvSharp.Size(640, 480));
                IsRecording = true;
            }
        }

        public void StopRecording()
        {
            if (IsRecording)
            {
                IsRecording = false;
                videoWriter.Release();
                videoWriter.Dispose();
                videoWriter = null;
            }
        }

        public void OpenFolder()
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recordings");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            Process.Start("explorer.exe", folderPath);
        }

        protected override void OnClose()
        {
            StopCamera();
            base.OnClose();
        }
    }
}