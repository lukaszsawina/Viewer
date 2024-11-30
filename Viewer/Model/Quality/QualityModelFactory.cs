using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Model.Quality;

public static class QualityModelFactory
{
    public static IQualityOption CreateOption(QualityType optionType)
    {
        return optionType switch
        {
            QualityType.LOW => new LowQualityModel(),
            QualityType.MEDIUM => new MediumQualityModel(),
            QualityType.HIGH => new HighQualityModel(),
            _ => throw new ArgumentException($"Unknown option type: {optionType}")
        };
    }
}

public enum QualityType
{
    LOW,
    MEDIUM, 
    HIGH
}