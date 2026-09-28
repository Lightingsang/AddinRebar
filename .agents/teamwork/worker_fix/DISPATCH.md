## 2026-09-27T16:59:07Z

# Dispatch: Worker Fix (Remediate Challenger 2 Findings)

- **Role**: teamwork_preview_worker
- **Task**: Remediate the 4 concrete findings identified by Challenger 2 in `HPRebar/HPRebar/KataRebar/`:
- **Challenger 2 Report**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_2\handoff.md`
- **Authoritative Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (under ## 2026-09-27T15:57:37Z)

Concrete Fixes:
1. `HPRebar/HPRebar/KataRebar/Service/KataBeamMatcher.cs`:
   - Add elevation and slope validation:
     Verify that all selected beam elements have consistent top elevations within tolerance ($\le 25\text{ mm}$), and that beams are horizontal ($|\text{line.Direction.Z}| \le 10^{-3}$).
     If a beam is sloped or on a different level, fail with an informative message.
2. `HPRebar/HPRebar/KataRebar/Service/KataRebarCleanupService.cs`:
   - Fix lines 44-47:
     ```csharp
     if (!string.IsNullOrWhiteSpace(beamName))
         return comment.Equals(targetComment, StringComparison.OrdinalIgnoreCase);

     return comment.StartsWith(CommentPrefix, StringComparison.OrdinalIgnoreCase);
     ```
     This prevents over-deleting other beam runs when a specific `beamName` is given.
3. `HPRebar/HPRebar/KataRebar/Service/KataRebarCreationService.cs`:
   - In `BuildCurves`, change curve distance threshold from `1e-4` to `2.0e-3` ft ($\approx 0.6\text{ mm}$) to guard against Revit's `ShortCurveTolerance`.
4. `HPRebar/HPRebar/KataRebar/Service/KataRebarOrchestrator.cs`:
   - Guard rollback in catch block:
     ```csharp
     if (group.HasStarted())
     {
         group.RollBack();
     }
     ```
5. Verification:
   - Run `dotnet test HPRebar/HPRebar.Core.Tests`
   - Run `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`
   - Verify all tests pass and solution builds with 0 errors.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.
