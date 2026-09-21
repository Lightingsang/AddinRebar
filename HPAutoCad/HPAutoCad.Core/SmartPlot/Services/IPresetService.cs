namespace HPAutoCad.Core.SmartPlot.Services;

using HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Service for managing and persisting plot configuration presets.
/// </summary>
public interface IPresetService
{
    /// <summary>
    /// File path where presets are stored.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Loads all saved presets, or creates and saves default presets if file does not exist or is corrupted.
    /// </summary>
    IReadOnlyList<PlotPreset> LoadPresets();

    /// <summary>
    /// Gets the default preset.
    /// </summary>
    PlotPreset GetDefaultPreset();

    /// <summary>
    /// Finds a preset by name (case-insensitive).
    /// </summary>
    PlotPreset? GetPreset(string name);

    /// <summary>
    /// Saves a collection of presets to disk.
    /// </summary>
    void SavePresets(IEnumerable<PlotPreset> presets, string? defaultPresetName = null);

    /// <summary>
    /// Adds or updates a single preset.
    /// </summary>
    void SavePreset(PlotPreset preset, bool setAsDefault = false);

    /// <summary>
    /// Deletes a preset by name.
    /// </summary>
    bool DeletePreset(string name);

    /// <summary>
    /// Asynchronously loads presets as a collection.
    /// </summary>
    Task<PlotPresetCollection> LoadPresetsAsync(CancellationToken ct = default);

    /// <summary>
    /// Asynchronously saves presets collection.
    /// </summary>
    Task SavePresetsAsync(PlotPresetCollection collection, CancellationToken ct = default);
}
