using System.IO;
using HPSap2000.McpBridge.Service;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPSap2000.McpBridge.Tests;

public sealed class SapSnapshotManagerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "hpsap-snap-" + Guid.NewGuid().ToString("N"));
    private readonly string _model;
    private DateTime _clock = new(2026, 9, 21, 10, 0, 0);

    public SapSnapshotManagerTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "models"));
        _model = Path.Combine(_root, "models", "Bridge.SDB");
        File.WriteAllText(_model, "user version 1");
    }

    private SapSnapshotManager Manager() => new(Path.Combine(_root, "snapshots"), () => _clock);

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
        Assert.Equal("20260921-100000-draw_frames.SDB", outcome.FileName);
        Assert.Equal("20260921-100000-draw_frames-presave.SDB", outcome.PresaveFileName);
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
        Assert.True(second.Succeeded);
        Assert.Null(second.PresaveFileName);

        File.WriteAllText(_model, "user save in between");
        File.SetLastWriteTimeUtc(_model, File.GetLastWriteTimeUtc(_model).AddSeconds(5));
        _clock = _clock.AddMinutes(1);

        var third = manager.Prepare(_model, "three", () => FakeSave("v3"), CancellationToken.None);
        Assert.True(third.Succeeded);
        Assert.NotNull(third.PresaveFileName);
        var modelDir = manager.ModelDirectory(_model);
        Assert.Equal("user save in between", File.ReadAllText(Path.Combine(modelDir, "presave", third.PresaveFileName!)));
    }

    [Fact]
    public void A_failed_save_copies_no_prerun_snapshot_and_reports_the_failure()
    {
        var manager = Manager();

        var outcome = manager.Prepare(_model, "failing", () => 42, CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Contains("SAP2000 returned 42 from File.Save", outcome.Failure);
        var modelDir = manager.ModelDirectory(_model);
        Assert.False(Directory.Exists(Path.Combine(modelDir, "prerun")));
    }

    [Fact]
    public void Timeout_aborts_without_running_and_reports_which_step_timed_out()
    {
        var manager = Manager();
        using var source = new CancellationTokenSource();
        source.Cancel();

        var outcome = manager.Prepare(_model, "draw", () => FakeSave(), source.Token);

        Assert.False(outcome.Succeeded);
        Assert.True(outcome.TimedOut);
        Assert.Contains("before the pre-run save", outcome.Failure);
    }

    [Fact]
    public void Pruning_goes_by_the_timestamp_in_the_name_not_the_copied_mtime()
    {
        var manager = Manager();
        for (var i = 0; i < 12; i++)
        {
            File.WriteAllText(_model, "user " + i);
            File.SetLastWriteTimeUtc(_model, i == 11 ? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) : DateTime.UtcNow.AddMinutes(i));
            _clock = _clock.AddMinutes(1);
            Assert.True(manager.Prepare(_model, "run" + i, () => FakeSave("b" + i), CancellationToken.None).Succeeded);
        }

        var presave = Directory.GetFiles(Path.Combine(manager.ModelDirectory(_model), "presave")).Select(Path.GetFileName).ToArray();
        Assert.Equal(SapSnapshotManager.PresaveKeep, presave.Length);
        Assert.Contains(presave, n => n!.Contains("run11"));
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
        Assert.Equal(expected, SapSnapshotManager.SanitizeLabel(label));

        var manager = Manager();
        var outcome = manager.Prepare(_model, label, () => FakeSave(), CancellationToken.None);
        Assert.True(outcome.Succeeded);
        Assert.Equal($"20260921-100000-{expected}.SDB", outcome.FileName);
        Assert.True(File.Exists(Path.Combine(manager.ModelDirectory(_model), "prerun", outcome.FileName!)));
    }

    [Fact]
    public void Two_runs_in_the_same_second_keep_both_copies()
    {
        var manager = Manager();
        var first = manager.Prepare(_model, "same", () => FakeSave("a"), CancellationToken.None);
        var second = manager.Prepare(_model, "same", () => FakeSave("b"), CancellationToken.None);

        Assert.Equal("20260921-100000-same.SDB", first.FileName);
        Assert.Equal("20260921-100000-same-2.SDB", second.FileName);
    }

    [Theory]
    [InlineData(null, "No model (.SDB) with a file path")]
    [InlineData("", "No model (.SDB) with a file path")]
    [InlineData("relative.SDB", "No model (.SDB) with a file path")]
    [InlineData(@"(Untitled)", "No model (.SDB) with a file path")]
    [InlineData(@"\\srv\share\m.SDB", "UNC")]
    [InlineData(@"C:\does\not\exist\Bridge.SDB", "does not exist")]
    public void Preconditions_refuse_empty_relative_and_UNC_paths(string? path, string expected)
    {
        var ex = Assert.Throws<BridgeRequestException>(() => SapSnapshotManager.EnsureSnapshotable(path));
        Assert.Equal(BridgeErrorCode.NoActiveDocument, ex.Code);
        Assert.Contains(expected, ex.Message);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); }
        catch { }
    }
}
