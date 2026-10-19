namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HPDynamo.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

/// <summary>
/// Tier 5: Roslyn Compilation & AST Mutation Stress Testing.
/// Evaluates compiler responses to syntax/semantic edge cases, Q-B1/Q-B2/Q-B3 rules, and ToolValidator invariants.
/// </summary>
public sealed class Tier5_RoslynAstMutationTests
{
    // =========================================================================
    // 1. AST Rule Testing (Q-B1, Q-B2, Q-B3)
    // =========================================================================

    [Fact]
    public void TC_T5_AST_01_LineCount_Boundary300And301_EnforcedByQualityCheck()
    {
        var bridge = new DynamoE2EFakeBridge();

        // 300 lines passes
        string code300 = string.Join("\n", Enumerable.Repeat("int x = 1;", 300));
        var res300 = bridge.ValidateScript(code300);
        Assert.True(res300.IsValid);

        // 301 lines fails
        string code301 = string.Join("\n", Enumerable.Repeat("int x = 1;", 301));
        var res301 = bridge.ValidateScript(code301);
        Assert.False(res301.IsValid);
        Assert.Contains(res301.Errors, e => e.Contains("exceeds 300 lines"));
    }

    [Theory]
    [InlineData("try { int a = 1; } catch { }", false)]
    [InlineData("try { int a = 1; } catch (Exception) { }", false)]
    [InlineData("try { int a = 1; } catch { /* expected error */ }", true)]
    [InlineData("try { int a = 1; } catch (Exception ex) { log(ex.Message); }", true)]
    public void TC_T5_AST_02_EmptyCatch_WithAndWithoutReasonComment_HandledCorrectly(string script, bool expectedValid)
    {
        var bridge = new DynamoE2EFakeBridge();
        var res = bridge.ValidateScript(script);
        Assert.Equal(expectedValid, res.IsValid);
    }

    [Theory]
    [InlineData("// var node = workspace.Nodes[0];\nreturn 1;", false)]
    [InlineData("// int count = 5;\nreturn 1;", false)]
    [InlineData("// This is an explanatory comment calculating volume\nreturn 1;", true)]
    public void TC_T5_AST_03_CommentedOutCode_Detection_DifferentiatesRealComments(string script, bool expectedValid)
    {
        var bridge = new DynamoE2EFakeBridge();
        var res = bridge.ValidateScript(script);
        Assert.Equal(expectedValid, res.IsValid);
    }

    // =========================================================================
    // 2. Roslyn Compiler Robustness under Syntax Mutations
    // =========================================================================

    [Theory]
    [InlineData("int x = \"unclosed string;")]
    [InlineData("if (x > 0 { return 1; }")]
    [InlineData("return 1 +++ 2;")]
    public void TC_T5_AST_04_SyntaxMutations_ProducesDiagnosticsWithoutCompilerCrash(string invalidCode)
    {
        var tree = CSharpSyntaxTree.ParseText(invalidCode, new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script));
        var compilation = CSharpCompilation.Create("mutation_test", [tree]);
        var diagnostics = compilation.GetDiagnostics();

        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void TC_T5_AST_05_DeepBinaryExpression_ParsedWithoutStackOverflow()
    {
        // 1,000 chained binary addition expressions
        string chainedExpr = string.Join(" + ", Enumerable.Repeat("1", 1000)) + ";";
        var tree = CSharpSyntaxTree.ParseText(chainedExpr, new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script));

        var root = tree.GetCompilationUnitRoot();
        Assert.NotNull(root);
        Assert.DoesNotContain(tree.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
    }

    // =========================================================================
    // 3. ToolValidator & DynamicToolRegistrar Invariants
    // =========================================================================

    [Theory]
    [InlineData("execute_dynamo_code")]
    [InlineData("execute_designscript")]
    [InlineData("get_dynamo_context")]
    [InlineData("cancel_execution")]
    [InlineData("propose_tool")]
    [InlineData("publish_tool")]
    public void TC_T5_REG_01_ToolValidator_ReservedNames_Rejected(string reservedName)
    {
        var candidate = CreateSampleRecord(reservedName, "auto");
        var report = ToolValidator.Validate(candidate, null, Array.Empty<ToolRecord>(), false, HostProfile.Dynamo);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e.Contains("is reserved by the server"));
    }

    [Fact]
    public void TC_T5_REG_02_ToolValidator_InvalidTransactionMode_Rejected()
    {
        var candidate = CreateSampleRecord("custom_sample_tool", "invalid_mode");
        var report = ToolValidator.Validate(candidate, null, Array.Empty<ToolRecord>(), false, HostProfile.Dynamo);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e.Contains("transaction must be auto, manual or none"));
    }

    [Fact]
    public void TC_T5_REG_03_DynamicToolRegistrar_BuildSchema_WritingTool_InjectsDryRun()
    {
        var record = CreateSampleRecord("mutating_wire_tool", "auto");
        var schema = RegistryToolFunction.BuildSchema(record);

        using var doc = JsonDocument.Parse(schema.GetRawText());
        var props = doc.RootElement.GetProperty("properties");
        Assert.True(props.TryGetProperty("dryRun", out var dryRunProp));
        Assert.Equal("boolean", dryRunProp.GetProperty("type").GetString());
    }

    [Fact]
    public void TC_T5_REG_04_DynamicToolRegistrar_BuildSchema_ReadOnlyTool_OmitsDryRun()
    {
        var record = CreateSampleRecord("readonly_inspect_tool", "none");
        var schema = RegistryToolFunction.BuildSchema(record);

        using var doc = JsonDocument.Parse(schema.GetRawText());
        var props = doc.RootElement.GetProperty("properties");
        Assert.False(props.TryGetProperty("dryRun", out _));
    }

    private static ToolRecord CreateSampleRecord(string name, string transaction)
    {
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "nodeId": { "type": "string" }
                },
                "required": ["nodeId"]
            }
            """).RootElement;

        return new ToolRecord
        {
            Name = name,
            Version = 1,
            Status = ToolStatus.Draft,
            Description = "A valid tool description of sufficient length for test purposes.",
            Category = "Graph",
            Transaction = transaction,
            TimeoutSeconds = 30,
            Code = "return args.Require(\"nodeId\");",
            InputSchema = schema,
            Examples = [new ToolExample { Title = "Sample run", Args = JsonDocument.Parse("{\"nodeId\":\"123\"}").RootElement }],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Author = "tester",
            Checksum = "hash123",
            Host = "dynamo"
        };
    }
}
