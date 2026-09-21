namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// 2D axis-aligned bounding box of a frame in CAD coordinates.
/// </summary>
public readonly record struct PlotBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => Math.Max(0.0, MaxX - MinX);
    public double Height => Math.Max(0.0, MaxY - MinY);
    public double CenterX => (MinX + MaxX) * 0.5;
    public double CenterY => (MinY + MaxY) * 0.5;
    public bool IsLandscape => Width >= Height;

    public bool IsValid =>
        double.IsFinite(MinX) && double.IsFinite(MinY) &&
        double.IsFinite(MaxX) && double.IsFinite(MaxY) &&
        MaxX >= MinX && MaxY >= MinY;

    /// <summary>
    /// Computes the vertical overlap height between this boundary and another boundary.
    /// </summary>
    public double VerticalOverlap(PlotBounds other)
    {
        var top = Math.Min(MaxY, other.MaxY);
        var bottom = Math.Max(MinY, other.MinY);
        return Math.Max(0.0, top - bottom);
    }

    /// <summary>
    /// Determines whether this boundary shares a significant vertical overlap band with another boundary.
    /// </summary>
    public bool OverlapsVertically(PlotBounds other, double ratioThreshold = 0.5)
    {
        var overlap = VerticalOverlap(other);
        var minHeight = Math.Min(Height, other.Height);
        if (minHeight <= 1e-6) return Math.Abs(CenterY - other.CenterY) < 1.0;
        return (overlap / minHeight) >= ratioThreshold;
    }
}
