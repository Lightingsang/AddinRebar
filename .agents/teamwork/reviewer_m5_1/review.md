# Architecture & Clean Code Review — Milestone M5 (HPKata.AutoCad)

**Reviewer**: `reviewer_m5_1` (`teamwork_preview_reviewer`)  
**Date**: 2026-10-17  
**Scope**: `HPKata.AutoCad` (.NET 8 AutoCAD 2026 plugin) and `HPKata.AutoCad.Tests`  
**Verdict**: **REQUEST_CHANGES**  

---

## Executive Summary

An in-depth architecture, clean code, and adversarial review was conducted on Milestone M5 (`HPKata.AutoCad` and `HPKata.AutoCad.Tests`).
While project configuration (`net8.0-windows`, `ExcludeAssets="runtime" PrivateAssets="all"`, `EnableDynamicLoading=true`), Clean Code line limits (Rule C2 $\le 300$, Rule M3 $\le 50$), and core analytical math (`FilletArcCalculator`, `XDataService`, `RebarGeometryService`, `ScheduleCommandRunner`, `SummaryCommandRunner`, `CuttingStockCommandRunner`, `ExportCommandRunner`) are implemented with excellent engineering rigor, **critical integrity violations** were discovered in several Phase 1 commands:
Specifically, `check_lap` (`CheckDuplicateMarksInteractive`) is a **dummy/facade implementation** that performs no drawing scan and hardcodes `"0 duplicate rebar marks detected"`. Similarly, `ghi_thep` (`GhiThepInteractive`) prompts for rebar and point but never creates or inserts any tag block or text, acting as a message-only stub.

Per strict system instructions:
> *"When reviewing work, actively check for integrity violations: Dummy or facade implementations that look correct but implement no real logic... If you detect ANY of these patterns, your verdict MUST be REQUEST_CHANGES with a Critical finding tagged as INTEGRITY VIOLATION. Do NOT approve work that cheats, regardless of test scores."*

Consequently, the verdict is **REQUEST_CHANGES**.

---

## Detailed Findings

### [Critical] Finding 1 — INTEGRITY VIOLATION: Dummy/Facade Implementation in `check_lap` (`CheckDuplicateMarksInteractive`)

- **What**: The implementation of command `check_lap` hardcodes a completion message without performing any database scan or duplicate check.
- **Where**: `HPKata/HPKata.AutoCad/Commands/HandleLinkController.cs`, lines 224–229:
  ```csharp
  public static void CheckDuplicateMarksInteractive()
  {
      Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
      if (doc == null) return;
      doc.Editor.WriteMessage("\n[HPKata] Scanned drawing: 0 duplicate rebar marks detected.\n");
  }
  ```
- **Why**: In structural detailing with KataPro, `check_lap` is an essential Quality Control tool to detect duplicate rebar marks (STT) across members. Hardcoding "0 duplicate rebar marks detected" provides false assurance to users and cheats the specification.
- **Required Fix**: Implement genuine duplicate mark scanning. Open a transaction, iterate ModelSpace (or use `ICadDocumentContext.GetAllRebarEntities()`), group rebars by `STT`, and identify any conflicting records (e.g. differing diameter, shape, or length sharing the same mark), reporting actual count and handles found.

---

### [Major] Finding 2 — Dummy/Facade Implementation in `ghi_thep` (`GhiThepInteractive`)

- **What**: Command `ghi_thep` (`gt`) prompts for a rebar polyline and a point, but does not create or insert any tag block or text entity into the drawing.
- **Where**: `HPKata/HPKata.AutoCad/Commands/HandleLinkController.cs`, lines 164–177:
  ```csharp
  public static void GhiThepInteractive()
  {
      Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
      if (doc == null) return;
      Editor ed = doc.Editor;

      ObjectId? rebarId = CadSelectionService.PromptSelectEntity(ed, "Select rebar polyline to tag: ", typeof(Polyline));
      if (!rebarId.HasValue) return;

      Point3d? pt = CadSelectionService.PromptPoint(ed, "Specify tag placement point: ");
      if (!pt.HasValue) return;

      ed.WriteMessage($"\n[HPKata] Ghi thep tag placed at {pt.Value}.\n");
  }
  ```
- **Why**: `ghi_thep` is a primary Phase 1 rebar detailing command. Leaving it as a console message stub means users cannot actually generate rebar callout tags on the drawing.
- **Required Fix**: Insert a block reference (e.g. `thep` or `kata_block_KHT` if present in the block table, or a fallback MText/callout entity) at `pt.Value`, populate its attributes (`SH`, `DK`, `KC`, `SL`, `CD1T`, `DKKC`) from the selected polyline's XData, and establish bidirectional linking via `HandleLinkService.CreateLink`.

---

### [Major] Finding 3 — Placeholder Stubs in `stt0`, `stt1`, and `sstk`

- **What**:
  - `AssignMarkNotation` (`stt0`, `stt1`) in `HandleLinkController.cs` (lines 179–185) writes an informational message to `doc.Editor` without setting or persisting any configuration mode.
  - `ExecuteCompareSchedule` (`sstk`) in `SummaryCommandRunner.cs` (lines 54–58) writes an informational message without scanning or comparing existing schedule tables.
- **Where**:
  - `HPKata/HPKata.AutoCad/Commands/HandleLinkController.cs`, lines 179–185
  - `HPKata/HPKata.AutoCad/Commands/SummaryCommandRunner.cs`, lines 54–58
- **Why**: While secondary to `check_lap` and `gt`, these commands are listed in the Phase 1 command inventory.
- **Required Fix**: Persist the mark notation preference into `KataConfig` for `stt0`/`stt1`, and implement schedule comparison logic for `sstk`.

---

### [Minor] Finding 4 — Missing `[assembly: ExtensionApplication]` Attribute

- **What**: `HPKataExtensionApplication` implements `IExtensionApplication`, but lacks the explicit assembly-level attribute.
- **Where**: `HPKata/HPKata.AutoCad/Commands/HPKataExtensionApplication.cs`
- **Why**: While AutoCAD automatically scans all exported types upon loading to find classes implementing `IExtensionApplication`, explicitly decorating the assembly with `[assembly: ExtensionApplication(typeof(HPKataExtensionApplication))]` eliminates runtime reflection scan overhead and prevents ambiguity if additional internal types are introduced.
- **Suggested Fix**: Add `[assembly: ExtensionApplication(typeof(HPKata.AutoCad.Commands.HPKataExtensionApplication))]` above the namespace or in an `AssemblyInfo.cs`.

---

## Positive Verified Items & Commendations

1. **Project Structure & Dependency Isolation**:
   - `HPKata.AutoCad.csproj` correctly targets `net8.0-windows` with `<EnableDynamicLoading>true</EnableDynamicLoading>`.
   - `AutoCAD.NET 25.1.0` is strictly referenced with `ExcludeAssets="runtime" PrivateAssets="all"`, guaranteeing no runtime AutoCAD DLL collisions.
   - References `HPKata.Core` and `HPKata.UI` cleanly without host-cross-wiring.

2. **Clean Code Rule Compliance**:
   - **Rule C2**: All 36 source and test files in `HPKata.AutoCad` and `HPKata.AutoCad.Tests` are strictly $\le 300$ lines (longest file: `RebarEditController.cs` at 257 lines).
   - **Rule M3**: 0 methods exceed 50 lines.
   - **Rule Q-B1**: 0 dead or commented-out code.
   - **Rule Q-B2**: All catch blocks are annotated with reason comments.

3. **Core Analytical Math & Interop Services**:
   - `FilletArcCalculator.cs`: Implements exact analytical vertex bulges ($b = \pm \tan(\theta/4)$) and tangent clipping ($t \le 0.45 \min(L_1, L_2)$).
   - `XDataService.cs`: Implements clean bidirectional conversion between AutoCAD `ResultBuffer` / `TypedValue` and `KataXDataRecord`.
   - `CadDocumentContext`: Clean abstraction enabling 100% headless testing via `MockCadDocumentContext`.

4. **Test Suite Verification**:
   - `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory $resultsDir`:
     - `HPKata.AutoCad.Tests`: **31 passed**, 0 failed, 0 skipped.
     - `HPKata.Core.Tests`: **168 passed**, 0 failed, 0 skipped.
     - `HPKata.UI.Tests`: **26 passed**, 0 failed, 0 skipped.
     - Total: **225 passed**, 0 failed, 0 skipped. Duration: ~1.5 seconds.
