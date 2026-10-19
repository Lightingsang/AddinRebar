# Dispatch: worker_m3_beam (teamwork_preview_worker)

## Identity & Parent
- Identity: `worker_m3_beam` (teamwork_preview_worker)
- Working Directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_beam\`
- Parent: `orchestrator_3` (Conversation ID: `4db7c650-146a-4a05-a1e1-c0fb57e2733b`)

## Mandatory Inputs & Governance
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read section `## 2026-10-07T22:29:51Z`)
- Project Scope Document: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Survey Findings: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_1\handoff.md` (detailed UI integration, 2D/3D split, toggle button, reactive debouncing)
- M2 Worker Handoff: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_beam\handoff.md` (Beam3DViewport UserControl interface)
- Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`

## File Ownership (Exclusive)
You have exclusive write ownership over:
- `HPRebar/HPRebar/BeamRebar/View/BeamRebarView.xaml`
- `HPRebar/HPRebar/BeamRebar/View/BeamRebarView.xaml.cs`
- `HPRebar/HPRebar/BeamRebar/ViewModel/BeamRebarViewModel.cs`
Do NOT modify existing canvas files or Core files.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Mission: Milestone M3 — Reactive BeamRebar UI Integration
1. **`BeamRebarView.xaml` Integration**:
   - In Row 1 of the main Grid, upgrade the preview container:
     - Add a segmented toggle control or radio group: `[ 2D Canvases ]`, `[ 3D Helix Viewport ]`, `[ Split: 2D + 3D ]`.
     - In `[ 2D Canvases ]` mode: Display `BeamElevationCanvas` + `BeamSectionCanvas` (current layout).
     - In `[ 3D Helix Viewport ]` mode: Display `Beam3DViewport` spanning the full preview area.
     - In `[ Split: 2D + 3D ]` mode: Display `BeamElevationCanvas` on the left and `Beam3DViewport` on the right (shared 50/50 with a vertical splitter or fixed ratio).
     - Add a `GridSplitter` between Row 0 (editing tabs) and Row 1 (preview pane) allowing the engineer to expand/resize the preview pane height smoothly.
2. **`BeamRebarViewModel.cs`**:
   - Add observable property `PreviewMode` (e.g. enum `BeamPreviewMode { TwoD, ThreeD, Split }` or string).
   - Ensure `Beam3DViewport` binds to `Session` just like `BeamElevationCanvas`.
   - Fast reactive update: when beam dimensions, rebar count, or stirrups change in any tab, `Beam3DViewport` re-renders in <50ms without freezing the UI.
3. **Clean Code & Quality**:
   - Keep `BeamRebarView.xaml` clean and styled with MaterialDesign.
   - Keep `BeamRebarViewModel.cs` $\le 300$ lines, methods $\le 50$ lines.
   - Verify build: `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` succeeds with 0 errors.
   - Verify solution: `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` succeeds.

## Output
Write your comprehensive implementation report to:
`G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_beam\handoff.md`
and notify orchestrator_3 via `send_message`.


## 2026-10-08T00:02:32Z
You are worker_m3_beam (teamwork_preview_worker).
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_beam\
Your parent is orchestrator_3 (conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b).

Read your dispatch file first:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_beam\DISPATCH.md
Read survey report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_1\handoff.md.
Read M2 worker handoff: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_beam\handoff.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your mission is Milestone M3: Reactive BeamRebar UI Integration:
1. In BeamRebarView.xaml: Upgrade preview area (Row 1) with segmented view selector: [2D Canvases], [3D Helix Viewport], [Split: 2D + 3D], and add GridSplitter between Row 0 and Row 1 for expandable preview height.
2. In BeamRebarViewModel.cs: Add PreviewMode property and wire fast reactive binding to Beam3DViewport (<50ms debounce) so changing beam dimensions, bar counts, or stirrups immediately updates 3D view.
3. Clean Code: <= 300 lines/file, <= 50 lines/method.
4. Verify build: dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false succeeds with 0 errors.

Write your report to: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_beam\handoff.md.
When done, report to orchestrator_3 via send_message.
