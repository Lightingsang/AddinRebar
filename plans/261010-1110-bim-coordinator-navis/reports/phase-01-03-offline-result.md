# Phase 1–3 + 5 offline result — 2026-10-10

## Done
- Generator `HPNavis/tools/bim-coordinator/generate-clash-matrix.py` → `Rules/hp-clash-matrix.json`: 184 rules, P 40/99/45, LOD 27/154/184, tol 50/30/10, LOD400 null; 0 diff vs ChatGPT starter. Finding: starter's 3 "duplicate" cells (P6/P8/P10) are legend swatches, not matrix cells — generator reads data region per row group only.
- Engine `HPNavis.BIMCoordinator` (net48): Rules/, SearchSets/, ClashTests/, Probe/, CoordinatorTools facade. Bridge: ProjectReference, compiler reference (only with Clash API), resolver allow-list, startup log `BIM coordinator engine OK`.
- Seeds `Coordination/`: bim_probe_disciplines (none), bim_sync_search_sets, bim_sync_clash_tests (auto, apply=false preview), bim_run_canary_tests (heavy).
- Tests: BIMCoordinator 57, Mcp.Server 64, McpBridge 157 (incl. 4 seeds compiled by the bridge compiler, Roslyn scan engine vs NavisHeavyGate.HeavyMembers).

## Review fixes (code-review-slice1.md)
| Id | Fix |
|---|---|
| H1 | identity = rule id + LOD parsed from name; priority change → Update with rename (CurrentName) |
| H2 | ruleIds capped at MaxCanaries (5) in selector + facade |
| H3 | duplicates reported (plan.Duplicates), read-back groups by name |
| H4 | probe walks ≤ 4 levels for file nodes (extension or ISO role), skips container root, 200 files / 20 000 nodes, warnings ≤ 50 |
| M1 | update starts from `CreateCopyWithoutChildren()` of stored test (ignore rules, primitive types kept) |
| M3 | Roslyn identifier scan of engine source vs gate list |
| M5 | priorities outside 1..3 → ArgumentException |
| M6 | size test: full 184-row listing < 48 KB |
| M7 | resolver allow-list + startup engine log |

## Incident
`dotnet test HPNavis.McpBridge.Tests` without `-p:DeployPlugin=false` ran the deploy target while Roamer held the plugin: RemoveDir deleted 10 unlocked files (ribbon en-US, 3 lazy System.* DLLs, pdbs, README) then failed. Restored from bin same session (same package versions, ribbon unchanged in git). Documented in CLAUDE.md.

## Open (live, HITL)
- Not verified in Navisworks: Source File / Category internal names on THCSLT, AddGroup OR semantics, file-node probe, M1 results kept after EditTestFromCopy, M2 timing of 21 searches (M5 = 72 groups), M4 test↔set link after ReplaceWithCopy, canary = manual run counts.
- Server exe not republished (locked by running hprebar-navis servers) → new seeds not in tools/list yet; live check can call the engine through execute_navis_code.
