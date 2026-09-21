namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Represents an individual plot frame detected in a drawing.
/// </summary>
public sealed record PlotItem
{
    public required string Id { get; init; }
    public required PlotBounds Bounds { get; init; }
    public string LayoutName { get; init; } = "Model";
    public string DisplayName { get; init; } = string.Empty;
    public string? AttributeValue { get; init; }
    public string? SheetNumber { get; init; }
    public string? SheetTitle { get; init; }
    public int Order { get; init; } = 1;
    public double Rotation { get; init; } = 0.0;
    public bool IsSelected { get; init; } = true;
    public string? SourceHandle { get; init; }
    public string? OutputFileName { get; init; }

    // Convenience delegates to Bounds
    public double MinX => Bounds.MinX;
    public double MinY => Bounds.MinY;
    public double MaxX => Bounds.MaxX;
    public double MaxY => Bounds.MaxY;
    public double Width => Bounds.Width;
    public double Height => Bounds.Height;
    public bool IsLandscape => Bounds.IsLandscape;
}
