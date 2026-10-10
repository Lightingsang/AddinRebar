# Code review — colour by search set (HPNavis.BIMCoordinator), 2026-10-10

Reviewer: code-reviewer (read-only). Scope: uncommitted diff on top of 7d23b42 —
`Colors/{ColorSetCatalog,ColorPaintPlan,NavisColorPainter}.cs`, `Colors/hp-color-sets.json`, `CoordinatorTools.Colors.cs`,
`CoordinatorTools.SearchSets.cs` (RegistryPaths), `SearchSets/{SetDefinitionModels,SearchSetPlan,BaseSetCatalog,NavisSearchSetCompiler}.cs`,
`hp-base-sets.json` (systemType), `tools/bim-coordinator/{generate-color-sets.py,color-set-mapping.json}`, seeds
`Coordination/{bim_sync_color_sets,bim_paint_colors}`, `ColorSetTests.cs`, seed-count tests.
Live facts in [colors-live-verify.md](colors-live-verify.md) taken as verified (NamedConstant System Type, 8/8 by display name,
1.3 ms/element enumeration, 450/450 read-back, reset keeps control wall) — not re-litigated.

## Checks run

| Check | Result |
|---|---|
| `python generate-color-sets.py --out <scratch>` vs committed JSON (CR stripped) | identical — 44 sets, 39 coloured, 5 Default, 11 pending, 23 verified |
| groups/conditions per active set (python over JSON) | 33 active, 220 groups, 584 conditions; max one set = 48 groups / 144 conds (`HP_P_PipeFitting+Accessory`) |
| `dotnet test HPNavis.BIMCoordinator.Tests` | 108/108 |
| `dotnet test HPNavis.Mcp.Server.Tests` | 76/76 |
| `dotnet test HPNavis.McpBridge.Tests -p:DeployPlugin=false` (Roamer open) | 177/177 (seed compile incl. 2 new seeds) |
| seed args ⇔ schema | `apply`/`allowUpdate`/`codes`, `mode`/`codes` — keys and literal defaults match schema defaults |
| 64 KB cap | sync ≤ 44 rows (~4 KB) + ≤ 88 warning lines; paint ≤ 44 rows × 3 lists — far under cap. OK |

## Acceptance vs agreed contract

| Contract item | Status | Evidence |
|---|---|---|
| 44 sets, sheet order, RGB from sheet, `<Default>` never paints | OK | ColorSetTests 18-46; ColorSetEntry.Paints ColorSetCatalog.cs:113 |
| pending not built, reported | OK | ColorSetCatalog.cs:37-44, CoordinatorTools.Colors.cs:23 |
| saved under HP BIMCoordinator/Color | OK | ColorSetCatalog.cs:42 |
| paint in sheet order, later wins | OK as written; expected-colour model not ancestry-aware (M2) | NavisColorPainter.cs:45-54, ColorPaintPlan.cs:20-25 |
| read-back via PermanentColor | OK, sample bias (L5) | NavisColorPainter.cs:71-94 |
| reset only painting sets' elements | OK; cannot reach elements that left a set (M4) | NavisColorPainter.cs:55-63 |
| upsert/conflict/never-delete shared | OK (same `NavisSearchSetCompiler.Apply`) | CoordinatorTools.Colors.cs:18-21 |
| net48 | OK (builds/tests on net48) | — |

## High

None found.

## Medium

**M1 — `apply`/`verify` answer `success:true` when nothing was painted or checked.**
NavisColorPainter.cs:37-39 skips every set missing from the document; with `bim_sync_color_sets` never applied (or the
Color folder renamed) `painting` is empty, `checks` empty → CoordinatorTools.Colors.cs:55 `success = mismatched.Count == 0` = true,
summary "0 set(s) read … 33 skipped". An agent reads success and moves on (NI7 spirit: a run that cannot do its job must say so).
Fix: `success = false` when mode is apply/verify and no painting set resolved, or when any non-pending set was skipped as
"not in the document"; keep pending skips informational.

**M2 — Expected colour ignores nesting (item-exact `Contains`), read-back picks "first geometry descendant".**
ColorPaintPlan.ExpectedRgb + NavisColorPainter.cs:83-87. With `PruneBelowMatch` per set, set X can hold a parent A
(Mechanical Equipment, Plumbing Fixture) while a later set Y holds a nested shared family D inside A (valve/fitting in an AHU
package, nested light in a fixture). Navisworks colours by geometry under the painted node, so the final colour of D's geometry
depends on paint order across *ancestors*, but `Contains(D)`/`Contains(A)` never sees the relation: false mismatches
(sample A → first geometry child may be D with Y's colour) or false passes. THCSLT showed "0 sampled taken by a later set",
so this path is unexercised — GIẢ ĐỊNH CHƯA XÁC MINH on HVAC/equipment-heavy models.
Fix: sample geometry nodes, expected = last set (paint order) containing any of `geometry.AncestorsAndSelf` (Contains is cheap);
add a pure test with a parent/child membership table. Also correct the doc claim "pipe fittings after their pipes"
(ColorPaintPlan.cs:8, seed description) — Pipes vs Pipe Fittings are disjoint categories, so that overlap cannot occur (CM1).

**M3 — Codes `C01..C44` are positional (`C{order:02d}`), so a sheet row insert renumbers every later code.**
generate-color-sets.py:71,77. Codes are an *input* of the approval gate (`allowUpdate` needs `codes`, SetUpsert.RequireCodesForUpdate)
and appear in examples/tests (`C17` in ColorSetTests.cs:81). After a regen, an approval written as `codes=["C24"]`
(today pending RawWater) targets another set. Saved-set identity is the display name, so no duplicates — the risk is
approving/painting the wrong set. Fix: stable code per mapping entry (store `code` in color-set-mapping.json, generator fails on
missing/duplicate), or drop codes from the public args and accept names only (K1: one identity).

**M4 — Stale colours cannot be removed: reset scope = current members of current painting sets.**
NavisColorPainter.cs:55-63. Cases: (a) a pipe re-typed/re-modelled out of every set after an NWC refresh keeps the old permanent
colour; (b) a sheet row switched to `<Default>` or made pending keeps its elements painted (Default sets are not reset);
(c) a renamed set → old saved set orphaned, its colour stays. Only `ResetAllPermanentMaterials` (excluded by contract) clears them.
Fix options for the user: reset scope = discipline-wide search of the files the engine paints (MEP roles) with a warning that
user colours on those files go too; or include Default + orphan sets under `Color/` in reset. At minimum document it in the seed
description and report orphan colour sets in `bim_sync_color_sets` (today only `bim_sync_search_sets` reports orphans).

**M5 — Painter resolves every set in every mode, including the two broadest searches that never paint; 120 s cap.**
NavisColorPainter.cs:33-40 calls `GetSelectedItems` (runs the saved search) for `HP_A_All`/`HP_S_All` (every ARC/STR element)
and the 3 other `<Default>` sets even in apply/verify/reset. ct is checked only between sets, a single search is not cancellable.
THCSLT (87 k elements): sync+paint 26 s, but an earlier combined run hit 120 s; the seed ceiling is 120 s (not heavy), so a
2–4× model times out and the whole paint rolls back. SearchSetPlan.cs:111-120 also keeps "category first" for category `*`,
where the wildcard matches everything and the source-file test is the selective one.
Fix: skip non-painting sets except in preview; for `*` put the source-file condition first (re-check `ValueEquals` → one-time
conflict on the two saved Default sets, approve via codes); return per-set elapsed ms; seed description: "large model → batch by codes".

**M6 — System-type rules are project vocabulary inside the company registry, matched by an English display name.**
color-set-mapping.json:3,21-26 (TNM/SH/TH/TP/TNT/TN(BM)/CNL — "Project abbreviations (THCSLT)") + hp-base-sets.json `systemType`
`byDisplayName` (NavisSearchSetCompiler.cs:28). On another project: no match → only "finds no element" warning; worse, a
project that uses `TH`/`SH` for a different service is painted with the wrong colour silently (`verified: true`). A non-English
Revit export names the tab differently → all 8 system sets empty. Logic on localised display strings (cf. R7).
Fix: tag these entries `projectSpecific: "THCSLT"` and warn in sync/paint when the document's file names do not carry that
project code; longer term a per-project overlay mapping file (rules folder, like AEC `rules\<name>.json`).

**M7 — Validation lets `notEquals` (and `bool`) take `values` alternatives → tautology.**
SetDefinitionModels.cs:42 + SearchSetPlan.cs:96-106: alternatives become OR-ed groups, so `notEquals values [a,b]` = (≠a) OR (≠b)
= every element; `bool values [true,false]` = all. BaseSetCatalog.ValidateDefinition (63-83) accepts both. Not used today
(latent), but the registry claims "a bad value must fail the load". Fix: `values` only with `equals`/`like`; reject
`value` + `values` both set; reject duplicate alternatives.

## Low

- **L1 — No group cap on the cartesian expansion.** SearchSetPlan.cs:96-106; today max 48 groups/144 conditions per set, 220
  groups total; two 6-value conditions on 2 categories × 4 roles = 288 groups in one set. Add a validation cap (e.g. 200
  groups/set) with a message naming the set.
- **L2 — Categories never validated.** ValidateDefinition checks count only (BaseSetCatalog.cs:67); a typo ("Pipe") or `*` mixed
  with named categories (redundant groups) passes; result = empty set + warning. Reject `*` unless sole entry; optionally a
  known-category list from the matrix workbook.
- **L3 — Mapping drift not pinned.** ColorSetTests pins the workbook sha256 only (48-55); a mapping edit without regen, or a
  hand edit of `hp-color-sets.json`, is not detected (regen was identical today). Add `source.mappingSha256` + test, or a test
  that re-reads the mapping and compares per-set rules.
- **L4 — Partial `codes` paint/reset breaks "later wins".** NavisColorPainter.cs:29: painting only C17 after a full paint
  overrides any later set sharing those elements; reset of a subset also clears colours other painting sets own. Warn when
  `codes` is given that order is enforced only inside the subset.
- **L5 — Read-back samples the first 25 of each set** (NavisColorPainter.cs:24,78) — same file/level every time, and 3 RGB pairs
  are shared (`0-128-192` RainWater/ElectricalFixtures/SupplyAir, `223-191-138` F/M fittings, `192-192-192` ×3) so a wrong-set
  paint is invisible; also `verify` passes where the model colour already equals the sheet (40/450 in dryRun). Info for report text.
- **L6 — Painter trusts conflicted saved sets.** A saved Color set left as `conflict` (user edit, older registry) is painted as-is
  without a word. `BuildSearch(plan).ValueEquals(saved.Search)` is cheap — warn "saved set differs from registry".
- **L7 — Colour-registry load failure now breaks `bim_sync_search_sets` after its writes.** CoordinatorTools.SearchSets.cs:27 then
  33/48-51: orphans need `ColorSetCatalog.Default`; an invalid embedded colour JSON throws InvalidOperationException after Apply
  → rollback, counted against stability. Tests guard the embedded file, so practical risk is low; compute RegistryPaths before
  Apply.
- **L8 — Error text says "search-set registry: Cnn" for colour sets** (BaseSetCatalog.cs:65 via ColorSetCatalog.cs:69). Pass a
  prefix or catch/rethrow with "colour registry".
- **L9 — `ResetPermanentMaterials` also clears transparency** on those elements; seed text says "removes the permanent colours".
- **L10 — Generator**: `entry["discipline"]`/`entry["categories"]` raise KeyError traceback instead of `fail()` (84-86);
  condition-level keys not checked (C# ignores unknown members → `vaues` typo becomes "value empty", acceptable); `data_only=True`
  (107) reads cached values — a formula-named row in a workbook saved without cache would be skipped silently (all cells are
  plain text today); blank merged B cell inherits the previous system (descriptive only).
- **L11 — Duplicated warning/summary code** between SyncColorSets and SyncSearchSets (conflict message, empty warning) (K1/PCC-232).

## Test quality

Good: workbook pin, sheet-order + RGB spot checks, pending never built, alternatives → groups, `*` plan shape, broken-registry
mutations, pure ExpectedRgb/Matches. Missing: ancestry case for expected colour (M2), success=false when nothing resolved (M1),
notEquals/bool alternatives rejected (M7), group cap (L1), mapping pin (L3), code stability across a row insert (M3).
`The_last_painting_set…` tests a lambda table, not the painter wiring (`Contains` semantics) — PCC-284 borderline.

## Positive

- Painter never enumerates whole sets; whole-collection paint/reset; Take/Contains only — matches the measured cost model.
- Reset never touches `ResetAllPermanentMaterials`; control-wall proof live.
- One upsert path shared with the search-set registry; pending sets refused with ArgumentException (caller error, NI7).
- Generator fails loud on sheet/mapping mismatch, bad RGB, non-HP_ names; output deterministic (regen identical).
- `PropertyKey.ByDisplayName` documented with the live reason; validation extracted and shared (`ValidateDefinition`).

## Recommended actions (order)

1. M1 success semantics (one line + test).
2. M5 skip Default sets outside preview + elapsed ms; describe batching.
3. M3 stable codes in mapping.
4. M2 ancestry-aware expected colour + test.
5. M7 + L1 + L2 validation tightening.
6. M4/M6 decisions for the user (reset scope beyond current members; per-project mapping).

## Unresolved (user decision)

- M4: may reset clear colours of MEP-file elements that left every set (also clears user colours on those files)?
- M6: keep THCSLT abbreviations in the company registry, or move them to a per-project mapping?

**Status:** DONE_WITH_CONCERNS
**Summary:** Feature matches the agreed contract, all 361 tests pass and the generator reproduces the committed JSON; no High
issue, but apply/verify report success with nothing painted, codes are positional, expected-colour logic ignores nesting, and
stale/project-specific colouring needs user decisions.
**Score:** 7.5/10

## Fix round (2026-10-10, same day)

| Finding | Fix | Evidence |
|---|---|---|
| M1 | `ColorPaintOutcome.Usable` (≥ 1 painting set resolved, none missing); `bim_paint_colors` success = Usable && 0 mismatches, every mode; missing sets are `blocking` skips | test `A_paint_run_that_found_no_painting_set_or_missed_one_is_not_usable` |
| M2 | expected colour = last set holding the sampled geometry node or any ancestor (`ExpectedRgb(AncestorsAndSelf, …)`); "fittings after pipes" wording removed (doc, seed) | test `A_nested_element_takes_the_colour_…` |
| M3 | `code` stored per entry in color-set-mapping.json; generator fails on missing / malformed / duplicate code; output identical (C01..C44 unchanged) | regen: 44 sets, codes == previous |
| M5 | `<Default>` sets resolved only in preview | code; live timing CHƯA TEST (needs redeploy) |
| M5 (`*` order) | not changed: reordering conditions changes the saved searches → conflict on the two Default sets; Default searches no longer run outside preview, so the gain is small | — |
| M7, L1, L2 | `values` only with equals/like on text (not notEquals/atLeast/bool); not value + values; no duplicate value/category; `*` alone; ≤ 50 groups per role | theory `A_definition_that_would_match_everything_or_explode_fails_validation` (7 cases) |
| L8 | `ValidateDefinition(definition, registry)` → "colour registry: …" | same theory |
| L9 | seed text says reset also clears transparency and leaves elements that left every set | tool.json |
| L10 | generator `fail()` on missing code/discipline/categories | — |
| M4, M6 | user decision | — |
| L3 L4 L5 L6 L7 L11 | not changed (low, documented) | — |

Gates: BIMCoordinator.Tests 117/117, Mcp.Server.Tests 76/76, McpBridge.Tests 177/177 (`-p:DeployPlugin=false`); server exe republished (tools/list 32), catalog regenerated + skill mirror synced (check exit 0). Live re-run of the fixed engine: CHƯA TEST — Roamer (pid 30044) still runs the previous engine DLL; redeploy needs it closed.
