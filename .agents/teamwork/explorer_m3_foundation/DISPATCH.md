# Task Dispatch — explorer_m3_foundation

## 2026-10-17T10:10:00Z

- **Agent Name**: `explorer_m3_foundation`
- **Role**: Foundation Domain & Architecture Spec Miner
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_foundation\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### Mandatory First Steps:
1. Read the authoritative original request at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Read the master project scope:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Inspect existing Phase 1, Phase 2, and Phase 3 implementations in `HPKata`:
   - `HPKata/HPKata.Core/Beams/` & `HPKata/HPKata.Core/Columns/` & `HPKata/HPKata.Core/Walls/`
   - `HPKata/HPKata.AutoCad/Controllers/`
   - `HPKata/HPKata.Revit/Services/`
4. Inspect reference sources:
   - `C:\kata_pro\Kata_pro64_Cad2025.dll` (use `ilspycmd` if needed)
   - `C:\kata_pro\Goi lenh.lsp`
   - `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`

### Your Mission:
Mine exact specifications and design the architecture for the Foundation detailing subsystem (Phase 4):
1. **Foundation Models & Geometry**:
   - `IsolatedFootingModel`: Pad footing dimensions ($L \times B \times H$, stepped or sloped pedestal $l_c \times b_c \times h_c$), bottom rebar mesh ($X$ and $Y$ directions, spacing, hooks), column starter dowels.
   - `PileCapModel`: Pile cap geometry ($L \times B \times H$, stepped pile cap `ve_mong_coc_giat_cap`), pile configuration (1 to 9+ piles, pile diameter $D$, embedment depth $h_{emb} \ge 100\text{ mm}$ into cap), bottom rebar mesh, top reinforcement, side skin bars, confinement ties around piles, column starter dowel placement.
   - Foundation layout: `ve_mat_bang_mong` (`vmbm`), `rai_mong` (`rm`).
2. **AutoCAD Foundation Commands & Aliases**:
   - `ve_mat_bang_mong` (`vmbm`), `rai_mong` (`rm`), `ve_mong_don` (`vmd`), `ve_mong_coc` (`vmc`), `ve_mong_coc_giat_cap`.
   - Dynamic table generation, DWG XRecord serialization (`KATA_FOUNDATION_DATA`), block attributes, XData.
3. **Revit Foundation Services**:
   - Native rebar generation in `OST_StructuralFoundation` elements (`FoundationRebarGenerator.cs`), host mapping, cover resolution, idempotent tagging.
4. **Excel Scheduling**:
   - `FoundationScheduleAggregator.cs` and ClosedXML export for sheet `Mong`.
5. **Clean Code & Test Architecture**:
   - Partition classes to guarantee Rule C2 ($\le 300$ lines/file) and Rule M3 ($\le 50$ lines/method).
   - Zero host dependencies in `HPKata.Core`.
   - Detailed test plan.

### Deliverables:
Write `foundation_spec_report.md` and `handoff.md` in your working directory. Notify parent when done.
