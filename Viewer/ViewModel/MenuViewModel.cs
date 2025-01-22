using Microsoft.Win32;
using OpenCvSharp;
using Stylet;
using StyletIoC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Viewer.Events;
using Viewer.Model;

namespace Viewer.ViewModel;

public class MenuViewModel : Screen
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IWindowManager _windowManager;

    public WorkspaceProcessingViewModel _workspaceProcessingViewModel { get; set; }
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

    [Inject]
    public MenuViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
    {
        _eventAggregator = eventAggregator;
        _windowManager = windowManager;
        CurrentState = InitializeState();
        CurrentText = "Select camera source and press \"Start\"";
        _ = RefreshCameras();
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

    public async Task RefreshCameras()
    {
        var updatedCameraList = await Task.Run(() =>
        {
            var cameras = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                using (var cap = new VideoCapture(i))
                {
                    if (cap.IsOpened())
                    {
                        cameras.Add($"Source {i}");
                    }
                }
            }
            return cameras;
        });

        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var camera in updatedCameraList)
            {
                if (!CameraList.Contains(camera))
                {
                    CameraList.Add(camera);
                }
            }

            for (int i = CameraList.Count - 1; i >= 0; i--)
            {
                if (!updatedCameraList.Contains(CameraList[i]))
                {
                    CameraList.RemoveAt(i);
                }
            }

            if (CameraList.Count > 0 && (SelectedCameraIndex < 0 || SelectedCameraIndex >= CameraList.Count))
            {
                SelectedCameraIndex = 0;
            }
        });

        if (CameraList.Count == 0)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("No available cameras");
            });
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
        string mainDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Workspaces/Workspace_{DateTime.Now.ToString("yyyMMdd")}");

        if(!Path.Exists(mainDictionaryPath))
            Directory.CreateDirectory(mainDictionaryPath); 

        CurrentState.WorkspaceFolderPath = Path.Combine(mainDictionaryPath, $"Workspace_{DateTime.Now.ToString("HHmmss")}");
        Directory.CreateDirectory(CurrentState.WorkspaceFolderPath);

        string imagesFolderPath = Path.Combine(CurrentState.WorkspaceFolderPath, "images");
        Directory.CreateDirectory(imagesFolderPath);

        IsStartedWorkspaceCapture = true;
        _eventAggregator.Publish(new StateChangeEvent(CurrentState));
    }

    public void StopWorkspaceCapture()
    {
        _workspaceProcessingViewModel = new WorkspaceProcessingViewModel(_currentState.WorkspaceFolderPath);

        _windowManager.ShowWindow(_workspaceProcessingViewModel);
        
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

    public void OpenWorkspaceProcessing()
    {
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspaces");

        var folderDialog = new OpenFolderDialog
        {
            InitialDirectory = folderPath
        };

        if (folderDialog.ShowDialog() == true)
        {
            if(CheckWorkspaceDirectory(folderDialog.FolderName))
            {
                _workspaceProcessingViewModel = new WorkspaceProcessingViewModel(folderDialog.FolderName);

                _windowManager.ShowWindow(_workspaceProcessingViewModel);
            }
            else
            {
                MessageBox.Show("No \'Images\' directory found in the workspace", "Error");
            }
        }
    }

    private bool CheckWorkspaceDirectory(string folderPath)
    {
        if (!Path.Exists(Path.Combine(folderPath, "Images")))
            return false;

        return true;
    }

    public async Task OpenVisualisation()
    {
        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspaces");

        var openFileDialog = new OpenFileDialog
        {
            Title = "Select a File",
            Filter = "Fused Mesh File (poisson_mesh.ply)|poisson_mesh.ply",
            InitialDirectory = folderPath
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string selectedFilePath = openFileDialog.FileName;

            await ExecuteCommandWithFile(selectedFilePath);
        }
    }

    private async Task ExecuteCommandWithFile(string filePath)
    {
        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspace_Visualiser", "show_mesh.py");

        string arguments = $"\"{scriptPath}\" \"{filePath}\"";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "python.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
            else
            {
                throw new InvalidOperationException("Unable to run python script.");
            }
        }
    }

    public void CloseWorkspaceProcessing()
    {
        if (_workspaceProcessingViewModel is not null)
        {
            if(_workspaceProcessingViewModel.IsLoading)
               _workspaceProcessingViewModel.CancelCommand();
            _workspaceProcessingViewModel.RequestClose();
        }
    }

    protected override void OnClose()
    {
        CloseWorkspaceProcessing();
        base.OnActivate();
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


