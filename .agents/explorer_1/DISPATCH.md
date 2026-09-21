# Task Assignment for Explorer 1: Core Logic & Test Architecture

## Objective
Investigate `HPAutoCad.Core/` and `HPAutoCad.Tests/` to prepare the foundation for `HPAutoCad.Core/SmartPlot/`:
1. Check `HPAutoCad.Core/` project structure, TargetFramework (net8.0), existing namespaces, conventions, and verify zero dependencies on AutoCAD APIs.
2. Check how tests are structured in `HPAutoCad.Tests/` (xUnit v3 / MTP, project configuration, references to `HPAutoCad.Core`).
3. Plan exact implementation details for:
   - Models: `PlotItem`, `PlotConfiguration`, `PlotPreset`, `PlotResult`, and Enums (`FrameSourceType`, `OutputMode`, `OrientationMode`).
   - `IPlotOrderService` / `PlotOrderService`: Zic-zac / row clustering algorithm using tolerance overlap band in Y coordinates before sorting X.
   - `LayoutRangeParser`: Safe parsing of range strings ("All", "1-5", "1,3,5", "1-3,5,8-10") without throwing exceptions on malformed input.
   - `IFileNameService` / `FileNameService`: Sanitize illegal characters via `Path.GetInvalidFileNameChars()`, token replacements (`{Prefix}`, `{Layout}`, `{Title}`, `{SheetNo}`).
   - `IPresetService` / `PresetService`: JSON read/write in `%AppData%\HPAutoCad\SmartPlot\presets.json` using `System.Text.Json`.

## Authoritative Reference
Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (see entry under ## 2026-09-20T22:21:59Z).

## Output Requirement
Write your findings to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_1\handoff.md`.

## 2026-09-20T22:23:25Z
You are Explorer 1. Your mission is to investigate the Core Logic & Test Architecture for Smart Plot Pro in HPAutoCad.
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_1
Read your task assignment: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_1\DISPATCH.md
Read the authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z)
Explore HPAutoCad.Core/ and HPAutoCad.Tests/ to examine existing architectures, models, math, namespaces, and test patterns.
Document your complete findings and architectural design in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_1\handoff.md.
Send a message back when complete.
