# Quy định bố trí thép dầm Kata — chuẩn của HPRebar (Kata Export / Kata Rebar)

> **Phạm vi:** luồng Kata Export → sheet `Dam` của Kata.xlsm → Đọc thép → canvas → Tạo thép Revit
> (`HPRebar/HPRebar.Core/KataRebar`, `HPRebar/HPRebar/KataExport`, `HPRebar/HPRebar/KataRebar`). Chỉ gồm quy tắc **bố trí thép**. Quy tắc **vẽ**
> (dim, tag, nét, mặt cắt) do các golden test của bản vẽ giữ, không nằm ở đây.
>
> **Bản này (2026-10-04) thay bản giai đoạn 1.** Bản cũ lấy số từ tài liệu
> `Q:\My Drive\03_ChuongTrinh\Kata\04_quy_dinh_thep_dam.md`. Tài liệu đó đã được đánh giá lại: phần mô tả ô sheet
> phần lớn đúng, nhưng phần quy tắc hình học phần lớn sai so với bản vẽ Kata
> ([đánh giá](../../plans/261004-1900-kata-beam-rules-rewrite/reports/source-evaluation.md)).
>
> **Bản 2026-10-05 (B01):** đây là **file quy tắc duy nhất** mà Tạo thép Revit phải tuân theo. Nghĩa từng ô nhập của
> sheet `Dam` (hàng 1–28), giá trị B01, bằng chứng DWG và trạng thái HPRebar: [kata-dam-sheet-cells.md](kata-dam-sheet-cells.md).
> Thêm bằng chứng dầm B01 (`T2-DY7.dwg`) và các quy tắc R-128…R-137 lấy từ 04_quy_dinh mà HPRebar chưa có.

## 0. Nguồn, thứ tự ưu tiên, ký hiệu

**Thứ tự nguồn khi mâu thuẫn:** bản vẽ Kata đã đo → code Kata dịch ngược (chỉ đối chiếu) → TCVN 5574:2018 (điều kiện
tối thiểu) → tài liệu `04_quy_dinh_thep_dam.md` → `QUY_TRINH_THUAT_TOAN`. Quyết định user 2026-10-05: DWG B01 > 04_quy_dinh >
QUY_TRINH khi lệch.

| Ký hiệu nguồn | Nghĩa |
|---|---|
| **DWG** | Đo từ bản vẽ Kata `T2-DY7.dwg` (dầm T2-DY7 3 nhịp, T2-DY14 4 nhịp, mặt cắt 1-1…9-9; dầm **B01** 5 nhịp + console, gối bề rộng 0 tại I, mặt cắt 1-1…14-14); khớp ±25 mm, có golden test và chạy live Revit |
| **SEC** | Chỉ khớp bản vẽ mặt cắt (2D), chưa phải mô hình 3D |
| **TCVN** | TCVN 5574:2018 — điều kiện tối thiểu, dùng để cảnh báo |
| **HP** | Quy ước riêng của HPRebar, chưa đối chiếu Kata |
| **CHƯA** | Chưa xác minh (chỉ có test đơn vị hoặc lấy theo tài liệu) |

**Code Kata dịch ngược** (`Q:\…\Kata\scratch`) chỉ dùng để đối chiếu, không chép code. Toàn bộ thân hàm tính thép của Kata
đã bị mã hoá, nên nguồn này **không xác nhận hay bác bỏ con số nào**. Chỉ còn tên trường gợi ý (ghi ở cột ghi chú):
`info_thep_gc_goi.keo`, `tap_cog_hook`, `tap_lap_anchor`, `phi_crank`, `L_max`…
([đối chiếu](../../plans/261004-1900-kata-beam-rules-rewrite/reports/kata-decompiled-crosscheck.md)).

**Trạng thái:** ✅ đã code + test · 🟡 một phần / lệch đã biết · ❌ chưa làm · ➖ không làm (quyết định của user).

**Đường dẫn code:**
- `C/` = `HPRebar/HPRebar.Core/KataRebar/`
- `X/` = `HPRebar/HPRebar.Core/KataExport/Calculators/`
- Test nằm trong `HPRebar/HPRebar.Core.Tests/KataRebar/` (tên lớp.tên test).

Bảng kiểm kê đầy đủ, có số dòng: [hprebar-rule-inventory.md](../../plans/261004-1900-kata-beam-rules-rewrite/reports/hprebar-rule-inventory.md).

**Luật chung:** ô sheet có giá trị thì thắng thiết lập. Ví dụ: G1, G2/G3, H3/H5, I3/I5, G6–G8, J7/I8, J9. Hình học Revit
(b, h từng nhịp, bề rộng gối, nhịp) thì thắng số trên sheet (R-04).

---

## 1. Đọc sheet `Dam` và đối chiếu với Revit

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-01 | Hàng 10 ghi "Cột"/"Nhịp" xen kẽ từ cột C. Danh sách dừng ở ô hàng 11 trống đầu tiên | h10, h11 | DWG | ✅ | `C/Parsers/KataDamSheetParser.cs` · `KataDamSheetParserTests.Parse_GoldenKataSample_*` |
| R-02 | Ô gối hàng 11: số = bề rộng cột; `bxh` (hoặc `b*h`, `b/h`) = gối là **dầm giao** rộng b, cao h | h11 gối | DWG (I 200x350, K 100x350) | ✅ | `KataBarNotationParser`, `KataDamSheetParser` · `Parse_CantileverAndBeamSupport_*` |
| R-03 | Chiều cao bê tông nhịp h_i = B5 − hàng 21 + hàng 19 (đáy theo hàng 21, đỉnh theo hàng 19, âm = thấp xuống). ≤ 0 thì chặn. Hàng 19/21 trống, hoặc chỉ ghi thép không có số (`3f20`), thì lấy giá trị của nhịp trước; số (kể cả `0`) hoặc `-` đặt lại | B5, h19, h21 nhịp | DWG (DY7, B01) + Revit B01 (H 650, console 900) | ✅ | `KataBeamRebarSpec.HeightOf/TopAt`, `KataDamSheetParser.InheritSteps` · `KataSpanCarryOverTests`, `KataB01DrawingTests` |
| R-04 | So sheet với Revit (b, h, từng ô hàng 11, h từng nhịp): lệch ≤ 2 mm bỏ qua; ≤ 50 mm cảnh báo, ghi rõ ô; > 50 mm hoặc khác số cột thì chặn. Revit thắng. Sheet được viết theo chiều nào cũng đọc được; dải đối xứng thì theo chiều ghi | B5, B6, h11, h21 | HP | ✅ | `C/Calculators/KataSheetGeometryCheck.cs` · `KataSheetGeometryCheckTests` |
| R-05 | Chặn khi: không có nhịp; số gối ≠ số nhịp + 1; hàng 11 gối không đọc được bề rộng (chữ, số âm); hai đầu đều console; B11, B12 trống và G6 ≤ 0. Gối giữa ghi `0` không phải gối: hai nhịp hai bên gộp làm một (ô của cột gối và nhịp phải được báo, không vẽ). `0` ở gối đầu/cuối = đầu console, không so với Revit, cảnh báo | h11 | DWG (B01) | ✅ | `P/KataZeroWidthSupports.cs`, `C/Calculators/KataScopeFilter.cs` · `KataSpanCarryOverTests`, `KataScopeFilterTests`, `KataB01DrawingTests` |
| R-06 | Bỏ qua và báo "chưa hỗ trợ": nhóm thứ 2 trở đi của B11/B12 và của hàng 19 / 21 nhịp; hàng 23 nhịp; hàng 24 gối (trừ `*`: ghi chú "đúng như bản vẽ", R-122) | B11/B12, h19, h21, h23, h24 | HP | ✅ | `KataScopeFilter` · `KataScopeFilterTests` |
| R-07 | Hàng 16 ở ô **gối** được đọc là gia cường **trên lớp 4**. Nhãn của template (B15 = B16 = "lớp 3") lại gợi ý hàng 16 là gia cường **dưới lớp 3** | h16 | CHƯA | 🟡 | `KataDamSheetParser` — cần một bản vẽ Kata có hàng 16 để chốt |

**Ô có trên sheet nhưng HPRebar chưa đọc / chưa dùng** (bảng đủ: [kata-dam-sheet-cells.md](kata-dam-sheet-cells.md)):
- I1 (checkbox "cách điểm cắt thép gối = h"): cố ý bỏ qua — B01 có I1 = TRUE mà bước lệch vẫn = G1 (R-42).
- Hàng 19 / 20 / 21 ở ô gối: cột trên, dầm giao `bxh`, lệch dầm giao — đọc số nhưng không dùng; `bxh` ở hàng 20 mất
  (R-128…R-130). Kata Export đã **ghi** hàng 20/21. Hàng 22/23 ô gối (tên trục, lệch trục) chỉ dùng để vẽ.
- B7 / hàng 12 nhịp dạng cú pháp `*h1;down1/…` (chỉ đọc số B7) (R-131).
- Tiền tố Ø ở hàng 22 (`d8a100/200`) (R-132); hàng 23 nhịp (R-133).
- `-` ở ô nhịp các hàng 13–17 (R-134, R-135).
- Hàng 17/18 ở ô gối có bề rộng (R-54).
- Hàng 11 gối số âm (R-136); bốn lựa chọn của I3/I5 (R-137).

## 2. Lớp bảo vệ và vị trí thanh trong tiết diện

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-10 | J9 `a/b`: b = lớp bảo vệ tới mặt ngoài đai; **a chỉ là khoảng cách đầu thanh thép chủ tới mép bê tông** (mút console, mép ngoài cột, chân neo) — không đẩy thép trong mặt cắt (user 2026-10-05: "luôn tựa mặt trong đai như Kata vẽ") | J9 | DWG B01 (J9 50/25: tâm 42.5 ở mặt cắt, mút 50) | ✅ | `KataDetailingRules.TopEndCover/BottomEndCover` · `KataDetailingRuleBuilderTests.The_bars_rest_on_the_stirrup_*` |
| R-11 | J9 chỉ ghi `a` thì b = a − max(d_trên, d_dưới)/2 − Ø_đai. b < 15 thì cảnh báo; b ≤ 0 thì chặn | J9 | HP | ✅ | `C/Calculators/KataDetailingRuleBuilder.cs` · `KataDetailingRuleBuilderTests.A_single_number_*` |
| R-12 | J9 trống: b = 25, thép chủ tựa lên đai | J9 | HP | ✅ | `An_empty_cell_uses_a_25_mm_stirrup_cover_*` |
| R-13 | Mặt cắt: thép chủ luôn tựa mặt trong đai, tâm = b + Ø_đai + d/2 trong Revit (B01 30/25 Ø10/Ø25: 47.5). Canvas vẽ kiểu Kata: đai là line tại b, tâm c + (d + Ø_đai)/2 (B01 42.5) | J9, G6 | DWG | ✅ | `KataDetailingRuleBuilder` · `A_bar_cover_smaller_than_the_stirrup_moves_only_the_ends`; canvas `KataSectionBars.Lay` |
| R-138 | Revit: Rebar Cover Top/Bottom/Other của dầm = loại cover **có sẵn** trong model có khoảng cách b (±0.5 mm); không có → giữ cover cũ + cảnh báo; có thanh không do Kata Rebar vẽ trên dầm → cảnh báo có thể dịch. Đặt sau khi xoá thép cũ, trước khi vẽ | J9 | User 2026-10-05 | ✅ live (B01 copy: "Rebar Cover 25mm", 0 mặt phải đổi) | `KataHostCoverService` |
| R-139 | Tag đai trong (U/C hàng 25–44) ở mặt cắt: cột bên phải, đầu tag b/2 + 275 + b/4; leader ngang từ chân phải U (tâm thanh + (d+Ø_đai)/2) / chân dài C (tâm thanh − (d+Ø_đai)/2), mỗi C một leader vào chung tag "nxØ..a..."; tag đai trong đầu tiên cách đỉnh 250, 300 khi bên phải đã có tag phía trên (lớp 2 trên), và 125 dưới tag phải ở nửa trên mà nó chạm (tag cốt giá); tag sau thấp hơn 125; C cách tag phải phía dưới < 125 → đưa lên trên dầm (z +62.5, leader gãy b/2 + 37.5). Mặt cắt có cốt giá (B01 1-1…3-3: U −820.8) chưa tìm ra quy tắc | DWG B01 | DWG B01 4-4…14-14 + B02 1-1…14-14 (trừ 1-3 U/C) ±1 mm | 🟡 | `KataSectionTags.InnerStirrups` · `KataB01DwgStirrupTagTests` |
| R-14 | Theo phương ngang: 2 thanh ngoài chạm mặt trong đai, các thanh còn lại chia đều; 1 thanh thì nằm giữa | B6, J9, G6 | DWG | ✅ | `KataRebarCalculator` · `Calculate_TransverseYCentering_*` |
| R-15 | Khe thông thủy giữa 2 **lớp**: mô hình dùng max(`LayerClearGap`, d lớn); bản vẽ Kata cho 25 | 30 (`LayerClearGap`) | 🟡 DWG/SEC = 25 | 🟡 | `KataDetailingRules.LayerGap` · `KataSupportTopBarTests` — **chưa chốt 30 hay 25** |
| R-16 | Khe giữa 2 thanh **trong một lớp** ≥ max(25, d): nhỏ hơn thì cảnh báo, chồng nhau thì chặn | 25 (hằng) | TCVN | ✅ | `KataLayerPositions.CheckSpacing` · `Bars_too_close_in_a_layer_are_reported` |

## 3. Thép chủ B11 / B12

| ID | Quy tắc | Ô | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-20 | Thép chủ: nhóm đầu của B11 (trên) và B12 (dưới); nhóm đầu ở hàng 19 / 21 của một nhịp thay thép chủ trên / dưới **từ nhịp đó trở đi** (kế thừa; ô chỉ có số giữ thép nhịp trước). So thép theo số thanh + Ø (không theo cách ghi). Tại gối đổi thép, thanh bị cắt một lần (phía nhiều thép hơn neo), mỗi đoạn lấy thép của nhịp chứa nó, mặt ngoài vẫn sát đai; neo / nối tính theo Ø của chính đoạn: trên nối G3·max(d nối, d neo) từ đầu thanh neo, dưới chạy G3·d của mình qua mặt gối phía mình. Gối đổi cả cao độ đỉnh và thép trên → chặn | B11, B12, h19, h21 | DWG B01 (L `3f20` → mặt cắt 11–14: 3Ø20; dưới L 24400…31800 = 25000 − 30·20 … 31200 + 30·20) | ✅ | `C/Calculators/KataMainBarSwap.cs`, `KataWidthProfile`, `KataBottomMainBarRuns` · `KataB01DrawingTests.The_narrow_spans_*`, `KataConsoleAndSpanBarsTests` |
| R-21 | Thép chủ trên: một thanh từ gối đầu tới gối cuối, neo hai đầu (mục 4), **theo cao độ đỉnh**: tại gối đổi cao độ uốn dốc 1:6 nằm giữa gối khi `Cranks` cho phép (Ø ≥ 16, (Δ − Ø)/b_gối ≤ 1/6); tại điểm gối rộng 0 uốn từ điểm đó sang nhịp sau (Δ ≤ 100); ngược lại cắt: thanh phía cao neo xuống trong gối như gối biên, thanh phía thấp chạy thẳng chồng G3·d tính từ đầu thanh đã neo. Chân móc ở đỉnh hạ thấp hạ theo, giữ trên đáy một lớp bảo vệ | B11, G2, G3, h19 | DWG B01 (17750→18050, 20600→20900, tách tại M 31550 / 30800) | ✅ | `C/Calculators/KataTopProfile.cs` · `KataB01DrawingTests.The_top_bars_crank_*` |
| R-22 | Thép chủ dưới bám đáy từng nhịp. Các nhịp cùng đáy dùng chung một thanh; tại bậc đáy thì uốn hoặc cắt (mục 9) | B12, G3, h21 | DWG | ✅ | `C/Calculators/KataBottomMainBarRuns.cs` · `KataDy7DrawingTests.Bottom_main_bars_crank_*` |
| R-22a | Gối đổi bề rộng (hàng 20): mọi thanh dọc qua gối bị cắt — thanh phía nhịp rộng neo trong gối (trên bẻ xuống, dưới bẻ lên), thanh phía nhịp hẹp chạy thẳng chồng G3·d tính từ đầu thanh đã neo; thanh thuộc một phía chỉ rút ngắn. Thanh, đai, đai trong, cốt giá lấy bề rộng của nhịp chứa chúng (căn giữa) | h20 | DWG B01 tại K (24945 / 24200) | ✅ | `C/Calculators/KataWidthProfile.cs` · `KataB01DrawingTests.The_narrow_spans_*` |
| R-23 | Mỗi thanh là **một cây liền**, dài bao nhiêu cũng vậy: không chia 11.7 m, không nối chồng, không coupler. Phần nối do Rebar Schedule tính | — | quyết định user 2026-10-01; Kata cũng vẽ thanh 16.2 m liền | ➖ | `KataMultiSpanPlanTests.A_main_bar_longer_than_a_stock_bar_*` |

## 4. Neo tại gối biên

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-30 | Chiều dài neo yêu cầu: thép trên G2·d, thép dưới G3·d. Ô trống thì 40 / 30 | G2, G3 | DWG | ✅ | `KataDetailingRuleBuilder` · `Anchorage_factors_come_from_G2_and_G3` |
| R-31 | Đủ chỗ (bề rộng gối − a ≥ yêu cầu) thì neo thẳng, đầu thanh cách mép trong đúng chiều dài yêu cầu | — | HP | ✅ | `C/Calculators/KataAnchorage.cs` · `KataAnchorageTests.A_wide_support_holds_the_bar_straight` |
| R-32 | Thiếu chỗ thì thanh chạy tới mép ngoài − a rồi bẻ 90° (trên bẻ xuống, dưới bẻ lên). **Chân = đúng phần còn thiếu**, tối thiểu `MinimumLegFactor`·d | 0 (`MinimumLegFactor`) | DWG | ✅ | `KataAnchorage` · `KataSpecRuleTests.A_leg_bends_exactly_the_missing_length_by_default` |
| R-33 | Chân làm tròn **lên** bội 25 nếu vẫn lọt; không lọt thì giữ số chính xác | 25 (`RoundLegMm`) | DWG (tròn lên 25) | ✅ | `A_leg_is_rounded_up_to_25_*`, `A_rounded_leg_that_would_not_fit_*` |
| R-34 | Chân bị giới hạn theo chiều cao: SupportDepth − tâm trên − tâm dưới; thiếu thì cảnh báo. Kata vẽ chân thò quá mặt dầm (300/400) | B5, h21, `bxh` | 🟡 DWG | 🟡 | `KataAnchorage` · `A_leg_longer_than_the_beam_allows_is_clamped_*` |
| R-35 | Chân dưới chồng chân trên thì chân dưới lùi vào (d₁ + d₂)/2 + max(25, d). Kata không lùi (DY7: x 35, HPRebar 133) | 25 (hằng) | HP | 🟡 | `KataAnchorage.BottomLegInset` · `A_bottom_leg_moved_inboard_*` |
| R-36 | Gối là dầm giao (`bxh`): chân không sâu quá h của dầm giao | h11 `bxh` | DWG | ✅ | `KataBeamRebarSpec` · `KataSteppedBeamEdgeTests.A_crossing_beam_support_limits_*` |
| R-37 | Neo theo **bảng chiều dài neo/nối theo Ø** (tab "Thông số đặc thù" của Kata; code Kata có `tap_lap_anchor`) thay cho G2/G3·d | — | CHƯA | ❌ | — |

## 5. Gia cường gối (hàng 13–16)

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-40 | Hàng 13 nằm cùng cao độ thép chủ trên, chen vào khe giữa các thanh chủ (thừa thì vào khe gần giữa trước). Hàng 14–16 xếp lớp xuống dưới, trải hết bề rộng; hàng trống không chiếm chỗ | h13–h16 | DWG | ✅ | `KataLayerPositions`, `KataTopLayerStack` · `Gaps_take_extra_bars_nearest_the_centre_first` |
| R-41 | **Điểm cắt:** mọi hàng 13–16 vươn RoundUp50(H5 × L_phía) tính từ mép gối (hoặc tâm gối nếu I5 có chữ "tâm"). L_phía là nhịp thông thủy **của chính phía đó**, không lấy nhịp lớn hơn. H5 trống thì dùng 0.25. H3/I3 **không** áp cho thép trên | H5, I5 | DWG | ✅ | `KataSupportTopBarLayout.Cuts` · `KataTopBarStaggerTests.Every_row_reaches_H5_of_its_own_span_*` |
| R-42 | **G1 = khoảng so le giữa các lớp:** ở mỗi phía gối, hàng ngoài phải vươn xa hơn hàng có thép liền trong nó ít nhất RoundUp50(G1). Chỉ điểm cắt của hàng ngoài bị đẩy ra. Không vượt mặt gối bên kia; thiếu chỗ thì cảnh báo. **I1 không đổi quy tắc này** (B01: I1 = TRUE, E14 trái vẫn 7700 = 8200 − G1, không phải − h) | G1 > 0, trống → 500 (`CurtailedExtensionMm`, 0 = tắt); I1 bỏ qua | DWG (DY7, B01 E14/E15); nhãn ENG "Length of between additional bar on 2 layer". Code Kata có cờ `keo` cho từng nhóm thép gia cường — khớp với cách hiểu này | ✅ | `C/Calculators/KataTopBarStagger.cs` · `KataTopBarStaggerTests` |
| R-43 | G1 chỉ nhận **một số dương**. Chữ hoặc nhiều số (`-300;11700`) thì dùng thiết lập và cảnh báo | G1 | HP | ✅ | `KataSpecRuleTests.G1_is_read_only_as_one_positive_number` |
| R-44 | `-` ở hàng 13 (ô gối) = thanh hàng 13 của gối kề kéo tới gối này; chuỗi `-` liên tiếp nối tiếp nhau. Tới gối biên thì neo (console thì dừng cách mút một lớp bảo vệ); tới gối giữa thì qua gối và cắt H5·L (+G1 nếu gối đó có hàng dưới) | h13 `-` | DWG (DY7 G→I, DY14 G→K) | ✅ | `C/Calculators/KataTopBarContinuation.cs` · `KataDy14DrawingTests.At_E_350_*` |
| R-45 | `-` ở hàng 14–16 chưa xử lý: cảnh báo, không vẽ. Chuỗi `-` bị hai phía cùng nhận thì thanh bị nhân đôi, chỉ cảnh báo | h14–16 | CHƯA | 🟡 | `A_dash_in_row_14_is_reported_*`, `A_dash_chain_between_two_supports_*` |
| R-46 | Ô `trái;phải`: mỗi phía một bộ thanh. Gối biên chỉ lấy vế phía nhịp. Ở gối giữa, phía có nhiều thép hơn (Σn·d²) neo ở mặt xa của gối; phía yếu chạy xuyên qua + G2·d sang nhịp bên kia. **Lớp ≥ 2, hai nhịp kề khác bề rộng:** mỗi phía rải riêng theo bề rộng nhịp của nó, dưới các thanh góc (B01 K `2f20;2f16`: 2Ø16 ở góc nhịp L 300, mặt cắt 11-11); cùng bề rộng thì chia xen kẽ để không va nhau | h13–16 | HP + DWG B01 11-11 | ✅ | `KataSupportTopBarTests.An_interior_support_with_different_sides_*`, `KataB01DwgSectionTests` |
| R-47 | Điểm cắt không ra khỏi đầu dầm (chặn ở gối biên − a). Ở gối giữa có nhịp ngắn, thanh có thể dừng trong lòng cột bên kia | — | HP | 🟡 | `KataSupportTopBarLayout` |
| R-48 | Lớp gia cường dưới cùng phải hở với thép chủ dưới ≥ khe lớp; không đủ thì chặn | — | HP | ✅ | `More_layers_than_the_depth_holds_block` |

## 6. Gia cường bụng (hàng 17–18)

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-50 | Hàng 18 = lớp 1, cùng cao độ thép chủ dưới, chen vào khe. Hàng 17 = lớp 2, nằm trên, trải hết bề rộng; thanh lớn ra mép. Hàng 17 đứng một mình thì lên lớp trên thép chủ | h17, h18 | DWG | ✅ | `C/Calculators/KataSpanBottomBarLayout.cs` · `KataSpanBottomBarTests` |
| R-51 | **Điểm cắt hàng 17** (hoặc hàng 18 khi đứng một mình), tính từ mỗi mặt gối: min(RoundUp50(H3·L) theo I3, RoundNearest50(L/6)). H3 trống thì dùng 0.20 | H3, I3, `BottomExtraCutFraction` 1/6 | DWG | ✅ | `KataDy7DrawingTests.Additional_bottom_bars_stop_at_the_smaller_of_H3_L_and_L_over_6_*` |
| R-52 | Hàng 18 khi có hàng 17: dừng gần gối hơn hàng 17 một khoảng G1 (≥ 0, không vượt mặt gối) | G1 | DWG (D18 850 / D17 1350) | ✅ | cùng test R-51 |
| R-53 | Thanh thẳng, không móc. Hai điểm cắt gặp nhau thì không vẽ và cảnh báo. Chạm thép trên thì chặn | — | DWG / HP | ✅ | `Row_17_blocks_where_it_meets_support_bars_*` |
| R-54 | Hàng 17 / 18 tại gối bề rộng 0 (nút giao, nhịp gộp): thép dưới qua nút → thành thép gia cường dưới của nhịp gộp, cắt theo R-51 — chỉ khi nhịp gộp chưa có hàng 17/18 riêng và nút nằm trong L/6…5L/6; ngược lại báo. Tại gối có bề rộng (hoặc không đọc được bề rộng): báo chưa hỗ trợ; ô `0` / `-` = trống | h17, h18 gối | DWG B01 (I17 2Ø20 lớp 2: Kata 18950…23300 = 850 / 1300 từ mặt gối, HPRebar 19200…23500 — quy tắc cắt Kata tại nút chưa rõ) | 🟡 | `KataZeroWidthSupports.BarsThroughTheJoint` · `KataJointCellsTests` |

## 7. Đai

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-60 | Bước đai: G7 = vùng dày, G8 = vùng giữa, G9 = console. Viết `a150`, `@150` hay `150` đều được. Ô trống thì 150 / 200 / 150 | G6–G9 | DWG | ✅ | `KataBarNotationParser.ParseStirrupSpacing` |
| R-61 | G6 = 0 thì không có đai. G6 **trống** thì parser cho Ø10 | G6 | HP | 🟡 | `KataDamSheetParser` — ô trống ≠ "không đai" |
| R-62 | **Vùng đai dày** ở mỗi đầu nhịp = min(Ln/2, Ceil50(max(k·h_nhịp, 0.25·Ln))). Mặc định k = 0, tức 0.25·Ln. Đặt k = 2 để có max(2h, 0.25Ln) cho dầm kháng chấn | `DenseZoneHeightFactor` 0, `EndZoneFraction` 0.25 | DWG (1400/1750/550) | ✅ | `KataDetailingRules.DenseZoneLength` · `Dense_stirrup_zones_are_a_quarter_of_each_span` |
| R-63 | Đai đầu và đai cuối cách mặt gối 50 | 50 (hằng) | DWG | ✅ | `Stirrups_run_16_at_a100_then_14_at_200_*` |
| R-64 | Vùng dày chia đều với bước ≤ G7. Vùng giữa chia đều với bước ≤ G8 (khe ≥ 2.5s thì chia đều; khe hẹp hơn thì 1 hoặc vài đai). Tag ghi bước danh định của sheet; Revit dùng bước thực đặt | G7, G8 | DWG | ✅ | `C/Calculators/KataStirrupZoneLayout.cs` · `Stirrup_gaps_never_exceed_the_spacing_of_their_zone` |
| R-65 | Hai vùng dày chạm nhau (nhịp ngắn) thì không có vùng giữa: lưới đai tính từ hai mặt gối, không có đai nào gần nhau hơn ½ bước | — | DWG (DY14 nhịp 250) | ✅ | `Dense_zones_that_fill_the_span_*`, `Stirrups_of_a_short_span_*` |
| R-66 | Hộp đai từng vùng: (b_nhịp − 2c) × (h đến đỉnh của vùng − 2c), đường tim ở c + Ø_đai/2. Vùng đai đi qua bậc đỉnh trong nhịp bị chia tại đó, mỗi phần cách bậc 50 và chia đều lại | J9, h19, h20, h21 | DWG (DY7, B01 19950…20550 / 20650…22750) | ✅ | `KataStirrupZoneLayout` · `KataB01DrawingTests.Stirrups_follow_*` |
| R-67 | Hàng 22 nhịp `a/b[/c]` ghi đè bước vùng dày / giữa / vùng phải của nhịp đó | h22 nhịp | CHƯA | ✅ | `KataInnerStirrupTests.Row_22_sets_the_span_spacings_*` |
| R-68 | Đai trong hàng 25–44: mỗi cặp ô (loại □ / U / C, thanh `a-b`) theo **thép chủ trên**, thanh 1 ở **bên trái mặt cắt Kata** (Kata nhìn dọc dầm theo +X — cờ mặt cắt chỉ chiều đó — nên trái = +Y). U: hai nhánh ôm ngoài thanh a, b, **hở trên**, đầu nhánh bẻ vào 4Ø rồi xuống 5,5Ø (không móc). C: nhánh dài bên trái thanh, móc 180° ôm thanh sang phải, mọi C cùng chiều. □ `1-n` là đai ngoài. **Bước theo I8**: I8 = 2 → cách đều J7 như móc C (sớm hơn móc C 1Ø đai), chạy qua cả nút giao; I8 = 1 → theo từng đai ngoài + Ø đai. Mặt cắt canvas vẽ U/C và ghi `Ø10 a500` (U) / `2xØ10 a500` (các C cùng số). **Đọc theo cặp cột của nhịp**: (C,D) = nhịp D, (E,F) = nhịp F, …, (M,N) = console N; mỗi hàng 25…44 một mục. **Nhịp không có mục kế thừa nhịp trước khi thép chủ trên giống nhau** (B01: U 3-4, C 2, C 5 của nhịp 1 có ở F, H, J — mặt cắt 4–10); thép chủ trên đổi (L 3Ø20) thì không có U/C. **Thanh đếm trên thép chủ trên của chính nhịp** (hàng 19 hoặc kế thừa), không phải B11: console B01 `Đai C 2` → thanh giữa của 3Ø20. Nhịp gộp qua gối bề rộng 0 dùng cặp của nửa trái; cặp nửa phải khác thì báo | h25–44, I8, J7 | DWG B01 mặt cắt 2-2 (U −59…59, lề 40, móc 55; C2 −145/−105, C5 104/144; số hiệu 24, 25); 14-14 (console: C quanh thanh giữa của 3Ø20, không U); 13-13 (L: không U/C); 4-10 (kế thừa) | ✅ | `C/Calculators/KataInnerStirrupLayout.cs`, `KataSectionLines.InnerU/InnerC` · `KataInnerStirrupTests`, `KataSectionInnerStirrupTests`, `KataB01DwgSectionTests` |
| R-69 | Xoá một vùng đai trên canvas thì xoá luôn đai U/C trong vùng đó | — | HP | ✅ | `KataLayoutRemovalTests.Inner_stirrups_go_with_their_zone_*` |

## 8. Cốt giá và đai C

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-70 | Cốt giá chung: \|G5\| lớp, mỗi lớp 2 thanh Ø G4 (một thanh mỗi mặt bên). G5 < 0 thì giữ các lớp nhưng bỏ đai C | G4, G5 | DWG | ✅ | `KataDamSheetParser` · `KataSideBarTests.A_negative_G5_*` |
| R-71 | Hàng 20 nhịp `nfd` = n lớp × 2 thanh, ghi đè G5/G4 cho nhịp đó. `0` = không có cốt giá, cố ý nên không cảnh báo. Ô trống (từ nhịp thứ 2) lấy cốt giá của nhịp trước; `-` đặt lại về không có. Số đứng đầu ô (`300`, `300;2f12`) = **bề rộng nhịp**, kế thừa sang nhịp sau | h20 nhịp | DWG (DY14, B01) + Revit B01 (L, console 300) | ✅ | `C/Calculators/KataSideBarLayout.cs`, `KataDamSheetParser` · `KataSpanCarryOverTests` |
| R-72 | Cao độ các lớp chia đều giữa tâm thép chủ trên và tâm thép chủ dưới của **nhịp nông nhất** trong nhóm; thanh chạm mặt trong đai | — | DWG | ✅ | `G5_layers_spread_between_the_main_bars_*` |
| R-73 | Các nhịp liền nhau có cùng cốt giá dùng chung một thanh chạy qua gối giữa. Neo 10d ở hai đầu nhóm: gối biên min(10d, bề rộng − a); ranh giới nhóm ở gối giữa min(10d, b_gối/2 − d/2) | `SideBarAnchorageFactor` 10 | DWG | ✅ | `KataDy7DrawingTests.Side_bars_run_on_through_E_*` |
| R-74 | h ≥ 700 mà không có cốt giá (G4/G5 không cho lớp nào và hàng 20 trống) thì cảnh báo "thiếu cốt giá", **không tự sinh** | `SideBarRequiredHeight` 700 | TCVN 5574 §10.3.1.2 | ✅ | `KataSpecRuleTests.A_deep_beam_without_side_bars_*` |
| R-75 | Đai C giữ cốt giá: mỗi nhịp, mỗi lớp một bộ, Ø = Ø đai, móc 180°/7.5d. HPRebar đặt **đoạn thẳng dưới** cốt giá; bản vẽ mặt cắt Kata đặt đoạn thẳng ở trên, đuôi ở dưới | G6, `CrossTieHook*` | 🟡 SEC | 🟡 | `KataSideBarLayout`, `KataTieWrap` · `KataTieWrapTests` |
| R-76 | Đai C kê lớp 2: tiết diện nào cắt qua ≥ N thanh của lớp gia cường trong (hàng 14–16 ở gối, hàng 17 ở nhịp) thì có đai C ôm 2 thanh ngoài cùng, đoạn thẳng nằm dưới lớp. **Chỉ lớp đúng 3 thanh** (DY7/DY14 3 thanh: có; B01 6 thanh / b 500 và 2 + 2 Ø16 chồng nhau sát gối M: không — số hiệu Kata không chừa số cho thanh đó). Đai C cách đầu thanh và mặt gối 50 + 2·Ø_đai, không đặt trong gối | `LayerTieMinBarCount` 3 | DWG | ✅ | `C/Calculators/KataLayerSpacerTieLayout.cs` · `KataLayerSpacerTieTests` |
| R-77 | Bước mọi đai C: I8 = 2 thì chia đều a(J7); I8 = 1 thì đặt cạnh mỗi đai ngoài (lệch 2·Ø_đai). J7 trống, sai hoặc ngoài 100–1000 thì a500 + cảnh báo | I8, J7 | DWG (I8 = 2); I8 = 1 CHƯA | ✅ | `C/Calculators/KataTieStations.cs` · `Parse_J7_and_the_option_group_in_I8_*` |
| R-78 | Hàng 20 không giới hạn số lớp (`10f12` ra 10 lớp) | h20 | CHƯA | 🟡 | — |

## 9. Bậc đáy (hàng 21) và đổi chiều cao

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-80 | Gối giữa có chiều cao = nhịp nông hơn trong hai nhịp kề; gối biên = nhịp của nó; không quá h của dầm giao | — | DWG | ✅ | `KataBeamRebarSpec` |
| R-81 | Thép chủ dưới **uốn 1:6** qua bậc khi Ø ≥ `CrankMinDiameter` **và** (\|bậc\| − Ø) / bề rộng gối ≤ 1/6. Đoạn uốn dài 6·\|bậc\|, tính từ mặt gối phía nhịp nông vào nhịp sâu | `CrankMinDiameter` 16, 1:6 (hằng) | DWG (DY7 82/500 uốn, DY14 82/350 cắt) | ✅ | `KataDetailingRules`, `KataBottomMainBarRuns` · `KataDy14DrawingTests.At_E_*`, `Only_bars_of_16_and_up_are_cranked` |
| R-82 | Không uốn được thì **cắt**: thanh nhịp sâu chạy tới mặt xa − a rồi bẻ lên (chân ≤ bậc − Ø − khe lớp, không chạm thép trên); thanh nhịp nông chạy thẳng G3·d qua mặt gối phía nó. Đoạn uốn chồng nhau hoặc vướng console cũng chuyển sang cắt | G3 | DWG | ✅ | `The_deep_bar_at_a_cut_step_*`, `Two_cranks_that_would_overlap_*` |
| R-83 | Cắt khi \|bậc\| − Ø < khe lớp thì 2 thanh chạm nhau: chỉ cảnh báo, chưa sửa hình | — | CHƯA | 🟡 | `A_small_step_that_must_be_cut_is_reported_*` |
| R-84 | Đai, cốt giá và gia cường bụng theo chiều cao từng nhịp | h21 | DWG | ✅ | `KataDy7DrawingTests` |
| R-85 | DY14: tại gối bị cắt, Kata vẽ đầu trái của các thanh ở gối đó ngắn hơn 75 mm. HPRebar chưa làm theo vì chưa rõ quy luật | — | DWG (lệch) | 🟡 | `At_E_350_the_bars_match_the_drawing_except_*` |
| R-86 | Bậc đỉnh (hàng 19) và đổi thép chủ đã làm (R-03, R-20, R-21). Còn thiếu: **một gối** vừa đổi cao độ đỉnh vừa đổi thép trên (`100;5f25`) → đang chặn; nhóm thép thứ 2 của ô | h19, h21 | CHƯA | 🟡 | `KataScopeFilter` (chặn + báo) |
| R-87 | Đổi bề rộng giữa các nhịp: Kata B01 **cắt + neo** tại gối (R-22a). Phương án "bẻ ≤ 1:6 gom vào" của 04_quy_dinh §10.1 không thấy trên DWG → không làm | h20 nhịp số thuần | DWG B01 (K) | ✅ | theo R-22a |

## 10. Nhiều nhịp, dầm giao, console

| ID | Quy tắc | Ô | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-90 | Số nhịp bất kỳ. Gốc x = mép ngoài gối đầu. Bề rộng gối, nhịp, chiều cao lấy theo Revit | — | DWG (3 và 4 nhịp) | ✅ | `C/Calculators/KataBeamStations.cs`, `KataRebarPlanner` · `KataMultiSpanPlanTests` |
| R-91 | Kata Export: gộp gối chồng nhau (cột > móng > dầm); dầm nhập vào cột thành trạm giao; gối chỉ là dầm thì ghi `bxh`; bê tông lót ≤ 100 bỏ qua; móng > 6000 dọc dải = móng băng/bè, không phải gối. Hàng 20: ô gối = dầm giao nhập vào cột `bxh` (`b` khi h = B5), không thấy dầm giao thì giữ ô người dùng nhập; ô nhịp = bề rộng nhịp từ Revit (ghi ở mọi nhịp khi có nhịp khác B6) ghép trước cốt giá người dùng nhập (`300;2f12`, giữ `0`) | h11, h21 | DWG | ✅ | `X/KataSegmenter.cs`, `X/KataRowBuilder.cs` · `KataSegmenterTests`, `KataRowBuilderTests` |
| R-92 | Console (hàng 11 = 0 ở đầu/cuối, cả hai đầu khi có gối giữa, cả dầm 1 nhịp): thép chủ trên dừng cách mút a, bẻ xuống tới cách thép dưới console một thanh (tâm–tâm (d₁+d₂)/2 + max d); thép dưới riêng của console từ cách mặt gối a, móc lên 10·max(d, Ø B12) nhưng không vượt dưới thép trên tại gối (thiếu → cảnh báo), tới cách mút a; thép dưới nhịp trong chạy G3·d của mình qua mặt gối vào console; console gần cùng đáy nhịp → cảnh báo thanh chồng; thép gia cường gối vươn vào console chạy tới mút, nối G3·max(d, Ø thép chủ nhịp neo); cốt giá dừng cách mút a; đai từ mặt gối + 50 tới (mút − a − 50), chia đều bước ≤ G9; **hộp đai theo đỉnh / đáy của chính console** (B01 N19 −200, N21 0: đai z −225…−1075, hộp 250 × 850); console nhận thép chủ trên kế thừa (B01: 3Ø20 của L) | G9, J9 | DWG B01 (N: 33550, chân −1030 trên thanh dưới −1070; dưới 31250…33550 móc 250; M14 30960…33550; cốt giá 31480…33550; đai Ø10a150 31650…33500, z −225…−1075) | ✅ | `KataMainBarLayout`, `KataBottomMainBarRuns.ConsoleRun`, `KataSupportTopBarLayout`, `KataTopProfile`, `KataSideBarLayout` · `KataB01DrawingTests.The_console_gets_*`, `KataConsoleAndSpanBarsTests`, `KataRebarCalculatorTests.*Cantilever*` |

## 11. Móc và uốn

| ID | Quy tắc | Thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-100 | Đai □/U trong: móc 135°, đuôi 7.5·Ø. Đai C (trong, giữ cốt giá, kê lớp): 180°, đuôi 7.5·Ø. Góc khác 90/135/180 thì về mặc định | `ClosedStirrupHook*`, `CrossTieHook*` | hộp "Detail thép" Kata | ✅ | `KataSettings` · `KataSettingsTests.Defaults_are_those_of_the_Kata_detail_dialog` |
| R-101 | Móc **đai ngoài** theo RebarShape của dự án (41/T1), đặt sao cho hai móc ở **góc trên phải của mặt cắt Kata** (−Y). Móc C cốt giá: thân trên cốt giá, móc ôm xuống (DWG 2-2). Móc C kê lớp: thân dưới lớp thép | — | DWG B01 2-2 (đai: 168,−22 · 208,18 … 185,−40; C cốt giá: thân −650 trên thanh −670) | ✅ | `KataStirrupSetCreator`, `KataSideBarLayout.Tie` · `KataSideBarTests` |
| R-102 | Móc theo **bảng Ø** (bán kính uốn, móc 135/180 theo từng Ø; code Kata có `tap_cog_hook`) | — | CHƯA | ❌ | — |
| R-103 | Thanh dọc là polyline góc nhọn; bán kính uốn do bar type Revit quyết định. Đai C ôm thanh với độ lệch = bán kính uốn của bar type | — | HP | ✅ | `KataTieWrap`, `KataBarSetCreator` |

## 12. Số hiệu thép

| ID | Quy tắc | Nguồn | TT | Code · Test |
|---|---|---|---|---|
| R-110 | Thứ tự đánh số: **mọi đoạn** thép chủ trên → các đoạn thép chủ dưới (trái → phải) → gia cường gối theo vị trí gối (hàng 13 → 16; gối bề rộng 0 đánh cả hàng 17/18 của nó ở đây — B01 I17 = 12) → gia cường bụng theo từng nhịp → cốt giá → **theo từng nhịp: đai C (một số cho mỗi Ø + bề rộng nhịp), đai ngoài, đai U/C** (đoạn đổi cao độ đỉnh tách số) → vai bò cuối. Khớp toàn bộ 1…35 của B01 (`KataB01DwgNumberTests`) và DY7/DY14. B01: thép chủ trên 6Ø25 = 1, thép chủ dưới 6Ø25 = **4** (04_quy_dinh / QUY_TRINH ghi 2 — DWG thắng), số đai 23…35 | DWG (DY7, DY14); B01 chưa test | ✅ / 🟡 B01 | `C/Calculators/KataBarNumbering.cs` · `KataBarNumberingTests` |
| R-111 | Thanh giống nhau (cùng Ø, cùng hình, mọi kích thước lệch ≤ 1 mm, kể cả ảnh gương hay đảo đầu) dùng chung số. Mọi đai C cùng Ø chung một số | DWG | ✅ | `T2_DY7_takes_Kata_s_numbers_*` |
| R-112 | Xoá nhóm thép trên canvas thì đánh số lại liên tục 1..n theo R-110 và R-111; Tạo thép Revit bỏ đúng các nhóm đã xoá | HP | ✅ | `C/Calculators/KataLayoutRemoval.cs` · `KataLayoutRemovalTests` |

## 12a. Nút giao trong nhịp (dầm giao, cột cấy — từ model Revit)

| ID | Quy tắc | Ô / thiết lập | Nguồn | TT | Code · Test |
|---|---|---|---|---|---|
| R-120 | **Dầm giao gác lên nhịp** (đáy dầm giao cao hơn đáy dầm, lấy từ model Revit — sheet không có ô): mỗi bên mặt dầm giao 5 đai ngoài Ø đai a50, đai đầu cách mặt 50; đai của nhịp dừng/nối lại cách đai gia cường một bước của vùng, chia đều lại; vai bò 2Ø16: nằm ngang 150 ở cao độ thép chủ trên, xiên 45° xuống dưới đáy dầm giao (đáy − b − d/2, không thấp hơn thép chủ dưới), nằm ngang từ mặt−50 tới mặt+50. Nút cách mặt cột ≤ 200 → dồn cả 10 đai về phía giữa nhịp. Nút gần nhau → chung một bộ đai. Vai bò không ra khỏi nhịp (quy tắc HPRebar): thiếu chỗ thì bỏ đoạn ngang, dốc 60°, cuối cùng dừng tại mặt gối + cảnh báo. Gối có cột (kể cả có dầm giao ở hàng 20 — B01 gối 1 `C20 400x500`) không có đai nút, có `*` ở hàng 24 hay không. Số hiệu vai bò sau cốt giá | `KataDetailingRules.Joint*`, `Hanger*` (mặc định theo DWG) | DWG B01 (giữa D: đai 4850…5050 ‖ 5550…5750, đai nhịp 3200…4650 ‖ 5950…8000, vai bò 2Ø16 4000·4150·5050‖5550·6450·6600) | ✅ | `C/Calculators/KataJointStirrups.cs`, `KataHangerBarLayout.cs`, `KataSupportCollector.CollectWithLoads` · `KataJointLoadTests`, `KataSupportRulesTests.LoadsFramingIn*` |
| R-121 | **Cột cấy** (cột đứng trên nhịp, từ model Revit): như R-120, vai bò xuống tới cao độ thép chủ dưới của dầm | như R-120 | DWG B01 (F: đai 14350…14550 ‖ 15050…15250, đai nhịp …14150 ‖ 15450…, vai bò 13750·13900·14550‖15050·15700·15850) | ✅ | như R-120 |

## 13. Chưa làm (backlog, mỗi mục một plan)

| ID | Chủ đề | Ô / thiết lập Kata | Căn cứ | TT |
|---|---|---|---|---|
| R-122 | Đai gia cường nhịp (hàng 23 nhịp, xem R-133) và đai chống xoắn (C24 số = đai ngoài thành 2 đai U ghép, nối chồng C24·Ø đai) chưa vẽ, báo chưa hỗ trợ. Hàng 24 gối `*` = không bố trí đai gia cường nút: DWG B01 không có đai nút ở **gối cột nào** (K có `*`; gối 1 không `*` cũng không có) → HPRebar không vẽ đai nút ở gối → ghi nhận "đúng như bản vẽ" | h23, h24 | DWG B01 (h24); C24 theo 04_quy_dinh, CHƯA đo | 🟡 | `KataDamSheetParser.ParseSupport` · `KataJointCellsTests.K24_star_*` |
| R-123 | Lỗ xuyên dầm (vùng được khoét, thép gia cường quanh lỗ) | — | TCVN; không tìm thấy trong code Kata | ❌ |
| R-124 | Nhiều nhóm thép chủ (`;` ở B11/B12) | B11, B12 | sheet | ❌ |
| R-125 | Kiểm tối thiểu theo TCVN: bước đai lớn nhất, hàm lượng thép tối thiểu, thép chạy suốt ≥ 2 thanh. Chỉ cảnh báo, không đổi thép | — | TCVN (cần dẫn điều khoản đúng trước khi làm) | ❌ |
| R-126 | Con kê / thép kê cách lớp | H6 (`25@2000`) | sheet | ❌ |
| R-127 | Chia thanh 11.7 m, nối chồng, coupler, vùng nối 0.25L / 0.2L | tab "Detail thép" | quyết định user (R-23) | ➖ |

## 13a. Quy tắc từ 04_quy_dinh mà HPRebar chưa có (2026-10-05)

Nguồn: `04_quy_dinh_thep_dam.md` §3.2 (dòng ghi ở cột Nguồn). Chưa có bằng chứng DWG trừ khi ghi rõ; DWG thắng khi có.

| ID | Quy tắc | Ô | Nguồn | TT | Code hiện tại |
|---|---|---|---|---|---|
| R-128 | Ô gối hàng 19 = **cột tầng trên** `b` hoặc `b;lệch` (lệch so với mép trái cột dưới); `0` = không cột trên; trống = như cột dưới. Ảnh hưởng thép (neo thép trên lên cột trên?) chưa rõ — trước mắt chỉ vẽ | h19 gối | 04 L1120–1129; CHƯA | ❌ | đọc `UpperColumnWidth`, không dùng |
| R-129 | Ô gối hàng 20 = **dầm giao tại cột** `bxh` (hoặc `b`, h = B5); trống = không có. Đọc đủ `bxh`; **không sinh thép** tại cột (DWG B01 gối 1 `400x500`: không đai nút, không vai bò) | h20 gối | 04 L1173–1179; DWG B01 | ❌ | `GetDouble` → `400x500` mất im lặng |
| R-130 | Ô gối hàng 21 = lệch tim dầm giao so với tâm cột, \|lt\| ≤ cột/2 (cảnh báo khi vượt) — chỉ vẽ | h21 gối | 04 L1194–1200 | ❌ | đọc, không dùng |
| R-131 | Sàn: B7 và ô nhịp hàng 12 `[*]h1;down1/h2;down2` (trái/phải; `*` = không dim; âm = sàn bằng đáy dầm, down = h − h_sàn; `150/0` = sàn một bên); ô nhịp hàng 12 ghi đè B7 — chỉ vẽ mặt cắt | B7, h12 nhịp | 04 L558–572, L859–886 | ❌ | B7 chỉ đọc số; h12 không đọc |
| R-132 | Hàng 22 nhịp có tiền tố Ø (`d8a100/200`, `f8a100/200/50`) = đổi Ø đai ngoài **và đai trong** của nhịp; không đọc được thì cảnh báo, không lấy bước sai | h22 nhịp | 04 L1257–1258, L1474 | ❌ | `ParseStirrupSpacing` bỏ qua tiền tố, bước vùng dày rơi về G7, không báo |
| R-133 | Hàng 23 nhịp = bước **đai trong** (U/C/□) riêng nhịp `a1/a2[/a3]` (1 số = đều cả nhịp); trống = theo I8/J7 (R-68) — 04 nói "trống → theo hàng 22", DWG B01 (I8 = 2, a500) bác | h23 nhịp | 04 L1317–1338; DWG B01 (trống) | ❌ | chỉ báo chưa hỗ trợ |
| R-134 | Ô **nhịp** hàng 13–16 `-` = thép gia cường gối của hai gối chạy suốt nhịp (console, M âm toàn nhịp) | h13–16 nhịp | 04 L923–925, L967–969, L1002–1004 | ❌ | bỏ qua, không báo |
| R-135 | Ô **nhịp** hàng 17/18 `-`: 04 nói thép gia cường bụng chạy suốt nhịp. B01 `J17 -` (nửa phải nhịp gộp H+J, cùng `I17 2f20`): DWG vẽ 2Ø20 dừng 23300, 1300 trước mặt K → **không** chạy suốt; nghĩa thật chưa chốt | h17/18 nhịp | 04 L1071–1073; DWG B01 | ❌ | chỉ báo (nửa phải nhịp gộp) |
| R-136 | Ô gối hàng 11 **âm** (`-400`) = thép dầm neo vào dầm/vách biên bề rộng \|b\| | h11 gối | 04 L846 | ❌ | chặn "không đọc được bề rộng" |
| R-137 | I3/I5 có 4 lựa chọn (N3 "L từ mép cột", N4 "L từ tâm cột", N5 "L từ tâm cột, tính từ tâm", N6 "L từ mép dầm bẹt"; I3 chỉ N3:N5): cách đo L và gốc đo điểm cắt khác nhau | I3, I5 | 04 L280, L439; data validation Kata.xlsm | 🟡 | chỉ phân biệt "có chữ tâm" → gốc tâm cột; L luôn thông thủy |

## 14. Sai khác đã biết với bản vẽ Kata (chấp nhận hoặc chờ quyết định)

| # | Nội dung | Mục |
|---|---|---|
| 1 | Khe lớp: mô hình max(30, d), Kata 25 — **chờ chốt** | R-15 |
| 2 | Tâm thép chủ: HPRebar 42, Kata vẽ 38 (thanh đầu dầm 30) | R-13 |
| 3 | Đoạn thẳng của đai C giữ cốt giá: HPRebar ở dưới, Kata ở trên | R-75 |
| 4 | Chân neo bị giới hạn theo chiều cao dầm; Kata vẽ thò quá | R-34 |
| 5 | Chân dưới lùi vào ở gối biên (quy ước HPRebar) | R-35 |
| 6 | DY14 đầu trái ngắn hơn 75 mm tại gối bị cắt | R-85 |
| 7 | Hàng 16 ở ô gối: trên lớp 4 hay dưới lớp 3 | R-07 |
| 8 | B01 I17 qua nút: Kata 18950…23300, HPRebar 19200…23500 | R-54, R-135 |
| 9 | B01 mặt cắt (quy tắc vẽ): Kata 14 cờ tại x = 1250, 5300, 9950, 11650, 14150, 17025, 18550, 20250, 22650, 24150, 25425, 27800, 30725, 32267 — H và J (nhịp gộp) mỗi nửa riêng, console 1 cờ, giữa nhịp D/F/L tại giữa − 300. HPRebar (`KataSectionCuts`: 0.1·L từ mặt gối, giữa − 150, nhịp gộp 3 cờ, console 2 cờ) lệch 25 mm tới > 1 m. 04_quy_dinh/QUY_TRINH (0.125/0.5/0.875 Ln) cũng sai | — (đợt 2 plan B01) |
| 10 | B01 số hiệu: thép chủ trên 1, thép chủ dưới 4, đai 23…35 — HPRebar chưa có test B01 | R-110 |

## 15. Tạo thép trong Revit

- **Gom bộ thép dọc** (chủ B11/B12, gia cường 13–18, cốt giá):
  - Mỗi nhóm ≥ 2 thanh cùng ô, cùng vai trò, Ø, hình dạng và cách đều nhau thì thành **1 Rebar layout Fixed Number**; 1 thanh thì là Single.
  - Ô trộn Ø (`2f16+1f14`) tách theo Ø; ô `trái;phải` mỗi vế một bộ.
  - Thanh không cách đều: các đoạn cách đều ≥ 2 thanh gom thành bộ, phần còn lại là Single.
  - Code: `KataLongitudinalSetGrouping` (Core) + `KataRebarCreationService` (Revit).
- **Revit tự kéo thanh mới về lớp bảo vệ** (vd Ø16 ở y −29 bị kéo về −33):
  - Thanh Single được dời lại đúng chỗ.
  - Bộ Fixed Number được sửa khoảng cách constraint ở hai đầu dải rồi kiểm ±0.5 mm; sai thì lùi về Single và báo trong thông báo.
- **Gắn dấu và xoá lần chạy cũ (2026-10-05, quyết định user):**
  - Mỗi thanh mang **Extensible Storage** ẩn (`KataRebarStorage`: UniqueId dầm host, số Kata, tên dầm). Không ghi Comments, không ghi Schedule Mark (Revit tự sinh).
  - Chạy lại thì chỉ xoá thanh có storage trỏ đúng dầm đã chọn; thanh vẽ tay không bị xoá. Thanh của bản cũ (dấu `HPRebar_Kata:{host}` trong Comments) vẫn được nhận ra và xoá một lần.
  - Live Revit test 2026-10-05: lần 2 xoá đúng 60 phần tử của lần 1.
- **Số hiệu → Rebar Number:** Partition = tên dầm (B3). Sau khi tạo xong, mỗi số Revit (nhóm thanh Revit coi là giống nhau) đổi sang số Kata bằng `NumberingSchema.ChangeNumber`, qua số tạm để đổi chéo được (`KataRebarNumberAssigner`).
  - Revit không cho số đã có thanh khác hình giữ, hoặc coi hai thanh khác số Kata là một: giữ số Revit / số Kata nhỏ hơn và ghi trong thông báo.
  - Live B01: 36 nhóm, số 1…37 trừ 18 (F17 dài bằng I17 trong HP nên Revit gộp vào 12; Kata cắt I17 khác, R-135 chưa rõ). Vai bò 36, 37 (Kata dùng lại số 25 / 28 trên mặt đứng — chưa làm theo).

## 16. Thiết lập

Thiết lập lưu ở `%AppData%\HPRebar\KataSettings.json`, sửa trong **Kata Export ▸ Thiết lập**. Mọi giá trị đi qua
`KataSettingsJson.Sanitize`: âm, NaN, ∞ hay tỉ lệ vượt nửa nhịp đều quay về mặc định. File cũ (không có
`SettingsVersion`, đang giữ đúng mặc định cũ 15d / 2h / 0.15) được tự chuyển sang mặc định mới; giá trị user đã đổi thì giữ
nguyên.

**Hằng trong code (chưa phải thiết lập):**

| Hằng | Giá trị | Quy tắc |
|---|---|---|
| ZoneRound | 50 | R-62 |
| FirstStirrupOffset | 50 | R-63 |
| MinimumLegGap | 25 | R-35 |
| BarClearSpacing | 25 | R-16 |
| CrankSlope | 6 | R-81 |
