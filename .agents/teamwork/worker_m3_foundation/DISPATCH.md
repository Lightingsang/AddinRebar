## 2026-10-07T03:53:02Z
You are worker_m3_foundation, the implementation specialist for Milestone M3 (Phần Móng: Cọc, Đài móng, Giằng móng).

Your working directory is:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\

First, READ the authoritative user request at:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-07T02:05:11Z, Requirement R3, and ĐIỀU 0: Nguyên Tắc Vàng.

Also READ the normative standard:
C:\Users\STR-HP03\.gemini\antigravity\brain\087c0df3-fff1-40c1-83ce-c0d4c6cb6acd\quy_che_phoi_hop_cad_revit_chuan_ky_su_ket_cau.md

DATA SOURCES AVAILABLE:
- Footings dataset (72 footings Nhóm 1):
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json
- Tie Beams dataset (22 GM beams Nhóm 1):
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_22_grade_beams.json
- Beam geometry / spans in scratch/:
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\cad_grade_beam_lines.json
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\gm_beams_plan_summary.json
- Piles model:
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json
- Revit Named Pipe IPC script reference:
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\survey_revit_live.py (Named Pipe: \\.\pipe\hprebar-mcp-r2026)

YOUR TASKS (Execute in strict sequential order):

### Task 1: Clean Revit Model (Purge Ghost Beams & Defensively Pin)
1. Defensively pin all 27 Grids and 10 Levels in Revit (`grid.Pinned = true`, `level.Pinned = true`).
2. Delete 100% of the 211 ghost framing elements (`OST_StructuralFraming`) currently in Revit.
3. Purge any legacy 57 clustered floor slabs that were generated without authentic CAD boundaries.
4. Verify by querying Revit:
   - `OST_StructuralFraming` count == 0
   - `Grid` count == 27 (all Pinned == true)
   - `Level` count == 10 (all Pinned == true)

### Task 2: Adjust 196 Piles D400 Embedment
1. Query the 196 piles (`OST_StructuralFoundation`, `1_STR_Coc_Vuong: P_300x300` or `4_STR_Coc_Tron: P400`). If D400 symbol `4_STR_Coc_Tron: P400` is present, ensure correct symbol assignment or confirm parameters.
2. Cross-reference with `verified_72_footings.json`:
   - 118 (or 122) piles under ĐC-1..3': pile top COTE is -2.500m (100mm embedment into bottom -2.600m).
   - 36 (or 50) piles under ĐC-4..5: pile top COTE is -2.700m (100mm embedment into bottom -2.800m).
   - 42 (or 24) piles under ĐC-6: pile top COTE is -2.800m (100mm embedment into bottom -2.900m).
3. Update pile top elevations in Revit. Verify all 196 piles have pile head center error <= 2.0mm and top elevations matching the exact bottom - 100mm embedment.

### Task 3: Model 72 Footings & 22 Grade Beams with 2-Phase Verification
1. Model 72 footings in Revit:
   - Top COTE: -1.600m
   - Thicknesses: 1000mm (ĐC-1..3'), 1200mm (ĐC-4..5), 1300mm (ĐC-6).
   - Geometry: Use vertices from `verified_72_footings.json` (converted from mm to ft: 1 ft = 304.8 mm).
   - Family / Type: Foundation Slabs or Structural Foundation (`HP-ES-CAP-CON-B20-1000`, `1200`, `1300` or equivalent).
2. Model 22 grade beams GM-1..22 in Revit:
   - Top COTE: -1.600m
   - Cross-sections: 300x400mm, 300x600mm, 200x300mm per `verified_22_grade_beams.json`.
   - Structural Framing lines from verified CAD spans.
3. 2-Phase Self-Verification:
   - Query back each created footing and beam element via Revit API.
   - Assert: delta b = 0.0mm, delta h = 0.0mm, delta Z <= 1.0mm.
   - If any element exceeds tolerance, rollback or delete.
4. Record all created elements into `state/bim_coordination_ledger.json` with lifecycle step "Dựng Revit" -> "Kiểm tra lại".

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

OUTPUT:
Write `handoff.md` and `progress.md` in your directory detailing:
- Exact counts of purged beams, pinned grids/levels, adjusted piles, created footings, and created grade beams.
- Verbatim verification outputs from Revit.
Send a message back to parent when complete.
