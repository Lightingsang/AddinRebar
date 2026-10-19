# BRIEFING — 2026-10-06T02:40:00Z

## Mission
Design the complete fix strategy and architecture blueprint for Milestone 3 Iteration 2 (remediating Reviewer 2 findings in HPBim).

## 🔒 My Identity
- Archetype: explorer
- Roles: Technical Investigator & Fix Strategy Architect
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_fix_gen2
- Original parent: 18c3ad8c-1fc9-42cc-83e7-40ab9000587e
- Milestone: Milestone 3 Iteration 2 Fix Architecture

## 🔒 Key Constraints
- Read-only investigation — do NOT implement code in HPBim source tree directly (worker will do implementation)
- All files in HPBim must be strictly <= 250 lines (well under 300 line clean code ceiling)
- All methods must be strictly <= 40 lines (well under 50 line clean code ceiling)
- No dead code
- All 8 stages must be present in composite BIM generation script (dry-run & apply)
- Concurrency safety in RevitPipeClient (SemaphoreSlim)
- File workspace convention: write only to .agents/teamwork/explorer_m3_fix_gen2/

## Current Parent
- Conversation ID: 18c3ad8c-1fc9-42cc-83e7-40ab9000587e
- Updated: 2026-10-06T02:40:00Z

## Investigation State
- **Explored paths**:
  - `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.cs` (225 lines, missing Stage 7 & 8)
  - `HPBim/src/HPBim.Client/Connection/RevitPipeClient.cs` (306 lines, unsynchronized ConnectAsync)
  - `HPBim/src/HPBim.Client/Verification/RevitModelVerifier.cs` (152 lines, CompareCounts 58 lines)
  - `HPBim/src/HPBim.Client/TypeResolver/TypeResolutionRules.cs` (32 lines, unused GetDoorSymbolName / GetWindowSymbolName)
  - `HPBim/src/HPBim.Client/ScriptGenerators/Stage3StructureGenerator.cs`, `Stage4WallGenerator.cs`, `Stage5OpeningGenerator.cs`, `Stage7MepGenerator.cs`, `Stage8RebarGenerator.cs`
  - All test suites in `HPBim/tests/HPBim.Client.Tests/`
- **Key findings**:
  1. Composite script omitted Stages 7 (MEP) and 8 (Rebar), causing dry-run to not validate them.
  2. Putting all 8 stages in one file causes > 380 lines. Solved via partial class partition (4 files, each <= 120 lines, all methods <= 40 lines).
  3. `RevitPipeClient.cs` is 306 lines (> 300 limit) and lacks concurrency lock. Solved by extracting `PipeMessageReader.cs` (~105 lines) and adding `_connectLock` (`SemaphoreSlim(1, 1)`), bringing `RevitPipeClient.cs` down to ~190 lines.
  4. `CompareCounts` is 58 lines. Solved by decomposing into 4 helper methods + `ElementCounts` record struct; `CompareCounts` reduced to 7 lines.
  5. `TypeResolutionRules` has dead methods. Solved by integrating `GetDoorSymbolName` & `GetWindowSymbolName` into `Stage5OpeningGenerator.cs` and adding dedicated unit tests.
  6. Fidelity fixed in Composite: column top level set, wall top level set, openings distinguish doors and windows with sill height.
- **Unexplored areas**: None. All 5 findings mapped and solutions designed.

## Key Decisions Made
- Partition `CompositeBimGenerator` into 4 partial class files: `CompositeBimGenerator.cs`, `CompositeBimGenerator.Stages1To3.cs`, `CompositeBimGenerator.Stages4To6.cs`, `CompositeBimGenerator.Stages7To8.cs`.
- Extract `PipeMessageReader` as an internal sealed class handling stream reading, JSON parsing, cancellation, and EOF dispatch.
- Protect `ConnectAsync` and `DisconnectAsync` in `RevitPipeClient` using `_connectLock` (`SemaphoreSlim(1, 1)`).
- Decompose `CompareCounts` in `RevitModelVerifier` with `ElementCounts` struct, bringing `CompareCounts` to 7 lines and all helpers <= 22 lines.
- Integrate `TypeResolutionRules` in `Stage5OpeningGenerator` and add unit test file `TypeResolutionRulesTests.cs`.

## Artifact Index
- DISPATCH.md — instructions and requirements
- progress.md — liveness heartbeat
- BRIEFING.md — persistent working memory
- handoff.md — final complete fix blueprint
