---
name: hp-dce-pro
description: "Thiết kế kết cấu và chi tiết hóa cốt thép tự động qua HPDcePro MCP server (stdio transport, server hpdcepro-mcp): tính toán dầm/cột/vách/móng/cọc/sàn/cầu thang/lanh tô theo TCVN 5574:2018 & TCVN 10304:2025, khung thép tiền chế PEB & tải trọng gió TCVN 5575:2024 & TCVN 2737:2023, tối ưu hóa cắt thép 1D 11.7m ToiUuCatThep, truy vấn máy học MayHoc/mauhoc.db, và chạy pipeline tự động 24 bước AutoStr xuất bảng khối lượng ClosedXML BOQ. TRIGGER when: user nhắc 'dce', 'autostr', 'tcvn 5574', 'tcvn 10304', 'tcvn 5575', 'tcvn 2737', 'tcvn 1651', 'kết cấu', 'cốt thép', 'thiết kế dầm', 'thiết kế cột', 'vách cứng', 'đài móng', 'móng cọc', 'móng đơn', 'sàn 2 phương', 'cầu thang', 'lanh tô', 'nhà thép', 'portal frame', 'bu lông neo', 'bản mã', 'cắt thép', 'tối ưu cắt thép', 'thống kê thép', 'mayhoc', 'mauhoc', hoặc 'boq'. Keywords: dce, autostr, tcvn, ket cau, cot thep, dam, cot, mong, san, nha thep, cat thep, boq, mcp."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hpdcepro-mcp
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# HPDcePro MCP — Structural Engineering & Rebar Automation

## 1. Overview & Architecture

`HPDcePro.Mcp` provides 100% headless C# .NET 8.0 structural engineering computation, rebar detailing generation, cutting stock optimization, and multi-sheet ClosedXML Excel BOQ export with zero dependency on running instances of AutoCAD, Revit, or Microsoft Excel.

```
Coding Agent (AI)
       │
       ▼ (stdio JSON-RPC 2.0: initialize, tools/list, tools/call)
┌─────────────────────────────────────────────────────────────┐
│                      HPDcePro.Mcp                           │
│  ├── DceProMcpServer (Stdio Loop)                           │
│  ├── DceProMcpToolCatalog (8 Tool Definitions & Schemas)    │
│  └── DceProMcpToolExecutor (Single-Responsibility Handlers) │
└──────────────┬──────────────────────────────┬───────────────┘
               │                              │
               ▼                              ▼
┌──────────────────────────────┐ ┌────────────────────────────┐
│        HPDcePro.Core         │ │       HPDcePro.Excel       │
│  - TCVN 5574:2018 RC design  │ │  - ClosedXML BOQ Exporter  │
│  - TCVN 10304:2025 Piles/Cap │ │  - ClosedXML Report Gen    │
│  - TCVN 5575:2024 Steel/PEB  │ │  - Calculation UDF Bridges │
│  - TCVN 2737:2023 Wind Loads │ │  - Zero COM dependencies   │
│  - 1D Cutting Stock ToiUu    │ └────────────────────────────┘
│  - 24-Step AutoStr Engine    │
│  - CAD Drafting Buffers 2D   │
└──────────────────────────────┘
```

---

## 2. Tool Catalog Reference (8 Tools)

### 2.1 `dce_design_beam`
- **Standards:** TCVN 5574:2018 Clause 8.1.2.3 (Flexure), Clause 8.1.3.2 (Shear), Clause 8.2.1.2 (Crack width $a_{crc}$).
- **Operations:**
  - `single_span`: Computes tensile/compressive rebar $A_s$, crack width, and shear stirrups.
  - `continuous`: Multi-span continuous beam with 15% moment redistribution, bar curtailment ($L/3, L/4$), and side skin bars.
  - `foundation`: Continuous ground beam / strap beam flexure and deep beam shear capacity.
  - `curved`: Curved circular beam under combined bending, shear, and torsion ($T$).
  - `shop_drawing`: Longitudinal elevation and cross-sections (1-1, 2-2) as CAD entity primitives.
- **Example Parameters:**
```json
{
  "operation": "single_span",
  "beamMark": "D1",
  "concreteGrade": "B25",
  "steelGrade": "CB400-V",
  "sectionWidthMm": 220,
  "sectionHeightMm": 400,
  "momentKnm": 85.0,
  "shearKn": 95.0
}
```

### 2.2 `dce_design_column_wall`
- **Standards:** TCVN 5574:2018 Clause 8.1.2.4 (Biaxial P-M-M interaction), Clause 8.1.2.5 (Slenderness $\eta$), Shear Wall Boundary Element.
- **Operations:**
  - `column_pmm`: Biaxial $P$-$M_x$-$M_y$ interaction check and capacity utilization ratio ($UR \le 1.0$).
  - `column_detailing`: Multi-story vertical rebar cranked splices (1:6) and confinement tie spacing.
  - `shear_wall`: Boundary element necessity check ($l_c, b_c, A_s$), web vertical/horizontal rebar, and diagonal crushing.
- **Example Parameters:**
```json
{
  "operation": "column_pmm",
  "elementMark": "C1",
  "shape": "Rectangular",
  "widthBMm": 300,
  "heightHMm": 300,
  "effectiveLengthL0Mm": 3300,
  "axialNKn": 850,
  "momentMxKnm": 75,
  "momentMyKnm": 60,
  "concreteGrade": "B30",
  "steelGrade": "CB400-V"
}
```

### 2.3 `dce_design_foundation_pile`
- **Standards:** TCVN 10304:2025 (Geotechnical Pile Capacity per SPT), TCVN 5574:2018 (Punching & Flexure).
- **Operations:**
  - `pile_capacity`: Tip resistance $Q_b$, shaft friction $\sum R_{si}$, and design capacity $R_c$.
  - `pile_cap`: 1-N pile reactions, column punching shear, corner pile punching shear, one-way beam shear, and flexural rebar.
  - `pad_footing`: Soil bearing pressure ($p_{\max}, p_{\min}, p_{\text{avg}}$), edge uplift check, punching shear, and bottom flexural rebar.
- **Example Parameters:**
```json
{
  "operation": "pile_capacity",
  "diameterM": 0.4,
  "lengthM": 20.0,
  "installationType": "DrivenPile",
  "tipSoilN60": 32.0
}
```

### 2.4 `dce_design_slab_stair_lintel`
- **Standards:** TCVN 5574:2018 (Marcus Elastic Slab Strips, Blondel Stair Ergonomics, Masonry Arching).
- **Operations:**
  - `two_way_slab`: Marcus elastic moment coefficients, top/bottom rebar layers, and span-to-depth deflection check.
  - `staircase`: Waist slab (`EngineBanThang`) or folded plate (`EngineZigZac`), Blondel formula ($2h+b \approx 600..640\text{mm}$), design moment, and rebar detailing.
  - `lintel`: Masonry triangular arching load, bearing length verification ($\ge 200\text{mm}$), flexure, and shear.
- **Example Parameters:**
```json
{
  "operation": "two_way_slab",
  "mark": "S1",
  "shortSpanLxMm": 4000,
  "longSpanLyMm": 5000,
  "slabThicknessHMm": 120,
  "deadLoadGKnM2": 3.5,
  "liveLoadPKnM2": 2.4
}
```

### 2.5 `dce_design_steel_peb`
- **Standards:** TCVN 5575:2024 (Steel Structures), TCVN 2737:2023 (Wind Actions).
- **Operations:**
  - `portal_frame`: PEB tapered built-up column/rafter geometry, slope length, purlin layout, and stress checks.
  - `moment_connection`: Extended end-plate bolted connection, bolt tension/shear interaction, prying forces, weld sizes, and column web stiffeners.
  - `base_plate`: Anchor bolt tension, base plate bearing stress, plate thickness, and embedment depth.
  - `wind_load`: Velocity pressure $q_0$ by zone (I-V), exposure factor $k(z_e)$ by terrain (A-D), and design wind pressure.
- **Example Parameters:**
```json
{
  "operation": "wind_load",
  "windZone": "ZoneII",
  "terrainCategory": "B",
  "heightZM": 12.0,
  "cpe": 0.8,
  "cpi": -0.2
}
```

### 2.6 `dce_optimize_rebar_cutting`
- **Standards:** TCVN 1651:2018 (Reinforcing Steel), 1D Cutting Stock Optimization (`ToiUuCatThep`).
- **Operations:**
  - `optimize_cutting`: Packs bar demands into 11.7m commercial bars, minimizes scrap percentage, and identifies reusable remnants ($\ge 1.5\text{m}$).
  - `aggregate_schedule`: Groups bar items by diameter and element mark, calculating unit mass and total weight in kg/tons.
- **Example Parameters:**
```json
{
  "operation": "optimize_cutting",
  "stockBarLengthMm": 11700,
  "strategy": "Auto",
  "demands": [
    { "id": "Bar_1", "diameterMm": 20, "lengthMm": 5800, "quantity": 8 },
    { "id": "Bar_2", "diameterMm": 20, "lengthMm": 4200, "quantity": 12 }
  ]
}
```

### 2.7 `dce_query_mayhoc_rebar`
- **Capabilities:** Machine Learning Rebar Parser & SQLite Intelligence (`mauhoc.db`).
- **Operations:**
  - `tokenize_text`: Normalizes CAD diameter characters (`%%c`, `\u03c6`, `d` prefix) and extracts structural tokens (count, diameter, spacing, length, position).
  - `query_database`: Reads learned samples from `mauhoc.db` with pagination.
  - `query_history`: Queries user rebar correction history (`LichSuSua`).
  - `model_info`: Reads model configuration metadata and sample counts.
- **Example Parameters:**
```json
{
  "operation": "tokenize_text",
  "text": "2d18 + 1d20 L=3500 a150"
}
```

### 2.8 `dce_run_autostr_pipeline`
- **Capabilities:** 24-step master structural engineering pipeline orchestrator and ClosedXML multi-sheet BOQ generation.
- **Operations:**
  - `load_config`: Parses `.autostr` project configuration.
  - `run_pipeline`: Executes all 24 pipeline steps with automatic section resizing loops.
  - `export_boq`: Exports a 5-sheet ClosedXML Excel workbook (`TongHop`, `BeTong`, `VanKhuon`, `CotThep`, `ChiTietTheoTang`).
- **Example Parameters:**
```json
{
  "operation": "export_boq",
  "projectName": "Chung Cư Cao Tầng Mẫu",
  "outputBoqPath": "output/BOQ_KetCau.xlsx"
}
```

---

## 3. Engineering Workflows

### Workflow 1: Continuous Beam Calculation to CAD Primitives
1. Call `dce_design_beam` with `operation: "continuous"` to compute moment redistribution and bar curtailment across all spans.
2. Verify flexural and shear compliance across support and midspan sections.
3. Call `dce_design_beam` with `operation: "shop_drawing"` to generate 2D CAD elevation and section entities.

### Workflow 2: PEB Portal Frame & Wind Load Design
1. Call `dce_design_steel_peb` with `operation: "wind_load"` to determine aerodynamic design wind pressures per TCVN 2737:2023.
2. Call `dce_design_steel_peb` with `operation: "portal_frame"` to calculate tapered built-up sections, purlin distributions, and knee moments.
3. Verify the rafter-column connection with `operation: "moment_connection"` and column base with `operation: "base_plate"`.

### Workflow 3: Rebar Cutting Stock Optimization
1. Extract or compute bar marks, diameters, and lengths.
2. Call `dce_optimize_rebar_cutting` with `operation: "optimize_cutting"` and `strategy: "Auto"`.
3. Review `totalStockBarsUsed`, `overallScrapPercentage`, and reuse leftover remnants $\ge 1.5\text{m}$.
4. Call `operation: "aggregate_schedule"` to produce steel schedule summaries categorized by diameter and total tonnage.

---

## 4. Diagnostics & Troubleshooting

| Symptom / Error | Root Cause | Recommended Action |
|---|---|---|
| `Unsupported operation 'xyz'` | Typo in operation parameter | Check the enum of valid operations for the specified tool. |
| `Database file not found at '...'` | `mauhoc.db` missing at path | In headless/CI environments, use `tokenize_text` which requires no database. |
| `UR > 1.0` in `column_pmm` | Section or rebar insufficient | Increase column dimensions $b \times h$, increase concrete grade (e.g. B25 -> B30), or increase bar diameters. |
| `PunchingRatio > 1.0` in pile cap / footing | Thickness inadequate | Increase foundation thickness $H$ or increase pedestal/column size. |
| `isBearingAdequate: false` in lintel | Bearing length $< 200\text{mm}$ | Ensure bearing length on masonry wall is at least $200\text{mm}$ per standard. |
