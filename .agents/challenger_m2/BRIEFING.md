# BRIEFING — 2026-09-20T22:56:00Z

## Mission
Adversarially challenge and stress-test Milestone M2 (Plot Engine, Frame Providers & PDF Merging).

## 🔒 My Identity
- Archetype: empirical challenger
- Roles: critic, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m2
- Original parent: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Milestone: Smart Plot Pro M2
- Instance: 1 of 1

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Report failures as findings — do NOT fix them yourself
- EMPIRICAL: Write and execute tests/harnesses; reproduce findings empirically

## Current Parent
- Conversation ID: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Updated: 2026-09-20T22:55:36Z

## Review Scope
- **Files to review**:
  - `HPAutoCad/HPAutoCad/SmartPlot/Pdf/IPdfMergeService.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Pdf/PdfMergeService.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/IAutoCadPlotEngine.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/AutoCadPlotEngine.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/PlotProgressUpdate.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/IFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/FrameScanOptions.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/BlockFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/LayerFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/LayoutFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/AutoCadFrameProvider.cs`
  - `HPAutoCad/HPAutoCad.Tests/SmartPlot/PdfMergeServiceTests.cs`
  - `HPAutoCad/HPAutoCad.Tests/SmartPlot/FrameScanOptionsTests.cs`
- **Interface contracts**: ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z), DISPATCH.md
- **Review criteria**: Correctness, stress resilience, edge cases, resource cleanup, system variable safety

## Attack Surface
- **Hypotheses tested**:
  - H1: `PdfMergeService` deletes source files in `finally` block even when merge throws an exception (corrupted PDF, zero-byte file, locked destination, locked source). -> CONFIRMED BUG (DATA LOSS).
  - H2: `PdfMergeService` deletes destination PDF if destination path matches one of the input source paths. -> CONFIRMED BUG (DATA LOSS).
  - H3: `AutoCadPlotEngine` cooperative cancellation via `PlotProgressDialog.IsPlotCancelled` reports success instead of cancellation. -> CONFIRMED BUG.
  - H4: `AutoCadPlotEngine` exception during plotting skips `engine.EndPlot()` and `progressDialog.OnEndPlot()/Destroy()`, locking AutoCAD plotting state indefinitely. -> CONFIRMED DEFECT.
  - H5: `AutoCadPlotEngine` in merged mode reports `TotalSheets = 1` and `PlottedSheets = 1` regardless of actual sheet count. -> CONFIRMED DEFECT.
  - H6: `AutoCadPlotEngine` system variable restoration (`BACKGROUNDPLOT`, `CMDECHO`). -> VERIFIED SAFE.
- **Vulnerabilities found**:
  - CRITICAL: Data loss in `PdfMergeService.cs` (lines 90-99). `finally` block unconditionally calls `TryDeleteFileWithRetry` on all existing source files regardless of whether `outputDocument.Save` succeeded or an exception occurred.
  - CRITICAL: Destination file self-deletion in `PdfMergeService.cs` when destination matches any source file.
  - HIGH: `AutoCadPlotEngine.cs` treats `progressDialog.IsPlotCancelled` as complete success with `PlotResult.Succeeded`.
  - HIGH: `AutoCadPlotEngine.cs` lacks `try/finally` around `engine.BeginPlot()` / `engine.EndPlot()`. An exception during sheet plotting leaves `PlotFactory.ProcessPlotState` in `ProcessPlotState.Plotting`, permanently breaking future plot commands until AutoCAD is restarted.
  - MEDIUM: `AutoCadPlotEngine.cs` in merged mode distorts sheet count statistics (`TotalSheets = 1`, `PlottedSheets = 1`).
- **Untested angles**:
  - Full native AutoCAD plot execution against real physical plotter PC3 devices (requires running in AutoCAD process; verified statically and via code review against ObjectARX / AutoCAD .NET API lifecycle rules).

## Loaded Skills
- None

## Key Decisions Made
- [Verdict] REJECT Milestone M2 with REQUEST_CHANGES. The implementation of `PdfMergeService` causes catastrophic source file data loss on any error, and `AutoCadPlotEngine` mismanages the plot session lifecycle on cancellation and exceptions.

## Artifact Index
- handoff.md — Final verdict and empirical findings
- progress.md — Liveness heartbeat

