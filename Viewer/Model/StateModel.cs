using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Model;

public class StateModel
{

    private bool _isStarted;
    public bool IsStarted {
        get => _isStarted;
        set
        {
            _isStarted = value;
            OnPropertyChanged();
        }
    }

    private bool _isRecording;
    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            _isRecording = value;
            OnPropertyChanged();
        }
    }

    private string _workspaceFolderPath;

    public string WorkspaceFolderPath
    {
        get => _workspaceFolderPath;
        set
        {
            _workspaceFolderPath = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
