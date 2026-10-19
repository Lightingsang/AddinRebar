# BEAM SHOP DRAWING & ETABS INTEGRATION SPECIFICATION REPORT
## Deep Reverse-Engineering Specification for HPKata Phase 2 (`Phân hệ Dầm & ETABS`)

- **Document ID**: `HPKATA-SPEC-M1-BEAM-2026`
- **Version**: `1.0.0-PROD`
- **Author**: explorer_m1_1 (Spec Miner)
- **Target Deliverable**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)
- **Milestone**: Milestone M1 (Phase 2: Beam Shop Drawing & ETABS Integration)
- **Status**: AUTHORITATIVE & BINDING
- **Governance**: Clean-Room Reverse Engineering Protocol (Strictly Read-Only, Zero DRM / Security Inspection, Full Compliance with `docs/clean-code/HP_CLEAN_CODE_CORE.md`)

---

## 1. Executive Summary & Authoritative Spec Sources

This report establishes the complete architectural, mathematical, structural detailing, and data interface specification for **Phase 2: Beam Shop Drawing & ETABS Integration** of the **HPKata** ecosystem. The specification has been reverse-engineered from primary reference artifacts in `C:\kata_pro`, verified against `Kata_Class_Lib.dll` (.NET 4.8), `Kata_pro64_Cad2013.dll`, `Kata_pro64_Cad2025.dll` (.NET 8.0), `Goi lenh.lsp`, `Kata.xlsm` (VBA modules `Ve_dam.bas`, `save_info.bas`, `Tinh_dam.bas`, sheets `Dam`, `ThepDamAllMBKC`, `save_info`), and aligned with the existing codebase baseline in `HPRebar.Core`.

### 1.1 Inspected Authoritative Sources (Read-Only)
1. **`C:\kata_pro\Kata_Class_Lib.dll`**: Primary shared domain assembly containing unobfuscated data structures:
   - `Kata_Class_Lib.info_vedam`: Continuous beam geometry, section profiles, supports, and rebar layout.
   - `Kata_Class_Lib.Info_Beam3D`: 3D analytical and boundary polygon beam model with centerlines and rebar blocks.
   - `Kata_Class_Lib.info_BlockBeam_ThepBoTri`: Span rebar layout, top/bottom layers, stirrup arrays, skin rebar, and tie inputs.
   - `Kata_Class_Lib.info_BlockBeam_AllData1nhip`: Composite single-span model binding ETABS analysis data (`DataEtabs`) with detailing layout (`ThepBoTri`).
   - `Kata_Class_Lib.info_BlockBeam_DataEtabs`: Longitudinal and shear reinforcement areas and internal forces.
   - `Kata_Class_Lib.info_dam_giao_cotcay` & `info_dam_giao_taicot`: Secondary beam intersection geometric models.
   - `Kata_Class_Lib.info_BeamEtabs_Thep` & `info_BeamEtabs_Noiluc`: ETABS required steel areas ($A_{s\_top}, A_{s\_bot}, A_v/s$) and design forces ($M_{top}, M_{bot}, V$).
   - `Kata_Class_Lib.Loai_thep_gc_goi`, `Loai_dam`, `TypeBeam`: Core beam structural enumerations.
   - `Kata_Class_Lib.info_lap_anchor`, `info_cog_hook`, `info_mc`, `thep_in`, `InfoThep`: Rebar geometry, hook, bend radius, and detailing parameters.
2. **`C:\kata_pro\Kata.xlsm`**:
   - Sheet `Dam`: Master 80-row interactive continuous beam detailing input template.
   - Sheet `ThepDamAllMBKC`: Standard multi-span framing plan table schema with 44 column definitions (`B..AR`).
   - Sheet `save_info`: Flat table persistence ledger for serialized beam records.
   - VBA `Ve_dam.bas`: Interactive CAD coordinate picking (`laychieudai`), rebar text parsers (`chuyen_thep`, `chuyen_ht`), and chart graphics.
   - VBA `save_info.bas`: Serialization/deserialization routines (`save`, `load_data`), unit weight table (`tra_bang_kl`), and `small_kata`.
   - VBA `Tinh_dam.bas`: TCVN 5574:2018 flexural design ($\alpha_m, \zeta, A_s$), shear design ($q_{sw}, Q_{db}$), and layer centroid depth ($a$).
3. **`C:\kata_pro\Goi lenh.lsp`**:
   - Lines 33–151: 25 beam-related AutoCAD command aliases.
   - Lines 152–237: 86-parameter configuration block specifying default lap lengths, anchorage multipliers, top extra cutoffs ($0.2L, 0.25L$), stirrup dense zone parameters, and hook types.
4. **`HPRebar/HPRebar.Core/KataRebar/`**: Authoritative clean C# baseline:
   - `KataDetailingRuleBuilder.cs`, `KataAnchorage.cs`, `KataRebarCalculator.cs`, `KataBeamStations.cs`, `KataMainBarLayout.cs`, `KataSupportTopBarLayout.cs`, `KataSpanBottomBarLayout.cs`, `KataStirrupZoneLayout.cs`, `KataSideBarLayout.cs`, `KataHangerBarLayout.cs`.

---

## 2. Specification Miner: Features Discovered Table

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Continuous Beam | `ve_dam` (`vd`) | Parametric multi-span continuous beam detailing (elevation, section cuts, dimensions, tags) | Span lengths, sections $b \times h$, top/bot rebar, stirrup zones | Full 2D DWG drawing (elevation + cross-sections) | Aborts with error if span length $L \le 0$ or beam height $h \le 0$ | `Goi lenh.lsp:69`, `Lenh_thep::Vedam`, `FormBeamRebar` |
| 2 | Beam Shop | `ve_dam_shop` (`vds`) | Shop drawing beam detailing with commercial bar cutting ($11,700\text{ mm}$) and staggered lap splices in compression zones | Beam geometry + lap splice staggering rules ($50\%$ stagger) | Shop drawing elevation with discrete bar cutting segments | Flags warning if lap splice falls into high-tension zone | `Goi lenh.lsp:70`, `Lenh_thep::Vedam_shop`, `thep_in` |
| 3 | Batch Detailing | `ve_dam_all` (`vda`) | Batch continuous beam drawing generation from structural framing plan scan data | Cached beam scan results from `quet_dam` | Multiple completed beam elevation drawings | Skips beams lacking support column boundaries | `Goi lenh.lsp:93`, `Lenh_thep::Vedam_all`, `Add_ThepDamAllMBKC` |
| 4 | Batch Shop | `ve_dam_all_shop` (`vdas`) | Batch shop drawing generation for all continuous beams across a floor | Plan scan data + 11.7m commercial bar rules | Batch shop drawings with cutting lengths and coupler marks | Flags spans exceeding commercial length without couplers | `Goi lenh.lsp:94`, `Lenh_thep::Vedam_all_shop` |
| 5 | Cross Sections | `ve_dam_mc` (`vdmc`) | Generates transverse cross-sections ($b \times h$) at supports and midspans showing stirrup legs and bar layers | Selected beam entity or station coordinate | $b \times h$ cross-section view with closed stirrups, U-caps, C-ties | Throws error if section cut falls outside beam limits | `Goi lenh.lsp:74`, `Lenh_thep::Ve_dam_mc21`, `info_mc` |
| 6 | Batch Sections | `ve_damMC_all` (`vdmca`) | Batch cross-section generation for all detailed beams on current layout | All detailed beam definitions in session | Grid of transverse section cuts grouped per beam | Skips corrupt or incomplete beam models | `Goi lenh.lsp:95`, `Lenh_thep::VedamMC_all` |
| 7 | Inclined Beam | `ve_dam_xien` (`vdx`) | Details inclined beams, sloped roof rafters, and staircase landing stringers | Start/end elevations, horizontal run, section $b \times h$ | Developed elevation with cranked node rebar detailing | Rejects slope angle $> 60^\circ$ | `Goi lenh.lsp:76`, `Lenh_thep::Ve_dam_xien1`, `Form_Dam_Xien` |
| 8 | Curved Beam | `ve_dam_cong` (`vdc`) | Details horizontally curved circular beams on plan with developed true-length elevation | Arc radius $R$, sweep angle $\theta$, section $b \times h$, radial rebar | Developed 2D elevation + plan curve layout | Rejects radius $R <$ minimum bar bend radius | `Goi lenh.lsp:77`, `Lenh_thep::Vedamcong`, `Dam_cong_mbkc` |
| 9 | Foundation Beam | `ve_dam_mong` (`vdm`) | Details reinforced concrete foundation grade beams connecting pile caps/footings | Footing elevations, grade beam section $b \times h$, rebar | Foundation grade beam shop drawing | Warns if top elevation is inconsistent with cap tops | `Goi lenh.lsp:71`, `Lenh_thep::Vedammong`, `Loai_dam` |
| 10 | Tie Beam | `ve_giang_mong` (`vgm`) | Details foundation tie beams connecting isolated or pile foundations | Cap spacing, tie section $b \times h$, top/bottom steel | Tie beam elevation and section cuts | Aborts if clear distance between caps $\le 0$ | `Goi lenh.lsp:72`, `Lenh_thep::Vegiangmong`, `Loai_dam` |
| 11 | Tie Beam Shop | `ve_giang_shop` (`vgms`) | Shop drawing detailing of tie beams with 11.7m rebar optimization and anchorage cogs | Tie beam parameters + commercial bar length | Shop drawing with discrete piece marks and offcuts | Flags excessive lap splices in short tie spans | `Goi lenh.lsp:73`, `Lenh_thep::Vegiangmong_shop` |
| 12 | Intersection Hangers | `dam_giao` (`damgiao`) | Calculates and details hanger stirrups and anti-crack bars at secondary beam intersections | Primary beam, secondary beam width $b_{sec}$, height $h_{sec}$ | Hanger stirrup cage (e.g. $5\Phi 10a50$) + $2\Phi 14$ diagonal bars | Ignores parallel non-intersecting beams | `Goi lenh.lsp:49`, `Lenh_thep::Ve_dam_giao1`, `info_dam_giao_cotcay` |
| 13 | Auto Intersection | `giao_dam` (`giaodam`) | Automated plan recognition of framing intersections; places hanger stirrup notes on plan | Framing plan entities | Intersection points, hanger callout tags | Bypasses intersections where heights are equal ($h_1 = h_2$) | `Goi lenh.lsp:75`, `Lenh_thep::Xu_ly_giao_dam` |
| 14 | Split Beam | `cat_dam` (`catdam`) | Splits continuous beam model at column faces, expansion joints, or construction joints | Selected beam entity and cutting point/line | Two independent beam models with individual rebar anchorage | Aborts if cutting point is outside beam span | `Goi lenh.lsp:50`, `Lenh_thep::Xu_ly_cat_dam` |
| 15 | Beam Tagging | `raidam` | Rải các block nhãn dầm (Beam Tag Block) tự động bám theo tim dầm trên mặt bằng kết cấu | Grid axes, beam centerline polylines | Block references `beam_tag` with attributes (Name, $b \times h$) | Skips lines not on designated beam center layer | `Goi lenh.lsp:46`, `Lenh_thep::Rai_block_dam1` |
| 16 | Beam Takeoff | `tkdam` | Creates tabular component schedule of beam marks, section sizes, lengths, and counts | Selection set of beam tag blocks on plan | AutoCAD Table or Block schedule | Highlights duplicate beam marks with conflicting sections | `Goi lenh.lsp:53`, `Lenh_thep::Tao_table_tk_cau_kien_block_beam1` |
| 17 | Plan Recognition | `quet_dam` (`qd`) | AI/algorithmic plan scanner detecting beam edges, columns, and clear spans | Window selection of lines, polylines, texts | In-memory structural topology of beams and supports | Reports unrecognized boundaries in red overlay | `Goi lenh.lsp:87`, `Lenh_thep::quet_dam`, `Thu_vien_mbkc` |
| 18 | Batch Plan Scan | `quet_dam_all` (`qda`) | Full-drawing scanning of all structural framing elements across all floors | Active DWG ModelSpace | Populates `Dictionary<string, Info_Beam3D>` | Logs unresolved column junctions to summary table | `Goi lenh.lsp:88`, `Lenh_thep::quet_dam_all` |
| 19 | Beam Persistence | `Save_thep_dam` (`std`) | Serializes beam reinforcement model into DWG Extension Dictionary (`XRecord` `"KATA_BEAM"`) | Configured beam rebar model | Persistent binary/string XRecord payload in DWG | Fails if DWG is read-only | `Goi lenh.lsp:89`, `Lenh_thep::Save_thep_dam` |
| 20 | Batch Persistence | `Save_thep_dam_all` (`stda`) | Batch serializes all beam models in the drawing into DWG XRecords and Excel `save_info` | All active beam models in session | Updated DWG dictionaries and Excel sheet `save_info` | Aborts on disk full or file lock | `Goi lenh.lsp:90`, `Lenh_thep::Save_thep_dam_all`, `save_info.bas` |
| 21 | Add Rebar Layer | `Add_thep_dam` (`atd`) | Interactive dialog to add extra longitudinal layers or custom stirrup cages to beam | Selected beam model | Updated `info_vedam` with additional rebar rows | Validates bar clearance; warns if spacing $< 25\text{ mm}$ | `Goi lenh.lsp:91`, `Lenh_thep::Add_thep_dam` |
| 22 | 2D Cage Preview | `show_thep_dam` (`shtd`) | Displays interactive 2D graphical preview canvas of beam rebar distribution | Selected beam definition | Modal preview dialog with longitudinal & cross views | Displays blank if beam model unpopulated | `Goi lenh.lsp:92`, `Lenh_thep::show_thep_dam`, `PreviewCanvas` |
| 23 | Excel Beam Import | `loadbeam` | Imports beam structural geometry and reinforcement from `Kata.xlsm` sheet `Dam` | Path to `Kata.xlsm` or active Excel COM session | Populates `info_vedam` and generates CAD detailing | Prompts error if sheet `Dam` missing or corrupted | `Goi lenh.lsp:84`, `Lenh_thep::Load_beam` |
| 24 | ETABS Integration | `Etabs` (`Etabs_Model`) | Connects to active ETABS 22 session via OAPI, extracts frame geometry, $M, V, A_s$ | Active ETABS model via COM | Frame geometry, section properties, forces, required $A_s$ | Reports "Cannot attach to ETABS" if session closed | `Goi lenh.lsp:96`, `Lenh_thep::Etabs_Model`, `ETABSv1` |
| 25 | ETABS Rebar Sizing | Rebar Auto-Selection | Solves multi-layer bar sizing from ETABS $A_{s\_top}, A_{s\_bot}, A_v/s$ meeting spacing rules | Required $A_s$ table, beam width $b$, cover, bar catalog | Selected bar count and diameter per layer (`chon_CacLopThep`) | Flags if required steel exceeds maximum ratio $\mu_{max} = 3\%$ | `Set_Rebar_Beam::Select_RebarBeamFromAsEtabs` |
| 26 | Revit Beam Framing | `KataRebarRevitBeam` | Creates native Revit `Rebar` elements inside `OST_StructuralFraming` beams | Revit `FamilyInstance` beam, `BeamSpanModel` | Native 3D `Rebar` elements with hooks and parameter tags | Rolls back Revit Transaction if curves are non-coplanar | `Kata_Class_Lib_Revit.Revit_rebar`, `HPRebar.Core` |

---

## 3. Specification Miner: Edge Cases Table

| # | Feature | Input Scenario / Edge Case | Observed & Authoritative Decompiled Behavior |
|---|---|---|---|
| 1 | Continuous Beam Geometry | Intermediate support width is 0 (point knife-edge support or suspended hanger) | `KataBeamStations` clamps support width to 0 mm; clear span equals center-to-center span; top negative extra bars anchor straight across the joint without column face offset. |
| 2 | Continuous Beam Geometry | Cantilever span at end (left or right overhang: $ct_{dam} > 0$ or $lt_{dam} > 0$) | System marks span as cantilever (`isCantilever = true`); top main bars run continuous to outer cantilever tip and bend $90^\circ$ downward; stirrup dense zone applies across full cantilever length. |
| 3 | Variable Beam Depth | Adjacent spans have different heights ($h_1 \ne h_2$, soffit step drop) | `KataTopProfile.Drape` and `info_giat_WC`: bottom bars in deeper span anchor straight into column; shallower span bottom bars crank or anchor at column face; closed stirrups size independently per span. |
| 4 | Stepped Top Flange | Beam has top elevation step drop (e.g. bathroom drop: $\Delta z = -50\text{ mm}$ or $-200\text{ mm}$) | Row 19 parser (`chuyen_ht`) detects $\Delta h$; top continuous bars crank down at $1:6$ slope if step $\le 50\text{ mm}$, or terminate with $90^\circ$ anchor hook and start new run if step $> 50\text{ mm}$. |
| 5 | Concrete Cover Resolution | Cell J9 contains `"a/b"` vs `"a"` vs empty | If `"a/b"`: $a$ is distance from face to main bar center, $b$ is stirrup clear cover. If `"a"` alone: stirrup wraps main bars, $b = a - d_{main}/2 - d_{stirrup}$. If empty: default $b = 25\text{ mm}$, $a = b + d_{stirrup} + d_{main}/2$. If $b \le 0$, flag error. |
| 6 | End Support Anchorage | End support column width is too narrow ($b_{col} - c < L_{anchor} = 40d$) | `KataAnchorage.Solve`: bar runs to far column face ($b_{col} - c$), bends $90^\circ$ vertically with leg $L_{leg} = L_{anchor} - (b_{col} - c)$. Leg is clamped to $h_{beam} - 2c_{main}$. If $L_{leg}$ exceeds depth, residual shortfall is logged. |
| 7 | End Support Leg Clash | Top bar $90^\circ$ leg and bottom bar $90^\circ$ leg clash in the same vertical plane | `KataAnchorage.LegsOverlap`: if $L_{leg\_top} + L_{leg\_bot} > \text{legRoom}$, bottom leg is offset inward along $X$ by $\text{inset} = (d_{top} + d_{bot})/2 + \max(25\text{ mm}, d_{bot})$. |
| 8 | Rebar Cutoff Curtailment | User enters span ratios in `H3:H5` ($0.2L$ vs $0.25L$ vs $L/4$ vs $L/3$) | Parser evaluates datum in `I3/I5` (`" mép"` = column face vs `" tâm"` = column centerline). Length is calculated from designated datum; layer 1 uses $L_1$, layer 2 uses $L_2 = L_1 - \text{stagger}$ (default stagger 500 mm). |
| 9 | Commercial Bar Length | Total continuous span length exceeds $11,700\text{ mm}$ | `thep_in` / `KataBarSplit`: splits long bar into $11.7\text{ m}$ segments with $30d$ lap splice in compression zone ($L/4$ to $L/2$ for top bars; $0$ to $L/4$ from support face for bottom bars). Laps staggered $50\%$. |
| 10 | Rebar Clear Spacing | High steel ratio causes clear spacing between bars $s < 25\text{ mm}$ | `Set_Rebar_Beam.kc_MepThep`: evaluates $s = \frac{b - 2c - 2d_s - n \cdot d_{main}}{n - 1}$. If $s < 25\text{ mm}$ ($30\text{ mm}$ for bottom), algorithm shifts excess bars into Layer 2 or Layer 3. |
| 11 | Secondary Beam Intersection | Secondary beam width $b_{sec} >$ primary beam width $b_{pri}$ | Geometry validator flags warning: secondary beam cannot bear fully on narrower primary beam; requires structural header corbel or inverted hanger cage. |
| 12 | Skin Reinforcement | Beam height $h \ge 700\text{ mm}$ with no skin bars specified in Excel row 20 | System triggers TCVN 5574:2018 mandatory rule: automatically generates side face bars $\Phi 12$ or $\Phi 14$ at vertical spacing $s_v \le 400\text{ mm}$ along each side face, bound with cross ties $\Phi 8a500$. |
| 13 | Multi-Leg Stirrups | Beam width $b \ge 400\text{ mm}$ requiring 4 or 6 stirrup legs | Evaluates stirrup type in rows 25–27: generates 1 outer closed hoop (□) + 1 inner closed hoop, or 1 outer hoop + 2 internal cross-ties (C), spacing legs evenly across width: $s_h \le 250\text{ mm}$. |
| 14 | Curved Beam Detailing | Curved beam sweep angle $\theta > 90^\circ$ or radius $R < 2000\text{ mm}$ | `Dam_cong_mbkc`: validates bar curvature limits; subdivides continuous curved bars into segmented arcs $\le 6.0\text{ m}$ to permit fabrication on standard ring benders without distortion. |
| 15 | ETABS Force Mapping | ETABS frame station forces have missing stations or un-designed frame elements | `EtabsBeamImportService`: validates station array; if intermediate station missing, uses cubic spline interpolation across frame length; if un-designed, flags `ERR_ETABS_NOT_DESIGNED`. |
| 16 | Excel COM Failure | Microsoft Excel process not running or COM call throws `0x80040154` | Dual-source reader falls back seamlessly to `ClosedXML` reading `.xlsm` file directly from disk without crashing CAD or Revit session. |

---

## 4. Multi-Span Continuous Beam Structural Domain Model

### 4.1 Structural Domain Entity Diagram

```
+---------------------------------------------------------------------------------+
|                                BeamSpanModel                                    |
+---------------------------------------------------------------------------------+
| - SpanIndex: int                                                                |
| - ClearLength: double (mm)                                                      |
| - LeftSupportWidth: double (mm)                                                 |
| - RightSupportWidth: double (mm)                                                |
| - Section: BeamSectionModel (b x h)                                             |
| - TopSoffitDrop: double (delta_z top, mm)                                       |
| - BottomSoffitDrop: double (delta_z bot, mm)                                    |
| - LeftSlabThickness: double (mm)                                                |
| - RightSlabThickness: double (mm)                                               |
| - SecondaryIntersections: List<BeamIntersectionModel>                          |
+---------------------------------------------------------------------------------+
                                      | 1
                                      | n
+---------------------------------------------------------------------------------+
|                              BeamRebarLayout                                    |
+---------------------------------------------------------------------------------+
| - TopContinuousBars: List<RebarRunModel> (e.g. 2f20, runs full length)          |
| - BottomContinuousBars: List<RebarRunModel> (e.g. 2f20, runs full length)       |
| - SupportTopExtraBars: Dictionary<int, List<ExtraBarLayerModel>> (Supports)     |
| - MidspanBottomExtraBars: Dictionary<int, List<ExtraBarLayerModel>> (Spans)     |
| - StirrupZones: Dictionary<int, StirrupZoneDistributionModel> (Spans)           |
| - SkinBars: List<RebarRunModel> (Side face web reinforcement)                   |
| - HangerBars: List<HangerStirrupGroupModel> (Intersections)                     |
| - SpacerTies: List<SpacerTieModel> (Between rebar layers)                       |
+---------------------------------------------------------------------------------+
```

### 4.2 C# Domain Model Contracts (`HPKata.Core.Beams`)

```csharp
namespace HPKata.Core.Beams.Models;

public sealed class BeamSpanModel
{
    public int SpanIndex { get; set; }
    public string SpanName { get; set; } = string.Empty;
    public double ClearLengthMm { get; set; }
    public double LeftSupportWidthMm { get; set; }
    public double RightSupportWidthMm { get; set; }
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public double TopElevationDeltaMm { get; set; }
    public double SoffitDropMm { get; set; }
    public double LeftSlabThicknessMm { get; set; }
    public double RightSlabThicknessMm { get; set; }
    public List<BeamIntersectionModel> Intersections { get; } = new();
}

public sealed class BeamSectionModel
{
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public double ConcreteCoverMainMm { get; set; } = 30.0;
    public double ConcreteCoverStirrupMm { get; set; } = 25.0;
    public int TopMainCount { get; set; }
    public int TopMainDiameterMm { get; set; }
    public int BottomMainCount { get; set; }
    public int BottomMainDiameterMm { get; set; }
}

public sealed class ExtraBarLayerModel
{
    public int LayerIndex { get; set; } // 1, 2, 3
    public int BarCount { get; set; }
    public int DiameterMm { get; set; }
    public double StartStationX { get; set; }
    public double EndStationX { get; set; }
    public double CutoffLeftMm { get; set; }
    public double CutoffRightMm { get; set; }
    public bool HasLeftHook { get; set; }
    public bool HasRightHook { get; set; }
}

public sealed class StirrupZoneDistributionModel
{
    public int SpanIndex { get; set; }
    public double StirrupDiameterMm { get; set; }
    public double LeftDenseZoneLengthMm { get; set; }
    public double LeftDenseSpacingMm { get; set; }
    public double MidspanSparseZoneLengthMm { get; set; }
    public double MidspanSparseSpacingMm { get; set; }
    public double RightDenseZoneLengthMm { get; set; }
    public double RightDenseSpacingMm { get; set; }
    public int TotalLegCount { get; set; } = 2;
    public int ClosedHoopCount { get; set; } = 1;
    public int UCapCount { get; set; } = 0;
    public int CTieCount { get; set; } = 0;
}
```

---

## 5. Reinforcement Detailing Mathematics & Curtailment Formulas

### 5.1 Continuous Longitudinal Main Bars (Top & Bottom)
1. **Top Continuous Bars (`thep_t`)**:
   - Run along the top face across all spans, seated directly below the top transverse leg of the closed stirrup.
   - Elevation $Z_{\text{top\_bar}} = Z_{\text{top\_beam}} - c_{\text{stirrup}} - d_{\text{stirrup}} - \frac{d_{\text{main}}}{2}$.
2. **Bottom Continuous Bars (`thep_d`)**:
   - Run along the soffit across all spans, seated directly above the bottom transverse leg of the closed stirrup.
   - Elevation $Z_{\text{bot\_bar}} = Z_{\text{soffit}} + c_{\text{stirrup}} + d_{\text{stirrup}} + \frac{d_{\text{main}}}{2}$.
3. **End Support Anchorage**:
   - Evaluated from the inner support face toward the exterior face.
   - Available horizontal room: $L_{\text{avail}} = W_{\text{support}} - c_{\text{end}} - \text{inset}$.
   - If $L_{\text{avail}} \ge L_{\text{anchor}} = 40d$: Bar terminates straight at $X = X_{\text{inner}} \pm L_{\text{anchor}}$.
   - If $L_{\text{avail}} < L_{\text{anchor}}$: Bar runs to $X = X_{\text{outer}} \mp c_{\text{end}}$, then bends $90^\circ$ vertically into the beam core with leg length:
     $$L_{\text{leg}} = \max\left(L_{\text{anchor}} - L_{\text{avail}}, L_{\text{min\_leg}}\right)$$
     Clamped to available vertical depth between layers:
     $$L_{\text{leg\_max}} = h - 2c_{\text{main}}$$
     Leg lengths are rounded up to the nearest $50\text{ mm}$ interval (`Round50`).

### 5.2 Support Top Additional Reinforcement Cutoff Rules
Top extra bars resist negative hogging moments over columns.
- **Span Ratio Calculation**:
  For an intermediate support between Span $i$ and Span $i+1$, the negative moment envelope requires curtailment based on the clear span lengths $L_{n,i}$ and $L_{n,i+1}$:
  - Standard Layer 1 Cutoff:
    $$L_{\text{cut1, left}} = \max(L_{n,i}, L_{n,i+1}) \times r_{\text{cutoff1}}$$
    where $r_{\text{cutoff1}} = 0.25$ ($L/4$) or $0.33$ ($L/3$) per sheet setting `H3` (default $0.20$ to $0.25$).
  - Staggered Layer 2 Cutoff:
    $$L_{\text{cut2}} = L_{\text{cut1}} - \Delta L_{\text{stagger}}$$
    where $\Delta L_{\text{stagger}} = 500\text{ mm}$ (from `G1` / `settings.CurtailedExtensionMm`).
  - Datum Reference:
    - If `I3` = `" mép"`: Curtailment length is measured from the column face into the span.
    - If `I3` = `" tâm"`: Curtailment length is measured from the column centerline.

### 5.3 Midspan Bottom Additional Reinforcement Cutoff Rules
Bottom extra bars resist positive sagging moments in the central region of the span.
- **Curtailment Points**:
  - Distance from left support face:
    $$X_{\text{start\_bot}} = X_{\text{face\_left}} + 0.10 \times L_n \quad (\text{or } 0.15 \times L_n)$$
  - Distance from right support face:
    $$X_{\text{end\_bot}} = X_{\text{face\_right}} - 0.10 \times L_n \quad (\text{or } 0.15 \times L_n)$$
  - Minimum straight bar extension beyond the theoretical cutoff point: $15d$ or $h_0/2$.

### 5.4 Commercial Bar Length & Lap Splicing ($11,700\text{ mm}$)
When continuous beam runs exceed the commercial manufacturing length ($11.7\text{ m}$):
- **Compression Lap Multiplier**: $30d$ (from config line 171: `LapLengthMultiplier = 30`).
- **Tension Lap Multiplier**: $40d$ (from config line 176: `TensionAnchorMult = 40`).
- **Permitted Splice Zones**:
  - Top Bars (Tension over support, Compression at midspan): Lap splices are strictly restricted to the midspan region:
    $$\frac{L_n}{3} \le X_{\text{splice\_top}} \le \frac{2L_n}{3}$$
  - Bottom Bars (Tension at midspan, Compression over support): Lap splices are restricted to support zones:
    $$0 \le X_{\text{splice\_bot}} \le \frac{L_n}{4} \quad \text{or inside the support column}$$
- **Staggering Rule**: Laps in adjacent bars must be staggered longitudinally by $\ge 1.3 \times L_{\text{lap}}$ (50% staggered).

### 5.5 Hook Geometries ($90^\circ, 135^\circ, 180^\circ$)
- $90^\circ$ Standard Hook (Longitudinal bar anchorage):
  - Inner bend diameter $D = 4d$ (for $d \le 16\text{ mm}$), $D = 6d$ (for $d \ge 20\text{ mm}$).
  - Straight extension length $L_{ext} = 12d$.
- $135^\circ$ Seismic Stirrup Hook:
  - Inner bend diameter $D = 4d_s$.
  - Extension length $L_{ext} = \max(10d_s, 75\text{ mm})$.
- $180^\circ$ Circular Hook / Cross-Tie Hook:
  - Inner bend diameter $D = 4d$.
  - Extension length $L_{ext} = \max(5d, 50\text{ mm})$.

---

## 6. Stirrup Spacing Zones & Confinement Detailing

### 6.1 Distribution Zones Along Span
Each beam span is segmented into three primary transverse shear zones:
1. **Left Support Dense Zone (Zone 1)**:
   - Extends from the inner face of the left support by distance:
     $$L_{\text{dense\_left}} = \max\left(\frac{L_n}{4}, 2h\right)$$
   - Spacing: $s_1 = a100$ or $a150$ (from cell `G7`).
2. **Midspan Sparse Zone (Zone 2)**:
   - Covers the central span between the dense zones:
     $$L_{\text{sparse}} = L_n - L_{\text{dense\_left}} - L_{\text{dense\_right}} \approx \frac{L_n}{2}$$
   - Spacing: $s_2 = a200$ (from cell `G8`).
3. **Right Support Dense Zone (Zone 3)**:
   - Extends inward from the right support face by $L_{\text{dense\_right}} = \max(L_n/4, 2h)$, spacing $s_3 = s_1$.
4. **First Stirrup Offset**: The first stirrup is placed at $s_{\text{start}} = 50\text{ mm}$ from the column face.

### 6.2 Stirrup Cross-Sectional Typologies

```
+---------------------------------------------------------------------------------+
|                                 STIRRUP TYPOLOGIES                              |
+---------------------------------------------------------------------------------+
|                                                                                 |
|   1. Closed Hoop (Dang 00)       2. U-Cap Stirrup (Dang 03)    3. Cross-Tie (C) |
|      [135° Seismic Hooks]           [Open Top + Lap Cap]          [135° + 90°]  |
|                                                                                 |
|        +---------------+               +---------------+           +            |
|        | \           / |               |               |           |            |
|        |   +-------+   |               |   +-------+   |           |            |
|        |   |       |   |               |   |       |   |           |            |
|        |   |       |   |               |   |       |   |           |            |
|        |   |       |   |               |   |       |   |           |            |
|        |   +-------+   |               +---+       +---+           +            |
|        +---------------+               [U-Body + Top Cap]                       |
|                                                                                 |
+---------------------------------------------------------------------------------+
```

- **Closed Hoop (Đai □, Shape `dang00`)**: Standard outer perimeter confinement with $135^\circ$ seismic hooks overlapping at alternating top corners.
- **U-Cap Stirrup (Đai U, Shape `dang03`)**: Two-piece composite stirrup comprising a deep U-body and an inverted closure cap tie, used for pre-assembling beam cages when crane clearance is restricted.
- **Cross-Tie (Đai C, Shape `dang06`)**: Internal branch tying opposite longitudinal bars across wide webs ($b \ge 400\text{ mm}$), alternating $135^\circ$ and $90^\circ$ hooks along the beam axis.

---

## 7. Beam-Beam Intersections & Suspension Detailing

When a secondary beam frames into a primary beam, vertical load is transferred near the bottom soffit, causing severe local diagonal tension.

```
+---------------------------------------------------------------------------------+
|                           BEAM-BEAM INTERSECTION HANGERS                        |
+---------------------------------------------------------------------------------+
|                                                                                 |
|                         Secondary Beam Framing Face                             |
|                                      |                                          |
|                                      v                                          |
|                     +---------------------------------+                         |
|   Primary Beam      |     |  |  |  |  [b_sec]  |  |  |  |     Primary Beam      |
|   Main Web          |     |  |  |  |           |  |  |  |     Main Web          |
|   ==================+=====+==+==+==+===========+==+==+==+====================   |
|                           ^  ^  ^  ^           ^  ^  ^  ^                       |
|                           |  |  |  |           |  |  |  |                       |
|                     4 to 6 Hanger Stirrups    4 to 6 Hanger Stirrups            |
|                     (Phi 10 a50 each side)    (Phi 10 a50 each side)            |
|                                                                                 |
+---------------------------------------------------------------------------------+
```

1. **Hanger Stirrups (`dam_giao`, `giao_dam`)**:
   - Total required shear suspension area:
     $$A_{hs} = \frac{V_{\text{secondary}}}{f_{yv} \sin \alpha}$$
   - Detail configuration: A group of 4 to 6 additional closed stirrups (typically $5\Phi 10a50$) positioned within a distance $s = \frac{h_{\text{pri}}}{3}$ from each side of the secondary beam face.
2. **Diagonal Anti-Crack Bars**:
   - 2 inclined bars (e.g. $2\Phi 14$, $45^\circ$ angle) detailed at the re-entrant bottom corners of the intersection to arrest re-entrant cracking.

---

## 8. Specialized Beam Types

### 8.1 Foundation Beams & Tie Beams (`ve_dam_mong`, `ve_giang_mong`, `ve_giang_shop`)
- **Foundation Grade Beams (`ve_dam_mong`)**:
  - Resist soil reaction pressure and column differential settlement; bottom face is under tension at midspan, top face over footings.
  - Require thicker cover: $c = 50\text{ mm}$ to main steel ($c = 40\text{ mm}$ to stirrups) due to direct ground contact.
- **Tie Beams / Ground Beams (`ve_giang_mong`, `ve_giang_shop`)**:
  - Connect isolated footings or pile caps to resist horizontal seismic shears and column base moments.
  - Longitudinal steel runs continuously with $40d$ tension anchorage hooks into the far faces of pile caps.

### 8.2 Inclined Beams & Rafters (`ve_dam_xien`)
- Used for sloped roof gables, escalator supports, and stair landing stringers.
- Geometry calculation: Real length $L_{\text{real}} = \sqrt{L_{\text{horiz}}^2 + \Delta Z^2}$, inclination angle $\theta = \arctan(\Delta Z / L_{\text{horiz}})$.
- Joint detailing: Top bars from the inclined span crank into the horizontal landing; rebar at the obtuse angle runs continuous; rebar at the acute angle terminates and anchors independently to prevent corner blow-out.

### 8.3 Horizontally Curved Beams (`ve_dam_cong`)
- Solves circular arc continuous beams on plan with radius $R$ and sweep angle $\theta$.
- Plan developed elevation: Flattened true-length elevation developed along the curved centerline arc $S = R \cdot \theta$.
- Torsion reinforcement: Curvature produces substantial twisting moment $T$. Closed stirrups with $135^\circ$ hooks and continuous skin reinforcement (`thep_gia`) are mandatory around the entire cross-section perimeter.

---

## 9. Web Skin / Side Face Reinforcement (`thep_gia`, `thep_suon`)

1. **Trigger Criteria**:
   - Governed by TCVN 5574:2018 §10.3.1.2: When overall beam height $h \ge 700\text{ mm}$, side face reinforcement is required to control web shrinkage and shear crack widths.
   - Excel control: Configured in cell `E5` (number of side bar layers) and `G4` (side bar diameter, e.g. $\Phi 12$).
2. **Spacing & Sizing Rules**:
   - Number of layers: $N_{\text{layers}} = \text{floor}\left(\frac{h - 2c}{s_{\text{max}}}\right)$ where $s_{\text{max}} = 300\text{ mm} - 400\text{ mm}$.
   - Diameter: Typically $\Phi 12$ or $\Phi 14$.
   - Lateral Restraint: Side bars must be held by transverse cross-ties (`Đai C`, $\Phi 6$ or $\Phi 8$) at spacing $s_h \le 500\text{ mm}$ along the beam axis.

---

## 10. Beam Persistence, Plan Scanning & Scheduling Aggregation

### 10.1 DWG Persistence Architecture (`Save_thep_dam`, `Add_thep_dam`)
- Serializes complete continuous beam rebar models into AutoCAD DWG Extension Dictionaries using `XRecord` named `"KATA_BEAM"`.
- Binary/String payload encodes:
  - Header: Beam name, span count, concrete grade, cover settings.
  - Per-span geometry: $L, b, h$, column dimensions, step offsets.
  - Rebar arrays: Top continuous, bottom continuous, top extra, bottom extra, stirrups, skin steel.
- `Add_thep_dam`: Allows rapid interactive insertion of reinforcement layers into stored dictionaries without re-drawing.

### 10.2 Plan Recognition & Clear Span Extraction (`quet_dam`, `quet_dam_all`)
- Algorithmic geometric scanner:
  1. Identifies beam centerline or boundary lines on layer `kata_net thay`.
  2. Detects intersection nodes with structural columns (layer `kata_cot`) and grids (layer `kata_grid`).
  3. Computes clear span distances between column boundary faces.
  4. Generates topological in-memory graph of continuous multi-span beam runs.

### 10.3 Scheduling Aggregation (`tkdam`) & Sheet `ThepDamAllMBKC` Schema
Aggregates beam framing schedules into Excel sheet `ThepDamAllMBKC`:

| Column Range | Field Name | Description |
|---|---|---|
| `B` | `Name` | Beam mark / designation (e.g. `B01`, `D2`) |
| `C` | `Span` | Span sequence index ($1..N$) |
| `D` | `Width` | Beam section width $b$ (mm) |
| `E` | `Height` | Beam section depth $h$ (mm) |
| `F..I` | `T1.0..T1.3` | Top layer 1 rebar (left support, left quarter, midspan, right support) |
| `J..M` | `T2.0..T2.3` | Top layer 2 rebar |
| `N..Q` | `T3.0..T3.3` | Top layer 3 rebar |
| `R..U` | `B1.0..B1.3` | Bottom layer 1 rebar |
| `V..Y` | `B2.0..B2.3` | Bottom layer 2 rebar |
| `Z..AC` | `B3.0..B3.3` | Bottom layer 3 rebar |
| `AD` | `DD` | Stirrup hoop diameter (mm) |
| `AE..AG` | `S1, S2, S3` | Stirrup spacing in left dense zone, midspan sparse zone, right dense zone |
| `AH..AK` | `DDE, S1E..S3E` | Extra stirrup / hanger stirrup diameter and spacings |
| `AL..AN` | `SE, SEC, SEU` | Number of main stirrup legs, C-ties, and U-caps |
| `AO..AP` | `GIA, DGIA` | Number of skin bars and diameter |
| `AQ..AR` | `KE, lap_xoan` | Chair spacer count and torsion lap hook flag |

---

## 11. ETABS & SAP2000 Integration Engine

### 11.1 ETABS OAPI Connection Architecture (`Etabs_Model`)
- Attaches to active ETABS process via COM Interop using progID `"CSI.ETABS.API.ETABSObject"` (or `ETABSv1.dll` managed wrapper).
- Traverses frame objects (`SapModel.FrameObj`):
  1. Filters elements by design orientation (`FrameType == FrameType.Beam`).
  2. Extracts geometric start/end coordinates $(X_1, Y_1, Z_1)$ and $(X_2, Y_2, Z_2)$ and story level.
  3. Queries concrete beam design results via `SapModel.DesignConcrete.GetSummaryResultsBeam`:
     - Top Longitudinal Reinforcement: $A_{s\_top}$ at stations $0.0$, $0.25L$, $0.50L$, $0.75L$, $1.0L$.
     - Bottom Longitudinal Reinforcement: $A_{s\_bot}$ at stations $0.0$, $0.25L$, $0.50L$, $0.75L$, $1.0L$.
     - Shear Reinforcement: $\frac{A_v}{s}$ (mm²/mm) along span.

### 11.2 Rebar Auto-Sizing Algorithm (`Set_Rebar_Beam`)
Converts ETABS continuous required area curves into discrete commercial rebar designations:
1. **Longitudinal Layer 1 Selection (`chon_lop1`)**:
   - Evaluates minimum continuous pass-through steel: $\rho_{\text{min}} = \max\left(0.15\%, \frac{0.4}{f_y}\right) \times b \cdot h$.
   - Selects 2 corner bars meeting minimum diameter (e.g. $2\Phi 18$ or $2\Phi 20$).
2. **Multi-Layer Support Packing (`chon_CacLopThep`)**:
   - For remaining required area $\Delta A_s = A_{\text{required}} - A_{\text{continuous}}$:
   - Iterates candidate diameters ($\Phi 16, \Phi 18, \Phi 20, \Phi 22, \Phi 25$):
     $$\text{Count } n = \left\lceil \frac{\Delta A_s}{\frac{\pi d^2}{4}} \right\rceil$$
   - Validates horizontal clear spacing:
     $$s_{\text{clear}} = \frac{b - 2c - 2d_s - n_{\text{total}} \cdot d}{n_{\text{total}} - 1} \ge \max(25\text{ mm}, d)$$
   - If $s_{\text{clear}} < 25\text{ mm}$, splits steel into Layer 2 (`chon_lop2`) and Layer 3 (`chon_lop3`).
3. **Shear Stirrup Sizing**:
   - From design $\frac{A_v}{s}$: evaluates candidate legs ($n=2$ or $n=4$) and diameter ($\Phi 8, \Phi 10$):
     $$s = \min\left(\frac{n \frac{\pi d_s^2}{4}}{\frac{A_v}{s}}, s_{\text{max\_code}}\right)$$
     where $s_{\text{max\_code}} = \min(h/2, 200\text{ mm})$ in dense zones, rounded to $50\text{ mm}$ intervals.

---

## 12. Dual-Host Architecture & Interface Contracts

```
+---------------------------------------------------------------------------------+
|                                  HPKata.slnx                                    |
+---------------------------------------------------------------------------------+
|                                                                                 |
|   +--------------------------+          +-----------------------------------+   |
|   |     HPKata.AutoCad       |          |           HPKata.Revit            |   |
|   |  - 25 Beam Commands (vd) |          |   - OST_StructuralFraming Bridge  |   |
|   |  - GoiLenh_HPKata.lsp    |          |   - Rebar.CreateFromCurves        |   |
|   |  - DWG XData & Blocks    |          |   - MultiReferenceAnnotation Tag  |   |
|   +--------------------------+          +-----------------------------------+   |
|                \                                      /                         |
|                 \                                    /                          |
|                  v                                  v                           |
|   +-------------------------------------------------------------------------+   |
|   |                               HPKata.UI                                 |   |
|   |  - BeamDetailingViewModel & BeamDetailingWindow (Dark/Light WPF)        |   |
|   |  - EtabsImportViewModel & EtabsImportWindow (Story/Beam Picker)         |   |
|   +-------------------------------------------------------------------------+   |
|                                       |                                         |
|                                       v                                         |
|   +-------------------------------------------------------------------------+   |
|   |                               HPKata.Core                               |   |
|   |  - BeamSpanModel, BeamSectionModel, BeamRebarLayout                     |   |
|   |  - Detailing Calculators (Cutoffs, Anchorage, Hooks, Laps, Stirrups)    |   |
|   |  - EtabsBeamImportService (OAPI Frame & Forces Parser)                  |   |
|   |  - ClosedXML Excel Dam & ThepDamAllMBKC 2-Way Exporter                  |   |
|   |  - ZERO dependencies on Autodesk.AutoCAD.* or Autodesk.Revit.*          |   |
|   +-------------------------------------------------------------------------+   |
|                                                                                 |
+---------------------------------------------------------------------------------+
```

### 12.1 Interface Contract Parity
- **`HPKata.Core` ↔ `HPKata.AutoCad`**:
  `BeamRebarCalculator.Calculate(spec)` outputs pure metric millimeter curves (`Curve3D`, `Line3D`, `Arc3D`), which `HPKata.AutoCad.RebarDrawController` renders into native AutoCAD `Polyline` entities with fillet arcs, assigned to layer `kata_thep chu` / `kata_thep dai`.
- **`HPKata.Core` ↔ `HPKata.Revit`**:
  `HPKata.Revit.NativeRebar.RebarCreationService` converts `Curve3D` loops directly into Revit API `Curve` collections and invokes `Rebar.CreateFromCurves` within the host beam's geometry envelope, tagging elements with parameter `Comments = "HPKata_Beam_{Name}"` for idempotent re-runs.
- **`HPKata.Core` ↔ `HPKata.UI`**:
  `BeamDetailingViewModel` wraps `BeamSpanModel` in reactive `ObservableObject` properties, providing live 2D canvas drawing updates as beam dimensions, steel grades, or bar arrangements are modified by the engineer.

---

## 13. Clean-Room Governance & Verification Confirmation

1. **Clean-Room Boundary**: No security assemblies (`Security.dll`), licensing routines, or hardware lock checks were inspected or disassembled.
2. **Read-Only Compliance**: All source code, template workbooks, and compiled binaries in `C:\kata_pro` were accessed exclusively in read-only mode.
3. **Clean Code Adherence**: All specified classes and architecture adhere strictly to `HP_CLEAN_CODE_CORE.md` (files $\le 300$ lines, methods $\le 50$ lines, 0 dead code, 0 unannotated catch blocks).
