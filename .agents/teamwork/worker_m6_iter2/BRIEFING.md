# BRIEFING — 2026-10-17T05:22:00Z

## Mission
Remediate Gate M6 Iteration 10 Forensic Audit defects in HPKata.Revit (Rule M3 in RevitParameterBridge.cs) and HPKata.Revit.Tests (MockRevitDocumentContext relocation) with 100% green tests and zero AST violations.

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\
- Original parent: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Milestone: M6 Remediation (Gate M6 Iteration 10)

## 🔒 Key Constraints
- Every `dotnet build` or `dotnet test` must include `--disable-build-servers -m:1`.
- For test runs, MUST use `--results-directory "$env:TEMP\mtp-worker6iter2-$([guid]::NewGuid())"` to prevent test results locking collisions.
- Do NOT spawn background build servers or run parallel compilation storms.
- Rule M3: all method bodies <= 50 physical lines (target <= 30 for refactored methods).
- Rule C2: all files <= 300 physical lines.
- Rule R5: 0 unit conversion factors (304.8) outside `RevitUnitConverter.cs`.
- Rule Q-B1: 0 dead or commented-out code.
- Rule Q-B2: 0 unannotated catches.
- Clean-Room: `C:\kata_pro` is strictly read-only (0 writes, 0 touches to `Security.dll`).
- Integrity Mandate: Genuine implementation, 0 hardcoded test results, 0 false attestations.

## Current Parent
- Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Updated: 2026-10-17T05:22:00Z

## Task Summary
- **What to build**:
  1. Refactor `ExtractFromRevitRebar` in `HPKata/HPKata.Revit/Bridge/RevitParameterBridge.cs` to extract shape and parameter extraction logic into helper `ResolveShapeAndParameters`, bringing body length to <= 30 physical lines (measured: 29 lines body).
  2. Relocate `HPKata/HPKata.Revit/Mocks/MockRevitDocumentContext.cs` into `HPKata/HPKata.Revit.Tests/Mocks/MockRevitDocumentContext.cs`, leaving 0 mocks in production assembly `HPKata.Revit`.
  3. Verify AST compliance with `python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40` (exit code 0, 0 strict violations).
  4. Verify full solution build and test execution (287/287 passing).
- **Success criteria**:
  - `verify_clean_code_ast.py` passes with exit 0 and 0 strict violations (>50 lines).
  - Production `HPKata.Revit` contains 0 test mock doubles (scanned 25 files).
  - Full build: 0 errors, 0 warnings.
  - Tests: 287/287 passed (100% green).
- **Interface contracts**: `HPKata/HPKata.slnx`, `HPKata.Core`, `HPKata.Revit`, `HPKata.Revit.Tests`
- **Code layout**: `HPKata/HPKata.Revit/`, `HPKata/HPKata.Revit.Tests/`

## Key Decisions Made
- Decomposed `ExtractFromRevitRebar` into private helper `ResolveShapeAndParameters(Rebar rebar, Document doc, bool isFreeform)` which reduced `ExtractFromRevitRebar` body to exactly 29 lines (span 30 lines), well below 30 lines.
- Moved `MockRevitDocumentContext.cs` from `HPKata.Revit` to `HPKata.Revit.Tests/Mocks/` under namespace `HPKata.Revit.Tests.Mocks`. Removed the production mock file and directory completely.
- Updated namespace imports in `RevitCuttingStockRunnerTests.cs`, `RevitExportExcelRunnerTests.cs`, and `RevitScheduleRunnerTests.cs`.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\DISPATCH.md` — assignment spec
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\progress.md` — progress heartbeat
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\handoff.md` — final handoff report

## Change Tracker
- **Files modified**:
  - `HPKata/HPKata.Revit/Bridge/RevitParameterBridge.cs`: refactored `ExtractFromRevitRebar` and added `ResolveShapeAndParameters` (122 total lines).
  - `HPKata/HPKata.Revit/Mocks/MockRevitDocumentContext.cs`: deleted from production project.
  - `HPKata/HPKata.Revit.Tests/Mocks/MockRevitDocumentContext.cs`: created in test project under namespace `HPKata.Revit.Tests.Mocks`.
  - `HPKata/HPKata.Revit.Tests/Runners/RevitCuttingStockRunnerTests.cs`: updated import to `using HPKata.Revit.Tests.Mocks;`.
  - `HPKata/HPKata.Revit.Tests/Runners/RevitExportExcelRunnerTests.cs`: updated import to `using HPKata.Revit.Tests.Mocks;`.
  - `HPKata/HPKata.Revit.Tests/Runners/RevitScheduleRunnerTests.cs`: updated import to `using HPKata.Revit.Tests.Mocks;`.
- **Build status**: PASS (0 warnings, 0 errors)
- **Pending issues**: None

## Quality Status
- **Build/test result**: PASS — 287/287 tests passed (100% green, 0 failed, 0 skipped)
- **Lint status**: PASS — 0 violations for Rule C2, 0 strict violations for Rule M3 (>50 lines), AST script exit code 0
- **Tests added/modified**: Updated 3 test files to use relocated mock context

## Loaded Skills
- None
