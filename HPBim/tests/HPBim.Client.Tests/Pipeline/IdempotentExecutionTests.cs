namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class IdempotentExecutionTests
{
    private sealed class SimulatedRevitStore
    {
        private long _nextId = 1000;
        public Dictionary<string, (string Mark, string TypeName, long ElementId)> Elements { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public ExecuteResult HandleExecute(ExecuteRequest req)
        {
            var records = new List<Dictionary<string, object>>();
            if (req.Args.HasValue && req.Args.Value.TryGetProperty("items", out var itemsElem))
            {
                foreach (var item in itemsElem.EnumerateArray())
                {
                    records.Add(ProcessItem(item));
                }
            }

            var rootObj = new Dictionary<string, object>
            {
                ["total"] = records.Count,
                ["created"] = records.Count(r => (string)r["status"] == "Created"),
                ["updated"] = records.Count(r => (string)r["status"] == "Updated"),
                ["noOp"] = records.Count(r => (string)r["status"] == "NoOp"),
                ["records"] = records
            };

            return new ExecuteResult
            {
                IsError = false,
                RolledBack = req.DryRun,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(rootObj)).RootElement
            };
        }

        private Dictionary<string, object> ProcessItem(JsonElement item)
        {
            string sourceKey = ExtractSourceKey(item);
            string category = item.TryGetProperty("category", out var cp) ? cp.GetString() ?? "Column" : "Column";
            string mark = item.TryGetProperty("mark", out var m) ? m.GetString() ?? "" : "";
            string targetUid = item.TryGetProperty("targetUniqueId", out var tu) ? tu.GetString() ?? "" : "";
            string reqType = item.TryGetProperty("resource", out var res) && res.TryGetProperty("typeName", out var tn) ? tn.GetString() ?? "" : "";

            if (!string.IsNullOrEmpty(targetUid) && Elements.TryGetValue(targetUid, out var existing))
            {
                bool changed = !string.IsNullOrEmpty(reqType) && !string.Equals(reqType, existing.TypeName, StringComparison.OrdinalIgnoreCase);
                if (changed) Elements[targetUid] = (mark, reqType, existing.ElementId);
                return MakeRecord(sourceKey, category, mark, changed ? "Updated" : "NoOp", targetUid, existing.ElementId);
            }

            long elemId = ++_nextId;
            string newUid = $"uid-{mark}-{elemId}";
            Elements[newUid] = (mark, reqType, elemId);
            return MakeRecord(sourceKey, category, mark, "Created", newUid, elemId);
        }

        private static string ExtractSourceKey(JsonElement item)
        {
            if (!item.TryGetProperty("sourceKey", out var sk)) return "";
            if (sk.ValueKind == JsonValueKind.String) return sk.GetString() ?? "";
            if (sk.ValueKind != JsonValueKind.Object) return "";

            string doc = sk.TryGetProperty("docIdentifier", out var d) ? d.GetString() ?? "" : "";
            string cat = sk.TryGetProperty("category", out var c) ? c.ToString() : "Column";
            string handle = sk.TryGetProperty("primaryHandle", out var h) ? h.GetString() ?? "" : "";
            return $"[{doc}]#[{cat}]:{handle}";
        }

        private static Dictionary<string, object> MakeRecord(
            string sourceKey, string category, string mark, string status, string uid, long elemId) =>
            new()
            {
                ["sourceKey"] = sourceKey,
                ["category"] = category,
                ["mark"] = mark,
                ["status"] = status,
                ["targetUniqueId"] = uid,
                ["targetElementId"] = elemId
            };
    }

    private static BimExecutionPlan CreateSamplePlan(string col1Type = "Col-220x220") =>
        new()
        {
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
                    Category = CoordinationCategory.Column,
                    Mark = "C1",
                    TargetLevelName = "Level 1",
                    Resource = new ResourceResolutionSpec { TypeName = col1Type }
                },
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C2"),
                    Category = CoordinationCategory.Column,
                    Mark = "C2",
                    TargetLevelName = "Level 1",
                    Resource = new ResourceResolutionSpec { TypeName = "Col-220x220" }
                },
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
                    Category = CoordinationCategory.Beam,
                    Mark = "B1",
                    TargetLevelName = "Level 1",
                    Resource = new ResourceResolutionSpec { TypeName = "Beam-200x350" }
                }
            }
        };

    [Fact]
    public async Task IdempotentExecution_InitialRunThenExactRerun_ProducesZeroDuplicates()
    {
        var store = new SimulatedRevitStore();
        var fakeClient = new FakeRevitPipeClient { CustomExecuteHandler = store.HandleExecute };
        var engine = new BimPipelineEngine(fakeClient);
        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("TestProject");

        var plan = CreateSamplePlan();

        // 1. Initial Apply Run
        var res1 = await engine.ExecutePlanAsync(plan, ledgerService);
        Assert.True(res1.Success);
        Assert.Equal(3, res1.CreatedCount);
        Assert.Equal(0, res1.NoOpCount);
        Assert.Equal(3, store.Elements.Count);

        // 2. Exact Rerun
        var res2 = await engine.ExecutePlanAsync(plan, ledgerService);
        Assert.True(res2.Success);
        Assert.Equal(0, res2.CreatedCount);
        Assert.Equal(3, res2.NoOpCount);
        Assert.Equal(0, res2.UpdatedCount);
        Assert.Equal(3, store.Elements.Count); // Zero new elements in host
    }

    [Fact]
    public async Task IdempotentExecution_WhenElementDeletedInHost_AutomaticallyRecreatesIt()
    {
        var store = new SimulatedRevitStore();
        var fakeClient = new FakeRevitPipeClient { CustomExecuteHandler = store.HandleExecute };
        var engine = new BimPipelineEngine(fakeClient);
        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("TestProject");

        var plan = CreateSamplePlan();
        var res1 = await engine.ExecutePlanAsync(plan, ledgerService);
        Assert.True(res1.Success);

        // Delete C2 from Revit store
        var c2Key = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C2");
        Assert.True(ledgerService.TryGetTarget(c2Key, out var c2Uid));
        store.Elements.Remove(c2Uid!);

        // Rerun plan: C2 recreated, C1 and B1 NoOp
        var res2 = await engine.ExecutePlanAsync(plan, ledgerService);
        Assert.True(res2.Success);
        Assert.Equal(1, res2.CreatedCount);
        Assert.Equal(2, res2.NoOpCount);

        Assert.True(ledgerService.TryGetTarget(c2Key, out var newC2Uid));
        Assert.NotEqual(c2Uid, newC2Uid);
    }

    [Fact]
    public async Task IdempotentExecution_WhenTypeModified_UpdatesElementInPlace()
    {
        var store = new SimulatedRevitStore();
        var fakeClient = new FakeRevitPipeClient { CustomExecuteHandler = store.HandleExecute };
        var engine = new BimPipelineEngine(fakeClient);
        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("TestProject");

        var plan1 = CreateSamplePlan("Col-220x220");
        await engine.ExecutePlanAsync(plan1, ledgerService);

        // Rerun with C1 modified to 300x300
        var plan2 = CreateSamplePlan("Col-300x300");
        var res2 = await engine.ExecutePlanAsync(plan2, ledgerService);

        Assert.True(res2.Success);
        Assert.Equal(0, res2.CreatedCount);
        Assert.Equal(1, res2.UpdatedCount);
        Assert.Equal(2, res2.NoOpCount);
    }
}
