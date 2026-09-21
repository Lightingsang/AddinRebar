using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

public sealed class PresetServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _testFilePath;

    public PresetServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "SmartPlotTests_" + Guid.NewGuid().ToString("N"));
        _testFilePath = Path.Combine(_tempDirectory, "presets.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup failures in test teardown
        }
    }

    [Fact]
    public void LoadPresets_WhenFileMissing_CreatesAndReturnsDefaults()
    {
        var service = new PresetService(_testFilePath);

        var presets = service.LoadPresets();

        Assert.True(File.Exists(_testFilePath));
        Assert.Equal(3, presets.Count);

        var defaultPreset = service.GetDefaultPreset();
        Assert.NotNull(defaultPreset);
        Assert.Equal("A1 Monochrome PDF", defaultPreset.Name);
        Assert.True(defaultPreset.IsDefault);
    }

    [Fact]
    public void SavePresets_And_LoadPresets_Roundtrip()
    {
        var service = new PresetService(_testFilePath);
        var customList = new List<PlotPreset>
        {
            new()
            {
                Name = "Custom A2 Setup",
                Description = "High resolution A2",
                IsDefault = true,
                Config = new PlotConfiguration
                {
                    DeviceName = "DWG To PDF.pc3",
                    MediaName = "ISO_full_bleed_A2_(594.00_x_420.00_MM)",
                    PlotStyle = "grayscale.ctb",
                    Orientation = OrientationMode.Landscape
                }
            }
        };

        service.SavePresets(customList, "Custom A2 Setup");

        // Reload via fresh service instance
        var reloadedService = new PresetService(_testFilePath);
        var reloaded = reloadedService.LoadPresets();

        Assert.Single(reloaded);
        Assert.Equal("Custom A2 Setup", reloaded[0].Name);
        Assert.Equal("High resolution A2", reloaded[0].Description);
        Assert.Equal("DWG To PDF.pc3", reloaded[0].Config.DeviceName);
        Assert.Equal(OrientationMode.Landscape, reloaded[0].Config.Orientation);
        Assert.True(reloaded[0].IsDefault);
    }

    [Fact]
    public void SavePreset_UpdatesExistingByName()
    {
        var service = new PresetService(_testFilePath);
        service.LoadPresets(); // initialize defaults

        var updatedA1 = new PlotPreset
        {
            Name = "A1 Monochrome PDF",
            Description = "Updated Description",
            Config = new PlotConfiguration
            {
                DeviceName = "DWG To PDF.pc3"
            }
        };

        service.SavePreset(updatedA1);

        var current = service.GetPreset("A1 Monochrome PDF");
        Assert.NotNull(current);
        Assert.Equal("Updated Description", current.Description);
        Assert.Equal("DWG To PDF.pc3", current.Config.DeviceName);
        Assert.Equal(3, service.LoadPresets().Count);
    }

    [Fact]
    public void SavePreset_AddNewPreset_IncreasesCount()
    {
        var service = new PresetService(_testFilePath);
        service.LoadPresets();

        var newPreset = new PlotPreset
        {
            Name = "A4 Detail Sheets",
            Description = "A4 for details",
            Config = new PlotConfiguration { MediaName = "A4" }
        };

        service.SavePreset(newPreset);

        var presets = service.LoadPresets();
        Assert.Equal(4, presets.Count);
        Assert.NotNull(service.GetPreset("A4 Detail Sheets"));
    }

    [Fact]
    public void SavePreset_SetAsDefault_UpdatesDefaultFlagAcrossCollection()
    {
        var service = new PresetService(_testFilePath);
        service.LoadPresets();

        var a3Preset = service.GetPreset("A3 Color PDF");
        Assert.NotNull(a3Preset);
        Assert.False(a3Preset.IsDefault);

        service.SavePreset(a3Preset, setAsDefault: true);

        var defaultPreset = service.GetDefaultPreset();
        Assert.Equal("A3 Color PDF", defaultPreset.Name);
        Assert.True(defaultPreset.IsDefault);

        var oldDefault = service.GetPreset("A1 Monochrome PDF");
        Assert.NotNull(oldDefault);
        Assert.False(oldDefault.IsDefault);
    }

    [Fact]
    public void DeletePreset_RemovesTargetPreset()
    {
        var service = new PresetService(_testFilePath);
        service.LoadPresets();

        var deleted = service.DeletePreset("A3 Color PDF");
        Assert.True(deleted);

        var presets = service.LoadPresets();
        Assert.Equal(2, presets.Count);
        Assert.Null(service.GetPreset("A3 Color PDF"));

        // Deleting non-existent returns false
        var deleteNonExistent = service.DeletePreset("NonExistentPreset");
        Assert.False(deleteNonExistent);
    }

    [Fact]
    public void CorruptedJson_FallbacksGracefully()
    {
        var dir = Path.GetDirectoryName(_testFilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_testFilePath, "{ this is not valid json content @#$$%^");

        var service = new PresetService(_testFilePath);
        var presets = service.LoadPresets();

        // Must not throw and must return default presets
        Assert.NotEmpty(presets);
        Assert.Equal(3, presets.Count);
    }

    [Fact]
    public async Task Async_LoadAndSave_Roundtrip()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = new PresetService(_testFilePath);
        var collection = await service.LoadPresetsAsync(ct);

        Assert.Equal(3, collection.Presets.Count);

        var updatedCollection = collection with
        {
            DefaultPresetName = "A3 Color PDF"
        };

        await service.SavePresetsAsync(updatedCollection, ct);

        var reloaded = await service.LoadPresetsAsync(ct);
        Assert.Equal("A3 Color PDF", reloaded.DefaultPresetName);
    }

    [Fact]
    public void LoadPresets_WithNullItemInList_FiltersOutNullAndLoadsDefaultsIfEmpty()
    {
        var dir = Path.GetDirectoryName(_testFilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_testFilePath, "{\"Presets\": [null, {\"Name\": null}, {\"Name\": \"   \"}]}");

        var service = new PresetService(_testFilePath);
        var presets = service.LoadPresets();

        Assert.NotEmpty(presets);
        Assert.Equal(3, presets.Count); // Fallback to defaults
        Assert.All(presets, p =>
        {
            Assert.NotNull(p);
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
        });
    }

    [Fact]
    public void LoadPresets_WithMixedValidAndNullItems_KeepsValidItemsOnly()
    {
        var dir = Path.GetDirectoryName(_testFilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_testFilePath, "{\"DefaultPresetName\": \"Valid Setup\", \"Presets\": [null, {\"Name\": \"Valid Setup\", \"Config\": {\"DeviceName\": \"PDF.pc3\"}}, {\"Name\": null}]}");

        var service = new PresetService(_testFilePath);
        var presets = service.LoadPresets();

        Assert.Single(presets);
        Assert.Equal("Valid Setup", presets[0].Name);
        Assert.Equal("Valid Setup", service.GetDefaultPreset().Name);
        Assert.NotNull(service.GetPreset("Valid Setup"));
        Assert.Null(service.GetPreset("NonExistent"));
    }

    [Fact]
    public void SavePreset_NullOrEmptyName_SafelyIgnored()
    {
        var service = new PresetService(_testFilePath);
        service.LoadPresets();

        service.SavePreset(null!);
        service.SavePreset(new PlotPreset { Name = null! });
        service.SavePreset(new PlotPreset { Name = "   " });

        Assert.Equal(3, service.LoadPresets().Count);
    }

    [Fact]
    public void ConcurrentWrites_DoNotCollide()
    {
        var service = new PresetService(_testFilePath);
        service.LoadPresets();

        Parallel.For(0, 20, i =>
        {
            var p = new PlotPreset
            {
                Name = $"Parallel_{i}",
                Config = new PlotConfiguration { DeviceName = "PDF.pc3" }
            };
            service.SavePreset(p);
        });

        var presets = service.LoadPresets();
        Assert.True(presets.Count >= 3);
    }
}
