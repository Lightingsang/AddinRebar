# Kata Rebar MVP — mapping ô sheet Dam + rule (user duyệt 2026-09-28)

Nguồn ô: dump Kata.xlsm (Update2025 + `C:\kata_pro`), VBA `Ve_dam` (`chuyen_thep`, `chuyen_ht`, `abc`, `bieudo`), `save_info` (save/load). VBA không chứa rule vẽ thép → rule là của HPRebar, tham số từ ô.

## Mapping ô → model → dùng

| Ô | Nghĩa (Kata) | Model | MVP |
|---|---|---|---|
| B3 | tên dầm | `BeamName` | Partition + log |
| B4 | số cấu kiện | `BeamCount` | chưa dùng |
| B5 / B6 | h / b | `Height` / `Width` | chỉ đối chiếu Revit (G1) |
| row 10 | `Cột`/`Nhịp` xen kẽ từ C | `Supports`/`Spans` + `SheetColumn` | thứ tự |
| row 11 | rộng gối / L nhịp | `ColumnWidth` / `Length` | đối chiếu Revit (G1) |
| B11 / B12 | thép chủ trên / dưới `nfd`, nhiều nhóm `;` | `TopContinuous` + `TopMainItems` … | vẽ nhóm đầu, nhóm sau → bỏ qua |
| G2 / G3 | neo vùng kéo / nén (×d) | `TensionLapMultiplier` / `CompressionLapMultiplier` | neo (A1–A4) |
| G4 / G5 | Ø cốt giá / số lớp | `GlobalSideBars` | bỏ qua |
| G6 | Ø đai | `GlobalStirrup.Diameter` | vẽ |
| G7 / G8 / G9 | bước đai gần gối / giữa nhịp / console | `SupportSpacing` / `MidspanSpacing` / `CantileverSpacing` | G7/G8 vẽ |
| H3+I3, H5+I5 | tỉ lệ cắt gia cường + gốc đo ("L từ tâm/mép cột") | `TopCutoffRatioLayer2/1`, `CutoffOriginLayer2/1` | H5/I5 hàng 13, H3/I3 hàng 14–16 |
| J9 | `a/b` | `CoverMain` / `CoverStirrup` (0 = không ghi) | cover (C1–C4) |
| 13–16 (gối) | gia cường trên (13 = lớp thép chủ, 14–16 xuống dưới), ô `trái;phải` | `TopExtraSides` (+ `TopExtraLayer1..4`) | **vẽ** (rule T1–T8, [phase-04](../phase-04-support-top-bars.md)) |
| 17 / 18 (nhịp) | gia cường dưới lớp 2 / 1 | `BottomExtraLayer2/1` | bỏ qua |
| 19 / 21 (nhịp) | giật mép trên / dưới `off;nfd` | `TopDrop(Bars)` / `SoffitDrop(Bars)` | bỏ qua |
| 20 (nhịp) | cốt giá override (`2f12`, `0`) | `SideBars` | bỏ qua (≠ 0) |
| 20–23 (gối) | rộng dầm giao / lệch / tên trục / lệch trục | `CrossingBeam*`, `Grid*` | không dùng |
| 22 / 23 (nhịp) | bước đai riêng / đai gia cường | `StirrupOverride` / note | bỏ qua |
| 24 (gối) | `30` đai chống xoắn, `*` không đai gia cường | note | bỏ qua |
| 25–44 cặp C/D, E/F… | loại đai (trái) + thanh ôm (phải, `3-4`, `2`); chỉ tính khi ô phải có giá trị (VBA `bieudo`) | `Branches` (Outer + trong) | chỉ đai □ ngoài |

## Rule MVP

| ID | Rule |
|---|---|
| C1 | J9 `a/b`: a = mép bê tông → tâm thép chủ; b = lớp bảo vệ tới mặt ngoài đai |
| C2 | J9 chỉ `a` → b = a − max(d)/2 − d_đai; b < 15 cảnh báo; b ≤ 0 chặn |
| C3 | J9 trống → b = 25, a = b + d_đai + d/2 |
| C4 | a < b + d_đai + d/2 → nâng a + cảnh báo. Ngang: thanh biên sát đai |
| A1–A4 | La trên = G2·d, dưới = G3·d, đo từ mép trong gối. c − a ≥ La → thẳng; không → tâm dừng ở mép ngoài + a, bẻ 90° (trên xuống, dưới lên), chân = max(La − (c − a), 10d) ≤ h − a_t − a_d; thiếu → cảnh báo |
| A5 | Chân trên + chân dưới > h − a_t − a_d → lùi chân dưới vào (d_t + d_d)/2 + max(25, d_d), tính lại |
| S1–S3 | L0 = nhịp thông thủy Revit; đai đầu cách mép 50; vùng đầu L0/4, n = ⌊(L0/4 − 50)/G7⌋ + 1 |
| S4 | Vùng giữa đúng G8, dư chia 2 khe chuyển tiếp (G8/2..G8] |
| G1 | Sheet vs Revit (b, h, row 11): ≤ 2 mm im lặng, ≤ 50 cảnh báo, > 50 hoặc khác số cột → chặn; đọc được cả chiều ngược |
| Scope | Chặn: ≠ 1 nhịp, gối rộng 0 (console), > 1 dầm, gối là dầm/điểm nối |

## Golden (test `KataRebarPlannerTests` = live check)
Dầm 300×600, cột 400 | 6000 | 400, B11 `3f20`, B12 `4f20`, G2 40, G3 30, G6 8, G7 a100, G8 a200, J9 `43/25`:
- Trên y −107/0/107, z −43, x 43→6757, chân 443. Dưới y ±107/±35.7, z −557, chân x 88/6712 dài 288.
- Đai hộp 250×550 tại (−125, −575): 15@100 từ 450; 15@200 từ 2000; 15@100 từ 4950 → 6350.
- J9 `40` → b 22, z trên −40, y ±110.
