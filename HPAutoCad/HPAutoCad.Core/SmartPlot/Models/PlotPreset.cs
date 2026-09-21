namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Represents a named configuration preset for plotting.
/// </summary>
public sealed record PlotPreset
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public PlotConfiguration Config { get; init; } = new();
}
