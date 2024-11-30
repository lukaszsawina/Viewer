using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Viewer.Model.Quality;

public class LowQualityModel : IQualityOption
{
    public int MaxImageSize => 1000;
    public int WindowRadius => 4;
    public int WindowStep => 2;
    public int NumSamples => 7;
    public int NumIterations => 3;
    public int CheckNumImages => 25;
}