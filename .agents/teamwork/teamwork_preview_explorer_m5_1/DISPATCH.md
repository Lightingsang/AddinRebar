# DISPATCH — teamwork_preview_explorer_m5_1

## Task Assignment
**Role**: Server Tests Architecture Explorer (teamwork_preview_explorer)
**Mission**: Investigate sibling test suites (`HPAutoCad.Mcp.Server.Tests`, `HPEtabs.Mcp.Server.Tests`, `HPNavis.Mcp.Server.Tests`, `HPCivil3d.Mcp.Server.Tests`) and design the full test architecture for `HPDynamo.Mcp.Server.Tests`.

## Required Reading
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Project Specifications: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
3. Sibling projects:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server.Tests\`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPEtabs\HPEtabs.Mcp.Server.Tests\`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPNavis\HPNavis.Mcp.Server.Tests\`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\`

## Scope of Investigation
1. **Test Project Configuration**: Target framework (`net10.0`), SDK, package references (xUnit v3, Microsoft.Testing.Platform, Microsoft.NET.Test.Sdk), project references (only `HPDynamo.Mcp.Server` and `HPRebar.Mcp.Server.Core`).
2. **Test Suites to Design**:
   - `ToolSurfaceTests`: Verify exact 24-tool surface count, verify 4 core tools present, verify 8 meta tools present, verify 12 seeds present, verify `inspect_type` is suppressed.
   - `SeedResourceTests`: Verify all 36 manifest resources exist in `HPDynamo.Mcp.Server.dll`, verify no seed types compiled statically, verify JSON schemas and examples are valid.
   - `ScriptQualityTests`: Verify that all 12 seeds pass `ScriptQuality.Find` with 0 errors and 0 warnings (no empty catch, no commented code, lines <= 300, methods <= 50).
   - `ScriptGuardTests`: Verify all 12 seeds pass `ScriptGuard.Check` with `GuardProfile.Dynamo` (0 security violations).
   - `RegistryLifecycleTests`: Verify dynamic tool proposal, publishing, retrieval, and search using `DynamicToolRegistrar`.
3. **Clean Code Rules**: Test files <= 300 lines, test methods <= 50 lines.

## Deliverable
Write your comprehensive architectural report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_1\handoff.md`
When complete, send a message back to orchestrator_4.

## 2026-10-08T11:44:48Z
You are teamwork_preview_explorer_m5_1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_1\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_1\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md

Investigate sibling server test projects (e.g., HPAutoCad.Mcp.Server.Tests, HPEtabs.Mcp.Server.Tests, HPNavis.Mcp.Server.Tests, HPCivil3d.Mcp.Server.Tests) to design the complete test architecture for HPDynamo.Mcp.Server.Tests:
1. Target framework (net10.0), project structure, references (only HPDynamo.Mcp.Server and McpShared).
2. Test suite design:
   - ToolSurfaceTests: 24 tools invariant, 4 core tools, 8 meta tools, 12 seeds, inspect_type suppression.
   - SeedManifestTests: 36 embedded resources, 0 compiled seed script classes, schema and example validation.
   - ScriptQualityTests: ScriptQuality.Find validation on all 12 seeds (0 errors, 0 warnings, rules C2, M3, Q-B1, Q-B2).
   - ScriptGuardTests: ScriptGuard.Check against GuardProfile.Dynamo (0 security violations).
   - RegistryLifecycleTests: Dynamic tool propose, publish, test, manage, get, and search.
3. Clean Code compliance for test classes and methods.
Write your comprehensive architectural blueprint to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_1\handoff.md
When finished, send a message back to orchestrator_4.

