# Code review — Kata B01 drawing parity + Revit numbering (2026-10-05)

Mode: read-only on source. Gates run by reviewer (allowed by task): `dotnet test HPRebar.Core.Tests` → **1428/1428 pass**; `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` → **Build succeeded**, 0 warnings in KataRebar/KataExport files. Edge cases probed with a scratch console (ProjectReference to HPRebar.Core, B01 sheet copied from `KataB01DrawingTests.Sheet()`). Revit side **CHƯA TEST** (phase 6 live check pending).

## Scope
| Area | Files |
|---|---|
| Core drawing | KataSectionLines/Style/Bars/Tags/Cuts, KataSectionDrawingBuilder, KataElevationDrawingBuilder, KataDrawingFrame, KataElevationDims, KataElevationOutline, KataBarTagBuilder |
| Core layout/numbering | KataInnerStirrupLayout, KataLayerSpacerTieLayout, KataSupportTopBarLayout, KataBarNumbering, KataRebarCalculator, KataLayoutRemoval, KataSpanRebarSpec, KataZeroWidthSupports |
| Revit | KataRebarStorage (new), KataRebarNumberAssigner (new), KataRebarStamp, KataRebarCleanupService, KataRebarOrchestrator, KataRebarSectionFit, KataBarSetCreator, KataRebarCreationService, KataStirrupSetCreator, KataRebarDrawing |
| Tests | KataB01Dwg*Tests, KataB01DwgFixture + Fixtures/b01-dwg.json, KataSectionInnerStirrupTests, KataSectionCutsTests, KataShapeCodeAndBarMarkTests (+ updated KataInnerStirrupTests/CalculatorTests/SideBarTests) |

## Checks run
| Check | Result |
|---|---|
| Core tests | ✅ 1428/1428 |
| Build Debug.R26 | ✅ |
| Probe: B01 base | ✅ 5 spans, joint at 2500 in span 3, 13 cuts, numbers 1..35 no zero |
| Probe: left cantilever (row-20 cells cleared) | ✅ one cut at 2000 − 666.7 = 1333 |
| Probe: left cantilever with B01 row-20 cells | ❌ `ArgumentOutOfRangeException` in KataAnchorage via KataWidthProfile.Cut — **pre-existing** (neither file touched today) |
| Probe: B11 empty (no top main bars) | ✅ no crash; same C25/C26/C27 warning emitted 3× (carry-over) |
| Probe: two zero-width joints in one span | ✅ second joint's row 17 reported, not drawn |
| Probe: K14 `3f20;3f16` (width change at K) | ❌ middle bars of both sides at y 0, z −103, overlapping x 24450…24898 (see #5) |
| API contracts (RevitAPI.xml 2026.4) | ChangeNumber: ArgumentException when toNumber exists / fromNumber absent / partition absent; ArgumentOutOfRange toNumber < 1; InvalidOperationException when elements cannot be modified. GetNumbers: ArgumentException for unknown partition. SchemaBuilder Public/Public needs no VendorId. |

## Findings
| # | Sev | File:line | Problem | Fix |
|---|---|---|---|---|
| 1 | High | HPRebar/HPRebar/KataRebar/Service/KataRebarNumberAssigner.cs:46, :54 | `ChangeNumber(partition, revit, temp)` renumbers **every** element holding that number in the partition, not only Kata bars. A hand-drawn or other-tool bar identical to a Kata bar in partition "B01" (or a bar of another beam of the same name drawn in an earlier run) is moved to temp and then to the Kata number. Breaks the contract the storage/cleanup docs state ("never a bar drawn by hand"). Worse when the sheet has no beam name: the partition stays Revit's default, shared model-wide. (PCC-075 spirit / R-contract) | While collecting, group **all** rebars of the partition by number; skip (and warn) any number group that holds a bar not in `ours`. Skip numbering entirely when the partition is empty. |
| 2 | Medium | KataRebarOrchestrator.cs:48 + KataRebarNumberAssigner.cs:46, :60 | Numbering runs as a `runner.Run` step; any Revit exception from it (ChangeNumber `InvalidOperationException` — elements not editable in a workshared model; the fallback `ChangeNumber` itself refused) propagates to the orchestrator catch and **rolls back the whole generation** — every bar lost for a cosmetic step. Contradicts the user decision "refused numbers keep Revit's number and are reported". (R2/R3) | Catch `Autodesk.Revit.Exceptions.ApplicationException` per partition (or per group) inside `Apply`, add a warning, continue; keep the step committed. |
| 3 | Medium | KataRebarNumberAssigner.cs:51-63 | Refusal cascade: a refused group goes back to its Revit number **immediately**, which may be the Kata target of a later group in `moved` → that later group is refused too although its Kata number was free. Revit numbers by creation order, Kata by drawing order, so Revit numbers commonly equal other groups' Kata numbers. | Two passes: first try temp→kata for every group and collect failures; then give failures their Revit number if still free, else the next free number. |
| 4 | Medium | KataRebarNumberAssigner.cs:20-63 | The two-phase swap is pure logic with no test (T1, PCC-279). Every case above is invisible to CI. | Extract the plan (groups → list of `(from, to)` moves + refusals) to Core behind a small `INumberSequence` seam; xUnit the swap, collision, cascade and foreign-bar cases. |
| 5 | Medium | HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs:122-126, :206-207 | New `Spread` at a support whose spans differ in width spreads each side's layer independently across B6. With odd counts both middle bars land at y 0 at the same z: probe K14 `3f20;3f16` → Ø20 at y 0 x 22950…24898 and Ø16 at y 0 x 24450…26550, **448 mm coincident** over support K (also `1f20;1f16`). The comment "they cannot meet" is false for odd counts. B01 (`2f20;2f16`) does not hit it. | When both sides overlap in X, keep `PartitionInterleaved` (or offset the second side's middle bar); add a test with `3f20;3f16` asserting no two bars share (y, z) over an X overlap. |
| 6 | Medium | KataInnerStirrupLayout.cs:160-161, :176; KataStirrupSetCreator.cs:67-68 | **CHƯA TEST in Revit.** (a) Inner U now ends in 4 Ø returns + 5.5 Ø drops (40/55 for Ø10, 32/44 for Ø8) built as straight segments; two 90° bends at the stirrup bend radius (centreline ≈ 2–2.5 Ø each) need ≥ 40–50 mm, so Revit is likely to refuse the curve loop → `KataBarSetCreator` catches and only warns: the U disappears from the 3D model. (b) Hoop sets now start on +Y with `across = −AxisY`; normal = across × up = −X, so the first `Layout(… barsOnNormalSide: true)` always grows the wrong way and every set takes the relayout fallback; a misplaced box falls back to single bars silently (log only). | Live-check both on a model copy before commit (phase 6); if the U fails, create it from a RebarShape or with the drop as a hook. Consider passing `barsOnNormalSide: false` first. |
| 7 | Low | KataRebarNumberAssigner.cs:28, :75-78 | Revit number read with `AsString()`. GIẢ ĐỊNH CHƯA XÁC MINH that `REBAR_NUMBER` is string-stored; if not, `ours` is empty and the step silently does nothing (no log, no warning). | Read by `StorageType` (or `schema.NumberingParameterId`); log when no Kata bar has a number. |
| 8 | Low | KataLayoutRemoval.cs:88-89 vs KataInnerStirrupLayout.cs:119, :144-157 | Striking a hoop zone no longer removes its inner stirrups when (a) the run was split at a top step (`"<zone> [2]"`) or (b) I8 = 2 (`"Đều a500"`, B01's mode). The removal comment still says "the inner stirrups of a removed zone go with it". | Decide the rule (J7 inner stirrups behave as ties?) and make removal match by span + station overlap, or keep the zone name on split parts. |
| 9 | Low | KataInnerStirrupLayout.cs:43, :130-137 | Carry-over re-emits the same `C25 … không vẽ` warning once per carried span (3× on B01 with B11 empty). Also `ref` accumulators in Core (PCC-075/076). | Return `(entries, carriedFromSpan)` without `ref`; warn only for the span that owns the cell. |
| 10 | Low | KataBarNumbering.cs:23-102 | `Apply` is ~80 lines with three local functions and nested loops (M2, PCC-067); optional `spec = null` silently changes the tie key (no width) and the support order (index instead of station) (PCC-063). | Make `spec` required (both callers pass it); split into `NumberSupports`, `NumberSpanSets`. |
| 11 | Low | KataInnerStirrupLayout.cs:160-161 vs KataSectionLines.cs:82, :102 | U/C geometry rule (4 Ø turn, 5.5 Ø drop/tail, 2 Ø inset) written twice — layout constants and section-drawing literals (K1, PCC-230/055). | Move to `KataSectionStyle` (already holds `TieBendDiameters`) and use from both. |
| 12 | Low | KataSupportTopBarLayout.cs:197-201 | New members inserted between `Cuts`'s XML doc and `Cuts`: `SpansDifferInWidth` now has two `<summary>` blocks, `Cuts` none (CM1). | Move the two members above line 197. |
| 13 | Low | KataElevationDims.cs:93 | Mid-point split only for `j > 0`; two zones of different numbers at j = 0 still split at the first zone's end. Unexplained asymmetry. | Apply to every j or comment why j = 0 differs. |
| 14 | Low | KataSectionDrawingBuilder.cs:59; KataSectionTags.cs InnerStirrups | Every U is tagged at −0.73 h: two U entries in one section stack their tags at the same point. | Offset per U or group like the C ties. |
| 15 | Low | KataSectionBars.cs:56, :103, :131 | `+ 0.0` to fold −0.0 is unexplained (CM1). | One-line comment or a `NoNegativeZero` helper. |
| 16 | Low | KataRebarSectionFit.cs:84 | `line == default` as "not found" sentinel on a tuple. | `FirstOrDefault` into a nullable / `Any` check. |
| 17 | Low | KataLayerSpacerTieLayout.cs:25, :103 | `MaxBarsHeld = 3` generalises two B01 cases to every beam: any 4+ bar layer (e.g. a 400-wide 4Ø layer) loses its layer-spacer ties. Behaviour change outside B01, rule unconfirmed by a Kata drawing with 4 bars on 300–400 width. | Log as rule assumption in `docs/specs/kata-beam-rebar-rules.md`; confirm with a DY drawing. |
| 18 | Low | KataB01DwgFixture.cs:25-27 | Fixture located via `[CallerFilePath]`: breaks under `PathMap`/`ContinuousIntegrationBuild` or a test run from copied binaries. `_views ??=` not thread-safe (harmless double load under parallel xUnit). | `<None Include="KataRebar/Fixtures/*.json" CopyToOutputDirectory="PreserveNewest" />` + `AppContext.BaseDirectory`; `Lazy<>`. |
| 19 | Low | many new lines (KataBarNumbering.cs:75-82, KataRebarNumberAssigner.cs:30, :38-39, :45, KataRebarStorage.cs:32-34, :41) | Unbraced single-line `if`/`for`; ~25 new lines > 140 chars (FM2). Test names sentence-style, not `Method_Scenario_Expected` (PCC-277) — consistent with the existing Kata suite. | Boy-scout on touched lines. |
| 20 | Info | KataRebarStorage.cs:14-52 | Correct API use: GUID identity, Public read/write (no VendorId needed), simple string/int fields, `GetEntity` → `IsValid()`. Notes: any field change needs a **new GUID** (Finish throws on a mismatched identity); `Beam` field written but never read (YAGNI); `FilteredElementCollector` scans every rebar — an `ExtensibleStorageFilter(SchemaId)` would narrow cleanup and numbering on big models. | — |
| 21 | Info | pre-existing: KataWidthProfile.cs:80/104 → KataAnchorage.cs:45 | Left cantilever with B01's row-20 cells → `ArgumentOutOfRangeException "A free end has nothing to anchor in"`. Not from today's diff; log as a B-xx behaviour defect per governance. | Log in CLEAN_CODE_AUDIT, fix in its own change. |

## Edge cases asked for
| Case | Verdict |
|---|---|
| Zero-width joint numbering (I17 → 12 between 11 and 13) | ✅ `BottomExtraJointAtMm` set only when the joined span has no own bars (KataZeroWidthSupports.cs:102, :120-121), so span-level flag is safe; BarIds unique (one counter in KataRebarCalculator) so `numbered[BarId]` cannot collide |
| Cantilever cut L/3 (left and right) | ✅ KataSectionCuts.cs:56-57, probe 1333 / B01 32267 |
| Span with no top main bars | ✅ no crash; warnings duplicated (#9) |
| Empty partitions | ⚠️ empty beam name ⇒ default partition shared with all model bars (#1) |
| ChangeNumber exception types | ⚠️ only `Autodesk.Revit.Exceptions.ArgumentException` caught; `InvalidOperationException` and a failing fallback roll back everything (#2) |
| ChangeNumber from == to | ✅ skipped (`revit == kata[0]`); fallback never targets its own temp |
| GetNumbers ranges | ✅ `High` max used for free numbers; `Used` checks Low..High; temporaries start above every range |
| Schema access / name collision / transaction | ✅ Public/Public, identity by GUID (name collisions irrelevant), `SetEntity`/`ChangeNumber` inside runner transactions |
| Regressions for DY7/DY14 | ✅ goldens green; behaviour changes outside B01: section mirrored (`sign`), bar order from +Y, U shape, C long leg on +Y, I8 = 2 inner stirrups at J7, carry-over of rows 25-27, layer ties ≤ 3 bars, hoop set orientation — all deliberate per DWG, but none live-checked in Revit |

## Plan follow-ups (report only)
- Phase 6 live check on a model copy must cover: Rebar Number per partition incl. a refused number, a hand bar in partition B01 (#1), inner U creation (#6a), hoop set orientation (#6b).
- Phases 2/4 remain 🟡 as the plan states (7/14 flags, U/C tags).

**Score: 6.5 / 10** — the Core parity work is careful, evidence-cited and well tested (1428 green, DWG fixture assertions per section/number, goldens intact). The Revit numbering step is the weak part: it can renumber bars Kata did not draw, a failure there discards the whole run, refusals cascade, and none of it is tested or live-verified. One real geometry clash (#5) in the new width-change spread.

**What is good:** fixture-driven tests against the DWG; joint-bar numbering isolated by a `kind` in the signature; storage-based identification with a legacy Comments fallback in cleanup; `ActualMid` parallel-line fix explained by the hanger case.

## Fix round 2026-10-05

| # | Status | Change |
|---|---|---|
| 1 High | fixed | numbering groups ALL rebars (Rebar + RebarInSystem) of the partition; a number shared with a bar not drawn by Kata Rebar for these hosts is left alone + warning; empty beam name (default partition) → not numbered + warning |
| 2 | fixed | `RevitNumbering.TryChange` catches `Autodesk.Revit.Exceptions.ApplicationException` (Argument + InvalidOperation), logs, returns false → never rolls back the run |
| 3 | fixed | two passes: every Kata number tried first, refusals settled after (back to Revit number, else lowest free) |
| 4 | fixed | algorithm moved to Core `KataRebarNumberSwap` + `IKataNumberingTarget`; `KataRebarNumberSwapTests` (6, fake numbering that refuses like Revit) |
| 5 | fixed | odd/odd counts over a width change share the level's slots (interleaved); `KataSupportWidthChangeTests` (4, mutation-checked: fails 2/4 without the fix) |
| 6 | open | inner U returns / hoop orientation: live run shows no refused shape in the log (20 stirrup sets, 16 flat-bar sets, 1 Revit warning as before); hoop relayout fallback cost not measured |
| 7 | verified | REBAR_NUMBER read as string: live log "36 changed" (2 runs); warning logged when Kata bars carry no number |
| 8 | fixed | inner sets follow zone name before " [k]"; even (I8 = 2) runs go once the span keeps no hoops; `KataInnerStirrupRemovalTests` (2) |
| 9 | fixed (dup part) | carried rows warn once; `ref` accumulators kept |
| 12 | fixed | `Cuts` doc moved back |
| 21 | logged | B-46 in `docs/clean-code/CLEAN_CODE_AUDIT.md` |
| 10, 11, 13–19 | open (Low) | noted, not changed |

Checks: Core 1440/1440; Debug.R26 build OK; live test Revit (copy of model, pid own) 2 runs: "36 numbers, 36 changed, 1 warnings" (the F17/I17 merge 12/18), run 2 "deleted 60", no rollback. Foreign bar in partition B01 / workshared refusal: Core tests only, CHƯA TEST live (no MCP into the test Revit).
