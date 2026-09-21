# BRIEFING — 2026-09-20T13:23:00Z

## Mission
Formulate exact implementation specification for WPF Views, ViewModels, WebView2 integration, and Theming in HPAutoCad/HPGeoLink/ for Milestone M2.

## 🔒 My Identity
- Archetype: explorer
- Roles: Read-only investigation, UI/MVVM architecture analysis, synthesis and reporting
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m2_ui\
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f
- Milestone: M2 (WPF MVVM UI & Theming)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement or modify source code
- Strictly formulate plans and specifications
- Deliverable: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m2_ui_plan.md

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T13:23:00Z

## Investigation State
- **Explored paths**: `HPGeo/HPGeo.AutoCad/UI/*`, `HPAutoCad/HPAutoCad.McpBridge/*`, `HPRebar/HPRebar/Resources/Themes/*`, `HPAutoCad/HPAutoCad.Core/HPGeoLink/*`
- **Key findings**:
  - Full inventory of 25 UI files mapped cleanly into `HPGeoLink/View/`, `ViewModel/`, `Model/`, and `Resources/Themes/`.
  - Namespace migration from `HPGeo.AutoCad.UI` to `HPAutoCad.HPGeoLink.View/ViewModel/Model` and `HPAutoCad.Resources.Themes`.
  - ALC Contextual Reflection pattern (`EnterContextualReflection`) verified as mandatory for BAML loading and theme switching in AutoCAD 2026 .NET 8 `AppLoadContext`.
  - WebView2 integration requires `%LocalAppData%\HPAutoCad\webview2` user-data directory and zero-crash fallback pattern.
  - Theming architecture with MaterialDesignThemes 5.3.2, `ThemeInfoAttribute`, `Theme.xaml`, `MaterialBridge.xaml`, `ThemeDark/Light.xaml`, and `AutocadHostTheme` responding to `COLORTHEME`.
- **Unexplored areas**: None. Exploration complete.

## Key Decisions Made
- Reorganized legacy flat `UI/` into standard MVVM folders: `View/`, `ViewModel/`, `Model/`, and shared `Resources/Themes/`.
- Decoupled shell dialogs via `IGeoExportShell` and `IGeoImportShell` for 100% testable ViewModels.
- Embedded Leaflet 1.9.4 via SRI-verified CDN with Esri World Imagery.
- Retained `%LocalAppData%\HPGeo\logs\` in `HPGeoLog` for harness compatibility.

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m2_ui_plan.md — Comprehensive implementation plan for Milestone M2 UI
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m2_ui\handoff.md — 5-component handoff report
