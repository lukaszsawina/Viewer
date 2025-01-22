using OpenCvSharp;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Viewer.Model.Quality;
using Path = System.IO.Path;


namespace Viewer.ViewModel;

public class WorkspaceProcessingViewModel : Screen
{
    private Process colmapProcess = null;
    private Process exeProcess = null;
    private IQualityOption _qualityOption;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly string _workspacePath;
    private bool taskCanceled = false;
    private static int currentProcesStateIndex = 0;

    public ObservableCollection<QualityType> Options { get; set; } = new ObservableCollection<QualityType>(
            Enum.GetValues(typeof(QualityType))
                .Cast<QualityType>()
                .Where(q => q != QualityType.UNDEFINED)
        );

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

    private bool _qualityOptionVisible = true;
    public bool QualityOptionVisible
    {
        get => _qualityOptionVisible;
        set
        {
            _qualityOptionVisible = value;
            NotifyOfPropertyChange(() => QualityOptionVisible);
        }
    }

    private QualityType _selectedOption = QualityType.UNDEFINED;
    public QualityType SelectedOption
    {
        get => _selectedOption;
        set
        {
            _selectedOption = value+1;
            _qualityOption = QualityModelFactory.CreateOption(_selectedOption);
            NotifyOfPropertyChange(() => SelectedOption);
            NotifyOfPropertyChange(() => IsOptionSelected);
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

    public bool IsOptionSelected => SelectedOption != QualityType.UNDEFINED;

    public WorkspaceProcessingViewModel(string workspacePath)
    {
        _workspacePath = workspacePath;
        _currentText = "Do you want to start processing Your workspace?";
    }

    public async Task YesCommand()
    {
        taskCanceled = false;
        CurrentText = "Reconstructor in progress...";
        IsLoading = true;
        AreButtonsVisible = false;
        currentProcesStateIndex = 0;

        Directory.CreateDirectory($"{_workspacePath}\\dense");
        Directory.CreateDirectory($"{_workspacePath}\\sparse");

        await RunColmapAsync("Feature extraction", $"feature_extractor --database_path {_workspacePath}\\database.db --image_path {_workspacePath}\\images --ImageReader.single_camera 1");
        await RunColmapAsync("Exhaustive matching", $"exhaustive_matcher --database_path {_workspacePath}\\database.db");
        await RunColmapAsync("Mapping", $"mapper --database_path {_workspacePath}\\database.db --image_path {_workspacePath}\\images --output_path {_workspacePath}\\sparse");
        await RunColmapAsync("Image undistortion", $"image_undistorter --image_path {_workspacePath}\\images --input_path {_workspacePath}\\sparse\\0 --output_path {_workspacePath}\\dense --output_type COLMAP --max_image_size {_qualityOption.MaxImageSize}");
        await RunColmapAsync("Patch match stereo", $"patch_match_stereo --workspace_path {_workspacePath}\\dense --workspace_format COLMAP --PatchMatchStereo.max_image_size {_qualityOption.MaxImageSize} --PatchMatchStereo.window_radius {_qualityOption.WindowRadius} --PatchMatchStereo.window_step {_qualityOption.WindowStep} --PatchMatchStereo.num_samples {_qualityOption.NumSamples} --PatchMatchStereo.num_iterations {_qualityOption.NumIterations}");
        await RunColmapAsync("Stereo fusion", $"stereo_fusion --workspace_path {_workspacePath}\\dense --workspace_format COLMAP --input_type geometric --output_path {_workspacePath}/dense/fused.ply --StereoFusion.max_image_size {_qualityOption.MaxImageSize} --StereoFusion.check_num_images {_qualityOption.CheckNumImages}");

        if (taskCanceled || !CheckResult())
        {
            DeleteAllExceptDirectory(_workspacePath, Path.Combine(_workspacePath, "images"));
            IsLoading = false;
            QualityOptionVisible = true;
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

    public async Task RunColmapAsync(string currentProcesName, string arguments)
    {
        if (taskCanceled)
            return;

        currentProcesStateIndex++;
        CurrentText = $"Reconstructor in progress...\n {currentProcesStateIndex}/6 - {currentProcesName}";
        QualityOptionVisible = false;
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        string colmapBatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "COLMAP-CL", "colmap.bat");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = colmapBatPath,
            Arguments = arguments,
            UseShellExecute = false,  
            CreateNoWindow = false,
        };

        try
        {
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
        string path = Path.Combine(_workspacePath, "dense");
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
        string inputPath = Path.Combine(_workspacePath, "dense", "fused.ply");
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

    protected override void OnClose()
    {
        if (IsLoading)
        {
            CancelCommand();
            if (!CheckResult())
                DeleteAllExceptDirectory(_workspacePath, Path.Combine(_workspacePath, "images"));
        }
        base.OnClose();
    }
}

