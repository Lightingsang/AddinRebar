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

public sealed class IdempotentExecutionStressTests
{
    private sealed class SimulatedRevitStore
    {
        private long _nextId = 2000;
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
                ["failed"] = records.Count(r => (string)r["status"] == "Failed"),
                ["records"] = records
            };

            return new ExecuteResult { IsError = records.Any(r => (string)r["status"] == "Failed"), RolledBack = req.DryRun, Value = JsonDocument.Parse(JsonSerializer.Serialize(rootObj)).RootElement };
        }

        private Dictionary<string, object> ProcessItem(JsonElement item)
        {
            string sourceKey = ExtractSourceKey(item);
            string category = item.TryGetProperty("category", out var cp) ? cp.GetString() ?? "Column" : "Column";
            string mark = item.TryGetProperty("mark", out var m) ? m.GetString() ?? "" : "";
            string targetUid = item.TryGetProperty("targetUniqueId", out var tu) ? tu.GetString() ?? "" : "";
            string reqType = item.TryGetProperty("resource", out var res) && res.TryGetProperty("typeName", out var tn) ? tn.GetString() ?? "" : "";

            if (reqType == "TRIGGER_FAIL")
            {
                return new Dictionary<string, object> { ["sourceKey"] = sourceKey, ["category"] = category, ["mark"] = mark, ["status"] = "Failed", ["errorCode"] = "ERR_SIMULATED", ["errorReason"] = "Simulated failure condition" };
            }

            if (!string.IsNullOrEmpty(targetUid) && Elements.TryGetValue(targetUid, out var existing))
            {
                bool changed = !string.IsNullOrEmpty(reqType) && !string.Equals(reqType, existing.TypeName, StringComparison.OrdinalIgnoreCase);
                if (changed) Elements[targetUid] = (mark, reqType, existing.ElementId);
                return MakeRecord(sourceKey, category, mark, changed ? "Updated" : "NoOp", targetUid, existing.ElementId);
            }

            long elemId = ++_nextId;
            string newUid = $"uid-{category}-{mark}-{elemId}";
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

    private static BimExecutionPlan CreateMultiItemPlan(int count, string docId = "ProjectDoc", string typeSuffix = "TypeA")
    {
        var items = new List<ElementPlanItem>(count);
        for (int i = 1; i <= count; i++)
        {
            var cat = (i % 3) switch
            {
                0 => CoordinationCategory.Column,
                1 => CoordinationCategory.Beam,
                _ => CoordinationCategory.Wall
            };
            string mark = $"{cat.ToString().Substring(0, 1)}{i}";
            items.Add(new ElementPlanItem
            {
                SourceKey = new CompositeSourceKey(docId, cat, mark),
                Category = cat,
                Mark = mark,
                TargetLevelName = "Level 1",
                Resource = new ResourceResolutionSpec { TypeName = $"{cat}-{typeSuffix}" }
            });
        }
        return new BimExecutionPlan { Items = items };
    }

    [Fact]
    public async Task StressTest_MultiRunIdempotency_10ConsecutiveRuns_NoDuplication()
    {
        var store = new SimulatedRevitStore();
        var fakeClient = new FakeRevitPipeClient { CustomExecuteHandler = store.HandleExecute };
        var engine = new BimPipelineEngine(fakeClient);
        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("StressTestProject");

        var plan = CreateMultiItemPlan(20);

        var firstRes = await engine.ExecutePlanAsync(plan, ledgerService);
        Assert.True(firstRes.Success);
        Assert.Equal(20, firstRes.CreatedCount);
        Assert.Equal(0, firstRes.NoOpCount);
        Assert.Equal(20, store.Elements.Count);

        for (int run = 2; run <= 11; run++)
        {
            var nextRes = await engine.ExecutePlanAsync(plan, ledgerService);
            Assert.True(nextRes.Success);
            Assert.Equal(0, nextRes.CreatedCount);
            Assert.Equal(20, nextRes.NoOpCount);
            Assert.Equal(0, nextRes.UpdatedCount);
            Assert.Equal(20, store.Elements.Count);
        }
    }

    [Fact]
    public async Task StressTest_MassDeletion_PartialAndTotal_RecoversCleanly()
    {
        var store = new SimulatedRevitStore();
        var fakeClient = new FakeRevitPipeClient { CustomExecuteHandler = store.HandleExecute };
        var engine = new BimPipelineEngine(fakeClient);
        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("StressTestProject");

        var plan = CreateMultiItemPlan(20);
        await engine.ExecutePlanAsync(plan, ledgerService);

        // Delete 5 elements from host store
        var deletedKeys = new List<CompositeSourceKey>();
        for (int i = 0; i < 5; i++)
        {
            var key = plan.Items[i * 4].SourceKey;
            deletedKeys.Add(key);
            Assert.True(ledgerService.TryGetTarget(key, out var uid));
            store.Elements.Remove(uid!);
        }

        // Run recovery
        var recoverRes = await engine.ExecutePlanAsync(plan, ledgerService);
        Assert.True(recoverRes.Success);
        Assert.Equal(5, recoverRes.CreatedCount);
        Assert.Equal(15, recoverRes.NoOpCount);
        Assert.Equal(20, store.Elements.Count);

        // Subsequent run should be all NoOp
        var followUp = await engine.ExecutePlanAsync(plan, ledgerService);
        Assert.True(followUp.Success);
        Assert.Equal(0, followUp.CreatedCount);
        Assert.Equal(20, followUp.NoOpCount);

        // Total deletion: delete ALL 20 elements
        store.Elements.Clear();
        var totalRecreateRes = await engine.ExecutePlanAsync(plan, ledgerService);
        Assert.True(totalRecreateRes.Success);
        Assert.Equal(20, totalRecreateRes.CreatedCount);
        Assert.Equal(0, totalRecreateRes.NoOpCount);
        Assert.Equal(20, store.Elements.Count);
    }

    [Fact]
    public async Task StressTest_SimultaneousParameterUpdates_AndRestoration()
    {
        var store = new SimulatedRevitStore();
        var fakeClient = new FakeRevitPipeClient { CustomExecuteHandler = store.HandleExecute };
        var engine = new BimPipelineEngine(fakeClient);
        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("StressTestProject");

        var planA = CreateMultiItemPlan(20, typeSuffix: "TypeA");
        await engine.ExecutePlanAsync(planA, ledgerService);

        // Mutate 7 items to TypeB
        var planBItems = planA.Items.Select((it, idx) =>
            idx < 7
                ? it with { Resource = new ResourceResolutionSpec { TypeName = $"{it.Category}-TypeB" } }
                : it).ToList();
        var planB = planA with { Items = planBItems };

        var updateRes = await engine.ExecutePlanAsync(planB, ledgerService);
        Assert.True(updateRes.Success);
        Assert.Equal(0, updateRes.CreatedCount);
        Assert.Equal(7, updateRes.UpdatedCount);
        Assert.Equal(13, updateRes.NoOpCount);
        Assert.Equal(20, store.Elements.Count);

        // Immediate rerun with planB should be all NoOp
        var rerunRes = await engine.ExecutePlanAsync(planB, ledgerService);
        Assert.True(rerunRes.Success);
        Assert.Equal(0, rerunRes.CreatedCount);
        Assert.Equal(0, rerunRes.UpdatedCount);
        Assert.Equal(20, rerunRes.NoOpCount);
    }

    [Fact]
    public async Task StressTest_CaseInsensitiveKeys_PreserveIdempotency()
    {
        var store = new SimulatedRevitStore();
        var fakeClient = new FakeRevitPipeClient { CustomExecuteHandler = store.HandleExecute };
        var engine = new BimPipelineEngine(fakeClient);
        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("StressTestProject");

        var plan1 = CreateMultiItemPlan(1, docId: "ProjectDoc");
        var res1 = await engine.ExecutePlanAsync(plan1, ledgerService);
        Assert.True(res1.Success);
        Assert.Equal(1, res1.CreatedCount);

        // Lowercase doc and handle
        var plan2 = CreateMultiItemPlan(1, docId: "projectdoc");
        var res2 = await engine.ExecutePlanAsync(plan2, ledgerService);
        Assert.True(res2.Success);
        Assert.Equal(0, res2.CreatedCount);
        Assert.Equal(1, res2.NoOpCount);

        // Uppercase doc and handle
        var plan3 = CreateMultiItemPlan(1, docId: "PROJECTDOC");
        var res3 = await engine.ExecutePlanAsync(plan3, ledgerService);
        Assert.True(res3.Success);
        Assert.Equal(0, res3.CreatedCount);
        Assert.Equal(1, res3.NoOpCount);
        Assert.Single(store.Elements);
    }

    [Fact]
    public async Task StressTest_DryRun_DoesNotMutateLedger()
    {
        var store = new SimulatedRevitStore();
        var fakeClient = new FakeRevitPipeClient { CustomExecuteHandler = store.HandleExecute };
        var engine = new BimPipelineEngine(fakeClient);
        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("StressTestProject");

        var dryPlan = CreateMultiItemPlan(5) with
        {
            Options = new ExecutionOptions { DryRun = true }
        };

        var dryRes = await engine.ExecutePlanAsync(dryPlan, ledgerService);
        Assert.True(dryRes.Success);
        Assert.True(dryRes.RolledBack);
        // Ledger mappings must be empty after dry-run
        Assert.Empty(ledgerService.Mappings);

        // Real Apply run
        var applyPlan = CreateMultiItemPlan(5);
        var applyRes = await engine.ExecutePlanAsync(applyPlan, ledgerService);
        Assert.True(applyRes.Success);
        Assert.False(applyRes.RolledBack);
        Assert.Equal(5, applyRes.CreatedCount);
        Assert.Equal(5, ledgerService.Mappings.Count);
    }
}
