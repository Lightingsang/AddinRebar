# Code review — Kata Export row 20 (2026-10-05), 6.5/10 → fixed

Rule: source doc `04_quy_dinh_thep_dam.md` l.1171–1190 (support = crossing beam `bxh` / `b` when h = B5; span = side bars and/or width `300;2f12`).

| # | Finding | Fix |
|---|---|---|
| H1 | `ComposeSpanCell` dropped a typed `0` (side bars off) | only a positive number in the first token is a width; `0` kept (tests `"0"`, `"300;0"`) |
| H2 | support cells overwritten with "" when the collector finds no crossing beam (one-sided beams at perimeter columns missed — GIẢ ĐỊNH CHƯA XÁC MINH) | no crossing beam → cell kept as typed (`null` in Row20); location-line fallback in `KataSupportCollector` NOT done |
| M1 | Kata may carry an empty span width over | once any span differs from B6, every span's width is written |
| M2 | preview shows built value, not merged | open (preview shows width only) |
| M3 | only `;` separator | `; + ,` like the parser |
| Low | culture on read-back, lower bound, tolerance name, duplicate Whole | `KataColumnLetters.CellText`, `GetLowerBound(0)`, `SameSizeToleranceMm`, `KataFormat.Whole` public |

Tests 1304/1304. Live (copy of Dam kata test.rvt + copy of KataB1.xlsm, user's files untouched): row 20 after export
`400x500 | 500 | 400 | 500;0f12 | 400 | 500 | · | 300 | 400x800 | 300 | 400` — supports kept (model has no crossing beams), spans written, `0f12` kept.
