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
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class Program
{
    private static int _total = 0;
    private static int _passed = 0;
    private static int _failed = 0;
    private static readonly List<string> _failures = new();

    public static async Task<int> Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  EMPIRICAL ADVERSARIAL CHALLENGER (M4 IT 2) — PREAMBLE & IDEMPOTENCY HARNESS");
        Console.WriteLine("================================================================================");
        Console.WriteLine();

        // Suite 1: Preamble Decomposition Fidelity
        TestPreambleFidelity();
        Console.WriteLine();

        // Suite 2: Roslyn Script Syntax and Token Stream Verification
        TestRoslynScriptParsing();
        Console.WriteLine();

        // Suite 3: Auto-Reconciliation & Idempotency Simulation
        await TestAutoReconcileAndIdempotencyAsync();
        Console.WriteLine();

        // Suite 4: Adversarial Edge Cases in Receipts & Ledger
        await TestAdversarialRecoveryEdgeCasesAsync();
        Console.WriteLine();

        Console.WriteLine("================================================================================");
        Console.WriteLine($"TOTAL ASSERTIONS: {_passed}/{_total} PASSED ({( _failed == 0 ? "100%" : $"{_failed} FAILED" )})");
        Console.WriteLine("================================================================================");

        if (_failures.Count > 0)
        {
            Console.WriteLine("FAILURES ENCOUNTERED:");
            foreach (var f in _failures) Console.WriteLine($" - {f}");
            return 1;
        }

        return 0;
    }

    private static void AssertTest(string name, bool condition, string detail = "")
    {
        _total++;
        if (condition)
        {
            _passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        else
        {
            _failed++;
            string msg = $"[FAIL] {name} | {detail}";
            Console.WriteLine(msg);
            _failures.Add(msg);
        }
    }

    private static void TestPreambleFidelity()
    {
        Console.WriteLine("--- SUITE 1: BimPlanRunnerScriptTemplate Preamble Decomposition Fidelity ---");
        string fullScript = BimPlanRunnerScriptTemplate.GetScriptCode();

        AssertTest("Full script is non-null and substantial (>5000 chars)", !string.IsNullOrWhiteSpace(fullScript) && fullScript.Length > 5000);

        // 1. Coordinate Helpers
        AssertTest("Contains Ft(mm) conversion helper", fullScript.Contains("double Ft(double mm) => mm / 304.8;"));
        AssertTest("Contains Mm(ft) conversion helper", fullScript.Contains("double Mm(double ft) => ft * 304.8;"));
        AssertTest("Contains ToXyz(x, y, z) coordinate helper", fullScript.Contains("XYZ ToXyz(double xMm, double yMm, double zMm = 0.0) => new XYZ(Ft(xMm), Ft(yMm), Ft(zMm));"));

        // 2. Pre-Flight Verification Gate
        AssertTest("Reads targetDoc object from args", fullScript.Contains("var targetDoc = args.Obj(\"targetDoc\");"));
        AssertTest("Extracts expected Title", fullScript.Contains("string expectedTitle = targetDoc.Str(\"title\", string.Empty);"));
        AssertTest("Extracts expected VersionGuid", fullScript.Contains("string expectedVersionGuidStr = targetDoc.Str(\"versionGuid\", string.Empty);"));
        AssertTest("Parses expected VersionGuid safely", fullScript.Contains("Guid.TryParse(expectedVersionGuidStr, out var parsedGuid)"));
        AssertTest("Queries active DocumentVersion via Document.GetDocumentVersion(doc)", fullScript.Contains("var docVer = Document.GetDocumentVersion(doc);"));
        AssertTest("Handles null docVer gracefully with Guid.Empty fallback", fullScript.Contains("Guid actualVersionGuid = docVer != null ? docVer.VersionGUID : Guid.Empty;"));
        AssertTest("Pre-Flight checks title mismatch with StringComparison.OrdinalIgnoreCase", fullScript.Contains("string.Equals(doc.Title, expectedTitle, StringComparison.OrdinalIgnoreCase)"));
        AssertTest("Pre-Flight checks version mismatch against actualVersionGuid", fullScript.Contains("actualVersionGuid != expectedVersionGuid"));
        AssertTest("Pre-Flight throws InvalidOperationException with [PRE_FLIGHT_FAILED] tag", fullScript.Contains("throw new InvalidOperationException(") && fullScript.Contains("[PRE_FLIGHT_FAILED]"));

        // 3. Datum Collectors
        AssertTest("Caches existing Levels via FilteredElementCollector(doc)", fullScript.Contains("var existingLevels = new FilteredElementCollector(doc)") && fullScript.Contains("OfClass(typeof(Level))"));
        AssertTest("Caches existing Grids via FilteredElementCollector(doc)", fullScript.Contains("var existingGrids = new FilteredElementCollector(doc)") && fullScript.Contains("OfClass(typeof(Grid))"));
        AssertTest("Caches existing ViewPlans", fullScript.Contains("var existingViews = new FilteredElementCollector(doc)") && fullScript.Contains("OfClass(typeof(ViewPlan))"));
        AssertTest("Extracts existingViewLevelIds HashSet", fullScript.Contains("var existingViewLevelIds = existingViews"));
        AssertTest("Resolves planType for FloorPlan", fullScript.Contains("ViewFamilyType planType = new FilteredElementCollector(doc)") && fullScript.Contains("ViewFamily.FloorPlan"));
        AssertTest("Collects StructuralColumns symbols dictionary", fullScript.Contains("BuiltInCategory.OST_StructuralColumns") && fullScript.Contains("columnSymbols"));
        AssertTest("Collects StructuralFraming symbols dictionary", fullScript.Contains("BuiltInCategory.OST_StructuralFraming") && fullScript.Contains("beamSymbols"));

        // 4. Type Collectors
        AssertTest("Collects Basic WallTypes dictionary", fullScript.Contains("var wallTypes = new FilteredElementCollector(doc)") && fullScript.Contains("WallKind.Basic"));
        AssertTest("Collects FloorTypes dictionary", fullScript.Contains("var floorTypes = new FilteredElementCollector(doc)") && fullScript.Contains("OfClass(typeof(FloorType))"));
        AssertTest("Collects Doors and Windows symbols dictionary", fullScript.Contains("BuiltInCategory.OST_Doors") && fullScript.Contains("doorWinSymbols"));
        AssertTest("Collects Plumbing Fixture symbols dictionary", fullScript.Contains("BuiltInCategory.OST_PlumbingFixtures") && fullScript.Contains("fixtureSymbols"));
        AssertTest("Collects RebarBarType dictionary", fullScript.Contains("Autodesk.Revit.DB.Structure.RebarBarType") && fullScript.Contains("rebarBarTypes"));
        AssertTest("Resolves PipingSystemType and PipeType", fullScript.Contains("Autodesk.Revit.DB.Plumbing.PipingSystemType") && fullScript.Contains("Autodesk.Revit.DB.Plumbing.PipeType"));

        // 5. Item Loop & SubTransaction Setup
        AssertTest("Initializes createdWalls and createdElements tracking dictionaries", fullScript.Contains("var createdWalls = new Dictionary<string, Wall>") && fullScript.Contains("var createdElements = new Dictionary<string, Element>"));
        AssertTest("Extracts items list and records tracking list", fullScript.Contains("var items = args.List(\"items\");") && fullScript.Contains("var records = new List<Dictionary<string, object>>();"));
        AssertTest("Initiates foreach loop over items", fullScript.Contains("foreach (var item in items)"));
        AssertTest("Wraps each item execution in atomic SubTransaction", fullScript.Contains("using (var subTx = new SubTransaction(doc))") && fullScript.Contains("subTx.Start();"));
        AssertTest("Captures SubTransaction errors with subTx.RollBack() and ERR_REVIT_API_EXCEPTION", fullScript.Contains("subTx.RollBack();") && fullScript.Contains("ERR_REVIT_API_EXCEPTION"));

        // 6. Extensible Storage Receipt Writing
        AssertTest("Receipt writing checks !dryRun && !string.IsNullOrEmpty(planHash)", fullScript.Contains("if (!dryRun && !string.IsNullOrEmpty(planHash))"));
        AssertTest("Receipt writing uses fixed schema GUID D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90", fullScript.Contains("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90"));
        AssertTest("Receipt writing stores on DataStorage element", fullScript.Contains("DataStorage.Create(doc)") || fullScript.Contains("receiptDs.SetEntity(entity);"));
    }

    private static void TestRoslynScriptParsing()
    {
        Console.WriteLine("--- SUITE 2: Roslyn Script Syntax and Token Stream Verification ---");
        string fullScript = BimPlanRunnerScriptTemplate.GetScriptCode();

        var parseOptions = CSharpParseOptions.Default.WithKind(SourceCodeKind.Script).WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(fullScript, parseOptions);
        var diags = syntaxTree.GetDiagnostics().ToList();

        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        AssertTest("Synthesized C# Roslyn script compiles with 0 syntax errors", errors.Count == 0,
            string.Join("; ", errors.Select(e => e.GetMessage())));

        var tokens = syntaxTree.GetRoot().DescendantTokens().ToList();
        AssertTest("Script token stream is rich (>1000 tokens)", tokens.Count > 1000, $"Total tokens: {tokens.Count}");

        // Assert crucial statements exist in AST
        var root = syntaxTree.GetRoot();
        bool hasTryCatch = root.DescendantNodes().Any(n => n.IsKind(SyntaxKind.TryStatement));
        bool hasUsing = root.DescendantNodes().Any(n => n.IsKind(SyntaxKind.UsingStatement));
        bool hasForeach = root.DescendantNodes().Any(n => n.IsKind(SyntaxKind.ForEachStatement));

        AssertTest("AST contains using statement for SubTransaction", hasUsing);
        AssertTest("AST contains try-catch statement for exception capture", hasTryCatch);
        AssertTest("AST contains foreach statement for item processing", hasForeach);
    }

    private static async Task TestAutoReconcileAndIdempotencyAsync()
    {
        Console.WriteLine("--- SUITE 3: Auto-Reconciliation & Idempotency Simulation ---");

        string tempDir = Path.Combine(Path.GetTempPath(), "challenger_idempotency_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string journalFile = Path.Combine(tempDir, "journal.jsonl");

        try
        {
            // =========================================================================
            // SCENARIO 1: Disconnect AFTER commit -> Receipt Found -> Auto-Reconcile Committed -> Rerun NoOp
            // =========================================================================
            Console.WriteLine("Testing Scenario 1: Disconnect after commit -> Receipt Found -> Rerun 0 duplicates...");
            using (var journal = new IntentJournal(journalFile))
            {
                var fakePipe = new FakePipeClient();
                var ledger = new BimCoordinationLedgerService();
                var engine = new BimPipelineEngine(fakePipe, intentJournal: journal) { LedgerService = ledger };

                var plan = BuildTestPlan(4);
                var opId = plan.Options.OperationId;

                // Step 1: Simulate network drop after Revit commit
                fakePipe.ExecuteFunc = req => throw new IOException("Pipe broken during response read.");

                var run1 = await engine.ExecutePlanJournaledAsync(plan, ledger);
                AssertTest("Run1 fails with IsOutcomeUnknown=true on pipe break", !run1.Success && run1.IsOutcomeUnknown);

                var inJournal = await journal.GetIntentAsync(opId);
                AssertTest("Journal logs intent as OutcomeUnknown", inJournal != null && inJournal.Status == IntentStatus.OutcomeUnknown);

                // Step 2: In Revit, transaction committed and receipt was persisted
                var committedMap = plan.Items.Select((it, idx) => new { it.SourceKey, Uid = $"rvt-col-uid-{idx}" })
                    .ToDictionary(x => x.SourceKey.ToString(), x => x.Uid);

                fakePipe.ReceiptsToReturn.Add(new CoordinationReceiptRecord
                {
                    OperationId = opId,
                    PlanHash = plan.PlanHash,
                    CommittedAtUtc = DateTime.UtcNow.ToString("o"),
                    ElementMap = committedMap
                });

                // Pipe reconnects
                fakePipe.ExecuteFunc = null;

                // Step 3: Auto-reconciliation
                var reconcileService = new ReconcileService(journal, fakePipe, ledger);
                var recResults = await reconcileService.ReconcileAllUnknownIntentsAsync();

                AssertTest("ReconcileAllUnknownIntents returns 1 result", recResults.Count == 1);
                AssertTest("Reconcile marks status as Committed", recResults[0].Status == ReconcileStatus.Committed);
                AssertTest("Reconcile mapped count is 4", recResults[0].MappedElementCount == 4);

                var afterRecIntent = await journal.GetIntentAsync(opId);
                AssertTest("Journal intent transitioned to Committed", afterRecIntent != null && afterRecIntent.Status == IntentStatus.Committed);

                // Check ledger has all 4 mappings
                bool allInLedger = plan.Items.All(it => ledger.TryGetTarget(it.SourceKey, out var u) && !string.IsNullOrEmpty(u));
                AssertTest("Ledger holds mappings for all 4 items", allInLedger);

                // Step 4: Re-run plan -> Expect 0 Created, 4 NoOp
                fakePipe.ExecuteFunc = req =>
                {
                    // Inspect that TargetUniqueId was populated from ledger into each item
                    var argsDoc = JsonDocument.Parse(req.Args!.Value.GetRawText());
                    var itemsArr = argsDoc.RootElement.GetProperty("items").EnumerateArray().ToList();
                    bool allHaveTarget = itemsArr.All(it => it.TryGetProperty("targetUniqueId", out var tu) && !string.IsNullOrEmpty(tu.GetString()));

                    var records = plan.Items.Select(it => new Dictionary<string, object>
                    {
                        ["sourceKey"] = it.SourceKey.ToString(),
                        ["category"] = it.Category.ToString(),
                        ["mark"] = it.Mark,
                        ["status"] = "NoOp",
                        ["targetUniqueId"] = committedMap[it.SourceKey.ToString()]
                    }).ToList();

                    var resDoc = JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        planId = opId.ToString("D"),
                        total = 4,
                        created = 0,
                        updated = 0,
                        noOp = 4,
                        skipped = 0,
                        failed = 0,
                        records = records
                    }));

                    return new ExecuteResult
                    {
                        IsError = !allHaveTarget,
                        Message = allHaveTarget ? null : "Missing targetUniqueIds in request payload!",
                        Value = resDoc.RootElement.Clone()
                    };
                };

                var run2 = await engine.ExecutePlanJournaledAsync(plan, ledger);
                AssertTest("Run2 succeeds with zero errors", run2.Success);
                AssertTest("Run2 creates exactly 0 duplicate elements", run2.CreatedCount == 0);
                AssertTest("Run2 records all 4 elements as NoOp", run2.NoOpCount == 4);
                AssertTest("Run2 has 0 failed items", run2.FailedCount == 0);
            }

            // =========================================================================
            // SCENARIO 2: Disconnect BEFORE commit -> Receipt Absent -> Auto-Reconcile Failed -> Rerun Cleanly
            // =========================================================================
            Console.WriteLine("Testing Scenario 2: Disconnect before commit -> Receipt Absent -> Rerun Cleanly...");
            string journalFile2 = Path.Combine(tempDir, "journal2.jsonl");
            using (var journal2 = new IntentJournal(journalFile2))
            {
                var fakePipe2 = new FakePipeClient();
                var ledger2 = new BimCoordinationLedgerService();
                var engine2 = new BimPipelineEngine(fakePipe2, intentJournal: journal2) { LedgerService = ledger2 };

                var plan2 = BuildTestPlan(2);
                var opId2 = plan2.Options.OperationId;

                fakePipe2.ExecuteFunc = req => throw new TimeoutException("Execution timed out before Revit committed.");

                var run1 = await engine2.ExecutePlanJournaledAsync(plan2, ledger2);
                AssertTest("Scenario2: Run1 fails with IsOutcomeUnknown=true", !run1.Success && run1.IsOutcomeUnknown);

                // Reconnect with 0 receipts in Revit
                fakePipe2.ExecuteFunc = null;
                fakePipe2.ReceiptsToReturn.Clear();

                var reconcileService2 = new ReconcileService(journal2, fakePipe2, ledger2);
                var recResults2 = await reconcileService2.ReconcileAllUnknownIntentsAsync();

                AssertTest("Scenario2: Reconcile identifies missing receipt and returns RolledBack", recResults2[0].Status == ReconcileStatus.RolledBack);
                AssertTest("Scenario2: Ledger remains completely empty", ledger2.Mappings.Count == 0);

                var afterRecIntent2 = await journal2.GetIntentAsync(opId2);
                AssertTest("Scenario2: Journal intent marked Failed with 'Receipt absent' reason",
                    afterRecIntent2 != null && afterRecIntent2.Status == IntentStatus.Failed && (afterRecIntent2.ErrorReason?.Contains("Receipt absent") ?? false));

                // Step 3: Re-run plan -> creates elements cleanly
                fakePipe2.ExecuteFunc = req =>
                {
                    var records = plan2.Items.Select((it, i) => new Dictionary<string, object>
                    {
                        ["sourceKey"] = it.SourceKey.ToString(),
                        ["category"] = it.Category.ToString(),
                        ["mark"] = it.Mark,
                        ["status"] = "Created",
                        ["targetUniqueId"] = $"clean-uid-{i}"
                    }).ToList();

                    var resDoc = JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        planId = plan2.Options.OperationId.ToString("D"),
                        total = 2,
                        created = 2,
                        updated = 0,
                        noOp = 0,
                        skipped = 0,
                        failed = 0,
                        records = records
                    }));

                    return new ExecuteResult { IsError = false, Value = resDoc.RootElement.Clone() };
                };

                var run2 = await engine2.ExecutePlanJournaledAsync(plan2, ledger2);
                AssertTest("Scenario2: Clean rerun succeeds", run2.Success);
                AssertTest("Scenario2: Clean rerun creates exactly 2 elements", run2.CreatedCount == 2);
                AssertTest("Scenario2: Ledger now populated with 2 elements", ledger2.Mappings.Count == 2);
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private static async Task TestAdversarialRecoveryEdgeCasesAsync()
    {
        Console.WriteLine("--- SUITE 4: Adversarial Edge Cases in Recovery ---");

        // 1. Corrupt Non-Object Array Elements in Receipts
        var corruptDoc = JsonDocument.Parse("""
        {
            "count": 3,
            "receipts": [
                "a string literal",
                12345,
                true,
                null,
                [1, 2, 3],
                {
                    "operationId": "A1B2C3D4-E5F6-0000-0000-111122223333",
                    "planHash": "valid-hash-1",
                    "committedAtUtc": "2026-10-07T22:00:00Z",
                    "elementMapJson": "{\"Doc#Col:C1\":\"uid-1\"}"
                }
            ]
        }
        """);

        var parsed = ExtensibleStorageReceiptReader.ParseReceipts(corruptDoc.RootElement);
        AssertTest("ExtensibleStorageReceiptReader skips non-object primitives and extracts only valid object", parsed.Count == 1);
        AssertTest("Extracted receipt operationId matches", parsed[0].OperationId == new Guid("A1B2C3D4-E5F6-0000-0000-111122223333"));
        AssertTest("Extracted receipt elementMap has 1 mapping", parsed[0].ElementMap.Count == 1 && parsed[0].ElementMap["Doc#Col:C1"] == "uid-1");

        // 2. Corrupt ElementMapJson in Receipt
        var corruptMapReceipt = ExtensibleStorageReceiptReader.ParseElementMap("{ corrupt json not closed ");
        AssertTest("Corrupt elementMapJson gracefully returns empty dictionary", corruptMapReceipt.Count == 0);

        // 3. Pipe Disconnect DURING ReconcileSingleIntentAsync
        string tempDir = Path.Combine(Path.GetTempPath(), "challenger_reconcile_disc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            using var journal = new IntentJournal(Path.Combine(tempDir, "journal.jsonl"));
            var opId = Guid.NewGuid();
            await journal.RecordIntentAsync(new IntentJournalRecord
            {
                OperationId = opId,
                PlanHash = "hash-x",
                Status = IntentStatus.OutcomeUnknown
            });

            var brokenPipe = new FakePipeClient { ExecuteFunc = req => throw new IOException("Pipe failed during query") };
            var ledger = new BimCoordinationLedgerService();
            var service = new ReconcileService(journal, brokenPipe, ledger);

            var recRes = await service.ReconcileIntentAsync(opId);
            AssertTest("Pipe failure during reconcile returns StillUnknown", recRes.Status == ReconcileStatus.StillUnknown);

            var intent = await journal.GetIntentAsync(opId);
            AssertTest("Intent status remains preserved as OutcomeUnknown", intent != null && intent.Status == IntentStatus.OutcomeUnknown);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private static BimExecutionPlan BuildTestPlan(int count)
    {
        var items = Enumerable.Range(1, count).Select(i => new ElementPlanItem
        {
            SourceKey = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, $"C{i}"),
            Category = CoordinationCategory.Column,
            Mark = $"C{i}",
            TargetLevelName = "Level 1"
        }).ToList();

        return new BimExecutionPlan
        {
            SourceModelName = "TestModel",
            PlanHash = "hash_" + Guid.NewGuid().ToString("N"),
            TargetDoc = new DocumentIdentitySnapshot { Title = "Model.rvt", VersionGuid = Guid.NewGuid() },
            Items = items,
            Options = new ExecutionOptions
            {
                OperationId = Guid.NewGuid(),
                DryRun = false,
                TransactionLabel = "Test Plan"
            }
        };
    }

    private sealed class FakePipeClient : IRevitPipeClient
    {
        public bool IsConnected { get; set; } = true;
        public string PipeName => "fake-pipe";
        public List<CoordinationReceiptRecord> ReceiptsToReturn { get; } = new();
        public Func<ExecuteRequest, ExecuteResult>? ExecuteFunc { get; set; }

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
            if (ExecuteFunc != null)
            {
                return Task.FromResult(ExecuteFunc(request));
            }

            // Default handler for receipt queries
            if (request.Code.Contains("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90") || request.Label.Contains("Receipt"))
            {
                var receiptsObj = ReceiptsToReturn.Select(r => new
                {
                    operationId = r.OperationId.ToString("D"),
                    planHash = r.PlanHash,
                    committedAtUtc = r.CommittedAtUtc,
                    elementMapJson = JsonSerializer.Serialize(r.ElementMap)
                }).ToList();

                var json = JsonSerializer.Serialize(new
                {
                    count = receiptsObj.Count,
                    receipts = receiptsObj
                });

                return Task.FromResult(new ExecuteResult
                {
                    IsError = false,
                    Value = JsonDocument.Parse(json).RootElement.Clone()
                });
            }

            return Task.FromResult(new ExecuteResult { IsError = false });
        }

        public Task<ContextResult> GetContextAsync(ContextRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ContextResult());

        public Task<JsonElement?> GetContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<JsonElement?>(null);

        public Task<bool> PingAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> CancelAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
