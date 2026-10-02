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

## 0. Cập nhật 2026-10-02 — chuẩn là bản vẽ Kata T2-DY7

User chốt: **bản vẽ Kata thắng spec** (`Documents\T2-DY7.dwg`, đọc qua MCP AutoCAD; x đo từ mép ngoài cột đầu). Các luật dưới đây **thay** dòng tương ứng ở mục 1 (R3, R4, R5 và luật cắt gia cường gối), suy từ 2 bản vẽ DY7 — **CHƯA XÁC MINH** với dầm Kata khác.

| ID | Luật | Mặc định | Code | Test |
|---|---|---|---|---|
| K1 | Chiều cao nhịp h_i = B5 − hàng 21 (mặt trên phẳng); Revit đo từng nhịp, lệch > 2 cảnh báo, > 50 chặn (ô hàng 21). B5 so với nhịp đầu theo chiều sheet | — | `KataSpanRebarSpec.Depth`, `KataBeamRebarSpec.DepthOf/SupportDepth`, `KataSheetGeometryCheck` | `KataDy7DrawingTests`, `KataSteppedBeamEdgeTests` |
| K2 | (Ngưỡng uốn/cắt thay bởi K13.) Thép chủ dưới: bậc đáy ≤ 100 → một thanh uốn Z 1:6 từ mặt gối phía nhịp nông vào nhịp sâu; > 100 (hoặc không đủ chỗ uốn: cạnh console / chồng đoạn uốn khác) → cắt: nhịp sâu tới mặt xa − a bẻ lên G3·d (chân không chạm thanh nhịp nông và thép trên), nhịp nông thẳng G3·d qua mặt gối phía nó | 100, 1:6 (`SoffitCrankMaxStep`, `CrankSlope`) | `KataBottomMainBarRuns` | `KataDy7DrawingTests.Bottom_main_bars_*`, `KataSteppedBeamEdgeTests` |
| K3 | Gối là dầm giao (`bxh` hàng 11): rộng b, chân móc không sâu quá h dầm giao | — | parser `BeamDepth`, planner | `A_crossing_beam_support_*` |
| K4 | Gia cường trên hàng 13–16: mọi hàng vươn H5·L (L = nhịp **từng bên**, gốc I5, tròn lên 50); hàng ngoài + G1 (R1). H3/I3 không dùng cho thép trên | H5 0.25 mép | `KataSupportTopBarLayout.Cuts` | `KataDy7DrawingTests.Additional_top_*`, `KataTopBarStaggerTests` |
| K5 | `-` ở hàng 13 = thanh hàng 13 gối kề kéo tới gối này: gối biên → neo (console → dừng cách mút một lớp bảo vệ); gối giữa → qua gối, cắt H5·L (+G1 nếu gối đó có hàng dưới). Hàng 14–16 chưa nhận `-` (cảnh báo) | — | `KataTopBarContinuation` | `Additional_top_*` (G13 → I), `A_dash_*` |
| K6 | Gia cường bụng: hàng 17 (hoặc 18 khi một mình) cách mặt gối min(H3·L theo I3 tròn lên 50, L/6 tròn xuống 50); hàng 18 dưới hàng 17 gần gối hơn G1 (≥ 0) | L/6 (`BottomExtraCutFraction` = trần) | `KataSpanBottomBarLayout.Cuts` | `Additional_bottom_*` |
| K7 | Vùng đai dày = 0.25·L (hệ số × h mặc định 0; đặt 2 để dùng max(2h, 0.25L) kháng chấn); hộp đai cao theo từng nhịp | 0 / 0.25 | `KataStirrupZoneLayout` | `Dense_stirrup_zones_*`, `With_a_height_factor_of_2_*` |
| K8 | Chân móc = phần thiếu (tối thiểu mặc định 0), tròn lên 25 khi vừa; chân dưới vẫn lùi khi chồng chân trên (Revit) | 0 (`MinimumLegFactor`) | `KataAnchorage` | `A_leg_bends_exactly_*` |
| K9 | Cốt giá: các nhịp liền nhau cùng cốt giá dùng một thanh chạy qua gối giữa, cao độ theo nhịp nông nhất nhóm, neo 10d hai đầu nhóm | — | `KataSideBarLayout` | `Side_bars_run_on_through_E_*` |
| K10 | File thiết lập cũ (không có `SettingsVersion`) đang giữ đúng mặc định cũ 15d / 2h / 0.15 → tự chuyển sang mặc định mới; giá trị user đã đổi giữ nguyên | `SettingsVersion` 2 | `KataSettingsJson.Migrate` | `A_settings_file_saved_with_the_old_defaults_*` |
| K11 | Thanh C kê: tiết diện nào cắt ≥ N thanh của lớp gia cường trong (gối hàng 14–16, nhịp hàng 17) có thanh C Ø đai, móc 180° ôm 2 thanh ngoài cùng của tiết diện, đoạn thẳng dưới lớp; đếm theo tiết diện nên ô "trái;phải" và lớp của 2 gối gặp nhau được xét đúng; lùi 50 + 2·Ø đai khỏi mặt gối và đầu thanh, không trong gối | N = 3 (`LayerTieMinBarCount`) | `KataLayerSpacerTieLayout` | `KataLayerSpacerTieTests` |
| K12 | Bước mọi móc C (cốt giá + thanh C kê) theo nhóm "Khoảng cách đai gia cường" của sheet: I8 = 2 đều a(J7), I8 = 1 tại mọi trạm đai ngoài (lệch 2·Ø đai); J7 trống/sai/ngoài 100–1000 → a500 + cảnh báo. Thay setting "Bước móc C giữ cốt giá" 400 | J7 a500 | `KataTieStations`, `KataDamSheetParser.ParseTieSpacing` | `Parse_J7_and_the_option_group_*`, `Like_hoops_*`, `An_unreadable_J7_*` |
| K13 | Thép chủ dưới qua bậc đáy (thay K2 "≤ 100"): uốn 1:6 khi Ø ≥ 16 và (bậc − Ø) / bề rộng gối ≤ 1/6 — hình "Chi tiết neo thép tại nút dầm" TH3; ngược lại cắt (TH2: nhịp nông thẳng 30φ qua mặt gối, nhịp sâu bẻ lên ở mặt xa) | 16 (`CrankMinDiameter`), 1/6 | `KataDetailingRules.Cranks`, `KataBottomMainBarRuns` | `KataDy14DrawingTests.At_E_*`, `Bottom_main_bars_thinner_than_16_*` |
| K14 | Trần gia cường bụng L/6 làm tròn **gần nhất** 50 (thay "tròn xuống" ở K6) | — | `KataSpanBottomBarLayout.Cuts` (`RoundNearest`) | `The_variant_with_E_500_*` |
| K15 | Hàng 20 `nfd` = n lớp × 2 thanh, như G5/G4 (thay "n thanh") | — | `KataSideBarLayout.Layers` | `Row_20_2f12_draws_two_layers_*`, `Row_20_counts_layers_like_G5` |
| K16 | "-" liên tiếp ở hàng 13 nối chuỗi tới "-" cuối rồi neo (gối biên) hoặc cắt H5·L (gối giữa) | — | `KataTopBarContinuation` | `At_E_350_*` (G13 → K) |

Lệch chấp nhận DY14: khi thép dưới bị cắt ở gối E, Kata vẽ đầu trái các thanh ở/trái gối đó ngắn hơn 75 mm (E13/E14, D17/D18, cốt giá); bản vẽ biến thể E = 500 (uốn) không có — chưa có luật, HPRebar giữ K4/K6/K9.

Sai khác còn lại với bản vẽ (chấp nhận): Revit nâng a từ J9 30 lên 42 (đai Ø8 + Ø18/2) → thanh lệch ~12 mm; chân thép dưới ở cột đầu lùi khi chồng chân thép trên (Kata 2D không lùi) → x 133 thay 35, chân 225 thay 125; chân trong dầm giao / nhịp nông bị giới hạn chiều cao (Kata vẽ 300/400 lố mặt dầm).

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
| Thép chủ liền suốt từ gối đầu tới gối cuối, **một thanh dù dài bao nhiêu** — mô hình thiết kế; chia cây 11.7 m / nối chồng / cổ chai / coupler thuộc shopdrawing, phần nối tính trong Rebar Schedule của template (user, 2026-10-01). Không cảnh báo | — | quyết định user | `KataMainBarLayout` |
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

1. Giật cấp sàn vệ sinh hàng 19/21 (Z-bar ≤ 100, tách 40d > 100) — spec §9.
2. Đai treo + vai bò tại dầm phụ / cột cấy (hàng 20 cột gối) — spec §8.

Không làm: Kata Export ghi G1–J9; tự sinh cốt giá; chia nối thép chủ theo cây 11.7 m (spec §10.2 — việc của shopdrawing, Rebar Schedule tính phần nối).

Đã biết, giữ nguyên: điểm cắt của một hàng gia cường gối giữa (khi nhịp bên kia ngắn) có thể dừng trong lòng cột bên kia — chỉ cột biên được chặn; R1 khi đó cảnh báo hàng ngoài "ngắn hơn".

## 4. Tạo thép trong Revit

- Thép dọc (chủ B11/B12, gia cường 13–18, cốt giá): mỗi nhóm ≥ 2 thanh cùng ô, cùng vai trò/Ø/hình, cách đều → **1 Rebar layout Fixed Number**; 1 thanh → Single. Ô trộn Ø (`2f16+1f14`) tách theo Ø; ô `trái;phải` mỗi vế một bộ; không đều → các đoạn cách đều ≥ 2 thanh, còn lại Single (user, 2026-10-02). Code `KataLongitudinalSetGrouping` (Core) + `KataRebarCreationService` (Revit).
- Revit tự snap mặt phẳng thanh mới vào lớp bảo vệ (Ø16 ở y −29 bị kéo về −33): thanh Single được dời lại; bộ Fixed Number sửa khoảng cách constraint hai đầu dải rồi kiểm ±0,5 mm, sai thì lùi về Single và báo trong thông báo.

## 5. Thiết lập

Mọi giá trị đi qua `KataSettingsJson.Sanitize` (file sửa tay, hộp thiết lập, rule builder): âm, NaN, ∞, tỉ lệ vượt nửa nhịp → mặc định. Hộp thiết lập từ chối ô không phải số hữu hạn.
