# Quy định bố trí thép dầm Kata — áp dụng trong HPRebar (Kata Export / Kata Rebar)

> Phạm vi: luồng Kata Export → sheet `Dam` của Kata.xlsm → Đọc thép → canvas → tạo thép Revit
> (`HPRebar.Core/KataRebar`, `HPRebar/KataExport`). Bản này ghi **luật số giai đoạn 1** (2026-10-01).
>
> Nguồn: `Q:\My Drive\03_ChuongTrinh\kata_structural_spec\kata_structural_spec\04_quy_dinh_thep_dam.md`
> (gọi tắt **spec**), TCVN 5574:2018, sheet `Dam` của `C:\kata_pro\Kata.xlsm` (chỉ đọc ô và chú thích ô).
>
> **Trạng thái mọi luật: chưa kiểm với Kata.** Mã Kata (DLL) mã hoá, không đối chiếu được; số lấy theo spec.
> Người dùng chỉnh trong **Kata Export ▸ Thiết lập** (lưu `%AppData%\HPRebar\KataSettings.json`).
> Ô sheet có giá trị thì thắng thiết lập (G1, G2/G3, H3/H5, I3/I5, G6–G8, J9).

## 1. Bảng luật

Đường dẫn code tính từ gốc repo; "Thiết lập" = trường của `KataSettings`.

| ID | Luật | Giá trị mặc định | Nguồn | Code | Test |
|---|---|---|---|---|---|
| R1 | **Cắt lệch gia cường trên**: tại mỗi gối, mỗi phía, hàng có thép ở ngoài vươn xa hơn đầu thanh xa nhất của hàng có thép liền trong ≥ G1 (hàng 13 ≥ 14 + G1 ≥ 15 + 2·G1 …), tầm vươn làm tròn lên bước 50. Chỉ kéo dài điểm cắt của hàng ngoài; không vượt mặt gối bên kia nhịp (chặn + cảnh báo, có ghi "ngắn hơn" khi hàng ngoài còn ngắn hơn hàng trong). Hàng trống không tính. Ô `trái;phải`: xét riêng từng phía; vế yếu chạy xuyên gối G2·d tính là đầu thanh của hàng nó nhưng không bị kéo dài | G1, trống → 500 mm (`CurtailedExtensionMm`; 0 = tắt) | spec §2.2, §4.3, §11.1 `CurtailedExtension`; TCVN 5574 §10.3.2 (w ≥ 500) | `KataTopBarStagger.Apply` (Calculators/KataTopBarStagger.cs), `KataSupportTopBarLayout.RunThrough` | `KataTopBarStaggerTests` |
| R2 | **Đọc G1**: một số dương → dùng; trống → thiết lập; khác (`-300;11700`, `0`, chữ) → thiết lập + cảnh báo "G1 '…' không phải một số dương" | — | ô G1 "Kéo thép gia cường" (comment Kata: "…tại vị trí có nhìu lớp thép gia cường bị cắt"); u2025 có `-300;11700` chưa rõ nghĩa | `KataBarNotationParser.ParsePositive`, `KataDetailingRuleBuilder.Build` | `KataSpecRuleTests.G1_*`, `KataTopBarStaggerTests` |
| R3 | **Vùng đai dày** mỗi đầu nhịp = min(Ln/2, max(2h, 0.25·Ln)); đai đầu cách mép gối 50. Hai vùng dày chạm nhau (Ln ≤ 4h) → không có vùng đai thưa: đai vùng phải cách đai cuối vùng trái < ½ bước thì bỏ (không trùng trạm), khe còn lại rộng hơn bước dày thì chèn đai đều nhau (bộ "Giữa nhịp (đai dày)", bước ≤ bước dày). Vùng không chạm: khe giữa rộng hơn bước thưa thì luôn có đai giữa (khe ≤ bước thưa) | 2 (`DenseZoneHeightFactor`), 0.25 (`EndZoneFraction`) | spec §6.1, §11.1 `ArrayZoneLength` (2h); TCVN 9386 (2h, dầm kháng chấn) | `KataDetailingRules.DenseZoneLength`, `KataStirrupZoneLayout.ThreeZones` | `KataSpecRuleTests.The_dense_zone_*`, `Dense_zones_that_fill_*`, `Stirrup_gaps_never_exceed_*` |
| R4 | **Gia cường bụng** (hàng 17/18) dừng cách mép gối 0.15·Ln, làm tròn xuống bước 50 | 0.15 (`BottomExtraCutFraction`, < 0.5) | spec §4.4 (0.15…0.20 Ln), §11.1 `BottomCutOffset` | `KataSpanBottomBarLayout.Build` (Calculators/KataSpanBottomBarLayout.cs:56) | `KataSpanBottomBarTests`, `KataSettingsTests` |
| R5 | **Chân móc neo** ≥ 15d | 15 (`MinimumLegFactor`) | spec §5.1 (L_vertical ≥ 15d) | `KataMainBarLayout` (:64, :91), `KataSupportTopBarLayout.Anchor` (:153) | `KataRebarPlannerTests`, `KataSpecRuleTests.The_shortest_leg_*` |
| R6 | **Làm tròn chân móc** lên bội 25 mm khi chân đã làm tròn vẫn lọt chiều cao dầm; không lọt thì giữ số chính xác | 25 mm (`RoundLegMm`; 0 = tắt) | spec §11.1 `RoundKe` / `RoundHookLength` | `KataAnchorage.Solve` (Calculators/KataAnchorage.cs:59) | `KataSpecRuleTests.A_leg_is_rounded_*`, `A_rounded_leg_that_would_not_fit_*` |
| R7 | **Khe thông thủy giữa hai lớp thép** ≥ max(30, d lớn) — lớp gia cường trên 14–16, lớp bụng 17, kiểm lớp dưới chạm lớp trên | 30 mm (`LayerClearGap`) | spec §4.2 (≥ max(d, 30)) | `KataDetailingRules.LayerGap` (Models/KataDetailingRules.cs:67), `KataTopLayerStack`, `KataSpanBottomBarLayout` | `KataSupportTopBarTests`, `KataSpanBottomBarTests`, `KataRebarCalculatorTests` |
| R8 | Khe giữa hai thanh cạnh nhau **trong một lớp** ≥ max(25, d) (giữ như cũ, tách khỏi R7) | 25 mm (cố định) | TCVN 5574 §10.3.1 (khoảng hở thanh đáy ≥ 25) | `KataDetailingRules.BarGap` (Models/KataDetailingRules.cs:74), `KataLayerPositions.CheckSpacing` | `KataSpecRuleTests.The_shortest_leg_*` |
| R9 | **Cốt giá**: h ≥ 700 mà G4/G5 không cho lớp nào và hàng 20 của nhịp trống → cảnh báo "thiếu cốt giá (TCVN 5574 mục 10.3.1.2)", **không tự sinh**. Hàng 20 = `0` là chủ ý → không cảnh báo | 700 mm (`SideBarRequiredHeight`; 0 = tắt) | spec §7.1; TCVN 5574 §10.3.1.2 | `KataSideBarLayout.Build` (Calculators/KataSideBarLayout.cs:62) | `KataSpecRuleTests.A_deep_beam_*` |

Luật có sẵn từ trước, không đổi ở giai đoạn này (vẫn "chưa kiểm với Kata"):

| Luật | Giá trị | Nguồn | Code |
|---|---|---|---|
| Neo thép chủ/gia cường gối biên: thẳng nếu đủ G2·d (trên) / G3·d (dưới) tính từ mép trong; thiếu → tới mép ngoài − a, bẻ 90° | G2 40, G3 30 | ô G2/G3; spec §5 | `KataAnchorage.Solve`, `KataMainBarLayout` |
| Chân móc dưới chồng chân móc trên cùng mặt phẳng → lùi vào (d₁+d₂)/2 + max(25, d) | 25 | — (quy ước HPRebar) | `KataAnchorage.BottomLegInset` |
| Gia cường gối: hàng 13 vươn H5·L, hàng 14–16 H3·L, đo từ mép hoặc tâm cột theo I5/I3; L = nhịp lớn hơn hai bên; làm tròn lên bước 50 | H5 0.25, H3 0.2 | ô H3/H5/I3/I5; spec §4.2 | `KataSupportTopBarLayout.Cuts` (:173) |
| Thép chủ liền suốt, dài > 11 700 → cảnh báo (chưa chia nối) | 11 700 | spec §10.2 | `KataMainBarLayout.ReportLength` |
| Cốt giá neo 10d vào gối; móc C a400, 180°/7.5d; đai □ 135°/7.5d | 10d, 400, 135°, 180° | hộp "Detail thép" Kata; spec §6.2, §7.2 | `KataSideBarLayout`, `KataSettings` |

## 2. Ví dụ số (dầm thử 300×600, cột 400, nhịp 6000 | 4500, J9 43/25, G2 40, G3 30, 3Ø20 / 4Ø20)

| Mục | Trước | Sau giai đoạn 1 |
|---|---|---|
| Chân móc thép chủ trên (800 − 357 = 443) | 443 | 450 (R6) |
| Chân móc thép chủ dưới (lùi 45, thiếu 288) | 288 | 300 (R5 15d) |
| Gia cường bụng nhịp 6000 | 850 → 5550 | 900 → 5500 (R4) |
| Vùng đai dày nhịp 4500 | 1125 (11 đai) | 1200 (12 đai) (R3) |
| Hàng 14 dưới thép chủ | 44 mm tâm–tâm | 49 mm (R7) |
| Gối giữa, hàng 13 H5 0.25 + hàng 14 H3 0.2 (từ mép) | 4900 / 8300 | 4700 / 8500 (R1) |

## 3. Chưa làm (mỗi mục một plan riêng, theo thứ tự user chọn)

1. Chia nối thép chủ theo cây 11.7 m (vùng nối, so le, nối chồng) — spec §10.2.
2. Giật cấp sàn vệ sinh hàng 19/21 (Z-bar ≤ 100, tách 40d > 100) — spec §9.
3. Đai treo + vai bò tại dầm phụ / cột cấy (hàng 20 cột gối) — spec §8.

Không làm: Kata Export ghi G1–J9; tự sinh cốt giá.

Đã biết, giữ nguyên: điểm cắt của một hàng gia cường gối giữa (khi nhịp bên kia ngắn) có thể dừng trong lòng cột bên kia — chỉ cột biên được chặn; R1 khi đó cảnh báo hàng ngoài "ngắn hơn".

## 4. Thiết lập

Mọi giá trị đi qua `KataSettingsJson.Sanitize` (file sửa tay, hộp thiết lập, rule builder): âm, NaN, ∞, tỉ lệ vượt nửa nhịp → mặc định. Hộp thiết lập từ chối ô không phải số hữu hạn.
