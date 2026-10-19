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
///     Adversarial challenge test suite for Milestone M2 Iteration 4:
///     Empirically validates AST detection for multidimensional arrays (Transaction[,]),
///     jagged arrays (Transaction[][]), and deeply nested generic containers
///     (Dictionary&lt;string, List&lt;Transaction&gt;&gt;), plus ensures zero false positives.
/// </summary>
public sealed class MultidimensionalAndNestedChallengeTests
{
    private static readonly AnalyzerProfile Profile = AnalyzerProfile.Grasshopper;

    [Theory]
    [InlineData("Transaction[,] grid = new Transaction[2, 3];")]
    [InlineData("Transaction[,,] cube = new Transaction[2, 2, 2];")]
    [InlineData("SubTransaction[,] subGrid;")]
    [InlineData("TransactionGroup[,] groupGrid = null;")]
    [InlineData("Autodesk.Revit.DB.Transaction[,] grid;")]
    [InlineData("global::Autodesk.Revit.DB.Transaction[,] grid;")]
    [InlineData("Transaction[][] jagged = new Transaction[2][];")]
    [InlineData("Transaction[,][] mixed = new Transaction[2, 2][];")]
    [InlineData("Transaction[][,] mixed2 = new Transaction[2][,];")]
    [InlineData("Transaction[,,,] hyper = new Transaction[1, 1, 1, 1];")]
    public void MultidimensionalAndJaggedArrays_Declarations_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect multidimensional/jagged array: {code}");
    }

    [Theory]
    [InlineData("Transaction[,] grid = new Transaction[1, 1]; grid[0, 0] = new(doc, \"1\");")]
    [InlineData("Transaction[,] grid = new[,] { { new(doc, \"1\") } };")]
    [InlineData("Transaction[,] grid = new Transaction[,] { { new(doc, \"1\") } };")]
    [InlineData("Transaction[][] jagged = [ [new(doc, \"1\")] ];")]
    public void MultidimensionalAndJaggedArrays_TargetTypedNew_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect target-typed new in multidimensional/jagged array: {code}");
    }

    [Theory]
    [InlineData("Dictionary<string, List<Transaction>> dict = new();")]
    [InlineData("Dictionary<string, List<Transaction>> dict = new Dictionary<string, List<Transaction>>();")]
    [InlineData("Dictionary<string, Dictionary<int, Transaction>> nestedDict = new();")]
    [InlineData("List<List<Transaction>> matrix = new();")]
    [InlineData("List<List<List<Transaction>>> tensor = new();")]
    [InlineData("HashSet<List<Transaction>> setOfLists = new();")]
    [InlineData("Queue<Stack<Transaction>> qs = new();")]
    [InlineData("Dictionary<string, Transaction[]> dictOfArrays = new();")]
    [InlineData("List<Transaction[,]> listOfMatrices = new();")]
    [InlineData("Dictionary<string, List<SubTransaction>> subDict = new();")]
    [InlineData("Dictionary<string, List<TransactionGroup>> groupDict = new();")]
    public void NestedGenericContainers_Declarations_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect nested generic container: {code}");
    }

    [Theory]
    [InlineData("Dictionary<string, List<Transaction>> dict = new() { [\"t\"] = [new(doc, \"1\")] };")]
    [InlineData("List<List<Transaction>> matrix = [[new(doc, \"1\")]];")]
    [InlineData("List<Transaction[]> listOfArrays = [ [new(doc, \"1\")] ];")]
    [InlineData("Dictionary<string, Transaction[]> dictOfArrays = new() { [\"k\"] = [new(doc, \"1\")] };")]
    [InlineData("Dictionary<string, List<Transaction>> dict; dict = null; dict[\"k\"] = [new(doc, \"assign\")];")]
    public void NestedGenericContainers_TargetTypedNew_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect target-typed new in nested generic container: {code}");
    }

    [Theory]
    [InlineData("Dictionary<string, List<Transaction>> GetTxMap() => new();")]
    [InlineData("Func<Dictionary<string, List<Transaction>>> f = () => new();")]
    [InlineData("Func<List<Transaction>> fList = () => [new(doc, \"f\")];")]
    [InlineData("List<Transaction> Prop => [new(doc, \"prop\")];")]
    [InlineData("void Worker() { Dictionary<string, List<Transaction>> d = new(); }")]
    public void NestedGenerics_MethodsAndLambdas_Detected(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.True(analysis.UsesTransaction, $"Failed to detect nested generic return type or local container: {code}");
    }

    [Theory]
    [InlineData("int[,] grid = new int[2, 2];")]
    [InlineData("string[,] textMatrix = new string[3, 3];")]
    [InlineData("double[,,] tensor = new double[2, 2, 2];")]
    [InlineData("Dictionary<string, List<string>> dict = new();")]
    [InlineData("Dictionary<string, List<int>> map = new() { [\"nums\"] = [1, 2, 3] };")]
    [InlineData("List<List<double>> matrix = [[1.0, 2.0], [3.0, 4.0]];")]
    [InlineData("Dictionary<string, List<string>> GetMap() => new();")]
    [InlineData("Func<Dictionary<string, List<int>>> f = () => new();")]
    [InlineData("class TransactionItem { public string Name; }\nDictionary<string, List<TransactionItem>> d = new();")]
    [InlineData("int[][] jaggedNumbers = [[1, 2], [3, 4]];")]
    public void MultidimensionalAndNestedContainers_ZeroFalsePositives(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, Profile);
        Assert.False(analysis.UsesTransaction, $"False positive detected in benign multidimensional/nested container: {code}");
    }

    [Theory]
    [InlineData("using Autodesk.Revit.DB;\nTransaction[,] grid = new Transaction[1, 1];")]
    [InlineData("using Autodesk.Revit.DB;\nDictionary<string, List<Transaction>> dict = new();")]
    [InlineData("using Autodesk.Revit.DB;\nDictionary<string, List<Transaction>> dict = new() { [\"t\"] = [new(doc, \"1\")] };")]
    [InlineData("using Autodesk.Revit.DB;\nTransaction[,] grid = new[,] { { new(doc, \"1\") } };")]
    public async Task ModeNone_MultidimensionalAndNested_EnforcesReadOnly(string code)
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var engine = new GrasshopperExecutionEngine(coordinator);
        using var cts = new CancellationTokenSource();
        var req = new ExecuteRequest(code, "none");

        var result = await engine.DispatchAsync(req, new BridgeSettings { ExecutionEnabled = true }, null, cts);

        bool blockedByModeNone = result.IsError && result.Message?.Contains("enforces read-only execution", StringComparison.OrdinalIgnoreCase) == true;
        Assert.True(blockedByModeNone, $"Mode 'none' failed to block transaction script! Result: {result.Message}");
    }
}
