## 2026-10-10T15:44:34Z
You are the Remediation Worker (worker_remediation_1) for the HPRebar MaterialDesign Migration project.
Your assigned working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_remediation_1\

MANDATORY READING:
You MUST read the authoritative request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project scope at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_11\PROJECT.md
Read the reports from the 3 Explorers:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_remediation_1\handoff.md (and analysis.md)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_remediation_2\handoff.md (and analysis.md)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_remediation_3\handoff.md (and analysis.md)
Read the domain skill at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\revit-xaml-styles\SKILL.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A forensic auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

EXCLUSIVE WRITE OWNERSHIP:
You own and can modify ONLY these two files:
- HPRebar/HPRebar/JoinElements/Service/JoinElementService.cs
- HPRebar/HPRebar/JoinElements/View/JoinElementView.xaml

REMEDIATION TASKS:

Task 1: Resolve Defect 1 in `HPRebar/HPRebar/JoinElements/Service/JoinElementService.cs`:
In lines 370–374, replace `NormalizePair`:
```csharp
    private static (ElementId, ElementId) NormalizePair(ElementId id1, ElementId id2)
    {
        // Multi-version: ElementId comparison for canonical pair ordering (Value on R24+, IntegerValue on R23)
#if REVIT2024_OR_GREATER
        return id1.Value < id2.Value ? (id1, id2) : (id2, id1);
#else
        return id1.IntegerValue < id2.IntegerValue ? (id1, id2) : (id2, id1);
#endif
    }
```
Tag with `// Multi-version: ElementId comparison` per AGENTS.md rules.

Task 2: Resolve Defect 2 in `HPRebar/HPRebar/JoinElements/View/JoinElementView.xaml`:
1. In `Window.Resources` (lines 30–70), completely remove the 5 deprecated custom styles:
   - `SectionHeaderStyle`
   - `CardBorderStyle`
   - `BadgeBorderStyle`
   - `DataGridHeaderStyle`
   - `DataGridRowStyle`
2. Update all usage sites in the body of `JoinElementView.xaml`:
   - Replace `CardBorderStyle` with `{DynamicResource Card}`
   - Replace `BadgeBorderStyle` with `{DynamicResource Tag}` (or `{DynamicResource Badge}`)
   - Replace `SectionHeaderStyle` with `{DynamicResource Subheading}`
   - Remove explicit `ColumnHeaderStyle="{StaticResource DataGridHeaderStyle}"` and `RowStyle="{StaticResource DataGridRowStyle}"` from the `<DataGrid>` (lines 244-245) so the implicit `MaterialDesignDataGrid` style from `Theme.xaml` governs the DataGrid.
   - Standardize toolbar buttons (Select All, Clear, Reverse) to `{DynamicResource LinkButton}`.
   - Standardize footer buttons (Close -> `{DynamicResource SecondaryButton}`, Run Auto Join -> `{DynamicResource PrimaryButton}`).
3. Invariant Preservation:
   - Preserve `<BooleanToVisibilityConverter x:Key="BoolToVis"/>`.
   - Preserve 100% of DataContext bindings, ViewModels, commands (`RunAutoJoinCommand`, `SelectAllPairsCommand`, `CloseCommand`), and Vietnamese text.

Task 3: Build & Test Verification:
Run each command and document exit code & output in your report:
1. `dotnet build HPRebar/HPRebar.csproj -c Debug.R25 -p:DeployAddin=false` (CRITICAL: Must pass with 0 errors)
2. `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` (Must pass with 0 errors)
3. `dotnet build HPRebar/HPRebar.csproj -c Debug.R27 -p:DeployAddin=false` (Must pass with 0 errors)
4. `dotnet build HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R25 -p:DeployAddin=false` (Must pass with 0 errors)
5. `dotnet build HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R26 -p:DeployAddin=false` (Must pass with 0 errors)
6. `dotnet test HPRebar.Core.Tests`
7. `dotnet test HPRebar.Mcp.Server.Tests`
8. `dotnet test HPRebar.Core.Tests -- --filter-class *MaterialDesignMigrationE2ETests*`
9. `dotnet test HPRebar.Core.Tests -- --filter-class *ThemeTokenCoverageTests*`

OUTPUT REQUIREMENTS:
Write `changes.md` (exact diffs with rationale) and `handoff.md` (5-component hard handoff) in your working directory:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_remediation_1\`
When complete, notify orchestrator_11 (conversation ID: 6b50efdd-bc13-44c7-a3d0-de935480c02b) via send_message.
