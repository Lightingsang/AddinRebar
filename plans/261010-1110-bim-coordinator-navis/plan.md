# HPNavis BIMCoordinator — slice 1 (rule → search set → clash test → canary)

Requirement contract confirmed 2026-10-10 (grill-me). Source standard: `HPBIM_MaTranKiemSoatVaCham.xlsx` sheet `RuleClash(HP)`
(project folder `03-TRUONG THCS LONG TAN\03_Settings\02_Nav`); ChatGPT starter JSON = cross-check only.

## Decisions (user)
| Topic | Decision |
|---|---|
| Product | engine `HPNavis/HPNavis.BIMCoordinator` (net48) + thin MCP seeds (like `HPAutoCad.Aec`); no WPF UI |
| Test unit | 1 clash test per matrix rule, whole federation; block (khối) is only a filter later |
| Discipline | role code in source-file name first (AA → ARC, ES → STR, EC/EE/EF/EP → MEP), Revit category inside |
| Rule source | regenerate from xlsx (`HPNavis/tools/generate-clash-matrix.py`); tests pin 184 / P 40-99-45 / LOD 27-154-184 |
| Issue state | native Clash Detective status/assigned/comments in the NWD (slice 2) |
| Acceptance model | THCSLT CM (ARC+STR+MEP), LOD350 = 184 tests @ 10 mm, upsert by name, never delete |
| Run | create all, run 3 canary tests (heavy); Run All stays manual |
| Report | slice 2: engine writes HTML/xlsx beside the NWD (needs path policy) |

## Phases
| # | Phase | Type | Status |
|---|---|---|---|
| 1 | Generator xlsx → `Rules/hp-clash-matrix.json` + offline tests | AFK | done (0 diff vs starter JSON) |
| 2 | Engine pure layer: matrix, LOD/tolerance, test names, base-set definitions, plan/diff | AFK | done, tests 57 |
| 3 | Navis adapters: property probe, search-set compiler, clash-test compiler, canary run + seeds `Coordination` | AFK | done, built; review 6.5/10 → H1–H4, M1, M3, M5–M7 fixed |
| 4 | Live verify on a copy of THCSLT (needs the pipe — one client at a time) | HITL | done 2026-10-10 — reports/phase-04-live-verify.md |
| 5 | Docs: CLAUDE.md, skill hp-mcp-navisworks, catalog | AFK | done; exe republished (28 tools) |

## Slice 1b — search-set rebuild (2026-10-10, user request)
| Step | Status |
|---|---|
| Analyze: RuleClash(HP), SearchSets(HP), 6 XML (36 sets), live properties | done — reports/searchset-analysis.md |
| Build: registry v2 (21 base + 11 detail + 5 auxiliary, evidence per set), validator, 2 new seeds | done, tests 67/70/167 |
| Dry Run / Backup / Apply / Verify on THCSLT copy | done — reports/searchset-validation.md (0 errors) |
| Approval → apply on the production THCSLT model | pending user |
| Clash tests re-pointed to the new sets | not started (Clash Detective out of scope) |

## Slice 1c — colour by search set (2026-10-10, user request)
| Step | Status |
|---|---|
| Contract (grill-me): 44 sets of ColorSearchSet(DSC), permanent colours + reset, mapping decisions | done |
| Generator + mapping + registry, 2 seeds (bim_sync_color_sets, bim_paint_colors) | done — tests 117/76/177 |
| Live on THCSLT copy: 33 sets, 13 982 painted, read-back 450/450, reset keeps user colour | done — reports/colors-live-verify.md |
| Review 7.5/10 → M1 M2 M3 M5 M7 L1 L2 L8 L10 fixed | done — live re-run 2026-10-11: M1/M2/M5 confirmed |
| M4 reset scope kept as is (documented); M6 per-project mapping deferred to a later slice | decided 2026-10-11 |
| Apply sets + colours on production THCSLT, user saves | pending user |

## Slice 1d — plumbing fixtures under MEP (2026-10-11, grill-me contract)
| Step | Status |
|---|---|
| Registry: X.1 + X.2 (ARC fixtures only) → `MEP/Plumbing fixtures`; new X.6 ARC sprinklers + plumbing equipment in `Not in matrix`; X.3–X.5 unchanged | done |
| Engine: `previously` paths → sync moves + renames a saved set (`Move`, `EditDisplayName`), `SetUpsert.Relocate/Describe/IsConflict` | done — tests 130/76/177, exe republished |
| Live on THCSLT copy (2026-10-11): 38 created → old layout rebuilt by script → preview move/move+conflict/create → apply moved, moved+conflict (success:false) → allowUpdate [X.2] updated → re-run 38 unchanged, 0 orphans; validate extras 0 errors (X.1 388, X.2 927, X.6 203) | done |
| Preview no longer lists to-be-moved sets as orphans | done, tests; live CHƯA TEST |
| Apply on the original model 2026-10-11: dryRun → 38 sets created, 33 colour sets created, paint 13 982 (dryRun first, read-back 450/450), validate extras 0 errors | done — Save by user pending |

## Slice 1e — clash tests LOD 200 / 350 / 400 by Nhóm (2026-10-11, grill-me contract)
| Step | Status |
|---|---|
| Analysis of Nhóm 1/2/3 → reports/clash-groups-analysis.md (generator tools/bim-coordinator/generate-clash-groups-report.py) | done |
| LOD400 = LOD350 pairs @ 10 mm (user decision; `LOD400_TOLERANCE_MM`) | done |
| Folders LOD > Nhóm: spike — the 2026 API refuses every Clash Detective folder (ClashTestFolder/FolderItem at TestsRoot, nested, TestsCopyFrom) → user chose flat list, names `HP|LOD<lod>|P<n>|…` (old `HP|P<n>|LOD…` parsed + renamed) | done |
| Tests 133/76/177; exe republished; catalog + skill sync | done |
| Live on THCSLT copy: LOD200 10 / LOD350 121 / LOD400 121 created (dryRun first), 252 in name order, tolerance 50/10/10, re-run all Unchanged, legacy name → Update rename, canaries 200: 0/0/0, 350: 0/17/2, 400: 0/17/2 | done |
| Apply on the original model 2026-10-11: dryRun (LOD350 P1 26 create, rolled back) → 252 tests created (10/121/121), read back OK, 0 orphans | done — user Save pending |

## Slice 1f — M4 → M4.9 and the Rules tab (2026-10-11, user decisions)
| Step | Status |
|---|---|
| M4 pairs select M4.9 (pipes D >= 32 mm, note 3) via base-set `clashSelection`; plain search-set sync writes M4.9 | done — tests |
| `IgnoreRulePolicy`: same group always, snap points only same discipline, never layer/file; MergeComposites on; drift → Update; rules from `LcClClashRules.LoadBaseRuleSet`, fallback test 'HP Rules Template' | done — tests 144/76/177, exe republished |
| Live on copy 2026-10-11: rule set loaded from Navisworks (no template needed); 252 created with rules; old-style tests (composite off, no rules, M4) → 252 Update, read back OK, re-run Unchanged; 163 × 1 rule, 89 × 2, composite 252/252, 32 tests on M4.9, 0 on M4; canaries 200 0/0/0, 350 0/17/2 + M4-M4 233 + M4-S3 71, 400 0/17/2. Found: an edit that flips MergeComposites drops the copy's rules and selections → flipped in a separate edit | done |
| Original model 2026-10-11: search sets 22 unchanged; dryRun 252 Update rolled back; apply 252 Update, read back OK; 163 × 1 rule, 89 × 2, composite 252/252; user's 'Test 1' untouched | done — user Save pending |

## Slice 1g — design phases, group names in test names, skill (2026-10-11, grill-me contract)
| Step | Status |
|---|---|
| Phases 200 / 300 / 350-400 (`ClashMatrix.PhaseOf`, 350 + 400 = one set @ 10 mm); names `HP|LOD<phase>|P<n>|<pair>|A5 Doors-S2 Structural Walls` (old forms parsed + renamed, planned-name twin kept) | done — tests 150/76/177 |
| `removeOtherPhases` (approval, whole model): deletes HP tests of other phases + duplicates via `TestsRemove`, never non-HP tests | done — tests |
| Skill `.claude/skills/hp-bim-clash-phase` (co-so / ky-thuat / thi-cong), routing rule, docs, report (THCSLT: 10 / 110 / 121) | done |
| Live on copy 2026-10-11: original-like state (LOD200 10 / LOD350 121 / LOD400 121 old names + Test 1) → thi-cong preview 121 Update (rename) + 121 duplicates + 10 other; dryRun with removal rolled back (131 removed); all 3 phases applied (350-400 121 renamed, 300 110 created, 200 10 renamed), read back OK; canaries 300 0/17/0, 350-400 0/17/2, 200 0/0/0; co-so + removal → 352 removed, 10 LOD200 + Test 1 left, re-run Unchanged | done |
| Original model 2026-10-11 → co-so: search sets 22 unchanged; preview 10 Update + 242 to remove (LOD350 121, LOD400 121); dryRun rolled back; apply removed 242, renamed 10, read back OK, re-run Unchanged; 10 HP LOD200 + Test 1 | done — user Save pending |

## Layout
```
HPNavis/HPNavis.BIMCoordinator/        net48, refs Navisworks Api + Clash (Private=false), System.Text.Json (already in Core graph)
  Rules/        ClashMatrix, ClashRule, LodPolicy, hp-clash-matrix.json (embedded), base-sets.default.json (embedded)
  SearchSets/   BaseSetDefinition, DisciplineRoles, SearchSetCompiler (Navis)
  ClashTests/   ClashTestPlanner (pure plan/diff), ClashTestCompiler (Navis), CanaryRunner (Navis)
  Probe/        PropertyProbe (Navis)
  CoordinatorTools.cs   facade the seeds call (fully qualified — no McpShared import change)
HPNavis/HPNavis.BIMCoordinator.Tests/  net48 xUnit v3, pure layer only
HPNavis/HPNavis.Mcp.Server/Registry/SeedLibrary/Coordination/<seed>/
```

## Gates
- `dotnet build HPNavis/HPNavis.slnx -c Debug -p:DeployPlugin=false` (Roamer open)
- `dotnet test` BIMCoordinator.Tests, McpBridge.Tests (seed compile), Mcp.Server.Tests (seed structure + quality)
- live: preview counts, apply LOD350 dryRun → 0 change, apply → 21 sets + N tests, re-apply idempotent, LOD400 refused, 3 canaries = manual run

## Known open items
- Methodology note 3 (no flex ducts/pipes, D < 32 mm): flex excluded by category; diameter filter needs live property → later.
- Search OR-groups + `Source File` property presence in a published NWD: confirm live before apply.
