namespace HPBim.Client.Tests.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using Xunit;

/// <summary>
/// Adversarial stress tests empirically probing CLI commands for speculation loopholes,
/// bypass flags, and fallback backdoors under ĐIỀU 0.
/// </summary>
public sealed class FailFastCliCommandsAdversarialTests
{
    [Fact]
    public async Task BuildGradeBeamNetwork_UnverifiedLines_ThrowsFailFastException_IgnoringAnyBypassFlags()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"gb_net_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string linesPath = Path.Combine(tempDir, "lines.json");
        string textsPath = Path.Combine(tempDir, "texts.json");
        string outputPath = Path.Combine(tempDir, "output.json");

        try
        {
            var linesData = new
            {
                xLines = new[]
                {
                    new { y = 0.0, x1 = 0.0, x2 = 5000.0, len = 5000.0 },
                    new { y = 200.0, x1 = 0.0, x2 = 5000.0, len = 5000.0 }
                },
                yLines = Array.Empty<object>()
            };
            var textsData = new
            {
                dkTexts = new[]
                {
                    new { text = "DK1-X", layer = "DK", rx = 2500.0, ry = 100.0 }
                }
            };

            await File.WriteAllTextAsync(linesPath, JsonSerializer.Serialize(linesData));
            await File.WriteAllTextAsync(textsPath, JsonSerializer.Serialize(textsData));

            using var sw = new StringWriter();
            var cmd = new BuildGradeBeamNetworkCommand(new ConsoleFormatter(sw), new FailFastGate());

            // Adversarial attempt: pass bypass flags --force, --no-fail-fast, --force-unverified
            var args = CliParser.Parse([
                "build-grade-beam-network",
                "--lines", linesPath,
                "--texts", textsPath,
                "--output", outputPath,
                "--force",
                "--no-fail-fast",
                "--force-unverified"
            ]);

            // Must throw FailFastCoordinationException unconditionally
            var ex = await Assert.ThrowsAsync<FailFastCoordinationException>(() => cmd.ExecuteAsync(args));
            Assert.Equal("BeamNetwork", ex.ElementId);
            Assert.NotEmpty(ex.Violations);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GenerateGradeBeams_UnverifiedSpecs_ThrowsFailFastException_EvenWithForceUnverifiedFlag()
    {
        string scratchDir = "scratch";
        bool createdScratch = false;
        if (!Directory.Exists(scratchDir))
        {
            Directory.CreateDirectory(scratchDir);
            createdScratch = true;
        }

        string dataPath = Path.Combine(scratchDir, "grade_beams_model.json");
        string? originalContent = File.Exists(dataPath) ? await File.ReadAllTextAsync(dataPath) : null;

        try
        {
            var unverifiedData = new
            {
                totalBeams = 1,
                beams = new[]
                {
                    new
                    {
                        name = "DK-SPECULATIVE",
                        width = 200.0,
                        height = 400.0,
                        handle = "", // Missing genuine CAD handle
                        supports = new[] { "COL-1" } // Insufficient supports (requires >= 2)
                    }
                }
            };
            await File.WriteAllTextAsync(dataPath, JsonSerializer.Serialize(unverifiedData));

            var fakeClient = new FakeRevitPipeClient();
            using var sw = new StringWriter();
            var cmd = new GenerateGradeBeamsCommand(new ConsoleFormatter(sw), fakeClient, new FailFastGate());

            // Adversarial attempt: pass --force-unverified flag
            var args = CliParser.Parse(["generate-grade-beams", "--force-unverified"]);

            var ex = await Assert.ThrowsAsync<FailFastCoordinationException>(() => cmd.ExecuteAsync(args));
            Assert.Equal("GenerateGradeBeams", ex.ElementId);
            Assert.NotEmpty(ex.Violations);

            // Revit must NEVER receive any requests
            Assert.Empty(fakeClient.ExecutedRequests);
        }
        finally
        {
            if (originalContent != null)
            {
                await File.WriteAllTextAsync(dataPath, originalContent);
            }
            else if (File.Exists(dataPath))
            {
                File.Delete(dataPath);
            }
            if (createdScratch && Directory.Exists(scratchDir))
            {
                Directory.Delete(scratchDir, true);
            }
        }
    }

    [Fact]
    public void GenerateGradeBeams_BuildRevitScript_ThrowsOnMissingType_NoDefaultFallback()
    {
        string script = GenerateGradeBeamsCommand.BuildRevitScript();

        // Must verify missing FamilySymbol throws InvalidOperationException with [ĐIỀU 0]
        Assert.Contains("throw new InvalidOperationException($\"[ĐIỀU 0] Không tìm thấy FamilySymbol '{typeName}' trong Revit. Cấm tự gán loại mặc định.\");", script);

        // Must NOT contain fallback to default symbol
        Assert.DoesNotContain("defaultSymbol", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sym = symbolsDict[\"B_200x400\"]", script);
    }

    [Fact]
    public async Task GenerateFoundationSlabs_WithSpeculativeClusterFlag_ThrowsFailFastExceptionImmediately()
    {
        string scratchDir = "scratch";
        bool createdScratch = false;
        if (!Directory.Exists(scratchDir))
        {
            Directory.CreateDirectory(scratchDir);
            createdScratch = true;
        }

        string dataPath = Path.Combine(scratchDir, "footings_slab_model.json");
        string? originalContent = File.Exists(dataPath) ? await File.ReadAllTextAsync(dataPath) : null;

        try
        {
            var speculativeData = new
            {
                isSpeculativeCluster = true,
                warning = "Speculative free radius cluster",
                footings = Array.Empty<object>()
            };
            await File.WriteAllTextAsync(dataPath, JsonSerializer.Serialize(speculativeData));

            var fakeClient = new FakeRevitPipeClient();
            using var sw = new StringWriter();
            var cmd = new GenerateFoundationSlabsCommand(new ConsoleFormatter(sw), fakeClient, new FailFastGate());

            var args = CliParser.Parse(["generate-slabs"]);

            var ex = await Assert.ThrowsAsync<FailFastCoordinationException>(() => cmd.ExecuteAsync(args));
            Assert.Equal("FoundationSlabs", ex.ElementId);
            Assert.Contains("ĐIỀU 0", ex.Message);

            // Revit pipe must never receive any calls
            Assert.Empty(fakeClient.ExecutedRequests);
        }
        finally
        {
            if (originalContent != null)
            {
                await File.WriteAllTextAsync(dataPath, originalContent);
            }
            else if (File.Exists(dataPath))
            {
                File.Delete(dataPath);
            }
            if (createdScratch && Directory.Exists(scratchDir))
            {
                Directory.Delete(scratchDir, true);
            }
        }
    }

    [Fact]
    public async Task GenerateFoundationSlabs_WithoutHandleOrCrossCheck_ThrowsFailFastException()
    {
        string scratchDir = "scratch";
        bool createdScratch = false;
        if (!Directory.Exists(scratchDir))
        {
            Directory.CreateDirectory(scratchDir);
            createdScratch = true;
        }

        string dataPath = Path.Combine(scratchDir, "footings_slab_model.json");
        string? originalContent = File.Exists(dataPath) ? await File.ReadAllTextAsync(dataPath) : null;

        try
        {
            var unverifiedFootings = new
            {
                isSpeculativeCluster = false,
                footings = new[]
                {
                    new
                    {
                        name = "DC-01",
                        handle = "", // Missing handle
                        topZ = -1600.0,
                        bottomZ = -2600.0,
                        widthX = 1500.0,
                        widthY = 1500.0,
                        pileIndices = new[] { 1, 2 },
                        crossCheckPassed = false // Cross check not passed
                    }
                }
            };
            await File.WriteAllTextAsync(dataPath, JsonSerializer.Serialize(unverifiedFootings));

            var fakeClient = new FakeRevitPipeClient();
            using var sw = new StringWriter();
            var cmd = new GenerateFoundationSlabsCommand(new ConsoleFormatter(sw), fakeClient, new FailFastGate());

            var args = CliParser.Parse(["generate-slabs"]);

            var ex = await Assert.ThrowsAsync<FailFastCoordinationException>(() => cmd.ExecuteAsync(args));
            Assert.Equal("GenerateFoundationSlabs", ex.ElementId);
            Assert.NotEmpty(ex.Violations);

            Assert.Empty(fakeClient.ExecutedRequests);
        }
        finally
        {
            if (originalContent != null)
            {
                await File.WriteAllTextAsync(dataPath, originalContent);
            }
            else if (File.Exists(dataPath))
            {
                File.Delete(dataPath);
            }
            if (createdScratch && Directory.Exists(scratchDir))
            {
                Directory.Delete(scratchDir, true);
            }
        }
    }

    [Fact]
    public async Task AnalyzeFootings_FreeRadiusClustering_ThrowsFailFastException_Unconditionally()
    {
        string scratchDir = "scratch";
        bool createdScratch = false;
        if (!Directory.Exists(scratchDir))
        {
            Directory.CreateDirectory(scratchDir);
            createdScratch = true;
        }

        string dataPath = Path.Combine(scratchDir, "piles_true_bim_model.json");
        string? originalContent = File.Exists(dataPath) ? await File.ReadAllTextAsync(dataPath) : null;

        try
        {
            var pilesData = new
            {
                piles = new[]
                {
                    new { stt = 1, name = "C-1", revit_x = 0.0, revit_y = 0.0 },
                    new { stt = 2, name = "C-2", revit_x = 500.0, revit_y = 0.0 }
                }
            };
            await File.WriteAllTextAsync(dataPath, JsonSerializer.Serialize(pilesData));

            using var sw = new StringWriter();
            var cmd = new AnalyzeFootingsCommand(new ConsoleFormatter(sw), new FailFastGate());

            // Adversarial attempt: pass --no-fail-fast, --force
            var args = CliParser.Parse(["analyze-footings", "--no-fail-fast", "--force"]);

            var ex = await Assert.ThrowsAsync<FailFastCoordinationException>(() => cmd.ExecuteAsync(args));
            Assert.Equal("FootingClusters", ex.ElementId);
            Assert.NotEmpty(ex.Violations);
        }
        finally
        {
            if (originalContent != null)
            {
                await File.WriteAllTextAsync(dataPath, originalContent);
            }
            else if (File.Exists(dataPath))
            {
                File.Delete(dataPath);
            }
            if (createdScratch && Directory.Exists(scratchDir))
            {
                Directory.Delete(scratchDir, true);
            }
        }
    }
}
