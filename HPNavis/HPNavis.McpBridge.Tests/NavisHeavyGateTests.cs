using HPNavis.McpBridge.Service;
using Xunit;

namespace HPNavis.McpBridge.Tests;

/// <summary>
///     The host-side pre-pass that stands between a script and the Navisworks calls that load, save, export
///     or run a clash test: refused with an actionable message while the heavy opt-in is off, screened for
///     network and install-folder paths when it is on, and never in the way of read-only scripts.
/// </summary>
public sealed class NavisHeavyGateTests
{
    private static NavisHeavyGate Gate(bool enabled) => new() { Enabled = enabled };

    [Fact]
    public void Heavy_off_refuses_AppendFile_with_the_HEAVY_id_and_names_the_checkbox()
    {
        var diagnostics = Gate(false).Check("doc.AppendFile(@\"C:\\models\\x.nwd\"); return 1;", out var hasHeavyCalls);

        Assert.True(hasHeavyCalls);
        var single = Assert.Single(diagnostics);
        Assert.Equal(NavisHeavyGate.DiagnosticId, single.Id);
        Assert.Contains("AppendFile is a heavy operation", single.Message);
        Assert.Contains("Allow heavy operations", single.Message);
        Assert.Equal(1, single.Line);
    }

    [Fact]
    public void Heavy_on_lets_AppendFile_through_but_still_flags_the_call()
    {
        var diagnostics = Gate(true).Check("doc.AppendFile(@\"C:\\models\\x.nwd\"); return 1;", out var hasHeavyCalls);

        Assert.True(hasHeavyCalls);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("doc.SaveFile(@\"\\\\server\\share\\x.nwd\"); return 1;", "Network (UNC)")]
    [InlineData("doc.SaveFile(\"//server/share/x.nwd\"); return 1;", "Network (UNC)")]
    [InlineData("doc.AppendFile($@\"C:\\Users\\me\\AppData\\Roaming\\HPNavis\\McpBridge\\{1}.nwd\"); return 1;", "own folders")]
    [InlineData("doc.SaveFile(@\"C:\\Program Files\\Autodesk\\Navisworks Manage 2026\\x.nwd\"); return 1;", "own folders")]
    public void Heavy_on_refuses_network_and_protected_paths(string code, string fragment)
    {
        var diagnostics = Gate(true).Check(code, out var hasHeavyCalls);

        Assert.True(hasHeavyCalls);
        Assert.Contains(diagnostics, d => d.Id == NavisHeavyGate.DiagnosticId && d.Message.Contains(fragment));
    }

    [Fact]
    public void A_read_only_script_may_mention_a_share_path()
    {
        // comparing SourceFileName against a share is legitimate; the path policy applies only where a heavy call could consume it
        var diagnostics = Gate(false).Check("return doc.Models.Where(m => m.SourceFileName.StartsWith(@\"\\\\srv\\models\")).Count();", out var hasHeavyCalls);

        Assert.False(hasHeavyCalls);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Document_Clear_is_heavy_but_clearing_the_selection_is_not()
    {
        Assert.Single(Gate(false).Check("doc.Clear(); return 1;", out var docClear));
        Assert.True(docClear);

        Assert.Empty(Gate(false).Check("doc.CurrentSelection.Clear(); return 1;", out var selectionClear));
        Assert.False(selectionClear);
    }

    [Fact]
    public void Every_listed_member_counts_as_heavy()
    {
        foreach (var member in NavisHeavyGate.HeavyMembers)
        {
            var diagnostics = Gate(false).Check($"doc.{member}(); return 1;", out var hasHeavyCalls);
            Assert.True(hasHeavyCalls, member);
            Assert.Contains(diagnostics, d => d.Message.StartsWith(member + " is a heavy operation", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Timeout_ceiling_follows_the_opt_in()
    {
        var gate = Gate(false);
        Assert.Equal(NavisHeavyGate.NormalMaxTimeoutSeconds, gate.MaxTimeoutSeconds);
        Assert.Equal(120, gate.MaxTimeoutSeconds);

        gate.Enabled = true;
        Assert.Equal(NavisHeavyGate.HeavyMaxTimeoutSeconds, gate.MaxTimeoutSeconds);
        Assert.Equal(600, gate.MaxTimeoutSeconds);
    }

    [Fact]
    public void Unparseable_code_yields_no_heavy_verdict_and_does_not_throw()
    {
        var diagnostics = Gate(false).Check("return doc.Title", out var hasHeavyCalls); // missing ';' — the compiler reports that, not the gate

        Assert.False(hasHeavyCalls);
        Assert.Empty(diagnostics);
    }
}
