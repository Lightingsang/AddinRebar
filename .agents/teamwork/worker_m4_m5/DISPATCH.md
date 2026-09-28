# Dispatch: Worker M4 & M5 (Revit 3D Rebar Generation, Idempotency, MVVM UI & Ribbon)

- **Role**: teamwork_preview_worker
- **Milestones**: M4 (Revit 3D Rebar Generation & Idempotency) & M5 (WPF MVVM UI & Ribbon Integration)
- **Scope**:
  1. Implement Revit Services in `HPRebar/HPRebar/KataRebar/Service/`:
     - `KataBeamMatcher.cs`: Matches user selection in Revit (selected structural framing elements) with `KataBeamRebarSpec` by beam name and span count; extracts beam axis, origin, basis vectors (local-to-world PointMapper/KataAxisFrame).
     - `KataRebarTypeResolver.cs`: Resolves bar notations (`f18`, `d8`, `f25`) to project `RebarBarType` by diameter (~18mm +/- 0.5mm, prioritizing `BarModelType.Deformed` for bars >= 12mm) and `RebarHookType`.
     - `KataRebarCleanupService.cs`: Finds all `Rebar` elements hosted on the beam run with parameter `Comments == $"HPRebar_Kata_{beamName}"` (or containing `"HPRebar_Kata_"`), deletes them via `doc.Delete(rebarIds)` in sub-transaction 1.
     - `KataRebarCreationService.cs`: Generates 3D rebars inside Revit host beam using `Rebar.CreateFromCurves` (`#pragma warning disable CS0618`) for longitudinal/extra/side bars and `Rebar.CreateFromRebarShape` (or fallback `CreateFromCurves`) for stirrup sets. Stamps `Comments = $"HPRebar_Kata_{beamName}"` on every generated rebar.
     - `KataRebarOrchestrator.cs`: Wraps execution in atomic `TransactionGroup("Kata Rebar - {beamName}")` with `group.Assimilate()`, isolated sub-transactions with `RebarFailureHandling.Apply(t)` (`SwallowWarnings`).
     - `KataRebarExternalEventHandler.cs`: Implements `IExternalEventHandler`, wraps `ExternalEvent.Create(this)` with async thread-safe execution.
  2. Implement MVVM UI in `HPRebar/HPRebar/KataRebar/`:
     - `ViewModel/KataRebarViewModel.cs`: ObservableObject with CommunityToolkit.Mvvm:
       - Auto-detects active Excel (via `ComKataDamReader`) or allows file selection (via `ClosedXmlKataDamReader`).
       - Loads and parses `KataBeamRebarSpec`.
       - Computes `KataRebarCalculator.Calculate(spec)`.
       - Displays preview of spans, bar layers, stirrup zones, and mapped `RebarBarType`s with interactive override ComboBoxes.
       - Validates matching Revit beam selection.
       - RelayCommand `GenerateRebarCommand` executing via `KataRebarExternalEventHandler`.
     - `View/KataRebarView.xaml` & `KataRebarView.xaml.cs`: Modeless WPF window using MaterialDesign 5.3.2 tokens, DynamicResource brushes (`ThemeDark`/`ThemeLight`), `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance)`.
     - `KataRebarCommand.cs`: External command `[Transaction(TransactionMode.Manual)] [Regeneration(RegenerationOption.Manual)]` opening the modeless view.
  3. Integrate Ribbon Button in `HPRebar/HPRebar/Application.cs`:
     - Add "Kata Rebar" push button on the "Rebar" panel adjacent to "Kata Export".
     - Add vector icon in `HPRebar/Resources/Icons/RibbonIcons.cs` (e.g. `KataRebar` icon property adapting to Revit dark/light theme).
  4. Run verification:
     - `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`
     - `dotnet test HPRebar/HPRebar.Core.Tests`
     - Verify 0 errors, 0 warnings treated as errors, and 100% passing tests.

- **Reference**:
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\report.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\handoff.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\PROJECT.md`
  - Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (under ## 2026-09-27T15:57:37Z)

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.
