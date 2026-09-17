using HPEtabs.McpBridge.Service;
using Xunit;

namespace HPEtabs.McpBridge.Tests;

/// <summary>
///     The semantic tier verdict: every call on an ETABSv1 interface is classified by the table whatever the
///     receiver is called in the script, members of other types are nobody's business, and a file-taking member
///     only passes with a path the policy can read. Binds against the installed ETABSv1.dll through the bridge's
///     own compiler, so a row here is what the bridge decides.
/// </summary>
public sealed class EtabsTierAnalyzerTests
{
    [Theory]
    [InlineData("return sapModel.GetModelFilename();", EtabsTier.ReadOnly)]
    [InlineData("int n = 0; string[] names = null; return sapModel.PointObj.GetNameList(ref n, ref names);", EtabsTier.ReadOnly)]
    [InlineData("int n = 0; string[] a = null; int[] b = null; return sapModel.Analyze.GetCaseStatus(ref n, ref a, ref b);", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.Results.Setup.SetCaseSelectedForOutput(\"Dead\");", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.View.RefreshView(0, false);", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.SelectObj.ClearSelection();", EtabsTier.ReadOnly)]
    [InlineData("return etabs.Visible();", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.GetPresentUnits().ToString() + sapModel.GetDatabaseUnits().GetHashCode();", EtabsTier.ReadOnly)]
    [InlineData("var l = new List<int>(); l.Add(1); l.Clear(); return args.Str(\"x\", \"F\").StartsWith(\"F\") ? l.Count : 0;", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");", EtabsTier.Write)]
    [InlineData("string name = \"\"; return sapModel.FrameObj.AddByCoord(0, 0, 0, 0, 0, 3000, ref name);", EtabsTier.Write)]
    [InlineData("return sapModel.SetPresentUnits(eUnits.kN_mm_C);", EtabsTier.Write)]
    [InlineData("return sapModel.EditGeneral.Move(500, 0, 0);", EtabsTier.Write)]
    [InlineData("return sapModel.Analyze.SetRunCaseFlag(\"Dead\", true);", EtabsTier.Write)]
    [InlineData("return sapModel.Analyze.RunAnalysis();", EtabsTier.Destructive)]
    [InlineData("return sapModel.SetModelIsLocked(false);", EtabsTier.Destructive)]
    [InlineData("return sapModel.Analyze.DeleteResults(\"Dead\", false);", EtabsTier.Destructive)]
    [InlineData("int a = 0, b = 0, c = 0, d = 0; string e = null; return sapModel.DatabaseTables.ApplyEditedTables(false, ref a, ref b, ref c, ref d, ref e);", EtabsTier.Destructive)]
    [InlineData("return sapModel.InitializeNewModel(eUnits.kN_mm_C);", EtabsTier.Destructive)]
    [InlineData("return sapModel.FrameObj.Delete(\"F1\", eItemType.Objects);", EtabsTier.Destructive)]
    [InlineData("return sapModel.DesignSteel.StartDesign();", EtabsTier.Destructive)]
    [InlineData("string[] keys = null; return sapModel.DatabaseTables.ShowTablesInExcel(ref keys, 0);", EtabsTier.Destructive)]
    [InlineData("return sapModel.File.Save();", EtabsTier.Destructive)]
    [InlineData("return sapModel.File.Save(\"\");", EtabsTier.Destructive)]
    [InlineData("var n = nameof(sapModel.FrameObj.Delete); return n.Length;", EtabsTier.ReadOnly)]
    public void Members_reached_through_the_globals_are_classified_by_the_table(string code, EtabsTier expected)
    {
        var verdict = EtabsScriptEnvironment.Inspect(code);

        Assert.Equal(expected, verdict.Tier);
        Assert.Empty(verdict.Refusals);
    }

    [Theory]
    [InlineData("var m = sapModel; return m.FrameObj.SetSection(\"F1\", \"C40x40\");", EtabsTier.Write, "cFrameObj.SetSection")]
    [InlineData("var fo = sapModel.FrameObj; return fo.SetSection(\"F1\", \"C40x40\");", EtabsTier.Write, "cFrameObj.SetSection")]
    [InlineData("return ((cSapModel)sapModel).Analyze.RunAnalysis();", EtabsTier.Destructive, "cAnalyze.RunAnalysis")]
    [InlineData("Func<cSapModel, int> f = m => m.SetModelIsLocked(false); return f(sapModel);", EtabsTier.Destructive, "cSapModel.SetModelIsLocked")]
    [InlineData("int Go(cSapModel m) => m.FrameObj.SetSection(\"F1\", \"C40x40\"); return Go(sapModel);", EtabsTier.Write, "cFrameObj.SetSection")]
    [InlineData("var o = etabs; var s = o.SapModel; return s.Analyze.RunAnalysis();", EtabsTier.Destructive, "cAnalyze.RunAnalysis")]
    [InlineData("Func<eUnits> g = sapModel.GetPresentUnits; return g();", EtabsTier.ReadOnly, "cSapModel.GetPresentUnits")]
    [InlineData("cSapModel m = sapModel; return m?.SetModelIsLocked(false);", EtabsTier.Destructive, "cSapModel.SetModelIsLocked")]
    public void Aliases_casts_lambdas_and_method_groups_bind_to_the_same_member(string code, EtabsTier expected, string member)
    {
        var verdict = EtabsScriptEnvironment.Inspect(code);

        Assert.Equal(expected, verdict.Tier);
        Assert.Contains(verdict.Hits, h => h.Member == member);
    }

    [Fact]
    public void Tier_is_the_highest_member_and_preview_lists_writes_and_destructives_only()
    {
        var verdict = EtabsScriptEnvironment.Inspect("sapModel.FrameObj.SetSection(\"F1\", \"C40x40\"); sapModel.Analyze.RunAnalysis(); return sapModel.GetModelFilename();");

        Assert.Equal(EtabsTier.Destructive, verdict.Tier);
        var preview = EtabsTierAnalyzer.Preview(verdict.Hits, EtabsTier.Write);
        Assert.Equal(2, preview.Count);
        Assert.All(preview, d => Assert.Equal("PREVIEW", d.Id));
        Assert.Contains(preview, d => d.Message == "cFrameObj.SetSection (W)");
        Assert.Contains(preview, d => d.Message == "cAnalyze.RunAnalysis (D)");
    }

    [Fact]
    public void Navigation_properties_are_not_members_and_a_script_without_oapi_calls_is_read_only()
    {
        var verdict = EtabsScriptEnvironment.Inspect("var x = sapModel.FrameObj; var y = sapModel.Results; return 1;");

        Assert.Equal(EtabsTier.ReadOnly, verdict.Tier);
        Assert.Empty(verdict.Hits);
    }

    [Theory]
    [InlineData("return sapModel.File.Save(\"C:\\\\Models\\\\out.EDB\");", "C:\\Models\\out.EDB")]
    [InlineData("return sapModel.File.Save(@\"C:\\Models\\out.EDB\");", "C:\\Models\\out.EDB")]
    public void Path_literals_are_collected_for_the_run_time_check(string code, string literal)
    {
        var verdict = EtabsScriptEnvironment.Inspect(code);

        Assert.Equal(EtabsTier.Destructive, verdict.Tier);
        Assert.Empty(verdict.Refusals);
        Assert.Equal([literal], verdict.PathLiterals);
        Assert.True(verdict.TakesPaths);
    }

    [Theory]
    [InlineData("return sapModel.File.Save(args.Str(\"out\"));", "out")]
    [InlineData("return sapModel.File.Save(args.Require(\"target\"));", "target")]
    [InlineData("return sapModel.File.OpenFile(FileName: args.Str(\"model\"));", "model")]
    public void Args_keys_used_for_paths_are_collected_for_the_run_time_check(string code, string key)
    {
        var verdict = EtabsScriptEnvironment.Inspect(code);

        Assert.Empty(verdict.Refusals);
        Assert.Equal([key], verdict.PathArgKeys);
    }

    [Theory]
    [InlineData("return sapModel.File.Save(\"\\\\\\\\srv\\\\share\\\\m.EDB\");", "UNC")]
    [InlineData("return sapModel.File.Save(\"//srv/share/m.EDB\");", "UNC")]
    [InlineData("return sapModel.File.Save(\"C:\\\\Users\\\\x\\\\AppData\\\\Roaming\\\\HPEtabs\\\\McpBridge\\\\settings.json\");", "off limits")]
    [InlineData("return sapModel.File.Save(\"C:\\\\Program Files\\\\Computers and Structures\\\\ETABS 22\\\\x.EDB\");", "off limits")]
    [InlineData("var p = \"C:\\\\Models\\\\out.EDB\"; return sapModel.File.Save(p);", "literal or args.Str")]
    [InlineData("return sapModel.File.Save(\"C:\\\\\" + \"Models\\\\out.EDB\");", "literal or args.Str")]
    [InlineData("return sapModel.File.Save($\"C:\\\\Models\\\\{args.Str(\"n\")}.EDB\");", "literal or args.Str")]
    [InlineData("var k = \"out\"; return sapModel.File.Save(args.Str(k));", "literal or args.Str")]
    [InlineData("return sapModel.File.ExportFile(\"\", eFileTypeIO.TextFile);", "empty")]
    [InlineData("return sapModel.File.Save(args.Str(\"out\", \"C:\\\\Temp\\\\x.EDB\"));", "literal or args.Str")]
    [InlineData("var mine = new HPRebar.McpBridge.Core.Scripting.ScriptArgs(null); return sapModel.File.Save(mine.Str(\"out\"));", "literal or args.Str")]
    [InlineData("class ScriptArgs { public string Str(string k) => \"\\\\\\\\srv\\\\x\"; } var fake = new ScriptArgs(); return sapModel.File.Save(fake.Str(\"out\"));", "literal or args.Str")]
    [InlineData("Func<string, string, string> str = args.Str; return sapModel.File.Save(str(\"out\", null));", "literal or args.Str")]
    public void Path_arguments_the_policy_cannot_read_or_that_point_off_limits_are_refused(string code, string reason)
    {
        var verdict = EtabsScriptEnvironment.Inspect(code);

        Assert.NotEmpty(verdict.Refusals);
        Assert.All(verdict.Refusals, d => Assert.Equal(EtabsTierAnalyzer.PathDiagnosticId, d.Id));
        Assert.Contains(verdict.Refusals, d => d.Message.Contains(reason, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_member_the_table_does_not_know_is_destructive()
    {
        // Helper's methods are not in the table (the guard blocks the identifier anyway); the analyzer alone fails closed.
        var table = EtabsTierTable.Parse(["cSapModel.GetModelFilename\tR"]);
        var analyzer = new EtabsTierAnalyzer(table);
        var compiled = EtabsScriptEnvironment.Compiler.GetOrCompile("return sapModel.GetModelFilename() + sapModel.GetPresentUnits();");

        var verdict = analyzer.Inspect(compiled.Script!);

        Assert.Equal(EtabsTier.Destructive, verdict.Tier);
        Assert.Contains(verdict.Hits, h => h.Member.StartsWith("cSapModel.GetPresentUnits (not in the tier table)", StringComparison.Ordinal));
    }
}
