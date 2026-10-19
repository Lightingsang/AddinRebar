# HƯỚNG DẪN SỬ DỤNG TOÀN DIỆN TÍNH NĂNG VẼ & TRIỂN KHAI CỐT THÉP DẦM TRONG HPKATA (`C:\HPKata`)

Tài liệu này hướng dẫn chi tiết từng bước sử dụng hệ thống **Vẽ & Triển khai Cốt thép Dầm (Continuous Beam Detailing & 3D Rebar)** trong bộ chương trình độc lập **HPKata** (`C:\HPKata\`), bao gồm cả 3 môi trường:
1. **AutoCAD 2026** (`ve_dam` / `vd`, `ve_dam_shop` / `vds`, `loadbeam`, `Etabs`, `ve_dam_all`)
2. **Microsoft Excel (`C:\HPKata\Kata.xlsm` — Sheet `Dam`, `save_info`, `BEAM DESIGN`, `To_hop_thep`)**
3. **Autodesk Revit 2026** (Nút lệnh **`Thép Dầm`** trên thanh công cụ Ribbon **`HPKata`**)

---

## PHẦN 1 — KIỂM TRA CÀI ĐẶT & KHỞI ĐỘNG NHANH TỪ `C:\HPKata\`

Mọi tệp chương trình, thư viện блок CAD (`Iso_kata.dwg`), tập lệnh tắt (`Goi lenh.lsp`), bảng tính Excel (`Kata.xlsm` đã mở khóa toàn bộ 21 module VBA) và bộ biểu tượng đều nằm độc lập tại **`C:\HPKata\`**:

- **Tự động cài đặt / cập nhật**:
  ```powershell
  C:\HPKata\HPKata_Setup.exe --install --target-dir "C:\HPKata"
  ```
- **Trên AutoCAD 2026**:
  - Bộ cài đã tự động đăng ký gói `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml` để tự nạp `HPKata.AutoCad.dll` và `Goi lenh.lsp` khi mở AutoCAD.
  - Nếu muốn nạp thủ công ngay trong phiên AutoCAD đang mở:
    1. Gõ lệnh `NETLOAD` $\rightarrow$ chọn `C:\HPKata\HPKata.AutoCad.dll`.
    2. Gõ lệnh `APPLOAD` $\rightarrow$ chọn `C:\HPKata\Goi lenh.lsp` (để có đầy đủ các lệnh tắt như `vd`, `vds`, `tkt`, `sua`...).
    3. Gõ lệnh `KATA_RIBBON` để hiển thị thanh công cụ Ribbon **HPKata**.
- **Trên Revit 2026**:
  - Bộ cài đã tự động đăng ký manifest `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin`.
  - Khi mở Revit 2026, trên thanh công cụ sẽ xuất hiện tab **`HPKata`** chứa nút **`Thép Dầm`** (cùng `Thép Cột`, `Thép Vách`, `Thép Móng`, `Thép Sàn`, `Thép Thang`, `Nhập ETABS`, `Thống Kê`, `Tổ Hợp Cắt`).

---

## PHẦN 2 — VẼ DẦM 2D SHOP DRAWING TRÊN AUTOCAD 2026 (`ve_dam` / `vd`, `ve_dam_shop` / `vds`, `loadbeam`)

### Cách 1: Vẽ dầm trực tiếp qua Hộp thoại Giao diện Trực quan (`ve_dam` hoặc lệnh tắt `vd`)

1. **Bước 1 — Gọi lệnh**:
   - Tại dòng lệnh AutoCAD, gõ **`ve_dam`** (hoặc lệnh tắt **`vd`**) rồi nhấn `Enter` (hoặc bấm nút **Vẽ Dầm** trên thanh Ribbon `HPKata`).
2. **Bước 2 — Khai báo thông số trên cửa sổ `HPKata — Triển khai Chi tiết Cốt thép Dầm liên tục`**:
   - **Thông tin chung (Góc trên bên phải)**:
     - **Dầm**: Nhập tên cấu kiện (ví dụ: `D1`, `B201`).
     - **Tầng**: Nhập tên tầng (ví dụ: `Tầng 2`).
     - **Chế độ Shop 11.7m**: Tích chọn nếu muốn chương trình tự động phân đoạn nối chồng thanh thép thương phẩm $11.7\text{ m}$ (so le mối nối vùng nén).
   - **Khung Preview 2D Trực quan (Ngay dưới thanh công cụ Excel)**:
     - Hiển thị trực tiếp theo thời gian thực **Mặt cắt dọc dầm nhiều nhịp** (gối cột, thép chủ trên/dưới, vùng đai dày/thưa) bên trái và **Mặt cắt ngang `1-1 (b×h)`** bên phải. Mọi thay đổi số liệu đều cập nhật ngay trên hình preview.
   - **Bảng Danh sách Nhịp dầm liên tục (Bên trái)**:
     - Nhấn **`+ Thêm nhịp`** hoặc **`- Bớt nhịp`** để tăng/giảm số nhịp của dầm liên tục.
     - Chỉnh sửa trực tiếp trên bảng:
       - `Nhịp`: Tên nhịp (`Nhịp 1`, `Nhịp 2`...).
       - `Ln (mm)`: Chiều dài thông thủy từng nhịp (ví dụ: `5400`, `6000`).
       - `b (mm)`: Bề rộng tiết diện dầm (ví dụ: `300`).
       - `h (mm)`: Chiều cao tiết diện dầm (ví dụ: `600`; khi $h \ge 700\text{ mm}$, mục **Thép giá bên hông** sẽ tự động bật theo quy chuẩn cấu tạo TCVN 5574).
       - `Gối T (mm)` / `Gối P (mm)`: Bề rộng cột/vách đỡ bên trái và bên phải nhịp (ví dụ: `400`).
   - **Thông số Cốt thép (Bên phải)**:
     - **Thép chủ dọc liên tục**: Khai báo số lượng thanh (`SL`) và đường kính (`Ø`) cho **Thép lớp trên** (ví dụ: `2` thanh `Ø20`) và **Thép lớp dưới** (ví dụ: `2` thanh `Ø20`).
     - **Cốt đai & Đai gia cường**: Khai báo đường kính đai (`Ø8`), số nhánh đai (`2` hoặc `4`), bước đai vùng gối **`a_dày`** (`100` mm) và bước đai giữa nhịp **`a_thưa`** (`200` mm).
     - **Thép giá bên hông**: Tích chọn và nhập số thanh + đường kính (ví dụ: `2` thanh `Ø12`).
3. **Bước 3 — Áp dụng & Chọn điểm đặt bản vẽ trên AutoCAD**:
   - Nhấn nút **`Áp dụng & Tạo bản vẽ`** ở góc dưới bên phải hộp thoại.
   - Hộp thoại đóng lại, dòng lệnh AutoCAD nhắc:
     ```text
     [HPKata] Chọn điểm đặt bản vẽ chi tiết dầm 'D1':
     ```
   - Click chuột chọn 1 điểm trên màn hình ModelSpace. **HPKata** lập tức vẽ trọn bộ bản vẽ chi tiết dầm gồm:
     - **Mặt cắt dọc dầm liên tục**: Đường bao bê tông (`kata_net thay`), cổ cột đỡ (`kata_net manh`), thép chủ trên `#1` & thép chủ dưới `#2` có móc neo 90° (`kata_thep chu`), thép giá `#3` (nếu có), cốt đai rải 3 vùng dày-thưa-dày (`kata_thep dai`), đường kích thước nhịp $L_n$ (`kata_dim`) và nhãn ghi chú thép (`kata_text`).
     - **Mặt cắt ngang `MẶT CẮT 1-1 (GỐI)` & `MẶT CẮT 2-2 (NHỊP)`** đặt bên phải mặt cắt dọc: Khung tiết diện $b \times h$, vòng đai kín, chấm tròn vị trí thanh thép chủ và ghi chú đường kính/bước đai.
     - **Siêu dữ liệu XData `KATA`**: Mọi thanh thép dọc và cốt đai được gắn tự động bản ghi XData `KATA` (`Dang`, `STT`, `Phi`, `SoLuong`, `TenCauKien`, `ChieuDaiCacDoanMm`) để sẵn sàng thống kê ngay bằng lệnh `tkt`.

---

### Cách 2: Vẽ Dầm Chế độ Shop Drawing Cắt Nối Thép 11.7m (`ve_dam_shop` hoặc `vds`)

- Gõ lệnh **`ve_dam_shop`** (hoặc **`vds`**) trong AutoCAD.
- Hộp thoại `BeamDetailingWindow` mở sẵn ở chế độ **`Chế độ Shop 11.7m = Bật`**.
- Khi tổng chiều dài dầm vượt quá chiều dài thanh thép thương phẩm $11,700\text{ mm}$, ngoài mặt cắt dọc và mặt cắt ngang `1-1` / `2-2`, **HPKata** tự động tính toán và vẽ thêm sơ đồ **tách thanh thương phẩm $11.7\text{ m}$** ngay bên dưới dầm (tự động bố trí vị trí nối chồng $L_{\text{lap}}$ ở vùng nén: gối cho thép dưới, giữa nhịp cho thép trên).

---

## PHẦN 3 — ĐỒNG BỘ 2 CHIỀU VỚI BẢNG TÍNH EXCEL `C:\HPKata\Kata.xlsm` (SHEET `Dam`)

Một trong những tính năng mạnh nhất của **HPKata** là khả năng đồng bộ 2 chiều với bảng tính **`C:\HPKata\Kata.xlsm`** (đã được mở khóa toàn bộ 21 module VBA):

### 1. Các nút điều khiển Excel ngay trên Hộp thoại Vẽ Dầm (`ve_dam` / `loadbeam`)
- Gõ lệnh **`loadbeam`** trong AutoCAD: Hộp thoại `BeamDetailingWindow` sẽ mở lên và **tự động nạp ngay dữ liệu hiện có từ sheet `Dam` của `C:\HPKata\Kata.xlsm`**.
- Trên thanh công cụ phía trên hộp thoại có 3 nút + 1 tùy chọn:
  1. **`Mở Kata.xlsm`**: Mở trực tiếp tệp `C:\HPKata\Kata.xlsm` trên ứng dụng Microsoft Excel để bạn xem hoặc chỉnh sửa bảng tính.
  2. **`Đọc từ Excel`**: Đọc tức thì toàn bộ dữ liệu từ sheet `Dam` (cả khi tệp đang mở trên Excel qua COM lẫn khi tệp đang đóng qua ClosedXML) vào bảng nhịp và thông số cốt thép trên hộp thoại.
  3. **`Ghi vào Excel`**: Lưu ngược toàn bộ thông số nhịp và cốt thép bạn vừa chỉnh trên hộp thoại vào đúng các ô chuẩn của sheet `Dam` trong `C:\HPKata\Kata.xlsm`.
  4. **`Tự động đồng bộ khi Áp dụng`**: Khi tích chọn ô này, mỗi lần bạn bấm **`Áp dụng & Tạo bản vẽ`**, chương trình sẽ tự động lưu cấu hình dầm vào `Kata.xlsm` trước khi vẽ ra CAD/Revit.

### 2. Sơ đồ Tọa độ Ô Chuẩn trên Sheet `Dam` (`C:\HPKata\Kata.xlsm`)
Khi làm việc trực tiếp trên sheet **`Dam`** của `C:\HPKata\Kata.xlsm`:
- **Khối Thông tin Chung & Thép Chạy Suốt (`A3:B12`)**:
  - Ô `B3`: Tên dầm (ví dụ: `D1`).
  - Ô `B4`: Bề rộng dầm mặc định $b$ (mm).
  - Ô `B5`: Chiều cao dầm mặc định $h$ (mm).
  - Ô `B11`: Thép chủ chạy suốt lớp trên (ví dụ: `2f20` hoặc `2Ø20`).
  - Ô `B12`: Thép chủ chạy suốt lớp dưới (ví dụ: `2f20` hoặc `2Ø20`).
- **Khối Thông số Từng Nhịp & Gối Đỡ (Từ Cột `C` trở đi, mỗi nhịp 1 cột)**:
  - **Dòng 10**: Tên trục / bề rộng gối đỡ cột (`Gối T`, `Gối P`).
  - **Dòng 11**: Chiều dài nhịp thông thủy $L_n$ (mm) — Trong Excel, bạn có thể đặt con trỏ tại dòng 11 và chạy macro `laychieudai` (`Ve_dam.bas`) để pick trực tiếp 2 điểm trên bản vẽ AutoCAD lấy chiều dài nhịp làm tròn $5\text{ mm}$!
  - **Dòng 14–15**: Thép gia cường gối trái, giữa nhịp, gối phải (ví dụ: `2f18`).
  - **Dòng 17**: Quy cách cốt đai theo nhịp (ví dụ: `Ø8a100` vùng gối, `Ø8a200` giữa nhịp).
- **Lưu & Nạp nhiều dầm trong Workbook (`save_info.bas`)**:
  - Nhập tên dầm vào ô `B3`, chạy macro `save()` để lưu dầm vào cơ sở dữ liệu nội bộ sheet `save_info` (mỗi dầm 33 dòng), hoặc `load_data()` để gọi lại bất kỳ dầm nào đã lưu.

---

## PHẦN 4 — NHẬP DỮ LIỆU DẦM TỪ MÔ HÌNH CSI ETABS (`Etabs`)

1. Tại dòng lệnh AutoCAD, gõ lệnh **`Etabs`** (hoặc bấm nút **`Nhập ETABS`** trên Ribbon Revit).
2. Cửa sổ **`HPKata — Nhập Dữ liệu Dầm từ ETABS`** mở ra:
   - Dán bảng diện tích cốt thép / tiết diện dầm xuất từ ETABS vào ô văn bản và nhấn **`Phân tích dữ liệu`** để chương trình tự động quy đổi diện tích thép yêu cầu $A_s\text{ (cm}^2\text{)}$ sang số thanh và đường kính cốt thép dọc + cốt đai.
3. Nhấn **`Áp dụng & Tạo bản vẽ`** $\rightarrow$ chọn điểm gốc trên AutoCAD để chương trình tự động vẽ hàng loạt các dầm vừa nhập từ ETABS (mỗi dầm cách nhau $2,200\text{ mm}$ theo phương đứng).

---

## PHẦN 5 — TRIỂN KHAI CỐT THÉP DẦM 3D TRÊN REVIT 2026 (`Thép Dầm`)

Trên **Autodesk Revit 2026**, tính năng **`Thép Dầm`** hỗ trợ cả **2 cách thao tác linh hoạt**:

### Cách A: Chọn dầm trước trên mô hình 3D / Mặt bằng $\rightarrow$ Bấm `Thép Dầm`
1. Trên cửa sổ Revit 2026, click chọn trước 1 cấu kiện dầm (**`Structural Framing`**).
2. Vào tab **`HPKata`** trên Ribbon $\rightarrow$ bấm nút **`Thép Dầm`**.
3. Hộp thoại **`BeamDetailingWindow`** mở lên và **tự động điền sẵn**:
   - **Mã dầm (`BeamMark`)** theo tên Family/Mark của dầm đang chọn.
   - **Kích thước thực tế của dầm**: Bề rộng $b\text{ (mm)}$, chiều cao $h\text{ (mm)}$ và chiều dài nhịp $L_n\text{ (mm)}$ đọc trực tiếp từ hình học cấu kiện Revit!
   - Nếu trong `C:\HPKata\Kata.xlsm` (sheet `Dam`) đang có dầm trùng tên, chương trình tự động đồng bộ luôn cấu hình thép từ Excel.
4. Kiểm tra/điều chỉnh số thanh thép trên, thép dưới, cốt đai trên hộp thoại (xem trực tiếp trên khung Preview 2D) rồi bấm **`Áp dụng & Tạo bản vẽ`**.
5. **HPKata** tự động:
   - Xóa sạch cốt thép HPKata cũ trong dầm đó (cơ chế **Idempotency** chống trùng lặp khi chạy lại),
   - Khởi tạo các thanh **cốt thép dọc 3D (`Rebar`)** lớp trên, lớp dưới và **cốt đai 3D (`RebarStyle.StirrupTie`)** phân bổ đúng 3 vùng (2 đầu gối bước dày $a_{\text{dày}}$, giữa nhịp bước thưa $a_{\text{thưa}}$) bên trong cấu kiện dầm Revit!

### Cách B: Bấm `Thép Dầm` trước $\rightarrow$ Cấu hình trên Hộp thoại $\rightarrow$ Chọn dầm trên Revit sau
1. Không cần chọn dầm trước, bấm trực tiếp nút **`Thép Dầm`** trên tab **`HPKata`**.
2. Hộp thoại **`BeamDetailingWindow`** mở ngay lập tức. Bạn có thể bấm **`Đọc từ Excel`** hoặc tự nhập thông số thép, sau đó bấm **`Áp dụng & Tạo bản vẽ`**.
3. Thanh trạng thái Revit nhắc: *"Chọn dầm (Structural Framing) để bố trí cốt thép 3D HPKata:"* $\rightarrow$ Click vào dầm trên mô hình Revit để sinh cốt thép 3D ngay lập tức!

---

## PHẦN 6 — THỐNG KÊ THÉP, TỐI ƯU CẮT 11.7M & XUẤT EXCEL SAU KHI VẼ DẦM

Sau khi đã vẽ dầm trên AutoCAD (hoặc tạo cốt thép 3D trên Revit), toàn bộ các thanh thép chủ và cốt đai đều mang siêu dữ liệu chuẩn `KATA`:

| Bước tiếp theo | Lệnh trong AutoCAD | Nút trên Ribbon Revit | Chức năng thực hiện |
|---|---|---|---|
| **1. Sửa thanh thép bất kỳ** | `sua_thep` (hoặc `sua`) | `Vẽ & Sửa Thép` | Chọn thanh thép dầm trên bản vẽ để sửa đường kính, số lượng, chiều dài đoạn uốn. |
| **2. Đánh lại số hiệu thanh (STT)** | `stt0` hoặc `stt1` | *(Tự động)* | Đánh lại số thứ tự `STT` liên tục hoặc theo cấu kiện cho toàn bộ thanh thép trên bản vẽ. |
| **3. Xuất Bảng Thống Kê Thép** | `thong_ke_thep` (hoặc `tkt`) | `Thống Kê` | Quét toàn bộ các dầm đã vẽ, tổng hợp chiều dài & trọng lượng (kg) theo từng đường kính và kẻ bảng thống kê ngay trên bản vẽ. |
| **4. Tối ưu Cắt Thép 1D (11.7m)** | `to_hop_thep` (hoặc `tohopthep`) | `Tổ Hợp Cắt` | Chạy thuật toán tối ưu cắt thanh thương phẩm $11.7\text{ m}$ giảm phế liệu (decop) và xuất bảng sơ đồ cắt thép. |
| **5. Xuất sang Excel / QH Plus** | `toexcel` / `toexcelqh` | *(Trong hộp thoại)* | Xuất toàn bộ bảng thống kê cốt thép dầm sang sheet `Thong_ke` của `C:\HPKata\Kata.xlsm` hoặc định dạng gia công `Iso_QHPlus.xls`. |
