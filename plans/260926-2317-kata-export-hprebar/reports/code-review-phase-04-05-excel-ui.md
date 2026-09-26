# Code review — phases 04–05: Excel COM writer + modeless UI (KataExport)

Date 2026-09-27. Static review only: no Revit run and no Excel run. Scope is 12 files plus 2 diffs, about 1 120 LOC:
- Excel side: [ExcelComAttach.cs](../../../HPRebar/HPRebar/KataExport/Service/ExcelComAttach.cs), [ComLateBinding.cs](../../../HPRebar/HPRebar/KataExport/Service/ComLateBinding.cs), [KataExcelWriter.cs](../../../HPRebar/HPRebar/KataExport/Service/KataExcelWriter.cs).
- Four root files, the VM, the runner, the view, and RevitDialogs.
- Diffs: [Application.cs](../../../HPRebar/HPRebar/Application.cs) and [RibbonIcons.cs](../../../HPRebar/HPRebar/Resources/Icons/RibbonIcons.cs).

Spec: [phase-04](../phase-04-excel-com-writer.md), [phase-05](../phase-05-ui-and-ribbon.md), [kata-cell-contract.md](kata-cell-contract.md).

**Score 7/10.**
- **Excel writer: solid.** COM lifetimes are scoped, the 2D array shapes are right, the clear range is right, nothing is saved, and busy errors get a friendly message.
- **UI: one High.** A failed re-pick crashes to the WPF dispatcher.
- **Mediums:** state and preview problems, a partial write that is not reported, and the workbook can change between probe and write.

## Verification run

| Check | Result |
|---|---|
| `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` | ✅ pass, 0 KataExport warnings |
| same, `-c Debug.R24` (net48), `--artifacts-path <scratch>` | ✅ pass, 0 Error, 0 KataExport warnings |
| `dotnet test HPRebar.Core.Tests` (includes `ThemeTokenCoverageTests`, which scans every add-in XAML) | ✅ 385/385 |
| Every `{DynamicResource}` key in `KataExportView.xaml` is defined (both palettes for `Brush.*`) | ✅ grep; no hex colour, no `StaticResource` except the converter |
| Every XAML binding path exists on the VM / `KataPreviewRow`; generated command names (`ExportCommand`, `RepickCommand`, `RefreshProbeCommand`, `CloseCommand`) | ✅ |
| `CommunityToolkit.Mvvm` 8.4.0 `AsyncRelayCommand` rethrows on the sync context by default (`AwaitAndThrowIfFailed` in the DLL) | ✅ verified → see H1 |
| Sample `C:\kata_pro\Kata.xlsm` (zip opened read-only), sheet `Dam` | `<sheetProtection selectLockedCells="1"/>` with no `sheet="1"`, so the sheet is **not protected**. B10 style xf 58 = `numFmtId 49` (`@`). No merged cell intersects C11:BZ23. Data validation only in rows 25–80 / I3 / B1 / B2 / I5. `vbaProject.bin` present (`Worksheet_Change` may fire) |
| `RevitAPIUI.xml` 2026: `Selection.SetElementIds` | throws only for null / SelectionChanged. Foreign ids are **not** rejected → see L1 |
| Plan refs in code | none (grep; "plan" hits are geometric plan-view) |
| Feature folder: 4 root files, singular subfolders, namespaces = folders | ✅ |
| VM 219 lines (< 250), XAML 270 (< 500), writer 194 | ✅ |

## Scout: edge cases checked

- **Re-pick failure path.** Every reader throw ends up at `TrySetException`, and the VM awaits it without a guard. This covers a non-straight run, sloped beams, mixed levels, a switched view during the pick, and no document → H1.
- **Build failure after re-pick.** More than 76 columns, or "shorter than one span", while probe succeeds → M3.
- **Workbook switch.** The user activates another workbook that also has `Dam` between probe and write → M2.
- **Kata VBA.** `Worksheet_Change` / `MsgBox` in Kata.xlsm makes Excel reject calls mid-write → M1.
- **Excel's text auto-parse.** Grid or beam names that look like dates or numbers → L4.
- **Document switch.** The user switches document while the window is open → Highlight selects foreign ids (L1).
- **Clicks during PickObjects.** Export or Close clicked while PickObjects runs inside the handler (L5).

## High

### H1 — A failed "Chọn lại dầm" (re-pick) throws an unhandled exception on Revit's UI thread
[KataExportViewModel.cs:190-206](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L190-L206), [KataExportExternalEventHandler.cs:87](../../../HPRebar/HPRebar/KataExport/KataExportExternalEventHandler.cs#L87), [KataExportExternalEventHandler.cs:97-100](../../../HPRebar/HPRebar/KataExport/KataExportExternalEventHandler.cs#L97-L100)

- **How the exception reaches the dispatcher:**
  1. The handler turns any exception into `TrySetException`. That includes `KataRunReader`'s `InvalidOperationException` ("not in line", "sloped", "different levels", "not a straight line"), and a PickObjects throw when the user switches view (documented).
  2. `RepickAsync` in the VM has no try/catch.
  3. `[RelayCommand]` on an async method uses `AsyncRelayCommand` with default options, so `Execute` calls `AwaitAndThrowIfFailed` (an `async void`). The exception is therefore rethrown on the WPF dispatcher.
- **Failure:** the user re-picks two beams that are not collinear, which is the most common mistake, and gets an unhandled dispatcher exception inside Revit.
- **Impact:** usually a Revit crash, with unsaved model work lost. This is GIẢ ĐỊNH CHƯA XÁC MINH (not run in Revit), but the toolkit's behaviour is verified in the DLL. `ExportAsync` is protected by its own try/catch. `BeamRebarViewModel` also catches, at line 98.
- **Fix:**
  ```csharp
  [RelayCommand]
  private async Task RepickAsync()
  {
      HasError = false;
      StatusMessage = "Vui lòng chọn dải dầm trên mặt bằng Revit...";
      try
      {
          var newSession = await _runner.RepickAsync();
          if (newSession is null) { StatusMessage = "Đã hủy thao tác chọn lại dầm."; return; }
          _session = newSession;
          UpdateFromSession();
          ProbeWorkbook();
      }
      catch (Exception ex)
      {
          Log.Warning(ex, "Kata Export re-pick failed");
          HasError = true;
          StatusMessage = $"Không đọc được dải dầm: {ex.Message}";   // old session stays usable
      }
  }
  ```

## Medium

### M1 — A write that fails mid-way is reported as "busy, try again" or a generic error, with no hint that the sheet is now half-written
[KataExcelWriter.cs:49-70](../../../HPRebar/HPRebar/KataExport/Service/KataExcelWriter.cs#L49-L70)

- **Order of COM writes:** B10 format → B3:B10 → clear C(3+n)..BZ rows 11–23 → rows 11 / 19 / 21 / 22 / 23. That is 8 separate calls.
- **Why a mid-way failure is realistic:** Kata.xlsm ships a VBA project. A `Worksheet_Change` handler that raises a `MsgBox`, or a recalculation, makes Excel reject the next call with `RPC_E_CALL_REJECTED` or `0x800AC472`.
- **What the user is left with:** new header, new row 11, and rows 19–23 from the previous beam. The message says "Excel đang bận… thử lại" and `ColumnsWritten = 0`.
- **Why this matters:** the spec asks for "lỗi giữa chừng → dừng, báo hàng lỗi" (on a mid-way error, stop and report the failing row). COM writes also clear Excel's undo stack.
- **Retry is safe:** the write is idempotent (same cells, full rows), so trying again is the right remedy. The user just has to be told.
- **Fix:**
  - Track a `step` string ("B3:B10", "xoá cột thừa", $"hàng {row}") and a `touched` flag.
  - On failure after the first mutation, append: "Sheet Dam đang ghi dở ở {step} — bấm Xuất Excel lại để ghi đè toàn bộ."

### M2 — The workbook shown as "Workbook đích" is not necessarily the one written
[KataExportViewModel.cs:86-101](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L86-L101), [KataExportViewModel.cs:166](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L166), [KataExcelWriter.cs:153](../../../HPRebar/HPRebar/KataExport/Service/KataExcelWriter.cs#L153)

- **Current behaviour:** the label is set when the window opens, on re-pick, and on "Kiểm tra lại Excel". `Write` re-attaches separately and writes into whatever `ActiveWorkbook` is at that moment.
- **Failure:**
  1. The probe shows `Kata_A.xlsm`.
  2. The user activates `Kata_B.xlsm` in Excel (another project, also with `Dam`) and comes back to Revit.
  3. "Xuất Excel" clears and overwrites B's beam, and Excel undo cannot restore it.
  4. The success message names B, but only after the damage.
- **Fix:**
  - Have `Probe` return `FullName`, and pass it to `Write(sheet, expectedFullName)`.
  - In `Target.Open`, refuse when `ActiveWorkbook.FullName` differs: "Workbook đang active là 'X', không phải 'Y' — bấm Kiểm tra lại Excel".
  - Refresh `TargetWorkbook` after each write.

### M3 — A sheet-build error is hidden by the probe status, and the preview goes stale after re-pick
[KataExportViewModel.cs:77-84](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L77-L84), [KataExportViewModel.cs:126-152](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L126-L152)

- **Build error overwritten by the probe:**
  - `RefreshSheet` sets `HasError=true` and "Lỗi tính toán…" when `KataRowBuilder.Build` throws (more than 76 columns, or "shorter than one span").
  - `ProbeWorkbook()` then runs right after, in the constructor and in re-pick. It overwrites the message with "Sẵn sàng ghi dữ liệu sang Excel." and sets `HasError=false`.
- **Preview left from the old run:** on failure `PreviewRows` and `Warnings` are not cleared. After a re-pick the window shows the new summary (beam count, section) above the **old run's rows**, next to "Đã cập nhật dải dầm mới thành công."
- **Probe error turns grey:** `RefreshSheet` success resets `HasError=false`, so a red probe error (Excel not running) turns grey as soon as the user toggles Reverse.
- **No wrong data gets written:** Export rebuilds and fails with a message. The problem is a misleading window.
- **Fix:**
  - Keep the built `KataSheet? _sheet`.
  - On failure, set `_sheet=null`, clear `PreviewRows`, and show only the session warnings.
  - Split the state into `SheetError` and `ExcelError`, and let status composition not overwrite a build error.
  - Add `[RelayCommand(CanExecute = nameof(CanExport))]` with `CanExport => _sheet is not null && !IsBusy`, plus `[NotifyCanExecuteChangedFor]`.
  - Export `_sheet`, which is exactly what was previewed.

### M4 — The preview does not show B3/B4 (or B9 after Reverse), and a missing `STR_*` silently picks an arbitrary parameter
[KataExportViewModel.cs:107-117](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L107-L117), [KataExportView.xaml:98-124](../../../HPRebar/HPRebar/KataExport/View/KataExportView.xaml#L98-L124)

- **Spec gap:** the spec asks for a "preview B3:B10 + 5 hàng", but only the five data rows and a geometric summary are shown.
  - The value that goes to **B3 (name)** and **B4 (count)** is never displayed, so an empty `STR_ElementName` writes a blank B3 unnoticed.
  - `AxisOffsetText` shows the session value unsigned. B9 is written as `axisOffset × sign`, so it is wrong in Reverse mode.
- **Arbitrary default:** without `STR_ElementName` / `STR_ElementCount`, both combos default to `AvailableParameters.FirstOrDefault()`, the alphabetically first instance parameter (e.g. "Assembly Code"). There is also no way to choose "no value".
- **Fix:**
  - Add a first preview row "B3..B10" built from `sheet.HeaderColumn`, and drive the B7/B8/B9 summary from it too.
  - Default to an explicit "(để trống)" entry plus a warning, not the first parameter.

## Low

| # | Where | Issue | Fix |
|---|---|---|---|
| L1 | [KataExportExternalEventHandler.cs:67](../../../HPRebar/HPRebar/KataExport/KataExportExternalEventHandler.cs#L67) | Highlight uses `app.ActiveUIDocument`. If the user switched document, `SetElementIds` does not reject foreign ids (RevitAPIUI.xml), so it selects unrelated elements or nothing. | Keep the session's `Document` (or `PathName`/`Title`). Skip Highlight when `!uidoc.Document.Equals(sessionDoc)`. |
| L2 | [KataExportViewModel.cs:171](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L171), [:179-183](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L179-L183) | A Highlight failure after a successful write replaces the success text with "Lỗi xuất Excel: …", which invites a pointless second export. | Wrap Highlight separately. Keep the success message and append "(không tô chọn được dầm)". Or fire-and-forget. |
| L3 | [KataExcelWriter.cs:26-27](../../../HPRebar/HPRebar/KataExport/Service/KataExcelWriter.cs#L26-L27) | The busy map omits `RPC_E_SERVERCALL_RETRYLATER` `0x8001010A` ("message filter indicated that the application is busy"), which is commonly reported with Excel. It gets the generic message. `0x800A03EC` (protected / merged) is also generic. | Add `0x8001010A`. Optionally map `0x800A03EC` to "Sheet Dam bị khoá hoặc có ô gộp trong vùng ghi". |
| L4 | [KataExcelWriter.cs:81-86](../../../HPRebar/HPRebar/KataExport/Service/KataExcelWriter.cs#L81-L86) | Strings assigned through `Value2` are parsed as if typed. A grid or beam name like `1-2` or `2/3` becomes a **date**, and `3E2` becomes 300. The old Dynamo tool did the same, so this is not a regression. | Only for strings that `double.TryParse` / `DateTime.TryParse` would accept and that are not meant to be numbers (row 22, B3, B8), prefix `'`. Keep plain numeric grid names ("1") as Dynamo wrote them unless Kata needs text. |
| L5 | [KataExportViewModel.cs:154-206](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L154-L206) | No shared busy guard. While PickObjects runs inside the handler, the window stays live, so "Xuất Excel" can export the **old** run and queue a Highlight. That Highlight runs right after the pick, in the same `Execute` loop. `IsExporting` is set but unused by the XAML. | One `IsBusy` flag used in both commands' `CanExecute`. Drop the redundant `if (IsExporting) return;` (AsyncRelayCommand already blocks re-entry). |
| L6 | [KataExcelWriter.cs:49](../../../HPRebar/HPRebar/KataExport/Service/KataExcelWriter.cs#L49) | `NumberFormat` is the only formatting call. On a sheet protected without *AllowFormattingCells* it throws before anything is written, even though value writes to unlocked cells would pass. The sample is unprotected and B10 is already `@` (xf 58, numFmt 49), so the risk is only runtime protection by Kata's VBA. | Read `NumberFormat` first and set it only when it is not `@`. Or write `'+3.300` (the plan allows either). |
| L7 | [KataExportView.xaml](../../../HPRebar/HPRebar/KataExport/View/KataExportView.xaml) | 27 raw `Margin`/`Padding` literals, some not multiples of 4 (`0,6,0,0` line 178, `4,6`, `14,6`, `0,2,0,0`), plus raw `CornerRadius="4"` and `Height="28"` on MaterialDesign ComboBoxes. FoundationRebarView uses 0. | Use `Spacing.Small/SmallBottom/SmallHorizontal/XSmall` tokens. Check the combo height in the theme gallery (`HPRebar/tools/theme-gallery`). |
| L8 | [KataGridReader.cs:52](../../../HPRebar/HPRebar/KataExport/Service/KataGridReader.cs#L52), [KataSessionReader.cs:24](../../../HPRebar/HPRebar/KataExport/Service/KataSessionReader.cs#L24), [KataRowBuilder.cs:98-100](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataRowBuilder.cs#L98-L100) | The window is Vietnamese, but the warnings list and reader exception texts ("Beam 123 is not in line…") are English and are shown inside Vietnamese dialogs and the warnings panel. | Translate the user-facing strings. Keep the Core exception text English only if it is mapped at the UI. |
| L9 | [RibbonIcons.cs:53](../../../HPRebar/HPRebar/Resources/Icons/RibbonIcons.cs#L53) | The arrow `M13,26 L16,29 L19,26 Z` uses **odd** coordinates. That breaks the file's own invariant ("every coordinate even"), and at 16 px the arrow becomes a 3×1.5 px blur. The column rings' top stroke (y 14–16) also doubles the beam's bottom stroke (12–14) into a 4 px band. | Arrow `M12,26 L16,30 L20,26 Z`. Start the columns at y=12 inside the beam ring, or drop their top stroke. Render with `HPRebar/tools/icons/preview-ribbon-icons.ps1` (not run here). |
| L10 | [KataExportView.xaml:235-241](../../../HPRebar/HPRebar/KataExport/View/KataExportView.xaml#L235-L241) | Long error texts (e.g. "Workbook '…' không có sheet 'Dam' — …") are cut by `CharacterEllipsis` between three buttons, with no tooltip. The Warnings `ItemsControl` has no max height, so many warnings squeeze the preview. | Add `ToolTip="{Binding StatusMessage}"` or wrap the text. Put the warnings in a `ScrollViewer MaxHeight`. |
| L11 | view | Esc does not close the window. The PickObjects Esc is handled correctly in both the command and the handler. | Add `IsCancel="True"` on "Đóng". In a modeless window it only fires the click, and `DialogResult` is not touched. |

Nits:
- `elem is FamilyInstance fi`: `fi` is unused ([KataExportSelectionFilter.cs:13](../../../HPRebar/HPRebar/KataExport/KataExportSelectionFilter.cs#L13)).
- The `session.Pieces.Count == 0` check can never fire, because the reader throws first ([KataExportCommand.cs:79](../../../HPRebar/HPRebar/KataExport/KataExportCommand.cs#L79)).
- `xmlns:vm` is unused.
- `ExternalEvent.Raise()` results are ignored, the same as BeamRebar.
- `Column()` does not assert `HeaderColumn.Count == 8` (a shorter array would fill `#N/A`).
- The button text is "Xuất Excel" while the spec says "Ghi Excel".

## Items (a)–(e) of the brief

- **(a) Excel**
  - ✅ Attach: P/Invoke `CLSIDFromProgID` + `GetActiveObject`, the same as HPExcel.
  - ✅ The `ActiveWorkbook` null case and the `Worksheets.Item("Dam")` failure (`DISP_E_BADINDEX` → `COMException`, unwrapped) are handled.
  - ✅ B10 gets `@` and then the string `+3.300` (KataText overrides `ToString`).
  - ✅ Array shapes: `object[8,1]` for B3:B10 and `object[1,n]` from C per row.
  - ✅ Clear range: C(3+n)..BZ rows 11–23, skipped when n = 76.
  - ✅ Only VT-safe cell types (string, double, int, long, bool; everything else goes through `ToString`).
  - ✅ No Save, `DisplayAlerts` untouched.
  - ✅ COM objects: every RCW is released once (`ReleaseComObject`, not `Final…`).
  - ✅ `TargetInvocationException` unwrapped with `ExceptionDispatchInfo`.
  - ✅ Called on the Revit main (STA) thread outside any API context, which is fine because no Revit API is used there.
  - ⚠️ Partial writes: M1. Workbook identity: M2. Busy codes: L3.
- **(b) Threading**
  - ✅ The VM never calls the Revit API (only the `ElementId` type).
  - ✅ PickObjects and `SetElementIds` run only in the handler (and PickObjects in the command's own API context).
  - ✅ Requests use `RunContinuationsAsynchronously`, and the queue is drained with exceptions when there is no document.
  - ✅ Static window with `Activate()`, owner via `WindowInteropHelper`, and `Closed` disposes the handler and clears the static.
  - ⚠️ Pending requests are left only when the window closes first (harmless).
  - ❌ H1.
- **(c) MVVM/XAML**
  - ✅ `sealed partial` + `[ObservableProperty]` + `[RelayCommand]`.
  - ✅ No DataContext in XAML; code-behind = Init + DataContext + `MaterialThemeBridge.Attach` + `CloseRequested += Close`.
  - ✅ No hex colours.
  - ✅ TwoWay bindings (`SelectedItem` → string, `IsChecked` → bool) have setters, and every path exists.
  - ⚠️ M3, M4, L5, L7.
- **(d) Ribbon**
  - ✅ Placed in the "Rebar" panel after "Foundation Rebar". `Track()` means `ApplyIcons` repaints it on `ThemeChanged` (R24+), and the window icon comes from the same glyph.
  - ⚠️ L9.
- **(e) Conventions**
  - ✅ Feature isolation (own `RevitDialogs`, the same pattern as the other features), namespaces match the folders, no plan refs.
  - ⚠️ Mixed languages: L8.

## Positive observations

- `Target : IDisposable` owns app / workbook / sheets / sheet together. Every intermediate `Range`/`Cells` RCW is released in `finally`.
- A whole row goes in one `Value2` round trip, which keeps the Excel call count and Worksheet_Change firing low.
- The clear range matches the old tool and the contract (rows 12–18/20 inside n untouched).
- The handler mirrors BeamRebar, and it adds the no-document drain that BeamRebar lacks.
- The build is cheap on the UI side: Reverse and the parameter choice rebuild from the snapshot with no API call.
- It compiles on R24 net48 and R26 net8 with no new warnings. Theme tokens are all covered.

## Plan follow-ups (report only — plan files not edited)

| Phase | Criterion | State |
|---|---|---|
| 04 | Build R26 + compile R24 | ✅ Built |
| 04 | Write into a draft workbook; block a workbook without `Dam` | ❌ CHƯA TEST (no Excel run). The block path is correct in the code |
| 05 | Build R26 + R24, `ThemeTokenCoverageTests` | ✅ Built + Tested |
| 05 | Icon preview 16/32 px | ❌ not run (L9 found statically) |

## Recommended actions

1. Fix **H1** before any live test in Revit.
2. M3 + M4: a single `_sheet` source for preview and export, split error states, a B3..B10 preview row, and an explicit "(để trống)" default.
3. M2: guard on workbook `FullName`. M1: step-aware partial-write message.
4. Lows in one pass: L1, L2, L3, L5 first. L7–L11 are cosmetic.
5. Then the live smoke test: a draft `Kata.xlsm` copy, write, read back `B3:B10` and `C11:BZ23`, Reverse, a workbook without `Dam`, and Excel in cell-edit mode.

**Status:** DONE_WITH_CONCERNS
**Summary:** Excel writer is sound. H1 is a likely Revit crash on a failed re-pick. Four Mediums: state/preview, partial-write report, workbook identity, header preview.
**Concerns:** H1 must be fixed before a live run. Everything here is static except the builds and the 385 tests.
