using System;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using HPExcel.McpBridge.Safety;
using Xunit;

namespace HPExcel.McpBridge.Tests;

/// <summary>
///     Comprehensive test suite for ExcelSnapshotManager.
///     Verifies pre-mutation snapshot creation, resolution of local vs temp directory,
///     20-file retention policy pruning oldest files, and filename sanitization.
/// </summary>
public sealed class ExcelSnapshotManagerTests : IDisposable
{
    private readonly string _testDir;

    public ExcelSnapshotManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "Snapshots_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    private string CreateDummyXlsx(string fileName)
    {
        var path = Path.Combine(_testDir, fileName);
        using var wb = new XLWorkbook();
        wb.Worksheets.Add("Sheet1").Cell("A1").Value = "TestData";
        wb.SaveAs(path);
        return path;
    }

    [Fact]
    public void ResolveSnapshotDirectory_WithValidPath_CreatesAdjacentFolder()
    {
        var manager = new ExcelSnapshotManager();
        var wbPath = Path.Combine(_testDir, "Model.xlsx");

        var snapDir = manager.ResolveSnapshotDirectory(wbPath);

        var expectedDir = Path.Combine(_testDir, ExcelSnapshotManager.SnapshotFolder);
        Assert.Equal(expectedDir, snapDir);
        Assert.True(Directory.Exists(snapDir));
    }

    [Fact]
    public void ResolveSnapshotDirectory_WithNullOrUnsavedPath_FallsBackToTemp()
    {
        var manager = new ExcelSnapshotManager();

        var snapDirNull = manager.ResolveSnapshotDirectory(null);
        var expectedTemp = Path.Combine(Path.GetTempPath(), ExcelSnapshotManager.SnapshotFolder);
        Assert.Equal(expectedTemp, snapDirNull);
        Assert.True(Directory.Exists(snapDirNull));

        var snapDirEmpty = manager.ResolveSnapshotDirectory("   ");
        Assert.Equal(expectedTemp, snapDirEmpty);
    }

    [Fact]
    public void ResolveSnapshotDirectory_WithUncPath_FallsBackToTemp()
    {
        var manager = new ExcelSnapshotManager();
        var uncPath = @"\\server\share\folder\workbook.xlsx";

        var snapDir = manager.ResolveSnapshotDirectory(uncPath);
        var expectedTemp = Path.Combine(Path.GetTempPath(), ExcelSnapshotManager.SnapshotFolder);
        Assert.Equal(expectedTemp, snapDir);
    }

    [Fact]
    public void ResolveSnapshotDirectory_WithOverrideDirectory_UsesOverride()
    {
        var overrideDir = Path.Combine(_testDir, "CustomSnapshots");
        var manager = new ExcelSnapshotManager(overrideDir);

        var snapDir = manager.ResolveSnapshotDirectory(@"C:\Some\File.xlsx");
        Assert.Equal(overrideDir, snapDir);
        Assert.True(Directory.Exists(overrideDir));
    }

    [Fact]
    public void CreateSnapshot_ViaFileCopyFallback_ProducesValidBackupFile()
    {
        var manager = new ExcelSnapshotManager();
        var wbFile = CreateDummyXlsx("SourceModel.xlsx");

        var snapshotPath = manager.CreateSnapshot(
            comWorkbook: null,
            workbookPath: wbFile,
            label: "before_table_create");

        Assert.True(File.Exists(snapshotPath));
        Assert.Contains("SourceModel", snapshotPath);
        Assert.Contains("before_table_create", snapshotPath);
        Assert.EndsWith(".xlsx", snapshotPath);

        // Verify contents of created snapshot file
        using var snapshotWb = new XLWorkbook(snapshotPath);
        var val = snapshotWb.Worksheet("Sheet1").Cell("A1").GetString();
        Assert.Equal("TestData", val);
    }

    [Fact]
    public void CreateSnapshot_WhenNoComAndNoFileOnDisk_ThrowsInvalidOperationException()
    {
        var manager = new ExcelSnapshotManager();

        Assert.Throws<InvalidOperationException>(() =>
            manager.CreateSnapshot(null, @"C:\NonExistent\Ghost.xlsx", "label"));
    }

    [Fact]
    public void Sanitize_CleansSpecialChars_AndLimitsLength()
    {
        Assert.Equal("snapshot", ExcelSnapshotManager.Sanitize(null));
        Assert.Equal("snapshot", ExcelSnapshotManager.Sanitize("   "));
        Assert.Equal("My_Workbook", ExcelSnapshotManager.Sanitize("My Workbook?"));
        Assert.Equal("Sales__Data___Final", ExcelSnapshotManager.Sanitize("Sales: Data / Final *"));

        // Max 40 chars clamp
        var veryLong = new string('A', 60);
        var sanitized = ExcelSnapshotManager.Sanitize(veryLong);
        Assert.Equal(40, sanitized.Length);
    }

    [Fact]
    public void Prune_RetainsNewest20Files_AndDeletesOldestBackups()
    {
        var snapDir = Path.Combine(_testDir, "PruneTestDir");
        Directory.CreateDirectory(snapDir);

        // Create 25 dummy snapshot files with timestamp names
        for (int i = 1; i <= 25; i++)
        {
            var fileName = $"20260921-1200{i:D2}_Workbook.xlsx";
            File.WriteAllText(Path.Combine(snapDir, fileName), $"Snapshot {i}");
        }

        Assert.Equal(25, Directory.GetFiles(snapDir, "*.xlsx").Length);

        // Prune with default 20
        var deletedCount = ExcelSnapshotManager.Prune(snapDir, maxRetained: 20);
        Assert.Equal(5, deletedCount);

        var remainingFiles = Directory.GetFiles(snapDir, "*.xlsx")
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToList();

        Assert.Equal(20, remainingFiles.Count);

        // The oldest 5 (120001 through 120005) must be deleted
        Assert.DoesNotContain("20260921-120001_Workbook.xlsx", remainingFiles);
        Assert.DoesNotContain("20260921-120005_Workbook.xlsx", remainingFiles);

        // The newest 20 (120006 through 120025) must remain
        Assert.Contains("20260921-120006_Workbook.xlsx", remainingFiles);
        Assert.Contains("20260921-120025_Workbook.xlsx", remainingFiles);
    }

    [Fact]
    public void Prune_WhenUnderLimit_DeletesZeroFiles()
    {
        var snapDir = Path.Combine(_testDir, "FewSnapshots");
        Directory.CreateDirectory(snapDir);

        for (int i = 1; i <= 5; i++)
        {
            File.WriteAllText(Path.Combine(snapDir, $"20260921-12000{i}_Workbook.xlsx"), "data");
        }

        var deleted = ExcelSnapshotManager.Prune(snapDir, maxRetained: 20);
        Assert.Equal(0, deleted);
        Assert.Equal(5, Directory.GetFiles(snapDir, "*.xlsx").Length);
    }
}
