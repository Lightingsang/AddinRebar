using SAP2000v1;
using HPSap2000.McpBridge.Service;
using Xunit;

namespace HPSap2000.McpBridge.Tests;

/// <summary>The runner's pure parts: the fingerprint diff (additions and deletions only) and the units guarantee (forced, then restored even after a throw).</summary>
public sealed class SapRunnerPartsTests
{
    private static SapFingerprint Print(bool locked = false, string? file = @"C:\m\Bridge.SDB", params (string receiver, string[]? names)[] sets) =>
        new(sets.ToDictionary(s => s.receiver, s => s.names is null ? null : (IReadOnlySet<string>?)new HashSet<string>(s.names), StringComparer.Ordinal), locked, file);

    [Fact]
    public void Equal_fingerprints_report_no_change()
    {
        var a = Print(sets: [("FrameObj", ["F1", "F2"]), ("PointObj", ["1", "2"])]);
        var b = Print(sets: [("FrameObj", ["F2", "F1"]), ("PointObj", ["2", "1"])]);

        var (changed, notes) = SapFingerprint.Diff(a, b);

        Assert.Equal((0, 0, 0), (changed.Added, changed.Modified, changed.Deleted));
        Assert.Empty(notes);
    }

    [Fact]
    public void New_names_are_additions_and_missing_names_deletions_modified_is_always_zero()
    {
        var before = Print(sets: [("FrameObj", ["F1", "F2"]), ("LoadCases", ["Dead"])]);
        var after = Print(sets: [("FrameObj", ["F1", "F3", "F4"]), ("LoadCases", ["Dead", "Live"])]);

        var (changed, notes) = SapFingerprint.Diff(before, after);

        Assert.Equal(3, changed.Added);
        Assert.Equal(1, changed.Deleted);
        Assert.Equal(0, changed.Modified);
        Assert.Contains("FrameObj: +2 -1", notes);
        Assert.Contains("LoadCases: +1 -0", notes);
    }

    [Fact]
    public void A_receiver_that_refused_on_either_side_is_unknown_and_never_counted()
    {
        var before = Print(sets: [("FrameObj", ["F1", "F2"]), ("LoadCases", null)]);
        var after = Print(sets: [("FrameObj", null), ("LoadCases", ["Dead", "Live"])]);

        var (changed, notes) = SapFingerprint.Diff(before, after);

        Assert.Equal((0, 0, 0), (changed.Added, changed.Modified, changed.Deleted));
        Assert.Empty(notes);
    }

    [Fact]
    public void Lock_and_file_changes_are_notes_not_counts()
    {
        var before = Print(locked: true, file: @"C:\m\Bridge.SDB", sets: [("FrameObj", ["F1"])]);
        var after = Print(locked: false, file: @"C:\m\Bridge-copy.SDB", sets: [("FrameObj", ["F1"])]);

        var (changed, notes) = SapFingerprint.Diff(before, after);

        Assert.Equal((0, 0, 0), (changed.Added, changed.Modified, changed.Deleted));
        Assert.Contains(notes, n => n.StartsWith("model lock: True → False", StringComparison.Ordinal));
        Assert.Contains(notes, n => n.Contains("Bridge.SDB → Bridge-copy.SDB"));
        Assert.Contains("GroupDef", SapFingerprint.Receivers);
    }

    [Fact]
    public void Units_are_forced_for_the_body_and_restored_even_when_it_throws()
    {
        var current = eUnits.kip_ft_F;
        var calls = new List<eUnits>();
        var logs = new List<string>();
        eUnits? seenInside = null;

        var failure = SapUnitsPolicy.Run(() => current, u => { calls.Add(u); current = u; return 0; }, logs, () => { seenInside = current; });
        Assert.Null(failure);
        Assert.Equal(SapUnitsPolicy.ForcedUnits, seenInside);
        Assert.Equal(eUnits.kip_ft_F, current);
        Assert.Equal([SapUnitsPolicy.ForcedUnits, eUnits.kip_ft_F], calls);

        Assert.Throws<InvalidOperationException>(() => SapUnitsPolicy.Run(() => current, u => { current = u; return 0; }, logs, () => throw new InvalidOperationException("boom")));
        Assert.Equal(eUnits.kip_ft_F, current);
        Assert.Empty(logs);
    }

    [Fact]
    public void A_refused_units_switch_fails_before_the_body_and_a_failed_restore_is_a_warning()
    {
        var ran = false;
        var logs = new List<string>();

        var failure = SapUnitsPolicy.Run(() => eUnits.N_mm_C, _ => 1, logs, () => ran = true);
        Assert.NotNull(failure);
        Assert.Contains("SetPresentUnits", failure);
        Assert.False(ran);

        var attempt = 0;
        failure = SapUnitsPolicy.Run(() => eUnits.N_mm_C, _ => attempt++ == 0 ? 0 : 7, logs, () => ran = true);
        Assert.Null(failure);
        Assert.True(ran);
        Assert.Contains(logs, l => l.Contains("returned 7 restoring present units"));
    }
}
