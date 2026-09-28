# Phase 05 — gia cường nhịp hàng 17–18 (1 nhịp)

## Context
- [plan.md](plan.md), [rule-table.md](reports/rule-table.md), phase 04 [phase-04-support-top-bars.md](phase-04-support-top-bars.md) (mẫu đối xứng).
- Sheet Dam: row 18 = lớp 1, row 17 = lớp 2 (user chốt 2026-09-28). Không ô nào cho điểm cắt thép dưới (chỉ H3/H5 cho thép trên) → rule B1 là mặc định.

## Overview
Priority P2. Status: done — reviewed, 736/736, build R26/R25/R24, live Revit 2026 khớp golden ([phase-live-verify.md](reports/phase-live-verify.md)). B1/B2 user duyệt giữ.

## Rule (B1/B2 = mặc định Claude chọn, user duyệt giữ 2026-09-28)
| ID | Rule |
|---|---|
| B1 ⚠ | Thẳng, cắt cách mép gối `BottomExtraCutFraction` = L0/7 (giữ rule cũ của code agent-team; không ô sheet) |
| B2 ⚠ | Hàng 18 cùng cao độ thép chủ dưới, xen giữa thép chủ (`BetweenMainBars`, đối xứng hàng 13) |
| B3 | Hàng 18: tâm = max(a dưới, b + d_đai + d/2) theo Ø từng thanh. Hàng 17: trên mặt trên cao nhất của lớp 1, khe max(25, d lớn); rải đều, Ø lớn ra biên; lớp 1 trống → ngồi trên đai |
| B4 | Chặn khi hàng 17 cách lớp thép trên < khe lớp: thép chủ trên (cả dầm) + thép gia cường gối có đoạn x chồng lên [xStart, xEnd] |
| B5 | Ô / phần ô không đọc được (≠ `0`/`-`/`*`) → cảnh báo; khe thanh < max(25, d) → cảnh báo; thanh chồng nhau → chặn |
| B6 | Mark `4.{nhịp}.{1=h18, 2=h17}`, shape 00; neo thép chủ dưới không đổi |

## Related code
- Core: `Calculators/KataSpanBottomBarLayout.cs` (viết lại), `KataRebarCalculator`, `KataScopeFilter` (bỏ skip 17/18), `Models/KataSpanRebarSpec` (+Text), `KataDetailingRules` (+cut), `KataRebarLayoutResult.LongitudinalBars`, `Parsers/KataDamSheetParser`.
- Revit: creation / log / handler dùng `LongitudinalBars`; type resolver + preview thêm "Gia cường nhịp"; orchestrator + result đếm `ExtraBottomBarCount`.
- Tests: `KataSpanBottomBarTests` (9), `KataScopeFilterTests` sửa.

## Golden (test)
Sheet live + B12 `2f20`, D18 `2f20`, D17 `2f18`: h18 y ±35.67 z −557; h17 y ±108 z −513; x 1257.1 → 5542.9.

## Todo
- [x] Core + test
- [x] Revit wiring, build R26/R25/R24
- [x] Review ([code-review-phase-05.md](reports/code-review-phase-05.md)) — H1/M1/M2/L1–L5 sửa
- [x] Live verify DT1 (D17/D18 trên sheet nháp)

## Risk
- B1 L0/7 có thể khác Kata CAD → user chỉnh hằng số `BottomExtraCutFraction` hoặc chỉ định ô.
- Hàng 13 (phase 04) vẫn dùng chung tâm thép chủ trên dù Ø lớn hơn (cùng lỗi H1, chưa sửa — phase 04 đã live).
