# DISPATCH: worker_m2_iter2 (Milestone M2 Remediation Worker)

**Assigned Agent**: worker_m2_iter2 (`teamwork_preview_worker`)  
**Parent**: orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_iter2\`  
**Assigned Task**: Milestone M2 Remediation of DEFECT-M2-01 in CuttingStockStrategies (DuBang0 Recursion Timeout & Pruning)  
**Timestamp**: 2026-10-17T01:38:00Z  

---

## Mandatory Reading List (Read in Order)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2\handoff.md` (exact defect details and root cause).
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2\challenge_report.md`.
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Section 7.2).
5. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.

---

## Defect Summary & Root Cause (`DEFECT-M2-01`)
In `HPKata/HPKata.Core/Optimization/CuttingStockStrategies.cs`:
- On combinatorial demand distributions (e.g. 150 items of 2000 mm where no subset equals 11,700 mm), `DuBang0` took **4,007 ms** (and **8,865 ms** on 180 items), violating the strict 1,000 ms time budget mandated by Section 7.2 of the Blueprint.
- **Root cause**: The timeout condition `sw.ElapsedMilliseconds < 1000` is checked only in the outer `while` loop. The inner recursive method `SearchCombinations(...)` traverses up to $\binom{150}{5} = 591,600,030$ combinations without passing the stopwatch or checking any timeout within recursion, blocking the thread for seconds without interruption.

---

## Remediation Tasks

1. **Fix `CuttingStockStrategies.cs` (or create partial class `CuttingStockStrategies.DuBang0.cs`)**:
   - Pass `Stopwatch sw` (or `long deadlineMs`) into `FindExactSubset` and `SearchCombinations`.
   - At the beginning of `SearchCombinations` (and on every recursive iteration), check:
     ```csharp
     if (sw.ElapsedMilliseconds >= 1000) return false;
     ```
   - Add upper-bound pruning in `SearchCombinations`:
     If items are sorted descending (`list[startIdx]` is the largest remaining available item), calculate:
     ```csharp
     int remainingCount = k - depth;
     if (currentSum + (remainingCount * list[startIdx]) < target - 1.0)
     {
         break; // Cannot reach target even if we pick the largest remaining items
     }
     ```
   - Add lower-bound pruning:
     ```csharp
     if (currentSum + (remainingCount * list[list.Count - 1]) > target + 1.0)
     {
         break; // Exceeds target even if we pick the smallest remaining items
     }
     ```
   - Ensure `DuBang0` falls back cleanly to BFD upon timeout expiry without exceeding 1,000 ms.

2. **Strict Clean Code Governance**:
   - **Rule C2 ($\le 300$ lines per file)**: `CuttingStockStrategies.cs` is currently at 296–297 lines! Adding code will exceed 300 lines!
     You MUST factor out `DuBang0` into a partial class `CuttingStockStrategies.DuBang0.cs` (or factor out helper methods) so that EVERY C# file stays well under 250 lines!
   - **Rule M3 ($\le 50$ lines per method)**: Every method body must stay $\le 50$ lines.
   - **Rule Q-B1**: Zero dead or commented-out code.
   - **Rule Q-B2**: Zero unannotated catch blocks.
   - **Host-Neutrality**: Zero references to `Autodesk.*`.

3. **Verify Build & Run Empirical Test Suite**:
   - Re-publish netstandard2.0:
     ```powershell
     dotnet publish "HPKata/HPKata.Core/HPKata.Core.csproj" -f netstandard2.0 -c Release -o "HPKata/HPKata.Core/bin/publish_netstandard"
     ```
   - Re-build solution:
     ```powershell
     dotnet build "HPKata/HPKata.slnx" -c Release
     ```
   - Run the empirical test harness authored by challenger_m2_2:
     ```powershell
     powershell -ExecutionPolicy Bypass -File "HPKata/tools/run_m2_empirical_tests.ps1"
     ```
   - Verify that **Scenario 4 PASSES** with `Time <= 1000ms`, and all 11 tests report `[PASS]`!

---

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

---

## Output Requirements
1. Deliver `handoff.md` in your working directory (`.agents/teamwork/worker_m2_iter2/handoff.md`).
2. Send a message to orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`) when complete.

## 2026-10-17T01:39:15Z
[Message] sender=4300a9ce-e3eb-419c-8493-f2001d2d04d6
You are worker_m2_iter2, the Remediation Worker for Milestone M2 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_iter2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_iter2\DISPATCH.md.
3. Read the challenger defect report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2\handoff.md.

YOUR TASK:
Remediate DEFECT-M2-01 in HPKata.Core:
1. Fix DuBang0 combinatorial explosion in HPKata/HPKata.Core/Optimization/CuttingStockStrategies.cs:
   - Pass Stopwatch / timeout deadline into FindExactSubset and SearchCombinations.
   - Add in-recursion timeout check: if (sw.ElapsedMilliseconds >= 1000) return false;
   - Add upper-bound and lower-bound mathematical pruning:
     - Upper-bound: if currentSum + (k - depth) * list[startIdx] < target - 1.0, break.
     - Lower-bound: if currentSum + (k - depth) * list[^1] > target + 1.0, break.
   - Clean cascade to BFD when timeout is reached.
2. Comply strictly with Clean Code Rule C2 (<= 300 lines per file):
   CuttingStockStrategies.cs is currently 296–297 lines. Factor out DuBang0 or helper methods into a partial class `CuttingStockStrategies.DuBang0.cs` so all files are comfortably under 250 lines!
3. Comply with Rule M3 (method body <= 50 lines), Rule Q-B1 (0 dead code), Rule Q-B2 (0 unannotated catch), and pure host-neutrality (0 Autodesk references).
4. Run:
   - dotnet publish HPKata/HPKata.Core/HPKata.Core.csproj -f netstandard2.0 -c Release -o HPKata/HPKata.Core/bin/publish_netstandard
   - dotnet build HPKata/HPKata.slnx -c Release
   - powershell -ExecutionPolicy Bypass -File "HPKata/tools/run_m2_empirical_tests.ps1"
   Verify that Scenario 4 PASSES (time <= 1000ms) and all 11 tests pass with code 0!
5. Deliver handoff.md in your working directory.
