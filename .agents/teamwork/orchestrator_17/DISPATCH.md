# ORCHESTRATOR DISPATCH SPECIFICATION — HPKATA PHASES 2, 3, 4 & 5

## Mission
Continue the clean-room reconstruction of **KataPro (`C:\kata_pro`)** inside **`HPKata/HPKata.slnx`** by implementing **Phases 2, 3, 4, and 5** across **`HPKata.Core`**, **`HPKata.UI`**, **`HPKata.AutoCad`** (AutoCAD 2026 `.NET 8`), and **`HPKata.Revit`** (Revit 2026 `.NET 8`), building upon the completed **Phase 1** baseline (179 source files, 437 passing tests) and the reverse-engineering blueprint at `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`.

## Context & Authoritative Request
- **Authoritative User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (section `## 2026-10-17T06:46:41Z`)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\`
- **Project Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\`
- **Target Solution**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- **Reverse-Engineering Blueprint**: `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`

## Reference Material & Target Artifacts (STRICTLY READ-ONLY)
- `C:\kata_pro\Kata_pro64_Cad2025.dll` (Decompile specific Beam, Column, Wall, Foundation, Slab, Stair, and QS classes via `%USERPROFILE%\.dotnet\tools\ilspycmd.exe` when exact calculation or geometry layout details are needed)
- `C:\kata_pro\Kata_Class_Lib.dll` & `C:\kata_pro\Kata_Class_Lib_Revit.dll`
- `C:\kata_pro\Goi lenh.lsp`, `C:\kata_pro\Iso_kata.dwg`, `C:\kata_pro\Kata.xlsm`, `C:\kata_pro\Iso_QHPlus.xls`
- `.agents/teamwork/survey_agent_1/survey_commands_config_report.md`
- `.agents/teamwork/survey_agent_2/survey_dwg_shapes_report.md`
- `.agents/teamwork/survey_agent_3/survey_scheduling_interop_report.md`

## Mandated Requirements (R1 – R5)

### R1. Phase 2 — Beam Shop Drawing & ETABS Integration (`Phân hệ Dầm & ETABS`)
1. **`HPKata.Core` (Beam Domain & ETABS Interop Engine):**
   - Multi-span continuous beam structural model (`BeamSpanModel`, `BeamSectionModel`, `BeamRebarLayout`, support/midspan top & bottom reinforcement zones, curtailment rules, stirrup spacing zones $L/4$, $L/2$, hanger bars, side face / skin reinforcement `thep_gia`, and main-secondary beam intersection `dam_giao` / `giao_dam` / `cat_dam`).
   - Foundation beam / tie beam detailing engine (`ve_dam_mong`, `ve_giang_mong`, `ve_giang_shop`), inclined beam (`ve_dam_xien`), and curved beam (`ve_dam_cong`) geometry & bar length calculations.
   - ETABS / SAP2000 import & design-to-detailing adapter (`EtabsBeamImportService`, parsing ETABS frame geometry, section dimensions, and longitudinal/shear reinforcement area `As_top`, `As_bot`, `Av/s` to auto-select rebar diameters and counts).
   - Beam rebar persistence (`Save_thep_dam`, `Add_thep_dam`) and beam schedule aggregation (`tkdam`, `quet_dam`).
2. **`HPKata.UI` (Beam & ETABS WPF ViewModels & Views):**
   - `BeamDetailingViewModel` / `BeamDetailingWindow` (Continuous beam spans, support/midspan bar configurations, section cuts, stirrup zones, shop mode vs design mode).
   - `EtabsImportViewModel` / `EtabsImportWindow` (Story/beam selection, rebar auto-sizing rules, preview table).
3. **`HPKata.AutoCad` & `HPKata.Revit` (Beam Commands & Lisp Aliases):**
   - AutoCAD commands & `GoiLenh_HPKata.lsp` aliases: `ve_dam` (`vd`), `ve_dam_all` (`vda`), `ve_dam_shop` (`vds`), `ve_dam_all_shop` (`vdas`), `ve_dam_mc` (`vdmc`), `ve_damMC_all` (`vdmca`), `ve_dam_mong` (`vdm`), `ve_giang_mong` (`vgm`), `ve_giang_shop` (`vgms`), `ve_dam_xien` (`vdx`), `ve_dam_cong` (`vdc`), `dam_giao` (`damgiao`), `giao_dam` (`giaodam`), `cat_dam` (`catdam`), `raidam`, `tkdam`, `quet_dam` (`qd`), `quet_dam_all` (`qda`), `Save_thep_dam` (`std`), `Save_thep_dam_all` (`stda`), `Add_thep_dam` (`atd`), `show_thep_dam` (`shtd`), `Etabs`, `loadbeam`.
   - Revit commands/services: Read/generate multi-span `Structural Framing` (`OST_StructuralFraming`) beam reinforcement, ETABS beam rebar import, and beam shop schedule generation.

### R2. Phase 3 — Column & Shear Wall Shop Drawing (`Phân hệ Cột & Vách`)
1. **`HPKata.Core` (Column & Shear Wall Domain Engine):**
   - Multi-story column elevation & cross-section detailing engine (`ColumnStorySegment`, `ColumnSectionModel`, longitudinal bar splice zones, crank/offset bends at story transitions and column size step-downs, tie/crosstie arrangements, seismic confinement zones $l_0$).
   - Shear wall & core wall detailing engine (`ShearWallModel`, `WallBoundaryElementModel`, boundary zone confinement ties, double-curtain horizontal/vertical web reinforcement, wall openings, and U-bar edge hooks).
   - ETABS column & pier/wall import service (`loadcolumn`, `loadwall`).
2. **`HPKata.UI` (Column & Wall WPF ViewModels & Views):**
   - `ColumnDetailingViewModel` / `ColumnDetailingWindow` (Multi-story column geometry, longitudinal bars, tie patterns, splice/coupler options).
   - `WallDetailingViewModel` / `WallDetailingWindow` (Shear wall elevation, boundary elements `ve_loi_vach`, web mesh, shop curtailment).
3. **`HPKata.AutoCad` & `HPKata.Revit` (Column & Wall Commands):**
   - AutoCAD commands & `GoiLenh_HPKata.lsp` aliases: `ve_cot` (`vc`), `ve_cot_shop` (`vcs`), `raicot`, `dimcot`, `tkcot`, `loadcolumn`, `ve_vach` (`vv`), `ve_vach_shop` (`vvs`), `ve_loi_vach` (`vlv`), `loadwall`.
   - Revit commands/services: `Structural Columns` (`OST_StructuralColumns`) and `Walls` (`OST_Walls`) rebar generation, boundary element confinement ties, and column/wall rebar scheduling.

### R3. Phase 4 — Foundation, Slab, Stair & Lintel Detailing (`Phân hệ Móng, Sàn, Thang, Lanh tô`)
1. **`HPKata.Core` (Foundation, Slab, Stair & Lintel Domain Engine):**
   - Isolated pad footing (`IsolatedFootingModel`, `ve_mong_don`), pile cap (`PileCapModel`, `ve_mong_coc`, stepped pile cap `ve_mong_coc_giat_cap`), and foundation plan layout (`ve_mat_bang_mong`, `rai_mong`).
   - Two-way slab reinforcement engine (`SlabPanelModel`, bottom/top mesh distribution, variable-length polyline slab panels `vesanpl`, 4-way/radial slab rebar `vesan4d`, chair/spacer bar calculation `thep_ke` / `cho_san`, slab panel splitting `chia_san`, and slab rebar block takeoff `thong_ke_block_san`).
   - Reinforced concrete staircase flight & landing detailing (`StairFlightModel`, `ve_thang`), lintel detailing (`LintelModel`, `velanhto`), and generic cross-section detailing (`vemc`).
2. **`HPKata.UI` (Foundation, Slab & Stair WPF ViewModels & Views):**
   - `FoundationDetailingViewModel` / `FoundationDetailingWindow` (Pad footings, pile caps, stepped pile caps).
   - `SlabStairDetailingViewModel` / `SlabStairDetailingWindow` (Slab top/bottom mesh, cranked slab bars, stair flight parameters, lintels).
3. **`HPKata.AutoCad` & `HPKata.Revit` (Foundation, Slab, Stair & Lintel Commands):**
   - AutoCAD commands & `GoiLenh_HPKata.lsp` aliases: `ve_mat_bang_mong` (`vmbm`), `rai_mong` (`rm`), `ve_mong_don` (`vmd`), `ve_mong_coc` (`vmc`), `ve_mong_coc_giat_cap`, `ve_san` (`vesan`), `vesanpl`, `vesan4d`, `ssan`, `pick_diem_san` (`dsan`), `chia_san` (`chiasan`), `thong_ke_block_san` (`tksan`), `ve_thang` (`vethang`), `velanhto`, `vemc`.
   - Revit commands/services: `Structural Foundations` (`OST_StructuralFoundation`), `Floors` (`OST_Floors`), and `Stairs` (`OST_Stairs`) rebar detailing & area reinforcement integration.

### R4. Phase 5 — Quantity Surveying (QS), Drawing Layout & Utility Commands (`Phân hệ Khối lượng QS & Tiện ích Bản vẽ`)
1. **`HPKata.Core` (QS Quantity Takeoff & Drawing Layout Engine):**
   - Concrete, formwork, and finish quantity takeoff engine (`QsTakeoffEngine`, `QsElementRecord`, structural framing plan takeoff `QS_mbkc`, column/pier vertical takeoff `QS_tru`, net vs gross deduction rules at beam-column-slab intersections, brick/tile grid layout `chia_tam` / `op_gach`, and Excel QS workbook export `xuatQS` / `tkqs`).
   - Drawing layout & sheet organization algorithms (`GridGenerator`, `DrawingSheetArranger` for `sap_ban_ve`, viewport layout `taomv` / `taomv1`, drawing index generator `danhmuc`, dimension alignment `loc_dim`, splice zone indicator `phan_vung_noi`, component name finder/renamer `tim_ten_ck` / `dten` / `tkten`).
2. **`HPKata.UI` (QS & Drawing Management WPF ViewModels & Views):**
   - `QsTakeoffViewModel` / `QsTakeoffWindow` (Concrete/formwork takeoff rules, intersection deduction toggles, summary preview, Excel export).
   - `DrawingUtilityViewModel` / `DrawingUtilityWindow` (Grid generator, sheet arrangement, batch print/PDF export `in_ban_ve` / `pdfkata`).
3. **`HPKata.AutoCad` & `HPKata.Revit` (QS & Utility Commands):**
   - AutoCAD commands & `GoiLenh_HPKata.lsp` aliases: `QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `tkqs`, `ve_grid` (`vg`), `chia_tam` (`chiatam`), `op_gach` (`opgach`), `loc_dim` (`locdim`), `phan_vung_noi` (`pvn`), `danhmuc`, `cat_doituong` (`cat`), `taomv`, `taomv1`, `sap_ban_ve` (`sbv`), `in_ban_ve` (`ibv`), `pukata`, `pdfkata`, `hideatt`, `Unhideatt`, `tim_ten_ck`, `dten`, `tkten`, `textblock`, `chuyen`, `show_thep_rai` (`str`).
   - Revit commands/services: Concrete & formwork quantity takeoff from Revit 3D elements (`OST_StructuralFraming`, `OST_StructuralColumns`, `OST_StructuralFoundation`, `OST_Floors`, `OST_Walls`) exported to Kata QS Excel format, plus sheet/viewport index helpers.

### R5. Clean-Room, Governance & Quality Constraints
1. Strictly read-only access to `C:\kata_pro`; never touch `Security.dll` or licensing/DRM code.
2. Every new or modified C# file in `HPKata/` must obey `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/CODE_REVIEW_CHECKLIST.md`:
   - Max 300 lines per file (Rule C2), max 50 lines per method (Rule M3).
   - Zero dead/commented-out code (Rule Q-B1), zero empty catch blocks without reason comments (Rule Q-B2).
   - In `HPKata.Revit`, `304.8` must appear only inside `RevitUnitConverter.cs` (Rule R5).
3. All 437 existing Phase 1 tests must remain 100% green alongside the new Phase 2–5 unit, component, host, and E2E tests.

## CRITICAL BUILD & CONCURRENCY CONSTRAINTS
- **NEVER remove or weaken** the concurrency/MTP isolation properties in `HPKata/Directory.Build.props`.
- Always pass `--disable-build-servers -m:1` to all `dotnet build` and `dotnet test` commands.
- Never run concurrent `dotnet build` or `dotnet test` across parallel gate subagents.
- If parallel verification is run, pass unique `--results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.

## Acceptance Criteria
- [ ] AC1: Solution builds with 0 warnings and 0 errors via `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1`. HPKata.Core has 0 CAD/Revit refs. All files <= 300 lines, 0 dead code, 0 unannotated catch.
- [ ] AC2: Phase 2 (Beam & ETABS) complete with all 25 AutoCAD commands, Lisp aliases, Revit commands, and Core/UI models.
- [ ] AC3: Phase 3 (Column & Shear Wall) complete with all 10 AutoCAD commands, Lisp aliases, Revit commands, and Core/UI models.
- [ ] AC4: Phase 4 (Foundation, Slab, Stair & Lintel) complete with all 15 AutoCAD commands, Lisp aliases, Revit commands, and Core/UI models.
- [ ] AC5: Phase 5 (QS & Drawing Utilities) complete with all 27 AutoCAD commands, Lisp aliases, Revit commands, and Core/UI models.
- [ ] AC6: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1` passes 100% (expanding from 437 to >= 650 tests) with zero FailFast or collision popups.


## 2026-10-17T06:49:21Z

You are orchestrator_17, the Project Orchestrator for HPKata Phases 2, 3, 4, and 5.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\

Please read your dispatch specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\DISPATCH.md
and the authoritative original user request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-17T06:46:41Z)

Your mission:
Continue the clean-room reconstruction of KataPro (C:\kata_pro) inside HPKata/HPKata.slnx by implementing Phases 2, 3, 4, and 5 across HPKata.Core, HPKata.UI, HPKata.AutoCad (AutoCAD 2026 .NET 8), and HPKata.Revit (Revit 2026 .NET 8), building upon the completed Phase 1 baseline (179 source files, 437 passing tests) and the reverse-engineering blueprint at docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md.

Execute the work systematically across the milestones:
- Milestone M1: Phase 2 — Beam Shop Drawing & ETABS Integration (Core, UI, AutoCAD, Revit, Tests)
- Milestone M2: Phase 3 — Column & Shear Wall Shop Drawing (Core, UI, AutoCAD, Revit, Tests)
- Milestone M3: Phase 4 — Foundation, Slab, Stair & Lintel Detailing (Core, UI, AutoCAD, Revit, Tests)
- Milestone M4: Phase 5 — Quantity Surveying (QS), Drawing Layout & Utility Commands (Core, UI, AutoCAD, Revit, Tests)
- Milestone M5: Full Solution Integration, Clean Code Audit & E2E Verification

Critical Constraints:
- Strictly read-only access to C:\kata_pro; never touch Security.dll or licensing/DRM code.
- Enforce docs/clean-code/HP_CLEAN_CODE_CORE.md (<= 300 lines/file, <= 50 lines/method, 0 dead code, 0 unannotated catch).
- Build and test using `--disable-build-servers -m:1`. NEVER remove or weaken the concurrency/MTP isolation properties in HPKata/Directory.Build.props.
- Maintain progress.md and BRIEFING.md in your working directory continuously.
- Report completion back to Sentinel with your handoff report when all acceptance criteria are met.
