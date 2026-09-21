# Task Assignment for Explorer 2: AutoCAD Plot Engine & PdfSharp Integration

## Objective
Investigate `HPAutoCad/` CAD plot capabilities, package references, and PDF merging:
1. Examine `HPAutoCad/HPAutoCad.csproj`:
   - Current NuGet packages, AutoCAD reference assemblies (AutoCAD.NET 25.1.0: `AcCoreMgd`, `AcDbMgd`, `AcMgd`).
   - Check if `AcPublishMgd.dll` or plot-related assemblies are referenced or available via AutoCAD 2026.
   - Check how to add `PdfSharp` (v6.x for .NET 8) without version conflicts or repack issues.
2. Investigate the AutoCAD Plot Pipeline:
   - `PlotSettings`, `PlotSettingsValidator.Current`, `PlotInfo`, `PlotInfoValidator`, `PlotEngine` from `PlotFactory.ProcessPlotState.CreatePlotEngine()`.
   - Window plot configuration: `SetPlotWindowArea(Extents2d)`, `SetPlotType(PlotType.Window)`.
   - Paper size / media name (`CanonicalMediaName`), plot orientation (`PlotRotation`), scale / fit (`PlotCentered = true`, `UseStandardScale = false`, `StandardScaleType.ScaleToFit`).
   - Plot progress dialog (`PlotProgressDialog`) and cooperative cancellation with `CancellationToken`.
   - Safe execution: `DocumentLock`, system variables `BACKGROUNDPLOT=0` and `CMDECHO=0` with strict restoration in `finally`.
3. Investigate Frame Providers:
   - `BlockFrameProvider`: Scanning `BlockReference` entities, retrieving `EffectiveName` (for dynamic blocks), reading attribute references (`AttributeReference`) for title and sheet number.
   - `LayerFrameProvider`: Scanning closed `Polyline`, `Polyline2d`, `Polyline3d` on specified layer, extracting bounding box `Extents3d`.
   - `LayoutFrameProvider`: Scanning `LayoutManager.Current`, querying layouts by `TabOrder`, filtering by parsed layout range.
4. Investigate PDF Merging:
   - `IPdfMergeService` / `PdfMergeService` using PdfSharp (`PdfDocument`, `PdfPage`), importing pages from temp files, saving merged PDF, deleting temp files.

## Authoritative Reference
Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (see entry under ## 2026-09-20T22:21:59Z).

## Output Requirement
Write your findings to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_2\handoff.md`.

## 2026-09-20T22:23:25Z
You are Explorer 2. Your mission is to investigate the AutoCAD Plot Engine & PdfSharp Integration for Smart Plot Pro in HPAutoCad.
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_2
Read your task assignment: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_2\DISPATCH.md
Read the authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z)
Explore HPAutoCad/ project dependencies, plot pipeline (PlotSettings, PlotSettingsValidator, PlotInfo, PlotInfoValidator, PlotEngine, PlotProgressDialog), frame providers (Block, Layer, Layout), and PdfSharp integration.
Document your complete findings and architectural design in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_2\handoff.md.
Send a message back when complete.

