# DISPATCH — Milestone M5 Remediation (HPKata.AutoCad)

**Assigned Agent**: `worker_m5_iter2` (`teamwork_preview_worker`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_iter2\`  
**Target Solution**: `HPKata/HPKata.slnx`  
**Date**: 2026-10-17  

---

## 1. Context & Objectives

Milestone M5 (AutoCAD 2026 Add-in & Lisp Loader) Gate Iteration 8 resulted in `FAIL` due to unanimous `REQUEST_CHANGES` verdicts from both independent reviewers (`reviewer_m5_1` and `reviewer_m5_2`), who uncovered critical integrity defects:
1. `HandleLinkController.CheckDuplicateMarksInteractive()` hardcodes `"\n[HPKata] Scanned drawing: 0 duplicate rebar marks detected.\n"` without performing any scan or duplicate mark analysis.
2. `HandleLinkingTests.cs` (lines 50–80) bypasses `HandleLinkService` and inlines local dictionary lookup logic in the test body, self-certifying without testing production code.
3. `HandleLinkController.GhiThepInteractive()` prompts for point and polyline, but only prints a string message without inserting any tag block or creating a link.
4. `stt0`/`stt1` and `sstk` are informational stubs.
5. Missing `[assembly: ExtensionApplication(typeof(HPKataExtensionApplication))]` attribute.

Your task is to remediate ALL these defects, add genuine unit tests, verify clean code governance, and ensure 100% test pass.

---

## 2. Mandatory Inputs to Read
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
3. Reviewer reports:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m5_1\handoff.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m5_2\handoff.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`

---

## 3. Specific Remediation Tasks

### Task 1: Genuine Duplicate Mark Detection in `check_lap` (DEFECT-M5-01)
In `HPKata/HPKata.AutoCad/Commands/HandleLinkController.cs` (or dedicated helper if needed to respect Rule C2 <= 300 lines):
- Implement genuine duplicate mark checking:
  - In `CheckDuplicateMarksInteractive()`:
    - Get `doc = AcadApp.DocumentManager.MdiActiveDocument`. If null, return.
    - Open a Read transaction on `doc.Database`.
    - Iterate over `BlockTableRecord.ModelSpace` (or current space).
    - For each entity that has `"KATA"` XData (via `XDataService.HasKataXData(ent)` / `XDataService.ReadXData(ent)`):
      Extract `KataXDataRecord`, record its handle, `STT`, `TenCauKien`, `DuongKinh`, `ChieuDai`, `ShapeCode`.
    - Group by `STT` (and optionally `TenCauKien`).
    - Detect duplicates: any `STT` assigned to multiple entities where geometry/diameter/length conflict, or multiple entities sharing the same mark number within the same member.
    - Report actual results to `doc.Editor.WriteMessage`:
      - If duplicates detected: list each duplicated mark number, the conflicting entity handles, and the total count.
      - If 0 duplicates detected: report `"Scanned X rebar entities: 0 duplicate marks detected."` based on actual count `X` scanned.
  - Extract a pure, testable core method:
    - `public static int CheckDuplicateMarks(IEnumerable<KataXDataRecord> records, out List<string> conflictReports)`
    - Or overload accepting `ICadDocumentContext`.
    - This allows 100% headless testability in `HPKata.AutoCad.Tests`.

### Task 2: Pure Testable Overloads & Genuine Tests for `HandleLinkService` (DEFECT-M5-02)
In `HPKata/HPKata.AutoCad/Interop/HandleLinkService.cs`:
- Expose pure, testable core methods on `HandleLinkService` that operate directly on `KataXDataRecord`:
  - `public static void RebindRecordHandles(KataXDataRecord record, IReadOnlyDictionary<string, string> oldToNewHandleMap)`
    (replaces `LinkedHandle` if present in map).
  - `public static void LinkRecords(KataXDataRecord rebarRecord, string rebarHandle, KataXDataRecord tagRecord, string tagHandle)`
    (sets `rebarRecord.LinkedHandle = tagHandle` and `tagRecord.LinkedHandle = rebarHandle`).
  - `public static void SeverRecordLink(KataXDataRecord record)`
    (sets `record.LinkedHandle = ""` or null).
- In the existing AutoCAD entity methods (`CreateLink`, `SeverLink`, `RebindClonedEntities`), delegate to these pure record methods!
- In `HPKata/HPKata.AutoCad.Tests/HandleLinkingTests.cs`:
  - Rewrite all tests (`HandleLink_CloneRebinding_RemapsOldHandlesToNewHandles`, `HandleLink_RecordLinking_EstablishesBidirectionalHandles`, `HandleLink_SeverLink_ClearsBothHandles`) so they directly call:
    - `HandleLinkService.RebindRecordHandles(...)`
    - `HandleLinkService.LinkRecords(...)`
    - `HandleLinkService.SeverRecordLink(...)`
  - Zero inlined dictionary remapping inside test methods!

### Task 3: Genuine Tag Block Insertion in `ghi_thep` / `gt` (DEFECT-M5-03)
In `HPKata/HPKata.AutoCad/Commands/HandleLinkController.cs`:
- Implement genuine tag creation in `GhiThepInteractive()`:
  - After user picks polyline and point:
    - Open Write transaction.
    - Read polyline's `KataXDataRecord` via `XDataService.ReadXData(poly)`.
    - If polyline has no XData or default, extract basic shape info or prompt.
    - Create/insert a tag block reference (check `BlockTable` for block named `"thep"` or `"kata_block_KHT"`; if not found in database, create a fallback `MText` or lightweight block reference with attributes `SH`, `DK`, `KC`, `SL`, `CD1T`, `DKKC` formatted from the record).
    - Establish bidirectional handle link between the polyline and the new tag via `HandleLinkService.CreateLink(poly, tagEntity, tr)`.
    - Commit transaction.
    - Write confirmation message to `ed.WriteMessage($"\n[HPKata] Tag placed at {pt.Value} and linked to rebar handle {poly.Handle}.\n")`.

### Task 4: Stubs in `stt0`/`stt1` and `sstk` (DEFECT-M5-04)
- In `HandleLinkController.cs`:
  - Wire `stt0` and `stt1` to persist the active mark notation mode (`KataConfig` or active session setting `MarkNotationMode.Suffix` vs `MarkNotationMode.Sequential`).
- In `SummaryCommandRunner.cs`:
  - Implement genuine schedule comparison logic in `ExecuteCompareSchedule(ICadDocumentContext context)`. If two schedule tables exist in context, compare their item counts, total mass, and report differences.

### Task 5: Extension Application Attribute (DEFECT-M5-05)
- In `HPKata/HPKata.AutoCad/Commands/HPKataExtensionApplication.cs`:
  - Add `[assembly: Autodesk.AutoCAD.Runtime.ExtensionApplication(typeof(HPKata.AutoCad.Commands.HPKataExtensionApplication))]`.

### Task 6: Unit Tests in `HPKata.AutoCad.Tests`
- Add unit tests for:
  - `CheckDuplicateMarks` with 0 duplicates (clean drawing).
  - `CheckDuplicateMarks` with conflicting marks (conflicting dimensions/diameters with same STT).
  - `HandleLinkService` pure methods (`RebindRecordHandles`, `LinkRecords`, `SeverRecordLink`).
  - `SummaryCommandRunner.ExecuteCompareSchedule` table comparison logic.
- Target: All solution tests pass 100% green.

---

## 4. Strict Clean Code & Resource Directives

1. **Clean Code Governance**:
   - Rule C2: EVERY file in `HPKata.AutoCad` and `HPKata.AutoCad.Tests` must be $\le 300$ lines.
   - Rule M3: EVERY method body must be $\le 50$ lines.
   - Rule Q-B1: Zero commented-out or dead code.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Clean-Room: `C:\kata_pro` is strictly read-only. ZERO references to `Security.dll` or licensing.
2. **Resource Directives (MANDATORY)**:
   - Run compilation via:
     `dotnet build HPKata/HPKata.slnx -c Release --disable-build-servers -m:1`
   - Run tests via:
     `dotnet test HPKata/HPKata.slnx -c Release --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-worker5iter2-$([guid]::NewGuid())"`
   - Never spawn parallel test runners into default directories.

---

## 5. Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor and reviewers will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.
