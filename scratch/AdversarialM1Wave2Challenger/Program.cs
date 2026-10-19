using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.Messages;

namespace AdversarialM1Wave2Challenger;

public static class Program
{
    private static int _totalChecks = 0;
    private static int _passedChecks = 0;
    private static int _failedChecks = 0;

    private static void Report(string name, bool passed, string detail = "")
    {
        _totalChecks++;
        if (passed)
        {
            _passedChecks++;
            Console.WriteLine($"  [PASS] {name} {(string.IsNullOrEmpty(detail) ? "" : "-> " + detail)}");
        }
        else
        {
            _failedChecks++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {name} {(string.IsNullOrEmpty(detail) ? "" : "-> " + detail)}");
            Console.ResetColor();
        }
    }

    private static BimExecutionPlan CreatePlan(params (CoordinationCategory cat, string mark)[] items)
    {
        var planItems = items.Select((it, idx) => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", it.cat, it.mark),
            Category = it.cat,
            Mark = it.mark,
            SequenceOrder = idx + 1
        }).ToList();

        return new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "adversarial-plan-hash",
            Items = planItems,
            Options = new ExecutionOptions { DryRun = false }
        };
    }

    private static FakeRevitPipeClient CreatePipeClient(object recordsPayload) =>
        new()
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = recordsPayload })).RootElement
            }
        };

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("   EMPIRICAL CHALLENGER: HPBIM WAVE 2 REMEDIATION M1 REQUIREMENT R1 HARNESS     ");
        Console.WriteLine("================================================================================");

        await RunDuplicateKeysTests();
        await RunEnumCaseInsensitivityAndInvalidStringsTests();
        await RunTargetUniqueIdCornerCasesTests();
        await RunCategoryAndMarkMismatchTests();
        await RunSkipPolicyTests();
        await RunStrictBatchRollbackTests();
        await RunHighVolumeStressTests();
        await RunAdversarialMultiInstanceAndRollbackStressTests();

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"SUMMARY: Total={_totalChecks}, Passed={_passedChecks}, Failed={_failedChecks}");
        Console.WriteLine("================================================================================");

        return _failedChecks == 0 ? 0 : 1;
    }

    // -----------------------------------------------------------------------------------------
    // CATEGORY 1: Duplicate Source Keys in Plan vs Duplicate Returned Records
    // -----------------------------------------------------------------------------------------
    private static async Task RunDuplicateKeysTests()
    {
        Console.WriteLine("\n[CATEGORY 1] Duplicate Source Keys in Plan vs Duplicate Records");

        // 1.1 Legitimate duplicate source keys across levels in plan, exactly matched by records
        {
            var plan = new BimExecutionPlan
            {
                PlanId = Guid.NewGuid(),
                PlanHash = "plan-dup-legit",
                Items = new[]
                {
                    new ElementPlanItem { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Room, "BanCong"), Category = CoordinationCategory.Room, Mark = "BanCong", SequenceOrder = 1 },
                    new ElementPlanItem { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Room, "BanCong"), Category = CoordinationCategory.Room, Mark = "BanCong", SequenceOrder = 2 }
                }
            };
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Room]:BanCong", category = "Room", mark = "BanCong", status = "Created", targetUniqueId = "uid-bc-1" },
                new { sourceKey = "[Doc]#[Room]:BanCong", category = "Room", mark = "BanCong", status = "Created", targetUniqueId = "uid-bc-2" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("1.1 Legitimate duplicate source keys in plan matched 1:1 by returned records succeeds",
                result.Success && result.ElementRecords.Count == 2 && result.ElementRecords.All(r => r.Status == ElementExecutionStatus.Created),
                $"Success={result.Success}, Created={result.CreatedCount}");
        }

        // 1.2 Excess duplicate records returned (3 records for 2 plan items)
        {
            var plan = new BimExecutionPlan
            {
                PlanId = Guid.NewGuid(),
                PlanHash = "plan-dup-excess",
                Items = new[]
                {
                    new ElementPlanItem { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Room, "BanCong"), Category = CoordinationCategory.Room, Mark = "BanCong", SequenceOrder = 1 },
                    new ElementPlanItem { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Room, "BanCong"), Category = CoordinationCategory.Room, Mark = "BanCong", SequenceOrder = 2 }
                }
            };
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Room]:BanCong", category = "Room", mark = "BanCong", status = "Created", targetUniqueId = "uid-bc-1" },
                new { sourceKey = "[Doc]#[Room]:BanCong", category = "Room", mark = "BanCong", status = "Created", targetUniqueId = "uid-bc-2" },
                new { sourceKey = "[Doc]#[Room]:BanCong", category = "Room", mark = "BanCong", status = "Created", targetUniqueId = "uid-bc-3" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("1.2 Excess duplicate records triggers ERR_DUPLICATE_RECORD and batch rollback",
                !result.Success && result.ElementRecords.All(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode == "ERR_DUPLICATE_RECORD"),
                $"Success={result.Success}, ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 1.3 Single plan item but 2 returned records
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1a" },
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1b" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("1.3 Duplicate record for single item triggers ERR_DUPLICATE_RECORD",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_DUPLICATE_RECORD",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 1.4 Foreign record returned (not in plan)
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:UNKNOWN", category = "Column", mark = "UNKNOWN", status = "Created", targetUniqueId = "uid-unk" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("1.4 Foreign record triggers ERR_FOREIGN_RECORD and rollback",
                !result.Success && result.ElementRecords.All(r => r.ErrorCode == "ERR_FOREIGN_RECORD"),
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 1.5 Deficit records (plan has 2, returned has 1)
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("1.5 Deficit records marks OutcomeUnknown and ERR_RECORD_DEFICIT",
                !result.Success && result.IsOutcomeUnknown && result.ElementRecords[1].ErrorCode == "ERR_RECORD_DEFICIT",
                $"IsOutcomeUnknown={result.IsOutcomeUnknown}, Rec2Status={result.ElementRecords[1].Status}");
        }
    }

    // -----------------------------------------------------------------------------------------
    // CATEGORY 2: Enum Case-Insensitivity & Invalid Strings
    // -----------------------------------------------------------------------------------------
    private static async Task RunEnumCaseInsensitivityAndInvalidStringsTests()
    {
        Console.WriteLine("\n[CATEGORY 2] Enum Case-Insensitivity & Invalid Strings");

        // 2.1 Lowercase / Mixed case valid statuses: "created", "CREATED", "cReAtEd"
        {
            var plan = CreatePlan(
                (CoordinationCategory.Column, "C1"),
                (CoordinationCategory.Column, "C2"),
                (CoordinationCategory.Column, "C3"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "CREATED", targetUniqueId = "uid-2" },
                new { sourceKey = "[Doc]#[Column]:C3", category = "Column", mark = "C3", status = "cReAtEd", targetUniqueId = "uid-3" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("2.1 Case-insensitive status parsing ('created', 'CREATED', 'cReAtEd')",
                result.Success && result.ElementRecords.All(r => r.Status == ElementExecutionStatus.Created),
                $"Success={result.Success}, CreatedCount={result.CreatedCount}");
        }

        // 2.2 Case-insensitive parsing for NoOp and Skipped (with valid reason)
        {
            var plan = CreatePlan(
                (CoordinationCategory.Column, "C1"),
                (CoordinationCategory.Column, "C2"));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "noop", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "SKIPPED", errorCode = "SKIP_FROZEN_ZONE" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("2.2 Case-insensitive 'noop' and 'SKIPPED' status parsing",
                result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.NoOp && result.ElementRecords[1].Status == ElementExecutionStatus.Skipped,
                $"Status0={result.ElementRecords[0].Status}, Status1={result.ElementRecords[1].Status}");
        }

        // 2.3 Invalid status string: "BogusStatus", "Pending", "0", "99"
        {
            var testCases = new[] { "BogusStatus", "Pending", "InFlight", "0", "99", "", "null" };
            foreach (var bogus in testCases)
            {
                var plan = CreatePlan((CoordinationCategory.Column, "C1"));
                var payload = new[]
                {
                    new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = bogus, targetUniqueId = "uid-1" }
                };
                var engine = new BimPipelineEngine(CreatePipeClient(payload));
                var result = await engine.ExecutePlanAsync(plan);

                Report($"2.3 Invalid status '{bogus}' flagged as ERR_INVALID_STATUS_ENUM",
                    !result.Success && result.ElementRecords[0].ErrorCode == "ERR_INVALID_STATUS_ENUM",
                    $"Status={result.ElementRecords[0].Status}, Code={result.ElementRecords[0].ErrorCode}");
            }
        }
    }

    // -----------------------------------------------------------------------------------------
    // CATEGORY 3: Empty/Whitespace/Missing TargetUniqueId on Created vs NoOp
    // -----------------------------------------------------------------------------------------
    private static async Task RunTargetUniqueIdCornerCasesTests()
    {
        Console.WriteLine("\n[CATEGORY 3] Empty/Whitespace/Omitted TargetUniqueId on Created vs NoOp");

        // 3.1 Created with empty string TargetUniqueId
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("3.1 Created with empty string TargetUniqueId fails with ERR_MISSING_TARGET_UNIQUE_ID",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 3.2 Created with whitespace TargetUniqueId
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "   \t  " }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("3.2 Created with whitespace TargetUniqueId fails with ERR_MISSING_TARGET_UNIQUE_ID",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 3.3 Created with explicit null TargetUniqueId
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = (string?)null }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("3.3 Created with explicit JSON null TargetUniqueId fails with ERR_MISSING_TARGET_UNIQUE_ID",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 3.4 Created with targetUniqueId property completely omitted in JSON
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var jsonString = "{\"records\":[{\"sourceKey\":\"[Doc]#[Column]:C1\",\"category\":\"Column\",\"mark\":\"C1\",\"status\":\"Created\"}]}";
            var fakeClient = new FakeRevitPipeClient
            {
                CustomExecuteHandler = _ => new ExecuteResult
                {
                    IsError = false,
                    RolledBack = false,
                    Value = JsonDocument.Parse(jsonString).RootElement
                }
            };
            var engine = new BimPipelineEngine(fakeClient);
            var result = await engine.ExecutePlanAsync(plan);

            Report("3.4 Created with targetUniqueId property completely omitted from JSON",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                $"Success={result.Success}, Status={result.ElementRecords[0].Status}, Code={result.ElementRecords[0].ErrorCode}");
        }

        // 3.5 Updated with empty TargetUniqueId fails
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Updated", targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("3.5 Updated with empty TargetUniqueId fails with ERR_MISSING_TARGET_UNIQUE_ID",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 3.6 NoOp with empty TargetUniqueId succeeds (NoOp does not create/require target ID)
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "NoOp", targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("3.6 NoOp with empty TargetUniqueId is accepted and succeeds",
                result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.NoOp,
                $"Success={result.Success}, Status={result.ElementRecords[0].Status}");
        }

        // 3.7 Skipped with empty TargetUniqueId succeeds if valid skip reason
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = "SKIP_FROZEN_ZONE", targetUniqueId = "" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("3.7 Skipped with empty TargetUniqueId is accepted when approved reason provided",
                result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Skipped,
                $"Success={result.Success}, Status={result.ElementRecords[0].Status}");
        }
    }

    // -----------------------------------------------------------------------------------------
    // CATEGORY 4: Category and Mark Mismatch Detection
    // -----------------------------------------------------------------------------------------
    private static async Task RunCategoryAndMarkMismatchTests()
    {
        Console.WriteLine("\n[CATEGORY 4] Category and Mark Mismatch Detection");

        // 4.1 Category mismatch (Plan=Column, Returned=Beam)
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Beam", mark = "C1", status = "Created", targetUniqueId = "uid-1" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("4.1 Category mismatch (Column vs Beam) fails with ERR_CATEGORY_MISMATCH",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_CATEGORY_MISMATCH",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 4.2 Invalid category string in JSON (category = "InvalidCategory")
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "InvalidCategory", mark = "C1", status = "Created", targetUniqueId = "uid-1" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("4.2 Invalid category string in JSON fails with ERR_CATEGORY_MISMATCH",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_CATEGORY_MISMATCH",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 4.3 Mark mismatch (Plan=C1, Returned=C2)
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-1" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("4.3 Mark mismatch (C1 vs C2) fails with ERR_MARK_MISMATCH",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_MARK_MISMATCH",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 4.4 Mark case-insensitivity (Plan=C1, Returned=c1)
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "c1", status = "Created", targetUniqueId = "uid-1" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("4.4 Mark case-insensitive match (C1 vs c1) succeeds",
                result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Created,
                $"Success={result.Success}, Mark={result.ElementRecords[0].Mark}");
        }

        // 4.5 Mark with whitespace mismatch (Plan=C1, Returned=" C1 ")
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = " C1 ", status = "Created", targetUniqueId = "uid-1" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("4.5 Mark with leading/trailing whitespace mismatch ('C1' vs ' C1 ') fails with ERR_MARK_MISMATCH",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_MARK_MISMATCH",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }
    }

    // -----------------------------------------------------------------------------------------
    // CATEGORY 5: Approved vs Unapproved Skip Handling
    // -----------------------------------------------------------------------------------------
    private static async Task RunSkipPolicyTests()
    {
        Console.WriteLine("\n[CATEGORY 5] Approved vs Unapproved Skip Handling");

        // 5.1 Unapproved skip without valid reason
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "Skipped because it felt like it" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("5.1 Unapproved skip without approved reason code fails with ERR_INTEGRITY_DEFECT_SKIPPED",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_INTEGRITY_DEFECT_SKIPPED",
                $"ErrorCode={result.ElementRecords[0].ErrorCode}");
        }

        // 5.2 Approved skip with reason code in errorCode
        {
            var approvedCodes = new[]
            {
                "SKIP_FROZEN_ZONE", "SKIP_STAGE_DISABLED", "SKIP_PRECONDITION_FAILED",
                "SKIP_PREREQUISITE_FAILED", "SKIP_USER_EXCLUDED"
            };

            foreach (var code in approvedCodes)
            {
                var plan = CreatePlan((CoordinationCategory.Column, "C1"));
                var payload = new[]
                {
                    new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = code }
                };
                var engine = new BimPipelineEngine(CreatePipeClient(payload));
                var result = await engine.ExecutePlanAsync(plan);

                Report($"5.2 Approved skip code '{code}' in errorCode is accepted",
                    result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Skipped,
                    $"Success={result.Success}, Status={result.ElementRecords[0].Status}");
            }
        }

        // 5.3 Approved skip code embedded in errorReason string (case-insensitive)
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "Zone was frozen by coordinator (skip_frozen_zone)." }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("5.3 Approved skip code in errorReason text is accepted (case-insensitive)",
                result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Skipped,
                $"Success={result.Success}, Status={result.ElementRecords[0].Status}");
        }

        // 5.4 Plan item with SkipAllowed=true allows arbitrary skip reason
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var item = plan.Items[0] with { Parameters = new Dictionary<string, object> { ["SkipAllowed"] = true } };
            plan = plan with { Items = new[] { item } };

            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "Custom arbitrary reason" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("5.4 Plan item with SkipAllowed=true permits arbitrary skip reason",
                result.Success && result.ElementRecords[0].Status == ElementExecutionStatus.Skipped,
                $"Success={result.Success}");
        }

        // 5.5 Plan item with SkipAllowed=false rejects arbitrary skip reason
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"));
            var item = plan.Items[0] with { Parameters = new Dictionary<string, object> { ["SkipAllowed"] = false } };
            plan = plan with { Items = new[] { item } };

            var payload = new[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "Custom arbitrary reason" }
            };
            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            Report("5.5 Plan item with SkipAllowed=false rejects arbitrary skip reason",
                !result.Success && result.ElementRecords[0].ErrorCode == "ERR_INTEGRITY_DEFECT_SKIPPED",
                $"Success={result.Success}, Code={result.ElementRecords[0].ErrorCode}");
        }
    }

    // -----------------------------------------------------------------------------------------
    // CATEGORY 6: Strict Batch Rollback Behavior on Integrity Defects
    // -----------------------------------------------------------------------------------------
    private static async Task RunStrictBatchRollbackTests()
    {
        Console.WriteLine("\n[CATEGORY 6] Strict Batch Rollback Behavior on Integrity Defects");

        // 6.1 Batch of 5 items: 1 defect at item 3, all non-defects rolled back with ERR_BATCH_ROLLED_BACK
        {
            var plan = CreatePlan(
                (CoordinationCategory.Column, "C1"),
                (CoordinationCategory.Column, "C2"),
                (CoordinationCategory.Column, "C3"),
                (CoordinationCategory.Column, "C4"),
                (CoordinationCategory.Column, "C5"));

            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" },
                new { sourceKey = "[Doc]#[Column]:C3", category = "Beam", mark = "C3", status = "Created", targetUniqueId = "uid-3" }, // Category mismatch
                new { sourceKey = "[Doc]#[Column]:C4", category = "Column", mark = "C4", status = "Created", targetUniqueId = "uid-4" },
                new { sourceKey = "[Doc]#[Column]:C5", category = "Column", mark = "C5", status = "Created", targetUniqueId = "uid-5" }
            };

            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            bool defectPreserved = result.ElementRecords[2].ErrorCode == "ERR_CATEGORY_MISMATCH";
            bool othersRolledBack = result.ElementRecords.Where((r, idx) => idx != 2)
                .All(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode == "ERR_BATCH_ROLLED_BACK");
            bool rootCauseCited = result.ElementRecords[0].ErrorReason?.Contains("ERR_CATEGORY_MISMATCH") == true;

            Report("6.1 Strict batch rollback: defect preserved, other 4 elements rolled back citing root cause",
                !result.Success && defectPreserved && othersRolledBack && rootCauseCited,
                $"DefectCode={result.ElementRecords[2].ErrorCode}, OtherCode={result.ElementRecords[0].ErrorCode}");
        }

        // 6.2 Multiple specific defects in batch: both root causes preserved
        {
            var plan = CreatePlan(
                (CoordinationCategory.Column, "C1"),
                (CoordinationCategory.Column, "C2"),
                (CoordinationCategory.Column, "C3"));

            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Beam", mark = "C1", status = "Created", targetUniqueId = "uid-1" }, // Category mismatch
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C99", status = "Created", targetUniqueId = "uid-2" }, // Mark mismatch
                new { sourceKey = "[Doc]#[Column]:C3", category = "Column", mark = "C3", status = "Created", targetUniqueId = "uid-3" }  // Normal
            };

            var engine = new BimPipelineEngine(CreatePipeClient(payload));
            var result = await engine.ExecutePlanAsync(plan);

            bool c1Preserved = result.ElementRecords[0].ErrorCode == "ERR_CATEGORY_MISMATCH";
            bool c2Preserved = result.ElementRecords[1].ErrorCode == "ERR_MARK_MISMATCH";
            bool c3RolledBack = result.ElementRecords[2].ErrorCode == "ERR_BATCH_ROLLED_BACK";

            Report("6.2 Multiple defects: both specific defects preserved while normal items rolled back",
                !result.Success && c1Preserved && c2Preserved && c3RolledBack,
                $"C1={result.ElementRecords[0].ErrorCode}, C2={result.ElementRecords[1].ErrorCode}, C3={result.ElementRecords[2].ErrorCode}");
        }

        // 6.3 External transaction rollback (isFailedRollback=true) converts created elements to ERR_BATCH_ROLLED_BACK
        {
            var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
            var payload = new object[]
            {
                new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
                new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Failed", errorCode = "ERR_SYMBOL_NOT_FOUND", errorReason = "Column type missing" }
            };

            var fakeClient = new FakeRevitPipeClient
            {
                CustomExecuteHandler = _ => new ExecuteResult
                {
                    IsError = true,
                    RolledBack = true,
                    Message = "Transaction rolled back due to Revit error",
                    Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
                }
            };

            var engine = new BimPipelineEngine(fakeClient);
            var result = await engine.ExecutePlanAsync(plan);

            Report("6.3 External failed rollback preserves specific error and rolls back created elements",
                !result.Success && result.ElementRecords[1].ErrorCode == "ERR_SYMBOL_NOT_FOUND" && result.ElementRecords[0].ErrorCode == "ERR_BATCH_ROLLED_BACK",
                $"Rec0={result.ElementRecords[0].ErrorCode}, Rec1={result.ElementRecords[1].ErrorCode}");
        }
    }

    // -----------------------------------------------------------------------------------------
    // CATEGORY 7: High Volume & Stress Tests
    // -----------------------------------------------------------------------------------------
    private static async Task RunHighVolumeStressTests()
    {
        Console.WriteLine("\n[CATEGORY 7] High Volume Stress Tests");

        // 7.1 Large batch (1000 items) with defect at item 999
        {
            int count = 1000;
            var planItems = Enumerable.Range(1, count).Select(i => new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, $"C{i:D4}"),
                Category = CoordinationCategory.Column,
                Mark = $"C{i:D4}",
                SequenceOrder = i
            }).ToList();

            var plan = new BimExecutionPlan
            {
                PlanId = Guid.NewGuid(),
                PlanHash = "stress-1000",
                Items = planItems
            };

            var returnRecords = Enumerable.Range(1, count).Select(i => new
            {
                sourceKey = $"[Doc]#[Column]:C{i:D4}",
                category = i == 999 ? "Beam" : "Column", // Defect at item 999
                mark = $"C{i:D4}",
                status = "Created",
                targetUniqueId = $"uid-{i}"
            }).ToArray();

            var engine = new BimPipelineEngine(CreatePipeClient(returnRecords));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await engine.ExecutePlanAsync(plan);
            sw.Stop();

            bool defectFound = result.ElementRecords[998].ErrorCode == "ERR_CATEGORY_MISMATCH";
            int rolledBackCount = result.ElementRecords.Count(r => r.ErrorCode == "ERR_BATCH_ROLLED_BACK");

            Report($"7.1 1000-item batch stress test (completed in {sw.ElapsedMilliseconds}ms)",
                !result.Success && defectFound && rolledBackCount == 999,
                $"RolledBackCount={rolledBackCount}, Elapsed={sw.ElapsedMilliseconds}ms");
        }
    }

    // -----------------------------------------------------------------------------------------
    // CATEGORY 8: Empirical Challenger Adversarial Multi-Instance & Rollback Stress Tests
    // -----------------------------------------------------------------------------------------
    private static async Task RunAdversarialMultiInstanceAndRollbackStressTests()
    {
        Console.WriteLine("\n[CATEGORY 8] Adversarial Multi-Instance Duplicate Keys & Rollback Stress");
        await TestMultiInstanceHappyPath();
        await TestMultiInstanceWithTargetUniqueIdOmittedDefect();
        await TestMultiInstanceWithCategoryOrMarkMismatch();
        await TestMultiInstanceWithUnapprovedSkip();
        await TestMultiInstanceExcessRecords();
        await TestMultiInstanceDeficitRecords();
        await TestInterleavedMultiInstanceBijection();
        await TestInterleavedMultiInstanceDefectRollback();
        await TestRollbackBoundaryFirstItem();
        await TestRollbackBoundaryLastItem();
        await TestValidSkipFrozenZoneInMultiInstance();
        await TestValidSkipAllowedParamInMultiInstance();
        await TestTruncatedResponseRollback();
        await TestExternalTransactionRollbackWithPayload();
    }

    private static async Task TestMultiInstanceHappyPath()
    {
        var planItems = Enumerable.Range(1, 5).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = i
        }).ToList();
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-multi-5", Items = planItems };

        var payload = Enumerable.Range(1, 5).Select(i => new
        {
            sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = $"uid-c1-{i}"
        }).ToArray();

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool allCreated = result.ElementRecords.Count == 5 &&
                          result.ElementRecords.All(r => r.Status == ElementExecutionStatus.Created) &&
                          result.ElementRecords.Select(r => r.TargetUniqueId).Distinct().Count() == 5;
        Report("8.1 Multi-instance 5 identical keys matched 1:1 with unique targetUniqueIds succeeds",
            result.Success && allCreated,
            $"Success={result.Success}, UniqueUIDs={result.ElementRecords.Select(r => r.TargetUniqueId).Distinct().Count()}");
    }

    private static async Task TestMultiInstanceWithTargetUniqueIdOmittedDefect()
    {
        var planItems = Enumerable.Range(1, 5).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = i
        }).ToList();
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-multi-defect-uid", Items = planItems };

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-2" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created" }, // OMITTED UID
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-4" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-5" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool defectMatch = result.ElementRecords[2].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID";
        bool othersRolledBack = result.ElementRecords.Where((_, idx) => idx != 2)
            .All(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode == "ERR_BATCH_ROLLED_BACK");

        Report("8.2 Multi-instance with omitted targetUniqueId at #3 triggers batch rollback",
            !result.Success && defectMatch && othersRolledBack,
            $"Item3Code={result.ElementRecords[2].ErrorCode}, OthersCode={result.ElementRecords[0].ErrorCode}");
    }

    private static async Task TestMultiInstanceWithCategoryOrMarkMismatch()
    {
        var planItems = Enumerable.Range(1, 5).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = i
        }).ToList();
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-multi-mark-mismatch", Items = planItems };

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1_CORRUPTED", status = "Created", targetUniqueId = "uid-2" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-3" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-4" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-5" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool defectMatch = result.ElementRecords[1].ErrorCode == "ERR_MARK_MISMATCH";
        bool othersRolledBack = result.ElementRecords.Where((_, idx) => idx != 1)
            .All(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode == "ERR_BATCH_ROLLED_BACK");

        Report("8.3 Multi-instance with mark mismatch at #2 triggers batch rollback",
            !result.Success && defectMatch && othersRolledBack,
            $"Item2Code={result.ElementRecords[1].ErrorCode}, OthersCode={result.ElementRecords[0].ErrorCode}");
    }

    private static async Task TestMultiInstanceWithUnapprovedSkip()
    {
        var planItems = Enumerable.Range(1, 5).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = i
        }).ToList();
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-multi-bad-skip", Items = planItems };

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-2" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-3" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "Bogus skip reason" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-5" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool defectMatch = result.ElementRecords[3].ErrorCode == "ERR_INTEGRITY_DEFECT_SKIPPED";
        bool othersRolledBack = result.ElementRecords.Where((_, idx) => idx != 3)
            .All(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode == "ERR_BATCH_ROLLED_BACK");

        Report("8.4 Multi-instance with unapproved skip at #4 triggers batch rollback",
            !result.Success && defectMatch && othersRolledBack,
            $"Item4Code={result.ElementRecords[3].ErrorCode}, OthersCode={result.ElementRecords[0].ErrorCode}");
    }

    private static async Task TestMultiInstanceExcessRecords()
    {
        var planItems = Enumerable.Range(1, 5).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = i
        }).ToList();
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-multi-excess", Items = planItems };

        var payload = Enumerable.Range(1, 7).Select(i => new
        {
            sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = $"uid-{i}"
        }).ToArray();

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool allDupes = result.ElementRecords.Count == 5 &&
                        result.ElementRecords.All(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode == "ERR_DUPLICATE_RECORD");

        Report("8.5 Multi-instance 5 items with 7 returned records triggers ERR_DUPLICATE_RECORD across all items",
            !result.Success && allDupes,
            $"AllFailedWithDupe={allDupes}");
    }

    private static async Task TestMultiInstanceDeficitRecords()
    {
        var planItems = Enumerable.Range(1, 5).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = i
        }).ToList();
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-multi-deficit", Items = planItems };

        var payload = Enumerable.Range(1, 3).Select(i => new
        {
            sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = $"uid-{i}"
        }).ToArray();

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool item4Deficit = result.ElementRecords[3].ErrorCode == "ERR_RECORD_DEFICIT" && result.ElementRecords[3].Status == ElementExecutionStatus.OutcomeUnknown;
        bool item5Deficit = result.ElementRecords[4].ErrorCode == "ERR_RECORD_DEFICIT" && result.ElementRecords[4].Status == ElementExecutionStatus.OutcomeUnknown;

        Report("8.6 Multi-instance 5 items with 3 returned records marks deficit items as OutcomeUnknown",
            !result.Success && result.IsOutcomeUnknown && item4Deficit && item5Deficit,
            $"IsOutcomeUnknown={result.IsOutcomeUnknown}, Rec4Code={result.ElementRecords[3].ErrorCode}");
    }

    private static async Task TestInterleavedMultiInstanceBijection()
    {
        var planItems = new List<ElementPlanItem>
        {
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 1 },
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Beam, "B1"), Category = CoordinationCategory.Beam, Mark = "B1", SequenceOrder = 2 },
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 3 },
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Beam, "B1"), Category = CoordinationCategory.Beam, Mark = "B1", SequenceOrder = 4 },
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 5 }
        };
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-interleaved", Items = planItems };

        var payload = new[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-c1-1" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-c1-2" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-c1-3" },
            new { sourceKey = "[Doc]#[Beam]:B1", category = "Beam", mark = "B1", status = "Created", targetUniqueId = "uid-b1-1" },
            new { sourceKey = "[Doc]#[Beam]:B1", category = "Beam", mark = "B1", status = "Created", targetUniqueId = "uid-b1-2" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool passed = result.Success &&
                      result.ElementRecords[0].TargetUniqueId == "uid-c1-1" &&
                      result.ElementRecords[1].TargetUniqueId == "uid-b1-1" &&
                      result.ElementRecords[2].TargetUniqueId == "uid-c1-2" &&
                      result.ElementRecords[3].TargetUniqueId == "uid-b1-2" &&
                      result.ElementRecords[4].TargetUniqueId == "uid-c1-3";

        Report("8.7 Interleaved multi-instance keys correctly matched FIFO by queue",
            passed,
            $"Success={result.Success}, C1_1={result.ElementRecords[0].TargetUniqueId}, B1_1={result.ElementRecords[1].TargetUniqueId}");
    }

    private static async Task TestInterleavedMultiInstanceDefectRollback()
    {
        var planItems = new List<ElementPlanItem>
        {
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 1 },
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Beam, "B1"), Category = CoordinationCategory.Beam, Mark = "B1", SequenceOrder = 2 },
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 3 }
        };
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-interleaved-defect", Items = planItems };

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-c1-1" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-c1-2" },
            new { sourceKey = "[Doc]#[Beam]:B1", category = "Beam", mark = "B1", status = "Failed", errorCode = "ERR_HOST_MISSING", errorReason = "Host missing" }
        };

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = true,
                RolledBack = true,
                Message = "Batch failed",
                Value = JsonDocument.Parse(JsonSerializer.Serialize(new { records = payload })).RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        bool beamPreserved = result.ElementRecords[1].ErrorCode == "ERR_HOST_MISSING";
        bool col1RolledBack = result.ElementRecords[0].ErrorCode == "ERR_BATCH_ROLLED_BACK";
        bool col2RolledBack = result.ElementRecords[2].ErrorCode == "ERR_BATCH_ROLLED_BACK";

        Report("8.8 Interleaved multi-instance defect rolls back all other instances to ERR_BATCH_ROLLED_BACK",
            !result.Success && beamPreserved && col1RolledBack && col2RolledBack,
            $"Beam={result.ElementRecords[1].ErrorCode}, Col0={result.ElementRecords[0].ErrorCode}");
    }

    private static async Task TestRollbackBoundaryFirstItem()
    {
        var plan = CreatePlan(
            (CoordinationCategory.Column, "C1"),
            (CoordinationCategory.Column, "C2"),
            (CoordinationCategory.Column, "C3"));

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "" }, // First item defect
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" },
            new { sourceKey = "[Doc]#[Column]:C3", category = "Column", mark = "C3", status = "Created", targetUniqueId = "uid-3" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool c1Defect = result.ElementRecords[0].ErrorCode == "ERR_MISSING_TARGET_UNIQUE_ID";
        bool c2Roll = result.ElementRecords[1].ErrorCode == "ERR_BATCH_ROLLED_BACK";
        bool c3Roll = result.ElementRecords[2].ErrorCode == "ERR_BATCH_ROLLED_BACK";

        Report("8.9 Boundary: First item defect preserves error and rolls back all subsequent items",
            !result.Success && c1Defect && c2Roll && c3Roll,
            $"C1={result.ElementRecords[0].ErrorCode}, C2={result.ElementRecords[1].ErrorCode}");
    }

    private static async Task TestRollbackBoundaryLastItem()
    {
        var plan = CreatePlan(
            (CoordinationCategory.Column, "C1"),
            (CoordinationCategory.Column, "C2"),
            (CoordinationCategory.Column, "C3"));

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" },
            new { sourceKey = "[Doc]#[Column]:C3", category = "Column", mark = "C3_WRONG", status = "Created", targetUniqueId = "uid-3" } // Last item defect
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool c1Roll = result.ElementRecords[0].ErrorCode == "ERR_BATCH_ROLLED_BACK";
        bool c2Roll = result.ElementRecords[1].ErrorCode == "ERR_BATCH_ROLLED_BACK";
        bool c3Defect = result.ElementRecords[2].ErrorCode == "ERR_MARK_MISMATCH";

        Report("8.10 Boundary: Last item defect preserves error and rolls back all preceding items",
            !result.Success && c1Roll && c2Roll && c3Defect,
            $"C1={result.ElementRecords[0].ErrorCode}, C3={result.ElementRecords[2].ErrorCode}");
    }

    private static async Task TestValidSkipFrozenZoneInMultiInstance()
    {
        var planItems = Enumerable.Range(1, 3).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = i
        }).ToList();
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-multi-skip", Items = planItems };

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorCode = "SKIP_FROZEN_ZONE" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-3" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool passed = result.Success &&
                      result.ElementRecords[0].Status == ElementExecutionStatus.Created &&
                      result.ElementRecords[1].Status == ElementExecutionStatus.Skipped &&
                      result.ElementRecords[2].Status == ElementExecutionStatus.Created;

        Report("8.11 Multi-instance valid SKIP_FROZEN_ZONE succeeds without false positive rollback",
            passed,
            $"Success={result.Success}, Rec0={result.ElementRecords[0].Status}, Rec1={result.ElementRecords[1].Status}, Rec2={result.ElementRecords[2].Status}");
    }

    private static async Task TestValidSkipAllowedParamInMultiInstance()
    {
        var planItems = new List<ElementPlanItem>
        {
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 1 },
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 2,
                    Parameters = new Dictionary<string, object> { ["SkipAllowed"] = true } },
            new() { SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 3 }
        };
        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "p-multi-skip-allowed", Items = planItems };

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Skipped", errorReason = "Coordinator requested skip" },
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-3" }
        };

        var engine = new BimPipelineEngine(CreatePipeClient(payload));
        var result = await engine.ExecutePlanAsync(plan);

        bool passed = result.Success &&
                      result.ElementRecords[0].Status == ElementExecutionStatus.Created &&
                      result.ElementRecords[1].Status == ElementExecutionStatus.Skipped &&
                      result.ElementRecords[2].Status == ElementExecutionStatus.Created;

        Report("8.12 Multi-instance item with SkipAllowed=true parameter succeeds without false positive rollback",
            passed,
            $"Success={result.Success}, Rec0={result.ElementRecords[0].Status}, Rec1={result.ElementRecords[1].Status}");
    }

    private static async Task TestTruncatedResponseRollback()
    {
        var plan = CreatePlan(
            (CoordinationCategory.Column, "C1"),
            (CoordinationCategory.Column, "C2"));

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                RolledBack = false,
                Truncated = true
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        bool passed = !result.Success && result.IsOutcomeUnknown &&
                      result.ElementRecords.All(r => r.Status == ElementExecutionStatus.OutcomeUnknown && r.ErrorCode == "ERR_RESPONSE_TRUNCATED");

        Report("8.13 Truncated response correctly marks all records OutcomeUnknown with ERR_RESPONSE_TRUNCATED",
            passed,
            $"Success={result.Success}, IsOutcomeUnknown={result.IsOutcomeUnknown}, Code={result.ElementRecords[0].ErrorCode}");
    }

    private static async Task TestExternalTransactionRollbackWithPayload()
    {
        var plan = CreatePlan(
            (CoordinationCategory.Column, "C1"),
            (CoordinationCategory.Column, "C2"));

        var payload = new object[]
        {
            new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created", targetUniqueId = "uid-1" },
            new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Failed", errorCode = "ERR_FAMILY_UNRESOLVED", errorReason = "Family missing" }
        };

        string msg = $"[BATCH_TRANSACTION_FAILED] Execution failed ||PAYLOAD:{JsonSerializer.Serialize(payload)}";
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = true,
                RolledBack = true,
                Message = msg
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecutePlanAsync(plan);

        bool c1Roll = result.ElementRecords[0].Status == ElementExecutionStatus.Failed && result.ElementRecords[0].ErrorCode == "ERR_BATCH_ROLLED_BACK";
        bool c2Defect = result.ElementRecords[1].Status == ElementExecutionStatus.Failed && result.ElementRecords[1].ErrorCode == "ERR_FAMILY_UNRESOLVED";

        Report("8.14 External failed rollback with payload parses records, preserving failure and rolling back created",
            !result.Success && c1Roll && c2Defect,
            $"C1Code={result.ElementRecords[0].ErrorCode}, C2Code={result.ElementRecords[1].ErrorCode}");
    }
}
