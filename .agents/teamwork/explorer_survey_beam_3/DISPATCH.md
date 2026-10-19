# Dispatch: explorer_survey_beam_3

## Identity
- Identity: explorer_survey_beam_3 (teamwork_preview_explorer)
- Working Directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_3\
- Parent: orchestrator_3 (Conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b)

## Authoritative Inputs
- Read `ORIGINAL_REQUEST.md` at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (specifically check section `## 2026-10-07T22:29:51Z`)
- Read Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`

## Mission & Scope: Survey Focus Area 3 (Revit 2026 Rebar API, HelixToolkit.Wpf & ILRepack Build Setup, Excel Export)
Investigate the Revit 2026 add-in build and Revit API rebar creation infrastructure:
1. HelixToolkit.Wpf & Build System:
   - Inspect `HPRebar/HPRebar/HPRebar.csproj` and `HPRebar/build/`.
   - What version of `HelixToolkit.Wpf` is compatible with `net8.0-windows7.0` (Revit 2026 target) as well as multi-version targets (`net48` for R23/R24, `net10.0-windows7.0` for R27)?
   - How does ILRepack operate in `HPRebar.csproj`? Look at `<IsRepackable>`, `<DoNotInternalize>`, etc.
   - Verify that adding `HelixToolkit.Wpf` will not cause assembly loading collisions or missing dependency errors in `Revit.exe`.
2. Revit 2026 Native Rebar Creation (`HPRebar/HPRebar/`):
   - What existing services create Revit rebars? Look at `RebarCreationService`, beam rebar generation logic, or column/footing rebar generation in HPRebar.
   - How does Revit 2026 handle `Rebar.CreateFromCurves` / shape-driven rebar vs freeform rebar?
   - How are `RebarBarType`, `RebarHookType`, and cover constraints resolved and applied in the Revit document?
   - Handling transactions, subtransactions, and preventing `InvalidOperationException`.
3. Calculation Report Export:
   - What Excel generation mechanisms exist in the repo (ClosedXML in Core or Addin)?
   - Layout and format for the structural calculation verification report (`.xlsx`) according to Vietnamese engineering practice.
4. Clean code and architecture boundaries:
   - Confirm layer separation: pure algorithms in `HPRebar.Core`, Revit API calls in `HPRebar`, view/styling in WPF.

## Output
Write your comprehensive investigation report to:
`G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_3\handoff.md`
and send a brief completion message back to orchestrator_3.
