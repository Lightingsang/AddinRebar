# Ý tưởng: tool vẽ thép móng đơn trong Revit

Trạng thái: **Planned**, chưa có code. Chưa triển khai trước khi người dùng nói "BẮT ĐẦU".

Nhãn bằng chứng: **[XT]** đã xác thực (SPEC_Mong Phần A) · **[TT]** chỉ biết tên trường · **[GT]** giả thuyết (Phần B) · **[TK]** tự thiết kế.
Nguồn Tới Hoàng: bản prompt dán vào còn placeholder; dùng [ht-structural-notes.md](ht-structural-notes.md) (chỉ tính năng và quy trình công khai, **chưa có nguồn** về code, công thức, móng đơn).

**Phát hiện chính:**
- SPEC_Mong không có luật bố trí thép móng đơn: thân hàm vẽ móng đơn bị stub (VM-U10), nên luật hình học dưới đây phần lớn là [TK].
- `FoundationRebar` là tool lưới thép cho **sàn móng `Floor`**: không nhận `FamilyInstance` móng, không biết cột, không tạo view, mỗi thanh là một `Rebar` riêng.
- Phần view, dim, tag, sheet, chạy lại đã có ở Kata (`KataRebar` + `KataExport`), nhưng quy tắc F1 cấm feature khác dùng trực tiếp.

## 1. Phạm vi MVP
- **Có:** 1 móng đơn chữ nhật đặc (`OST_StructuralFoundation`, `FamilyInstance`), đáy và đỉnh phẳng, xoay bất kỳ trên mặt bằng; 1 cột trên móng, được phép lệch tâm so với móng.
- **Có:** lưới thép đáy 2 phương, thép mặt trên tùy chọn, 1 mặt bằng + 2 mặt cắt trên 1 sheet, chạy lại thay thế lần trước.
- **Để sau:** móng vát, móng bậc, cổ cột, giằng móng, nhiều cột trên 1 móng, móng cọc, rải hàng loạt, tính thép từ nội lực, bảng thống kê.
- **Từ chối có thông báo:** móng không phải hộp chữ nhật, không tìm thấy cột, model thiếu loại thép.

## 2. Luồng người dùng
1. Bấm nút, chọn 1 móng. Tool đọc móng và cột phía trên.
2. Cửa sổ hiện kích thước đo được, cột, cao độ, các ô nhập thép.
3. Xem trước mặt bằng và 2 mặt cắt, số thanh, chiều dài từng loại (học từ HT: mặt bố trí xem trước).
4. Bấm **Tạo thép**: xoá thép lần trước → tạo thép → đánh số → tạo view, dim, tag → xếp sheet. Tất cả là 1 Undo.
5. Trang báo số thanh, số view, tên sheet, cảnh báo. Lỗi ở bước view chỉ là cảnh báo, thép vẫn giữ.

Tách 3 bước nội bộ (thép → ghi chú view → sheet) để sau này chạy lại riêng phần view (học từ HT: tool ghi chú và tool chèn sheet tách riêng).

## 3. Dữ liệu đầu vào
| Dữ liệu | Nguồn | Ghi chú |
|---|---|---|
| Kích thước, cao độ, trục móng | Đọc solid Revit | Không tin tham số family; đọc được family lồng nhau (HT từng phải sửa lỗi này) |
| Tiết diện, vị trí cột | Đọc Revit | Tìm cột quanh mặt móng, tương tự `KataColumnStackReader` nhưng theo chiều ngược lại |
| Ø, bước, lớp bảo vệ, móc, thép trên | Người dùng nhập | Lưu dùng lại cho móng sau |
| Excel Kata | **Không dùng ở MVP** | SPEC_Mong không ghi sheet móng trong `Kata.xlsm`; chỉ có `SLAB DESIGN` |

Không đưa bộ trường của Kata (X1..X3, Y1..Y3, Z1..Z4, C1, C2, D1, D2, hCho1..3, moc_*) vào thiết kế: [TT], chưa biết nghĩa. HP đặt tên riêng theo nghĩa đo được.

## 4. Luật thép
| # | Luật | Giá trị đề xuất | Nhãn và căn cứ |
|---|---|---|---|
| 1 | Thép đáy 2 phương; thanh theo cạnh dài nằm dưới cùng | Lớp 1 cạnh dài, lớp 2 đè lên | [TK] thông lệ: phương cạnh dài chịu mô men lớn hơn. Cần kỹ sư xác nhận |
| 2 | Ø và bước gợi ý | Ø 10, 12, 14, 16, 18, 20; bước 100, 150, 200 | [XT] VM-046 (chỉ danh mục). Cách dò tổ hợp là [GT] VM-U11 |
| 3 | Giá trị mặc định | Ø16 a150 | [TK] theo `FoundationRebarSpec` |
| 4 | Vị trí và số thanh | N = floor(L / s); N + 1 thanh; phần dư chia đều 2 bên | [TK] theo `FoundationMeshCalculator.CalculateBarPositions` |
| 5 | Lớp bảo vệ | 3 giá trị trên / dưới / cạnh, mặc định 50 | [TK] theo repo. Kata có 3 giá trị này (VM-107) nhưng [TT], không có số. VM-029 = 25 là mức chung, không áp cho móng. **Phải đối chiếu TCVN 5574:2018 trước khi chốt** |
| 6 | Móc thép đáy | Đoạn bẻ 90° lên, 15d, cắt ngắn để không chạm lớp bảo vệ trên; cho tắt | [TK] theo `GetHookLength`, `RisingHook` |
| 7 | Thép mặt trên | Mặc định tắt, bật khi cần | [TK] repo mặc định bật vì tool đó cho sàn móng |
| 8 | Chiều dài thanh | Quá 11700 thì từ chối, không tự nối ở MVP | [XT] VM-028 cho giới hạn; cách xử lý là [TK] |
| 9 | Neo, nối | 30d, tra theo mác bê tông để sau | [XT] VM-028, VM-029 |
| 10 | Thép chờ cột | Không tạo ở MVP, chỉ kiểm tra và cảnh báo (câu hỏi 2) | Xem dưới |
| 11 | Cao độ mặt móng | Đọc từ Revit | -1.500 của Kata [XT] VM-025 chỉ là mặc định CAD, không dùng |
| 12 | Tính As từ nội lực | Để sau | Công thức [XT] VM-033..037 áp cho bản; dùng cho móng là [GT] |

**Thép chờ:** Kata Column đã tạo thép chờ trong đài, chân bẻ F5 × d, nên tạo thêm sẽ trùng thanh. Kata còn có cấu hình bẻ chân 250 (VM-024, [XT]), nối thép chờ (VM-023) và kiểu bố trí (VM-026, chưa biết hình học). Hai giá trị bẻ chân khác nhau cần người dùng chốt.

## 5. Trình bày sheet
| View | Loại | Tỷ lệ | Nội dung |
|---|---|---|---|
| Mặt bằng móng | Structural plan, cắt trên mặt móng | 1:25 | Dim tổng 2 phương, dim từ mép móng tới cột; tag 2 bộ thép |
| MC 1-1 | Section dọc cạnh dài, qua tâm cột | 1:25 | Dim dài, dày; tag thép; cột phía trên cắt ngắn |
| MC 2-2 | Section dọc cạnh ngắn, qua tâm cột | 1:25 | Như trên |

Tỷ lệ 1:25 là [TK] theo `KataSectionViews.Scale`. SPEC không có luật sheet móng đơn.

**Bố cục [TK]:** mặt bằng bên trái, 2 mặt cắt xếp bên phải, căn theo khung tên nhiều sheet dùng nhất (`KataSheetComposer.TitleBlock`).

**Chạy lại không nhân đôi:**
- `KataDraftingStorage.RunKey` theo `UniqueId` của móng; đóng dấu view, dim, ghi chú, sheet theo loại.
- `KataSectionViews.FindOrCreate` dùng lại section; plan dùng logic của `KataColumnSectionViews.FindOrCreate`.
- Đổi cắt và crop trước, xoá drafting cũ sau, để view bị template khoá vẫn giữ bản cũ.
- View bị người dùng xoay khác: bỏ dấu, giữ view của họ, tạo view mới. Viewport cũ được dời lại, viewport thừa bị xoá, như `KataSheetComposer.Compose`.
- Cắt theo khung móng, dùng `KataColumnFrame` cho móng xoay.

**Tag:** dùng `KataColumnViewAnnotator` (đã đo: tag thép phải tham chiếu subelement). Từ chối nào cũng được đếm và báo.

## 6. So với FoundationRebar và các phương án
| | A. Mở rộng FoundationRebar | B. Thêm vào họ Kata (khuyến nghị) | C. Chuyển view/sheet vào Shared rồi làm feature mới |
|---|---|---|---|
| Giao diện | WPF, R23–R27 | Trang web, Revit 2026 | Tùy chọn |
| Dùng lại thép | Calculator + test có sẵn | Viết planner nhỏ | Như A |
| Dùng lại view, sheet | Không được (F1) | Trực tiếp | Sau khi chuyển |
| Sửa code cũ | Đổi `Floor` sang `Element`, filter, session | Mở vài hàm private | Đổi namespace nhiều file |
| Rủi ro | Trộn 2 bài toán sàn móng và móng đơn | Chỉ Revit 2026, thêm một trang web | Là refactor, cần duyệt wave, không trộn với feature |

**Khuyến nghị B**, cùng mẫu với Kata Column (ADR-0014). Đánh đổi: chỉ có trên Revit 2026.

**Dùng lại trong B:** `KataDraftingStorage`, `KataSectionViews`, `KataDrawingDims`, `KataColumnViewAnnotator`, `KataColumnFrame`, `KataColumnViewsStep` (làm mẫu), `KataRebarStamp`, `KataRebarCleanupService`, `KataRebarNumberAssigner`, `KataHostCoverService`, `KataTransactionRunner`.

**Viết mới:** đọc móng + cột, planner lưới thép theo set, dim mặt bằng và mặt cắt, view mặt bằng, bố cục sheet, trang web.

**Sửa nhỏ:** `KataSheetComposer` đang gắn với dầm (hằng số T2-DY7, số sheet theo dầm); mở `TitleBlock` và `NewSheet` để composer móng dùng chung. Luật N + 1 thanh trùng tri thức với `FoundationMeshCalculator`: nếu hai bên dùng chung thì chuyển vào `HPRebar.Core/Shared/` trong commit riêng (ADR-0004).

**Khác FoundationRebar:** dùng rebar set (`SetLayoutAsNumberWithSpacing`, như `KataColumnRebarCreator.Lay`) thay vì từng thanh, để tag và số hiệu theo bộ.

## 7. Kiến trúc (phương án B)
```text
HPRebar.Core/KataRebar/Footing/          thuần, không Autodesk
  KataFootingSpec, KataFootingGeometry   dữ liệu vào, mm
  KataFootingMeshPlanner                 bộ thép theo lớp
  KataFootingDims, KataFootingDrawing    chuỗi dim, dữ liệu cho trang
HPRebar.Core.Tests/KataRebar/Footing/    xUnit, fixture cho trang
HPRebar/KataExport/
  KataFootingRebarCommand.cs             chọn móng, mở cửa sổ
  Service/KataFootingReader.cs           solid móng, cột, khung
  Service/KataFootingRebarCreator.cs     TransactionGroup, 1 Undo
  Service/KataFootingViewsStep.cs        view lỗi chỉ thành cảnh báo
  Service/KataFootingPlanView.cs, KataFootingSectionViews.cs, KataFootingSheetComposer.cs
  View/Web/Footing/                      cửa sổ + bridge
HPBimToolkit.Frontend/footing.html, src/kataFooting/
```
Tiền tố `Kata` ở đây là quy ước đặt tên của repo HP, không sao chép lớp nào của Kata Pro. Cần ADR-0015 mở rộng ngoại lệ web, gate Revit 2026 trong csproj và nút ribbon. `KataExport` + `KataRebar` là một feature (ADR-0006), nên không vi phạm F1.

## 8. Nghiệm thu và rủi ro
**Nghiệm thu:**
- [ ] Core test: số thanh đúng N + 1 ở 3 cỡ móng; móng hẹp hơn bước vẫn có 1 thanh; móc không chạm lớp bảo vệ trên.
- [ ] Core test: hai lớp đáy không trùng cao độ; móng xoay 30° cho kết quả trong khung đúng.
- [ ] Revit 2026, bản sao detached: móng thật tạo đủ thép, 0 cảnh báo thép ngoài host, số thanh trong Revit bằng số trang báo.
- [ ] Tạo đúng 3 view + 1 sheet; 0 dim và 0 tag bị từ chối.
- [ ] Chạy lại: số thanh, số view, số sheet không đổi; 1 Undo trả model về như cũ.
- [ ] Móng là family lồng nhau và móng xoay đều chạy được; móng vát bị từ chối có thông báo.
- [ ] Build `Debug.R26` và toàn bộ test pass.

**Rủi ro:**
| Rủi ro | Xử lý |
|---|---|
| Family móng trong dự án thật không phải hộp đơn giản | Khảo sát family thật trước khi code; từ chối rõ ràng |
| Revit từ chối `FamilyInstance` móng làm host, hoặc family không có lớp thép | GIẢ ĐỊNH CHƯA XÁC MINH: spike đầu tiên trên bản sao |
| Mặt bằng hiện cả bộ thép dày đặc, tag từ chối set | GIẢ ĐỊNH CHƯA XÁC MINH: thử `SetPresentationMode` (repo chưa dùng) |
| Va chạm chân thép chờ với lưới đáy | Kiểm tra cao độ chân thép chờ của Kata Column với lưới thép; cảnh báo |
| Luật móng [TK] sai thông lệ HP | Kỹ sư duyệt bảng mục 4 trước khi code |
| `KataHostCoverService` nhận cover cho đai | Xem lại khi áp cho móng 3 giá trị cover |

## 9. Câu hỏi cần người dùng quyết định
1. **Đặt tool ở đâu?** Khuyến nghị **B**: họ Kata, trang web, chỉ Revit 2026. Chọn A nếu bắt buộc chạy trên Revit cũ hơn.
2. **Thép chờ cột do ai tạo?** Khuyến nghị để **Kata Column** tạo, tool móng chỉ kiểm tra và cảnh báo, tránh trùng thanh. Cần chốt thêm chân bẻ theo 15d hay 250.
3. **Thép móng lấy từ đâu ở MVP?** Khuyến nghị **nhập tay**, kèm danh mục Ø và bước gợi ý. Tính từ nội lực (ETABS hoặc Excel) để lát sau.
