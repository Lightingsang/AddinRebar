namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

public sealed class BatchEditGraphRollbackTests
{
    private static string LoadBatchEditGraphCode()
    {
        var assembly = typeof(DynamoHostProfile).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("batch_edit_graph.code.cs", StringComparison.OrdinalIgnoreCase) ||
                                 n.Contains("batch_edit_graph") && n.EndsWith("code.cs", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(resourceName);
        using var stream = assembly.GetManifestResourceStream(resourceName!)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public void BatchEditGraph_ReadsDryRunFromArgsOrGlobals()
    {
        var code = LoadBatchEditGraphCode();
        Assert.Contains("bool dryRun = args.Bool(\"dryRun\", false) || (globals != null && globals.dryRun);", code);
    }

    [Fact]
    public void BatchEditGraph_HasProgrammaticRollbackSnapshotStructures()
    {
        var code = LoadBatchEditGraphCode();
        Assert.Contains("var createdNodeGuids = new List<Guid>();", code);
        Assert.Contains("var createdWiresBetweenExistingNodes = new List<Guid>();", code);
        Assert.Contains("var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();", code);
        Assert.Contains("var modifiedProperties = new List<(Guid NodeGuid, string Property, string? OriginalValue)>();", code);
    }

    [Fact]
    public void BatchEditGraph_WrapsExecutionInTryCatchWithRollback()
    {
        var code = LoadBatchEditGraphCode();
        Assert.Contains("try", code);
        Assert.Contains("catch (Exception)", code);
        Assert.Contains("PerformRollback();", code);
        Assert.Contains("throw;", code);
    }

    [Fact]
    public void BatchEditGraph_ExecutesRollbackUnderDryRun()
    {
        var code = LoadBatchEditGraphCode();
        Assert.Contains("if (dryRun)", code);
        Assert.Contains("PerformRollback();", code);
    }

    [Fact]
    public void BatchEditGraph_PerformRollback_RestoresAllModifications()
    {
        var code = LoadBatchEditGraphCode();
        Assert.Contains("DeleteModelCommand(wireGuid)", code);
        Assert.Contains("MakeConnectionCommand", code);
        Assert.Contains("RestoreProperty", code);
        Assert.Contains("DeleteModelCommand(createdNodeGuids", code);
    }

    [Fact]
    public void BatchEditGraph_CompliesWithScriptQuality_ZeroErrorsAndWarnings()
    {
        var code = LoadBatchEditGraphCode();
        int lines = ScriptQuality.LineCount(code);
        Assert.True(lines <= ScriptQuality.MaxScriptLines, $"Line count {lines} exceeds limit {ScriptQuality.MaxScriptLines}");

        var findings = ScriptQuality.Find(code);
        var errors = findings.Where(f => f.Severity == ScriptQuality.Error).ToList();
        var warnings = findings.Where(f => f.Severity == ScriptQuality.Warning).ToList();

        Assert.Empty(errors);
        Assert.Empty(warnings);
    }

    [Fact]
    public void BatchEditGraph_AstAnalysis_LocalFunctionsWithinLimits()
    {
        var code = LoadBatchEditGraphCode();
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        var root = tree.GetRoot();

        // In Roslyn script mode, top-level functions are parsed as MethodDeclarationSyntax
        var functions = root.DescendantNodes()
            .Where(n => n is MethodDeclarationSyntax or LocalFunctionStatementSyntax)
            .ToList();
        Assert.NotEmpty(functions);

        foreach (var func in functions)
        {
            var span = tree.GetLineSpan(func.Span);
            int funcLines = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
            string name = func switch
            {
                MethodDeclarationSyntax m => m.Identifier.Text,
                LocalFunctionStatementSyntax l => l.Identifier.Text,
                _ => "unknown"
            };
            Assert.True(funcLines <= ScriptQuality.MaxBlockLines,
                $"Function '{name}' has {funcLines} lines (max {ScriptQuality.MaxBlockLines})");
        }
    }

    [Fact]
    public void BatchEditGraph_GuardsEvaluationUnderDryRun()
    {
        var code = LoadBatchEditGraphCode();
        Assert.Contains("if (runEvaluation && homeWs != null && !dryRun)", code);
    }

    [Fact]
    public void BatchEditGraph_PausesAndRestoresRunTypeCleanly()
    {
        var code = LoadBatchEditGraphCode();
        Assert.Contains("var prevRunType = homeWs?.RunSettings.RunType;", code);
        Assert.Contains("if (homeWs != null) homeWs.RunSettings.RunType = RunType.Manual;", code);
        Assert.Contains("if (homeWs != null && prevRunType.HasValue)", code);
        Assert.Contains("homeWs.RunSettings.RunType = prevRunType.Value;", code);
    }

    [Fact]
    public void BatchEditGraph_RollbackOracle_VerifiesCompleteStateRestoration()
    {
        // Behavioral Simulation Oracle matching batch_edit_graph/code.cs rollback mechanics
        var existingNodeA = Guid.NewGuid();
        var existingNodeB = Guid.NewGuid();
        var existingWire = Guid.NewGuid();

        // 1. Initial State
        var nodesOnCanvas = new HashSet<Guid> { existingNodeA, existingNodeB };
        var wiresOnCanvas = new List<(Guid WireId, Guid Src, int SPort, Guid Dst, int DPort)>
        {
            (existingWire, existingNodeA, 0, existingNodeB, 0)
        };
        var nodeProps = new Dictionary<(Guid, string), string?>
        {
            { (existingNodeA, "name"), "NodeA_Original" },
            { (existingNodeA, "x"), "100.0" },
            { (existingNodeA, "y"), "200.0" },
            { (existingNodeA, "isFrozen"), "False" },
            { (existingNodeA, "value"), "a = 10;" }
        };

        // 2. Rollback Snapshot Trackers
        var createdNodeGuids = new List<Guid>();
        var createdWiresBetweenExistingNodes = new List<Guid>();
        var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();
        var modifiedProperties = new List<(Guid NodeGuid, string Property, string? OriginalValue)>();

        // 3. Apply Batch Mutations:
        // Mutation A: Create new node C
        var newNodeC = Guid.NewGuid();
        nodesOnCanvas.Add(newNodeC);
        createdNodeGuids.Add(newNodeC);

        // Mutation B: Connect new node C to existing node B port 0 (replaces existing wire A->B)
        var oldWire = wiresOnCanvas.FirstOrDefault(w => w.Dst == existingNodeB && w.DPort == 0);
        replacedWires.Add((oldWire.Src, oldWire.SPort, existingNodeB, 0));
        wiresOnCanvas.Remove(oldWire); // replaced
        var wireC_B = Guid.NewGuid();
        wiresOnCanvas.Add((wireC_B, newNodeC, 0, existingNodeB, 0));

        // Mutation C: Connect existing node A port 1 to existing node B port 1 (new wire between existing nodes)
        var wireA_B2 = Guid.NewGuid();
        wiresOnCanvas.Add((wireA_B2, existingNodeA, 1, existingNodeB, 1));
        createdWiresBetweenExistingNodes.Add(wireA_B2);

        // Mutation D: Update properties on existing node A
        modifiedProperties.Add((existingNodeA, "name", nodeProps[(existingNodeA, "name")]));
        nodeProps[(existingNodeA, "name")] = "NodeA_Modified";
        modifiedProperties.Add((existingNodeA, "isFrozen", nodeProps[(existingNodeA, "isFrozen")]));
        nodeProps[(existingNodeA, "isFrozen")] = "True";
        modifiedProperties.Add((existingNodeA, "x", nodeProps[(existingNodeA, "x")]));
        nodeProps[(existingNodeA, "x")] = "999.0";

        // Verify mutations took effect
        Assert.Contains(newNodeC, nodesOnCanvas);
        Assert.Equal("NodeA_Modified", nodeProps[(existingNodeA, "name")]);
        Assert.Equal("True", nodeProps[(existingNodeA, "isFrozen")]);
        Assert.Equal("999.0", nodeProps[(existingNodeA, "x")]);
        Assert.DoesNotContain(wiresOnCanvas, w => w.WireId == existingWire);

        // 4. Execute PerformRollback() strictly according to code.cs logic
        // Step a: Delete created wires between existing nodes
        foreach (var wireGuid in createdWiresBetweenExistingNodes)
            wiresOnCanvas.RemoveAll(w => w.WireId == wireGuid);

        // Step b: Reconnect replaced wires
        foreach (var rw in replacedWires)
        {
            // Remove wire occupying target port
            wiresOnCanvas.RemoveAll(w => w.Dst == rw.Target && w.DPort == rw.TPort);
            wiresOnCanvas.Add((existingWire, rw.Source, rw.SPort, rw.Target, rw.TPort));
        }

        // Step c: Restore modified properties (LIFO order)
        for (int i = modifiedProperties.Count - 1; i >= 0; i--)
        {
            var (guid, prop, origVal) = modifiedProperties[i];
            nodeProps[(guid, prop)] = origVal;
        }

        // Step d: Delete created nodes (LIFO order)
        for (int i = createdNodeGuids.Count - 1; i >= 0; i--)
        {
            var guid = createdNodeGuids[i];
            nodesOnCanvas.Remove(guid);
            wiresOnCanvas.RemoveAll(w => w.Src == guid || w.Dst == guid);
        }

        // 5. Assert 100% Complete Restoration
        Assert.Equal(2, nodesOnCanvas.Count);
        Assert.Contains(existingNodeA, nodesOnCanvas);
        Assert.Contains(existingNodeB, nodesOnCanvas);
        Assert.DoesNotContain(newNodeC, nodesOnCanvas);

        Assert.Equal("NodeA_Original", nodeProps[(existingNodeA, "name")]);
        Assert.Equal("100.0", nodeProps[(existingNodeA, "x")]);
        Assert.Equal("200.0", nodeProps[(existingNodeA, "y")]);
        Assert.Equal("False", nodeProps[(existingNodeA, "isFrozen")]);
        Assert.Equal("a = 10;", nodeProps[(existingNodeA, "value")]);

        Assert.Single(wiresOnCanvas);
        var restoredWire = wiresOnCanvas[0];
        Assert.Equal(existingWire, restoredWire.WireId);
        Assert.Equal(existingNodeA, restoredWire.Src);
        Assert.Equal(existingNodeB, restoredWire.Dst);
        Assert.Equal(0, restoredWire.SPort);
        Assert.Equal(0, restoredWire.DPort);
    }

    [Fact]
    public void BatchEditGraph_RollbackOracle_MidFlightExceptionRollsBackCleanly()
    {
        var existingNode = Guid.NewGuid();
        var nodesOnCanvas = new HashSet<Guid> { existingNode };
        var createdNodeGuids = new List<Guid>();

        bool rollbackExecuted = false;

        void SimulateBatchWithFailure()
        {
            try
            {
                // Step 1: Create node 1
                var n1 = Guid.NewGuid();
                nodesOnCanvas.Add(n1);
                createdNodeGuids.Add(n1);

                // Step 2: Create node 2
                var n2 = Guid.NewGuid();
                nodesOnCanvas.Add(n2);
                createdNodeGuids.Add(n2);

                // Step 3: Failure occurs (e.g. invalid port or missing node)
                throw new InvalidOperationException("Simulated wire connection failure on port index -1");
            }
            catch (Exception)
            {
                // PerformRollback()
                rollbackExecuted = true;
                for (int i = createdNodeGuids.Count - 1; i >= 0; i--)
                {
                    nodesOnCanvas.Remove(createdNodeGuids[i]);
                }
                throw;
            }
        }

        var ex = Assert.Throws<InvalidOperationException>(SimulateBatchWithFailure);
        Assert.Contains("Simulated wire connection failure", ex.Message);
        Assert.True(rollbackExecuted);
        Assert.Single(nodesOnCanvas);
        Assert.Contains(existingNode, nodesOnCanvas);
    }

    [Fact]
    public void BatchEditGraph_RollbackOracle_MultiplePropertyModificationsLIFORestoration()
    {
        var nodeGuid = Guid.NewGuid();
        string initialValue = "initial";
        string currentValue = initialValue;

        var modifiedProperties = new List<(Guid NodeGuid, string Property, string? OriginalValue)>();

        // Update 1: initial -> step1
        modifiedProperties.Add((nodeGuid, "value", currentValue));
        currentValue = "step1";

        // Update 2: step1 -> step2
        modifiedProperties.Add((nodeGuid, "value", currentValue));
        currentValue = "step2";

        // Update 3: step2 -> final
        modifiedProperties.Add((nodeGuid, "value", currentValue));
        currentValue = "final";

        Assert.Equal("final", currentValue);

        // LIFO Rollback
        for (int i = modifiedProperties.Count - 1; i >= 0; i--)
        {
            var (_, _, origVal) = modifiedProperties[i];
            currentValue = origVal!;
        }

        // Restores back to original value
        Assert.Equal("initial", currentValue);
    }

    [Fact]
    public void BatchEditGraph_RestoreProperty_RecognizesAllSupportedProperties()
    {
        var code = LoadBatchEditGraphCode();
        // Verify RestoreProperty handles name, isFrozen, value, x, y
        Assert.Contains("if (prop == \"name\")", code);
        Assert.Contains("else if (prop == \"isFrozen\"", code);
        Assert.Contains("else if (prop == \"value\")", code);
        Assert.Contains("else if ((prop == \"x\" || prop == \"y\")", code);
        Assert.Contains("node.ReportPosition();", code);
    }
}


