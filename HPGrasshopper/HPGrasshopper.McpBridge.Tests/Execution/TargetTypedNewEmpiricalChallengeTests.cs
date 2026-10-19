namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using ScriptAnalyzer = HPGrasshopper.McpBridge.Execution.ScriptAnalyzer;
using Xunit;

/// <summary>
///     Empirical adversarial challenge suite for Milestone M2:
///     Validates target-typed new AST detection under transaction mode "none"
///     across diverse C# syntax variations, and ensures zero false positives.
/// </summary>
public sealed class TargetTypedNewEmpiricalChallengeTests
{
    private static readonly AnalyzerProfile Profile = AnalyzerProfile.Grasshopper;

    [Theory]
    [InlineData("SubTransaction s = new(doc);")]
    [InlineData("Transaction t = new(doc, \"Adversarial\");")]
    [InlineData("TransactionGroup tg = new(doc, \"Adversarial\");")]
    [InlineData("SubTransaction s1 = new(doc), s2 = new(doc);")]
    public void LocalVariable_TargetTypedNew_DetectedAsTransaction(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect transaction in: {code}");
    }

    [Theory]
    [InlineData("using SubTransaction s = new(doc);")]
    [InlineData("using Transaction t = new(doc, \"Adversarial\");")]
    [InlineData("using TransactionGroup tg = new(doc, \"Adversarial\");")]
    [InlineData("using (SubTransaction s = new(doc)) { }")]
    [InlineData("using (Transaction t = new(doc, \"Adversarial\")) { }")]
    [InlineData("using (TransactionGroup tg = new(doc, \"Adversarial\")) { }")]
    public void UsingStatements_TargetTypedNew_DetectedAsTransaction(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect transaction in using statement: {code}");
    }

    [Theory]
    [InlineData("Autodesk.Revit.DB.SubTransaction s = new(doc);")]
    [InlineData("Autodesk.Revit.DB.Transaction t = new(doc, \"Adversarial\");")]
    [InlineData("Autodesk.Revit.DB.TransactionGroup tg = new(doc, \"Adversarial\");")]
    [InlineData("using Autodesk.Revit.DB.SubTransaction s = new(doc);")]
    [InlineData("using (Autodesk.Revit.DB.SubTransaction s = new(doc)) { }")]
    [InlineData("using (Autodesk.Revit.DB.Transaction t = new(doc, \"Adversarial\")) { }")]
    [InlineData("global::Autodesk.Revit.DB.Transaction t = new(doc, \"Adversarial\");")]
    [InlineData("global::Autodesk.Revit.DB.SubTransaction s = new(doc);")]
    public void FullyQualifiedNames_TargetTypedNew_DetectedAsTransaction(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect transaction in qualified name: {code}");
    }

    [Theory]
    [InlineData("var a = 1;")]
    [InlineData("int x = 42;")]
    [InlineData("string s = \"hello\";")]
    [InlineData("var list = new List<string>();")]
    [InlineData("List<string> items = new();")]
    [InlineData("Point3d pt = new(1, 2, 3);")]
    [InlineData("Curve c = new Line(p1, p2);")]
    [InlineData("using var cts = new CancellationTokenSource();")]
    [InlineData("double area = Math.PI * 10 * 10;")]
    [InlineData("bool flag = false;\nif (flag) { int y = 2; }")]
    [InlineData("var coords = new double[] { 1.0, 2.0, 3.0 };")]
    [InlineData("Dictionary<string, int> dict = new();")]
    public void ValidNonTransactionScripts_ZeroFalsePositives(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.False(analysis.UsesTransaction, $"False positive detected in non-transaction script: {code}");
    }

    [Theory]
    [InlineData("{ using SubTransaction s = new(doc); }")]
    [InlineData("{ using Transaction t = new(doc, \"Block\"); }")]
    [InlineData("void Run() { using SubTransaction s = new(doc); }")]
    public void UsingDeclarationInsideBlock_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed in block using: {code}");
    }

    [Theory]
    [InlineData("using Autodesk.Revit.DB;\nTransaction t;\nt = new(doc, \"Bypass\");")]
    [InlineData("using Autodesk.Revit.DB;\nSubTransaction st;\nst = new(doc);")]
    [InlineData("using Autodesk.Revit.DB;\nTransaction t = null;\nt = new(doc, \"Bypass\");")]
    [InlineData("using Autodesk.Revit.DB;\nusing SubTransaction s = new(doc);")]
    public async Task ModeNone_BypassVulnerability_ProvedEmpirically(string code)
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var engine = new GrasshopperExecutionEngine(coordinator);
        using var cts = new CancellationTokenSource();
        var req = new ExecuteRequest(code, "none");

        var result = await engine.DispatchAsync(req, new BridgeSettings { ExecutionEnabled = true }, null, cts);

        // If AST detection worked, it would fail with "enforces read-only execution".
        // If bypassed, it will either fail on compile/runtime or NOT report read-only violation.
        bool blockedByModeNone = result.IsError && result.Message?.Contains("enforces read-only execution", StringComparison.OrdinalIgnoreCase) == true;
        Assert.True(blockedByModeNone, $"CRITICAL BYPASS: Script was NOT blocked by mode 'none' pre-flight AST analyzer! Result message: {result.Message}");
    }
}
