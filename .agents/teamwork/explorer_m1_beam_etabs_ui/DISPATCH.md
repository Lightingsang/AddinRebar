## 2026-10-17T06:51:38Z
You are explorer_m1_3 (ETABS & UI Explorer), working on Milestone M1 (Phase 2: Beam Shop Drawing & ETABS Integration).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_beam_etabs_ui\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-17T06:46:41Z)
2. Read the master project scope at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Inspect existing Phase 1 implementation in HPKata:
   - HPKata/HPKata.UI/ (WPF views, viewmodels, themes, MahApps styling)
   - HPKata/HPKata.Core.Tests/ (Test structure, assertions, test isolation)

YOUR MISSION:
1. ETABS Beam Import Service:
   - Parsing ETABS / SAP2000 frame geometry tables, story levels, beam span coordinates.
   - Parsing design internal forces and reinforcement requirements: As_top, As_bot, shear Av/s.
   - Algorithm to auto-select rebar count and diameters (e.g. 2d18 + 1d20) satisfying required As and minimum bar clearance according to TCVN standards.
2. WPF UI Design (HPKata.UI):
   - BeamDetailingViewModel & BeamDetailingWindow: Continuous beam span table, support/midspan reinforcement inputs, section cuts, stirrup zones, shop drawing options, Dark/Light theme binding.
   - EtabsImportViewModel & EtabsImportWindow: Story/beam selection, rebar auto-sizing rules, preview table of imported beams.
3. Unit Test Plan (HPKata.Core.Tests):
   - Detailed list of unit test scenarios for Beam models, ETABS importer, bar calculators, intersection solvers.
4. Clean code structure:
   - Strict adherence to HP_CLEAN_CODE_CORE.md (<= 300 lines/file, <= 50 lines/method).

DELIVERABLE:
Write your ETABS, UI & Test architecture report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_beam_etabs_ui\beam_etabs_ui_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with the report path and summary.
