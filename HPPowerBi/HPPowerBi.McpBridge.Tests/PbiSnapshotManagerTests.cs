using System;
using System.IO;
using System.Text;
using HPPowerBi.McpBridge.Safety;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class PbiSnapshotManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PbiSnapshotManager _manager;

    public PbiSnapshotManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_Test_Snapshots_" + Guid.NewGuid().ToString("N"));
        _manager = new PbiSnapshotManager(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    [Fact]
    public void SanitizeFileName_ReplacesInvalidCharacters()
    {
        var sanitized = PbiSnapshotManager.SanitizeFileName("Sales/Model:2026*New?Version");

        Assert.DoesNotContain("/", sanitized);
        Assert.DoesNotContain(":", sanitized);
        Assert.DoesNotContain("*", sanitized);
        Assert.DoesNotContain("?", sanitized);
        Assert.Contains("Sales_Model_2026_New_Version", sanitized);
    }

    [Fact]
    public void PruneOldSnapshots_ExceedingThreshold_RemovesOldestFiles()
    {
        Directory.CreateDirectory(_tempDir);

        // Create 15 synthetic snapshot files
        for (var i = 1; i <= 15; i++)
        {
            var path = Path.Combine(_tempDir, $"Snapshot_Db_{i:D2}.json");
            File.WriteAllText(path, "{}", Encoding.UTF8);
            File.SetCreationTimeUtc(path, DateTime.UtcNow.AddMinutes(i));
        }

        Assert.Equal(15, Directory.GetFiles(_tempDir, "Snapshot_*.json").Length);

        // Prune to retain 5
        var pruned = _manager.PruneOldSnapshots(maxRetained: 5);

        Assert.Equal(10, pruned);
        var remaining = Directory.GetFiles(_tempDir, "Snapshot_*.json");
        Assert.Equal(5, remaining.Length);
    }
}
