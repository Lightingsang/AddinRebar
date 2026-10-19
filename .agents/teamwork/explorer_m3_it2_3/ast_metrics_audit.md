# Clean Code Metrics & Static AST Audit Report
**Project Subsystem**: `HPGrasshopper/HPGrasshopper.McpBridge` (29 `.cs` files)  
**Agent**: `explorer_m3_it2_3` (Clean Code Metrics & Static AST Explorer)  
**Date**: 2026-10-10  
**Audit Standard**: `docs/clean-code/HP_CLEAN_CODE_CORE.md` (Rule M3 <= 50 lines, Rule C2 <= 300 lines)  
**Parser Engines Used**:  
1. Official Microsoft Roslyn C# Syntax Tree Engine (`Microsoft.CodeAnalysis.CSharp 4.12.0`)  
2. Multi-line C# Structural Tokenizer & AST Parser (`verify_clean_code_ast.py`)  

---

## 1. Executive Summary

A comprehensive static syntax tree audit was performed across **all 29 C# source files** (187 methods, constructors, local functions, and accessors) of the `HPGrasshopper.McpBridge` project.

### Key Audit Findings:
- **Total C# Source Files Scanned**: 29 files (100% of non-generated source code).
- **Rule C2 File Lengths (<= 300 lines)**: **PASS** (0 files > 300 lines).  
  *High-Risk Warning*: `Execution/RevitTransactionScope.cs` currently has **289 lines** (only 11 lines below the 300-line limit). Any careless extraction within this file could immediately trigger a Rule C2 rejection!
- **Rule M3 Method Length Violations (> 50 lines)**: **3 Strict Violations** confirmed:
  1. `Execution/RevitTransactionScope.cs: ExecuteCore` (78 lines total span, 68 lines body)
  2. `Tools/ImageExporter.cs: Export` (58 lines total span, 52 lines body)
  3. `Tools/PlayerRunner.cs: Run` (53 lines total span, 48 lines body)
- **Borderline Methods (41–50 lines)**: **6 Borderline Methods** identified that must be refactored or guarded to prevent future audit rejections:
  4. `Coordinator/GrasshopperContextExtractor.cs: BuildGrasshopperInfo` (50 lines total span, 49 lines body) — *Critical: at the exact 50-line limit!*
  5. `Host/GrasshopperAddinApplication.cs: OnStartup` (47 lines total span, 46 lines body)
  6. `Tools/CanvasReader.cs: Read` (46 lines total span, 41 lines body)
  7. `UI/ShowStatusDialogCommand.cs: ShowWindow` (44 lines total span, 43 lines body)
  8. `Coordinator/GrasshopperContextExtractor.cs: ExtractFromRevit` (42 lines total span, 37 lines body)
  9. `Execution/GrasshopperExecutionEngine.cs: RunRoslynCodeAsync` (42 lines total span, 37 lines body)

---

## 2. Complete Inventory of Methods Exceeding 40 Lines

| # | Severity | File Path | Method / Member | Start–End Lines | Total Span | Body Span |
|---|---|---|---|---|:---:|:---:|
| 1 | **FAIL M3** | `Execution/RevitTransactionScope.cs` | `ExecuteCore` | 108–185 | **78** | **68** |
| 2 | **FAIL M3** | `Tools/ImageExporter.cs` | `Export` | 15–72 | **58** | **52** |
| 3 | **FAIL M3** | `Tools/PlayerRunner.cs` | `Run` | 19–71 | **53** | 48 |
| 4 | *BORDERLINE* | `Coordinator/GrasshopperContextExtractor.cs` | `BuildGrasshopperInfo` | 93–142 | **50** | 49 |
| 5 | *BORDERLINE* | `Host/GrasshopperAddinApplication.cs` | `OnStartup` | 30–76 | 47 | 46 |
| 6 | *BORDERLINE* | `Tools/CanvasReader.cs` | `Read` | 21–66 | 46 | 41 |
| 7 | *BORDERLINE* | `UI/ShowStatusDialogCommand.cs` | `ShowWindow` | 28–71 | 44 | 43 |
| 8 | *BORDERLINE* | `Coordinator/GrasshopperContextExtractor.cs` | `ExtractFromRevit` | 50–91 | 42 | 37 |
| 9 | *BORDERLINE* | `Execution/GrasshopperExecutionEngine.cs` | `RunRoslynCodeAsync` | 143–184 | 42 | 37 |

---

## 3. Deep-Dive Analysis & Refactoring Blueprints

### 3.1 `Execution/RevitTransactionScope.cs: ExecuteCore` (STRICT VIOLATION)
- **Location**: Lines 108–185 (Total: 78 lines, Body: 68 lines).
- **Current Signature**:
  ```csharp
  private static ExecuteResult ExecuteCore(
      ITransactionContext txContext,
      ExecuteRequest request,
      BridgeSettings settings,
      IProgress<ScriptProgress>? progress,
      CancellationTokenSource cancelSource,
      Func<CancellationToken, Action<string?>, object?> action,
      bool isDocModifiable,
      DocumentChangeCounter? counter,
      bool startAutoTransaction = true)
  ```
- **Root Cause of Inflation**:
  Mixes three distinct responsibilities:
  1. Transaction lifecycle orchestration and script execution.
  2. Exception classification (`OperationCanceledException` vs `Exception`) and rollback.
  3. Result serialization and `ExecuteResult` DTO mapping (14 lines alone).
- **Rule C2 Conflict Warning**:
  `RevitTransactionScope.cs` is already **289 lines long**. Adding private helper methods inside this file will easily push the file over 300 lines!
- **Recommended Extraction Blueprint**:
  1. Extract result building into a private static method or a dedicated helper class `TransactionResultFactory.cs`.
  2. Extract exception translation:
  ```csharp
  private static (string? ErrorMessage, bool RolledBack, bool TimedOut) HandleException(
      Exception ex,
      ITransactionContext txContext,
      ExecuteRequest request,
      CancellationTokenSource cancelSource,
      CancellationTokenSource timeoutSource,
      string label)
  {
      if (ex is OperationCanceledException)
      {
          bool timedOut = timeoutSource.IsCancellationRequested && !cancelSource.IsCancellationRequested;
          string error = timedOut
              ? $"Execution timed out after {request.TimeoutSeconds}s. Nothing was committed."
              : "Execution was cancelled. Nothing was committed.";
          return (error, SafeRollback(txContext), timedOut);
      }
      Log.Warning(ex, "Execution failed in RevitTransactionScope for label '{Label}'", label);
      return (SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}"), SafeRollback(txContext), false);
  }
  ```
  3. Extract `ExecuteResult` construction into:
  ```csharp
  private static ExecuteResult BuildResult(
      object? returnValue, string? errorMessage, bool rolledBack, bool timedOut,
      Stopwatch sw, List<string> logs, int droppedLogs, BridgeSettings settings,
      ITransactionContext txContext, DocumentChangeCounter? counter)
  ```
  *Result*: `ExecuteCore` drops from 78 lines down to **~30 lines**. If extracted to `TransactionResultFactory.cs`, `RevitTransactionScope.cs` shrinks to **~235 lines**, eliminating Rule C2 pressure entirely.

---

### 3.2 `Tools/ImageExporter.cs: Export` (STRICT VIOLATION)
- **Location**: Lines 15–72 (Total: 58 lines, Body: 52 lines).
- **Current Signature**:
  ```csharp
  public object Export(
      GH_Canvas? canvas,
      string mode = "FullCanvas",
      double zoom = 1.0,
      string? outputPath = null,
      bool returnBase64 = true)
  ```
- **Root Cause of Inflation**:
  Concurrently performs Canvas GDI+ drawing, Base64 memory stream serialization, disk directory creation and file I/O, and result DTO construction in a single method.
- **Recommended Extraction Blueprint**:
  Extract 3 single-responsibility private helper methods:
  ```csharp
  private static Bitmap RenderCanvas(GH_Canvas canvas, double zoom)
  {
      int width = Math.Max(200, (int)(canvas.Width * Math.Clamp(zoom, 0.25, 4.0)));
      int height = Math.Max(200, (int)(canvas.Height * Math.Clamp(zoom, 0.25, 4.0)));
      var bmp = new Bitmap(width, height);
      using var g = Graphics.FromImage(bmp);
      g.Clear(Color.FromArgb(240, 240, 240));
      canvas.DrawToBitmap(bmp, new Rectangle(0, 0, width, height));
      return bmp;
  }

  private static string EncodeToBase64(Bitmap bmp)
  {
      using var ms = new MemoryStream();
      bmp.Save(ms, ImageFormat.Png);
      return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
  }

  private static void SaveToFile(Bitmap bmp, string outputPath)
  {
      var dir = Path.GetDirectoryName(outputPath);
      if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
          Directory.CreateDirectory(dir);
      bmp.Save(outputPath, ImageFormat.Png);
  }
  ```
  *Result*: `Export` body shrinks to **~16 lines** (Total span: **~22 lines**). File length remains well under 100 lines (Rule C2 safe).

---

### 3.3 `Tools/PlayerRunner.cs: Run` (STRICT VIOLATION)
- **Location**: Lines 19–71 (Total: 53 lines, Body: 48 lines).
- **Current Signature**:
  ```csharp
  public object Run(
      string filePath,
      JsonElement? inputs = null,
      IReadOnlyList<string>? requestedOutputs = null,
      bool headless = true)
  ```
- **Root Cause of Inflation**:
  Contains multi-line signature (5 lines), archive file deserialization and validation (11 lines), graph solving, output extraction, and disposal logic.
- **Bonus Adversarial Note (Flaw A2 from Reviewer)**:
  Line 47 currently invokes `doc.NewSolution(true)`, which clears volatile inputs set by `ApplyInputs`. Changing to `doc.NewSolution(false)` fixes this bug during refactoring.
- **Recommended Extraction Blueprint**:
  Extract archive extraction:
  ```csharp
  private static (bool Success, string? Error) LoadDefinitionArchive(string filePath, GH_Document doc)
  {
      var archive = new GH_Archive();
      if (!archive.ReadFromFile(filePath))
          return (false, "Failed to deserialize Grasshopper definition from file.");
      if (!archive.ExtractObject(doc, "Definition"))
          return (false, "Failed to extract definition graph from archive.");
      return (true, null);
  }
  ```
  *Result*: `Run` drops from 53 lines down to **~24 lines**.

---

### 3.4 `Coordinator/GrasshopperContextExtractor.cs: BuildGrasshopperInfo` (CRITICAL BORDERLINE)
- **Location**: Lines 93–142 (Total: 50 lines, Body: 49 lines).
- **Current Signature**: `private static GrasshopperInfo BuildGrasshopperInfo()`
- **Risk Assessment**:
  Currently exactly **50 lines total**! Any small refactoring or added variable will immediately breach Rule M3.
- **Root Cause of Inflation**:
  Lines 106–123 manually iterate `doc.Objects` counting selected objects, component inputs, and parameter sources.
- **Recommended Extraction Blueprint**:
  ```csharp
  private static (int wireCount, int selectedCount) CountGraphMetrics(GH_Document doc)
  {
      int wireCount = 0;
      int selectedCount = 0;
      foreach (var obj in doc.Objects)
      {
          if (obj.Attributes?.Selected == true) selectedCount++;
          if (obj is IGH_Component comp)
          {
              foreach (var inp in comp.Params.Input) wireCount += inp.Sources.Count;
          }
          else if (obj is IGH_Param param)
          {
              wireCount += param.Sources.Count;
          }
      }
      return (wireCount, selectedCount);
  }
  ```
  *Result*: `BuildGrasshopperInfo` drops from 50 lines to **~28 lines**.

---

### 3.5 `Host/GrasshopperAddinApplication.cs: OnStartup` (BORDERLINE)
- **Location**: Lines 30–76 (Total: 47 lines, Body: 46 lines).
- **Current Signature**: `public Result OnStartup(UIControlledApplication application)`
- **Recommended Extraction Blueprint**:
  Extract `McpBridgeHost` assembly and configuration (lines 51–62) to:
  ```csharp
  private static McpBridgeHost CreateBridgeHost(
      GrasshopperBridgeCoordinator coordinator,
      BridgeSettings settings,
      BridgeSettingsStore settingsStore,
      string pipeName)
  ```
  *Result*: `OnStartup` drops from 47 lines to **~25 lines**.

---

### 3.6 `Tools/CanvasReader.cs: Read` (BORDERLINE)
- **Location**: Lines 21–66 (Total: 46 lines, Body: 41 lines).
- **Current Signature**: `public CanvasReadResult Read(...)`
- **Recommended Extraction Blueprint**:
  Extract component and wire extraction loop (lines 50–63):
  ```csharp
  private static (List<object> components, List<object> wires) ExtractComponentsAndWires(
      IEnumerable<IGH_DocumentObject> objects, string? categoryFilter, bool includeValues)
  ```
  *Result*: `Read` drops from 46 lines to **~22 lines**.

---

### 3.7 `UI/ShowStatusDialogCommand.cs: ShowWindow` (BORDERLINE)
- **Location**: Lines 28–71 (Total: 44 lines, Body: 43 lines).
- **Current Signature**: `public static void ShowWindow(IntPtr? ownerHwnd = null)`
- **Recommended Extraction Blueprint**:
  Promote embedded local function `OpenCore()` (lines 33–61: 29 lines) to a private static method:
  `private static void OpenCore(Dispatcher dispatcher, IntPtr? ownerHwnd)`
  *Result*: `ShowWindow` becomes a concise **8-line method**.

---

### 3.8 `Coordinator/GrasshopperContextExtractor.cs: ExtractFromRevit` (BORDERLINE)
- **Location**: Lines 50–91 (Total: 42 lines, Body: 37 lines).
- **Current Signature**: `private static ContextResult ExtractFromRevit(...)`
- **Recommended Extraction Blueprint**:
  Extract open documents collection (lines 65–72) into:
  `private static List<string> GetOpenDocTitles(Autodesk.Revit.ApplicationServices.Application? app)`
  *Result*: `ExtractFromRevit` drops from 42 lines to **~34 lines**.

---

### 3.9 `Execution/GrasshopperExecutionEngine.cs: RunRoslynCodeAsync` (BORDERLINE)
- **Location**: Lines 143–184 (Total: 42 lines, Body: 37 lines).
- **Current Signature**: `private async Task<ExecuteResult> RunRoslynCodeAsync(...)`
- **Recommended Extraction Blueprint**:
  Extract the 17-line inline lambda callback (lines 164–180) that prepares `GrasshopperScriptGlobals` and calls `compiled.Script!.RunAsync`:
  ```csharp
  private static object? ExecuteCompiledScript(
      UIApplication uiapp, object doc, CancellationToken linkedToken,
      Action<string?> log, ExecuteRequest request, CompiledScript compiled,
      IProgress<ScriptProgress>? progress)
  ```
  *Result*: `RunRoslynCodeAsync` drops from 42 lines to **~25 lines**.

---

## 4. Rule C2 File Length Comprehensive Inventory (All 29 Files)

| Rank | File Relative Path | Total Lines | Rule C2 Status (Limit <= 300) | Headroom Remaining |
|:---:|---|:---:|:---:|:---:|
| 1 | `Execution/RevitTransactionScope.cs` | **289** | PASS (High Risk) | **11 lines** |
| 2 | `Coordinator/GrasshopperBridgeCoordinator.cs` | **273** | PASS | 27 lines |
| 3 | `Execution/GrasshopperExecutionEngine.cs` | **215** | PASS | 85 lines |
| 4 | `Tools/CanvasEditor.cs` | **205** | PASS | 95 lines |
| 5 | `Host/GrasshopperAddinApplication.cs` | **195** | PASS | 105 lines |
| 6 | `Tools/PlayerRunner.cs` | **189** | PASS | 111 lines |
| 7 | `Host/GrasshopperAssemblyResolver.cs` | **188** | PASS | 112 lines |
| 8 | `Coordinator/GrasshopperContextExtractor.cs` | **176** | PASS | 124 lines |
| 9 | `Tools/CanvasReader.cs` | **172** | PASS | 128 lines |
| 10 | `Coordinator/GrasshopperCompilerReferences.cs` | **168** | PASS | 132 lines |
| 11 | `Execution/CanvasUndoScope.cs` | **141** | PASS | 159 lines |
| 12 | `Resources/Themes/MaterialThemeBridge.cs` | 95 | PASS | 205 lines |
| 13 | `Tools/ImageExporter.cs` | 74 | PASS | 226 lines |
| 14 | `UI/ShowStatusDialogCommand.cs` | 73 | PASS | 227 lines |
| 15 | `Execution/RevitTransactionContext.cs` | 66 | PASS | 234 lines |
| 16 | `Resources/Icons/GrasshopperRibbonIcons.cs` | 62 | PASS | 238 lines |
| 17 | `UI/GrasshopperBridgeStatusViewModel.cs` | 60 | PASS | 240 lines |
| 18 | `Support/GrasshopperScriptGlobals.cs` | 56 | PASS | 244 lines |
| 19 | `Resources/Themes/GrasshopperHostTheme.cs` | 43 | PASS | 257 lines |
| 20 | `Support/AutoDismissFailurePreprocessor.cs` | 39 | PASS | 261 lines |
| 21 | `Support/DocumentChangeCounter.cs` | 38 | PASS | 262 lines |
| 22 | `UI/GrasshopperBridgeStatusView.xaml.cs` | 35 | PASS | 265 lines |
| 23 | `Coordinator/WorkItemTracker.cs` | 30 | PASS | 270 lines |
| 24 | `Coordinator/RevitExternalEventRaiser.cs` | 28 | PASS | 272 lines |
| 25 | `Coordinator/GrasshopperExternalEventHandler.cs` | 24 | PASS | 276 lines |
| 26 | `Coordinator/McpBridgeWorkRequest.cs` | 21 | PASS | 279 lines |
| 27 | `Execution/ITransactionContext.cs` | 21 | PASS | 279 lines |
| 28 | `Resources/Themes/IHostTheme.cs` | 15 | PASS | 285 lines |
| 29 | `Coordinator/IExternalEventRaiser.cs` | 12 | PASS | 288 lines |

---

## 5. Root-Cause Analysis: Why Worker M3 Regex Verification Failed

Worker M3 used a regular expression that tested:
```python
any(l.strip().startswith(m) for m in ['public ', 'private ', 'protected ', 'internal ', 'static ']) and '(' in l
```
This naive approach failed due to three major structural patterns in idiomatic C#:
1. **Multi-line parameter lists**:
   In methods like `ExecuteCore`, `Export`, and `Run`, parameters are formatted on separate lines. A line like `private static ExecuteResult ExecuteCore(` does not contain `)` or `{` on the same line. If the lookahead window is too small or expects single-line declarations, the method is bypassed.
2. **Attributes with argument parentheses**:
   Attributes like `[Transaction(TransactionMode.Manual)]` or `[MethodImpl(MethodImplOptions.NoInlining)]` contain parentheses. Simple parsers confuse these attributes with method declarations and match the opening brace of the class or outer block, leading to false line calculations.
3. **Multi-line string literals and raw string literals**:
   String literals containing braces `{` or `}` corrupt brace-counting unless lexically stripped.

---

## 6. Automated Pre-Handoff Verification Scripts

Two ready-to-run verification scripts have been developed and validated against the entire codebase:

### Script A: Standalone Python AST Validator (`scratch/verify_clean_code_ast.py`)
- **Zero dependencies** (uses Python stdlib 3.x).
- Performs full tokenization of C# code, handles raw strings, verbatim strings, escaped quotes, block comments, and square-bracket attributes.
- Measures both total declaration span and inner body span.
- Exits with `code 0` on PASS, `code 1` on ANY Rule C2 or Rule M3 violation.

**Run Command**:
```powershell
python scratch/verify_clean_code_ast.py "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge" 40
```

### Script B: Roslyn C# Syntax Tree Validator (`scratch/ast_tool`)
- Powered by official Microsoft Roslyn compiler AST (`Microsoft.CodeAnalysis.CSharp 4.12.0`).
- Provides 100% indisputable AST metrics matching the C# compiler.
- Outputs detailed JSON metrics report to `metrics_result.json`.

**Run Command**:
```powershell
dotnet run --project scratch/ast_tool
```

### Worker Agent Pre-Handoff Command Gate:
Before submitting handoff for Milestone M3, worker agents MUST run:
```powershell
python scratch/verify_clean_code_ast.py "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge" 50
```
And verify that the output reports:
```
================================================================================
STRICT RULE M3 VIOLATIONS (> 50 LINES): 0 violations
  PASS: Zero methods violate Rule M3 (<= 50 lines).
================================================================================
```
