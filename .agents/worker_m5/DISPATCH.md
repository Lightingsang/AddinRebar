# DISPATCH — worker_m5

## Mission
You are the Implementation Worker for Milestone M5 (Ribbon Integration & Multi-Version Verification).

## Mandatory Reading
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Repository Rules: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\AGENTS.md`
4. Codebase under verification:
   - `HPRebar/HPRebar/Application.cs`
   - `HPRebar/HPRebar/Beam Rebar/BeamRebarCommand.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/BeamRebarView.xaml`
   - `HPRebar/HPRebar/Beam Rebar/View Models/BeamRebarViewModel.cs`
   - `HPRebar/HPRebar.Core/BeamRebar/`
   - `HPRebar/HPRebar.Core.Tests/BeamRebar/`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Objectives
1. **Ribbon Integration Verification**:
   - Inspect `HPRebar/HPRebar/Application.cs` to ensure the "Beam Rebar" push button is correctly registered on the "Rebar" ribbon panel under the "HPRebar" tab.
   - Verify that button icons (`RibbonIcon16.png` and `RibbonIcon32.png`) are properly linked.
   - Verify that `BeamRebarCommand` derives from `Nice3point.Revit.Toolkit.External.ExternalCommand` with `[Transaction(TransactionMode.Manual)]`.
2. **Build Verification**:
   - Verify compilation under Revit 2026 configuration:
     `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`
   - Verify compilation under Revit 2025 configuration:
     `dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false`
   - Ensure 0 errors and zero warnings treated as errors.
3. **Core Unit Tests Verification**:
   - Run the pure domain unit tests:
     `dotnet test HPRebar/HPRebar.Core.Tests`
   - Verify that all 102 unit tests in `HPRebar.Core.Tests` pass with 100% success rate.
4. **Architectural & Quality Compliance**:
   - Confirm file-scoped namespaces throughout all files in `HPRebar/HPRebar/Beam Rebar/`.
   - Confirm zero references to `Autodesk.Revit.*` in `HPRebar.Core`.
   - Confirm 100% `{DynamicResource}` token usage in XAML.
   - Confirm `TransactionGroup("Beam Rebar")` assimilation/rollback safety in `BeamRebarOrchestrator.cs`.

## Output
Write your comprehensive handoff report to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m5\handoff.md`
Notify orchestrator via `send_message` when done.
