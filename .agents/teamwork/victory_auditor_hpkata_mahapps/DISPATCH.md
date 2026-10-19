# DISPATCH — Victory Auditor (HPKata MahApps.Metro & Revit Dimension Reader)

## Mission
You are the independent post-victory auditor (`teamwork_preview_victory_auditor`) for the HPKata MahApps.Metro migration and Revit element geometry upgrade.
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\victory_auditor_hpkata_mahapps\`
Sentinel conversation ID: `7e24d92c-711c-48ce-bbb8-c38e9b46a4f5`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T14:27:21Z`)
Orchestrator handoff report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_19\handoff.md`

## Audit Mandate & Verification Protocol (3 Phases)
Perform an independent, blocking 3-phase audit with ZERO shared context from the implementation swarm:

### Phase 1: Authenticity & Timeline Verification
- Verify that real work was performed and that no pre-baked fake passes, mock facades, or test circumventions were introduced.
- Verify that `HPKata.UI.csproj`, `HPKata.Revit.csproj`, and `HPKata.AutoCad.csproj` reference `MahApps.Metro` 2.4.11 and have ZERO references to `MaterialDesignThemes` or `MaterialDesignColors`.
- Verify that `grep_search` across all of `HPKata/` for `materialdesign` or `http://materialdesigninxaml.net/winfx/xaml/themes` or `xmlns:md` yields ZERO occurrences.
- Verify that all 13 dialogs in `HPKata/HPKata.UI/Views/` inherit from `mah:MetroWindow` and `: MetroWindow`.

### Phase 2: Requirements & StaticResource Parity Verification
- Verify `Theme.xaml` merges standard MahApps dictionaries (`Controls.xaml`, `Fonts.xaml`, `Dark.Blue.xaml`) and defines all 15 required style keys: `StandardDataGrid`, `StandardTextBox`, `StandardComboBox`, `PrimaryButton`, `Button.Primary`, `SecondaryButton`, `Button.Secondary`, `Card.Panel`, `GroupBox.Header`, `Text.Title`, `Text.Section`, `Text.Muted`, `Text.Bold`, `Text.Label`, and implicit styles.
- Verify `ThemeManager.cs` provides `EnsureAssembliesLoaded()` and dynamic theme switching without MaterialDesign.
- Verify `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs` exists, implements 4-tier dimension extraction ($b \times h \times L/H$ in mm), and strictly obeys Rule R5 (all feet-to-mm conversions route through `RevitUnitConverter.FeetToMm(...)` with ZERO literal `304.8` occurrences).
- Verify `FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, and `WallRebarCommands.cs` handle pre-selection, catch `OperationCanceledException` (ESC key) to safely open the dialog in manual mode without aborting, and execute rebar transactions only on `dialogResult == true`.

### Phase 3: Independent Build, Clean Code Governance & Test Execution
- Run independent build: `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`. Must produce 0 warnings, 0 errors (`TreatWarningsAsErrors=true`).
- Run independent test execution with unique isolated results directory (TestResults Isolation Directive):
  `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`
  Verify that 100% of tests pass across all test projects (`HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, `HPKata.E2E.Tests`).
- Verify Clean Code Governance across all `.cs` files in `HPKata/`:
  - Rule C2: Every `.cs` file <= 300 lines.
  - Rule M3: Every method <= 50 lines.
  - Rule Q-B1: Zero commented-out / dead code.
  - Rule Q-B2: Zero empty catch blocks without reason comments.
  - Rule R5: Literal `304.8` exists ONLY in `RevitUnitConverter.cs`.

## Verdict Reporting
Deliver your verdict (`VICTORY CONFIRMED` or `VICTORY REJECTED`) via message to Sentinel `7e24d92c-711c-48ce-bbb8-c38e9b46a4f5` with your full structured audit report.
