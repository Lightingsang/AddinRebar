---
name: clean-code-wave-refactor-review-checks
description: Recurring checks for HPRebar clean-code Wave 2+ "long method → steps" refactors pinned by characterization hashes
metadata:
  type: project
---

Wave refactors (plan 261003-2133, REFACTORING_PLAN §2a) are pinned by characterization hashes; the hashes only cover
the scenarios given, so equivalence outside them must be proven by reading.

**Why:** first Wave 2 review (2026-10-03, ac4604b/10b01ee foundation mesh, 16d2d35/2617a5d beam support top bars)
found the code equivalent but the net fail-open in places a regression could slip through.

**How to apply:**
- Rewritten guards: `x > 0.0` → `x != 0.0` (signed encodings like `HookRise`) differ for negative/NaN — check the
  validator actually excludes them (FoundationValidationCalculator lets NaN through: `NaN <= 0` is false).
- `HPRebar.Core.Tests/CharacterizationText` walks public *properties* only: public fields, Guid/char/decimal/short
  hash as `{}` or scale only (fail-open); DateTime-like self-returning props throw at depth 8 (fail-closed, fine).
- Doubles rounded to 1e-6, `-0` prints "-0" on net8 — sign-of-zero changes fail the hash (strict, acceptable).
- FM5: extracted private helpers/records often land between a public method and its public overload — check order.
- Behaviour quirks preserved by the refactor (e.g. beam layer-2 z from Layer1Diameter when Layer1Count == 0) are
  logged as B-xx candidates, never fixed in the wave commit.
- Differential probe beats reading: `git show <sha>^:<file> | sed` the class to `OldX` in namespace `Probe`, scratch
  net8 console with `ProjectReference` to `HPRebar.Core.csproj`, random inputs incl. NaN/±∞/0 supports, compare
  bit-exact (`DoubleToInt64Bits`) + exception type/ParamName/message. 2026-10-04 beam main bars/stirrups: 200k/300k
  cases; caught `0 * spacing` / `startX + 0*∞` = NaN where old code wrote the literal (only +∞ spacing reaches it).
- Spacing/length guards written `x <= 0` let NaN/+∞ through (Core and `BeamRebarSession.Validate`) — NaN start offset
  surfaces as `ArgumentOutOfRangeException(capacity)` from `new List(count)`.
- Beam bottom splice = `Supports[Count/2]` (min 1): 2 supports → lap over the end column (piece still > stock);
  left cantilever → lap at the cantilever root (80 mm stub + piece reaching into the cantilever); < 2 supports → index
  crash. Pinned by `cantilever-left-spliced`; log as B-xx, never fix inside a wave.
  Fixed 47a1465 (nearest-middle interior support, L/4 fallback). Review facts: nearest-middle != Count/2 for any
  3-span with L1 > L3 (6/6/5 moves S2→S1) — "2/3/4-span unchanged" claims are false for asymmetric stacks; still ONE
  splice per run, so 3×(6/5/6) gives a 12 445 mm piece and a single span > ~13.8 m c/c overflows at L/4 (defaults
  Ø20/40Ø/1.3/hook 350); `CantileverEnds` flags a single cantilever span as BOTH ends (one framing element tip→root→end
  = 400 mm bottom stub). Probe: scratch net8 console + HintPath to bin/Debug.R26 Core dll, splice = bar0.EndX − lap/2 + stagger/2.
- Wave 3 (move to Core) column checks, 2026-10-04 (7ed4d65/144152d): a `catch (ArgumentOutOfRangeException)` swapped
  for `IsLayoutValid` is equivalent only while `ColumnSpecRules.IsLayoutValid` and `BarLayoutCalculator`'s guards
  encode the same rule (no test pins it); `SpliceCalculator.ComputeUpperPositions` needs splices.Count == bars.Count
  *exactly*; `layout.BarCount` == bars.Count only because `ColumnSpecEditor.ToLayout` zeroes Nx/Ny for circles
  (record defaults Nx=Ny=2); `layout.StirrupDiameter` duplicates `StirrupBarType.DiameterMm` at every call site.
  Leftover column dups: `ElevationPainter` rebuilds StirrupSpec + RunsFor, `SectionPainter` still try/catches.
- Unit-conversion moved before the maths (ft→mm first): positive scaling is monotone but NOT strictly — two lengths
  1 ulp apart can collapse to equal, flipping a strict `>` "first wins" tie rule. Python probe 2026-10-04 (foundation
  plan frame): ~57 % of float-located squares already have ulp-unequal sides (axis = noise), ~0.3 % flip after the
  move; spec has separate X/Y diameters so a flip changes the layout. Log as B-xx (no tie tolerance), don't claim
  "order preserved". Also check public Core `Fit(localX…)` style APIs normalise their direction input.
- Wave 3 Beam supports (BeamSupportLayout, 2026-10-04, uncommitted at review): `SupportType.Column == InteriorColumn == 1`
  — a refactor that drops the explicit Column→InteriorColumn mapping is value-identical but breaks silently the day
  the enum is de-aliased (the natural B-29 fix); ask for the explicit `is Column or ExteriorColumn`. Merge = 100 mm
  *grid* (banker's rounding): 6040/6060 stay two, 51/149 merge; a split duplicate inflates the `pieces+1` quota and
  starves the stand-in at a bare joint. Validator `AreSpansContiguous` lets overlapping pieces through; run end =
  End of last-by-start piece. Non-line beams never reach FindSupports (validator rule 3 + reader NRE) → 4000 mm dead.
  Differential probe: tuple copy of old logic vs `Arrange`, 300k random cases incl NaN/±∞, 0 diffs.
  B-31 fix (2026-10-04, uncommitted at review): greedy anchor merge `x - lastKept >= 100`. Probe: new count <= old
  except exact-100 pairs in an even (closed) banker's bucket; the same element found by two pieces has bit-identical
  centres, so real merges are *different* elements (wall + column) — survivor = lower centre, not widest/column.
  Kept supports 100..width apart still overlap → B-33. A NaN centre sorts first (Comparer<double>) and now swallows
  every real support (old: own group). Fix commits also close the audit row + add a REFACTORING_LOG entry (B-34 did).
- Wave 3 Beam cut stations + span assembly (BeamSectionStations / BeamSpanAssembly, 2026-10-04, uncommitted at review):
  400k-case probe 0 diffs. Facts: `SectionViewCreator`'s `is not null` skip is dead (`ViewSection.CreateDetail/CreateSection`
  throw, RevitAPI.xml) so dims/tables walking SectionViews by a parallel cut index cannot misalign today (latent only;
  B-19 is a different defect: table rows by cut index). Clear-length fallback (`<= 0` → centre length) keeps startX at the
  left face → span EndX = right centre + left half-width (overruns). `BeamSpan.Cover` 25 is overwritten per run by
  `ContinuousStack.WithCover(spec.Stirrups.Cover)` (B-01). Session offers sectionsPerSpan {2,3} only.
- "Compare in double, cast after" fixes (B-34, 2026-10-04): in-range bit-exact (900k cases net8/net10/net48), but watch
  NaN drift: `Math.Max(0.0, NaN)` = NaN (old `(int)` + `if (<0) 0` mapped NaN → 0; use `x > 0 ? x : 0`), and moving
  the `+ 1` inside the double before the cast turns `(int)NaN + 1` into `(int)NaN` (count 1 → 0 on net10). Casts:
  net8/net48 x64 overflow/NaN → int.MinValue; .NET 9+ saturate (NaN → 0, big → MaxValue, so `+1` still wraps).
  Probe can add `net48` with `<PlatformTarget>x64</PlatformTarget>` (swap GetValueOrDefault/IsFinite). Same overflow,
  no limit at all: `BeamSideBarCalculator` row count + cross-tie count (spacings unvalidated in the window).
- B-39 Beam fix (2026-10-04, uncommitted at review): `ComputeHangingStirrupStations` already returned nothing for NaN
  (comparisons false) and production passes finite min/max, so only +∞ with default ±∞ bounds discriminates — NaN test
  cases there pass on old code. Window-only tightening (0/negative lap/stock refused) is beyond "refuse NaN/∞" — B-36's
  "greater than zero" was a user decision for side spacings only; ask. `BeamElevationPainter` lap mark reads raw
  `LapFactor`/`MaxStockLength` (preview runs before validation). Core `FirstProblem` can't take add-in `BeamRebarSpec`
  (Revit types) — a PCC-060 parameter object needs a new Core record → AUD row, not in a fix.
- Related: [[kata-rebar-review-checks]]
