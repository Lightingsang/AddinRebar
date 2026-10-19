# Task Assignment: Challenger 2 (Steel Drafting, CAD Commands & Control Center Stress Testing)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_2\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Specification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Objectives
Empirically challenge and stress-test the implementation:
1. **Stress-test `SteelFrameDraftingGenerator`**:
   - Test all 9 drafting routines with boundary and extreme configurations:
     - Span = 0, Height = 0, Eave Height > Ridge Height (inverted slope).
     - Extreme slope angles ($0^\circ, 89^\circ$).
     - Thousands of purlins (very small spacing).
     - Layer names containing special characters and unicode.
   - Verify buffer outputs (`CadEntityPrimitive` collection) have valid coordinates, non-NaN/non-Infinity values, and non-empty bounding boxes.
2. **Stress-test `SteelCadCommands` & `AutoStrCommands`**:
   - Verify that all 10 steel CAD commands and `GETFORM` handle null `MdiActiveDocument` without crashing or throwing `InvalidProgramException` or `NullReferenceException`.
   - Verify that LISP command aliases in `HPDcePro.lsp` match the exact names of the 10 CAD commands.
3. **Stress-test `MainControlCenterViewModel`**:
   - Test rapid repeated navigation command execution.
   - Test corrupted or empty `.autostr` project files passed to `LoadFromFile`.
   - Test `ApplyConfig` with empty floor lists and extreme story counts ($N=0, N=500$).
   - Verify that properties fire `PropertyChanged` events accurately and no memory leaks occur.
4. **Execution & Evidence**:
   - Run tests and empirical verifications.
   - Run `dotnet test HPDcePro/HPDcePro.slnx -c Debug`.

Deliver verdict: **APPROVE** (no vulnerabilities/bugs found) or **REQUEST_CHANGES** (with reproducible failure case) in `handoff.md`.
Notify orchestrator via send_message.

## 2026-10-18T01:18:06Z
You are Challenger 2 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_2\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_2\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

Empirically challenge and stress-test SteelFrameDraftingGenerator, SteelCadCommands, and MainControlCenterViewModel:
- Extreme frame spans, heights, inverted roof slopes, dense purlins.
- Headless execution of all 10 CAD commands and GETFORM with null active document.
- Corrupted .autostr project deserialization and extreme story counts.
- Verify tests pass: `dotnet test HPDcePro/HPDcePro.slnx -c Debug`.

Deliver verdict: APPROVE (no vulnerabilities) or REQUEST_CHANGES (with repro) in handoff.md and notify orchestrator via send_message.
