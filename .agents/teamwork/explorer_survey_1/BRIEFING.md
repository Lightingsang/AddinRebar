# BRIEFING — 2026-09-27T16:16:00Z

## Mission
Investigate Kata spreadsheet contract, KataExport implementation, Excel COM / ClosedXML usage, cell specifications for sheet 'Dam', and propose strongly-typed DTO design for Kata Rebar.

## 🔒 My Identity
- Archetype: explorer
- Roles: teamwork_preview_explorer (Kata & Excel Domain / Spec Miner)
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1
- Original parent: aa8876fc-b61d-4725-aacd-616632eb9cc0
- Milestone: Kata Rebar Phase 0 - Domain / Spec Survey

## 🔒 Key Constraints
- Read-only investigation — do NOT implement production code
- Write only to your own working directory: `.agents/teamwork/explorer_survey_1/`
- Deep analysis of Kata workbook cell schema, bar notation parsing, COM vs ClosedXML, and DTO contracts

## Current Parent
- Conversation ID: aa8876fc-b61d-4725-aacd-616632eb9cc0
- Updated: 2026-09-27T16:16:00Z

## Investigation State
- **Explored paths**:
  - `HPRebar/KataExport` (`ExcelComAttach.cs`, `ComLateBinding.cs`, `KataExcelWriter.cs`)
  - `HPRebar.Core/KataExport` (`KataRowBuilder.cs`, `KataExcelCell.cs`, `KataRunModels.cs`)
  - `plans/260926-2317-kata-export-hprebar/reports/` (`kata-cell-contract.md`, `phase-06-live-verify.md`)
  - `plans/reports/analysis-260926-drawingkata-dynamo-export-excel.md`
  - `C:\kata_pro\Kata.xlsm` (direct OpenXML inspection of sheet `Dam` via `inspect_dam.py` and `dump_table.py`)
  - `C:\kata_pro\Goi lenh.lsp`
  - `HPExcel/HPExcel.McpBridge/Headless/ClosedXmlWorkbookService.cs`
  - `HPRebar.Core/BeamRebar/Models/` (`BeamContinuousStack.cs`, `BeamMainBarSpec.cs`, etc.)
- **Key findings**:
  - Master template `Kata.xlsm` located at `C:\kata_pro\Kata.xlsm`.
  - Complete cell map of sheet `Dam` verified 100%: B3..B10 header, G2..G3 tension/compression anchorage (40d/30d), H3/H5 cutoff ratios (0.2L/0.25L), G6..G9 stirrups, B11/B12 continuous bars, alternating Cột/Nhịp columns from Col C, Rows 13-16 top extra bars at supports, Rows 17-18 bottom extra bars at spans, Row 20 side bars, Rows 25-27 stirrup types.
  - Dual-source reading architecture: COM (`oleaut32` batch `Value2` 2D array, <5ms) + ClosedXML 0.104.2 offline fallback. Pure memory `IKataDamCellAccessor` keeps `HPRebar.Core` netstandard2.0 zero-dependency.
  - Strongly-typed DTO proposal `KataBeamRebarSpec` designed and mapped cleanly to existing `HPRebar.Core/BeamRebar/Models`.
- **Unexplored areas**: None within the survey scope.

## Key Decisions Made
- Use Accessor abstraction (`IKataDamCellAccessor`) in Core so Core remains zero-dependency netstandard2.0.
- Implement `ComKataDamReader` in `HPRebar` reading full range `A1:BZ30` via `Value2` in a single COM call.
- Provide `ClosedXmlKataDamReader` in `HPRebar` for offline/fallback file reading.

## Artifact Index
- `DISPATCH.md` — Initial dispatch instructions
- `BRIEFING.md` — Working memory index
- `progress.md` — Liveness heartbeat
- `inspect_dam.py` — Python script inspecting sheet Dam OpenXML
- `inspect_dam_details.py` — Python script dumping comments and validation rules
- `dump_table.py` — Python script dumping formatted rows 10-30
- `report.md` — Authoritative technical survey report (7 sections, ~400 lines)
- `handoff.md` — 5-component handoff report
