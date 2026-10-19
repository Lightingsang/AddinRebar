# DISPATCH: test_writer_e2e (E2E Testing Track Orchestrator / Test Writer)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\test_writer_e2e\`
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`

## Mission
You are the lead Test Writer for the E2E Testing Track.
Derive a comprehensive 4-tier opaque-box test suite based purely on user requirements in `ORIGINAL_REQUEST.md` and the 21 features in `PROJECT.md § Feature Inventory`:
1. Author `TEST_INFRA.md` at project root (`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_INFRA.md`) following the template in Project Pattern:
   - Test Philosophy: Opaque-box, requirement-driven, zero dependency on implementation design.
   - 4 Tiers: Tier 1 (Feature coverage >= 5 per feature), Tier 2 (Boundary & Corner cases >= 5 per feature), Tier 3 (Cross-feature interactions), Tier 4 (Real-world application scenarios).
2. Inspect existing tests in `HPKata/HPKata.E2E.Tests/` (currently 160 tests).
3. Design and add new test classes in `HPKata.E2E.Tests` covering:
   - Standalone `C:\HPKata\` environment & zero `kata_pro` validation
   - Bi-directional Excel round-tripping (`Dam`, `Cot`, `Vach`, `Iso_QHPlus.xls`) with mock/fallback
   - 86-parameter config round-tripping on `Goi lenh.lsp`
   - AutoCAD template importing and block definitions
   - Headless CLI installer (`HPKata_Setup.exe --install --target-dir <path>`)
   - WPF window instantiation on STA thread for all 13 windows under Dark and Light themes
4. Verify tests pass with `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`.
5. When complete, publish `TEST_READY.md` at project root with the full coverage summary and checklist.
6. Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\test_writer_e2e\handoff.md`.
