# Task Assignment for Worker M2 (Iteration 2: Reviewer & Challenger Fixes)

## Objective
Remediate the 4 concrete findings from Reviewer M2 and Challenger M2:
1. `HPAutoCad/SmartPlot/Pdf/PdfMergeService.cs`:
   - **Fix Data Loss on Exception**: Only delete source files when merge succeeds (`isSuccess = true`). Do NOT delete source files in `finally` when an exception or cancellation occurs!
   - **Fix Self-Deletion**: When cleaning up source files, check `string.Equals(Path.GetFullPath(file), Path.GetFullPath(destinationPdfPath), StringComparison.OrdinalIgnoreCase)`. Never delete the destination file!
   - Ensure all 6 failing stress tests in `HPAutoCad.Tests/SmartPlot/PdfMergeServiceStressTests.cs` pass 100%.

2. `HPAutoCad/SmartPlot/Cad/Plot/AutoCadPlotEngine.cs`:
   - **Fix Thread Affinity**: Remove `.ConfigureAwait(false)` from all async awaits inside the document lock / AutoCAD execution context so that `Application.SetSystemVariable` and `docLock.Dispose()` always execute on the main AutoCAD UI thread.
   - **Fix Plot State Lockout**: Wrap `engine.BeginPlot()` and the plotting loop in `try ... finally` to guarantee `engine.EndPlot(null)` and `engine.Destroy()` are executed even when exceptions occur, avoiding permanent AutoCAD plot state lockout (`ProcessPlotState.Plotting`).
   - **Fix Cancellation Status**: If `isCancelled` is true (from `ct.IsCancellationRequested` or `progressDialog.IsPlotCancelled`), return `PlotResult.Failed("Plotting was cancelled by user.", ...)` instead of `PlotResult.Succeeded`. Do not merge partial PDFs on cancellation unless requested.
   - **Fix Merged Metrics**: In merged mode, pass the actual count of plotted sheets (`plottedFiles.Count`) into `PlotResult.Succeeded(outputFiles: [finalMergedPdf], mergedFile: finalMergedPdf, elapsed: ..., totalSheets: items.Count, plottedSheets: plottedFiles.Count)`.

## Verification
- Run `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug`
- Run `dotnet test HPAutoCad.Tests` (ensure all tests including `PdfMergeServiceStressTests` pass)

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine.

## Authoritative References
- Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2\handoff.md`
- Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m2\handoff.md`

## Output


## 2026-09-20T23:01:13Z
You are Worker M2 Iteration 2. Your mission is to fix the 4 issues in HPAutoCad/SmartPlot/Pdf/PdfMergeService.cs and HPAutoCad/SmartPlot/Cad/Plot/AutoCadPlotEngine.cs:
1. In PdfMergeService: delete temporary source files ONLY if merging was completely successful (not on exceptions/cancellations); skip deleting if source path equals destination path.
2. In AutoCadPlotEngine: remove ConfigureAwait(false) from async calls inside docLock; wrap BeginPlot() in try/finally to guarantee EndPlot() and Destroy() are called on exception; return PlotResult.Failed on user cancellation; correctly report plotted sheet count in merged mode.

