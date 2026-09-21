## 2026-09-20T22:22:44Z

You are the Project Orchestrator (orchestrator_4) for Smart Plot Pro.

Your identity and working directory:
- Identity: orchestrator_4
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_4
- Project working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad
- Authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (see entry under ## 2026-09-20T22:21:59Z)

Mission:
Build Smart Plot Pro, an advanced automated batch plotting and PDF publishing tool integrated into the HPAutoCad ecosystem (AutoCAD 2026, .NET 8), supporting frame extraction by Block/Layer/Layout, automatic paper size/orientation detection, spatial sorting, and single/merged PDF generation via PdfSharp.

Requested team: Full team (chuyên biệt hóa theo từng phân lớp: Core logic, CAD plot engine, WPF MVVM UI, Ribbon/Loader, và Unit tests).
Integrity mode: demo.

Requirements Summary:
1. Pure Logic Engine (HPAutoCad.Core/SmartPlot/):
   - Models: PlotItem, PlotConfiguration, PlotPreset, PlotResult, Enums (FrameSourceType, OutputMode, OrientationMode).
   - Services: IPlotOrderService / PlotOrderService (sorting with tolerance overlap band for zic-zac frames), LayoutRangeParser (safe parsing of All, 1-5, 1,3,5, etc.), IFileNameService / FileNameService (token replacement and illegal char sanitization), IPresetService / PresetService (JSON storage in %AppData%\HPAutoCad\SmartPlot\presets.json). Zero AutoCAD references.
2. AutoCAD Plot Engine & Providers (HPAutoCad/SmartPlot/Cad/):
   - Frame Providers: IFrameProvider, BlockFrameProvider (EffectiveName, Dynamic Block, attribute extraction for sheet title/number), LayerFrameProvider (closed polyline bounds on target layer), LayoutFrameProvider (tab order and range filter).
   - AutoCadPlotEngine: Standard AutoCAD plot pipeline (PlotSettings -> PlotSettingsValidator -> PlotInfo -> PlotInfoValidator -> PlotEngine), Window plot, auto orientation detection, printer/paper/plot style/fit/center, DocumentLock, BACKGROUNDPLOT=0 and CMDECHO=0 in try/finally, PlotProgressDialog, cooperative CancellationToken.
3. In-Process PDF Merging Service (HPAutoCad/SmartPlot/Pdf/):
   - Package PdfSharp (v6.x for .NET 8) in HPAutoCad.csproj.
   - IPdfMergeService / PdfMergeService: merge temp PDFs in-process, clean up temporary files.
4. WPF MVVM Modeless UI & Theme Synchronization (HPAutoCad/SmartPlot/UI/):
   - CommunityToolkit.Mvvm (8.4.0) and MaterialDesignThemes (5.3.2).
   - Modeless window (Application.ShowModelessWindow) allowing viewport pan/zoom and "Pick" frame.
   - Theme sync via AutocadHostTheme.Instance (COLORTHEME 55 vs 245) + MaterialBridge.xaml.
   - Assembly isolation: RepackMaterialDesign target via ILRepack (no loose MaterialDesignThemes.Wpf.dll).
   - SmartPlotWindow.xaml & SmartPlotViewModel.
5. Command Registration & Ribbon Integration:
   - Commands/SmartPlotCommands.cs: HPSMARTPLOT and HPLOT.
   - Wiring via HPAutoCad.Loader (delegates in Entry.Start / AppLoadContext).
   - Ribbon button "Smart Plot Pro" on "Plot" panel in HPAUTOCAD_MCP_TAB with vector/theme-aware icon.
6. Verification & Automated Tests:
   - Unit tests in HPAutoCad.Tests/SmartPlot/: PlotOrderServiceTests, LayoutRangeParserTests, FileNameServiceTests, PresetServiceTests.
   - Build: dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
   - Tests: dotnet test HPAutoCad.Tests
