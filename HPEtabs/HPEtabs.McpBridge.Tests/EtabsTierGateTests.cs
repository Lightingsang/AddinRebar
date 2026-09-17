using HPEtabs.McpBridge.Service;
using Xunit;

namespace HPEtabs.McpBridge.Tests;

/// <summary>
///     The static tier verdict the bridge takes before it runs anything: reads stay read-only, a writing member
///     makes the script a write, the destructive names are destructive on any receiver, and members reached
///     through a local variable are only caught when they are on the destructive list (the semantic pass that
///     binds receivers comes next). Needs ETABS 22 installed only because the project references the bridge.
/// </summary>
public sealed class EtabsTierGateTests
{
    [Theory]
    [InlineData("return sapModel.GetModelFilename();", EtabsTier.ReadOnly)]
    [InlineData("int n = 0; string[] names = null; return sapModel.PointObj.GetNameList(ref n, ref names);", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.Analyze.GetCaseStatus(ref a, ref b, ref c);", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.DesignResults.GetDesignResultsAvailable(ref a, ref b, ref c);", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.DatabaseTables.GetTableForDisplayArray(k, ref f, g, ref v, ref fk, ref n, ref d);", EtabsTier.ReadOnly)]
    [InlineData("return etabs.Visible();", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.GetPresentUnits().ToString() + sapModel.GetDatabaseUnits().GetHashCode();", EtabsTier.ReadOnly)]
    [InlineData("var u = sapModel.GetPresentUnits(); return u.ToString();", EtabsTier.ReadOnly)]
    [InlineData("return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");", EtabsTier.Write)]
    [InlineData("return sapModel.FrameObj.AddByCoord(0, 0, 0, 0, 0, 3000, ref name);", EtabsTier.Write)]
    [InlineData("return sapModel.SetPresentUnits(eUnits.kN_mm_C);", EtabsTier.Write)]
    [InlineData("return sapModel.Analyze.RunAnalysis();", EtabsTier.Destructive)]
    [InlineData("return sapModel.SetModelIsLocked(false);", EtabsTier.Destructive)]
    [InlineData("return sapModel.File.Save();", EtabsTier.Destructive)]
    [InlineData("return sapModel.File.OpenFile(args.Str(\"p\"));", EtabsTier.Destructive)]
    [InlineData("return sapModel.DatabaseTables.GetTableForDisplayCSVFile(k, ref f, g, ref v, args.Str(\"p\"));", EtabsTier.Destructive)]
    [InlineData("return sapModel.EditGeneral.Move(500, 0, 0);", EtabsTier.Write)]
    [InlineData("return sapModel.DesignSteel.StartDesign();", EtabsTier.Destructive)]
    [InlineData("return sapModel.FrameObj.Delete(\"F1\");", EtabsTier.Destructive)]
    public void Members_reached_from_the_globals_are_classified_by_name(string code, EtabsTier expected)
    {
        var (tier, _) = EtabsTierGate.Inspect(code);

        Assert.Equal(expected, tier);
    }

    [Theory]
    [InlineData("var m = sapModel; return m.FrameObj.SetSection(\"F1\", \"C40x40\");")]
    [InlineData("return ((cSapModel)sapModel).FrameObj.SetSection(\"F1\", \"C40x40\");")]
    [InlineData("return sapModel?.SetModelIsLocked(false);")]
    [InlineData("Func<cSapModel, int> f = m => m.FrameObj.SetSection(\"F1\", \"C40x40\"); return f(sapModel);")]
    [InlineData("int Go(cSapModel m) => m.Analyze.RunAnalysis(); return Go(sapModel);")]
    [InlineData("var o = etabs; return o.ApplicationExit(false);")]
    [InlineData("return sapModel == null ? 0 : 1;")]
    public void Globals_used_outside_a_plain_member_chain_fail_closed_as_destructive(string code)
    {
        var (tier, hits) = EtabsTierGate.Inspect(code);

        Assert.Equal(EtabsTier.Destructive, tier);
        Assert.Contains(hits, h => h.Member.Contains("used outside a plain member access", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("var fo = sapModel.FrameObj; return fo.SetSection(\"F1\", \"C40x40\");")]
    [InlineData("var m = etabs.SapModel; return m.FrameObj.SetSection(\"F1\", \"C40x40\");")]
    [InlineData("Func<eUnits> g = sapModel.GetPresentUnits; return g();")]
    [InlineData("return sapModel.FrameObj;")]
    public void Oapi_sub_objects_taken_as_values_fail_closed_as_destructive(string code)
    {
        var (tier, hits) = EtabsTierGate.Inspect(code);

        Assert.Equal(EtabsTier.Destructive, tier);
        Assert.Contains(hits, h => h.Member.Contains("taken as a value", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("return sapModel.Results.FrameForce(\"F1\", eItemType.Objects, ref n, ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h, ref i, ref j, ref k, ref l);")]
    [InlineData("return sapModel.Results.JointReact(\"1\", eItemType.Objects, ref n, ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h, ref i, ref j, ref k);")]
    [InlineData("return sapModel.Results.Setup.SetCaseSelectedForOutput(\"Dead\");")]
    [InlineData("return sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();")]
    public void Everything_under_Results_reads(string code)
    {
        var (tier, _) = EtabsTierGate.Inspect(code);

        Assert.Equal(EtabsTier.ReadOnly, tier);
    }

    [Theory]
    [InlineData("var s = args.Str(\"x\"); return s.StartsWith(\"F\");")]
    [InlineData("var l = new List<int>(); l.Clear(); return l.Count;")]
    [InlineData("return Environment.NewLine.Length;")]
    [InlineData("var sw = Stopwatch.StartNew(); return sw.ElapsedMilliseconds;")]
    [InlineData("var d = new Dictionary<string,int>(); d.Remove(\"x\"); return d.Count;")]
    public void Bcl_members_on_other_receivers_are_not_classified(string code)
    {
        var (tier, hits) = EtabsTierGate.Inspect(code);

        Assert.Equal(EtabsTier.ReadOnly, tier);
        Assert.Empty(hits);
    }

    [Fact]
    public void Members_on_other_receivers_are_ignored_unless_destructive()
    {
        var (readOnly, _) = EtabsTierGate.Inspect("var list = new List<string>(); list.Add(args.Str(\"x\")); return list.Count;");
        Assert.Equal(EtabsTier.ReadOnly, readOnly);

        var (destructive, hits) = EtabsTierGate.Inspect("var f = sapModel.File; return f.Save(args.Str(\"p\"));");
        Assert.Equal(EtabsTier.Destructive, destructive);
        Assert.Contains(hits, h => h.Member == "f.Save");
    }

    [Fact]
    public void Preview_lists_writing_and_destructive_members_with_their_tier()
    {
        var (_, hits) = EtabsTierGate.Inspect("sapModel.FrameObj.SetSection(\"F1\", \"C40x40\"); sapModel.Analyze.RunAnalysis(); return sapModel.GetModelFilename();");

        var preview = EtabsTierGate.Preview(hits, EtabsTier.Write);

        Assert.Equal(2, preview.Count);
        Assert.All(preview, d => Assert.Equal("PREVIEW", d.Id));
        Assert.Contains(preview, d => d.Message == "FrameObj.SetSection (W)");
        Assert.Contains(preview, d => d.Message == "Analyze.RunAnalysis (D)");
    }

    [Fact]
    public void Unknown_prefixes_default_to_write_and_path_parameter_names_are_pinned()
    {
        Assert.Equal(EtabsTier.Write, EtabsTierGate.Classify("Assign"));
        Assert.Equal(EtabsTier.Write, EtabsTierGate.Classify("ChangeName"));
        Assert.Equal(EtabsTier.Destructive, EtabsTierGate.Classify("ExportFile"));
        Assert.Equal(EtabsTier.ReadOnly, EtabsTierGate.Classify("GetNameList"));
        Assert.Contains("FileName", EtabsTierGate.PathParameterNames);
        Assert.Contains("csvFilePath", EtabsTierGate.PathParameterNames);
    }
}
