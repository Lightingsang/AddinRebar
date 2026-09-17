using System.IO;
using HPEtabs.McpBridge.Service;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPEtabs.McpBridge.Tests;

/// <summary>
///     The save-and-copy sequence with a fake `File.Save()` on a temp folder: the user's own save is preserved
///     once, the bridge's own saves are not copied twice, a failed save copies nothing, buckets are pruned,
///     labels cannot escape their bucket, and only file names come back.
/// </summary>
public sealed class EtabsSnapshotManagerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "hpetabs-snap-" + Guid.NewGuid().ToString("N"));
    private readonly string _model;
    private DateTime _clock = new(2026, 9, 17, 10, 0, 0);

    public EtabsSnapshotManagerTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "models"));
        _model = Path.Combine(_root, "models", "Tower.EDB");
        File.WriteAllText(_model, "user version 1");
    }

    private EtabsSnapshotManager Manager() => new(Path.Combine(_root, "snapshots"), () => _clock);

    /// <summary>What ETABS does on Save: rewrites the file (new mtime, maybe new size).</summary>
    private int FakeSave(string content = "saved by bridge")
    {
        File.WriteAllText(_model, content);
        File.SetLastWriteTimeUtc(_model, File.GetLastWriteTimeUtc(_model).AddSeconds(1));
        return 0;
    }

    [Fact]
    public void First_run_keeps_the_users_file_then_saves_and_copies_the_prerun_snapshot()
    {
        var manager = Manager();

        var outcome = manager.Prepare(_model, "draw frames", () => FakeSave(), CancellationToken.None);

        Assert.True(outcome.Succeeded);
        Assert.Equal("20260917-100000-draw_frames.EDB", outcome.FileName);
        Assert.Equal("20260917-100000-draw_frames-presave.EDB", outcome.PresaveFileName);
        var modelDir = manager.ModelDirectory(_model);
        Assert.Equal("user version 1", File.ReadAllText(Path.Combine(modelDir, "presave", outcome.PresaveFileName!)));
        Assert.Equal("saved by bridge", File.ReadAllText(Path.Combine(modelDir, "prerun", outcome.FileName!)));
        Assert.DoesNotContain(Path.DirectorySeparatorChar, outcome.FileName!);
    }

    [Fact]
    public void A_second_run_after_the_bridges_own_save_takes_no_presave_but_a_user_save_in_between_does()
    {
        var manager = Manager();
        manager.Prepare(_model, "one", () => FakeSave("v1"), CancellationToken.None);
        _clock = _clock.AddMinutes(1);

        var second = manager.Prepare(_model, "two", () => FakeSave("v2"), CancellationToken.None);
        Assert.Null(second.PresaveFileName);
        Assert.True(second.Succeeded);

        // The user saves in ETABS (or edits the file any other way): the next run preserves that state first.
        File.WriteAllText(_model, "user version 2");
        File.SetLastWriteTimeUtc(_model, DateTime.UtcNow.AddMinutes(5));
        _clock = _clock.AddMinutes(1);
        var third = manager.Prepare(_model, "three", () => FakeSave("v3"), CancellationToken.None);
        Assert.NotNull(third.PresaveFileName);
        Assert.Equal("user version 2", File.ReadAllText(Path.Combine(manager.ModelDirectory(_model), "presave", third.PresaveFileName!)));
        Assert.Equal(2, Directory.GetFiles(Path.Combine(manager.ModelDirectory(_model), "presave")).Length);
        Assert.Equal(3, Directory.GetFiles(Path.Combine(manager.ModelDirectory(_model), "prerun")).Length);
    }

    [Fact]
    public void A_failed_save_copies_nothing_and_reports_the_return_code()
    {
        var manager = Manager();

        var outcome = manager.Prepare(_model, "x", () => 1, CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Contains("ETABS returned 1 from File.Save", outcome.Failure);
        Assert.Contains("nothing ran", outcome.Failure);
        Assert.False(Directory.Exists(Path.Combine(manager.ModelDirectory(_model), "prerun")));
        Assert.NotNull(outcome.PresaveFileName); // the user's file was preserved before the attempt
    }

    [Fact]
    public void Buckets_keep_the_newest_ten_prerun_and_five_presave_copies()
    {
        var manager = Manager();
        for (var i = 0; i < 12; i++)
        {
            // Every run counts as a user save too, so both buckets fill.
            File.WriteAllText(_model, "user " + i);
            File.SetLastWriteTimeUtc(_model, DateTime.UtcNow.AddMinutes(i));
            _clock = _clock.AddMinutes(1);
            Assert.True(manager.Prepare(_model, "run" + i, () => FakeSave("b" + i), CancellationToken.None).Succeeded);
        }

        var modelDir = manager.ModelDirectory(_model);
        var prerun = Directory.GetFiles(Path.Combine(modelDir, "prerun")).Select(Path.GetFileName).OrderBy(n => n).ToArray();
        var presave = Directory.GetFiles(Path.Combine(modelDir, "presave")).Select(Path.GetFileName).OrderBy(n => n).ToArray();
        Assert.Equal(EtabsSnapshotManager.PrerunKeep, prerun.Length);
        Assert.Equal(EtabsSnapshotManager.PresaveKeep, presave.Length);
        Assert.Contains(prerun, n => n!.Contains("run11"));
        Assert.DoesNotContain(prerun, n => n!.Contains("run0."));
        Assert.DoesNotContain(prerun, n => n!.Contains("run1."));
    }

    [Fact]
    public void Pruning_goes_by_the_timestamp_in_the_name_not_the_copied_mtime()
    {
        var manager = Manager();
        // Twelve runs; the user's file carries an OLD mtime on the last run (a restore), which File.Copy preserves on the presave copy.
        for (var i = 0; i < 12; i++)
        {
            File.WriteAllText(_model, "user " + i);
            File.SetLastWriteTimeUtc(_model, i == 11 ? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) : DateTime.UtcNow.AddMinutes(i));
            _clock = _clock.AddMinutes(1);
            Assert.True(manager.Prepare(_model, "run" + i, () => FakeSave("b" + i), CancellationToken.None).Succeeded);
        }

        var presave = Directory.GetFiles(Path.Combine(manager.ModelDirectory(_model), "presave")).Select(Path.GetFileName).ToArray();
        Assert.Equal(EtabsSnapshotManager.PresaveKeep, presave.Length);
        Assert.Contains(presave, n => n!.Contains("run11")); // the newest copy stays although its mtime says year 2000
        Assert.DoesNotContain(presave, n => n!.Contains("run6-"));
    }

    [Theory]
    [InlineData("../x", "x")]
    [InlineData(@"..\..\evil", "evil")]
    [InlineData("draw frames (test) #1", "draw_frames__test___1")]
    [InlineData("", "script")]
    [InlineData(null, "script")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyzabcdefghijklmn")]
    public void Labels_are_sanitized_and_cannot_leave_the_bucket(string? label, string expected)
    {
        Assert.Equal(expected, EtabsSnapshotManager.SanitizeLabel(label));

        var manager = Manager();
        var outcome = manager.Prepare(_model, label, () => FakeSave(), CancellationToken.None);
        Assert.True(outcome.Succeeded);
        Assert.Equal($"20260917-100000-{expected}.EDB", outcome.FileName);
        Assert.True(File.Exists(Path.Combine(manager.ModelDirectory(_model), "prerun", outcome.FileName!)));
    }

    [Fact]
    public void Two_runs_in_the_same_second_keep_both_copies()
    {
        var manager = Manager();
        var first = manager.Prepare(_model, "same", () => FakeSave("a"), CancellationToken.None);
        var second = manager.Prepare(_model, "same", () => FakeSave("b"), CancellationToken.None);

        Assert.Equal("20260917-100000-same.EDB", first.FileName);
        Assert.Equal("20260917-100000-same-2.EDB", second.FileName);
    }

    [Fact]
    public void An_exhausted_budget_stops_before_the_save_and_reports_a_timeout()
    {
        var manager = Manager();
        using var budget = new CancellationTokenSource();
        budget.Cancel();
        var saved = false;

        var outcome = manager.Prepare(_model, "late", () => { saved = true; return 0; }, budget.Token);

        Assert.False(outcome.Succeeded);
        Assert.True(outcome.TimedOut);
        Assert.False(saved);
        Assert.Contains("did not run", outcome.Failure);
    }

    [Theory]
    [InlineData(null, "No model (.EDB)")]
    [InlineData("", "No model (.EDB)")]
    [InlineData("(Untitled)", "No model (.EDB)")]
    [InlineData(@"\\srv\share\Tower.EDB", "UNC share")]
    [InlineData(@"C:\does\not\exist\Tower.EDB", "does not exist")]
    public void Models_without_a_local_file_are_refused_with_the_no_document_code(string? path, string reason)
    {
        var exception = Assert.Throws<BridgeRequestException>(() => EtabsSnapshotManager.EnsureSnapshotable(path));

        Assert.Equal(BridgeErrorCode.NoActiveDocument, exception.Code);
        Assert.Contains(reason, exception.Message);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { /* temp */ }
    }
}
