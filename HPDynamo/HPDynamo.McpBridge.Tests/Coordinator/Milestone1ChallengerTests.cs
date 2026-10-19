namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Text.Json;
using HPDynamo.McpBridge.Coordinator;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

/// <summary>
///     Adversarial stress-test harness for Milestone 1 (RV-01 & RV-06).
///     Empirically stress-tests ShouldStartAutoTransaction and MergeDryRun edge cases.
/// </summary>
public sealed class Milestone1ChallengerTests
{
    // =========================================================================
    // 1. ShouldStartAutoTransaction — Edge Cases
    // =========================================================================

    [Theory]
    [InlineData("", "auto", true)]
    [InlineData("   ", "auto", true)]
    [InlineData("\r\n\t  \t\r\n", "auto", true)]
    [InlineData("", null, true)]
    [InlineData("", "", true)]
    [InlineData("", "manual", false)]
    [InlineData("", "none", false)]
    [InlineData("   ", "MANUAL", false)]
    public void ShouldStartAutoTransaction_EmptyAndWhitespace_HandledSafely(string code, string? mode, bool expected)
    {
        bool actual = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("// new Transaction(doc);\r\ndoc.Create.NewFamilyInstance(xyz);", "auto", true)]
    [InlineData("/* new Transaction(doc); */ doc.Create.NewWall(c, l, false);", "auto", true)]
    [InlineData("// Revit.Elements.Wall.ByCurveAndHeight(c, 100, l);\ndoc.Delete(id);", "auto", true)]
    [InlineData("/* Revit.Elements.Element */ doc.Create.NewFamilyInstance(xyz);", "auto", true)]
    [InlineData("// TransactionManager.Instance\ndoc.Create.NewFamilyInstance(xyz);", "auto", true)]
    [InlineData("// Safe comment without keywords\ndoc.Create.NewFamilyInstance(xyz);", "auto", true)]
    public void ShouldStartAutoTransaction_CommentsContainingKeywords_AllowsAutoTx(string code, string? mode, bool expected)
    {
        // Roslyn AST syntax parsing ignores comments and trivia, allowing auto-tx when keywords only appear in comments.
        bool actual = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("var log = \"Revit.Elements.Wall\"; doc.Create.NewWall(c, l, false);", "auto", true)]
    [InlineData("string msg = \"new Transaction(doc)\"; doc.Delete(id);", "auto", true)]
    [InlineData("string msg = \"TransactionManager\"; doc.Delete(id);", "auto", true)]
    public void ShouldStartAutoTransaction_StringLiteralsWithKeywords_AllowsAutoTx(string code, string? mode, bool expected)
    {
        // Roslyn AST syntax parsing ignores string literals, allowing auto-tx when keywords only appear in literals.
        bool actual = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("doc.Create.NewWall(c, l, false);\r\nRevit.Elements.Wall.ByCurveAndHeight(c, 3000, l);", "auto", false)]
    [InlineData("doc.Create.NewWall(c, l, false);\r\nTransactionManager.Instance.EnsureInTransaction(doc);", "auto", false)]
    [InlineData("doc.Create.NewWall(c, l, false);\r\nDynamo.Applications.DynamoRevit.RevitDynamoModel.ToString();", "auto", false)]
    [InlineData("doc.Create.NewWall(c, l, false);\r\nhomeWs.Run();", "auto", false)]
    [InlineData("doc.Create.NewWall(c, l, false);\r\nws.Run();", "auto", false)]
    [InlineData("doc.Create.NewWall(c, l, false);\r\nactiveCanvasWs.Run();", "auto", false)]
    [InlineData("doc.Create.NewWall(c, l, false);\r\nnew Transaction(doc);", "auto", false)]
    [InlineData("doc.Create.NewWall(c, l, false);\r\nnew Autodesk.Revit.DB.Transaction(doc);", "auto", false)]
    public void ShouldStartAutoTransaction_MixedKeywords_DynamoWrappersTakePrecedence(string code, string? mode, bool expected)
    {
        // When mixed DB and Dynamo/manual calls coexist, Dynamo wrapper/manual transaction takes precedence
        bool actual = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("doc.Create.NewWall(c, l, false);", "  AUTO  ", true)]
    [InlineData("doc.Create.NewWall(c, l, false);", "auto\t\r\n", true)]
    [InlineData("doc.Create.NewWall(c, l, false);", "AuTo", true)]
    [InlineData("doc.Create.NewWall(c, l, false);", "UNKNOWN_MODE", true)]
    public void ShouldStartAutoTransaction_ModeNormalizationAndFallback(string code, string? mode, bool expected)
    {
        bool actual = DynamoExecutionEngine.ShouldStartAutoTransaction(code, mode);
        Assert.Equal(expected, actual);
    }

    // =========================================================================
    // 2. MergeDryRun — Edge Cases
    // =========================================================================

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MergeDryRun_NullArgs_ReturnsValidObjectWithDryRun(bool dryRun)
    {
        var element = DynamoExecutionEngine.MergeDryRun(null, dryRun);
        var args = new ScriptArgs(element);

        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.Equal(dryRun, args.Bool("dryRun", !dryRun));
    }

    [Fact]
    public void MergeDryRun_EmptyJsonObject_AppendsDryRun()
    {
        using var doc = JsonDocument.Parse("{}");
        var element = DynamoExecutionEngine.MergeDryRun(doc.RootElement, true);
        var args = new ScriptArgs(element);

        Assert.True(args.Bool("dryRun", false));
    }

    [Theory]
    [InlineData("\"a plain string value\"", true)]
    [InlineData("123456", true)]
    [InlineData("3.14159", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("[1, 2, 3]", true)]
    [InlineData("[{\"id\": 1}]", false)]
    [InlineData("null", true)]
    public void MergeDryRun_NonObjectJsonPrimitivesAndArrays_NeverThrowsAndEmitsDryRun(string json, bool dryRun)
    {
        using var doc = JsonDocument.Parse(json);
        var element = DynamoExecutionEngine.MergeDryRun(doc.RootElement, dryRun);
        var args = new ScriptArgs(element);

        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.Equal(dryRun, args.Bool("dryRun", !dryRun));
    }

    [Fact]
    public void MergeDryRun_DefaultJsonElement_NeverThrows()
    {
        JsonElement defaultElement = default;
        var element = DynamoExecutionEngine.MergeDryRun(defaultElement, true);
        var args = new ScriptArgs(element);

        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.True(args.Bool("dryRun", false));
    }

    [Fact]
    public void MergeDryRun_ExistingDryRunFalse_OverwritesWithTrue()
    {
        using var doc = JsonDocument.Parse("{\"dryRun\": false, \"label\": \"test\"}");
        var element = DynamoExecutionEngine.MergeDryRun(doc.RootElement, true);
        var args = new ScriptArgs(element);

        Assert.True(args.Bool("dryRun", false));
        Assert.Equal("test", args.Str("label"));
    }

    [Theory]
    [InlineData("{\"dryrun\": false}", true)]
    [InlineData("{\"DRYRUN\": false}", true)]
    [InlineData("{\"DryRun\": false}", true)]
    [InlineData("{\"dryRun\": \"invalidBool\"}", true)]
    public void MergeDryRun_CasingAndTypeVariations_CaseInsensitiveOverwrite(string json, bool expectedDryRun)
    {
        using var doc = JsonDocument.Parse(json);
        var element = DynamoExecutionEngine.MergeDryRun(doc.RootElement, expectedDryRun);
        var args = new ScriptArgs(element);

        Assert.Equal(expectedDryRun, args.Bool("dryRun", false));
    }

    [Fact]
    public void MergeDryRun_ComplexNestedJson_PreservesAllStructures()
    {
        const string json = """
        {
            "name": "CompositeNode",
            "count": 42,
            "nested": {
                "subKey": "subVal",
                "array": [10, 20, 30]
            },
            "nullField": null
        }
        """;
        using var doc = JsonDocument.Parse(json);
        var element = DynamoExecutionEngine.MergeDryRun(doc.RootElement, true);
        var args = new ScriptArgs(element);

        Assert.Equal("CompositeNode", args.Str("name"));
        Assert.Equal(42, args.Int("count"));
        Assert.True(args.Bool("dryRun", false));

        Assert.True(element.TryGetProperty("nested", out var nestedProp));
        Assert.Equal("subVal", nestedProp.GetProperty("subKey").GetString());
        Assert.Equal(3, nestedProp.GetProperty("array").GetArrayLength());
        Assert.True(element.TryGetProperty("nullField", out var nullProp));
        Assert.Equal(JsonValueKind.Null, nullProp.ValueKind);
    }
}
