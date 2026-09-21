using System.Text.Json;
using HPSap2000.McpBridge.Service;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

namespace HPSap2000.McpBridge.Tests;

/// <summary>Where a file-taking member may point: never a share or the bridge's own folders, only under the model or the bridge's local root, and `args` values are screened at run time.</summary>
public sealed class SapPathPolicyTests
{
    private const string ModelDir = @"C:\Projects\Bridge\Models";

    [Theory]
    [InlineData(@"\\srv\share\m.SDB", "UNC")]
    [InlineData("//srv/share/m.SDB", "UNC")]
    [InlineData(@"C:\Users\x\AppData\Roaming\HPSap2000\McpBridge\settings.json", "off limits")]
    [InlineData(@"C:\Users\x\AppData\Roaming\hpsap2000\mcpserver\registry.db", "off limits")]
    [InlineData(@"C:\Program Files\Computers and Structures\SAP2000 27\SAP2000.exe", "off limits")]
    [InlineData("", "empty")]
    public void Static_rules_refuse_shares_and_the_bridge_server_and_install_folders(string value, string reason)
    {
        var refusal = SapPathPolicy.StaticRefusal(value);

        Assert.NotNull(refusal);
        Assert.Contains(reason, refusal, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(@"C:\Projects\Bridge\Models\out.SDB")]
    [InlineData(@"C:\Projects\Bridge\Models\exports\forces.csv")]
    [InlineData(@"c:\projects\bridge\models\OUT.sdb")]
    public void Paths_under_the_model_folder_pass(string value)
    {
        Assert.Null(SapPathPolicy.RuntimeRefusal(value, ModelDir, @"C:\Users\x\AppData\Local\HPSap2000"));
    }

    [Fact]
    public void Paths_under_the_bridge_local_root_pass()
    {
        Assert.Null(SapPathPolicy.RuntimeRefusal(@"C:\Users\x\AppData\Local\HPSap2000\exports\a.csv", ModelDir, @"C:\Users\x\AppData\Local\HPSap2000"));
    }

    [Theory]
    [InlineData(@"C:\Users\x\AppData\Local\HPSap2000\.\McpBridge\snapshots\Bridge\prerun\a.SDB", "off limits")]
    [InlineData(@"C:\Users\x\AppData\Local\HPSap2000\\McpBridge\audit\a.log", "off limits")]
    [InlineData(@"C:\Users\x\AppData\Local\HPSap2000\exports\..\McpBridge\settings.json", "off limits")]
    [InlineData(@"C:\Users\x\AppData\Local\HPSap2000\mcpbridge\x.SDB", "off limits")]
    [InlineData(@"C:\Projects\Bridge\Models\out.SDB:stream", "alternate data streams")]
    [InlineData(@"C:\Projects\Bridge\other.SDB", "under the model folder")]
    [InlineData(@"D:\out.SDB", "under the model folder")]
    [InlineData(@"C:\Projects\Bridge\Models\..\..\secret.SDB", "under the model folder")]
    [InlineData(@"out.SDB", "absolute")]
    [InlineData(@"Models\out.SDB", "absolute")]
    [InlineData(@"\out.SDB", "absolute")]
    [InlineData(@"\\srv\share\m.SDB", "UNC")]
    public void Paths_elsewhere_relative_or_traversing_out_are_refused(string value, string reason)
    {
        var refusal = SapPathPolicy.RuntimeRefusal(value, ModelDir, @"C:\Users\x\AppData\Local\HPSap2000");

        Assert.NotNull(refusal);
        Assert.Contains(reason, refusal, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_model_at_a_drive_root_gets_no_folder_of_its_own()
    {
        Assert.NotNull(SapPathPolicy.RuntimeRefusal(@"C:\out.SDB", @"C:\", @"C:\Users\x\AppData\Local\HPSap2000"));
        Assert.NotNull(SapPathPolicy.RuntimeRefusal(@"C:\Windows\x.SDB", @"C:\", @"C:\Users\x\AppData\Local\HPSap2000"));
        Assert.Null(SapPathPolicy.RuntimeRefusal(@"C:\Users\x\AppData\Local\HPSap2000\a.SDB", @"C:\", @"C:\Users\x\AppData\Local\HPSap2000"));
    }

    [Fact]
    public void A_declared_path_key_must_be_present_as_a_string()
    {
        var verdict = new TierVerdict(SapTier.Destructive, [], [], [], ["out"]);

        var missing = SapScriptRunner.PathRefusals(verdict, JsonDocument.Parse("""{"other":"x"}""").RootElement, ModelDir);
        Assert.Single(missing);
        Assert.Contains("args.out is read as a file path", missing[0].Message);

        var number = SapScriptRunner.PathRefusals(verdict, JsonDocument.Parse("""{"out":12}""").RootElement, ModelDir);
        Assert.Single(number);

        Assert.Single(SapScriptRunner.PathRefusals(verdict, null, ModelDir));
        Assert.Empty(SapScriptRunner.PathRefusals(verdict, JsonDocument.Parse("""{"out":"C:\\Projects\\Bridge\\Models\\b.SDB"}""").RootElement, ModelDir));
    }

    [Fact]
    public void Without_a_model_folder_only_the_local_root_passes()
    {
        Assert.Null(SapPathPolicy.RuntimeRefusal(@"C:\Users\x\AppData\Local\HPSap2000\a.csv", null, @"C:\Users\x\AppData\Local\HPSap2000"));
        Assert.NotNull(SapPathPolicy.RuntimeRefusal(@"C:\Projects\Bridge\Models\out.SDB", null, @"C:\Users\x\AppData\Local\HPSap2000"));
    }

    [Theory]
    [InlineData(@"C:\x", true)]
    [InlineData(@"\\srv\x", true)]
    [InlineData("/etc/x", true)]
    [InlineData(@"sub\file", true)]
    [InlineData("W14X90", false)]
    [InlineData("Dead+Live/2", false)]
    [InlineData("F1", false)]
    public void Path_shaped_strings_are_told_from_ordinary_values(string value, bool looksLikePath)
    {
        Assert.Equal(looksLikePath, SapPathPolicy.LooksLikePath(value));
    }

    [Fact]
    public void Args_strings_are_walked_with_their_key_paths()
    {
        var args = JsonDocument.Parse("""{"out":"C:\\a.csv","n":3,"nested":{"p":"x"},"list":["y",{"q":"z"}]}""").RootElement;

        var values = SapPathPolicy.StringValues(args).ToArray();

        Assert.Equal([("out", @"C:\a.csv"), ("nested.p", "x"), ("list[0]", "y"), ("list[1].q", "z")], values);
    }

    [Fact]
    public void Run_time_screening_checks_declared_path_keys_strictly_and_other_values_only_when_path_shaped()
    {
        var verdict = new TierVerdict(SapTier.Destructive, [], [], [@"C:\Projects\Bridge\Models\a.SDB"], ["out"]);
        var ok = JsonDocument.Parse("""{"out":"C:\\Projects\\Bridge\\Models\\b.SDB","section":"W14X90","combo":"D+L/2"}""").RootElement;
        Assert.Empty(SapScriptRunner.PathRefusals(verdict, ok, ModelDir));

        var bareName = JsonDocument.Parse("""{"out":"b.SDB"}""").RootElement;
        var refusals = SapScriptRunner.PathRefusals(verdict, bareName, ModelDir);
        Assert.Single(refusals);
        Assert.Equal(SapTierAnalyzer.PathDiagnosticId, refusals[0].Id);
        Assert.Contains("args.out", refusals[0].Message);

        var unc = JsonDocument.Parse("""{"out":"C:\\Projects\\Bridge\\Models\\b.SDB","note":"\\\\srv\\share\\x"}""").RootElement;
        refusals = SapScriptRunner.PathRefusals(verdict, unc, ModelDir);
        Assert.Single(refusals);
        Assert.Contains("args.note", refusals[0].Message);
        Assert.Contains("UNC", refusals[0].Message);
    }

    [Fact]
    public void Run_time_screening_refuses_a_literal_outside_the_model_folder_and_skips_scripts_without_paths()
    {
        var elsewhere = new TierVerdict(SapTier.Destructive, [], [], [@"D:\elsewhere\a.SDB"], []);
        var refusals = SapScriptRunner.PathRefusals(elsewhere, null, ModelDir);
        Assert.Single(refusals);
        Assert.Contains(@"D:\elsewhere\a.SDB", refusals[0].Message);

        var noPaths = new TierVerdict(SapTier.Write, [], [], [], []);
        var args = JsonDocument.Parse("""{"x":"\\\\srv\\share"}""").RootElement;
        Assert.Empty(SapScriptRunner.PathRefusals(noPaths, args, ModelDir));
        Assert.All(refusals, d => Assert.IsType<ScriptDiagnostic>(d));
    }
}
