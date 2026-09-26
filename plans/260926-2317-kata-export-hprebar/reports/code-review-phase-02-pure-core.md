# Code review — phase 02 pure core (KataExport)

Date 2026-09-26. Scope: `HPRebar.Core/KataExport/**` (7 files, ~430 LOC) + `HPRebar.Core.Tests/KataExport/**` (3 files, 22 tests). Spec: [kata-cell-contract.md](kata-cell-contract.md), [phase-02](../phase-02-pure-core-and-tests.md).

**Score 7/10.** Contract (a) met and verified; conventions (c) clean; build/test (d) green. Weak on (b): no input validation, joint detection too eager, near-face slivers, gaps silent, most edge cases untested.

## Verification run

| Check | Result |
|---|---|
| `dotnet test HPRebar.Core.Tests` | ✅ 359/359 (22 new Kata tests) |
| `dotnet build HPRebar.Core --no-incremental` | ✅ 0 warnings, 0 errors |
| Edge-case probe (scratch console over HPRebar.Core, 25 inputs) | results cited below |
| Plan refs in code | none (only "Dynamo" = external tool name, OK) |

## Acceptance (a) — contract

All verified by tests + probe: support/span/joint order; full-width supports past beam end; merge Column > Foundation > Beam (probe: footing 600 beats girder, not covered by a unit test); span attrs by midpoint ([KataSegmenter.cs:111](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs#L111)); girder "bxh" from own support ([KataRowBuilder.cs:68](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataRowBuilder.cs#L68)); console pad 0/0/""/""/"" both ends and single ends; Reverse order + sign on row 19 offset, rows 21/23; header B3..B10; > 76 throws (boundary 75 ok, 77 throws); AwayFromZero, no "-0" (probe z −0.4, upper offset −0.3, grid −0.4 all → `0` / `"400;0"`).

## High

### H1 — No validation of NaN / Infinity / negative inputs → silently wrong sheet
[KataSegmenter.cs:14-26](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs#L14-L26), [Interval1D.cs:10-14](../../../HPRebar/HPRebar.Core/KataExport/Models/Interval1D.cs#L10-L14)
- `Math.Min/Max` propagate NaN; every comparison with NaN is false.
- Probe: piece `(6000, NaN)` → run = NaN → **both real columns dropped** by `Overlaps(run)`, output `0 | NaN | 0` (NaN goes to Excel).
- Probe: support `(-200, NaN)` → silently dropped, start becomes a console: `0 | 5800 | 400` (wrong numbers, no error).
- Header level NaN → B10 = NaN. Negative width/height accepted.
- Failure: adapter reads a missing param / degenerate projection as NaN → user's Kata gets plausible-looking wrong values.
- Fix: validate at `Segment`/`Build` entry — every station/size finite, sizes > 0, throw `ArgumentException` naming the element key (`KataBeamPiece.ElementKey`, `KataSupport.ElementKey`, grid name). Add tests.

## Medium

### M1 — Joints inserted at every piece endpoint, not only where one piece ends and the next begins
[KataSegmenter.cs:78-89](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs#L78-L89)
- Probe: long piece 0..12000 + short overlapping piece 3000..4000 → `400 | 2800 | 0 | 1000 | 0 | 7800 | 400` (two spurious joints, span split in 3).
- Probe: zero-length piece at 3000 inside 0..6000 → spurious joint column.
- Real source: duplicated beams, a crossing beam projected onto the axis (zero length) if the adapter lets it through.
- Fix: joint at `s` only if some piece ends at `s` AND another starts at `s` (± StationMm) AND no piece covers `s` strictly inside; drop/reject pieces with `Length < StationMm`; reject or warn on overlapping pieces.

### M2 — Gaps between pieces written silently; no warnings channel
[KataSegmenter.cs:111-112](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs#L111-L112), [KataSegmentModels.cs:26](../../../HPRebar/HPRebar.Core/KataExport/Models/KataSegmentModels.cs#L26)
- Probe: pieces 0..3000 + 3500..6000 → `400 | 2800 | 0 | 500 | 0 | 2300 | 400`: 500 mm gap becomes a span with the attributes of the nearest piece (first by order on a tie), plus two joints.
- Phase architecture lists `KataSheet.Warnings`; not implemented. Other silent drops: 2nd grid in one support (probe "A'" dropped), supports outside run, grids outside supports.
- Fix: add `IReadOnlyList<string> Warnings` to `KataSegmentation`/`KataSheet`; gap > StationMm → throw (run not continuous) or warn; dropped grids → warn.

### M3 — Sliver span + joint when a piece joint sits a few mm outside a support face
[Tolerance.cs:9-12](../../../HPRebar/HPRebar.Core/KataExport/Tolerance.cs#L9-L12), [KataSegmenter.cs:96](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs#L96)
- Probe: pieces split at 5798.5, column 5800..6200 → `… | 5599 | 0 | 2 | 400 …`; at 5797 → `… | 5597 | 0 | 3 | 400 …`.
- Beam drawn to a column face with a small modelling error produces a 2–3 mm span column + a joint column → Kata draws a phantom span.
- Fix: snap a joint into the adjacent support when its distance to the face < a named `JointSnapMm` (e.g. 50 mm; confirm with golden case e), or raise `MinimumSpanMm` and fold the sliver into the neighbouring span.

### M4 — Edge cases of acceptance (b) untested
[KataSegmenterTests.cs](../../../HPRebar/HPRebar.Core.Tests/KataExport/KataSegmenterTests.cs), [KataRowBuilderTests.cs](../../../HPRebar/HPRebar.Core.Tests/KataExport/KataRowBuilderTests.cs)
- Missing: gap between pieces, support entirely outside run, zero-length piece, touching supports, multiple grids in one support, joint at support face, NaN inputs, Foundation priority (`TestKataData.Footing` unused), start-only / end-only console (phase asks 4 combinations; only both-ends tested), exactly 76 columns, negative midpoint (−x.5 → away from zero) and −0, row 21 under Reverse.
- `KataGoldenTests.cs` not created — blocked on user golden files (P1), acceptable.
- Fix: add one test per case above, pinning the behaviour chosen in H1/M1–M3.

## Low

- **L1** Merge inconsistency: touching supports (gap 0) merge ([KataSegmenter.cs:56](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs#L56), tolerance 0) but a 0.5 mm gap does not → two adjacent support columns, and a grid in the gap is written to **both** (probe `"X" | "X"`, offsets 200 / −125). Use `Tolerance.StationMm` in the merge.
- **L2** Two sources for h₁: row 21 uses first piece by station ([KataRowBuilder.cs:30](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataRowBuilder.cs#L30)), B5 uses `Header.HeightMm`. Use `input.Header.HeightMm` so row 21 is relative to B5 by construction. Reverse keeps axis-first reference (probe: `"" | -200 | "" | 0 | ""`) — still open point 3/4 of the contract.
- **L3** Column count duplicates the padding rule ([KataRowBuilder.cs:22-24](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataRowBuilder.cs#L22-L24) vs lines 33/57); drift risk. Fine now.
- **L4** Third public `Tolerance` class (BeamRebar, ColumnRebar, KataExport) → CS0104 if a file imports two of the namespaces. Consider `KataTolerance`.
- **L5** Single zero-length piece with a support returns one support column instead of the "shorter than one span" error ([KataSegmenter.cs:35](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs#L35)); `Supports`/`Grids` null → NRE not `ArgumentNullException`. Covered by H1 validation.

## Conventions (c)
✅ file-scoped namespaces, `sealed record` models (`Interval1D` = `readonly record struct`, fine), nullable clean, files ≤ 136 lines, why-comments, no plan refs, tests `public sealed class` + sentence names like BeamRebar tests.

## Plan follow-ups (for lead)
- Phase 2 success criterion 1 met (test count baseline in phase file "334" is stale: 337 + 22 = 359).
- Criterion 2: regressions for index pairing (`SpanTakesAttributesFromTheBeamElementUnderItsMidpointNotFromItsIndex`) and girder section per support (`GirderSupportWritesItsOwnSectionInRow11`) present; h ≥ 900 upper-column detection belongs to the Revit adapter — no core test possible.
- `KataGoldenTests` pending golden files.

## Recommended actions
1. H1 input validation + tests.
2. M1 joint rule + M2 warnings/gap policy + M3 snap tolerance, each with a test.
3. M4 remaining edge-case tests; L1/L2 small fixes.

Status: DONE_WITH_CONCERNS
