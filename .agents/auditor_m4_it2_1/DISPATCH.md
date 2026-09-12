# DISPATCH — auditor_m4_it2_1

## Mission
You are the Forensic Integrity Auditor for Milestone M4 Iteration 2 (WPF MVVM UI, Dynamic Theming, and Interactive Preview Canvases).

## Mandatory Reading
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4_it2\handoff.md`
4. Codebase under audit:
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamElevationPainter.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamElevationCanvas.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamSectionPainter.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamSectionCanvas.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/CanvasPalette.cs`
   - `HPRebar/HPRebar/Beam Rebar/View Models/BeamRebarSession.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/BeamRebarView.xaml`
   - `HPRebar/HPRebar/Beam Rebar/View/Tabs/GeometryTabView.xaml`
   - `HPRebar/HPRebar/Beam Rebar/View/Tabs/AdditionalBarsTabView.xaml`
   - `HPRebar.Core/BeamRebar/Models/BeamContinuousStack.cs`
   - `HPRebar/HPRebar/Beam Rebar/Models/BeamStack.cs`
   - `HPRebar/HPRebar.Core/`

## Forensic Audit Objectives
Conduct an exhaustive forensic integrity audit across all modified and newly authored files:
1. **Zero Cheating & Authentic Logic**:
   - Verify that there are no dummy, stubbed, or facade implementations.
   - Verify that all calculations in `BeamElevationPainter`, `BeamSectionPainter`, `BeamContinuousStack`, and `BeamRebarSession` represent genuine geometry, validation, and rendering logic.
2. **Decoupling Integrity**:
   - Confirm that `HPRebar.Core` continues to have **ZERO references** to `Autodesk.Revit.*`.
3. **API Modernity**:
   - Confirm zero deprecated Revit APIs in newly modified files.
4. **Binding & Theme Authenticity**:
   - Confirm that `BeamRebarView.xaml` and tab views use authentic MVVM bindings to `BeamRebarViewModel` and `BeamRebarSession`.
   - Confirm that all resource references use valid `{DynamicResource Brush.X}` and `{DynamicResource Spacing.X}` / `{DynamicResource Font.Size.X}` tokens.
5. **Binary Verdict**:
   - If any cheating, hardcoding, tautological test, or decoupling violation is detected, your verdict MUST be **INTEGRITY_VIOLATION**.
   - If completely clean, your verdict is **CLEAN**.

## Output
Write your forensic audit report to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m4_it2_1\audit_report.md`
Write your handoff to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m4_it2_1\handoff.md`
Notify orchestrator via `send_message` with your binary verdict: **CLEAN** or **INTEGRITY_VIOLATION**.

## 2026-09-07T10:05:47Z
You are auditor_m4_it2_1.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m4_it2_1
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m4_it2_1\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the worker handoff at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4_it2\handoff.md
Read the target codebase at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\
Read core project at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core\

Conduct an exhaustive Forensic Integrity Re-Audit on Milestone M4:
- Inspect all files modified in Iteration 2 for cheating, hardcoding, dummy/facade implementations, or stubbed methods.
- Confirm HPRebar.Core continues to have ZERO references to Autodesk.Revit.*.
- Confirm zero deprecated APIs used across all files.
- Verify that BeamRebarView.xaml and tab views use 100% authentic bindings and valid dynamic resource keys.
- Binary verdict: CLEAN or INTEGRITY_VIOLATION.

Write your forensic audit report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m4_it2_1\audit_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m4_it2_1\handoff.md
Notify orchestrator via send_message with your binary verdict (CLEAN or INTEGRITY_VIOLATION).

