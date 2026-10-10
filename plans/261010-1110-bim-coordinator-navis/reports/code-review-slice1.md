# Code review — BIM coordinator slice 1 (engine + 4 Coordination seeds)

Date 2026-10-10. Read-only review. Scope: `HPNavis/HPNavis.BIMCoordinator/**`, `HPNavis.BIMCoordinator.Tests/**`,
`HPNavis.Mcp.Server/Registry/SeedLibrary/Coordination/**`, `tools/bim-coordinator/generate-clash-matrix.py`, the diffs to
`BridgeEntry.cs`, `HPNavis.McpBridge.csproj`, `HPNavis.slnx`, `NavisHostProfile.cs`, `SeedLibraryStructureTests.cs`, `SeedLibraryCompileTests.cs`.
Rules: HP_CLEAN_CODE_CORE + host appendix net48-inprocess (NI1–NI10) + CODE_REVIEW_CHECKLIST.

Score **6.5/10**. Status: Implemented + Built + Tested (offline); live = CHƯA TEST.

## Gates run
| Check | Result |
|---|---|
| `dotnet test HPNavis.BIMCoordinator.Tests` | 51/51 pass |
| `dotnet test HPNavis.Mcp.Server.Tests` | 64/64 pass |
| `dotnet test HPNavis.McpBridge.Tests -p:DeployPlugin=false` | 155/155 pass (seed compile + heavy gate) |
| Matrix recount (python over embedded JSON) | 184 rules, P 40/99/45, LOD 200/300/350 = 27/154/184, tol 50/30/10, 400 = null — matches intent |
| Engine source grep for heavy members | none |

## Design intent — verified
| Claim | Verdict | Evidence |
|---|---|---|
| apply=false never writes (EnsureFolder unreachable) | ✅ | CoordinatorTools.cs:47 → `Preview` (NavisSearchSetCompiler.cs:35-48) = FindSet + FindAll only; SyncClashTests preview = `Plan` only (reads) |
| Stale wrappers after AddCopy/ReplaceWithCopy/EditTestFromCopy | ✅ | parent folder, index and existing test re-looked-up per iteration (NavisSearchSetCompiler.cs:59-61, NavisClashTestCompiler.cs:46); ReplaceWithCopy index counts all `Children` (IndexOfSet) — correct |
| Missing/empty set → skip, never widen | ✅ | ClashTestPlanner PlanOne; test `Missing_or_empty_sets_skip_the_test_and_never_widen_it` |
| Sets only under `HP BIMCoordinator/<disc>`, never delete | ✅ | FindFolder/EnsureFolder; no Remove call anywhere |
| LOD400 refused | ✅ | ClashMatrix.ToleranceMmFor; test |
| Tolerance via mmPerUnit + read back | ✅ | same semantics as the live-verified `create_and_run_clash_test` (`units.ToDrawing`) |
| Engine never calls heavy members; canary run visible to gate | ✅ today | grep + NavisHeavyGate sees `TestsRunTest` in canary code.cs:15; runner refuses dryRun for heavy (NavisScriptRunner.cs:45) |
| No new guard/heavy-gate bypass via the engine reference | ✅ | engine exposes no heavy/transaction/reflection surface; any script could already write sets/tests under `auto`; `none` still enforced by the fingerprint |

## Findings (ranked)

### High

**H1 — Test identity contains the priority: a workbook priority edit duplicates tests and strands their issue history.**
ClashTestNaming.cs:14 puts `P{priority}` in the name; ClashTestPlanner.cs:55-57 / PlanOne match by full name. Change HP_A8_S2 from P3 → P2 in the
xlsx → new test `HP|P2|…` created, old `HP|P3|…` reported orphan (never deleted) with all results/status/comments (the slice-2 issue store). The
`Drift` priority check can then never fire from a matrix change. Plan says "name is identity" — but the name embeds a mutable attribute. (K1, NI7-ish contract)
Fix: match existing tests by `ClashTestNaming.Parse(name)` → (ruleId, lod), ignore the P segment; on priority drift do `Update` and rename (DisplayName in the
copy). Add test: stored `HP|P3|LOD350|ARC-STR|A8-S2` + matrix P2 → Update, not Create + orphan.

**H2 — Canary cap bypassed by `ruleIds`.** CoordinatorTools.cs:92 caps only `count`; CanarySelector.cs:17-23 returns every listed rule. `ruleIds`
of 184 ids → 184 uninterruptible `TestsRunTest` calls in one 600 s run (cooperative ct is only checked between runs; Roamer frozen until all finish).
Fix: `if (ruleIds.Count > MaxCanaries) throw ArgumentException`; add `"maxItems": 5` to the schema (run_tool does not validate schema — the engine check is the real one). Test it.

**H3 — `ReadBack` throws on any duplicate test name in the document, after everything was written.**
NavisClashTestCompiler.cs:58 `ToDictionary(t => t.Name)` over *all* tests (users' too). Two tests named alike anywhere (common: user copies, or
`create_and_run_clash_test` run twice with one name) → `ArgumentException: same key` → run fails → bridge rolls the whole apply back, and the error is
excluded from stability as a "caller error". Planner already tolerates duplicates (GroupBy, ClashTestPlanner.cs:52), so the two halves disagree.
Fix: `GroupBy(...).ToDictionary(g => g.Key, g => g.First())` (same rule as the planner and `Apply`'s `FirstOrDefault`); test with a duplicated foreign name.

**H4 — Probe `Files()` reports levels as files for an ordinary federation, unbounded, can blow the 64 KB cap.** NavisModelProbe.cs:23 descends into
`RootItem.Children` whenever the root has any — for each NWC model in `doc.Models` the root *is* the file and its children are levels/categories. Result:
"file 'Level 1' has no known ISO role code" per level, `success` unaffected but the survey is wrong; 30 files × 15 levels = 450 `files` + 450 warnings ≈ 60–80 KB
→ the bridge returns a truncated *string* instead of the envelope (NavisResultSerializer catch path). The 200-item cap applies only to Navisworks collections,
not to these `List<ProbedFile>`. GIẢ ĐỊNH CHƯA XÁC MINH for THCSLT's exact tree (the unit test expecting role `ZZ` for `…-CM-ZZ-0001.nwd` suggests the author saw the
NWD as a node — i.e. a nested NWD — which is the one case this branch fits).
Fix: one row per `doc.Models` entry from `Model.SourceFileName ?? FileName`; descend only into children that are themselves model roots (`ModelItem.HasModel`);
cap `files`/warnings (e.g. 100 + count) and pin the envelope size in a test.

### Medium

**M1 — `TestsEditTestFromCopy` with a fresh `ClashTest` resets whatever the BIM lead customised.** NavisClashTestCompiler.cs:48 + Build (:83-92): rules
(ignore same file/composite), SelectionA/B primitive types, self-intersect, description go back to API defaults on any drift-triggered update; `Drift` does not look
at them, so they are never re-reported either. Also the claim "results, statuses, comments stay" (class doc :10) is unverified for EditTestFromCopy with a copy
that has no children. Fix: start the copy from the existing test (`current.CreateCopy()` then set name/type/tolerance/selections), and add "results kept after
update" + "rules kept" to the phase-4 live checklist.

**M2 — Performance on the 95 MB federation: 21 full-model searches per call, up to 72 OR groups each.** `Plan` runs `GetSelectedItems` for all 21 sets
(NavisClashTestCompiler.cs:25) in both `SyncClashTests` and `CanaryTestNames` (canary needs only ~6 sets); `SyncSearchSets` runs 21 `FindAll`
(NavisSearchSetCompiler.cs:44/58). DNF expansion = roles × categories: M5 9×8 = 72 groups, M3/M4 27, M2/M7 18. ct is checked only between sets; timeout 120 s
(`none`/`auto` ceiling without heavy). No measurement yet. Fix options: one traversal (`FindIncremental` over items having Element>Category) bucketing
(role-of-source-file, category) once → all 21 counts; canary counts only the sets of candidate rules; measure live and record per-set ms in the summary.

**M3 — Heavy-call boundary test is weaker than the gate it protects.** HeavyCallBoundaryTests.cs:12-14: own regex list, line-based, needs `.Member(` on one
line. Misses `TryAppendFile`, `AppendFiles`, `MergeFiles`, `TryMergeFile`, `TrySaveFile`, `TryExportToNwd`, `TryPublishFile`, `TryOpenFile`,
`TryRemoveFile`, `TryOpenAggregate` (all in `NavisHeavyGate.HeavyMembers`), method groups (`Action<ClashTest> run = clash.TestsRunTest;`) and multi-line calls.
This test is the *only* fence: the gate scans seed text, never engine IL. Fix: in `HPNavis.McpBridge.Tests` (references the bridge) scan the engine
assembly's MemberReferences via System.Reflection.Metadata against `NavisHeavyGate.HeavyMembers` (plus `Document.Clear`), so the list is single-sourced (K1).

**M4 — Search-set update by `ReplaceWithCopy` of a brand-new `SelectionSet`.** NavisSearchSetCompiler.cs:62/74: if Navisworks binds a clash
`SelectionSource` to the set's Guid rather than its path, every set update silently detaches all tests (ours → next plan = mass Update; users' tests → broken
selection, no report). Path-based binding is likely but unverified. Related: `SetName` (NavisClashTestCompiler.cs:102-107) compares `DisplayName` only, so a test
pointing at a same-named set in another folder reads as Unchanged. Fix: live check (update one set → its tests still resolve); compare the resolved set's folder
path, not just its name.

**M5 — Out-of-range filters succeed silently.** `priorities: [4]` or `[0]` → empty plan, `success: true`, "0 rule(s)" (ClashMatrix.cs:52; seed
bim_sync_clash_tests/code.cs:4 casts `long`→`int` unchecked). run_tool does not enforce schema min/max. Fix: `ArgumentException` for priorities ∉ 1..3 (NI7, M8).
Same for unknown `codes` duplicates (harmless) — fine.

**M6 — Envelope size not pinned.** Worst case `bim_sync_clash_tests`: 184 Update rows with selection+tolerance+priority reasons ≈ 37–40 KB (computed),
plus `orphans` (unbounded, ≤ ~6.5 KB) and `mismatched` (≤ ~6.5 KB) ≈ 50 KB of the 64 KB cap — fits today, no headroom test. Over the cap the bridge returns a
truncated string, not JSON. Fix: `CoordinatorToolsEnvelopeTests` serialising the worst case with `BridgeJson.Options` < 60 KB (AEC practice); cap orphans list.

**M7 — Engine not in the resolver allow-list nor in the self-check (NI1).** PluginAssemblyResolver.cs:17-25 and ScriptingSelfCheck.cs:33 do not list
`HPNavis.BIMCoordinator`; it loads today only through default plugin-folder probing (versions equal). `ScriptingSelfCheck` never touches it, so a broken
engine shows up only when a seed runs. Fix: add the name to both lists and log one line at startup (e.g. `bim matrix 184 rules`), like AutoCAD's `aec tolerance 0.5`.

### Low

- **L1** IsoFileName.cs:12 needs 4 *digits* and upper case; set wildcard SearchSetPlan.cs:25 accepts any 4 chars, case-insensitive (`IgnoreStringValueCase`) →
  probe and sets disagree on `…-AA-R01A.nwc` / lower-case names. One rule, two implementations (K1).
- **L2** Revit curtain walls are category `Walls`; with `PruneBelowMatch` A8 takes the whole curtain-wall subtree (panels + mullions) if the NWC nests them under
  the wall node → A4-A8 / A8-A8 noise at 10 mm. GIẢ ĐỊNH CHƯA XÁC MINH — check in the probe/live run.
- **L3** M4/M5: `SyncClashTests` 8 params, `NavisClashTestCompiler.Plan` 8, `Apply` 6; `bool apply` / `bool listUnchanged` switch behaviour. A
  `ClashSyncRequest` record + `Preview*`/`Apply*` pairs would read better.
- **L4** ClashMatrix.Validate (ClashMatrix.cs:78) does not pin `id == HP_{left}_{right}`, `disciplinePair` vs groups, tolerance > 0, rule LOD = AND of group
  LODs; `ClashTestNaming.Parse` rebuilds ids from names, so a hand-edited id breaks orphan detection silently.
- **L5** `ReadBack` ignores Priority although `Drift` checks it; Preview verbs `create/update` vs Apply `created/updated` differ in `summary`.
- **L6** K4: `ClashRule.IsSelfPair` (ClashMatrixDocument.cs:53) unused.
- **L7** Navis wrappers never disposed (`Search`, `FindAll` collections up to 500 k items, copies) — same as existing seeds; `using var` is cheap.
- **L8** Probe property sample = first 50 geometry items (NavisModelProbe.cs:35) = first model only; a non-Revit first model (DWG/IFC) flips `success` to false.
- **L9** Generator `read_tolerances` (generate-clash-matrix.py:92-102): last match anywhere in the sheet wins; fail on conflicting duplicates instead.

## Test quality
Pure layer well covered (counts, LOD, naming round trip, skip rules, canary spread, catalog/role mapping). Gaps: no test for H1 (priority change), H2 (ruleIds
cap), H3 (duplicate names — adapter, but the dictionary rule can live in the pure layer), M5 filters, envelope size (M6); heavy fence weak (M3). Adapters
(`Navis*`) untested by design until phase 4 live.

## Recommended order
1. H2, H3, M5 (one-line guards + tests). 2. H1 identity by parsed (ruleId, lod). 3. H4 probe files from `doc.Models` + caps. 4. M3 IL-level heavy fence,
M7 allow-list + self-check line. 5. Phase-4 live checklist add: EditTestFromCopy keeps results/rules (M1), set update keeps test links (M4), per-set search ms on
the 95 MB model (M2), curtain-wall subtree (L2).

**Status:** DONE_WITH_CONCERNS
**Summary:** Engine is well-layered, preview paths are read-only, wrapper handling and skip-never-widen are correct, and no heavy-gate bypass exists; but test identity embeds priority (H1), the canary cap is bypassable (H2), duplicate names abort apply (H3) and the file probe likely lists levels and can exceed 64 KB (H4).
**Score:** 6.5/10
