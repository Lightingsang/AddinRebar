# Dispatch: Forensic Integrity Auditor (auditor_1)

- **Role**: teamwork_preview_auditor
- **Task**: Perform forensic integrity audit across all Kata Rebar implementations (`HPRebar.Core/KataRebar/`, `HPRebar/KataRebar/`, tests, and configs).
- **Authoritative Request**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under ## 2026-09-27T15:57:37Z)
- **Scope**:
  Perform comprehensive static analysis, code tracing, and integrity checks:
  1. Check for HARDCODED outputs, fake/mock test returns, or dummy implementations.
  2. Check for FACADE patterns (classes that look implemented but do nothing or throw NotImplementedException).
  3. Verify that `HPRebar.Core` has ZERO references to `Autodesk.Revit.*` (inspect `.csproj`, `using` directives, type references).
  4. Verify that `Polyline3.Simplify(1.0)` is genuinely executed on rebar curves.
  5. Verify that `KataDamSheetParser` genuinely reads grid cells and extracts data, and `KataRebarCalculator` genuinely computes 3D coordinates.
  6. Verify that `KataRebarCleanupService` and `KataRebarCreationService` genuinely invoke Revit API calls (`doc.Delete`, `Rebar.CreateFromCurves`, `Rebar.CreateFromRebarShape`).
  7. Verify that `TransactionGroup.Assimilate()` is used for atomic single-undo transactions.
  8. Verify that tests in `HPRebar.Core.Tests` genuinely assert expected values rather than trivial `Assert.True(true)`.

- **Verdict Requirement**:
  Produce a binary integrity verdict: **CLEAN** or **INTEGRITY VIOLATION**.
  Include exhaustive evidence and line citations in `report.md` and deliver `handoff.md`.

## 2026-09-27T16:49:34Z
You are auditor_1 (Forensic Integrity Auditor).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_1
Read your dispatch instructions at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-27T15:57:37Z.
Read the master architectural plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\PROJECT.md

Your Task:
Perform a comprehensive forensic integrity audit across all Kata Rebar code:
1. Static analysis & facade check: Are there any dummy classes, empty stubs, or NotImplementedExceptions?
2. Hardcoding check: Are test outputs or calculation outputs hardcoded?
3. Architecture check: Does `HPRebar.Core` reference any `Autodesk.Revit.*` assembly? (MUST BE ZERO).
4. Revit API check: Are `Rebar.CreateFromCurves` and `Rebar.CreateFromRebarShape` genuine? Is `TransactionGroup.Assimilate()` called?
5. Idempotency check: Are `Comments` stamped and queried correctly for cleanup?
6. Unit tests check: Do tests in `HPRebar.Core.Tests` genuinely assert mathematical outputs and structural rules?
7. Deliver a binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
Write your full evidence report to `report.md` and handoff to `handoff.md`, then send a message.
