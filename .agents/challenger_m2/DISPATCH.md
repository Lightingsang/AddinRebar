# Challenge Assignment: Challenger (Milestone M2)

## Objective
Adversarially challenge and stress-test the Milestone M2 implementation:
1. Stress-test `PdfMergeService`:
   - Merge empty lists, single file, multi-page PDFs, corrupt PDF files, locked files.
   - Verify page count preservation and temp file cleanup behavior.
2. Stress-test `FrameScanOptions` and provider contracts.
3. Inspect `AutoCadPlotEngine` for edge cases:
   - System variable recovery under exception conditions (`BACKGROUNDPLOT`, `CMDECHO`).
   - Cooperative cancellation timing during engine progress.
   - Resource disposal (`PlotEngine.Destroy()`, `PlotProgressDialog.Dispose()`).
4. Deliver verdict: `APPROVE` or `REJECT` / `REQUEST_CHANGES`.

## Output
## 2026-09-20T22:55:36Z

<USER_REQUEST>
You are Challenger for Milestone M2 (Plot Engine & PDF Merging Stress).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m2
Read your task: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m2\DISPATCH.md
Read the authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z)
Read worker_m2 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2\handoff.md

Adversarially challenge and stress-test PdfMergeService and AutoCadPlotEngine.
Run builds and tests:
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
dotnet test HPAutoCad.Tests

Write your report with verdict APPROVE or REJECT to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m2\handoff.md.
Send a message when complete.
</USER_REQUEST>
