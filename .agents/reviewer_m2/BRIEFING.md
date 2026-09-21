# BRIEFING — 2026-09-20T22:55:36Z

## Mission
Review Milestone M2 (AutoCAD Plot Engine & PDF Merging) implementation and test suite for correctness, quality, adversarial robustness, and integrity.

## 🔒 My Identity
- Archetype: reviewer / critic
- Roles: reviewer, critic
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2
- Original parent: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Milestone: M2 (AutoCAD Plot Engine & PDF Merging)
- Instance: 1 of 1

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Integrity check: actively check for hardcoded test results, facade implementations, bypassed work, fabricated outputs
- Verify packaging isolation: PDFsharp 6.1.1 present and not repacked into HPAutoCad.dll
- Issue verdict APPROVE or REQUEST_CHANGES

## Current Parent
- Conversation ID: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Updated: 2026-09-20T22:55:36Z

## Review Scope
- **Files to review**:
  - `HPAutoCad/SmartPlot/Pdf/` (`IPdfMergeService.cs`, `PdfMergeService.cs`)
  - `HPAutoCad/SmartPlot/Cad/Providers/` (`IFrameProvider.cs`, `FrameScanOptions.cs`, `BlockFrameProvider.cs`, `LayerFrameProvider.cs`, `LayoutFrameProvider.cs`, `AutoCadFrameProvider.cs`)
  - `HPAutoCad/SmartPlot/Cad/Plot/` (`IAutoCadPlotEngine.cs`, `PlotProgressUpdate.cs`, `AutoCadPlotEngine.cs`)
  - `HPAutoCad.Tests/SmartPlot/` (`PdfMergeServiceTests.cs`, `AutoCadPlotEngineTests.cs`, `FrameProviderTests.cs`)
  - `HPAutoCad/HPAutoCad.csproj`
- **Interface contracts**: `ORIGINAL_REQUEST.md`, `worker_m2/handoff.md`
- **Review criteria**: Correctness, Completeness, Quality, Risk, Adversarial Edge Cases, Integrity

## Review Checklist
- **Items reviewed**:
  - `HPAutoCad/HPAutoCad/HPAutoCad.csproj`
  - `HPAutoCad/HPAutoCad/SmartPlot/Pdf/IPdfMergeService.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Pdf/PdfMergeService.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/IFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/FrameScanOptions.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/BlockFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/LayerFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/LayoutFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Providers/AutoCadFrameProvider.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/IAutoCadPlotEngine.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/PlotProgressUpdate.cs`
  - `HPAutoCad/HPAutoCad/SmartPlot/Cad/Plot/AutoCadPlotEngine.cs`
  - `HPAutoCad/HPAutoCad.Tests/SmartPlot/PdfMergeServiceTests.cs`
  - `HPAutoCad/HPAutoCad.Tests/SmartPlot/FrameScanOptionsTests.cs`
- **Verdict**: REQUEST_CHANGES
- **Unverified claims**: None; all builds, packaging DLL outputs, and test suites independently executed and verified.

## Attack Surface
- **Hypotheses tested**:
  - Thread affinity under `ConfigureAwait(false)` during `docLock.Dispose()` and `SetSystemVariable` in `AutoCadPlotEngine.PlotAsync`
  - Data loss in `PdfMergeService` when `Save()` throws or cancellation occurs with `deleteSourceFilesAfterMerge: true`
  - False success reporting when user clicks "Cancel" on native `PlotProgressDialog` in `AutoCadPlotEngine`
  - Multi-page per sheet vs single-page document in `BeginPage(isLast)`
  - Assembly packaging collision and ALC isolation for `PDFsharp` and `MaterialDesignThemes`
- **Vulnerabilities found**:
  - Major: `AutoCadPlotEngine.cs` awaits `MergeAsync(...).ConfigureAwait(false)` inside `docLock` scope, causing ThreadPool resumption that violates AutoCAD thread affinity on `Application.SetSystemVariable` and `docLock.Dispose()`.
  - Major: `PdfMergeService.cs` deletes source PDF files unconditionally in `finally` even when merge fails (e.g. locked output file, disk full, or cancellation), causing irreversible data loss.
  - Major: `AutoCadPlotEngine.cs` breaks on `progressDialog.IsPlotCancelled` but post-loop logic ignores cancellation and reports `PlotResult.Succeeded`.
- **Untested angles**: Live CAD plotting against a physical or system plotter driver (requires running AutoCAD process with graphics/hardware drivers).

## Key Decisions Made
- Confirmed zero integrity violations: genuine implementations across all subsystems, no fake mocks or test tampering.
- Confirmed packaging isolation: PDFsharp 6.1.1 is external DLL; MaterialDesignThemes is repacked into HPAutoCad.dll.
- Issued verdict REQUEST_CHANGES due to 3 high-impact runtime bugs in `AutoCadPlotEngine` and `PdfMergeService`.

## Artifact Index
- DISPATCH.md — incoming dispatch instructions
- BRIEFING.md — working memory
- progress.md — liveness heartbeat
- handoff.md — final review and challenge report
