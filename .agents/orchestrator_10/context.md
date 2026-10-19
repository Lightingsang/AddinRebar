# Orchestrator 10 Context - HPBim Deliverable

Project: High-level independent deliverable HPBim/ (C# .NET, HPBim.slnx) for native 3D BIM auto-generation in Revit 2026 via Revit MCP Named Pipe
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z)
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Orchestrator Workspace: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10

## Governance & Architecture Guardrails
- Clean Code: docs/clean-code/HP_CLEAN_CODE_CORE.md (method <= 50 lines, class <= 300 lines, no empty catch without explanation, no dead code, 3 blocking runtime rules Q-B1..Q-B3).
- Architecture: docs/architecture/ARCHITECTURE.md, docs/clean-code/host-appendix/revit.md.
- Repository Layout & Dependencies: HPBim/ references McpShared/ ONLY. Zero cross-referencing between MCP folders (HPAutoCad, HPNavis, etc.).
- HPBim.Core MUST target netstandard2.0 and have ZERO references to Autodesk.Revit.*.
- Shared infrastructure: McpShared/ (Contracts, Core, Roslyn compiler, Named Pipe client/server engine).
- Target Revit host: Named Pipe \\.\pipe\hprebar-mcp-r2026 from HPRebar.McpBridge in Revit 2026.
- Benchmark reference: vn-autocad-skill-main (Python vncad logic -> port to C#), benchmark dataset nha_pho_5x18m -> NhaPho5x18mFixture.cs.

## Requirements Summary
- R1. Cấu Trúc Solution & Domain C# Thuần (HPBim.Core):
  - HPBim/HPBim.slnx, global.json (pin .NET SDK 10.0.300 / MTP runner), Directory.Build.props.
  - HPBim.Core (netstandard2.0, pure C# domain):
    - Geometry data structures ported from vncad: AxisGrid, LevelDef, WallDef, ColumnDef, BeamDef, SlabDef, OpeningDef, RoomDef, MepFixtureDef, PipeRunDef, RebarDef.
    - Spatial calculations, offset logic, door/window positioning, room polygon closure & area calculations, TCVN rebar detailing rules.
    - QC Engine: Clash detection, clearance checking, TCVN rule validation offline before dispatch to Revit.
- R2. Bộ Điều Phối & Client Giao Tiếp Revit (HPBim.Client):
  - HPBim.Client (net8.0/net10.0, references HPBim.Core and McpShared):
    - Connects directly to \\.\pipe\hprebar-mcp-r2026 of HPRebar.McpBridge in Revit 2026.
    - Orchestrates BIM creation order: Levels -> Grids -> Columns/Beams/Floors -> Walls -> Doors/Windows -> Rooms -> MEP.
    - Family/Type resolution & on-the-fly duplicate/sizing scripts (e.g. Column 220x220, Wall 110) via Roslyn scripts executed on bridge.
    - Safe execution mode (dryRun: true rollback TransactionGroup).
- R3. Ứng Dụng Dòng Lệnh HPBim CLI & Bộ Tác Tử AI (HPBim.Cli & Agents):
  - Console app HPBim.Cli (net10.0):
    - validate --input <model.json>: offline geometry, clash & TCVN check.
    - generate --input <model.json> [--dry-run]: dispatch to Revit pipe.
    - verify --input <model.json>: query Revit elements & verify match.
    - estimate --input <model.json>: export BOQ.
  - Package AI Agent instructions/roles (Architecture, Structure, MEP, QC, Orchestrator) compatible with Antigravity / Claude Code.
- R4. Kịch Bản Kiểm Thử & Xác Thực Khách Quan (xUnit / MTP Tests):
  - HPBim.Core.Tests (xUnit v3 / MTP): NhaPho5x18mFixture.cs, geometry math, space division, TCVN rebar, QC rules (runs in seconds without Revit).
  - HPBim.Client.Tests: Named Pipe round-trip and Revit element verification tests.

## Acceptance Criteria
- 100% C# (0 Python runtime).
- 100% adherence to HP_CLEAN_CODE_CORE.md.
- HPBim only references McpShared/. No circular or cross-MCP references.
- HPBim.Core has zero Autodesk.Revit.* references.
- 100% Unit tests pass via `dotnet test`.
- Live/DryRun verification with NhaPho 5x18m benchmark.
