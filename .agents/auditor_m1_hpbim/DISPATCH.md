## 2026-10-06T00:00:05Z
You are teamwork_preview_auditor assigned to Milestone 1 (M1: Solution Setup & Pure Domain Library HPBim.Core).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m1_hpbim

Read the authoritative request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z)

Read the project specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md

Read Worker handoff at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_hpbim\handoff.md

Your mission: Forensic integrity verification.
You must conduct deep static and forensic checks to verify:
1. CHEATING & FACADE CHECK:
   - Ensure all geometry math (Shoelace, Liang-Barsky, distances), TCVN rebar formulas, QC clash checks, and models are authentic, fully implemented algorithms.
   - Ensure there are NO hardcoded test outputs, dummy implementations, or fake mocks.
2. REPO GOVERNANCE RULES:
   - Ensure `HPBim/src/HPBim.Core/` has ZERO references to `Autodesk.Revit.*` (pure domain library).
   - Ensure `HPBim/` has ZERO references to other host MCP folders (`HPAutoCad`, `HPNavis`, `HPEtabs`, etc.). It must ONLY reference `McpShared/`.
3. CLEAN CODE CORE RULES (docs/clean-code/HP_CLEAN_CODE_CORE.md):
   - Q-B1: Zero commented-out code.
   - Q-B2: Zero empty catch blocks without explanatory reason comment.
   - Q-B3: All classes <= 300 lines (methods <= 50 lines).
4. BUILD INTEGRITY:
   - Run `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release` and verify 0 errors, 0 warnings.

Provide a binary verdict:
- CLEAN (no violations found)
- INTEGRITY VIOLATION (with detailed evidence chain)

Write your full audit report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m1_hpbim\handoff.md`, update `progress.md`, and send message to parent.
