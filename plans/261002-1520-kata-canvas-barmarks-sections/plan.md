---
title: "Kata Export canvas: số hiệu Kata, tag mặt đứng, mặt cắt"
description: "Số hiệu thép theo Kata (thanh giống nhau chung số) → Schedule Mark Revit + tag mặt đứng kiểu Kata (đợt 1); mặt cắt 1-1… (đợt 2)."
status: in_progress
priority: P1
branch: RebarVersion1
tags: [revit, hprebar, kata, canvas, bar-marks]
created: 2026-10-02
related: [261002-0930-kata-dy7-match, 261002-1130-kata-layer-spacer-ties, 261002-1420-kata-dy14-match]
---

# Số hiệu Kata, tag mặt đứng, mặt cắt

Hợp đồng: /grill-me 2026-10-02 (user "implement"). Nguồn: `Documents\T2-DY7.dwg` (block `kata_block_KHT` SH1/SH2/DKKC1, `kata_block_SECBAL` DETAIL), đọc qua MCP AutoCAD.

## Bằng chứng (verified, DWG)
| Dầm | Số hiệu |
|---|---|
| DY7 | 1 trên · 2, 3 dưới · 4 C13 · 5 C14 · **6 E13 = F17** (cùng 1Ø18/3Ø18 thẳng 4650) · 7 E14 · 8 G13 · 9 G14 · 10 D18 · 11 D17 · 12 F18 · 13 cốt giá · 14 mọi móc C · 15/16/17 đai nhịp |
| DY14 | 1 · 2/3/4 dưới · 5–10 C13 C14 E13 E14 G13 G14 · 11–14 D18 D17 F18 F17 · 15 cốt giá `2x2Ø12` · 16 móc C · 17/18/19 đai, **nhịp 4 = 19** |
Tag mặt đứng: gối gộp `1+5 2Ø18+1Ø18` (trên) và `2+11 2Ø18+1Ø18` (dưới, hàng 18); lớp 2, thép chủ giữa nhịp, cốt giá tag riêng; đai `17 Ø8a100` theo vùng; móc C chỉ ghi ở mặt cắt.

Plan cũ sai: Schedule Mark/Partition đã có (`KataRebarStamp`, `NUMBER_PARTITION_PARAM`); không có `KataRebarCreator.cs`; thiếu luật "thanh giống nhau chung số"; `BarMark` nội bộ dùng để gom bộ Fixed Number nên không thay được.

## Đợt 1 (đang làm)
| # | Việc | Status |
|---|---|---|
| 1 | `BarNumber` trên `KataRebarCurve`/`KataBarSet`/`KataStirrupZoneResult`; `KataBarNumbering` (thứ tự Kata, chung số khi cùng Ø + hình + kích thước ±1 mm, kể cả ảnh gương) | done |
| 2 | Revit: Schedule Mark = số Kata (thanh, bộ thanh, bộ đai); `BarMark` nội bộ giữ để gom Fixed Number | done |
| 3 | Canvas: `KataBarTagBuilder` + `KataElevationBarTagPainter` (tag gộp ở gối, hàng trong riêng phía trên, tag trượt tránh đè), checkbox "Hiện số hiệu" | done |
| 4 | Test 871/871 (golden DY7/DY14 số + tag), build R26/R25/R24, review ([report](reports/code-review.md): M1 gương, M2 dung sai, M3 đường dẫn — đã sửa), live ([report](reports/live-verify.md)) | done |

Giả định CHƯA XÁC MINH: mọi móc C cùng Ø chung số; đai U/C trong (hàng 25–44) đánh số sau móc C, trước đai ngoài.

## Đợt 2 (sau)
Mặt cắt `MẶT CẮT n-n`: 3/nhịp (nhịp < 1000 mm: 1 ở giữa), sát gối lùi L/10 tròn 25 từ mặt gối, giữa L/2 − 150 (L < 3000: −50) — gần đúng vị trí Kata (lệch 0–75 mm, cắt cùng tập thanh); ký hiệu vết cắt trên mặt đứng; số hiệu trên chấm thép.
