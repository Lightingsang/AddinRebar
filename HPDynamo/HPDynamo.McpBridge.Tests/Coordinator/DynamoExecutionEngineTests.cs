namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HPDynamo.McpBridge.Coordinator;
using HPDynamo.McpBridge.Support;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class DynamoExecutionEngineTests
{
    [Theory]
    [InlineData("doc.Create.NewFamilyInstance(xyz, sym, level, StructuralType.NonStructural);", "auto")]
    [InlineData("var param = wall.LookupParameter(\"Comments\"); param.Set(\"Updated\");", "auto")]
    [InlineData("doc.Delete(elementId);", "auto")]
    [InlineData("doc.Create.NewWall(curve, level, false);", null)]
    public void ShouldStartAutoTransaction_DirectRevitDb_ReturnsTrue(string code, string? mode)
    {
        bool startAuto = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.True(startAuto);
    }

    [Theory]
    [InlineData("var wall = Revit.Elements.Wall.ByCurveAndHeight(c, 3000, level);", "auto")]
    [InlineData("Revit.Elements.Element.SetParameterByName(elem, \"Mark\", \"W1\");", "auto")]
    public void ShouldStartAutoTransaction_DynamoWrappers_ReturnsFalse(string code, string? mode)
    {
        bool startAuto = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.False(startAuto);
    }

    [Theory]
    [InlineData("TransactionManager.Instance.EnsureInTransaction(doc);", "auto")]
    [InlineData("Dynamo.Applications.DynamoRevit.RevitDynamoModel.ToString();", "auto")]
    public void ShouldStartAutoTransaction_DynamoTransactionManager_ReturnsFalse(string code, string? mode)
    {
        bool startAuto = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.False(startAuto);
    }

    [Theory]
    [InlineData("homeWs.Run();", "auto")]
    [InlineData("ws.Run();", "auto")]
    [InlineData("activeCanvasWs.Run();", "auto")]
    public void ShouldStartAutoTransaction_WorkspaceRun_ReturnsFalse(string code, string? mode)
    {
        bool startAuto = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.False(startAuto);
    }

    [Theory]
    [InlineData("using var tx = new Transaction(doc); tx.Start(); tx.Commit();", "auto")]
    [InlineData("var tx = new Autodesk.Revit.DB.Transaction(doc);", "auto")]
    public void ShouldStartAutoTransaction_ManualTransactionInCode_ReturnsFalse(string code, string? mode)
    {
        bool startAuto = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.False(startAuto);
    }

    [Theory]
    [InlineData("doc.Create.NewFamilyInstance(xyz, sym, level, StructuralType.NonStructural);", "manual")]
    [InlineData("doc.Create.NewFamilyInstance(xyz, sym, level, StructuralType.NonStructural);", "none")]
    public void ShouldStartAutoTransaction_ManualOrNoneMode_ReturnsFalse(string code, string mode)
    {
        bool startAuto = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.False(startAuto);
    }

    [Fact]
    public void MergeDryRun_WhenArgsNull_AddsDryRunTrue()
    {
        var element = DynamoExecutionEngine.MergeDryRun(null, true);
        var args = new ScriptArgs(element);

        Assert.True(args.Bool("dryRun", false));
    }

    [Fact]
    public void MergeDryRun_WhenArgsPreservesExistingProperties()
    {
        using var doc = JsonDocument.Parse("{\"nodeId\": \"1234\", \"name\": \"TestNode\"}");
        var element = DynamoExecutionEngine.MergeDryRun(doc.RootElement, true);
        var args = new ScriptArgs(element);

        Assert.Equal("1234", args.Str("nodeId"));
        Assert.Equal("TestNode", args.Str("name"));
        Assert.True(args.Bool("dryRun", false));
    }

    [Fact]
    public void MergeDryRun_WhenDryRunIsFalse_SetsFalse()
    {
        using var doc = JsonDocument.Parse("{\"name\": \"TestNode\"}");
        var element = DynamoExecutionEngine.MergeDryRun(doc.RootElement, false);
        var args = new ScriptArgs(element);

        Assert.False(args.Bool("dryRun", true));
    }

    [Fact]
    public void DynamoScriptGlobals_HasDryRunAndGlobalsProperties()
    {
        var dryRunProp = typeof(DynamoScriptGlobals).GetProperty("dryRun");
        Assert.NotNull(dryRunProp);
        Assert.Equal(typeof(bool), dryRunProp.PropertyType);

        var globalsProp = typeof(DynamoScriptGlobals).GetProperty("globals");
        Assert.NotNull(globalsProp);
        Assert.Equal(typeof(DynamoScriptGlobals), globalsProp.PropertyType);
    }
}
