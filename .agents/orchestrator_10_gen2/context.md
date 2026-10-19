# Orchestrator 10 Generation 2 Context - HPBim Deliverable

Project: High-level independent deliverable HPBim/ (C# .NET, HPBim.slnx) for native 3D BIM auto-generation in Revit 2026 via Revit MCP Named Pipe
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z and ## 2026-10-06T00:48:56Z)
Predecessor Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\handoff.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Orchestrator Workspace: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2

## Current State Inherited from Generation 1:
- Phase 0 (Survey): Completed. Full architecture and 17 features documented in `.agents/orchestrator_10/PROJECT.md`.
- Milestone 1 (Solution & HPBim.Core Domain): 31 source files created across Geometry, Models, Algorithms, Tcvn, Qc, and Serialization in `HPBim/src/HPBim.Core`. Builds cleanly with 0 errors, 0 warnings.
- Milestone 1 Audit Finding to Fix Immediately:
  * In `HPBim/src/HPBim.Core/Qc/QcEngine.cs`, `QcOptions` was defined as `public readonly record struct QcOptions(bool CheckClashes = true, ...);`. When `Validate(model, options = null)` runs `options ?? new QcOptions()`, the C# parameterless struct constructor zeroes all fields to `false`, causing all checks to be bypassed!
  * Fix: Convert `QcOptions` to a `sealed record class` or add parameterless constructor `public QcOptions() : this(true, true, true) {}`, and ensure `options ?? new QcOptions(true, true, true)` is used in `Validate()`. Run verification tests.
- After M1 Gate Closure:
  * Milestone 2 (M2): Port `nha_pho_5x18m` benchmark dataset into `NhaPho5x18mFixture.cs`, implement `HPBim.Core.Tests` (xUnit v3 / MTP runner). 100% green tests via `dotnet test`.
  * Milestone 3 (M3): Implement `HPBim.Client` connecting to `\\.\pipe\hprebar-mcp-r2026`, 8-stage BIM generation, dynamic type duplication, dryRun rollback, and `HPBim.Client.Tests`.
  * Milestone 4 (M4): Implement `HPBim.Cli` console app (validate, generate, verify, estimate) and package AI Agent roles/prompts.
  * Milestone 5 (M5): Full verification, benchmark validation, and final victory claim.
