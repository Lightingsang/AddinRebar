namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Root document collection for persisted plot presets.
/// </summary>
public sealed record PlotPresetCollection
{
    public int Version { get; init; } = 1;
    public string? DefaultPresetName { get; init; }
    public List<PlotPreset> Presets { get; init; } = [];
}
