# Phase 01 — Core (HPRebar.Core/KataRebar)

Status: done (700/700 `dotnet test HPRebar/HPRebar.Core.Tests`).

## Thay đổi
- Parser: J9 → `ParseCover` trả 0 khi thiếu; `TopMainItems/BottomMainItems`; `SheetColumn`; hàng 25–44 theo cặp cột (`KataStirrupSectionParser`, chỉ tính khi ô "thanh ôm" có giá trị); ghi chú hàng 23 nhịp / 24 gối (`KataCellNote`); G9 đọc `a150`; `KataCellTable` bỏ lỗi bound 1-based.
- Rules: `KataDetailingRuleBuilder` (C1–C4) → `KataDetailingRules`.
- Layout tách từ calculator 1 031 dòng: `KataBeamStations`, `KataAnchorage` (A1–A5), `KataMainBarLayout`, `KataStirrupZoneLayout` + `KataStirrupCurveFactory`, `KataAdditionalBarLayout` (+`.Bottom`), `KataSideBarLayout` (2 cái cuối chuyển nguyên, chưa vẽ trong Revit). Console giữ bố trí cũ (chưa hỗ trợ, bị chặn).
- `KataScopeFilter` (skipped/blocking theo địa chỉ ô), `KataSheetGeometryCheck` (G1, chiều ngược), `KataRebarPlanner` (Revit geometry thay số sheet, bỏ cốt giá tự động h ≥ 700), `KataRebarTag`.

## Test
Sửa: ParseCover (2), neo/cover trong `KataRebarCalculatorTests`, `KataStressAdversarialTests` (IsValid → chỉ cho phép cảnh báo "Neo thép chủ"). Xoá `KataRebarContractVerificationTests` (tự mô phỏng predicate, không gọi code thật). Thêm: rule builder, anchorage, planner (golden = live check), geometry check, scope filter, stirrup section parser, tag.
