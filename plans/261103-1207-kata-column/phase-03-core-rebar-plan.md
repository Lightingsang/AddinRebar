# Phase 3 — Core rebar plan from sheet `Cot` (parity with VC)

## Overview
Priority high. Status 🟡 2026-11-03: implemented (`KataColumnSpecReader`, `KataColumnSectionPlanner`, `KataColumnHoopPlanner`, `KataColumnBarPlanner`), 44 tests, review pending. Pure `HPRebar.Core/KataRebar/ColumnStack/`: read `Cot` cells
(`KataCellTable`) → model → bar plan (positions on the column's own faces, per storey) asserted against the VC dumps in
[reports/variants/](reports/variants/).

## Rules read from v00 (Kata sample, VC drawing) so far
- Section, rectangle, layer 1: E bars on each b face, G on each h face, corners shared (E=G=7 → 24); centres at
  cover + hoop Ø + bar Ø/2 from the faces (F4 first number = cover to hoop), evenly spaced.
- Layer 2 (G3 off): a ring one bar-spacing inside layer 1 on the same grid; with E=F=G=H=7 it holds 5 + 5 + 3 + 3 = 16
  (40 bars total). Counts for other F/H values not derived yet. G3 on: layer 2 at one bar Ø inside layer 1.
- Round (D = 0): E + F bars on one circle (r = Ø/2 − cover − hoop − bar/2).
- Elevation, starter bars: foot F5·d (375 = 15×25) at cap bottom + F4 second number (100), up to lap end in storey 1.
- Laps: 30d (`30d=` dims), 50 % in two groups 250 mm apart; lap start above each storey base = J, else Kata default
  (sample: 400 / 550 / 200 / 1550 / 250 / 850 — rule to derive: beam depth? zone? next variants).
- Crank: bar shifts one bar Ø over 1:6 below the lap (e.g. 65 → 40 over 150).
- Last row = stub; top anchorage never drawn by Kata (Revit: user rule = anchor F5·d at the roof).

## Lap / bar-cut rules from 31 probe variants (2026-11-03, [reports/variants3/](reports/variants3/), `tools/bars.py`)
- **I "a/b"** = group A (first 50 %) Ø a, group B Ø b (labels "8Ø20+12Ø25"); lap = 30 × Ø of the bar above.
- **J given:** group A lap starts at storey base + J exactly (j-all0, j-all500); group B lap starts at
  A start + lap_B + 10·Ø_B (Ø25: +1000, Ø20: +800). Deterministic → implementable now.
- **J empty:** Kata cuts bars to the stock length 11 700 / n (n = 2, 3, 4 → 5850 / 3900 / 2925; 2.00 / 3.00 / 4.00 in
  every A chain of v00, h-all3000, h-all4500, d-all25, k-all800) and chooses n per bar so each lap lands low in the
  next storey. K does not matter (k-all800 = v00). Window and tie-break not pinned: h-all3000 takes 4 then 3 where 3 then
  4 lands on the same final lap; group B then follows A by the J rule offsets with non-stock lengths (3552, 5092).
  Group A starter: n = 4 in all probes (2925 incl. foot), even where n = 3 would fit (h-all4500).
- **Decision (user 2026-11-03): Kata Column Rebar requires J on every storey** (lists the storeys without it); no
  guessing of Kata's optimiser, so VC and Revit lap at the same place.
- **Layer 2 (s-*, G3 off) — derived:** not a ring. Layer 1 puts E bars on each b face and G on each h face on an even
  grid (corners shared; slot pitch (b − 2a)/(E − 1), (h − 2a)/(G − 1), a = cover + hoop Ø + bar Ø/2). Layer 2 adds a
  row parallel to each b face on the second grid row (index 1 and G − 2) with F bars, and a column parallel to each h
  face on the second grid column (index 1 and E − 2) with H bars, both on layer-1 slots chosen symmetrically from the
  outside in (pairs 0/N−1, 1/N−2 …, the centre when the count is odd); positions already taken are not doubled.
  Proven on E=G=7 with F/H ∈ {0, 2, 3, 4, 5, 6, 7}: 24, 24, 26, 28, 32, 36, 40 bars. **Corrected 2026-11-04 ([reports/variants5/](reports/variants5/), `tools/sections.py`, 9 more VC drawings):** each
  count includes the line's ends — F: its two layer-1 bars, F − 2 go symmetrically on the slots between; H: its two
  layer-1 bars, plus the two layer-2 row slots on it when F ≥ 3 (drawn or not), H − 2 / H − 4 between. Nothing between
  → the line is absent, ties included (F = 1, 2; H = 3, 4 beside F ≥ 3). Matches all 17 drawings (24…40 bars, bar
  positions and ties of both directions) and E = 4 / F = 2 / G = 5 / H = 3. F = 1 with H = 3: VC draws nothing (Kata error).
  Open: odd count on an even slot count; G3 (ghép sát) puts the row one bar Ø inside layer 1 instead.
- **Hoops (H4 when "đai tự động" I3 is ticked) — derived on v00, z-*, live ES-0001:** first hoop 50 above the storey
  base, last 50 below its top. Dense zone (s1) from base + 50 to base + Ld and from soffit − Ld to the top; with K3
  ("đai xuyên dầm") the top zone runs through the beam (third value of "d10a100/250/50" = spacing inside the beam, else
  s1); K3 off stops it at soffit − 50. Middle zone (s2) from bottom zone end + s2 to top zone start − s2, dropped when it
  does not fit (basement 2400 clear: one zone). **Ld = max(max side, Hclear / 6, 450) rounded to the nearest 50** (TCVN 9386
  critical zone): 1000 (1000×800), 1050 / 1100 (7 m storeys, 6400 / 6600 clear), 500 / 600 / 700 / 450 (300×300 at
  2900 / 3500 / 4200 / 2700 clear), 450 (2400 clear) — variants4 hoop-tall / hoop-small. H3 ("đai dày tại
  vị trí nối") adds dense zones over the laps (z-g2-on).
- **Ties with I3 ("đai tự động") ticked — derived (variants4 tie-*):** rows M–V are ignored; Ø and spacing from H5.
  Per direction (bar slots 0…N−1 along b for vertical ties, along h for horizontal ones) restrain slots k and N−1−k for
  k = 2, 4, 6 … (k = 1, 3, 5 … when that direction has a layer-2 row/column) while k < N−1−k → closed □ around the
  pair (two pairs → □(k1,k2) and □(N−1−k2, N−1−k1)); k = N−1−k → a C tie on the centre slot. Matches E = 3, 4, 5, 6,
  7, 8, 9, 11 with and without layer 2. The outer hoop (H4) holds slots 0 and N−1. Round columns: hoop only.

## Implementation choices (where the drawings say nothing)
- Stagger groups: within each layer, bars alternate A/B around the section (Kata's section labels give 12Ø25 + 8Ø20 per
  group = half of each layer; which bar is which is not drawn). Group A laps with I's first Ø, group B with the second.
- Bars sit on the real Revit faces (sheet b/h only for the sheet); a lower bar outside the upper storey's bar zone is
  cranked 1:6 from the soffit when the move ≤ 75 mm, else every bar of that storey is cut 30+10+Ø/2 under the top with
  an F5·d leg inward and the storey above starts one lap below its base (user decision).
- Top storey: F5·d leg inward under the roof unless `TopContinues`.

## Next
1. VC variant matrix (dedicated acad, user's AutoCAD closed): F/H ∈ {0, 2, 3, 4, 5}, E/G ∈ {3, 4, 5}, J empty with
   K ∈ {300, 500, 700} and heights ∈ {3.0, 4.5} to pin the default lap start, hoop zones vs `d10a100/200`, ties M–V.
2. Core model + planner + xUnit against the dumps (± 1 mm positions, ± 1 hoop per zone).
