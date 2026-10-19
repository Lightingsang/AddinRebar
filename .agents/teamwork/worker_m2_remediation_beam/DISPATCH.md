# DISPATCH — worker_m2_remediation_beam

You are worker_m2_remediation_beam (teamwork_preview_worker).
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_remediation_beam\
Your parent is orchestrator_3 (conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b).

## Mandatory Inputs to Read First:
1. ORIGINAL_REQUEST.md at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (check section ## 2026-10-07T22:29:51Z).
2. PROJECT.md at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
3. Challenger rejection handoff report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_beam_1\handoff.md.
4. Reviewer handoff report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_beam_1\handoff.md.

## MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Remediation Tasks:
1. Fix runtime XAML errors in `HPRebar/HPRebar/BeamRebar/View/Controls/Beam3DViewport.xaml`:
   - Line 42: Change `Style="{StaticResource MaterialDesignFlatButton}"` to `Style="{DynamicResource MaterialDesignFlatButton}"` (or `{DynamicResource SecondaryButton}`).
   - Line 48: Replace `Kind="CameraReticleOutline"` with a valid `PackIconKind` enum value: `Kind="CameraOutline"`.
2. Fix lifecycle symmetry in `HPRebar/HPRebar/BeamRebar/View/Controls/Beam3DViewport.xaml.cs`:
   - In `OnLoaded`, ensure that if `Session is not null`, `Attach(Session)` is safely called (with an `_isAttached` guard flag to prevent duplicate subscriptions) so that switching view modes or tabs does not leave property subscriptions detached.
3. Clean Code constraints:
   - `Beam3DViewport.xaml` <= 300 lines.
   - `Beam3DViewport.xaml.cs` <= 300 lines.
   - All methods <= 50 lines.
4. Verification:
   - Build: `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` succeeds with 0 errors.
   - Verify empirical test harness in `scratch/verify_m2_beam`: run `dotnet run -c Debug` in `scratch/verify_m2_beam` and confirm all 7/7 tests pass!
   - Run `dotnet test HPRebar/HPRebar.Core.Tests` and confirm 100% pass (1682 tests).

Write your report to: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_remediation_beam\handoff.md.
When done, report to orchestrator_3 via send_message.
