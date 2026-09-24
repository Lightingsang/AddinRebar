# Context: Archify v2.16.0 Skill Integration & Interactive Architecture Diagrams

## Objective
Integrate a project-local Archify v2.16.0 skill into the HPRebar repository for development architecture documentation, create two verifiable interactive architecture diagrams (HPRebar System Architecture and Column Rebar Workflow), and link them in the existing architecture documentation.

## Working Directory
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

## Authoritative User Request
Recorded verbatim in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` under section `## 2026-09-23T23:47:49Z`.

## Requirements Summary
1. Extract and install minimal runtime of Archify v2.16.0 into `.agents/skills/archify/` and mirror to `.claude/skills/archify/`.
   - Include only: `SKILL.md`, `package.json`, `assets/template.html`, `bin/` (`archify.mjs`, `preview.mjs`, `visual-check.mjs`, `open-artifact.mjs`), `renderers/`, `schemas/`, `recipes/`, `references/`, `delta/`, and minimal test fixtures/examples required to satisfy `archify doctor`.
   - Exclude: website, demo, benchmarks, experiments, tests, DeepSeek integration (`archify-dsh`), `archify-review`.
   - Do NOT run a global `sync-agent-skills.py apply`.
   - Do NOT add Node or Archify to any `.csproj`, installer, Revit runtime, or MCP build process.
2. Create `docs/architecture/diagrams/hprebar-system.architecture.json` adhering to `architecture.schema.json`.
   - Model multi-tier boundary: `HPRebar.dll` (in-process add-in, UI/MVVM, commands, external event handlers, Nice3point SDK, R23-R27), `HPRebar.Core` (referenced library, netstandard2.0, pure math/geometry, zero Autodesk.Revit references), `Revit Process / Document` (Autodesk Revit host, API thread, element database, transactions), `HPRebar.McpBridge` (separate in-process plugin add-in on R25/R26, Roslyn compiler, pipe listener), `HPRebar.Mcp.Server` (separate net10 stdio process connecting via named pipe `hprebar-mcp-2026`).
3. Create `docs/architecture/diagrams/column-rebar.workflow.json` adhering to `workflow.schema.json`.
   - Step 1: User interaction & UI parameters (`ColumnRebarView` / `ColumnRebarViewModel`).
   - Step 2: Bar layout calculation & core rules (`HPRebar.Core`: `BarLayoutCalculator`, `SpliceCalculator`, `Tolerance`).
   - Step 3: Dispatching to Revit main thread (`ColumnRebarExternalEventHandler.RunAsync()` -> `ExternalEvent.Raise()`).
   - Step 4: Transaction orchestration (`ColumnRebarOrchestrator.Run()` opening `TransactionGroup("Column Rebar")`).
   - Step 5: Element generation (`CreateViews`, `CreateDimensions`, `RebarCreationService.Create`, `CreateTables`).
   - Step 6: Commit transaction group (`group.Assimilate()`) or rollback on error (`group.RollBack()`).
   - Step 7: [Proposed] Model QA/QC & clash check (badged with status `proposed` and title `[Đề xuất] Hậu kiểm mô hình`).
   - Step 8: [Proposed] Report & BOM/schedule export (badged with status `proposed` and title `[Đề xuất] Xuất báo cáo & thống kê`).
4. Verification & Documentation:
   - `node .agents/skills/archify/bin/archify.mjs doctor` passes with code 0 and reports `[ok]` for all check items.
   - `node .agents/skills/archify/bin/archify.mjs validate` passes on both JSON diagrams.
   - `node .agents/skills/archify/bin/archify.mjs deliver` generates `docs/architecture/diagrams/hprebar-system.architecture.html` and `docs/architecture/diagrams/column-rebar.workflow.html`.
   - Update `docs/system-architecture.md` with relative links and explanations.
   - Ensure `HPRebar.Core` remains 100% free of Autodesk API references and zero C# logic is modified.
