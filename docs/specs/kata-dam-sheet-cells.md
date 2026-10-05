# Sheet `Dam` của Kata — nghĩa từng ô nhập (hàng 1–28), bằng chứng B01, trạng thái HPRebar

> Bảng tra ô cho [kata-beam-rebar-rules.md](kata-beam-rebar-rules.md) (file quy tắc duy nhất mà Tạo thép Revit phải theo).
> Lập 2026-10-05 cho [plan B01](../../plans/261005-1338-kata-b01-drawing-parity/plan.md). Giá trị B01: bản dump chỉ đọc
> [kataB1-dam-cells.txt](../../plans/261005-1338-kata-b01-drawing-parity/reports/kataB1-dam-cells.txt).

## 0. Nguồn và ký hiệu

**Ưu tiên khi lệch (quyết định user 2026-10-05):** DWG B01 (`T2-DY7.dwg`, đã đo) > `04_quy_dinh_thep_dam.md` (mục 3.2) >
`QUY_TRINH_THUAT_TOAN`. Nghĩa ô viết lại bằng lời của repo, không chép tài liệu nguồn.

| Cột | Nội dung |
|---|---|
| Nghĩa | Theo 04_quy_dinh §3.2, đã sửa chỗ DWG bác (đánh dấu ⚑, xem §8) |
| B01 | Giá trị trong KataB1.xlsm; `—` = trống |
| DWG | Bằng chứng đo trên bản vẽ Kata của B01 (x tính từ mặt ngoài gối 1, z từ đỉnh dầm); "chưa có trong B01" khi ô trống |
| HPRebar | ✅ đúng · 🟡 một phần / chưa test trên B01 · ❌ sai hoặc chưa đọc · ➖ không ảnh hưởng thép (chỉ vẽ/form) |

**Đường dẫn code** (từ gốc repo):
`DSP` = `HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs` ·
`BNP` = `…/Parsers/KataBarNotationParser.cs` · `ZWS` = `…/Parsers/KataZeroWidthSupports.cs` ·
`SSP` = `…/Parsers/KataStirrupSectionParser.cs` · `C/` = `HPRebar/HPRebar.Core/KataRebar/Calculators/`.
Trạng thái theo **cây làm việc 2026-10-05** (có thay đổi chưa commit của đợt 1).

**Bố cục B01 (hàng 11):** C 400 · D 10400 · E 400 · F 6500 · G 400 · H 2500 · I **0** · J 4000 · K 400 · L 6200 · M 400 ·
N 2000 (console) · O **0** (mút). Gối I bề rộng 0 → H+J gộp một nhịp 6500 (18100…24600). Trạm: gối 1 = 0…400, D 400…10800,
F 11200…17700, H+J 18100…24600, L 25000…31200, console 31600…33600.

## 1. Hàng 1–9 — tham số chung

| Ô | Nghĩa | B01 | DWG | HPRebar | R-xx |
|---|---|---|---|---|---|
| B1 | Tỷ lệ mặt đứng dầm (list M1:Q1) | 1:50 | — | ➖ không đọc (Revit dùng tỷ lệ view) | — |
| G1 | ⚑ Bước so le giữa các lớp gia cường trên: hàng ngoài vươn xa hơn hàng liền trong ≥ G1 (04 gọi là "w +500 TCVN" — sai) | 500 | E14 trái 7700 = (10800 − 2600) − 500 vì E15 có thép bên trái | ✅ `DSP:40`, `C/KataTopBarStagger.cs` · test `Additional_top_bars_of_row_14_over_C_and_E_reach_as_drawn` | R-42, R-43, R-52 |
| I1 | Checkbox "cách điểm cắt thép gối = h" | TRUE | ⚑ I1 = TRUE mà bước lệch E14/E15 vẫn = G1 (500), không phải h (1100) → I1 không đổi điểm cắt | ✅ bỏ qua (đúng DWG) | R-42 |
| B2 | Tỷ lệ mặt cắt (list N2:Q2) | 1:25 | — | ➖ không đọc; canvas ghi cố định "TL: 1/25" (`C/KataDrawingStyle.cs:85`) | — |
| G2 | Hệ số neo **thép trên** (×d) (04: "vùng kéo") | 40 | thép trên neo trong K, gối biên | ✅ `C/KataDetailingRuleBuilder.cs:88` | R-30 |
| B3 | Tên dầm | B01 | tiêu đề "B01 (SL=1; L=33600)" | ✅ `DSP:29` (tiêu đề `C/KataElevationDrawingBuilder.cs:45`) | — |
| G3 | Hệ số neo **thép dưới** (×d) (04: "vùng nén") | 30 | dưới nhịp L bắt đầu 24400 = 25000 − 30·20 | ✅ `C/KataDetailingRuleBuilder.cs:89` | R-30, R-20 |
| H3 | Tỉ lệ cắt gia cường **bụng** (hàng 17/18) | 0.2 | F17, L17 chưa đo trên B01 (DY7 đã đo) | ✅ `DSP:44`, `C/KataSpanBottomBarLayout.cs:110` | R-51 |
| I3 | Gốc đo của H3 (list N3:N5) | L từ mép cột | — | 🟡 chỉ phân biệt "có chữ tâm" (`DSP:52`) | R-137 |
| B4 | Số cấu kiện | 1 | "SL=1" | ✅ `DSP:30` | — |
| G4 | Ø cốt giá | 12 | nhịp 1: 4Ø12 (2 lớp) | ✅ `DSP:92` | R-70 |
| B5 | h dầm mặc định | 1100 | nhịp 1 và console cao theo B5 (1100 / 900 do N19) | ✅ `DSP:32` · test `B01_reads_as_five_spans_*` | R-03 |
| G5 | Số lớp cốt giá; âm = bỏ đai C cốt giá; 0 = không | 2 | nhịp 1 cốt giá z −387 / −713 | ✅ `DSP:94` · test `Side_bars_of_span_1_*` | R-70 |
| H5 | Tỉ lệ cắt gia cường **gối** (mọi hàng 13–16) | 0.25 | C14 tới 3000 = 400 + 0.25·10400; E14 phải 12850 = 11200 + 1650 | ✅ `DSP:45`, `C/KataSupportTopBarLayout.cs:200` | R-41 |
| I5 | Gốc đo của H5 (list N3:N6) | L từ mép cột | như H5 | 🟡 chỉ phân biệt "có chữ tâm" (`DSP:47`) | R-41, R-137 |
| B6 | b dầm mặc định | 500 | nhịp 1–3 rộng 500 | ✅ `DSP:33` | R-04 |
| G6 | Ø đai | 10 | Ø10 mọi nơi | 🟡 `DSP:60` — ô trống → Ø10 thay vì "không đai" | R-61 |
| H6 | Thép kê giữa 2 lớp (`25` → Ø25@2000) | — | chưa có trong B01 | ❌ không đọc | R-126 |
| B7 | h sàn; cú pháp `*h1;down1/h2;down2`, âm = sàn đáy dầm | 150 | mặt cắt có sàn (chưa đo kích thước) | 🟡 chỉ đọc số (`DSP:34`), dùng để vẽ sàn mặt cắt | R-131 |
| G7 | Bước đai vùng dày (`a150`, `150`, `@150`) | a150 | đai dày a150 các nhịp | ✅ `DSP:63` · test `Stirrup_zones_*` | R-60, R-64 |
| H7 | Cờ trạng thái form | FALSE | — | ➖ | — |
| J7 | Bước đều của đai C / đai trong khi I8 = 2 | a500 | 2-2: tag "Ø10 a500" (U), "2xØ10 a500" (C); 14-14: C a500 | ✅ `DSP:68` | R-68, R-77 |
| B8 | Tên trục dọc dầm | — | chưa có trong B01 | ➖ đọc, chỉ để vẽ (`DSP:35`) | — |
| G8 | Bước đai vùng giữa | a200 | vùng giữa a200 | ✅ `DSP:64` | R-60, R-64 |
| I8 | ⚑ Radio "khoảng cách đai gia cường": 1 = giống đai ngoài, 2 = đều theo J7 (04 ghi ngược) | 2 | U/C rải đều a500 | ✅ `DSP:69` | R-68, R-77 |
| B9 | Độ lệch trục dầm | −100 | chưa đo | ➖ đọc, chưa dùng (`DSP:36`) | — |
| G9 | Bước đai console | 150 | console Ø10a150 31650…33500 | ✅ `DSP:65` · test `Stirrups_follow_*` | R-60, R-92 |
| J9 | `a/b`: a = mép → tâm thép chủ, b = bảo vệ tới mặt ngoài đai | 50/25 | console hộp 250×850 = (300 − 50) × (900 − 50) | ✅ `DSP:57`; tâm thanh lệch 4–12 mm | R-10…R-13 |
| K2:K8, L9, M1:Q1, N2:Q2, N3:N6 | Tác giả, URL, nguồn dropdown | … | — | ➖ | — |

## 2. Hàng 10–12 — khung gối/nhịp, thép chủ

| Ô | Nghĩa | B01 | DWG | HPRebar | R-xx |
|---|---|---|---|---|---|
| B10 | Cao trình đỉnh dầm | +3.300 | ghi trên mặt đứng | ➖ đọc để vẽ (`DSP:37`) | — |
| h10 từ C | "Cột"/"Nhịp" xen kẽ (nhãn template chạy tới P) | C…P | — | ✅ `DSP:115-137` | R-01 |
| B11 | Thép chủ trên toàn dầm, nhóm `;` | 6f25 | 6Ø25 từ gối 1 tới K | ✅ nhóm đầu (`DSP:86`); nhóm 2 ❌ | R-20, R-124 |
| h11 gối | Bề rộng cột dưới; `bxh` = gối là dầm; `0` = không gối (nút giữa / mút console); âm = neo vào dầm/vách | 400 ×5, I 0, O 0 | I: H+J vẽ thành 1 nhịp; O: mút console | ✅ số, `bxh`, `0` (`DSP:192`, `ZWS:23`); âm ❌ (bị chặn) | R-02, R-05, R-136 |
| h11 nhịp | Chiều dài nhịp thông thủy; danh sách dừng ở ô h11 trống đầu tiên | 10400…2000 | L = 33600 | ✅ `DSP:261` | R-01, R-90 |
| B12 | Thép chủ dưới toàn dầm, nhóm `;` | 6f25 | 6Ø25 | ✅ nhóm đầu (`DSP:87`) | R-20, R-22 |
| h12 gối | Không dùng (đường dim) | — | — | ➖ | — |
| h12 nhịp | Ghi đè sàn từng nhịp (cú pháp như B7) | — | chưa có trong B01 | ❌ không đọc | R-131 |

## 3. Hàng 13–16 — gia cường trên (ô gối)

| Ô | Nghĩa | B01 | DWG | HPRebar | R-xx |
|---|---|---|---|---|---|
| h13 gối | Lớp 1, cùng cao độ thép chủ trên, chen khe; `trái;phải`; `x;0`; `-` = nhận thanh gối kề kéo sang | — | chưa có trong B01 | ✅ `DSP:198` | R-40, R-44, R-46 |
| h14 gối | Lớp 2 (dưới lớp 1) | C 6f25, E 6f20, G 2f20, K 2f20;2f16, M 2f16 | C14 105…3000, E14 7700…12850, M14 30960…33550 (vươn tới mút console) | ✅ `DSP:199` · test `Additional_top_bars_of_row_14_*`, `The_console_gets_*` | R-41, R-42, R-46, R-92 |
| h15 gối | Lớp 3 | E 6f20;0 | chỉ bên trái E; đẩy E14 trái ra thêm G1 | ✅ `DSP:200` | R-41, R-42 |
| h16 gối | ⚑ 04: gia cường **dưới** lớp 3 (ô gối luôn trống); HPRebar đọc ô gối = trên lớp 4 | — | chưa có trong B01 | 🟡 chưa chốt | R-07 |
| h13–16 nhịp | `-` = thép gia cường gối chạy suốt nhịp (console, M âm toàn nhịp) | — | chưa có trong B01 | ❌ bỏ qua, không báo | R-134 |

## 4. Hàng 17–18 — gia cường dưới

| Ô | Nghĩa | B01 | DWG | HPRebar | R-xx |
|---|---|---|---|---|---|
| h17 nhịp | Lớp 2 (trên lớp 1), cắt min(H3·L, L/6) | D 6f25, F 2f20, L 2f20 | D, F, L có 1 lớp gia cường dưới (chưa đo điểm cắt trên B01) | ✅ `DSP:265` | R-50, R-51 |
| h17 nhịp `-` | ⚑ 04: chạy suốt nhịp. Ý nghĩa thật chưa đo | J `-` | I17 2Ø20 dừng 23300, 1300 trước mặt K → không "chạy suốt" J | ❌ chỉ báo (`ZWS:52`) | R-135 |
| h17 gối | Thép dưới qua nút (gối không cột) | I 2f20 | 2Ø20 18950…23300 (850 / 1300 từ mặt gối) | 🟡 HPRebar 19200…23500 (`ZWS:98`) | R-54 |
| h18 nhịp | Lớp 1, cùng cao độ thép chủ dưới, chen khe; ngắn hơn hàng 17 G1 | — | chưa có trong B01 | ✅ `DSP:264` | R-50, R-52 |
| h18 gối | Như h17 gối | — | chưa có trong B01 | 🟡 như h17 gối | R-54 |

## 5. Hàng 19–21 — đỉnh, bề rộng, đáy, dầm giao

| Ô | Nghĩa | B01 | DWG | HPRebar | R-xx |
|---|---|---|---|---|---|
| h19 gối | Cột tầng trên `b` hoặc `b;lệch`; `0` = không cột trên; trống = như cột dưới | — | chưa có trong B01 | ❌ đọc (`DSP:210`), không dùng | R-128 |
| h19 nhịp | `Δđỉnh[;thép trên]`: số âm hạ đỉnh, dương nâng; chỉ thép = đổi thép chủ trên từ nhịp đó; `0`/`-` đặt lại; trống = kế thừa nhịp trước | H −50, J 0, L 3f20, N −200 | thép trên uốn xuống 50 qua G, lên lại tại I; L: 3Ø20 (mặt cắt 11–14); console đỉnh −200, đai −225…−1075 | ✅ `DSP:269`, `DSP:318-355`, `ZWS:61` · tests `The_top_bars_crank_*`, `The_narrow_spans_*`, `Stirrups_follow_*` | R-03, R-20, R-21, R-66, R-92 |
| h20 gối | Dầm giao tại cột `bxh` (hoặc `b`, h = B5); trống = không có | C 400x500, E/G/M 400, K 400x800 | ⚑ không có đai nút tại gối cột nào (gối 1 có dầm giao 400x500 cũng không) | ❌ chỉ đọc số (`DSP:211`): `400x500` mất im lặng; không ảnh hưởng thép | R-120, R-129 |
| h20 nhịp | `nfd` = n lớp cốt giá ×2 thanh (`0`/`0f12` = không); số đứng đầu = bề rộng nhịp (`300`, `300;2f12`); trống = kế thừa | F 0f12, L 300, N 1f12 | F, H+J, L không cốt giá (13-13 không có); L, console rộng 300; console 1 lớp 2Ø12 giữa cao | ✅ `DSP:270`, `DSP:321-352` · test `Side_bars_stop_after_span_1_*` | R-22a, R-71 |
| h21 gối | Lệch tim dầm giao so với tâm cột, \|lt\| ≤ cột/2 | — | chưa có trong B01 | ❌ đọc (`DSP:212`), không dùng | R-130 |
| h21 nhịp | `Δđáy[;thép dưới]`: dương = đáy nâng (h giảm); chỉ thép = đổi thép chủ dưới; `0` đặt lại | F 400, L 3f20, N 0 | F, H+J, L cao 700; L dưới 3Ø20; console về 1100 − 200 = 900 | ✅ `DSP:271`, `DSP:149` · test `B01_reads_as_five_spans_*` | R-03, R-20, R-81 |

## 6. Hàng 22–24 — trục, đai nhịp, cờ nút

| Ô | Nghĩa | B01 | DWG | HPRebar | R-xx |
|---|---|---|---|---|---|
| h22 gối | Tên trục cột | 1…6 (C…M) | bong bóng trục 1…6 | ➖ vẽ (`DSP:213`, `C/KataElevationDrawingBuilder.cs:38`) | — |
| h22 nhịp | Bước đai ngoài riêng nhịp `a1/a2[/a3]` (1 số = đều cả nhịp); tiền tố `d8`/`f8` đổi Ø đai nhịp | — | chưa có trong B01 | ✅ bước (`DSP:273-288`); tiền tố Ø ❌ (rơi về G7, không báo) | R-67, R-132 |
| h23 gối | Lệch trục định vị so với tâm cột | C −150 | trục 1 lệch 150 so với tâm cột 1 | ➖ vẽ (`DSP:214`, `C/KataDrawingFrame.cs:72`) | — |
| h23 nhịp | Bước đai trong (U/C/□) riêng nhịp `a1/a2[/a3]` | — | chưa có trong B01 | ❌ chỉ báo (`DSP:290`) | R-133 |
| C24 (số) | Đai ngoài = 2 đai U ghép, nối chồng `C24·Ø đai` (đai chống xoắn) | — | chưa có trong B01 | ❌ chỉ báo | R-122 |
| h24 gối `*` | Không bố trí đai gia cường nút tại gối đó | K `*` | ⚑ không có đai nút ở **mọi** gối cột (cả gối 1 không `*`) → `*` không đổi gì trên B01 | ✅ ghi chú "đúng như bản vẽ" (`DSP:216-220`) · test `K24_star_*` | R-120, R-122 |
| h24 nhịp | Không dùng | — | — | ➖ | — |

## 7. Hàng 25–44 — đai trong (theo **cặp cột** của nhịp), hàng 28

Đọc theo cặp (cột lẻ = loại `Đai □` / `Đai U` / `Đai C`, cột chẵn = thanh `a-b` hoặc `a`) — cặp (C,D) thuộc nhịp D,
(E,F) nhịp F, …, (M,N) console N. Thanh đánh số **trên thép chủ trên của chính nhịp đó**, thanh 1 ở bên trái mặt cắt Kata.
Mỗi hàng 25…44 là một mục; A25:A27 là danh sách dropdown, không phải nhãn hàng.

| Ô | Nghĩa | B01 | DWG | HPRebar | R-xx |
|---|---|---|---|---|---|
| (C,D) 25–27 | Đai trong nhịp 1 | U 3-4, C 2, C 5 | 2-2: U ôm thanh 3-4 (hở trên), C cạnh thanh 2 và 5, a500 | ✅ `SSP`, `C/KataInnerStirrupLayout.cs:33-39` · test `KataSectionInnerStirrupTests` | R-68 |
| (M,N) 25 | Đai trong console | C 2 | 14-14 (console 300×900, trên 3Ø20): C quanh thanh **giữa** (thanh 2/3 của 3Ø20 console), không U, a500 | 🟡 code dùng thép chủ trên của nhịp (`C/KataInnerStirrupLayout.cs:34`); chưa có test B01 | R-68 |
| (E,F) (G,H) (I,J) (K,L) | Đai trong nhịp F, H, J, L | — | 13-13 (L 300×700): không U/C | ✅ không vẽ; ô của nửa phải nhịp gộp được báo (`ZWS:53`) | R-68 |
| loại `Đai □ a-b` | Đai kín trong ôm thanh a..b; `□ 1-n` = đai ngoài | — | chưa có trong B01 | ✅ | R-68 |
| A28 | `1` — không phải ô nhập (04 gọi "dầm phụ giằng": sai) | 1 | — | ➖ | — |

## 8. Mâu thuẫn nguồn (04_quy_dinh ↔ DWG B01) — DWG thắng

| # | 04_quy_dinh nói | DWG B01 cho thấy | Theo |
|---|---|---|---|
| 1 | Mỗi gối / nút có dầm giao (h20) tự sinh chùm đai gia cường (5Ø10a50 + vai bò); `*` ở h24 tắt | Không có đai nút ở **gối cột nào** (gối 1 có C20 400x500, không `*`); đai nút chỉ quanh dầm giao gác giữa nhịp D (4850…5050 ‖ 5550…5750) và cột cấy nhịp F (14350…14550 ‖ 15050…15250) — lấy từ model Revit | DWG (R-120) |
| 2 | I1 tick → lớp 2 ngắn hơn lớp 1 một đoạn h | I1 = TRUE nhưng bước lệch = G1 = 500 | DWG (R-42) |
| 3 | G1 = đoạn w TCVN +500 cộng cho thanh | G1 = bước so le giữa lớp ngoài và lớp trong | DWG (R-42) |
| 4 | I8 = 1 "đều J7", I8 = 2 "giống đai ngoài" | I8 = 2 → U/C đều a500 = J7 | DWG (R-68, R-77) |
| 5 | Bước đai trong theo h23, trống → theo h22 / G7–G8 | h23 trống, I8 = 2 → a500 | DWG (R-68, R-133) |
| 6 | Thép chủ dưới mang số hiệu 2 | Thép chủ trên 6Ø25 = 1, thép chủ dưới 6Ø25 = **4**; số đai 23…35 | DWG (R-110) |
| 7 | Mặt cắt tại 0.125 / 0.5 / 0.875 Ln | 14 cờ tại x = 1250, 5300, 9950, 11650, 14150, 17025, 18550, 20250, 22650, 24150, 25425, 27800, 30725, 32267 (H và J mỗi nhịp riêng; console 1 cờ) | DWG (§14 file quy tắc) |
| 8 | h20 gối `bxh` dùng cho đai treo / vai bò tại cột | Không có thép nào thêm tại cột | DWG (R-129) |
| 9 | h17 `-` = chạy suốt nhịp | I17/J17 `-`: 2Ø20 dừng 1300 trước mặt K | chưa rõ — R-135 |
| 10 | Thép gia cường gối Ln/3 (+500), bụng 0.15–0.2 Ln, đai dày 2h, neo chân ≥ 15d | đã bị DY7/DY14 bác ([đánh giá nguồn](../../plans/261004-1900-kata-beam-rules-rewrite/reports/source-evaluation.md)) | DWG |
| 11 | h16 = gia cường dưới lớp 3 | B01 không có h16 | chưa chốt — R-07 |
