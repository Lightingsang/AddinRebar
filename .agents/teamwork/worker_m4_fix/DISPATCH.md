# DISPATCH — worker_m4_fix: Remediation of SKILL.md Schemas & Tools

## Mission
Remediate the documentation defects in `.agents/skills/hp-mcp-grasshopper/SKILL.md` identified by `challenger_m4_2` and `reviewer_m4_2`.
Ensure 100% fidelity to production manifests (`tool.json`), server reflection (`tests/grasshopper_tools_list.json`), and C# tool signatures.

## Authoritative Inputs
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Scope & Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_14\plan.md`
3. Reviewer Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2\handoff.md`
4. Challenger Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_2\handoff.md`
5. Seed Manifests: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/`
6. Server Tools: `HPGrasshopper/HPGrasshopper.Mcp.Server/Tools/`

## Exact Remediation Steps in `.agents/skills/hp-mcp-grasshopper/SKILL.md`

1. **Frontmatter & Overview**:
   - Update description and overview to list the actual 8 registry tools:
     `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
   - Update total tool count: 27 tools (5 native/script core, 1 context, 1 cancel_execution, 8 registry meta tools, 12 embedded seeds).

2. **Section 4.1 Core Tools Table**:
   - `get_grasshopper_context`: no parameters.
   - `cancel_execution`: cancels running execution / resets busy flag.
   - `read_grasshopper_canvas`: `includeValues (bool, default false)`, `selectedOnly (bool, default false)`, `categoryFilter (string?)`.
   - `batch_edit_grasshopper_canvas`: `componentsToAdd (array)`, `componentsToRemove (string[])`, `wiresToConnect (array)`, `wiresToDisconnect (array)`, `dryRun (bool)`.
   - `run_grasshopper_player`: `filePath (string, required)`, `inputs (object?)`, `outputs (string[]?)`, `headless (bool, default true)`.
   - `export_grasshopper_canvas_image`: `mode ("FullCanvas"/"Viewport"/"Selection", default "FullCanvas")`, `zoom (number, default 1.0)`, `outputPath (string?)`, `returnBase64 (bool, default true)`.
   - `execute_grasshopper_code`: `code (string, required)`, `transaction ("auto"/"manual"/"none")`, `dryRun (bool)`, `timeoutSeconds (int, 5–600)`.
   - Clarify in notes that `targetDocTitle`, `targetDocPath`, and `targetDefId` are bridge-level document identity validators checked during execution and can be passed in `execute_grasshopper_code`'s `args` dictionary or raw Bridge JSON-RPC.

3. **Section 4.2 Embedded Seed Tools Table**:
   Correct the parameter schemas for all 12 seeds to match their `tool.json` manifests verbatim:
   - `create_levels_grids`: `elevations (number[], required), gridSpacings (number[], required), gridLength (number, default 30.0)`
   - `generate_columns_beams`: `columnPoints (object[], required), beamLines (object[], required), columnHeight (number, default 3.6)`
   - `create_floors_slabs`: `boundaryLoops (array[], required), elevation (number, default 0.0), structural (boolean, default true)`
   - `generate_rebar_curves`: `curvePoints (array[], required), barDiameterMm (number, default 16.0), diameterToleranceMm (number, default 2.0)`
   - `generate_stirrup_distribution`: `beamElementId (integer, required), denseSpacingMm (number, default 100.0), sparseSpacingMm (number, default 200.0), supportZoneRatio (number, default 0.25), stirrupDiameterMm (number, default 8.0)`
   - `create_wall_openings`: `wallElementId (integer, required), openings (object[], required)`
   - `curtain_grid_layout`: `curtainWallElementId (integer, required), uSpacingMeters (number, default 1.5), vSpacingMeters (number, default 3.0), layoutRule (string, default "FixedDistance")`
   - `directshape_from_geometry`: `shapeType ("Solid"/"Mesh", default "Solid"), category (string, default "OST_GenericModel"), boundingDimensions ({width, length, height}, required)`
   - `query_revit_geometry`: `category (string, default "OST_StructuralFraming"), includeDetailGeometry (boolean, default false)`
   - `evaluate_solar_radiation`: `sunVector (object, required), targetSurfaceNormals (object[], required), directRadiationWatts (number, default 1000.0)`
   - `facade_panelization`: `surfaceWidthMeters (number, default 30.0), surfaceHeightMeters (number, default 15.0), uDivisions (integer, required), vDivisions (integer, required), panelFamilyName (string, default "Adaptive-Quad-Panel")`
   - `export_model_to_rhino`: `categories (string[]), targetRhinoUnits (string, default "Meters"), groupByLevel (boolean, default true)`

4. **Section 4.3 Registry Meta Tools Table**:
   Document the actual 8 registry tools:
   - `search_tools`: query by keyword, category, tag.
   - `get_tool`: view tool metadata, schema, and optionally C# code (`includeCode=true`).
   - `run_tool`: execute a published registry tool with arguments.
   - `get_run`: view previous tool execution result and logs.
   - `propose_tool`: propose a new C# tool script (runs ADR-0007 quality walker).
   - `test_tool`: test a proposed tool in sandbox with sample args.
   - `publish_tool`: publish a verified tool to the active library.
   - `manage_tool`: deprecate, retire, or delete a tool.

## Governance & Safety
- ZERO modifications in `McpShared/`.
- Run tests:
  - `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false`
  - `dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false`
  - `dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false`
- Write handoff report to `handoff.md` and send message to orchestrator (`be638180-e3a9-46c4-834a-a5beaaf11a17`).


## 2026-10-12T18:44:16Z
[Message] timestamp=2026-10-12T18:44:16Z sender=be638180-e3a9-46c4-834a-a5beaaf11a17 priority=MESSAGE_PRIORITY_HIGH
Remediate `.agents/skills/hp-mcp-grasshopper/SKILL.md`:
1. In description frontmatter and Section 4.3: replace the 4 phantom registry tools with the actual 8 registry tools:
   `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
2. In Section 4.1: add `cancel_execution` as core execution cancellation tool, making total exactly 27 tools. Fix `read_grasshopper_canvas` parameters to `includeValues`, `selectedOnly`, `categoryFilter`. Fix `export_grasshopper_canvas_image` mode values to `FullCanvas`/`Viewport`/`Selection`.
3. In Section 4.2: update parameter schemas for all 12 embedded seeds to match `tool.json` manifests verbatim (`elevations`/`gridSpacings` for create_levels_grids; `columnPoints`/`beamLines` for generate_columns_beams; `beamElementId`/`denseSpacingMm`/`sparseSpacingMm`/`supportZoneRatio`/`stirrupDiameterMm` for generate_stirrup_distribution; `boundaryLoops`/`elevation` for create_floors_slabs; `wallElementId`/`openings` for create_wall_openings; `curtainWallElementId`/`uSpacingMeters`/`vSpacingMeters`/`layoutRule` for curtain_grid_layout; `sunVector`/`targetSurfaceNormals`/`directRadiationWatts` for evaluate_solar_radiation; `surfaceWidthMeters`/`surfaceHeightMeters`/`uDivisions`/`vDivisions`/`panelFamilyName` for facade_panelization; `categories`/`targetRhinoUnits`/`groupByLevel` for export_model_to_rhino; `curvePoints`/`barDiameterMm`/`diameterToleranceMm` for generate_rebar_curves).
4. Run all test suites: `HPGrasshopper.McpBridge.Tests`, `HPGrasshopper.Mcp.Server.Tests`, `HPGrasshopper.E2E.Tests`.
5. Check that McpShared remains 100% untouched.
