using HPSap2000.McpBridge.Service;
using Xunit;

namespace HPSap2000.McpBridge.Tests;

/// <summary>
///     The semantic tier verdict: every call on a SAP2000v1 interface is classified by the table whatever the
///     receiver is called in the script, members of other types are ignored, and a file-taking member
///     only passes with a path the policy can read.
/// </summary>
public sealed class SapTierAnalyzerTests
{
    [Theory]
    [InlineData("return sapModel.GetModelFilename();", SapTier.ReadOnly)]
    [InlineData("int n = 0; string[] names = null; return sapModel.PointObj.GetNameList(ref n, ref names);", SapTier.ReadOnly)]
    [InlineData("int n = 0; string[] a = null; int[] b = null; return sapModel.Analyze.GetCaseStatus(ref n, ref a, ref b);", SapTier.ReadOnly)]
    [InlineData("return sapModel.Results.Setup.SetCaseSelectedForOutput(\"Dead\");", SapTier.ReadOnly)]
    [InlineData("return sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();", SapTier.ReadOnly)]
    [InlineData("return sapModel.View.RefreshView(0, false);", SapTier.ReadOnly)]
    [InlineData("return sapModel.SelectObj.ClearSelection();", SapTier.ReadOnly)]
    [InlineData("return sap.Visible();", SapTier.ReadOnly)]
    [InlineData("return sapModel.GetPresentUnits().ToString() + sapModel.GetDatabaseUnits().GetHashCode();", SapTier.ReadOnly)]
    [InlineData("var l = new List<int>(); l.Add(1); l.Clear(); return args.Str(\"x\", \"F\").StartsWith(\"F\") ? l.Count : 0;", SapTier.ReadOnly)]
    [InlineData("return sapModel.FrameObj.SetSection(\"F1\", \"W14X90\");", SapTier.Write)]
    [InlineData("string name = \"\"; return sapModel.FrameObj.AddByCoord(0, 0, 0, 0, 0, 3, ref name);", SapTier.Write)]
    [InlineData("return sapModel.SetPresentUnits(eUnits.kN_m_C);", SapTier.Write)]
    [InlineData("return sapModel.EditGeneral.Move(0.5, 0, 0);", SapTier.Write)]
    [InlineData("return sapModel.Analyze.SetRunCaseFlag(\"Dead\", true);", SapTier.Write)]
    [InlineData("return sapModel.Analyze.RunAnalysis();", SapTier.Destructive)]
    [InlineData("return sapModel.SetModelIsLocked(false);", SapTier.Destructive)]
    [InlineData("return sapModel.Analyze.DeleteResults(\"Dead\", false);", SapTier.Destructive)]
    [InlineData("return sapModel.InitializeNewModel(eUnits.kN_m_C);", SapTier.Destructive)]
    [InlineData("return sapModel.FrameObj.Delete(\"F1\", eItemType.Objects);", SapTier.Destructive)]
    [InlineData("return sapModel.File.Save();", SapTier.Destructive)]
    [InlineData("return sapModel.File.Save(\"\");", SapTier.Destructive)]
    [InlineData("var n = nameof(sapModel.FrameObj.Delete); return n.Length;", SapTier.ReadOnly)]
    public void Members_reached_through_the_globals_are_classified_by_the_table(string code, SapTier expected)
    {
        var verdict = SapScriptEnvironment.Inspect(code);

        Assert.Equal(expected, verdict.Tier);
        Assert.Empty(verdict.Refusals);
    }

    [Theory]
    [InlineData("var m = sapModel; return m.FrameObj.SetSection(\"F1\", \"W14X90\");", SapTier.Write, "cFrameObj.SetSection")]
    [InlineData("var fo = sapModel.FrameObj; return fo.SetSection(\"F1\", \"W14X90\");", SapTier.Write, "cFrameObj.SetSection")]
    [InlineData("return ((cSapModel)sapModel).Analyze.RunAnalysis();", SapTier.Destructive, "cAnalyze.RunAnalysis")]
    [InlineData("Func<cSapModel, int> f = m => m.SetModelIsLocked(false); return f(sapModel);", SapTier.Destructive, "cSapModel.SetModelIsLocked")]
    [InlineData("int Go(cSapModel m) => m.FrameObj.SetSection(\"F1\", \"W14X90\"); return Go(sapModel);", SapTier.Write, "cFrameObj.SetSection")]
    [InlineData("var o = sap; var s = o.SapModel; return s.Analyze.RunAnalysis();", SapTier.Destructive, "cAnalyze.RunAnalysis")]
    [InlineData("Func<eUnits> g = sapModel.GetPresentUnits; return g();", SapTier.ReadOnly, "cSapModel.GetPresentUnits")]
    [InlineData("cSapModel m = sapModel; return m?.SetModelIsLocked(false);", SapTier.Destructive, "cSapModel.SetModelIsLocked")]
    public void Aliases_casts_lambdas_and_method_groups_bind_to_the_same_member(string code, SapTier expected, string member)
    {
        var verdict = SapScriptEnvironment.Inspect(code);

        Assert.Equal(expected, verdict.Tier);
        Assert.Contains(verdict.Hits, h => h.Member == member);
    }

    [Fact]
    public void Tier_is_the_highest_member_and_preview_lists_writes_and_destructives_only()
    {
        var verdict = SapScriptEnvironment.Inspect("sapModel.FrameObj.SetSection(\"F1\", \"W14X90\"); sapModel.Analyze.RunAnalysis(); return sapModel.GetModelFilename();");

        Assert.Equal(SapTier.Destructive, verdict.Tier);
        var preview = SapTierAnalyzer.Preview(verdict.Hits, SapTier.Write);
        Assert.Equal(2, preview.Count);
        Assert.All(preview, d => Assert.Equal("PREVIEW", d.Id));
        Assert.Contains(preview, d => d.Message == "cFrameObj.SetSection (W)");
        Assert.Contains(preview, d => d.Message == "cAnalyze.RunAnalysis (D)");
    }

    [Fact]
    public void Navigation_properties_are_not_members_and_a_script_without_oapi_calls_is_read_only()
    {
        var verdict = SapScriptEnvironment.Inspect("var x = sapModel.FrameObj; var y = sapModel.Results; return 1;");

        Assert.Equal(SapTier.ReadOnly, verdict.Tier);
        Assert.Empty(verdict.Hits);
    }

    [Theory]
    [InlineData("return sapModel.File.Save(\"C:\\\\Models\\\\out.SDB\");", "C:\\Models\\out.SDB")]
    [InlineData("return sapModel.File.Save(@\"C:\\Models\\out.SDB\");", "C:\\Models\\out.SDB")]
    public void Path_literals_are_collected_for_the_run_time_check(string code, string literal)
    {
        var verdict = SapScriptEnvironment.Inspect(code);

        Assert.Equal(SapTier.Destructive, verdict.Tier);
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
        var verdict = SapScriptEnvironment.Inspect(code);

        Assert.Empty(verdict.Refusals);
        Assert.Equal([key], verdict.PathArgKeys);
    }

    [Theory]
    [InlineData("return sapModel.File.Save(\"\\\\\\\\srv\\\\share\\\\m.SDB\");", "UNC")]
    [InlineData("return sapModel.File.Save(\"//srv/share/m.SDB\");", "UNC")]
    [InlineData("return sapModel.File.Save(\"C:\\\\Users\\\\x\\\\AppData\\\\Roaming\\\\HPSap2000\\\\McpBridge\\\\settings.json\");", "off limits")]
    [InlineData("return sapModel.File.Save(\"C:\\\\Program Files\\\\Computers and Structures\\\\SAP2000 27\\\\x.SDB\");", "off limits")]
    [InlineData("var p = \"C:\\\\Models\\\\out.SDB\"; return sapModel.File.Save(p);", "literal or args.Str")]
    [InlineData("return sapModel.File.Save(\"C:\\\\\" + \"Models\\\\out.SDB\");", "literal or args.Str")]
    [InlineData("return sapModel.File.Save($\"C:\\\\Models\\\\{args.Str(\"n\")}.SDB\");", "literal or args.Str")]
    [InlineData("var k = \"out\"; return sapModel.File.Save(args.Str(k));", "literal or args.Str")]
    [InlineData("return sapModel.File.OpenFile(\"\");", "empty")]
    [InlineData("return sapModel.File.Save(args.Str(\"out\", \"C:\\\\Temp\\\\x.SDB\"));", "literal or args.Str")]
    public void Path_arguments_the_policy_cannot_read_or_that_point_off_limits_are_refused(string code, string reason)
    {
        var verdict = SapScriptEnvironment.Inspect(code);

        Assert.NotEmpty(verdict.Refusals);
        Assert.All(verdict.Refusals, d => Assert.Equal(SapTierAnalyzer.PathDiagnosticId, d.Id));
        Assert.Contains(verdict.Refusals, d => d.Message.Contains(reason, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_member_the_table_does_not_know_is_destructive()
    {
        var table = SapTierTable.Parse(["cSapModel.GetModelFilename\tR"]);
        var analyzer = new SapTierAnalyzer(table);
        var compiled = SapScriptEnvironment.Compiler.GetOrCompile("return sapModel.GetModelFilename() + sapModel.GetPresentUnits();");

        var verdict = analyzer.Inspect(compiled.Script!);

        Assert.Equal(SapTier.Destructive, verdict.Tier);
        Assert.Contains(verdict.Hits, h => h.Member.StartsWith("cSapModel.GetPresentUnits (not in the tier table)", StringComparison.Ordinal));
    }
}
