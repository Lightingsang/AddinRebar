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
///     Empirical adversarial challenge suite (Milestone M2 Iteration 3):
///     Stress-tests nested scopes, lambdas, collection/array initializers,
///     and rigorously checks non-transaction scripts for false positives.
/// </summary>
public sealed class AdversarialEdgeCaseEmpiricalTests
{
    private static readonly AnalyzerProfile Profile = AnalyzerProfile.Grasshopper;

    [Theory]
    [InlineData("{ { Transaction t = new(doc, \"inner\"); } }")]
    [InlineData("if (true) { SubTransaction st = new(doc); }")]
    [InlineData("for (int i = 0; i < 1; i++) { TransactionGroup tg = new(doc, \"loop\"); }")]
    [InlineData("Transaction t;\nif (true) { t = new(doc, \"deep\"); }")]
    [InlineData("try { Transaction t = new(doc, \"try\"); } finally { }")]
    [InlineData("void DoWork() { Transaction t = new(doc, \"local\"); }")]
    [InlineData("switch (1) { case 1: Transaction t = new(doc, \"switch\"); break; }")]
    public void NestedScopes_WithTransactionVariables_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect transaction in nested scope: {code}");
    }

    [Theory]
    [InlineData("var f = () => new Transaction(doc, \"lambda\");")]
    [InlineData("Action a = () => { Transaction t = new(doc, \"l\"); };")]
    [InlineData("Transaction GetTx() => new(doc, \"method\");")]
    [InlineData("Transaction Tx => new(doc, \"prop\");")]
    [InlineData("Transaction GetTx() { return new(doc, \"method\"); }")]
    [InlineData("Func<Transaction> f = () => new(doc, \"lambda\");")]
    [InlineData("Func<Transaction> f = () => { return new(doc, \"lambda\"); };")]
    public void LambdasAndMethods_ReturningTransactions_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect transaction in lambda/method: {code}");
    }

    [Theory]
    [InlineData("var arr = new Transaction[] { new Transaction(doc, \"1\") };")]
    [InlineData("var t = (Transaction)new(doc, \"cast\");")]
    [InlineData("Transaction t = true ? new(doc, \"1\") : new(doc, \"2\");")]
    [InlineData("Transaction? opt = new(doc, \"nullable\");")]
    [InlineData("Transaction[] arr = [new(doc, \"1\")];")]
    [InlineData("Transaction[] arr = new Transaction[] { new(doc, \"1\") };")]
    [InlineData("List<Transaction> list = [new(doc, \"1\")];")]
    public void CollectionsAndCasts_WithTransactions_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect transaction: {code}");
    }

    [Theory]
    [InlineData("new Transaction(doc, \"direct\");")]
    [InlineData("using (new Transaction(doc, \"using\")) { }")]
    [InlineData("_ = new Transaction(doc, \"discard\");")]
    [InlineData("Transaction t;")]
    [InlineData("Transaction t = null;")]
    [InlineData("SubTransaction st;")]
    [InlineData("TransactionGroup tg = null;")]
    public void StandaloneDeclarationsAndCreations_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect transaction in standalone construct: {code}");
    }

    [Theory]
    [InlineData("int transactionCount = 5;")]
    [InlineData("string transactionName = \"myTx\";")]
    [InlineData("bool isTransactionActive = false;")]
    [InlineData("string s = \"Starting Transaction now...\";")]
    [InlineData("var msg = $\"Transaction {1} completed\";")]
    [InlineData("// Transaction t = new(doc, \"commented\");")]
    [InlineData("/* using (Transaction t = new(doc)) { } */")]
    [InlineData("class TransactionInfo { public string Name = \"info\"; }\nTransactionInfo info = new();")]
    [InlineData("class CustomTransactionManager { }\nCustomTransactionManager m = new();")]
    [InlineData("List<int> numbers = [1, 2, 3];")]
    [InlineData("Dictionary<string, string> map = new() { [\"a\"] = \"b\" };")]
    [InlineData("HashSet<double> set = [1.0, 2.5];")]
    [InlineData("var p1 = new Point3d(0, 0, 0);\nvar p2 = new Point3d(10, 10, 0);\nvar line = new Line(p1, p2);\nvar length = line.Length;")]
    [InlineData("using var cts = new CancellationTokenSource();")]
    public void NonTransactionScripts_ZeroFalsePositives(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.False(analysis.UsesTransaction, $"False positive detected in non-transaction code: {code}");
    }

    [Theory]
    [InlineData("using Autodesk.Revit.DB;\nTransaction[] arr = [new(doc, \"Bypass\")];")]
    [InlineData("using Autodesk.Revit.DB;\nFunc<Transaction> f = () => new(doc, \"Bypass\");")]
    [InlineData("using Autodesk.Revit.DB;\nTransaction[] arr = new Transaction[] { new(doc, \"Bypass\") };")]
    public async Task ModeNone_CollectionAndLambda_BypassVulnerability_ProvedEmpirically(string code)
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var engine = new GrasshopperExecutionEngine(coordinator);
        using var cts = new CancellationTokenSource();
        var req = new ExecuteRequest(code, "none");

        var result = await engine.DispatchAsync(req, new BridgeSettings { ExecutionEnabled = true }, null, cts);

        bool blockedByModeNone = result.IsError && result.Message?.Contains("enforces read-only execution", StringComparison.OrdinalIgnoreCase) == true;
        Assert.True(blockedByModeNone, $"CRITICAL BYPASS: Script was NOT blocked by mode 'none' pre-flight AST analyzer! Result message: {result.Message}");
    }
}
