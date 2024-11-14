using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using OpenCvSharp;
using Stylet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Viewer.ViewModel;

public class WorkspaceProcessingViewModel : Screen
{

    private string _currentText;
    private static int _currentProcessIndex;
    Process colmapProcess = null;
    Process exeProcess = null;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly string _workspacePath = "D:\\DEV\\Projekt_inzynierka\\projekt_komoda_2";

    public string CurrentText
    {
        get => _currentText;
        set
        {
            _currentText = value;
            NotifyOfPropertyChange(() => CurrentText);
        }
    }

    private bool _areButtonsVisible = true;
    public bool AreButtonsVisible
    {
        get => _areButtonsVisible;
        set
        {
            _areButtonsVisible = value;
            NotifyOfPropertyChange(() => AreButtonsVisible);
        }
    }

    private bool _isLoading = false;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            NotifyOfPropertyChange(() => IsLoading);
        }
    }

    public WorkspaceProcessingViewModel()
    {
        _currentText = "Do you want to start processing Your workspace?";
        
    }

    public async Task YesCommand()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        string colmapBatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "COLMAP-CL", "colmap.bat");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = colmapBatPath,
            Arguments = $"automatic_reconstructor --workspace_path {_workspacePath} --image_path {_workspacePath}\\images --quality low --data_type individual",
            UseShellExecute = true,  // Ustawienie na false dla pełnej kontroli nad procesem
            CreateNoWindow = false,    // Ukryj okno terminala
        };

        try
        {
            CurrentText = "Reconstructor in progress...";
            IsLoading = true;
            AreButtonsVisible = false;

            // Task uruchamiający proces w tle
            await Task.Run(() =>
            {
                using (colmapProcess = new Process { StartInfo = startInfo })
                {
                    colmapProcess.Start();

                    while (!colmapProcess.HasExited)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        Task.Delay(500).Wait();
                    }

                    string exeProcessName = "colmap";

                    exeProcess = Process.GetProcessesByName(exeProcessName).FirstOrDefault();
                    if (exeProcess != null)
                    {
                        exeProcess.Kill();
                    }
                }

                IsLoading = false;
                AreButtonsVisible = true;
                CurrentText = "Do you want to start processing Your workspace?";

            });
        }
        catch (Exception ex)
        {
            IsLoading = false;
            AreButtonsVisible = true;
            MessageBox.Show($"Wystąpił błąd podczas uruchamiania colmap.bat: {ex.Message}");
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
        }
    }


    public void NoCommand()
    {
        this.RequestClose();
    }

    public void CancellCommand()
    {
        _cancellationTokenSource?.Cancel();
    }

    protected override void OnClose()
    {
        CancellCommand();
        base.OnClose();
    }
}

