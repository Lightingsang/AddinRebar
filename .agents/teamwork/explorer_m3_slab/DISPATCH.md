# Task Dispatch — explorer_m3_slab

## 2026-10-17T10:10:00Z

- **Agent Name**: `explorer_m3_slab`
- **Role**: Slab Domain & Reinforcement Spec Miner
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_slab\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### Mandatory First Steps:
1. Read the authoritative original request at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Read the master project scope:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Inspect existing Phase 1, Phase 2, and Phase 3 implementations in `HPKata`:
   - `HPKata/HPKata.Core/Beams/`, `Columns/`, `Walls/`
   - `HPKata/HPKata.AutoCad/Controllers/`
   - `HPKata/HPKata.Revit/Services/`
4. Inspect reference sources:
   - `C:\kata_pro\Kata_pro64_Cad2025.dll`
   - `C:\kata_pro\Goi lenh.lsp`
   - `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`

### Your Mission:
Mine exact specifications and design the architecture for the Slab reinforcement subsystem (Phase 4):
1. **Slab Models & Geometry**:
   - `SlabPanelModel`: Rectangular and polygonal slab boundaries, slab thickness $h_s$, bottom continuous mesh (layers X & Y, spacing $s_x, s_y$, bar diameters), top negative moment reinforcement (support zones $L/4$, $L/3$, cantilevers), corner crack-control mesh.
   - Variable-length polyline slabs (`vesanpl`): polygon boundary segmentation, rebar fan/radial distribution or parallel stepping.
   - 4-way/radial slab reinforcement (`vesan4d`): orthogonal + 45-degree diagonal top/bottom bars for high punch-through or complex geometry.
   - Slab chair/spacer bar calculator: `thep_ke` / `cho_san` (chairs per $m^2$, stool height $h_{stool} = h_s - 2c - d_{top} - d_{bot}$, leg lengths, shape code "dang02" or "dang05").
   - Slab panel splitting: `pick_diem_san` (`dsan`), `chia_san` (`chiasan`), `ssan`.
2. **AutoCAD Slab Commands & Aliases**:
   - `ve_san` (`vesan`), `vesanpl`, `vesan4d`, `ssan`, `pick_diem_san` (`dsan`), `chia_san` (`chiasan`), `thong_ke_block_san` (`tksan`).
   - Dynamic block placement, polyline boundary tracing, DWG XRecord serialization (`KATA_SLAB_DATA`).
3. **Revit Slab Services**:
   - Native rebar / Area Reinforcement in `OST_Floors` elements (`SlabRebarGenerator.cs`), top/bottom layers, idempotent identification.
4. **Excel Scheduling**:
   - `SlabScheduleAggregator.cs` and ClosedXML export for sheet `San`.
5. **Clean Code & Test Architecture**:
   - Partition classes to guarantee Rule C2 ($\le 300$ lines/file) and Rule M3 ($\le 50$ lines/method).
   - Zero host dependencies in `HPKata.Core`.
   - Detailed test plan.

### Deliverables:
Write `slab_spec_report.md` and `handoff.md` in your working directory. Notify parent when done.
