# Code review — search-set registry rework (BIMCoordinator)

Date 2026-10-10. Read-only adversarial review. Scope: `HPNavis.BIMCoordinator/SearchSets/**` (hp-base-sets.json, SetDefinitionModels,
BaseSetCatalog, SearchSetPlan, NavisSearchSetCompiler, IsoFileName), `Validation/**`, `CoordinatorTools.SearchSets.cs` + `CoordinatorTools.cs`,
`HPNavis.BIMCoordinator.Tests/SearchSetPlanTests.cs`, seeds `Coordination/{bim_sync_search_sets, bim_validate_search_sets, bim_list_selection_sets}`,
consumer `ClashTests/NavisClashTestCompiler.cs` + `ClashTestPlanner.cs`. Rules: HP_CLEAN_CODE_CORE + net48-inprocess (NI1–NI12) + CODE_REVIEW_CHECKLIST.
Live facts taken as given (not re-litigated): AddGroup = OR of AND-groups, Negate keeps absent params, FromDoubleLength on Diameter, ModelItem value
equality, category-first 4×, apply/dryRun/re-apply/conflict guard on the THCSLT copy.

Score **7/10**. Status: Implemented + Built + Tested (offline) + Verified live on one copy (searchset-validation.md); findings below = CHƯA TEST unless marked.

## Gates run (this review)
| Check | Result |
|---|---|
| `dotnet test HPNavis.BIMCoordinator.Tests` | 67/67 pass |
| `dotnet test HPNavis.Mcp.Server.Tests` | 70/70 pass |
| `dotnet test HPNavis.McpBridge.Tests -p:DeployPlugin=false` | 167/167 pass (seed compile + engine heavy boundary) |
| `hp-base-sets.json` vs `reports/hp-search-set-registry.json` | byte-identical (LF-normalised) |
| Condition count of the registry (python over JSON) | 37 sets, 522 OR-groups, **1 219 conditions**; folder `HP BIMCoordinator/MEP` alone 1 047 |
| Reflection on installed `Autodesk.Navisworks.Api.dll` | `SearchCondition` overrides `ToString`; group boundary lives in `Options` (`StartGroup`, `NegateCondition`); `FindAll(doc, bool reportProgress)` |

## Design intent — verified by reading
| Claim | Verdict | Evidence |
|---|---|---|
| Apply never overwrites a differing set without allowUpdate | ✅ | NavisSearchSetCompiler.cs:59-64 — `differs && !allowUpdate` → `conflict`, no write path |
| Never deletes | ✅ | only `AddCopy` / `ReplaceWithCopy` in the engine; no `Remove*` |
| Preview never writes | ✅ | `Preview` → `Run(write:false)`; `Create`/`Replace`/`EnsureFolder` reachable only with `write` (:61, :64) |
| Stale GroupItem wrappers | ✅ | `FindSet` per plan (:58), `EnsureFolder` re-reads each depth (:111-118) and returns a fresh lookup (:120), `Replace` re-finds parent + index (:79-80); `IndexOfSet` counts all `Children` (folders included) = the index `ReplaceWithCopy` expects |
| Clash compiler finds the base sets after the folder rename | ✅ for creation | NavisClashTestCompiler.cs:22-25, :98-102 go through `SearchSetPlan.All/For` → new `Architecture/Structure/MEP` path. ❌ for drift detection — see H1 |
| Base categories disjoint per discipline | ✅ (test) | SearchSetPlanTests.cs:79-87 |
| Extras scope pulls parents | ✅ | NavisSearchSetValidator.cs:54-57 (M2, M4 added before details) |
| Gap logic consistent with sets | ✅ item level | gap search and sets share `DescendantsAndSelf` + `PruneBelowMatch`; see M3 for the ancestor/descendant blind spot |
| Envelopes of Sync / Validate under 64 KB | ✅ | Sync 37 rows ≈ 7 KB + ≤ 37 warnings ≈ 4 KB; Validate ≤ 21 rows + 60 issues + 60 gaps ≈ 16 KB |
| Envelope of ListSelectionSets under 64 KB | ❌ | H2 |

## Findings (ranked)

### High

**H1 — Clash tests stay bound to the pre-rename sets and the planner reports them `Unchanged`.**
Slice 1 wrote base sets under `HP BIMCoordinator/ARC|STR|MEP` (CLAUDE.md still says so); v2 writes under `…/Architecture|Structure|MEP`
(hp-base-sets.json:4, SearchSetPlan.cs:57). On any model where slice 1 ran (at least the `bimverify` copy: 21 sets + 121 tests), `bim_sync_search_sets`
creates 21 new sets beside the old ones (old never deleted — by design). Tests bind by path (live-verified in phase 4), so they keep feeding from the **old**
sets. `ExistingTests` reads only `DisplayName` of the resolved source (NavisClashTestCompiler.cs:105-110) and `Drift` compares names only
(ClashTestPlanner.cs:99) → if display names are unchanged (`A2 Ceilings` both sides — GIẢ ĐỊNH CHƯA XÁC MINH, slice-1 names not in the repo) every test reads
`Unchanged`; any later registry change (allowUpdate on a new set, a new condition) never reaches the tests and `ReadBack` (:62-73) says OK. Same blind spot
for a user's own same-named set in another folder. This is slice-1 M4 second half, made real by the rename (K1: identity = path, implemented as name).
Fix: `SetName` returns the full path (walk `SavedItem.Parent` to the root) and the planner compares with `plan.Folder + "/" + DisplayName`; a path drift
→ `Update` (keeps results, live-verified M1) and rebinds. Test: stored `SetA = "HP BIMCoordinator/ARC/A2 Ceilings"` + plan path `…/Architecture/…` → Update.

**H2 — `bim_list_selection_sets` (the "backup" tool) overflows the 64 KB cap once the registry is applied.**
CoordinatorTools.SearchSets.cs:83 emits one string per `SearchCondition`; the cap is 400 *entries* (:13), not bytes. Applied registry = 1 219 conditions;
the tool's own example `folder: "HP BIMCoordinator/MEP"` = 1 047. At ~50–90 B per JSON string (internal names `LcRevitData_Element` /
`lcldrevit_parameter_-1140325` + value + quotes) → 55–110 KB → the bridge returns a truncated string, not JSON (memory: slice-1 H4 class). Never exercised live:
the inventory ran only on the empty tree ("0 folders / 0 sets", searchset-validation.md). Also the flat `ToString` list drops `Options` (StartGroup/Negate) unless
`ToString` prints them (GIẢ ĐỊNH CHƯA XÁC MINH) → the OR structure is not recoverable, so "backup" overstates it (the real backup is the NWD copy + SHA).
Fix: byte budget (stop at ~48 KB, `truncated` + `remaining`), `conditionCount` + `groupCount` per set by default and conditions only with `detail: true`
(cap per set, e.g. 40), group index per condition from `Options.HasFlag(StartGroup)`; envelope-size test with a 1 219-condition fake (T4); rename "backup" →
"inventory" in tool.json until it round-trips.

### Medium

**M1 — Set identity = `"<code> <name>"` inside one folder: a rename or a folder move in the registry creates a second set, and nothing reports the old one.**
SetDefinitionModels.cs:60 + NavisSearchSetCompiler.cs:85-86. Renaming `M4.6 Other (rain water)` or moving a detail to another folder → `created`, old set left
(fine: never delete) but **invisible**: Sync has no orphan list, Validate checks only registry codes. Same defect class as slice-1 H1 (identity embeds a mutable
attribute). Fix: match by parsed code (`^(\S+) `) under `catalog.Folder` anywhere; a name/folder drift → `conflict`/`update` like a search drift; add
`orphans` = sets under `catalog.Folder` that no plan owns (old `ARC/STR/MEP` folders included), reported, never deleted.

**M2 — `allowUpdate` is all-or-nothing.** CoordinatorTools.SearchSets.cs:19 / NavisSearchSetCompiler.cs:63-64: approving one conflict (A2) with
`allowUpdate:true, includeExtras:true` also replaces every other conflicting set in that call, incl. a lead's deliberate static set (HasSearch false →
`differs` → replaced, explicit items lost). The guard holds, the approval granularity does not. Fix: `allowUpdate` only with explicit `codes`
(`ArgumentException` otherwise, M8/NI7), or `approve: ["A2"]` list; preview already lists conflicts per code.

**M3 — Overlap/gap checks are item-identity only; ancestor/descendant overlap is invisible.** NavisSearchSetValidator.cs:104-112 compares `ModelItem`
equality. Each set searches with `PruneBelowMatch` independently, so if a NWC nests `Curtain Panels`/`Curtain Wall Mullions` under the `Walls` node of a
curtain wall, A8 holds the wall (geometry incl. panels) and A4 the panels → `OVERLAP 0` while Clash Detective clashes panels against their own wall
(phase-04 already saw "A4 13 635 items → noisy tests"). Same for railings/top rails, stairs/runs, nested shared families. GIẢ ĐỊNH CHƯA XÁC MINH for the
THCSLT tree (slice-1 L2, still open). Fix: in CheckOverlaps also count items of set B having an ancestor in set A (`item.Ancestors` depth ≤ ~6), issue
`NESTED_OVERLAP`; one live probe on a curtain wall decides severity.

**M4 — Two implementations of the role rule disagree, and the validator's errors depend on it.** Wildcard `*-{role}-????*` (SearchSetPlan.cs:49,
case-insensitive via NavisSearchSetCompiler.cs:31) vs `IsoFileName` regex `-[A-Z]{1,2}-\d{4}` upper-case (IsoFileName.cs:13), used by WRONG_DISCIPLINE
(NavisSearchSetValidator.cs:95) and gaps (:156). Single-letter roles `A, S, M, E, P` match any `-X-` field: an ARC file with volume/zone `E`
(`PRJ-HPC-E-01-M3-AA-0001`) lands in every MEP set → caught as WRONG_DISCIPLINE (good) but not preventable; a lower-case or `…-AA-R01A` name is in the set and
flagged WRONG_DISCIPLINE (false error). K1. Fix: a pure test that runs a small wildcard matcher (`*`/`?`) over a name corpus and asserts
`{roles whose wildcard matches} == {IsoFileName.Role}`; drop single-letter roles not used by HP files or document the volume-code constraint in the registry.

**M5 — Registry validation gaps (fail at run time or silently, not at load).** BaseSetCatalog.cs:75-93:
- detail without `parent` passes load (:92 checks only non-null) → `CheckDetails` `members[p.Parent!]` (NavisSearchSetValidator.cs:116-118) throws
  `ArgumentNullException` — an engine fault, counted against stability (NI7).
- detail categories ⊄ parent categories, detail discipline ≠ parent discipline, detail roles ⊄ parent roles: all load fine, found only live
  (DETAIL_OUTSIDE_PARENT).
- inside one detail folder, two details sharing a category without mutually exclusive conditions (e.g. a future M4.8 with Pipes and no System
  Classification) → only DETAIL_OVERLAP warning live; a pure check is cheap (same property, `equals`, different values).
- `roles` on a **base** set is honoured (SearchSetPlan.cs:75) but never validated against its discipline; `folder`/`discipline` on a base set silently ignored.
- `value` not parsed at load: `bool.Parse` / `double.Parse` (NavisSearchSetCompiler.cs:34, :40) throw `FormatException` mid-apply after N sets were created
  (rolled back, but late and counted). Parse at load.
- detail folders hard-code `"MEP/Details/…"` instead of deriving from `disciplineFolders` — renaming the MEP folder splits the tree.
Fix: extend `Validate` + one mutation test per rule (pattern of SearchSetPlanTests.cs:153-162).

**M6 — Detail and auxiliary sets never get the membership check; auxiliary overlap with base sets unchecked.** CheckMembership runs on `basePlans` only
(NavisSearchSetValidator.cs:39-40, :86). X.* sets exist precisely to be *outside* the base sets ("nothing silently dropped", BaseSetCatalog.cs:8-11) yet a
wrong X.* (e.g. a role typo) is never flagged; M2.1–M2.3 role narrowing is checked only indirectly. Fix: run CheckMembership for every plan (plan carries
Roles + Categories); in `extras`, load base members once (or the X.* disciplines' base sets) and flag `AUX_OVERLAP`.

**M7 — 120 s budget scales linearly with no resumable path.** One full search per set: apply 37 = 30.8 s, validate ≈ 20 s per scope on 95 MB
(searchset-validation.md). A 3–4× federation (common) times out; the whole apply rolls back and nothing tells the caller which `codes` batch to use.
`ct` is checked only between sets (NavisSearchSetCompiler.cs:55). Fix: time budget (e.g. stop starting new sets after ~80 s), return `remaining` codes and
`elapsedMs` per set — re-apply is idempotent (`unchanged`), so batches resume safely. `codes` is also not de-duplicated or capped (SearchSetPlan.cs:68-70):
`codes` of 500 duplicates = 500 searches + 500 rows (> 64 KB). `Distinct()` + cap at the registry size.

**M8 — Core safety logic untested offline (T4, T7).** The upsert decision (exists × differs × write × allowUpdate → action) is 4 lines inside the
Navis adapter (NavisSearchSetCompiler.cs:59-64); overlap / detail / gap arithmetic is generic set algebra inside the Navis adapter (validator :104-167).
The promise "never overwrite without allowUpdate, never delete" is pinned only by one live run. Fix: pure `SetUpsert.Decide(...)` + `[Theory]` over the 8
combinations; validator checks over `IReadOnlyDictionary<string, HashSet<T>>` with string items in tests. Also SearchSetPlanTests.cs:140-144 asserts the
wildcard string only (tautological).

### Low
- **L1** Category values are display strings (`EqualValue(FromDisplayString("Walls"))`, SearchSetPlan.cs:92): a model exported from a non-English Revit gives
  empty sets everywhere. Probe warns (`MissingCategories`) — document it in tool.json.
- **L2** GAP severity is one `info` total (validator :165); matrix categories in another discipline's file (ARC Lighting Fixtures 40, outside X.*) are
  elements that will never be clash-tested — split `GAP_MATRIX_CATEGORY` as warning.
- **L3** Stringly typed actions `create/created/update/updated/conflict/unchanged` (NavisSearchSetCompiler.cs:61-64) vs enum `PlanAction` on the clash side (N9).
- **L4** M4/M5: `SyncSearchSets` 7 params incl. 3 bools (CoordinatorTools.SearchSets.cs:19); `Run(write, allowUpdate)` bool switches.
- **L5** BridgeEntry.cs:172-176 two consecutive `<summary>` blocks; the first describes `CoordinationEngine` but sits on `LogCoordinationEngine` (CM1).
- **L6** CLAUDE.md BIMCoordinator paragraph says both "written under `HP BIMCoordinator > ARC|STR|MEP`" and v2 `Architecture|Structure|MEP` — stale sentence.
- **L7** `ReplaceWithCopy` gives the set a new Guid; clash binding by path is verified, other Guid consumers (Timeliner attachments, viewpoint links) are not.

## Edge cases scouted
- Duplicate same-named sets in our folder: `FindSet` and `IndexOfSet` both take the first → consistent, no wrong-index replace.
- A FolderItem named like a set (or vice versa) in our folder: `FindSet` ignores folders → `create` beside it; no overwrite.
- Static set with the registry name: `HasSearch` false → `conflict` (good); replaced only with allowUpdate (M2).
- Unit change of the document: M4.9 threshold = `32 / mmPerUnit`, deterministic per unit → re-apply in another unit reads `conflict`, not a silent update.
- Extras scope with base sets not applied → parents NOT_SAVED errors (expected).

## Test quality
Registry tests are good (coverage of groups, disjoint base categories, role map, mutation tests for property/op/parent, DNF shape, A6/M4.9 conditions).
Missing: upsert decision table (M8), validator algebra (M8), wildcard ⇔ IsoFileName agreement (M4), registry rules of M5, ListSelectionSets envelope size
(H2), clash planner with a path-drifted set (H1).

## Recommended order
1. H1 path identity in `SetName`/`Drift` + test. 2. H2 byte budget + summary mode + size test. 3. M1 orphan list + code identity. 4. M5 load-time
validation + M8 pure decision/validator tests. 5. M2 approval per code, M7 time budget + `remaining`. 6. Live: one curtain wall / railing ancestry probe (M3),
`SearchCondition.ToString` sample + `bim_list_selection_sets` on the applied copy (H2), re-run `bim_sync_clash_tests` on the `bimverify` copy after
re-applying sets (H1 reproduction).

**Status:** DONE_WITH_CONCERNS
**Summary:** Upsert/conflict/never-delete logic and stale-wrapper handling are correct and the registry is well evidenced; but clash tests stay silently
bound to the pre-rename sets (name-only identity, H1) and the inventory/backup tool overflows 64 KB once the 37 sets exist (H2).
**Score:** 7/10
