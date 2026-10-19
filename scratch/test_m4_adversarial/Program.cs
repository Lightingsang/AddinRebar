using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Recovery;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.Messages;

namespace AdversarialM4Test;

class Program
{
    static int _passed = 0;
    static int _failed = 0;

    static void Assert(bool condition, string testName, string? details = null)
    {
        if (condition)
        {
            Console.WriteLine($"[PASS] {testName}");
            _passed++;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {testName}: {details ?? "Condition false"}");
            Console.ResetColor();
            _failed++;
        }
    }

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Milestone 4 Adversarial Stress Testing ===");

        TestWireFormatBackwardCompatibility();
        TestDictionaryKeySerialization();
        await TestNoOpTargetUniqueIdSemantics();
        await TestSkipCodesPolicy();
        await TestAmbiguousModelStateRecovery();

        Console.WriteLine($"\n=== Test Results: {_passed} Passed, {_failed} Failed ===");
        return _failed == 0 ? 0 : 1;
    }

    static void TestWireFormatBackwardCompatibility()
    {
        Console.WriteLine("\n--- 1. Wire Format Backward Compatibility ---");

        // Null provenance handles
        var keyNullProv = new CompositeSourceKey("DocA", CoordinationCategory.Column, "C1", null);
        string jsonNull = JsonSerializer.Serialize(keyNullProv);
        Assert(jsonNull == "\"[DocA]#[Column]:C1\"", "Null provenance serializes to canonical string", jsonNull);

        // Empty provenance handles
        var keyEmptyProv = new CompositeSourceKey("DocA", CoordinationCategory.Beam, "B1", Array.Empty<string>());
        string jsonEmpty = JsonSerializer.Serialize(keyEmptyProv);
        Assert(jsonEmpty == "\"[DocA]#[Beam]:B1\"", "Empty provenance array serializes to canonical string", jsonEmpty);

        // Non-empty provenance handles
        var keyWithProv = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1", "h_axis", "h_block");
        string jsonWithProv = JsonSerializer.Serialize(keyWithProv);
        Assert(jsonWithProv.StartsWith("{") && jsonWithProv.Contains("\"provenanceHandles\":[\"h_axis\",\"h_block\"]"),
            "Non-empty provenance serializes as JSON object with array", jsonWithProv);

        // Roundtrip non-empty provenance
        var restoredWithProv = JsonSerializer.Deserialize<CompositeSourceKey>(jsonWithProv);
        Assert(restoredWithProv != null && restoredWithProv.ProvenanceHandles.Count == 2 &&
               restoredWithProv.ProvenanceHandles[0] == "h_axis" && restoredWithProv.ProvenanceHandles[1] == "h_block",
            "Roundtrip preserves all provenance handles");

        // Deserializing legacy string format
        string legacyStringJson = "\"[LegacyDoc]#[Wall]:W100\"";
        var restoredLegacy = JsonSerializer.Deserialize<CompositeSourceKey>(legacyStringJson);
        Assert(restoredLegacy != null && restoredLegacy.DocIdentifier == "LegacyDoc" &&
               restoredLegacy.Category == CoordinationCategory.Wall && restoredLegacy.PrimaryHandle == "W100" &&
               restoredLegacy.ProvenanceHandles.Count == 0,
            "Deserializing legacy canonical string works and yields empty provenance");

        // Deserializing plain string format without brackets
        string plainStringJson = "\"PlainDoc#Slab:SL_1\"";
        var restoredPlain = JsonSerializer.Deserialize<CompositeSourceKey>(plainStringJson);
        Assert(restoredPlain != null && restoredPlain.DocIdentifier == "PlainDoc" &&
               restoredPlain.Category == CoordinationCategory.Slab && restoredPlain.PrimaryHandle == "SL_1",
            "Deserializing plain string format works");

        // Deserializing empty string
        var restoredEmpty = JsonSerializer.Deserialize<CompositeSourceKey>("\"\"");
        Assert(restoredEmpty == CompositeSourceKey.Empty, "Deserializing empty string returns CompositeSourceKey.Empty");
    }

    static void TestDictionaryKeySerialization()
    {
        Console.WriteLine("\n--- 2. Dictionary Key Serialization ---");

        var dict = new Dictionary<CompositeSourceKey, string>
        {
            [CompositeSourceKey.Create("Doc1", CoordinationCategory.Column, "C1")] = "uid-c1",
            [CompositeSourceKey.Create("Doc2", CoordinationCategory.Beam, "B2", "h1", "h2")] = "uid-b2"
        };

        string dictJson = JsonSerializer.Serialize(dict);
        Assert(dictJson.Contains("\"[Doc1]#[Column]:C1\":\"uid-c1\""), "Dictionary key 1 serialized as string property name", dictJson);
        Assert(dictJson.Contains("\"[Doc2]#[Beam]:B2\":\"uid-b2\""), "Dictionary key 2 with provenance serialized as string property name", dictJson);

        var restoredDict = JsonSerializer.Deserialize<Dictionary<CompositeSourceKey, string>>(dictJson);
        Assert(restoredDict != null && restoredDict.Count == 2, "Deserialized dictionary contains 2 keys");

        var lookupKey1 = CompositeSourceKey.Create("Doc1", CoordinationCategory.Column, "C1");
        Assert(restoredDict!.ContainsKey(lookupKey1) && restoredDict[lookupKey1] == "uid-c1", "Lookup key 1 succeeds in deserialized dictionary");

        var lookupKey2 = CompositeSourceKey.Create("Doc2", CoordinationCategory.Beam, "B2");
        Assert(restoredDict.ContainsKey(lookupKey2) && restoredDict[lookupKey2] == "uid-b2", "Lookup key 2 succeeds in deserialized dictionary");
    }

    static async Task TestNoOpTargetUniqueIdSemantics()
    {
        Console.WriteLine("\n--- 3. NoOp TargetUniqueId & Whitespace/Case Insensitivity ---");

        // Case A: NoOp with empty TargetUniqueId -> rejected with ERR_MISSING_TARGET_UNIQUE_ID
        {
            var plan = CreateTestPlan(("Column", "C1", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);
            Assert(!result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Failed &&
                   result.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                "NoOp with empty TargetUniqueId rejected with ERR_MISSING_TARGET_UNIQUE_ID");
        }

        // Case B: NoOp with whitespace-only TargetUniqueId -> rejected with ERR_MISSING_TARGET_UNIQUE_ID
        {
            var plan = CreateTestPlan(("Column", "C1", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "   \t\n  " }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);
            Assert(!result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Failed &&
                   result.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                "NoOp with whitespace-only TargetUniqueId rejected with ERR_MISSING_TARGET_UNIQUE_ID");
        }

        // Case C: NoOp with matching TargetUniqueId (different casing) -> accepted as NoOp
        {
            var plan = CreateTestPlan(("Column", "C1", "3a334139-3993-41a4-bba9-1f4a413efb8e-00023fa0"));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp",
                      targetUniqueId = "3A334139-3993-41A4-BBA9-1F4A413EFB8E-00023FA0" } // uppercase
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);
            Assert(result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.NoOp,
                "NoOp with case-insensitive matching TargetUniqueId accepted successfully");
        }

        // Case E: TargetUniqueId with leading/trailing whitespace
        {
            var plan = CreateTestPlan(("Column", "C1", "uid-1"));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "  uid-1  " }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);
            // Note: Does string.Equals match "  uid-1  " with "uid-1"? No, unless trimmed.
            bool isMismatch = result.ElementRecords[0].ErrorCode == "ERR_NOOP_TARGET_MISMATCH";
            Assert(isMismatch, "TargetUniqueId with un-trimmed whitespace treated as mismatch (strict literal string matching)");
        }

        // Case F: Special characters and unicode in ProvenanceHandles
        {
            var keySpecial = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1", "handle/with:colons", "tiếng_việt_cột", "handle\"quote");
            string jsonSpecial = JsonSerializer.Serialize(keySpecial);
            var restoredSpecial = JsonSerializer.Deserialize<CompositeSourceKey>(jsonSpecial);
            Assert(restoredSpecial != null && restoredSpecial.ProvenanceHandles.Count == 3 &&
                   restoredSpecial.ProvenanceHandles[1] == "tiếng_việt_cột",
                "ProvenanceHandles with unicode and special characters roundtrip cleanly");
        }

        // Case G: JSON with provenanceHandles = null
        {
            string jsonWithNullArray = """
            {
                "docIdentifier": "DocA",
                "category": "Column",
                "primaryHandle": "C1",
                "provenanceHandles": null
            }
            """;
            var restoredNullArray = JsonSerializer.Deserialize<CompositeSourceKey>(jsonWithNullArray);
            Assert(restoredNullArray != null && restoredNullArray.ProvenanceHandles.Count == 0,
                "JSON with provenanceHandles: null deserializes safely with empty provenance list");
        }
    }

    static async Task TestSkipCodesPolicy()
    {
        Console.WriteLine("\n--- 4. Permitted Skip Codes & Policy ---");

        string[] standardValidCodes = {
            "SKIP_FROZEN_ZONE",
            "skip_frozen_zone", // test lowercase
            "SKIP_STAGE_DISABLED",
            "SKIP_PRECONDITION_FAILED",
            "SKIP_PREREQUISITE_FAILED",
            "SKIP_USER_EXCLUDED"
        };

        foreach (var code in standardValidCodes)
        {
            var plan = CreateTestPlan(("Column", "C1", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = code, targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);
            Assert(result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Skipped,
                $"Skip with standard code '{code}' accepted as Skipped");
        }

        // Substring / casual mention in error text must be rejected
        {
            var plan = CreateTestPlan(("Column", "C1", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped",
                      errorCode = "ERR_RANDOM", errorReason = "Something failed during SKIP_FROZEN_ZONE check", targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);
            Assert(!result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Failed &&
                   result.ElementRecords[0].ErrorCode == "ERR_INTEGRITY_DEFECT_SKIPPED",
                "Skip with casual substring in errorReason rejected with ERR_INTEGRITY_DEFECT_SKIPPED");
        }

        // Plan item with SkipAllowed = true permits custom skip code
        {
            var plan = CreateTestPlan(("Column", "C1", null));
            plan = plan with
            {
                Items = new[] { plan.Items[0] with { Parameters = new Dictionary<string, object> { ["SkipAllowed"] = true } } }
            };
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped",
                      errorCode = "CUSTOM_BUSINESS_SKIP", targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);
            Assert(result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Skipped,
                "Plan item with SkipAllowed=true permits custom skip reason");
        }

        // Plan item without SkipAllowed rejects custom skip code
        {
            var plan = CreateTestPlan(("Column", "C1", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped",
                      errorCode = "CUSTOM_UNAPPROVED_SKIP", targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);
            Assert(!result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Failed &&
                   result.ElementRecords[0].ErrorCode == "ERR_INTEGRITY_DEFECT_SKIPPED",
                "Plan item without SkipAllowed rejects custom skip code");
        }
    }

    static async Task TestAmbiguousModelStateRecovery()
    {
        Console.WriteLine("\n--- 5. Ambiguous Model State Recovery (OutcomeUnknown) ---");

        // When host did NOT roll back (RolledBack == false), client defect causes OutcomeUnknown
        {
            var plan = CreateTestPlan(("Column", "C1", null), ("Column", "C2", null));
            var payload = new object[]
            {
                // C1 has defect: Created with empty targetUniqueId
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "" },
                // C2 is normal Created
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: false));
            var result = await engine.ExecutePlanAsync(plan);

            Assert(!result.Success, "Execution fails when defect present");
            Assert(result.IsOutcomeUnknown, "Overall result is marked IsOutcomeUnknown = true");

            var c1 = result.ElementRecords.First(r => r.Mark == "C1");
            Assert(c1.Status == ElementExecutionStatus.Failed && c1.ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                "Defective item C1 marked Failed with ERR_MISSING_TARGET_UNIQUE_ID");

            var c2 = result.ElementRecords.First(r => r.Mark == "C2");
            Assert(c2.Status == ElementExecutionStatus.OutcomeUnknown && c2.ErrorCode == "ERR_POST_DISPATCH_INTEGRITY_DEFECT",
                "Non-defective item C2 marked OutcomeUnknown with ERR_POST_DISPATCH_INTEGRITY_DEFECT");
        }

        // When host DID roll back (RolledBack == true), non-defective items get ERR_BATCH_ROLLED_BACK
        {
            var plan = CreateTestPlan(("Column", "C1", null), ("Column", "C2", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
            };
            var engine = new BimPipelineEngine(new MockRevitPipeClient(payload, rolledBack: true));
            var result = await engine.ExecutePlanAsync(plan);

            Assert(!result.Success, "Execution fails when batch rolled back");
            Assert(!result.IsOutcomeUnknown, "IsOutcomeUnknown is FALSE when host confirmed rollback");

            var c2 = result.ElementRecords.First(r => r.Mark == "C2");
            Assert(c2.Status == ElementExecutionStatus.Failed && c2.ErrorCode == "ERR_BATCH_ROLLED_BACK",
                "Non-defective item C2 marked Failed with ERR_BATCH_ROLLED_BACK when host confirmed rollback");
        }
    }

    static BimExecutionPlan CreateTestPlan(params (string category, string mark, string? targetUid)[] items)
    {
        var planItems = items.Select((it, idx) =>
        {
            Enum.TryParse<CoordinationCategory>(it.category, true, out var cat);
            return new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create("Doc", cat, it.mark),
                Category = cat,
                Mark = it.mark,
                TargetUniqueId = it.targetUid ?? string.Empty,
                SequenceOrder = idx + 1
            };
        }).ToList();

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "adversarial-test-hash",
            Items = planItems,
            Options = new ExecutionOptions { DryRun = false }
        };
    }
}

class MockRevitPipeClient : IRevitPipeClient
{
    private readonly object _recordsPayload;
    private readonly bool _rolledBack;

    public MockRevitPipeClient(object recordsPayload, bool rolledBack)
    {
        _recordsPayload = recordsPayload;
        _rolledBack = rolledBack;
    }

    public bool IsConnected => true;
    public string PipeName => "mock-pipe";

    public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DisconnectAsync() => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public void Dispose() { }

    public Task<bool> PingAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<JsonElement?> GetContextAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> CancelAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(new { records = _recordsPayload });
        var doc = JsonDocument.Parse(json);
        return Task.FromResult(new ExecuteResult
        {
            IsError = false,
            RolledBack = _rolledBack,
            Value = doc.RootElement
        });
    }

    public Task<TResult> ExecuteAsync<TResult>(ExecuteRequest request, CancellationToken cancellationToken = default)
        where TResult : class => throw new NotImplementedException();
}
