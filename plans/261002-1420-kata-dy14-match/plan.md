---
title: "Kata Rebar khớp bản vẽ Kata T2-DY14 (nút dầm uốn/cắt, cốt giá hàng 20, '-' nối chuỗi)"
description: "T2-DY14 (E 350) + biến thể E 500 trong T2-DY7.dwg: luật nút dầm e/H (e = bậc − Ø, Ø ≥ 16), L/6 làm tròn gần nhất 50, hàng 20 nfd = n lớp, '-' nối liên tiếp."
status: completed
priority: P1
branch: RebarVersion1
tags: [revit, hprebar, kata, rebar, beam, section-change]
created: 2026-10-02
related: [261002-0930-kata-dy7-match, 261002-1130-kata-layer-spacer-ties]
---

# Kata Rebar ↔ bản vẽ Kata T2-DY14

Hợp đồng: /grill-me 2026-10-02 (user "implement"). Nguồn: `Documents\T2-DY7.dwg` (khung T2-DY14 L 16650 + biến thể E11 = 500 L 16800, MCP AutoCAD chỉ đọc), tab Detail thép của Kata (ảnh), hình "Chi tiết neo thép tại nút dầm" (TH2 e/H > 1/6 cắt + neo 30φ, TH3 e/H ≤ 1/6 uốn).

## Luật (K13–K16)
| # | Luật | Bằng chứng |
|---|---|---|
| K13 | Thép chủ dưới qua bậc đáy: uốn 1:6 khi Ø ≥ `CrankMinDiameter` (16, "Bẻ cổ chai cho thép có phi từ") và (bậc − Ø) / bề rộng gối ≤ 1/6; ngược lại cắt (TH2). Thay ngưỡng cũ "bậc ≤ 100" | DY7 82/500 uốn, DY14 82/350 cắt, biến thể 82/500 uốn |
| K14 | Gia cường bụng: trần L/6 làm tròn **gần nhất** 50 (không còn tròn xuống) | 5650/6 → 950 (biến thể), 5500/6 → 900 (DY7), 6950/6 → 1150 |
| K15 | Hàng 20 `nfd` = n lớp × 2 thanh (như G5) | DY14 "15 2x2Ø12", 2 cao độ −383/−217 |
| K16 | "-" liên tiếp ở hàng 13 nối chuỗi: thanh chạy tới "-" cuối cùng rồi neo/cắt | G13 qua I (-) tới K (-) neo, 16615 |

## Lệch chấp nhận (chưa có luật)
Khi thép dưới bị **cắt** ở gối E (DY14, E 350), Kata vẽ đầu trái các thanh nằm ở/trái gối E ngắn hơn 75 mm (E13/E14 4225/4725, D17/D18 1475/975, cốt giá 6405); biến thể E 500 (uốn) không có. HPRebar giữ luật (4150/4650, 1400/900, 6330).

## Phases
| # | Việc | Status |
|---|---|---|
| 1 | Luật K13–K16 + setting `CrankMinDiameter` (UI) | done |
| 2 | Test 858/858 (golden DY14 + biến thể), build R26/R25/R24 | done |
| 3 | Live DY14 qua Kata Export trên Revit thứ hai ([report](reports/live-verify-dy14.md)) | done |
