# Search-set rebuild — Validate / Dry Run / Apply / Verify (2026-10-10)

Model: `HPNavis/output/live-verify/bim/THCSLT-HPC-ZZ-ZZ-CM-ZZ-0001-searchsets.nwd` = byte copy of the original (SHA256 `B27F32327A9DF97480B26FE1E2E9A00D67308432736E03F9527C7E170696D667`, original untouched). 19 appended files, 86 999 Revit elements. Plugin Debug 13:09. Registry: [hp-search-set-registry.json](hp-search-set-registry.json), table: [searchset-registry.md](searchset-registry.md), analysis: [searchset-analysis.md](searchset-analysis.md).

## Pipeline
| Step | Result |
|---|---|
| Analyze — inventory before | 0 folders / 0 sets in the model (the "old" sets exist only in the 6 XML) |
| Analyze — legacy XML on this model | 36 sets evaluated live (7.5 s): see "Legacy defects" |
| Build — registry v2 | 21 base + 11 detail + 5 auxiliary = 37 sets, each with its evidence |
| Validate (pre) — preview | 37 × create, 32 s |
| Dry Run | 37 created → rolled back; 0 sets after |
| Backup | original NWD untouched + SHA256 above; inventory-before = empty |
| Approval | copy only (creates, overwrites nothing). Production model = **pending user approval** |
| Apply | 37 created, 30.8 s, one Undo entry |
| Verify base | 21 sets, **0 errors**, 4 warnings (EMPTY A1/A3/M1/M3), saved = registry for 21/21, 22 s |
| Verify extras | 16 sets + 2 parents, **0 errors**, 0 warnings, 20 s |
| Re-run | 37 × unchanged, total still 37 sets (no duplicates) |
| Conflict guard | hand-edited A2 → `conflict`, left as edited; `allowUpdate` → `updated`, back to the registry (142) |

## Checks (error = 0 everywhere)
| Check | Result |
|---|---|
| WRONG_CATEGORY (element category not the set's) | 0 in all 21 base sets |
| WRONG_DISCIPLINE (source file not the set's discipline) | 0 |
| OVERLAP between base sets | 0 pairs |
| SAVED_DIFFERS / NOT_SAVED | 0 |
| DETAIL_OUTSIDE_PARENT | 0 |
| DETAIL_OVERLAP | 0 |
| NOT_COVERED_BY_DETAILS M4 by system | 12 (7 EF pipe accessories without classification, 5 fittings classified "Sanitary,Vent") — info |
| NOT_COVERED_BY_DETAILS M4 clash scope | 1 479 = pipes D < 32 mm — intended |
| Sums vs category counts | A7 979, M4 10 744, M4.9 9 265, M5 1 089, M7 1 677 — all equal the probe's per-category sums |
| API vs Navisworks | every count is Navisworks resolving the saved set (`GetSelectedItems`), compared with the registry search (`ValueEquals`) |

## Legacy defects proven on this model (fixed by the registry)
| Legacy set | Live | Problem → fix |
|---|---|---|
| STR-Walls | 0 | Structural parameter absent on every wall → all 121 structural walls lost; **S2 = 121** (ES file) |
| ARC-Walls | 8 623 | includes the 121 ES walls → **A8 = 8 502** |
| STR-Floors | 445 | 273 ARC + 4 EP floors inside → **S4 = 168** |
| ARC-Floors | 208 | 4 EP floors inside, 20 unflagged ARC floors lost → **A6 = 224** |
| STR-Columns / STR-Framings | 1 712 / 6 098 | 132 / 29 ARC elements inside → S3 1 580 / S5 6 069 (ARC ones in X.3) |
| ARC-CurtainWalls | 13 728 | 93 ES mullions inside → A4 13 635 (ES ones in X.5) |
| ARC-Stair&Railings | 370 | top rails, handrails, supports, ramps missing → **A7 = 979** |
| ELEC-ElectricalEquipments | 162 | 48 Mechanical Equipment (wall AC units) inside → **M6 = 48**, M5 without them |
| ELEC-LightingFixtures | 1 717 | 40 ARC fixtures inside → M7 1 677 (MEP files) |
| M1 / M3 / M6 | — | no legacy set → created (M1, M3 empty in THCSLT: no air terminals or ducts) |
| FIRE-PipeAccessories | 47 | project-specific name filter → System Classification (M4.1) |

## Elements in no base set (gaps, all reported, visible through X.* where relevant)
| Discipline / category | Count | Note |
|---|---:|---|
| STR Generic Models | 26 604 | family `STR_GenericModels_GachBong-1Vien` (breeze blocks) — not a matrix group |
| MEP Center Line / Center line | 10 544 | pipe centre lines, no geometry to clash |
| ARC Plumbing Fixtures / Sprinklers / Plumbing Equipment | 927 / 198 / 5 | modelled in the ARC file → X.2 |
| STR Plates / Bolts / Anchors | 660 / 240 / 30 | steel connection parts |
| ARC Generic Models | 638 | fences (hang-rao, tru-hang-rao), podiums (BỤC GIẢNG), downpipes (ONG THOAT NUOC) |
| MEP Plumbing Fixtures / Equipment | 381 / 7 | X.1 |
| ARC Rooms | 138 | no geometry |
| ARC Structural Columns / Connections / Framing | 132 / 118 / 29 | X.3 |
| STR Curtain Wall Mullions | 93 | X.5 |
| ARC Floors flagged structural | 49 | X.4 |
| ARC Lighting Fixtures | 40 | in no set (MEP-only M7) |
| ARC Wall Sweeps 23, Roofs 12, Roads 7, Room Separation 4, Planting 1, Specialty Equipment 1; MEP Floors 4 | — | not matrix groups |

## Decisions for the BIM lead (defaults applied)
1. Fire Alarm Devices in M5 (legacy put them beside fire pipes).
2. Sprinklers and Fire Protection in M4.
3. A6 excludes ARC floors flagged structural (49, legacy rule) — or treat them as S4?
4. Cross-discipline elements (X.2, X.3, X.5, ARC lighting fixtures, ARC downpipes) — move to the right model, or widen a base set to that file?
5. STR `GachBong-1Vien` (26 604) and ARC fences/podiums — need a matrix group?
6. Clash scope D ≥ 32 mm filters pipes only; fittings/accessories have no common size property.

## Review round (code-review-searchsets.md, 7/10) and final re-verify on a new clean copy (13:34)
| Finding | Fix | Live result |
|---|---|---|
| H1 clash tests bound by set name | selection identity = folder path + name (`SetPath` / `PathOf`); a test on a same-named set in another folder is re-pointed (Update) | clash plan LOD350 → 121 Create on `HP BIMCoordinator/{Architecture, Structure, MEP}/…` |
| H2 inventory over 64 KB | conditions opt-in (`withConditions`), 40 000-character budget, condition count always | all 47 folders/sets with conditions = 49 970 bytes, budget note in summary |
| M1 renamed/moved sets silently duplicated | orphan report (`SetUpsert.Orphans`) in every sync | hand-made `MEP/my conduits` reported, not removed |
| M2 allowUpdate for everything | allowUpdate requires explicit `codes` | refused without codes |
| M3 nested elements across sets | probe: 0 base-set elements under another base set's element | no change needed |
| M4 wildcard vs regex roles | regex case-insensitive + upper-cased; single-letter roles dropped (registry now AA / ES / EC, EE, EF, EP — the codes seen in THCSLT and THBB2) | counts identical, OR-groups 522 → 216, preview 32 s → 14.5 s |
| M5 registry errors at run time | load-time checks: bool/length values, detail ⊆ parent categories and discipline, base sets cannot override roles/folder | unit-tested |
| M8 upsert rule untested offline | `SetUpsert.Decide` pure + 6-case truth table | tests 82 / 70 / 167 |

Final re-verify: inventory before 0 sets → preview 37 create (14.5 s) → dryRun rolled back → apply 37 created (14.3 s, 0 orphans) → validate base 0 errors / 4 EMPTY warnings / 21/21 saved = registry (15.6 s) → validate extras 0 errors (10.4 s) → re-apply 37 unchanged.

## Production apply (approved by the user 2026-10-10 15:04, BIM-lead decisions = registry defaults)
| Step | Result |
|---|---|
| Backup | `HPNavis/output/live-verify/bim/backup/THCSLT-HPC-ZZ-ZZ-CM-ZZ-0001-before-searchsets-261010-1504.nwd`, SHA256 = original `B27F…D667` |
| Model | `Q:\My Drive\05_PracticAI\02_BIM\03-TRUONG THCS LONG TAN\02_NAV\THCSLT-HPC-ZZ-ZZ-CM-ZZ-0001.nwd` (Roamer pid 25444) |
| Inventory before | 0 folders / 0 sets |
| Preview / Dry Run | 37 create / 37 created → rolled back |
| Apply | 37 created, 14.3 s, Undo entry `MCP: HP search sets` |
| Verify | base 0 errors (4 EMPTY warnings A1/A3/M1/M3), extras 0 errors, saved = registry 39/39, counts identical to the copy, re-run 37 unchanged, 37 sets total |
| Save | **not saved by Claude** — the user saves in Navisworks |

## Limits
- Run time: ~15 s to preview/apply 37 sets, ~10–16 s per validation scope on this 95 MB model; a much larger model may need `codes` batches (120 s per call).
- No Navisworks XML export of the new sets (the API has no search-set XML writer); the reusable config is the registry JSON.
- Clash Detective untouched (as requested).
