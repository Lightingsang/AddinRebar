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
