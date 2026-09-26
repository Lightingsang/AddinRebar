# Code review — tie beams on foundations + B10 text (2026-09-27)

**Score 6.5/10.** The change is small and clean. It compiles with 0 CS warnings, and the Revit API use is correct. The live run matches the user contract for the one geometry it covers (caps 1700–1830 mm wide).

Raising the family/slab limit from 3000 to 6000 without raising the probe reach (1500 mm) does two things:
- footings wider than 3000 mm at a run end are measured wrong, with no warning;
- a raft cannot be detected under a run of 3000 mm or less.

The standing-warning fix also misses the most common false positive: a column above a column support in a per-storey model.

**Mode:** static review + gates allowed by lead. No Revit/Excel run by reviewer. The live numbers come from [phase-06-live-verify.md](phase-06-live-verify.md). The B10 claim was cross-checked against the lead's COM dump `scratchpad/p6/dam-after-gmx3.txt`: `B10 numfmt=@ value=-2.000 type=String`, PrefixCharacter `'`, so the apostrophe is a prefix, not literal.

## Scope

| File | Lines | Focus |
|---|---|---|
| [KataSupportCollector.cs](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs) | 264 | `Foundation`, `SlabThicknessMm`, `AddStanding`, `Finish`, constants |
| [KataTieBeamTests.cs](../../../HPRebar/HPRebar.Core.Tests/KataExport/KataTieBeamTests.cs) | 78 | 3 core tests |
| [KataExcelCell.cs](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataExcelCell.cs) + [KataExcelCellTests.cs](../../../HPRebar/HPRebar.Core.Tests/KataExport/KataExcelCellTests.cs) | 35 + 53 | B10 text |
| [KataExcelWriter.cs](../../../HPRebar/HPRebar/KataExport/Service/KataExcelWriter.cs) | 207 | `Cell` → `KataExcelCell`, B10 `@` |
| Read for context | — | `KataSolidReader`, `KataCandidateCollector`, `KataAxisFrame`, `KataBeamGeometry`, `KataSegmenter`, `KataRowBuilder`, [kata-cell-contract.md](kata-cell-contract.md) ▸ Giằng móng |

The files are untracked, so there is no git diff. The previous state was taken from [phase-03-revit-data-extraction.md](../phase-03-revit-data-extraction.md): "H3: bỏ móng băng / móng bè / móng dài > 3 m", meaning every foundation `Floor` was skipped before.

## Checks run

| Check | Result |
|---|---|
| `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false --no-incremental` (scratch `--artifacts-path`) | ✅ 0 `warning CS` / 0 `error CS` (24 ILRepack EXEC warnings, pre-existing) |
| `dotnet test HPRebar.Core.Tests` | ✅ 402/402 |
| RevitAPI.xml 2026.4.10: `HostObjAttributes.GetCompoundStructure` | Returns null when there is no compound structure; the `?.` handles it ✅ |
| RevitAPI.xml: `WallFoundation` (since 2016), `FloorType.IsFoundationSlab`, `FLOOR_ATTR_DEFAULT_THICKNESS_PARAM` "Default Thickness", `FLOOR_ATTR_THICKNESS_PARAM` "Thickness" | ✅ exist; a foundation slab is a `Floor` whose type is a `FloorType` |
| Conventions: < 300 lines, file-scoped namespace, why-comments, no plan refs (grep `phase\|H[0-9]\|M[0-9]\|review\|plan` in KataExport) | ✅ |

## Contract check (user-confirmed rules)

| Rule | Verdict | Evidence |
|---|---|---|
| Slab foundation > 100 mm and ≤ 6000 mm along the axis = support, row 11 = its width | 🟡 | [KataSupportCollector.cs:137-141](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L137-L141). The width is clipped above 3000 mm at run ends (H1) |
| Column on it = column above (row 19); no standing warning for it | ✅ | [:115](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L115), [:178-179](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L178-L179), `UpperOver` [:252-257](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L252-L257) |
| Slab ≤ 100 mm dropped silently | ✅ (Floor only, see M3) | [:137](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L137) |
| Thickness: type → instance → compound → solid height | ✅ | [:210-227](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L210-L227) |
| Slab > 6000 mm and WallFoundation → skipped + strip/raft warning | 🟡 | [:128-132](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L128-L132), [:140](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L140). A WallFoundation is warned even when the run does not touch it (M1); a raft is never "> 6000" under runs ≤ 3000 (H1) |
| Family foundations use 6000 | ✅ (value) / ⚠️ (effect, H1) | [:39](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L39) |
| Tie beam above the footing with a column stub → support = column | ✅ | The footing is not hit when its top is more than 20 mm below the soffit (probe at soffit − 20, [:104](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L104)). The stub's bottom is below the soffit, so `StandsOnRun` is false ([:246-250](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L246-L250)) |
| Standing warning only when the plan box straddles the centre line and overlaps the extent | ✅ as written / ⚠️ H2 | [:195-204](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L195-L204) |
| B10 written as text "-2.000" | ✅ | [KataExcelCell.cs:24-26](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataExcelCell.cs#L24-L26), [KataExcelWriter.cs:63-65](../../../HPRebar/HPRebar/KataExport/Service/KataExcelWriter.cs#L63-L65); live dump shows Value2 `-2.000` String |

## Findings

| # | Sev | File:line | Problem → failure scenario | Fix |
|---|---|---|---|---|
| H1 | High | [KataSupportCollector.cs:20](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L20), [:39](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L39), [:229-230](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L229-L230), [:140-141](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L140-L141) | **The probe reach (1500) is less than half the new limit (6000), so foundations wider than 3000 are clipped.** Probes run from `Start − 1500` to `End + 1500`. Tie beams are drawn cap-centre to cap-centre, so a cap of width W > 3000 at a run end measures `1500 + W/2`. Example, W = 4000 cap centred at s = 0: interval [−1500, 2000] → row 11 = 3500 (should be 4000); centre moves +250 → row 19 `350;-250` (should be `350;0`) and rows 21/23 −250 (should be 0). This is silent. With the old 3000 limit it could not happen: every accepted footing had W/2 ≤ 1500. Second symptom: a probe is only `run + 3000` long, so under a run ≤ 3000 mm a raft (thick foundation slab) always measures ≤ 6000. It becomes one Foundation support covering the whole run: row 11 = a single support, no span, no warning. D800 two-pile caps are about 3200–4000 long, so the first symptom is realistic. The live model's caps were 1700–1830, which is why it passed. | Give foundations their own probes, reach ≥ `MaxFoundationSupportMm` (e.g. `const FoundationProbeReachMm = MaxFoundationSupportMm + 500`). Filter them by `Overlaps(_run.Extent)` **before** counting strips, because a longer reach will also see rafts past the run end. If any interval of the element is longer than the limit, skip the whole element (see L3). Keep 1500 for columns, walls and beams. |
| H2 | High | [KataSupportCollector.cs:178-180](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L178-L180), [:115](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L115) | **In a per-storey model, every floor-beam run still warns about columns "standing on the beam".** The column of the storey above starts at level N, which is the beam top, so `Min.Z ≥ soffit − 20` → `AddStanding`. Its box straddles the centre line and overlaps the extent at every column support, and only *Foundation* supports exempt it. Result: "N column(s)/wall(s) standing on the beam…" on almost every floor-beam run (not the roof), which buries the genuine transfer-column warning. The same reasoning as the new comment on :177 applies to a column support. Live run 1 missed this because THCPHCS2 columns are continuous through the storeys. Not a data regression; the rows are unaffected. | `var carried = _supports.Where(s => s.Kind != KataSupportKind.Beam).Select(s => s.Extent).ToList();` then `_standing.Count(e => !carried.Any(c => c.Overlaps(e)))`. Update the :177 comment to "a support's column above". |
| M1 | Medium | [KataSupportCollector.cs:128-132](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L128-L132) | **A WallFoundation is counted as "under the beam" without probing it.** The candidate box is ±5 ft in plan and 1 ft below ([KataSupportCollector.cs:60](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L60), [KataCandidateCollector.cs:20-22](../../../HPRebar/HPRebar/KataExport/Service/KataCandidateCollector.cs#L20-L22)). So a strip footing under a parallel wall 1 m beside the tie beam, or one crossing below the soffit reach, gives "1 strip/raft foundation(s) under the beam were not taken as supports". That is a false warning. | `if (foundation is WallFoundation) { if (Intervals(KataSolidReader.GetSolids(foundation), _probes).Any(i => i.Overlaps(_run.Extent))) _stripFoundations++; return; }` |
| M2 | Medium | [KataSupportCollector.cs:139](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L139), [:104](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L104) | **Foundation slabs are probed at all 4 heights, including top − 20 and mid.** A slab-on-grade or ground slab modelled as *Structural Foundation: Slab* (150–250 mm) that surrounds a ground beam is hit by the top-20 probe when the slab wins the join or is not joined. It then becomes a Foundation support. That happens for panels ≤ 6000 along the run, and for any size under a run ≤ 3000 (H1). Before this change every foundation `Floor` was skipped, so this is a **possible regression for ground-floor beam runs**. How often it occurs in the user's models is GIẢ ĐỊNH CHƯA XÁC MINH. A run only bears on a foundation that reaches below its soffit. | Gate: accept a foundation only when the soffit − 20 probe hits it (`BottomFt − inset`). Keep the measured width from all probes, so tapered footings keep their current widths. The gate changes nothing for the live caps (−2800..−2000 vs probe −2520). It fits in the same foundation-probe set as H1. |
| M3 | Medium | [KataSupportCollector.cs:137](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L137) | **The lean-concrete filter only covers `Floor`.** Blinding modelled as a loadable or in-place family, or as a DirectShape in the Structural Foundation category, and placed under a tie beam, has its top at the soffit. It is hit by the soffit − 20 probe and, being ≤ 6000 long, becomes a Foundation support covering the span. Before, only spans ≤ 3000 were affected; now spans up to 6000 are. Separately, the 100 mm threshold is a cliff: 120/150 mm blinding slabs give the same silent result. | (a) Apply the thickness test to every foundation, using the solid height when the element is not a `Floor` (no real footing is ≤ 100 mm). This extends the user rule consistently rather than changing it. (b) Keep the 100 mm threshold the user chose, but add a warning when a foundation support is ≤ ~200 mm thick ("N thin foundation(s) taken as supports — check they are not lean concrete"). |
| M4 | Medium | [KataSupportCollector.cs:252-257](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L252-L257), [:36-39](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L36-L39) | **A two-column footing loses one column silently.** The limit went to 6000 explicitly so that "two-column footings stay below it". Both columns stand on it, so both are exempted from the standing warning (:179). `UpperOver` then writes only the one with the larger overlap to row 19. A stair or secondary column standing within a cap is handled the same way. | In `Finish`, count supports that have more than one `_uppers` interval overlapping them, and warn: "support at s≈X carries N columns; row 19 shows the one with the largest overlap". |
| M5 | Medium | [KataTieBeamTests.cs](../../../HPRebar/HPRebar.Core.Tests/KataExport/KataTieBeamTests.cs) | **The classification the change introduced has no test.** The three tests hand-build `Footing`/`Upper` and exercise `KataRowBuilder`, which already worked. The 100 / +0.5 / 6000 thresholds, the WallFoundation rule, the standing exemption and the straddle rule all live in the Revit layer. None of H1–M4 could be caught. | Move the pure decisions to Core (e.g. `KataFoundationRule.Classify(double? thicknessMm, IReadOnlyList<Interval1D> intervals, Interval1D run)` → Lean / Support / Strip, and a standing-warning filter over intervals) and test the boundaries: 60 / 100 / 100.4 / 101 mm; 5999 / 6000 / 6001 mm; a 4000 cap at the run end; a column above a column support. |
| L1 | Low | [KataSupportCollector.cs:203](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L203) | Only elements that straddle the *centre line* count. A column standing inside the beam width but off the centre line (wide transfer beam, eccentric column) is neither a support nor a warning. A rotated column's AABB is also inflated. | Test `offsets` against the beam band `[CenterOffsetMm ± WidthMm/2]` instead of the centre line. |
| L2 | Low | [KataSupportCollector.cs:137](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L137) | `+ 0.5` is an unnamed tolerance. It works: 100 mm converted from feet stays ≤ 100.5. | Name it (`ThicknessToleranceMm`) with a one-line why, or move it to `KataTolerance`. |
| L3 | Low | [KataSupportCollector.cs:140-141](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L140-L141) | Strips are counted per interval, not per element. A raft with an opening under the run gives a strip warning **and** a short Foundation support from the same element. A column standing on a raft or strip is reported as "standing on the beam". | If any interval of the element is longer than the limit, count the element once and add none of its intervals. Exempt standing elements over skipped strips from the "on the beam" text, or reword it. |
| L4 | Low | [KataExcelCell.cs:14](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataExcelCell.cs#L14), [:33](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataExcelCell.cs#L33) | (a) `\d` matches Unicode digits (full-width `１`, Arabic-Indic) → `double.Parse(Invariant)` throws `FormatException` → the write fails after B10's format is set ("ghi dở"). (b) `"01"` matches → written as the number 1, so a grid "01" shows as "1". | Use `^-?(0\|[1-9][0-9]*)(\.[0-9]+)?$` (ASCII only, no leading zero). Add tests for `"01"` and `"１"`. |
| L5 | Low | [KataTieBeamTests.cs:26](../../../HPRebar/HPRebar.Core.Tests/KataExport/KataTieBeamTests.cs#L26) | The comment says "column 100 mm past the footing centre (6000)". The column is centred at 5900, i.e. 100 mm *before* the centre, which is why the value is −100. | Reword to "100 mm short of the footing centre". |

## Edge cases walked (asked by lead)

| Case | Outcome |
|---|---|
| Thick raft, shorter than 6000 along a short run | Always a support under runs ≤ 3000 (probe clip) → H1. A slab short along the axis but long across it (a strip crossing the tie beam) is a support by contract, which is right for a crossing strip. |
| Pile cap partly below the probes (cap top 0–20 mm below the soffit) | Hit by soffit − 20 → footing support. A column on the cap has `Min.Z` = cap top ≥ soffit − 20 → column above. More than 20 mm → the column is the support (stub rule). Consistent with the contract; the switch happens at 20 mm. |
| Pile cap laterally eccentric so the centre line misses it | No footing and no upper (the probes follow the centre line); the column, if it straddles the line, becomes the support. Same design as before. |
| Blinding > 100 mm | Becomes a support if ≤ 6000 → M3(b). Family blinding at any thickness → M3(a). |
| Blinding under the caps (top = cap bottom) | Never reached by the probes, not a problem. |
| Type thickness 60 on a type named "D100" | Type parameter first → 60 → dropped (matches live). |
| Foundation `Floor` with no parameters and no solids | `null` thickness → falls through → no intervals → nothing. ✅ |
| Floor-beam runs | Rows unchanged unless a ground slab is a foundation slab (M2) or a 3–6 m family footing touches the soffit (H1). Warnings: fewer than before, but H2 remains. B10 changes from number to text on every run; Kata reads text (the template's `+3.300` is text). |

## Positives

- The type-then-instance-then-compound-then-solid order is well chosen. `GetCompoundStructure` is null-safe, and `WallFoundation`/`Floor` are pattern-matched before any geometry cost.
- `AddStanding` now uses the plan box relative to the run, which removes the false warnings from the wider-than-beam search box.
- `KataExcelCell` moved into Core with 14 tests. B10 is set `@` + `'`, and the live dump proves Value2 has no literal apostrophe.
- Comments explain *why* (blinding touches every tie beam; foundations longer than the limit are strips), with no plan references.

## Plan follow-ups (report only)

- The phase-06 "Còn lại" list should add: a run with a cap wider than 3000 at a run end (H1); a per-storey floor-beam run to check the warning list (H2); a ground beam on a foundation slab-on-grade (M2).
- Re-run the floor-beam regression (T1-DX12) with the new build. It is still marked "chưa chạy lại".

**Status:** DONE_WITH_CONCERNS
**Summary:** The build and 402 tests pass, the API use is correct and the live tie-beam case matches the contract, but two High findings remain. H1: the 1500 mm probe reach clips 3–6 m footings and hides rafts under short runs. H2: per-storey columns above column supports still raise the standing warning.
**Concerns:** H1 gives wrong numbers with no warning; H2 is warning noise. M2 and M3 can turn ground slabs or blinding into supports without any warning.
