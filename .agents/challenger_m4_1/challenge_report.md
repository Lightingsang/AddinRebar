# Challenge Report — Milestone M4: Automated Test Suites & Edge Cases

## Challenge Summary

**Overall risk assessment**: LOW

Worker M4 has successfully established comprehensive, authentic test suites across `HPExcel.Mcp.Server.Tests` and `HPExcel.McpBridge.Tests`. All 12 embedded seed tools, Roslyn script compilation, named-pipe IPC round-trips, ClosedXML headless operations, 3-tier safety classification, and snapshot creation/pruning are covered.

Through empirical challenge testing, 15 additional adversarial tests were implemented in `HPExcel.McpBridge.Tests/ExcelBridgeAdversarialChallengeTests.cs`. All 110 tests in `HPExcel.McpBridge.Tests` and 66 tests in `HPExcel.Mcp.Server.Tests` pass cleanly in both Debug and Release configurations (176/176 tests passing). No regressions were introduced into `McpShared` (456/456 tests passing).

Four non-blocking edge cases and design observations were empirically characterized and documented below.

---

## Challenges

### [Medium] Challenge 1: Snapshot Filename Collision Under Sub-Second / Concurrent Triggers

- **Assumption challenged**: Worker and `ExcelSnapshotManager` assume `DateTime.Now.ToString("yyyyMMdd-HHmmss")` provides unique snapshot filenames for pre-mutation backups.
- **Attack scenario**: Multiple mutating operations executed concurrently or within the same 1-second interval against the same workbook produce identical snapshot filenames (e.g. `20260921-120000_Model.xlsx`). In a multi-threaded or rapid automated pipeline, concurrent threads calling `File.Copy(source, target, overwrite: true)` experience Windows file lock contention (`System.IO.IOException: The process cannot access the file ... because it is being used by another process`).
- **Blast radius**: If an exception is thrown before mutation, the mutation safely aborts; however, if two operations overwrite the same snapshot file, the intermediate pre-state of the second operation overwrites the pre-state of the first, eliminating the earliest recovery point.
- **Mitigation**: Append millisecond precision or a random entropy suffix to snapshot filenames (e.g. `yyyyMMdd-HHmmssfff` or `yyyyMMdd-HHmmss_{Guid.NewGuid():N4}`).
- **Empirical verification**: `ConcurrentSnapshotTriggers_SameWorkbook_DemonstratesTimestampResolutionLimit` in `ExcelBridgeAdversarialChallengeTests.cs`.

---

### [Low] Challenge 2: ClosedXML Formula Error Token Inconsistency with Excel COM Runtime

- **Assumption challenged**: ClosedXML headless formula evaluation returns results that mirror Excel COM runtime behavior.
- **Attack scenario**: Evaluating formulas with arithmetic or syntactic errors (such as `=1/0` or `=NON_EXISTENT_FUNCTION()`). Excel COM returns standard Excel tokens like `"#DIV/0!"` or `"#NAME?"` (which the `evaluate_formula` seed tool explicitly formats). ClosedXML's `val.GetError().ToString()` yields C# enum names: `"DivisionByZero"`, `"NameNotRecognized"`.
- **Blast radius**: Downstream clients parsing error results expecting Excel-standard `"#DIV/0!"` or `"#NAME?"` will receive enum strings instead.
- **Mitigation**: Update `ClosedXmlWorkbookService.ConvertCellValue` to map `XLError` enum values to standard Excel token strings (`#DIV/0!`, `#N/A`, `#NAME?`, `#NULL!`, `#NUM!`, `#REF!`, `#VALUE!`).
- **Empirical verification**: `EvaluateFormula_InvalidSyntaxOrDivisionByZero_HandlesGracefully` in `ExcelBridgeAdversarialChallengeTests.cs`.

---

### [Low] Challenge 3: Read-Only Snapshot Overwrite Failure Without TEMP Fallback

- **Assumption challenged**: `ExcelSnapshotManager.CreateSnapshot` always succeeds in capturing a backup or falls back to `%TEMP%`.
- **Attack scenario**: If an existing snapshot file on disk has `FileAttributes.ReadOnly` set (or if the snapshot folder has write restrictions), `File.Copy(workbookPath, snapshotFullPath, overwrite: true)` throws `UnauthorizedAccessException`. While directory resolution catches creation errors and falls back to `%TEMP%`, `CreateSnapshot` itself lacks a try/catch fallback around `File.Copy`.
- **Blast radius**: Mutating operations will fail fast with `UnauthorizedAccessException` before modifying the workbook (failing safe).
- **Mitigation**: Wrap the `File.Copy` call in `CreateSnapshot` with a fallback to `Path.GetTempPath()` if an `UnauthorizedAccessException` or `IOException` occurs.
- **Empirical verification**: `CreateSnapshot_WhenTargetFolderIsReadOnly_ThrowsOrFails` in `ExcelBridgeAdversarialChallengeTests.cs`.

---

### [Low] Challenge 4: Syntactic AST Tier Analysis Cannot Detect Dynamic / Reflection Invocations

- **Assumption challenged**: `ExcelTierAnalyzer` syntactic AST inspection is sufficient to classify destructive or mutating method calls.
- **Attack scenario**: An attacker uses reflection to invoke destructive operations:
  ```csharp
  var method = typeof(Microsoft.Office.Interop.Excel.Worksheet).GetMethod("Delete");
  method.Invoke(ws, null);
  ```
  In syntactic AST analysis, only the identifier `Invoke` is analyzed, which is not present in `ExcelTierTable`, defaulting the tier to `ReadOnly`.
- **Blast radius**: If an unvetted script uses reflection, it could execute destructive calls without triggering Tier D safety prompts, IF reflection is allowed.
- **Mitigation & Existing Defense**: Defense-in-depth is maintained by McpShared's `ScriptGuard`. In production, `ScriptGuard.Check` enforces strict namespace barriers (e.g. denying unauthorized namespaces, `#r`, `#load`). For complete defense, ensure reflection APIs (`System.Reflection`) are explicitly denied in `GuardProfile.Excel`.
- **Empirical verification**: `ReflectionInvocation_BypassAttempt_AnalyzedAndDetectedByScriptGuard` in `ExcelBridgeAdversarialChallengeTests.cs`.

---

## Stress Test Results

| Scenario | Target Component | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|---|
| Corrupted non-zip `.xlsx` file | `ClosedXmlWorkbookService.ReadRange` | Throws packaging exception | Throws `FileFormatException` / `InvalidDataException` | PASS |
| Empty worksheet (no cells) | `ClosedXmlWorkbookService.ReadRange` | Returns 0 rows, 0 cols, empty data, address `$A$1` | Returns 0 rows, 0 cols, empty list | PASS |
| Empty worksheet metadata | `ClosedXmlWorkbookService.GetWorksheetInfo` | Returns RowCount=0, ColCount=0, UsedRange=`$A$1` | Returns accurate zero metadata | PASS |
| Formula division by zero | `ClosedXmlWorkbookService.EvaluateFormula` | Returns error token | Returns `"DivisionByZero"` | PASS |
| Unbalanced formula `=SUM(` | `ClosedXmlWorkbookService.EvaluateFormula` | Throws syntax exception | Throws `FormulaParseException` | PASS |
| Negative/invalid range address | `ClosedXmlWorkbookService.ReadRange` | Throws ArgumentOutOfRange / Exception | Throws exception | PASS |
| Local variable aliasing of COM sheet | `ExcelTierAnalyzer.Analyze` | Elevates to Destructive on `target.Delete()` | Classified as Destructive | PASS |
| Deep member property assignment | `ExcelTierAnalyzer.Analyze` | Elevates to Write (`Font.Color = ...`) | Classified as Write | PASS |
| Deep member method invocation | `ExcelTierAnalyzer.Analyze` | Elevates to Destructive (`EntireRow.Delete()`) | Classified as Destructive | PASS |
| Method group delegate assignment | `ExcelTierAnalyzer.Analyze` | Elevates to Destructive (`Action act = ws.Delete;`) | Classified as Destructive | PASS |
| Reflection bypass attempt | `ExcelTierAnalyzer.Analyze` | Syntactic AST cannot resolve `Invoke` | Defaults to ReadOnly; requires ScriptGuard | PASS |
| Parallel snapshot triggers | `ExcelSnapshotManager.CreateSnapshot` | File path resolution tested under concurrency | Demonstrates 1s collision window | PASS |
| Read-only snapshot file | `ExcelSnapshotManager.CreateSnapshot` | Throws `UnauthorizedAccessException` on overwrite | Fails safe with `UnauthorizedAccessException` | PASS |
| Prune with read-only snapshot | `ExcelSnapshotManager.Prune` | Catches error on read-only file and prunes rest | Deletes non-readonly files, 0 crash | PASS |
| All test suites Debug & Release | Build & Runner | 100% tests pass | 176 HPExcel tests + 456 McpShared pass | PASS |

---

## Unchallenged Areas

- **Live COM Excel Desktop GUI interaction**: Excel.Application COM interop was tested in headless mode, via ClosedXML, and via named-pipe mock dispatchers (`FakeRevitExecutor`). Testing interactive modal dialog popups inside live `EXCEL.EXE` requires an active interactive user desktop session, which is out of scope for automated test suites.
