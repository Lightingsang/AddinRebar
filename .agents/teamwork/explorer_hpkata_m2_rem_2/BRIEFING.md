# BRIEFING — 2026-10-18T04:48:30Z

## Mission
Investigate and provide exact diffs for Rule M3 violations in HPKata.Revit commands (ColumnRebarCommands.cs, FramingRebarCommands.cs, WallRebarCommands.cs) and resolve safe teardown isolation in RevitExportExcelRunner / RevitExportExcelRunnerTests to protect C:\HPKata\Kata.xlsm.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\
- Original parent: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Milestone: M2 Iteration 2 (Features 5–8)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement directly in HPKata source code
- All proposed changes must be delivered as exact diffs / patches in handoff.md
- Rule C2: all .cs files <= 300 lines
- Rule M3: all methods <= 50 lines (target <= 35 lines for Execute)
- Zero warnings, no commented-out code (Q-B1), no empty catch without reason comment (Q-B2)
- Safe teardown: never delete or mutate master library files in C:\HPKata\

## Current Parent
- Conversation ID: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Updated: 2026-10-18T04:48:30Z

## Investigation State
- **Explored paths**:
  - `HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs`
  - `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs`
  - `HPKata/HPKata.Revit/Commands/WallRebarCommands.cs`
  - `HPKata/HPKata.Revit/Runners/RevitExportExcelRunner.cs`
  - `HPKata/HPKata.Revit.Tests/Runners/RevitExportExcelRunnerTests.cs`
  - `HPKata/HPKata.AutoCad/Commands/ExportCommandRunner.cs`
  - `HPKata/HPKata.Core/Configuration/KataInstallEnvironment.cs`
- **Key findings**:
  1. `Execute` in `ColumnRebarCommands.cs`, `FramingRebarCommands.cs`, and `WallRebarCommands.cs` were each 56 lines (exceeding Rule M3 50-line ceiling).
  2. In `RevitExportExcelRunner.cs`, `ResolveOutputPath` unconditionally returned `KataWorkbookPath` (`C:\HPKata\Kata.xlsm`) whenever it existed on disk, rather than deriving a document-specific export path when `outputFilePath == null`.
  3. In `RevitExportExcelRunnerTests.cs`, `Execute_ResolvesOutputPathFromContextTitle` failed because `"C:\HPKata\Kata.xlsm"` did not contain `"DuAnTest"`, and the test teardown (`finally { if (File.Exists(generatedPath)) File.Delete(generatedPath); }`) deleted `C:\HPKata\Kata.xlsm`.
  4. Extracted helper methods (`ShowDialogAndGenerateRebar` [15 lines] and `CreateViewModel` [19 lines]) reduce `Execute` in all three commands to 31 lines (<= 35 lines).
  5. Refactored `ResolveOutputPath` to resolve from `context.DocumentPath` or `%TEMP%\{safeTitle}_ThongKeThep.xlsx`, guarded fallback export from overwriting master library assets, and isolated test execution via `KataInstallEnvironment.UseRootDirectory(isolatedDir)` with master asset deletion protection.
- **Unexplored areas**: None within the assigned scope.

## Key Decisions Made
- `Execute` in `ColumnRebarCommands`, `FramingRebarCommands`, and `WallRebarCommands` refactored to 31 lines using `ShowDialogAndGenerateRebar` and `CreateViewModel`.
- `RevitExportExcelRunner.ResolveOutputPath` decoupled from master library path when `outputFilePath` is not specified, resolving to document directory or `%TEMP%`.
- `ClosedXmlExporter` fallback guarded against overwriting master library assets.
- `RevitExportExcelRunnerTests.Execute_ResolvesOutputPathFromContextTitle` scoped in `KataInstallEnvironment.UseRootDirectory(isolatedDir)` with safe teardown guard.

## Artifact Index
- `m2_revit_remediation.patch` — Unified diff patch for all 5 files
- `proposed_ColumnRebarCommands.cs` — Full proposed replacement
- `proposed_FramingRebarCommands.cs` — Full proposed replacement
- `proposed_WallRebarCommands.cs` — Full proposed replacement
- `proposed_RevitExportExcelRunner.cs` — Full proposed replacement
- `proposed_RevitExportExcelRunnerTests.cs` — Full proposed replacement
- `scan_methods.py` — Scanner script verifying C2 and M3 compliance
- `inspect_methods.py` — Method line counter script
- `handoff.md` — 5-component handoff report
- `progress.md` — Liveness heartbeat
