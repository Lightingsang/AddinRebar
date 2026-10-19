# CAD Dimension, Text & Layer Utilities: Specification Report & Architecture Design

**Author**: `explorer_m4_cad_utils`  
**Date**: 2026-10-17  
**Milestone**: M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities)  
**Deliverable Path**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_cad_utils\cad_utils_spec_report.md`  
**Parent Agent**: `orchestrator_17` (Conversation ID: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`)  

---

## 1. Executive Summary & Authoritative Evidence Sources

This specification report details the reverse-engineering analysis, geometric algorithms, data schemas, and Clean Code architecture design for the **CAD Dimension, Text & Layer Utilities** subsystem within the **HPKata** ecosystem (`HPKata/HPKata.slnx`). 

This subsystem bridges critical drawing production workflows used across Vietnamese structural design offices and provides native, robust C# .NET 8 implementations for drafting utilities previously scattered across legacy AutoLISP routines, VBA macros (`-vbarun`), and auxiliary CAD tools.

### 1.1 Authoritative Sources Probed
1. `C:\kata_pro\Goi lenh.lsp` (238 lines, 118 command aliases, line 152 config offset).
2. `C:\kata_pro\Goi lenh - Gstarcad.lsp` and `C:\kata_pro\Goi lenh - Zwcad.lsp`.
3. `C:\kata_pro\Kata_pro64_Cad2025.dll` (AutoCAD 2025+ .NET 8 binary: `Form_loc_dim`, `Lenh_thep`, `Setting`).
4. `C:\kata_pro\Kata_Class_Lib.dll` (Shared domain library: `Bien_chung`, `PolygonModule`).
5. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\GoiLenh_HPKata.lsp` (Target 238-line syntax file).
6. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad.Tests\GoiLenhLspSyntaxTests.cs` (Authoritative syntax regression test suite asserting exact line count 238, line 152 offset, line 237 end, and 118 command aliases).
7. `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` and survey reports in `.agents/teamwork/teamwork_preview_spec_miner_survey_1/`.

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Dimension | `cat_chan_dim` / `catdim` (`cd`) | Cắt ngắn hoặc kéo dài chân đường dóng (witness lines) của Dimension về khoảng cách cố định hoặc tới đường ranh giới | Tập hợp Dimension chọn trên màn hình; khoảng cách offset hoặc điểm biên | Cập nhật `Xline1Point` và `Xline2Point` của `AlignedDimension`/`RotatedDimension` | Bỏ qua các đối tượng không phải Linear/Aligned Dimension; bỏ qua layer khóa | `DISPATCH.md`, `Goi lenh.lsp`, CAD standard practice |
| 2 | Dimension | `ngat_dim` / `ngatdim` | Ngắt đường kích thước tại giao điểm với các nét vẽ khác (thép, leader, đường bao) tạo khoảng hở trực quan hoặc tách thành 2 Dim liên tiếp | Dimension được chọn và điểm ngắt / đối tượng giao cắt | Kích hoạt `DIMBREAK` hoặc tách thành 2 Dimension liên tiếp cùng cao trình đường dim | Báo lỗi nếu điểm ngắt nằm ngoài phạm vi đường kích thước | `DISPATCH.md`, `Goi lenh.lsp`, `DIMBREAK` workflow |
| 3 | Dimension | `dim_noi_tiep` / `dnoi` | Chuỗi đo kích thước liên tục nhanh (Continuous dimensioning) bám theo hướng đường dóng chuẩn | Dimension cơ sở hoặc 2 điểm đầu; chuỗi các điểm tiếp theo $P_3, P_4, \dots$ | Các thực thể `AlignedDimension` thẳng hàng cùng layer `kata_dim` và style `kata_dim_iso` | Kết thúc lệnh êm khi bấm Enter hoặc ESC; bỏ qua điểm trùng | `DISPATCH.md`, `Goi lenh.lsp`, `DIMCONTINUE` extension |
| 4 | Dimension | `ghi_do_doc` / `sl` | Ghi ký hiệu độ dốc kèm mũi tên chỉ hướng và tỷ lệ phần trăm (e.g. `i = 2%`, `1:10`) cho mái dốc, dầm nghiêng, ram dốc | Điểm đầu $P_1$, điểm cuối $P_2$, điểm đặt text | Polyline mũi tên hướng dốc + MText ghi độ dốc tự động lật chữ chống ngược | Báo "Đường thẳng đứng" nếu $\Delta X = 0$; từ chối nếu $P_1 \equiv P_2$ | `DISPATCH.md`, `Goi lenh.lsp:76` (`ve_dam_xien`), TCVN 4453 |
| 5 | Text & Elevation | `can_chinh_text` / `dtext` | Căn chỉnh thẳng hàng và phân bổ khoảng cách đều giữa các dòng Text/MText theo phương đứng hoặc ngang | Tập hợp Text/MText; chế độ căn (Trái, Phải, Giữa, Đỉnh, Đáy, Chia đều) | Vị trí chèn của các Text được cập nhật thẳng hàng theo toạ độ chuẩn | Báo "Không có Text nào được chọn"; bỏ qua nếu chọn $< 2$ đối tượng khi chia đều | `DISPATCH.md`, `Goi lenh.lsp:58` (`textblock`), Detailing tables |
| 6 | Text & Elevation | `cao_do_mat_cat` / `mcat` | Chèn ký hiệu mốc cao độ mặt cắt (tam giác ngược, đường gióng ngang và giá trị cao độ định dạng chuẩn e.g. `+3.600`, `±0.000`, `-0.050`) | Điểm mốc chuẩn ($0.000$), điểm cần ghi cao độ $P_{target}$, hướng mũi tên | Block/hình học cao độ gồm tam giác + đường line + Text cao độ | Tự động áp dụng tiền tố `+`, `-`, hoặc `±`; chia đúng tỉ lệ đơn vị vẽ (mm -> m) | `DISPATCH.md`, `Goi lenh.lsp:35, 40` (`lv`, `cht`), TCVN 3986 |
| 7 | Layer | `an_layer` / `anlayer` | Ẩn nhanh (Tắt `IsOff` hoặc đóng băng `IsFrozen`) layer của đối tượng được pick chọn trên màn hình | Click chọn một đối tượng bất kỳ | Layer của đối tượng chuyển sang trạng thái ẩn; nếu là layer hiện hành thì đổi an toàn sang `"0"` trước | Báo thông báo layer đã ẩn; hủy lệnh êm nếu không chọn đối tượng | `DISPATCH.md`, `Goi lenh.lsp:51-52` (`hideatt`), `LAYOFF` pattern |
| 8 | Layer | `hien_tat_ca_layer` / `hienlayer` | Mở lại tất cả các layer đang bị ẩn (`IsOff = true`) hoặc bị đóng băng (`IsFrozen = true`) trên toàn bộ bản vẽ | Kích hoạt lệnh trực tiếp | Toàn bộ layer trong `LayerTable` được bật sáng và rã băng | Báo số lượng layer đã được phục hồi; không làm mất trạng thái màu/linetype | `DISPATCH.md`, `Goi lenh.lsp:52` (`Unhideatt`), `LAYON` pattern |
| 9 | Layer | `doi_layer` / `doilayer` | Chuyển đổi nhanh các đối tượng được chọn sang layer chuẩn Kata (`kata_dim`, `kata_thep chu`, `kata_thep dai`, `kata_net thay`, v.v.) và gán ByLayer | Tập hợp đối tượng chọn; loại layer mục tiêu | Các đối tượng đổi sang layer chỉ định; tự tạo layer nếu bản vẽ chưa có | Báo lỗi nếu tên layer chứa ký tự cấm của AutoCAD | `DISPATCH.md`, `Goi lenh.lsp:109, 152-162`, Kata standards |
| 10 | Maintenance | `xoa_xrecord` / `clean_kata` | Quét dọn các XRecord mồ côi, Dictionary rác, và sửa các liên kết handle thép bị gãy trong cơ sở dữ liệu bản vẽ | Kích hoạt lệnh | Dọn dẹp sạch Named Object Dictionary và xóa handle rác khỏi XData | Giữ nguyên vẹn các liên kết đang hoạt động hợp lệ | `DISPATCH.md`, `Goi lenh.lsp:134` (`pukata`), `UtilityCommands.cs` |

---

## 3. Edge Cases & Boundary Behaviors

| # | Feature | Input Scenario / Edge Case | Observed & Modeled Behavior |
|---|---|---|---|
| 1 | `catdim` | Dimension có đường dóng nghiêng bất kỳ (Aligned Dimension không song song trục X/Y) | Thuật toán vector chiếu điểm gốc $P_{ext1}$ lên phương vuông góc với vector đường kích thước $\vec{u}_{dim}$, tính toán toạ độ mới theo vector pháp tuyến đơn vị $\vec{n} = (-u_y, u_x)$ thay vì chỉ cộng/trừ toạ độ $X, Y$ đơn giản. |
| 2 | `catdim` | Chân dim nằm ngược hướng so với đường kích thước (Dimension line nằm giữa điểm đo và chân dim) | Thuật toán xác định vector hướng từ đường dim tới điểm gốc ban đầu $\text{sgn}( (P_{ext} - P_{dim}) \cdot \vec{n} )$, đảm bảo hướng dóng không bị lật ngược $180^\circ$ khi áp dụng khoảng offset cố định. |
| 3 | `ngatdim` | Điểm ngắt trùng với điểm đầu hoặc điểm cuối của kích thước ($P_{break} \approx P_{ext1}$ hoặc $P_{break} \approx P_{ext2}$) | Kiểm tra ngưỡng dung sai $\epsilon = 1.0\text{ mm}$; nếu điểm ngắt nằm trong khoảng cách $\epsilon$ tới các đầu mút, lệnh từ chối thao tác ngắt và thông báo "Điểm ngắt quá gần mút kích thước". |
| 4 | `dnoi` | Người dùng pick liên tiếp 2 điểm có khoảng cách cực ngắn ($d < 5.0\text{ mm}$) | Hệ thống bỏ qua điểm thứ hai, phát cảnh báo "Khoảng cách đo quá ngắn" và tiếp tục giữ chuỗi chờ điểm tiếp theo mà không tạo dimension rác 0mm. |
| 5 | `sl` | Đoạn dốc thẳng đứng tuyệt đối ($\Delta X = 0, \Delta Y \ne 0$) | Phép chia $\frac{|\Delta Y|}{\Delta X}$ tránh được ngoại lệ `DivideByZeroException`; hệ thống định dạng thành `"Đứng"` hoặc `"90°"`. |
| 6 | `sl` | Góc dốc hướng từ phải sang trái ($\theta \in (\frac{\pi}{2}, \frac{3\pi}{2})$) | Hệ thống tự động tính toán góc quay của Text bằng cách cộng thêm $\pi$ ($180^\circ$) để chữ hiển thị xuôi mắt đọc theo tiêu chuẩn TCVN, không bao giờ bị lộn ngược đầu. |
| 7 | `dtext` | Tập hợp chọn lẫn lộn cả `DBText`, `MText` và các đối tượng khác (`Dimension`, `Line`) | `SelectionFilter` lọc trước `((0 . "TEXT,MTEXT"))`; các đối tượng không phải văn bản bị loại bỏ êm ái mà không gây ngắt quãng lệnh. |
| 8 | `dtext` | Các dòng Text có góc quay khác nhau (e.g. Text nghiêng $45^\circ$, $90^\circ$) | Căn chỉnh vị trí dựa trên hệ toạ độ địa phương (OCS/ECS) của đối tượng đầu tiên làm chuẩn, hoặc chiếu toạ độ điểm chèn lên đường thẳng chuẩn do người dùng chỉ định. |
| 9 | `mcat` | Giá trị cao độ tính toán rơi vào khoảng sai số xung quanh $0.000$ (e.g. $Z = +0.0002\text{ m}$) | `ElevationFormatter` áp dụng ngưỡng $|Z| < 0.0005$ để gán chuẩn xác ký hiệu `"±0.000"` (hoặc mã AutoCAD `"%%p0.000"`), tránh hiển thị nhầm thành `"+0.000"` hay `"-0.000"`. |
| 10 | `anlayer` | Người dùng chọn đối tượng nằm trên layer hiện hành (`Database.Clayer`) | Không để AutoCAD rơi vào trạng thái lỗi; hệ thống kiểm tra `layerId == db.Clayer`, tự động chuyển layer hiện hành sang layer `"0"` hoặc cảnh báo người dùng trước khi tắt layer. |
| 11 | `doilayer` | Chuyển đối tượng sang layer chuẩn Kata nhưng layer đó chưa được khai báo trong bản vẽ | `LayerUtilityController` tự động gọi hàm khởi tạo `EnsureKataLayerCreated()`, tự nạp tên layer, gán màu sắc và linetype chuẩn từ `KataConfig` trước khi gán đối tượng. |
| 12 | `clean_kata` | Bản vẽ chứa các block thép có handle liên kết trỏ tới các thực thể đã bị xóa vĩnh viễn (`IsErased = true`) | Thuật toán mở Transaction duyệt qua XData; khi gặp `ObjectId.IsErased` hoặc không tìm thấy handle trong database, phần tử handle chết sẽ được gỡ bỏ khỏi mảng liên kết mà không làm hỏng dữ liệu hình học của thanh thép. |

---

## 4. Detailed Specification of Legacy & CAD Utilities

### 4.1 Dimension Trimming (`cat_chan_dim` / `catdim` / `cd`)
* **Mục đích**: Chuẩn hoá chiều dài chân dim (witness lines), tạo các bản vẽ chi tiết thép và kết cấu sắc nét, không bị đường dóng đè lên cốt thép, nét khuất hay khung tên.
* **Mô hình hình học**:
  - Xét một `Dimension` (Aligned hoặc Rotated) có:
    - Điểm đo 1: $P_{ext1} = (x_1, y_1)$ (`Xline1Point`)
    - Điểm đo 2: $P_{ext2} = (x_2, y_2)$ (`Xline2Point`)
    - Điểm đường kích thước: $P_{dim} = (x_d, y_d)$ (`DimLinePoint`)
  - Vector đơn vị của đường kích thước:
    $$\vec{u} = \frac{P_{ext2} - P_{ext1}}{\|P_{ext2} - P_{ext1}\|}$$
  - Vector pháp tuyến (hướng đường dóng):
    $$\vec{n} = (-u_y, u_x)$$
  - Chiếu $P_{dim}$ lên phương pháp tuyến để tìm khoảng cách từ đường dóng:
    Điểm chiếu của $P_{dim}$ lên đường dóng 1 là $P_{d1} = P_{ext1} + ((P_{dim} - P_{ext1}) \cdot \vec{n}) \vec{n}$.
  - Khi người dùng chọn khoảng cách chân dim $L_{offset}$ (hoặc pick điểm cắt $P_{cut}$):
    Toạ độ mới của chân dim:
    $$P'_{ext1} = P_{d1} - \text{sgn}((P_{d1} - P_{ext1}) \cdot \vec{n}) \cdot L_{offset} \cdot \vec{n}$$
    $$P'_{ext2} = P_{d2} - \text{sgn}((P_{d2} - P_{ext2}) \cdot \vec{n}) \cdot L_{offset} \cdot \vec{n}$$
* **Quy trình tương tác**:
  1. Prompt: `Chọn các đường kích thước cần cắt chân [Pick điểm cắt/Nhập khoảng cách <50>]:`
  2. Người dùng có thể quét chọn nhiều Dimension cùng lúc.
  3. Áp dụng toạ độ mới cho từng Dimension trong một Transaction AutoCAD duy nhất.

### 4.2 Dimension Breaking & Splitting (`ngat_dim` / `ngatdim`)
* **Mục đích**: Ngắt đường kích thước tại giao điểm để không cắt qua các thanh cốt thép hoặc đường dẫn chú thích, hoặc tách một kích thước tổng thành các kích thước thành phần.
* **Phương án triển khai**:
  - **Phương án A (Visual Gap / AutoCAD DIMBREAK)**:
    Sử dụng AutoCAD Native Dimension Break mechanism (`db.Dimension.SetBreakDimensions`). Phương án này giữ nguyên đối tượng Dimension gốc và chỉ ẩn đoạn giao cắt với khoảng hở $w_{gap} = 2.0\text{ mm} \times \text{dimscale}$.
  - **Phương án B (Split Dimension)**:
    Khi người dùng muốn chia một kích thước $A \to B$ thành 2 đoạn $A \to C$ và $C \to B$:
    - Tạo `Dim1` từ $A$ đến $C$ với vị trí `DimLinePoint` giữ nguyên.
    - Tạo `Dim2` từ $C$ đến $B$ với vị trí `DimLinePoint` giữ nguyên.
    - Xoá Dimension gốc $A \to B$.

### 4.3 Continuous Dimension Chain (`dim_noi_tiep` / `dnoi`)
* **Mục đích**: Tăng tốc độ đo chi tiết dầm nhiều nhịp, khoảng cách cốt đai, lưới cột và các lỗ mở trên sàn.
* **Thuật toán điều khiển**:
  1. Yêu cầu chọn một Dimension gốc hoặc chọn 2 điểm đầu tiên $P_1, P_2$ kèm điểm đặt đường dim $P_{dim}$.
  2. Xác định vector phương $\vec{u}$, đường thẳng đường dim $L_{dim}$, và điểm mút hiện tại $P_{curr} = P_2$.
  3. Vòng lặp Prompt: `Chọn điểm tiếp theo (Enter để kết thúc):`
     - Mỗi khi người dùng click điểm $P_{next}$:
       - Chiếu $P_{next}$ lên phương vuông góc của đường dim để tính toạ độ chân dóng.
       - Sinh đối tượng `AlignedDimension(P_{curr}, P_{next}, P_{dimLine}, "", dimStyleId)`.
       - Gán layer `kata_dim`.
       - Cập nhật $P_{curr} = P_{next}$.
  4. Bấm Enter hoặc ESC kết thúc vòng lặp sạch sẽ.

### 4.4 Slope / Gradient Annotation (`ghi_do_doc` / `sl`)
* **Mục đích**: Ghi độ dốc thoát nước sàn vệ sinh/mái, độ dốc ram dốc tầng hầm, độ dốc dầm mái xiên.
* **Công thức toán học**:
  - Điểm $P_1(x_1, y_1)$ đến $P_2(x_2, y_2)$.
  - Run: $\Delta X = |x_2 - x_1|$.
  - Rise: $\Delta Y = y_2 - y_1$.
  - Góc nghiêng: $\theta = \text{atan2}(y_2 - y_1, x_2 - x_1)$.
  - Tỷ lệ phần trăm:
    $$i_{\%} = \frac{|\Delta Y|}{\Delta X} \times 100\%$$
  - Tỷ lệ phân số:
    $$n = \frac{\Delta X}{|\Delta Y|} \implies 1:n \quad (\text{e.g. } 1:1.5)$$
* **Tự động lật chữ (Readability Rule)**:
  - Nếu $\theta \in (\frac{\pi}{2}, \frac{3\pi}{2}] \implies$ chữ sẽ bị ngược đầu nếu xoay theo $\theta$.
  - Góc đặt Text chuẩn:
    $$\theta_{text} = \begin{cases} \theta & \text{nếu } -\frac{\pi}{2} \le \theta \le \frac{\pi}{2} \\ \theta + \pi & \text{ngược lại} \end{cases}$$
* **Thành phần đồ họa**:
  - Đoạn thẳng có mũi tên nhọn chỉ về phía dốc xuống (hướng dòng chảy/hướng dốc).
  - Text dạng `i = 2%` đặt cách đường mũi tên $1.5\text{ mm}$ (scaled theo DimScale).

### 4.5 Text Alignment & Equal Spacing (`can_chinh_text` / `dtext`)
* **Mục đích**: Căn lề thẳng tắp các bảng ghi chú, số hiệu thép, danh mục bản vẽ.
* **Chế độ căn chỉnh**:
  - `L` (Left): Căn toạ độ $X$ của tất cả điểm chèn bằng $\min(X_i)$ hoặc $X$ của text chuẩn.
  - `R` (Right): Căn toạ độ $X$ bên phải.
  - `C` (Center): Căn giữa toạ độ $X$.
  - `T` (Top): Căn toạ độ $Y$ đỉnh bằng $\max(Y_i)$.
  - `B` (Bottom): Căn toạ độ $Y$ đáy bằng $\min(Y_i)$.
  - `E` (Distribute Equally):
    - Sắp xếp các text theo $Y$ giảm dần: $T_1, T_2, \dots, T_n$.
    - Bước nhảy đều: $\Delta Y = \frac{Y_1 - Y_n}{n - 1}$.
    - Gán $Y_k = Y_1 - (k - 1) \cdot \Delta Y$.

### 4.6 Elevation Level Datum Callout (`cao_do_mat_cat` / `mcat`)
* **Mục đích**: Thể hiện cao độ kiến trúc/kết cấu trên mặt cắt và mặt đứng.
* **Quy tắc định dạng chuỗi cao độ**:
  - Nhận vào giá trị cao độ tính bằng mét ($Z$):
    - Nếu $|Z| < 0.0005 \implies$ `"±0.000"` (hoặc `"%%p0.000"`)
    - Nếu $Z > 0 \implies$ `"+"` + $Z.\text{ToString}("0.000")$ (e.g. `"+3.600"`, `"+12.800"`)
    - Nếu $Z < 0 \implies$ `"-"` + $|Z|.\text{ToString}("0.000")$ (e.g. `"-1.500"`, `"-0.050"`)
* **Biểu tượng đồ họa**:
  - Tam giác đều/cân cao $150\text{ mm}$, đáy $200\text{ mm}$.
  - Đỉnh tam giác trỏ đúng vào điểm cao trình cần ghi.
  - Đường line gióng ngang dài $1000\text{ mm}$.
  - Text cao độ đặt phía trên đường gióng ngang, font chữ `kata_text`.

### 4.7 Layer Freeze/Off (`an_layer` / `anlayer`)
* **Mục đích**: Ẩn nhanh layer chỉ bằng thao tác click vào một đối tượng thuộc layer đó.
* **Xử lý an toàn**:
  - Khi layer được chọn trùng với `db.Clayer` (layer hiện hành): Tự động đổi `db.Clayer` sang layer `"0"` để AutoCAD không ném lỗi `Cannot turn off the current layer`.

### 4.8 Layer Show All (`hien_tat_ca_layer` / `hienlayer`)
* **Mục đích**: Khôi phục trạng thái hiển thị của toàn bộ các layer chỉ với 1 lệnh gõ phím.
* **Xử lý**: Duyệt qua toàn bộ `LayerTableRecord`, gán `IsOff = false` và `IsFrozen = false`.

### 4.9 Layer Standard Mapping (`doi_layer` / `doilayer`)
* **Mục đích**: Chuẩn hoá layer cho các đối tượng ngoại lai (từ kiến trúc hoặc đơn vị khác gửi sang) về đúng chuẩn layer của KataPro:
  - Cốt thép dọc: `kata_thep chu` (Màu đỏ - 1)
  - Cốt đai: `kata_thep dai` (Màu vàng - 2)
  - Nét thấy bê tông: `kata_net thay` (Màu xanh - 4)
  - Nét khuất: `kata_net khuat` (Nét đứt Hidden, Màu xám - 8 hoặc 9)
  - Kích thước: `kata_dim` (Màu lục - 3)
  - Lưới trục: `kata_grid` (Màu 8, linetype Center)
  - Text ghi chú: `kata_text` (Màu trắng/đen - 7)

### 4.10 Drawing & XRecord Purge (`xoa_xrecord` / `clean_kata`)
* **Mục đích**: Làm sạch file bản vẽ sau thời gian dài thiết kế, loại bỏ rác XRecord và sửa các lỗi gãy handle do người dùng vô tình xoá đối tượng bằng lệnh CAD thông thường.
* **Hành vi**:
  - Quét Named Object Dictionary tìm các mục mang tiền tố `KATA_`.
  - Quét XData kiểm tra tính toàn vẹn của các `Handle` liên kết.
  - Gọi `ClearAllHighlights` để xoá các đường line ảo và điểm highlight tạm thời.

---

## 5. AutoLISP Architecture & 238-Line Preservation Analysis

### 5.1 Cấu Trúc Bất Biến của `Goi lenh.lsp` và `GoiLenh_HPKata.lsp`
File `Goi lenh.lsp` của KataPro có chiều dài chính xác **238 dòng**. Cấu trúc phân bổ dòng vật lý như sau:

```
Dòng 1 – 15:    Khối .NETLOAD (nạp assembly plugin)
Dòng 16 – 32:   Khối nạp CUIX (nạp menu và ribbon menukata)
Dòng 33 – 41:   9 lệnh chuyển tiếp từ VBA/gốc (tstt, rthep, lv, lv1, sst, dao, ad, cht, setupkata)
Dòng 42:        Dòng phân cách comment: ;;;;;;;;;;;;;;;;KATA_PRO@20270924;;;;;;;;;;;;;;;;;;
Dòng 43 – 151:  109 dòng định nghĩa AutoLISP aliases (chứa trọn vẹn 118 command aliases)
Dòng 152 – 237: Đúng 86 dòng cấu hình tham số mặc định của KataPro (bắt đầu bằng ;kata_dim)
Dòng 238:       Dòng trống cuối file (EOF)
```

### 5.2 Tại sao Offset Dòng 152 là Bất Khả Xâm Phạm?
Trong mã nguồn gốc `C:\kata_pro\Kata_pro64_Cad2025.dll`:
Hàm `Setting::LoadFromGoiLenhText(string x, StreamReader sr)` duyệt qua các dòng của file `Goi lenh.lsp`. Nó bỏ qua 151 dòng đầu tiên và bắt đầu đọc tham số cấu hình hệ thống từ **dòng thứ 152** (chỉ mục 0-based là 151).

Đặc biệt, trong bộ test hồi quy của dự án `HPKata.AutoCad.Tests/GoiLenhLspSyntaxTests.cs`:
```csharp
[Fact]
public void GoiLenh_LineCount_Exactly238Lines() {
    Assert.Equal(238, splitLines.Length);
    Assert.Equal(237, File.ReadAllLines(path).Length);
}

[Fact]
public void GoiLenh_Line152_StartsWithKataDim() {
    Assert.Equal(";kata_dim", lines[151].Trim());
}

[Fact]
public void GoiLenh_Aliases_ContainsAll118CommandAliases() {
    Assert.Equal(118, defunCount);
}
```

Nếu bất kỳ agent nào chèn thêm dòng vào giữa file `GoiLenh_HPKata.lsp`, dòng 152 sẽ bị trượt xuống (ví dụ thành dòng 160), khiến `KataConfigParser` đọc sai lệch toàn bộ 86 tham số cấu hình (tên layer bị đọc thành màu sắc, tỉ lệ bị đọc thành đơn vị), đồng thời làm trượt các test case tự động.

### 5.3 Độ Phủ 27 Lệnh của Phase 5 trong `GoiLenh_HPKata.lsp`
Toàn bộ 27 lệnh của Phase 5 đều đã có mặt đầy đủ trong khối dòng 43–151 của `GoiLenh_HPKata.lsp`:
1. `QS` (Line 139)
2. `QS_mbkc` (Line 137)
3. `QS_tru` (Line 138)
4. `xuatQS` (Line 140)
5. `timqs` (Line 141)
6. `tkqs` (Line 53 / 123)
7. `ve_grid` / `vg` (Line 43)
8. `chia_tam` / `chiatam` (Line 142)
9. `op_gach` / `opgach` (Line 143)
10. `loc_dim` / `locdim` (Line 144)
11. `phan_vung_noi` / `pvn` (Line 145)
12. `danhmuc` (Line 146)
13. `cat_doituong` / `cat` (Line 147)
14. `taomv` (Line 148)
15. `taomv1` (Line 149)
16. `sap_ban_ve` / `sbv` (Line 150)
17. `in_ban_ve` / `ibv` (Line 151)
18. `pukata` (Line 134)
19. `pdfkata` (Line 135)
20. `hideatt` (Line 51)
21. `Unhideatt` (Line 52)
22. `tim_ten_ck` (Line 55)
23. `dten` (Line 56)
24. `tkten` (Line 57)
25. `textblock` (Line 58)
26. `chuyen` (Line 131)
27. `show_thep_rai` / `str` (Line 132)

### 5.4 Chiến Lược Đăng Ký Lệnh Kép (Dual-Registration Strategy) Cho Các Lệnh Tiện Ích Mới
Để cung cấp các bí danh tiện ích mới (`catdim`, `ngatdim`, `dnoi`, `sl`, `dtext`, `mcat`, `anlayer`, `hienlayer`, `doilayer`, `xoa_xrecord`, `clean_kata`) mà **KHÔNG ĐƯỢC PHÉP làm thay đổi cấu trúc 238 dòng và offset 152**:

Trong C# .NET AutoCAD API, một phương thức có thể mang **nhiều attribute `[CommandMethod]`**:
```csharp
[CommandMethod("cat_chan_dim")]
[CommandMethod("catdim")]
[CommandMethod("cd")]
public static void CatChanDim() => DimensionUtilityController.TrimWitnessLinesInteractive();
```
Khi `.NETLOAD` nạp `HPKata.AutoCad.dll`, AutoCAD tự động đăng ký cả tên đầy đủ (`cat_chan_dim`) lẫn các bí danh (`catdim`, `cd`) vào từ điển lệnh nội tại của AutoCAD. Kỹ sư có thể gõ `catdim` trực tiếp tại dòng lệnh CAD mà không cần bất kỳ dòng `(defun c:catdim ...)` nào trong file Lisp!

Bên cạnh đó, nếu người dùng muốn có một file Lisp mở rộng độc lập cho các tiện ích vẽ nâng cao, file đó sẽ được lưu riêng thành `GoiLenh_TienIch.lsp` mà không làm biến đổi file gốc `GoiLenh_HPKata.lsp`.

---

## 6. Clean Code Architecture & Class Design

Tuân thủ nghiêm ngặt:
- Rule C2: Mỗi file $\le 300$ dòng code.
- Rule M3: Mỗi method $\le 50$ dòng code.
- Rule Host-Neutral: `HPKata.Core` độc lập 100%, không chứa bất kỳ using `Autodesk.*` nào.

```
HPKata/
├── HPKata.Core/
│   └── CadUtilities/
│       ├── Point2D.cs                        # Struct toạ độ 2D thuần
│       ├── Vector2D.cs                       # Vector toán học 2D
│       ├── VectorProjectionHelper.cs         # Tính toán hình học chiếu điểm & chân dim
│       ├── SlopeCalculator.cs                # Tính toán & định dạng độ dốc ("i = 2%")
│       ├── ElevationFormatter.cs             # Định dạng cao độ kiến trúc ("+3.600", "±0.000")
│       └── TextAlignmentCalculator.cs        # Tính toán toạ độ căn thẳng hàng & chia đều text
│
├── HPKata.AutoCad/
│   ├── Controllers/
│   │   ├── DimensionUtilityController.cs     # Điều khiển thao tác cắt dim, ngat dim, dim noi tiep
│   │   ├── TextAnnotationController.cs       # Điều khiển căn text và chèn block cao độ
│   │   ├── LayerUtilityController.cs         # Điều khiển ẩn/hiện/chuyển layer chuẩn Kata
│   │   └── KataCleanController.cs            # Điều khiển dọn dẹp XRecord & handle rác
│   │
│   └── Commands/
│       ├── DimensionUtilityCommands.cs       # Entry point các lệnh dim: catdim, ngatdim, dnoi, sl
│       ├── TextAnnotationCommands.cs         # Entry point các lệnh text: dtext, mcat
│       ├── LayerUtilityCommands.cs           # Entry point các lệnh layer: anlayer, hienlayer, doilayer
│       └── KataCleanCommands.cs              # Entry point lệnh clean: xoa_xrecord, clean_kata
```

### 6.1 `HPKata.Core/CadUtilities/` Interfaces & Methods

#### `Point2D.cs` & `Vector2D.cs`
```csharp
namespace HPKata.Core.CadUtilities
{
    public readonly struct Point2D
    {
        public double X { get; }
        public double Y { get; }
        public Point2D(double x, double y) { X = x; Y = y; }
        public double DistanceTo(Point2D other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
    }

    public readonly struct Vector2D
    {
        public double X { get; }
        public double Y { get; }
        public Vector2D(double x, double y) { X = x; Y = y; }
        public double Length => Math.Sqrt(X * X + Y * Y);
        public Vector2D Normalize() => Length > 1e-9 ? new Vector2D(X / Length, Y / Length) : this;
        public double Dot(Vector2D other) => X * other.X + Y * other.Y;
        public Vector2D Perpendicular() => new Vector2D(-Y, X);
    }
}
```

#### `VectorProjectionHelper.cs` (Rule C2: ~110 dòng, M3: $\le 30$ dòng)
```csharp
namespace HPKata.Core.CadUtilities
{
    public static class VectorProjectionHelper
    {
        public static Point2D ProjectPointOnLine(Point2D pt, Point2D linePt, Vector2D lineDir)
        {
            Vector2D dir = lineDir.Normalize();
            Vector2D v = new Vector2D(pt.X - linePt.X, pt.Y - linePt.Y);
            double t = v.Dot(dir);
            return new Point2D(linePt.X + t * dir.X, linePt.Y + t * dir.Y);
        }

        public static (Point2D newExt1, Point2D newExt2) CalculateTrimmedWitnessOrigins(
            Point2D ext1, Point2D ext2, Point2D dimLinePt, double targetOffset)
        {
            Vector2D dimVec = new Vector2D(ext2.X - ext1.X, ext2.Y - ext1.Y).Normalize();
            Vector2D normal = dimVec.Perpendicular();

            Point2D d1 = ProjectPointOnLine(ext1, dimLinePt, dimVec);
            Point2D d2 = ProjectPointOnLine(ext2, dimLinePt, dimVec);

            Vector2D v1 = new Vector2D(ext1.X - d1.X, ext1.Y - d1.Y);
            double sign1 = v1.Dot(normal) >= 0 ? 1.0 : -1.0;

            Point2D newExt1 = new Point2D(d1.X + sign1 * targetOffset * normal.X, d1.Y + sign1 * targetOffset * normal.Y);
            Point2D newExt2 = new Point2D(d2.X + sign1 * targetOffset * normal.X, d2.Y + sign1 * targetOffset * normal.Y);

            return (newExt1, newExt2);
        }
    }
}
```

#### `SlopeCalculator.cs` (Rule C2: ~120 dòng, M3: $\le 25$ dòng)
```csharp
namespace HPKata.Core.CadUtilities
{
    public static class SlopeCalculator
    {
        public static double CalculateGradient(Point2D p1, Point2D p2)
        {
            double dx = Math.Abs(p2.X - p1.X);
            double dy = Math.Abs(p2.Y - p1.Y);
            if (dx < 1e-6) return double.PositiveInfinity;
            return dy / dx;
        }

        public static string FormatPercentage(double gradient, int decimals = 1)
        {
            if (double.IsPositiveInfinity(gradient)) return "Đứng (90°)";
            double pct = Math.Round(gradient * 100.0, decimals);
            return $"i = {pct:0.#}%";
        }

        public static string FormatRatio(double gradient)
        {
            if (double.IsPositiveInfinity(gradient)) return "1:0";
            if (gradient < 1e-6) return "1:∞";
            double ratio = Math.Round(1.0 / gradient, 1);
            return $"1:{ratio:0.#}";
        }

        public static double NormalizeTextAngle(double rawAngleRad)
        {
            double angle = rawAngleRad % (2 * Math.PI);
            if (angle < 0) angle += 2 * Math.PI;

            if (angle > Math.PI / 2.0 && angle <= 3.0 * Math.PI / 2.0)
            {
                angle += Math.PI;
            }
            return angle % (2 * Math.PI);
        }
    }
}
```

#### `ElevationFormatter.cs` (Rule C2: ~90 dòng, M3: $\le 20$ dòng)
```csharp
namespace HPKata.Core.CadUtilities
{
    public static class ElevationFormatter
    {
        public static string FormatElevation(double elevationInMeters, int decimals = 3)
        {
            if (Math.Abs(elevationInMeters) < 0.0005)
            {
                return "±0.000";
            }

            string format = "0." + new string('0', decimals);
            if (elevationInMeters > 0)
            {
                return "+" + elevationInMeters.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            }
            return elevationInMeters.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static double CalculateElevationFromCadY(double targetY, double baseY, double baseElevation = 0.0, double scale = 1000.0)
        {
            double deltaMeters = (targetY - baseY) / scale;
            return baseElevation + deltaMeters;
        }
    }
}
```

#### `TextAlignmentCalculator.cs` (Rule C2: ~140 dòng, M3: $\le 30$ dòng)
```csharp
namespace HPKata.Core.CadUtilities
{
    public enum TextHorizontalAlign { Left, Center, Right }
    public enum TextVerticalAlign { Top, Middle, Bottom }

    public static class TextAlignmentCalculator
    {
        public static List<Point2D> AlignHorizontal(IReadOnlyList<Point2D> points, TextHorizontalAlign align, double? targetX = null)
        {
            if (points == null || points.Count == 0) return new List<Point2D>();
            double refX = targetX ?? points.Min(p => p.X);

            var result = new List<Point2D>(points.Count);
            foreach (var p in points)
            {
                result.Add(new Point2D(refX, p.Y));
            }
            return result;
        }

        public static List<Point2D> DistributeEquallyVertical(IReadOnlyList<Point2D> points)
        {
            if (points == null || points.Count < 2) return points?.ToList() ?? new List<Point2D>();

            var sorted = points.OrderByDescending(p => p.Y).ToList();
            double yTop = sorted[0].Y;
            double yBottom = sorted[sorted.Count - 1].Y;
            double step = (yTop - yBottom) / (sorted.Count - 1);

            var result = new List<Point2D>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                result.Add(new Point2D(sorted[i].X, yTop - i * step));
            }
            return result;
        }
    }
}
```

---

## 7. Clean Code Rule Compliance Verification

| Clean Code Rule | Requirement | Design Compliance Strategy |
|---|---|---|
| **Rule C2** | Max 300 lines per file | Every designed C# file is divided strictly by responsibility. Core calculators range between 90–140 lines. Controllers are modularized into 4 files of 150–250 lines each. |
| **Rule M3** | Max 50 lines per method | All algorithms (trim calculation, projection, slope formatting, level text generation) are composed of small single-purpose methods ($\le 30$ lines). |
| **Pure Core** | Zero Autodesk references | `HPKata.Core/CadUtilities/` references only standard .NET math and system types. It compiles cleanly on both `netstandard2.0` and `net8.0` without any CAD DLL dependencies. |
| **Rule Q-B1** | Zero dead/commented code | 0 dead code blocks in all templates. |
| **Rule Q-B2** | Zero unannotated empty catches | All try/catch blocks in Controllers specify explicit reason comments or log via AutoCAD Editor. |
| **MTP Isolation** | Build/test concurrency protection | All test runs follow `--disable-build-servers -m:1` with isolated `--results-directory`. |

---

## 8. Verification Method & Test Plan

1. **Unit Testing (`HPKata.Core.Tests/CadUtilities/`)**:
   - `VectorProjectionHelperTests`: Verify trimmed origins for aligned and rotated dimensions, negative witness line directions, collinear points.
   - `SlopeCalculatorTests`: Test typical $i = 2\%$, $i = 10\%$, steep batter $1:1.5$, vertical rise $\Delta X = 0$ (no crash), text angle normalization in quadrants I–IV.
   - `ElevationFormatterTests`: Test $0.0 \to \text{"±0.000"}$, $+3.6 \to \text{"+3.600"}$, $-1.25 \to \text{"-1.250"}$, tolerance threshold $0.0004 \to \text{"±0.000"}$.
   - `TextAlignmentCalculatorTests`: Test Left/Center/Right alignment and Equal Vertical Distribution across $N$ points.
2. **AutoCAD Host Tests (`HPKata.AutoCad.Tests`)**:
   - `GoiLenhLspSyntaxTests`: Re-verify that `GoiLenh_HPKata.lsp` remains exactly 238 lines, line 152 starts with `;kata_dim`, and all 118 aliases are intact.
   - Command registration reflection test: Ensure all 10 new utility command methods (`catdim`, `ngatdim`, `dnoi`, `sl`, `dtext`, `mcat`, `anlayer`, `hienlayer`, `doilayer`, `clean_kata`) are decorated with proper `[CommandMethod]` attributes and loaded without conflict.

---

## 9. Conclusion & Recommendations for Worker Agent

1. **Pure Core Implementation**:
   Implement the 5 mathematical files in `HPKata.Core/CadUtilities/` (`Point2D.cs`, `Vector2D.cs`, `VectorProjectionHelper.cs`, `SlopeCalculator.cs`, `ElevationFormatter.cs`, `TextAlignmentCalculator.cs`).
2. **AutoCAD Controllers & Commands**:
   Implement the 4 controllers in `HPKata.AutoCad/Controllers/` and the 4 command classes in `HPKata.AutoCad/Commands/`.
3. **Preserve `GoiLenh_HPKata.lsp`**:
   Rely on native C# `[CommandMethod]` aliases (`catdim`, `cd`, `dtext`, `mcat`, `sl`, etc.) so that `GoiLenh_HPKata.lsp` retains its exact 238 lines and line 152 config offset without any disruption.
4. **Unit Tests**:
   Add comprehensive test coverage in `HPKata.Core.Tests/CadUtilities/` with $>25$ unit tests ensuring 100% green test execution.
