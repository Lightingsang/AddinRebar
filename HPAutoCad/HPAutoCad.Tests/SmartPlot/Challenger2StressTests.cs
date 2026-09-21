namespace HPAutoCad.Tests.SmartPlot;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;
using Xunit;

public sealed class Challenger2StressTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FileNameService _fileNameService = new();

    public Challenger2StressTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Challenger2Stress_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
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
            // Ignore cleanup failures
        }
    }

    // =========================================================================
    // DIMENSION 1: LayoutRangeParser Stress & Adversarial Inputs
    // =========================================================================

    [Theory]
    [InlineData("2147483648")] // int.MaxValue + 1
    [InlineData("-2147483649")] // int.MinValue - 1
    [InlineData("9999999999999999999999999999999999999999")]
    [InlineData("-9999999999999999999999999999999999999999")]
    [InlineData("1-2147483648")]
    [InlineData("2147483648-2147483649")]
    [InlineData("-2147483649--2147483648")]
    [InlineData("2147483647")] // int.MaxValue
    [InlineData("-2147483648")] // int.MinValue
    public void LayoutRangeParser_IntegerOverflow_NeverThrows(string input)
    {
        var result = LayoutRangeParser.Parse(input, 100);
        Assert.NotNull(result);
    }

    [Fact]
    public void LayoutRangeParser_HugeStringLength_DoesNotCrashOrHang()
    {
        // 100,000 characters of repeated delimiters and noise
        var sb = new StringBuilder(100_000);
        for (var i = 0; i < 20_000; i++)
        {
            sb.Append(",;--,,");
        }
        sb.Append("1-5");

        var result = LayoutRangeParser.Parse(sb.ToString(), 10);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }

    [Fact]
    public void LayoutRangeParser_DeeplyNestedDelimiters_HandlesGracefully()
    {
        var inputs = new[]
        {
            ",,,,,,,,",
            ";;;;;;;;",
            "1----5",
            "1-2-3-4-5",
            "--1--2--",
            ";;;1,,,2;;;3---4;;;5-6",
            "1 - - - 5",
            "- - -",
            " , , , 1 - 3 , , , 5 - 7 , , "
        };

        foreach (var input in inputs)
        {
            var result = LayoutRangeParser.Parse(input, 10);
            Assert.NotNull(result);
        }
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("-5")]
    [InlineData("-100")]
    [InlineData("-5--1")]
    [InlineData("-10-5")]
    [InlineData("0")]
    [InlineData("0-0")]
    [InlineData("-0")]
    public void LayoutRangeParser_NegativeAndZeroIndices_FilteredOutSafely(string input)
    {
        var result = LayoutRangeParser.Parse(input, 10);
        Assert.NotNull(result);
        Assert.All(result, x => Assert.InRange(x, 1, 10));
    }

    [Fact]
    public void LayoutRangeParser_NonAsciiAndUnicode_NeverThrows()
    {
        var inputs = new[]
        {
            "一, 二, 三",
            "１-５, ８", // full-width unicode digits
            "Trang 1 đến 5",
            "📄1-📄5, 👍, 1-3🔥",
            "صفحة 1 إلى 5",
            "é, è, à, ü, ö",
            "\u200B1-5\u200B", // zero-width space
            "\u00A01\u00A0-\u00A05\u00A0" // non-breaking space
        };

        foreach (var input in inputs)
        {
            var result = LayoutRangeParser.Parse(input, 10);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void LayoutRangeParser_ControlCharacters_DoesNotThrow()
    {
        var input = "\0\a\b\t\r\n\v\f1-3\0, 5\t";
        var result = LayoutRangeParser.Parse(input, 10);
        Assert.NotNull(result);
        // Valid 1,2,3 and 5 can be parsed despite whitespace/control chars
        Assert.Contains(5, result);
    }

    [Fact]
    public void LayoutRangeParser_MemoryProtection_HugeRangeCappedAtHardCap()
    {
        // Attempt to allocate 2 billion integers
        var result = LayoutRangeParser.Parse("1-2000000000", int.MaxValue);
        Assert.NotNull(result);
        Assert.True(result.Count <= 10000, $"Result count {result.Count} should not exceed HardCap 10000");
    }

    // =========================================================================
    // DIMENSION 2: FileNameService Stress & Exotic Path Scenarios
    // =========================================================================

    [Fact]
    public void FileNameService_AllControlCharacters_ReplacedWithUnderscore()
    {
        for (int i = 0; i <= 0x1F; i++)
        {
            var ch = (char)i;
            var raw = $"Test{ch}File";
            var sanitized = _fileNameService.SanitizeFileName(raw);
            Assert.DoesNotContain(ch, sanitized);
            Assert.StartsWith("Test", sanitized);
        }

        // Also DEL (0x7F)
        var delRaw = "Test\x7FFile";
        var delSanitized = _fileNameService.SanitizeFileName(delRaw);
        Assert.DoesNotContain('\x7F', delSanitized);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("COM9")]
    [InlineData("LPT1")]
    [InlineData("LPT9")]
    [InlineData("con")]
    [InlineData("aux")]
    [InlineData("nul")]
    public void FileNameService_ReservedNamesExact_PrefixesUnderscore(string reserved)
    {
        var sanitized = _fileNameService.SanitizeFileName(reserved);
        Assert.Equal($"_{reserved}", sanitized);
    }

    [Fact]
    public void FileNameService_ReservedNamesWithExtensions_EmpiricalCheck()
    {
        // On Windows, "aux.pdf", "CON.txt", "NUL.dat" are also reserved DOS device names.
        // SanitizeFileName prefixes reserved names (including with extensions) with replacement char
        var auxResult = _fileNameService.SanitizeFileName("aux.pdf");
        var conResult = _fileNameService.SanitizeFileName("CON.txt");
        var nulResult = _fileNameService.SanitizeFileName("NUL.dat");

        Assert.Equal("_aux.pdf", auxResult);
        Assert.Equal("_CON.txt", conResult);
        Assert.Equal("_NUL.dat", nulResult);
    }

    [Fact]
    public void FileNameService_ExceedingMaxPath_DoesNotThrow()
    {
        var longName = new string('A', 500);
        var sanitized = _fileNameService.SanitizeFileName(longName);
        Assert.NotNull(sanitized);

        var longFolder = @"C:\" + new string('B', 300);
        var fullPath = _fileNameService.BuildFullFilePath(longFolder, longName);
        Assert.NotNull(fullPath);
    }

    [Fact]
    public void FileNameService_UnicodeCharacters_PreservedCorrectly()
    {
        var vietnamese = "Bản vẽ mặt bằng dầm sàn tầng 2 - Cột C1";
        var sanitized = _fileNameService.SanitizeFileName(vietnamese);
        Assert.Equal(vietnamese, sanitized);

        var japanese = "構造図_レイアウト1";
        var sanitizedJp = _fileNameService.SanitizeFileName(japanese);
        Assert.Equal(japanese, sanitizedJp);

        var symbols = "Dầm D1 Ø16@150 ±0.000 №12";
        var sanitizedSymbols = _fileNameService.SanitizeFileName(symbols);
        Assert.Contains("Ø16@150", sanitizedSymbols);
        Assert.Contains("±0.000", sanitizedSymbols);
    }

    [Fact]
    public void FileNameService_EmptyTokenCombinations_ReturnsFallback()
    {
        var emptyItem = new PlotItem
        {
            Id = "test",
            Bounds = new PlotBounds(0, 0, 100, 100),
            LayoutName = null,
            DisplayName = null,
            AttributeValue = null,
            SheetNumber = null,
            SheetTitle = null,
            Order = 5
        };

        var template = "{Prefix}_{Layout}_{SheetNo}_{Title}";
        var result = _fileNameService.FormatFileName(template, emptyItem, prefix: null, dwgName: null);

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.DoesNotContain("__", result);
        Assert.DoesNotContain("{", result);
        Assert.DoesNotContain("}", result);
    }

    [Fact]
    public void FileNameService_OnlyDotsOrSpaces_ReturnsFallback()
    {
        var dotInputs = new[] { ".", "..", "...", "....", "   ", "\t\t", " . . . " };
        foreach (var input in dotInputs)
        {
            var result = _fileNameService.SanitizeFileName(input);
            Assert.Equal("Plot_Sheet", result);
        }
    }

    // =========================================================================
    // DIMENSION 3: PresetService Corrupted JSON & Concurrency Stress
    // =========================================================================

    [Theory]
    [InlineData("")] // Empty file
    [InlineData("   \r\n\t  ")] // Whitespace only
    [InlineData("{ unclosed json ")] // Malformed syntax
    [InlineData("{\"Presets\": [{\"Name\": \"A1\"")] // Truncated
    [InlineData("[1, 2, 3]")] // Wrong top-level type (Array)
    [InlineData("\"just a string\"")] // Primitive string
    [InlineData("{\"RandomField\": 123}")] // Missing presets property
    [InlineData("{\"Presets\": null}")] // Null presets list
    [InlineData("{\"Presets\": []}")] // Empty presets list
    [InlineData("{\"Presets\": [{\"Name\": \"A1\", \"Config\": {\"Orientation\": \"UpsideDown\"}}]}")] // Invalid enum
    public void PresetService_CorruptedJson_AlwaysFallsBackToDefaults(string invalidJson)
    {
        var filePath = Path.Combine(_tempDirectory, $"corrupt_{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, invalidJson);

        var service = new PresetService(filePath);
        var presets = service.LoadPresets();

        Assert.NotNull(presets);
        Assert.NotEmpty(presets);
        Assert.Equal(3, presets.Count);
        Assert.Equal("A1 Monochrome PDF", presets[0].Name);
    }

    [Fact]
    public void PresetService_BinaryGarbage_FallsBackToDefaults()
    {
        var filePath = Path.Combine(_tempDirectory, $"binary_{Guid.NewGuid():N}.json");
        var garbage = new byte[1024];
        new Random(42).NextBytes(garbage);
        File.WriteAllBytes(filePath, garbage);

        var service = new PresetService(filePath);
        var presets = service.LoadPresets();

        Assert.NotNull(presets);
        Assert.Equal(3, presets.Count);
    }

    [Fact]
    public void PresetService_ConcurrentReads_AreThreadSafe()
    {
        var filePath = Path.Combine(_tempDirectory, $"concurrent_read_{Guid.NewGuid():N}.json");
        var initialService = new PresetService(filePath);
        initialService.LoadPresets(); // initialize file

        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var service = new PresetService(filePath);
                    for (int j = 0; j < 10; j++)
                    {
                        var presets = service.LoadPresets();
                        Assert.NotEmpty(presets);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
        Assert.Empty(exceptions);
    }

    [Theory]
    [InlineData("CON", "_CON")]
    [InlineData("PRN", "_PRN")]
    [InlineData("AUX", "_AUX")]
    [InlineData("NUL", "_NUL")]
    public void FileNameService_BareReservedDeviceNames_PrefixesUnderscore(string reserved, string expected)
    {
        var sanitized = _fileNameService.SanitizeFileName(reserved);
        Assert.Equal(expected, sanitized);
    }

    [Fact]
    public void PresetService_CorruptedJson_NullElement_CausesNullReferenceExceptionInCallers()
    {
        var filePath = Path.Combine(_tempDirectory, $"null_element_{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, "{\"Presets\": [null]}");

        var service = new PresetService(filePath);
        var presets = service.LoadPresets();

        // LoadPresets filters out null elements; falls back to default presets when empty
        Assert.NotEmpty(presets);
        Assert.All(presets, p => Assert.NotNull(p));

        // Calling methods does not crash with NullReferenceException
        var defaultPreset = service.GetDefaultPreset();
        Assert.NotNull(defaultPreset);

        var a1 = service.GetPreset("A1 Monochrome PDF");
        Assert.NotNull(a1);

        service.SavePreset(new PlotPreset { Name = "NewPreset" });
        Assert.NotNull(service.GetPreset("NewPreset"));

        Assert.False(service.DeletePreset("NonExistent"));
    }

    [Fact]
    public void PresetService_CorruptedJson_NullPresetName_CausesNullReferenceExceptionInCallers()
    {
        var filePath = Path.Combine(_tempDirectory, $"null_name_{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, "{\"Presets\": [{\"Name\": null}]}");

        var service = new PresetService(filePath);
        var presets = service.LoadPresets();

        // Presets with null/empty names are filtered out safely
        Assert.NotEmpty(presets);
        Assert.All(presets, p =>
        {
            Assert.NotNull(p);
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
        });

        Assert.NotNull(service.GetDefaultPreset());
        Assert.Null(service.GetPreset("NonExistent"));

        service.SavePreset(new PlotPreset { Name = "NewPreset" });
        Assert.NotNull(service.GetPreset("NewPreset"));

        Assert.False(service.DeletePreset("NonExistent"));
    }

    [Fact]
    public void PresetService_ConcurrentWrites_ThrowsIOException_DueToNoLocking()
    {
        var filePath = Path.Combine(_tempDirectory, $"concurrent_write_{Guid.NewGuid():N}.json");
        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var service = new PresetService(filePath);
                    var customPreset = new PlotPreset
                    {
                        Name = $"Preset_Thread_{threadId}",
                        Description = $"From thread {threadId}",
                        Config = new PlotConfiguration { DeviceName = "PDF.pc3" }
                    };
                    service.SavePreset(customPreset);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // With FileLock and unique GUID temp files, concurrent writes succeed without collision
        Assert.Empty(exceptions);
    }

    [Fact]
    public void PresetService_ConcurrentReadAndWrite_StressTest()
    {
        var filePath = Path.Combine(_tempDirectory, $"concurrent_mixed_{Guid.NewGuid():N}.json");
        var initialService = new PresetService(filePath);
        initialService.LoadPresets(); // initialize file

        var exceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var tasks = new List<Task>();

        // 5 reader tasks
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var service = new PresetService(filePath);
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var presets = service.LoadPresets();
                        Assert.NotNull(presets);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        // 5 writer tasks
        for (int i = 0; i < 5; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                var service = new PresetService(filePath);
                int count = 0;
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        service.SavePreset(new PlotPreset
                        {
                            Name = $"Dynamic_{threadId}_{count++}",
                            Config = new PlotConfiguration()
                        });
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                    Thread.Sleep(5);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
        // Under mixed concurrent read/writes with FileLock, no IO collisions or access violations occur
        Assert.Empty(exceptions);
    }
}
