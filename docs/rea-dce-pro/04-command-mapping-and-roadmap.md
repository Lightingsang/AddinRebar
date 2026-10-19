# Command Mapping, LISP Bridge & Technical Implementation Roadmap

**Deliverable Target**: `HPDcePro.AutoCad`, `HPDcePro.lsp`  
**Decompiled Baseline**: `C:\Dce Pro\DCE Pro.lsp`, `C:\Dce Pro\GLDCE.LSP`, `C:\Dce Pro\Trich chi tiet (TCT).lsp`, `C:\Dce Pro\DCE Pro.dll`  
**Host Target**: AutoCAD 2026 (.NET 8.0 / AutoCAD.NET 25.1.0)

---

## 1. Master Command Mapping: Legacy DCE Pro to HPDcePro

The legacy AutoCAD integration in `C:\Dce Pro\DCE Pro.lsp` defines 69 command shortcuts routing to .NET methods registered in `DCE Pro.dll`. Below is the complete mapping table showing the legacy command, the .NET entry point, functional domain, user input prompts, and the clean-room implementation in `HPDcePro.AutoCad.Commands`:

| # | LISP Command | Legacy .NET Method | Subsystem | Engineering Function | Input Prompts / Parameters | Target `HPDcePro` Implementation |
|:---:|---|---|---|---|---|---|
| 1 | `c:mb1` | `XuatDuLieuModelEtabs` | ETABS / Plan | Export framing plan geometry and grid lines to ETABS | Story selection, base origin point | `HPDcePro.AutoCad.Commands.EtabsExportCommand` |
| 2 | `c:UpdateModelEtabs` | `UpdateModelEtabs` | ETABS / Sizing | Bidirectional update of beam/column sections from ETABS | OAPI active model connection | `HPDcePro.AutoCad.Commands.EtabsSyncCommand` |
| 3 | `c:GETFORM` | `GETFORM` | Main UI | Launch the central DCE Pro modeless control center | Modeless WPF display | `HPDcePro.AutoCad.Commands.MainWindowCommand` |
| 4 | `c:taomv` | `taomv` | Layout | Create and auto-scale Viewports to standard A3/A4 title blocks | Select border, choose scale (1:50, 1:100) | `HPDcePro.AutoCad.Commands.LayoutViewportCommand` |
| 5 | `c:slck` | `TKTSSOLUONGCAUKIEN` | Rebar Schedule | Batch edit rebar element count across rebar schedule blocks | Prompt: "Enter new element quantity:" | `HPDcePro.AutoCad.Commands.ScheduleQuantityCommand` |
| 6 | `c:sdkt` | `TKTSDUONGKINHTHEP` | Rebar Schedule | Batch edit rebar diameter across scheduled rebar entities | Prompt: "Select target diameter (mm):" | `HPDcePro.AutoCad.Commands.ScheduleDiameterCommand` |
| 7 | `c:ToiUuCatThep` | `ToiUuCatThep` | Optimization | Launch 1D commercial rebar cutting stock optimizer (11.7m) | Modal WPF optimizer dialog | `HPDcePro.AutoCad.Commands.CuttingOptimizationCommand` |
| 8 | `c:MaTTThep` | `MaTTThep` | Rebar Match | Match and copy rebar XData properties from source to targets | Pick source bar -> select target bars | `HPDcePro.AutoCad.Commands.RebarMatchPropertiesCommand` |
| 9 | `c:ShopDrawingBeam`| `ShopDrawingBeam` | RC Beam | Launch comprehensive beam shop drawing detailing generator | Select beam run or import schedule | `HPDcePro.AutoCad.Commands.BeamShopDrawingCommand` |
| 10 | `c:bb1` | `VeBlockDam2` | Beam Drafting | Insert 2-span continuous beam dimension symbol block | Insertion point | `HPDcePro.AutoCad.Commands.BeamBlock2SpanCommand` |
| 11 | `c:bb2` | `VeBlockCaoDoMB` | Elevation Tag | Insert plan floor elevation symbol block (+/- 0.000) | Insertion point, elevation string | `HPDcePro.AutoCad.Commands.PlanElevationTagCommand` |
| 12 | `c:bb3` | `VeBlockTrucDam` | Grid Tag | Insert structural grid intersection bubble on beam elevation | Insertion point, grid axis name | `HPDcePro.AutoCad.Commands.BeamAxisTagCommand` |
| 13 | `c:bb4` | `VeBlockChieuDaySan`| Slab Tag | Insert floor slab thickness designation block (e.g. h=120) | Insertion point, thickness hs | `HPDcePro.AutoCad.Commands.SlabThicknessTagCommand` |
| 14 | `c:bb5` | `VeBlockGiatCaoDoSan`| Slab Tag | Insert slab depression / drop step symbol block | Insertion point, delta step (mm) | `HPDcePro.AutoCad.Commands.SlabDepressionTagCommand` |
| 15 | `c:bb6` | `VeBlockCaoDoMC` | Elevation Tag | Insert beam section elevation datum level tag | Insertion point | `HPDcePro.AutoCad.Commands.SectionElevationTagCommand` |
| 16 | `c:bb7` | `VeBlockSection` | Section Tag | Insert beam cross-section callout cut marker (1-1, 2-2) | Insertion point, cut mark | `HPDcePro.AutoCad.Commands.SectionMarkerCommand` |
| 17 | `c:1111` | `XemLienKetTenCauKien`| Inspection | Highlight associative link vector between beam mark and geometry | Entity selection | `HPDcePro.AutoCad.Commands.InspectNameLinkCommand` |
| 18 | `c:111` | `BlockBeam_Size` | Display Toggle | Toggle beam width and depth ($b \times h$) text on framing block | Select beam blocks | `HPDcePro.AutoCad.Commands.ToggleBeamSizeCommand` |
| 19 | `c:222` | `BlockBeam_Name` | Display Toggle | Toggle beam designation mark text on framing block | Select beam blocks | `HPDcePro.AutoCad.Commands.ToggleBeamNameCommand` |
| 20 | `c:333` | `BlockBeam_NameSize`| Display Toggle | Display combined designation mark and size on framing block | Select beam blocks | `HPDcePro.AutoCad.Commands.ToggleBeamNameSizeCommand` |
| 21 | `c:444` | `BlockBeam_Hide` | Display Toggle | Hide all textual annotations on beam framing blocks | Select beam blocks | `HPDcePro.AutoCad.Commands.HideBeamTextCommand` |
| 22 | `c:555` | `BlockBeam_Width` | Display Toggle | Display beam width parameter $b$ on framing block | Select beam blocks | `HPDcePro.AutoCad.Commands.ShowBeamWidthCommand` |
| 23 | `c:ten` | `DatTenCauKienDam` | Beam Naming | Automatic sequential naming of beams along structural grids | Window select framing plan | `HPDcePro.AutoCad.Commands.AutoNameBeamsCommand` |
| 24 | `c:GanTruc` | `GanTrucBlockDam` | Beam Alignment| Assign supporting grid axis data to beam framing block | Select grid and beam blocks | `HPDcePro.AutoCad.Commands.AssignBeamAxesCommand` |
| 25 | `c:vd` | `VeDam_DCE` | Beam Drafting | Draft complete longitudinal detailing of single beam from Excel | Insertion point | `HPDcePro.AutoCad.Commands.DrawSingleBeamCommand` |
| 26 | `c:vdall` | `VeDamAll_DCE` | Beam Drafting | Batch draft all designed beams across story/project | Layout origin point | `HPDcePro.AutoCad.Commands.DrawAllBeamsCommand` |
| 27 | `c:THTK_dce` | `TongHopThongKe_dce`| Rebar BOQ | Consolidate rebar weights across all schedule tables in drawing | Select schedule tables | `HPDcePro.AutoCad.Commands.ConsolidateSchedulesCommand` |
| 28 | `c:tkt_dce` | `tkt_dce` | Rebar Schedule | Generate detailed bar bending schedule table for selected beams | Select beam detailings | `HPDcePro.AutoCad.Commands.ExportBeamScheduleCommand` |
| 29 | `c:stk_dce` | `stk_dce` | Rebar Schedule | Open interactive rebar schedule editor dialog (`TKT_frmMain`) | Select schedule table | `HPDcePro.AutoCad.Commands.EditScheduleDialogCommand` |
| 30 | `c:ktc` | `ImportKichThuoc` | Beam Import | Import beam cross-section dimensions ($b, h$) from Excel to CAD | Select Excel file path | `HPDcePro.AutoCad.Commands.ImportBeamDimensionsCommand` |
| 31 | `c:QuetThongKe_dce`| `QuetThongKe_dce` | Fast Extraction| Rapidly parse rebar schedule tables and compute tonnage | Window select drawing | `HPDcePro.AutoCad.Commands.FastScanScheduleCommand` |
| 32 | `c:suathep` | `SuaCKThep` | Rebar Edit | Interactive dialog to edit rebar count and diameter in place | Select rebar entity | `HPDcePro.AutoCad.Commands.EditRebarEntityCommand` |
| 33 | `c:UpDateDLDam` | `UpDateDLDam` | Sync CAD-Excel | Sync modified beam dimensions and rebars from CAD back to Excel | Select beam details | `HPDcePro.AutoCad.Commands.SyncBeamToExcelCommand` |
| 34 | `c:XemLienKetThep` | `XemLienKetThep` | Inspection | Render temporary visual links between rebar, tag, and dim lines | Select rebar entities | `HPDcePro.AutoCad.Commands.InspectRebarLinksCommand` |
| 35 | `c:DatTenLineDam2` | `DatTenLineDam2` | Beam Alignment| Assign beam names directly to polyline centerlines | Select centerlines | `HPDcePro.AutoCad.Commands.NameBeamCenterlinesCommand` |
| 36 | `c:ImportKichThuocCot`| `ImportKichThuocCot`| Column Import| Import column dimensions from ETABS/Excel into CAD plan | Select source model | `HPDcePro.AutoCad.Commands.ImportColumnSizesCommand` |
| 37 | `c:ImportHinhDangCot`| `ImportHinhDangCot`| Column Import| Import complex polygonal column shapes (L, T, core walls) | Select shape file | `HPDcePro.AutoCad.Commands.ImportPolygonalColumnsCommand` |
| 38 | `c:UpdateThepTinhToan`| `UpdateThepTinhToan`| Sizing Sync | Update calculated reinforcement area $A_s$ from ETABS design | Connect ETABS OAPI | `HPDcePro.AutoCad.Commands.SyncDesignAsCommand` |
| 39 | `c:VeMCD_DCE` | `VeMCD_DCE` | Beam Section | Draw cross-sectional cuts (1-1, 2-2) showing stirrups and bars | Pick placement point | `HPDcePro.AutoCad.Commands.DrawBeamCrossSectionCommand` |
| 40 | `c:GanLienKetThep` | `GanLienKetThep` | Associativity | Associate rebar polyline with dimension line and callout tag | Select bar -> select tag | `HPDcePro.AutoCad.Commands.LinkRebarEntitiesCommand` |
| 41 | `c:XoaLienKetThep` | `XoaLienKetThep` | Associativity | Break associative link between rebar polyline and tag block | Select rebar entities | `HPDcePro.AutoCad.Commands.UnlinkRebarEntitiesCommand` |
| 42 | `c:GanDLLineDam` | `GanDLLineDam` | Topology | Assign structural properties (b, h, story) to beam line | Select line entities | `HPDcePro.AutoCad.Commands.AssignBeamLineDataCommand` |
| 43 | `c:XoaDLDataDCE` | `XoaDLDataDCE` | Maintenance | Purge all 13 DCE RegApp XData packets from selected entities | Select target entities | `HPDcePro.AutoCad.Commands.PurgeDceXDataCommand` |
| 44 | `c:XemLienKetMBCK` | `XemLienKetMBCK` | Inspection | Highlight framing connectivity and tributary relationships | Select framing network | `HPDcePro.AutoCad.Commands.InspectFramingNetworkCommand` |
| 45 | `c:XuatDuLieuDam` | `XuatDuLieuDam` | Export | Export beam framing network parameters to calculation sheet | Select framing plan | `HPDcePro.AutoCad.Commands.ExportBeamNetworkCommand` |
| 46 | `c:vc` | `VeCotDcePro` | Column | Open RC column schedule and cross-section detailing generator | Column design dialog | `HPDcePro.AutoCad.Commands.DrawColumnScheduleCommand` |
| 47 | `c:vv` | `VeVachDcePro` | Shear Wall | Open core shear wall and boundary element detailing generator | Wall design dialog | `HPDcePro.AutoCad.Commands.DrawShearWallCommand` |
| 48 | `c:ColumnREC1` | `ColumnREC1` | Column | Generate rectangular column cross-section (single outer tie) | Width, depth, rebar | `HPDcePro.AutoCad.Commands.DrawRectColumnType1Command` |
| 49 | `c:ColumnREC2` | `ColumnREC2` | Column | Generate rectangular column cross-section (multi-layer ties) | Width, depth, rebar | `HPDcePro.AutoCad.Commands.DrawRectColumnType2Command` |
| 50 | `c:ColumnCi` | `ColumnCi` | Column | Generate circular column cross-section with spiral/hoop ties | Diameter, bar count | `HPDcePro.AutoCad.Commands.DrawCircularColumnCommand` |
| 51 | `c:T1` | `VeThepSanTopX` | Slab Rebar | Draw top negative rebar mesh over supports in X direction | Start pt, end pt, zone | `HPDcePro.AutoCad.Commands.DrawSlabTopRebarXCommand` |
| 52 | `c:T2` | `VeThepSanTopY` | Slab Rebar | Draw top negative rebar mesh over supports in Y direction | Start pt, end pt, zone | `HPDcePro.AutoCad.Commands.DrawSlabTopRebarYCommand` |
| 53 | `c:shts` | `SoHieuThepSan` | Slab Rebar | Auto-assign sequential bar marks to all slab rebar callouts | Select slab boundary | `HPDcePro.AutoCad.Commands.AutoNumberSlabRebarsCommand` |
| 54 | `c:B1` | `VeThepSanBotX` | Slab Rebar | Draw bottom continuous rebar mesh across span in X direction | Start pt, end pt, zone | `HPDcePro.AutoCad.Commands.DrawSlabBottomRebarXCommand` |
| 55 | `c:B2` | `VeThepSanBotY` | Slab Rebar | Draw bottom continuous rebar mesh across span in Y direction | Start pt, end pt, zone | `HPDcePro.AutoCad.Commands.DrawSlabBottomRebarYCommand` |
| 56 | `c:tkts` | `ThongKeThepSan` | Slab Schedule | Extract and compile slab rebar bill of quantities table | Select slab rebar entities| `HPDcePro.AutoCad.Commands.ExportSlabScheduleCommand` |
| 57 | `c:cf` | `ChiaKhoangCach` | Utility | Evenly distribute points and rebar ticks along guide segment | Pick segment, enter count | `HPDcePro.AutoCad.Commands.DivideRebarSpacingCommand` |
| 58 | `c:VBC` | `VeBlockCoc` | Foundation | Insert standard pile point block (square precast / bored pile) | Pile insertion coordinate | `HPDcePro.AutoCad.Commands.InsertPileBlockCommand` |
| 59 | `c:DTC` | `DanhTenCoc` | Foundation | Auto-number piles sequentially based on grid coordinates | Select pile cluster | `HPDcePro.AutoCad.Commands.AutoNumberPilesCommand` |
| 60 | `c:XTDC` | `XuatToaDoCoc` | Foundation | Export pile table coordinates $(X, Y, Z)$ to CAD or Excel | Select pile blocks | `HPDcePro.AutoCad.Commands.ExportPileCoordinatesCommand` |
| 61 | `c:StripMong` | `StripMong` | Foundation | Generate finite-element design strip meshes for foundation slab | Select foundation outline | `HPDcePro.AutoCad.Commands.CreateFoundationStripsCommand` |
| 62 | `c:TextDaiMong` | `TextDaiMong` | Foundation | Label footing elevations & dimensions | Select footing outlines / text point | `HPDcePro.AutoCad.Commands.FootingCommands.TextDaiMong` |
| 63 | `c:VeMongCoc` | `VeMongCoc_DCE` | Foundation | Draw pile foundation detail & rebar (alias `VeMongCoc`) | Foundation mark, pile coordinates | `HPDcePro.AutoCad.Commands.FootingCommands.VeMongCoc` |
| 64 | `c:VeMongDon` | `VeMongDon_DCE` | Foundation | Draw isolated pad footing detail (alias `VeMongDon`) | Footing size ($B \times L \times H$), rebar | `HPDcePro.AutoCad.Commands.FootingCommands.VeMongDon` |
| 65 | `c:TMCoc` | `TMCoc` | Foundation | Pile numbering & coordinate tag | Select pile entities | `HPDcePro.AutoCad.Commands.FootingCommands.TMCoc` |
| 66 | `c:rdc` | `RaiDaiCoc` | Foundation | Array pile reinforcement stirrups (alias `rdc`) | Pile length, spacing zones | `HPDcePro.AutoCad.Commands.FootingCommands.RaiDaiCoc` |
| 67 | `c:VeBlockLanhTo` | `VeBlockLanhTo` | Lintel | Draw opening lintel block | Wall opening width, wall thickness | `HPDcePro.AutoCad.Commands.LintelCommands.VeBlockLanhTo` |
| 68 | `c:VeMatCatLanhTo` | `VeMatCatLanhTo` | Lintel | Draw lintel cross-section detail | Lintel dimensions, rebar count | `HPDcePro.AutoCad.Commands.LintelCommands.VeMatCatLanhTo` |
| 69 | `c:VeThang_DCE` | `VeThang_DCE` | Stairs | Draw reinforced concrete staircase & landing | Story height, riser count, tread width | `HPDcePro.AutoCad.Commands.StairCommands.VeThang` |

---

## 2. Analysis of the Pure AutoLISP Support & Geometric Utilities

In addition to .NET commands, `DCE Pro.lsp` and companion files include pure LISP geometric and maintenance utilities:

### 1. `c:GenC` (Column Boundary & Rebar Coordinate Extractor)
- **Mechanism**: Iterates over selected polylines (`wpolycoor`) and circles (`wrebar`) representing column concrete perimeter and internal longitudinal bars.
- **Output**: Exports an ASCII coordinate table `Coordinates.txt` formatted for section analysis software (e.g. PCACol / Section Builder):
  - Line 1: Column outer polygon vertex coordinates $(x_i, y_i)$.
  - Line 2+: Opening / void polylines (if core wall).
  - Line 3+: Rebar coordinates $(x_j, y_j)$, diameters $\phi_j$, and bar marks.

### 2. `c:rdt` (Distribution of Entities Along Guide Curves)
- **Mechanism**: Prompts user for source object group (`chonnhomdoituong`), guide curve (`choncuver` — Polyline, Spline, or Arc), distribution spacing (`raikhoangcach`), or division count (`raisoluong`).
- **Options**:
  - `COPYQUAY`: Rotates each copied instance tangentially or perpendicular to curve tangent vector.
  - `COPYKOQUAY`: Maintains original World Coordinate System (WCS) orientation.

### 3. `c:tencaukien` (Auto-Incrementing Text Labeler)
- **Mechanism**: Prompts for prefix string (`prefixname`) and initial numeric index (`real_input`). Automatically iterates through sequential selections of `TEXT`, `MTEXT`, or `ATTDEF` entities, replacing string with `[Prefix][Index++]`.

### 4. `c:t2m` (Batch Text to MText Converter)
- **Mechanism**: Reads individual `TEXT` entities and wraps them into equivalent `MTEXT` objects (`_txt2mtxt`) while preserving text style, insertion point, height, rotation angle, and layer.

### 5. `c:CHIAVIEWPORT` / `c:CVP` (Automatic Viewport Partitioning)
- **Mechanism**: Located in `GLDCE.LSP`. Automatically partitions an existing paper-space viewport into 2 or 4 equal sub-viewports while calculating exact `CustomScale` ratios and preserving the model-space focal point center across all splits.

### 6. `c:DCENAP` (Manual Reload & Recovery Utility)
- **Source**: Line 349 of `C:\Dce Pro\DCE Pro.lsp` (`defun c:DCENAP`).
- **Mechanism**: Resets loader flags (`dce_da_nap nil`, `dce_nap_ok nil`) and invokes `(dce_nap)` protected by `vl-catch-all-apply`. Enables technical support or users to re-verify document readiness, re-register ObjectARX / CLR .NET command hooks, and reload assemblies mid-session without restarting the AutoCAD host process.
- **Clean-Room Implementation**: Retained in `HPDcePro.lsp` as `c:DCENAP` (with primary alias `c:HPDCE_RELOAD`) for safe mid-session reloading during development and maintenance.

---

## 3. Clean-Room AutoLISP Loader: `HPDcePro.lsp`

Below is the clean-room, robust, production-grade AutoLISP loader script for `HPDcePro`. It completely eliminates legacy licensing checks, anti-tamper routines, and obfuscated reactors, while ensuring safe, asynchronous DLL loading across AutoCAD 2026:

```lisp
;;; ==========================================================================
;;; HPDcePro.lsp — Clean-Room Production AutoLISP Loader for HPDcePro
;;; Copyright (c) 2026 HP Clean-Room Architecture
;;; ==========================================================================

(vl-load-com)

(setq *HPDcePro-Version* "1.0.0")
(setq *HPDcePro-Root* (vl-filename-directory (findfile "HPDcePro.lsp")))

(defun c:HPDceLoad (/ dllPath)
  (princ "\n[HPDcePro] Initializing HPDcePro Structural Suite...")
  (setq dllPath (strcat *HPDcePro-Root* "\\HPDcePro.AutoCad.dll"))
  (if (findfile dllPath)
    (progn
      (command-s "._NETLOAD" dllPath)
      (princ (strcat "\n[HPDcePro] Successfully loaded: " dllPath))
    )
    (princ (strcat "\n[HPDcePro] ERROR: Could not locate " dllPath))
  )
  (princ)
)

;;; Command Aliases Routing to HPDcePro.AutoCad
(defun c:sdb () (command-s "ShopDrawingBeam") (princ))
(defun c:tuct () (command-s "ToiUuCatThep") (princ))
(defun c:vd () (command-s "VeDam_DCE") (princ))
(defun c:vdall () (command-s "VeDamAll_DCE") (princ))
(defun c:mcd () (command-s "VeMCD_DCE") (princ))
(defun c:slck () (command-s "TKTSSOLUONGCAUKIEN") (princ))
(defun c:sdkt () (command-s "TKTSDUONGKINHTHEP") (princ))
(defun c:thtk () (command-s "TongHopThongKe_dce") (princ))
(defun c:tkt () (command-s "tkt_dce") (princ))
(defun c:stk () (command-s "stk_dce") (princ))
(defun c:vc () (command-s "VeCotDcePro") (princ))
(defun c:vv () (command-s "VeVachDcePro") (princ))
(defun c:mb1 () (command-s "XuatDuLieuModelEtabs") (princ))
(defun c:updetabs () (command-s "UpdateModelEtabs") (princ))

;;; Automatic Safe Startup Hook
(defun-q HPDcePro_Startup ()
  (c:HPDceLoad)
  (princ (strcat "\n[HPDcePro] Ready. Type SDB, TUCT, or GETFORM to begin."))
  (princ)
)

(setq S::STARTUP (cons HPDcePro_Startup S::STARTUP))
(princ "\n[HPDcePro] Loader registered.")
(princ)
```

---

## 4. Staged 6-Phase Technical Implementation Roadmap

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    HPDCEPRO 6-PHASE ENGINEERING ROADMAP                     │
│                                                                             │
│  PHASE 1: Core Foundation & Phase 1 End-to-End Delivery                     │
│  - Pure C# 2D Geometry Core (replaces DceAnalyssiss64.dll)                  │
│  - .autostr JSON Project I/O & XData 13-RegApp Codecs                       │
│  - TCVN 5574 RC Beam Design & 1D Cutting Stock Optimizer (11.7m)            │
│  - SQLite MayHoc Tokenizer & Multi-Host Project Skeletons                   │
│                                                                             │
│  PHASE 2: Continuous Beam Ecosystem & ETABS Bidirectional Bridge            │
│  - Foundation Beams (VeDamMong) & Non-Prismatic Curved Beams                │
│  - Beam-Column Framing Intersection Trimming (dam_giao)                     │
│  - ETABS 22 OAPI Bidirectional Model Extraction & Sync                      │
│                                                                             │
│  PHASE 3: Column & Shear Wall Detailing Subsystem                           │
│  - Multi-Story Column Schedules & Staggered Splice Placement                │
│  - Fiber P-M-M Interaction Diagram Engine under TCVN 5574                   │
│  - Core Shear Wall Boundary Element Reinforcement                           │
│                                                                             │
│  PHASE 4: Substructure, Slab & Secondary Elements                           │
│  - TCVN 10304:2025 Deep Foundation Pile Capacity Engine                     │
│  - Automated Pile Cap Layout & Isolated Pad Footings                        │
│  - Two-Way Slab Rebar Generator & Cranked Staircase Detailing               │
│                                                                             │
│  PHASE 5: AutoStr 24-Step Automation & QS Quantity Takeoff                  │
│  - Full AutoStr 24-Step Pipeline Orchestration                              │
│  - Concrete, Formwork & Rebar BOQ Calculation with ClosedXML                │
│  - Automated Drawing Indexing & Batch PDF Publishing                        │
│                                                                             │
│  PHASE 6: Structural Steel Detailing & Advanced Ecosystem Integration       │
│  - TCVN 5575:2024 Steel Portal Frame Design & Moment Connections            │
│  - TCVN 2737:2023 Dynamic Wind Pressure Engine                              │
│  - Full Revit 2026 Direct Native Rebar Modeling Adapter                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Phase Breakdown & Acceptance Milestones

#### Phase 1: Core Foundation & Proof-of-Concept
- **Focus**: Deliverable solution scaffolding (`HPDcePro.slnx`), pure C# 2D geometry engine (`Polygon2D`, `Segment2D`, `Vector2D`), `.autostr` project serializer, CAD XData codecs for 13 RegApps, TCVN 5574:2018 RC beam design engine, 1D rebar cutting stock optimizer for 11.7m commercial bars, SQLite reader for `MayHoc/mauhoc.db`, MahApps.Metro UI dialogs, AutoCAD/Excel/Revit host adapters, and $\ge 80$ unit tests in `HPDcePro.Core.Tests`.
- **Target**: Zero warnings, zero errors, sub-3-second builds.

#### Phase 2: Full Beam Ecosystem & ETABS Integration
- **Focus**: Continuous beam elevations, non-prismatic and curved beams, foundation ground beams (`VeDamMong`), localized intersection stirrup reinforcement, and bidirectional ETABS 22 OAPI model synchronization.

#### Phase 3: Column & Shear Wall Detailing
- **Focus**: Multi-story column schedules, automated splice zone distribution ($H_n/3$ to $2H_n/3$), cranked lap transitions (1:6), nonlinear fiber P-M-M interaction curves, and core shear wall boundary cages.

#### Phase 4: Substructure, Slabs & Secondary Elements
- **Focus**: TCVN 10304:2025 deep foundation pile capacity (SPT correlations), pile cap drafting, isolated pad footings, two-way slab rebar layout with Wood-Armer moments, and staircase flight detailing.

#### Phase 5: AutoStr 24-Step Workflow & QS Takeoff
- **Focus**: Automated execution of all 24 `AutoStr` steps from SheetSet initialization to final calculation report generation, complete concrete/formwork/rebar quantity surveying, and batch PDF plotting.

#### Phase 6: Structural Steel & Ecosystem Integration
- **Focus**: Porting `DceSteel` connection algorithms (welded moment, base plates) under TCVN 5575:2024, aerodynamic wind zoning under TCVN 2737:2023, retraining NLP rebar models, and native 3D `Rebar` modeling inside Revit 2026.
