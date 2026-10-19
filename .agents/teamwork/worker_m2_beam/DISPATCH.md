# Dispatch: worker_m2_beam (teamwork_preview_worker)

## Identity & Parent
- Identity: `worker_m2_beam` (teamwork_preview_worker)
- Working Directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_beam\`
- Parent: `orchestrator_3` (Conversation ID: `4db7c650-146a-4a05-a1e1-c0fb57e2733b`)

## Mandatory Inputs & Governance
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read section `## 2026-10-07T22:29:51Z`)
- Project Scope Document: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Survey Findings: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_1\handoff.md` (detailed 3D Viewport architecture, materials, mesh builders, performance batching)
- Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`

## File Ownership (Exclusive)
You have exclusive write ownership over:
- `HPRebar/HPRebar/BeamRebar/View/Controls/Beam3DViewport.xaml`
- `HPRebar/HPRebar/BeamRebar/View/Controls/Beam3DViewport.xaml.cs`
- `HPRebar/HPRebar/BeamRebar/View/Controls/Beam3DMaterials.cs`
- `HPRebar/HPRebar/BeamRebar/View/Controls/Beam3DConcreteMeshBuilder.cs`
- `HPRebar/HPRebar/BeamRebar/View/Controls/Beam3DRebarMeshBuilder.cs`
- `HPRebar/HPRebar/BeamRebar/View/Controls/Beam3DSceneBuilder.cs`
Do NOT modify existing 2D canvas files or Core files.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Mission: Milestone M2 — Beam3DViewport Component
Build the interactive, real-time 3D beam viewport using `HelixToolkit.Wpf`:
1. **`Beam3DMaterials.cs`**:
   - Provide frozen WPF `Material` instances:
     - Semi-transparent concrete: `#59B0C4DE` (LightSteelBlue, alpha ~35%) with specular highlights.
     - Top main longitudinal bars: `#E5484D` (Vivid Red / Crimson).
     - Bottom main longitudinal bars: `#1E88E5` (Vivid Blue / DodgerBlue).
     - Additional top bars: `#FF7043` (Coral/Salmon).
     - Additional bottom bars: `#29B6F6` (Cyan/LightBlue).
     - Side/skin bars: `#43A047` (Forest Green).
     - Cross-ties: `#26A69A` (Teal).
     - Stirrups: `#FFB300` (Amber/Gold).
     - Support column stubs: `#809E9E9E` (Grey, semi-opaque).
2. **`Beam3DConcreteMeshBuilder.cs`**:
   - Builds semi-transparent concrete box meshes for each span in `BeamContinuousStack` ($b \times h \times L$) and support columns.
   - Note: In HelixToolkit 3.x, `MeshBuilder` resides in `HelixToolkit.Geometry`.
3. **`Beam3DRebarMeshBuilder.cs`**:
   - Converts 3D polylines (`BarPolyline` / `Polyline3` from `HPRebar.Core.BeamRebar.Calculators`) into cylindrical 3D tube meshes (`meshBuilder.AddTube(...)` with 8-10 radial divisions).
   - Generates stirrups, top main bars, bottom main bars, additional bars, side bars, and cross-ties.
4. **`Beam3DSceneBuilder.cs`**:
   - Batches geometry into 7 frozen `GeometryModel3D` instances inside a single `Model3DGroup` (<3ms CPU build time, 60+ FPS):
     - Model 1: Concrete Boxes
     - Model 2: Top Main Bars
     - Model 3: Bottom Main Bars
     - Model 4: Additional Bars
     - Model 5: Side Bars & Ties
     - Model 6: Stirrups
     - Model 7: Supports
5. **`Beam3DViewport.xaml` & `Beam3DViewport.xaml.cs`**:
   - UserControl encapsulating `HelixViewport3D` with:
     - `ModelUpDirection="0,0,1"` (matches HPRebar.Core: X=length, Y=width, Z=height).
     - `DefaultLights`, `GridLinesVisual3D`, `ViewCube`.
     - Mouse bindings: Orbit (right click), Pan (middle click), Zoom (wheel).
     - Debounced reactive update (<50ms) when input properties or session change.
     - Reset Camera command / button.
6. **Clean Code & Quality**:
   - Every file $\le 300$ lines, every method $\le 50$ lines.
   - 0 errors, 0 warnings.
   - Verify build: `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` succeeds.

## Output
Write your comprehensive implementation and verification report to:
`G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_beam\handoff.md`
and notify orchestrator_3 via `send_message`.


## 2026-10-07T23:40:44Z
[Message] timestamp=2026-10-07T23:40:44Z sender=4db7c650-146a-4a05-a1e1-c0fb57e2733b priority=MESSAGE_PRIORITY_HIGH content=You are worker_m2_beam (teamwork_preview_worker).
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_beam\
Your parent is orchestrator_3 (conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b).
Mission: Milestone M2: Beam3DViewport Component.
