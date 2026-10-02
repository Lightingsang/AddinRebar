---
title: "Kata Rebar khớp bản vẽ Kata T2-DY7 (đổi tiết diện + luật cắt)"
description: "Revit rebar cho T2-DY7 khớp Documents\\T2-DY7.dwg (±25 mm): h theo nhịp, thép dưới Z 1:6 / cắt rời, gối dầm, luật cắt gia cường trên/dưới theo bản vẽ, '-' nối tiếp, đai dày 0.25Ln, chân móc = phần thiếu, cốt giá liên tục."
status: completed
priority: P1
branch: RebarVersion1
tags: [revit, hprebar, kata, rebar, beam, section-change]
created: 2026-10-02
related: [261001-0900-kata-beam-rules-phase1]
---

# Kata Rebar ↔ bản vẽ Kata T2-DY7

Hợp đồng: /grill-me 2026-10-02 (user "implement"). Chuẩn = bản vẽ Kata `C:\Users\STR-HP03.HOANGPHUC\Documents\T2-DY7.dwg` (đọc qua MCP AutoCAD, chỉ đọc) + sheet `kata-rebar-live.xlsm` (J9 30/25, H3 0.2 mép, H5 0.25 mép, G1 500).

## Luật suy từ bản vẽ (CHƯA XÁC MINH ngoài DY7)
| # | Luật | Mốc bản vẽ (x từ mép ngoài cột C) |
|---|---|---|
| 1 | h_i = B5 − hàng 21; thép dưới qua gối \|Δh\| ≤ 100: uốn Z 1:6 bắt đầu ở mặt gối phía nhịp nông; > 100: cắt rời — nhịp sâu tới mặt xa − a bẻ lên (G3·d), nhịp nông thẳng G3·d từ mặt gối phía nó | Z 5950→6550; sâu tới 13815; nông từ 13310 |
| 2 | Gối là dầm (`bxh` hàng 11) = gối, rộng b | I 200 |
| 3 | Gia cường trên: mọi hàng cắt H5·Ln (Ln nhịp **từng bên**, gốc I5, tròn lên 50); hàng ngoài + G1 | C 1850/2350; E 4550·8200 / 4050·8700; G 11650·14400 |
| 4 | `-` ở hàng 13–16 = thanh cùng hàng của gối kề kéo tới gối này rồi neo | G13 → I |
| 5 | Gia cường dưới: hàng 17 cắt min(H3·Ln theo I3 (tròn lên 50 theo gốc), Ln/6 tròn xuống 50) từ mặt gối; hàng 18 = hàng 17 − G1 | 900 / 1150; 400 / 650 |
| 6 | Đai dày 0.25·Ln (hệ số × h mặc định 0) | nhịp 3: 550 |
| 7 | Chân móc = phần thiếu (tối thiểu mặc định 0), tròn lên 25 khi vừa | dưới Ø18 cột 450: 125 |
| 8 | Cốt giá liên tục qua gối giữa cho các nhịp liền nhau có cùng cốt giá, z theo nhịp nông nhất nhóm, neo 10d hai đầu | 330 → 13520 |

## Phases
| # | Việc | Status |
|---|---|---|
| 1 | Model/parser/planner: h theo nhịp, gối dầm, đo h từng nhịp ở Revit | done |
| 2 | Layout theo h nhịp: thép chủ dưới Z/cắt rời, đai, gia cường dưới, cốt giá, chân móc | done |
| 3 | Luật cắt trên/dưới, `-`, đai dày, chân móc tối thiểu, cốt giá liên tục; settings | done |
| 4 | Test 841/841 + build R26/R25/R24 + review (4 High đã sửa); live DY7 + DY14; docs | done ([live](reports/live-verify-dy7-dy14.md)) |
