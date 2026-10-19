namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Empirical adversarial test suite for Milestone M2 (Findings N8, N9, N10).
/// </summary>
public sealed class ChallengerM2N8N9N10AdversarialTests : IDisposable
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    public ChallengerM2N8N9N10AdversarialTests() => ResetCoordinator();

    public void Dispose() => ResetCoordinator();

    private static void ResetCoordinator()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        coordinator.UpdateActiveSnapshot(null, null, null, null, null);
        coordinator.UpdateCachedUiContext(null);
        coordinator.MarkContextFresh();
    }

    private static object CreateSimulatedPlayerRun() => new
    {
        success = false,
        errors = new[] { "Solver divergence at Component #14", "Recursion limit hit" },
        warnings = new[] { "Input tolerance 0.05 exceeds standard threshold" },
        rejectedInputs = new[]
        {
            new PlayerInputFailure("BeamSpan", "Expected float, got string"),
            new PlayerInputFailure("SectionCode", "Profile not in catalog")
        },
        durationMs = 342,
        error = "Player execution failed during iteration"
    };

    [Fact]
    public void N8_PlayerRevitRollback_PreservesCompleteDiagnosticsPayload()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("test script", "auto", false) { Label = "PlayerDiagnostics" };
        using var cts = new CancellationTokenSource();
        var simulatedPlayerRun = CreateSimulatedPlayerRun();

        var execRes = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) =>
            {
                var elem = BridgeJson.ToElement(simulatedPlayerRun) ?? default;
                var err = elem.TryGetProperty("error", out var e) ? e.GetString() : "Player failed";
                throw new InvalidOperationException(err);
            }, startAutoTransaction: false);

        if (execRes.IsError)
            execRes.Value = GrasshopperExecutionEngine.MergeOutcomeValue(simulatedPlayerRun, execRes.RolledBack, execRes.Message);

        Assert.True(execRes.IsError);
        Assert.True(execRes.RolledBack);
        Assert.NotNull(execRes.Value);

        var val = execRes.Value.Value;
        Assert.True(val.TryGetProperty("errors", out var errs));
        Assert.Equal(2, errs.GetArrayLength());

        Assert.True(val.TryGetProperty("warnings", out var warns));
        Assert.Single(warns.EnumerateArray());

        Assert.True(val.TryGetProperty("rejectedInputs", out var rejs));
        Assert.Equal(2, rejs.GetArrayLength());

        Assert.True(val.TryGetProperty("durationMs", out var dur));
        Assert.Equal(342, dur.GetInt64());

        Assert.True(val.TryGetProperty("rolledBack", out var rb));
        Assert.True(rb.GetBoolean());
    }

    [Fact]
    public void N8_PublicCanvasReadResult_SerializesStrictCamelCaseOverPipe()
    {
        var sampleDoc = new { documentId = "doc-guid-1", name = "TestCanvas.gh", objectCount = 1 };
        var sampleComponents = new List<object>
        {
            new { guid = "c-1", name = "Addition", nickName = "Add", category = "Maths" }
        };
        var sampleWires = new List<object>
        {
            new { fromComponent = "c-1", fromParam = "p-1", toComponent = "c-2", toParam = "p-2" }
        };

        var readResult = new CanvasReader.CanvasReadResult(sampleDoc, sampleComponents, sampleWires);
        var execResult = GrasshopperExecutionEngine.MapToExecuteResult(readResult);

        Assert.False(execResult.IsError);
        Assert.NotNull(execResult.Value);

        var jsonElem = execResult.Value.Value;
        Assert.True(jsonElem.TryGetProperty("document", out var docElem));
        Assert.False(jsonElem.TryGetProperty("Document", out _));
        Assert.True(jsonElem.TryGetProperty("components", out var compElem));
        Assert.False(jsonElem.TryGetProperty("Components", out _));
        Assert.True(jsonElem.TryGetProperty("wires", out var wireElem));
        Assert.False(jsonElem.TryGetProperty("Wires", out _));

        var pipeSerialized = BridgeJson.Serialize(execResult);
        Assert.Contains("\"document\":", pipeSerialized);
        Assert.DoesNotContain("\"Document\":", pipeSerialized);
        Assert.Contains("\"components\":", pipeSerialized);
        Assert.DoesNotContain("\"Components\":", pipeSerialized);
        Assert.Contains("\"wires\":", pipeSerialized);
        Assert.DoesNotContain("\"Wires\":", pipeSerialized);
    }

    [Fact]
    public void N9_BatchEdit_ComponentsToAddAsObject_PreflightRejects_ZeroMutations()
    {
        using var jsonDoc = JsonDocument.Parse(
            """
            {
                "componentsToAdd": { "type": "NumberSlider", "x": 100.0, "y": 200.0 },
                "componentsToRemove": [ "6ba7b810-9dad-11d1-80b4-00c04fd430c8" ],
                "wiresToConnect": [ { "source": "A", "target": "B" } ]
            }
            """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "componentsToAdd" &&
            e.Reason.Contains("must be a JSON array", StringComparison.OrdinalIgnoreCase));

        var editor = new CanvasEditor();
        var outcome = editor.ExecuteBatch(null, jsonDoc.RootElement, dryRun: false);
        var outcomeElem = BridgeJson.ToElement(outcome) ?? default;

        Assert.True(outcomeElem.TryGetProperty("success", out var succ));
        Assert.False(succ.GetBoolean());
        Assert.True(outcomeElem.TryGetProperty("error", out var errStr));
        Assert.Contains("No active Grasshopper document", errStr.GetString());
    }

    [Fact]
    public void N9_BatchEdit_PrimitiveAndMalformedItemsInsideArray_StructuredErrors()
    {
        using var jsonDoc = JsonDocument.Parse(
            """
            {
                "componentsToAdd": [ "string_not_obj", 12345, true ],
                "componentsToRemove": [ 9999, true, { "invalid": "obj" } ],
                "wiresToConnect": [ "bad_wire", 777 ],
                "wiresToDisconnect": [ false, 888 ]
            }
            """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);
        Assert.False(preflight.IsValid);

        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToAdd" && e.Reason.Contains("must be a JSON object"));
        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToRemove" && e.Reason.Contains("must be a GUID string"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("must be a JSON object"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToDisconnect" && e.Reason.Contains("must be a JSON object"));

        foreach (var error in preflight.Errors)
        {
            Assert.False(string.IsNullOrWhiteSpace(error.Operation));
            Assert.True(error.Index >= 0);
            Assert.False(string.IsNullOrWhiteSpace(error.Reason));
        }
    }

    [Fact]
    public void N10_DocumentTitleSwitch_NullUiApp_IsolatesStaleFactsFromOldDocument()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var oldContext = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            DocTitle = "ProjectOld.rvt",
            DocPath = "C:\\ProjectOld.rvt",
            Units = new UnitsInfo("inches"),
            Selection = new[] { new ElementInfo(404, "Columns", "Col-Old") },
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "OldDef.gh", "C:\\OldDef.gh", 25, 12, 3, false, true)
        };
        coordinator.UpdateCachedUiContext(oldContext);

        coordinator.UpdateActiveSnapshot("ProjectNew.rvt", "C:\\ProjectNew.rvt", Guid.NewGuid(), "NewDef.gh", "C:\\NewDef.gh");

        var result = GrasshopperContextExtractor.Extract(null, coordinator, includeSelection: true, executionEnabled: true);

        Assert.Equal("ProjectNew.rvt", result.DocTitle);
        Assert.Equal("unavailable", result.Units.Length);
        Assert.NotEqual("inches", result.Units.Length);
        Assert.Empty(result.Selection);
        Assert.NotEqual("OldDef.gh", result.Grasshopper?.ActiveDefinitionName);
        Assert.Equal(0, result.Grasshopper?.ComponentCount);
        Assert.Equal(0, result.Grasshopper?.WireCount);
        Assert.False(result.Grasshopper?.IsEditorLoaded);
        Assert.True(coordinator.IsContextUnavailable);
        Assert.True(coordinator.IsContextStale);
    }

    [Fact]
    public void N10_ContextMarkersAndTimestamps_WhenStaleOrUnavailable()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var cached = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            DocTitle = "SameDoc.rvt",
            DocPath = "C:\\SameDoc.rvt",
            Units = new UnitsInfo("mm"),
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "Same.gh", "C:\\Same.gh", 1, 1, 0, false, true)
        };
        coordinator.UpdateCachedUiContext(cached);
        coordinator.SetActiveDocumentTitle("SameDoc.rvt");

        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        Assert.Equal("SameDoc.rvt", result.DocTitle);
        Assert.True(coordinator.IsContextStale);
        Assert.NotNull(coordinator.LastSnapshotTimestamp);
        Assert.True((DateTimeOffset.UtcNow - coordinator.LastSnapshotTimestamp.Value).TotalSeconds < 5);

        coordinator.UpdateCachedUiContext(null);
        coordinator.SetActiveDocumentTitle("UncachedDoc.rvt");

        var uncachedResult = GrasshopperContextExtractor.Extract(null, coordinator, false, true);
        Assert.Equal("unavailable", uncachedResult.Units.Length);
        Assert.True(coordinator.IsContextUnavailable);
        Assert.True(coordinator.IsContextStale);
    }
}
