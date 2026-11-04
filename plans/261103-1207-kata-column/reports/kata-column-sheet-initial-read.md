# Sheet `Cot` — initial read (2026-11-03)

Source: copy of `C:\kata_pro\Kata.xlsm` read with openpyxl (original not opened). Sheets: Vach, Cot, Dam, ThepDamAllMBKC,
save_info, Thong_ke, Thong_ke_QS, To_hop_thep, Kho_thep, Kata_QS, BEAM DESIGN, SLAB DESIGN, In_tem_thep, Data, Gan_tag_block.
`Cot` used range A1:BA216. No sheet `Thep cot` (spec `SPEC_VeCot_Kata.md` R-VECOT-06 wrong).

## Header (sample values)

| Cell | Label (col A / D / G) | Sample |
|---|---|---|
| C1 | Bề rộng dầm (mm) — `A3` says "Tên vách" when D1>0 | 800 |
| C2 | Chiều dày sàn (mm) | 200 |
| C3 | Tên cột | C1 |
| C4 | Số cấu kiện | 1 |
| C5 | Cao trình đáy đài móng (m) | -5.5 |
| F2 / F3 | Tỷ lệ bản vẽ cột / mặt cắt (DV lists G1:J1, G2:J2; comment "In layout") | 1:30 / 1:20 |
| F4 | a bảo vệ (mm) | `30/100` |
| F5 | Đoạn bẻ ke (d) | 15 |
| G3..K3 | unlabeled flags | False False True 2 True |
| H4 | Đai bao | d10a100/200 |
| H5 | Đai gc | d8a400 |

## Storey rows (headers rows 7–9, data from row 9)

| Col | Meaning (row 7/8/9 header) | Sample row 10 |
|---|---|---|
| A | TẦNG (A9 = `PILECAPS`) | BASEMENT |
| B | Chiều cao (m) (B9 = 1.5 pile cap) | 3 |
| C / D | b / h (mm); comment C11: negative = shrink one side (edge column), positive = shrink both sides | 1000 / 800 |
| E / F | bar count face b, layer 1 / 2 (comment E8: `20f30` = 20 bars Ø30) | 7 / 7 |
| G / H | bar count face h, layer 1 / 2 | 7 / 7 |
| I | Ø (mm), `25/20` seen | 25 |
| J | Vị trí nối thép (mm) | empty |
| K | Chiều cao dầm (mm) | 600 |
| L | Đai bao (đk+kc) | d10a100/200 |
| M / N / O | Đai □ Ver: tỷ lệ / đk+kc / số lượng | 0.5 / d10a400 / – |
| P / Q / R | Đai □ Hor: tỷ lệ / đk+kc / số lượng | 0.5 / d10a400 / – |
| S / T | Đai C Ver: đk+kc / số lượng | d10a400 / 2 |
| U / V | Đai C Hor: đk+kc / số lượng | d10a400 / 2 |

Rows 10–15 sample: BASEMENT 3 m, FLOOR 1 3.5, FLOOR 2 3.9 (Ø `25/20`, K 400), FLOOR 3 4.6 (Ø20), FLOOR 4 3.3, FLOOR 5 3.3.

## Helper area (formulas, not input)

X22 "Mặt cắt bố trí thép chịu lực tầng", AA22 data validation list A10:A216 (section picker); AJ..AX formulas compute
bar coordinates per layer (`Tọa độ thép b1/h1/b2/h2`) from VLOOKUP of the picked storey — useful to cross-check bar
positions in phase 3.

## Open (phase 0 resolves by driving VC)

- Is the sign rule for C also valid for D (h)? Which side is "one side"?
- J values and meaning (mm above floor? empty = default)?
- `25/20` = Ø per face b/h or layer 1/2?
- G3..K3 flags, F4 `30/100` second number, ratio columns M/P.
- Does VC read the active workbook or a file; prompts/dialogs; insertion point.
