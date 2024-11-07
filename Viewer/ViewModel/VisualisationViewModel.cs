using System;
using System.Collections.Generic;
using System.IO;
using Stylet;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using HelixToolkit.Wpf;
using Viewer.Model;

namespace Viewer.ViewModel;

public class VisualisationViewModel : Screen
{
    private PLYModel _plyModel;
    private GeometryModel3D _mesh;

    public GeometryModel3D Mesh
    {
        get { return _mesh; }
        set
        {
            _mesh = value;
            NotifyOfPropertyChange(() => Mesh);
        }
    }

    public VisualisationViewModel()
    {
        _plyModel = new PLYModel();
    }

    public void LoadPLYCommand()
    {
        string solutionDir = AppDomain.CurrentDomain.BaseDirectory;
        _plyModel.LoadPLY($"{solutionDir}\\Test\\test.ply");
        Mesh = _plyModel.GeometryModel;
    }
}

