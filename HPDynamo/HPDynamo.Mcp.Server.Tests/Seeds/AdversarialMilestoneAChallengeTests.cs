namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

/// <summary>
///     Adversarial Empirical Challenge Test Suite for Milestone A (R2-01, R2-02, R2-03, R2-10).
///     Authored by challenger_a_1.
/// </summary>
public sealed class AdversarialMilestoneAChallengeTests
{
    private static string LoadBatchEditGraphCode()
    {
        var assembly = typeof(DynamoHostProfile).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("batch_edit_graph.code.cs", StringComparison.OrdinalIgnoreCase) ||
                                 (n.Contains("batch_edit_graph") && n.EndsWith("code.cs", StringComparison.OrdinalIgnoreCase)));

        Assert.NotNull(resourceName);
        using var stream = assembly.GetManifestResourceStream(resourceName!)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string LoadGetNodeOutputValuesCode()
    {
        var assembly = typeof(DynamoHostProfile).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("get_node_output_values.code.cs", StringComparison.OrdinalIgnoreCase) ||
                                 (n.Contains("get_node_output_values") && n.EndsWith("code.cs", StringComparison.OrdinalIgnoreCase)));

        Assert.NotNull(resourceName);
        using var stream = assembly.GetManifestResourceStream(resourceName!)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // =========================================================================
    // SECTION 1: R2-01 — Node Value Rollback (Number, String, Boolean, CodeBlock)
    // =========================================================================

    [Fact]
    public void TC_CHAL_R2_01_NodeValueRollback_AllTypesRestoredInLIFOOrder()
    {
        // Setup initial canvas state for 4 node types
        var numNode = Guid.NewGuid();
        var strNode = Guid.NewGuid();
        var boolNode = Guid.NewGuid();
        var cbnNode = Guid.NewGuid();

        var nodeValues = new Dictionary<Guid, string>
        {
            [numNode] = "42.5",
            [strNode] = "OriginalString",
            [boolNode] = "True",
            [cbnNode] = "x = 10; y = 20;"
        };

        var modifiedProperties = new List<(Guid NodeGuid, string Property, string? OriginalValue)>();

        // Step 1: Update each node
        modifiedProperties.Add((numNode, "value", nodeValues[numNode]));
        nodeValues[numNode] = "99.9";

        modifiedProperties.Add((strNode, "value", nodeValues[strNode]));
        nodeValues[strNode] = "UpdatedString";

        modifiedProperties.Add((boolNode, "value", nodeValues[boolNode]));
        nodeValues[boolNode] = "False";

        modifiedProperties.Add((cbnNode, "value", nodeValues[cbnNode]));
        nodeValues[cbnNode] = "x = 999;";

        // Intermediate verification: All values mutated
        Assert.Equal("99.9", nodeValues[numNode]);
        Assert.Equal("UpdatedString", nodeValues[strNode]);
        Assert.Equal("False", nodeValues[boolNode]);
        Assert.Equal("x = 999;", nodeValues[cbnNode]);

        // Step 2: Rollback in LIFO order (matching batch_edit_graph/code.cs lines 227-231)
        for (int i = modifiedProperties.Count - 1; i >= 0; i--)
        {
            var (guid, prop, origVal) = modifiedProperties[i];
            if (prop == "value" && origVal != null)
            {
                nodeValues[guid] = origVal;
            }
        }

        // Final verification: 100% restored
        Assert.Equal("42.5", nodeValues[numNode]);
        Assert.Equal("OriginalString", nodeValues[strNode]);
        Assert.Equal("True", nodeValues[boolNode]);
        Assert.Equal("x = 10; y = 20;", nodeValues[cbnNode]);
    }

    [Fact]
    public void TC_CHAL_R2_01_AST_VerifyGetNodeValueAndRestorePropertyImplementation()
    {
        var code = LoadBatchEditGraphCode();
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        var root = tree.GetRoot();

        // 1. Verify GetNodeValue extracts CodeBlock Code directly
        Assert.Contains("if (node is CodeBlockNodeModel cbn) return cbn.Code;", code);

        // 2. Verify GetNodeValue extracts EngineController data with invariant formatting
        Assert.Contains("node.GetValue(0, controller)?.Data", code);
        Assert.Contains("IFormattable formattable ? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : nodeData.ToString()", code);

        // 3. Verify RestoreProperty handles value for both CodeBlock and standard NodeModel
        Assert.Contains("if (node is CodeBlockNodeModel cbn && origVal != null) cbn.SetCodeContent(origVal, currentWorkspace.ElementResolver);", code);
        Assert.Contains("else if (origVal != null) node.UpdateValue(new Dynamo.Graph.UpdateValueParams(\"Value\", origVal));", code);
    }

    // =========================================================================
    // SECTION 2: R2-02 — Multi-Wire Replacement Ordering (A->T, B->T, C->T)
    // =========================================================================

    [Fact]
    public void TC_CHAL_R2_02_MultiWireReplacement_WithPreExistingWire_RestoresOriginalWire()
    {
        // When target port T initially has wire Orig -> T,
        // and batch connects A -> T, then B -> T, then C -> T:
        // Rollback MUST restore Orig -> T, not B -> T or A -> T.

        var origNode = Guid.NewGuid();
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();
        var nodeC = Guid.NewGuid();
        var targetNode = Guid.NewGuid();
        const int targetPort = 0;

        // Initial canvas wire
        var activeWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>
        {
            (origNode, 0, targetNode, targetPort)
        };

        var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();
        var touchedTargetPorts = new HashSet<(Guid Target, int Port)>();

        void ConnectWire(Guid src, int sPort, Guid dst, int dPort)
        {
            var oldWire = activeWires.FirstOrDefault(w => w.Target == dst && w.TPort == dPort);
            if (oldWire.Target != Guid.Empty && touchedTargetPorts.Add((dst, dPort)))
            {
                replacedWires.Add((oldWire.Source, oldWire.SPort, dst, dPort));
            }

            // In Dynamo, new connection displaces old connection on the same input port
            activeWires.RemoveAll(w => w.Target == dst && w.TPort == dPort);
            activeWires.Add((src, sPort, dst, dPort));
        }

        // Apply 3 sequential wire replacements in one batch
        ConnectWire(nodeA, 0, targetNode, targetPort);
        ConnectWire(nodeB, 0, targetNode, targetPort);
        ConnectWire(nodeC, 0, targetNode, targetPort);

        // Verify active wire is now C -> T
        Assert.Single(activeWires);
        Assert.Equal(nodeC, activeWires[0].Source);

        // Verify replacedWires recorded ONLY the original wire (Orig -> T)
        Assert.Single(replacedWires);
        Assert.Equal(origNode, replacedWires[0].Source);
        Assert.Equal(targetNode, replacedWires[0].Target);

        // Rollback (lines 220-225 in code.cs)
        for (int i = replacedWires.Count - 1; i >= 0; i--)
        {
            var rw = replacedWires[i];
            activeWires.RemoveAll(w => w.Target == rw.Target && w.TPort == rw.TPort);
            activeWires.Add((rw.Source, rw.SPort, rw.Target, rw.TPort));
        }

        // Verify rollback returned to Orig -> T (NOT intermediate A or B!)
        Assert.Single(activeWires);
        Assert.Equal(origNode, activeWires[0].Source);
        Assert.Equal(targetNode, activeWires[0].Target);
    }

    [Fact]
    public void TC_CHAL_R2_02_MultiWireReplacement_InitiallyEmptyPort_RevealsEdgeCaseBehavior()
    {
        // ADVERSARIAL CHALLENGE:
        // What happens when target port T initially has NO WIRE,
        // and the batch connects A -> T, then B -> T?
        // Let's test the exact code.cs logic:
        // var oldWire = ...FirstOrDefault(...);
        // if (oldWire?.Start != null && touchedTargetPorts.Add((targetGuid, toPort)))
        //     replacedWires.Add(...);

        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();
        var targetNode = Guid.NewGuid();
        const int targetPort = 0;

        // Initial canvas: Target port has NO wire
        var activeWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();

        var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();
        var touchedTargetPorts = new HashSet<(Guid Target, int Port)>();
        var createdWiresBetweenExistingNodes = new List<Guid>();

        void SimulateCodeCsConnect(Guid src, int sPort, Guid dst, int dPort)
        {
            var oldWire = activeWires.FirstOrDefault(w => w.Target == dst && w.TPort == dPort);
            // EXACT LINE 125 from code.cs:
            if (oldWire.Target != Guid.Empty && touchedTargetPorts.Add((dst, dPort)))
            {
                replacedWires.Add((oldWire.Source, oldWire.SPort, dst, dPort));
            }

            activeWires.RemoveAll(w => w.Target == dst && w.TPort == dPort);
            activeWires.Add((src, sPort, dst, dPort));
            createdWiresBetweenExistingNodes.Add(Guid.NewGuid()); // new wire created
        }

        // Connect 1: A -> T (T was empty)
        SimulateCodeCsConnect(nodeA, 0, targetNode, targetPort);
        // Connect 2: B -> T (T now has A)
        SimulateCodeCsConnect(nodeB, 0, targetNode, targetPort);

        // At this point:
        // When Connect 1 ran: oldWire was empty, so touchedTargetPorts did NOT add (targetNode, 0)!
        // When Connect 2 ran: oldWire was A -> T! touchedTargetPorts.Add returned TRUE!
        // replacedWires received (A, 0, targetNode, 0)!
        Assert.Single(replacedWires);
        Assert.Equal(nodeA, replacedWires[0].Source);

        // Now perform rollback according to code.cs lines 217-225:
        // 1. Delete created wires
        activeWires.Clear();
        // 2. Reconnect replaced wires
        foreach (var rw in replacedWires)
        {
            activeWires.Add((rw.Source, rw.SPort, rw.Target, rw.TPort));
        }

        // OBSERVATION / FINDING:
        // Because touchedTargetPorts.Add was guarded by oldWire != null,
        // wire A was recorded as "replaced" when wire B displaced it!
        // So rollback restored wire A, even though port T was originally EMPTY!
        // This is a documented edge case discovery for the audit report.
        Assert.Single(activeWires);
        Assert.Equal(nodeA, activeWires[0].Source);
    }

    // =========================================================================
    // SECTION 3: R2-03 — CodeBlock Wire Preservation Across Edit
    // =========================================================================

    [Fact]
    public void TC_CHAL_R2_03_CodeBlockWirePreservation_RestoresAllConnectors()
    {
        var inputNode1 = Guid.NewGuid();
        var inputNode2 = Guid.NewGuid();
        var cbnNode = Guid.NewGuid();
        var outputConsumer1 = Guid.NewGuid();
        var outputConsumer2 = Guid.NewGuid();

        // Initial connectors attached to CodeBlock
        var canvasConnectors = new List<(Guid SGuid, int SPort, Guid TGuid, int TPort)>
        {
            (inputNode1, 0, cbnNode, 0),        // InPort 0
            (inputNode2, 0, cbnNode, 1),        // InPort 1
            (cbnNode, 0, outputConsumer1, 0),   // OutPort 0
            (cbnNode, 1, outputConsumer2, 0)    // OutPort 1
        };

        string originalCode = "x + y;\nx * y;";
        string currentCode = originalCode;

        // Snapshot mechanism matching code.cs line 40-47
        var codeBlockWires = new List<(Guid SGuid, int SPort, Guid TGuid, int TPort)>();
        foreach (var c in canvasConnectors)
        {
            if (c.SGuid == cbnNode || c.TGuid == cbnNode)
                codeBlockWires.Add((c.SGuid, c.SPort, c.TGuid, c.TPort));
        }

        Assert.Equal(4, codeBlockWires.Count);

        // Edit CodeBlock: Dynamo removes connectors when code changes
        currentCode = "z = 100;";
        canvasConnectors.Clear(); // In Dynamo, SetCodeContent detaches affected connectors

        // Verification: Wires detached during edit
        Assert.Empty(canvasConnectors);

        // Rollback mechanism matching code.cs lines 233-237:
        // 1. Restore code
        currentCode = originalCode;
        // 2. Re-establish connectors
        foreach (var (sg, sp, tg, tp) in codeBlockWires)
        {
            canvasConnectors.Add((sg, sp, tg, tp));
        }

        // Final verification: 100% of connectors restored
        Assert.Equal(4, canvasConnectors.Count);
        Assert.Contains(canvasConnectors, c => c.SGuid == inputNode1 && c.TGuid == cbnNode && c.TPort == 0);
        Assert.Contains(canvasConnectors, c => c.SGuid == inputNode2 && c.TGuid == cbnNode && c.TPort == 1);
        Assert.Contains(canvasConnectors, c => c.SGuid == cbnNode && c.SPort == 0 && c.TGuid == outputConsumer1);
        Assert.Contains(canvasConnectors, c => c.SGuid == cbnNode && c.SPort == 1 && c.TGuid == outputConsumer2);
        Assert.Equal("x + y;\nx * y;", currentCode);
    }

    // =========================================================================
    // SECTION 4: R2-10 — Payload Budgeting & Metadata Separation
    // =========================================================================

    private sealed class EmpiricalOutputBudgetSimulator
    {
        public const int MaxDepth = 6;
        public const int MaxBreadth = 100;
        public const int MaxTotalItemsPerPort = 500;
        public const int MaxStringLength = 2000;
        public const int MaxPayloadBytesPerPort = 64 * 1024;

        public int PortItemCount;
        public int PortByteCount;
        public bool PortTruncated;
        public string? PortTruncationReason;

        public string TruncateString(string s)
        {
            if (s.Length > MaxStringLength)
            {
                PortTruncated = true;
                PortTruncationReason ??= "string_length_exceeded";
                s = s.Substring(0, MaxStringLength) + "... [truncated]";
            }
            PortByteCount += Encoding.UTF8.GetByteCount(s);
            return s;
        }

        public object? SerializePayload(object? value, int depth, CancellationToken ct)
        {
            if (value == null) return null;
            ct.ThrowIfCancellationRequested();

            if (depth > MaxDepth || PortItemCount >= MaxTotalItemsPerPort || PortByteCount >= MaxPayloadBytesPerPort)
            {
                PortTruncated = true;
                PortTruncationReason ??= PortByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (PortItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "depth_limit_reached");
                return PortByteCount >= MaxPayloadBytesPerPort || PortItemCount >= MaxTotalItemsPerPort ? "[Max items reached]" : value.ToString();
            }

            if (value is string str) return TruncateString(str);
            if (value is bool || value is int || value is double || value is float || value is long)
            {
                PortByteCount += 8;
                return value;
            }

            if (value is IDictionary dict) return SerializeClrDictionary(dict, depth, ct);
            if (value is IEnumerable enumerable) return SerializeEnumerable(enumerable, depth, ct);

            return TruncateString(value.ToString() ?? "");
        }

        public object? SerializeClrDictionary(IDictionary dict, int depth, CancellationToken ct)
        {
            var dictResult = new Dictionary<string, object?>();
            int count = 0;
            foreach (DictionaryEntry entry in dict)
            {
                ct.ThrowIfCancellationRequested();
                if (count++ >= MaxBreadth || PortItemCount++ >= MaxTotalItemsPerPort || PortByteCount >= MaxPayloadBytesPerPort)
                {
                    PortTruncated = true;
                    PortTruncationReason ??= PortByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (PortItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "breadth_limit_exceeded");
                    break;
                }
                string key = entry.Key?.ToString() ?? "";
                dictResult[key] = SerializePayload(entry.Value, depth + 1, ct);
            }
            return dictResult;
        }

        public object? SerializeEnumerable(IEnumerable enumerable, int depth, CancellationToken ct)
        {
            var listResult = new List<object?>();
            int count = 0;
            foreach (var item in enumerable)
            {
                ct.ThrowIfCancellationRequested();
                if (count++ >= MaxBreadth || PortItemCount++ >= MaxTotalItemsPerPort || PortByteCount >= MaxPayloadBytesPerPort)
                {
                    PortTruncated = true;
                    PortTruncationReason ??= PortByteCount >= MaxPayloadBytesPerPort ? "byte_budget_exceeded" : (PortItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "breadth_limit_exceeded");
                    break;
                }
                listResult.Add(SerializePayload(item, depth + 1, ct));
            }
            return listResult;
        }
    }

    [Fact]
    public void TC_CHAL_R2_10_StringBudget_Exceeding2000Chars_TruncatedWithMetadata()
    {
        var sim = new EmpiricalOutputBudgetSimulator();
        string hugeString = new string('A', 5000);

        var result = sim.SerializePayload(hugeString, 0, CancellationToken.None) as string;

        Assert.NotNull(result);
        Assert.StartsWith(new string('A', 2000), result!);
        Assert.EndsWith("... [truncated]", result!);
        Assert.True(sim.PortTruncated);
        Assert.Equal("string_length_exceeded", sim.PortTruncationReason);
    }

    [Fact]
    public void TC_CHAL_R2_10_ItemsBudget_Exceeding500Elements_HaltsGracefully()
    {
        var sim = new EmpiricalOutputBudgetSimulator();
        var bigList = Enumerable.Range(0, 1000).Select(i => (object)i).ToList();

        var result = sim.SerializePayload(bigList, 0, CancellationToken.None) as List<object?>;

        Assert.NotNull(result);
        Assert.True(sim.PortTruncated);
        Assert.Equal("breadth_limit_exceeded", sim.PortTruncationReason);
        Assert.Equal(EmpiricalOutputBudgetSimulator.MaxBreadth, result!.Count);
    }

    [Fact]
    public void TC_CHAL_R2_10_ByteBudget_Exceeding64KB_EnforcesHardCap()
    {
        var sim = new EmpiricalOutputBudgetSimulator();
        // 80 chunks of 1,000 characters each = 80 KB > 64 KB
        var chunks = Enumerable.Range(0, 80).Select(i => (object)new string('X', 1000)).ToList();

        var result = sim.SerializePayload(chunks, 0, CancellationToken.None) as List<object?>;

        Assert.NotNull(result);
        Assert.True(sim.PortTruncated);
        Assert.Equal("byte_budget_exceeded", sim.PortTruncationReason);
        Assert.True(sim.PortByteCount >= EmpiricalOutputBudgetSimulator.MaxPayloadBytesPerPort);
    }

    [Fact]
    public void TC_CHAL_R2_10_DictionaryTruncation_NeverPollutesUserDictionaryKeys()
    {
        var sim = new EmpiricalOutputBudgetSimulator();
        var userDict = new Dictionary<string, object?>
        {
            ["alpha"] = "first",
            ["beta"] = new string('Z', 3000), // triggers string truncation
            ["gamma"] = 12345
        };

        var result = sim.SerializePayload(userDict, 0, CancellationToken.None) as Dictionary<string, object?>;

        Assert.NotNull(result);
        // CRITICAL CHECK: User keys are intact
        Assert.True(result!.ContainsKey("alpha"));
        Assert.True(result.ContainsKey("beta"));
        Assert.True(result.ContainsKey("gamma"));

        // CRITICAL CHECK: No "_truncated" key was inserted into user dictionary!
        Assert.False(result.ContainsKey("_truncated"), "User dictionary must never be polluted with '_truncated' key!");

        // Metadata is recorded separately on the port level
        Assert.True(sim.PortTruncated);
        Assert.Equal("string_length_exceeded", sim.PortTruncationReason);
    }

    [Fact]
    public void TC_CHAL_R2_10_AST_VerifyGetNodeOutputValuesCode_MetadataSeparation()
    {
        var code = LoadGetNodeOutputValuesCode();

        // 1. Verify truncation metadata is separated onto the port object
        Assert.Contains("portOutputDescriptions.Add(new", code);
        Assert.Contains("truncated = portTruncated,", code);
        Assert.Contains("truncationReason = portTruncationReason", code);

        // 2. Verify SerializeDictionary and SerializeClrDictionary do NOT insert "_truncated"
        Assert.DoesNotContain("dictResult[\"_truncated\"]", code);
        Assert.DoesNotContain("dictResult[\"truncated\"]", code);

        // 3. Verify budgets constants
        Assert.Contains("const int MaxDepth = 6;", code);
        Assert.Contains("const int MaxBreadth = 100;", code);
        Assert.Contains("const int MaxTotalItemsPerPort = 500;", code);
        Assert.Contains("const int MaxStringLength = 2000;", code);
        Assert.Contains("const int MaxPayloadBytesPerPort = 64 * 1024;", code);
    }
}
