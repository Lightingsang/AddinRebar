namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Text;
using System.Text.Json;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class Tier5_RoslynAstMutationTests
{
    [Fact]
    public void TC_T5_AST_01_LineCount_Boundary300And301()
    {
        var sb300 = new StringBuilder();
        for (int i = 0; i < 300; i++) sb300.AppendLine($"int v{i} = {i};");
        var code300 = sb300.ToString();

        var findings300 = ScriptQuality.Find(code300);
        Assert.DoesNotContain(findings300, f => f.RuleId == "Q-B3");
        Assert.Equal(300, ScriptQuality.LineCount(code300));

        var sb301 = new StringBuilder();
        for (int i = 0; i < 301; i++) sb301.AppendLine($"int v{i} = {i};");
        var code301 = sb301.ToString();

        var findings301 = ScriptQuality.Find(code301);
        Assert.Contains(findings301, f => f.RuleId == "Q-B3" && f.Severity == ScriptQuality.Error);
        Assert.Equal(301, ScriptQuality.LineCount(code301));
    }

    [Fact]
    public void TC_T5_AST_02_EmptyCatch_WithAndWithoutReasonComment()
    {
        var codeBad = "try { int x = 0; } catch { }";
        var findingsBad = ScriptQuality.Find(codeBad);
        Assert.Contains(findingsBad, f => f.RuleId == "Q-B2" && f.Severity == ScriptQuality.Error);

        var codeGood = "try { int x = 0; } catch { /* expected parameter fallback */ }";
        var findingsGood = ScriptQuality.Find(codeGood);
        Assert.DoesNotContain(findingsGood, f => f.RuleId == "Q-B2");
    }

    [Fact]
    public void TC_T5_AST_03_CommentedOutCode_Detection()
    {
        var codeBad = "// var x = 10;\nint y = 20;";
        var findingsBad = ScriptQuality.Find(codeBad);
        Assert.Contains(findingsBad, f => f.RuleId == "Q-B1" && f.Severity == ScriptQuality.Error);

        var codeGood = "// calculate span deflection based on Eurocode 2\nint y = 20;";
        var findingsGood = ScriptQuality.Find(codeGood);
        Assert.DoesNotContain(findingsGood, f => f.RuleId == "Q-B1");
    }

    [Fact]
    public void TC_T5_AST_04_SyntaxMutations_NoCompilerCrash()
    {
        var mutations = new[]
        {
            "string s = \"unclosed;",
            "int a = 10",
            "void Test() { { int x = 1; }",
            "public void (int a) {"
        };

        foreach (var mutation in mutations)
        {
            var guard = ScriptGuard.Check(mutation, GuardProfile.Grasshopper);
            var analysis = ScriptAnalyzer.Analyze(mutation, AnalyzerProfile.Grasshopper);
            Assert.NotNull(guard);
            Assert.NotNull(analysis);
        }
    }

    [Fact]
    public void TC_T5_AST_05_DeepBinaryExpression_StackOverflowDefense()
    {
        var sb = new StringBuilder("int sum = 1");
        for (int i = 0; i < 200; i++) sb.Append(" + 1");
        sb.Append(';');
        var deepCode = sb.ToString();

        var guard = ScriptGuard.Check(deepCode, GuardProfile.Grasshopper);
        var analysis = ScriptAnalyzer.Analyze(deepCode, AnalyzerProfile.Grasshopper);

        Assert.Empty(guard);
        Assert.Equal(1, analysis.LineCount);
    }

    [Fact]
    public void TC_T5_AST_06_ToolValidator_ReservedNames_Rejected()
    {
        foreach (var coreTool in HostProfile.Grasshopper.CoreToolNames)
        {
            Assert.True(ToolValidator.IsReserved(coreTool, HostProfile.Grasshopper));
            var candidate = new ToolRecord { Name = coreTool, Category = "Geometry", Description = "A reserved candidate tool.", Code = "return 1;" };
            var report = ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper);
            Assert.False(report.IsValid);
            Assert.Contains(report.Errors, e => e == $"name '{coreTool}' is reserved by the server.");
        }

        Assert.False(ToolValidator.IsReserved("parametric_facade_fin", HostProfile.Grasshopper));
    }

    [Fact]
    public void TC_T5_AST_07_ToolValidator_InvalidTransactionMode()
    {
        var invalidModes = new[] { "semi_auto", "destructive", "any", "invalid" };
        foreach (var inv in invalidModes)
        {
            Assert.Null(TransactionModes.Normalize(inv));
            var candidate = new ToolRecord { Name = "test_tool", Category = "Geometry", Description = "Testing invalid transaction mode.", Transaction = inv, Code = "return 1;" };
            var report = ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper);
            Assert.Contains(report.Errors, e => e == "transaction must be auto, manual or none.");
        }

        Assert.Equal("auto", TransactionModes.Normalize("auto"));
        Assert.Equal("auto", TransactionModes.Normalize(""));
        Assert.Equal("manual", TransactionModes.Normalize("manual"));
        Assert.Equal("none", TransactionModes.Normalize("none"));
    }

    [Fact]
    public void TC_T5_AST_08_DynamicToolRegistrar_MutatingTool_InjectsDryRun()
    {
        var schemaJson = """{"type":"object","properties":{"scale":{"type":"number"}}}""";
        var mutatingRecord = new ToolRecord
        {
            Name = "mutate_geometry",
            Category = "Geometry",
            Description = "Mutates Revit elements in document.",
            Transaction = "auto",
            InputSchema = JsonSerializer.Deserialize<JsonElement>(schemaJson)
        };

        var schemaElement = RegistryToolFunction.BuildSchema(mutatingRecord);
        Assert.True(schemaElement.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("dryRun", out var dryRunProp));
        Assert.Equal("boolean", dryRunProp.GetProperty("type").GetString());
    }

    [Fact]
    public void TC_T5_AST_09_DynamicToolRegistrar_ReadOnlyTool_OmitsDryRun()
    {
        var schemaJson = """{"type":"object","properties":{"scale":{"type":"number"}}}""";
        var readOnlyRecord = new ToolRecord
        {
            Name = "query_geometry",
            Category = "Geometry",
            Description = "Queries elements without modifications.",
            Transaction = "none",
            InputSchema = JsonSerializer.Deserialize<JsonElement>(schemaJson)
        };

        var schemaElement = RegistryToolFunction.BuildSchema(readOnlyRecord);
        Assert.True(schemaElement.TryGetProperty("properties", out var properties));
        Assert.False(properties.TryGetProperty("dryRun", out _));
    }

    [Fact]
    public void TC_T5_AST_10_CompilerDirectives_LoadAndReference_Blocked()
    {
        var codeWithRef = "#r \"system.dll\"\nreturn 1;";
        var codeWithLoad = "#load \"file.cs\"\nreturn 1;";

        var diagnosticsRef = ScriptGuard.Check(codeWithRef, GuardProfile.Grasshopper);
        var diagnosticsLoad = ScriptGuard.Check(codeWithLoad, GuardProfile.Grasshopper);

        Assert.NotEmpty(diagnosticsRef);
        Assert.Equal(ScriptGuard.DiagnosticId, diagnosticsRef[0].Id);
        Assert.Contains("#r", diagnosticsRef[0].Message);

        Assert.NotEmpty(diagnosticsLoad);
        Assert.Equal(ScriptGuard.DiagnosticId, diagnosticsLoad[0].Id);
        Assert.Contains("#load", diagnosticsLoad[0].Message);
    }
}
