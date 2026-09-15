using HPNavis.McpBridge.Service;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

namespace HPNavis.McpBridge.Tests;

/// <summary>
///     The commit-then-rollback rules without Navisworks: the label is made unique against the current undo
///     top, an entry is "ours" only when it appeared with our label, and only our own entry is ever undone.
/// </summary>
public sealed class NavisUndoDecisionTests
{
    [Theory]
    [InlineData(null, null, "MCP: script")]
    [InlineData("  ", null, "MCP: script")]
    [InlineData(" list walls ", null, "MCP: list walls")]
    [InlineData("list walls", "MCP: something else", "MCP: list walls")]
    [InlineData("list walls", "MCP: list walls", "MCP: list walls (2)")]
    public void Label_is_prefixed_trimmed_and_made_unique_against_the_undo_top(string? requestLabel, string? currentTop, string expected)
    {
        Assert.Equal(expected, NavisUndoDecision.LabelFor(requestLabel, currentTop));
    }

    [Fact]
    public void Label_is_one_line_and_bounded_because_the_undo_menu_echoes_it_verbatim()
    {
        Assert.Equal("MCP: list all walls", NavisUndoDecision.LabelFor("list\r\n  all\twalls", null));

        var longLabel = new string('x', 100);
        var label = NavisUndoDecision.LabelFor(longLabel, null);
        Assert.StartsWith("MCP: " + new string('x', NavisUndoDecision.MaxLabelLength), label);
        Assert.EndsWith("…", label);
        Assert.True(label.Length <= "MCP: ".Length + NavisUndoDecision.MaxLabelLength + 1);
    }

    [Fact]
    public void The_suffix_is_only_added_while_the_plain_label_sits_on_top()
    {
        // the registry runs a tool under its name every time: run 1 → "MCP: t", run 2 → "MCP: t (2)", run 3 → "MCP: t" again
        // (the plain label now differs from the top), so consecutive runs always alternate and stay distinguishable
        Assert.Equal("MCP: t (2)", NavisUndoDecision.LabelFor("t", "MCP: t"));
        Assert.Equal("MCP: t", NavisUndoDecision.LabelFor("t", "MCP: t (2)"));
    }

    [Theory]
    [InlineData(true, "MCP: x", "user edit", "MCP: x", true)]     // appeared with our label
    [InlineData(true, "MCP: x", "MCP: x", "MCP: x", false)]       // was already on top: not ours (LabelFor prevents this, belt and braces)
    [InlineData(true, "MCP: x", "user edit", "user edit", false)] // empty transaction: nothing appeared
    [InlineData(false, "MCP: x", "user edit", "MCP: x", false)]   // commit failed: never touch the stack
    [InlineData(true, "MCP: x", null, null, false)]               // empty document, empty transaction
    public void An_entry_is_ours_only_when_it_appeared_with_our_label(bool committed, string label, string? before, string? after, bool expected)
    {
        Assert.Equal(expected, NavisUndoDecision.IsOurs(committed, label, before, after));
    }

    [Theory]
    [InlineData(TransactionModes.Auto, false, false, true, true, UndoReason.Keep)]
    [InlineData(TransactionModes.Auto, true, false, true, true, UndoReason.DryRun)]
    [InlineData(TransactionModes.Auto, true, false, false, false, UndoReason.DryRun)]
    [InlineData(TransactionModes.Auto, false, true, true, true, UndoReason.Failure)]
    [InlineData(TransactionModes.None, false, true, true, true, UndoReason.Failure)]   // failure wins over the none check
    [InlineData(TransactionModes.None, false, false, true, true, UndoReason.NoneViolation)]
    [InlineData(TransactionModes.None, false, false, false, true, UndoReason.NoneViolation)] // fingerprint moved without our entry
    [InlineData(TransactionModes.None, false, false, false, false, UndoReason.Keep)]
    [InlineData(TransactionModes.None, true, false, false, false, UndoReason.DryRun)]
    [InlineData(TransactionModes.Manual, false, false, true, true, UndoReason.Keep)]      // manual behaves like auto
    public void Classification_follows_the_transaction_table(string mode, bool dryRun, bool failed, bool ours, bool changed, UndoReason expected)
    {
        Assert.Equal(expected, NavisUndoDecision.Classify(mode, dryRun, failed, ours, changed));
    }

    [Theory]
    [InlineData(UndoReason.Keep, true, false)]
    [InlineData(UndoReason.Keep, false, false)]
    [InlineData(UndoReason.DryRun, true, true)]
    [InlineData(UndoReason.DryRun, false, false)]
    [InlineData(UndoReason.Failure, true, true)]
    [InlineData(UndoReason.Failure, false, false)]
    [InlineData(UndoReason.NoneViolation, true, true)]
    [InlineData(UndoReason.NoneViolation, false, false)]
    public void Only_our_own_entry_is_ever_rolled_back(UndoReason reason, bool ours, bool expected)
    {
        Assert.Equal(expected, NavisUndoDecision.ShouldRollBack(reason, ours));
    }

    [Fact]
    public void None_violation_message_states_whether_the_rollback_happened()
    {
        Assert.Contains("rolled back: yes", NavisUndoDecision.NoneViolationMessage(true));
        Assert.Contains("rolled back: no", NavisUndoDecision.NoneViolationMessage(false));
        Assert.Contains("transaction=\"auto\"", NavisUndoDecision.NoneViolationMessage(false));
    }
}
