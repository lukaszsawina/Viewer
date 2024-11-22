using OpenCvSharp;
using Stylet;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using Path = System.IO.Path;


namespace Viewer.ViewModel;

public class WorkspaceProcessingViewModel : Screen
{

    private string _currentText;
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

    private bool _isFinished = false;
    public bool IsFinished
    {
        get => _isFinished;
        set
        {
            _isFinished = value;
            NotifyOfPropertyChange(() => IsFinished);
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

        //string colmapArgument = $"automatic_reconstructor --workspace_path {_workspacePath} --image_path {_workspacePath}\\images --quality low --data_type individual --single_camera 1";
        string colmapArgument = $"--help";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = colmapBatPath,
            Arguments = colmapArgument,
            UseShellExecute = false,  
            CreateNoWindow = false,
        };

        try
        {
            CurrentText = "Reconstructor in progress...";
            IsLoading = true;
            AreButtonsVisible = false;

            bool taskCanceled = false;

            await Task.Run(() =>
            {
                using (colmapProcess = new Process { StartInfo = startInfo })
                {
                    colmapProcess.Start();

                    while (!colmapProcess.HasExited)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            string exeProcessName = "colmap";

                            exeProcess = Process.GetProcessesByName(exeProcessName).FirstOrDefault();
                            if (exeProcess != null)
                            {
                                taskCanceled = true;
                                exeProcess.Kill();
                            }
                        }

                        Task.Delay(500).Wait();
                    }
                }
            });

            if(taskCanceled || !CheckResult())
            {
                DeleteAllExceptDirectory(_workspacePath, Path.Combine(_workspacePath, "images"));
                IsLoading = false;
                AreButtonsVisible = true;
                CurrentText = "Process was cancelled or failed. Do you want to start processing Your workspace?";
            }
            else
            {
                IsLoading = false;
                IsFinished = true;
                CurrentText = "Reconstruction is ready, do You want to display it?";
            }
        }
        catch (Exception ex)
        {
            IsLoading = false;
            AreButtonsVisible = true;
            MessageBox.Show($"Program was unable to start reconstruction: {ex.Message}");
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
        }
    }

    private bool CheckResult()
    {
        string path = Path.Combine(_workspacePath, "dense", "0");
        if (!Directory.Exists(path))
        {
            return false;
        }

        string filePath = Path.Combine(path, "fused.ply");

        return File.Exists(filePath);
    }

    private void DeleteAllExceptDirectory(string parentDirectory, string directoryToKeep)
    {
        if (!Directory.Exists(parentDirectory))
        {
            throw new DirectoryNotFoundException($"{parentDirectory} don't exist.");
        }

        var directories = Directory.GetDirectories(parentDirectory);
        var files = Directory.GetFiles(parentDirectory);
        var fullDirectoryToKeep = Path.GetFullPath(directoryToKeep);

        foreach (var directory in directories)
        {
            if (!string.Equals(Path.GetFullPath(directory), fullDirectoryToKeep, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Delete(directory, true);
            }
        }

        foreach (var file in files)
        {
            File.Delete(file);
        }
    }

    public async Task RunPythonScriptAsync(string scriptName, string inputPath, string outputPath = null)
    {
        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspace_Visualiser", scriptName);

        string arguments;
        if (string.IsNullOrEmpty(outputPath))
        {
            arguments = $"\"{scriptPath}\" \"{inputPath}\"";
        }
        else
        {
            arguments = $"\"{scriptPath}\" \"{inputPath}\" \"{outputPath}\"";
        }

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
                throw new InvalidOperationException("Nie udało się uruchomić skryptu Python.");
            }
        }
    }

    public void NoCommand()
    {
        this.RequestClose();
    }

    public void CancelCommand()
    {
        _cancellationTokenSource?.Cancel();
    }

    public async Task ShowReconstruction()
    {
        string denseDirectory = ChooseDenseDirectory();

        string inputPath = Path.Combine(_workspacePath, "dense", denseDirectory, "fused.ply");
        string outputPath = Path.Combine(_workspacePath, "poisson_mesh.ply");

        if (!File.Exists(outputPath))
        {
            CurrentText = "Preparing model to display...";
            IsLoading = true;
            IsFinished = false;

            await RunPythonScriptAsync("save_mesh.py", inputPath, outputPath);

            IsLoading = false;
            IsFinished = true;

            CurrentText = "Reconstruction is ready, do you want to display it?";
        }

        await RunPythonScriptAsync("show_mesh.py", outputPath);
    }

    private string ChooseDenseDirectory()
    {
        try
        {
            string pathToDenses = Path.Combine(_workspacePath, "dense");

            if (Directory.Exists(pathToDenses))
            {
                string[] directories = Directory.GetDirectories(pathToDenses);

                if (directories.Length == 1)
                    return "0";

                string maxDirectory = null;
                int maxElements = -1;

                foreach (var dir in directories)
                {
                    string dirPath = Path.Combine(pathToDenses, dir, "images");

                    int elementCount = Directory.GetFileSystemEntries(dirPath).Length;

                    if (elementCount > maxElements)
                    {
                        maxElements = elementCount;
                        maxDirectory = dir;
                    }
                }

                return maxDirectory;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Wystąpił błąd: {ex.Message}");
        }

        return "0";
    }

    protected override void OnClose()
    {
        if (IsLoading)
            CancelCommand();
        base.OnClose();
    }
}

