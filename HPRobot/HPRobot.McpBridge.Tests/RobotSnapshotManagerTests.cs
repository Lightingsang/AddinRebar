using System;
using System.IO;
using System.Linq;
using System.Threading;
using HPRobot.McpBridge.Safety;
using Xunit;

namespace HPRobot.McpBridge.Tests;

public sealed class RobotSnapshotManagerTests : IDisposable
{
    private readonly string _testRoot;

    public RobotSnapshotManagerTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "HPRobot_SnapshotTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    // =========================================================================
    // 1. Directory Resolution
    // =========================================================================

    [Fact]
    public void ResolveSnapshotDirectory_WithExplicitOverride_ReturnsOverride()
    {
        var overrideDir = Path.Combine(_testRoot, "CustomSnapshots");
        var manager = new RobotSnapshotManager(overrideDir);

        var resolved = manager.ResolveSnapshotDirectory(@"C:\Some\Path\Model.rtd");
        Assert.Equal(overrideDir, resolved);
        Assert.True(Directory.Exists(overrideDir));
    }

    [Fact]
    public void ResolveSnapshotDirectory_NullOrEmptyModelPath_FallsBackToTemp()
    {
        var manager = new RobotSnapshotManager();

        var resolvedNull = manager.ResolveSnapshotDirectory(null);
        var resolvedEmpty = manager.ResolveSnapshotDirectory("");
        var resolvedWhitespace = manager.ResolveSnapshotDirectory("   ");

        var expectedTemp = Path.Combine(Path.GetTempPath(), RobotSnapshotManager.SnapshotFolder);
        Assert.Equal(expectedTemp, resolvedNull);
        Assert.Equal(expectedTemp, resolvedEmpty);
        Assert.Equal(expectedTemp, resolvedWhitespace);
    }

    [Fact]
    public void ResolveSnapshotDirectory_UnrootedOrUncPath_FallsBackToTemp()
    {
        var manager = new RobotSnapshotManager();

        var relativeResolved = manager.ResolveSnapshotDirectory("Model.rtd");
        var uncResolved = manager.ResolveSnapshotDirectory(@"\\server\share\models\Model.rtd");

        var expectedTemp = Path.Combine(Path.GetTempPath(), RobotSnapshotManager.SnapshotFolder);
        Assert.Equal(expectedTemp, relativeResolved);
        Assert.Equal(expectedTemp, uncResolved);
    }

    [Fact]
    public void ResolveSnapshotDirectory_ValidModelFile_ResolvesAdjacentFolder()
    {
        var manager = new RobotSnapshotManager();
        var modelFile = Path.Combine(_testRoot, "BridgeStructure.rtd");
        File.WriteAllText(modelFile, "dummy rtd data");

        var resolved = manager.ResolveSnapshotDirectory(modelFile);

        var expected = Path.Combine(_testRoot, RobotSnapshotManager.SnapshotFolder);
        Assert.Equal(expected, resolved);
        Assert.True(Directory.Exists(resolved));
    }

    // =========================================================================
    // 2. Name Sanitization
    // =========================================================================

    [Theory]
    [InlineData(null, "snapshot")]
    [InlineData("", "snapshot")]
    [InlineData("   ", "snapshot")]
    [InlineData("SimpleModel", "SimpleModel")]
    [InlineData("Model 2026 (Rev A)", "Model_2026__Rev_A")]
    [InlineData("Special/\\:*?\"<>|Chars", "Special_________Chars")]
    public void Sanitize_CleansInputStringsCorrectly(string? input, string expected)
    {
        var result = RobotSnapshotManager.Sanitize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_ClampsLengthTo40Characters()
    {
        var longName = new string('A', 60);
        var result = RobotSnapshotManager.Sanitize(longName);

        Assert.Equal(40, result.Length);
        Assert.Equal(new string('A', 40), result);
    }

    // =========================================================================
    // 3. Snapshot Creation (Headless File-Copy)
    // =========================================================================

    [Fact]
    public void CreateSnapshot_HeadlessExistingFile_CreatesTimestampedRtdCopy()
    {
        var manager = new RobotSnapshotManager(Path.Combine(_testRoot, "snaps"));
        var modelFile = Path.Combine(_testRoot, "TestModel.rtd");
        var testContent = "SAMPLE ROBOT MODEL BINARY DATA";
        File.WriteAllText(modelFile, testContent);

        var snapshotPath = manager.CreateSnapshot(null, modelFile, "pre_mutation");

        Assert.True(File.Exists(snapshotPath));
        Assert.EndsWith(".rtd", snapshotPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TestModel", Path.GetFileName(snapshotPath));
        Assert.Contains("pre_mutation", Path.GetFileName(snapshotPath));

        var copiedContent = File.ReadAllText(snapshotPath);
        Assert.Equal(testContent, copiedContent);
    }

    [Fact]
    public void CreateSnapshot_NonExistentFileAndNullRobot_ThrowsInvalidOperationException()
    {
        var manager = new RobotSnapshotManager();
        var nonExistent = Path.Combine(_testRoot, "Ghost.rtd");

        Assert.Throws<InvalidOperationException>(() =>
            manager.CreateSnapshot(null, nonExistent));
    }

    // =========================================================================
    // 4. Pruning & Retention Policy (20 Files Limit)
    // =========================================================================

    [Fact]
    public void Prune_NonExistentDirectory_ReturnsZero()
    {
        var fakeDir = Path.Combine(_testRoot, "Does_Not_Exist");
        var deleted = RobotSnapshotManager.Prune(fakeDir, 20);
        Assert.Equal(0, deleted);
    }

    [Fact]
    public void Prune_DirectoryWith20OrFewerFiles_DeletesNothing()
    {
        var snapDir = Path.Combine(_testRoot, "snaps_under_limit");
        Directory.CreateDirectory(snapDir);

        for (int i = 1; i <= 15; i++)
        {
            var fileName = $"20260921-1200{i:D2}_Model.rtd";
            File.WriteAllText(Path.Combine(snapDir, fileName), $"content {i}");
        }

        var deleted = RobotSnapshotManager.Prune(snapDir, 20);
        Assert.Equal(0, deleted);
        Assert.Equal(15, Directory.GetFiles(snapDir, "*.rtd").Length);
    }

    [Fact]
    public void Prune_DirectoryWith25Files_DeletesOldest5Files_RetainsNewest20()
    {
        var snapDir = Path.Combine(_testRoot, "snaps_25");
        Directory.CreateDirectory(snapDir);

        // Create 25 files with sequential timestamps
        for (int i = 1; i <= 25; i++)
        {
            var fileName = $"20260921-1200{i:D2}_Model.rtd";
            File.WriteAllText(Path.Combine(snapDir, fileName), $"snapshot data #{i}");
        }

        Assert.Equal(25, Directory.GetFiles(snapDir, "*.rtd").Length);

        // Prune to DefaultMaxRetained (20)
        var deleted = RobotSnapshotManager.Prune(snapDir, RobotSnapshotManager.DefaultMaxRetained);

        Assert.Equal(5, deleted);

        var remainingFiles = Directory.GetFiles(snapDir, "*.rtd")
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToList();

        Assert.Equal(20, remainingFiles.Count);

        // The oldest 5 files (01..05) must be deleted
        for (int i = 1; i <= 5; i++)
        {
            var expectedDeleted = $"20260921-1200{i:D2}_Model.rtd";
            Assert.DoesNotContain(expectedDeleted, remainingFiles);
        }

        // The newest 20 files (06..25) must remain
        for (int i = 6; i <= 25; i++)
        {
            var expectedKept = $"20260921-1200{i:D2}_Model.rtd";
            Assert.Contains(expectedKept, remainingFiles);
        }
    }

    [Fact]
    public void Prune_DoesNotDeleteNonRtdFiles()
    {
        var snapDir = Path.Combine(_testRoot, "snaps_with_other_files");
        Directory.CreateDirectory(snapDir);

        // Create 22 .rtd files
        for (int i = 1; i <= 22; i++)
        {
            var rtd = $"20260921-1200{i:D2}_Model.rtd";
            File.WriteAllText(Path.Combine(snapDir, rtd), "rtd data");
        }

        // Create other file types
        var txtFile = Path.Combine(snapDir, "readme.txt");
        var logFile = Path.Combine(snapDir, "backup.log");
        File.WriteAllText(txtFile, "documentation");
        File.WriteAllText(logFile, "log info");

        var deleted = RobotSnapshotManager.Prune(snapDir, 20);

        Assert.Equal(2, deleted); // Exactly 2 .rtd files deleted
        Assert.Equal(20, Directory.GetFiles(snapDir, "*.rtd").Length);
        Assert.True(File.Exists(txtFile), "Non-rtd files must be preserved.");
        Assert.True(File.Exists(logFile), "Non-rtd files must be preserved.");
    }

    [Fact]
    public void CreateSnapshot_WhenExceedingMaxRetained_AutomaticallyPrunesTo20()
    {
        var snapDir = Path.Combine(_testRoot, "snaps_autoprune");
        Directory.CreateDirectory(snapDir);
        var manager = new RobotSnapshotManager(snapDir);
        var modelFile = Path.Combine(_testRoot, "AutoPruneModel.rtd");
        File.WriteAllText(modelFile, "dummy model data");

        // Pre-populate with 20 snapshots with distinct labels
        for (int i = 1; i <= 20; i++)
        {
            var preFile = Path.Combine(snapDir, $"20260921-1000{i:D2}_AutoPruneModel_seed{i:D2}.rtd");
            File.WriteAllText(preFile, $"content {i}");
        }

        Assert.Equal(20, Directory.GetFiles(snapDir, "*.rtd").Length);

        // Now create 3 new snapshots via manager.CreateSnapshot
        for (int i = 21; i <= 23; i++)
        {
            var snapPath = manager.CreateSnapshot(null, modelFile, $"label_{i}");
            Assert.True(File.Exists(snapPath));
        }

        // Must still be exactly 20 files in directory (the 3 oldest seed files were pruned)
        var remaining = Directory.GetFiles(snapDir, "*.rtd");
        Assert.Equal(RobotSnapshotManager.DefaultMaxRetained, remaining.Length);
        Assert.Equal(20, remaining.Length);

        // Seed 01, 02, 03 must have been pruned
        Assert.False(File.Exists(Path.Combine(snapDir, "20260921-100001_AutoPruneModel_seed01.rtd")));
        Assert.False(File.Exists(Path.Combine(snapDir, "20260921-100002_AutoPruneModel_seed02.rtd")));
        Assert.False(File.Exists(Path.Combine(snapDir, "20260921-100003_AutoPruneModel_seed03.rtd")));
    }
}
