namespace Viewer.Model.Quality;

public interface IQualityOption
{
    public int MaxImageSize { get; }
    public int WindowRadius { get; }
    public int WindowStep { get; }
    public int NumSamples { get; }
    public int NumIterations { get; }
    public int CheckNumImages { get; }
}
