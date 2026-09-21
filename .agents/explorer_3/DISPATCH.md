# Task Assignment for Explorer 3: UI, Theming, Ribbon, Loader & ILRepack

## Objective
Investigate WPF MVVM UI, themes, ribbon integration, loader wiring, and ILRepack packaging:
1. Examine WPF MVVM and Theme Infrastructure:
   - `CommunityToolkit.Mvvm` (8.4.0) and `MaterialDesignThemes` (5.3.2) setup in `HPAutoCad`.
   - `AutocadHostTheme.Instance` (listening to `COLORTHEME` 55 vs 245) and `MaterialBridge.xaml`, `ThemeDark.xaml`, `ThemeLight.xaml`.
   - Modeless window pattern via `Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessWindow()` allowing viewport pan/zoom and "Pick" frame.
   - `SmartPlotWindow.xaml` and `SmartPlotViewModel`: Structure, tabs (Plot, Presets, Settings, About), cards (Frame Source, Printer & Paper, Plot Style, Orientation, Output & Naming), progress bar.
2. Examine Command Registration and Ribbon Architecture:
   - Existing command registration in `HPAutoCad/Commands/` (e.g., `HPGeoLinkCommands.cs`).
   - `HPAutoCad.Loader` architecture: `Entry.Start`, `AppLoadContext`, how commands and ribbon panels are registered.
   - Ribbon tab `HPAUTOCAD_MCP_TAB` ("HPAutoCad"): adding a "Plot" panel with a "Smart Plot Pro" button, icon resource handling.
3. Examine ILRepack packaging (`RepackMaterialDesign` target in `HPAutoCad.csproj`):
   - How `MaterialDesignThemes.Wpf.dll` is repackaged into `HPAutoCad.dll`.
   - Verify if `PdfSharp.dll` needs any special repack or copy-to-output handling in the bundle.

## Authoritative Reference
Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (see entry under ## 2026-09-20T22:21:59Z).

## Output Requirement
Write your findings to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_3\handoff.md`.

## 2026-09-20T22:23:25Z
You are Explorer 3. Your mission is to investigate the UI, Theming, Ribbon, Loader & ILRepack Architecture for Smart Plot Pro in HPAutoCad.
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_3
Read your task assignment: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_3\DISPATCH.md
Read the authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z)
Explore HPAutoCad/ WPF MVVM, AutocadHostTheme, MaterialBridge.xaml, ThemeDark/Light palettes, RepackMaterialDesign target, HPAutoCad.Loader (Entry.Start, AppLoadContext, ribbon registration), and command registration.
Document your complete findings and architectural design in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_3\handoff.md.
Send a message back when complete.

