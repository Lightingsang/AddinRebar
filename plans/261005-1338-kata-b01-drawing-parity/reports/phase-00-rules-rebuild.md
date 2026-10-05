# Đợt 0 — lập lại quy định thép dầm (B01): danh sách việc HPRebar phải sửa

Ngày 2026-10-05. Chỉ tài liệu, không sửa code/test. Trạng thái code = cây làm việc hôm nay (có thay đổi chưa commit của đợt 1).

## Kết quả

| File | Nội dung |
|---|---|
| [docs/specs/kata-dam-sheet-cells.md](docs/specs/kata-dam-sheet-cells.md) | MỚI. Mọi ô nhập hàng 1–28 (+ 25–44): nghĩa, giá trị B01, bằng chứng DWG, HPRebar ✅/🟡/❌/➖ có `file:line`, R-xx; §8 mâu thuẫn nguồn (11 mục, DWG thắng) |
| [docs/specs/kata-beam-rebar-rules.md](docs/specs/kata-beam-rebar-rules.md) | Cập nhật: file quy tắc duy nhất cho Tạo thép Revit; link bảng ô; thứ tự nguồn thêm QUY_TRINH + quyết định user; DWG thêm B01; R-06, R-42 (I1), R-68 (cặp cột, thép chủ của nhịp, console 14-14, 13-13), R-86, R-87, R-92 (đai console −225…−1075), R-110 (B01 1/4/23…35), R-120, R-122; mục mới §13a R-128…R-137; §14 thêm #8–#10. Không đổi số, không xoá quy tắc đã xác minh |

Lệch nhỏ với plan: [plan.md](plans/261005-1338-kata-b01-drawing-parity/plan.md) đợt 0 ghi tên `kata-dam-sheet-cells-b01.md`; đã dùng tên theo đề bài `kata-dam-sheet-cells.md` (bảng không chỉ cho B01). plan.md chưa sửa.

## Đã khớp DWG B01 (không cần sửa)

K24 `*` + không đai nút ở mọi gối cột (R-120/R-122) · đai nút chỉ quanh dầm giao / cột cấy từ model Revit (R-120/R-121) ·
hàng 19/20/21 kế thừa, F21 400 → h 700, L 300, console 300×900 (R-03, R-71) · hộp đai console 250×850, z −225…−1075 (R-92,
test `Stirrups_follow_*`) · U/C đọc theo cặp cột, đếm trên thép chủ trên của nhịp (R-68, code) · I1 = TRUE không đổi bước lệch G1 (R-42).

## Danh sách việc (ưu tiên giảm dần)

### Mức 1 — làm lệch bản vẽ / thép B01

| # | Ô / R | Code | Hiện tại | Phải làm |
|---|---|---|---|---|
| 1 | Mặt cắt (§14 #9) — đợt 2 | [KataSectionCuts.cs:51](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionCuts.cs#L51) | 0.1·L từ mặt gối + giữa − 150; nhịp gộp H+J 3 cờ; console 2 cờ | 14 cờ tại x = 1250, 5300, 9950, 11650, 14150, 17025, 18550, 20250, 22650, 24150, 25425, 27800, 30725, 32267: cắt theo **nhịp của sheet** (H, J riêng dù gộp), console 1 cờ (≈ 1/3 console từ mặt gối: 32267 − 31600 = 667), giữa D/F/L = giữa − 300, cờ gần gối 425–850 từ mặt. Tìm một quy tắc chung với DY7/DY14 (không vỡ golden); không tìm được → hỏi user |
| 2 | h17 gối I `2f20` + J17 `-` (R-54, R-135) | [KataZeroWidthSupports.cs:98](HPRebar/HPRebar.Core/KataRebar/Parsers/KataZeroWidthSupports.cs#L98) | 2Ø20 19200…23500 (cắt R-51 của nhịp gộp) | Kata 18950…23300 = 850 từ mặt G / 1300 trước mặt K. Xác định quy tắc (vai trò của `J17 -`), sửa + test `I17_becomes_*` |
| 3 | Console đai trong `M25:N25 Đai C 2` (R-68) | [KataInnerStirrupLayout.cs:34](HPRebar/HPRebar.Core/KataRebar/Calculators/KataInnerStirrupLayout.cs#L34) | code đúng, **chưa có test B01** | Test mặt cắt 14-14: C Ø10 a500 quanh thanh giữa của 3Ø20, không U; móc C cốt giá Ø10 a500; 13-13 (L): không U/C, không cốt giá |
| 4 | Số hiệu (R-110) — đợt 5 | [KataBarNumbering.cs:43](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarNumbering.cs#L43) | thứ tự "mọi đoạn thép trên trước" đã có; B01 chưa test | Test B01: thép chủ trên 6Ø25 = 1, thép chủ dưới 6Ø25 = 4, đai 23…35; lệch thì sửa thứ tự / chữ ký thanh |
| 5 | J9 tâm thanh, khe lớp (R-13, R-15) | [KataDetailingRules.cs](HPRebar/HPRebar.Core/KataRebar/Models/KataDetailingRules.cs) `LayerClearGap` 30 | tâm 42 (Kata 38), khe max(30, d) (Kata 25) | Lộ ra ở 13-13 / 14-14 (lớp 2 trên 2Ø16). Khe 25/30 **chờ user chốt** (§14 #1) |

### Mức 2 — đọc sai ô sheet (không làm lệch B01 hiện tại)

| # | Ô / R | Code | Hiện tại | Phải làm |
|---|---|---|---|---|
| 6 | h20 gối `bxh` (R-129) | [KataDamSheetParser.cs:211](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L211) | `GetDouble` → `400x500` thành 0, không báo | Đọc b, h (như h11 `bxh`); lưu; không sinh thép tại cột |
| 7 | h22 nhịp tiền tố Ø (R-132) | [KataBarNotationParser.cs:146](HPRebar/HPRebar.Core/KataRebar/Parsers/KataBarNotationParser.cs#L146) | `d8a100/200` → vùng dày rơi về G7, Ø giữ G6, không báo | Tách Ø khỏi bước; Ø áp cho đai ngoài + đai trong của nhịp; không đọc được → cảnh báo |
| 8 | h23 nhịp (R-133) | [KataDamSheetParser.cs:290](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L290) | chỉ báo chưa hỗ trợ | Bước đai trong riêng nhịp `a1/a2[/a3]`; trống = I8/J7 |
| 9 | G6 trống (R-61) | [KataDamSheetParser.cs:60](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L60) | Ø10 | Phân biệt trống / 0; quy ước cần quyết định (Kata mặc định 10?) |
| 10 | h13–16 ô nhịp `-` (R-134) | [KataDamSheetParser.cs:254](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L254) | bỏ qua, **không báo** | Tối thiểu báo; sau đó thép gia cường gối chạy suốt nhịp |
| 11 | h11 gối âm (R-136) | [KataDamSheetParser.cs:192](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L192) | chặn "không đọc được bề rộng" | Neo vào dầm/vách biên bề rộng \|b\| — cần một bản vẽ Kata |
| 12 | I3/I5 4 lựa chọn (R-137) | [KataDamSheetParser.cs:47](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L47) | chỉ "có chữ tâm" | Phân biệt N3/N4/N5/N6 (L đo từ đâu, gốc cắt ở đâu) — cần bản vẽ Kata |
| 13 | h16 ô gối (R-07) | [KataDamSheetParser.cs:201](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L201) | trên lớp 4 | Chốt bằng bản vẽ Kata có hàng 16 |

### Mức 3 — chỉ vẽ / chưa có dữ liệu

| # | Ô / R | Hiện tại | Phải làm |
|---|---|---|---|
| 14 | h19 gối cột trên (R-128), h21 gối lệch dầm giao (R-130) | đọc, không dùng | Vẽ cột trên / dầm giao trên mặt đứng nếu DWG có |
| 15 | B7 / h12 nhịp sàn (R-131) | B7 chỉ số, h12 không đọc | Cú pháp `[*]h1;down1/h2;down2` cho mặt cắt |
| 16 | C24 số (R-122), H6 thép kê (R-126) | không vẽ | Khi có bản vẽ Kata dùng tới |
| 17 | B9 lệch trục, B1/B2 tỷ lệ | đọc / không đọc | Chỉ khi canvas cần |

## Câu hỏi cần user

- Khe giữa 2 lớp thép: 25 (Kata vẽ) hay 30 (HPRebar)? (R-15, đã treo từ trước.)

**Status:** DONE_WITH_CONCERNS
**Summary:** Bảng ô hàng 1–28 + file quy tắc cập nhật + danh sách 17 việc ưu tiên; B01 lệch chính ở mặt cắt, thép qua nút I, test console/số hiệu.
**Concerns:** tên file bảng ô khác plan.md; quy tắc vị trí mặt cắt và điểm cắt I17 của Kata chưa suy ra được từ số đo.
