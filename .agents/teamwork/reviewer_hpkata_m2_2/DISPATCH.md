# Dispatch Specification — M2 Adversarial Reviewer (reviewer_hpkata_m2_2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Test Certification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md`

## Mission:
Adversarially review UI integration (Feature 6) and Host Detailing Commands (Features 7 & 8):
1. Review `HPKata.UI` ViewModels (`*.Excel.cs` partials, `BeamDetailingViewModel`, `ColumnDetailingViewModel`, `WallDetailingViewModel`) and Views (`BeamDetailingView.xaml`, `ColumnDetailingView.xaml`, `WallDetailingView.xaml`).
2. Verify strict UI constraints: must retain `System.Windows.Window` + `MaterialDesignThemes` v5.2.1, zero `MahApps.Metro`, all `Theme.xaml` `StaticResource` keys intact. Verify that parameterless constructors and optional DI ensure STA window instantiation passes without throwing.
3. Review AutoCAD detailing commands (`loadbeam`, `loadcolumn`, `loadwall`, `ve_dam`, `ve_cot`, `ve_vach`, `toexcel`, `toexcelqh`), canonical layer styling via `KataLayerHelper`, and `KATA` XData tagging via `KataEntityStyler`.
4. Review Revit detailing commands (`FramingRebarCommands`, `ColumnRebarCommands`, `WallRebarCommands`) and schedule export routing.
5. Run `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`.
6. Formulate your clear verdict: **APPROVE** or **REQUEST_CHANGES**.
7. Write your report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_2\handoff.md`.


## 2026-10-18T04:25:09Z
You are reviewer_hpkata_m2_2 (M2 Adversarial Reviewer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_2\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_2\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 5–8).
Test Certification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
