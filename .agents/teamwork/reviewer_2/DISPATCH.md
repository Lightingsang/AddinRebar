## 2026-09-27T16:49:34Z
# Dispatch: Reviewer 2 (Revit Integration, MVVM UI & Ribbon Reviewer)

- **Role**: teamwork_preview_reviewer
- **Task**: Independently review and verify `HPRebar/HPRebar/KataRebar/`, `Application.cs`, and `RibbonIcons.cs`.
- **Authoritative Request**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under ## 2026-09-27T15:57:37Z)
- **Scope**:
  1. Inspect `HPRebar/HPRebar/KataRebar/`:
     - Services: `KataBeamMatcher.cs`, `KataRebarTypeResolver.cs`, `KataRebarCleanupService.cs`, `KataRebarCreationService.cs`, `KataRebarOrchestrator.cs`, `KataRebarExternalEventHandler.cs`.
     - Excel: `ComKataDamReader.cs`, `ClosedXmlKataDamReader.cs`.
     - UI: `ViewModel/KataRebarViewModel.cs`, `View/KataRebarView.xaml`, `View/KataRebarView.xaml.cs`.
     - Command: `KataRebarCommand.cs`.
     - Ribbon: `Application.cs`, `RibbonIcons.cs`.
  2. Verify Requirements:
     - Smart `RebarBarType` resolution by diameter (±0.5mm, prioritizing `Deformed` for bars >= 12mm) and `RebarHookType`.
     - Idempotency: Tagging all rebars with `Comments = $"HPRebar_Kata_{beamName}"` (and partition). Deleting prior rebars on host beam before recreation.
     - Single atomic Undo: `TransactionGroup("Kata Rebar - {beamName}")` with `group.Assimilate()`, isolated sub-transactions with `RebarFailureHandling.Apply(t)`.
     - WPF MVVM: Modeless dialog, DynamicResource brushes (`Brush.*`), `MaterialThemeBridge.Attach()`, ThemeDark/ThemeLight support.
     - Ribbon button "Kata Rebar" on "Rebar" panel adjacent to "Kata Export".
  3. Run build and tests:
     - `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`
     - `dotnet test HPRebar/HPRebar.Mcp.Server.Tests`
  4. Provide explicit gate verdict in `handoff.md`: **APPROVE** or **REQUEST_CHANGES**.
