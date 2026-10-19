# Dispatch Specification — Project Orchestrator (orchestrator_24, Gen 2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_24\`
- Predecessor Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_23\`
- Parent Conversation ID: `863591cc-183d-404f-9810-d0f191f6ebe6`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md`
- E2E Test Certification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`

## Mission
Take over from `orchestrator_23` and continue driving the standalone transformation of `HPKata`.
Milestone M1 is DONE (certified & audited).
Milestone M2 Exploration is COMPLETE: 3 comprehensive reports are available in:
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_1\handoff.md` (Core Excel Engine, 19 modular files <= 250 lines in HPKata.Core/Excel/)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\handoff.md` (UI ViewModels & Views, MaterialDesign toolbar, Theme.xaml)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_3\handoff.md` (AutoCAD & Revit detailing commands sync, canonical layers, KATA XData, 3D Rebar)

## Concrete Immediate Task
1. Initialize your state in `orchestrator_24`: create `BRIEFING.md`, `progress.md`, and start heartbeat cron.
2. Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_23\handoff.md`.
3. Dispatch `worker_hpkata_m2` (`teamwork_preview_worker`) to implement Milestone M2 (Features 5–8) with single-node build verification (`dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`) and test execution (`dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`).
4. Dispatch M2 Gate Verification: 2 Reviewers, 2 Challengers, Forensic Auditor.
5. Record gate verdict in `GATE_STATUS.md`. If passed cleanly, mark M2 DONE and advance to Milestone M3.
