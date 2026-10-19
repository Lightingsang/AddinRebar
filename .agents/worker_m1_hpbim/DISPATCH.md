## 2026-10-05T23:46:13Z
You are teamwork_preview_worker assigned to implement Milestone 1 (M1: Solution Setup & Pure Domain Library HPBim.Core).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_hpbim

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Read the authoritative request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z)

Read the project specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md

Read the exact technical blueprints formulated by the M1 Explorers:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_1_hpbim\handoff.md (Solution configs & Geometry primitives)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_2_hpbim\handoff.md (Domain BIM Schema models & Spatial algorithms)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_3_hpbim\handoff.md (TCVN detailing rules, QC Pre-flight engine & Serialization)

Exclusive file ownership:
You exclusively own and will create:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\global.json
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\Directory.Build.props
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\README.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\HPBim.Core.csproj
- All source files in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\
  (Geometry, Models, Algorithms, Tcvn, Qc, Serialization)

Strict Governance Rules:
- Methods <= 50 lines
- Classes <= 300 lines
- Zero empty catch blocks without comment
- Zero references to Autodesk.Revit.* (HPBim.Core is pure C# netstandard2.0 + Polyfill 11.0.1)
- Nullable=enable, TreatWarningsAsErrors=true

Verification Steps:
- Build `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\HPBim.Core.csproj" -c Release`
- Verify 0 errors, 0 warnings.
- Document build outputs and write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_hpbim\handoff.md`.
- Update `progress.md` and send message to parent.
