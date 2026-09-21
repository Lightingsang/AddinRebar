# Audit Assignment: Final Forensic Victory Auditor (Smart Plot Pro)

## Objective
Perform the comprehensive, final Forensic Victory Audit for Smart Plot Pro across all 4 deliverables:
1. Pure Logic Engine (`HPAutoCad.Core/SmartPlot/`):
   - Zero references to `Autodesk.*`, WPF, or native libraries.
   - Genuine spatial sorting with vertical overlap band (`PlotOrderService`).
   - Zero-exception safe range parsing (`LayoutRangeParser`).
   - Windows path sanitization & reserved device name prefixing (`FileNameService`).
   - Resilient JSON persistence with default fallback & locking (`PresetService`).
2. AutoCAD Plot Engine & PDF Merging (`HPAutoCad/SmartPlot/Cad/` & `Pdf/`):
   - Genuine AutoCAD plot pipeline (`PlotSettingsValidator`, `PlotInfoValidator`, `PlotFactory.CreatePublishEngine()`, `PlotEngine`).
   - Document locking: `using (doc.LockDocument())`.
   - System variables: `BACKGROUNDPLOT = 0` and `CMDECHO = 0` strictly saved and restored in `finally`.
   - Non-destructive plotting: `plotInfo.OverrideSettings = plotSettings`.
   - Thread affinity: no async `.ConfigureAwait(false)` inside document lock.
   - Dynamic block handling via `DynamicBlockTableRecord` (`EffectiveName`), attribute extraction, closed polyline detection, layout tab ordering.
   - In-process PDF merging via `PDFsharp` (v6.1.1) with safe temporary file cleanup only upon successful merge, avoiding self-deletion.
3. WPF MVVM Modeless UI & Theming (`HPAutoCad/SmartPlot/UI/`):
   - Modeless window (`SmartPlotWindow.xaml`) via `Application.ShowModelessWindow()`.
   - Dynamic dark/light theme sync via `MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance)`.
   - Interactive Pick Frame viewport support with window minimization and active document locking.
   - ViewModel (`SmartPlotViewModel.cs`) with `CommunityToolkit.Mvvm`, reactive properties, and cooperative cancellation.
4. Commands & Ribbon Integration (`HPAutoCad.Loader/` & `HPAutoCad/Entry.cs`):
   - `[CommandMethod("HPSMARTPLOT")]` and `[CommandMethod("HPLOT")]` registered in `HPAutoCad.Loader/Commands/SmartPlotCommands.cs`.
   - Entry point wired to `HPAutoCad.Entry.Start` (`["smartplot"]`).
   - Ribbon panel "Plot" on shared tab `HPAUTOCAD_MCP_TAB` with vector icon `Plot` in `RibbonIcons.cs`.
5. Assembly Repack & Packaging Isolation:
   - `HPAutoCad.dll` contains merged `MaterialDesignThemes.Wpf.dll` (~10.7 MB).
   - Zero loose `MaterialDesignThemes.Wpf.dll` in `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\App\`.
   - `PdfSharp.dll` deployed as clean loose assembly in `Contents\App\` for `AppLoadContext`.
6. Empirical Verification:
   - Run `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` -> Must be 0 errors.
   - Run `dotnet test HPAutoCad.Tests` -> Must pass all 412+ tests.
   - Check regression suites: `HPAutoCad.Mcp.Server.Tests` (280), `HPAutoCad.Aec.Tests` (225), `HPCivil3d.McpBridge.Tests` (60).
7. Deliver Verdict:
   - **`CLEAN`** (Full Victory Audit Pass) or **`INTEGRITY VIOLATION`**.

## Authoritative Reference
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (entry ## 2026-09-20T22:21:59Z)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_4\PROJECT.md`

## 2026-09-20T23:14:35Z
You are the Forensic Integrity Auditor conducting the final Victory Audit for Smart Plot Pro in HPAutoCad.
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_victory
Read your task: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_victory\DISPATCH.md
Read the authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z)
Read PROJECT.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_4\PROJECT.md

Conduct an exhaustive forensic audit across all 5 layers:
1. Pure logic engine in HPAutoCad.Core/SmartPlot/ (0 host references).
2. AutoCAD plot engine and frame providers in HPAutoCad/SmartPlot/Cad/.
3. In-process PDF merging via PDFsharp 6.1.1 in HPAutoCad/SmartPlot/Pdf/ (0 external CLI calls, safe cleanup).
4. Modeless WPF MVVM window, theme sync (AutocadHostTheme.Instance), and pick frame support in HPAutoCad/SmartPlot/UI/.
5. Command registration (HPSMARTPLOT/HPLOT) and ribbon integration (HPPLOT_PANEL) in HPAutoCad.Loader/.
6. Packaging isolation: RepackMaterialDesign into HPAutoCad.dll, loose PdfSharp.dll, zero loose MaterialDesignThemes.Wpf.dll.
7. Build & test verification:
   dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
   dotnet test HPAutoCad.Tests

Write your comprehensive forensic victory audit report with verdict CLEAN or INTEGRITY VIOLATION to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_victory\handoff.md.
Send a message when complete.
