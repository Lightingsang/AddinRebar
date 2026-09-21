namespace HPAutoCad.Core.SmartPlot.Services;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// JSON-backed implementation of <see cref="IPresetService"/> storing presets in AppData.
/// </summary>
public sealed class PresetService : IPresetService
{
    private static readonly object FileLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; }

    public PresetService(string? customFilePath = null)
    {
        FilePath = customFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HPAutoCad",
            "SmartPlot",
            "presets.json");
    }

    public IReadOnlyList<PlotPreset> LoadPresets()
    {
        lock (FileLock)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    var defaults = CreateDefaultPresets();
                    SavePresetsInternal(defaults.Presets, defaults.DefaultPresetName);
                    return defaults.Presets;
                }

                var json = File.ReadAllText(FilePath);
                var collection = JsonSerializer.Deserialize<PlotPresetCollection>(json, JsonOptions);

                if (collection?.Presets is { Count: > 0 } list)
                {
                    var valid = list.Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Name)).ToList();
                    if (valid.Count > 0)
                    {
                        return valid;
                    }
                }

                return CreateDefaultPresets().Presets;
            }
            catch
            {
                // Graceful fallback on corrupted or unreadable JSON
                return CreateDefaultPresets().Presets;
            }
        }
    }

    public PlotPreset GetDefaultPreset()
    {
        var presets = LoadPresets();
        return presets.FirstOrDefault(p => p?.IsDefault == true)
            ?? presets.FirstOrDefault(p => p != null)
            ?? CreateDefaultPresets().Presets[0];
    }

    public PlotPreset? GetPreset(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return LoadPresets().FirstOrDefault(p => p?.Name != null && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void SavePresets(IEnumerable<PlotPreset> presets, string? defaultPresetName = null)
    {
        lock (FileLock)
        {
            SavePresetsInternal(presets, defaultPresetName);
        }
    }

    private void SavePresetsInternal(IEnumerable<PlotPreset> presets, string? defaultPresetName = null)
    {
        var list = (presets ?? Enumerable.Empty<PlotPreset>())
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Name))
            .ToList();

        if (list.Count == 0)
        {
            list = CreateDefaultPresets().Presets.ToList();
        }

        var defName = defaultPresetName
            ?? list.FirstOrDefault(p => p.IsDefault)?.Name
            ?? list.FirstOrDefault()?.Name;

        // Ensure default flag consistency
        var normalized = list.Select(p => p with { IsDefault = string.Equals(p.Name, defName, StringComparison.OrdinalIgnoreCase) }).ToList();

        var collection = new PlotPresetCollection
        {
            Version = 1,
            DefaultPresetName = defName,
            Presets = normalized
        };

        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(collection, JsonOptions);

        var tempDir = string.IsNullOrEmpty(dir) ? "." : dir;
        var tempFile = Path.Combine(tempDir, $"{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(tempFile, json);

            const int maxRetries = 5;
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    File.Move(tempFile, FilePath, overwrite: true);
                    break;
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    Thread.Sleep(10 * attempt);
                }
                catch (UnauthorizedAccessException) when (attempt < maxRetries)
                {
                    Thread.Sleep(10 * attempt);
                }
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore temp file cleanup failure
                }
            }
        }
    }

    public void SavePreset(PlotPreset preset, bool setAsDefault = false)
    {
        if (preset == null || string.IsNullOrWhiteSpace(preset.Name)) return;

        lock (FileLock)
        {
            var current = LoadPresets().ToList();
            var index = current.FindIndex(p => p.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase));

            var updatedPreset = preset with { IsDefault = setAsDefault };

            if (index >= 0)
            {
                current[index] = updatedPreset;
            }
            else
            {
                current.Add(updatedPreset);
            }

            SavePresetsInternal(current, setAsDefault ? preset.Name : null);
        }
    }

    public bool DeletePreset(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        lock (FileLock)
        {
            var current = LoadPresets().ToList();
            var removed = current.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                SavePresetsInternal(current);
            }
            return removed;
        }
    }

    public Task<PlotPresetCollection> LoadPresetsAsync(CancellationToken ct = default)
    {
        var presets = LoadPresets();
        var defaultName = presets.FirstOrDefault(p => p?.IsDefault == true)?.Name ?? presets.FirstOrDefault()?.Name;
        var collection = new PlotPresetCollection
        {
            Version = 1,
            DefaultPresetName = defaultName,
            Presets = presets.ToList()
        };
        return Task.FromResult(collection);
    }

    public Task SavePresetsAsync(PlotPresetCollection collection, CancellationToken ct = default)
    {
        if (collection == null) return Task.CompletedTask;
        SavePresets(collection.Presets, collection.DefaultPresetName);
        return Task.CompletedTask;
    }

    private static PlotPresetCollection CreateDefaultPresets()
    {
        return new PlotPresetCollection
        {
            Version = 1,
            DefaultPresetName = "A1 Monochrome PDF",
            Presets =
            [
                new PlotPreset
                {
                    Name = "A1 Monochrome PDF",
                    Description = "Single-sheet PDF export with monochrome plot style on A1 full bleed paper.",
                    IsDefault = true,
                    Config = new PlotConfiguration
                    {
                        DeviceName = "AutoCAD PDF (General Documentation).pc3",
                        MediaName = "ISO_full_bleed_A1_(841.00_x_594.00_MM)",
                        PlotStyle = "monochrome.ctb",
                        Orientation = OrientationMode.Auto,
                        OutputMode = OutputMode.SingleFiles,
                        FitToPaper = true,
                        CenterPlot = true
                    }
                },
                new PlotPreset
                {
                    Name = "A1 Monochrome Merged",
                    Description = "Multi-page merged PDF document with monochrome plot style on A1 paper.",
                    IsDefault = false,
                    Config = new PlotConfiguration
                    {
                        DeviceName = "AutoCAD PDF (General Documentation).pc3",
                        MediaName = "ISO_full_bleed_A1_(841.00_x_594.00_MM)",
                        PlotStyle = "monochrome.ctb",
                        Orientation = OrientationMode.Auto,
                        OutputMode = OutputMode.MergedPdf,
                        MergedFileName = "PlotSet_A1.pdf",
                        FitToPaper = true,
                        CenterPlot = true
                    }
                },
                new PlotPreset
                {
                    Name = "A3 Color PDF",
                    Description = "Color PDF plotting on A3 paper for presentations and approval sets.",
                    IsDefault = false,
                    Config = new PlotConfiguration
                    {
                        DeviceName = "AutoCAD PDF (General Documentation).pc3",
                        MediaName = "ISO_full_bleed_A3_(420.00_x_297.00_MM)",
                        PlotStyle = "acad.ctb",
                        Orientation = OrientationMode.Auto,
                        OutputMode = OutputMode.SingleFiles,
                        FitToPaper = true,
                        CenterPlot = true
                    }
                }
            ]
        };
    }
}
