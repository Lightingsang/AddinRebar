# Phase 4 — live verify, 2026-10-10

Model: copy `HPNavis/output/live-verify/bim/THCSLT-HPC-ZZ-ZZ-CM-ZZ-0001-bimverify.nwd` (99.5 MB, Feet, 19 appended files). Navisworks Manage 2026 (23.3), plugin Debug deployed 11:47, engine log `BIM coordinator engine OK: 184 matrix rules … 21 base sets`. Calls through `execute_navis_code` (session server = old exe), same code as the seeds.

## Findings that changed code
| Found live | Fix |
|---|---|
| `Item > Source File` on elements = Revit local copy `THCSLT-HPC-TT-ZZ-M3-AA-0001_sangtq6ANLE.rvt` → wildcard `*-AA-????.*` matched nothing | wildcard `*-<role>-????*`; `IsoFileName` regex allows `_suffix` |
| Revit `Element` tab sits on the composite element node, not the solids → probe said "Category not found" | probe samples first 2000 items, not geometry only |
| `GetSelectedItems` count of 21 sets in the clash plan ~55 s | `FindFirst` (empty or not) |
| priority 4 rejected only after the set scan (call ~119 s, near the 120 s cap) | args validated before the scan (built, unit-tested, NOT redeployed live) |

## Results
| Check | Result |
|---|---|
| probe: files / roles | ✅ 19 files, 19 roles (8 AA, 7 ES, EC/EE/EF/EP) |
| probe: properties | ✅ Source File + Element Category by internal name |
| elements per discipline | ARC 26 418 / STR 35 953 / MEP 24 628 (6.2 s) |
| search OR-groups (`AddGroup`) | ✅ counts plausible per set |
| sets dryRun | ✅ 21 created then rolled back, 0 sets after |
| sets apply | ✅ 21 created, 56.5 s; EMPTY A1, A3, M1, M3 |
| sets re-apply | ✅ 21 unchanged |
| tests preview LOD350 | ✅ 121 Create, 63 SkipEmptySet, 40 s |
| tests dryRun | ✅ 121 written, read back OK, rolled back |
| tests apply | ✅ 121 written, 0 read-back mismatch; sample M2-S5 Hard 0.0328 ft = 10 mm, P1, 1 set per side |
| tests re-plan | ✅ 121 Unchanged, 0 orphans, 0 duplicates |
| canary (heavy) | ✅ A2-M2 0, A2-S5 17, M2-M2 2 results; 46 s total |
| M1 update keeps results | ✅ tolerance drift 20 → 10 mm: 17 results + description kept; test Old (needs rerun) |
| M4 test ↔ set link | ✅ by path: replaced set feeds the test (275 doors while drifted, 142 ceilings after repair) |
| error paths | ✅ LOD400, priority 4, 6 ruleIds, not-ready canary → ArgumentException |
| published exe tools/list | ✅ 28 tools (4 Coordination seeds) |

## Not done / for the BIM lead
- Compare canary counts with a manual Run in Clash Detective (same test) — user.
- Category coverage review: ARC files hold Plumbing Fixtures 927, Sprinklers 198, Structural Columns 132, Generic Models 638 (in no set); STR Generic Models 26 604 (in no set); MEP Plumbing Fixtures 381, Fire Protection 73, Conduits counted in M5. A4 Curtain Walls = 13 635 items (mullions + panels) → noisy tests.
- Methodology note 3 (no pipes D < 32 mm) not implemented.
- Arg-validation-before-scan fix not redeployed live.
