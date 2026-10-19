# DISPATCH: challenger_m4_1_iter2 (Milestone M4 Empirical Re-Challenger)

**Assigned Agent**: challenger_m4_1_iter2 (`teamwork_preview_challenger`)  
**Parent**: orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_1_iter2\`  
**Assigned Task**: Gate M4 Iteration 2 Empirical Re-Challenge of Defects DEFECT-M4-02 & DEFECT-M4-03  
**Timestamp**: 2026-10-17T03:15:00Z  

---

## Mandatory Reading List (Read in Order)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
4. Previous Challenge Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_1\handoff.md`
5. Worker Remediation Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_iter2\handoff.md`

---

## RESOURCE DIRECTIVE (MANDATORY)
- Worker has built the solution. If executing tests, you MUST use `dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1`.
- Do NOT spawn background build servers or run parallel compilation storms.

---

## Empirical Challenge Scope
1. **DEFECT-M4-02 Empirical Re-Challenge (SetupViewModel.IsDirty Reactivity)**:
   - Run the dedicated unit tests:
     `dotnet test HPKata/HPKata.UI.Tests/HPKata.UI.Tests.csproj --filter "FullyQualifiedName~SetupViewModelTests" --no-build --disable-build-servers -m:1`
   - Empirically verify that mutating configuration properties (e.g. `ConcreteCoverMm`, `BaseBarDiameter`) sets `IsDirty = true`.
   - Empirically verify that constructor initialization and `StatusMessage` updates do NOT leave `IsDirty = true`.
   - Empirically verify that `SaveToFile` and `ApplyAndClose` clear `IsDirty = false`.
2. **DEFECT-M4-03 Empirical Re-Challenge (PatternCat Kerf Over-Length Guard)**:
   - Run the dedicated unit tests:
     `dotnet test HPKata/HPKata.UI.Tests/HPKata.UI.Tests.csproj --filter "FullyQualifiedName~PatternCat_WithSawKerf" --no-build --disable-build-servers -m:1`
     `dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj --filter "FullyQualifiedName~Solve_PatternCat_WithKerf" --no-build --disable-build-servers -m:1`
   - Empirically stress-test `PatternCat` under various non-zero kerf values (e.g., 3mm, 5mm, 10mm) on modular 5850mm and pair 6000mm/5700mm cuts to verify zero patterns produce `UsedLengthMm > StockLengthMm`.
3. **Full Solution Test Suite Execution**:
   - Run `dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1` (verify all 194 tests pass 100% green).

---

## Deliverables
- Write `challenge_report.md` with empirical test evidence.
- Deliver `handoff.md` with explicit verdict: **`APPROVE`** or **`REQUEST_CHANGES`**.
- Send coordination message to orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`).
