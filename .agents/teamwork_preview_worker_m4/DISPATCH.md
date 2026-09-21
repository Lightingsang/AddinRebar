# Dispatch Assignment: Milestone 4 — Automated Test Suites & Live Verification Harness

## Role: Worker (teamwork_preview_worker)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Sibling Reference: `HPRobot/HPRobot.Mcp.Server.Tests/` and `HPCivil3d/HPCivil3d.Mcp.Server.Tests/`

---

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

---

## Write Ownership
You have exclusive write access to:
- `HPTekla/HPTekla.Mcp.Server.Tests/**` (all files and subdirectories)
- `HPTekla/tools/harness/**` (Python and PowerShell harness files)
- `.agents/teamwork_preview_worker_m4/**` (your working directory)

---

## Detailed Task Specification

### 1. `HPTekla/HPTekla.Mcp.Server.Tests/HPTekla.Mcp.Server.Tests.csproj`
- TargetFramework: `net10.0`
- LangVersion: `latest`
- Nullable: `enable`, ImplicitUsings: `enable`
- RootNamespace: `HPTekla.Mcp.Server.Tests`
- OutputType: `Exe`
- UseMicrosoftTestingPlatformRunner: `true`
- Packages:
  - `xunit.v3` (Version 3.1.0)
  - `xunit.runner.visualstudio` (Version 3.1.5)
  - `Microsoft.Bcl.AsyncInterfaces` (Version 10.0.12)
- ProjectReferences:
  - `..\HPTekla.Mcp.Server\HPTekla.Mcp.Server.csproj`
  - `..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj`
  - `..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj`
  - `..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj`
- Linked items:
  - `<Compile Include="..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs" Link="Fakes\FakeRevitExecutor.cs" />`

### 2. Test Classes in `HPTekla/HPTekla.Mcp.Server.Tests/`
Follow the exact structure and assertions from `HPRobot.Mcp.Server.Tests` / `HPCivil3d.Mcp.Server.Tests`:
1. `TeklaHostProfileTests.cs`:
   - Profile properties: HostId ("tekla"), DisplayName ("Tekla Structures"), ServerName ("HPTekla MCP"), PipeName ("hptekla-mcp-2025"), MethodPrefix ("tekla."), ExecuteToolName ("execute_tekla_code"), ContextToolName ("get_tekla_context"), ProductFolder ("HPTekla"), EnvPrefix ("HPTEKLA_MCP_"), DefaultVersion (2025), ValidVersions ([2025]), MaxTimeoutSeconds (600), ScriptImports (contains Tekla imports, Model, Geometry3d, Catalogs), Globals, etc.
   - Context tool description check.
   - Execute tool description check.
   - Builder option seeding.
   - Tool surface: 4 core tools + 8 registry tools = 12 static tools. Zero foreign host names.
   - Resources (`tekla://model/info`, `tekla://selection`) and Prompts (`tekla_query_template`, `tekla_modify_template`, `tekla_rebar_template`, `toolify_run`).
2. `SeedCatalogTests.cs`:
   - Discovers all 12 embedded seeds across 6 categories (Model, Geometry, Property, Rebar, Drawing, Export).
   - Validates `tool.json` schemas, required fields, naming conventions.
   - Validates `examples.json` (at least 2 valid examples per tool matching schema).
   - Validates `code.cs`: ends with return statement, no forbidden patterns, passes `GuardProfile.Tekla`, argument extraction matches declared properties.
   - Validates `ToolValidator.Validate`.
   - Validates 24 tools total in combined catalog (12 static + 12 seeds).
3. `SeedCompilationTests.cs`:
   - Compiles each seed's `code.cs` using Roslyn `CSharpCompilation` against Tekla 2025 assemblies found at `C:\Program Files\Tekla Structures\2025.0\bin` (or `HPTEKLA_DIR`).
   - Uses `Assert.SkipWhen(assembliesNotFound, ...)` for portability if Tekla is not installed.
   - Since Tekla 2025 is installed on this machine, all 12 seeds must compile cleanly with 0 errors!
4. `SeedExecutionTests.cs`:
   - Sets up `PipeListener` with `FakeRevitExecutor` and `RevitBridgeClient` for Tekla profile.
   - Tests `tekla.ping` round-trip.
   - Tests `get_tekla_context` returns `TeklaInfo` block and hides other host fields.
   - Tests `tekla://model/info` resource.
   - Tests `execute_tekla_code` round-trip over named pipe, timeout clamping, bridge refusal handling.

### 3. Python & PowerShell Live Verification Harness (`HPTekla/tools/harness/`)
1. `live-verify.py`:
   - Reuses `McpShared/tools/harness_common.py` (via relative path import).
   - 6 sequential test stages:
     - Stage A: Handshake & Stdio Pipe (`initialize`, `tools/list` returns 24 tools).
     - Stage B: Context & Resources (`get_tekla_context`, `tekla://model/info`, `tekla://selection`).
     - Stage C: Type Inspection (`inspect_type` on `Tekla.Structures.Model.Beam`).
     - Stage D: Read-only Seeds (`get_model_info`, `select_objects`, `list_drawings`).
     - Stage E: Dry-Run Write Verification (`create_beam`, `create_column` with `dryRun = true`).
     - Stage F: Real Mutation & Reinforcement (`create_column`, `create_rebar_group`, `get_part_properties`, `get_reinforcement_info`).
   - Generates structured JSON summary with PASS/FAIL/SKIP bookkeeping.
2. `run-live-verify.ps1`:
   - PowerShell script checking for Tekla process (`tekla.exe` or `TeklaStructures.exe`), checking named pipe, compiling solution if needed, and executing `live-verify.py`.

---

## Verification Requirements
1. Run `dotnet test HPTekla/HPTekla.Mcp.Server.Tests/HPTekla.Mcp.Server.Tests.csproj`:
   All tests must pass 100%!
2. Run `dotnet test HPTekla/HPTekla.McpBridge.Tests/HPTekla.McpBridge.Tests.csproj`:
   All 24 tests must pass 100%!
3. Run `(cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests)` and `(cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)`:
   All tests must pass 100% (zero regressions).
4. Run python verification of `live-verify.py` syntax and stage validation.
5. Write report to `.agents/teamwork_preview_worker_m4/report.md` and handoff to `.agents/teamwork_preview_worker_m4/handoff.md`.
6. Message orchestrator upon completion.

## 2026-09-21T18:54:52Z
Received dispatch assignment for Milestone 4 (Automated Test Suites & Live Verification Harness).

