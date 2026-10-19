# HPKATA PHASE 2 ARCHITECTURAL SPECIFICATION & COMMAND MAPPING REPORT
## Comprehensive Blueprint for Beam Shop Drawing & ETABS Integration (AutoCAD 2026 & Revit 2026)

- **Document ID**: `HPKATA-M1-BEAM-CAD-REVIT-SPEC-2026`
- **Author**: `explorer_m1_2` (CAD & Revit Explorer)
- **Milestone**: M1 (Phase 2: Beam Shop Drawing & ETABS Integration)
- **Target Deliverable**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)
- **Date**: 2026-10-17
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`
- **Governance**: Clean-Room Reverse Engineering Protocol, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `host-appendix/autocad-civil.md`, `host-appendix/revit.md`

---

## 1. Executive Summary & Scope Definition

Milestone M1 (Phase 2) implements the complete **Beam Shop Drawing & ETABS Integration Subsystem** across the dual-host `HPKata` ecosystem. This subsystem bridges structural engineering calculations from ETABS and Excel (`Kata.xlsm` sheet `Dam`) with production-grade 2D detailing in AutoCAD 2026 and native 3D parametric BIM reinforcement in Revit 2026.

### 1.1 Core Mission Objectives
1. **AutoCAD Detailing Parity**: Map, architect, and design all **25 Phase 2 AutoCAD commands** and their AutoLISP aliases in `GoiLenh_HPKata.lsp`, covering continuous beams, foundation/tie beams, inclined/curved beams, intersection hanger reinforcement, framing plan recognition, DWG XRecord persistence, and ETABS live OAPI communication.
2. **DWG Graphic & Data Model**: Define complete CAD entity creation standards (polylines, lineweights, colors, dimstyles, block attributes) and exact XData RegApp `"KATA"` schemas preserving 100% two-way data compatibility with legacy KataPro drawings.
3. **Revit Native Framing Bridge**: Architect the multi-span `Structural Framing` (`OST_StructuralFraming`) recognition engine, 3D `Rebar` curve generation, diameter resolution, hook attachment, parameter synchronization (`REBAR_PARTITION`, `ALL_MODEL_MARK`), view automation, and idempotent regeneration.
4. **Clean Code Compliance**: Decompose all classes and methods such that every file is $\le 300$ physical lines (Rule C2), every method is $\le 50$ lines (Rule M3), zero dead code exists (Rule Q-B1), and zero unannotated empty catch blocks exist (Rule Q-B2).

---

## 2. Comprehensive Inventory of All 25 Phase 2 AutoCAD Commands

The table below catalogs all 25 Phase 2 commands, mapping their AutoLISP alias from `GoiLenh_HPKata.lsp`, AutoCAD `[CommandMethod]` attribute, controller delegation, functional grouping, and operational parameters.

| # | Command Name | Lisp Alias | CommandMethod | Backing Controller & Method | Group | Description & Operational Parameters |
|---|---|---|---|---|---|---|
| **1** | `ve_dam` | `vd` | `[CommandMethod("ve_dam")]`<br>`[CommandMethod("vd")]` | `BeamDrawController.DrawBeamInteractive()` | Continuous Beam | Interactive continuous beam detailing. Prompts for spans, dimensions, top/bottom bars, stirrups. Draws longitudinal elevation, sections, dimensions, and callouts. |
| **2** | `ve_dam_all` | `vda` | `[CommandMethod("ve_dam_all")]`<br>`[CommandMethod("vda")]` | `BeamDrawController.DrawBeamAllBatch()` | Continuous Beam | Batch beam detailing generation across a floor plan by consuming framing plan scan data (`quet_dam_all`). |
| **3** | `ve_dam_shop` | `vds` | `[CommandMethod("ve_dam_shop")]`<br>`[CommandMethod("vds")]` | `BeamShopController.DrawBeamShopInteractive()` | Beam Shop Detailing | Beam shop drawing generation incorporating standard 11,700 mm commercial stock bar cuts, lap splice zones in compression, and 50% staggered splices. |
| **4** | `ve_dam_all_shop` | `vdas` | `[CommandMethod("ve_dam_all_shop")]`<br>`[CommandMethod("vdas")]` | `BeamShopController.DrawBeamAllShopBatch()` | Beam Shop Detailing | Batch generation of shop drawings for all beams on a floor with cut-length optimization and coupler flags for long spans. |
| **5** | `ve_dam_mc` | `vdmc` | `[CommandMethod("ve_dam_mc")]`<br>`[CommandMethod("vdmc")]` | `BeamSectionController.DrawSectionInteractive()` | Cross-Section | Generates transverse cross-sections ($b \times h$) at supports (left/right) and midspans, showing multi-layer rebar placement and stirrups. |
| **6** | `ve_damMC_all` | `vdmca` | `[CommandMethod("ve_damMC_all")]`<br>`[CommandMethod("vdmca")]` | `BeamSectionController.DrawSectionAllBatch()` | Cross-Section | Batch generation of all cross-sections for all detailed beams on the sheet. |
| **7** | `ve_dam_mong` | `vdm` | `[CommandMethod("ve_dam_mong")]`<br>`[CommandMethod("vdm")]` | `FoundationBeamController.DrawFoundationBeam()` | Foundation Beams | Details foundation tie beams and grade beams connecting pile caps/footings with deep bottom reinforcement anchorage. |
| **8** | `ve_giang_mong` | `vgm` | `[CommandMethod("ve_giang_mong")]`<br>`[CommandMethod("vgm")]` | `FoundationBeamController.DrawTieBeam()` | Foundation Beams | Details foundation ground tie beams (giằng móng) resisting differential settlement. |
| **9** | `ve_giang_shop` | `vgms` | `[CommandMethod("ve_giang_shop")]`<br>`[CommandMethod("vgms")]` | `FoundationBeamController.DrawTieBeamShop()` | Foundation Beams | Foundation tie beam shop drawing generation with 11.7m commercial bar optimization and offcut minimization. |
| **10** | `ve_dam_xien` | `vdx` | `[CommandMethod("ve_dam_xien")]`<br>`[CommandMethod("vdx")]` | `CurvedInclinedBeamController.DrawInclinedBeam()` | Inclined & Curved | Details sloped beams, inclined ramp beams, and stair stringers with knee-joint rebar folding and anchorage cogs. |
| **11** | `ve_dam_cong` | `vdc` | `[CommandMethod("ve_dam_cong")]`<br>`[CommandMethod("vdc")]` | `CurvedInclinedBeamController.DrawCurvedBeam()` | Inclined & Curved | Details horizontally curved beams, generating developed planar elevation, radial stirrup spacing, and curved bar schedules. |
| **12** | `dam_giao` | `damgiao` | `[CommandMethod("dam_giao")]`<br>`[CommandMethod("damgiao")]` | `BeamIntersectionController.DrawIntersectionInteractive()` | Intersections | Details secondary-main beam intersection, automatically generating hanger stirrups ($5\Phi 10a50$) and anti-crack bars ($2\Phi 14$). |
| **13** | `giao_dam` | `giaodam` | `[CommandMethod("giao_dam")]`<br>`[CommandMethod("giaodam")]` | `BeamIntersectionController.AutoDetectIntersections()` | Intersections | Automated scan of framing layout to detect all secondary-to-main beam framing points and batch insert hanger stirrup cages. |
| **14** | `cat_dam` | `catdam` | `[CommandMethod("cat_dam")]`<br>`[CommandMethod("catdam")]` | `BeamIntersectionController.SplitBeamAtBoundary()` | Intersections | Splits continuous beam runs at expansion joints, shear walls, or column faces into independently detailed beam spans. |
| **15** | `raidam` | `raidam` | `[CommandMethod("raidam")]` | `BeamScanController.PlaceBeamPlanTags()` | Framing Layout | Places beam identification tag blocks (`kata_block_Beam_plan`) along grid centerlines on structural framing plans. |
| **16** | `tkdam` | `tkdam` | `[CommandMethod("tkdam")]` | `BeamScanController.GenerateBeamScheduleTable()` | Schedule Takeoff | Generates tabular summary takeoff table of beam marks, section dimensions ($b \times h$), clear lengths, and counts. |
| **17** | `quet_dam` | `qd` | `[CommandMethod("quet_dam")]`<br>`[CommandMethod("qd")]` | `BeamScanController.ScanFramingPlanRegion()` | Plan Recognition | Intelligent window selection scanner detecting beam boundaries, supporting columns, clear spans, and intersections. |
| **18** | `quet_dam_all` | `qda` | `[CommandMethod("quet_dam_all")]`<br>`[CommandMethod("qda")]` | `BeamScanController.ScanFramingPlanAll()` | Plan Recognition | Full-drawing framing scanner caching recognized beam spans into memory for subsequent batch commands (`vda`, `vdas`). |
| **19** | `Save_thep_dam` | `std` | `[CommandMethod("Save_thep_dam")]`<br>`[CommandMethod("std")]` | `BeamPersistenceController.SaveBeamRebarToDwg()` | Persistence | Serializes complete reinforcement model of selected beam into DWG Extension Dictionary (`XRecord` `"KATA"` JSON). |
| **20** | `Save_thep_dam_all` | `stda` | `[CommandMethod("Save_thep_dam_all")]`<br>`[CommandMethod("stda")]` | `BeamPersistenceController.SaveAllBeamsToDwg()` | Persistence | Batch serialization of all beam reinforcement models in current DWG to Extension Dictionaries. |
| **21** | `Add_thep_dam` | `atd` | `[CommandMethod("Add_thep_dam")]`<br>`[CommandMethod("atd")]` | `BeamPersistenceController.AddRebarLayerInteractive()` | Editing & Preview | Opens interactive dialog to add, modify, or delete extra rebar layers (top/bottom) on selected beam model. |
| **22** | `show_thep_dam` | `shtd` | `[CommandMethod("show_thep_dam")]`<br>`[CommandMethod("shtd")]` | `BeamPersistenceController.PreviewBeamRebar2D()` | Editing & Preview | Displays 2D WPF graphical preview dialog showing rebar cage layout, curtailment cutoffs, and stirrup density zones. |
| **23** | `Etabs` | `Etabs` | `[CommandMethod("Etabs")]` | `BeamEtabsController.ConnectEtabsAndImport()` | Design Interop | Connects to active ETABS OAPI session, extracts frame elements, bending moments $M_3$, shear $V_2$, and required rebar area $A_s$. |
| **24** | `loadbeam` | `loadbeam` | `[CommandMethod("loadbeam")]` | `BeamEtabsController.LoadFromExcelKataDam()` | Design Interop | Imports beam geometry, spans, sections, and reinforcement specification from active or selected `Kata.xlsm` sheet `Dam`. |
| **25** | `GanTai` / `HoatTai` | `GanTai`<br>`HoatTai` | `[CommandMethod("GanTai")]`<br>`[CommandMethod("HoatTai")]` | `BeamEtabsController.AssignEtabsLoads()` | Design Interop | Distributes dead load (`GanTai`) and live load (`HoatTai`) from slab tributary widths directly into active ETABS beam elements via OAPI. |

---

## 3. DWG Entity Creation & XData Schema Specification

### 3.1 Polyline Profile Geometries
AutoCAD beam detailing creates structured 2D vector profiles in ModelSpace on standard Kata layers:

```
+-----------------------------------------------------------------------------------------------+ Top Concrete
|   c1     |                              Clear Span Ln1                             |    c2    |
|<-Column->|                                                                         |<-Column->|
|          +-------------------------------------------------------------------------+          | Soffit Concrete
|          |                                                                         |          |
|          |         [Zone 1: Ln/4]                   [Zone 2: Ln/2]                 |          |
|          |         Stirrup a100                     Stirrup a200                   |          |
|          |                                                                         |          |
|==========+=========================================================================+==========| Bottom Concrete
```

1. **Elevation Concrete Contours (`kata_net thay`, Color 4 Cyan, Lineweight 0.30mm)**:
   - Top edge: Continuous horizontal line along the top of all spans at beam elevation `ct_dam`.
   - Bottom edge: Horizontal lines along the soffit of each span at `ct_dam - h_i`.
   - Vertical column boundaries: Lines at each support face defining clear span $L_{ni} = L_{\text{grid}} - c_{\text{left}}/2 - c_{\text{right}}/2$.
   - Cantilever drops: Vertical stepped profiles for cantilevers or variable depth spans.
2. **Cross-Section Contours (`kata_net thay`)**:
   - Closed rectangular polylines $b \times h$ at three standard cut locations:
     - Section 1-1: Left support dense zone ($L_n/4$).
     - Section 2-2: Midspan zone ($L_n/2$).
     - Section 3-3: Right support dense zone ($L_n/4$).
   - Slab soffit flanges: When monolithic with floor slab, flanges are drawn at depth $h_{\text{san}}$ (Config item 16, default cover $c=30\text{ mm}$).

### 3.2 Rebar Line & Distribution Geometry
All rebar entities are created as AutoCAD 2D `Polyline` (`AcDbPolyline`) with fillet arcs at bend corners on dedicated layers:

1. **Top Longitudinal Main Bars (`kata_thep chu`, Color 1 Red, Lineweight 0.50mm)**:
   - Positioned at concrete top cover offset ($y = \text{Top} - c - \Phi_{\text{stirrup}} - \Phi/2$).
   - Continuous through interior supports.
   - At exterior end supports, terminates with a standard $90^\circ$ cog bending downward into column core:
     $$L_{\text{cog}} = \min(h - 2c, 12\Phi) \ge 150\text{ mm}$$
2. **Bottom Longitudinal Main Bars (`kata_thep chu`, Color 1 Red, Lineweight 0.50mm)**:
   - Positioned at bottom cover offset ($y = \text{Bottom} + c + \Phi_{\text{stirrup}} + \Phi/2$).
   - Continuous across interior supports.
   - At exterior end supports, terminates with a standard $90^\circ$ cog bending upward ($L_{\text{cog}} = 12\Phi$).
3. **Top Additional Reinforcement / Support Extra Bars (`thep_goi`)**:
   - **Layer 1 (`stt_thep_chinh = "T1"`)**: Cutoff from column face at:
     $$L_{\text{cutoff,1}} = \max\left( \frac{L_{n,\text{left}}}{4}, \frac{L_{n,\text{right}}}{4} \right) \text{ or } 0.25 L_n$$
   - **Layer 2 (`stt_thep_chinh = "T2"`)**: Cutoff from column face at:
     $$L_{\text{cutoff,2}} = \max\left( \frac{L_{n,\text{left}}}{5}, \frac{L_{n,\text{right}}}{5} \right) \text{ or } 0.20 L_n$$
   - Shifted down vertically by clearance distance $s_{\text{layer}} = \max(\Phi, 25\text{ mm})$ to prevent overlapping.
4. **Bottom Additional Reinforcement / Midspan Extra Bars (`thep_nhip`)**:
   - **Layer 1 & Layer 2 (`stt_thep_chinh = "D1"`, `"D2"`)**: Centered at midspan.
   - Start cutoff: $0.10 L_n \text{ to } 0.15 L_n$ from left column face.
   - End cutoff: $0.10 L_n \text{ to } 0.15 L_n$ from right column face.
5. **Side Face Web Skin Bars (`cot_gia` / `thep_gia`, Layer `kata_thep chu`)**:
   - Generated when beam depth $h \ge 700\text{ mm}$ (TCVN 5574:2018 §10.3.2).
   - Diameter $\Phi 12 \text{ to } \Phi 14$, vertical spacing $s \le 400\text{ mm}$.
   - Tied with cross-ties (`dang18`) across the beam width.
6. **Stirrups (`kata_thep dai`, Color 220 Magenta, Lineweight 0.35mm)**:
   - Outer closed stirrups: Shape `dang22` (Box with two $135^\circ$ seismic hooks $L_{\text{hook}} = 10\Phi \ge 75\text{ mm}$).
   - Zone distribution along clear span $L_n$:
     - **Left dense zone** ($0$ to $L_n/4$): Spacing $a_{\text{dense}}$ (e.g. `a100`).
     - **Midspan zone** ($L_n/4$ to $3L_n/4$): Spacing $a_{\text{mid}}$ (e.g. `a200`).
     - **Right dense zone** ($3L_n/4$ to $L_n$): Spacing $a_{\text{dense}}$ (e.g. `a100`).
   - First stirrup placed at $s_0 = 50\text{ mm}$ from column face.
7. **Secondary Beam Intersections (`dam_giao`, `giao_dam`)**:
   - Hanger stirrup cage: $5\Phi 10a50$ (5 closed stirrups spaced at $50\text{ mm}$) centered on secondary beam centerline.
   - Anti-crack diagonal rebar: $2\Phi 14$, $L = 1200\text{ mm}$ inclined at $45^\circ$ across intersection corners.
8. **Shop Drawing 11.7m Curtailment (`vds`, `vdas`, `vgms`)**:
   - Maximum bar stock length: $11,700\text{ mm}$.
   - Splice locations restricted to compression zones:
     - Top bars: Spliced within middle $L_n/2$ of span.
     - Bottom bars: Spliced over supports within $L_n/4$ of column centerline or inside column core.
   - Compression lap length: $L_{\text{lap}} = 30\Phi$.
   - Staggered splices: Adjacent bars have splice centers offset by $\ge 1.3 L_{\text{lap}}$ (50% stagger rule).

### 3.3 Dimension Styles, Leaders & Blocks

1. **Dimension Styles**:
   - Master style: `kata_dim_iso` (Arrow: `kata_block_ArchTick`, Text: `kata_text`, TextHeight: $2.5\text{ mm}$, DimScale: $1/25$).
   - Rebar distribution dimstyle: `kata_rai_thep` (Arrow: `kata_block_RUNTIC`, Text: $2.0\text{ mm}$, formatting: e.g. `12%%c10a100`).
2. **Rebar Tag Block (`kata_block_KHT` / `thep`)**:
   - Circle tag divided by horizontal separator line.
   - Upper attribute: `SH` (Bar mark integer, e.g. `"1"`, `"2a"`).
   - Lower attribute: `SL_DK_KC` (e.g. `"2Ø20"` or `"Ø10a100"`).
   - Additional attributes: `CD1T` (cutting length in mm), `TCD` (total length in meters).
3. **Beam Plan Tag Block (`kata_block_Beam_plan`)**:
   - Tag placed on framing plan along beam axis.
   - Attributes: `NAME_BEAM` (`"D1"`), `B` (`"300"`), `H` (`"600"`), `LEVEL` (`"+3.600"`), `SPAN` (`"3"`).
4. **Beam Section Rebar Block (`Kata_Block_Beam_Rebar`)**:
   - Carried on cross-sections to store section reinforcement data.
   - Attributes: `U_NGOAI`, `THEP_T`, `THEP_D`, `THEP_GC`, `CD`, `A_DAI`.

### 3.4 XData RegApp `"KATA"` Schemas

Every beam rebar polyline carries structured XData under RegApp `"KATA"` matching legacy KataPro layout:

```
DxfCode 1001: "KATA"
  ├── 1000: Dang              -> "22" (Closed stirrup) or "01" (Straight) or "02" (U-bar)
  ├── 1070: STT               -> Bar mark integer (e.g. 1, 2, 5)
  ├── 1070: phi               -> Nominal diameter mm (e.g. 10, 18, 20, 22)
  ├── 1000: kc                -> Spacing notation (e.g. "@100", "@200", or "" for main bars)
  ├── 1000: sl                -> Bar count string (e.g. "2", "3", "24")
  ├── 1070: sl_ck             -> Member count multiplier (Int16: e.g. 1 or 4)
  ├── 1000: L                 -> Semicolon-delimited segment lengths: "L1;L2;L3;L4;L5"
  ├── 1000: goc_uon           -> Special bend angle (e.g. "90", "135")
  ├── 1000: ten_ck            -> Beam mark name (e.g. "D1-TANG 2", "DM1")
  ├── 1000: stt_thep_chinh    -> Sub-layer identifier: "T" (top), "D" (bottom), "G" (side)
  ├── 1000: chu_thich         -> Fabrication notes (e.g. "Coupler 1 đầu")
  ├── 1000: Z                 -> Floor elevation string (e.g. "+3.600")
  ├── 1005: LinkedHandle      -> Database Handle of linked tag block (e.g. "4A2C")
  ├── 1000: LinkType          -> "THEP_DON" or "THEP_DAI"
  └── 1005: LinkedDimHandle   -> Database Handle of distribution dimension line
```

### 3.5 Extension Dictionary XRecord JSON Persistence (`std`, `stda`)

For lossless persistence of full continuous beam models (`info_vedam`), the beam root entity (or a dedicated `KATA_BEAM_DATA` dictionary in Named Object Dictionary) stores the serialized JSON payload:

```json
{
  "TenDam": "D1",
  "SoCauKien": 1,
  "SoNhip": 3,
  "CaoTrinh": 3.6,
  "LopBaoVe": 30.0,
  "Spans": [
    {
      "SpanIndex": 0,
      "ClearSpanLength": 5400.0,
      "Width": 300.0,
      "Height": 600.0,
      "LeftColumnWidth": 400.0,
      "RightColumnWidth": 400.0,
      "SlabThickness": 120.0,
      "TopMainBars": "2Ø20",
      "BottomMainBars": "2Ø20",
      "TopExtraBarsLeft": "2Ø18",
      "TopExtraBarsRight": "2Ø18",
      "BottomExtraBars": "2Ø18",
      "StirrupDense": "Ø10a100",
      "StirrupSparse": "Ø10a200",
      "DenseZoneLength": 1350.0
    }
  ],
  "Intersections": [
    {
      "DistanceAlongSpan": 2700.0,
      "SecondaryBeamName": "D10",
      "SecondaryBeamWidth": 220.0,
      "HangerStirrup": "5Ø10a50"
    }
  ]
}
```

### 3.6 Updates to `GoiLenh_HPKata.lsp`

All 25 command aliases are fully verified and aligned with `GoiLenh_HPKata.lsp`:
- Lines 46, 49, 50, 53: `raidam`, `giaodam`, `catdam`, `tkdam`.
- Lines 69–77: `vd`, `vds`, `vdm`, `vgm`, `vgms`, `vdmc`, `damgiao`, `vdx`, `vdc`.
- Lines 84, 87–96: `loadbeam`, `qd`, `qda`, `std`, `stda`, `atd`, `shtd`, `vda`, `vdas`, `vdmca`, `Etabs`.
- Companion commands: `GanTai`, `HoatTai`.

---

## 4. Revit Framing Integration & Services Architecture

### 4.1 Multi-Span Continuous Beam Recognition (`OST_StructuralFraming`)

In Revit 2026, structural beams are modeled as individual `FamilyInstance` elements of category `BuiltInCategory.OST_StructuralFraming`. KataPro requires assembling these discrete elements into continuous multi-span beam runs:

```
[Column C1] ====== Framing Element 1 ====== [Column C2] ====== Framing Element 2 ====== [Column C3]
            (Collinear vector, touching endpoints, same Level, shared Mark or axis)
```

#### Assembly Algorithm:
1. **Query Framing Elements**:
   Filter all `FamilyInstance` of category `OST_StructuralFraming` on the selected level.
2. **Axis Collinearity Check**:
   For candidate elements $A$ and $B$, evaluate their location curves $C_A$ and $C_B$.
   - Direction vectors: $\vec{u}_A = \text{norm}(P_{A2} - P_{A1})$, $\vec{u}_B = \text{norm}(P_{B2} - P_{B1})$.
   - Collinear condition: $|\vec{u}_A \cdot \vec{u}_B| \ge 0.999$.
3. **Endpoint Proximity & Chain Assembly**:
   - Test touching distance: $\min(\text{dist}(P_{A,\text{end}}, P_{B,\text{start}})) \le 50\text{ mm}$ ($0.164\text{ ft}$).
   - Order spans sequentially from left-to-right (or bottom-to-top) along positive coordinate axis.
4. **Support Column Detection (`OST_StructuralColumns`)**:
   - At each beam node $P_i$, execute a `BoundingBoxIntersectsFilter` query for `OST_StructuralColumns`.
   - Measure column cross-section dimensions projected along the beam axis to extract column support widths $c_1, c_2, \dots$ in feet.
   - Calculate clear span length: $L_{ni} = L_{\text{centerline},i} - c_i/2 - c_{i+1}/2$.

### 4.2 Rebar Geometry Mapping & Creation Engine

All millimeter dimensions from `HPKata.Core` models (`BeamSpanModel`, `BeamRebarLayout`) are mapped to Revit internal coordinates (feet) via `RevitUnitConverter`:

1. **Curve Construction (`KataRebarCurveFactory`)**:
   - Continuous top main bars: Single linear `Line` segment extending from start column core to end column core, with downward $90^\circ$ start and end hooks.
   - Continuous bottom main bars: Linear `Line` segment with upward $90^\circ$ start and end hooks.
   - Extra support top bars: Placed symmetrically across interior column supports, centered on column centerline with total length $L_{\text{extra}} = c_i + L_{\text{cutoff,left}} + L_{\text{cutoff,right}}$.
   - Stirrups: Created as closed rectangular curve loops:
     - 4 bound `Line` segments forming the perimeter inside concrete cover.
     - Plane normal $\vec{n} = \vec{u}_{\text{beam}}$ (oriented along longitudinal beam axis).
2. **Revit 2026 API Rebar Creation**:
   - Uses `Rebar.CreateFromCurves`:
     ```csharp
     var terminations = new BarTerminationsData(doc);
     Rebar rebar = Rebar.CreateFromCurves(
         doc,
         RebarStyle.Standard, // or RebarStyle.StirrupTie for stirrups
         barType,
         hostBeam,
         normalVector,
         curves,
         terminations,
         true,
         true);
     ```
3. **Smart `RebarBarType` Resolution**:
   - Scans project `FilteredElementCollector(doc).OfClass(typeof(RebarBarType))`.
   - Matches by bar diameter in millimeters:
     $$|\text{RebarBarType.BarModelDiameter} \times 304.8 - \Phi_{\text{target}}| < 0.5\text{ mm}$$
   - If not found, duplicates the closest existing type and sets `BarModelDiameter = ToFeet(\Phi_{\text{target}})`.
4. **Hook Resolution (`RebarHookType`)**:
   - Main bar anchor hooks: `"Kata Rebar Hook - 90 deg."` (or standard `"Standard - 90 deg."`).
   - Stirrup seismic hooks: `"Kata Stirrup - 135 deg."` (or standard `"Stirrup/Tie - 135 deg."`).
   - Assigned via `rebar.SetHookTypeId(0, startHookId)` and `rebar.SetHookTypeId(1, endHookId)`.
5. **Array Layout Rules (`RebarShapeDrivenAccessor`)**:
   - **Stirrup Sets**:
     - Dense support zone ($L_n/4$): `RebarLayoutRule.MaximumSpacing`, `Spacing = ToFeet(100)`, `ArrayLength = ToFeet(L_dense)`.
     - Midspan zone ($L_n/2$): `RebarLayoutRule.MaximumSpacing`, `Spacing = ToFeet(200)`, `ArrayLength = ToFeet(L_mid)`.
   - **Longitudinal Bars**:
     - Multi-bar bundles (e.g. $3\Phi 20$): `RebarLayoutRule.FixedNumber`, `Quantity = 3`, `ArrayLength = ToFeet(b - 2c - 2\Phi_{\text{stirrup}} - \Phi)`.
6. **Parameter Synchronization & Traceability**:
   - `BuiltInParameter.NUMBER_PARTITION_PARAM` / `REBAR_PARTITION = "{BeamName}"` (e.g. `"D1-TANG 2"`).
   - `BuiltInParameter.REBAR_NUMBER` / `ALL_MODEL_MARK = "{STT}"` (e.g. `"1"`, `"2"`).
   - `BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS = "HPKata_{BeamName}"`.

### 4.3 Idempotency & Clean Re-run Pattern

To prevent duplication of rebar elements when re-running detailing or updating beam designs:

```csharp
public static void CleanExistingKataRebars(Document doc, Element hostBeam, string beamName)
{
    string targetComment = $"HPKata_{beamName}";
    var rebarsToDelete = new FilteredElementCollector(doc)
        .OfClass(typeof(Rebar))
        .WherePasses(new ElementOwnerViewFilter(ElementId.InvalidElementId)) // 3D model rebars
        .Cast<Rebar>()
        .Where(r => r.GetHostId() == hostBeam.Id)
        .Where(r => {
            var comment = r.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
            return comment != null && comment.StartsWith(targetComment, StringComparison.OrdinalIgnoreCase);
        })
        .Select(r => r.Id)
        .ToList();

    if (rebarsToDelete.Count > 0)
    {
        doc.Delete(rebarsToDelete);
    }
}
```

### 4.4 View Automation & Partition Isolation

1. **Longitudinal Section Generation (`ViewSection`)**:
   - Calculates oriented bounding box along continuous beam axis $\vec{u}_{\text{beam}}$.
   - Sets section cut depth to $b + 200\text{ mm}$ ($0.65\text{ ft}$).
   - Assigns View Name: `"HPKata_Elevation_{BeamName}"`.
2. **Transverse Cross-Section Generation**:
   - Places transverse section cuts at support ($0.15 L_n$) and midspan ($0.5 L_n$).
   - Assigns View Names: `"HPKata_Section_{BeamName}_Support"`, `"HPKata_Section_{BeamName}_Midspan"`.
3. **Partition Parameter Filter (`ParameterFilterElement`)**:
   - Creates view filter with rule: `REBAR_PARTITION != beamName`.
   - Adds filter to beam section views with visibility set to `False`, completely hiding rebars from adjacent beams, slabs, or columns.
4. **Rebar Schedule Automation (`ViewSchedule`)**:
   - Generates Structural Rebar Schedule filtered by `REBAR_PARTITION = beamName`.
   - Fields: `Mark`, `Bar Diameter`, `Total Bar Length`, `Quantity`, `Shape`, `Comments`.

---

## 5. Clean Code Architecture & Class Decomposition

Every file in the Phase 2 implementation strictly obeys `HP_CLEAN_CODE_CORE.md`:
- Max 300 lines per file (Rule C2).
- Max 50 lines per method (Rule M3).
- Zero dead code / commented-out blocks (Rule Q-B1).
- Zero unannotated empty catch blocks (Rule Q-B2).

### 5.1 Project Package Decomposition Blueprint

```
HPKata/
├── HPKata.Core/                              # Target: netstandard2.0;net8.0 (Host-Neutral)
│   └── Beams/
│       ├── BeamSpanModel.cs                  # [<=120 lines] Single span geometry, clear length, support widths, sections
│       ├── BeamContinuousModel.cs            # [<=150 lines] Multi-span container, member count, elevation, grid lines
│       ├── BeamSectionModel.cs               # [<=100 lines] Section geometry (b, h, cover, slab thickness, flanges)
│       ├── BeamRebarLayout.cs                # [<=180 lines] Longitudinal bars, extra cutoff layers, stirrup zones, skin bars
│       ├── BeamCurtailmentCalculator.cs      # [<=160 lines] Cutoff ratios (0.2L, 0.25L), anchor cogs, dense stirrup math
│       ├── BeamShopCurtailmentEngine.cs      # [<=220 lines] 11.7m commercial bar division, compression splice staggering
│       ├── BeamIntersectionSolver.cs         # [<=160 lines] Secondary-main intersection hanger stirrups and anti-crack bars
│       ├── FoundationBeamCalculator.cs       # [<=180 lines] Grade beam & tie beam detailing algorithms
│       ├── CurvedInclinedBeamCalculator.cs   # [<=200 lines] Sloped knee-joints & curved beam developed geometry
│       ├── EtabsBeamImportService.cs         # [<=240 lines] ETABS geometry, force envelopes, As auto-sizing
│       ├── BeamPersistenceSerializer.cs      # [<=180 lines] DWG Extension Dictionary XRecord JSON serialization
│       └── BeamScheduleTakeoffEngine.cs      # [<=150 lines] Tabular schedule takeoff calculation from beam models
│
├── HPKata.UI/                                # Target: net8.0-windows (Shared WPF MVVM)
│   ├── ViewModels/
│   │   ├── BeamDetailingViewModel.cs         # [<=250 lines] Continuous beam spans, bar sizing, section cuts, reactive preview
│   │   ├── BeamShopViewModel.cs              # [<=200 lines] 11.7m stock bar cutting, lap splice offsets, coupler options
│   │   └── EtabsImportViewModel.cs           # [<=220 lines] ETABS story/frame tree, forces preview, rebar assignment rules
│   └── Views/
│       ├── BeamDetailingWindow.xaml/.cs      # [<=200 lines] Theme-aware dialog with 2D elevation/section canvases
│       └── EtabsImportWindow.xaml/.cs        # [<=180 lines] Theme-aware dialog for ETABS import and force mapping
│
├── HPKata.AutoCad/                           # Target: net8.0-windows (AutoCAD 2026 Add-In)
│   ├── Commands/
│   │   ├── BeamCommands.cs                   # [<=120 lines] [CommandMethod] vd, vda, vds, vdas, vdmc, vdmca
│   │   ├── FoundationBeamCommands.cs         # [<=90 lines]  [CommandMethod] vdm, vgm, vgms
│   │   ├── CurvedInclinedBeamCommands.cs     # [<=80 lines]  [CommandMethod] vdx, vdc
│   │   ├── BeamIntersectionCommands.cs      # [<=90 lines]  [CommandMethod] damgiao, giaodam, catdam, raidam
│   │   ├── BeamScanCommands.cs               # [<=90 lines]  [CommandMethod] tkdam, qd, qda
│   │   ├── BeamPersistenceCommands.cs        # [<=90 lines]  [CommandMethod] std, stda, atd, shtd
│   │   └── BeamEtabsCommands.cs              # [<=90 lines]  [CommandMethod] Etabs, loadbeam, GanTai, HoatTai
│   ├── Controllers/
│   │   ├── BeamDrawController.cs             # [<=220 lines] Interactive & batch elevation drafting
│   │   ├── BeamShopController.cs             # [<=200 lines] Shop drawing drafting with 11.7m curtailment marks
│   │   ├── BeamSectionController.cs          # [<=180 lines] Cross-section cut generation at supports/midspan
│   │   ├── BeamIntersectionController.cs     # [<=160 lines] Hanger cage insertion and beam splitting
│   │   ├── BeamScanController.cs             # [<=220 lines] Framing plan recognition scanner and tag layout
│   │   ├── BeamPersistenceController.cs      # [<=180 lines] XRecord / JSON serialization and 2D WPF preview
│   │   └── BeamEtabsController.cs            # [<=240 lines] ETABS OAPI bridge and Excel sheet Dam loader
│   └── Interop/
│       ├── BeamDwgGeometryService.cs         # [<=250 lines] Drafting helper for beam polylines, hatches, dims, text
│       └── BeamBlockAttributeService.cs      # [<=160 lines] Synchronizing kata_block_Beam_plan and section blocks
│
└── HPKata.Revit/                             # Target: net8.0-windows (Revit 2026 Add-In)
    ├── Commands/
    │   └── RevitBeamCommands.cs              # [<=120 lines] ExternalCommand entry points for beam detailing & ETABS import
    ├── Framing/
    │   ├── RevitFramingAnalyzer.cs           # [<=240 lines] Collinear beam chaining, column support detection, clear spans
    │   └── RevitFramingSelector.cs           # [<=150 lines] User selection filters for Structural Framing elements
    ├── NativeRebar/
    │   ├── RevitBeamRebarService.cs          # [<=250 lines] 3D Rebar creation for main bars, cutoffs, and stirrup arrays
    │   └── RevitBeamHookResolver.cs          # [<=140 lines] Hook type resolution and attachment for beam rebar ends
    ├── ViewAutomation/
    │   ├── RevitBeamViewService.cs           # [<=240 lines] Longitudinal/transverse ViewSection generation & tagging
    │   └── RevitBeamFilterService.cs         # [<=150 lines] ParameterFilterElement partition isolation rules
    └── Runners/
        ├── RevitBeamDetailingRunner.cs       # [<=200 lines] Orchestrates analysis, cleanup, rebar creation, and view generation
        └── RevitEtabsBeamRunner.cs           # [<=180 lines] Orchestrates ETABS force mapping to Revit beam instances
```

---

## 6. Detailed Implementation Algorithms & Code Sketches

### 6.1 Continuous Beam Detailing & Curtailment Calculator (`BeamCurtailmentCalculator.cs`)

```csharp
namespace HPKata.Core.Beams
{
    public static class BeamCurtailmentCalculator
    {
        public static double CalculateTopCutoffL1(double clearSpanMm, double ratio = 0.25) =>
            Math.Round(clearSpanMm * ratio, MidpointRounding.AwayFromZero);

        public static double CalculateTopCutoffL2(double clearSpanMm, double ratio = 0.20) =>
            Math.Round(clearSpanMm * ratio, MidpointRounding.AwayFromZero);

        public static (double StartOffsetMm, double EndOffsetMm) CalculateBottomCutoffs(double clearSpanMm, double ratio = 0.10)
        {
            double offset = Math.Round(clearSpanMm * ratio, MidpointRounding.AwayFromZero);
            return (offset, clearSpanMm - offset);
        }

        public static double CalculateDenseStirrupZone(double clearSpanMm, double beamHeightMm)
        {
            // Standard: greater of Ln/4 or 2*h (TCVN 5574:2018 §10.3.3)
            double byRatio = clearSpanMm / 4.0;
            double byHeight = 2.0 * beamHeightMm;
            return Math.Round(Math.Max(byRatio, byHeight), MidpointRounding.AwayFromZero);
        }

        public static double CalculateAnchorCog(int diaMm, double availableDepthMm)
        {
            double standardCog = 12.0 * diaMm;
            double maxDepthCog = availableDepthMm - 50.0; // allow for covers
            return Math.Max(150.0, Math.Min(standardCog, maxDepthCog));
        }
    }
}
```

### 6.2 11.7m Commercial Bar Curtailment Engine (`BeamShopCurtailmentEngine.cs`)

```csharp
namespace HPKata.Core.Beams
{
    public class BeamShopCurtailmentEngine
    {
        public const double CommercialBarLengthMm = 11700.0;

        public static List<RebarCutSegment> SegmentLongitudinalBar(
            double totalRunLengthMm,
            int diaMm,
            bool isTopBar,
            IReadOnlyList<BeamSpanModel> spans)
        {
            var segments = new List<RebarCutSegment>();
            double lapLength = 30.0 * diaMm;

            if (totalRunLengthMm <= CommercialBarLengthMm)
            {
                segments.Add(new RebarCutSegment(0, totalRunLengthMm, diaMm));
                return segments;
            }

            double currentStart = 0.0;
            while (currentStart < totalRunLengthMm)
            {
                double targetLength = Math.Min(CommercialBarLengthMm, totalRunLengthMm - currentStart);
                double candidateEnd = currentStart + targetLength;

                // Adjust cut point to avoid high tension zones
                double validCutPoint = FindValidSpliceLocation(candidateEnd, isTopBar, spans, lapLength);
                double actualLength = validCutPoint - currentStart;

                segments.Add(new RebarCutSegment(currentStart, actualLength, diaMm));
                currentStart = validCutPoint - lapLength; // overlap for next bar
            }

            return segments;
        }

        private static double FindValidSpliceLocation(
            double candidateCoord,
            bool isTopBar,
            IReadOnlyList<BeamSpanModel> spans,
            double lapLength)
        {
            // Top bars: splice near midspan (0.4 Ln to 0.6 Ln)
            // Bottom bars: splice near supports (within 0.2 Ln of column face)
            // Snaps candidateCoord to the nearest valid zone
            return candidateCoord; // simplified stub
        }
    }
}
```

### 6.3 Secondary-Main Intersection Solver (`BeamIntersectionSolver.cs`)

```csharp
namespace HPKata.Core.Beams
{
    public static class BeamIntersectionSolver
    {
        public static BeamIntersectionResult SolveIntersection(
            double mainBeamWidthMm,
            double mainBeamHeightMm,
            double secondaryBeamWidthMm,
            double secondaryBeamHeightMm,
            int hangerDiaMm = 10,
            int hangerCount = 5,
            double hangerSpacingMm = 50.0)
        {
            double cageZoneWidth = (hangerCount - 1) * hangerSpacingMm;
            double startOffset = -(secondaryBeamWidthMm / 2.0) - (cageZoneWidth / 2.0);

            var hangerOffsets = new List<double>(hangerCount);
            for (int i = 0; i < hangerCount; i++)
            {
                hangerOffsets.Add(startOffset + i * hangerSpacingMm);
            }

            return new BeamIntersectionResult
            {
                HangerOffsetsMm = hangerOffsets,
                HangerDiameterMm = hangerDiaMm,
                HangerCount = hangerCount,
                RequiresAntiCrackBars = secondaryBeamHeightMm >= 400.0,
                AntiCrackBarSpec = "2Ø14 L=1200"
            };
        }
    }
}
```

### 6.4 ETABS Beam Force & Rebar Importer (`EtabsBeamImportService.cs`)

```csharp
namespace HPKata.Core.Beams
{
    public class EtabsBeamImportService
    {
        public static (string BarString, int BarCount, int DiameterMm) SelectOptimalRebar(
            double requiredAsCm2,
            int beamWidthMm,
            int coverMm = 30)
        {
            int[] availableDiameters = [16, 18, 20, 22, 25, 28, 32];
            double usableWidth = beamWidthMm - 2 * coverMm - 20; // minus stirrups

            foreach (int dia in availableDiameters)
            {
                double singleArea = Math.PI * dia * dia / 400.0; // cm2
                int count = (int)Math.Ceiling(requiredAsCm2 / singleArea);
                if (count < 2) count = 2; // minimum 2 bars

                double requiredSpacing = (usableWidth - count * dia) / (count - 1);
                if (requiredSpacing >= 25.0) // TCVN clear spacing >= 25mm
                {
                    return ($"{count}Ø{dia}", count, dia);
                }
            }

            // Fallback to two layers of Dia 25
            return ("4Ø25", 4, 25);
        }
    }
}
```

---

## 7. Verification & Testing Strategy

### 7.1 Automated Unit Tests (`HPKata.Core.Tests`)
Expand test coverage with $\ge 50$ dedicated unit tests covering Phase 2 models and calculators:
1. `BeamSpanModelTests`: Verify span clear length, support width offsets, and section geometry.
2. `BeamCurtailmentCalculatorTests`:
   - Single-span, 3-span continuous, and cantilever beam cutoff locations.
   - Stirrup dense zone boundary math ($L_n/4$ vs $2h$).
   - Exterior column anchor cog calculation ($12\Phi$ vs column depth clamp).
3. `BeamShopCurtailmentEngineTests`:
   - Bars $< 11.7\text{ m}$ yield single uncut segment.
   - Bars $> 11.7\text{ m}$ segment cleanly with compression zone lap splices ($30\Phi$).
   - 50% staggered splice offsets ($1.3 L_{\text{lap}}$).
4. `BeamIntersectionSolverTests`:
   - Symmetry of hanger stirrups around secondary beam web.
   - Accurate hanger spacing ($50\text{ mm}$) and anti-crack trigger.
5. `EtabsBeamImportServiceTests`:
   - Auto-sizing algorithms from $A_s$ (cm²) to valid bar diameters and bar counts satisfying clear spacing rules.
6. `BeamPersistenceSerializerTests`:
   - Full round-trip JSON serialization and deserialization of `BeamContinuousModel`.

### 7.2 AutoCAD Host Verification (`HPKata.AutoCad.Tests`)
1. All 25 `[CommandMethod]` entry points registered with zero reflection or binding errors.
2. `XDataService` round-trip verifies correct `1001:"KATA"` and handle linking fields on generated rebar polylines and tag blocks.
3. Extension Dictionary `XRecord` verified to store and retrieve valid `BeamContinuousModel` JSON payloads.

### 7.3 Revit Host Verification (`HPKata.Revit.Tests`)
1. `RevitFramingAnalyzer` correctly chains collinear `OST_StructuralFraming` elements and measures column support widths.
2. `RevitBeamRebarService` creates native 3D `Rebar` elements with correct `RebarBarType`, `RebarHookType`, and `RebarShapeDrivenAccessor` arrays.
3. `NUMBER_PARTITION_PARAM`, `ALL_MODEL_MARK`, and `ALL_MODEL_INSTANCE_COMMENTS` parameters populated accurately.
4. `CleanExistingKataRebars` successfully purges existing rebars idempotently before regeneration without affecting unmanaged rebars.
5. `RevitUnitConverter` enforces single source of truth for coordinate conversion ($1 / 304.8$, Rule R5).

### 7.4 Concurrency & MTP Isolation
All build and test executions must strictly supply `--disable-build-servers -m:1` and unique test results directories to satisfy the mandatory concurrency directives.

---
*End of Phase 2 Architectural Specification & Command Mapping Report.*
