namespace HPGrasshopper.McpBridge.Tests.Fixtures;

using System;
using System.IO;

/// <summary>
///     Provides an isolated temporary directory for Grasshopper test fixture generation,
///     guaranteeing zero writes to git-tracked repository folders and clean teardown.
/// </summary>
public sealed class IsolatedFixtureDirectory : IDisposable
{
    public string DirectoryPath { get; }

    public IsolatedFixtureDirectory()
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "HPGrasshopper_Fixtures_" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(DirectoryPath);
        GenerateFixtures(DirectoryPath);
    }

    private static void GenerateFixtures(string targetDir)
    {
        var mathArchive = FixtureBuilder.CreateMathSliderArchive();
        FixtureBuilder.SaveFixturePair(targetDir, "MathSlider", mathArchive);

        var curveArchive = FixtureBuilder.CreateCurveGeneratorArchive();
        FixtureBuilder.SaveFixturePair(targetDir, "CurveGenerator", curveArchive);

        var revitArchive = FixtureBuilder.CreateRevitDirectShapeSyncMockArchive();
        FixtureBuilder.SaveFixturePair(targetDir, "RevitDirectShapeSyncMock", revitArchive);

        var scriptPath = ResolveContinuousBeamScriptPath();
        var renumberArchive = FixtureBuilder.CreateContinuousBeamRenumberingArchive(scriptPath);
        FixtureBuilder.SaveFixturePair(targetDir, "ContinuousBeamRenumbering", renumberArchive);
    }

    private static string? ResolveContinuousBeamScriptPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "definitions", "ContinuousBeamRenumbering.cs"));
        return File.Exists(candidate) ? candidate : null;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
        catch (Exception ex)
        {
            // Best effort temp cleanup; ignore filesystem lock during teardown
            _ = ex;
        }
    }
}
