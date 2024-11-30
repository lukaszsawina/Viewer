using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Model.Quality;

public class HighQualityModel : IQualityOption
{
    public int MaxImageSize => 2400;
    public int WindowRadius => 5;
    public int WindowStep => 1;
    public int NumSamples => 15;
    public int NumIterations => 5;
    public int CheckNumImages => 50;
}