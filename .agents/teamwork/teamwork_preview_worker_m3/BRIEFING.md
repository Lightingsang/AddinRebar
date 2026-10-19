# BRIEFING — 2026-10-18T06:06:18Z

## Mission
Complete Milestone M3 & M4 implementation for HPKata: DWG template importer, Revit 86-parameter config sync, Visual Asset service, Standalone Installer (`HPKata_Setup.exe`), deployment configuration to `C:\HPKata\`, and comprehensive unit/integration tests with 100% build & test pass.

## 🔒 My Identity
- Archetype: implementer
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m3\
- Original parent: d6327505-50dd-47d2-9377-859bd91f34c5
- Milestone: M3 (Complete DCE Excel & DceSteel UDF Parity)
- Session 2 Parent: 41cec756-0576-4a33-970c-d780ca3d299e
- Session 2 Milestone: M3 & M4 (HPKata Standalone Implementation & Installer)

## 🔒 Key Constraints
- [constraints from dispatch message]
- Exclusive write ownership:
  - `HPDcePro/src/HPDcePro.Excel/RcGeotechUdfBridge.cs`
  - `HPDcePro/src/HPDcePro.Excel/SteelWindUdfBridge.cs`
  - `HPDcePro/src/HPDcePro.Excel/CalculationUdfBridge.cs`
- Every file strictly <= 300 lines (target <= 180 lines), methods <= 50 lines.
- 0 commented-out code, 0 unannotated catch blocks.
- Never throw unhandled exceptions from UDFs: return 0.0 for invalid input, fallback to standard materials (B25, CB400-V).
- Mandatory Integrity: No cheating, no hardcoded values or dummy implementations. Real calculations delegating to Core calculators.
- `TreatWarningsAsErrors=true`.
- Zero occurrences of `kata_pro` in production code.
- UI Framework Constraint: System.Windows.Window + MaterialDesignThemes v5.2.1. Do NOT switch to MahApps.Metro.
- Single-Node Build Rule: `dotnet build` and `dotnet test` with `--disable-build-servers -m:1`.

## Current Parent
- Conversation ID: 41cec756-0576-4a33-970c-d780ca3d299e
- Updated: 2026-10-18T06:06:18Z

## Task Summary
- **What to build**:
  1. `HPKata/HPKata.AutoCad/Interop/KataDwgTemplateImporter.cs` (<= 300 lines, methods <= 50 lines) with fallback.
  2. Wire `HPKata/HPKata.Revit/Commands/SetupCommand.cs` with 86-parameter config sync via `KataInstallEnvironment.GetConfigFilePath()`.
  3. `HPKata/HPKata.Core/Assets/KataVisualAssetService.cs` (<= 300 lines) for typed asset resolution.
  4. `HPKata/HPKata.Installer/HPKata.Installer.csproj` and `HPKata_Setup.exe` (WPF UI + headless `--install`, `--uninstall`, `--target-dir`), add to `HPKata/HPKata.slnx`, configure deployment to `C:\HPKata\`.
  5. Comprehensive tests in `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, `HPKata.Core.Tests`, and `HPKata.E2E.Tests`.
  6. Clean build, 100% tests pass, execute `HPKata_Setup.exe --install`.
- **Success criteria**: 0 errors, 0 warnings, 100% pass, genuine implementations, `C:\HPKata\` populated with assemblies and manifests.
- **Interface contracts**: `ORIGINAL_REQUEST.md` (lines 1460-1512), `PROJECT.md`.
- **Code layout**: `HPKata/` projects.

## Key Decisions Made
- Placing new classes in matching project directories: `HPKata.AutoCad/Interop/`, `HPKata.Core/Assets/`, `HPKata.Revit/Commands/`, `HPKata.Installer/`.
- Installer supports both interactive WPF and headless CLI args (`--install`, `--uninstall`, `--target-dir <path>`).

## Artifact Index
- `handoff.md` — completion report to orchestrator
- `progress.md` — progress heartbeat

## Change Tracker
- **Files modified**:
  - [TBD]
- **Build status**: Initial: 1097/1097 passed.
- **Pending issues**: None.

## Quality Status
- **Build/test result**: Initial 1097 tests pass.
- **Lint status**: 0 warnings, clean code rules enforced.
- **Tests added/modified**: [TBD]

## Loaded Skills
- None
