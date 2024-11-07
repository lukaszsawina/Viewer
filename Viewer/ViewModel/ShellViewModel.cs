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
using System.Diagnostics;


namespace Viewer.ViewModel
{
    public class ShellViewModel : Screen
    {
        private VideoCapture _videoCapture;
        private VideoWriter videoWriter;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly IWindowManager windowManager;
        private string workspaceFolderPath;
        private int frameCount = 0;

        public ObservableCollection<string> CameraList { get; set; }

        private string _currentText;

        public string CurrentText
        {
            get => _currentText;
            set
            {
                _currentText = value;
                NotifyOfPropertyChange(() => CurrentText);
            }
        }

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
                NotifyOfPropertyChange(() => StartedWorkspaceCaptureVisible);
                NotifyOfPropertyChange(() => StopedWorkspaceCaptureVisible);
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

        private bool _isStartedWorkspaceCapture = false;

        public bool IsStartedWorkspaceCapture
        {
            get => _isStartedWorkspaceCapture;
            set
            {
                SetAndNotify(ref _isStartedWorkspaceCapture, value);
                NotifyOfPropertyChange(() => StartedWorkspaceCaptureVisible);
                NotifyOfPropertyChange(() => StopedWorkspaceCaptureVisible);
            }
        }

        public bool StartedWorkspaceCaptureVisible => !IsStartedWorkspaceCapture && IsStarted;
        public bool StopedWorkspaceCaptureVisible => IsStartedWorkspaceCapture && IsStarted;

        public ShellViewModel(IWindowManager windowManager)
        {
            CurrentText = "Select camera source and press \"Start\"";
            CameraList = new ObservableCollection<string>();
            LoadAvailableCameras();
            this.windowManager = windowManager;
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
            try
            {
                _videoCapture = new VideoCapture(SelectedCameraIndex);
                if (!_videoCapture.IsOpened())
                {
                    MessageBox.Show("Unable to open camera.");
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => CaptureFrames(_cancellationTokenSource.Token));

                IsStarted = true;
                CurrentText = "To stop capturing press \"Stop\", \nTo start capturing the surroundings press \"Start workspace capture\", \nTo record video press \"Record\"";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open camera");
            }
        }

        private async Task CaptureFrames(CancellationToken token)
        {
            using (var mat = new Mat())
            {
                while (!token.IsCancellationRequested)
                {
                    try
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

                            if (!string.IsNullOrEmpty(workspaceFolderPath) && frameCount % 10 == 0)
                            {
                                await Task.Run(() => SaveFrameAsImage(mat, frameCount / 10));
                            }

                            frameCount++;
                        }
                        else
                        {
                            MessageBox.Show("Unable to fetch video from camera.");
                            StopCamera();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to fetch video from camera.");
                        await Task.Delay(100);
                    }

                    await Task.Delay(30);
                }
            }
        }

        private void SaveFrameAsImage(Mat frame, int imageIndex)
        {
            string imagesFolderPath = Path.Combine(workspaceFolderPath, "Images");
            string filePath = Path.Combine(imagesFolderPath, $"{imageIndex}.jpg");

            Cv2.ImWrite(filePath, frame);
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

            if (IsRecording)
            {
                StopRecording();
            }

            workspaceFolderPath = null;
            IsStarted = false;
            CurrentText = "Select camera source and press \"Start\"";
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

        public void StartWorkspaceCapture()
        {
            string date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            workspaceFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Files/Workspace_{date}");
            Directory.CreateDirectory(workspaceFolderPath);

            string imagesFolderPath = Path.Combine(workspaceFolderPath, "Images");
            Directory.CreateDirectory(imagesFolderPath);

            IsStartedWorkspaceCapture = true;
        }

        public void StopWorkspaceCapture()
        {
            OpenVisualisation();
            workspaceFolderPath = null;
            IsStartedWorkspaceCapture = false;
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

        public void OpenFolderWorkspaces()
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            Process.Start("explorer.exe", folderPath);
        }

        public void OpenVisualisation()
        {
            var view = new VisualisationViewModel();

            this.windowManager.ShowWindow(view);
        }

        protected override void OnClose()
        {
            StopCamera();
            base.OnClose();
        }
    }
}