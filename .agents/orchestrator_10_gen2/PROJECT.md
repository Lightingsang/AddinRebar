# Project: HPBim Deliverable

## Architecture

HPBim is an independent, high-level C# .NET deliverable (`HPBim/HPBim.slnx`) that automates the generation of native 3D BIM models (Architecture, Structure, MEP) in Revit 2026 via the existing Revit MCP Named Pipe infrastructure (`\\.\pipe\hprebar-mcp-r2026`).

```
┌────────────────────────────────────────────────────────────────────────┐
│                              HPBim.Cli                                 │
│  Commands: validate, generate [--dry-run], verify, estimate            │
│  Packaged AI Agent Roles (kien-truc-su, ky-su-ket-cau, etc.)           │
└───────────────────┬────────────────────────────────┬───────────────────┘
                    │                                │
                    ▼                                ▼
┌───────────────────────────────────────┐  ┌─────────────────────────────┐
│              HPBim.Core               │  │        HPBim.Client         │
│  - netstandard2.0 + Polyfill 11.0.1   │  │  - net8.0;net10.0           │
│  - 0 Revit references                 │  │  - Named Pipe Client        │
│  - Geometry Primitives & Math         │  │  - Stage 1-8 BIM Builders   │
│  - BIM Schema DTOs (vncad port)       │  │  - Dynamic Type Duplication │
│  - TCVN 5574:2018 Rules & Weights     │  │  - dryRun Rollback Engine   │
│  - Offline Pre-flight QC Engine       │  │  - Model Verification       │
└───────────────────▲───────────────────┘  └──────────────┬──────────────┘
                    │                                     │
                    │                                     ▼
┌───────────────────┴───────────────────┐  ┌─────────────────────────────┐
│           HPBim.Core.Tests            │  │      McpShared / Pipe       │
│  - net10.0 / xUnit v3 / MTP           │  │  \\.\pipe\hprebar-mcp-r2026 │
│  - NhaPho5x18mFixture (Ground Truth)  │  │  HPRebar.McpBridge (Revit)  │
│  - 100% green tests in seconds        │  │  Roslyn Script Execution    │
└───────────────────────────────────────┘  └─────────────────────────────┘
```

## Feature Inventory

Every feature from the Survey phase is enumerated below with its assigned milestone:

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Solution & SDK Setup | `HPBim.slnx`, `global.json` (SDK 10.0.300 + MTP), `Directory.Build.props` with strict clean code rules | M1 | Survey |
| 2 | Pure Geometry Core | `Point2D`, `Point3D`, `Vector2D`, `Polygon2D`, `BoundingBox2D`, `LiangBarskyClipping` | M1 | Survey |
| 3 | Domain BIM Schema DTOs | Port of vncad models: `AxisGrid`, `LevelDef`, `ColumnDef`, `WallDef`, `OpeningDef`, `BeamDef`, `SlabDef`, `RoomDef`, `MepFixtureDef`, `PipeRunDef`, `RebarDef` | M1 | Survey |
| 4 | Spatial Algorithms | Wall span segmentation around openings, normal offset math, Shoelace room polygon area calculation | M1 | Survey |
| 5 | TCVN 5574:2018 Detailing Rules | Anchorage ($30D$), lap ($40D$), hook ($12D$), stirrup density ($a100/a200$), concrete covers ($15..50$mm), unit weights ($W = D^2/162.2 \times L$) | M1 | Survey |
| 6 | Offline Pre-flight QC Engine | Clash detection (columns vs openings, beams vs slabs), text/line clipping, clearance rules, spatial grid bucketing | M1 | Survey |
| 7 | NhaPho5x18m Ground-Truth Fixture | Complete 5-level townhouse fixture: 5 levels, 7 grids, 10 columns 220x220, 26 walls, openings, rooms, beams, slabs, MEP, 7966.5 kg rebar totals | M2 | Survey |
| 8 | Core Unit Test Suite | Comprehensive xUnit v3 / MTP suite in `HPBim.Core.Tests` running in seconds without Revit (100% pass) | M2 | Survey |
| 9 | Named Pipe Transport Client | Direct connection to `\\.\pipe\hprebar-mcp-r2026` via `NdjsonPipeTransport` and JSON-RPC 2.0 without intermediary server | M3 | Survey |
| 10 | 8-Stage BIM Creation Pipeline | Ordered script generation: Levels -> Grids -> Columns/Beams/Floors -> Walls -> Doors/Windows -> Rooms -> MEP | M3 | Survey |
| 11 | Dynamic Family/Type Resolution | On-the-fly detection and duplication of missing types (`WallType` 110/220, `FamilySymbol` C220x220, doors/windows) and symbol activation | M3 | Survey |
| 12 | Safe Execution & dryRun | Non-destructive dry-run rollback via outer `TransactionGroup.RollBack()` and warning auto-dismissal | M3 | Survey |
| 13 | Revit Model Verification | Querying Revit elements via `FilteredElementCollector` to verify 1:1 count and coordinates against input schema | M3 | Survey |
| 14 | Client Integration Tests | Integration tests in `HPBim.Client.Tests` validating pipe framing, payload serialization, and round-trip logic | M3 | Survey |
| 15 | HPBim.Cli Console Executable | Multi-command CLI tool (`validate`, `generate`, `verify`, `estimate`) targeting `net10.0` | M4 | Survey |
| 16 | AI Agent Roles Packaging | Packaging 6 Vietnamese construction AI agent prompts in `HPBim/agents/` (kien-truc-su, ky-su-ket-cau, ky-su-co-dien, kiem-soat-ban-ve, ky-su-du-toan, dieu-phoi-bim) | M4 | Survey |
| 17 | Final Verification & Audit | End-to-end dry-run and live verification of NhaPho 5x18m benchmark, test coverage pass, and Clean Code checklist audit | M5 | Survey |

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Solution & Core Domain | `HPBim.slnx`, `Directory.Build.props`, `global.json`, `HPBim.Core` (`netstandard2.0`) with domain DTOs, geometry math, TCVN detailing rules, offline QC engine | none | **DONE** |
| M2 | Benchmark & Core Tests | `HPBim.Core.Tests` (`net10.0`, xUnit v3 / MTP), `NhaPho5x18mFixture.cs`, 100% green tests for geometry, TCVN rules, and QC clash engine | M1 | **DONE** |
| M3 | Revit Client & Pipe Engine | `HPBim.Client` (`net8.0;net10.0`), Named Pipe connection to `\\.\pipe\hprebar-mcp-r2026`, 8-stage BIM generation scripts, dynamic type synthesis, dryRun rollback, `HPBim.Client.Tests` | M1, M2 | **DONE** |
| M4 | CLI & Agent Packaging | `HPBim.Cli` (`net10.0`) with `validate`, `generate`, `verify`, `estimate` commands; 6 packaged AI agent roles in `HPBim/agents/` | M1, M2, M3 | **DONE** |
| M5 | Full Verification & Audit | End-to-end verification of NhaPho 5x18m benchmark, build and test verification, Clean Code audit against `HP_CLEAN_CODE_CORE.md` | M1, M2, M3, M4 | **DONE** |

## Interface Contracts

### `HPBim.Core` ↔ `HPBim.Client`
- **Models**: `BimModel`, `LevelDef`, `AxisGrid`, `ColumnDef`, `WallDef`, `OpeningDef`, `BeamDef`, `SlabDef`, `RoomDef`, `MepFixtureDef`, `PipeRunDef`, `RebarDef`.
- **Validation**: `QcEngine.Validate(BimModel model, QcOptions? options = null) -> QcReport { IsValid, Errors, Warnings, Clashes }`.
- **Serialization**: `BimModel.FromJson(string json) -> BimModel`, `BimModel.ToJson() -> string`.

### `HPBim.Client` ↔ Revit MCP (`HPRebar.McpBridge`)
- **Transport**: Windows Named Pipe `\\.\pipe\hprebar-mcp-r2026`, NDJSON framing, JSON-RPC 2.0.
- **RPC Request**: `ExecuteRequest(string Code, string Transaction = "auto", bool DryRun = false, int TimeoutSeconds = 120, string? Label = null, JsonElement? Args = null)`.
- **RPC Response**: `ExecuteResult(bool IsError, JsonElement? Value, string? Message, List<string>? Logs, int Changed, bool RolledBack, bool TimedOut, long DurationMs)`.
- **Globals in Script**: `doc`, `uidoc`, `app`, `uiapp`, `ct`, `log`, `progress`, `args`.

### `HPBim.Cli` ↔ `HPBim.Core` & `HPBim.Client`
- **Command CLI Syntax**:
  * `hpbim validate --input <path>`: Runs offline `QcEngine.Validate()`.
  * `hpbim generate --input <path> [--dry-run]`: Executes 8-stage BIM creation over named pipe.
  * `hpbim verify --input <path>`: Runs read-only Revit verification script to match element counts.
  * `hpbim estimate --input <path> [--output <path>]`: Computes 4-part BOQ and exports report.

## Code Layout

```
HPBim/
├── HPBim.slnx
├── global.json
├── Directory.Build.props
├── README.md
├── agents/
│   ├── kien-truc-su.md
│   ├── ky-su-ket-cau.md
│   ├── ky-su-co-dien.md
│   ├── kiem-soat-ban-ve.md
│   ├── ky-su-du-toan.md
│   └── dieu-phoi-bim.md
├── src/
│   ├── HPBim.Core/
│   │   ├── HPBim.Core.csproj
│   │   ├── Geometry/
│   │   ├── Models/
│   │   ├── Tcvn/
│   │   ├── Qc/
│   │   └── Serialization/
│   ├── HPBim.Client/
│   │   ├── HPBim.Client.csproj
│   │   ├── Connection/
│   │   ├── ScriptGenerators/
│   │   ├── TypeResolver/
│   │   └── Pipeline/
│   └── HPBim.Cli/
│       ├── HPBim.Cli.csproj
│       ├── Program.cs
│       └── Commands/
└── tests/
    ├── HPBim.Core.Tests/
    │   ├── HPBim.Core.Tests.csproj
    │   ├── Fixtures/NhaPho5x18mFixture.cs
    │   ├── GeometryTests.cs
    │   ├── TcvnRulesTests.cs
    │   └── QcEngineTests.cs
    └── HPBim.Client.Tests/
        ├── HPBim.Client.Tests.csproj
        └── ScriptGenerationTests.cs
```
