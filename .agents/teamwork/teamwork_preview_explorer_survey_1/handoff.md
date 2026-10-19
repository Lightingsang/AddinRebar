# Handoff Report: Survey Explorer 1 (HPDcePro Wave 3 — MCP Infrastructure & Architecture)

**Author:** Survey Explorer 1  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_1\`  
**Target Solution:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`  
**Date:** 2026-10-18T00:22:00Z  

---

## 1. Observation

### 1.1 Existing Solution & Project Structure
Direct inspection of `HPDcePro/` confirms a clean, multi-project solution structure adhering to repository layout rules:
- **Solution File:** `HPDcePro/HPDcePro.slnx` currently groups:
  - `/Solution Items/`: `global.json`, `Directory.Build.props`
  - `/src/`:
    - `src/HPDcePro.Core/HPDcePro.Core.csproj` (`netstandard2.0;net8.0`) — pure domain models, calculators, geometry, serialization, XData, and ML tokenizer.
    - `src/HPDcePro.Excel/HPDcePro.Excel.csproj` (`net8.0`) — headless ClosedXML report/BOQ exporters and static calculation UDF bridges.
    - `src/HPDcePro.UI/HPDcePro.UI.csproj` (`net8.0-windows`) — WPF MVVM views and viewmodels (`MahApps.Metro` 2.4.11, `CommunityToolkit.Mvvm` 8.4.0).
    - `src/HPDcePro.AutoCad/HPDcePro.AutoCad.csproj` (`net8.0-windows`) — AutoCAD 25.1.0 command handlers and drafting adapters.
    - `src/HPDcePro.Revit/HPDcePro.Revit.csproj` (`net8.0-windows`) — Revit 2026 rebar placement and data exchange adapters.
  - `/tests/`:
    - `tests/HPDcePro.Core.Tests/HPDcePro.Core.Tests.csproj` (`net8.0`, xUnit v3 / MTP)
    - `tests/HPDcePro.Adapters.Tests/HPDcePro.Adapters.Tests.csproj` (`net8.0-windows`, xUnit v3 / MTP)

### 1.2 Baseline Verification Execution
Running the solution test suite via `dotnet test HPDcePro/HPDcePro.slnx -c Debug` produced:
- `HPDcePro.Core.Tests.dll`: Passed 237, Failed 0, Skipped 0 (Duration: 1.8s)
- `HPDcePro.Adapters.Tests.dll`: Passed 43, Failed 0, Skipped 0 (Duration: 1.5s)
- **Total:** Exactly **280 passed**, 0 failed, 0 skipped.
- Build output: 0 warnings, 0 errors with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

### 1.3 Key Governance & Architectural Constraints Observed
1. **Target Frameworks:** `HPDcePro.Core` supports `netstandard2.0;net8.0`. `HPDcePro.Excel` is `net8.0`. The new `HPDcePro.Mcp` project must target `net8.0` (`TargetFramework=net8.0`).
2. **Read-Only Shipped Assets:** `C:\Dce Pro\` is strictly read-only input (`mauhoc.db`, `DuAnMau.autostr`, binaries). Code in `HPDcePro` must never modify or create files in `C:\Dce Pro\`.
3. **Clean Code Thresholds:**
   - Maximum lines per file: $\le 300$ lines (`HP_CLEAN_CODE_CORE.md` Rule C2).
   - Maximum lines per method: $\le 50$ lines (Rule M3).
   - Zero commented-out code (Rule Q-B1).
   - Zero empty `catch` blocks without explanatory comment (Rule Q-B2).
   - No circular references; `HPDcePro` must not reference other host MCP folders (`HPAutoCad/`, `HPEtabs/`, etc.).

---

## 2. Logic Chain

### 2.1 Integration Strategy for `HPDcePro.Mcp` (`net8.0`)
1. **Project Placement:**
   - Location: `HPDcePro/src/HPDcePro.Mcp/HPDcePro.Mcp.csproj`.
   - Solution Registration: Add `<Project Path="src/HPDcePro.Mcp/HPDcePro.Mcp.csproj" />` under `<Folder Name="/src/">` in `HPDcePro.slnx`.
2. **Target & Output Type:**
   - Target Framework: `net8.0`.
   - Output Type: `Exe` (Console Application) with `Program.cs` implementing standard stdio JSON-RPC loop.
   - Being an executable assembly does not prevent unit testing; `HPDcePro.Adapters.Tests` can directly reference `HPDcePro.Mcp.csproj` and test `DceProMcpToolCatalog` and `DceProMcpToolExecutor` in-memory.
3. **Project References:**
   - `<ProjectReference Include="..\HPDcePro.Core\HPDcePro.Core.csproj" />`
   - `<ProjectReference Include="..\HPDcePro.Excel\HPDcePro.Excel.csproj" />`
   - Package References: `System.Text.Json` (v8.0.5) is already pinned and available transitively from `HPDcePro.Core`. No heavy unneeded dependencies.
4. **Test Project Integration:**
   - Update `tests/HPDcePro.Adapters.Tests/HPDcePro.Adapters.Tests.csproj` to reference `HPDcePro.Mcp.csproj`.
   - Add automated test suites exercising all 8 tools across positive cases, invalid inputs, and parameter fallbacks, raising total tests from 280 to $\ge 330$ as required by Wave 3.

---

### 2.2 Complete Architecture for R1: The 8 MCP Tools

`DceProMcpToolCatalog` defines tool metadata and JSON Schemas. `DceProMcpToolExecutor` acts as the dispatcher, parsing arguments and routing calls to dedicated single-responsibility handlers.

#### 1. `dce_design_beam`
- **Domain Scope:** TCVN 5574:2018 RC beam design, detailing, and 2D drafting primitives.
- **Operations Supported:**
  - `single_span`: Computes required tensile/compressive rebar area ($A_s, A_{s}'$), crack width ($a_{crc}$), and shear stirrup spacing ($s_w$).
  - `continuous`: Multi-span continuous beam with moment redistribution ($15\%$), bar curtailment ($L/3, L/4$), lap splices, and side web skin bars ($h \ge 700\text{mm}$).
  - `foundation`: Continuous ground beam / strap beam flexure and shear.
  - `curved`: Circular curved beam under bending, shear, and torsion ($T$).
  - `shop_drawing`: Longitudinal beam elevation and cross-sections (1-1, 2-2) as `CadDraftingBuffer` primitives.
- **Key Parameters:**
  - `operation` (string: "single_span" | "continuous" | "foundation" | "curved" | "shop_drawing")
  - `beamMark` (string, default: "D1")
  - `concreteGrade` (string: "B20".."B40", default: "B25")
  - `steelGrade` (string: "CB300-V".."CB500-V", default: "CB400-V")
  - `sectionWidthMm` (double, default: 220)
  - `sectionHeightMm` (double, default: 400)
  - `momentKnm` (double, for single span)
  - `shearKn` (double, for single span)
  - `spans` (array of `BeamSpanInput` DTOs, for continuous/foundation/drawing)
- **Delegating Services:**
  - `HPDcePro.Core.Calculations.Beam.BeamFlexureCalculator`
  - `HPDcePro.Core.Calculations.Beam.BeamShearCalculator`
  - `HPDcePro.Core.Calculations.Beam.BeamCrackCalculator`
  - `HPDcePro.Core.Calculations.Beam.ContinuousBeamCalculator`
  - `HPDcePro.Core.Calculations.Beam.FoundationBeamCalculator`
  - `HPDcePro.Core.Calculations.Beam.CurvedBeamCalculator`
  - `HPDcePro.Core.Drafting.BeamShopDrawingGenerator`

#### 2. `dce_design_column_wall`
- **Domain Scope:** TCVN 5574:2018 RC column biaxial bending interaction ($N$-$M_x$-$M_y$), slenderness buckling magnification ($\eta$), detailing, and shear wall boundary elements.
- **Operations Supported:**
  - `column_pmm`: Biaxial $P$-$M_x$-$M_y$ interaction checking for rectangular/circular columns.
  - `column_detailing`: Multi-story column vertical bar splice zones ($H_n/3..2H_n/3$), 1:6 cranked offsets, and tie spacing.
  - `shear_wall`: Boundary element necessity check ($l_c, b_c, A_s$), web vertical/horizontal reinforcement check, and diagonal shear check.
- **Key Parameters:**
  - `operation` (string: "column_pmm" | "column_detailing" | "shear_wall")
  - `elementMark` (string, default: "C1" / "W1")
  - `concreteGrade` (string, default: "B30")
  - `steelGrade` (string, default: "CB400-V")
  - For column: `shape` ("Rectangular"|"Circular"), `widthBMm`, `heightHMm`, `effectiveLengthL0Mm`, `axialNKn`, `momentMxKnm`, `momentMyKnm`, `bars`
  - For wall: `lengthLwMm`, `thicknessTwMm`, `heightHwMm`, `axialNKn`, `shearVuKn`, `momentMuKnm`, `webVerticalBarDiameter`, `webVerticalBarSpacingMm`
- **Delegating Services:**
  - `HPDcePro.Core.Calculations.Column.ColumnPmmInteractionCalculator`
  - `HPDcePro.Core.Drafting.ColumnDetailingGenerator`
  - `HPDcePro.Core.Calculations.Wall.ShearWallCalculator`

#### 3. `dce_design_foundation_pile`
- **Domain Scope:** TCVN 10304:2025 geotechnical pile capacity, multi-pile cap design, and shallow pad footing design.
- **Operations Supported:**
  - `pile_capacity`: SPT $N_{60}$ / soil friction calculation for driven or bored piles.
  - `pile_cap`: 1-N pile reactions under $N, M_x, M_y$, column punching shear, corner pile punching shear, one-way shear, flexural rebar, and pile schedule.
  - `pad_footing`: Base contact pressure ($p_{\max}, p_{\min}, p_{\text{avg}}$), edge uplift check, punching shear, and bottom flexural reinforcement.
- **Key Parameters:**
  - `operation` (string: "pile_capacity" | "pile_cap" | "pad_footing")
  - `elementMark` (string, default: "DC1" / "M1")
  - For pile capacity: `diameterM`, `lengthM`, `installationType` ("DrivenPile"|"BoredPile"), `soilLayers`, `tipN60`
  - For pile cap: `widthBMm`, `lengthLMm`, `thicknessHMm`, `columnWidthMm`, `columnHeightMm`, `axialNKn`, `momentMxKnm`, `momentMyKnm`, `pileCapacityRcKn`, `piles`
  - For pad footing: `lengthLMm`, `widthBMm`, `depthHMm`, `foundationDepthM`, `allowableSoilPressureKpa`, `axialNKn`, `momentMxKnm`, `momentMyKnm`
- **Delegating Services:**
  - `HPDcePro.Core.Calculations.Pile.PileCapacityCalculator`
  - `HPDcePro.Core.Calculations.Foundation.PileCapCalculator`
  - `HPDcePro.Core.Calculations.Foundation.PadFootingCalculator`
  - `HPDcePro.Core.Drafting.FoundationDetailingGenerator`

#### 4. `dce_design_slab_stair_lintel`
- **Domain Scope:** Two-way rectangular slabs, RC staircases, and masonry wall opening lintels.
- **Operations Supported:**
  - `two_way_slab`: Marcus strip moment distribution under 6 boundary support configurations, top/bottom layer rebar requirements.
  - `staircase`: Waist slab (`EngineBanThang`) or folded plate (`EngineZigZac`) rise/tread ergonomics (Blondel $2h+b$), design load, bending moment, and rebar detailing.
  - `lintel`: Masonry triangular arching load, clear span bearing length check ($b_g \ge 200\text{mm}$), flexure, and shear.
- **Key Parameters:**
  - `operation` (string: "two_way_slab" | "staircase" | "lintel")
  - `mark` (string)
  - For slab: `shortSpanLxMm`, `longSpanLyMm`, `slabThicknessHMm`, `deadLoadGKnM2`, `liveLoadPKnM2`, `boundaryCondition`
  - For staircase: `type` ("WaistSlab"|"FoldedPlateZigZag"), `flightSpanLMm`, `totalRiseHMm`, `waistThicknessHMm`, `numberOfSteps`, `stairWidthBMm`
  - For lintel: `clearSpanL0Mm`, `bearingLengthMm`, `wallThicknessBMm`, `lintelDepthHMm`, `wallHeightAboveMm`
- **Delegating Services:**
  - `HPDcePro.Core.Calculations.Slab.TwoWaySlabCalculator`
  - `HPDcePro.Core.Calculations.Secondary.StaircaseCalculator`
  - `HPDcePro.Core.Calculations.Secondary.LintelCalculator`

#### 5. `dce_design_steel_peb`
- **Domain Scope:** TCVN 5575:2024 Pre-Engineered Building (PEB) portal frames, connections, base plates, and TCVN 2737:2023 wind loads.
- **Operations Supported:**
  - `portal_frame`: PEB portal frame tapered column/rafter layout and purlin coordinate distribution.
  - `moment_connection`: Bolted extended end-plate beam-column moment connection check (bolt tension/shear interaction, prying force, stiffeners, welds).
  - `base_plate`: Anchor bolt base plate bearing stress, anchor bolt tension, embedment depth, and base shear.
  - `wind_load`: Velocity pressure $q_0$ by wind zone (I-V), exposure factor $k(z_e)$ by terrain (A-D), aerodynamic coefficients, and design wind pressure.
- **Key Parameters:**
  - `operation` (string: "portal_frame" | "moment_connection" | "base_plate" | "wind_load")
  - For portal frame: `spanMm`, `eaveHeightMm`, `roofSlopePercent`, `baySpacingMm`, `targetPurlinSpacingMm`, tapered section dims
  - For moment connection: beam/column section dims, `bendingMomentKnm`, `shearForceKn`, `boltDiameterMm`, `boltGrade`, `endPlateThicknessMm`
  - For base plate: plate dims, `axialForceKn`, `bendingMomentKnm`, `shearForceKn`, `anchorBoltCount`, `anchorBoltDiameterMm`, `pedestalGrade`
  - For wind load: `windZone` ("ZoneI".."ZoneV"), `terrainCategory` ("A".."D"), `heightZM`, `buildingWidthM`, `buildingLengthM`
- **Delegating Services:**
  - `HPDcePro.Core.Calculations.Steel.PreGableFrameCalculator`
  - `HPDcePro.Core.Calculations.Steel.SteelMomentConnectionCalculator`
  - `HPDcePro.Core.Calculations.Steel.AnchorBoltBasePlateCalculator`
  - `HPDcePro.Core.Calculations.Wind.WindLoadCalculator`

#### 6. `dce_optimize_rebar_cutting`
- **Domain Scope:** 1D rebar cutting stock optimizer (`ToiUuCatThep`) and project schedule aggregator.
- **Operations Supported:**
  - `optimize_cutting`: Solves 1D cutting stock for 11.7m commercial bars, minimizes waste percent, categorizes reusable remnants ($\ge 1.5$m), supports Auto/BestFitDecreasing/FirstFitDecreasing.
  - `aggregate_schedule`: Aggregates project bar list into diameter schedules, element schedules, and total weight by TCVN 1651:2018 mass tables ($\le 10\text{mm}, 12-18\text{mm}, > 18\text{mm}$).
- **Key Parameters:**
  - `operation` (string: "optimize_cutting" | "aggregate_schedule")
  - For cutting: `stockBarLengthMm` (default: 11700), `minReusableRemnantLengthMm` (default: 1500), `strategy` ("Auto"|"BestFitDecreasing"|"FirstFitDecreasing"), `demands` (`id`, `diameterMm`, `lengthMm`, `quantity`)
  - For schedule: `items` (`elementMark`, `barMark`, `diameterMm`, `shapeCode`, `lengthMm`, `quantityPerElement`, `elementCount`)
- **Delegating Services:**
  - `HPDcePro.Core.Rebar.RebarCuttingOptimizer`
  - `HPDcePro.Core.Rebar.RebarScheduleAggregator`

#### 7. `dce_query_mayhoc_rebar`
- **Domain Scope:** Machine learning rebar intelligence and CAD callout normalization/tokenization.
- **Operations Supported:**
  - `tokenize_text`: Normalizes diameter signs (`%%c`, `\u03c6`, `d` prefix) and extracts structural tokens (count, diameter, spacing, length, position features).
  - `query_database`: Queries `C:\Dce Pro\MayHoc\mauhoc.db` for learned training samples by prefix/feature.
  - `query_history`: Queries user rebar correction history (`LichSuSua`).
  - `model_info`: Reads model configuration metadata (`CauHinhModel`) and training sample count.
- **Key Parameters:**
  - `operation` (string: "tokenize_text" | "query_database" | "query_history" | "model_info")
  - `text` (string, for tokenization)
  - `dbPath` (string, optional override for `mauhoc.db`)
  - `filterText` (string, optional)
  - `limit` (int, default: 20)
- **Delegating Services:**
  - `HPDcePro.Core.MachineLearning.RebarTextTokenizer`
  - `HPDcePro.Core.MachineLearning.RebarFeatureExtractor`
  - `HPDcePro.Core.MachineLearning.MauHocDatabaseReader`

#### 8. `dce_run_autostr_pipeline`
- **Domain Scope:** Automated 24-step structural engineering pipeline orchestrator and multi-sheet ClosedXML BOQ generation.
- **Operations Supported:**
  - `run_pipeline`: Executes all 24 pipeline steps (grid, columns, framing, loads, Etabs, beam/column/slab detailing, foundation, schedules, sheets) with iterative resizing when capacity is exceeded.
  - `load_config`: Reads and parses a `.autostr` project file into strongly-typed `AutoStrConfig`.
  - `export_boq`: Generates a 5-sheet ClosedXML Excel BOQ workbook (`TongHop`, `BeTong`, `VanKhuon`, `CotThep`, `ChiTietTheoTang`).
- **Key Parameters:**
  - `operation` (string: "run_pipeline" | "load_config" | "export_boq")
  - `autostrFilePath` (string, path to `.autostr` file)
  - `checkpointPath` (string, for resuming interrupted runs)
  - `outputBoqPath` (string, destination `.xlsx` path)
  - `maxSteps` (int, default: 24)
  - `enableAutoResizing` (bool, default: true)
- **Delegating Services:**
  - `HPDcePro.Core.AutoStr.AutoStrOrchestrator`
  - `HPDcePro.Core.Serialization.AutoStrProjectSerializer`
  - `HPDcePro.Core.Calculations.Takeoff.QuantityTakeoffCalculator`
  - `HPDcePro.Excel.ClosedXmlBoqExporter`
  - `HPDcePro.Excel.ClosedXmlReportExporter`

---

### 2.3 Structure of `.agents/skills/hp-dce-pro/SKILL.md`

The skill document should be organized as follows:
1. **YAML Frontmatter:**
   - Standard trigger keywords (`dce`, `autostr`, `tcvn`, `ket cau`, `cot thep`, `dam`, `cot`, `mong`, `san`, `nha thep`, `cat thep`, `boq`).
   - Server name: `hpdcepro-mcp`.
2. **Portable Host Contract:**
   - Universal Antigravity / Claude Code / Codex compatibility comments.
3. **Overview & Architectural Diagram:**
   - Explaining that `HPDcePro.Mcp` provides 100% headless C# engineering computation with zero reliance on AutoCAD or Revit running.
   - Flow: `Coding Agent -> HPDcePro.Mcp -> HPDcePro.Core & HPDcePro.Excel -> Results & ClosedXML Exporters`.
4. **Tool Catalog Reference (All 8 Tools):**
   - For each tool: Purpose, TCVN standards cited, operations enum, parameters table, sample input JSON, sample output JSON.
5. **Practical Multi-Step Workflows:**
   - Workflow 1: End-to-end continuous beam calculation, crack check, and shop drawing extraction.
   - Workflow 2: PEB portal frame design, wind load calculation, and moment connection verification.
   - Workflow 3: Rebar cutting schedule optimization for minimum scrap.
   - Workflow 4: Automated 24-step structural design execution via `AutoStrOrchestrator`.
6. **Diagnostics & Troubleshooting:**
   - Capacity ratio $> 1.0$ resolution, geotechnical SPT layers formatting, file paths handling.

---

### 2.4 Clean Code File Decomposition Strategy

To guarantee that no file exceeds 300 lines and no method exceeds 50 lines:
```
HPDcePro/src/HPDcePro.Mcp/
├── HPDcePro.Mcp.csproj                 (Target net8.0)
├── Program.cs                          (~55 lines: entry point, stdio loop invocation)
├── DceProMcpServer.cs                  (~130 lines: JSON-RPC 2.0 transport loop)
├── Catalog/
│   ├── DceProMcpToolCatalog.cs         (~80 lines: registry collecting all 8 tools)
│   └── Schemas/
│       ├── BeamToolSchema.cs           (~85 lines)
│       ├── ColumnWallToolSchema.cs     (~85 lines)
│       ├── FoundationToolSchema.cs     (~85 lines)
│       ├── SlabSecondaryToolSchema.cs  (~80 lines)
│       ├── SteelPebToolSchema.cs       (~90 lines)
│       ├── RebarToolSchema.cs          (~70 lines)
│       ├── MayHocToolSchema.cs         (~70 lines)
│       └── AutoStrToolSchema.cs        (~80 lines)
├── Execution/
│   ├── DceProMcpToolExecutor.cs        (~110 lines: dispatcher to handlers)
│   └── Handlers/
│       ├── BeamToolHandler.cs          (~140 lines)
│       ├── ColumnWallToolHandler.cs    (~150 lines)
│       ├── FoundationToolHandler.cs    (~155 lines)
│       ├── SlabSecondaryToolHandler.cs (~145 lines)
│       ├── SteelPebToolHandler.cs      (~155 lines)
│       ├── RebarCuttingToolHandler.cs  (~115 lines)
│       ├── MayHocToolHandler.cs        (~110 lines)
│       └── AutoStrToolHandler.cs       (~135 lines)
└── Models/
    ├── DceProToolDefinition.cs         (~45 lines)
    ├── DceProToolResult.cs             (~50 lines)
    └── DceProJsonRpcMessage.cs         (~65 lines)
```

**Decomposition Metrics:**
- Total files: 23 files.
- Maximum file size: ~160 lines (far below 300-line ceiling).
- Maximum method size: ~35 lines (far below 50-line ceiling).
- 0 warnings (`TreatWarningsAsErrors=true` compliant).
- 0 commented-out code, 0 unannotated empty catch blocks.

---

## 3. Caveats

1. **AutoStr Real-File Dependency:** Steps in `AutoStrOrchestrator` that read `DuAnMau.autostr` rely on `C:\Dce Pro\AutoStr\DuAnMau.autostr` when no custom path is given. In environments where `C:\Dce Pro` is not present, fallback synthetic configurations (`AutoStrConfig.CreateDefault()`) must be provided for tests to run green on any dev machine.
2. **MayHoc Live SQLite Database:** `MauHocDatabaseReader` connects to `C:\Dce Pro\MayHoc\mauhoc.db`. When testing in headless environments without this path, handlers must gracefully handle missing files by returning `isSuccess: false` with a clear explanatory error message rather than crashing with unhandled exceptions.
3. **Execution Scope:** As an explorer agent, no production code has been modified or created in this step. The design and schemas mapped here serve as the blueprint for implementation.

---

## 4. Conclusion

- The `HPDcePro` solution is in a healthy, fully-tested baseline state with **280 passing tests** across `HPDcePro.Core.Tests` (237) and `HPDcePro.Adapters.Tests` (43) with 0 errors and 0 warnings.
- All underlying domain services, structural calculators (TCVN 5574:2018, TCVN 10304:2025, TCVN 5575:2024, TCVN 2737:2023, TCVN 1651:2018), 1D cutting stock algorithms, ML tokenizers, and 24-step orchestrators already exist in `HPDcePro.Core` and `HPDcePro.Excel`.
- The architecture for `HPDcePro.Mcp` (`net8.0`) is completely mapped out with a modular 23-file decomposition plan that respects all repository rules ($\le 300$ lines/file, $\le 50$ lines/method, `TreatWarningsAsErrors=true`).
- The 8 MCP tools (`dce_design_beam`, `dce_design_column_wall`, `dce_design_foundation_pile`, `dce_design_slab_stair_lintel`, `dce_design_steel_peb`, `dce_optimize_rebar_cutting`, `dce_query_mayhoc_rebar`, `dce_run_autostr_pipeline`) have exact parameter definitions, sub-mode operations, and mapping to existing services.
- The structure for `.agents/skills/hp-dce-pro/SKILL.md` is fully defined and ready for authoring.

---

## 5. Verification Method

To independently verify this survey:
1. **Verify Baseline Tests:**
   ```bash
   dotnet test HPDcePro/HPDcePro.slnx -c Debug
   ```
   *Expected Result:* 280 passed (237 in Core.Tests, 43 in Adapters.Tests), 0 failed, 0 skipped.
2. **Inspect Existing Projects:**
   ```bash
   dotnet build HPDcePro/HPDcePro.slnx -c Debug
   ```
   *Expected Result:* Build succeeded with 0 warnings, 0 errors.
3. **Check Code Quality Limits on Planned Files:**
   Verify proposed file layout against `docs/clean-code/HP_CLEAN_CODE_CORE.md` (no file $> 300$ lines, no method $> 50$ lines).
4. **Invalidation Conditions:**
   - Any failure in the 280 baseline tests.
   - Any missing domain service required by the 8 tools.
   - Any reference from `HPDcePro.Mcp` to host-specific UI/AutoCAD libraries (`Autodesk.*` or `MahApps.Metro`).
