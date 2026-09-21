using System.Reflection;
using HPSap2000.McpBridge.Service;
using Xunit;

namespace HPSap2000.McpBridge.Tests;

/// <summary>
///     The tier fixture is complete and sane: every method of every installed SAP2000v1 interface has a row,
///     rows are unique with a valid tier, and members sit where the classification rules put them.
/// </summary>
public sealed class SapTierFixtureTests
{
    [Fact]
    public void Every_method_of_every_installed_interface_has_a_row()
    {
        var table = SapTierTable.Embedded;
        var missing = new List<string>();

        foreach (var type in SapScriptEnvironment.Wrapper.GetTypes().Where(t => t.IsInterface && t.Namespace == SapTierAnalyzer.WrapperNamespace))
        foreach (var method in type.GetMethods().Where(m => !m.IsSpecialName))
        {
            if (!table.TryGet(type.Name, method.Name, out _)) missing.Add(type.Name + "." + method.Name);
        }

        Assert.True(missing.Count == 0, "not in the fixture: " + string.Join(", ", missing.Take(20)));
        Assert.InRange(table.Count, 1800, 2500);
    }

    [Fact]
    public void Rows_are_unique_and_a_duplicate_or_bad_tier_is_rejected_on_load()
    {
        Assert.Throws<FormatException>(() => SapTierTable.Parse(["cX.A\tR", "cX.A\tW"]));
        Assert.Throws<FormatException>(() => SapTierTable.Parse(["cX.A\tQ"]));
        Assert.Throws<FormatException>(() => SapTierTable.Parse(["cX.A"]));

        var table = SapTierTable.Parse(["# comment", "", "cX.A\tD\tpath=0,2", "cX.B\tR"]);
        Assert.Equal(2, table.Count);
        Assert.True(table.TryGet("cX", "A", out var a));
        Assert.Equal(SapTier.Destructive, a.Tier);
        Assert.Equal([0, 2], a.PathParameters);
    }

    [Theory]
    [InlineData("cDatabaseTables", "GetTableForDisplayCSVFile", SapTier.Destructive)]
    [InlineData("cDatabaseTables", "SetTableForEditingCSVFile", SapTier.Destructive)]
    [InlineData("cFile", "Save", SapTier.Destructive)]
    [InlineData("cFile", "OpenFile", SapTier.Destructive)]
    [InlineData("cFile", "NewBlank", SapTier.Destructive)]
    [InlineData("cFile", "New2DFrame", SapTier.Destructive)]
    [InlineData("cFile", "New3DFrame", SapTier.Destructive)]
    [InlineData("cAnalyze", "ModifyUndeformedGeometry", SapTier.Destructive)]
    [InlineData("cAnalyze", "RunAnalysis", SapTier.Destructive)]
    [InlineData("cAnalyze", "DeleteResults", SapTier.Destructive)]
    [InlineData("cAnalyze", "CreateAnalysisModel", SapTier.Destructive)]
    [InlineData("cSapModel", "SetModelIsLocked", SapTier.Destructive)]
    [InlineData("cSapModel", "InitializeNewModel", SapTier.Destructive)]
    [InlineData("cFrameObj", "Delete", SapTier.Destructive)]
    [InlineData("cPropFrame", "ImportProp", SapTier.Destructive)]
    [InlineData("cOAPI", "ApplicationExit", SapTier.Destructive)]
    [InlineData("cHelper", "GetObject", SapTier.Destructive)]
    [InlineData("cEditGeneral", "Move", SapTier.Write)]
    [InlineData("cAnalyze", "SetRunCaseFlag", SapTier.Write)]
    [InlineData("cFrameObj", "SetSection", SapTier.Write)]
    [InlineData("cFrameObj", "AddByCoord", SapTier.Write)]
    [InlineData("cSapModel", "SetPresentUnits", SapTier.Write)]
    [InlineData("cSapModel", "GetModelFilename", SapTier.ReadOnly)]
    [InlineData("cPointObj", "GetNameList", SapTier.ReadOnly)]
    [InlineData("cView", "RefreshView", SapTier.ReadOnly)]
    [InlineData("cSelect", "ClearSelection", SapTier.ReadOnly)]
    [InlineData("cAnalysisResults", "FrameForce", SapTier.ReadOnly)]
    [InlineData("cAnalysisResultsSetup", "SetCaseSelectedForOutput", SapTier.ReadOnly)]
    [InlineData("cPropFrame", "GetAngle", SapTier.ReadOnly)]
    [InlineData("cOAPI", "Visible", SapTier.ReadOnly)]
    public void Members_sit_in_their_expected_tier(string interfaceName, string member, SapTier expected)
    {
        Assert.True(SapTierTable.Embedded.TryGet(interfaceName, member, out var entry), interfaceName + "." + member + " missing");
        Assert.Equal(expected, entry.Tier);
    }

    [Theory]
    [InlineData("cFile", "Save", 0)]
    [InlineData("cFile", "OpenFile", 0)]
    [InlineData("cDatabaseTables", "SetTableForEditingCSVFile", 2)]
    [InlineData("cPropFrame", "ImportProp", 2)]
    [InlineData("cHelper", "CreateObject", 0)]
    public void Path_taking_members_mark_the_by_value_path_parameter(string interfaceName, string member, int parameter)
    {
        Assert.True(SapTierTable.Embedded.TryGet(interfaceName, member, out var entry));
        Assert.Equal([parameter], entry.PathParameters);

        var method = SapScriptEnvironment.Wrapper.GetType(SapTierAnalyzer.WrapperNamespace + "." + interfaceName)!.GetMethod(member)!;
        var parameterInfo = method.GetParameters()[parameter];
        Assert.False(parameterInfo.ParameterType.IsByRef);
        Assert.Equal(typeof(string), parameterInfo.ParameterType);
    }

    [Fact]
    public void Outputs_named_like_paths_do_not_make_a_read_destructive()
    {
        // cPropFrame.GetAngle(Name, ref FileName, …): the wrapper reports the section's source file, the call writes nothing.
        Assert.True(SapTierTable.Embedded.TryGet("cPropFrame", "GetAngle", out var entry));
        Assert.Equal(SapTier.ReadOnly, entry.Tier);
        Assert.Empty(entry.PathParameters);
        Assert.Contains(SapScriptEnvironment.Wrapper.GetType("SAP2000v1.cPropFrame")!.GetMethod("GetAngle")!.GetParameters(), p => p.Name == "FileName" && p.ParameterType.IsByRef);
    }
}
