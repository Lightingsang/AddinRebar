# Kata section rules read from T2-DY7.dwg (runs 726–736, read-only)

27 sections: DY7 1-1..9-9 (titles y −20751/−20601), DY14 E350 (−27409.7/−27259.7), DY14 E500 (−34250/−34100).
Coordinates: x from beam centre, z from beam top (mm, TL 1/25). All b = 300, slab 120, Ø18 mains, Ø8 hoops, Ø12 sides.

## Geometry
- Outline (kata_net thay): slab top (−270,0)→(270,0); (−270,−t)(−150,−t)(−150,−h)(150,−h)(150,−t)(270,−t). Slab reach 120.
- Break (kata_dim) at x = ±270: (x,−t−20)(x,−t/2−20)(x+20,−t/2−20)(x−20,−t/2+20)(x,−t/2+20)(x,20) — same shape both sides.
- Hoop (kata_thep dai): centreline at cover c = 25 from faces (xc = b/2 − c = 125); corner bulge −0.414214 r = (d+ds)/2 = 13;
  hook start (xc−r−5ds, −c−5ds) = (72,−65) → (xc−r,−c) bulge −0.668179 (135°) → (xc,−c−r) … → (xc,−c−r) → (xc−5ds, −c−r−5ds) = (85,−78).
- Bars (kata_block_THEP, scale d): top layer1 z = −(c+(d+ds)/2) = −38, layer2 −81 (clear 25); bottom mirrored;
  x ±(xc − (d+ds)/2) = ±112, middle 0. Side Ø12 x ±115; z: evenly between −(c+ds+dTop) and −hGroup+(c+ds+dBot)
  (DY7 −250 in h 500 and h 600; DY14 h 600 two layers −217 / −383; real centres −214 / −386).
- THEP block: circle r 0.5 d (polyline width d/12) + cross ±0.5 d and diagonals ±0.35 d.
- Layer-2 tie: (−(xc−2ds−5.5ds), z+16)(−(xc−2ds), z+16) b1 (−(xc−2ds), z−16)((xc−2ds), z−16) b1 ((xc−2ds), z+16)((xc−7.5ds), z+16)
  = (−65,z+16)(−109,…)…; straight below the bars, tails above (top and bottom layer 2 alike).
- Side tie: (65,z−16)(109,z−16) b1 (109,z+16)(−109,z+16) b1 (−109,z−16)(−65,z−16): straight above, tails below.

## Dimensions / title
- b: (−b/2,−h)–(b/2,−h), line z = −h − 300, −h − 450 when a second bottom tag row exists.
- Vertical at x −b/2 − 375: (−h → −t), (−t → 0); overall at −b/2 − 506.25 (−656 measured): (−h → 0).
- Title kata_block_TD at (0, dimZ − 250): "n-n" + "TL: 1/25".

## Tags (kata_block_KHT, scale 25) and leaders
| What | Arrow | Path | Insert | Text |
|---|---|---|---|---|
| top layer-1 corners | _DotBlank 1.5 | each bar ↑ z 100 → left | leftmost − 200 | 2Ø18 |
| top layer-1 middle | _DotBlank | bar ↑ z 175 → right | rightmost + 200 | 1Ø18 |
| top layer 2 | none + circles r18.75 + stubs | leftmost bar ↓ 50 → right | b/2 + 370 | 3Ø18 |
| top layer-2 tie | closed | (−xc'/2, tie straight) ↑ 295.5 → left | −b/2 − 127.875 | Ø8a500 |
| bottom corners | _DotBlank | ↓ −h − 125 → left | leftmost − 200 | 2Ø18 |
| bottom middle | _DotBlank | ↓ −h − 272 → right | +200 | 1Ø18 |
| bottom layer 2 | none + circles | leftmost ↑ 50 → right | b/2 + 250 | 3Ø18 |
| bottom layer-2 tie | closed | ↓ −h − 301.25 → left | −b/2 − 127.875 | Ø8a500 |
| side bars 1 layer | _DotBlank 1 both ways between bars; tag _DotSmall from right bar → right | — | 520 (h500) / 425 (h600) | 2Ø12 |
| side tie 1 layer | closed | (−34.333, z+16) ↑ z+91 → left | −356.375 | Ø8a500 |
| side bars 2 layers | feet (59.333, z1), (59.333, z2) → joint mid → right | — | 481.5 | 2x2Ø12 |
| side ties 2 layers | closed, feet (−65.333, zi+16) → joint mid → left | — | −362.375 | 2xØ8a500 |
| hoop | closed | (−xc, zH) → left | −xc − 238 | Ø8a100 |

Hoop tag zH: no side bars (−t − h)/2 (−310 h500, −235 h350); with side bars −0.75 h + 25 − 37.5 (n − 1)
(−350 h500, −425 h600, −462.5 h600 two layers). Side-tag insert, one layer: b/2 + 370 − 0.95 (h − 500).
These two are fits of 5 / 2 samples — unknown Kata rule.
