using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.Messages;

namespace M4AdversarialHarness;

public sealed class FakePipeClient : IRevitPipeClient
{
    public bool IsConnected { get; set; } = true;
    public string PipeName { get; set; } = "fake-pipe";
    public Func<ExecuteRequest, ExecuteResult>? ExecuteHandler { get; set; }

    public Task<bool> PingAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<JsonElement?> GetContextAsync(CancellationToken cancellationToken = default) => Task.FromResult<JsonElement?>(null);

    public Task<bool> CancelAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public void Dispose() { }

    public ValueTask DisposeAsync() => default;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken = default)
    {
        if (ExecuteHandler != null) return Task.FromResult(ExecuteHandler(request));
        return Task.FromResult(new ExecuteResult { IsError = false, RolledBack = false });
    }
}

public static class Program
{
    private static int _passedCount = 0;
    private static int _failedCount = 0;
    private static readonly List<string> _failures = new();

    private static void Assert(bool condition, string testName, string message = "")
    {
        if (condition)
        {
            _passedCount++;
            Console.WriteLine($"[PASS] {testName}");
        }
        else
        {
            _failedCount++;
            string err = $"[FAIL] {testName}: {message}";
            _failures.Add(err);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(err);
            Console.ResetColor();
        }
    }

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Milestone 4 Adversarial Stress Harness ===");
        var sw = Stopwatch.StartNew();

        TestProvenanceHandlesStress();
        await TestNoOpRejectionStressAsync();
        await TestSkippedSemanticsStressAsync();
        await TestTruthfulRollbackAndDefectBifurcationAsync();
        await TestLedgerPopulatedNoOpMismatchAsync();

        sw.Stop();
        Console.WriteLine("\n=== Stress Harness Summary ===");
        Console.WriteLine($"Passed: {_passedCount}");
        Console.WriteLine($"Failed: {_failedCount}");
        Console.WriteLine($"Duration: {sw.ElapsedMilliseconds} ms");

        if (_failures.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nFailures Detail:");
            foreach (var f in _failures) Console.WriteLine(f);
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nALL ADVERSARIAL CHALLENGES COMPLETED SUCCESSFULLY!");
        Console.ResetColor();
        return 0;
    }

    private static void TestProvenanceHandlesStress()
    {
        Console.WriteLine("\n--- Testing ProvenanceHandles Serialization / Deserialization Stress ---");
        var options = new JsonSerializerOptions();

        // 1. Large number of handles (100 handles)
        var largeHandles = Enumerable.Range(1, 100).Select(i => $"Handle_Entity_{i:D4}").ToList();
        var keyLarge = CompositeSourceKey.Create("Doc1.dwg", CoordinationCategory.Column, "Col100", largeHandles.ToArray());
        string jsonLarge = JsonSerializer.Serialize(keyLarge, options);
        var resLarge = JsonSerializer.Deserialize<CompositeSourceKey>(jsonLarge, options);
        Assert(resLarge != null && resLarge.ProvenanceHandles.Count == 100,
            "ProvenanceHandles.Large100", $"Expected 100 handles, got {resLarge?.ProvenanceHandles.Count}");
        Assert(resLarge != null && resLarge.ProvenanceHandles.SequenceEqual(largeHandles),
            "ProvenanceHandles.Large100.OrderPreserved", "Order or content was altered in large handle list");

        // 2. Duplicate handles in list
        var dupHandles = new[] { "H1", "H1", "H2", "H3", "H2" };
        var keyDup = CompositeSourceKey.Create("Doc1.dwg", CoordinationCategory.Beam, "B1", dupHandles);
        string jsonDup = JsonSerializer.Serialize(keyDup, options);
        var resDup = JsonSerializer.Deserialize<CompositeSourceKey>(jsonDup, options);
        Assert(resDup != null && resDup.ProvenanceHandles.Count == 5 && resDup.ProvenanceHandles[0] == "H1" && resDup.ProvenanceHandles[1] == "H1",
            "ProvenanceHandles.DuplicatesPreserved", "Duplicates or ordering were collapsed");

        // 3. Empty and whitespace handle items
        var spaceHandles = new[] { "Valid1", "", "   ", "Valid2" };
        var keySpace = CompositeSourceKey.Create("Doc1.dwg", CoordinationCategory.Wall, "W1", spaceHandles);
        string jsonSpace = JsonSerializer.Serialize(keySpace, options);
        var resSpace = JsonSerializer.Deserialize<CompositeSourceKey>(jsonSpace, options);
        Assert(resSpace != null && resSpace.ProvenanceHandles.Count == 4 && resSpace.ProvenanceHandles[1] == "" && resSpace.ProvenanceHandles[2] == "   ",
            "ProvenanceHandles.EmptyAndWhitespaceStrings", "Empty or whitespace strings were stripped or altered");

        // 4. Special characters: Quotes, slashes, brackets, colons, unicode, newlines
        var specialHandles = new[]
        {
            "Handle\"With\"Quotes",
            "Handle\\With\\Backslashes",
            "Handle/With/ForwardSlashes",
            "[Doc]#[Col]:123",
            "Cột_Tầng_1_Đài_Móng_ĐC01_tiết_diện_220x220",
            "Line\nBreak\tTab",
            "{\"json\":\"nested\"}"
        };
        var keySpecial = CompositeSourceKey.Create("SpecialDoc.dwg", CoordinationCategory.Pile, "P1", specialHandles);
        string jsonSpecial = JsonSerializer.Serialize(keySpecial, options);
        var resSpecial = JsonSerializer.Deserialize<CompositeSourceKey>(jsonSpecial, options);
        Assert(resSpecial != null && resSpecial.ProvenanceHandles.SequenceEqual(specialHandles),
            "ProvenanceHandles.SpecialCharactersRoundtrip", "Special characters corrupted in JSON roundtrip");

        // 5. Null and Empty ProvenanceHandles behavior
        var keyNull = new CompositeSourceKey("DocN", CoordinationCategory.Slab, "F1", null);
        string jsonNull = JsonSerializer.Serialize(keyNull, options);
        Assert(jsonNull == "\"[DocN]#[Slab]:F1\"", "ProvenanceHandles.NullSerializesCanonicalString",
            $"Expected canonical string, got {jsonNull}");
        var resNull = JsonSerializer.Deserialize<CompositeSourceKey>(jsonNull, options);
        Assert(resNull != null && resNull.ProvenanceHandles != null && resNull.ProvenanceHandles.Count == 0,
            "ProvenanceHandles.NullDeserializesEmptyCollection", "Expected empty collection, got null or non-empty");

        var keyEmpty = new CompositeSourceKey("DocE", CoordinationCategory.Slab, "F2", Array.Empty<string>());
        string jsonEmpty = JsonSerializer.Serialize(keyEmpty, options);
        Assert(jsonEmpty == "\"[DocE]#[Slab]:F2\"", "ProvenanceHandles.EmptyArraySerializesCanonicalString",
            $"Expected canonical string, got {jsonEmpty}");
        var resEmpty = JsonSerializer.Deserialize<CompositeSourceKey>(jsonEmpty, options);
        Assert(resEmpty != null && resEmpty.ProvenanceHandles.Count == 0,
            "ProvenanceHandles.EmptyArrayDeserializesEmpty", "Expected empty list");

        // 6. Deserialization from JSON object with "key" property
        string jsonWithKeyProp = """
        {
            "key": "[MyDoc]#[Footing]:FT-01",
            "provenanceHandles": ["h_grid1", "h_block2"]
        }
        """;
        var resKeyProp = JsonSerializer.Deserialize<CompositeSourceKey>(jsonWithKeyProp, options);
        Assert(resKeyProp != null && resKeyProp.DocIdentifier == "MyDoc" && resKeyProp.Category == CoordinationCategory.Footing &&
               resKeyProp.PrimaryHandle == "FT-01" && resKeyProp.ProvenanceHandles.Count == 2,
            "ProvenanceHandles.DeserializeObjectWithKeyProperty", "Failed to deserialize object with 'key' property");

        // 7. Deserialization from JSON object with "sourceKey" property
        string jsonWithSourceKeyProp = """
        {
            "sourceKey": "DocPlain#Wall:W-10",
            "provenanceHandles": ["ph_cad1"]
        }
        """;
        var resSourceKeyProp = JsonSerializer.Deserialize<CompositeSourceKey>(jsonWithSourceKeyProp, options);
        Assert(resSourceKeyProp != null && resSourceKeyProp.DocIdentifier == "DocPlain" && resSourceKeyProp.Category == CoordinationCategory.Wall &&
               resSourceKeyProp.PrimaryHandle == "W-10" && resSourceKeyProp.ProvenanceHandles.Count == 1,
            "ProvenanceHandles.DeserializeObjectWithSourceKeyProperty", "Failed to deserialize object with 'sourceKey' property");

        // 8. Deserialization with Numeric Category
        string jsonNumericCat = """
        {
            "docIdentifier": "NumDoc",
            "category": 3,
            "primaryHandle": "GB100",
            "provenanceHandles": ["h1"]
        }
        """;
        var resNumCat = JsonSerializer.Deserialize<CompositeSourceKey>(jsonNumericCat, options);
        Assert(resNumCat != null && resNumCat.Category == CoordinationCategory.GradeBeam && resNumCat.ProvenanceHandles.Count == 1,
            "ProvenanceHandles.DeserializeNumericCategory", $"Expected GradeBeam (3), got {resNumCat?.Category}");

        // 9. Dictionary<CompositeSourceKey, string> roundtrip and equality
        var dict = new Dictionary<CompositeSourceKey, string>
        {
            [CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1", "h1", "h2")] = "val1",
            [CompositeSourceKey.Create("DocA", CoordinationCategory.Beam, "B1")] = "val2"
        };
        string jsonDict = JsonSerializer.Serialize(dict, options);
        var restoredDict = JsonSerializer.Deserialize<Dictionary<CompositeSourceKey, string>>(jsonDict, options);
        Assert(restoredDict != null && restoredDict.Count == 2, "ProvenanceHandles.DictionaryRoundtripCount",
            $"Expected 2 entries, got {restoredDict?.Count}");

        // Value equality in lookup: lookup with key having different provenance should match because identity is (Doc, Cat, Handle)
        var lookupKey = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1", "different_h");
        Assert(restoredDict != null && restoredDict.TryGetValue(lookupKey, out string? val) && val == "val1",
            "ProvenanceHandles.DictionaryLookupByCoreIdentity", "Dictionary lookup failed with different provenance handle");

        // 10. Mixed JSON array element types in provenanceHandles
        string jsonMixedArray = """
        {
            "docIdentifier": "MixedDoc",
            "category": "Wall",
            "primaryHandle": "W1",
            "provenanceHandles": ["valid_h1", 12345, null, true, {"foo":"bar"}, "valid_h2"]
        }
        """;
        var resMixed = JsonSerializer.Deserialize<CompositeSourceKey>(jsonMixedArray, options);
        Assert(resMixed != null && resMixed.ProvenanceHandles.Count == 2 &&
               resMixed.ProvenanceHandles[0] == "valid_h1" && resMixed.ProvenanceHandles[1] == "valid_h2",
            "ProvenanceHandles.MixedArrayElementsFilterNonStrings",
            $"Expected 2 string elements, got {resMixed?.ProvenanceHandles.Count}");

        // 11. Property name case-insensitivity (UPPERCASE properties)
        string jsonUppercase = """
        {
            "DOCIDENTIFIER": "UpperDoc",
            "CATEGORY": "COLUMN",
            "PRIMARYHANDLE": "UP-10",
            "PROVENANCEHANDLES": ["h_upper"]
        }
        """;
        var resUpper = JsonSerializer.Deserialize<CompositeSourceKey>(jsonUppercase, options);
        Assert(resUpper != null && resUpper.DocIdentifier == "UpperDoc" && resUpper.Category == CoordinationCategory.Column &&
               resUpper.PrimaryHandle == "UP-10" && resUpper.ProvenanceHandles.Count == 1,
            "ProvenanceHandles.UppercasePropertyNames", "Failed to deserialize uppercase JSON properties");

        // 12. Invalid string format throwing JsonException
        bool threwOnBadFormat = false;
        try
        {
            JsonSerializer.Deserialize<CompositeSourceKey>("\"completely-invalid-key-no-category\"", options);
        }
        catch (JsonException)
        {
            threwOnBadFormat = true;
        }
        Assert(threwOnBadFormat, "ProvenanceHandles.InvalidStringThrowsJsonException", "Expected JsonException for malformed key string");

        // 13. Whitespace string returning Empty
        var resWhitespaceKey = JsonSerializer.Deserialize<CompositeSourceKey>("\"     \"", options);
        Assert(resWhitespaceKey == CompositeSourceKey.Empty, "ProvenanceHandles.WhitespaceStringReturnsEmpty",
            "Whitespace string should deserialize to CompositeSourceKey.Empty");
    }

    private static async Task TestNoOpRejectionStressAsync()
    {
        Console.WriteLine("\n--- Testing NoOp Rejection Semantics Stress ---");

        var plan = CreatePlan(("Column", "C1", null));

        // 1. Returned NoOp with null TargetUniqueId -> REJECTED
        var payloadNullUid = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = (string?)null }
        };
        var engine1 = new BimPipelineEngine(CreateFakeClient(payloadNullUid));
        var res1 = await engine1.ExecutePlanAsync(plan);
        Assert(!res1.Success, "NoOp.NullTargetUniqueId.SuccessIsFalse", "Expected failed execution on null TargetUniqueId");
        Assert(res1.ElementRecords[0].Status == ElementExecutionStatus.Failed, "NoOp.NullTargetUniqueId.StatusFailed", "Record status should be Failed");
        Assert(res1.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID", "NoOp.NullTargetUniqueId.ErrorCode",
            $"Expected ERR_MISSING_TARGET_UNIQUE_ID, got {res1.ElementRecords[0].ErrorCode}");

        // 2. Returned NoOp with empty TargetUniqueId -> REJECTED
        var payloadEmptyUid = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "" }
        };
        var engine2 = new BimPipelineEngine(CreateFakeClient(payloadEmptyUid));
        var res2 = await engine2.ExecutePlanAsync(plan);
        Assert(res2.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID", "NoOp.EmptyTargetUniqueId.ErrorCode",
            $"Expected ERR_MISSING_TARGET_UNIQUE_ID, got {res2.ElementRecords[0].ErrorCode}");

        // 3. Returned NoOp with whitespace-only TargetUniqueId -> REJECTED
        var payloadWhitespaceUid = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "   \t  " }
        };
        var engine3 = new BimPipelineEngine(CreateFakeClient(payloadWhitespaceUid));
        var res3 = await engine3.ExecutePlanAsync(plan);
        Assert(res3.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID", "NoOp.WhitespaceTargetUniqueId.ErrorCode",
            $"Expected ERR_MISSING_TARGET_UNIQUE_ID, got {res3.ElementRecords[0].ErrorCode}");

        // 4. Returned NoOp with valid TargetUniqueId when plan item has no TargetUniqueId -> ACCEPTED
        var payloadValidUid = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "revit-unique-id-999" }
        };
        var engine4 = new BimPipelineEngine(CreateFakeClient(payloadValidUid));
        var res4 = await engine4.ExecutePlanAsync(plan);
        Assert(res4.Success, "NoOp.ValidUidNoPlanTarget.Success", "Expected Success on valid TargetUniqueId");
        Assert(res4.ElementRecords[0].Status == ElementExecutionStatus.NoOp, "NoOp.ValidUidNoPlanTarget.StatusNoOp",
            $"Expected NoOp, got {res4.ElementRecords[0].Status}");
        Assert(res4.ElementRecords[0].TargetUniqueId == "revit-unique-id-999", "NoOp.ValidUidNoPlanTarget.TargetIdPreserved", "ID was not preserved");

        // 5. Returned NoOp with Mismatched TargetUniqueId when plan item has expected TargetUniqueId -> REJECTED
        var planWithTarget = CreatePlan(("Column", "C1", "expected-uid-100"));
        var payloadMismatchedUid = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "different-uid-200" }
        };
        var engine5 = new BimPipelineEngine(CreateFakeClient(payloadMismatchedUid));
        var res5 = await engine5.ExecutePlanAsync(planWithTarget);
        Assert(!res5.Success, "NoOp.MismatchedTargetUniqueId.SuccessIsFalse", "Expected failed execution on mismatched TargetUniqueId");
        Assert(res5.ElementRecords[0].Status == ElementExecutionStatus.Failed, "NoOp.MismatchedTargetUniqueId.StatusFailed", "Record status should be Failed");
        Assert(res5.ElementRecords[0].ErrorCode == "ERR_NOOP_TARGET_MISMATCH", "NoOp.MismatchedTargetUniqueId.ErrorCode",
            $"Expected ERR_NOOP_TARGET_MISMATCH, got {res5.ElementRecords[0].ErrorCode}");

        // 6. Returned NoOp with Case-Insensitive Matching TargetUniqueId -> ACCEPTED
        var payloadCaseUid = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "EXPECTED-UID-100" }
        };
        var engine6 = new BimPipelineEngine(CreateFakeClient(payloadCaseUid));
        var res6 = await engine6.ExecutePlanAsync(planWithTarget);
        Assert(res6.Success, "NoOp.CaseInsensitiveTargetUniqueId.Success", "Expected Success on case-insensitive match");
        Assert(res6.ElementRecords[0].Status == ElementExecutionStatus.NoOp, "NoOp.CaseInsensitiveTargetUniqueId.StatusNoOp", "Status should be NoOp");

        // 7. Category mismatch -> REJECTED with ERR_CATEGORY_MISMATCH
        var payloadCatMismatch = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Beam", mark = "C1", status = "Created", targetUniqueId = "uid-1" }
        };
        var engine7 = new BimPipelineEngine(CreateFakeClient(payloadCatMismatch));
        var res7 = await engine7.ExecutePlanAsync(plan);
        Assert(!res7.Success && res7.ElementRecords[0].ErrorCode == "ERR_CATEGORY_MISMATCH",
            "IntegrityDefect.CategoryMismatch", $"Expected ERR_CATEGORY_MISMATCH, got {res7.ElementRecords[0].ErrorCode}");

        // 8. Mark mismatch -> REJECTED with ERR_MARK_MISMATCH
        var payloadMarkMismatch = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C2_WRONG", status = "Created", targetUniqueId = "uid-1" }
        };
        var engine8 = new BimPipelineEngine(CreateFakeClient(payloadMarkMismatch));
        var res8 = await engine8.ExecutePlanAsync(plan);
        Assert(!res8.Success && res8.ElementRecords[0].ErrorCode == "ERR_MARK_MISMATCH",
            "IntegrityDefect.MarkMismatch", $"Expected ERR_MARK_MISMATCH, got {res8.ElementRecords[0].ErrorCode}");
    }

    private static async Task TestSkippedSemanticsStressAsync()
    {
        Console.WriteLine("\n--- Testing Skipped Semantics & Reason Whitelist Stress ---");

        var validCodes = new[]
        {
            "SKIP_FROZEN_ZONE",
            "SKIP_STAGE_DISABLED",
            "SKIP_PRECONDITION_FAILED",
            "SKIP_PREREQUISITE_FAILED",
            "SKIP_USER_EXCLUDED",
            "skip_frozen_zone",
            "Skip_Stage_Disabled"
        };

        foreach (var code in validCodes)
        {
            var plan = CreatePlan(("Column", "C1", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = code }
            };
            var engine = new BimPipelineEngine(CreateFakeClient(payload));
            var res = await engine.ExecutePlanAsync(plan);
            Assert(res.Success && res.ElementRecords[0].Status == ElementExecutionStatus.Skipped,
                $"Skipped.WhitelistedCode.{code}", $"Expected Success & Skipped for valid code '{code}'");
        }

        var invalidCodes = new[]
        {
            "SKIP_UNAPPROVED_REASON",
            "ERR_TIMEOUT",
            "SKIP_CUSTOM",
            "ERR_SKIP_FROZEN_ZONE_FAILED",
            "",
            "   "
        };

        foreach (var code in invalidCodes)
        {
            var plan = CreatePlan(("Column", "C1", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = code }
            };
            var engine = new BimPipelineEngine(CreateFakeClient(payload));
            var res = await engine.ExecutePlanAsync(plan);
            Assert(!res.Success && res.ElementRecords[0].Status == ElementExecutionStatus.Failed &&
                   res.ElementRecords[0].ErrorCode == "ERR_INTEGRITY_DEFECT_SKIPPED",
                $"Skipped.InvalidCodeRejected.{code}", $"Expected ERR_INTEGRITY_DEFECT_SKIPPED for invalid code '{code}'");
        }

        // Substring trick: errorReason contains SKIP_FROZEN_ZONE but errorCode is arbitrary
        {
            var plan = CreatePlan(("Column", "C1", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped",
                      errorCode = "ERR_INTERNAL_ERROR", errorReason = "Error occurred inside SKIP_FROZEN_ZONE handler" }
            };
            var engine = new BimPipelineEngine(CreateFakeClient(payload));
            var res = await engine.ExecutePlanAsync(plan);
            Assert(!res.Success && res.ElementRecords[0].ErrorCode == "ERR_INTEGRITY_DEFECT_SKIPPED",
                "Skipped.SubstringInErrorReasonRejected", "Permissive substring allowed unapproved error code");
        }

        // Plan item parameter SkipAllowed = true permits custom skip reason
        {
            var planItem = new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"),
                Category = CoordinationCategory.Column,
                Mark = "C1",
                Parameters = new Dictionary<string, object> { ["SkipAllowed"] = true }
            };
            var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash", Items = new[] { planItem }, Options = new ExecutionOptions() };
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "Custom engineering skip reason" }
            };
            var engine = new BimPipelineEngine(CreateFakeClient(payload));
            var res = await engine.ExecutePlanAsync(plan);
            Assert(res.Success && res.ElementRecords[0].Status == ElementExecutionStatus.Skipped,
                "Skipped.SkipAllowedParameterPermitsCustomReason", "SkipAllowed=true failed to permit custom skip reason");
        }
    }

    private static async Task TestTruthfulRollbackAndDefectBifurcationAsync()
    {
        Console.WriteLine("\n--- Testing Truthful Rollback & Defect Bifurcation ---");

        // Scenario 1: Host RolledBack == false, item 2 has defect (Missing TargetUniqueId on NoOp)
        // Item 1 (Created) should become OutcomeUnknown with ERR_POST_DISPATCH_INTEGRITY_DEFECT
        // Item 2 (NoOp with missing UID) should be Failed with ERR_MISSING_TARGET_UNIQUE_ID
        // Overall: Success = false, RolledBack = false, IsOutcomeUnknown = true
        {
            var plan = CreatePlan(("Column", "C1", null), ("Column", "C2", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "NoOp", targetUniqueId = "" }
            };
            var client = new FakePipeClient
            {
                ExecuteHandler = _ => new ExecuteResult
                {
                    IsError = false,
                    RolledBack = false,
                    Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
                }
            };
            var engine = new BimPipelineEngine(client);
            var res = await engine.ExecutePlanAsync(plan);

            Assert(!res.Success, "TruthfulRollback.HostNotRolledBack.SuccessIsFalse", "Plan success should be false");
            Assert(!res.RolledBack, "TruthfulRollback.HostNotRolledBack.RolledBackIsFalse", "Plan RolledBack should be false");
            Assert(res.IsOutcomeUnknown, "TruthfulRollback.HostNotRolledBack.IsOutcomeUnknownIsTrue", "Plan IsOutcomeUnknown should be true");

            var c2 = res.ElementRecords.First(r => r.Mark == "C2");
            Assert(c2.Status == ElementExecutionStatus.Failed && c2.ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                "TruthfulRollback.DefectiveItemRetainsDefectCode", $"Expected ERR_MISSING_TARGET_UNIQUE_ID, got {c2.ErrorCode}");

            var c1 = res.ElementRecords.First(r => r.Mark == "C1");
            Assert(c1.Status == ElementExecutionStatus.OutcomeUnknown && c1.ErrorCode == "ERR_POST_DISPATCH_INTEGRITY_DEFECT",
                "TruthfulRollback.NonDefectiveItemReportsOutcomeUnknown",
                $"Expected OutcomeUnknown / ERR_POST_DISPATCH_INTEGRITY_DEFECT, got {c1.Status} / {c1.ErrorCode}");
        }

        // Scenario 2: Host RolledBack == true, item 2 has defect
        // Both items should be Failed; item 1 gets ERR_BATCH_ROLLED_BACK, item 2 gets ERR_MISSING_TARGET_UNIQUE_ID
        // Overall: Success = false, RolledBack = true, IsOutcomeUnknown = false
        {
            var plan = CreatePlan(("Column", "C1", null), ("Column", "C2", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "NoOp", targetUniqueId = "" }
            };
            var client = new FakePipeClient
            {
                ExecuteHandler = _ => new ExecuteResult
                {
                    IsError = false,
                    RolledBack = true,
                    Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
                }
            };
            var engine = new BimPipelineEngine(client);
            var res = await engine.ExecutePlanAsync(plan);

            Assert(!res.Success, "TruthfulRollback.HostRolledBack.SuccessIsFalse", "Plan success should be false");
            Assert(res.RolledBack, "TruthfulRollback.HostRolledBack.RolledBackIsTrue", "Plan RolledBack should be true");
            Assert(!res.IsOutcomeUnknown, "TruthfulRollback.HostRolledBack.IsOutcomeUnknownIsFalse", "Plan IsOutcomeUnknown should be false");

            var c1 = res.ElementRecords.First(r => r.Mark == "C1");
            Assert(c1.Status == ElementExecutionStatus.Failed && c1.ErrorCode == "ERR_BATCH_ROLLED_BACK",
                "TruthfulRollback.HostRolledBack.ReportsBatchRolledBack", $"Expected ERR_BATCH_ROLLED_BACK, got {c1.ErrorCode}");
        }

        // Scenario 3: Response Truncated
        {
            var plan = CreatePlan(("Column", "C1", null));
            var client = new FakePipeClient
            {
                ExecuteHandler = _ => new ExecuteResult
                {
                    IsError = false,
                    Truncated = true
                }
            };
            var engine = new BimPipelineEngine(client);
            var res = await engine.ExecutePlanAsync(plan);

            Assert(!res.Success && res.IsOutcomeUnknown, "TruncatedResponse.OutcomeUnknown", "Truncated response must report OutcomeUnknown");
            Assert(res.ElementRecords[0].ErrorCode == "ERR_RESPONSE_TRUNCATED", "TruncatedResponse.ErrorCode",
                $"Expected ERR_RESPONSE_TRUNCATED, got {res.ElementRecords[0].ErrorCode}");
        }

        // Scenario 4: Foreign Record Returned -> Entire batch failed with ERR_FOREIGN_RECORD
        {
            var plan = CreatePlan(("Column", "C1", null), ("Column", "C2", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" },
                new { sourceKey = "[Doc]#[Column]:CFOREIGN", category = "Column", mark = "CFOREIGN", status = "Created", targetUniqueId = "uid-f" }
            };
            var client = new FakePipeClient
            {
                ExecuteHandler = _ => new ExecuteResult
                {
                    IsError = false,
                    RolledBack = false,
                    Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
                }
            };
            var engine = new BimPipelineEngine(client);
            var res = await engine.ExecutePlanAsync(plan);

            Assert(!res.Success, "BatchDefect.ForeignRecord.SuccessIsFalse", "Expected failed execution on foreign record");
            Assert(res.ElementRecords.All(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode == "ERR_FOREIGN_RECORD"),
                "BatchDefect.ForeignRecord.AllFailedWithErrorCode", "All items should be marked Failed / ERR_FOREIGN_RECORD");
        }

        // Scenario 5: Duplicate Record Returned -> Entire batch failed with ERR_DUPLICATE_RECORD
        {
            var plan = CreatePlan(("Column", "C1", null), ("Column", "C2", null));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1-dup" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
            };
            var client = new FakePipeClient
            {
                ExecuteHandler = _ => new ExecuteResult
                {
                    IsError = false,
                    RolledBack = false,
                    Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
                }
            };
            var engine = new BimPipelineEngine(client);
            var res = await engine.ExecutePlanAsync(plan);

            Assert(!res.Success, "BatchDefect.DuplicateRecord.SuccessIsFalse", "Expected failed execution on duplicate record");
            Assert(res.ElementRecords.All(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode == "ERR_DUPLICATE_RECORD"),
                "BatchDefect.DuplicateRecord.AllFailedWithErrorCode", "All items should be marked Failed / ERR_DUPLICATE_RECORD");
        }

        // Scenario 6: Multiple Distinct Defects in One Batch (Host RolledBack == false)
        // Item 1 (Created, valid) -> OutcomeUnknown (ERR_POST_DISPATCH_INTEGRITY_DEFECT)
        // Item 2 (NoOp with empty UID) -> Failed (ERR_MISSING_TARGET_UNIQUE_ID)
        // Item 3 (NoOp with mismatched UID) -> Failed (ERR_NOOP_TARGET_MISMATCH)
        {
            var plan = CreatePlan(
                ("Column", "C1", null),
                ("Column", "C2", null),
                ("Column", "C3", "expected-c3-uid"));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "NoOp", targetUniqueId = "" },
                new { sourceKey = "[Doc]#[Column]:C3", category = "Column", mark = "C3", status = "NoOp", targetUniqueId = "wrong-c3-uid" }
            };
            var client = new FakePipeClient
            {
                ExecuteHandler = _ => new ExecuteResult
                {
                    IsError = false,
                    RolledBack = false,
                    Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
                }
            };
            var engine = new BimPipelineEngine(client);
            var res = await engine.ExecutePlanAsync(plan);

            Assert(!res.Success && res.IsOutcomeUnknown, "MultipleDefects.OutcomeUnknownIsTrue", "Plan outcome must be unknown");
            var c1 = res.ElementRecords.First(r => r.Mark == "C1");
            var c2 = res.ElementRecords.First(r => r.Mark == "C2");
            var c3 = res.ElementRecords.First(r => r.Mark == "C3");

            Assert(c1.Status == ElementExecutionStatus.OutcomeUnknown && c1.ErrorCode == "ERR_POST_DISPATCH_INTEGRITY_DEFECT",
                "MultipleDefects.NonDefectiveItemReportsOutcomeUnknown", $"Expected OutcomeUnknown, got {c1.Status}");
            Assert(c2.Status == ElementExecutionStatus.Failed && c2.ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                "MultipleDefects.Item2RetainsMissingUidError", $"Expected ERR_MISSING_TARGET_UNIQUE_ID, got {c2.ErrorCode}");
            Assert(c3.Status == ElementExecutionStatus.Failed && c3.ErrorCode == "ERR_NOOP_TARGET_MISMATCH",
                "MultipleDefects.Item3RetainsMismatchError", $"Expected ERR_NOOP_TARGET_MISMATCH, got {c3.ErrorCode}");
        }
    }

    private static async Task TestLedgerPopulatedNoOpMismatchAsync()
    {
        Console.WriteLine("\n--- Testing Ledger Target Population & NoOp Mismatch ---");

        var key = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1");
        var mapping = LedgerElementMapping.Create(key, "ledger-known-uid-42", 100L, CoordinationCategory.Column, "C1", "h1");

        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.RegisterMapping(mapping);

        var plan = CreatePlan(("Column", "C1", null)); // targetUniqueId not populated in plan yet

        // Returned NoOp with mismatched UID against the ledger
        var payloadMismatch = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "some-other-uid-99" }
        };
        var engine1 = new BimPipelineEngine(CreateFakeClient(payloadMismatch));
        var res1 = await engine1.ExecutePlanAsync(plan, ledgerService);

        Assert(!res1.Success, "LedgerSync.MismatchRejection.SuccessIsFalse", "Execution should fail when returned NoOp mismatches ledger UID");
        Assert(res1.ElementRecords[0].ErrorCode == "ERR_NOOP_TARGET_MISMATCH", "LedgerSync.MismatchRejection.ErrorCode",
            $"Expected ERR_NOOP_TARGET_MISMATCH, got {res1.ElementRecords[0].ErrorCode}");

        // Returned NoOp with matching UID against the ledger
        var payloadMatch = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "ledger-known-uid-42" }
        };
        var engine2 = new BimPipelineEngine(CreateFakeClient(payloadMatch));
        var res2 = await engine2.ExecutePlanAsync(plan, ledgerService);

        Assert(res2.Success, "LedgerSync.MatchAcceptance.SuccessIsTrue", "Execution should succeed when returned NoOp matches ledger UID");
        Assert(res2.ElementRecords[0].Status == ElementExecutionStatus.NoOp, "LedgerSync.MatchAcceptance.StatusNoOp",
            $"Expected NoOp status, got {res2.ElementRecords[0].Status}");
    }

    private static BimExecutionPlan CreatePlan(params (string cat, string mark, string? targetUid)[] items)
    {
        var planItems = items.Select((it, idx) =>
        {
            Enum.TryParse<CoordinationCategory>(it.cat, true, out var cat);
            return new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create("Doc", cat, it.mark),
                Category = cat,
                Mark = it.mark,
                TargetUniqueId = it.targetUid,
                SequenceOrder = idx + 1
            };
        }).ToList();

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-adversarial-hash",
            Items = planItems,
            Options = new ExecutionOptions { DryRun = false }
        };
    }

    private static FakePipeClient CreateFakeClient(object payload) =>
        new()
        {
            ExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
            }
        };
}
