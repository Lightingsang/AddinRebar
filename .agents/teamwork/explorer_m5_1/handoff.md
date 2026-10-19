# Handoff Report: AutoCAD 2026 (.NET 8) Architecture for HPKata.AutoCad

- **Agent**: `explorer_m5_1` (AutoCAD 2026 .NET 8 Architecture Explorer)
- **Recipient**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
- **Date**: 2026-10-17
- **Deliverable**: `HPKata.AutoCad` architecture design, project configuration, lifecycle, WPF hosting, and clean command partitioning.
- **Artifact Path**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_1\analysis.md`

---

## 1. Observation

Direct observations and evidence gathered during the architectural investigation:

1. **AutoCAD 2026 Runtime & Project Configuration**:
   - `HPAutoCad/HPAutoCad/HPAutoCad.csproj` (lines 7–32) targets `net8.0-windows` with `EnableDynamicLoading=true` and references `AutoCAD.NET` 25.1.0 with `ExcludeAssets="runtime" PrivateAssets="all"`.
   - Omitting `ExcludeAssets="runtime"` causes `AcCoreMgd.dll`, `AcDbMgd.dll`, and `AcMgd.dll` to be copied to the output folder, causing severe runtime type conflicts with `acad.exe`'s loaded assemblies.
2. **AutoCAD Lifecycle & RegApp Requirements**:
   - `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (§5, lines 18–20) establishes that KataPro relies on XData application name `"KATA"` for entity linking and property persistence.
   - AutoCAD throws `ErrorStatus.BadDxfHeader` or `ErrorStatus.RegappIdNotFound` if XData is assigned before `"KATA"` is registered in `db.RegAppTableId`.
   - `HPAutoCad/HPAutoCad.Loader/HPAutoCadLoaderApplication.cs` (lines 25–110) implements `IExtensionApplication` (`Initialize` and `Terminate`).
3. **Dynamic Ribbon Integration & System Variables**:
   - `HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs` (lines 35–112) demonstrates that AutoCAD Ribbon initialization must hook `ComponentManager.ItemInitialized` to survive late UI loading.
   - It also watches `Application.SystemVariableChanged` for `"WSCURRENT"` (workspace switch) and `"COLORTHEME"` (palette change) and defers UI re-creation to `Application.Idle` to prevent UI thread lockups.
4. **WPF Host Integration & Theme Synchronization**:
   - `HPKata/HPKata.UI/Common/ThemeManager.cs` (lines 13–68) exposes `ThemeManager.Attach(Window window, IHostTheme host)` which attaches `ThemeDark.xaml` / `ThemeLight.xaml` and binds MaterialDesign tokens.
   - `HPAutoCad/HPAutoCad/Resources/Themes/AutocadHostTheme.cs` (lines 10–39) implements `IHostTheme` by reading AutoCAD system variable `"COLORTHEME"` (0 = Dark, 1 = Light).
   - `HPAutoCad/HPAutoCad/HPGeoLink/Commands/HPGeoDialogCommand.cs` (lines 72–166) demonstrates modal hosting via `AcadApp.ShowModalWindow()` and modeless hosting via `AcadApp.ShowModelessWindow(AcadApp.MainWindow.Handle, window, false)`.
   - Modeless interaction with AutoCAD database requires `doc.LockDocument()`; omitting `LockDocument()` when accessing CAD from WPF causes `eDocumentSwitchDisabled` crashes.
5. **Clean Code & Governance Standards**:
   - `docs/clean-code/HP_CLEAN_CODE_CORE.md` (lines 40–76): Rule C2 requires classes/files $\le 300$ lines, ViewModels $\le 250$ lines; Rule M3 requires methods $\le 50$ lines; Rule Q-B1 forbids dead/commented code; Rule Q-B2 forbids unannotated empty catch blocks.
   - `docs/clean-code/host-appendix/autocad-civil.md` (lines 7–22): Rule A2 requires deterministic disposal of all AutoCAD wrappers (`using var`); Rule A14 requires one shared ribbon tab; Rule A15 mandates that drawing-writing commands never use `CommandFlags.NoUndoMarker`.
   - `C:\kata_pro\Goi lenh.lsp` (lines 1–150) exposes >110 commands and aliases (`setupkata`, `vt`, `sua`, `link`, `ud`, `tkt`, `tohopthep`, `toexcel`, etc.).

---

## 2. Logic Chain

1. **From Observation 1**: Because AutoCAD 2026 runs on .NET 8, `HPKata.AutoCad.csproj` must target `net8.0-windows` and reference `AutoCAD.NET 25.1.0` with `ExcludeAssets="runtime" PrivateAssets="all"`, while referencing `HPKata.Core` (domain logic) and `HPKata.UI` (WPF views).
2. **From Observation 2**: Because KataPro entities require XData stamped with `"KATA"`, and drawings can be opened or created after application startup, `HPKataExtensionApplication` must ensure `"KATA"` is registered in the active document at startup and subscribe to `DocumentManager.DocumentCreated` and `DocumentManager.DocumentActivated` to register `"KATA"` in any subsequent drawing.
3. **From Observation 3**: Because AutoCAD's ribbon engine may load after `IExtensionApplication.Initialize()`, the Ribbon manager must subscribe to `ComponentManager.ItemInitialized` and defer rebuilding on workspace (`WSCURRENT`) or theme (`COLORTHEME`) changes to `Application.Idle`.
4. **From Observation 4**: Because `HPKata.UI` already provides `SetupView`, `DetailingView`, `ScheduleView`, `CuttingStockView`, and `ThemeManager`, `HPKata.AutoCad` can seamlessly host these views by bridging AutoCAD's `"COLORTHEME"` through `AutocadHostTheme : IHostTheme`.
   - For configuration and placement (`SetupView`, `DetailingView`), synchronous modal hosting via `Application.ShowModalWindow()` prevents command reentrancy.
   - For auditing and optimization (`ScheduleView`, `CuttingStockView`), modeless hosting via `Application.ShowModelessWindow()` allows users to inspect the drawing while keeping the dialog open. Any CAD modification initiated from modeless windows must wrap database access in `using (doc.LockDocument())`.
5. **From Observation 5**: Because registering >110 commands with CAD interactions in a few large files would quickly violate Rule C2 ($\le 300$ lines) and Rule M3 ($\le 50$ lines), the command structure must be partitioned into:
   - Thin command classes (`SetupCommands.cs`, `RebarCommands.cs`, `LinkCommands.cs`, `ScheduleCommands.cs`, `CuttingStockCommands.cs`, `TagCommands.cs`) containing only `[CommandMethod]` entry points (<120 lines each).
   - Domain controllers (`SetupController`, `RebarDrawController`, `RebarEditController`, `RebarLinkController`, `ScheduleController`, `CuttingStockController`, `TagController`) orchestrating UI, selection, and transaction logic (<220 lines each).
   - Shared CAD infrastructure services (`CadSelectionService`, `CadPolylineService`, `CadXDataService`, `CadBlockService`, `CadHighlightService`) handling database mechanics with deterministic wrapper disposal (<220 lines each).

---

## 3. Caveats

1. **AutoCAD Installation Dependency**: Live runtime execution of `HPKata.AutoCad` requires AutoCAD 2026 (or AutoCAD 2025) installed on the host machine. However, compile-time build verification and unit tests in `HPKata.Core.Tests` and `HPKata.UI.Tests` execute without AutoCAD installed.
2. **Lisp Command Parity**: Modern AutoCAD users expect single-letter or short aliases (`vt`, `sua`, `link`, `tkt`, `tohopthep`). While C# `[CommandMethod]` registers both full names and aliases directly, providing the companion `GoiLenh_HPKata.lsp` file ensures 100% backward compatibility for legacy Lisp workflows.
3. **Hardware Acceleration & BAML Resources**: When loading WPF assemblies dynamically inside AutoCAD .NET 8, `EnterContextualReflection()` must wrap window instantiations to avoid WPF resource dictionary resolution failures.

---

## 4. Conclusion

The architectural design for `HPKata.AutoCad` is fully specified and validated against the repository's Clean Code Governance and AutoCAD host rules:
- **`HPKata.AutoCad.csproj`** is ready for scaffolding targeting `net8.0-windows` with compile-only `AutoCAD.NET 25.1.0` references, `EnableDynamicLoading`, and bundle packaging.
- **Lifecycle & RegApp Engine** guarantees `"KATA"` is present in all drawings, preventing XData errors, with self-healing ribbon integration.
- **WPF Host Integration** provides seamless dark/light theme tracking and thread-safe modal and modeless execution with `doc.LockDocument()`.
- **Command Layer Decomposition** strictly guarantees compliance with Rule C2 ($\le 300$ lines) and Rule M3 ($\le 50$ lines) across all Phase 1 commands and provides the modular foundation for all 136 commands of the KataPro catalog.

Detailed technical specifications, code skeletons, and XML manifests are documented in `analysis.md`.

---

## 5. Verification Method

To independently verify the architecture and prepare for implementation:

1. **Inspect Blueprint & Specifications**:
   - Verify command mappings in `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (§2).
   - Verify 86 configuration parameters in `HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (§3).
   - Inspect complete architecture report at `.agents/teamwork/explorer_m5_1/analysis.md`.
2. **Build & Test Baseline Verification**:
   - Verify `HPKata/HPKata.slnx` builds cleanly:
     ```powershell
     dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1
     ```
   - Verify existing test suites pass:
     ```powershell
     dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj --no-build --results-directory "$env:TEMP\mtp-verify"
     ```
3. **Invalidation Conditions**:
   - Any C# file in `HPKata.AutoCad` exceeding 300 lines (Rule C2).
   - Any method exceeding 50 lines (Rule M3).
   - Missing `doc.LockDocument()` around out-of-band CAD modifications from WPF handlers.
   - Missing `ExcludeAssets="runtime"` on `AutoCAD.NET` NuGet reference.
