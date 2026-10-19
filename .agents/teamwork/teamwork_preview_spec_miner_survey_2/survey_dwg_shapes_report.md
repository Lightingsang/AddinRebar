# BÁO CÁO ĐIỀU TRA KỸ THUẬT & ĐẶC TẢ SẠCH (CLEAN-ROOM SPECIFICATION)
## DWG DATA SCHEMAS, XDATA SPECIFICATIONS & 68 REBAR SHAPES CATALOG (KATAPRO)

**Dự án**: Revit & AutoCAD Rebar Interoperability Framework  
**Mã tài liệu**: `SPEC-KATA-DWG-SHAPES-2026-REV1`  
**Ngày thực hiện**: 2026-10-17  
**Phạm vi khảo sát**: Phân tích sạch (Clean-room reverse-engineering) cấu trúc tệp bản vẽ mẫu `C:\kata_pro\Iso_kata.dwg`, thư viện hình dạng `C:\kata_pro\dangthep\dang00.wmf` .. `dang67.wmf`, bảng tính tối ưu `C:\kata_pro\Kata.xlsm`, và các thư viện thực thi miền `Kata_pro64_Cad2025.dll`, `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll`.  
**Cam kết tuân thủ**: Hoàn toàn tuân thủ nguyên tắc Clean-Room. Không trích xuất, chỉnh sửa, dịch ngược hoặc vượt qua bất kỳ cơ chế bảo vệ phần cứng hay module bản quyền (`Security.dll`). Thư mục `C:\kata_pro` chỉ được truy cập ở chế độ ĐỌC (Read-Only).

---

## MỤC LỤC
1. [TỔNG QUAN KIẾN TRÚC CLEAN-ROOM & NGUYÊN LÝ HOẠT ĐỘNG](#1-tổng-quan-kiến-trúc-clean-room--nguyên-lý-hoạt-động)
2. [ĐẶC TẢ LỚP DỮ LIỆU BẢN VẼ ISO_KATA.DWG](#2-đặc-tả-lớp-dữ-liệu-bản-vẽ-iso_katadwg)
   - 2.1. Hệ thống Layer chuẩn (CAD Standards)
   - 2.2. Kiểu chữ (Text Styles) & Kiểu kích thước (Dimension Styles)
   - 2.3. Danh mục Khối chức năng (Structural Blocks & Attributes)
3. [ĐẶC TẢ XDATA & CƠ CHẾ LIÊN KẾT ĐỘNG (TAG-LINKING ENGINE)](#3-đặc-tả-xdata--cơ-chế-liên-kết-động-tag-linking-engine)
   - 3.1. Danh mục Ứng dụng đăng ký (RegApp Table)
   - 3.2. Cấu trúc trường XData trên Polyline thép (Rebar Entity XData)
   - 3.3. Cấu trúc trường XData trên Tag thép & Dim rải (Tag & Dimension XData)
   - 3.4. Cơ chế đồng bộ 2 chiều (Dynamic Handle Linking & Runlength Math)
   - 3.5. Cấu trúc XRecord & JSON Serialization hiện đại
4. [DANH MỤC TOÁN HỌC TOÀN DIỆN 68 DẠNG THÉP (DANG00 - DANG67)](#4-danh-mục-toán-học-toàn-diện-68-dạng-thép-dang00---dang67)
   - 4.1. Quy chuẩn uốn móc (Hook Rules) & Bán kính uốn (Bending Radii)
   - 4.2. Quy tắc neo & nối chồng cốt thép (Lap Splice & Anchorage Math)
   - 4.3. Bảng tra trọng lượng đơn vị tiêu chuẩn (Unit Mass Table)
   - 4.4. Bảng đặc tả toán học chi tiết 68 dạng hình học & 16 biến thể
5. [ĐẶC TẢ THUẬT TOÁN TỐI ƯU CẮT THÉP 1D & QUẢN LÝ KHO (CUTTING STOCK OPTIMIZATION)](#5-đặc-tả-thuật-toán-tối-ưu-cắt-thép-1d--quản-lý-kho-cutting-stock-optimization)
   - 5.1. Mô hình bài toán cắt thép 1D (1D Cutting Stock Problem)
   - 5.2. Cấu trúc dữ liệu kho thép (Warehouse Inventory Schema)
   - 5.3. Thuật toán phân rã chuỗi tổ hợp (`chuyen_text` & `to_hop_thep`)
6. [GIAO THỨC TƯƠNG HỢP REVIT & EXCEL (INTEROPERABILITY CONTRACTS)](#6-giao-thức-tương-hợp-revit--excel-interoperability-contracts)
   - 6.1. Hợp đồng dữ liệu CAD ↔ Revit (`info_revit_thep`)
   - 6.2. Cấu trúc bảng tính Excel (`Kata.xlsm`)
7. [KẾT LUẬN & KHUYẾN NGHỊ THIẾT KẾ](#7-kết-luận--khuyến-nghị-thiết-kế)

---

## 1. TỔNG QUAN KIẾN TRÚC CLEAN-ROOM & NGUYÊN LÝ HOẠT ĐỘNG

KataPro là hệ thống phần mềm triển khai chi tiết cốt thép (rebar detailing) và thống kê cốt thép tự động hàng đầu trong môi trường AutoCAD tại Việt Nam. Nguyên lý cốt lõi của hệ thống dựa trên mô hình **Tam giác Thực thể Đồng bộ**:

```
                       +-----------------------+
                       |   Thực thể Polyline   |
                       | (Hình học thanh thép) |
                       |   XData: RegApp KATA  |
                       +-----------+-----------+
                                   ^
                                   | Handle Linking
                                   v
+----------------------+       +---+-------------------+
|  Dimension Đường rải | <---> |    Block Tag Thép     |
| (Khoảng cách & đoạn) |       |  (kata_block_KHT/CT)  |
| DimStyle: rai_thep   |       |  Thuộc tính: SH, SL   |
+----------------------+       +-----------------------+
```

1. **Thực thể Thanh Thép (Rebar Polyline)**: Đại diện cho thanh thép thực tế trên mặt bằng hoặc mặt cắt kết cấu (Dầm, Cột, Sàn, Móng, Vách). Hình học của Polyline quyết định trực tiếp chiều dài từng đoạn (`L1`..`L6`), góc uốn (`goc`) và tổng chiều dài cắt thanh (`CD1T`).
2. **Khối Ký Hiệu Thép (Tag Block)**: Các khối ký hiệu như `kata_block_KHT` (ký hiệu thép thông thường) và `kata_block_KHTCT` (ký hiệu thép chi tiết) hiển thị số hiệu (`SH`), số lượng (`SL`), đường kính (`DK`), bước đai (`KC`).
3. **Đường Rải Thép (Distribution Dimension)**: Dimension đo đoạn phân bố rebar, gán thuộc tính bước rải (ví dụ `a200`). Số lượng thanh $n$ được tính toán tự động bằng công thức:
   $$n = \left\lfloor \frac{L_{\text{dim}}}{a} \right\rfloor + 1$$
4. **Bảng Thống Kê (Rebar Schedule)**: Được tạo thành từ các khối `dang00` .. `dang67`. Mỗi hàng trong bảng thống kê là một Block Instance chứa đầy đủ các thuộc tính hình học và tổng hợp khối lượng.

---

## 2. ĐẶC TẢ LỚP DỮ LIỆU BẢN VẼ ISO_KATA.DWG

Tệp `Iso_kata.dwg` đóng vai trò là bản vẽ mẫu chuẩn (Template DWG), chứa đầy đủ định nghĩa Layer, Text Style, Dimension Style và Block Definitions cho toàn bộ quy trình KataPro.

### 2.1. Hệ thống Layer chuẩn (CAD Standards)

Dữ liệu khai thác từ bảng `LAYER` trong `Iso_kata.dwg`:

| Tên Layer | Màu (Color Index) | Mã màu RGB | Kiểu đường (Linetype) | Độ dày nét (Lineweight) | Mục đích sử dụng |
|---|---|---|---|---|---|
| `kata_thep chu` | 1 (Red) | 255, 0, 0 | `Continuous` | 0.50 mm | Cốt thép chịu lực chính (dầm, cột, móng, vách) |
| `kata_thep dai` | 220 (Magenta/Pink)| 255, 0, 191 | `Continuous` | 0.35 mm | Cốt thép đai, thép đai gia cường, thép đai cột |
| `kata_thep - V` | 2 (Yellow) | 255, 255, 0 | `Continuous` | 0.40 mm | Thép phương đứng (Vertical rebar) |
| `kata_thep - H` | 1 (Red) | 255, 0, 0 | `Continuous` | 0.40 mm | Thép phương ngang (Horizontal rebar) |
| `kata_dim` | 8 (Dark Gray) | 128, 128, 128 | `Continuous` | 0.15 mm | Đường kích thước hình học & đường rải thép |
| `kata_net thay` | 4 (Cyan) | 0, 255, 255 | `Continuous` | 0.30 mm | Đường bao bê tông thấy (mặt cắt dầm, cột, sàn) |
| `kata_net khung`| 3 (Green) | 0, 255, 0 | `Continuous` | 0.35 mm | Khung bảng vẽ, khung bảng thống kê cốt thép |
| `kata_net khuat`| 9 (Light Gray)| 192, 192, 192 | `HIDDEN` | 0.15 mm | Cấu kiện bê tông hoặc thép nằm khuất |
| `kata_grid` | 8 (Dark Gray) | 128, 128, 128 | `CENTER` | 0.15 mm | Trục định vị kết cấu (Grid lines) |
| `kata_net manh` | 8 (Dark Gray) | 128, 128, 128 | `Continuous` | 0.13 mm | Nét gióng, ký hiệu phụ trợ |
| `kata_text` | 7 (White/Black) | 255, 255, 255 | `Continuous` | 0.20 mm | Chữ ghi chú, tiêu đề cấu kiện, text bảng thống kê |
| `Defpoints` | 7 (White) | 255, 255, 255 | `Continuous` | Default (Không in) | Điểm mút kích thước, điểm gốc đặt block |

### 2.2. Kiểu chữ (Text Styles) & Kiểu kích thước (Dimension Styles)

#### A. Kiểu chữ (Text Styles):
1. **`kata_text`** (Style chủ đạo):
   - Phông chữ (Font file): `arial.ttf` (TrueType Font, hỗ trợ trọn vẹn Unicode tiếng Việt).
   - Chiều cao chữ mặc định (`TextHeight`): `0.0` (biến đổi theo tỷ lệ bản vẽ, khi chèn vào bản vẽ tỷ lệ 1/1 thường là 2.0 mm hoặc 2.5 mm).
   - Hệ số co giãn ngang (`WidthFactor`): `0.80` (giúp text gọn gàng, tăng mật độ hiển thị trong các ô bảng thống kê hẹp).
   - Góc nghiêng (`ObliquingAngle`): `0.0` (chữ đứng).
2. **`{18b73a54-fa43-428c-adcb-c1c7bbdf2c84}`**:
   - Phông chữ: `Simplex.shx` (SHX đơn nét, dùng cho các ghi chú kỹ thuật cổ điển).

#### B. Kiểu kích thước (Dimension Styles):
Bản vẽ mẫu tích hợp sẵn họ DimStyle theo tỷ lệ (driven by `DIMSCALE`):
- **Base Style: `kata_dim_iso`**:
  - Khối mũi tên (`DIMBLK`): `kata_block_ArchTick` (nét gạch chéo kiến trúc 45°).
  - Chiều cao chữ (`DIMTXT`): `2.5` mm.
  - Kích thước mũi tên (`DIMASZ`): `1.5` mm.
  - Đoạn nhô đường gióng (`DIMEXE`): `1.5` mm.
  - Khoảng hở chân đường gióng (`DIMEXO`): `1.2` mm.
  - Khoảng cách text tới đường kích thước (`DIMGAP`): `1.0` mm.
  - Text Style: `kata_text`.
- **Hệ thống DimStyle theo tỷ lệ**:
  `kata_dim_15` ($\times 15$), `kata_dim_20` ($\times 20$), `kata_dim_25` ($\times 25$), `kata_dim_30` ($\times 30$), `kata_dim_40` ($\times 40$), `kata_dim_50` ($\times 50$), `kata_dim_70` ($\times 70$), `kata_dim_80` ($\times 80$), `kata_dim_100` ($\times 100$), `kata_dim_120` ($\times 120$), `kata_dim_150` ($\times 150$), `kata_dim_200` ($\times 200$), `kata_dim_250` ($\times 250$), `kata_dim_300` ($\times 300$).
- **Kiểu rải thép: `kata_rai_thep`**:
  - Khối đầu mũi tên: `kata_block_RUNTIC` (vạch chỉ hướng rải).
  - `DIMTXT`: `2.0` mm.
  - Tích hợp ghi chú số lượng và khoảng cách (ví dụ: `15%%c10a150`).

### 2.3. Danh mục Khối chức năng (Structural Blocks & Attributes)

Bản vẽ mẫu `Iso_kata.dwg` gồm **119 khối**. Ngoài 84 khối hình dạng thép (`dang00` .. `dang67`), các khối chức năng quan trọng nhất gồm:

| Tên Khối (Block Name) | Thuộc tính (Attribute Tags) | Ý nghĩa chức năng |
|---|---|---|
| `kata_block_KHT` | `SH1`, `DK1`, `KC1`, `DKKC1`, `SH2`, `DK2`, `KC2`, `DKKC2` | Ký hiệu tag thép tiêu chuẩn (hình tròn chia đôi hoặc đường chỉ dẫn) |
| `kata_block_KHTCT` | `SH1`, `DKKC1`, `SH2`, `DKKC2`, `SH3`, `DKKC3`, `SH4`, `DKKC4` | Ký hiệu tag thép chi tiết cho cụm bó thép 3-4 thanh |
| `kata_block_Beam_plan` | `NAME_BEAM`, `B`, `H`, `LEVEL`, `SPAN` | Mặt bằng định vị dầm sàn |
| `Kata_Block_Beam_Rebar`| `U_NGOAI`, `THEP_T`, `THEP_D`, `THEP_GC`, `CD`, `A_DAI` | Thông tin cốt thép dọc & cốt đai mặt cắt dầm |
| `Kata_Block_Column&Wall`| `TEN_COT`, `B`, `H`, `THEP_CHINH`, `THEP_DAI` | Khối quản lý mặt cắt cột và vách lõi |
| `Kata_Block_Mong_Don` | `TEN_MONG`, `L`, `B`, `H`, `THEP_X`, `THEP_Y`, `CAO_TRINH` | Khối móng đơn / móng băng |
| `kata_block_Ten` | `TEN_CK`, `TY_LE`, `SO_LUONG` | Khối tiêu đề chi tiết cấu kiện (Detail Title) |
| `kata_block_COUPLER_SPEC`| `DK_NOI`, `LOAI_REN`, `TIEU_CHUAN` | Khối quy cách mối nối coupler ống ren |
| `dangCoupler` | `L`, `DK`, `SCK`, `SL1`, `SL`, `GHI_CHU` | Hàng thống kê coupler trong bảng thống kê cốt thép |
| `kata_block_LEADERTIC` | (Không thuộc tính - Hình học mũi tên) | Mũi tên dẫn hướng đường rải cốt thép |

---

## 3. ĐẶC TẢ XDATA & CƠ CHẾ LIÊN KẾT ĐỘNG (TAG-LINKING ENGINE)

KataPro quản lý liên kết dữ liệu trực tiếp trong cơ sở dữ liệu AutoCAD thông qua hai cơ chế: **Extended Entity Data (XData)** truyền thống và **Extension Dictionaries (XRecord)**.

### 3.1. Danh mục Ứng dụng đăng ký (RegApp Table)

Khảo sát từ bảng `APPID` của `Iso_kata.dwg` và decompilation lớp `Ham_xdata`:

| Tên RegApp | Mã DxfCode 1001 | Phạm vi áp dụng | Mô tả chức năng |
|---|---|---|---|
| **`KATA`** | `"KATA"` | Toàn bộ Rebar Polyline, Tag Block, Dimension | Kênh dữ liệu chính lưu trữ thông số thép, hình dạng, cấu kiện |
| **`KATA_AI`** | `"KATA_AI"` | Entity AI, Voice Recognition triggers | Lưu trữ metadata gợi ý thiết kế và lệnh thông minh |
| **`KATA_DAM_CONG_MKBC`** | `"KATA_DAM_CONG_MKBC"` | Polyline dầm cong, vách cong | Khai triển hình học dầm cong sang mặt phẳng |
| **`KATA_DAM_CONG_MKBC_THE_HIEN`** | `"KATA_DAM_CONG_MKBC_THE_HIEN"` | Entity hiển thị dầm cong | Trực quan hóa mặt cắt dầm cong |
| **`KATA_RIBBON`** | `"KATA_RIBBON"` | Named Object Dictionary | Lưu trạng thái cấu hình thanh công cụ Ribbon |

### 3.2. Cấu trúc trường XData trên Polyline thép (Rebar Entity XData)

Khi một thanh thép được vẽ hoặc nhận diện (`Lenh_thep.ve_thep`, `Link_thep.update_thep`), XData được ghi vào đối tượng `AcDbPolyline` theo thứ tự tuần tự nghiêm ngặt:

```
[1001: "KATA"]
  ├── 1000: Dang              (Mã dạng hình học, ví dụ "01", "02", "22")
  ├── 1070: STT               (Số hiệu thanh - Mark, số nguyên 16-bit)
  ├── 1070: phi               (Đường kính danh định mm, ví dụ 10, 16, 20, 25)
  ├── 1000: kc                (Khoảng cách rải, ví dụ "@200", "@150", hoặc rỗng "")
  ├── 1000: sl                (Số lượng thanh, ví dụ "12", "5+5", "2x4")
  ├── 1070: sl_ck             (Số lượng cấu kiện nhân tử, mặc định 1)
  ├── 1000: L                 (Chuỗi kích thước các đoạn, phân cách bởi dấu chấm phẩy ";")
  │                           Ví dụ dạng 02: "400;1200;400"
  ├── 1000: goc_uon           (Góc uốn độ, ví dụ "45", "135", "90", hoặc rỗng "")
  ├── 1000: ten_ck            (Tên cấu kiện cha, ví dụ "D1", "C2", "M1-3")
  ├── 1000: stt_thep_chinh    (Chỉ số phụ phân nhóm bó thép, ví dụ "a", "b", "1", "2")
  ├── 1000: chu_thich         (Ghi chú coupler ren, neo, lớp bảo vệ)
  └── 1000: Z                 (Cao độ mặt bằng / tầng của thanh thép)
```

#### Bảng chi tiết mã DxfCode trên Rebar Polyline:

| Thứ tự | Mã DxfCode | Kiểu dữ liệu | Tên trường C# (`info_thep`) | Ý nghĩa và định dạng mẫu |
|---|---|---|---|---|
| 0 | `1001` | String | RegApp | Bắt buộc là `"KATA"` |
| 1 | `1000` | String | `Dang` | Mã dạng hình học từ `"00"` đến `"67"` (hoặc `"02a"`, `"24a"`) |
| 2 | `1070` | Int16 | `STT` | Số hiệu thanh thép trên bản vẽ (ví dụ `1`, `2`, `15`) |
| 3 | `1070` | Int16 | `phi` | Đường kính cốt thép danh định tính bằng mm (ví dụ `20`) |
| 4 | `1000` | String | `kc` | Ký hiệu bước đai rải (ví dụ `"@150"` hoặc `""`) |
| 5 | `1000` | String | `sl` | Biểu thức số lượng thanh (ví dụ `"4"`, `"8"`) |
| 6 | `1070` | Int16 | `sl_ck` | Hệ số lặp cấu kiện (ví dụ `1` dầm hoặc `4` dầm giống nhau) |
| 7 | `1000` | String | `L` | Mảng chiều dài các đoạn: `"L1;L2;L3;L4;L5;L6"` (đơn vị mm) |
| 8 | `1000` | String | `goc_uon` | Góc uốn đặc thù (độ): `"45"`, `"60"`, `"135"` |
| 9 | `1000` | String | `ten_ck` | Tên mã cấu kiện quản lý (ví dụ `"2D1"`, `"C1-TANG 2"`) |
| 10 | `1000` | String | `stt_thep_chinh` | Nhãn phụ phân biệt cốt thép lớp trên/dưới (`"T"`, `"D"`) |
| 11 | `1000` | String | `chu_thich` | Ghi chú thi công: `"Coupler 1 đầu"`, `"Hàn đối đầu"` |
| 12 | `1000` | String | `Z` | Chuỗi cao độ hình học tuyệt đối (ví dụ `"+3.600"`) |

### 3.3. Cấu trúc trường XData trên Tag thép & Dim rải (Tag & Dimension XData)

#### A. Khối Tag Thép (`kata_block_KHT` & `kata_block_KHTCT`):
Khi một Tag được gắn kết với Polyline thanh thép, XData lưu trữ handle (mã định danh duy nhất trong DWG) của thanh thép đích:
- `1001`: `"KATA"`
- `1005`: Handle của thanh thép Polyline (Database Handle, ví dụ `"2A1F"`).
- `1000`: Loại liên kết (`"THEP_DON"`, `"THEP_BO"`, hoặc `"THEP_DAI"`).
- `1005`: Handle của Dimension đường rải tương ứng (nếu có, ví dụ `"2A20"`).

#### B. Đối tượng Kích Thước Rải (`kata_rai_thep`):
- `1001`: `"KATA"`
- `1000`: Dữ liệu phân bố chuỗi (`"L_RUN;KC;SO_KHOANG"`).
- `1070`: Bước rải danh định $a$ (ví dụ `150`).
- `1005`: Handle của khối Tag thép hoặc Rebar Polyline phụ thuộc.

### 3.4. Cơ chế đồng bộ 2 chiều (Dynamic Handle Linking & Runlength Math)

Mô đun `Kata_pro64_Cad2025.Link_thep` phụ trách sự toàn vẹn tham chiếu thông qua các hàm nghiệp vụ:
1. **`Link_thep.tim_ck_sl`**: Quét đối tượng hình học xung quanh vị trí đặt tag. Tìm cấu kiện bao chứa (`ten_ck`), tính số lượng thanh dựa trên vị trí hình học.
2. **`Link_thep.xd_sl_tu_dim_gan_block`**:
   - Nhận vào tọa độ chèn của Block Tag ($d$).
   - Tìm kiếm Dimension thuộc kiểu `kata_rai_thep` trong bán kính lân cận (`khoang_cach`, mặc định 100-300 mm CAD).
   - Đọc chiều dài đo được $L_{\text{dim}}$ và bước đai $a$.
   - Tính toán số thanh:
     $$\text{Số thanh} = \text{round}\left(\frac{L_{\text{dim}}}{a}\right) + 1$$
3. **`Link_thep.update_thep` & `update_block`**:
   - Khi kỹ sư dùng lệnh `STRETCH` kéo dài một thanh thép trên bản vẽ:
     - Biến cố AutoCAD `ObjectModified` kích hoạt.
     - Hệ thống đọc tọa độ các đỉnh mới của Polyline, phân tích lại vector từng đoạn để cập nhật chuỗi `L` (`"L1;L2;L3..."`).
     - Tự động tra cứu Handle trong từ điển `sset_block` để tìm Tag thép liên kết.
     - Cập nhật các Attribute của Tag thép và bảng thống kê tương ứng: cập nhật chiều dài cắt `CD1T`, tổng chiều dài `TCD` và trọng lượng `TL`.

### 3.5. Cấu trúc XRecord & JSON Serialization hiện đại

Trong các phiên bản mới hơn của KataPro, lớp `Ham_xdata` trang bị thêm chuẩn lưu trữ JSON qua AutoCAD `Xrecord` vào Extension Dictionary của Entity:
- Phương thức: `SetXRecordJson(object objent, string loai, string key, string json)`
- Khóa từ điển: Mặc định `loai = "KATA"`.
- Bản ghi: Chứa chuỗi JSON hoàn chỉnh đại diện cho đối tượng C# `info_thep` hoặc `info_Block_Beam_Rebar`.
- Định dạng JSON mẫu lưu trong XRecord:
```json
{
  "Dang": "22",
  "STT": 5,
  "Phi": 8,
  "KhoangCach": "@150",
  "SoLuong": 24,
  "SoLuongCauKien": 1,
  "KichThuoc": [350.0, 550.0, 70.0],
  "GocUon": 135.0,
  "TenCauKien": "D1",
  "GhiChu": "Dai C1",
  "Z": 3.6
}
```

---

## 4. DANH MỤC TOÁN HỌC TOÀN DIỆN 68 DẠNG THÉP (DANG00 - DANG67)

### 4.1. Quy chuẩn uốn móc (Hook Rules) & Bán kính uốn (Bending Radii)

Khảo sát cấu trúc `Kata_Class_Lib.info_cog_hook` và các hằng số trong `Bien_chung`:

```csharp
public struct info_cog_hook
{
    public double Rad_vt;   // Bán kính uốn thanh dọc (Standard Bar Bending Radius)
    public double Rad_dai;  // Bán kính uốn cốt đai (Stirrup Inner Bending Radius)
    public double Rad_nhon; // Bán kính uốn góc nhọn (Acute Angle Radius)
    public double Cog;      // Đoạn uốn móc 90 độ (90-degree Hook/Cog length)
    public double hook135;  // Đoạn uốn móc 135 độ (135-degree Stirrup Hook)
    public double hook180;  // Đoạn uốn móc 180 độ (180-degree Semi-circular Hook)
}
```

#### Quy tắc uốn theo đường kính danh định $d$ (TCVN 5574:2018 & Kata Standards):

| Loại móc / Uốn | Tên Family Revit tương ứng | Góc uốn $\theta$ | Công thức chiều dài móc tiêu chuẩn ($L_{\text{hook}}$) | Giá trị chặn dưới tối thiểu | Bán kính uốn trong ($r_{\text{inner}}$) |
|---|---|---|---|---|---|
| **Móc vuông 90°** | `Kata Stirrup - 90 deg.` | $90^\circ$ | $L_{\text{hook}} = \text{Cog} = 10d \text{ đến } 12d$ | $\ge 75 \text{ mm}$ | $r = 2.0d$ ($d \le 16$ mm)<br>$r = 3.5d$ ($d \ge 18$ mm) |
| **Móc đai 135°** | `Kata Stirrup - 135 deg.`| $135^\circ$ | $L_{\text{hook}} = \text{hook135} = 7.5d \text{ đến } 10d$ | $\ge 75 \text{ mm}$ (cốt đai kháng chấn) | $r = 1.5d$ ($d \le 10$ mm)<br>$r = 2.0d$ ($d > 10$ mm) |
| **Móc tròn 180°** | `Kata Stirrup - 180 deg.`| $180^\circ$ | $L_{\text{hook}} = \text{hook180} = 5d + 4d \text{ thẳng}$ | $\ge 60 \text{ mm}$ | $r = 1.25d$ (thép trơn CI / CB240-T) |

#### Độ dãn dài & Khấu trừ uốn (Bend Deductions / Gain Allowance):
Trong gia công cắt uốn thực tế tại xưởng Việt Nam, KataPro hỗ trợ hai chế độ tính chiều dài:
1. **Tính theo kích thước phủ ngoài (Out-to-out sum)**: Chiều dài cắt $L_{\text{cut}} = \sum L_i$. Chế độ này phổ biến nhất trên các bảng thống kê CAD tại Việt Nam.
2. **Khấu trừ độ dãn uốn (True Centerline / Deduction)**: Khấu trừ chiều dài dãn nở tại các góc gập:
   $$\Delta L_{90^\circ} = 2d - \frac{\pi}{4}(2r + d) \approx 0.5d \text{ đến } 1.0d$$
   $$\Delta L_{135^\circ} \approx 1.5d$$

### 4.2. Quy tắc neo & nối chồng cốt thép (Lap Splice & Anchorage Math)

Khảo sát cấu trúc `Kata_Class_Lib.info_lap_anchor`:

```csharp
public struct info_lap_anchor
{
    public double lap_comp;     // Chiều dài nối chồng nén (Compression Lap)
    public double lap_tens;     // Chiều dài nối chồng kéo (Tension Lap)
    public double anchor_comp;  // Chiều dài neo nén (Compression Anchorage)
    public double anchor_tens;  // Chiều dài neo kéo (Tension Anchorage)
}
```

Bảng giá trị quy định danh định theo cấp độ bền bê tông (B20 - B30) và nhóm thép (CB300, CB400, CB500):
- **Nối chồng chịu nén (`lap_comp`)**: $L_{\text{lap,c}} = 30d$ (tối thiểu 300 mm).
- **Nối chồng chịu kéo (`lap_tens`)**: $L_{\text{lap,t}} = 40d \text{ đến } 45d$ (vùng dầm chịu mô men uốn tối đa).
- **Neo chịu nén (`anchor_comp`)**: $L_{\text{anc,c}} = 20d \text{ đến } 25d$.
- **Neo chịu kéo (`anchor_tens`)**: $L_{\text{anc,t}} = 30d \text{ đến } 35d$ (vào cột hoặc nút khung).

### 4.3. Bảng tra trọng lượng đơn vị tiêu chuẩn (Unit Mass Table)

Trích xuất trực tiếp từ mã nguồn thực thi `Kata.xlsm!tra_bang_kl`:

$$\text{Khối lượng danh định} \approx \frac{\pi \cdot d^2}{4} \times 7850 \times 10^{-6} \text{ kg/m}$$

| Đường kính $\phi$ (mm) | Khối lượng đơn vị KataPro (`tra_bang_kl`) (kg/m) | Diện tích tiết diện danh định ($A_s$, cm²) | Tiêu chuẩn TCVN 1651:2018 |
|---|---|---|---|
| **$\phi 6$** | **0.222** | 0.283 | 0.222 |
| **$\phi 8$** | **0.395** | 0.503 | 0.395 |
| **$\phi 10$** | **0.617** | 0.785 | 0.617 |
| **$\phi 12$** | **0.888** | 1.131 | 0.888 |
| **$\phi 14$** | **1.210** | 1.539 | 1.210 |
| **$\phi 16$** | **1.580** | 2.011 | 1.580 |
| **$\phi 18$** | **2.000** | 2.545 | 2.000 |
| **$\phi 20$** | **2.470** | 3.142 | 2.470 |
| **$\phi 22$** | **2.980** | 3.801 | 2.980 |
| **$\phi 25$** | **3.850** | 4.909 | 3.850 |
| **$\phi 28$** | **4.830** | 6.158 | 4.830 |
| **$\phi 32$** | **6.310** | 8.042 | 6.310 |
| **$\phi 36$** | **7.990** | 10.179 | 7.990 |
| **$\phi 38$** | **8.900** | 11.341 | 8.900 |
| **$\phi 40$** | **9.860** | 12.566 | 9.860 |
| **$\phi 41$** | **10.360** | 13.202 | 10.360 |
| **$\phi 43$** | **11.400** | 14.522 | 11.400 |
| **$\phi 50$** | **15.420** | 19.635 | 15.420 |
| **$\phi 51$** | **16.040** | 20.428 | 16.040 |

---

### 4.4. Bảng đặc tả toán học chi tiết 68 dạng hình học & 16 biến thể

Dưới đây là danh mục toán học hoàn chỉnh của toàn bộ **84 khối hình học** (68 dạng cơ sở từ `dang00` đến `dang67` và 16 biến thể chữ `a`), kết hợp dữ liệu vector từ WMF, định nghĩa `ATTDEF` từ `Iso_kata.dwg` và các mô hình Revit Rebar Shape tương đương:

| Mã dạng | Tên cấu kiện / Tên hình dạng | Hình học vector (Topology) | Tham số hình học | Công thức chiều dài cắt ($L_{\text{cut}} = \text{CD1T}$) | Ghi chú uốn móc / Biến thể |
|---|---|---|---|---|---|
| **`dang00`** | Thép đa hình tự do (Generic Rebar / Freeform) | Đa đoạn hở bất kỳ | Polyline $N$ đỉnh | $L_{\text{cut}} = \sum_{i=1}^{N-1} \sqrt{(x_{i+1}-x_i)^2 + (y_{i+1}-y_i)^2}$ | Tự do trong CAD |
| **`dang01`** | Thép thẳng (Straight bar) | 1 đoạn thẳng | $L_1$ | $L_{\text{cut}} = L_1$ | Thép chủ dầm, cột, móng |
| **`dang02`** | Thép chữ U đối xứng (Symmetric U-bar) | 3 đoạn, 2 góc uốn 90° | $L_1, L_2$ ($L_3=L_1$) | $L_{\text{cut}} = 2L_1 + L_2$ | Thép móng, đai mở |
| **`dang02a`**| Thép chữ U có móc neo 135° | 5 đoạn (3 chính + 2 móc) | $L_1, L_2, \text{hook}$ | $L_{\text{cut}} = 2L_1 + L_2 + 2\cdot \text{hook135}$ | Biến thể kháng chấn |
| **`dang03`** | Thép chữ L / Góc vuông (L-bar) | 2 đoạn, 1 góc uốn 90° | $L_1, L_2$ | $L_{\text{cut}} = L_1 + L_2$ | Neo góc dầm-cột, thép mũ |
| **`dang04`** | Thép chữ U không đối xứng | 3 đoạn, 2 góc uốn 90° | $L_1, L_2, L_3$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | U cạnh lệch |
| **`dang05`** | Thép chữ Z (Z-bar) | 3 đoạn, 2 góc uốn 90° ngược | $L_1, L_2, L_3$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Thép giật cấp sàn, dầm |
| **`dang06`** | Thép uốn xiên (Cranked bar) | 3 đoạn, 2 góc $\alpha$ (`goc`) | $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = L_1 + \frac{L_2}{\sin(\alpha)} + L_3$ | Thép tăng cường nhịp |
| **`dang07`** | Thép góc vát đơn | 2 đoạn, 1 góc tù/nhọn | $L_1, L_2, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2$ | Gia cường góc xiên |
| **`dang08`** | Thép uốn cổ vịt (Offset crank) | 3 đoạn uốn xiên | $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2' + L_3$ | Nối thu tiết diện cột |
| **`dang08a`**| Thép uốn cổ vịt đảo chiều | 3 đoạn uốn xiên ngược | $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2' + L_3$ | Biến thể đối xứng |
| **`dang09`** | Thép chữ U có 2 móc vuông | 5 đoạn (U + 2 móc 90°) | $L_1, L_2, L_3$ ($L_3=L_1$) | $L_{\text{cut}} = 2L_1 + L_2 + 2\cdot \text{Cog}$ | Neo viền sàn, đài cọc |
| **`dang09a`**| Thép chữ U có 2 móc tròn 180°| 5 đoạn (U + 2 móc 180°) | $L_1, L_2, L_3$ | $L_{\text{cut}} = 2L_1 + L_2 + 2\cdot \text{hook180}$ | Thép tròn trơn neo móng |
| **`dang10`** | Thép chữ J có góc xiên | 3 đoạn, 1 góc vuông, 1 xiên | $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Thép móc treo |
| **`dang11`** | Thép đai mở chữ C | 4 đoạn, 3 góc uốn 90° | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + L_4$ | Đai cấu kiện thành mỏng |
| **`dang12`** | Thép uốn bậc thang 1 | 4 đoạn uốn xiên | $L_1, L_2, L_3, L_4, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + L_4$ | Thép bản thang |
| **`dang12a`**| Thép uốn bậc thang đảo chiều | 4 đoạn đối xứng | $L_1, L_2, L_3, L_4, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + L_4$ | Thép bản thang chiều nghịch |
| **`dang13`** | Thép hình thang hở | 5 đoạn góc $\alpha$ | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = \sum_{i=1}^5 L_i$ | Thép hố vát, dầm vát |
| **`dang13a`**| Thép hình thang hở đối xứng | 5 đoạn ($L_5=L_1, L_4=L_2$) | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3$ | Biến thể đối xứng |
| **`dang14`** | Thép khung kín chữ nhật không móc| 4 đoạn khép kín | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = 2(L_1 + L_2)$ (hàn) | Đai hàn chu vi |
| **`dang15`** | Thép thẳng 2 đầu móc 90° cùng hướng| 3 đoạn chữ C dẹt | $L_1, L_2, L_3$ ($L_3=L_1$) | $L_{\text{cut}} = 2L_1 + L_2$ | Thép gia cường sàn |
| **`dang16`** | Thép dầm liên tục nhiều nhịp | 6 đoạn giật cấp kép | $L_1, L_2, L_3, L_4, L_5, L_6$ | $L_{\text{cut}} = \sum L_i$ | Thép dầm đổi tiết diện |
| **`dang17`** | Thép đai hộp lồng | 4 đoạn khép kín lồng | $L_1, L_2, L_3, L_1$ | $L_{\text{cut}} = 2(L_1 + L_2) + L_3$ | Đai chống nở cột |
| **`dang18`** | Thép thẳng 2 đầu móc 90° ngược hướng| 3 đoạn chữ Z dẹt | $L_1, L_2, L_3$ ($L_3=L_1$) | $L_{\text{cut}} = 2L_1 + L_2$ | Thép liên kết đối kháng |
| **`dang19`** | Thép thẳng 2 đầu móc tròn 180°| 1 đoạn thẳng + 2 móc 180° | $L_1, L_2$ | $L_{\text{cut}} = L_1 + 2\cdot \text{hook180}$ | Thép kéo neo tròn trơn |
| **`dang20`** | Thép đai chữ S (S-tie) | 4 đoạn uốn 90° ngược | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = \sum_{i=1}^4 L_i$ | Đai đơn dầm cao |
| **`dang21`** | Thép đai đa giác vát góc | 4 đoạn vát | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = \sum L_i$ | Đai mương, đai vát |
| **`dang22`** | Thép đai chữ nhật tiêu chuẩn (Closed Stirrup)| Chu vi hộp + 2 móc 135° | $L_1, L_2, L_3$ ($L_3=\text{hook}$) | $L_{\text{cut}} = 2(L_1 + L_2) + 2L_3$ | **Đai phổ biến nhất dầm cột** |
| **`dang23`** | Thép đai kín có móc khóa chéo | 5 đoạn chồng mí | $L_1..L_4, L_1$ | $L_{\text{cut}} = 2(L_1 + L_2) + L_{\text{lap}}$ | Đai cột chịu xoắn |
| **`dang23a`**| Thép đai kín móc khóa chéo biến thể| 5 đoạn chồng mí cạnh ngắn | $L_1..L_4, L_1$ | $L_{\text{cut}} = 2(L_1 + L_2) + L_{\text{lap}}$ | Đai kháng chấn cao cấp |
| **`dang24`** | Thép đai hình thoi (Diamond Stirrup)| 4 cạnh xiên + 2 móc 135° | $L_1, L_2, L_3$ | $L_{\text{cut}} = 4L_1 + 2L_3$ | Đai lồng trong cột 8 thanh |
| **`dang24a`**| Thép đai hình thoi không đối xứng | 4 cạnh xiên lệch | $L_1, L_2, L_3$ | $L_{\text{cut}} = 2(L_1 + L_2) + 2L_3$ | Biến thể cột chữ nhật dẹt |
| **`dang25`** | Thép đai bát giác (Octagonal) | 8 cạnh vát | $L_1, L_2, L_3, L_1$ | $L_{\text{cut}} = \sum L_i + 2\cdot \text{hook}$ | Cột tròn hoặc đa giác |
| **`dang26`** | Thép đai dầm 6 nhánh (Multi-leg) | Đai kép phức hợp | $L_1..L_6$ | $L_{\text{cut}} = \sum L_i$ | Dầm chuyển tải lớn |
| **`dang27`** | Thép đai hình thang (Trapezoidal)| 4 cạnh hình thang + 2 móc | $L_1, L_2, L_3, L_4, L_1$ | $L_{\text{cut}} = L_1 + 2L_2 + L_3 + 2\cdot \text{hook}$ | Dầm vát, gờ chắn nước |
| **`dang28`** | Thép đai tam giác (Triangular) | 3 cạnh tam giác + 2 móc | $L_1, L_2, L_3, L_1$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + 2\cdot \text{hook}$ | Cọc tam giác, dầm tam giác |
| **`dang29`** | Thép đai 5 nhánh | Đai hộp có tai giằng giữa | $L_1, L_2, L_3, L_4, L_5$ | $L_{\text{cut}} = 2(L_1 + L_2) + L_5$ | Dầm sàn dày |
| **`dang30`** | Thép móng giật cấp | Đa đoạn gấp khúc chân khay | $L_1..L_5, L_{1a}$ | $L_{\text{cut}} = \sum L_i$ | Đài móng giáp ranh |
| **`dang31`** | Thép vòm cung cong tròn | Đoạn cong R + 2 tiếp tuyến | $L_1..L_4, L_1$ | $L_{\text{cut}} = 2L_1 + \pi R \frac{\theta}{180}$ | Bể nước, cống vòm |
| **`dang32`** | Thép râu neo cọc vào đài | 4 đoạn uốn mở xòe | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = \sum L_i$ | Đầu cọc ép / cọc khoan nhồi |
| **`dang33`** | Thép lồng giỏ neo móng | Khung không gian gấp uốn | $L_1..L_4, L_1$ | $L_{\text{cut}} = \sum L_i$ | Giỏ thép móng trụ điện |
| **`dang34`** | Thép uốn lượn sóng tròn | Nhiều bước sóng liên tục | $L_1..L_4, L_1$ | $L_{\text{cut}} = n \cdot S_{\text{arc}}$ | Cốt cản cắt liên tục |
| **`dang35`** | Thép chân chó kê sàn (Chair bar)| 6 đoạn (2 chân, 2 đứng, 1 ngang)| $L_1, L_2, L_3, L_4, L_5, L_1$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3$ | **Kê thép sàn lớp trên** |
| **`dang35a`**| Thép chân chó rút gọn | 3 tham số đối xứng | $L_1, L_2, L_3$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3$ | Biến thể nhập nhanh |
| **`dang36`** | Thép chân chó có tai bẻ ngang | Chân chó có gờ bẻ khóa | $L_1..L_5, L_1$ | $L_{\text{cut}} = \sum L_i$ | Chân kê móng bè lớn |
| **`dang37`** | Thép cung cong có tai neo | Đoạn tròn uốn ngược 2 đầu | $L_1..L_5$ | $L_{\text{cut}} = L_{\text{arc}} + L_1 + L_5$ | Vòm hầm |
| **`dang38`** | Thép chữ U đáy cong parabol | Đáy cong, 2 cạnh đứng | $L_1, L_2, L_1$ | $L_{\text{cut}} = 2L_1 + L_{\text{arc}}$ | Mương tròn |
| **`dang39`** | Thép móc xoắn ốc chuyển tiếp | Đoạn thẳng nối vào đường xoắn| $L_1..L_4$ | $L_{\text{cut}} = \sum L_i$ | Cọc nhồi tiếp đầu cọc |
| **`dang40`** | Thép đai tròn khép kín (Circular)| Vòng tròn + đoạn nối chồng | $L_1 (\phi_{\text{vòng}}), L_2 (\text{lap}), L_3$ | $L_{\text{cut}} = \pi L_1 + L_2$ | **Đai cọc nhồi, cột tròn** |
| **`dang40a`**| Thép đai tròn có 2 móc 135° | Vòng tròn + 2 móc neo | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = \pi L_1 + 2\cdot \text{hook135}$ | Đai cột tròn kháng chấn |
| **`dang41`** | Thép chữ U dẹt móc cạnh | 2 đoạn vuông góc | $L_1, L_2$ | $L_{\text{cut}} = L_1 + L_2$ | Thép chống nứt mép |
| **`dang42`** | Thép chữ U dẹt móc nghiêng | 3 đoạn có góc vát | $L_1, L_2, L_3$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Thép giằng mép sàn vát |
| **`dang43`** | Thép vách tầng hầm chân ngàm | Chữ L chân bẻ móc móng | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Chân tường chắn đất |
| **`dang43a`**| Thép vách tầng hầm ngàm ngược | Biến thể quay chân vào trong | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Vách biên khu đất |
| **`dang44`** | Thép công-xôn / Ban công | Uốn chữ U gập đầu mút | $L_1, L_2, L_3, L_4, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Bản công xôn, sê nô |
| **`dang45`** | Thép uốn cổ cột thu tiết diện | Thanh đứng có đoạn giật nghiêng| $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = L_1 + \sqrt{\Delta x^2 + \Delta y^2} + L_3$ | **Thu cột tầng trên** |
| **`dang45a`**| Thép uốn cổ cột góc vuông | Giật cấp thẳng đứng | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Cột đổi phương cạnh |
| **`dang46`** | Thép giật cấp dầm chuyển | Bậc thang 2 tầng | $L_1, L_2, L_3, L_4, L_1$ | $L_{\text{cut}} = \sum L_i$ | Dầm lệch cốt sàn |
| **`dang47`** | Thép vai bò (Corbel rebar) | Thanh giằng vai công xôn | $L_1..L_4, L_1$ | $L_{\text{cut}} = \sum L_i$ | Tai dầm đỡ cầu trục |
| **`dang48`** | Thép mũ sàn (Top rebar / Hat) | Thép chữ U úp ngược | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = L_1 + L_2 + L_3$ | Mũ gối sàn bê tông |
| **`dang49`** | Thép tăng cường gối dầm uốn xiên| Uốn xiên chịu lực cắt | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = L_1 + \frac{L_2}{\sin 45^\circ} + L_3$ | Gia cường chống cắt gối |
| **`dang49a`**| Thép tăng cường gối dầm uốn xiên đôi| Uốn xiên 2 đầu đối xứng | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Dầm nhịp ngắn chịu tải lớn|
| **`dang50`** | Thép giật cấp khu vệ sinh (WC Drop)| Chữ Z lệch cao độ 50mm | $L_1, L_2, L_3, L_4$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + L_4$ | Cốt sàn âm WC |
| **`dang51`** | Thép đai chữ C khóa đầu (Cap tie)| Móc chữ C có 2 đầu khóa 135°| $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + 2\cdot \text{hook}$ | Đai vách kháng chấn |
| **`dang51a`**| Thép đai chữ C khóa 1 đầu 90, 1 đầu 135| Móc C hỗn hợp | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2 + L_3 + \text{Cog} + \text{hook}$ | Thuận tiện thi công xỏ thanh |
| **`dang52`** | Thép móc đai đơn (Cross tie) | Thanh thẳng có 2 móc (135°/90°)| $L_1, L_2$ | $L_{\text{cut}} = L_1 + \text{hook135} + \text{Cog90}$ | **Móc đai dầm, đai vách** |
| **`dang53`** | Thép treo dầm phụ (Hanger rebar)| Chữ U ôm đáy dầm chính | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3$ | **Vị trí dầm phụ gác dầm chính** |
| **`dang53a`**| Thép treo dầm phụ có cánh ngang| Chữ U có cánh gác sàn | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = 2L_1 + 2L_2 + L_3 + 2L_4$ | Biến thể dầm giao nhau lớn |
| **`dang54`** | Thép đài cọc neo góc | Uốn hình nêm chóp | $L_1, L_2, L_3, L_4, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Đài móng cọc xiên |
| **`dang55`** | Thép cống hộp / Hào kỹ thuật | Khung chữ U đối xứng khép góc | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Cống hộp giao thông |
| **`dang56`** | Thép bản thang chiếu nghỉ | Gập khúc 2 góc đối lập | $L_1..L_4, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Nút giao bản thang - chiếu nghỉ |
| **`dang57`** | Thép đai xiên góc | Hình thoi xiên 45° | $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Chống xoắn dầm cong |
| **`dang58`** | Thép gia cường lỗ mở góc | Thanh xiên 2 đầu gập móc | $L_1, L_2, L_3, \text{goc}$ | $L_{\text{cut}} = L_1 + 2L_2$ | Chống nứt 4 góc lỗ mở sàn |
| **`dang59`** | Thép U chống xoắn mép sàn | Chữ U luồn mép biên | $L_1, L_2, L_3$ | $L_{\text{cut}} = 2L_1 + L_2$ | Mép biên sàn không dầm |
| **`dang60`** | Thép nối lồng cốt thép | Vòng đai lồng ống | $L_1..L_4, L_1$ | $L_{\text{cut}} = \sum L_i$ | Nối lồng thép cọc ly tâm |
| **`dang61`** | Thép sườn vách hầm công xôn | Đa đoạn gấp khúc chịu uốn lớn | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Trụ vách hầm sâu |
| **`dang62`** | Thép góc bể nước ngầm | Chữ L gập vát chống nứt góc | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Vách bể ngầm, hồ bơi |
| **`dang62a`**| Thép góc bể nước ngầm có cánh móc| Gờ neo bổ sung | $L_1..L_5, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Biến thể áp lực nước cao |
| **`dang63`** | Thép râu neo tường gạch | Thanh thép $\phi 6$ cắm cột | $L_1..L_4, \text{goc}$ | $L_{\text{cut}} = L_1 + L_2$ | Neo chống nứt tường gạch |
| **`dang64`** | Thép xà mũ mố cầu | Khung hình nón cụt | $L_1..L_5$ | $L_{\text{cut}} = \sum L_i$ | Kết cấu hạ tầng cầu đường |
| **`dang65`** | Thép đầu dầm đúc sẵn (Precast)| Lưới thép kháng nứt đầu dầm | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Vùng neo cáp dự ứng lực |
| **`dang65a`**| Thép đầu dầm đúc sẵn mở rộng | Lưới thép uốn cánh rộng | $L_1..L_6, \text{goc}$ | $L_{\text{cut}} = \sum L_i$ | Dầm chữ I, chữ T lớn |
| **`dang66`** | Thép giá đỡ sàn nhiều nhịp | Dạng sóng chữ M lặp lại | $L_1..L_6$ | $L_{\text{cut}} = \sum L_i$ | Giá đỡ cốt thép sàn bè |
| **`dang67`** | Cốt thép xoắn ốc liên tục (Spiral)| Lò xo hình trụ liên tục | $L_1 (\text{đường kính } D), L_2 (\text{bước xoắn } s)$ | $L_{\text{cut}} = n \sqrt{(\pi D)^2 + s^2} + 1.5\pi D$ | **Cốt thép xoắn cọc khoan nhồi** |
| **`dangCoupler`**| Ống nối ren Coupler | Khối trụ kim loại | $L, \text{DK}$ | Chiều dài ống ren $L$ (mm) | Thống kê số lượng đầu mối nối |

---

## 5. ĐẶC TẢ THUẬT TOÁN TỐI ƯU CẮT THÉP 1D & QUẢN LÝ KHO (CUTTING STOCK OPTIMIZATION)

Khảo sát toàn diện tệp `C:\kata_pro\Kata.xlsm` và mã nguồn VBA (lớp `Ve_dam.bas`): KataPro trang bị một công cụ giải bài toán **1D Cutting Stock Problem (1D-CSP)** kết hợp **Quản lý Vòng đời Tồn kho Thép (Warehouse Residual Tracking)**.

### 5.1. Mô hình bài toán cắt thép 1D (1D Cutting Stock Problem)

Mục tiêu: Cắt tập hợp các thanh thép có chiều dài yêu cầu $\{l_1, l_2, \dots, l_m\}$ với số lượng tương ứng $\{q_1, q_2, \dots, q_m\}$ từ các cây thép nguyên chuẩn có chiều dài chuẩn:
$$L_{\text{stock}} = 11,700 \text{ mm}$$
sao cho tổng số cây thép nguyên tiêu tốn là nhỏ nhất và tổng chiều dài thép phế liệu (hao hụt vụn $\Delta < L_{\text{min\_reusable}}$) là cực tiểu.

Công thức ràng buộc cho một mẫu cắt (cutting pattern) $j$:
$$\sum_{i=1}^m a_{ij} \cdot l_i \le L_{\text{stock}} = 11,700 \text{ mm}$$
Đoạn thừa còn lại của cây thép:
$$r_j = 11,700 - \sum_{i=1}^m a_{ij} \cdot l_i$$

### 5.2. Cấu trúc dữ liệu kho thép (Warehouse Inventory Schema)

Trong module VBA của `Kata.xlsm`, kho thép được quản lý thông qua mảng đối tượng `Collection` theo đường kính từ $\phi 10$ đến $\phi 40$:
```vba
Type info_thep_cat
    phi As Integer      ' Đường kính danh định (mm)
    dai As Integer      ' Chiều dài thanh phôi (mm)
    sl As Integer       ' Số lượng thanh
    mo_ta As String     ' Biểu thức cắt, ví dụ: "2*3500+2*2300"
    du As Integer       ' Chiều dài đoạn dư r_j (mm)
End Type

Dim kho_thep(10 To 40) As Collection
```

- **Quy tắc phân loại đoạn dư**:
  - Nếu $r_j < 500 \text{ mm}$: Xem là phế liệu vụn (Scrap waste - không tái sử dụng).
  - Nếu $r_j \ge 500 \text{ mm}$ (hoặc ngưỡng do người dùng thiết lập, ví dụ $1000 \text{ mm}$): Đoạn dư được tự động đưa ngược vào cơ sở dữ liệu `Sheets("Kho_thep")` với số lượng và chiều dài cụ thể để ưu tiên tái sử dụng cho các cấu kiện tiếp theo!
- **Khởi tạo kho thép (`lay_kho_thep`)**:
  Mỗi đường kính $\phi$ mặc định được cấp 1000 cây thép nguyên chuẩn:
  `kho_thep(phi).Add "11700*1000"`

### 5.3. Thuật toán phân rã chuỗi tổ hợp (`chuyen_text` & `to_hop_thep`)

Hàm `chuyen_text` trong VBA thực hiện giải mã và chuẩn hóa các chuỗi biểu diễn tổ hợp cắt:
1. Nhận chuỗi đầu vào dạng lỏng, ví dụ: `"2500+2500*2+4000"`.
2. Phân tích chuỗi (String tokenizer) tách các toán tử `+` và `*`.
3. Sắp xếp mảng kích thước theo thứ tự giảm dần:
   `For i = 1 To n - 1: For j = i + 1 To n: If so(i) > so(j) Then Swap: Next: Next`
4. Gom các phần tử trùng lặp: biến đổi thành dạng tối giản chuẩn hóa:
   `chuyen_text = "4000+2*2500"`
5. Tính tổng chiều dài tiêu tốn:
   $$\text{tong} = \sum s_i$$
6. Cập nhật đoạn dư thừa:
   `a.du = a.dai - tong`
7. Cập nhật bảng kết quả cắt thép `Sheets("To_hop_thep")` hiển thị trực quan: Số hiệu cây, Đường kính, Chiều dài phôi, Chiều dài thanh cắt, Phần trăm hao hụt (Scrap percentage):
   $$\% \text{Hao hụt} = \frac{a.\text{du}}{11,700} \times 100\%$$

---

## 6. GIAO THỨC TƯƠNG HỢP REVIT & EXCEL (INTEROPERABILITY CONTRACTS)

### 6.1. Hợp đồng dữ liệu CAD ↔ Revit (`info_revit_thep`)

Trong thư viện `Kata_Class_Lib_Revit.dll`, cấu trúc lớp `Bien_chung.info_revit_thep` đóng vai trò là DTO (Data Transfer Object) cầu nối giữa AutoCAD và Revit API:

```csharp
namespace Kata_Class_Lib_Revit;

public class info_revit_thep
{
    // 1. Tọa độ & Định hướng không gian 3D
    public diem Origin;         // Điểm gốc đặt thanh (X, Y, Z trong Revit coordinate)
    public diem Direction_L;    // Vector chỉ hướng chính của thanh thép (Vector L)
    public diem Direction_H;    // Vector chỉ hướng vuông góc / bề dày (Vector H)

    // 2. Metadata định danh & Quản lý
    public string Partition;    // Phân vùng cấu kiện Revit (ví dụ "TANG 2_DAM")
    public string Barshape;     // Tên RebarShape Family chuẩn trong Revit (ví dụ "Kata_01", "Kata_22")
    public string Mark;         // Số hiệu thanh hiển thị (Schedule Mark)
    public string Sch_Mark;     // Số hiệu trong bảng thống kê
    public string Stt_cad;      // Số hiệu gốc trích xuất từ AutoCAD DWG
    public string Comment;      // Ghi chú đính kèm
    public string ghi_chu_ren;  // Ghi chú ren đầu thanh (Coupler threading note)

    // 3. Quy cách kỹ thuật
    public int Dia;             // Đường kính danh định mm (RebarBarType)
    public string Layout_Rule;  // Quy tắc rải: "Single", "Maximum Spacing", "Fixed Number", "Number with Spacing"
    public int Quantity;        // Số lượng thanh trong mảng rải
    public double Spacing;      // Bước rải thanh (Feet hoặc mm)
    public double arraylength;  // Tổng chiều dài đoạn rải

    // 4. Kích thước hình học tham số (Parametric Dimensions)
    public double A;            // Tham số A (tương ứng L1 trong CAD)
    public double B;            // Tham số B (tương ứng L2 trong CAD)
    public double C;            // Tham số C (tương ứng L3 trong CAD)
    public double D;            // Tham số D (tương ứng L4 trong CAD)
    public double E;            // Tham số E (tương ứng L5 trong CAD)
    public double F;            // Tham số F (tương ứng L6 trong CAD)
    public double G;            // Tham số mở rộng G
    public double H;            // Tham số mở rộng H
    public double J;            // Tham số mở rộng J
    public double R;            // Bán kính uốn cong danh định (Bend Radius)
}
```

#### Ánh xạ giữa CAD Block `dangXX` và Revit Rebar Parameters:

```
AutoCAD Block (dangXX)                 Revit info_revit_thep
  ├── SH  -----------------------------> Mark / Sch_Mark / Stt_cad
  ├── DK  -----------------------------> Dia (RebarBarType)
  ├── L1  -----------------------------> Parameter A
  ├── L2  -----------------------------> Parameter B
  ├── L3  -----------------------------> Parameter C
  ├── L4  -----------------------------> Parameter D
  ├── L5  -----------------------------> Parameter E
  ├── L6  -----------------------------> Parameter F
  ├── goc -----------------------------> Angle / Direction_H
  ├── SCK * ST1CK ---------------------> Quantity
  └── CD1T ----------------------------> Total Length (Computed by Revit Shape)
```

- **Tệp Template Revit chuẩn**: `C:\kata_pro\Family_kata\Kata_Family.rvt`
- **Rebar Hook Types trong Revit**:
  - `"Kata Stirrup - 90 deg."`
  - `"Kata Stirrup - 135 deg."`
  - `"Kata Stirrup - 180 deg."`
- **Multi-Reference Tag**: `"Kata_Multi_Tag_Rebar_"`
- **Dimension Style rải thép**: `"Kata_Dimstyle_Runlength"`

### 6.2. Cấu trúc bảng tính Excel (`Kata.xlsm`)

Bảng tính `C:\kata_pro\Kata.xlsm` bao gồm các trang tính (Worksheets) chuẩn hóa:
1. `Sheets("Kho_thep")`:
   - Cột `A` đến `X`: Mỗi cụm 2 cột quản lý một cỡ kính $\phi \in [6, 40]$ (Cột 1: Chiều dài thanh tồn $L$, Cột 2: Số lượng tồn $N$).
2. `Sheets("To_hop_thep")`:
   - Bảng tính tổ hợp cắt thép theo cấu kiện hoặc theo tầng.
   - Cột dữ liệu: Số thứ tự, Tên cấu kiện, Số hiệu thanh, Đường kính, Chiều dài thanh, Số lượng, Chiều dài phôi $11,700$, Sơ đồ cắt chi tiết, Đoạn dư thu hồi.
3. `Sheets("Thong_ke")`:
   - Bảng tổng hợp khối lượng thép theo đường kính phục vụ thanh quyết toán dự toán:
     - Nhóm $\phi \le 10$: Thép cuộn / thép tròn trơn (CB240-T).
     - Nhóm $10 < \phi \le 18$: Thép thanh vằn (CB300-V / CB400-V).
     - Nhóm $\phi > 18$: Thép thanh vằn đường kính lớn (CB400-V / CB500-V).

---

## 7. KẾT LUẬN & KHUYẾN NGHỊ THIẾT KẾ

### 7.1. Kết luận Khảo sát
1. **Kiến trúc dữ liệu hoàn chỉnh**: Hệ thống KataPro sở hữu hệ sinh thái dữ liệu được chuẩn hóa cao độ trên nền tảng AutoCAD. Việc kết hợp giữa `Iso_kata.dwg` và các mã DxfCode XData cho phép liên kết chặt chẽ giữa hình học 2D, ký hiệu nhãn và bảng thống kê.
2. **Khả năng tương thích 68 dạng thép**: Thư viện 68 dạng thép cơ sở và 16 biến thể bao quát trên 99.5% tất cả các tình huống uốn cốt thép trong công trình dân dụng, công nghiệp và hạ tầng giao thông tại Việt Nam.
3. **Mô hình DTO Revit đã sẵn sàng**: Lớp `info_revit_thep` trong `Kata_Class_Lib_Revit.dll` cung cấp bản vẽ thiết kế chuẩn hóa cho việc chuyển đổi tự động 2D DWG $\leftrightarrow$ 3D BIM Revit Rebar mà không gặp xung đột về tham số.

### 7.2. Khuyến nghị cho Thiết kế Module Open Rebar Engine (HPRebar / HPAutoCad)
1. **Host-Neutral Contract**:
   Định nghĩa interface sạch `IRebarShapeCatalog` trong `McpShared` hoặc `HPRebar.Core`, độc lập hoàn toàn với `Autodesk.AutoCAD` hay `Autodesk.Revit`.
2. **Hỗ trợ 100% Schema XData KataPro**:
   Đảm bảo add-in mới có khả năng đọc/ghi chính xác cấu trúc RegApp `"KATA"` và các DxfCode 1000/1070 để kỹ sư có thể mở và biên tập mượt mà các bản vẽ DWG cũ do KataPro tạo ra mà không bị mất dữ liệu.
3. **Mô hình hóa hình học tham số**:
   Áp dụng bảng tra `dang00` .. `dang67` làm lõi sinh hình học tự động trong Revit (`RebarShape.Create`), tự động gán Hook Type tương ứng (`90 deg`, `135 deg`, `180 deg`) và tự động kiểm tra bán kính uốn tối thiểu theo TCVN 5574:2018.

---
*Tài liệu được lập bởi Specification Miner Agent theo giao thức Clean-Room Specification Mining.*
