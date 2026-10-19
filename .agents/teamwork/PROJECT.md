# Project: HPKata Standalone C:\HPKata Suite & Installer

## Architecture
HPKata is a structural rebar detailing & fabrication automation suite for AutoCAD 2026 and Revit 2026 (`HPKata/HPKata.slnx`):
- `HPKata.Core` (`netstandard2.0;net8.0`): Pure domain models, detailing calculators, `KataInstallEnvironment`, Excel read/write (`Kata.xlsm` & `Iso_QHPlus.xls`), 86-parameter config parser (`Goi lenh.lsp`), and CAD drafting primitives. Zero host API dependency.
- `HPKata.UI` (`net8.0-windows`): `System.Windows.Window` + `MaterialDesignThemes` (v5.2.1) WPF MVVM detailing dialogs, shape diagrams, and theme management.
- `HPKata.AutoCad` (`net8.0-windows`): AutoCAD 2026 commands (`ve_dam`, `ve_cot`, `ve_vach`, `loadbeam`, `loadcolumn`, `loadwall`, `toexcel`, `setupkata`), `KataDwgTemplateImporter`, Ribbon, and LISP bridge.
- `HPKata.Revit` (`net8.0-windows`): Revit 2026 external commands (`FramingRebarCommands`, `ColumnRebarCommands`, `WallRebarCommands`, `SetupCommand`), 3D Rebar creation, Ribbon, and asset resolution.
- `HPKata.Installer` (`net8.0-windows`): Dedicated standalone installer `HPKata_Setup.exe` with WPF UI (Install / Repair / Uninstall) and headless CLI flags (`--install`, `--uninstall`, `--target-dir`).
- Test Suites (`HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, `HPKata.E2E.Tests`).

## Requirements
- R1. Standalone `C:\HPKata\` Installation Directory & Complete Asset Migration (Zero `C:\kata_pro\` Dependency).
- R2. Full KataPro Operational Parity Using `C:\HPKata\` Library (Excel + DWG + Config + Visual Diagrams).
- R3. Centralized DLL Deployment at `C:\HPKata\` & Standalone Installer (`HPKata_Setup.exe`).
