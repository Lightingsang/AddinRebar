# Forensic Remediation Exploration Report: CanvasGraphRunner.cs

**Agent**: `explorer_dynamo_m2_it2_3`  
**Task**: Explore concrete code edits for `CanvasGraphRunner.cs` remediation (Auditor Finding: Rule M3 method length in `ExecuteDesignScript`).  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_3\`  
**Target File**: `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs`  
**Date**: 2026-10-09  

---

## 1. Executive Summary

During the Milestone 2 forensic audit (`auditor_dynamo_m2`), the auditor declared an **INTEGRITY VIOLATION** under **Rule M3** (Method length $\le 50$ lines exceeded) for:
- `ExecuteDesignScript` (original lines 120–189): **70 lines total**, **62 lines body** (`{ ... }`).
- Secondary concern: `RunDynFile` (original lines 67–118): **52 lines total** (body is 44 lines, but multi-line signature + braces pushes total to 52 lines).

This exploration report provides a complete, mathematically verified refactoring design that decomposes `ExecuteDesignScript` and compacts `RunDynFile`. Under the proposed refactoring:
1. **All 8 methods** in `CanvasGraphRunner.cs` strictly satisfy $\le 50$ lines for **both** total lines and body lines.
2. The entire file is **256 lines**, safely below the 300-line limit (Rule C2).
3. 100% semantic equivalence is preserved: exception throwing orders, cancellation handling, cleanup on error, and output extraction are identical.
4. All existing tests in `Milestone2Tests.cs` and `Milestone2ChallengerTests.cs` remain 100% compatible with zero regressions.

---

## 2. Before vs. After Method Metrics

Measured using the forensic auditor's AST and line-counting regex parser (`scratch/audit_m2_forensics.py`):

| Method Name | Original Total Lines | Original Body Lines | Original Status | Proposed Total Lines | Proposed Body Lines | Proposed Status |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| `RunActiveGraph` | 44 (L22–L65) | 38 | **PASS** | 44 (L22–L65) | 38 | **PASS** |
| `RunDynFile` | 52 (L67–L118) | 44 | **WARNING** (total>50) | 48 (L67–L114) | 40 | **PASS** |
| `ExecuteDesignScript` | 70 (L120–L189) | 62 | **OVER_50 (FAIL)** | 29 (L116–L144) | 21 | **PASS** |
| `OpenHomeWorkspace` *(new)* | — | — | — | 10 (L146–L155) | 5 | **PASS** |
| `PlaceCodeBlockNode` *(new)* | — | — | — | 22 (L157–L178) | 16 | **PASS** |
| `EvaluateAndExtract` *(new)* | — | — | — | 38 (L180–L217) | 30 | **PASS** |
| `ApplyParameterOverrides` | 15 (L191–L205) | 13 | **PASS** | 15 (L219–L233) | 13 | **PASS** |
| `ExtractOutputs` | 21 (L207–L227) | 19 | **PASS** | 21 (L235–L255) | 19 | **PASS** |
| **Total File Lines** | **229** | — | **PASS** ($\le 300$) | **256** | — | **PASS** ($\le 300$) |

---

## 3. Detailed Refactoring Architecture & Decompositions

### 3.1 Remediation of `ExecuteDesignScript` (Lines 120–189 $\to$ Lines 116–144)

#### Original Problem
`ExecuteDesignScript` bundled five separate concerns into 70 lines:
1. Input argument guard clauses (`ArgumentNullException`, `ArgumentException`).
2. Current workspace retrieval and type validation.
3. Canvas coordinate calculation and `CodeBlockNodeModel` creation / placement.
4. Synchronous evaluation (`DynamoEvaluationSync.RunAndWait`) and state verification.
5. Ephemeral node deletion (`dryRun`) and error cleanup catch block.

#### Refactoring Strategy
Extract two private static helpers:
1. `PlaceCodeBlockNode`: Handles canvas placement offset calculation and `CreateNodeCommand`.
2. `EvaluateAndExtract`: Encapsulates evaluation sync, error state checking, output extraction, and ephemeral node deletion on `dryRun`.

The orchestrator method `ExecuteDesignScript` is reduced to **29 lines total (21 lines body)**:
```csharp
    public object? ExecuteDesignScript(
        DynamoViewModel viewModel,
        string code,
        JsonElement? args,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("DesignScript code cannot be empty", nameof(code));

        var ws = viewModel.Model.CurrentWorkspace as HomeWorkspaceModel
                 ?? throw new InvalidOperationException("Active workspace is not available.");

        var cbn = PlaceCodeBlockNode(viewModel, ws, code, log);

        try
        {
            return EvaluateAndExtract(viewModel, ws, cbn, dryRun, ct, log);
        }
        catch (Exception ex)
        {
            viewModel.Model.ExecuteCommand(new DynamoModel.DeleteModelCommand(cbn.GUID));
            if (ex is OperationCanceledException)
                throw;
            throw new InvalidOperationException($"DesignScript canvas evaluation failed: {ex.Message}", ex);
        }
    }
```

#### Helper 1: `PlaceCodeBlockNode` (22 lines total, 16 lines body)
```csharp
    private static CodeBlockNodeModel PlaceCodeBlockNode(
        DynamoViewModel viewModel,
        HomeWorkspaceModel ws,
        string code,
        Action<string?> log)
    {
        log("Placing DesignScript CodeBlock node on canvas");

        double x = 100.0;
        double y = 100.0;
        if (ws.Nodes.Any())
        {
#pragma warning disable CS0618
            x = ws.Nodes.Max(n => n.X) + 250.0;
            y = ws.Nodes.First().Y;
#pragma warning restore CS0618
        }

        var cbn = new CodeBlockNodeModel(code, Guid.NewGuid(), x, y, viewModel.Model.LibraryServices, ws.ElementResolver);
        viewModel.Model.ExecuteCommand(new DynamoModel.CreateNodeCommand(cbn, x, y, false, false));
        return cbn;
    }
```

#### Helper 2: `EvaluateAndExtract` (38 lines total, 30 lines body)
```csharp
    private static object EvaluateAndExtract(
        DynamoViewModel viewModel,
        HomeWorkspaceModel ws,
        CodeBlockNodeModel cbn,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        bool evalOk = DynamoEvaluationSync.RunAndWait(
            viewModel.Model,
            () => ws.Run(),
            TimeSpan.FromSeconds(30),
            ct,
            log);

        if (!evalOk)
        {
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException("DesignScript evaluation was cancelled.", ct);
            throw new InvalidOperationException("DesignScript evaluation failed or timed out.");
        }

        if (cbn.State == ElementState.Error)
        {
            throw new InvalidOperationException("DesignScript CodeBlock node evaluated to error state.");
        }

        var outputs = ExtractOutputs(viewModel.Model.EngineController, cbn);
        log($"DesignScript executed on canvas with {outputs.Count} outputs");

        if (dryRun)
        {
            log("dryRun=true: Removing ephemeral CodeBlock from canvas");
            viewModel.Model.ExecuteCommand(new DynamoModel.DeleteModelCommand(cbn.GUID));
        }

        return new { Success = true, NodeId = cbn.GUID, Outputs = outputs };
    }
```

---

### 3.2 Remediation of `RunDynFile` (Lines 67–118 $\to$ Lines 67–114)

#### Original Problem
`RunDynFile` spanned 52 lines total (body 44 lines). While the body was technically $\le 50$, the multi-line signature pushed the total lines over 50, triggering a forensic auditor warning.

#### Refactoring Strategy
Extract the workspace loading logic into `OpenHomeWorkspace` (10 lines total, 5 lines body).

#### Helper 3: `OpenHomeWorkspace` (10 lines total, 5 lines body)
```csharp
    private static HomeWorkspaceModel OpenHomeWorkspace(
        DynamoViewModel viewModel,
        string filePath,
        Action<string?> log)
    {
        log($"Opening and executing file '{Path.GetFileName(filePath)}' on canvas");
        viewModel.OpenIfSavedCommand.Execute(new DynamoModel.OpenFileCommand(filePath, false));
        return viewModel.Model.CurrentWorkspace as HomeWorkspaceModel
               ?? throw new InvalidOperationException("Failed to load workspace onto canvas.");
    }
```

#### Resulting `RunDynFile` (48 lines total, 40 lines body)
```csharp
    public object? RunDynFile(
        DynamoViewModel viewModel,
        string filePath,
        JsonElement? args,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Dynamo file not found", filePath);

        var ws = OpenHomeWorkspace(viewModel, filePath, log);

        if (args.HasValue && args.Value.ValueKind == JsonValueKind.Object)
        {
            ApplyParameterOverrides(ws, args.Value, log);
        }

        bool evalOk = DynamoEvaluationSync.RunAndWait(
            viewModel.Model,
            () => ws.Run(),
            TimeSpan.FromSeconds(60),
            ct,
            log);

        if (!evalOk)
        {
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException("Dynamo graph evaluation was cancelled.", ct);
            throw new InvalidOperationException($"Dynamo graph evaluation failed or timed out for '{Path.GetFileName(filePath)}'.");
        }

        int errorCount = ws.Nodes.Count(n => n.State == ElementState.Error);
        if (errorCount > 0)
        {
            throw new InvalidOperationException($"Dynamo graph evaluation completed with {errorCount} node(s) in error state.");
        }

        return new
        {
            Success = true,
            FilePath = filePath,
            NodesCount = ws.Nodes.Count(),
            ConnectorsCount = ws.Connectors.Count(),
            ErrorNodesCount = errorCount
        };
    }
```

---

## 4. Clean Code Governance Compliance Matrix

| Rule | Requirement | Assessment | Compliance Evidence |
| :--- | :--- | :---: | :--- |
| **M3** | Method length $\le 50$ lines | **PASS** | Max method length in proposed file is 48 lines (`RunDynFile`), followed by 44 lines (`RunActiveGraph`), 38 lines (`EvaluateAndExtract`), 29 lines (`ExecuteDesignScript`). Every method is strictly $\le 50$. |
| **C2** | Class/file length $\le 300$ lines | **PASS** | Proposed file is 256 lines (margin of 44 lines under limit). |
| **Q-B1** | Zero commented-out dead code | **PASS** | 0 lines of commented-out code. Explanatory comments are present only where mandated (e.g. catch block logging justification). |
| **Q-B2** | No empty catch blocks | **PASS** | All catch blocks contain cleanup commands, rethrow statements, or documented logging. |
| **FM** | Method parameters and cohesion | **PASS** | Each helper has a single, cohesive responsibility. Parameters are typed and validated. |
| **N** | Naming standards | **PASS** | Descriptive, PascalCase methods (`OpenHomeWorkspace`, `PlaceCodeBlockNode`, `EvaluateAndExtract`). |

---

## 5. Test Suite Verification & Invariant Proof

The proposed changes alter **only private implementation details** of `CanvasGraphRunner`:
1. **Public API Unchanged**:
   - `RunActiveGraph(DynamoViewModel, bool, CancellationToken, Action<string?>)`
   - `RunDynFile(DynamoViewModel, string, JsonElement?, bool, CancellationToken, Action<string?>)`
   - `ExecuteDesignScript(DynamoViewModel, string, JsonElement?, bool, CancellationToken, Action<string?>)`
2. **Exception Propagation Order Preserved**:
   - `ArgumentNullException.ThrowIfNull(viewModel)` remains line 1 of every entry point.
   - `ArgumentException` on empty code remains line 2 of `ExecuteDesignScript`.
   - `FileNotFoundException` on missing file remains line 2 of `RunDynFile`.
   - `OperationCanceledException` is rethrown intact on cancellation.
   - `InvalidOperationException` is thrown on node evaluation errors or missing home workspace.
3. **Tests in `Milestone2Tests.cs` and `Milestone2ChallengerTests.cs`**:
   - `CanvasGraphRunner_ExecuteDesignScript_WhenViewModelNull_ThrowsArgumentNullException`: Verified.
   - `CanvasGraphRunner_RunDynFile_WhenViewModelNull_ThrowsArgumentNullException`: Verified.
   - `CanvasGraphRunner_RunActiveGraph_WhenViewModelNull_ThrowsArgumentNullException`: Verified.
   - `CanvasGraphRunner_ExecuteDesignScript_EmptyCode_ThrowsArgumentException`: Verified.
   - `CanvasGraphRunner_RunDynFile_MissingFile_ThrowsFileNotFoundException`: Verified.

---

## 6. Generated Artifacts

The following artifacts have been generated in the explorer workspace:
1. `proposed_CanvasGraphRunner.cs`: Full drop-in replacement file at:  
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_3\proposed_CanvasGraphRunner.cs`
2. `CanvasGraphRunner.patch`: Unified diff patch file at:  
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_3\CanvasGraphRunner.patch`
3. `verify_proposed.py`: Standalone Python validator verifying all method and file limits at:  
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_3\verify_proposed.py`
