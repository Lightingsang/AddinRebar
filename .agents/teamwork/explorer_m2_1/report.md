# Investigation Report: Target Document Identity Binding & Preflight Check (R2.1)

**Agent**: `explorer_m2_1`  
**Date**: 2026-10-08  
**Milestone**: M2 (Phase 2 — P1 Document Identity, Complete Hash & Journal Hydration)  
**Deliverable**: `HPBim` (`HPBim.Cli`, `HPBim.Client`, `HPBim.Core`)  

---

## 1. Executive Summary

This investigation analyzes **Requirement R2.1 (Target Document Identity Binding & Preflight Check)**:
1. **CLI `generate` & Pipeline Binding**: Ensure CLI `generate` (`HPBim.Cli/Commands/GenerateCommand.cs`) and `BimPipelineEngine` automatically query active Revit document metadata (`Title`, `PathName`, `VersionGUID`) on the Revit API thread via Named Pipe (`DocumentIdentityQuery`) before compiling the plan. Tightly bind this snapshot into both `BimExecutionPlan` and `IntentJournalRecord`.
2. **Roslyn Runner Preflight Verification**: In `BimPlanRunnerScriptTemplate.cs`, strengthen pre-flight checks inside the Revit bridge script to verify active document Title, PathName, and VersionGUID against the plan's target identity, rejecting mismatches with explicit error codes (`[ERR_DOCUMENT_MISMATCH]`, `[ERR_STALE_DOCUMENT_SNAPSHOT]`, `[ERR_NO_ACTIVE_DOCUMENT]`).
3. **Zero-Regression & Clean Code Compliance**: Guarantee that all methods adhere to Rule M3 ($\le 50$ lines), files adhere to Rule C2 ($\le 300$ lines), and all 840 existing tests continue to pass.

---

## 2. As-Is Codebase Analysis & Identified Deficiencies

### 2.1. CLI `generate` Decoupled from Active Document Identity
- **Current Observation** (`HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs`, lines 66–85):
  `GenerateCommand.RunPipelineAsync` instantiates `BimPipelineEngine` and immediately calls `pipeline.ExecuteAsync(model, options, ct)` with `options = new BimPipelineOptions { DryRun = dryRun, AtomicDryRun = dryRun, UseExecutionPlan = true }`.
  It does **not** query the active Revit document before compiling or executing the plan.
- **Current Observation** (`HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs`, lines 60–71):
  ```csharp
  if (options.UseExecutionPlan)
  {
      var plan = ExecutionPlanBuilder.Build(model, options: new ExecutionOptions
      {
          DryRun = options.DryRun,
          TimeoutSeconds = options.TimeoutSecondsPerStage * 2,
          CreatePlanViews = true
      });
      return IntentJournal != null
          ? await ExecutePlanJournaledAsync(plan, cancellationToken).ConfigureAwait(false)
          : await ExecutePlanAsync(plan, cancellationToken).ConfigureAwait(false);
  }
  ```
  `ExecutionPlanBuilder.Build` is called with `targetDoc: null`, defaulting to `new DocumentIdentitySnapshot()`, which has `Title = string.Empty` and `VersionGuid = Guid.Empty`.
- **Consequence**:
  `plan.TargetDoc.IsEmpty` is `true`. `plan.PlanHash` is computed without target document identity. When the plan is dispatched over the pipe, `args.Obj("targetDoc")` contains empty values.

### 2.2. Preflight Check Bypass in Roslyn Runner Script
- **Current Observation** (`HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`, lines 43–64):
  ```csharp
  var targetDoc = args.Obj("targetDoc");
  string expectedTitle = targetDoc.Str("title", string.Empty);
  string expectedVersionGuidStr = targetDoc.Str("versionGuid", string.Empty);
  Guid expectedVersionGuid = Guid.TryParse(expectedVersionGuidStr, out var parsedGuid) ? parsedGuid : Guid.Empty;

  var docVer = Document.GetDocumentVersion(doc);
  Guid actualVersionGuid = docVer != null ? docVer.VersionGUID : Guid.Empty;

  bool hasExpectedTitle = !string.IsNullOrEmpty(expectedTitle);
  bool hasExpectedVersion = expectedVersionGuid != Guid.Empty;

  if (hasExpectedTitle || hasExpectedVersion)
  {
      bool titleMismatch = hasExpectedTitle && !string.Equals(doc.Title, expectedTitle, StringComparison.OrdinalIgnoreCase);
      bool versionMismatch = hasExpectedVersion && actualVersionGuid != expectedVersionGuid;

      if (titleMismatch || versionMismatch)
      {
          throw new InvalidOperationException(
              $"[PRE_FLIGHT_FAILED] Target document mismatch: expected '{expectedTitle}' (VersionGUID {expectedVersionGuid:D}), but active document is '{doc.Title}' (VersionGUID {actualVersionGuid:D})");
      }
  }
  ```
- **Consequences**:
  1. Because `targetDoc` was empty, `hasExpectedTitle || hasExpectedVersion` evaluated to `false`. Pre-flight validation was **entirely bypassed** in production CLI runs!
  2. The script does not check `pathName` (`doc.PathName`). If a user opens a different project file on disk that happens to share a title, the script would not detect the file path discrepancy.
  3. When a mismatch occurs, the exception message lacks distinct error codes distinguishing between targeting the wrong document (`[ERR_DOCUMENT_MISMATCH]`) vs. a concurrent modification / stale revision (`[ERR_STALE_DOCUMENT_SNAPSHOT]`).
  4. If `doc` is null (no document opened in Revit), the script throws an unhandled NullReferenceException instead of a clean, structured `[PRE_FLIGHT_FAILED] [ERR_NO_ACTIVE_DOCUMENT]` error.

### 2.3. Test Double Deficiencies (`FakeRevitPipeClient.cs`)
- In `HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs`:
  `ExecuteAsync` only handles `request.Label == "Query Extensible Storage Receipts"` explicitly.
  For `request.Label == "Query Active Document Identity"`, it falls back to `GenerateDefaultRecordsPayload(request)`, which returns `null`.
  When `DocumentIdentityQuery.QueryActiveDocumentAsync` receives `result.Value == null`, it throws `RevitPipeException`.
  Therefore, any test executing `GenerateCommand` with a default `FakeRevitPipeClient` would fail unless `FakeRevitPipeClient` is upgraded to support `SimulatedActiveDocument`.
  Additionally, `DocumentIdentityQuery.QueryActiveDocumentAsync` should accept `bool dryRun = false` so that when CLI is invoked with `--dry-run`, the query request respects the caller's dry-run context.

---

## 3. Target Architecture & Remediation Strategy

```
CLI: hpbim generate --input NhaPho_5x18m.json [--dry-run]
                      │
                      ▼
[1] GenerateCommand.ResolveTargetDocumentAsync
    ├── Connects to Named Pipe (\\.\pipe\hprebar-mcp-r2026)
    ├── Sends DocumentIdentityQuery ("Query Active Document Identity")
    └── Validates non-empty DocumentIdentitySnapshot (Title, Path, VersionGUID)
                      │
                      ▼
[2] Options & Pipeline Dispatch
    ├── BimPipelineOptions.TargetDoc = activeDoc
    └── BimPipelineEngine.ExecuteAsync(model, options)
                      │
                      ▼
[3] Single-Source-of-Truth Compilation
    ├── ExecutionPlanBuilder.Build(model, targetDoc: activeDoc)
    ├── BimExecutionPlan: PlanHash binds activeDoc.Title & VersionGUID
    └── IntentJournal: IntentJournalRecord binds activeDoc snapshot to disk
                      │
                      ▼
[4] Named Pipe Dispatch to Revit
    ├── ExecuteRequest(Args: BimExecutionPlan including targetDoc)
    └── Revit Bridge runs BimPlanRunnerScriptTemplate (Roslyn)
                      │
                      ▼
[5] Pre-flight Check on Revit Thread
    ├── Assert doc != null -> [ERR_NO_ACTIVE_DOCUMENT]
    ├── Compare Title & Path -> [ERR_DOCUMENT_MISMATCH]
    └── Compare VersionGUID -> [ERR_STALE_DOCUMENT_SNAPSHOT]
```

### 3.1. Binding Flow Details
1. **Query Active Document First**:
   In `GenerateCommand.RunPipelineAsync`, query the active document via `DocumentIdentityQuery` before building the plan.
   Log target document info to console:
   `[Target Document] Đã phát hiện tài liệu Revit: '{targetDoc.Title}' (VersionGUID: {targetDoc.VersionGuid:D})`.
2. **Bind via Pipeline Options**:
   Add `public DocumentIdentitySnapshot? TargetDoc { get; init; }` to `BimPipelineOptions`.
   In `BimPipelineEngine.ExecuteAsync`, if `options.TargetDoc` is provided, use it; if null/empty and `options.UseExecutionPlan` is true, automatically query via `DocumentIdentityQuery` as a robust fallback.
3. **Bind into `BimExecutionPlan`**:
   `ExecutionPlanBuilder.Build(model, targetDoc: targetDoc, ...)` embeds `targetDoc` into `BimExecutionPlan.TargetDoc`.
   `BimExecutionPlan.ComputePlanHash` already binds `targetDoc.Title` and `targetDoc.VersionGuid`.
4. **Bind into `IntentJournal`**:
   `BimPipelineEngine.RecordPreDispatchIntentAsync` sets `record.TargetDoc = plan.TargetDoc`. The target document snapshot is persisted to `.hpbim_journal/intent_journal.jsonl` before dispatch.
5. **Roslyn Runner Preflight**:
   In `BimPlanRunnerScriptTemplate.cs`, extract `targetDoc` from `args`.
   If `title` or `pathName` mismatches:
   `throw new InvalidOperationException($"[PRE_FLIGHT_FAILED] [ERR_DOCUMENT_MISMATCH] Target document mismatch: expected '{expectedTitle}' (Path: '{expectedPath}', VersionGUID {expectedVersionGuid:D}), but active document is '{doc.Title}' (Path: '{doc.PathName}', VersionGUID {actualVersionGuid:D})");`
   If `versionGuid` mismatches:
   `throw new InvalidOperationException($"[PRE_FLIGHT_FAILED] [ERR_STALE_DOCUMENT_SNAPSHOT] Target document version snapshot is stale: expected VersionGUID {expectedVersionGuid:D}, but active document VersionGUID is {actualVersionGuid:D}");`

---

## 4. Affected Files, Contracts, and Method Inventory

| # | Project | File Path | Impact / Changes | Clean Code Check |
|---|---|---|---|---|
| 1 | `HPBim.Cli` | `src/HPBim.Cli/Commands/GenerateCommand.cs` | Add `ResolveTargetDocumentAsync` helper; bind `TargetDoc` into `BimPipelineOptions`. | Method: 27 lines ($\le 50$); File: ~160 lines ($\le 300$). |
| 2 | `HPBim.Client` | `src/HPBim.Client/Pipeline/BimPipelineOptions.cs` | Add `public DocumentIdentitySnapshot? TargetDoc { get; init; }`. | File: ~46 lines ($\le 300$). |
| 3 | `HPBim.Client` | `src/HPBim.Client/Pipeline/DocumentIdentityQuery.cs` | Add `QueryActiveDocumentAsync(bool dryRun, CancellationToken ct)` overload; guard against `doc == null` in `QueryScript`. | Methods: $\le 35$ lines; File: ~105 lines ($\le 300$). |
| 4 | `HPBim.Client` | `src/HPBim.Client/Pipeline/BimPipelineEngine.cs` | Extract `BuildExecutionPlanAsync` to resolve `options.TargetDoc` or query automatically; pass `targetDoc` to `ExecutionPlanBuilder.Build`. | Method: 19 lines ($\le 50$); File: ~240 lines ($\le 300$). |
| 5 | `HPBim.Client` | `src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` | Refactor preamble into `GetPreambleHelpersScript()` and `GetPreamblePreFlightScript()`; add `pathName` check, `[ERR_DOCUMENT_MISMATCH]`, and `[ERR_STALE_DOCUMENT_SNAPSHOT]`. | Methods: $\le 45$ lines; File: ~245 lines ($\le 300$). |
| 6 | `HPBim.Core` | `src/HPBim.Core/Execution/DocumentIdentitySnapshot.cs` | Add `Matches(string actualTitle, string? actualPathName, Guid actualVersionGuid)` overload. | Method: 22 lines; File: ~65 lines ($\le 300$). |
| 7 | `HPBim.Client.Tests` | `tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs` | Add `SimulatedActiveDocument` property; handle `Query Active Document Identity` with `RolledBack = request.DryRun`. | Method: 20 lines; File: ~255 lines ($\le 300$). |
| 8 | `HPBim.Client.Tests` | `tests/HPBim.Client.Tests/Pipeline/PreFlightGateTests.cs` | Add test cases for explicit error codes (`ERR_DOCUMENT_MISMATCH`, `ERR_STALE_DOCUMENT_SNAPSHOT`, `ERR_NO_ACTIVE_DOCUMENT`) and path mismatch. | Methods: $\le 40$ lines; File: ~250 lines ($\le 300$). |
| 9 | `HPBim.Client.Tests` | `tests/HPBim.Client.Tests/Cli/GenerateCommandTests.cs` | Add unit tests verifying target document query and binding during CLI generate execution. | Methods: $\le 35$ lines; File: ~200 lines ($\le 300$). |

---

## 5. Drop-in Implementation Code Snippets for Worker

### 5.1. `HPBim.Core/Execution/DocumentIdentitySnapshot.cs`
Add the 3-parameter overload to `DocumentIdentitySnapshot`:

```csharp
    /// <summary>
    /// Validates whether the live document identity matches this snapshot, checking Title, PathName, and VersionGuid.
    /// </summary>
    public bool Matches(string actualTitle, string? actualPathName, Guid actualVersionGuid)
    {
        if (!string.IsNullOrEmpty(Title) &&
            !string.Equals(Title, actualTitle, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(PathName) &&
            !string.Equals(PathName, actualPathName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (VersionGuid != Guid.Empty && actualVersionGuid != VersionGuid)
        {
            return false;
        }

        return true;
    }
```

### 5.2. `HPBim.Client/Pipeline/BimPipelineOptions.cs`
Add `TargetDoc` property:

```csharp
    /// <summary>
    ///     Identity snapshot of the target Revit document. If null and UseExecutionPlan is true,
    ///     the pipeline automatically queries the active document via Named Pipe.
    /// </summary>
    public DocumentIdentitySnapshot? TargetDoc { get; init; }
```

### 5.3. `HPBim.Client/Pipeline/DocumentIdentityQuery.cs`
Update interface and implementation to support `dryRun` flag and `doc == null` guard in `QueryScript`:

```csharp
public interface IDocumentIdentityQuery
{
    Task<DocumentIdentitySnapshot> QueryActiveDocumentAsync(CancellationToken cancellationToken = default);
    Task<DocumentIdentitySnapshot> QueryActiveDocumentAsync(bool dryRun, CancellationToken cancellationToken = default);
}

public sealed class DocumentIdentityQuery : IDocumentIdentityQuery
{
    private readonly IRevitPipeClient _pipeClient;

    public const string QueryScript = """
        if (doc == null)
        {
            throw new InvalidOperationException("[ERR_NO_ACTIVE_DOCUMENT] No active document open in Revit.");
        }
        var docVer = Document.GetDocumentVersion(doc);
        return new
        {
            title = doc.Title ?? string.Empty,
            pathName = string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName,
            versionGuid = docVer != null ? docVer.VersionGUID.ToString("D") : Guid.Empty.ToString("D"),
            numberOfSaves = docVer != null ? docVer.NumberOfSaves : 0,
            centralGuid = (doc.IsWorkshared ? doc.WorksharingCentralGUID : Guid.Empty).ToString("D"),
            activeViewName = uidoc?.ActiveView?.Name
        };
        """;

    public DocumentIdentityQuery(IRevitPipeClient pipeClient)
    {
        _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
    }

    public Task<DocumentIdentitySnapshot> QueryActiveDocumentAsync(CancellationToken cancellationToken = default) =>
        QueryActiveDocumentAsync(dryRun: false, cancellationToken);

    public async Task<DocumentIdentitySnapshot> QueryActiveDocumentAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        if (!_pipeClient.IsConnected)
        {
            await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        var request = new ExecuteRequest(
            Code: QueryScript,
            Transaction: TransactionModes.None,
            DryRun: dryRun,
            TimeoutSeconds: 30,
            Label: "Query Active Document Identity");

        var result = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsError || result.Value == null)
        {
            throw new RevitPipeException($"Failed to query active document identity: {result.Message}");
        }

        return ParseSnapshot(result.Value.Value);
    }
```

### 5.4. `HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`
Refactor `GetPreambleScript` to split helpers and pre-flight logic (Rule M3 compliant, each $\le 50$ lines):

```csharp
    private static string GetPreambleScript() =>
        GetPreambleHelpersScript() +
        GetPreamblePreFlightScript() +
        GetPreambleDatumCollectorsScript() +
        GetPreambleTypeCollectorsScript();

    private static string GetPreambleHelpersScript() => """
        // =========================================================================
        // HPBim Unified Plan Runner (Single Source of Truth IR)
        // =========================================================================
        double Ft(double mm) => mm / 304.8;
        double Mm(double ft) => ft * 304.8;
        XYZ ToXyz(double xMm, double yMm, double zMm = 0.0) => new XYZ(Ft(xMm), Ft(yMm), Ft(zMm));

        string NormalizeSourceKey(ScriptArgs it)
        {
            string raw = it.Str("sourceKey", string.Empty);
            if (string.IsNullOrEmpty(raw) || !raw.StartsWith("{")) return raw;
            var obj = it.Obj("sourceKey");
            string d = obj.Str("docIdentifier", string.Empty);
            string c = obj.Str("category", string.Empty);
            string h = obj.Str("primaryHandle", string.Empty);
            return $"[{d}]#[{c}]:{h}";
        }

        """;

    private static string GetPreamblePreFlightScript() => """
        if (doc == null)
        {
            throw new InvalidOperationException(
                "[PRE_FLIGHT_FAILED] [ERR_NO_ACTIVE_DOCUMENT] No active document open in Revit.");
        }

        var targetDoc = args.Obj("targetDoc");
        string expectedTitle = targetDoc.Str("title", string.Empty);
        string expectedPath = targetDoc.Str("pathName", string.Empty);
        string expectedVersionGuidStr = targetDoc.Str("versionGuid", string.Empty);
        Guid expectedVersionGuid = Guid.TryParse(expectedVersionGuidStr, out var parsedGuid) ? parsedGuid : Guid.Empty;

        var docVer = Document.GetDocumentVersion(doc);
        Guid actualVersionGuid = docVer != null ? docVer.VersionGUID : Guid.Empty;

        bool hasExpectedTitle = !string.IsNullOrEmpty(expectedTitle);
        bool hasExpectedPath = !string.IsNullOrEmpty(expectedPath);
        bool hasExpectedVersion = expectedVersionGuid != Guid.Empty;

        if (hasExpectedTitle || hasExpectedPath || hasExpectedVersion)
        {
            bool titleMismatch = hasExpectedTitle && !string.Equals(doc.Title, expectedTitle, StringComparison.OrdinalIgnoreCase);
            bool pathMismatch = hasExpectedPath && !string.IsNullOrEmpty(doc.PathName) && !string.Equals(doc.PathName, expectedPath, StringComparison.OrdinalIgnoreCase);
            bool versionMismatch = hasExpectedVersion && actualVersionGuid != expectedVersionGuid;

            if (titleMismatch || pathMismatch)
            {
                throw new InvalidOperationException(
                    $"[PRE_FLIGHT_FAILED] [ERR_DOCUMENT_MISMATCH] Target document mismatch: expected '{expectedTitle}' (Path: '{expectedPath}', VersionGUID {expectedVersionGuid:D}), but active document is '{doc.Title}' (Path: '{doc.PathName}', VersionGUID {actualVersionGuid:D})");
            }

            if (versionMismatch)
            {
                throw new InvalidOperationException(
                    $"[PRE_FLIGHT_FAILED] [ERR_STALE_DOCUMENT_SNAPSHOT] Target document version snapshot is stale: expected VersionGUID {expectedVersionGuid:D}, but active document VersionGUID is {actualVersionGuid:D}");
            }
        }

        var items = args.List("items");
        var records = new List<Dictionary<string, object>>();

        """;
```

### 5.5. `HPBim.Client/Pipeline/BimPipelineEngine.cs`
In `ExecuteAsync`, extract `BuildExecutionPlanAsync`:

```csharp
        if (options.UseExecutionPlan)
        {
            var plan = await BuildExecutionPlanAsync(model, options, cancellationToken).ConfigureAwait(false);
            return IntentJournal != null
                ? await ExecutePlanJournaledAsync(plan, cancellationToken).ConfigureAwait(false)
                : await ExecutePlanAsync(plan, cancellationToken).ConfigureAwait(false);
        }
```

Add helper method:

```csharp
    private async Task<BimExecutionPlan> BuildExecutionPlanAsync(
        BimModel model,
        BimPipelineOptions options,
        CancellationToken ct)
    {
        var targetDoc = options.TargetDoc;
        if (targetDoc == null || targetDoc.IsEmpty)
        {
            var query = new DocumentIdentityQuery(_pipeClient);
            targetDoc = await query.QueryActiveDocumentAsync(options.DryRun, ct).ConfigureAwait(false);
        }

        return ExecutionPlanBuilder.Build(model, targetDoc: targetDoc, options: new ExecutionOptions
        {
            DryRun = options.DryRun,
            TimeoutSeconds = options.TimeoutSecondsPerStage * 2,
            CreatePlanViews = true
        });
    }
```

### 5.6. `HPBim.Cli/Commands/GenerateCommand.cs`
Update `RunPipelineAsync` to query active document and log target doc binding:

```csharp
    private async Task<int> RunPipelineAsync(BimModel model, bool dryRun, string pipeName, CancellationToken ct)
    {
        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName });
        using var journal = dryRun ? null : new IntentJournal();
        try
        {
            var targetDoc = await ResolveTargetDocumentAsync(client, dryRun, ct).ConfigureAwait(false);
            if (targetDoc == null) return 1;

            var pipeline = new BimPipelineEngine(client)
            {
                IntentJournal = journal
            };
            pipeline.StageCompleted += stage =>
            {
                string status = stage.Success ? "OK" : "FAILED";
                _formatter.WriteInfo($"[Stage {stage.StageIndex}] {stage.StageName}: {status} ({stage.DurationMs}ms, +{stage.Changed.Added} elements)");
            };

            var options = new BimPipelineOptions
            {
                DryRun = dryRun,
                AtomicDryRun = dryRun,
                UseExecutionPlan = true,
                TargetDoc = targetDoc
            };
            var result = await pipeline.ExecuteAsync(model, options, ct).ConfigureAwait(false);

            RenderPipelineResult(result, dryRun);
            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                _formatter.WriteError($"Chi tiết lỗi từ Revit: {result.ErrorMessage}");
            }
            bool isOk = dryRun ? (result.Success && result.RolledBack) : result.Success;
            return isOk ? 0 : 1;
        }
        finally
        {
            if (_injectedClient == null) await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<DocumentIdentitySnapshot?> ResolveTargetDocumentAsync(
        IRevitPipeClient client,
        bool dryRun,
        CancellationToken ct)
    {
        try
        {
            if (!client.IsConnected)
            {
                await client.ConnectAsync(ct).ConfigureAwait(false);
            }

            var docQuery = new DocumentIdentityQuery(client);
            var snapshot = await docQuery.QueryActiveDocumentAsync(dryRun, ct).ConfigureAwait(false);

            if (snapshot.IsEmpty)
            {
                _formatter.WriteError("Tài liệu Revit hiện tại không hợp lệ hoặc chưa sẵn sàng (Title và VersionGUID rỗng).");
                return null;
            }

            _formatter.WriteInfo($"[Target Document] Đã phát hiện tài liệu Revit: '{snapshot.Title}' (VersionGUID: {snapshot.VersionGuid:D})");
            return snapshot;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Không thể truy vấn thông tin Revit document qua Named Pipe: {ex.Message}");
            return null;
        }
    }
```

### 5.7. `HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs`
Add `SimulatedActiveDocument` and `HandleQueryActiveDocumentIdentity`:

```csharp
    public DocumentIdentitySnapshot SimulatedActiveDocument { get; set; } = new()
    {
        Title = "NhaPho_5x18m.rvt",
        PathName = "C:\\Models\\NhaPho_5x18m.rvt",
        VersionGuid = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        NumberOfSaves = 1,
        ActiveViewName = "Level 1"
    };
```

In `ExecuteAsync`:
```csharp
        if (request.Label == "Query Active Document Identity")
        {
            return Task.FromResult(HandleQueryActiveDocumentIdentity(request));
        }
```

Add helper:
```csharp
    private ExecuteResult HandleQueryActiveDocumentIdentity(ExecuteRequest request)
    {
        var payload = new
        {
            title = SimulatedActiveDocument.Title,
            pathName = SimulatedActiveDocument.PathName,
            versionGuid = SimulatedActiveDocument.VersionGuid.ToString("D"),
            numberOfSaves = SimulatedActiveDocument.NumberOfSaves,
            centralGuid = (SimulatedActiveDocument.CentralGuid ?? Guid.Empty).ToString("D"),
            activeViewName = SimulatedActiveDocument.ActiveViewName
        };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        using var doc = JsonDocument.Parse(jsonBytes);
        return new ExecuteResult
        {
            IsError = false,
            Value = doc.RootElement.Clone(),
            RolledBack = request.DryRun,
            DurationMs = 5
        };
    }
```

---

## 6. Verification and Test Design

### 6.1. Unit Test Matrix for Worker & Challengers
1. **Roslyn Syntax Validation**:
   `BimPlanRunnerScriptTemplateTier5AdversarialTests.GetScriptCode_ParsesAsValidCSharpScript_WithZeroSyntaxErrors`:
   Verifies that the new template parses with 0 diagnostics/errors.
2. **Explicit Error Code Verification**:
   Assert script contains:
   - `[ERR_DOCUMENT_MISMATCH]`
   - `[ERR_STALE_DOCUMENT_SNAPSHOT]`
   - `[ERR_NO_ACTIVE_DOCUMENT]`
3. **Target Document Query in CLI Generate**:
   Verify `GenerateCommandTests`:
   - `Execute_DryRun_SendsDryRunFlagAndReturnsExitCode0` passes cleanly.
   - `Execute_BindsTargetDocumentIdentity_BeforeCompilingPlan`: Asserts that `fakeClient.ExecutedRequests` contains a request with `Label == "Query Active Document Identity"` sent prior to plan dispatch, and that `options.TargetDoc` contains the queried title and version GUID.
   - `Execute_DocumentQueryFails_ReturnsExitCode1`: Simulates pipe failure on document query, asserts CLI displays error and exits with code 1.
4. **Preflight Rejection Tests**:
   - `PreFlightGate_WhenTitleMismatches_ThrowsDocumentMismatchExplicitCode`: Asserts error message contains `[ERR_DOCUMENT_MISMATCH]`.
   - `PreFlightGate_WhenPathMismatches_ThrowsDocumentMismatchExplicitCode`: Asserts error message contains `[ERR_DOCUMENT_MISMATCH]` and mismatched path.
   - `PreFlightGate_WhenVersionGuidMismatches_ThrowsStaleSnapshotExplicitCode`: Asserts error message contains `[ERR_STALE_DOCUMENT_SNAPSHOT]`.

### 6.2. Execution Commands
```powershell
cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim"
dotnet build HPBim.slnx
dotnet test HPBim.slnx
```
Expectation: 840+ tests passed, 0 failures, 0 skipped.
