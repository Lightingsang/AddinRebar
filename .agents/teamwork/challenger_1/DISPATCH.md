# Dispatch: Challenger 1 (Core Geometry & Parser Stress Testing)

- **Role**: teamwork_preview_challenger
- **Task**: Adversarially stress-test `KataBarNotationParser` and `KataRebarCalculator`.
- **Authoritative Request**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under ## 2026-09-27T15:57:37Z)
- **Scope**:
  1. Stress-test `KataBarNotationParser` against extreme and malformed inputs:
     - Empty strings, whitespace, single characters, numbers without 'f' or 'd', mixed separators (';', '+', ','), negative step drop values, malformed stirrup spacing ('a', 'a0', 'a-100', '150').
  2. Stress-test `KataRebarCalculator` against boundary geometric configurations:
     - Zero spans, single span, 10+ spans, extreme cantilever overhangs (left only, right only, both).
     - Unequal spans ($L_1 = 2000\text{ mm}, L_2 = 12000\text{ mm}$).
     - Shallow beams ($h = 200\text{ mm}$) vs Deep transfer beams ($h = 2500\text{ mm}$).
     - Extreme covers ($c = 50\text{ mm}$, $c = 10\text{ mm}$).
     - Multiple layers of top extra bars (Layers 1-4) verifying physical non-overlap in $Z$.
  3. Verify all generated polylines pass `Polyline3.Simplify(1.0)` with no segment shorter than $1.0\text{ mm}$.
  4. Write test harness or run tests to empirically prove stability and correctness.
  5. Provide explicit gate verdict in `handoff.md`: **APPROVE** or **REQUEST_CHANGES**.

## 2026-09-27T16:49:34Z
Adversarially stress-test `KataBarNotationParser` and `KataRebarCalculator`:
1. Test extreme notation inputs: malformed tokens, weird spacings, missing diameters, step drops.
2. Test extreme geometric configurations: single span, double cantilever, deep beam (h=2000mm), shallow beam (h=250mm), 4-layer top extra bars, unequal spans (2m vs 12m).
3. Test that all polylines are simplified (no segment < 1.0mm).
4. Run or write empirical stress tests to prove absence of crashes, infinite loops, or NaN/infinite coordinates.
5. State your explicit gate verdict (**APPROVE** or **REQUEST_CHANGES**) in `handoff.md` and send a message.
