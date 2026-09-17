using System.Text.Json;
using HPEtabs.McpBridge.Service;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

namespace HPEtabs.McpBridge.Tests;

/// <summary>Where a file-taking member may point: never a share or the bridge's own folders, only under the model or the bridge's local root, and `args` values are screened at run time.</summary>
public sealed class EtabsPathPolicyTests
{
    private const string ModelDir = @"C:\Projects\Tower\Models";

    [Theory]
    [InlineData(@"\\srv\share\m.EDB", "UNC")]
    [InlineData("//srv/share/m.EDB", "UNC")]
    [InlineData(@"C:\Users\x\AppData\Roaming\HPEtabs\McpBridge\settings.json", "off limits")]
    [InlineData(@"C:\Users\x\AppData\Roaming\hpetabs\mcpserver\registry.db", "off limits")]
    [InlineData(@"C:\Program Files\Computers and Structures\ETABS 22\ETABS.exe", "off limits")]
    [InlineData("", "empty")]
    public void Static_rules_refuse_shares_and_the_bridge_server_and_install_folders(string value, string reason)
    {
        var refusal = EtabsPathPolicy.StaticRefusal(value);

        Assert.NotNull(refusal);
        Assert.Contains(reason, refusal, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(@"C:\Projects\Tower\Models\out.EDB")]
    [InlineData(@"C:\Projects\Tower\Models\exports\forces.csv")]
    [InlineData(@"c:\projects\tower\models\OUT.edb")]
    public void Paths_under_the_model_folder_pass(string value)
    {
        Assert.Null(EtabsPathPolicy.RuntimeRefusal(value, ModelDir, @"C:\Users\x\AppData\Local\HPEtabs"));
    }

    [Fact]
    public void Paths_under_the_bridge_local_root_pass()
    {
        Assert.Null(EtabsPathPolicy.RuntimeRefusal(@"C:\Users\x\AppData\Local\HPEtabs\exports\a.csv", ModelDir, @"C:\Users\x\AppData\Local\HPEtabs"));
    }

    [Theory]
    [InlineData(@"C:\Users\x\AppData\Local\HPEtabs\.\McpBridge\snapshots\Tower\prerun\a.EDB", "off limits")]
    [InlineData(@"C:\Users\x\AppData\Local\HPEtabs\\McpBridge\audit\a.log", "off limits")]
    [InlineData(@"C:\Users\x\AppData\Local\HPEtabs\exports\..\McpBridge\settings.json", "off limits")]
    [InlineData(@"C:\Users\x\AppData\Local\HPEtabs\mcpbridge\x.EDB", "off limits")]
    [InlineData(@"C:\Projects\Tower\Models\out.EDB:stream", "alternate data streams")]
    [InlineData(@"C:\Projects\Tower\other.EDB", "under the model folder")]
    [InlineData(@"D:\out.EDB", "under the model folder")]
    [InlineData(@"C:\Projects\Tower\Models\..\..\secret.EDB", "under the model folder")]
    [InlineData(@"out.EDB", "absolute")]
    [InlineData(@"Models\out.EDB", "absolute")]
    [InlineData(@"\out.EDB", "absolute")]
    [InlineData(@"\\srv\share\m.EDB", "UNC")]
    public void Paths_elsewhere_relative_or_traversing_out_are_refused(string value, string reason)
    {
        var refusal = EtabsPathPolicy.RuntimeRefusal(value, ModelDir, @"C:\Users\x\AppData\Local\HPEtabs");

        Assert.NotNull(refusal);
        Assert.Contains(reason, refusal, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_model_at_a_drive_root_gets_no_folder_of_its_own()
    {
        Assert.NotNull(EtabsPathPolicy.RuntimeRefusal(@"C:\out.EDB", @"C:\", @"C:\Users\x\AppData\Local\HPEtabs"));
        Assert.NotNull(EtabsPathPolicy.RuntimeRefusal(@"C:\Windows\x.EDB", @"C:\", @"C:\Users\x\AppData\Local\HPEtabs"));
        Assert.Null(EtabsPathPolicy.RuntimeRefusal(@"C:\Users\x\AppData\Local\HPEtabs\a.EDB", @"C:\", @"C:\Users\x\AppData\Local\HPEtabs"));
    }

    [Fact]
    public void A_declared_path_key_must_be_present_as_a_string()
    {
        var verdict = new TierVerdict(EtabsTier.Destructive, [], [], [], ["out"]);

        var missing = EtabsScriptRunner.PathRefusals(verdict, JsonDocument.Parse("""{"other":"x"}""").RootElement, ModelDir);
        Assert.Single(missing);
        Assert.Contains("args.out is read as a file path", missing[0].Message);

        var number = EtabsScriptRunner.PathRefusals(verdict, JsonDocument.Parse("""{"out":12}""").RootElement, ModelDir);
        Assert.Single(number);

        Assert.Single(EtabsScriptRunner.PathRefusals(verdict, null, ModelDir));
        Assert.Empty(EtabsScriptRunner.PathRefusals(verdict, JsonDocument.Parse("""{"out":"C:\\Projects\\Tower\\Models\\b.EDB"}""").RootElement, ModelDir));
    }

    [Fact]
    public void Without_a_model_folder_only_the_local_root_passes()
    {
        Assert.Null(EtabsPathPolicy.RuntimeRefusal(@"C:\Users\x\AppData\Local\HPEtabs\a.csv", null, @"C:\Users\x\AppData\Local\HPEtabs"));
        Assert.NotNull(EtabsPathPolicy.RuntimeRefusal(@"C:\Projects\Tower\Models\out.EDB", null, @"C:\Users\x\AppData\Local\HPEtabs"));
    }

    [Theory]
    [InlineData(@"C:\x", true)]
    [InlineData(@"\\srv\x", true)]
    [InlineData("/etc/x", true)]
    [InlineData(@"sub\file", true)]
    [InlineData("C40x40", false)]
    [InlineData("Dead+Live/2", false)]
    [InlineData("F1", false)]
    public void Path_shaped_strings_are_told_from_ordinary_values(string value, bool looksLikePath)
    {
        Assert.Equal(looksLikePath, EtabsPathPolicy.LooksLikePath(value));
    }

    [Fact]
    public void Args_strings_are_walked_with_their_key_paths()
    {
        var args = JsonDocument.Parse("""{"out":"C:\\a.csv","n":3,"nested":{"p":"x"},"list":["y",{"q":"z"}]}""").RootElement;

        var values = EtabsPathPolicy.StringValues(args).ToArray();

        Assert.Equal([("out", @"C:\a.csv"), ("nested.p", "x"), ("list[0]", "y"), ("list[1].q", "z")], values);
    }

    [Fact]
    public void Run_time_screening_checks_declared_path_keys_strictly_and_other_values_only_when_path_shaped()
    {
        var verdict = new TierVerdict(EtabsTier.Destructive, [], [], [@"C:\Projects\Tower\Models\a.EDB"], ["out"]);
        var ok = JsonDocument.Parse("""{"out":"C:\\Projects\\Tower\\Models\\b.EDB","section":"C40x40","combo":"D+L/2"}""").RootElement;
        Assert.Empty(EtabsScriptRunner.PathRefusals(verdict, ok, ModelDir));

        var bareName = JsonDocument.Parse("""{"out":"b.EDB"}""").RootElement;
        var refusals = EtabsScriptRunner.PathRefusals(verdict, bareName, ModelDir);
        Assert.Single(refusals);
        Assert.Equal(EtabsTierAnalyzer.PathDiagnosticId, refusals[0].Id);
        Assert.Contains("args.out", refusals[0].Message);

        var unc = JsonDocument.Parse("""{"out":"C:\\Projects\\Tower\\Models\\b.EDB","note":"\\\\srv\\share\\x"}""").RootElement;
        refusals = EtabsScriptRunner.PathRefusals(verdict, unc, ModelDir);
        Assert.Single(refusals);
        Assert.Contains("args.note", refusals[0].Message);
        Assert.Contains("UNC", refusals[0].Message);
    }

    [Fact]
    public void Run_time_screening_refuses_a_literal_outside_the_model_folder_and_skips_scripts_without_paths()
    {
        var elsewhere = new TierVerdict(EtabsTier.Destructive, [], [], [@"D:\elsewhere\a.EDB"], []);
        var refusals = EtabsScriptRunner.PathRefusals(elsewhere, null, ModelDir);
        Assert.Single(refusals);
        Assert.Contains(@"D:\elsewhere\a.EDB", refusals[0].Message);

        var noPaths = new TierVerdict(EtabsTier.Write, [], [], [], []);
        var args = JsonDocument.Parse("""{"x":"\\\\srv\\share"}""").RootElement;
        Assert.Empty(EtabsScriptRunner.PathRefusals(noPaths, args, ModelDir));
        Assert.All(refusals, d => Assert.IsType<ScriptDiagnostic>(d));
    }
}
