# Task Dispatch — explorer_m3_stair_lintel

## 2026-10-17T10:10:00Z

- **Agent Name**: `explorer_m3_stair_lintel`
- **Role**: Stair & Lintel Domain Explorer
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_stair_lintel\`
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
Mine exact specifications and design the architecture for the Stair & Lintel detailing subsystem (Phase 4):
1. **Staircase Models & Detailing**:
   - `StairFlightModel`: Flight geometry (riser count $n_r$, riser height $h_r$, tread width $b_t$, waist/throat thickness $t_w$, landing length $L_{land}$, landing thickness $t_{land}$, flight inclination $\tan \alpha = h_r / b_t$).
   - Reinforcement layout: Bottom continuous tension bars along inclined flight running into landing, top negative bars at landing re-entrant corners, cranked/scissor bars at junction (preventing tension breakout at re-entrant angles), step transverse distribution bars (1 bar per step).
   - Command `ve_thang` (`vethang`).
2. **Lintel Models & Detailing**:
   - `LintelModel`: Wall opening clear span $L_0$, bearing length $L_{bear} \ge 200\text{ mm}$, cross-section $b \times h$, top bars, bottom bars, closed stirrups $\le 150\text{ mm}$.
   - Command `velanhto`.
3. **Generic Cross-Section Detailing**:
   - Generic section cut engine: `vemc` (section cuts across beams, columns, walls, stairs with callout tags, dimensions, rebar callouts).
4. **AutoCAD Commands & Aliases**:
   - `ve_thang` (`vethang`), `velanhto`, `vemc`.
   - Dynamic block placement, DWG XRecord serialization (`KATA_STAIR_DATA`, `KATA_LINTEL_DATA`).
5. **Revit Stair & Lintel Services**:
   - Rebar generation in `OST_Stairs` elements (`StairRebarGenerator.cs`), landing & flight mapping.
6. **Excel Scheduling**:
   - `StairScheduleAggregator.cs` and ClosedXML export for sheet `Thang`.
7. **Clean Code & Test Architecture**:
   - Partition classes to guarantee Rule C2 ($\le 300$ lines/file) and Rule M3 ($\le 50$ lines/method).
   - Zero host dependencies in `HPKata.Core`.
   - Detailed test plan.

### Deliverables:
Write `stair_lintel_spec_report.md` and `handoff.md` in your working directory. Notify parent when done.
