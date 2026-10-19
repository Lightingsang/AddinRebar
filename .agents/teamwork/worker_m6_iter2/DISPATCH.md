# WORKER DISPATCH SPECIFICATION — Milestone M6 Remediation (worker_m6_iter2)

## Mission
Remediate the integrity violation and architectural hygiene defects identified during Gate M6 Iteration 10 Forensic Integrity Audit in `HPKata.Revit` and `HPKata.Revit.Tests`.

## Authoritative Inputs
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read this first!)
- **Full Forensic Audit Report**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\audit_report.md`
- **Auditor Handoff Report**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\handoff.md`
- **Previous Worker Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6\handoff.md`
- **Clean Code Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md`
- **AST Verification Tool**: `scratch/verify_clean_code_ast.py`

---

## Forensic Audit Findings to Remediate

### 1. DEFECT-M6-01 (Rule M3 Violation in `RevitParameterBridge.cs`)
- **Location**: `HPKata/HPKata.Revit/Bridge/RevitParameterBridge.cs:62-114` (`ExtractFromRevitRebar`)
- **Finding**: Method body spans 53 lines with 52 physical body lines, strictly violating Rule M3 ($\le 50$ physical lines).
- **AST Failure**: `python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40` exited with code 1:
  `[FAIL M3 > 50] Bridge/RevitParameterBridge.cs:62-114 -> ExtractFromRevitRebar() | Total: 53 lines, Body: 52 lines`
- **Remediation**:
  In `RevitParameterBridge.cs`, decompose `ExtractFromRevitRebar` by extracting shape, dimension, and curve extraction into private helper method(s):
  ```csharp
  private static (string shapeName, double[] parametersMm) ResolveShapeAndParameters(
      Rebar rebar,
      Document doc,
      bool isFreeform)
  ```
  This must reduce `ExtractFromRevitRebar` method body to $\le 30$ physical lines (well below the 40-line warning threshold and $\le 50$ limit).

### 2. Architectural Mock Leakage
- **Location**: `HPKata/HPKata.Revit/Mocks/MockRevitDocumentContext.cs`
- **Finding**: Test mock context is physically compiled into the production `HPKata.Revit` assembly.
- **Remediation**:
  Move `MockRevitDocumentContext.cs` to `HPKata/HPKata.Revit.Tests/Mocks/MockRevitDocumentContext.cs` (and delete from `HPKata.Revit/Mocks/`).
  Update `HPKata.Revit.Tests` references and namespaces so test suites compile cleanly while the production library contains zero mock doubles.

### 3. Accurate Attestation Discipline
- Ensure handoff report records exact line count and method body length measured by AST script.
- Zero inaccurate claims regarding method length limits.

---

## Mandatory Resource Directive
- Every `dotnet build` or `dotnet test` must include `--disable-build-servers -m:1`.
- For test runs, you MUST use `--results-directory "$env:TEMP\mtp-worker6iter2-$([guid]::NewGuid())"` to prevent test log file collisions.
- Do NOT spawn full builds or background build servers in parallel.

---

## Clean Code Governance (Non-Negotiable)
- Rule C2: all files $\le 300$ physical lines.
- Rule M3: all method bodies $\le 50$ physical lines (target $< 35$).
- Rule Q-B1: 0 dead or commented-out code.
- Rule Q-B2: 0 unannotated catches.
- Rule R5: 0 unit conversion factors outside `RevitUnitConverter.cs`.
- Clean-Room: `C:\kata_pro` is strictly read-only (0 writes, 0 touches to `Security.dll`).

---

## Mandatory Verification Sequence
1. Run AST verification:
   ```powershell
   python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40
   ```
   *Required*: Exit code 0, 0 strict violations.
2. Build solution:
   ```powershell
   dotnet build HPKata/HPKata.slnx -c Release --disable-build-servers -m:1
   ```
   *Required*: 0 errors, 0 warnings.
3. Run tests:
   ```powershell
   dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-worker6iter2-$([guid]::NewGuid())"
   ```
   *Required*: All 287 tests pass 100% green.
4. Deliver `handoff.md` in `.agents/teamwork/worker_m6_iter2/handoff.md`.


## 2026-10-17T05:15:49Z
[Message] timestamp=2026-10-17T05:15:49Z sender=4300a9ce-e3eb-419c-8493-f2001d2d04d6 priority=MESSAGE_PRIORITY_HIGH content=You are worker_m6_iter2, the Remediation Worker for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\DISPATCH.md.
3. Read the full forensic audit report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\audit_report.md and handoff at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\handoff.md.
4. Read docs/clean-code/HP_CLEAN_CODE_CORE.md and docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md.

RESOURCE DIRECTIVE (MANDATORY):
- Every `dotnet build` or `dotnet test` must include `--disable-build-servers -m:1`.
- For test runs, you MUST use `--results-directory "$env:TEMP\mtp-worker6iter2-$([guid]::NewGuid())"` to prevent test results locking collisions.
- Do NOT spawn background build servers or run parallel compilation storms.

YOUR TASK:
Remediate the defects identified in Gate M6 Iteration 10 Forensic Audit:
1. DEFECT-M6-01 (Rule M3 Breach in `RevitParameterBridge.cs`):
   - In `HPKata/HPKata.Revit/Bridge/RevitParameterBridge.cs`, refactor lines 62–114 (`ExtractFromRevitRebar`).
   - Extract shape, dimension parameter, and curve extraction logic into a private helper method (e.g., `ResolveShapeAndParameters(Rebar rebar, Document doc, bool isFreeform, out string shapeName, out double[] parametersMm)`).
   - Ensure the body of `ExtractFromRevitRebar` is <= 30 physical lines (well below the 40-line warning and strictly <= 50 line limit).
2. Mock Relocation (Hygiene & Assembly Separation):
   - Move `HPKata/HPKata.Revit/Mocks/MockRevitDocumentContext.cs` into `HPKata/HPKata.Revit.Tests/Mocks/MockRevitDocumentContext.cs`.
   - Remove `MockRevitDocumentContext.cs` from production project `HPKata.Revit`.
   - Update namespaces/imports in `HPKata.Revit.Tests` as necessary so tests compile cleanly.
   - Ensure `HPKata.Revit` contains 0 test mock doubles.
3. Verify Clean Code AST Compliance:
   - Run: `python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40`.
   - Ensure exit code is 0 with 0 strict Rule M3 violations (> 50 lines).
4. Verify Build and Tests:
   - Run: `dotnet build HPKata/HPKata.slnx -c Release --disable-build-servers -m:1`.
   - Run: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-worker6iter2-$([guid]::NewGuid())"`.
   - Ensure all 287 tests pass 100% green with 0 errors and 0 warnings.
5. Deliver handoff.md in your working directory (.agents/teamwork/worker_m6_iter2/handoff.md).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.
