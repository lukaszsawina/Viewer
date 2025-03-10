﻿using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Stylet;
using StyletIoC;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Viewer.Events;
using Viewer.Model;
using Rect = OpenCvSharp.Rect;

namespace Viewer.ViewModel;
public class CameraImageViewModel: 
    Screen, 
    IHandle<StateChangeEvent>, 
    IHandle<CameraSourceChangeEvent>,
    IHandle<RecordingChangeEvent>
{
    private readonly IEventAggregator _eventAggregator;

    VideoCapture _videoCapture;
    private VideoWriter _videoWriter;
    private CancellationTokenSource _cancellationTokenSource;

    private long frameCount = 0;

    private BitmapSource _cameraImage;
    public BitmapSource CameraImage
    {
        get
        {
            if (!CurrentState.IsStarted)
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

    private StateModel _currentState;
    public StateModel CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState != null)
            {
                _currentState.PropertyChanged -= CurrentState_PropertyChanged;
            }

            _currentState = value;

            if (_currentState != null)
            {
                _currentState.
                    PropertyChanged += CurrentState_PropertyChanged;
            }

            NotifyOfPropertyChange(() => CurrentState);
        }
    }

    [Inject]
    public CameraImageViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        _eventAggregator.Subscribe(this);
    }

    private BitmapImage LoadDefaultImage()
    {
        string solutionDir = AppDomain.CurrentDomain.BaseDirectory;
        string imagePath = Path.Combine(solutionDir, "Images/default.jpg");
        return new BitmapImage(new Uri(imagePath, UriKind.Absolute));
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

                        if (CurrentState.IsRecording && _videoWriter != null)
                        {
                            _videoWriter.Write(mat);
                        }

                        if (!string.IsNullOrEmpty(CurrentState.WorkspaceFolderPath) )
                        {
                            frameCount++;
                            if (frameCount % 5 == 0)
                            await Task.Run(() => SaveFrameAsImage(mat, frameCount / 5));
                        }
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

    private void SaveFrameAsImage(Mat frame, long imageIndex)
    {
        string imagesFolderPath = Path.Combine(CurrentState.WorkspaceFolderPath, "images");

        if (!Directory.Exists(imagesFolderPath))
        {
            Directory.CreateDirectory(imagesFolderPath);
        }

        string filePath = Path.Combine(imagesFolderPath, $"{imageIndex}.jpg");

        int centerX = frame.Width / 2;
        int centerY = frame.Height / 2;
        int width = frame.Width / 2;
        int height = frame.Height / 2;

        Rect roi = new Rect(centerX - width / 2, centerY - height / 2, width, height);

        using (var croppedFrame = new Mat(frame, roi))
        {
            Cv2.ImWrite(filePath, croppedFrame);
        }
    }

    public void Handle(StateChangeEvent message)
    {
        CurrentState = message.State;

        if (CurrentState.WorkspaceFolderPath is not null)
            frameCount = 0;
    }

    public void Handle(CameraSourceChangeEvent message)
    {
        _videoCapture = message.VideoCapture;

        if( _videoCapture is null )
            StopCamera();
        else
            StartCamera();
    }

    public void Handle(RecordingChangeEvent message)
    {
        var fileName = message.RecordingPath;

        if(fileName is null )
            StopRecording();
        else
            StartRecording(fileName);
    }

    private void StartCamera()
    {
        try
        {
            if (!_videoCapture.IsOpened())
            {
                MessageBox.Show("Unable to open camera.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => CaptureFrames(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unable to open camera.");
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

        if (CurrentState.IsRecording)
        {
            StopRecording();
        }
    }

    private void StartRecording(string fileName)
    {
        if (!CurrentState.IsRecording)
        {
            _videoWriter = new VideoWriter(fileName, FourCC.H264, 30, new OpenCvSharp.Size(640, 480));
        }
    }

    private void StopRecording()
    {
        if (CurrentState.IsRecording)
        {
            _videoWriter.Release();
            _videoWriter.Dispose();
            _videoWriter = null;
        }
    }

    private void CurrentState_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StateModel.IsStarted))
        {
            NotifyOfPropertyChange(() => CameraImage);
        }
    }
}

