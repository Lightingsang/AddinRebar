# BRIEFING — 2026-10-17T16:05:00Z

## Mission
Implement Milestone M4: Comprehensive STA Unit Tests & StaticResource Parity in HPKata.UI.Tests.

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_1
- Original parent: 6ca2dcc3-e478-4dfe-bcd3-44e77f9ffa52
- Milestone: M4 (Comprehensive STA Unit Tests & StaticResource Parity)

## 🔒 Key Constraints
- File size <= 300 lines (Rule C2)
- Method size <= 50 lines (Rule M3)
- 0 warnings (TreatWarningsAsErrors=true)
- 0 dead code (Rule Q-B1), 0 unannotated catches (Rule Q-B2)
- Exclusive write ownership: HPKata/HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs, HPKata/HPKata.UI.Tests/Views/StaticResourceParityScannerTests.cs
- Use RunOnSta with new Application if Application.Current == null
- Assert window is MahApps.Metro.Controls.MetroWindow
- Test all 13 views under Dark and Light themes
- Verify 100% of {StaticResource <Key>} references resolve cleanly
- Verification command: dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1

## Current Parent
- Conversation ID: 6ca2dcc3-e478-4dfe-bcd3-44e77f9ffa52
- Updated: not yet

## Task Summary
- **What to build**: STA unit tests for all 13 MetroWindow views in HPKata.UI and automated StaticResource parity scanner in HPKata.UI.Tests.
- **Success criteria**: 100% pass on dotnet test, 0 warnings, genuine implementations (no dummy/facade).
- **Interface contracts**: PROJECT.md, ORIGINAL_REQUEST.md
- **Code layout**: HPKata/HPKata.UI.Tests/Views/

## Key Decisions Made
- Separated tests into `AllWindowsXamlInstantiationTests.cs` (159 lines) and `StaticResourceParityScannerTests.cs` (220 lines) to satisfy Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).
- Created a robust `GetViewsDirectory()` resolver in `StaticResourceParityScannerTests.cs` that dynamically searches parent directories from `AppContext.BaseDirectory` so that test execution is independent of the working directory.
- Implemented theory-based per-view parity tests (`StaticResourceParity_PerView_AllKeysResolveCleanly`) and comprehensive all-views scan (`StaticResourceParity_ComprehensiveScan_ZeroUnresolvedKeysAcrossAllViews`).

## Artifact Index
- `HPKata/HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs` — STA instantiation, parameterless constructors, Dark/Light theme testing, modal/modeless closing
- `HPKata/HPKata.UI.Tests/Views/StaticResourceParityScannerTests.cs` — 100% StaticResource parity verification, local key extraction, forbidden token elimination
- `.agents/teamwork/worker_m4_1/progress.md` — Progress tracker
- `.agents/teamwork/worker_m4_1/handoff.md` — 5-component handoff report

## Change Tracker
- **Files modified**:
  - `HPKata/HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs`: Updated to test all 13 MetroWindow views on STA thread with Dark/Light themes and safe closing.
  - `HPKata/HPKata.UI.Tests/Views/StaticResourceParityScannerTests.cs`: New test suite for automated StaticResource resolution parity across all 13 view XAML files.
- **Build status**: PASS (0 warnings, 0 errors).
- **Pending issues**: None.

## Quality Status
- **Build/test result**: All 1,046 tests passed (167 in HPKata.UI.Tests, 540 in HPKata.Core.Tests, 160 in HPKata.E2E.Tests, 104 in HPKata.Revit.Tests, 75 in HPKata.AutoCad.Tests).
- **Lint status**: 0 violations.
- **Tests added/modified**: 52 test cases added/refined in `HPKata.UI.Tests/Views/`.

## Loaded Skills
- None
