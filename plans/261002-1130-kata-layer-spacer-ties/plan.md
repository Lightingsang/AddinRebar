---
title: "Kata Rebar: thanh C kê thép lớp 2+ và bước móc C theo J7/I8"
description: "Thanh C (Ø đai, móc 180° ôm 2 thanh ngoài cùng) dưới mỗi lớp thép gia cường lớp 2+ có ≥ N thanh; móc C cốt giá và thanh C kê rải theo J7 (Bố trí đều với) hoặc theo trạm đai ngoài (Giống đai ngoài, I8)."
status: completed
priority: P1
branch: RebarVersion1
tags: [revit, hprebar, kata, rebar, crosstie, beam]
created: 2026-10-02
related: [261002-0930-kata-dy7-match]
---

# Thanh C kê lớp 2 + bước móc C theo J7/I8

Hợp đồng: /grill-me 2026-10-02 (user "implement"). Chuẩn = bản vẽ Kata `Documents\T2-DY7.dwg` (MCP AutoCAD, chỉ đọc) + `kata-rebar-live.xlsm`.

## Bằng chứng (verified)
| Mục | Nguồn |
|---|---|
| Mặt cắt 1-1…7-7: lớp 2 = 3Ø18 → móc 14 Ø8a500; 8-8, 9-9 (1 lớp) không có | DWG `kata_thep dai` + tag `kata_block_KHT` SH2=14 |
| Móc 180° ôm 2 thanh ngoài cùng lớp 2, đoạn thẳng dưới tâm thanh 16 mm (bán kính uốn) — cùng kiểu móc C cốt giá (`WrapEnds`) | 5-5: thanh y −19970, móc 12673/12891, đoạn thẳng −19986 |
| Móc C cốt giá cũng 14 Ø8a500 | 5-5 tag @−19605 |
| Nhóm radio "Khoảng cách đai gia cường": I8 = 1 "Giống đai ngoài", I8 = 2 "Bố trí đều với" J7 | Bật/tắt radio trên workbook nháp, đọc I8 |
| Parser cũ đọc I8 = số nhánh đai (không ai dùng), J7 = ghi chú chưa rõ | `KataDamSheetParser` |

## Luật
| # | Luật |
|---|---|
| L1 | Thép gia cường lớp ≥ 2 (gối hàng 14–16, nhịp hàng 17) có ≥ N thanh (setting, mặc định 3) → bộ thanh C Ø đai, móc 180° ôm 2 thanh ngoài cùng, đoạn thẳng phía dưới |
| L2 | I8 = 2 (hoặc trống): đều với J7 (trống/sai → a500 + cảnh báo). I8 = 1: tại mọi trạm đai ngoài, lệch 2·Ø đai dọc dầm |
| L3 | Chỉ trong đoạn có cả 2 thanh ngoài cùng, mỗi nhịp một bộ, không trong cột; lùi 50 + 2·Ø đai khỏi mặt gối (chế độ đều) và đầu thanh |
| L4 | Móc C cốt giá theo L2 (bỏ setting `SideBarTieSpacing` 400) |
| L5 | Thanh C có thể chạm lớp dưới → cảnh báo |

## Phases
| # | Việc | Status |
|---|---|---|
| 1 | Parser J7/I8, rules/settings (N, bỏ SideBarTieSpacing), `KataTieStations` dùng chung | done |
| 2 | `KataLayerSpacerTieLayout` (đếm theo tiết diện) + tích hợp calculator; móc cốt giá theo L2 | done |
| 3 | Test 852/852; build R26/R25/R24; review ([report](reports/code-review.md): M1, M3 sửa, M2 → cảnh báo Revit) | done |
| 4 | UI settings; live DY7 qua Kata Export ([report](reports/live-verify-dy7.md)): sửa Revit kéo thanh C/thép lớp 2 lệch cao độ (`KataRebarSectionFit`); docs | done |

Ngoài phạm vi: đai U/C hàng 25–44 (giữ bước đai ngoài), thép kê E5 "25@2000", ghi đè hàng 22/23, lớp 2 thép chủ.
