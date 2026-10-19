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
///     Adversarial empirical challenge suite for Milestone M2 Iteration 4 (challenger_m2_iter4_2):
///     Empirically validates that lambda expressions returning or creating transactions are detected,
///     that GrasshopperExecutionEngine.DispatchAsync strictly enforces read-only under mode "none",
///     and that non-transaction lambdas and delegates have zero false positives.
/// </summary>
public sealed class LambdaAndModeNoneGateEmpiricalChallengeTests
{
    private static readonly AnalyzerProfile Profile = AnalyzerProfile.Grasshopper;

    [Theory]
    [InlineData("Func<Transaction> f = () => new(doc, \"f1\");")]
    [InlineData("Func<Transaction> f = () => { return new(doc, \"f2\"); };")]
    [InlineData("Func<Transaction> f = () => new Transaction(doc, \"f3\");")]
    [InlineData("Func<Transaction> f = () => { return new Transaction(doc, \"f4\"); };")]
    [InlineData("Action<Document> a = doc => new Transaction(doc, \"a1\");")]
    [InlineData("Action<Document> a = doc => { Transaction t = new(doc, \"a2\"); };")]
    [InlineData("Action<Document> a = doc => { using (var t = new Transaction(doc, \"a3\")) { } };")]
    [InlineData("Action<Document> a = doc => { using Transaction t = new(doc, \"a4\"); };")]
    [InlineData("Func<Document, Transaction> f = d => new(d, \"f5\");")]
    [InlineData("Func<Document, string, Transaction> f = (d, n) => new(d, n);")]
    [InlineData("Func<SubTransaction> f = () => new(doc);")]
    [InlineData("Func<TransactionGroup> f = () => new(doc, \"tg\");")]
    [InlineData("Func<Autodesk.Revit.DB.Transaction> f = () => new(doc, \"f_qual\");")]
    [InlineData("Func<global::Autodesk.Revit.DB.Transaction> f = () => new(doc, \"f_alias\");")]
    [InlineData("Func<Transaction>[] funcs = [() => new(doc, \"arr1\"), () => new(doc, \"arr2\")];")]
    [InlineData("List<Func<Transaction>> list = [() => new(doc, \"l1\"), () => new(doc, \"l2\")];")]
    [InlineData("Func<Func<Transaction>> higherOrder = () => () => new(doc, \"ho\");")]
    [InlineData("Func<Task<Transaction>> asyncF = async () => new(doc, \"async1\");")]
    [InlineData("Func<Task<Transaction>> asyncF = async () => { await Task.Yield(); return new(doc, \"async2\"); };")]
    [InlineData("Transaction t = null; Action a = () => { t = new(doc, \"capture\"); };")]
    [InlineData("Action<Document> a = (doc) => { if (true) { Transaction t = new(doc, \"nested_block\"); } };")]
    [InlineData("Func<Transaction?> nullableF = () => new(doc, \"nullable\");")]
    [InlineData("var lambda = (Func<Transaction>)(() => new(doc, \"cast_lambda\"));")]
    public void LambdaExpressions_CreatingOrReturningTransactions_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect transaction in lambda: {code}");
    }

    [Theory]
    [InlineData("Func<int> f = () => 42;")]
    [InlineData("Func<string, string> f = s => $\"Hello {s}\";")]
    [InlineData("Action<Document> a = doc => { var title = doc.Title; };")]
    [InlineData("Action<string> a = s => Console.WriteLine(s);")]
    [InlineData("Func<List<int>> f = () => [1, 2, 3];")]
    [InlineData("Func<Point3d> f = () => new(1, 2, 3);")]
    [InlineData("Action a = () => { var p = new Point3d(0, 0, 0); };")]
    [InlineData("Func<CancellationTokenSource> f = () => new();")]
    [InlineData("Action<Action> run = act => act();")]
    [InlineData("List<Func<int>> list = [() => 1, () => 2];")]
    [InlineData("Func<string> f = () => \"Transaction in string is harmless\";")]
    [InlineData("class TransactionModel { public string Name { get; set; } }\nFunc<TransactionModel> f = () => new() { Name = \"test\" };")]
    [InlineData("Func<int, int> fib = null;\nfib = n => n <= 1 ? n : fib(n - 1) + fib(n - 2);")]
    [InlineData("Action<Document> inspect = doc => { var isModified = doc.IsModified; };")]
    [InlineData("Func<Task<int>> asyncInt = async () => { await Task.Yield(); return 100; };")]
    [InlineData("var calc = (Func<double, double>)(x => x * 2.0);")]
    public void NonTransactionLambdasAndDelegates_ZeroFalsePositives(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.False(analysis.UsesTransaction, $"False positive detected in benign lambda: {code}");
    }

    [Theory]
    [InlineData("using Autodesk.Revit.DB;\nFunc<Transaction> f = () => new(doc, \"1\");")]
    [InlineData("using Autodesk.Revit.DB;\nAction<Document> a = doc => new Transaction(doc, \"2\");")]
    [InlineData("using Autodesk.Revit.DB;\nAction<Document> a = doc => { Transaction t = new(doc, \"3\"); };")]
    [InlineData("using Autodesk.Revit.DB;\nFunc<Document, Transaction> f = d => new(d, \"4\");")]
    [InlineData("using Autodesk.Revit.DB;\nFunc<SubTransaction> f = () => new(doc);")]
    [InlineData("using Autodesk.Revit.DB;\nFunc<TransactionGroup> f = () => new(doc, \"tg\");")]
    [InlineData("using Autodesk.Revit.DB;\nFunc<Transaction>[] arr = [() => new(doc, \"arr\")];")]
    [InlineData("using Autodesk.Revit.DB;\nList<Func<Transaction>> list = [() => new(doc, \"list\")];")]
    [InlineData("using Autodesk.Revit.DB;\nFunc<Func<Transaction>> ho = () => () => new(doc, \"ho\");")]
    [InlineData("using Autodesk.Revit.DB;\nTransaction t = null;\nAction a = () => { t = new(doc, \"outer\"); };")]
    public async Task ModeNone_BlocksLambdaTransactionCreations_Empirically(string code)
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var engine = new GrasshopperExecutionEngine(coordinator);
        using var cts = new CancellationTokenSource();
        var req = new ExecuteRequest(code, "none");

        var result = await engine.DispatchAsync(req, new BridgeSettings { ExecutionEnabled = true }, null, cts);

        bool blockedByModeNone = result.IsError && (result.Message?.Contains("enforces read-only execution", StringComparison.OrdinalIgnoreCase) ?? false);
        Assert.True(blockedByModeNone, $"Mode 'none' read-only gate failed to block lambda transaction instantiation! Result: {result.Message}");
    }
}
