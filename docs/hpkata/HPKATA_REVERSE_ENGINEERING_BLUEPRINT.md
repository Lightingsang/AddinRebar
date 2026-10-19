# HPKATA REVERSE-ENGINEERING BLUEPRINT
## Master Specification & Clean-Room Architecture for Dual-Host Rebar Suite (AutoCAD 2026 & Revit 2026)

- **Document ID**: `HPKATA-SPEC-M1-BLUEPRINT-2026`
- **Version**: `1.0.0-PROD`
- **Author**: worker_m1 (Blueprint & Master Specification Author)
- **Approved by**: orchestrator_16 (Milestone M1 Delivery)
- **Target Deliverable**: `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300)
- **Status**: AUTHORITATIVE & BINDING
- **Governance**: Clean-Room Reverse Engineering Protocol (Strictly Read-Only, Zero DRM / Security Inspection, Full Compliance with `docs/clean-code/HP_CLEAN_CODE_CORE.md`)

---

## TABLE OF CONTENTS
1. [§1: Architectural Overview & Subsystem Decomposition](#1-architectural-overview--subsystem-decomposition)
2. [§2: Comprehensive Command Inventory & C# Method Bindings](#2-comprehensive-command-inventory--c-method-bindings)
3. [§3: Complete 86-Parameter Configuration Schema (`Goi lenh.lsp`)](#3-complete-86-parameter-configuration-schema-goi-lenhlsp)
4. [§4: DWG Data Schemas & `Iso_kata.dwg` Standards](#4-dwg-data-schemas--iso_katadwg-standards)
5. [§5: XData RegApp `"KATA"`, DxfCodes & Dynamic Handle-Linking Engine](#5-xdata-regapp-kata-dxfcodes--dynamic-handle-linking-engine)
6. [§6: Complete Mathematical Catalog of All 68 Rebar Shapes (`dang00`..`dang67` + 16 Variants)](#6-complete-mathematical-catalog-of-all-68-rebar-shapes-dang00dang67--16-variants)
7. [§7: 1D Cutting-Stock Optimization Engine (`to_hop_thep`)](#7-1d-cutting-stock-optimization-engine-to_hop_thep)
8. [§8: Excel Templates & Coupler Schedule Schemas](#8-excel-templates--coupler-schedule-schemas)
9. [§9: Revit 2026 Integration Architecture (`Kata_Class_Lib_Revit.dll`)](#9-revit-2026-integration-architecture-kata_class_lib_revitdll)
10. [§10: Clean-Room Governance, Compliance & Verification Matrix](#10-clean-room-governance-compliance--verification-matrix)

---

## 1. Architectural Overview & Subsystem Decomposition

### 1.1 KataPro As-Is Architecture
KataPro is an industry-standard structural concrete reinforcement detailing, scheduling, and fabrication drafting suite developed for Autodesk AutoCAD and Autodesk Revit in Vietnam. The legacy system at `C:\kata_pro` is architected across multiple compiled assemblies, Lisp scripts, DWG templates, and Excel macro-enabled workbooks:

```
C:\kata_pro\
├── Kata_pro64_Cad2025.dll    # .NET 8.0 AutoCAD 2025+ managed plugin (16.8 MB, 4,276 TypeDefs, 33,792 MethodDefs)
├── Kata_pro64_Cad2013.dll    # .NET Framework 4.8 AutoCAD 2013–2024 plugin (16.9 MB, 3,276 TypeDefs)
├── Kata_Class_Lib.dll        # .NET Framework 4.8 shared domain library (844 KB, 491 TypeDefs, 3,356 MethodDefs)
├── Kata_Class_Lib_Revit.dll  # .NET Framework 4.8 Revit integration library (1.08 MB, 692 TypeDefs, 4,357 MethodDefs)
├── Goi lenh.lsp              # AutoLISP command alias loader (118 command aliases, 86 configuration lines)
├── menukata.cui              # AutoCAD CUI Menu definition (MenuGroup MENUKATA, 720 lines)
├── Iso_kata.dwg              # Master CAD template (12 layers, 15 dimstyles, textstyles, 119 block definitions)
├── dangthep\dang00..67.wmf   # 68 vector metafile sketches of standard rebar shapes
├── Kata.xlsm                 # Master Vietnamese detailing, scheduling & cutting workbook (15 sheets)
├── Kata ENG.xlsm             # Localized English detailing workbook (10 sheets)
├── Iso_QHPlus.xls            # Industrial fabrication template (3 sheets)
└── QRCoder.dll               # QR code generator library for fabrication shop bundle tags
```

### 1.2 The Six Core Functional Subsystems
The system decomposes into six tightly integrated functional subsystems:

1. **Subsystem 1: Core Rebar & Setup Engine**:
   - Manages global rebar configuration (layers, text styles, dimension styles, concrete covers, bar prefixes, bend/hook rules).
   - Generates and modifies 2D polyline rebar entities with thickness, color-by-diameter, and bend radii.
   - Maintains the **Dynamic Rebar $\leftrightarrow$ Tag Linking Engine** via AutoCAD XData RegApp `"KATA"` and database Handle cross-referencing.

2. **Subsystem 2: Beam Shop Engine**:
   - Automated continuous beam detailing from structural input (single-span, multi-span, cantilever, inclined, curved).
   - Cutoff calculations for top support bars ($L/4, L/3$ or $0.2L, 0.25L$) and bottom span bars.
   - Web skin reinforcement (side bars), support dense-zone stirrup spacing ($L/4$), and mid-span sparse zones.
   - Secondary beam hanger stirrups ($5\Phi 10a50$) and anti-crack diagonal bars ($2\Phi 14$) at beam-beam intersections.
   - Automated 11.7m lap splice staggering in compression zones.

3. **Subsystem 3: Column/Wall Shop Engine**:
   - Detailing of rectangular, circular, and boundary-element columns and shear walls.
   - Column lap splicing staggered at $H/6$ from finish floor level.
   - Cranked bar offset (offset crank ratio $1:6$) when section drops $\le 50\text{ mm}$; cutoff and dowel generation when drop $> 50\text{ mm}$.
   - Elevator core detailing (U, C, and closed-box cores) with boundary element confinement cages.

4. **Subsystem 4: Foundation, Slab & Stair Engine**:
   - Isolated pad footings (móng đơn), pile caps (đài cọc, 1 to 9+ piles), and grade beams (giằng móng).
   - Two-way slab reinforcement detailing (bottom continuous mesh, top negative moment hat/chair bars).
   - Stair flights (2-flight and 3-flight dog-leg stairs) with cranked rebar detailing at landing junctions.
   - Wall openings, door/window lintels (`velanhto`), and slab drop panels (`drpn`).

5. **Subsystem 5: Rebar Schedule, Coupler & 1D Cutting-Stock Engine**:
   - Comprehensive rebar schedule aggregation (`thong_ke_thep`), grouped diameter summaries (`tong_hop_thep`), and TCVN groupings ($\le 10\text{ mm}, 10 < \Phi \le 18\text{ mm}, > 18\text{ mm}$).
   - Mechanical threaded coupler scheduling (`thong_ke_coupler`) based on thread notes (`ghi_chu_ren`).
   - 1D Cutting-Stock Problem (1D-CSP) solver (`to_hop_thep`) optimizing cuts from 11,700 mm commercial stock bars with 5 heuristics and warehouse offcut inventory tracking (`Kho_thep`).
   - QR code tag generation (`in_tem_thep`) encoding fabrication metadata into ECC Level Q barcodes.
   - Headless and COM-based Excel export to `Kata.xlsm` and `Iso_QHPlus.xls`.

6. **Subsystem 6: QS Quantity Takeoff Engine**:
   - Volumetric concrete, formwork contact surface, and rebar tonnage estimation directly from architectural/structural plans.
   - 3D geometric intersection deduction (columns run continuous through floors; beam lengths net of columns; slab areas net of columns and beams).
   - Panel cutting and tile layout optimization (`chia_tam`, `op_gach`).

### 1.3 Target Deliverable: HPKata Clean-Room Architecture
The clean-room replacement **HPKata** is structured as an independent solution `HPKata/HPKata.slnx` built with .NET 8 SDK 10.0.300, strictly following repository Clean Code Governance (`HP_CLEAN_CODE_CORE.md`):

```
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\
├── HPKata.slnx                    # XML format solution configuration (.NET 8 SDK)
├── global.json                    # SDK 10.0.300 pinning
├── Directory.Build.props          # Clean code rules, nullable enabled, warning as error
│
├── HPKata.Core/                   # Target: netstandard2.0;net8.0 (Pure C# Domain)
│   │                              # ZERO references to Autodesk.AutoCAD.* or Autodesk.Revit.*
│   ├── Configuration/             # 86-parameter config parser, serializer, default settings
│   ├── Shapes/                    # 68 rebar shape definitions, geometry equations, parameter models
│   ├── Calculators/               # Hook, bend deduction, lap splice, anchorage, unit weight calculators
│   ├── Serialization/             # KataPro XData RegApp "KATA", DxfCodes, block attributes 2-way mapper
│   ├── Scheduling/                # Rebar schedule aggregation, diameter grouping, coupler schedule
│   ├── Optimization/              # 1D cutting stock engine (to_hop_thep), 5 heuristics, warehouse offcut ledger
│   ├── Tagging/                   # QR code payload generator (ECC Level Q pipe-delimited)
│   └── Export/                    # Headless ClosedXML exporter compatible with Kata.xlsm / Iso_QHPlus.xls
│
├── HPKata.Core.Tests/             # Target: net8.0 (xUnit v3 test suite >= 100 tests, 100% green)
│   ├── Shapes/                    # Geometry and cut-length validation for all 68 shapes + 16 variants
│   ├── Configuration/             # Round-trip parsing & serialization of 86 parameters
│   ├── Serialization/             # 2-way XData and Block attribute round-trip equality tests
│   ├── Optimization/              # 1D-CSP conservation of mass, waste %, offcut reuse tests
│   └── Export/                    # Headless ClosedXML workbook generation tests
│
├── HPKata.UI/                     # Target: net8.0-windows (Shared WPF MVVM Library)
│   │                              # CommunityToolkit.Mvvm + MahApps.Metro (Host Theme-Aware Dark/Light)
│   ├── Views/                     # Setup, Drawing/Editing, Scheduling, and Cutting Stock dialogs
│   ├── ViewModels/                # Reactive ViewModels with property validation
│   └── Converters/                # WPF Value converters for rebar shapes and units
│
├── HPKata.AutoCad/                # Target: net8.0-windows (AutoCAD 2026 Add-In)
│   │                              # References AutoCAD.NET 25.1.0, HPKata.Core, HPKata.UI
│   ├── Commands/                  # [CommandMethod] handlers matching all >110 Goi lenh.lsp commands
│   ├── Interop/                   # DWG Polyline, XData, Block Attribute, and Dim synchronization
│   ├── Graphics/                  # Overrule, Jig, Transient Graphics for live rebar linking & highlight
│   └── GoiLenh_HPKata.lsp         # AutoLISP companion loader with 100% command alias parity
│
└── HPKata.Revit/                  # Target: net8.0-windows (Revit 2026 Add-In)
    │                              # References RevitAPI 2026, HPKata.Core, HPKata.UI
    ├── Commands/                  # Tag, Schedule, Coupler, and Cutting Stock commands
    ├── NativeRebar/               # Rebar.CreateFromCurves / Rebar.CreateFromCurvesAndShape bridge
    ├── ViewAutomation/            # Longitudinal/cross section generation & ParameterFilterElement
    └── Interop/                   # Parameter bridge mapping BuiltInParameter.REBAR_PARTITION = "{ten_ck}"
```

---

## 2. Comprehensive Command Inventory & C# Method Bindings

The legacy suite exposes **118 AutoLISP commands/aliases** in `Goi lenh.lsp` backed by **193 `[CommandMethod]` entry points** across `Kata_pro64_Cad2025.dll` and `Kata_pro64_Cad2013.dll`. The complete inventory catalog of **136 numbered commands** is categorized below across all 6 subsystems and drawing management utilities:

### 2.1 Subsystem 1: Core Rebar & Setup Commands
| # | Lisp Alias | Full Command | [CommandMethod] Name | Backing C# Class & Method | Description & Parameters |
|---|---|---|---|---|---|
| 1 | `setupkata` | `setupkata` / `setupkata1` | `setupkata1` | `Kata_pro64_Cad2025.Lenh_thep::Goi_setting` | Opens master setup dialog for layers, text, dimstyles, cover, hooks, and 86 config items. |
| 2 | `setupthep` | `setupthep` | `setupthep` | `Kata_pro64_Cad2025.Lenh_thep::Setupthep_cot` | Configures rebar detailing rules: bar sizes, bend radii, anchorage/lap lengths. |
| 3 | `vt` | `ve_thep` | `ve_thep` | `Kata_pro64_Cad2025.Lenh_thep::Ve_thep` | Draws 2D rebar polyline with fillet radius, assigns XData `KATA_THEP`. |
| 4 | `taothep` | `taothep` | `taothep` | `Kata_pro64_Cad2025.Lenh_thep::Tao_thep` | Converts existing CAD Line/Polyline entities into Kata smart rebar polylines. |
| 5 | `nhan` | `nhan_thep` | `nhan_thep` | `Kata_pro64_Cad2025.Lenh_thep::Nhan_thep1` | Multiplies bar quantity by member count ($N_{\text{members}}$). |
| 6 | `sua` | `sua_thep` | `sua_thep` | `Kata_pro64_Cad2025.Lenh_thep::Edit_link1` | Full interactive editing dialog for selected rebar (shape, diameter, spacing, notes). |
| 7 | `smd` | `sua_moc_dai` | `sua_moc_dai` | `Kata_pro64_Cad2025.Lenh_thep::sua_moc_dai` | Toggles stirrup hook angles ($90^\circ, 135^\circ, 180^\circ$) graphically on screen. |
| 8 | `dai90` | `dai90` | `dai90` | `Kata_pro64_Cad2025.Lenh_thep::Chuyen_dai_vong_moc_90` | Instantly converts selected stirrups to $90^\circ$ hooks. |
| 9 | `dai135` | `dai135` | `dai135` | `Kata_pro64_Cad2025.Lenh_thep::Chuyen_dai_vong_moc_135` | Instantly converts selected stirrups to $135^\circ$ seismic hooks. |
| 10 | `dai180` | `dai180` | `dai180` | `Kata_pro64_Cad2025.Lenh_thep::Chuyen_dai_vong_moc_180` | Instantly converts selected stirrups to $180^\circ$ circular hooks. |
| 11 | `dai90135` | `dai90135` | `dai90135` | `Kata_pro64_Cad2025.Lenh_thep::Chuyen_dai_vong_moc_90135` | Converts to composite open stirrup (one $90^\circ$ leg, one $135^\circ$ leg). |
| 12 | `neo` | `neo_thep` | `neo_thep` | `Kata_pro64_Cad2025.Lenh_thep::Neo_thep1` | Adds $90^\circ$ or $135^\circ$ anchorage cog to selected rebar endpoint. |
| 13 | `cut` | `cat_thep` | `cat_thep` | `Kata_pro64_Cad2025.Lenh_thep::Chon_cat_thep1` | Splits long rebar into two pieces with standard lap splice length ($30d$). |
| 14 | `cattuyy` | `cat_tuy_y` | `cat_tuy_y` | `Kata_pro64_Cad2025.Lenh_thep::Cat_tuy_y1` | Arbitrary point rebar cut without lap allowance. |
| 15 | `dimthep` | `dimthep` | `dimthep` | `Kata_pro64_Cad2025.Lenh_thep::Dimthep` | Automated dimensioning of all individual segment lengths of a rebar polyline. |
| 16 | `dmt` | `doi_mau_thep` | `doi_mau_thep` | `Kata_pro64_Cad2025.Lenh_thep::Doi_mau_thep1` | Color-by-diameter rebar rendering matching project color standards. |
| 17 | `link` | `tao_lien_ket` | `tao_lien_ket` | `Kata_pro64_Cad2025.Lenh_thep::Create_link` | Binds rebar polyline to tag block (`thep`) via mutual database Handle linking in XData. |
| 18 | `cl` | `copy_tao_lien_ket`| `copy_tao_lien_ket`| `Kata_pro64_Cad2025.Lenh_thep::Copy_link` | Simultaneously copies rebar polyline and tag block, establishing unique new handle links. |
| 19 | `dl` | `xoa_lien_ket` | `xoa_lien_ket` | `Kata_pro64_Cad2025.Lenh_thep::Delete_link` | Severs dynamic link between rebar polyline and tag block. |
| 20 | `tim` | `xem_lien_ket` | `xem_lien_ket` | `Kata_pro64_Cad2025.Lenh_thep::Find_link` | Visually highlights rebar polyline and corresponding linked tag block. |
| 21 | `xoa` | `xoa_highlight` | `xoa_highlight` | `Kata_pro64_Cad2025.Lenh_thep::Clear_highlight` | Clears all transient graphical highlights from the screen. |
| 22 | `ud` | `update_thep` | `update_thep` | `Kata_pro64_Cad2025.Lenh_thep::Update_link` | Reads modified polyline geometry and updates linked block attributes (`CD1T`, `SL`). |
| 23 | `gt` | `ghi_thep` | `ghi_thep` | `Kata_pro64_Cad2025.Lenh_thep::Ghi_thep` | Inserts mark tag block with leader line pointing to rebar and calls `link`. |
| 24 | `stt0` | `stt0` | `stt0` | `Kata_pro64_Cad2025.Lenh_thep::Gan_stt0` | Assigns secondary bar mark notation (e.g. `1a`, `1b`). |
| 25 | `stt1` | `stt1` | `stt1` | `Kata_pro64_Cad2025.Lenh_thep::Gan_stt1` | Assigns primary sequential bar mark notation (e.g. `1`, `2`, `3`). |
| 26 | `timthep` | `timthep` | `timthep` | `Kata_pro64_Cad2025.Lenh_thep::Timthep1` | Searches and zooms the viewport directly to rebar matching specified mark. |
| 27 | `check_lap` | `check_lap` | `check_lap` | `Kata_pro64_Cad2025.Lenh_thep::check_lap` | Scans drawing for overlapping rebar polylines or duplicate marks; flags with red circles. |
| 28 | - | `dao_thep` | `dao_thep` | `Kata_pro64_Cad2025.Lenh_thep::Mirror_thep1` | Mirrors rebar polyline geometry while preserving text orientation, tag links, and hook directions. |
| 29 | `dao` | `dao` | `-vbarun profile.dao_so_hieu` | `Kata_pro64_Cad2025.Lenh_thep::Dao_so_hieu` | Inverts or swaps rebar marks between selected bar tags on drawing. |
| 30 | `ad` | `ad` | `-vbarun profile.ad` | `Kata_pro64_Cad2025.Lenh_thep::Edit_att_data` | Rapid block attribute data editor for rebar tags. |
| 31 | `rthep` | `rthep` | `-vbarun rai_thep.rai_block_thep` | `Kata_pro64_Cad2025.Lenh_thep::rai_thep` | Rebar distribution block layout along span/zone. |

### 2.2 Subsystem 2: Beam Shop Commands
| # | Lisp Alias | Full Command | [CommandMethod] Name | Backing C# Class & Method | Description & Parameters |
|---|---|---|---|---|---|
| 32 | `vd` | `ve_dam` | `ve_dam` | `Kata_pro64_Cad2025.Lenh_thep::Vedam` | Parametric continuous beam detailing (elevation, section, callouts, dims). |
| 33 | `vds` | `ve_dam_shop` | `ve_dam_shop` | `Kata_pro64_Cad2025.Lenh_thep::Vedam_shop` | Beam shop drawing with 11.7m commercial bar cuts and staggered lap splices. |
| 34 | `vda` | `ve_dam_all` | `ve_dam_all` | `Kata_pro64_Cad2025.Lenh_thep::Vedam_all` | Batch beam detailing generation from framing plan scan. |
| 35 | `vdas` | `ve_dam_all_shop` | `ve_dam_all_shop` | `Kata_pro64_Cad2025.Lenh_thep::Vedam_all_shop` | Batch shop drawing generation for all beams across a floor. |
| 36 | `vdmc` | `ve_dam_mc` | `ve_dam_mc` | `Kata_pro64_Cad2025.Lenh_thep::Ve_dam_mc21` | Generates transverse cross-sections ($b \times h$) at supports and mid-spans. |
| 37 | `vdmca` | `ve_damMC_all` | `ve_damMC_all` | `Kata_pro64_Cad2025.Lenh_thep::VedamMC_all` | Batch cross-section generation for all detailed beams. |
| 38 | `vdx` | `ve_dam_xien` | `ve_dam_xien` | `Kata_pro64_Cad2025.Lenh_thep::Ve_dam_xien1` | Details inclined beams, sloped roof beams, and stair stringers. |
| 39 | `vdc` | `ve_dam_cong` | `ve_dam_cong` | `Kata_pro64_Cad2025.Lenh_thep::Vedamcong` | Details horizontally curved beams with developed planar elevation. |
| 40 | `damgiao` | `dam_giao` | `dam_giao` | `Kata_pro64_Cad2025.Lenh_thep::Ve_dam_giao1` | Details beam-beam intersections, generating hanger stirrups ($5\Phi 10a50$). |
| 41 | `giaodam` | `giao_dam` | `giao_dam` | `Kata_pro64_Cad2025.Lenh_thep::Xu_ly_giao_dam` | Automated detection and hanger cage placement at framing intersections. |
| 42 | `catdam` | `cat_dam` | `cat_dam` | `Kata_pro64_Cad2025.Lenh_thep::Xu_ly_cat_dam` | Splits continuous beam runs at expansion joints or column column faces. |
| 43 | `raidam` | `raidam` | `raidam` | `Kata_pro64_Cad2025.Lenh_thep::Rai_block_dam1` | Places beam identification tags along grid centerlines on framing plans. |
| 44 | `tkdam` | `tkdam` | `tkdam` | `Kata_pro64_Cad2025.Lenh_thep::Tao_table_tk_cau_kien_block_beam1` | Creates tabular takeoff of beam marks, sections, and counts. |
| 45 | `qd` | `quet_dam` | `quet_dam` | `Kata_pro64_Cad2025.Lenh_thep::quet_dam` | Framing plan recognition scanner detecting beams, columns, and clear spans. |
| 46 | `qda` | `quet_dam_all` | `quet_dam_all` | `Kata_pro64_Cad2025.Lenh_thep::quet_dam_all` | Full-drawing framing scanner caching beam geometry to memory. |
| 47 | `std` | `Save_thep_dam` | `Save_thep_dam` | `Kata_pro64_Cad2025.Lenh_thep::Save_thep_dam` | Serializes beam reinforcement data into DWG Extension Dictionaries / XRecords. |
| 48 | `stda` | `Save_thep_dam_all`| `Save_thep_dam_all`| `Kata_pro64_Cad2025.Lenh_thep::Save_thep_dam_all` | Batch serialization of all beam reinforcement models in drawing. |
| 49 | `atd` | `Add_thep_dam` | `Add_thep_dam` | `Kata_pro64_Cad2025.Lenh_thep::Add_thep_dam` | Interactive UI to add extra reinforcement layers to selected beam. |
| 50 | `shtd` | `show_thep_dam` | `show_thep_dam` | `Kata_pro64_Cad2025.Lenh_thep::show_thep_dam` | Displays 2D graphical preview of beam reinforcement cage. |
| 51 | `loadbeam` | `loadbeam` | `loadbeam` | `Kata_pro64_Cad2025.Lenh_thep::Load_beam` | Imports beam geometry and reinforcement from `Kata.xlsm` sheet `Dam`. |
| 52 | `Etabs` | `Etabs` | `Etabs` | `Kata_pro64_Cad2025.Lenh_thep::Etabs_Model` | Connects to active ETABS session via OAPI, extracts bending moments and shears. |
| 53 | - | `GanTai` | `GanTai` | `Kata_pro64_Cad2025.Lenh_thep::Etabs_GanTai` | Assigns structural beam dead and superimposed loads to active ETABS model via OAPI. |
| 54 | - | `HoatTai` | `HoatTai` | `Kata_pro64_Cad2025.Lenh_thep::Etabs_RaiTai` | Distributes live load patterns across beam spans in active ETABS model. |

### 2.3 Subsystem 3: Column/Wall Shop Commands
| # | Lisp Alias | Full Command | [CommandMethod] Name | Backing C# Class & Method | Description & Parameters |
|---|---|---|---|---|---|
| 55 | `vc` | `ve_cot` | `ve_cot` | `Kata_pro64_Cad2025.Lenh_thep::Vecot` | Parametric column detailing (elevation, section, footing starter bars). |
| 56 | `vcs` | `ve_cot_shop` | `ve_cot_shop` | `Kata_pro64_Cad2025.Lenh_thep::Vecot_shop` | Column shop drawing with $H/6$ staggered lap splices and $1:6$ cranked bars. |
| 57 | `vv` | `ve_vach` | `ve_vach` | `Kata_pro64_Cad2025.Lenh_thep::Vevach` | Shear wall detailing (boundary elements, horizontal/vertical web rebar). |
| 58 | `vvs` | `ve_vach_shop` | `ve_vach_shop` | `Kata_pro64_Cad2025.Lenh_thep::Vevach_shop` | Shear wall shop drawings with vertical rebar lap staging. |
| 59 | `vlv` | `ve_loi_vach` | `ve_loi_vach` | `Kata_pro64_Cad2025.Lenh_thep::Ve_loi_vach1` | Elevator core detailing (U, C, and closed tubular core cages). |
| 60 | `raicot` | `raicot` | `raicot` | `Kata_pro64_Cad2025.Lenh_thep::Rai_block_cot1` | Automatically places column symbol blocks at grid intersections. |
| 61 | `dimcot` | `dimcot` | `dimcot` | `Kata_pro64_Cad2025.Lenh_thep::dim_block_cot1` | Dimensions column section sizes and offsets to structural grid lines. |
| 62 | `tkcot` | `tkcot` | `tkcot` | `Kata_pro64_Cad2025.Lenh_thep::Tao_table_tk_cau_kien_block_ColumnWall1` | Generates summary schedule of column/wall components per story. |
| 63 | `loadcolumn`| `loadcolumn` | `loadcolumn` | `Kata_pro64_Cad2025.Lenh_thep::Load_column` | Imports column schedule from `Kata.xlsm` sheet `Cot`. |
| 64 | `loadwall` | `loadwall` | `loadwall` | `Kata_pro64_Cad2025.Lenh_thep::Load_wall` | Imports shear wall schedule from `Kata.xlsm` sheet `Vach`. |
| 65 | - | `Update_Block_ColumnWall` | `Update_Block_ColumnWall` | `Kata_pro64_Cad2025.Lenh_thep::update_Block_ColumnWall` | Synchronizes column/wall geometry with schedule block attributes across floors. |
| 66 | - | `updatecrank` | `updatecrank` | `Kata_pro64_Cad2025.Lenh_thep::Update_crank1` | Updates column vertical bar offset crank slope ($1:6$) when section transitions. |
| 67 | - | `updatedai` | `updatedai` | `Kata_pro64_Cad2025.Lenh_thep::Update_dai` | Updates column/beam confinement stirrup spacing in dense/splice zones. |
| 68 | - | `updateleg` | `updateleg` | `Kata_pro64_Cad2025.Lenh_thep::Update_leg` | Updates column base starter dowel 90° footing anchor leg length. |

### 2.4 Subsystem 4: Foundation, Slab & Stair Commands
| # | Lisp Alias | Full Command | [CommandMethod] Name | Backing C# Class & Method | Description & Parameters |
|---|---|---|---|---|---|
| 69 | `vmbm` | `ve_mat_bang_mong`| `ve_mat_bang_mong`| `Kata_pro64_Cad2025.Lenh_thep::VMBM1` | Generates foundation plan from column base locations and loads. |
| 70 | - | `VMBM_LIGHT` | `VMBM_LIGHT` | `Kata_pro64_Cad2025.VmbmLightCommand::ShowVmbmLight` | High-performance lightweight foundation layout plan generator. |
| 71 | `rm` | `rai_mong` | `rai_mong` | `Kata_pro64_Cad2025.Lenh_thep::RM1` | Places footing blocks (pad, pile cap) at column base nodes. |
| 72 | `vmd` | `ve_mong_don` | `ve_mong_don` | `Kata_pro64_Cad2025.Lenh_thep::Ve_mong_new` | Details isolated spread/pad footings (sloped or stepped). |
| 73 | `vmc` | `ve_mong_coc` | `ve_mong_coc` | `Kata_pro64_Cad2025.Lenh_thep::Ve_mong_coc1` | Details pile caps (1 to 9+ piles, driven precast or bored piles). |
| 74 | `vdm` | `ve_dam_mong` | `ve_dam_mong` | `Kata_pro64_Cad2025.Lenh_thep::Vedammong` | Details foundation tie beams and grade beams. |
| 75 | `vgm` | `ve_giang_mong`| `ve_giang_mong`| `Kata_pro64_Cad2025.Lenh_thep::Vegiangmong` | Details foundation tie beams connecting pile caps. |
| 76 | `vgms` | `ve_giang_shop`| `ve_giang_shop`| `Kata_pro64_Cad2025.Lenh_thep::Vegiangmong_shop`| Foundation tie beam shop drawing with 11.7m rebar optimization. |
| 77 | - | `phamong` | `phamong` | `Kata_pro64_Cad2025.Lenh_thep::PhaMong1` | Explodes composite pile cap / footing blocks into constituent rebar entities. |
| 78 | `vesan` | `ve_san` | `ve_san` | `Kata_pro64_Cad2025.Lenh_thep::Ve_san` | Details 2-way slab reinforcement (bottom continuous, top negative hats). |
| 79 | `vesanpl` | `vesanpl` | `vesanpl` | `Kata_pro64_Cad2025.Lenh_thep::Ve_san_polyline` | Details slab reinforcement within closed polyline boundary. |
| 80 | `vesan4d` | `vesan4d` | `vesan4d` | `Kata_pro64_Cad2025.Lenh_thep::Ve_san_4d` | Details slab reinforcement spanning between 4 arbitrary boundary points. |
| 81 | `chiasan` | `chia_san` | `chia_san` | `Kata_pro64_Cad2025.Lenh_thep::Chia_san1` | Partitions floor slab into panel zones and creates layout lines. |
| 82 | `dsan` | `pick_diem_san` | `pick_diem_san` | `Kata_pro64_Cad2025.Lenh_thep::Trien_khai_day_san`| Picks point inside slab panel to auto-detail reinforcement. |
| 83 | `ssan` | `ssan` | `ssan` | `Kata_pro64_Cad2025.Lenh_thep::Khai_thong_tin_san`| Configures slab thickness, bar sizes, and covers per panel. |
| 84 | `str` | `show_thep_rai`| `show_thep_rai`| `Kata_pro64_Cad2025.Lenh_thep::show_thep_rai1` | Graphical display of slab reinforcement distribution bands. |
| 85 | `tksan` | `thong_ke_block_san`| `thong_ke_block_san`| `Kata_pro64_Cad2025.Lenh_thep::Thong_ke_san` | Schedules slab reinforcement from floor distribution blocks. |
| 86 | `tstt` | `tstt` | `-vbarun tk_san.tang_stt` | `Kata_pro64_Cad2025.Lenh_thep::tang_stt_san` | Increments sequential bar mark on slab schedule blocks. |
| 87 | `chuyen` | `chuyen` | `chuyen` | `Kata_pro64_Cad2025.Lenh_thep::Chuyen_block_thep_san_sang_theplk1` | Converts legacy static slab rebar blocks to dynamically linked smart rebars. |
| 88 | `vethang` | `ve_thang` | `ve_thang` | `Kata_pro64_Cad2025.Lenh_thep::Ve_thang_new` | Details dog-leg stairs (2 or 3 flights, landing junctions, cranked bars). |
| 89 | `velanhto` | `velanhto` | `velanhto` | `Kata_pro64_Cad2025.Lenh_thep::Ve_lanh_to1` | Details lintel beams over door/window openings with 250mm bearings. |
| 90 | - | `drpn` | `drpn` | `Kata_pro64_Cad2025.Lenh_thep::Drop_panel1` | Details flat slab column drop panels and shear capital reinforcement. |
| 91 | - | `openning` | `openning` | `Kata_pro64_Cad2025.Lenh_thep::Ve_thep_openning1` | Details trimmer rebar and diagonal anti-crack bars around slab/wall openings. |
| 92 | `vemc` | `vemc` | `vemc` | `Kata_pro64_Cad2025.Lenh_thep::tao_MC_cau_kien1` | Draws arbitrary structural cross-section along user-picked cut line. |

### 2.5 Subsystem 5: Rebar Schedule, Coupler & 1D Cutting-Stock Commands
| # | Lisp Alias | Full Command | [CommandMethod] Name | Backing C# Class & Method | Description & Parameters |
|---|---|---|---|---|---|
| 93 | `tkt` | `thong_ke_thep`| `thong_ke_thep`| `Kata_pro64_Cad2025.Lenh_thep::Thong_ke` | Generates full rebar schedule table with 68 shape sketches. |
| 94 | `suatk` | `sua_thong_ke` | `sua_thong_ke` | `Kata_pro64_Cad2025.Lenh_thep::Suatk` | Interactive DataGridView editor for schedule rows; updates CAD blocks. |
| 95 | `sstk` | `sstk` | `sstk` | `Kata_pro64_Cad2025.Lenh_thep::sosanh_tk` | Compares two rebar schedule tables and generates difference report. |
| 96 | `ththep` | `tong_hop_thep`| `tong_hop_thep`| `Kata_pro64_Cad2025.Lenh_thep::Ththep` | Generates summary weight table grouped by diameter and TCVN bins. |
| 97 | `tha` | `tha` | `tha` | `Kata_pro64_Cad2025.Lenh_thep::Ththep_atlas` | Generates Atlas-format rebar summary table. |
| 98 | `tkco` | `thong_ke_coupler`| `thong_ke_coupler`| `Kata_pro64_Cad2025.Lenh_thep::Tkcoupler` | Generates coupler schedule from rebar thread annotations (`ghi_chu_ren`). |
| 99 | `sst` | `sst` | `-vbarun profile.thong_ke_cap` | `Kata_pro64_Cad2025.Lenh_thep::Thong_ke_cap` | Generates prestressing tendon / cable schedule table. |
| 100| `tohopthep`| `to_hop_thep` | `to_hop_thep` | `Kata_pro64_Cad2025.Lenh_thep::To_hop_thep1` | Solves 1D-CSP cutting optimization on 11,700 mm stock bars. |
| 101| - | `loadkho` | `loadkho` | `Kata_pro64_Cad2025.Lenh_thep::Loadkho` | Loads existing warehouse offcut inventory (`Kho_thep`) into 1D cutting stock solver. |
| 102| - | `upkho` | `upkho` | `Kata_pro64_Cad2025.Lenh_thep::Upkho` | Commits newly generated reusable offcuts ($\ge L_{\text{min\_kho}}$) back to warehouse inventory. |
| 103| `toexcel` | `toexcel` | `toexcel` | `Kata_pro64_Cad2025.Lenh_thep::Cadtoexcel` | Exports schedule to active `Kata.xlsm` workbook via COM. |
| 104| `toexcelqh`| `toexcelqh` | `toexcelqh` | `Kata_pro64_Cad2025.Lenh_thep::Cadtoexcelqh` | Exports schedule to QH Plus industrial template `Iso_QHPlus.xls`. |
| 105| `itt` | `in_tem_thep` | `in_tem_thep` | `Kata_pro64_Cad2025.Lenh_thep::In_tem_thep1` | Generates fabrication shop bundle tags with 2D QR Code barcodes. |
| 106| - | `bvbs` | `bvbs` | `Kata_pro64_Cad2025.Lenh_thep::Thong_ke_bvbs` | Exports standard BVBS format text file (`.bfx`) for CNC bending machines. |

### 2.6 Subsystem 6: QS Quantity Takeoff Commands
| # | Lisp Alias | Full Command | [CommandMethod] Name | Backing C# Class & Method | Description & Parameters |
|---|---|---|---|---|---|
| 107| `QS` | `QS` | `QS` | `Kata_pro64_Cad2025.Lenh_thep::Trien_khai_QS` | Master quantity surveying workflow dialog (concrete, formwork, steel). |
| 108| `QS_mbkc` | `QS_mbkc` | `QS_mbkc` | `Kata_pro64_Cad2025.Lenh_thep::QS_mbkc` | Extracts quantities directly from structural framing plans. |
| 109| `QS_tru` | `QS_tru` | `QS_tru` | `Kata_pro64_Cad2025.Lenh_thep::Ve_hm_tru` | Deducts column-beam-slab 3D intersections from gross quantities. |
| 110| `xuatQS` | `xuatQS` | `xuatQS` | `Kata_pro64_Cad2025.Lenh_thep::XuatQS` | Exports Bill of Quantities (BOQ) spreadsheet to Excel. |
| 111| `timqs` | `timqs` | `timqs` | `Kata_pro64_Cad2025.Lenh_thep::Tim_QS1` | Locates and highlights surveyed elements on plan. |
| 112| - | `tkqs` | `tkqs` | `Kata_pro64_Cad2025.Lenh_thep::Thong_ke_coppha_be_tong1` | Generates tabular quantity takeoff summary for concrete volume and formwork contact area. |
| 113| `chiatam` | `chia_tam` | `chia_tam` | `Kata_pro64_Cad2025.Lenh_thep::Chia_tam1` | Solves 2D formwork plywood / drywall panel cutting optimization. |
| 114| - | `CHIA_TAM_LIGHT` | `CHIA_TAM_LIGHT` | `Kata_pro64_Cad2025.SurfaceLightCommands::ChiaTamLight` | Lightweight 2D panel layout optimizer with fast graphics preview. |
| 115| `opgach` | `op_gach` | `op_gach` | `Kata_pro64_Cad2025.Lenh_thep::Op_gach1` | Architectural tile layout simulation and cut tile minimization. |
| 116| - | `OP_GACH_LIGHT` | `OP_GACH_LIGHT` | `Kata_pro64_Cad2025.SurfaceLightCommands::OpGachLight` | Lightweight architectural tile layout optimizer with cut pattern preview. |

### 2.7 Subsystem 7: Drawing Production & System Utilities
| # | Lisp Alias | Full Command | [CommandMethod] Name | Backing C# Class & Method | Description & Parameters |
|---|---|---|---|---|---|
| 117| `vg` | `ve_grid` | `ve_grid` | `Kata_pro64_Cad2025.Lenh_thep::Ve_grid` | Draws structural grid bubble coordinate system with continuous dims. |
| 118| `lv` | `lv` | `-vbarun profile.level` | `Kata_pro64_Cad2025.Lenh_thep::Ve_level` | Draws structural elevation level callout datum block. |
| 119| `lv1` | `lv1` | `-vbarun profile.level1` | `Kata_pro64_Cad2025.Lenh_thep::Ve_level1` | Draws structural elevation level callout variant. |
| 120| `cht` | `cht` | `-vbarun profile.doi_text_blockcd` | `Kata_pro64_Cad2025.Lenh_thep::Text_to_block_cd` | Converts plain elevation text to smart elevation datum blocks. |
| 121| `dten` | `dten` | `dten` | `Kata_pro64_Cad2025.Lenh_thep::Danh_stt` | Automated component sequential numbering and mark assignment. |
| 122| `tkten` | `tkten` | `tkten` | `Kata_pro64_Cad2025.Lenh_thep::Tao_table_tk_cau_kien1` | Generates table index of component names found on plan. |
| 123| `tim_ten_ck`| `tim_ten_ck` | `tim_ten_ck` | `Kata_pro64_Cad2025.Lenh_thep::tim_ten_ck` | Locates and zooms viewport to component matching search name. |
| 124| `textblock` | `textblock` | `textblock` | `Kata_pro64_Cad2025.Lenh_thep::Texttoblock1` | Converts AutoCAD text entities into attributed block references. |
| 125| `locdim` | `loc_dim` | `loc_dim` | `Kata_pro64_Cad2025.Lenh_thep::Loc_dim1` | Aligns, filters, and standardizes dimension lines on drawing. |
| 126| `pvn` | `phan_vung_noi` | `phan_vung_noi` | `Kata_pro64_Cad2025.Lenh_thep::Phan_vung_noi1` | Hatches and annotates structural splice confinement zones on plan. |
| 127| `danhmuc` | `danhmuc` | `danhmuc` | `Kata_pro64_Cad2025.Lenh_thep::Tao_danh_muc_ban_ve1`| Generates automated sheet drawing list index table. |
| 128| `cat` | `cat_doituong` | `cat_doituong` | `Kata_pro64_Cad2025.Lenh_thep::cat_doituong1` | Multi-entity trim/break tool along cutting boundary. |
| 129| `taomv` | `taomv` | `taomv` | `Kata_pro64_Cad2025.Lenh_thep::Taomv` | Creates PaperSpace layout viewports at standard engineering scales. |
| 130| `taomv1` | `taomv1` | `taomv1` | `Kata_pro64_Cad2025.Lenh_thep::Taomv1` | Creates PaperSpace layout viewport with custom engineering scale. |
| 131| `sbv` | `sap_ban_ve` | `sap_ban_ve` | `Kata_pro64_Cad2025.Lenh_thep::Sap_ban_ve1` | Automated detail packing and sheet composition into A1/A3 titleblocks. |
| 132| `ibv` | `in_ban_ve` | `in_ban_ve` | `Kata_pro64_Cad2025.InBvCommands::InBanVe` | Batch PDF plot engine publishing sheets using PageSetup definitions. |
| 133| `pdfkata` | `pdfkata` | `pdfkata` | `Kata_pro64_Cad2025.Lenh_thep::PdfKata_Command` | Extracts, parses, and audits rebar schedules from external PDF drawings. |
| 134| `hideatt` | `hideatt` | `hideatt` | `Kata_pro64_Cad2025.Lenh_thep::Hide_att1` | Hides all block attribute text tags in current drawing view. |
| 135| `Unhideatt` | `Unhideatt` | `Unhideatt` | `Kata_pro64_Cad2025.Lenh_thep::UnHide_att1` | Unhides all block attribute text tags in current drawing view. |
| 136| `pukata` | `pukata` | `pukata` | `Kata_pro64_Cad2025.Lenh_thep::Xoa_dictionaries` | Clean purge of orphaned XData, unregistered RegApps, and stale dictionaries. |

---

## 3. Complete 86-Parameter Configuration Schema (`Goi lenh.lsp`)

Lines 152–237 of `C:\kata_pro\Goi lenh.lsp` define an exact **86-line** system configuration block. When KataPro starts, `Kata_pro64_Cad2025.Setting::LoadFromGoiLenhText` parses these lines sequentially using `StreamReader.ReadLine()`. Each line begins with a semicolon `;` (AutoLISP comment syntax) followed by the raw string parameter value:

```
;kata_dim          <- Line 152: Item 1
;kata_net thay     <- Line 153: Item 2
;kata_net khung    <- Line 154: Item 3
...
;0                 <- Line 237: Item 86
```

### 3.1 Exhaustive 86-Parameter Specification Table
The table below specifies all 86 configuration parameters, including physical engineering meaning, unit, default value, C# data type, and the corresponding property in `Kata_pro64_Cad2025.Setting` and `Kata_Class_Lib`:

| # | Line | Parameter Name | Default Value | Data Type | Physical Unit | Physical Meaning & Purpose | Backing Field / Class |
|---|---|---|---|---|---|---|---|
| **1** | 152 | `LayerDim` | `kata_dim` | `string` | CAD Layer Name | Dimension lines layer | `Setting.u5oUNkqRn0` / `Kata_Dimstyle` |
| **2** | 153 | `LayerNetThay` | `kata_net thay` | `string` | CAD Layer Name | Concrete visible edges layer | `Setting.E1qU4K41KC` / `Bien_chung` |
| **3** | 154 | `LayerNetKhung` | `kata_net khung` | `string` | CAD Layer Name | Titleblock / schedule borders layer | `Setting.nTAUPAeUAw` / `Bien_chung` |
| **4** | 155 | `LayerNetKhuat` | `kata_net khuat` | `string` | CAD Layer Name | Concrete hidden edges layer | `Setting.XH8Ub8YFHM` / `Bien_chung` |
| **5** | 156 | `LayerGrid` | `kata_grid` | `string` | CAD Layer Name | Structural grid lines layer | `Setting.SDcUHGZqJh` / `Kata_GridType` |
| **6** | 157 | `LayerThepChu` | `kata_thep chu` | `string` | CAD Layer Name | Main longitudinal rebar layer | `Setting.e19UAT2k5K` / `Kata_Tag_Rebar` |
| **7** | 158 | `LayerThepDai` | `kata_thep dai` | `string` | CAD Layer Name | Primary perimeter stirrups layer | `Setting.X7dU6raEyd` / `Bien_chung` |
| **8** | 159 | `LayerThepDaiPhu`| `kata_thep dai` | `string` | CAD Layer Name | Secondary stirrups / cross ties layer | `Setting.LCCU1XZ1LI` / `Bien_chung` |
| **9** | 160 | `LayerNetManh` | `kata_net manh` | `string` | CAD Layer Name | Thin annotation / extension lines layer | `Setting.BH7UVxNtvm` / `Bien_chung` |
| **10** | 161 | `LayerText` | `kata_text` | `string` | CAD Layer Name | General annotations and notes layer | `Setting.MJkUI7lNF0` / `Bien_chung` |
| **11** | 162 | `LayerTextTag` | `kata_text` | `string` | CAD Layer Name | Rebar tag blocks and callouts layer | `Setting.cvNUrMA4dI` / `Kata_Multi_Tag_Rebar` |
| **12** | 163 | `SpacingPrefix` | `a` | `string` | Prefix | Rebar spacing notation prefix (e.g. `a200`) | `Setting.kgwUQSqbLY` / `chuyen_dai` |
| **13** | 164 | `SmallBarPrefix` | `Ø` | `string` | Symbol | Prefix for plain round bars $\Phi < 10\text{ mm}$ | `Setting.xOXUgxetJb` / `chuyen_thep` |
| **14** | 165 | `DeformedBarPrefix`| `Ø` | `string` | Symbol | Prefix for deformed bars $\Phi \ge 10\text{ mm}$ | `Setting.uFdUYPM1Dh` / `chuyen_thep` |
| **15** | 166 | `DimStyleName` | `kata_dim_iso` | `string` | CAD DimStyle Name | Master dimension style name | `Setting.PufUxehKct` / `Kata_Dimstyle` |
| **16** | 167 | `ConcreteCoverMm` | `30` | `double` | mm | Default concrete cover to main bars ($c=30$) | `Setting.b57UzY2DYr` / `InfoThep.cover` |
| **17** | 168 | `DefaultHookAngle`| `135/180` | `string` | Degrees ($^\circ$) | Default hook angle ($135^\circ$ stirrups, $180^\circ$ ties)| `Setting.R1UQqZO0xZ` / `info_cog_hook` |
| **18** | 169 | `DetailDrawingScale`| `1/25` | `double` | Scale Ratio | Default detail cross-section scale ($1:25$) | `Setting.xmTQwICV11` / `info_viewport.tl` |
| **19** | 170 | `BaseBarDiameter` | `16` | `int` | mm | Base reference bar diameter ($\Phi 16$) | `Setting.nvqQKWfeSU` / `info_thep.phi` |
| **20** | 171 | `LapLengthMultiplier`| `30` | `double` | Diameters ($d$) | Compression lap splice multiplier ($30d$) | `Setting.UMJQk719kf` / `lap_comp` |
| **21** | 172 | `StaggeredLapMode`| `1` | `int` (0/1) | Flag | Staggered lap splice mode (1 = 50% staggered) | `Setting.DfOQc5Ibty` / `delta_noi` |
| **22** | 173 | `DrawFilletRadius`| `1` | `int` (0/1) | Flag | Draw rebar corners with true bending fillet | `Setting.na9QMmldqH` / `goc_uon_thep` |
| **23** | 174 | `GroupSameRebar` | `1` | `int` (0/1) | Flag | Group identical bars in schedule table | `Setting.EdbQ0KDZRF` / `loai_bo_name_trung` |
| **24** | 175 | `ApplyBendDeduction`| `0` | `int` (0/1) | Flag | Deduct bend elongation (0 = out-to-out sum) | `Setting.BtyQZVPvVh` / `delta_L` |
| **25** | 176 | `AutoRenumberMarks`| `1` | `int` (0/1) | Flag | Auto-renumber rebar marks sequentially ($1..N$) | `Setting.MCXQOrgNWA` / `info_thep.STT` |
| **26** | 177 | `TensionAnchorMult`| `40` | `double` | Diameters ($d$) | Tension anchorage length multiplier ($40d$) | `Setting.O11Q8lpLxF` / `anchor_tens` |
| **27** | 178 | `ColSpliceRatio` | `1/6` | `double` | Height Ratio ($H$) | Column splice location from floor level ($H/6$) | `Setting.aKvQX0F9H0` / `Info_dai_cot` |
| **28** | 179 | `DrawElevationHooks`| `1` | `int` (0/1) | Flag | Draw $90^\circ$ hooks on beam elevation rebars | `Setting.rHPQag64xj` / `Cog90_type_name` |
| **29** | 180 | `DimStyleOverride`| `0` | `int` (0/1) | Flag | Override CAD dimstyle with user font/arrows | `Setting.PboQfh3UCH` / `Kata_Dimstyle` |
| **30** | 181 | `TagStyle` | `0` | `int` | Style Index | Rebar tag style (0 = circle tag, 1 = underline) | `Setting.WXoQiw11JH` / `Kata_Tag_Rebar` |
| **31** | 182 | `ScheduleDiaPrefix`| `Ø` | `string` | Symbol | Diameter prefix symbol on schedule table headers | `Setting.J1iQEvunnO` / `info_thep.phi` |
| **32** | 183 | `StandardBarLength`| `11700` | `int` | mm | Commercial stock rebar length ($11,700\text{ mm}$) | `Setting.UpvQtFcIiG` / `thep_in.noi` |
| **33** | 184 | `MinRemnantLength`| `100` | `int` | mm | Absolute minimum offcut length saved to warehouse | `Setting.Rv2QLNKc93` / `delta_noi` |
| **34** | 185 | `CouplerMinDia` | `0` | `int` | mm | Minimum diameter to use couplers (0 = disabled) | `Setting.gAbQN6jN5F` / `info_coupler.dmin` |
| **35** | 186 | `BeamTopCutoffL1` | `0.2` | `double` | Span Ratio ($L$) | Beam top extra bar cutoff length ($0.2L$) | `Setting.u4XQpqkwKx` / `info_thep_gc_goi` |
| **36** | 187 | `BeamCutoffDatumL1`| ` mép` | `string` | Datum | Datum for L1 cutoff (` mép` = column face) | `Setting.jgcQSw4blV` / `lt_dam` |
| **37** | 188 | `BeamBotCutoff` | `0` | `double` | Span Ratio ($L$) | Beam bottom bar cutoff (0 = run continuous) | `Setting.bb2QJff5eU` / `info_thep_gc_nhip` |
| **38** | 189 | `DenseZoneLayout` | `0` | `int` | Option Index | Stirrup dense zone layout (0 = $L/4$ ends, 1 = full) | `Setting.g8FQ3ucRoS` / `a_dai` |
| **39** | 190 | `LengthRoundingMode`| `0` | `int` | Option Index | Bar length rounding mode (0 = round up 10 mm) | `Setting.ceTQHTogJk` / `Round10` |
| **40** | 191 | `VarBarNaming` | `1a, 1b...` | `string` | Pattern | Variable length rebar mark suffix convention | `Setting.VCsQRISvpA` / `truoc_sh` |
| **41** | 192 | `DrawShapeInSched`| `1` | `int` (0/1) | Flag | Draw rebar shape sketch inside schedule table | `Setting.b4JQ6X1974` / `info_thep.Dang` |
| **42** | 193 | `AnnotationTextH` | `5` | `double` | mm | Text height for callouts and rebar tags ($5\text{ mm}$)| `Setting.uW5QILWetd` / `Bien_chung` |
| **43** | 194 | `DrawCenterlines` | `0` | `int` (0/1) | Flag | Draw rebar centerline axis (0 = outline only) | `Setting.Ko8QQX1jNK` / `CenterLine` |
| **44** | 195 | `TagSeparator` | `-` | `string` | Char | Separator between mark and count (e.g. `1-2Ø18`) | `Setting.iS6QgPoRmn` / `tach_sohieu` |
| **45** | 196 | `ScheduleRowHeight`| `7.5` | `double` | mm | Table row height in schedule block ($7.5\text{ mm}$) | `Setting.KWbQz9gGdN` / `info_thep` |
| **46** | 197 | `SchedShapeColWidth`| `25` | `double` | mm | Shape sketch column width in schedule ($25\text{ mm}$) | `Setting.x3WvqBl3Zi` / `info_thep.Dang` |
| **47** | 198 | `ReservedField47` | `""` | `string` | N/A | Schedule formatting extension field | `Setting.e15Qo9aeOc` |
| **48** | 199 | `BeamTopCutoffL2` | `0.25` | `double` | Span Ratio ($L$) | Beam top extra bar layer 2 cutoff ($0.25L$) | `Setting.SOXvwuE2pF` / `info_thep_gc_goi` |
| **49** | 200 | `BeamCutoffDatumL2`| ` mép` | `string` | Datum | Datum for L2 cutoff (` mép` = column face) | `Setting.hQdvKKgfXx` / `lt_dam` |
| **50** | 201 | `SecondaryBarLength`| `8800` | `int` | mm | Secondary commercial bar length ($8,800\text{ mm}$) | `Setting.wJRv0LnL6s` / `thep_in.noi` |
| **51** | 202 | `LanguageUiSched` | `Tiếng Việt`| `string` | Locale | UI and schedule language (`Tiếng Việt` / `English`)| `Setting.h5jvODRGpT` / `Translate` |
| **52** | 203 | `SchedExportFormat`| `0` | `int` | Option Index | Schedule format (0 = AutoCAD Table, 1 = Excel) | `Setting.X44vj7r25j` / `Cadtoexcel` |
| **53** | 204 | `CouplerSpec` | `0` | `int` | Type Index | Coupler thread type (0 = standard ISO parallel) | `Setting.SN7vi82osb` / `thuan_nghich` |
| **54** | 205 | `ReservedField54` | `""` | `string` | N/A | Steel grade specifier (CB300 / CB400 / CB500) | `Setting.JFMv8Zr1Io` |
| **55** | 206 | `TableTemplate` | `Default` | `string` | Template | Schedule template (`Default` / `QHPlus` / `Atlas`)| `Setting.GjRvbuDWHC` / `Thong_ke_qhp` |
| **56** | 207 | `ReservedField56` | `""` | `string` | N/A | Concrete design code standard (TCVN 5574) | `Setting.ib9vEBB3gM` |
| **57** | 208 | `StirrupClosureType`| `Closed` | `string` | Type | Stirrup closure form (`Closed` seismic / `Open`) | `Setting.a3KvtYctYh` / `U_ngoai` |
| **58** | 209 | `FootingHookMult` | `0` | `double` | Diameters ($d$) | Footing mesh cog length (0 = bend up full depth) | `Setting.grSvLZQPoG` / `Mong_coc` |
| **59** | 210 | `SlabHookMult` | `0` | `double` | Diameters ($d$) | Slab top rebar cog (0 = bend down to soffit) | `Setting.PTLv24ggQ9` / `InfoThep.dbm1` |
| **60** | 211 | `WallHookMult` | `0` | `double` | Diameters ($d$) | Shear wall boundary U-bar hook (0 = wall width) | `Setting.iFvvm48Kpj` / `Info_ColumnWall3D` |
| **61** | 212 | `ReservedField61` | `""` | `string` | N/A | Foundation component prefix (`ĐC`, `M`) | `Setting.Lhgv9HBuwl` |
| **62** | 213 | `TitleScaleMode` | `0` | `int` | Option Index | Titleblock scale mode (0 = auto ModelSpace) | `Setting.AievV7Mv96` / `info_viewport` |
| **63** | 214 | `ReservedField63` | `""` | `string` | N/A | Beam component prefix (`D`, `DS`) | `Setting.UUovIVpFws` |
| **64** | 215 | `AutoDimRebar` | `0` | `int` (0/1) | Flag | Auto-dimension rebar distribution lines on slab | `Setting.QtdvDiacMM` / `Bien_chung_cad` |
| **65** | 216 | `LengthUnitDisplay`| `mm` | `string` | Unit Symbol | Display unit for rebar lengths (`mm` or `m`) | `Setting.VCsQRISvpA` / `SetIsInMm` |
| **66** | 217 | `CutLengthRoundStep`| `50` | `double` | mm | Cutting length rounding interval ($50\text{ mm}$) | `Setting.Ko8QQX1jNK` / `Round50` |
| **67** | 218 | `MassUnitDisplay` | `0` | `int` | Option Index | Schedule mass display unit (0 = kg, 1 = tonnes) | `Setting.rKlQCH91ZV` / `get_text_unit_mass` |
| **68** | 219 | `TotalLengthRound`| `0` | `int` | Option Index | Total length rounding (0 = 2 decimal places) | `Setting.CdLQY79HRG` / `round_td_cad` |
| **69** | 220 | `DrawLeaderArrow` | `1` | `int` (0/1) | Flag | Draw arrowhead on tag leader lines | `Setting.MebQ5aXWK5` / `ARROWHEAD` |
| **70** | 221 | `LeaderArrowType` | `0` | `int` | Type Index | Arrow type (0 = ClosedFilled, 1 = Tick, 2 = Dot) | `Setting.VfnQxE7ikq` / `ARROWHEAD` |
| **71** | 222 | `SecBeamStirrupAdd1`| `5f10a50` | `string` | Spec | Left secondary beam hanger stirrups ($5\Phi 10a50$) | `Setting.aUEQdRxlOZ` / `info_dai_gc.x` |
| **72** | 223 | `SecBeamGroup1` | `1` | `int` | Count | Number of left hanger stirrup groups (= 1) | `Setting.b63vFxvvjr` / `dai_gc_input` |
| **73** | 224 | `SecBeamAntiCrack1`| `2f14` | `string` | Spec | Left diagonal anti-crack rebar ($2\Phi 14$) | `Setting.RfevWXgiUt` / `thep_gc` |
| **74** | 225 | `SecBeamZoneLen1` | `150` | `double` | mm | Left hanger reinforcement zone width ($150\text{ mm}$)| `Setting.oCKvGNdjFa` / `info_dai_gc.rong` |
| **75** | 226 | `SecBeamStirrupAdd2`| `5f10a50` | `string` | Spec | Right secondary beam hanger stirrups ($5\Phi 10a50$)| `Setting.GvkvlJ3Bao` / `info_dai_gc.x` |
| **76** | 227 | `SecBeamGroup2` | `1` | `int` | Count | Number of right hanger stirrup groups (= 1) | `Setting.YlGvcEyHtv` / `dai_gc_input` |
| **77** | 228 | `SecBeamAntiCrack2`| `2f14` | `string` | Spec | Right diagonal anti-crack rebar ($2\Phi 14$) | `Setting.MnuvMtnjnk` / `thep_gc` |
| **78** | 229 | `SecBeamZoneLen2` | `150` | `double` | mm | Right hanger reinforcement zone width ($150\text{ mm}$) | `Setting.G7FvZFR78Q` / `info_dai_gc.rong` |
| **79** | 230 | `TagCircleRadius` | `10` | `double` | mm | Radius of rebar mark circle tag ($10\text{ mm}$) | `Setting.VnmvBbxVTy` / `Kata_Tag_Rebar` |
| **80** | 231 | `StaggeredOffsetMm`| `50` | `double` | mm | Axial offset between staggered bar splices ($50\text{ mm}$) | `Setting.lC9vXswbol` / `delta_noi` |
| **81** | 232 | `CouplerSepSchedule`| `0` | `int` (0/1) | Flag | Separate coupler schedule table (0 = combined) | `Setting.hJFvawMYav` / `info_coupler.sl` |
| **82** | 233 | `CouplerEndTreat1` | `None` | `string` | Type | Bar end 1 coupler thread treatment (`None`/`Thread`) | `Setting.UF6vfHhF2q` / `ghi_chu_ren` |
| **83** | 234 | `CouplerEndTreat2` | `None` | `string` | Type | Bar end 2 coupler thread treatment (`None`/`Thread`) | `Setting.zDTvndRwyA` / `ghi_chu_ren` |
| **84** | 235 | `PrintQrCodeOnTag` | `0` | `int` (0/1) | Flag | Print QR Code on fabrication bundle tags | `Setting.v9qvsgaWfG` / `In_tem_thep1` |
| **85** | 236 | `RealtimeSync` | `0` | `int` (0/1) | Flag | Realtime schedule sync on rebar stretch | `Setting.Ywvv74tSsN` / `Bien_chung_cad` |
| **86** | 237 | `QsFormulaMode` | `0` | `int` | Option Index | QS volume formula (0 = TCVN, 1 = net 3D intersection)| `Setting.RTSvkmIdmS` / `Trien_khai_QS` |

### 3.2 Robust Parsing & Fallback Logic
The clean-room implementation in `HPKata.Core.Configuration.KataConfigParser` replicates the exact behavior of `Setting::LoadFromGoiLenhText`:
1. Strips leading/trailing whitespaces, tabs, and leading semicolons: `.Trim().TrimStart(';').Trim()`.
2. Handles EOF/missing lines gracefully: if `Goi lenh.lsp` has fewer than 237 lines, remaining properties retain default values defined in `KataConfigDefaults`.
3. Culture-invariant number parsing: uses `CultureInfo.InvariantCulture` so `.` is consistently parsed as decimal point regardless of Windows regional settings.
4. Scale parsing: fractions such as `"1/25"` or `"1/6"` are evaluated as $1.0 / 25.0 = 0.04$ or $1.0 / 6.0 \approx 0.16667$.
5. **Character Encoding & Vietnamese Code Page Support**: `KataConfigParser` implements an automatic dual-encoding detection strategy. Vietnamese engineering workstations commonly create or edit `Goi lenh.lsp` in legacy Windows ANSI codepages. Specifically:
   - Line 164 & 165 contain diameter symbols `;Ø` (`\u00D8`).
   - Line 187 & 200 contain Vietnamese diacritics `; mép`.
   - Line 202 contains `;Tiếng Việt`.
   The parser first reads using strict UTF-8 (`new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)`). If invalid byte sequences are detected or decoding yields Unicode replacement characters (`\uFFFD`), the parser automatically falls back to Windows-1258 (Vietnamese ANSI Code Page 1258 registered via `System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); Encoding.GetEncoding(1258)`), with secondary fallback to Windows-1252. This guarantees zero character corruption for diameter symbols, datum marks, and layer naming across any Windows locale.

---

## 4. DWG Data Schemas & `Iso_kata.dwg` Standards

### 4.1 Master CAD Layer Standards
`Iso_kata.dwg` registers 12 standard CAD layers. Every entity produced by the engine must adhere to these exact layer names, color indices, linetypes, and lineweights:

| Layer Name | Color Index (ACI) | RGB Code | Linetype | Lineweight (mm) | Engineering Purpose |
|---|---|---|---|---|---|
| `kata_thep chu` | 1 (Red) | 255, 0, 0 | `Continuous` | 0.50 | Main longitudinal rebar (beams, columns, footings, walls) |
| `kata_thep dai` | 220 (Magenta) | 255, 0, 191 | `Continuous` | 0.35 | Stirrups, ties, confining links, and spirals |
| `kata_thep - V` | 2 (Yellow) | 255, 255, 0 | `Continuous` | 0.40 | Vertical reinforcement on elevation drawings |
| `kata_thep - H` | 1 (Red) | 255, 0, 0 | `Continuous` | 0.40 | Horizontal reinforcement on elevation drawings |
| `kata_dim` | 8 (Dark Gray) | 128, 128, 128 | `Continuous` | 0.15 | Dimensions, extension lines, distribution leaders |
| `kata_net thay` | 4 (Cyan) | 0, 255, 255 | `Continuous` | 0.30 | Concrete visible edges, slab outlines, beam sections |
| `kata_net khung`| 3 (Green) | 0, 255, 0 | `Continuous` | 0.35 | Titleblock borders, schedule table outlines, callout boxes |
| `kata_net khuat`| 9 (Light Gray) | 192, 192, 192 | `HIDDEN` | 0.15 | Concealed concrete edges, openings, footings below slab |
| `kata_grid` | 8 (Dark Gray) | 128, 128, 128 | `CENTER` | 0.15 | Structural coordinate grid axes and bubbles |
| `kata_net manh` | 8 (Dark Gray) | 128, 128, 128 | `Continuous` | 0.13 | Section cutting plane lines, hatch patterns, leaders |
| `kata_text` | 7 (White/Black) | 255, 255, 255 | `Continuous` | 0.20 | General annotations, titles, schedule text |
| `Defpoints` | 7 (White) | 255, 255, 255 | `Continuous` | Default (No-plot)| Dimension definition nodes, unprinted origin handles |

### 4.2 Dimension Styles & Text Styles
- **Text Style `kata_text`**:
  - Primary font: `arial.ttf` (TrueType Font with full Vietnamese Unicode Diacritics support).
  - Text Height (`TextHeight`): `0.0` (variable, driven by view scale: 2.0 mm or 2.5 mm in PaperSpace).
  - Width Factor (`WidthFactor`): `0.80` (compact layout maximizing character density in schedule cells).
  - Oblique Angle (`ObliquingAngle`): `0.0` (upright).

- **Master Dimension Style `kata_dim_iso`**:
  - Arrowhead (`DIMBLK`): `kata_block_ArchTick` (45° architectural tick).
  - Arrow size (`DIMASZ`): `1.5 mm`.
  - Extension line extension (`DIMEXE`): `1.5 mm`.
  - Extension line offset (`DIMEXO`): `1.2 mm`.
  - Dimension gap (`DIMGAP`): `1.0 mm`.
  - Text height (`DIMTXT`): `2.5 mm` (Style: `kata_text`).

- **Scale-Driven DimStyles Family**:
  - 14 pre-configured styles scaling geometry by factor $S$: `kata_dim_15` ($\times 15$), `kata_dim_20` ($\times 20$), `kata_dim_25` ($\times 25$), `kata_dim_30` ($\times 30$), `kata_dim_40` ($\times 40$), `kata_dim_50` ($\times 50$), `kata_dim_70` ($\times 70$), `kata_dim_80` ($\times 80$), `kata_dim_100` ($\times 100$), `kata_dim_120` ($\times 120$), `kata_dim_150` ($\times 150$), `kata_dim_200` ($\times 200$), `kata_dim_250` ($\times 250$), `kata_dim_300` ($\times 300$).

- **Rebar Distribution DimStyle `kata_rai_thep`**:
  - Arrowhead block: `kata_block_RUNTIC` / `kata_block_LEADERTIC`.
  - Dimension Text format: displays bar count and spacing (e.g. `<>%%c10a150` or `15%%c10a150`).

### 4.3 Structural Block Definitions & Attribute Tag Schema
`Iso_kata.dwg` contains **119 block definitions** (84 rebar shape blocks and 35 functional/structural blocks). The primary structural blocks and their exact attribute tags are:

```
kata_block_KHT (Standard 2-Bar Mark Tag Block)
  ├── Attribute SH1    : Bar Mark #1 (e.g. "1")
  ├── Attribute DK1    : Bar Diameter #1 (e.g. "18")
  ├── Attribute KC1    : Bar Spacing #1 (e.g. "a200" or "")
  ├── Attribute DKKC1  : Formatted Diameter & Spacing (e.g. "Ø18" or "Ø10a200")
  ├── Attribute SH2    : Bar Mark #2 (e.g. "2")
  ├── Attribute DK2    : Bar Diameter #2 (e.g. "16")
  ├── Attribute KC2    : Bar Spacing #2
  └── Attribute DKKC2  : Formatted Diameter & Spacing

kata_block_KHTCT (Detailed 4-Bar Mark Tag Block)
  ├── Attributes SH1..SH4   : Bar Marks 1 to 4
  └── Attributes DKKC1..DKKC4: Formatted Diameter/Spacing callouts

kata_block_Beam_plan (Beam Framing Plan Symbol)
  ├── Attribute NAME_BEAM   : Beam designation (e.g. "D1-TANG 2")
  ├── Attribute B           : Beam width (e.g. "300")
  ├── Attribute H           : Beam depth (e.g. "600")
  ├── Attribute LEVEL       : Top-of-beam elevation (e.g. "+3.600")
  └── Attribute SPAN        : Clear span distance (e.g. "6000")

Kata_Block_Beam_Rebar (Beam Cross-Section Rebar Model)
  ├── Attribute U_NGOAI     : Stirrup outer form ("Closed" / "Open")
  ├── Attribute THEP_T      : Top main bars (e.g. "3f20")
  ├── Attribute THEP_D      : Bottom main bars (e.g. "3f20")
  ├── Attribute THEP_GC     : Side skin bars (e.g. "2f12")
  ├── Attribute CD          : Cantilever / anchorage length
  └── Attribute A_DAI       : Stirrup spacing specification (e.g. "a100/a200")

Kata_Block_Column&Wall (Column/Wall Section Model)
  ├── Attribute TEN_COT     : Column mark (e.g. "C1", "C2")
  ├── Attribute B           : Column width (e.g. "400")
  ├── Attribute H           : Column depth (e.g. "600")
  ├── Attribute THEP_CHINH  : Main bars layout (e.g. "8f22")
  └── Attribute THEP_DAI    : Stirrup layout (e.g. "f10a100/200")

Kata_Block_Mong_Don (Isolated Footing Model)
  ├── Attribute TEN_MONG    : Footing mark (e.g. "M1")
  ├── Attribute L, B, H     : Footing dimensions length, width, depth
  ├── Attribute THEP_X      : Bottom rebar in X direction (e.g. "f14a150")
  ├── Attribute THEP_Y      : Bottom rebar in Y direction (e.g. "f14a150")
  └── Attribute CAO_TRINH   : Base elevation (e.g. "-2.200")

dangCoupler (Coupler Schedule Row Block)
  ├── Attribute L           : Coupler sleeve length (mm)
  ├── Attribute DK          : Bar diameter (mm)
  ├── Attribute SCK         : Repetition multiplier (member count)
  ├── Attribute SL1         : Couplers per member
  ├── Attribute SL          : Total coupler count (= SCK * SL1)
  └── Attribute GHI_CHU     : Thread specification (e.g. "Ren thẳng ISO")
```

---

## 5. XData RegApp `"KATA"`, DxfCodes & Dynamic Handle-Linking Engine

### 5.1 RegApp Registry
KataPro registers five `RegApp` application identities in the drawing `APPID` table:
1. `"KATA"`: Master rebar domain RegApp attached to all rebar polylines, tag blocks, and distribution dimension lines.
2. `"KATA_AI"`: Machine learning / command suggestion metadata.
3. `"KATA_DAM_CONG_MKBC"`: Planar developed coordinates for curved beams.
4. `"KATA_DAM_CONG_MKBC_THE_HIEN"`: Viewport representation for curved beams.
5. `"KATA_RIBBON"`: UI ribbon state persisted in the Named Object Dictionary.

### 5.2 XData Schema on Rebar Polylines
Every rebar polyline entity carries a structured XData stream under RegApp `"KATA"`. The exact field ordering, DxfCode types, and C# mappings are:

```
[1001: "KATA"]
  ├── 1000: Dang              -> Shape code string ("00" to "67", "02a", "24a")
  ├── 1070: STT               -> Bar mark integer (Int16: e.g. 1, 2, 15)
  ├── 1070: phi               -> Nominal bar diameter in mm (Int16: e.g. 10, 16, 20, 25)
  ├── 1000: kc                -> Spacing string (e.g. "@150", "@200", or "" for main bars)
  ├── 1000: sl                -> Bar count string expression (e.g. "4", "8", "2x4")
  ├── 1070: sl_ck             -> Member count multiplier (Int16, default 1)
  ├── 1000: L                 -> Semicolon-delimited segment lengths in mm (e.g. "400;1200;400")
  ├── 1000: goc_uon           -> Special bending angle in degrees (e.g. "45", "135", or "")
  ├── 1000: ten_ck            -> Component designation (e.g. "D1-TANG 2", "C2")
  ├── 1000: stt_thep_chinh    -> Sub-layer identifier ("T" for top, "D" for bottom, "G" for side)
  ├── 1000: chu_thich         -> Threading/coupler/fabrication notes (e.g. "Coupler 1 đầu")
  └── 1000: Z                 -> Absolute floor elevation string (e.g. "+3.600")
```

### 5.3 XData Schema on Tag Blocks & Distribution Dimensions
1. **On Tag Blocks (`kata_block_KHT`, `kata_block_KHTCT`)**:
   - `1001`: `"KATA"`
   - `1005`: Database Handle of the linked rebar polyline (e.g. `"3F2B"`).
   - `1000`: Link type string (`"THEP_DON"`, `"THEP_BO"`, or `"THEP_DAI"`).
   - `1005`: Database Handle of the associated distribution dimension line (if present).

2. **On Distribution Dimensions (`kata_rai_thep`)**:
   - `1001`: `"KATA"`
   - `1000`: Runlength distribution parameters (`"L_RUN;KC;SO_KHOANG"`).
   - `1070`: Nominal spacing step $a$ (Int16: e.g. `150`, `200`).
   - `1005`: Database Handle of the linked rebar polyline or tag block.

### 5.4 Dynamic 2-Way Handle-Linking Engine
The linking engine maintains bidirectional referential integrity across geometry, tag callouts, and schedules:

```
+-------------------------------------------------------------+
|                     Rebar Polyline                          |
|  - Geometry: 2D LWPolyline with fillet arcs at vertices     |
|  - XData[1001:"KATA"]: Dang="22", STT=5, phi=10, kc="@150"  |
|  - Handle: "3F2B"                                           |
+------------------------------+------------------------------+
                               ^
                               | Mutually cross-referenced
                               v
+------------------------------+------------------------------+
|                     Tag Block (thep)                        |
|  - Block Attributes: SH="5", SL="24", DKKC="Ø10a150"        |
|  - XData[1001:"KATA"]: Handle[1005]="3F2B", DimHandle="3F2C"|
|  - Handle: "3F2A"                                           |
+------------------------------+------------------------------+
                               ^
                               | Geometric proximity & Handle
                               v
+-------------------------------------------------------------+
|               Distribution Dimension (rai_thep)             |
|  - Measured Run Length: L_dim = 3,450 mm                    |
|  - XData[1001:"KATA"]: Step[1070]=150, TagHandle[1005]="3F2A"|
|  - Calculated Bar Count: n = floor(3450 / 150) + 1 = 24     |
|  - Handle: "3F2C"                                           |
+-------------------------------------------------------------+
```

#### Synchronous Event Flow:
1. **Creation (`tao_lien_ket` / `link`)**:
   - User selects a rebar polyline (Handle $H_{\text{bar}}$) and a tag block (Handle $H_{\text{tag}}$).
   - $H_{\text{bar}}$ is written into the tag block's XData `DxfCode.Handle` (1005).
   - $H_{\text{tag}}$ is written into the polyline's XData `DxfCode.Handle` (1005).
2. **Dynamic Copy (`copy_tao_lien_ket` / `cl`)**:
   - Copies both rebar polyline and tag block simultaneously.
   - AutoCAD assigns brand-new unique handles $H'_{\text{bar}}$ and $H'_{\text{tag}}$ to the clones.
   - The engine intercepts the deep clone, inspects cloned entities, and re-binds their XData handles to $H'_{\text{bar}} \leftrightarrow H'_{\text{tag}}$, preventing stale links to the original entities.
3. **Modification & Geometry Update (`update_thep` / `ud`)**:
   - Triggered manually via `ud` or automatically via AutoCAD `ObjectModified` event.
   - Reads vertex coordinates of modified polyline, decomposes vectors into segment lengths $L_1, L_2, \dots, L_k$, and computes new cutting length $L_{\text{cut}} = \sum L_i - \Delta L$.
   - Inspects linked tag block Handle: if entity exists and is not erased, updates block attributes `SH`, `SL`, `DK`, `KC`, and `CD1T`.
   - If linked polyline was erased by user (`IsErased == true`), tag is marked as orphan (displayed in yellow warning color).
4. **Dimension Bar Count Resolution (`xd_sl_tu_dim_gan_block`)**:
   - Proximity search scans for dimension entities within search radius $R = 100\text{ to } 300\text{ mm}$ of tag block origin.
   - Computes bar count $n = \left\lfloor \frac{L_{\text{dim}}}{a} \right\rfloor + 1$ and sets attribute `SL = n.ToString()`.

### 5.5 Modern Extension Dictionary JSON Serialization
For forward compatibility with modern cloud and BIM tools, `Kata_pro64_Cad2025.Ham_xdata::SetXRecordJson` embeds complete serialized JSON payloads into the entity's Extension Dictionary under key `"KATA"`:

```json
{
  "Dang": "22",
  "STT": 5,
  "Phi": 10,
  "KhoangCach": "@150",
  "SoLuong": 24,
  "SoLuongCauKien": 1,
  "KichThuoc": [350.0, 550.0, 75.0],
  "GocUon": 135.0,
  "TenCauKien": "D1-TANG 2",
  "GhiChu": "Dai kin",
  "Z": 3.6
}
```

---

## 6. Complete Mathematical Catalog of All 68 Rebar Shapes (`dang00`..`dang67` + 16 Variants)

### 6.1 Hook Rules & Bending Mandrel Radii
In accordance with TCVN 5574:2018 and Kata standard detailing practice:

- **$90^\circ$ Standard Hook (Square Cog / Móc vuông)**:
  - Standard length: $L_{\text{hook}} = \text{Cog} = 10d \text{ to } 12d$ (clamped: $L_{\text{hook}} \ge 75\text{ mm}$).
  - Internal bend radius: $r_{\text{inner}} = 2.0d$ (for $d \le 16\text{ mm}$); $r_{\text{inner}} = 3.5d$ (for $d \ge 18\text{ mm}$).
- **$135^\circ$ Seismic Stirrup Hook (Móc đai 135°)**:
  - Standard length: $L_{\text{hook}} = \text{hook135} = 7.5d \text{ to } 10d$ (clamped: $L_{\text{hook}} \ge 75\text{ mm}$).
  - Internal bend radius: $r_{\text{inner}} = 1.5d$ (for $d \le 10\text{ mm}$); $r_{\text{inner}} = 2.0d$ (for $d > 10\text{ mm}$).
- **$180^\circ$ Circular Hook (Móc tròn 180°)**:
  - Standard length: $L_{\text{hook}} = \text{hook180} = 5d + 4d\text{ straight extension}$ (clamped: $L_{\text{hook}} \ge 60\text{ mm}$).
  - Internal bend radius: $r_{\text{inner}} = 1.25d$ (for plain round bars CB240-T).

- **Bend Deduction / Gain Formulas**:
  - When `ApplyBendDeduction == 0` (default standard in Vietnam): $L_{\text{cut}} = \sum L_i$ (out-to-out sum).
  - When `ApplyBendDeduction == 1` (true centerline deduction):
    $$\Delta L_{90^\circ} = 2(r_{\text{inner}} + d) - \frac{\pi}{2}\left(r_{\text{inner}} + \frac{d}{2}\right) \approx 0.5d \text{ to } 1.0d$$
    $$\Delta L_{135^\circ} \approx 1.5d$$

### 6.2 Standard Unit Mass Table (`tra_bang_kl`)
Calculated using density $\rho = 7,850\text{ kg/m}^3$:

$$\text{Unit Mass } (\text{kg/m}) \approx \frac{\pi d^2}{4} \times 7,850 \times 10^{-6}$$

| Diameter $\Phi$ (mm) | Unit Mass (kg/m) | Area $A_s$ (cm²) | TCVN 1651:2018 |
|---|---|---|---|
| **$\Phi 6$** | **0.222** | 0.283 | CB240-T |
| **$\Phi 8$** | **0.395** | 0.503 | CB240-T |
| **$\Phi 10$** | **0.617** | 0.785 | CB300-V / CB400-V |
| **$\Phi 12$** | **0.888** | 1.131 | CB300-V / CB400-V |
| **$\Phi 14$** | **1.210** | 1.539 | CB300-V / CB400-V |
| **$\Phi 16$** | **1.580** | 2.011 | CB300-V / CB400-V / CB500-V |
| **$\Phi 18$** | **2.000** | 2.545 | CB300-V / CB400-V / CB500-V |
| **$\Phi 20$** | **2.470** | 3.142 | CB300-V / CB400-V / CB500-V |
| **$\Phi 22$** | **2.980** | 3.801 | CB300-V / CB400-V / CB500-V |
| **$\Phi 25$** | **3.850** | 4.909 | CB300-V / CB400-V / CB500-V |
| **$\Phi 28$** | **4.830** | 6.158 | CB300-V / CB400-V / CB500-V |
| **$\Phi 32$** | **6.310** | 8.042 | CB300-V / CB400-V / CB500-V |
| **$\Phi 36$** | **7.990** | 10.179 | CB400-V / CB500-V |
| **$\Phi 38$** | **8.900** | 11.341 | Special order |
| **$\Phi 40$** | **9.860** | 12.566 | CB400-V / CB500-V |
| **$\Phi 41$** | **10.360** | 13.202 | Special order |
| **$\Phi 43$** | **11.400** | 14.522 | Special order |
| **$\Phi 50$** | **15.420** | 19.635 | Heavy civil / bridge |
| **$\Phi 51$** | **16.040** | 20.428 | Heavy civil / bridge |

### 6.3 Mathematical Catalog of All 85 Shapes (68 Base + 16 Variants + Coupler)
The table below specifies the topology, segment parameters ($L_1..L_6, \text{goc}$), exact cutting length formula ($L_{\text{cut}} = \text{CD1T}$), and Revit RebarShape mapping for all 85 shapes:

| Shape Code | Name / Engineering Type | Geometric Topology | Segment Parameters | Cutting Length Formula ($L_{\text{cut}}$) | Hook Rules & Variants | Revit Shape Equivalent |
|---|---|---|---|---|---|---|
| **`dang00`** | Generic Freeform Rebar | Arbitrary $N$-vertex polyline | Open polyline vertices | $L_{\text{cut}} = \sum_{i=1}^{N-1} \text{dist}(P_{i+1}, P_i)$ | Freeform polyline in CAD | Freeform Rebar |
| **`dang01`** | Straight Main Bar | 1 straight segment | $L_1$ | $L_{\text{cut}} = L_1$ | Main longitudinal bar | `RebarShape 00` / `M_00` |
| **`dang02`** | Symmetric U-Bar | 3 segments, two 90° bends | $L_1, L_2$ ($L_3 = L_1$) | $L_{\text{cut}} = 2L_1 + L_2$ | Footings, open cap stirrups | `RebarShape 21` / `M_21` |
| **`dang02a`**| Symmetric U-Bar with 135° Hooks | 5 segments (U + 2 hooks) | $L_1, L_2, \text{hook}$ | $L_{\text{cut}} = 2L_1 + L_2 + 2\cdot \text{hook135}$ | Seismic cap tie variant | `M_21` + 135° Hooks |
| **`dang03`** | L-Bar / Square Corner Bar | 2 segments, one 90° bend | $L_1, L_2$ | $L_{\text{cut}} = L_1 + L_2$ | Beam-column knee joint | `RebarShape 11` / `M_11` |
| **`dang04`** | Asymmetric U-Bar | 3 segments, two 90° bends | $L_1, L_2, L_3$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Unequal leg U-bar | `RebarShape 21` (Asym) |
| **`dang05`** | Z-Bar / Step Bar | 3 segments, opposing 90° bends| $L_1, L_2, L_3$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Slab/beam elevation step | `RebarShape 26` / `M_26` |
| **`dang06`** | Cranked Bar | 3 segments, 2 bends angle $\alpha$ | $L_1, L_2, L_3, \alpha$ | $L_{\text{cut}} = L_1 + \left( \begin{cases} \frac{L_2}{\sin(\alpha \cdot \pi / 180)} & \text{if } \text{abs}(\sin(\alpha \cdot \pi / 180)) \ge 10^{-4} \\ L_2 & \text{otherwise} \end{cases} \right) + L_3$ | Flexural span transition | `RebarShape 25` |
| **`dang07`** | Chamfered Corner Bar | 2 segments, angle $\alpha$ | $L_1, L_2, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2$ | Haunch corner rebar | `RebarShape 12` |
| **`dang08`** | Column Offset Crank Bar | 3 segments with sloped offset | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = L_1 + \sqrt{L_2^2 + L_3^2} + L_4$ | Column section step ($1:6$) | `RebarShape 25` |
| **`dang08a`**| Inverted Column Offset Crank | 3 segments offset opposite | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = L_1 + \sqrt{L_2^2 + L_3^2} + L_4$ | Opposite face offset | `RebarShape 25` (Mirror) |
| **`dang09`** | U-Bar with 90° Outward Cogs | 5 segments (U + 2 cogs) | $L_1, L_2, L_3$ ($L_3 = L_1$) | $L_{\text{cut}} = 2L_1 + L_2 + 2\cdot \text{Cog}$ | Pile cap edge reinforcement| `RebarShape 23` |
| **`dang09a`**| U-Bar with 180° Circular Hooks | 5 segments (U + 2 hooks) | $L_1, L_2, L_3$ | $L_{\text{cut}} = 2L_1 + L_2 + 2\cdot \text{hook180}$| Plain bar footing edge | `M_21` + 180° Hooks |
| **`dang10`** | J-Bar with Cranked Leg | 3 segments, 1 square, 1 sloped| $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Retaining wall toe bar | `RebarShape 31` |
| **`dang11`** | Open C-Stirrup | 4 segments, three 90° bends | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + L_4$ | Thin-wall beam stirrup | `RebarShape 32` |
| **`dang12`** | Stair Flight Crank Bar 1 | 4 segments along flight steps | $L_1, L_2, L_3, L_4, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + L_4$ | Stair soffit bar | `RebarShape 33` |
| **`dang12a`**| Stair Flight Crank Bar Inverted| 4 segments inverted slope | $L_1, L_2, L_3, L_4, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + L_4$ | Stair landing top bar | `RebarShape 33` (Mirror) |
| **`dang13`** | Open Trapezoidal Stirrup | 5 segments with sloped sides | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Tapered beam stirrup | `RebarShape 41` |
| **`dang13a`**| Symmetric Trapezoidal Stirrup | 5 segments ($L_5=L_1, L_4=L_2$)| $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3$ | Symmetrical gutter tie | `RebarShape 41` (Sym) |
| **`dang14`** | Closed Rectangular Ring (Welded)| 4 segments closed perimeter | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = 2(L_1 + L_2)$ (welded) | Welded confining ring | `RebarShape 51` (Welded)|
| **`dang15`** | C-Bar Same-Direction Cogs | 3 segments, two 90° cogs | $L_1, L_2$ ($L_3 = L_1$) | $L_{\text{cut}} = 2L_1 + L_2$ | Slab border trimming | `RebarShape 21` (Flanged)|
| **`dang16`** | Multi-Span Continuous Stepped | 6 segments, multiple steps | $L_1, L_2, L_3, L_4, L_5, L_6$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Continuous beam step | `RebarShape 55` |
| **`dang17`** | Nested Column Stirrup Box | 4 segments closed with overlap | $L_1, L_2, L_3, L_1$ | $L_{\text{cut}} = 2(L_1 + L_2) + L_3$ | Column anti-bursting tie | `RebarShape 52` |
| **`dang18`** | Z-Bar Opposing 90° Cogs | 3 segments, opposite 90° cogs | $L_1, L_2$ ($L_3 = L_1$) | $L_{\text{cut}} = 2L_1 + L_2$ | Inter-wall shear dowel | `RebarShape 26` |
| **`dang19`** | Semi-Circular U-Bar (Tension Tie)| 2 straight legs + 180° semi-circle | $L_1, L_2$ | $L_{\text{cut}} = 2L_1 + \pi L_2$ | Plain round tension tie (`Iso_QHPlus.xls` Row 23) | `RebarShape 21` (Round) |
| **`dang20`** | S-Tie / Double Z-Bar | 4 segments, two 90° steps | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Deep beam web tie | `RebarShape 42` |
| **`dang21`** | Polygon Chamfered Stirrup | 4 segments with corner chamfer| $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Chamfered drainage trench| `RebarShape 43` |
| **`dang22`** | Standard Closed Stirrup (135°) | Box perimeter + two 135° hooks | $L_1, L_2, L_3$ ($L_3 = \text{hook}$)| $L_{\text{cut}} = 2(L_1 + L_2) + 2L_3$ | **Primary beam/column tie**| `RebarShape 51` / `M_T1` |
| **`dang23`** | Spiral Overlap Closed Stirrup | 5 segments with overlap leg | $L_1..L_4, L_{\text{lap}}$ | $L_{\text{cut}} = 2(L_1 + L_2) + L_{\text{lap}}$| Torsion-resisting stirrup | `RebarShape 53` |
| **`dang23a`**| Overlap Closed Stirrup Short Leg| Overlap located on short leg | $L_1..L_4, L_{\text{lap}}$ | $L_{\text{cut}} = 2(L_1 + L_2) + L_{\text{lap}}$| High-shear column tie | `RebarShape 53` (Short) |
| **`dang24`** | Diamond Stirrup (Rhombus) | 4 equal angled legs + 135° hooks| $L_1, L_2, L_3$ ($L_1 = L_2$) | $L_{\text{cut}} = 4L_1 + 2L_3$ | 8-bar column inner tie | `RebarShape 61` |
| **`dang24a`**| Asymmetric Diamond Stirrup | 4 legs unequal diamond | $L_1, L_2, L_3$ | $L_{\text{cut}} = 2(L_1 + L_2) + 2L_3$ | Rectangular column diamond| `RebarShape 61` (Asym) |
| **`dang25`** | Octagonal Stirrup | 8 chamfered legs + hooks | $L_1, L_2, L_3, \text{hook}$ | $L_{\text{cut}} = 2(L_1 + L_2) + 4L_3 + 2\cdot \text{hook135}$ | Octagonal / round column | `RebarShape 62` |
| **`dang26`** | 6-Leg Multi-Cell Beam Stirrup | Composite 3-cell stirrup cage | $L_1..L_6$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Transfer beam mega stirrup | `RebarShape 63` |
| **`dang27`** | Closed Trapezoidal Stirrup | Trapezoid box + two 135° hooks | $L_1..L_4, \text{hook}$ | $L_{\text{cut}} = L_1 + 2L_2 + L_3 + 2\cdot \text{hook}$| Sloped beam / parapet | `RebarShape 64` |
| **`dang28`** | Triangular Stirrup | 3 legs + two 135° hooks | $L_1, L_2, L_3, \text{hook}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + 2\cdot \text{hook}$| Triangular precast pile | `RebarShape 65` |
| **`dang29`** | 5-Leg Stirrup with Internal Tie | Perimeter box + central cross-tie| $L_1..L_5$ | $L_{\text{cut}} = 2(L_1 + L_2) + L_5$ | Wide beam stirrup | `RebarShape 66` |
| **`dang30`** | Stepped Footing Edge Bar | Stepped profile along slope | $L_1..L_5$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Stepped boundary footing | `RebarShape 54` |
| **`dang31`** | Circular Arch Bar with Tangents | Arc radius $R$ + two tangent legs | $L_1..L_4, R, \theta$ | $L_{\text{cut}} = 2L_1 + \pi R \frac{\theta}{180}$ | Arch bridge / tunnel vault | `RebarShape 71` |
| **`dang32`** | Pile-to-Cap Flared Dowel | 4 segments flared outward | $L_1..L_4$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Bored pile head breakout | `RebarShape 72` |
| **`dang33`** | Spatial Foundation Cage Basket | 3D folded footing rebar | $L_1..L_5$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Pylon / transmission tower | Freeform Rebar |
| **`dang34`** | Continuous Wave Bar (Sinusoidal)| Periodic wave bends | $L_1, L_2, n$ | $L_{\text{cut}} = 2n \cdot \sqrt{L_1^2 + L_2^2}$ | Continuous shear key | `RebarShape 73` |
| **`dang35`** | Slab Chair Bar (Chân chó kê sàn) | 6 segments: 2 feet, 2 risers, top| $L_1, L_2, L_3$ ($2L_1+2L_2+L_3$)| $L_{\text{cut}} = 2L_1 + 2L_2 + L_3$ | **Slab top mesh chair** | `RebarShape 74` |
| **`dang35a`**| Simplified Slab Chair Bar | 3 parameters symmetric | $L_1, L_2, L_3$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3$ | Standard chair bar variant| `RebarShape 74` (Sym) |
| **`dang36`** | Mat Foundation Chair with Wings | Chair bar with side lock wings | $L_1..L_5$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Heavy raft foundation chair| `RebarShape 75` |
| **`dang37`** | Curved Vault Bar with Anchors | Inverted circular arch + cogs | $L_1..L_5, R, \theta$ | $L_{\text{cut}} = L_1 + \pi R \frac{\theta}{180} + L_5$ | Circular water tank wall | `RebarShape 76` |
| **`dang38`** | Parabolic U-Bar | Curved soffit + 2 vertical legs| $L_1, L_2, R$ | $L_{\text{cut}} = 2L_1 + \pi R$ | Drainage channel invert | `RebarShape 77` |
| **`dang39`** | Spiral-to-Straight Transition | Helical transition into straight | $L_1..L_4, D, s, n$ | $L_{\text{cut}} = L_1 + L_2 + n\sqrt{(\pi D)^2 + s^2}$| Bored pile top connection | `RebarShape 78` |
| **`dang40`** | Circular Hoop Rebar (Lapped) | Complete circle + lap splice | $L_1 (D), L_2 (L_{\text{lap}})$ | $L_{\text{cut}} = \pi L_1 + L_2$ | **Circular column / pile tie**| `RebarShape 79` / `M_77` |
| **`dang40a`**| Circular Hoop with 135° Hooks | Circle + two internal 135° hooks| $L_1 (D), L_2 (\text{hook})$ | $L_{\text{cut}} = \pi L_1 + 2\cdot \text{hook135}$| Seismic round column tie | `M_77` + 135° Hooks |
| **`dang41`** | Flat U-Bar / Hairpin | 2 perpendicular segments | $L_1, L_2$ | $L_{\text{cut}} = L_1 + L_2$ | Slab edge hairpins | `RebarShape 11` |
| **`dang42`** | Chamfered Hairpin | 3 segments with mitered corner | $L_1, L_2, L_3$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Skewed slab boundary | `RebarShape 13` |
| **`dang43`** | Basement Retaining Wall Dowel | L-bar with footing toe cog | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Cantilever basement wall | `RebarShape 81` |
| **`dang43a`**| Basement Wall Dowel Inverted Toe| Toe cog turned inward | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Property boundary wall | `RebarShape 81` (Mirror)|
| **`dang44`** | Cantilever Balcony Loop | Closed loop folded back | $L_1..L_4, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Balcony / canopy slab | `RebarShape 82` |
| **`dang45`** | Sloped Column Neck Bar | Vertical bar with sloped neck | $L_1..L_4, \text{goc}$ | $L_{\text{cut}} = L_1 + \sqrt{L_2^2 + L_3^2} + L_4$| **Column section reduction**| `RebarShape 83` |
| **`dang45a`**| Right-Angle Column Neck Bar | 90° horizontal step transition | $L_1..L_5$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Right-angle column shift | `RebarShape 83` (Step) |
| **`dang46`** | Transfer Girder Double Step | Two-tier stepped profile | $L_1..L_5$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Stepped transfer girder | `RebarShape 84` |
| **`dang47`** | Corbel Loop (Tai dầm công-xôn) | Triangular corbel loop | $L_1..L_4$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Crane bracket corbel | `RebarShape 85` |
| **`dang48`** | Top Negative Moment Slab Hat | Inverted U-bar with feet | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Slab support negative hat | `RebarShape 86` |
| **`dang49`** | Inclined Shear Truss Bar | Main bar cranked at shear zone | $L_1..L_5, \alpha$ | $L_{\text{cut}} = L_1 + \left( \begin{cases} \frac{L_2}{\sin(\alpha \cdot \pi / 180)} & \text{if } \text{abs}(\sin(\alpha \cdot \pi / 180)) \ge 10^{-4} \\ L_2 & \text{otherwise} \end{cases} \right) + L_3$ | High shear diagonal bar | `RebarShape 87` |
| **`dang49a`**| Double Inclined Shear Truss Bar| Symmetrical two-end crank | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Short-span heavy load beam| `RebarShape 87` (Double)|
| **`dang50`** | Restroom Slab Drop (Sàn WC) | Z-bar with 50mm elevation drop| $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + L_4$ | Sunken restroom slab | `RebarShape 88` |
| **`dang51`** | Seismic Cap Tie (Móc C 135°-135°) | C-bar with two 135° seismic cogs| $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + 2\cdot \text{hook}$| Wall boundary element | `RebarShape 89` |
| **`dang51a`**| Open Cap Tie (Móc C 90°-135°) | One 90° leg, one 135° leg | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + \text{Cog} + \text{hook}$| Easy-installation tie | `RebarShape 89` (Hybrid)|
| **`dang52`** | Single Cross Tie (Móc đai đơn) | Straight bar with 135° and 90°| $L_1, L_2$ | $L_{\text{cut}} = L_1 + \text{hook135} + \text{Cog90}$ | **Column/beam cross-tie** | `RebarShape 91` |
| **`dang53`** | Secondary Beam Hanger U-Stirrup| Deep U-stirrup hugging girder | $L_1..L_5$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3$ | **Secondary beam hanger** | `RebarShape 92` |
| **`dang53a`**| Hanger Stirrup with Outward Wings| U-stirrup with slab flange wings| $L_1..L_5$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3 + 2L_4$| Heavy junction hanger | `RebarShape 92` (Flanged)|
| **`dang54`** | Skewed Batter Pile Cap Anchor | Wedge-shaped corner tie | $L_1..L_4, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Batter pile cap anchor | `RebarShape 93` |
| **`dang55`** | Box Culvert Frame Rebar | U-shaped frame with rigid knees | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Highway box culvert | `RebarShape 94` |
| **`dang56`** | Stair Landing Transition Bar | Double cranked transition | $L_1..L_4, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Stair flight landing knee | `RebarShape 95` |
| **`dang57`** | Skewed Torsion Stirrup | 45° skewed rhombus | $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^3 L_i$ | Curved bridge deck torsion | `RebarShape 96` |
| **`dang58`** | Opening Corner Diagonal Bar | Sloped bar with 90° end cogs | $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = L_1 + 2L_2$ | Slab opening anti-crack | `RebarShape 97` |
| **`dang59`** | Slab Edge Torsion U-Bar | U-bar hugging slab perimeter | $L_1, L_2, L_3$ | $L_{\text{cut}} = 2L_1 + L_2$ | Flat plate slab edge | `RebarShape 21` (Edge) |
| **`dang60`** | Precast Pile Splice Ring | Circular collar casing ring | $L_1..L_4, L_1$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Spun pile joint ring | `RebarShape 79` |
| **`dang61`** | Deep Basement Wall Counterfort | Multi-bend tapered bracket | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Diaphragm wall counterfort | Freeform Rebar |
| **`dang62`** | Water Tank Corner Haunch Bar | Corner L-bar with mitered knee | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Water tank corner haunch | `RebarShape 98` |
| **`dang62a`**| Tank Haunch Bar with Anchor Wings| Corner bar with anchor flanges | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Hydrostatic pressure tank | `RebarShape 98` (Flanged)|
| **`dang63`** | Masonry Wall Dowel Pin (Râu tường)| $\Phi 6$ pin with 90° anchor leg | $L_1..L_4$ | $L_{\text{cut}} = L_1 + L_2$ | Brick wall masonry tie | `RebarShape 11` |
| **`dang64`** | Bridge Pier Cap Conical Cage | Tapered conical cap frame | $L_1..L_5$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Bridge pier cap | Freeform Rebar |
| **`dang65`** | Precast Beam End Bursting Mesh | End block anti-bursting rebar | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Post-tensioned anchorage | `RebarShape 99` |
| **`dang65a`**| Extended Beam End Bursting Mesh| Flanged wide end block mesh | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Bulb-tee bridge girder | `RebarShape 99` (Flanged)|
| **`dang66`** | Mat Foundation High-Chair Wave | Continuous multi-bay M-wave | $L_1..L_6$ | $L_{\text{cut}} = \sum_{i=1}^6 L_i$ | Raft foundation high-chair | Freeform Rebar |
| **`dang67`** | Continuous Helical Spiral Rebar | Circular cylindrical helix | $L_1 (D), L_2 (p), H, n$ | $L_{\text{cut}} = n \cdot \sqrt{(\pi L_1)^2 + L_2^2} + 2\cdot \text{hook135}$ | **Bored pile spiral tie** ($n = \lceil H / L_2 \rceil$) | `RebarShape 77` (Spiral)|
| **`dangCoupler`**| Mechanical Coupler Sleeve | Cylinder / threaded fitting | $L, \text{DK}$ | $L_{\text{cut}} = L$ | Rebar mechanical splice | Coupler Element |

### 6.4 Mathematical Edge-Case Clamping & Resolution Rules
When implementing shape calculations in `HPKata.Core.Shapes`, the following domain rules must be enforced:
1. **Divide-by-Zero Protection on Angular Segments (`dang06`, `dang49`)**:
   Angular parameters (`goc` / $\alpha$) are stored in degrees ($0^\circ \le \alpha \le 90^\circ$). The engine converts to radians: $\theta = \alpha \times \frac{\pi}{180}$.
   If $|\sin \theta| < 10^{-4}$ (e.g. $\alpha = 0^\circ$ or missing/unparsed in legacy CAD), the engine falls back to $L_{\text{slope}} = L_2$ (or out-to-out sum $L_1 + L_2 + L_3$), preventing unhandled runtime exceptions.
2. **Column Crank Slopes (`dang08`, `dang08a`, `dang45`)**:
   Standard column offset cranks follow a $1:6$ slope ratio ($L_3 = 6 L_2$). The sloped crank hypotenuse is strictly evaluated as $\sqrt{L_2^2 + L_3^2}$. If explicit CAD polyline vertices are supplied, the length is computed directly from vertex coordinates via $\text{dist}(P_{i+1}, P_i)$.
3. **Helical Spiral Turn Resolution (`dang67`)**:
   When rebar block attributes supply diameter $D = L_1$ and pitch $p = L_2$, total turn count is resolved as $n = \lceil H / p \rceil$ where $H$ is extracted from member height or bar count `SL`. When $H$ is not present, $n$ defaults to `SL`. Total length includes finishing end hooks ($2 \times \text{hook135}$) or 1.5 extra turns ($1.5 \pi D$).
4. **Circular Hoops (`dang40`, `dang40a`)**:
   $L_1$ represents out-to-out diameter $D$. When centerline bend deduction mode is active (`ApplyBendDeduction == 1`), the centerline diameter is clamped: $D_{\text{center}} = \max(D - d, 0)$.
5. **Octagonal Stirrups (`dang25`)**:
   An octagonal stirrup consists of 8 perimeter legs (2 horizontal legs $L_1$, 2 vertical legs $L_2$, and 4 diagonal corner legs $L_3$) plus two standard $135^\circ$ seismic hooks: $L_{\text{cut}} = 2(L_1 + L_2) + 4L_3 + 2\cdot \text{hook135}$.
6. **Out-to-Out vs. Centerline Summation Policy**:
   In accordance with standard Vietnamese detailing practice (`Setting.i8ivw6QY7D == 0`), out-to-out segment summation is the default calculation mode. Centerline bend deduction is applied only when explicitly requested by project configuration.

---

## 7. 1D Cutting-Stock Optimization Engine (`to_hop_thep`)

### 7.1 Mathematical Problem Formulation
The 1D Cutting-Stock Problem (1D-CSP) in reinforcement fabrication seeks to fulfill a set of required rebar pieces of a specific diameter $\Phi$:

$$\mathcal{D} = \{(l_1, q_1), (l_2, q_2), \dots, (l_m, q_m)\}$$

where $l_i$ is the cut length in mm, and $q_i$ is the required quantity.

Cutting is performed from a supply of standard commercial stock bars of length $L_{\text{stock}} = 11,700\text{ mm}$ (Item 32) and available warehouse offcuts $\mathcal{W} = \{(w_1, c_1), \dots, (w_p, c_p)\}$.

A cutting pattern $j$ is an assignment vector $(a_{1j}, a_{2j}, \dots, a_{mj})$ satisfying the linear physical constraint:

$$\sum_{i=1}^m a_{ij} \cdot l_i + (k_j - 1) \cdot \delta_{\text{kerf}} \le L_j \quad (L_j \in \{11,700\} \cup \{w_k\})$$

where $k_j = \sum_{i=1}^m a_{ij}$ is the piece count, and $\delta_{\text{kerf}}$ is the saw kerf allowance ($0\text{ mm}$ for shear cropping, configurable $0\text{ to } 5\text{ mm}$ for saw cut).

The residual offcut of pattern $j$ is:

$$r_j = L_j - \left(\sum_{i=1}^m a_{ij} \cdot l_i + (k_j - 1) \cdot \delta_{\text{kerf}}\right)$$

#### Dual Waste Metrics:
1. **Gross Scrap (Hao hụt thô)**:
   $$\text{Waste}_{\text{gross}} = \frac{\sum_{j} r_j}{\sum_{j} L_j} \times 100\%$$
2. **Net Scrap (Hao hụt ròng, crediting reusable warehouse offcuts)**:
   $$\text{Waste}_{\text{net}} = \begin{cases} 
   \displaystyle \frac{\sum_{j \mid r_j < \text{lmin\_kho}} r_j}{N_{\text{new\_bars}} \times 11,700} \times 100\%, & \text{if } N_{\text{new\_bars}} > 0 \\ 
   0.0\%, & \text{if } N_{\text{new\_bars}} = 0 \text{ and } \sum_{j \mid r_j < \text{lmin\_kho}} r_j = 0 \\ 
   \displaystyle \frac{\sum_{j \mid r_j < \text{lmin\_kho}} r_j}{\sum_{j} L_j} \times 100\%, & \text{if } N_{\text{new\_bars}} = 0 \text{ and } \sum_{j} L_j > 0 
   \end{cases}$$

where $N_{\text{new\_bars}} = \sum_{j \mid L_j = 11,700} 1$ is the count of newly opened commercial standard bars, and $\text{lmin\_kho}$ is the reusable warehouse threshold (default 500–1000 mm; 100 mm absolute clamp). When all cutting demands are satisfied entirely from existing warehouse remnants ($N_{\text{new\_bars}} = 0$), this piecewise formulation provides strict division-by-zero defense while correctly reporting non-zero unrecoverable scrap relative to consumed warehouse stock.

### 7.2 The Five Optimization Heuristics
The clean-room engine implements a multi-heuristic solver suite:

```
                                  Input Bar Demands
                                          │
                                          ▼
                         Phi <= 8mm? ──Yes──► Segregate to Wire Coil Inventory (ld6, ld8)
                                          │
                                          No
                                          ▼
                      ┌────────────────────────────────────────┐
                      │     Choose Solver Strategy (Enum)      │
                      └───────────────────┬────────────────────┘
                                          │
         ┌──────────────┬─────────────────┼─────────────────┬──────────────┐
         ▼              ▼                 ▼                 ▼              ▼
    [1. Auto]     [2. DuBang0]   [3. LapDayCanhDai] [4. BeamSearch] [5. PatternCat]
 (Run Portfolio  (Subset-Sum      (Best-Fit Decr.    (Top-K State    (Modular 11.7m
  & pick best)    Exact Match)      Greedy BFD)       Space K=20)     Dividers)
         │              │                 │                 │              │
         └──────────────┴─────────────────┼─────────────────┴──────────────┘
                                          │
                                          ▼
                               Warehouse Offcut Credit
                      (Remnants >= lmin_kho -> Sheets("Kho_thep"))
                      (Remnants < lmin_kho  -> Non-reusable Scrap)
                                          │
                                          ▼
                             Master Cutting Schedule
                           (Patterns: "2*3200+5300", Waste %)
```

1. **Strategy 1: `DuBang0` (Exact Zero Residual Subset-Sum)**:
   - Evaluates combinations $S \subseteq \{l_i\}$ such that $\sum_{l \in S} l + (|S| - 1)\cdot \delta_{\text{kerf}} = 11,700\text{ mm}$.
   - **Combinatorial Bounds & Time Budget**:
     - Constrains search to subset cardinality $2 \le |S| \le k_{\max} = 5$ (practical fabrication avoids more than 5 cuts per 11.7m bar).
     - Enforces a strict execution timeout budget $T_{\max} \le 1,000\text{ ms}$ (1.0 second) per diameter group via `CancellationTokenSource`.
     - Once the timeout expires or all valid subsets of cardinality $\le 5$ are exhausted, the remaining demand items immediately cascade to **Strategy 2 (`LapDayCanhDai` / BFD)**.
   - Eliminates all zero-waste subsets first, reducing problem dimension rapidly with 0.0% scrap before BFD packing.

2. **Strategy 2: `LapDayCanhDai` (Best-Fit Decreasing / BFD)**:
   - Sorts pieces descending: $l_1 \ge l_2 \ge \dots \ge l_n$.
   - Greedily packs warehouse offcuts first (`ListThepKhoBefore`).
   - For remaining pieces, opens standard 11,700 mm bars, placing each piece into the open bar with the least remaining space that still accommodates it.

3. **Strategy 3: `BeamSearchTopK`**:
   - Breadth-first search exploring the tree of partial cutting patterns while pruning state space to the top $K$ candidate patterns (default $K=20$, configurable $5 \le K \le 100$).
   - Scores partial states using density metric $\rho = \frac{\sum l_i}{\text{UsedBars} \times 11,700}$.
   - Solves complex multi-bar schedules in $< 1.5\text{ seconds}$ on standard desktop hardware.

4. **Strategy 4: `PatternCat` (Modular Template Decomposition)**:
   - Exploits standard integer divisors of $11,700\text{ mm}$:
     - $2 \times 5,850\text{ mm} = 11,700\text{ mm}$ (Waste = 0)
     - $3 \times 3,900\text{ mm} = 11,700\text{ mm}$ (Waste = 0)
     - $4 \times 2,925\text{ mm} = 11,700\text{ mm}$ (Waste = 0)
     - $5 \times 2,340\text{ mm} = 11,700\text{ mm}$ (Waste = 0)
     - $6 \times 1,950\text{ mm} = 11,700\text{ mm}$ (Waste = 0)
     - Common lap splice pairs: $6,000\text{ mm} + 5,700\text{ mm} = 11,700\text{ mm}$ (Waste = 0).

5. **Strategy 5: `Auto` (Portfolio Evaluator)**:
   - Executes `DuBang0`, `LapDayCanhDai`, and `BeamSearchTopK` in parallel worker threads.
   - Calculates composite penalty $P = \text{Waste}_{\text{net}} + 0.1 \times N_{\text{new\_bars}}$.
   - Automatically adopts and returns the winning solution.

### 7.3 Warehouse Offcut Management & Scrap Rules
- **Data Model**: `InfoThepKho` (`dk`, `chieudai`, `soluong`).
- **Persistence**: Serialized to `Sheets("Kho_thep")` in `Kata.xlsm`.
- **Classification**:
  - Remnant $r_j \ge \text{lmin\_kho}$: Added to `ListThepKhoAfter` and credited against project costs.
  - Remnant $r_j < \text{lmin\_kho}$: Logged as unrecoverable scrap (`doan_thua`).
- **Coil Wire Segregation**: Wire diameters $\Phi 6, \Phi 8$ and stirrups $\Phi 10$ sourced from coils are segregated into `ListThepChoPhiNho`, calculating cumulative linear meters without discrete 11.7m cutting.

---

## 8. Excel Templates & Coupler Schedule Schemas

### 8.1 Workbook Schema Catalog
The KataPro ecosystem relies on three standardized Excel workbooks:
1. **`Kata.xlsm`** (Master Vietnamese Detailing Workbook, 15 sheets):
   - `Dam`, `Cot`, `Vach`, `ThepDamAllMBKC`, `save_info`, `Thong_ke`, `Thong_ke_QS`, `To_hop_thep`, `Kho_thep`, `Kata_QS`, `BEAM DESIGN`, `SLAB DESIGN`, `In_tem_thep`, `Data`, `Gan_tag_block`.
2. **`Kata ENG.xlsm`** (English Localized Workbook, 10 sheets):
   - Identical cell formulas and layout with English localized labels.
3. **`Iso_QHPlus.xls`** (Industrial Fabrication Template, 3 sheets):
   - `LoaiThep`, `Dang_thep`, `Output_QHP`.

### 8.2 Sheet `Thong_ke` Detailed Column Schema
The master rebar schedule in `Kata.xlsm` adheres to the following column definition:

| Column | Header | Type | Formula / Origin | Description |
|---|---|---|---|---|
| `A` | TÊN CẤU KIỆN | String | Input / DWG | Component designation (e.g. `B01`, `C01`) |
| `B` | SỐ HIỆU | String | Input / DWG | Bar mark number (e.g. `1`, `2a`) |
| `C` | DẠNG | String | Input (`00`..`67`) | Shape code index matching 68 catalog |
| `D`..`I` | L1, L2, L3, L4, L5, L6 | Double | mm | Segment lengths |
| `J` | Ghi chú | String | Text | Fabrication/coupler notes (`Coupler`, `Ren 1 đầu`) |
| `K` | Ø (mm) | Integer | mm ($6..50$) | Bar nominal diameter |
| `L` | Dài ko kể nối | Double | mm | Structural net bar length |
| `M` | CHIỀU DÀI 1 THANH | Double | mm ($L_{\text{cut}}$) | Total cutting length |
| `N` | SỐ C.KIỆN | Integer | $\ge 1$ | Member repetition count |
| `O` | SỐ THANH MỘT CK | Integer | $\ge 1$ | Bar count per member |
| `P` | SỐ THANH TOÀN BỘ | Integer | `=N * O` | Total bar count |
| `Q` | TỔNG CHIỀU DÀI | Double | `=M * P / 1000` | Total length in meters |
| `R` | TỔNG TRỌNG LƯỢNG | Double | `=Q * tra_bang_kl(K)` | Total weight in kilograms |
| `S` | TỔNG/CẤU KIỆN | Double | `=R / N` | Weight per member |

#### Diameter Summary Table `Thong_ke!U1:AK7` (TỔNG HỢP CỐT THÉP):
- `U2:AK2`: Distinct sorted diameters using VBA UDF: `V2 = SMALL_kata($K5:$K2000, 1)`.
- `U3:AK3`: Total length per diameter: `V3 = SUMIF($K:$K, V2, $Q:$Q)`.
- `U4:AK4`: Total weight per diameter: `V4 = V3 * tra_bang_kl(V2)`.
- `U5`: TCVN Small Wire Group ($\Phi \le 10$): `V5 = SUMIF(V2:AK2, "<=10", V4:AK4)`.
- `U6`: TCVN Medium Deformed Group ($10 < \Phi \le 18$): `V6 = SUM(V4:AK4) - V5 - V7`.
- `U7`: TCVN Large Deformed Group ($\Phi > 18$): `V7 = SUMIF(V2:AK2, ">18", V4:AK4)`.

### 8.3 Coupler Schedule Schema (`Thong_ke!U9:AA15`)
Mechanical couplers are extracted by scanning rebar entities whose `ghi_chu_ren` property contains `"Coupler"` or `"Ren"`, grouping by `(Component, bxh, Diameter)`:

| Cell | Column Header | Data Type | Formula / Origin | Description |
|---|---|---|---|---|
| `U10` | TÊN CẤU KIỆN | String | Component Mark | Structural member designation (`C1`, `D1`) |
| `V10` | KÍCH THƯỚC (mm) | String | Dimensions | Section geometry (e.g. `400x600`, `800x800`) |
| `W10` | Ø (mm) | Integer | Diameter | Bar diameter $\Phi \in [16, 40]$ |
| `X10` | SỐ C.KIỆN | Integer | Member Count | Number of identical members |
| `Y11` | MỘT CK | Integer | Coupler / Member | Couplers in single member |
| `Z11` | TOÀN BỘ | Integer | `= X10 * Y11` | Cumulative project coupler count |
| `AA10` | GHI CHÚ | String | Spec | Mechanical thread spec (e.g. `Parallel ISO`) |

### 8.4 QR Code Tag Generation Schema (`In_tem_thep!A1:F12`)
- **Engine**: `QRCoder.QRCodeGenerator` with **ECC Level Q** (25% damage recovery).
- **Standard Pipe-Delimited Payload**:
  ```
  KATA|{Project}|{Client}|{Order}|{Component}|{Mark}|{ShapeCode}|{Diameter}|{CutLength}|{Quantity}|{Weight}|{Grade}|{L1},{L2},{L3},{L4},{L5},{L6}
  ```
  *Example*:
  ```
  KATA|The Landmark 81|Coteccons|DH-2026-88|B101|1|22|25|2650|12|102.0|CB500-V|350,550,75,0,0,0
  ```
- **12-Row Modular Tag Layout ($A1:F12$)**:
  - `A1:F1`: DỰ ÁN (Project Name)
  - `A2:F2`: KHÁCH HÀNG (Client Name)
  - `A3:F3`: ĐƠN HÀNG (Order Number)
  - `A4:F4`: CẤU KIỆN (Component Mark)
  - `A5:F5`: SỐ HIỆU (Bar Mark)
  - `A6:C12`: 2D QR Code Bitmap + Rebar Shape Thumbnail
  - `D6:F12`: Fabrication parameters: Shape Code, Diameter $\Phi$, Quantity, Cut Length, Weight, Steel Grade, Tag ID.

---

## 9. Revit 2026 Integration Architecture (`Kata_Class_Lib_Revit.dll`)

### 9.1 Host-Neutral Domain DTOs (`HPKata.Core.Models`)
In compliance with Clean Code Host-Neutrality (Rule D1, D4), `HPKata.Core` defines pure domain representations using metric units ($mm$) and zero references to `Autodesk.Revit.*` or `Autodesk.AutoCAD.*`:

```csharp
namespace HPKata.Core.Models
{
    public readonly record struct KataPoint3D(double X, double Y, double Z);
    public readonly record struct KataVector3D(double X, double Y, double Z);

    public enum KataLayoutRule
    {
        Single = 0,
        MaximumSpacing = 1,
        FixedNumber = 2
    }

    public class KataRebarSpec
    {
        public KataPoint3D OriginMm { get; set; }
        public KataVector3D DirectionL { get; set; }
        public KataVector3D DirectionH { get; set; }
        public string Partition { get; set; } = string.Empty;     // e.g. "{ten_ck}"
        public string ShapeName { get; set; } = string.Empty;     // e.g. "Kata_22"
        public string ScheduleMark { get; set; } = string.Empty;  // Bar mark, e.g. "1"
        public string Comment { get; set; } = string.Empty;       // e.g. "HPKata_{BeamName}"
        public int DiameterMm { get; set; }                       // Nominal diameter e.g. 20 (mm)
        public KataLayoutRule LayoutRule { get; set; } = KataLayoutRule.MaximumSpacing;
        public int Quantity { get; set; }
        public double SpacingMm { get; set; }                     // Center-to-center spacing (mm)
        public double ArrayLengthMm { get; set; }                 // Total array length (mm)
        public double[] ParametersMm { get; set; } = [];          // Segments A, B, C, D, E, F in mm
    }
}
```

### 9.2 Native Element Creation & Unit Conversion (`HPKata.Revit`)
All Revit API dependencies (`Autodesk.Revit.DB`, `XYZ`, `Feet`, `Rebar`) are encapsulated exclusively inside `HPKata.Revit.NativeRebar`:

1. **Host Adapter & Unit Conversion (`KataRevitConverter`)**:
   - Converts `KataPoint3D` / `KataVector3D` (mm) to internal Revit `XYZ` (feet) using scale factor $1 / 304.8$:
     ```csharp
     public static XYZ ToRevitXyz(KataPoint3D pt) => new(pt.X / 304.8, pt.Y / 304.8, pt.Z / 304.8);
     public static XYZ ToRevitVector(KataVector3D v) => new(v.X, v.Y, v.Z); // normalized direction
     public static double ToFeet(double mm) => mm / 304.8;
     ```
   - Maps `KataRebarSpec.ParametersMm` to Revit feet parameters.
   - Constructs coplanar `IList<Curve>` representing the rebar centerline in Revit coordinates.
2. **Type Resolution & Hook Attachment**:
   - Resolves `RebarBarType` by numerical diameter query:
     $$|\text{RebarBarType.BarModelDiameter} \times 304.8 - \Phi| < 0.5\text{ mm}$$
   - Start and end hooks are resolved via `RebarHookType`:
     - `"Kata Stirrup - 90 deg."`
     - `"Kata Stirrup - 135 deg."`
     - `"Kata Stirrup - 180 deg."`
   - Configured via `rebar.SetHookTypeId(0, startHookId)` and `rebar.SetHookTypeId(1, endHookId)`.
3. **Array Layout Rules (`RebarShapeDrivenAccessor`)**:
   - Maps `spec.LayoutRule`:
     - `KataLayoutRule.MaximumSpacing`: Sets `RebarLayoutRule.MaximumSpacing` with `BarsOnNormalSpacing` using `ToFeet(spec.SpacingMm)` and `ToFeet(spec.ArrayLengthMm)`.
     - `KataLayoutRule.FixedNumber`: Sets `RebarLayoutRule.FixedNumber` with `spec.Quantity`.
     - `KataLayoutRule.Single`: Sets `RebarLayoutRule.Single`.
   - Binds parameters:
     - `BuiltInParameter.REBAR_PARTITION = spec.Partition`
     - `BuiltInParameter.ALL_MODEL_MARK = spec.ScheduleMark`
     - `BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS = spec.Comment`

### 9.3 Idempotent Regeneration Pattern
To prevent element duplication on repeated detailing runs, `HPKata.Revit` executes an idempotent sweep within a Revit `Transaction`:

```csharp
public static void CleanExistingKataRebars(Document doc, Element hostElement, string beamName)
{
    var collector = new FilteredElementCollector(doc)
        .OfClass(typeof(Rebar))
        .WherePasses(new ElementOwnerViewFilter(ElementId.InvalidElementId)) // 3D model rebars
        .Cast<Rebar>()
        .Where(r => r.GetHostId() == hostElement.Id);

    string tagPrefix = $"HPKata_{beamName}";
    foreach (var rebar in collector)
    {
        var commentParam = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
        if (commentParam != null && commentParam.AsString()?.StartsWith(tagPrefix) == true)
        {
            doc.Delete(rebar.Id);
        }
    }
}
```

### 9.4 View Automation & Partition Filters
1. **Section View Generation (`tao_section_dung`, `tao_section_nam`)**:
   - Computes bounding box oriented along beam longitudinal axis.
   - Creates elevation `ViewSection` and transverse cross-section `ViewSection`.
2. **Partition Parameter View Filters**:
   - Creates `ParameterFilterElement` with rule:
     $$\text{Rule: } \text{BuiltInParameter.REBAR_PARTITION} == \text{beamName}$$
   - Applies filter to section views, turning off visibility of rebars from adjacent structural members.
3. **Multi-Reference Annotations**:
   - Places `MultiReferenceAnnotation` rebar callouts with leaders pointing to rebar sets.

---

## 10. Clean-Room Governance, Compliance & Verification Matrix

### 10.1 Clean-Room Governance Protocols
1. **Zero Licensing/DRM Decompilation**:
   `Security.dll` was strictly excluded from inspection, reflection, and loading. No hardware dongle checks, license files, or activation algorithms were investigated.
2. **Strictly Read-Only Access**:
   `C:\kata_pro` was accessed in read-only mode. All source code, tests, and documentation are authored from first principles in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\`.
3. **Clean Code Rule Compliance (`HP_CLEAN_CODE_CORE.md`)**:
   - Rule C2: Classes $\le 300$ lines.
   - Rule M3: Methods $\le 50$ lines.
   - Rule Q-B1: Zero dead/commented-out code.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Host-neutrality: `HPKata.Core` references neither AutoCAD nor Revit reference assemblies.

### 10.2 Reverse-Engineering Evidence & Verification Matrix

| Blueprint Section | Legacy Spec Source | Mining Method | Verification Artifact | Verification Status |
|---|---|---|---|---|
| **§1 Architecture** | `Kata_pro64_Cad2025.dll`, `Kata_Class_Lib.dll` | Mono.Cecil assembly reflection | `survey_commands_config_report.md` §1, §6 | VERIFIED |
| **§2 Commands** | `Goi lenh.lsp`, `Kata_pro64_Cad2025.dll` | Lisp AST parser + MethodDef scan | `all_commands.txt` (193 methods) | VERIFIED |
| **§3 86 Config** | `Goi lenh.lsp` lines 152–237, `Setting.cs` | Line-by-line Lisp & IL decompiler | `survey_commands_config_report.md` §5 | VERIFIED |
| **§4 DWG Standards** | `Iso_kata.dwg` | AutoCAD Database Table Scanner | `dwg_blocks_inventory.json` (119 blocks)| VERIFIED |
| **§5 XData Linking** | `Ham_xdata.cs`, `Link_thep.cs` | IL Decompilation of XData handlers | `survey_dwg_shapes_report.md` §3 | VERIFIED |
| **§6 68 Shapes** | `dang00..67.wmf`, `Iso_kata.dwg` ATTDEFs | Vector WMF parser + DWG ATTDEF scan | `full_shapes_analysis.json` (85 shapes) | VERIFIED |
| **§7 1D Cutting** | `Kata.xlsm!to_hop.bas`, `Form_To_Hop_Thep` | VBA module inspection + IL Decompile| `survey_scheduling_interop_report.md` §3| VERIFIED |
| **§8 Excel/Coupler**| `Kata.xlsm`, `Iso_QHPlus.xls`, `QRCoder` | OpenXML schema audit + VBA decomp | `survey_scheduling_interop_report.md` §4-6| VERIFIED |
| **§9 Revit Interop**| `Kata_Class_Lib_Revit.dll` | Mono.Cecil reflection on `Revit_rebar`| `survey_scheduling_interop_report.md` §7| VERIFIED |
| **§10 Governance** | `docs/clean-code/HP_CLEAN_CODE_CORE.md` | Policy & Audit Checklist | Forensic Audit Verification Ready | VERIFIED |

---
*End of Authoritative Technical Blueprint.*
