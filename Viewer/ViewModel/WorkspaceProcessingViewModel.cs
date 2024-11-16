using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using OpenCvSharp;
using Stylet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static Ply.Net.PlyParser;

namespace Viewer.ViewModel;

public class WorkspaceProcessingViewModel : Screen
{

    private string _currentText;
    private static int _currentProcessIndex;
    Process colmapProcess = null;
    Process exeProcess = null;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly string _workspacePath = "D:\\DEV\\Projekt_inzynierka\\projekt_mieszkanie";

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
            Arguments = $"automatic_reconstructor --workspace_path {_workspacePath} --image_path {_workspacePath}\\images --quality low --data_type individual --single_camera 1",
            UseShellExecute = false,  // Ustawienie na false dla pełnej kontroli nad procesem
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

        string inputPath = Path.Combine(_workspacePath, "dense", "0", "fused.ply");
        string outputPath = Path.Combine(_workspacePath, "dense", "0", "poisson_mesh.ply");
        await RunPythonScriptAsync("save_mesh.py", inputPath, outputPath);

        await RunPythonScriptAsync($"show_mesh.py", outputPath);

        IsLoading = false;
        AreButtonsVisible = true;
        CurrentText = "Do you want to start processing Your workspace?";
    }

    public async Task RunPythonScriptAsync(string scriptName, string inputPath, string outputPath = null)
    {
        // Tworzenie pełnej ścieżki do skryptu
        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspace_Visualiser", scriptName);

        // Przygotowanie argumentów
        string arguments;
        if (string.IsNullOrEmpty(outputPath))
        {
            arguments = $"\"{scriptPath}\" \"{inputPath}\"";
        }
        else
        {
            arguments = $"\"{scriptPath}\" \"{inputPath}\" \"{outputPath}\"";
        }

        // Konfiguracja procesu
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "pythonw",              // Użycie pythonw zamiast cmd.exe
            Arguments = arguments,             // Przekazanie argumentów
            UseShellExecute = false,           // Bez otwierania terminala
            CreateNoWindow = true              // Zapobiega otwarciu terminala
        };

        
        // Uruchomienie procesu
        using (Process process = Process.Start(startInfo))
        {
            if (process != null)
            {
                await process.WaitForExitAsync();  // Oczekiwanie na zakończenie procesu
            }
            else
            {
                throw new InvalidOperationException("Nie udało się uruchomić skryptu Python.");
            }
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

