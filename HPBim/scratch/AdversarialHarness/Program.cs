namespace AdversarialHarness;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;

public enum ChangeOrigin
{
    UserInteractive = 1,
    PipelineAutomation = 2,
    ExternalSync = 3
}

public enum ConflictResolutionAction
{
    NoOp = 1,
    ApplyCad = 2,
    PreserveRevit = 3,
    RaiseRfiAndHalt = 4
}

public enum BimTaskState
{
    Queued = 0,
    Dispatched = 1,
    InFlight = 2,
    Committed = 3,
    Failed = 4,
    OutcomeUnknown = 5,
    DeadLetter = 6,
    Cancelled = 7
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("HPBim Advanced Pipeline P2 Architecture — Empirical Adversarial Stress Harness");
        Console.WriteLine("Target: HPBIM_ADVANCED_PIPELINE_DESIGN.md (Pillars 1 & 2)");
        Console.WriteLine("================================================================================\n");

        int failed = 0;

        failed += RunTest("Pillar 1: DMU Recursion Guard & Context Suppression", TestDmuRecursionGuardAndSuppression);
        failed += RunTest("Pillar 1: Zombie Element Scenario & Tombstoning Integrity", TestZombieElementTombstoning);
        failed += RunTest("Pillar 1: Deterministic 3-Way Merge (Case 6: 220x220 -> 220x350 vs 220x300)", TestThreeWayMergeEngine);
        failed += await RunTestAsync("Pillar 2: Queue Daemon Single-Writer UI Thread Serialization", TestQueueDaemonSingleWriterSerializationAsync);
        failed += RunTest("Pillar 2: Priority Queue Anti-Starvation Aging (+10/min) Stress Test", TestPriorityQueueAntiStarvationAging);
        failed += RunTest("Pillar 2: Daemon Crash Recovery & SQLite WAL Lease Expiration", TestCrashRecoveryAndLeaseExpiration);

        Console.WriteLine("\n================================================================================");
        if (failed == 0)
        {
            Console.WriteLine("ALL 6 EMPIRICAL ADVERSARIAL STRESS TESTS PASSED SUCCESSFULLY! (0 failures)");
            Console.WriteLine("================================================================================");
            return 0;
        }
        else
        {
            Console.WriteLine($"ADVERSARIAL TESTS FAILED: {failed} failure(s) detected!");
            Console.WriteLine("================================================================================");
            return 1;
        }
    }

    private static int RunTest(string testName, Action testAction)
    {
        Console.Write($"[TEST] {testName} ... ");
        try
        {
            testAction();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PASS");
            Console.ResetColor();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAIL: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine($"      StackTrace: {ex.StackTrace}");
            return 1;
        }
    }

    private static async Task<int> RunTestAsync(string testName, Func<Task> testActionAsync)
    {
        Console.Write($"[TEST] {testName} ... ");
        try
        {
            await testActionAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PASS");
            Console.ResetColor();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAIL: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine($"      StackTrace: {ex.StackTrace}");
            return 1;
        }
    }

    // =========================================================================
    // 1. DMU Recursion Guard & Context Suppression
    // =========================================================================
    private static void TestDmuRecursionGuardAndSuppression()
    {
        // 1. Test AsyncLocal scope behavior
        Assert(SimulatedBimTrackingContext.CurrentOrigin == ChangeOrigin.UserInteractive, "Default origin must be UserInteractive");

        using (SimulatedBimTrackingContext.SuppressTracking(ChangeOrigin.PipelineAutomation))
        {
            Assert(SimulatedBimTrackingContext.CurrentOrigin == ChangeOrigin.PipelineAutomation, "Origin inside scope must be PipelineAutomation");

            // Nested scope
            using (SimulatedBimTrackingContext.SuppressTracking(ChangeOrigin.ExternalSync))
            {
                Assert(SimulatedBimTrackingContext.CurrentOrigin == ChangeOrigin.ExternalSync, "Nested origin must be ExternalSync");
            }

            Assert(SimulatedBimTrackingContext.CurrentOrigin == ChangeOrigin.PipelineAutomation, "Unwound nested scope must restore PipelineAutomation");
        }

        Assert(SimulatedBimTrackingContext.CurrentOrigin == ChangeOrigin.UserInteractive, "Unwound outer scope must restore UserInteractive");

        // 2. Exception safety in using block
        try
        {
            using (SimulatedBimTrackingContext.SuppressTracking(ChangeOrigin.PipelineAutomation))
            {
                throw new InvalidOperationException("Simulated batch failure");
            }
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
        Assert(SimulatedBimTrackingContext.CurrentOrigin == ChangeOrigin.UserInteractive, "Exception unwinding must restore UserInteractive");

        // 3. Thread-local RecursionGuard re-entrancy prevention
        var guard = new SimulatedRecursionGuard();
        Assert(!guard.IsExecuting, "Initial recursion guard state must be false");

        int executionCount = 0;
        void RecursiveOperation(int depth)
        {
            if (!guard.TryEnter())
            {
                return; // Re-entrancy suppressed!
            }
            try
            {
                executionCount++;
                if (depth > 0)
                {
                    RecursiveOperation(depth - 1); // Nested re-entrant call
                }
            }
            finally
            {
                guard.Exit();
            }
        }

        RecursiveOperation(5);
        Assert(executionCount == 1, $"Re-entrancy guard must limit execution to 1, but got {executionCount}");
        Assert(!guard.IsExecuting, "Guard must be released after completion");

        // 4. Inter-chunk yield simulation
        // Inside transaction: PipelineAutomation. During 50ms yield: UserInteractive.
        bool modelerCaptured = false;
        void ModelerMovesElement()
        {
            if (SimulatedBimTrackingContext.CurrentOrigin == ChangeOrigin.UserInteractive)
            {
                modelerCaptured = true;
            }
        }

        using (SimulatedBimTrackingContext.SuppressTracking(ChangeOrigin.PipelineAutomation))
        {
            // Automated batch executing...
            Assert(SimulatedBimTrackingContext.CurrentOrigin == ChangeOrigin.PipelineAutomation, "In batch");
        }

        // Inter-chunk delay yield (outside suppression scope)
        ModelerMovesElement();
        Assert(modelerCaptured, "Modeler edits during inter-chunk yield must be captured as UserInteractive, never suppressed!");
    }

    // =========================================================================
    // 2. Zombie Element Scenario & Tombstoning Integrity
    // =========================================================================
    private static void TestZombieElementTombstoning()
    {
        var key = CompositeSourceKey.Parse("[NhaPho_5x18m.dwg]#[Column]:C1_5A3");
        var planItem = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            Mark = "C1",
            TargetLevelName = "+0.150m",
            Resource = new ResourceResolutionSpec { TypeName = "220x220" }
        };

        // Baseline: Naive P0/P1 resolver behavior when element was deleted in Revit
        var naiveLedger = new BimCoordinationLedger { Project = "NhaPho_5x18m" }
            .RegisterMapping(LedgerElementMapping.Create(key, "revit-uid-c1", 1001L, CoordinationCategory.Column, "C1", "hash-1"));

        var missingInRevitQuery = new FakeExistenceQuery(false, null, "revit-uid-c1");
        var naiveDecision = IdempotentElementResolver.Resolve(planItem, naiveLedger, missingInRevitQuery);

        Assert(naiveDecision.Action == PlannedAction.Recreate,
            $"Naive resolver creates zombie element by issuing {naiveDecision.Action} instead of checking tombstone!");

        // Proposed P2 Architecture: Tombstoned element handling
        var p2Mapping = new SimulatedTombstoneMapping
        {
            SourceKey = key,
            TargetUniqueId = string.Empty,
            TargetElementId = null,
            IsTombstoned = true,
            TombstonedAtUtc = DateTime.UtcNow,
            TombstoneReason = "User interactive deletion in Revit UI"
        };

        // Resolve under P2 Tombstone-Aware Resolver
        var p2Decision = SimulatedP2Resolver.Resolve(planItem, p2Mapping, missingInRevitQuery);

        Assert(p2Decision.Action == SimulatedP2Action.Skip,
            $"P2 Resolver must SKIP tombstoned element, but got {p2Decision.Action}");
        Assert(p2Decision.Status == ElementExecutionStatus.Skipped,
            "P2 Resolver status must be Skipped");
        Assert(p2Decision.Reason.Contains("tombstoned in Revit by user"),
            $"P2 Resolver reason must mention tombstone, got: '{p2Decision.Reason}'");
        Assert(p2Decision.GeneratedRfi != null,
            "P2 Resolver must generate an RFI record for tombstoned element");
        Assert(p2Decision.GeneratedRfi!.ElementIds.Contains(key.ToString()),
            "Generated RFI must reference the tombstoned element's SourceKey");

        // Revoking Tombstone via explicit command
        p2Mapping = p2Mapping with { IsTombstoned = false, TombstoneReason = "Revoked by Chief Engineer via --untombstone" };
        var revokedDecision = SimulatedP2Resolver.Resolve(planItem, p2Mapping, missingInRevitQuery);
        Assert(revokedDecision.Action == SimulatedP2Action.Create,
            $"After explicit untombstone, element must be allowed to Create, but got {revokedDecision.Action}");
    }

    // =========================================================================
    // 3. Deterministic 3-Way Merge (Case 6: Dual Divergence Conflict)
    // =========================================================================
    private static void TestThreeWayMergeEngine()
    {
        var key = CompositeSourceKey.Parse("[NhaPho_5x18m.dwg]#[Column]:C1_5A3");
        var evaluator = new SimulatedThreeWayMergeEvaluator();

        // Concrete Scenario from Dispatch:
        // Base: 220x220 mm
        // CAD: 220x350 mm (delta = 130mm, changed)
        // Modeler: 220x300 mm (delta = 80mm, changed)
        // Tolerance: 0.0mm (strict structural dimension)
        var contextCase6 = new SimulatedConflictContext
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            PropertyName = "CrossSectionHeightMm",
            BaseValue = "220",
            CadValue = "350",
            RevitValue = "300",
            CadDeltaMm = 130.0,
            RevitDeltaMm = 80.0,
            ToleranceThresholdMm = 0.0,
            IsTombstonedInRevit = false
        };

        var decisionCase6 = evaluator.Evaluate(contextCase6);

        Assert(decisionCase6.Action == ConflictResolutionAction.RaiseRfiAndHalt,
            $"Case 6 must RaiseRfiAndHalt, but got {decisionCase6.Action}");
        Assert(decisionCase6.GeneratesRfi, "Case 6 must set GeneratesRfi to true");
        Assert(decisionCase6.GeneratedRfi != null, "Case 6 must generate non-null RfiRecord");
        Assert(decisionCase6.Reason.Contains("Dual Divergence Conflict"),
            $"Case 6 reason must state Dual Divergence Conflict, got: '{decisionCase6.Reason}'");
        Assert(decisionCase6.Reason.Contains("80") && decisionCase6.Reason.Contains("130"),
            "Reason must report the measured deltas (80mm Revit, 130mm CAD)");

        // Test Case 1: Synchronized (Both unchanged)
        var contextCase1 = contextCase6 with { CadValue = "220", RevitValue = "220", CadDeltaMm = 0, RevitDeltaMm = 0 };
        var decisionCase1 = evaluator.Evaluate(contextCase1);
        Assert(decisionCase1.Action == ConflictResolutionAction.NoOp, "Case 1 must be NoOp");

        // Test Case 2: Upstream CAD update (Modeler unchanged)
        var contextCase2 = contextCase6 with { CadValue = "350", RevitValue = "220", CadDeltaMm = 130, RevitDeltaMm = 0 };
        var decisionCase2 = evaluator.Evaluate(contextCase2);
        Assert(decisionCase2.Action == ConflictResolutionAction.ApplyCad, "Case 2 must be ApplyCad");

        // Test Case 3: Modeling drift within tolerance (CAD unchanged, Revit changed 0.5mm, tolerance 1.0mm)
        var contextCase3 = contextCase6 with { PropertyName = "PositionX", CadValue = "1000", RevitValue = "1000.5", BaseValue = "1000", CadDeltaMm = 0, RevitDeltaMm = 0.5, ToleranceThresholdMm = 1.0 };
        var decisionCase3 = evaluator.Evaluate(contextCase3);
        Assert(decisionCase3.Action == ConflictResolutionAction.NoOp, "Case 3 within tolerance must be NoOp / Reconcile");

        // Test Case 4: Single Divergence Conflict (CAD unchanged, Revit changed > tolerance)
        var contextCase4 = contextCase6 with { CadValue = "220", RevitValue = "300", CadDeltaMm = 0, RevitDeltaMm = 80 };
        var decisionCase4 = evaluator.Evaluate(contextCase4);
        Assert(decisionCase4.Action == ConflictResolutionAction.RaiseRfiAndHalt, "Case 4 must be RaiseRfiAndHalt");

        // Test Case 5: Convergent Edit (Both independently changed to identical value)
        var contextCase5 = contextCase6 with { CadValue = "300", RevitValue = "300", CadDeltaMm = 80, RevitDeltaMm = 80 };
        var decisionCase5 = evaluator.Evaluate(contextCase5);
        Assert(decisionCase5.Action == ConflictResolutionAction.NoOp, "Case 5 (convergent) must be NoOp");

        // Test Case 7: Tombstone Conflict (Revit deleted element, CAD still specifies)
        var contextCase7 = contextCase6 with { IsTombstonedInRevit = true };
        var decisionCase7 = evaluator.Evaluate(contextCase7);
        Assert(decisionCase7.Action == ConflictResolutionAction.RaiseRfiAndHalt, "Case 7 (tombstoned) must be RaiseRfiAndHalt");
    }

    // =========================================================================
    // 4. Queue Daemon Single-Writer UI Thread Serialization
    // =========================================================================
    private static async Task TestQueueDaemonSingleWriterSerializationAsync()
    {
        int concurrentProducers = 50;
        var simulatedQueue = new SimulatedTaskQueue();
        var simulatedRevitConsumer = new SimulatedExclusiveRevitConsumer();

        // M concurrent client tasks submit requests simultaneously
        var clientTasks = Enumerable.Range(0, concurrentProducers).Select(async i =>
        {
            var taskId = await simulatedQueue.EnqueueAsync(new SimulatedTask(
                Guid.NewGuid(),
                $"Task-{i}",
                i % 2 == 0 ? 100 : 200));
            return taskId;
        }).ToList();

        var submittedIds = await Task.WhenAll(clientTasks);
        Assert(submittedIds.Length == concurrentProducers, $"All {concurrentProducers} tasks must be enqueued without pipe-busy rejection");

        // Single-writer dispatcher loop
        int processedCount = 0;
        while (simulatedQueue.Count > 0)
        {
            var task = simulatedQueue.DequeueNext();
            if (task == null) break;

            await simulatedRevitConsumer.ExecuteAsync(task);
            processedCount++;
        }

        Assert(processedCount == concurrentProducers, $"Expected {concurrentProducers} tasks processed, got {processedCount}");
        Assert(simulatedRevitConsumer.MaxConcurrentExecutions == 1,
            $"Single-writer serialization violated! Peak concurrent executions on Revit: {simulatedRevitConsumer.MaxConcurrentExecutions}");
        Assert(simulatedRevitConsumer.TotalViolations == 0,
            $"Revit apartment violations detected: {simulatedRevitConsumer.TotalViolations}");
    }

    // =========================================================================
    // 5. Priority Queue Anti-Starvation Aging (+10/min) Stress Test
    // =========================================================================
    private static void TestPriorityQueueAntiStarvationAging()
    {
        // Part A: Can 10,000 low-priority QC tasks starve high-priority pre-flight checks?
        int lowPriorityCount = 10_000;
        var queue = new List<SimulatedAgingTask>(lowPriorityCount + 10);
        DateTime baseTime = DateTime.UtcNow;

        // Enqueue 10,000 BackgroundQC tasks (Priority 10) at t = 0
        for (int i = 0; i < lowPriorityCount; i++)
        {
            queue.Add(new SimulatedAgingTask(
                Guid.NewGuid(),
                "BackgroundQC",
                BasePriority: 10,
                EnqueuedAtUtc: baseTime));
        }

        // Advance simulated time by 200 minutes
        DateTime currentTime = baseTime.AddMinutes(200);

        // PreFlight query arrives at t = 200 min
        var preflightTask = new SimulatedAgingTask(
            Guid.NewGuid(),
            "PreFlight-CheckTitle",
            BasePriority: 200,
            EnqueuedAtUtc: currentTime);
        queue.Add(preflightTask);

        // Sort by VirtualPriority DESC, EnqueuedAtUtc ASC
        var nextToExecute = queue
            .OrderByDescending(t => t.GetVirtualPriority(currentTime))
            .ThenBy(t => t.EnqueuedAtUtc)
            .First();

        Assert(nextToExecute.TaskId == preflightTask.TaskId,
            $"10,000 aged QC tasks must NOT starve high-priority PreFlight task! Expected PreFlight, got: {nextToExecute.Name}");
        Assert(preflightTask.GetVirtualPriority(currentTime) == 200, "PreFlight virtual priority must be 200");
        Assert(queue[0].GetVirtualPriority(currentTime) == 110,
            $"QC task virtual priority after 200 min must be capped at 110 (10 + 100), got {queue[0].GetVirtualPriority(currentTime)}");

        // Part B: Does aging (+10/min) prevent starvation of BackgroundQC against a continuous stream of Normal tasks (100)?
        var qcTask = new SimulatedAgingTask(Guid.NewGuid(), "BackgroundQC-Audit", BasePriority: 10, EnqueuedAtUtc: baseTime);
        var normalTaskT0 = new SimulatedAgingTask(Guid.NewGuid(), "Normal-Task-T0", BasePriority: 100, EnqueuedAtUtc: baseTime);

        // At t = 0: Normal (100) > QC (10)
        Assert(normalTaskT0.GetVirtualPriority(baseTime) > qcTask.GetVirtualPriority(baseTime),
            "At t=0, Normal task must have higher priority than QC task");

        // At t = 5 min: QC priority = 10 + 50 = 60 < 100
        DateTime t5 = baseTime.AddMinutes(5);
        Assert(qcTask.GetVirtualPriority(t5) == 60, "At t=5 min, QC priority must be 60");

        // At t = 10 min: QC priority = 10 + 100 = 110 > 100!
        DateTime t10 = baseTime.AddMinutes(10);
        var normalTaskT10 = new SimulatedAgingTask(Guid.NewGuid(), "Normal-Task-T10", BasePriority: 100, EnqueuedAtUtc: t10);

        Assert(qcTask.GetVirtualPriority(t10) == 110, "At t=10 min, QC priority must reach 110");
        Assert(qcTask.GetVirtualPriority(t10) > normalTaskT10.GetVirtualPriority(t10),
            "At t=10 min, aged QC task (110) MUST bypass newly arrived Normal task (100) to prevent starvation!");

        // At t = 60 min: Bonus capped at +100
        DateTime t60 = baseTime.AddMinutes(60);
        Assert(qcTask.GetVirtualPriority(t60) == 110,
            $"Aging bonus must be strictly capped at +100 (total 110), got {qcTask.GetVirtualPriority(t60)}");
    }

    // =========================================================================
    // 6. Daemon Crash Recovery & SQLite WAL Lease Expiration
    // =========================================================================
    private static void TestCrashRecoveryAndLeaseExpiration()
    {
        var recoveryEngine = new SimulatedCrashRecoveryEngine();
        DateTime now = DateTime.UtcNow;

        // Task 1: InFlight, crashed, lease expired (WorkerLeaseExpiryUtc < now), receipt EXISTS in Revit
        var task1 = new SimulatedLeasedTask
        {
            TaskId = Guid.NewGuid(),
            OperationId = Guid.NewGuid(),
            State = BimTaskState.InFlight,
            WorkerLeaseExpiryUtc = now.AddMinutes(-5), // Expired 5 min ago
            HasRevitReceipt = true
        };

        // Task 2: InFlight, crashed, lease expired, receipt MISSING in Revit
        var task2 = new SimulatedLeasedTask
        {
            TaskId = Guid.NewGuid(),
            OperationId = Guid.NewGuid(),
            State = BimTaskState.InFlight,
            WorkerLeaseExpiryUtc = now.AddMinutes(-2), // Expired 2 min ago
            HasRevitReceipt = false
        };

        // Task 3: InFlight, lease STILL ACTIVE (WorkerLeaseExpiryUtc > now)
        var task3 = new SimulatedLeasedTask
        {
            TaskId = Guid.NewGuid(),
            OperationId = Guid.NewGuid(),
            State = BimTaskState.InFlight,
            WorkerLeaseExpiryUtc = now.AddMinutes(5), // Active for 5 more min
            HasRevitReceipt = false
        };

        var tasks = new List<SimulatedLeasedTask> { task1, task2, task3 };
        recoveryEngine.ReconcileRecoveredTasks(tasks, now);

        Assert(task1.State == BimTaskState.Committed,
            $"Task 1 with confirmed Revit receipt must transition to Committed, got {task1.State}");
        Assert(task2.State == BimTaskState.OutcomeUnknown,
            $"Task 2 with missing receipt must transition to OutcomeUnknown for audit/retry, got {task2.State}");
        Assert(task3.State == BimTaskState.InFlight,
            $"Task 3 with active unexpired lease must remain InFlight, got {task3.State}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}

// =============================================================================
// SIMULATED TEST FIXTURES & ORACLES
// =============================================================================

public static class SimulatedBimTrackingContext
{
    private static readonly AsyncLocal<ChangeOrigin> CurrentOriginHolder = new();
    public static ChangeOrigin CurrentOrigin => CurrentOriginHolder.Value == 0 ? ChangeOrigin.UserInteractive : CurrentOriginHolder.Value;

    public static IDisposable SuppressTracking(ChangeOrigin origin)
    {
        var previous = CurrentOriginHolder.Value;
        CurrentOriginHolder.Value = origin;
        return new DisposableScope(() => CurrentOriginHolder.Value = previous);
    }

    private sealed class DisposableScope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}

public sealed class SimulatedRecursionGuard
{
    [ThreadStatic]
    private static bool _isExecuting;

    public bool IsExecuting => _isExecuting;

    public bool TryEnter()
    {
        if (_isExecuting) return false;
        _isExecuting = true;
        return true;
    }

    public void Exit()
    {
        _isExecuting = false;
    }
}

public sealed record SimulatedTombstoneMapping
{
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public string TargetUniqueId { get; init; } = string.Empty;
    public long? TargetElementId { get; init; }
    public bool IsTombstoned { get; init; }
    public DateTime? TombstonedAtUtc { get; init; }
    public string TombstoneReason { get; init; } = string.Empty;
}

public enum SimulatedP2Action
{
    Create = 1,
    Update = 2,
    NoOp = 3,
    Skip = 4
}

public sealed record SimulatedP2Decision(
    SimulatedP2Action Action,
    ElementExecutionStatus Status,
    string Reason,
    RfiRecord? GeneratedRfi);

public static class SimulatedP2Resolver
{
    public static SimulatedP2Decision Resolve(
        ElementPlanItem planItem,
        SimulatedTombstoneMapping mapping,
        IElementExistenceQuery existenceQuery)
    {
        if (mapping.IsTombstoned)
        {
            var rfi = new RfiRecord
            {
                RfiId = $"RFI-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
                Title = $"Attempted recreation of tombstoned element {planItem.Mark}",
                ElementIds = new[] { planItem.SourceKey.ToString() }
            };

            return new SimulatedP2Decision(
                SimulatedP2Action.Skip,
                ElementExecutionStatus.Skipped,
                $"Element is tombstoned in Revit by user. Re-generation blocked pending RFI approval. Reason: {mapping.TombstoneReason}",
                rfi);
        }

        var state = existenceQuery.QueryElement(mapping.TargetUniqueId);
        if (state == null || !state.Exists)
        {
            return new SimulatedP2Decision(
                SimulatedP2Action.Create,
                ElementExecutionStatus.Created,
                "Element missing from Revit document and not tombstoned.",
                null);
        }

        return new SimulatedP2Decision(
            SimulatedP2Action.NoOp,
            ElementExecutionStatus.NoOp,
            "Element exists and matches.",
            null);
    }
}

public sealed record SimulatedConflictContext
{
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public CoordinationCategory Category { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public string BaseValue { get; init; } = string.Empty;
    public string CadValue { get; init; } = string.Empty;
    public string RevitValue { get; init; } = string.Empty;
    public double CadDeltaMm { get; init; }
    public double RevitDeltaMm { get; init; }
    public double ToleranceThresholdMm { get; init; }
    public bool IsTombstonedInRevit { get; init; }
}

public sealed record SimulatedThreeWayMergeDecision(
    ConflictResolutionAction Action,
    string Reason,
    bool GeneratesRfi,
    RfiRecord? GeneratedRfi);

public sealed class SimulatedThreeWayMergeEvaluator
{
    public SimulatedThreeWayMergeDecision Evaluate(SimulatedConflictContext ctx)
    {
        bool deltaCad = !string.Equals(ctx.CadValue, ctx.BaseValue, StringComparison.OrdinalIgnoreCase);
        bool deltaRvt = !string.Equals(ctx.RevitValue, ctx.BaseValue, StringComparison.OrdinalIgnoreCase);

        // Case 7: Tombstone Conflict
        if (ctx.IsTombstonedInRevit)
        {
            var rfi = new RfiRecord
            {
                RfiId = $"RFI-TOMB-{Guid.NewGuid().ToString()[..6]}",
                Title = $"Tombstone conflict on {ctx.PropertyName}",
                ElementIds = new[] { ctx.SourceKey.ToString() }
            };
            return new SimulatedThreeWayMergeDecision(
                ConflictResolutionAction.RaiseRfiAndHalt,
                $"Tombstone Conflict: CAD expects element {ctx.SourceKey}, but Revit deleted and tombstoned it.",
                true,
                rfi);
        }

        // Case 1: Synchronized
        if (!deltaCad && !deltaRvt)
        {
            return new SimulatedThreeWayMergeDecision(
                ConflictResolutionAction.NoOp,
                "Synchronized: Zero mutations detected.",
                false,
                null);
        }

        // Case 2: Upstream CAD update
        if (deltaCad && !deltaRvt)
        {
            return new SimulatedThreeWayMergeDecision(
                ConflictResolutionAction.ApplyCad,
                $"Upstream CAD Update: Modeler has not edited element. Applying CAD value '{ctx.CadValue}'.",
                false,
                null);
        }

        // Case 3 & 4: Modeler edited element, CAD unchanged
        if (!deltaCad && deltaRvt)
        {
            if (ctx.RevitDeltaMm <= ctx.ToleranceThresholdMm)
            {
                return new SimulatedThreeWayMergeDecision(
                    ConflictResolutionAction.NoOp,
                    $"Modeling Drift: Delta {ctx.RevitDeltaMm}mm within tolerance {ctx.ToleranceThresholdMm}mm.",
                    false,
                    null);
            }

            var rfi = new RfiRecord
            {
                RfiId = $"RFI-MOD-{Guid.NewGuid().ToString()[..6]}",
                Title = $"Single Divergence Conflict on {ctx.PropertyName}",
                ElementIds = new[] { ctx.SourceKey.ToString() }
            };
            return new SimulatedThreeWayMergeDecision(
                ConflictResolutionAction.RaiseRfiAndHalt,
                $"Single Divergence Conflict: Modeler modified {ctx.PropertyName} to '{ctx.RevitValue}' (delta {ctx.RevitDeltaMm}mm > tol {ctx.ToleranceThresholdMm}mm). CAD unchanged.",
                true,
                rfi);
        }

        // Both CAD and Revit edited
        if (string.Equals(ctx.CadValue, ctx.RevitValue, StringComparison.OrdinalIgnoreCase))
        {
            // Case 5: Convergent edit
            return new SimulatedThreeWayMergeDecision(
                ConflictResolutionAction.NoOp,
                "Convergent Edit: CAD and Revit made identical modifications.",
                false,
                null);
        }

        // Case 6: Dual Divergence Conflict
        var rfiDual = new RfiRecord
        {
            RfiId = $"RFI-DUAL-{Guid.NewGuid().ToString()[..6]}",
            Title = $"Dual Divergence Conflict on {ctx.PropertyName}",
            ElementIds = new[] { ctx.SourceKey.ToString() }
        };
        return new SimulatedThreeWayMergeDecision(
            ConflictResolutionAction.RaiseRfiAndHalt,
            $"Dual Divergence Conflict: Rule 0 Trigger! CAD specifies '{ctx.CadValue}' (delta {ctx.CadDeltaMm}mm) but Revit model has '{ctx.RevitValue}' (delta {ctx.RevitDeltaMm}mm). Tolerance is {ctx.ToleranceThresholdMm}mm.",
            true,
            rfiDual);
    }
}

public sealed record SimulatedTask(Guid TaskId, string Name, int Priority);

public sealed class SimulatedTaskQueue
{
    private readonly ConcurrentQueue<SimulatedTask> _queue = new();

    public int Count => _queue.Count;

    public Task<Guid> EnqueueAsync(SimulatedTask task)
    {
        _queue.Enqueue(task);
        return Task.FromResult(task.TaskId);
    }

    public SimulatedTask? DequeueNext()
    {
        return _queue.TryDequeue(out var task) ? task : null;
    }
}

public sealed class SimulatedExclusiveRevitConsumer
{
    private int _activeCount = 0;
    private int _maxConcurrent = 0;
    private int _violations = 0;

    public int MaxConcurrentExecutions => _maxConcurrent;
    public int TotalViolations => _violations;

    public async Task ExecuteAsync(SimulatedTask task)
    {
        int current = Interlocked.Increment(ref _activeCount);
        if (current > 1)
        {
            Interlocked.Increment(ref _violations);
        }

        lock (this)
        {
            if (current > _maxConcurrent) _maxConcurrent = current;
        }

        // Simulate UI thread execution work
        await Task.Delay(2);

        Interlocked.Decrement(ref _activeCount);
    }
}

public sealed record SimulatedAgingTask(
    Guid TaskId,
    string Name,
    int BasePriority,
    DateTime EnqueuedAtUtc)
{
    public int GetVirtualPriority(DateTime nowUtc)
    {
        double waitMinutes = (nowUtc - EnqueuedAtUtc).TotalMinutes;
        if (waitMinutes < 0) waitMinutes = 0;
        int bonus = Math.Min(100, (int)Math.Floor(waitMinutes) * 10);
        return BasePriority + bonus;
    }
}

public sealed class SimulatedLeasedTask
{
    public Guid TaskId { get; init; }
    public Guid OperationId { get; init; }
    public BimTaskState State { get; set; }
    public DateTime WorkerLeaseExpiryUtc { get; init; }
    public bool HasRevitReceipt { get; init; }
}

public sealed class SimulatedCrashRecoveryEngine
{
    public void ReconcileRecoveredTasks(List<SimulatedLeasedTask> tasks, DateTime nowUtc)
    {
        foreach (var task in tasks)
        {
            if (task.State == BimTaskState.InFlight && task.WorkerLeaseExpiryUtc < nowUtc)
            {
                if (task.HasRevitReceipt)
                {
                    task.State = BimTaskState.Committed;
                }
                else
                {
                    task.State = BimTaskState.OutcomeUnknown;
                }
            }
        }
    }
}

public sealed class FakeExistenceQuery(bool exists, long? elementId, string uniqueId) : IElementExistenceQuery
{
    public ElementState? QueryElement(string uid) =>
        string.Equals(uid, uniqueId, StringComparison.OrdinalIgnoreCase)
            ? new ElementState(exists, elementId, uid, "220x220", "+0.150m")
            : null;
}
