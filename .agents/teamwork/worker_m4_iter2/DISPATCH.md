# DISPATCH: worker_m4_iter2 (Milestone M4 Remediation Worker)

**Assigned Agent**: worker_m4_iter2 (`teamwork_preview_worker`)  
**Parent**: orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_iter2\`  
**Assigned Task**: Remediate Gate M4 Defects (DEFECT-M4-01, DEFECT-M4-02, DEFECT-M4-03) in HPKata.UI & HPKata.Core  
**Timestamp**: 2026-10-17T03:06:00Z  

---

## Mandatory Reading List (Read in Order)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`
5. Forensic Auditor defect report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_1\handoff.md`
6. Challenger defect report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_1\handoff.md`

---

## RESOURCE DIRECTIVE (MANDATORY)
- Every `dotnet build` or `dotnet test` must include `--disable-build-servers -m:1`.
- DO NOT run background build servers or parallel compilation storms.
- Keep `<UseSharedCompilation>false</UseSharedCompilation>` and `<BuildInParallel>false</BuildInParallel>` in `HPKata/Directory.Build.props`.

---

## Detailed Remediation Tasks

### 1. DEFECT-M4-01 (Auditor Binary Veto: Rule M3 Method Length Breach)
**Target File**: `HPKata/HPKata.UI/ViewModels/SetupViewModel.RebarDetails.cs`
- In `LoadFromConfig(KataConfig config)` (lines 74–139, 66 physical lines), decompose into:
  - `private void LoadRebarRuleSettings(KataConfig config)` (~30 lines).
  - `private void LoadCuttingStockSettings(KataConfig config)` (~30 lines).
  Call both helpers from `LoadFromConfig(config)`.
- In `ToConfig()` (lines 141–209, 69 physical lines), decompose into:
  - `private void ApplyRebarRuleSettings(KataConfig config)` (~30 lines).
  - `private void ApplyCuttingStockSettings(KataConfig config)` (~30 lines).
  Call both helpers from `ToConfig()`.
- Ensure EVERY method in `SetupViewModel.RebarDetails.cs` has body $\le 40$ physical lines (Rule M3 strictly $\le 50$).
- Ensure total lines in `SetupViewModel.RebarDetails.cs` $\le 250$ lines (Rule C2 strictly $\le 300$).

### 2. DEFECT-M4-02 (Challenger Finding: SetupViewModel.IsDirty Reactivity)
**Target File**: `HPKata/HPKata.UI/ViewModels/SetupViewModel.cs`
- Currently `IsDirty` only updates on `ResetToDefaults()`, but does NOT react when any of the 86 config parameters are changed in UI or code.
- Add an `_isInitializing` flag (set `true` during construction, `LoadFromConfig`, `LoadFromFile`, and `false` after).
- Override `OnPropertyChanged(PropertyChangedEventArgs e)`:
  ```csharp
  protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
  {
      base.OnPropertyChanged(e);
      if (!_isInitializing && e.PropertyName != nameof(IsDirty) && e.PropertyName != nameof(StatusMessage))
      {
          IsDirty = true;
      }
  }
  ```
- Ensure `SaveToFile()` and `ApplyAndClose()` reset `IsDirty = false`.

### 3. DEFECT-M4-03 (Challenger Finding: PatternCat Kerf Over-Length Pattern)
**Target File**: `HPKata/HPKata.Core/Optimization/CuttingStockStrategies.cs`
- In `ExtractModularDividers(List<double> list, List<CuttingPattern> patterns, double stockLength, double kerf, double pieceLen, int count)`:
  Check if `count * pieceLen + (count > 1 ? (count - 1) * kerf : 0.0) <= stockLength + 1e-4`.
  If total length required exceeds `stockLength` (e.g. $2 \times 5850 + 5 = 11705 > 11700$), immediately `return;` without extracting invalid patterns.
- Ensure file length $\le 250$ lines (Rule C2) and method length $\le 45$ lines (Rule M3).

---

## Clean Code Governance & Verification
1. Verify Rule C2: all modified files $\le 300$ physical lines.
2. Verify Rule M3: all methods in modified files $\le 50$ physical lines.
3. Verify Rule Q-B1: 0 dead code or commented-out code.
4. Verify Rule Q-B2: 0 unannotated catch blocks.
5. Verify Host Neutrality (Rules D1, D4): ZERO references to `Autodesk.*`.
6. Run full verification:
   `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1`
   Must pass with 0 warnings and 0 errors across all 190+ tests.
7. Add a unit test in `HPKata.UI.Tests` verifying `SetupViewModel.IsDirty` becomes `true` when a property (e.g. `ConcreteCoverMm`) is modified.

---

## Deliverables
- Deliver `handoff.md` in your working directory with verified method spans and test results.
- Send a message to orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`) upon completion.

## 2026-10-17T03:05:31Z
[Message] timestamp=2026-10-17T03:05:31Z sender=4300a9ce-e3eb-419c-8493-f2001d2d04d6 priority=MESSAGE_PRIORITY_HIGH content=You are worker_m4_iter2, the Remediation Worker for Milestone M4 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_iter2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_iter2\DISPATCH.md.
3. Read the forensic auditor defect report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_1\handoff.md.
4. Read the challenger defect report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_1\handoff.md.

RESOURCE DIRECTIVE (MANDATORY):
- Every `dotnet build` or `dotnet test` must include `--disable-build-servers -m:1`.
- Do NOT spawn background build servers or run parallel compilation storms.

YOUR TASK:
Remediate the 3 defects identified in Gate M4 Iteration 1:
1. DEFECT-M4-01 (Auditor Binary Veto - Rule M3 Method Length Breach):
   In HPKata/HPKata.UI/ViewModels/SetupViewModel.RebarDetails.cs:
   - Decompose `LoadFromConfig(KataConfig config)` (lines 74–139, 66 lines) into `LoadRebarRuleSettings(config)` and `LoadCuttingStockSettings(config)`.
   - Decompose `ToConfig()` (lines 141–209, 69 lines) into `ApplyRebarRuleSettings(config)` and `ApplyCuttingStockSettings(config)`.
   - Ensure EVERY method in SetupViewModel.RebarDetails.cs has body <= 40 lines (Rule M3 strictly <= 50).
2. DEFECT-M4-02 (Challenger - SetupViewModel.IsDirty Reactivity):
   In HPKata/HPKata.UI/ViewModels/SetupViewModel.cs:
   - Implement `_isInitializing` guard flag.
   - Override `OnPropertyChanged` to automatically set `IsDirty = true` when any configuration parameter changes (except IsDirty and StatusMessage).
   - Ensure `SaveToFile()` and `ApplyAndClose()` reset `IsDirty = false`.
   - Add a test in HPKata.UI.Tests verifying `IsDirty` becomes true when a property is mutated.
3. DEFECT-M4-03 (Challenger - PatternCat Kerf Over-Length Pattern):
   In HPKata/HPKata.Core/Optimization/CuttingStockStrategies.cs:
   - In `ExtractModularDividers`, guard against kerf over-length: if `count * pieceLen + (count > 1 ? (count - 1) * kerf : 0.0) > stockLength + 1e-4`, immediately return.
4. Verification:
   - Run `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1`.
   - Verify 0 warnings and 0 errors across all tests.
   - Verify Clean Code compliance: Rule C2 (all files <= 300 lines), Rule M3 (all methods <= 50 lines), Rule Q-B1 (0 dead code), Rule Q-B2 (0 unannotated catch), Host Neutrality (0 Autodesk references).
5. Deliver handoff.md in your working directory with an honest report of AST method spans and test results.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.
