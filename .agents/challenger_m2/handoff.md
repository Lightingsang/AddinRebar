# Handoff Report — Milestone M2 Adversarial Challenge

**Agent**: Challenger M2 (Empirical Challenger)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m2\`  
**Milestone**: Smart Plot Pro M2 (CAD Plot Engine, Frame Providers & PDF Merging Stress)  
**Date**: 2026-09-20T23:01:00Z  
**Verdict**: **`REJECT / REQUEST_CHANGES`**

---

## 1. Observation

### 1.1 Baseline Build and Test Executions
1. **Compilation Check**:
   - Command: `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug`
   - Result: 0 Errors, 1 Warning (`ILRepack SwatchesProvider reference notice`). Output bundle deployed successfully.
2. **Baseline Unit Test Suite**:
   - Command: `dotnet test HPAutoCad.Tests --no-build`
   - Result: 405 total tests (402 passed, 3 skipped live tile tests, 0 failed).

### 1.2 Empirical Stress-Test Execution & Failures
An empirical test harness `HPAutoCad/HPAutoCad.Tests/SmartPlot/PdfMergeServiceStressTests.cs` was constructed and executed against `PdfMergeService`:
- Command: `dotnet test HPAutoCad.Tests --no-build`
- Result: **Failed! total: 415, failed: 6, succeeded: 406, skipped: 3. Exit code: 2.**

Verbatim test failure logs:
```
failed HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_CorruptPdfFile_ThrowsException_AndCheckSourceFilePreservation
  DATA LOSS BUG: Valid source file was deleted by finally block even though merge failed!
  at HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_CorruptPdfFile_ThrowsException_AndCheckSourceFilePreservation() in HPAutoCad.Tests\SmartPlot\PdfMergeServiceStressTests.cs:99

failed HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_DestinationFileLocked_ThrowsException_AndCheckSourceFilePreservation
  DATA LOSS BUG: Source file 1 was deleted even though destination was locked!
  at HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_DestinationFileLocked_ThrowsException_AndCheckSourceFilePreservation() in HPAutoCad.Tests\SmartPlot\PdfMergeServiceStressTests.cs:123

failed HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.MergeInternal_CancellationInsideTryBlock_DeletesSourceFilesBug
  DATA LOSS BUG: Source file 1 was deleted when canceled inside try block!
  at HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.MergeInternal_CancellationInsideTryBlock_DeletesSourceFilesBug() in HPAutoCad.Tests\SmartPlot\PdfMergeServiceStressTests.cs:177

failed HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_DestinationIsSameAsSource_CheckOutputPreservation
  BUG: Destination file was deleted by finally block because it matched a source path!
  at HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_DestinationIsSameAsSource_CheckOutputPreservation() in HPAutoCad.Tests\SmartPlot\PdfMergeServiceStressTests.cs:193

failed HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_ZeroByteFile_ThrowsException_AndCheckSourceFilePreservation
  DATA LOSS BUG: Valid source file was deleted when zero-byte file caused merge failure!
  at HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_ZeroByteFile_ThrowsException_AndCheckSourceFilePreservation() in HPAutoCad.Tests\SmartPlot\PdfMergeServiceStressTests.cs:208

failed HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_SourceFileLockedByReader_ThrowsException_AndCheckSourcePreservation
  DATA LOSS BUG: File 1 was deleted when File 2 could not be read!
  at HPAutoCad.Tests.SmartPlot.PdfMergeServiceStressTests.Merge_SourceFileLockedByReader_ThrowsException_AndCheckSourcePreservation() in HPAutoCad.Tests\SmartPlot\PdfMergeServiceStressTests.cs:228
```

### 1.3 Code Inspection & Static Observations
1. **`HPAutoCad/HPAutoCad/SmartPlot/Pdf/PdfMergeService.cs` (lines 58–100)**:
   ```csharp
   try
   {
       using var outputDocument = new PdfDocument();
       foreach (var filePath in existingFiles)
       {
           ct.ThrowIfCancellationRequested();
           using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
           ...
       }
       ...
       outputDocument.Save(destinationPdfPath);
       return true;
   }
   finally
   {
       if (deleteSourceFilesAfterMerge)
       {
           foreach (var file in existingFiles)
           {
               TryDeleteFileWithRetry(file);
           }
       }
   }
   ```
   *Direct Observation*: The `finally` block runs unconditionally on both normal and exceptional exits. If `PdfReader.Open` throws on a corrupt file, or destination file is locked when calling `outputDocument.Save`, or cancellation is requested, all files in `existingFiles` are permanently deleted even though no valid merged PDF was generated. Furthermore, if `destinationPdfPath` matches any file in `existingFiles`, `finally` deletes the newly saved destination PDF.

2. **`HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/AutoCadPlotEngine.cs` (lines 124–127 & 265–300)**:
   ```csharp
   for (int i = 0; i < selectedItems.Count; i++)
   {
       ct.ThrowIfCancellationRequested();
       if (progressDialog.IsPlotCancelled)
       {
           break;
       }
       ...
   }

   engine.EndPlot(null);
   progressDialog.OnEndPlot();

   if (isMerged && generatedFiles.Count > 0)
   {
       ...
       mergedSuccessfully = await _pdfMergeService.MergeAsync(...);
   }

   progress?.Report(new PlotProgressUpdate { StatusMessage = "Plotting complete!", IsCompleted = true });
   return PlotResult.Succeeded(...);
   ```
   *Direct Observation*: If the user clicks "Cancel Job" on AutoCAD's native `PlotProgressDialog`, `progressDialog.IsPlotCancelled` is true and the loop executes `break;`. The engine proceeds to merge whatever partial sheets were printed, reports "Plotting complete!", and returns `PlotResult.Succeeded`. A user-cancelled batch plot is falsely reported as complete success.

3. **`HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/AutoCadPlotEngine.cs` (lines 118–267 & 302–316)**:
   ```csharp
   using var engine = PlotFactory.CreatePublishEngine();
   engine.BeginPlot(progressDialog, null);

   for (int i = 0; i < selectedItems.Count; i++)
   {
       ... // Sheet setup and engine.BeginDocument()
   }

   engine.EndPlot(null);
   progressDialog.OnEndPlot();
   ```
   *Direct Observation*: There is no `try/finally` block enclosing `engine.BeginPlot()` and `engine.EndPlot()`. If an exception occurs during the loop (e.g. `ct.ThrowIfCancellationRequested()`, device initialization failure, or drawing error), execution jumps directly to `catch (System.Exception ex)`. `engine.EndPlot(null)` and `progressDialog.OnEndPlot()` are never called, and `progressDialog.Destroy()` is never invoked. AutoCAD's internal plot state is left stuck in `ProcessPlotState.Plotting`, permanently breaking all subsequent plot operations until AutoCAD is restarted.

4. **`HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/AutoCadPlotEngine.cs` (lines 297–300)**:
   ```csharp
   return PlotResult.Succeeded(
       isMerged ? (mergedSuccessfully && finalMergedPdf is not null ? [finalMergedPdf] : []) : generatedFiles,
       merged: isMerged ? finalMergedPdf : null,
       elapsed: stopwatch.Elapsed);
   ```
   *Direct Observation*: When `isMerged` is true, the files list passed to `PlotResult.Succeeded` contains only `[finalMergedPdf]` (1 item). `PlotResult.Succeeded` sets `TotalSheets = files.Count` and `PlottedSheets = files.Count`. Thus, plotting 50 sheets into a single merged PDF reports `TotalSheets = 1` and `PlottedSheets = 1` instead of `50`.

---

## 2. Logic Chain

1. **Catastrophic Source File Loss in `PdfMergeService`**:
   - In `PdfMergeService.MergeInternal`, `existingFiles` collects all existing file paths.
   - The deletion logic is placed directly in the `finally` block of `MergeInternal` without verifying whether `outputDocument.Save` succeeded.
   - When any error occurs during reading (corrupt PDF, 0-byte file, locked file) or during writing (destination locked by Acrobat or read-only directory), or upon `ct` cancellation, the `finally` block executes and deletes all existing files in `existingFiles`.
   - Result: Users lose their original PDF files without obtaining the merged PDF.
   - Furthermore, if `destinationPdfPath` equals one of the source paths, `finally` deletes the output file immediately after saving.

2. **False Success on User Cancellation in `AutoCadPlotEngine`**:
   - When `progressDialog.IsPlotCancelled` is true, the loop breaks cleanly.
   - But execution falls through to lines 265–300, calling `_pdfMergeService.MergeAsync` on partial files, reporting `"Plotting complete!"`, and returning `PlotResult.Succeeded`.
   - Calling code (ViewModel/UI) has no way of knowing the user cancelled the plot; the UI displays complete success for an incomplete job.

3. **AutoCAD Plot Pipeline State Lockout**:
   - AutoCAD's `PlotEngine` and `PlotProgressDialog` represent stateful native subsystems. Calling `engine.BeginPlot()` places AutoCAD in `ProcessPlotState.Plotting`.
   - Because `engine.EndPlot(null)` is outside any `try/finally` block, any unhandled exception in the loop bypasses `EndPlot`.
   - The next call to `AutoCadPlotEngine.PlotAsync` evaluates `if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)` at line 63 and immediately returns failure: *"Another plot operation is already in progress in AutoCAD"*. AutoCAD remains locked in a plotting state until killed and restarted.

4. **Distorted Metric Reporting in Merged Mode**:
   - In `PlotResult.cs`, `Succeeded` sets `TotalSheets` and `PlottedSheets` to `files.Count`.
   - In `AutoCadPlotEngine.cs`, `files` in merged mode is `[finalMergedPdf]`.
   - As a consequence, batch plotting reports 1 sheet plotted even when dozens were processed.

---

## 3. Caveats

- **AutoCAD Native Runtime Verification**: The native AutoCAD plot engine methods (`PlotFactory.CreatePublishEngine()`, `PlotSettingsValidator.Current`) require an active AutoCAD process with loaded graphics devices. They cannot be executed inside a standalone test runner. Their failure modes were diagnosed and confirmed through static code auditing and architectural inspection against the AutoCAD .NET API lifecycle rules.
- **Pure Logic and In-Process PDF Merging**: `PdfMergeService` is pure in-process .NET and was empirically proven defective via 6 failing tests in `HPAutoCad.Tests`.

---

## 4. Conclusion

**VERDICT**: **`REJECT / REQUEST_CHANGES`**

Milestone M2 cannot be approved in its current state due to critical data loss bugs in `PdfMergeService` and severe session lifecycle defects in `AutoCadPlotEngine`.

### Required Fixes for Worker M2:

1. **Fix `PdfMergeService.cs` (Critical)**:
   - Introduce a `bool mergeSucceeded = false;` flag inside `MergeInternal`. Set it to `true` ONLY after `outputDocument.Save(destinationPdfPath)` finishes without throwing.
   - In `finally`, delete source files ONLY if `deleteSourceFilesAfterMerge && mergeSucceeded`.
   - Guard against deleting the destination file if it matches one of the source files:
     ```csharp
     if (deleteSourceFilesAfterMerge && mergeSucceeded)
     {
         foreach (var file in existingFiles)
         {
             if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(destinationPdfPath), StringComparison.OrdinalIgnoreCase))
                 continue;
             TryDeleteFileWithRetry(file);
         }
     }
     ```

2. **Fix `AutoCadPlotEngine.cs` Cooperative Cancellation (High)**:
   - Check `progressDialog.IsPlotCancelled`. If cancelled, clean up temporary files and return `PlotResult.Failed("Plot canceled by user.", selectedItems.Count, generatedFiles.Count);` instead of `PlotResult.Succeeded`.

3. **Fix `AutoCadPlotEngine.cs` Plot Session Lifecycle (High)**:
   - Wrap the plot loop in a `try / finally` block to guarantee `engine.EndPlot(null)` and `progressDialog.OnEndPlot()` are called, and ensure `progressDialog.Destroy()` and `engine.Destroy()` are executed in `finally`.
   - Ensure `ProcessPlotState` is always reset to `NotPlotting`.

4. **Fix `AutoCadPlotEngine.cs` Merged Mode Metrics (Medium)**:
   - In merged mode, pass the actual plotted sheet count (`selectedItems.Count` or `generatedFiles.Count`) to `PlotResult.Succeeded` so metrics accurately reflect the number of sheets processed.

---

## 5. Verification Method

To verify the defects and validate future fixes:

1. **Run Empirical Stress Suite**:
   ```powershell
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\HPAutoCad.Tests.csproj" --no-build
   ```
   - *Current Result*: 6 tests in `PdfMergeServiceStressTests` FAIL with data loss assertions.
   - *Pass Condition after Fix*: All tests in `PdfMergeServiceStressTests` must PASS 100%.

2. **Run Full Solution Build**:
   ```powershell
   dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.slnx" -c Debug
   ```
   - Must build with 0 errors.
