using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.ChangeSets;
using HPAutoCad.Aec.Model;
using Xunit;

namespace HPAutoCad.Aec.Tests;

/// <summary>The change-set ledger: ids, caps, the state machine, what a recorded op remembers, and the envelopes at the caps.</summary>
public sealed class ChangeSetTests
{
    private static readonly JsonSerializerOptions Bridge = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReferenceHandler = ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    private static readonly DateTimeOffset T0 = new(2026, 9, 17, 10, 0, 0, TimeSpan.FromHours(7));

    private static JsonElement Args(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private sealed class Bag : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private static CommitRecord Commit(Bag? bag = null, string[]? created = null, string[]? modified = null) =>
        new(T0.AddMinutes(1), [new OpOutcome(1, "create_entities_batch", true, 2, 0, 0, null)], created ?? ["2A", "2B"], modified ?? [], [], ["HP-MCP-ISSUES"], [], bag, true);

    [Fact]
    public void Sets_get_sequential_ids_and_trimmed_labels()
    {
        var ledger = new ChangeSetLedger("plan.dwg");
        var a = ledger.Begin("  L3 reroute ", T0);
        var b = ledger.Begin(null, T0);
        Assert.Equal(("CS-001", "L3 reroute", ChangeSetState.Pending), (a.Id, a.Label, a.State));
        Assert.Equal(("CS-002", (string?)null), (b.Id, b.Label));
        Assert.Same(a, ledger.Get(" cs-001 "));
        Assert.Contains("CS-001 (pending)", Assert.Throws<ArgumentException>(() => ledger.Get("CS-009")).Message);
        Assert.Contains("required", Assert.Throws<ArgumentException>(() => ledger.Get("")).Message);
        Assert.Contains("120", Assert.Throws<ArgumentException>(() => ledger.Begin(new string('x', ChangeSetLedger.MaxLabelChars + 1), T0)).Message);
    }

    [Fact]
    public void A_recorded_op_keeps_a_copy_of_its_arguments_and_reads_them_back_in_one_line()
    {
        var ledger = new ChangeSetLedger("plan.dwg");
        var set = ledger.Begin("x", T0);
        var op = ledger.Record(set, "update_entities_batch", Args("""{"items":[{"handle":"2A","set":{"text":"A"}},{"handle":"2B"}],"handles":["2C"],"changeSetId":"CS-001"}"""), ["2A", "2B", "2c", "2C"], T0);
        Assert.Equal((1, "update_entities_batch"), (op.Index, op.Tool));
        Assert.Equal(["2A", "2B", "2c"], op.Handles);
        Assert.Equal("items 2 · handles 1", op.Summary);
        Assert.Equal("op create · items 1 · apply false", ledger.Record(set, "manage_annotations", Args("""{"op":"create","items":[{}],"apply":false}"""), [], T0).Summary);
        Assert.EndsWith("…", op.ArgsText(20));
        Assert.Equal(21, op.ArgsText(20).Length);
        var d = op.Describe(500);
        Assert.Equal(["args", "handleCount", "handles", "index", "recordedAt", "summary", "tool"], d.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(2, set.Ops.Count);
    }

    [Fact]
    public void Recording_is_capped_per_set_and_per_op_and_only_into_a_pending_set()
    {
        var ledger = new ChangeSetLedger("plan.dwg");
        var set = ledger.Begin(null, T0);
        Assert.Contains("65,536", Assert.Throws<ArgumentException>(() => ledger.Record(set, "create_entities_batch", Args("{\"items\":\"" + new string('x', ChangeSetLedger.MaxArgsBytes) + "\"}"), [], T0)).Message);
        for (var i = 0; i < ChangeSetLedger.MaxOps; i++) ledger.Record(set, "create_entities_batch", Args("{}"), [], T0);
        Assert.Contains("200 ops", Assert.Throws<ArgumentException>(() => ledger.Record(set, "create_entities_batch", Args("{}"), [], T0)).Message);
        ledger.MarkCommitted(set, Commit());
        var refused = Assert.Throws<ArgumentException>(() => ledger.Record(set, "create_entities_batch", Args("{}"), [], T0));
        Assert.Contains("committed, not pending", refused.Message);
    }

    [Fact]
    public void The_state_machine_commits_once_rolls_back_or_discards_and_releases_snapshots()
    {
        var ledger = new ChangeSetLedger("plan.dwg");
        var set = ledger.Begin("a", T0);
        ledger.Record(set, "create_entities_batch", Args("{}"), [], T0);
        var bag = new Bag();
        ledger.MarkCommitted(set, Commit(bag));
        Assert.Equal(ChangeSetState.Committed, set.State);
        Assert.Contains("committed, not pending", Assert.Throws<ArgumentException>(() => ledger.MarkCommitted(set, Commit())).Message);
        Assert.Throws<ArgumentException>(() => ledger.MarkDiscarded(set, T0));
        ledger.MarkRolledBack(set, T0.AddMinutes(2), null);
        Assert.False(bag.Disposed); // the undo may itself be rolled back by its request: the snapshots wait for the drawing to confirm
        Assert.Equal((ChangeSetState.RolledBack, T0.AddMinutes(2)), (set.State, set.EndedAt));
        ledger.RevertToCommitted(set, "the rollback was rolled back");
        Assert.Equal((ChangeSetState.Committed, (DateTimeOffset?)null, "the rollback was rolled back"), (set.State, set.EndedAt, set.Note));
        Assert.False(bag.Disposed);
        ledger.MarkRolledBack(set, T0.AddMinutes(4), null);
        ledger.ConfirmRolledBack(set);
        Assert.True(bag.Disposed);
        Assert.Null(set.Commit!.Snapshots);
        Assert.Throws<ArgumentException>(() => ledger.MarkRolledBack(set, T0, null));
        Assert.Throws<ArgumentException>(() => ledger.MarkCommitted(set, Commit()));
        Assert.Throws<ArgumentException>(() => ledger.Close(set, T0));

        var kept = ledger.Begin("kept", T0);
        var keptBag = new Bag();
        ledger.MarkCommitted(kept, Commit(keptBag));
        ledger.MarkRollbackIncomplete(kept, "a rollback left 3 handle(s) undone");
        Assert.Equal((ChangeSetState.Committed, false), (kept.State, keptBag.Disposed));
        ledger.Close(kept, T0.AddMinutes(5));
        Assert.Equal((ChangeSetState.Closed, true, (string?)null), (kept.State, keptBag.Disposed, kept.Note));
        Assert.False(kept.IsLive);

        var pending = ledger.Begin("b", T0);
        ledger.MarkDiscarded(pending, T0.AddMinutes(3));
        Assert.Equal(ChangeSetState.Discarded, pending.State);
        Assert.False(pending.IsLive);

        // a commit rolled back by its request (dryRun) is pending again, snapshots released, and the note travels with the set
        var dry = ledger.Begin("c", T0);
        var dryBag = new Bag();
        ledger.MarkCommitted(dry, Commit(dryBag));
        ledger.RevertToPending(dry, "the commit was rolled back");
        Assert.True(dryBag.Disposed);
        Assert.Equal((ChangeSetState.Pending, (CommitRecord?)null, "the commit was rolled back"), (dry.State, dry.Commit, dry.Note));
        ledger.MarkCommitted(dry, Commit());
        Assert.Null(dry.Note);
        Assert.Equal(new Dictionary<string, int> { ["pending"] = 0, ["committed"] = 1, ["rolled_back"] = 1, ["closed"] = 1, ["discarded"] = 1 }, ledger.ByState());
    }

    [Fact]
    public void At_the_live_cap_the_oldest_committed_set_is_closed_to_make_room()
    {
        var ledger = new ChangeSetLedger("plan.dwg");
        var bags = new List<Bag>();
        for (var i = 0; i < ChangeSetLedger.MaxSets; i++)
        {
            var s = ledger.Begin($"c{i}", T0);
            bags.Add(new Bag());
            ledger.MarkCommitted(s, Commit(bags[^1]));
        }

        var next = ledger.Begin("after twenty commits", T0, out var closed);
        Assert.Equal(("CS-021", "CS-001", ChangeSetState.Closed, true), (next.Id, closed!.Id, closed.State, bags[0].Disposed));
        Assert.Contains("closed automatically", closed.Note);
        Assert.False(bags[1].Disposed);
        Assert.Equal(ChangeSetLedger.MaxSets, ledger.Sets.Count(s => s.IsLive));
    }

    [Fact]
    public void Live_sets_are_capped_and_finished_ones_are_forgotten_oldest_first()
    {
        var ledger = new ChangeSetLedger("plan.dwg");
        for (var i = 0; i < ChangeSetLedger.MaxSets; i++) ledger.Begin($"s{i}", T0);
        Assert.Contains("20 change sets are pending", Assert.Throws<ArgumentException>(() => ledger.Begin("one more", T0)).Message);
        ledger.MarkDiscarded(ledger.Get("CS-003"), T0);
        ledger.MarkDiscarded(ledger.Get("CS-001"), T0);
        var next = ledger.Begin("after", T0); // room for one live set; the oldest finished set (CS-001) makes room in the list
        Assert.Equal("CS-021", next.Id);
        Assert.Equal(ChangeSetLedger.MaxSets, ledger.Sets.Count);
        Assert.DoesNotContain(ledger.Sets, s => s.Id == "CS-001");
        Assert.Contains(ledger.Sets, s => s.Id == "CS-003"); // still listed: only as many finished sets go as the cap needs
    }

    [Fact]
    public void Envelopes_at_the_caps_stay_under_the_result_budget()
    {
        var ledger = new ChangeSetLedger("a-drawing-with-a-long-file-name-for-the-summary-line.dwg");
        var set = ledger.Begin(new string('L', 120), T0);
        var items = string.Join(",", Enumerable.Range(0, 40).Select(i => "{\"handle\":\"" + $"{0x2A00 + i:X}FFFF" + "\",\"set\":{\"text\":\"a rather long text value number " + i + "\",\"layer\":\"A-ANNO-TEXT-0.25\"}}"));
        var args = Args("{\"items\":[" + items + "],\"handles\":[\"2AFFFF\"],\"atomic\":false}");
        for (var i = 0; i < ChangeSetLedger.MaxOps; i++) ledger.Record(set, "update_entities_batch", args, Enumerable.Range(0, 40).Select(k => $"{0x2A00 + k:X}FFFF").ToArray(), T0);
        var page = new AnalysisResult<Dictionary<string, object?>>
        {
            Items = set.Ops.Take(AecTools.MaxChangeOpLimit).Select(o => o.Describe(AecTools.MaxOpArgsChars)).ToArray(), Count = set.Ops.Count, Truncated = true,
            Summary = new { document = ledger.Document, set = set.Describe(), handles = 40 },
        };
        var bytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(page, Bridge));
        Assert.True(bytes < 60_000, $"preview page {bytes} B");

        var committed = new ChangeSetLedger("plan.dwg");
        for (var i = 0; i < ChangeSetLedger.MaxSets; i++)
        {
            var s = committed.Begin($"set {i}", T0);
            for (var k = 0; k < 12; k++) committed.Record(s, "create_entities_batch", Args("{}"), [], T0);
            committed.MarkCommitted(s, new CommitRecord(T0, Enumerable.Range(0, 200).Select(k => new OpOutcome(k + 1, "manage_annotations", k % 7 != 0, 1, 1, 0, k % 7 == 0 ? "LAYER_LOCKED: the layer is locked" : null)).ToArray(),
                Enumerable.Range(0, 2000).Select(k => $"{0x2A00 + k:X}FF").ToArray(), Enumerable.Range(0, 2000).Select(k => $"{0x3A00 + k:X}FF").ToArray(), [], ["HP-MCP-ISSUES", "HP-MCP-OPENINGS", "A-ANNO-ROOM"], ["DOOR-TEST", "COL-400"], null, false));
        }

        var summary = new AnalysisResult<Dictionary<string, object?>> { Items = committed.Sets.Select(s => s.Describe()).ToArray(), Count = committed.Sets.Count, Summary = new { document = committed.Document, byState = committed.ByState() } };
        var summaryBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(summary, Bridge));
        Assert.True(summaryBytes < 60_000, $"summary {summaryBytes} B");

        // a 200-op commit where every op warns three different things and 29 ops failed with three errors each
        var big = committed.Sets[0];
        var outcomes = Enumerable.Range(1, 200).Select(k => new OpOutcome(k, k % 2 == 0 ? "create_issue_markup" : "arch_create_room_tags", k % 7 != 0, 3, 1, 0, k % 7 == 0 ? "LAYER_LOCKED: layer 'A-ANNO-ROOM' is locked; nothing drawn for this issue" : null)).ToArray();
        var warnings = outcomes.SelectMany(o => new[] { (o.Index, "space auto: the issue was drawn in the space of its entities (Model)"), (o.Index, "layer HP-MCP-ISSUES created"), (o.Index, "the text index was truncated at 5000 texts; nearby-text evidence may be missing") }).ToArray();
        var errors = outcomes.Where(o => !o.Ok).SelectMany(o => Enumerable.Range(0, 3).Select(i => (o.Index, ToolError.ForHandle("LAYER_LOCKED", $"{0x4A00 + o.Index + i:X}FF", $"layer 'A-ANNO-ROOM' is locked: entity {0x4A00 + o.Index + i:X}FF cannot be modified")))).ToArray();
        var commit = ChangeSetEnvelopes.Commit(new ChangeSetEnvelopes.CommitInput(big, outcomes, warnings, errors, Enumerable.Range(0, 600).Select(k => $"{0x5A00 + k:X}FF").ToArray(), Enumerable.Range(0, 200).Select(k => $"{0x6A00 + k:X}FF").ToArray(), [],
            ["HP-MCP-ISSUES", "A-ANNO-ROOM"], Enumerable.Range(0, 40).Select(k => $"ROOM-TAG-{k}").ToArray(), 200, false, 3, false));
        var commitBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(commit, Bridge));
        Assert.True(commitBytes < 60_000, $"commit {commitBytes} B");
        Assert.Equal(3, commit.Warnings.Count(w => w.Contains("(items")));
        Assert.Equal(EditResult.MaxListedErrors + 1, commit.Errors.Count);
        Assert.False(commit.Success);
        Assert.Equal(200, commit.Items.Count);

        // a rollback of 2 000 modified handles on a layer the user locked after the commit
        var failures = Enumerable.Range(0, 2000).Select(k => ToolError.ForHandle(k % 100 == 0 ? "INTERNAL" : "LAYER_LOCKED", $"{0x3A00 + k:X}FF", $"{0x3A00 + k:X}FF: OnLockedLayer while undoing.")).ToArray();
        var rollback = ChangeSetEnvelopes.Rollback(new ChangeSetEnvelopes.RollbackInput(big, Enumerable.Range(0, 2000).Select(k => $"{0x2A00 + k:X}FF").ToArray(), [], [], [], failures, ["HP-MCP-ISSUES"], ["DOOR-TEST"], false));
        var rollbackBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(rollback, Bridge));
        Assert.True(rollbackBytes < 60_000, $"rollback {rollbackBytes} B");
        Assert.Equal(ChangeSetEnvelopes.MaxListedFailures + 1, rollback.Errors.Count);
        Assert.Contains("1980 more handle(s) failed (LAYER_LOCKED 1980, INTERNAL 20)", rollback.Errors[^1].Message);
        Assert.False(rollback.Success);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0, false)]  // touched nothing: cannot be told
    [InlineData(3, 3, 0, 0, 0, 0, true)]   // every created entity gone
    [InlineData(3, 2, 0, 0, 0, 0, false)]  // one created entity still there: the work is there
    [InlineData(3, 3, 1, 0, 0, 0, false)]  // created gone but the erased one still erased
    [InlineData(3, 3, 1, 1, 2, 2, true)]   // every class agrees
    [InlineData(0, 0, 0, 0, 2, 2, true)]   // modify-only, both read as their snapshot
    [InlineData(0, 0, 0, 0, 2, 1, false)]  // modify-only, one still modified (linetype, hatch pattern…): the fingerprint sees it
    [InlineData(2, 0, 0, 0, 2, 2, false)]  // the user erased nothing, the modifications read as before by accident: created decides
    public void The_undo_rule_needs_every_class_of_handle_to_agree(int created, int createdGone, int deleted, int deletedBack, int modified, int modifiedAsSnapshot, bool gone)
    {
        Assert.Equal(gone, UndoRule.WorkIsGone(new UndoEvidence(created, createdGone, deleted, deletedBack, modified, modifiedAsSnapshot)));
    }
}
