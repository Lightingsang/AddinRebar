# DISPATCH — explorer_target_1

Role: Target Architecture Explorer
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_target_1

## Objective
Read ORIGINAL_REQUEST.md at `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md` and repository guidelines in `AGENTS.md`.
Explore and document the target repository structure, architectural standards, conventions, and existing patterns in `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar`.

## Tasks
1. Investigate `HPRebar.Core/`:
   - Framework (netstandard2.0), dependencies, pure math models, immutability, records, error handling.
   - What patterns exist in `HPRebar.Core` (e.g. for ColumnRebar)?
2. Investigate `HPRebar.Core.Tests/`:
   - Framework (xUnit v3 with Microsoft.Testing.Platform), test layout, test helpers, assertion style, coverage.
3. Deep dive into `HPRebar/HPRebar/Column Rebar/` (the golden reference feature folder):
   - ExternalCommand entry (`ColumnRebarCommand.cs`), Transaction/TransactionGroup handling.
   - Readers (`ColumnStackReader.cs`, etc.), Services, Creators, Validators.
   - Folder structure: `Models/`, `View/`, `View Models/`.
   - MVVM patterns: CommunityToolkit.Mvvm (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`), modal window (`ShowDialog()`).
   - DynamicResource theming (Revit Light/Dark match, `Resources/Themes/`).
4. Investigate Ribbon integration:
   - `HPRebar/HPRebar/Application.cs` (`CreateRibbon()`, panel creation, push button registration, icons).
5. Investigate Build & Packaging:
   - `HPRebar.slnx`, configurations (`Debug.R25`, `Debug.R26`), build properties (`-p:DeployAddin=false`).

## Output
Write your comprehensive analysis to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_target_1\target_arch_analysis.md` and complete `handoff.md`.
Report completion to orchestrator via send_message.
