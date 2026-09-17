using System.Reflection;
using HPEtabs.McpBridge.Service;
using Xunit;

namespace HPEtabs.McpBridge.Tests;

/// <summary>
///     The tier fixture is complete and sane: every method of every installed ETABSv1 interface and every method
///     topic of the documentation index has a row, rows are unique with a valid tier, and the members the red
///     team named (writes that a prefix rule missed) sit where the rules put them.
/// </summary>
public sealed class EtabsTierFixtureTests
{
    [Fact]
    public void Every_method_of_every_installed_interface_has_a_row()
    {
        var table = EtabsTierTable.Embedded;
        var missing = new List<string>();

        foreach (var type in EtabsScriptEnvironment.Wrapper.GetTypes().Where(t => t.IsInterface && t.Namespace == EtabsTierAnalyzer.WrapperNamespace))
        foreach (var method in type.GetMethods().Where(m => !m.IsSpecialName))
        {
            if (!table.TryGet(type.Name, method.Name, out _)) missing.Add(type.Name + "." + method.Name);
        }

        Assert.True(missing.Count == 0, "not in the fixture: " + string.Join(", ", missing.Take(20)));
        Assert.InRange(table.Count, 1200, 1500);
    }

    [Fact]
    public void Every_documented_method_topic_has_a_row()
    {
        var table = EtabsTierTable.Embedded;
        var index = EtabsTierTable.EmbeddedIndex();
        var missing = index.Where(topic => !table.TryGet(topic[..topic.IndexOf('.')], topic[(topic.IndexOf('.') + 1)..], out _)).ToArray();

        Assert.InRange(index.Count, 1000, 1300);
        Assert.True(missing.Length == 0, "documented but not in the fixture: " + string.Join(", ", missing.Take(20)));
    }

    [Fact]
    public void Rows_are_unique_and_a_duplicate_or_bad_tier_is_rejected_on_load()
    {
        Assert.Throws<FormatException>(() => EtabsTierTable.Parse(["cX.A\tR", "cX.A\tW"]));
        Assert.Throws<FormatException>(() => EtabsTierTable.Parse(["cX.A\tQ"]));
        Assert.Throws<FormatException>(() => EtabsTierTable.Parse(["cX.A"]));

        var table = EtabsTierTable.Parse(["# comment", "", "cX.A\tD\tpath=0,2", "cX.B\tR"]);
        Assert.Equal(2, table.Count);
        Assert.True(table.TryGet("cX", "A", out var a));
        Assert.Equal(EtabsTier.Destructive, a.Tier);
        Assert.Equal([0, 2], a.PathParameters);
    }

    [Theory]
    [InlineData("cFile", "ExportFile", EtabsTier.Destructive)]
    [InlineData("cFile", "ImportFile", EtabsTier.Destructive)]
    [InlineData("cFile", "Save", EtabsTier.Destructive)]
    [InlineData("cFile", "OpenFile", EtabsTier.Destructive)]
    [InlineData("cFile", "NewBlank", EtabsTier.Destructive)]
    [InlineData("cDatabaseTables", "GetTableForDisplayCSVFile", EtabsTier.Destructive)]
    [InlineData("cDatabaseTables", "SetTableForEditingCSVFile", EtabsTier.Destructive)]
    [InlineData("cDatabaseTables", "ShowTablesInExcel", EtabsTier.Destructive)]
    [InlineData("cDatabaseTables", "ApplyEditedTables", EtabsTier.Destructive)]
    [InlineData("cDesignSteel", "StartDesign", EtabsTier.Destructive)]
    [InlineData("cDesignSteel", "ResetOverwrites", EtabsTier.Destructive)]
    [InlineData("cAnalyze", "ModifyUndeformedGeometry", EtabsTier.Destructive)]
    [InlineData("cAnalyze", "MergeAnalysisResults", EtabsTier.Destructive)]
    [InlineData("cAnalyze", "RunAnalysis", EtabsTier.Destructive)]
    [InlineData("cAnalyze", "DeleteResults", EtabsTier.Destructive)]
    [InlineData("cAnalyze", "CreateAnalysisModel", EtabsTier.Destructive)]
    [InlineData("cTower", "RenameTower", EtabsTier.Destructive)]
    [InlineData("cSapModel", "SetModelIsLocked", EtabsTier.Destructive)]
    [InlineData("cSapModel", "InitializeNewModel", EtabsTier.Destructive)]
    [InlineData("cFrameObj", "Delete", EtabsTier.Destructive)]
    [InlineData("cPropFrame", "ImportProp", EtabsTier.Destructive)]
    [InlineData("cOAPI", "ApplicationExit", EtabsTier.Destructive)]
    [InlineData("cHelper", "GetObject", EtabsTier.Destructive)]
    [InlineData("cEditGeneral", "Move", EtabsTier.Write)]
    [InlineData("cAnalyze", "SetRunCaseFlag", EtabsTier.Write)]
    [InlineData("cFrameObj", "SetSection", EtabsTier.Write)]
    [InlineData("cFrameObj", "AddByCoord", EtabsTier.Write)]
    [InlineData("cDatabaseTables", "SetTableForEditingArray", EtabsTier.Write)]
    [InlineData("cSapModel", "SetPresentUnits", EtabsTier.Write)]
    [InlineData("cSapModel", "GetModelFilename", EtabsTier.ReadOnly)]
    [InlineData("cPointObj", "GetNameList", EtabsTier.ReadOnly)]
    [InlineData("cDatabaseTables", "GetTableForDisplayArray", EtabsTier.ReadOnly)]
    [InlineData("cDatabaseTables", "GetTableForDisplayCSVString", EtabsTier.ReadOnly)]
    [InlineData("cView", "RefreshView", EtabsTier.ReadOnly)]
    [InlineData("cSelect", "ClearSelection", EtabsTier.ReadOnly)]
    [InlineData("cAnalysisResults", "FrameForce", EtabsTier.ReadOnly)]
    [InlineData("cAnalysisResultsSetup", "SetCaseSelectedForOutput", EtabsTier.ReadOnly)]
    [InlineData("cPropFrame", "GetAngle", EtabsTier.ReadOnly)]
    [InlineData("cDesignSteel", "VerifyPassed", EtabsTier.ReadOnly)]
    [InlineData("cOAPI", "Visible", EtabsTier.ReadOnly)]
    public void Members_the_red_team_and_the_rules_named_sit_in_their_tier(string interfaceName, string member, EtabsTier expected)
    {
        Assert.True(EtabsTierTable.Embedded.TryGet(interfaceName, member, out var entry), interfaceName + "." + member + " missing");
        Assert.Equal(expected, entry.Tier);
    }

    [Theory]
    [InlineData("cFile", "Save", 0)]
    [InlineData("cFile", "OpenFile", 0)]
    [InlineData("cFile", "ExportFile", 0)]
    [InlineData("cDatabaseTables", "GetTableForDisplayCSVFile", 4)]
    [InlineData("cDatabaseTables", "SetTableForEditingCSVFile", 2)]
    [InlineData("cPropFrame", "ImportProp", 2)]
    [InlineData("cHelper", "CreateObject", 0)]
    public void Path_taking_members_mark_the_by_value_path_parameter(string interfaceName, string member, int parameter)
    {
        Assert.True(EtabsTierTable.Embedded.TryGet(interfaceName, member, out var entry));
        Assert.Equal([parameter], entry.PathParameters);

        // The flag names a by-value string parameter of the real method — a `ref FileName` output never counts.
        var method = EtabsScriptEnvironment.Wrapper.GetType(EtabsTierAnalyzer.WrapperNamespace + "." + interfaceName)!.GetMethod(member)!;
        var parameterInfo = method.GetParameters()[parameter];
        Assert.False(parameterInfo.ParameterType.IsByRef);
        Assert.Equal(typeof(string), parameterInfo.ParameterType);
    }

    [Fact]
    public void Outputs_named_like_paths_do_not_make_a_read_destructive()
    {
        // cPropFrame.GetAngle(Name, ref FileName, …): the wrapper reports the section's source file, the call writes nothing.
        Assert.True(EtabsTierTable.Embedded.TryGet("cPropFrame", "GetAngle", out var entry));
        Assert.Equal(EtabsTier.ReadOnly, entry.Tier);
        Assert.Empty(entry.PathParameters);
        Assert.Contains(EtabsScriptEnvironment.Wrapper.GetType("ETABSv1.cPropFrame")!.GetMethod("GetAngle")!.GetParameters(), p => p.Name == "FileName" && p.ParameterType.IsByRef);
    }
}
