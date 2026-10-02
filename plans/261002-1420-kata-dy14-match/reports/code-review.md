# Code review — Kata DY14 match (K13–K16)

Date 2026-10-02. Scope: K13 `Cranks`/`CrankMinDiameter` + settings/UI, K14 `RoundNearest`, K15 row 20 = layers, K16 "-" chains, the listed tests. Older uncommitted Kata work not reviewed.
Tests: `dotnet test HPRebar/HPRebar.Core.Tests` → 858/858 pass. Edge cases probed with a scratch console referencing `HPRebar.Core` (DY14 sheet from `KataDy14DrawingTests.Sheet`); no source edited, Revit not run.

Verdict: the rules match the plan and the drawings; one real geometry hole opened by K13 (High), one pre-existing ambiguity widened by K16 (Medium), test gaps on the new gate.

## High

### H1 — A cut step can leave the two bottom runs overlapping with less than one layer gap, or touching (K13)
[KataBottomMainBarRuns.cs:58](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataBottomMainBarRuns.cs#L58), [:86](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataBottomMainBarRuns.cs#L86), [:126](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataBottomMainBarRuns.cs#L126)

The old ≤ 100 rule cut only steps > 100, so a cut always left more than 100 − Ø between the two runs. Now a cut happens whenever:
- Ø < `CrankMinDiameter`, **whatever the step** (10, 20, 40 mm), or
- Ø ≥ 16 over a narrow support (crossing beam `bxh`, H < 6 × LayerGap = 180): e/H > 1/6 with e < 30.

In the cut layout the shallow run goes on G3·d past the support face and the deep run starts at the far face, **at the same Y**, |step| apart centre to centre. Nothing checks that |step| − Ø ≥ `LayerGap`, and `DeepLegRoom` silently clamps to 0.

Measured (DY14 sheet, scratch probe):
| Input | Shallow run | Deep run | Overlap | Clear gap |
|---|---|---|---|---|
| B12 2f14, F21 −10 (step 10) | Z −460 to x 6520 | Z −470 from x 6140, leg 0 | 380 mm | **−4 mm (bars intersect)** |
| B12 2f14, F21 −20 | Z −460 to 6520 | Z −480 from 6140 | 380 mm | 6 mm |
| B12 2f16, E11 100, F21 −40 | Z −459 to 6580 | Z −499 from 6141, leg 0 | 440 mm | 24 mm |

No warning or blocking for the clash; the only message is the anchorage one, which says "chân bẻ bị giới hạn 417 mm bởi chiều cao dầm" while the leg is actually 0 because of the shallow bar (see L3). In Revit this creates coincident/intersecting bars in one vertical plane.

Fix (pick one, lead/user decides the Kata semantics):
- when a cut would leave |step| − Ø < `rules.LayerGap(Ø, Ø)`, add a blocking (or warning) naming the support and the clear gap; or
- treat such a step as "no room to cut" and crank it anyway (a crank is always physically possible for a small step), with a warning that the Kata rule was overridden; or
- shift the deep run's start to the shallow run's end (no overlap) — loses anchorage, so needs its own warning.
A regression test per row of the table above.

## Medium

### M1 — A "-" chain between two supports with bars is claimed by both, giving coincident bars over whole spans (K16)
[KataTopBarContinuation.cs:28-38](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataTopBarContinuation.cs#L28-L38), [:45-56](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataTopBarContinuation.cs#L45-L56)

`Left` and `Right` each follow the chain to its far end with no notion of ownership. Probe C13 1f18, E13 "-", G13 "-", I13 1f18: support 0's bar runs x 42..14400 and support 3's bar runs 4650..16400, **both at y 0, same Z** → ~9.75 m of duplicated bars in one place. With one "-" (pre-existing K5) the overlap was 4650..8200; the chain multiplies it. No warning.

Fix: a "-" run reached from both sides → warning/blocking ("'-' ở E13..G13 nằm giữa hai gối có thép — không rõ nối từ phía nào"), or give the chain to one side (Excel "ditto" convention = the left neighbour, which is what DY14 shows: G → I → K). Add a test.

### M2 — The Ø < 16 gate is not actually tested
[KataDy14DrawingTests.cs `Bottom_main_bars_thinner_than_16_are_never_cranked`](../../../HPRebar/HPRebar.Core.Tests/KataRebar/KataDy14DrawingTests.cs)

E = 500, Ø14: e/H = (100 − 14)/500 = 0.172 > 1/6, so the ratio alone already cuts; the test passes even with the diameter clause removed. Use a support where the ratio passes (e.g. E11 = 600 → 86/600 = 0.143) and assert 3 bars for Ø14 and 2 for Ø16. Also uncovered: Ø exactly 16 (the `+ 1e-6` side), e/H exactly 1/6 (e.g. step 100, Ø16, H 504), `CrankMinDiameter` = 0 / large via `KataSettings` → `KataDetailingRuleBuilder`, |step| < Ø (negative e → crank). No test references `CrankMinDiameter` or `Cranks`.

## Low

- **L1 — Midpoint direction of `RoundNearest` unpinned.** [KataDetailingRules.cs:67](../../../HPRebar/HPRebar.Core/KataRebar/Models/KataDetailingRules.cs#L67) `AwayFromZero`: L/6 = 975 (L 5850) → cap 1000, the bar gets *shorter*. No drawing evidence for a midpoint (941.7/916.7/1158 only). fp is exact for L multiples of 150 (checked), measured non-integer L is fine. Add a test once Kata's choice is known. Also [KataSettings.cs:26](../../../HPRebar/HPRebar.Core/KataRebar/Models/KataSettings.cs#L26) still says "reaches round up, bars only grow" — no longer true for the L/6 cap.
- **L2 — Settings migration: no issue.** A file without `CrankMinDiameter` keeps 16 ([KataSettingsJson.cs:42-47](../../../HPRebar/HPRebar.Core/KataRebar/Parsers/KataSettingsJson.cs#L42-L47)); a leftover `SoffitCrankMaxStep` key is ignored; NaN/negative/∞ → 16 ([:93](../../../HPRebar/HPRebar.Core/KataRebar/Parsers/KataSettingsJson.cs#L93)). A user's customised old step limit is dropped silently — acceptable, the rule changed.
- **L3 — Misleading anchorage warning at a cut step.** [KataMainBarLayout.cs:55](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataMainBarLayout.cs#L55) reports `LegRoom(support)` (beam depth) for the step end, while the real limit is `DeepLegRoom` (shallow bar). Now reached far more often (every Ø < 16 step).
- **L4 — Narrow "-" end support uses the main-bar diameter for its level.** [KataTopLayerStack.cs:32](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataTopLayerStack.cs#L32): at a "-" support row 13 has no items, so rows 14–16 and the bottom-leg clearance are stacked under `mainD` even when the chained row-13 bar is thicker (Ø25 over Ø18 main → gap short by 3.5 mm). Pre-existing, now spans more supports.
- **L5 — Stale text.** [docs/codebase-summary.md:228](../../../docs/codebase-summary.md#L228) still says row 20 `2f12` = 1 layer and `SideBarTieSpacing`; [docs/specs/kata-beam-rebar-rules.md:20](../../../docs/specs/kata-beam-rebar-rules.md#L20) K2 still names `SoffitCrankMaxStep` (K13 says it replaces K2, OK, but the symbol no longer exists); test comment [KataSteppedBeamEdgeTests.cs:121](../../../HPRebar/HPRebar.Core.Tests/KataRebar/KataSteppedBeamEdgeTests.cs#L121) "Steps of 150 (> 100, cut)" — now cut because 128/400 > 1/6. `Notation` in [KataSideBarLayout.cs:165](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSideBarLayout.cs#L165) is dead after the odd-count warning went.
- **L6 — Settings dialog message** ([KataSettingsViewModel.cs:69](../../../HPRebar/HPRebar/KataExport/ViewModel/KataSettingsViewModel.cs#L69)) does not mention the crank diameter when it is the rejected field.
- **L7 — Row 20 has no layer-count sanity check.** `10f12` → 10 layers at (zTop − zBottom)/11, no spacing check ([KataSideBarLayout.cs:64](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSideBarLayout.cs#L64)). Before K15 the same text gave 5.

## Checked, no finding
- `Cranks`: H = 0 / cantilever → cut (then the cantilever branch drops the cantilever's own bar as before); negative steps via `Math.Abs`; |step| < Ø → e < 0 → crank (good: tiny steps are bent, Ø ≥ 16 only); Ø ≫ 16 fine; e/H exactly 1/6 → crank (`+ 1e-9`); NaN → cut.
- Crank run 6·|step| exceeds H when 6e < H < 6·step (spills ≤ 6Ø into the deeper span) — matches DY14 E 500 (crank 6100 → 6700, face 6600).
- Old 100 mm rule: no code reference left (`SoffitCrank` grep → docs only).
- Row 20 round trip: Kata Export writes only B3..B10 and rows 11/19/21/22/23 ([KataRowBuilder.cs:61](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataRowBuilder.cs#L61)); the preview shows "-" for row 20; read-back reads the sheet, never Revit side bars → no "2f12 means one layer" round trip exists.
- K16 loops terminate (bounded by 0 / `SpanCount`); "-" at support 0 or last ends the chain there; cantilever tip → cover stop; layer > 0 "-" still warned and not drawn.
- `RoundNearest` with a large user fraction can make the two cut points cross; `Add` refuses `xEnd − xStart < 1` with a warning ([KataSpanBottomBarLayout.cs:194](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L194)).

## Unresolved (user)
- H1 and M1 need a Kata-semantics choice: at a small cut step (Ø < 16), what does Kata draw? Which neighbour owns a "-" run lying between two supports with bars?

**Status:** DONE_WITH_CONCERNS
