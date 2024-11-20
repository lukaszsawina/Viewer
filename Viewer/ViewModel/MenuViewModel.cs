using OpenCvSharp;
using Stylet;
using StyletIoC;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Viewer.Events;
using Viewer.Model;

namespace Viewer.ViewModel;

public class MenuViewModel : Screen
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IWindowManager _windowManager;

    public ObservableCollection<string> CameraList { get; set; } = new ObservableCollection<string>();

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

    public bool StartButtonVisible => !CurrentState.IsStarted;
    public bool StopButtonVisible => CurrentState.IsStarted;


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

    public bool StartedWorkspaceCaptureVisible => !IsStartedWorkspaceCapture && CurrentState.IsStarted;
    public bool StopedWorkspaceCaptureVisible => IsStartedWorkspaceCapture && CurrentState.IsStarted;

    public bool StartRecButtonVisible => (CurrentState.IsStarted && !CurrentState.IsRecording);
    public bool StopRecButtonVisible => (CurrentState.IsStarted && CurrentState.IsRecording);


    private string _currentText;
    public string CurrentText
    {
        get => _currentText;
        set
        {
            
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

    [Inject]
    public MenuViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
    {
        _eventAggregator = eventAggregator;
        _windowManager = windowManager;
        CurrentState = InitializeState();
        CurrentText = "Select camera source and press \"Start\"";
        LoadAvailableCameras();
    }

    private StateModel InitializeState()
    {
        var state = new StateModel()
        {
            IsStarted = false,
            IsRecording = false,
            WorkspaceFolderPath = null
        };

        _eventAggregator.Publish(new StateChangeEvent(state));

        return state;
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

    public void StartCamera()
    {
        var videoCapture = new VideoCapture(SelectedCameraIndex);

        _eventAggregator.Publish(new CameraSourceChangeEvent(videoCapture));

        CurrentState.IsStarted = true;
        CurrentText = "To stop capturing press \"Stop\", \nTo start capturing the surroundings press \"Start workspace capture\", \nTo record video press \"Record\"";
    }

    public void StopCamera()
    {
        CurrentState.IsStarted = false;
        CurrentText = "Select camera source and press \"Start\"";

        if(IsStartedWorkspaceCapture)
            StopWorkspaceCapture();

        if (CurrentState.IsRecording)
            StopRecording();

        _eventAggregator.Publish(new CameraSourceChangeEvent(null));
    }

    public void StartWorkspaceCapture()
    {
        string date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        CurrentState.WorkspaceFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Workspaces/Workspace_{date}");
        Directory.CreateDirectory(CurrentState.WorkspaceFolderPath);

        string imagesFolderPath = Path.Combine(CurrentState.WorkspaceFolderPath, "images");
        Directory.CreateDirectory(imagesFolderPath);

        IsStartedWorkspaceCapture = true;
        _eventAggregator.Publish(new StateChangeEvent(CurrentState));
    }

    public void StopWorkspaceCapture()
    {
        var view = new WorkspaceProcessingViewModel();
        _windowManager.ShowWindow(view);

        CurrentState.WorkspaceFolderPath = null;
        IsStartedWorkspaceCapture = false;
        _eventAggregator.Publish(new StateChangeEvent(CurrentState));
    }

    public void StartRecording()
    {
        if (!CurrentState.IsRecording)
        {
            string solutionDir = AppDomain.CurrentDomain.BaseDirectory;
            string fileName = Path.Combine(solutionDir, $"Recordings/video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            _eventAggregator.Publish(new RecordingChangeEvent(fileName));
            CurrentState.IsRecording = true;
            _eventAggregator.Publish(new StateChangeEvent(CurrentState));
        }
    }

    public void StopRecording()
    {
        if (CurrentState.IsRecording)
            _eventAggregator.Publish(new RecordingChangeEvent(null));
        
        CurrentState.IsRecording = false;

        _eventAggregator.Publish(new StateChangeEvent(CurrentState));
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
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspaces");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        Process.Start("explorer.exe", folderPath);
    }

    private void CurrentState_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StateModel.IsStarted))
        {
            NotifyOfPropertyChange(() => StartButtonVisible);
            NotifyOfPropertyChange(() => StopButtonVisible);
            NotifyOfPropertyChange(() => StartedWorkspaceCaptureVisible);
            NotifyOfPropertyChange(() => StopedWorkspaceCaptureVisible);
            NotifyOfPropertyChange(() => StartRecButtonVisible);
            NotifyOfPropertyChange(() => StopRecButtonVisible);
        }

        if (e.PropertyName == nameof(StateModel.IsRecording))
        {
            NotifyOfPropertyChange(() => StartRecButtonVisible);
            NotifyOfPropertyChange(() => StopRecButtonVisible);
        }
    }
}


