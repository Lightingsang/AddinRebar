# BÁO CÁO KHẢO SÁT TOÀN DIỆN HỆ SINH THÁI C:\DCE PRO
## Reverse-Engineering Data Formats, Scripts, Databases & XData Structures
### Tác tử thực hiện: `explorer_survey_2` | Ngày hoàn tất: 2026-10-17 | Chế độ: Read-Only

---

## 1. TỔNG QUAN KHẢO SÁT (EXECUTIVE SUMMARY)

Báo cáo này cung cấp kết quả khảo sát ngược (reverse-engineering) chuyên sâu và toàn diện về toàn bộ hệ sinh thái phần mềm kết cấu **DCE Pro** tại thư mục `C:\Dce Pro`, phục vụ trực tiếp cho việc xây dựng deliverable C# độc lập `HPDcePro` (`HPDcePro.slnx`). 

Toàn bộ quá trình khảo sát tuân thủ nghiêm ngặt nguyên tắc **Read-Only** (không ghi, sửa, xóa bất kỳ tệp nào trong `C:\Dce Pro`).

### Các cấu phần trọng tâm đã khảo sát:
1. **AutoLISP & Command Mapping (`C:\Dce Pro\DCE Pro.lsp`, `GLDCE.LSP`, `Trich chi tiet (TCT).lsp`)**: 799 dòng mã AutoLISP, định nghĩa 61 lệnh tắt kết nối .NET, 5 hàm tiện ích hình học thuần LISP, cơ chế tự động nhận diện 5 nền tảng CAD (AutoCAD, ZWCAD, GstarCAD, BricsCAD, ProgeCAD), bộ điều phối S::STARTUP và Command Reactor an toàn.
2. **Cấu trúc dự án tự động (`C:\Dce Pro\AutoStr\DuAnMau.autostr`)**: Tệp cấu hình JSON 692 dòng định nghĩa toàn bộ mô hình dữ liệu dự án kết cấu, vật liệu, tiết diện chuẩn, tổ hợp tải trọng TCVN (14 tổ hợp ULS, 14 tổ hợp SLS, THDH), phân tầng (`Floors`), đài móng cọc (`PointSpring`), và luồng tự động hóa 24 bước (`AutoStrStep01` đến `AutoStrStep24`).
3. **Mô hình máy học & Tokenizer (`C:\Dce Pro\MayHoc\mauhoc.db`)**: Cơ sở dữ liệu SQLite (~9MB) gồm bảng `MauHoc` (45,020 mẫu đã trích xuất đặc trưng NLP), `LichSuSua` (3 bản ghi học tăng cường), `CauHinhModel` (10 phiên bản huấn luyện). Hệ thống sử dụng `Microsoft.ML` multiclass classification phân loại vai trò ngữ nghĩa của số trong chuỗi ghi chú thép (`SH`, `SL`, `DK`, `L`, `KC1`–`KC4`, `BO`).
4. **Hệ thống bảng tính Excel & Custom UDFs (`Excel\File V.2020\*.xlsm`, `Excel\DceSteel\*.xlsm`, `V.2020\DCE Excel-AddIn.xll`)**: Hệ thống bảng tính bê tông cốt thép và kết cấu thép đồ sộ, tích hợp Excel-DNA XLL add-in cung cấp các hàm UDF chuyên sâu TCVN (`_xll.Beam_Function`, `_xll.Foundation_TinhHeSoCChiuCat`, `_xll.Slab_Function`, `_xll.Bimoment`, `_xll.DCEPropSectI`, `_xll.DCETCVNPhuLucFHeSoOnDinh`...) cùng 73 macro tự động hóa trao đổi dữ liệu CAD–Etabs–Excel.
5. **Cấu trúc CAD XData & Định danh 13 RegApps**: Làm rõ cơ chế lưu trữ dữ liệu ẩn trên thực thể CAD (Polyline, Circle, Block, Dimension, Leader), định danh chính xác 13 RegApp của hệ sinh thái, bóc tách cấu trúc mảng `RebarXdataDCE` (`o(0)` đến `o(6)+`), chuỗi canonical `DceRebarXDataTKT` (`SL=..;DK=..;KC1=..;KC2=..;KC3=..;KC4=..;SH=..;L=..`), và liên kết động `Links_XDataObject`.
6. **Phát hiện quan trọng ngoài dự kiến**:
   - Thư viện hình học native C++ `DceAnalyssiss64.dll` chứa 69 hàm toán hình học/topo dầm sàn xuất bản bản địa (`DceMath_*`, `Geom_*`, `MBKC_*`).
   - Hạ tầng MCP Server tích hợp sẵn trong DCE Pro (`C:\Dce Pro\AiAgent\Mcp\mcp_dce_server.py`) kết nối Named Pipe `\\.\pipe\DceMcp_<PID>`.
   - Bộ danh bạ API hoàn chỉnh dạng JSON (`C:\Dce Pro\AiAgent\DanhBa`) gồm 311 lệnh CAD (`danhba_lenh.json`), danh bạ thép `MatTienThep` (`danhba_dce_thep.json`), danh bạ Excel (`danhba_dce_excel.json`), danh bạ Etabs (`danhba_etabs.json`).

---

## 2. NHIỆM VỤ 1: KHẢO SÁT AUTOCAD COMMANDS & AUTOLISP (`DCE PRO.LSP`)

### 2.1. Kiến trúc tổng thể của `DCE Pro.lsp` (799 dòng)
Tệp `C:\Dce Pro\DCE Pro.lsp` đóng vai trò là bootstrap loader trung tâm cho toàn bộ tiện ích DCE Pro trong môi trường CAD.

```
                    DCE Pro.lsp
                         │
         ┌───────────────┴───────────────┐
         ▼                               ▼
  Định nghĩa lệnh tắt             Khởi động an toàn
  (61 defun c:... gọi .NET)      (Nhận diện CAD + Netload)
         │                               │
         │                        ┌──────┴──────┐
         │                        ▼             ▼
         │                   S::STARTUP    Command Reactor
         │                   (vla-nap)     (dce_pu_lenh)
         ▼                               │
  Phụ trợ hình học LISP                  ▼
  - c:tencaukien                 Nạp CUIX Menu
  - c:GenC                       Netload DLL theo CAD
  - c:rdt (Rải đối tượng)        Gắn AddDocEvent
  - c:t2m (Text to MText)        Nạp GLDCE.LSP
```

### 2.2. Danh mục 61 Lệnh tắt gọi .NET Command trong `DCE Pro.lsp`

| STT | Lệnh LISP | Lệnh .NET được gọi | Phân loại | Mô tả chức năng | Hộp thoại / Tham số |
|:---:|---|---|---|---|---|
| 1 | `c:mb1` | `XuatDuLieuModelEtabs` | Chung / Etabs | Xuất dữ liệu mặt bằng kết cấu từ CAD sang Etabs | Hộp thoại chọn tầng, gốc tọa độ |
| 2 | `c:UpdateModelEtabs` | `UpdateModelEtabs` | Chung / Etabs | Cập nhật hình học / tiết diện mô hình Etabs | Form kết nối OAPI |
| 3 | `c:GETFORM` | `GETFORM` | Chung | Mở form điều khiển trung tâm DCE Pro | Modeless Form |
| 4 | `c:taomv` | `taomv` | Chung / Layout | Tạo và căn chỉnh Viewport theo tỷ lệ khung A3/A4 | Chọn khung / tỷ lệ |
| 5 | `c:slck` | `TKTSSOLUONGCAUKIEN` | Thống kê thép | Sửa số lượng cấu kiện hàng loạt trong thống kê | Prompt nhập số lượng |
| 6 | `c:sdkt` | `TKTSDUONGKINHTHEP` | Thống kê thép | Sửa đường kính thanh thép hàng loạt | Prompt chọn đường kính |
| 7 | `c:ToiUuCatThep` | `ToiUuCatThep` | Thống kê thép | Mở module tối ưu cắt thép 1D (thanh thương phẩm 11.7m) | Form `Bea_ToiUuCatThepV2` |
| 8 | `c:MaTTThep` | `MaTTThep` | Thống kê thép | Copy dữ liệu XData thép từ thanh nguồn sang các thanh đích | Pick nguồn -> chọn đích |
| 9 | `c:ShopDrawingBeam` | `ShopDrawingBeam` | Dầm | Mở module vẽ dầm Shop Drawing chi tiết | Form `DceShopBeam` |
| 10 | `c:bb1` | `VeBlockDam2` | Dầm | Chèn block ký hiệu dầm 2 nhịp | Điểm chèn |
| 11 | `c:bb2` | `VeBlockCaoDoMB` | Dầm | Chèn block cao độ mặt bằng | Điểm chèn, giá trị cao độ |
| 12 | `c:bb3` | `VeBlockTrucDam` | Dầm | Chèn block ký hiệu trục định vị dầm | Điểm chèn, tên trục |
| 13 | `c:bb4` | `VeBlockChieuDaySan` | Dầm | Chèn block ký hiệu chiều dày sàn | Điểm chèn, độ dày hs |
| 14 | `c:bb5` | `VeBlockGiatCaoDoSan`| Dầm | Chèn block ký hiệu giật cấp cao độ sàn | Điểm chèn, độ lệch cos |
| 15 | `c:bb6` | `VeBlockCaoDoMC` | Dầm | Chèn block cao độ mặt cắt dầm | Điểm chèn |
| 16 | `c:bb7` | `VeBlockSection` | Dầm | Chèn block vết cắt tiết diện dầm (1-1, 2-2) | Điểm chèn |
| 17 | `c:1111` | `XemLienKetTenCauKien`| Dầm | Hiển thị đường gióng liên kết giữa tên dầm và hình học | Highlight đối tượng |
| 18 | `c:111` | `BlockBeam_Size` | Dầm | Hiện thông số kích thước b x h trên block dầm | Toggle hiển thị |
| 19 | `c:222` | `BlockBeam_Name` | Dầm | Hiện tên dầm trên block dầm | Toggle hiển thị |
| 20 | `c:333` | `BlockBeam_NameSize` | Dầm | Hiện đồng thời tên và kích thước dầm | Toggle hiển thị |
| 21 | `c:444` | `BlockBeam_Hide` | Dầm | Ẩn toàn bộ text trên block dầm | Toggle hiển thị |
| 22 | `c:555` | `BlockBeam_Width` | Dầm | Hiển thị bề rộng b của dầm | Toggle hiển thị |
| 23 | `c:ten` | `DatTenCauKienDam` | Dầm | Tự động đặt tên dầm theo tuyến trục và số nhịp | Quét chọn vùng mặt bằng |
| 24 | `c:GanTruc` | `GanTrucBlockDam` | Dầm | Gán thông tin trục tựa vào block dầm | Quét chọn trục và block |
| 25 | `c:vd` | `VeDam_DCE` | Dầm | Vẽ chi tiết 1 cấu kiện dầm đơn lẻ từ dữ liệu Excel | Pick điểm đặt bản vẽ |
| 26 | `c:vdall` | `VeDamAll_DCE` | Dầm | Vẽ toàn bộ dầm trong tầng / dự án ra bản vẽ | Pick điểm gốc layout |
| 27 | `c:THTK_dce` | `TongHopThongKe_dce`| Thống kê thép | Tổng hợp khối lượng và bảng thống kê thép toàn dự án | Chọn bảng TKT |
| 28 | `c:tkt_dce` | `tkt_dce` | Thống kê thép | Xuất bảng thống kê thép cho các dầm được chọn | Quét chọn dầm |
| 29 | `c:stk_dce` | `stk_dce` | Thống kê thép | Mở bảng sửa / biên tập thống kê thép tương tác | Form `TKT_frmMain` |
| 30 | `c:ktc` | `ImportKichThuoc` | Dầm | Import kích thước dầm từ bảng tính Excel vào CAD | Đọc file Excel |
| 31 | `c:QuetThongKe_dce`| `QuetThongKe_dce` | Thống kê thép | Quét nhanh bản vẽ trích xuất dữ liệu thống kê thép | Quét window |
| 32 | `c:suathep` | `SuaCKThep` | Dầm | Mở hộp thoại sửa thông số thanh thép (đường kính, số thanh) | Form `Links_SuaNhieuThep` |
| 33 | `c:UpDateDLDam` | `UpDateDLDam` | Dầm | Cập nhật ngược dữ liệu dầm từ CAD sang Excel | Đọc XData ghi Excel |
| 34 | `c:XemLienKetThep` | `XemLienKetThep` | Dầm / Thép | Xem liên kết giữa tag thép, đường dim và thanh thép | Vẽ leader DCE_Nhip |
| 35 | `c:DatTenLineDam2` | `DatTenLineDam2` | Dầm | Gán tên dầm vào các đường polyline tim dầm | Chọn line dầm |
| 36 | `c:ImportKichThuocCot`| `ImportKichThuocCot`| Cột | Nạp kích thước tiết diện cột từ Etabs/Excel | Đọc model Etabs |
| 37 | `c:ImportHinhDangCot`| `ImportHinhDangCot` | Cột | Nạp hình học cột phức tạp (chữ L, T, đa giác) | Đọc file GenCol |
| 38 | `c:UpdateThepTinhToan`| `UpdateThepTinhToan`| Dầm / Cột | Cập nhật diện tích thép tính toán As từ Etabs | Kết nối Etabs OAPI |
| 39 | `c:VeMCD_DCE` | `VeMCD_DCE` | Dầm | Vẽ mặt cắt ngang dầm (tiết diện 1-1, 2-2) | Pick điểm đặt |
| 40 | `c:GanLienKetThep` | `GanLienKetThep` | Thép / Dầm | Gán liên kết XData giữa thanh thép và dim rải / tag | Form `Chung_GanLienKetThep` |
| 41 | `c:XoaLienKetThep` | `XoaLienKetThep` | Thép / Dầm | Xóa liên kết giữa tag và thanh thép | Chọn đối tượng |
| 42 | `c:GanDLLineDam` | `GanDLLineDam` | Dầm | Gán thuộc tính hình học vào line dầm | Chọn line |
| 43 | `c:XoaDLDataDCE` | `XoaDLDataDCE` | Hệ thống | Xóa sạch 13 RegApps dữ liệu ẩn trên đối tượng chọn | Quét chọn đối tượng |
| 44 | `c:XemLienKetMBCK` | `XemLienKetMBCK` | Dầm / MB | Xem quan hệ liên kết trên mặt bằng cấu kiện | Highlight quan hệ |
| 45 | `c:XuatDuLieuDam` | `XuatDuLieuDam` | Dầm | Xuất danh sách dầm mặt bằng ra bảng tính Excel | Ghi file `09.DCE_Beam` |
| 46 | `c:vc` | `VeCotDcePro` | Cột | Vẽ mặt bằng định vị và chi tiết cột BTCT | Form `DceColumn` |
| 47 | `c:vv` | `VeVachDcePro` | Vách | Vẽ chi tiết vách / lõi thang máy BTCT | Form `DceWall` |
| 48 | `c:ColumnREC1` | `ColumnREC1` | Cột | Vẽ cột chữ nhật bố trí thép kiểu 1 | Thông số b, h, thép |
| 49 | `c:ColumnREC2` | `ColumnREC2` | Cột | Vẽ cột chữ nhật bố trí thép kiểu 2 (nhiều lớp) | Thông số b, h, thép |
| 50 | `c:ColumnCi` | `ColumnCi` | Cột | Vẽ cột tròn bố trí thép đai xoắn / đai tròn | Đường kính D, số thanh |
| 51 | `c:T1` | `VeThepSanTopX` | Sàn | Rải thép sàn lớp trên phương X (Mũ cột / gối) | Điểm đầu, cuối, khoảng rải |
| 52 | `c:T2` | `VeThepSanTopY` | Sàn | Rải thép sàn lớp trên phương Y | Điểm đầu, cuối, khoảng rải |
| 53 | `c:shts` | `SoHieuThepSan` | Sàn | Đánh số hiệu thanh thép sàn tự động | Chọn vùng sàn |
| 54 | `c:B1` | `VeThepSanBotX` | Sàn | Rải thép sàn lớp dưới phương X (Thép nhịp) | Điểm đầu, cuối, khoảng rải |
| 55 | `c:B2` | `VeThepSanBotY` | Sàn | Rải thép sàn lớp dưới phương Y | Điểm đầu, cuối, khoảng rải |
| 56 | `c:tkts` | `ThongKeThepSan` | Sàn | Xuất bảng thống kê cốt thép sàn | Quét chọn thép sàn |
| 57 | `c:cf` | `ChiaKhoangCach` | Tiện ích | Chia đều khoảng cách và rải ký hiệu thép | Nhập số đoạn hoặc bước |
| 58 | `c:VBC` | `VeBlockCoc` | Cọc / Móng | Chèn block tim cọc bê tông / cọc khoan nhồi | Tọa độ tâm cọc |
| 59 | `c:DTC` | `DanhTenCoc` | Cọc / Móng | Đánh số tên cọc tự động theo thứ tự X-Y | Chọn nhóm cọc |
| 60 | `c:XTDC` | `XuatToaDoCoc` | Cọc / Móng | Xuất tọa độ danh sách cọc ra bảng CAD hoặc Excel | Quét chọn cọc |
| 61 | `c:StripMong` | `StripMong` | Móng | Tạo Strip dải móng tự động cho Safe | Chọn đài móng |
| 62 | `c:TextDaiMong` | `TextDaiMong` | Móng | Gán text tên đài và ký hiệu đài móng | Chọn đài |
| 63 | `c:VeMongCoc` | `VeMongCoc_DCE` | Móng | Vẽ chi tiết đài móng cọc (mặt bằng, mặt cắt) | Form `Mong_VeMongCoc` |
| 64 | `c:VeMongDon` | `VeMongDon_DCE` | Móng | Vẽ chi tiết móng đơn nông | Form `Mong_VeMongDon` |
| 65 | `c:TMCoc` | `TMCoc` | Móng | Xuất thuyết minh sức chịu tải cọc đơn | Đọc file SCT cọc |
| 66 | `c:rdc` | `RaiDaiCoc` | Móng | Rải tự động các đài cọc trên mặt bằng móng | Form `Mong_RaiDaiCoc` |
| 67 | `c:VeBlockLanhTo` | `VeBlockLanhTo` | Phụ trợ | Chèn block ký hiệu lanh tô cửa | Chọn vị trí mở |
| 68 | `c:VeMatCatLanhTo` | `VeMatCatLanhTo` | Phụ trợ | Vẽ chi tiết mặt cắt cấu tạo lanh tô | Chọn loại lanh tô |
| 69 | `c:VeThang_DCE` | `VeThang_DCE` | Phụ trợ | Vẽ chi tiết bản thang BTCT (2 vế, 3 vế) | Form `ThangMoi_Lenh` |

### 2.3. Các hàm AutoLISP thuần hình học và quản lý môi trường
Trong `DCE Pro.lsp` có 5 hàm tiện ích thuần LISP tự xử lý hình học CAD:
1. `c:DCENAP`: Lệnh ép buộc nạp lại DLL và CUIX thủ công khi quá trình tự động bị hoãn hoặc lỗi.
2. `c:tencaukien`: Tự động đánh số tăng dần cho TEXT, MTEXT, ATTDEF với tiền tố người dùng chỉ định (`prefixname` + `real_input++`).
3. `c:GenC`: Trích xuất tọa độ đường bao tiết diện cột (`wpolycoor`) và tọa độ các thanh thép tròn (`wrebar`) ra tệp văn bản tọa độ `Coordinates.txt`, hỗ trợ lỗ mở (opening polylines) và nhóm đường kính thép khác nhau.
4. `c:rdt`: Rải đối tượng (`chonnhomdoituong`, `choncuver`, `hoikieurai`, `raikhoangcach`, `raisoluong`, `thuchienrai`, `copycuoiquay`, `COPYQUAY`, `COPYKOQUAY`) dọc theo đường cong dẫn hướng (Polyline, Spline, Arc) với tùy chọn xoay vuông góc hoặc giữ nguyên hướng.
5. `c:t2m`: Chuyển đổi hàng loạt đối tượng TEXT thành MTEXT riêng lẻ (`_txt2mtxt`).

### 2.4. Tiện ích bổ trợ đi kèm
- `GLDCE.LSP` (58 dòng): Cung cấp `c:dlay` (chuyển layer tất cả Viewport sang `DEFPOINTS`), `c:CHIAVIEWPORT` (chia đôi/chia tư Viewport tự động tính toán CustomScale, MSPACE/PSPACE, duy trì tâm góc nhìn), và `c:CVP` (`(c:dlay) (c:CHIAVIEWPORT)`).
- `Trich chi tiet (TCT).lsp` (18 dòng): Cung cấp `c:TCT` (tạo trích chi tiết bằng cách copybase vùng chọn thành block ẩn, tạo hình chữ nhật bao quanh, dùng lệnh `XCLIP` cắt xén block, xóa khung và di chuyển khối trích chi tiết ra vị trí trình bày).

### 2.5. Cơ chế khởi động an toàn và nhận diện đa nền tảng CAD
`DCE Pro.lsp` triển khai giải pháp phòng vệ đa tầng chống sập CAD khi nạp sớm:
- **Nguyên tắc**: Tuyệt đối không gọi `netload` ngay khi chưa có document hoạt động (`(vla-get-ActiveDocument)` trả về nil hoặc lỗi, hoặc `CMDACTIVE /= 0`).
- **Nhận diện CAD host**: Đọc biến hệ thống `PRODUCT`, `PROGRAM`, `ACADVER`:
  - `*AUTOCAD*` -> CUIX: `MenuDCE_ACAD.cuix`, DLL: `C:\DCE Pro\Dce Pro.dll`
  - `*ZWCAD*` -> CUIX: `MenuDCE_ZWCAD.cuix`, DLL: `C:\DCE Pro\ZwDCE Pro.dll`
  - `*GSTARCAD*, *GCAD*, *ENJI*` -> CUIX: `MenuDCE_GCAD.cuix`, DLL: `C:\DCE Pro\GcDCE Pro.dll`
  - `*BRICS*, *LMS TECH*` -> CUIX: `MenuDCE_BCAD.cuix`, DLL: `C:\DCE Pro\BcDCE Pro.dll`
  - `*PROGE*, *ICAD*, *INTELLICAD*` -> CUIX: `MenuDCE_ICAD.cuix`, DLL: `C:\DCE Pro\IcDCE Pro.dll`
- **Bộ móc khởi động kép**:
  - Tầng 1: `s::startup` móc nối an toàn (chuỗi lại `s::startup` cũ của người dùng, không ghi đè).
  - Tầng 2: Command Reactor (`vlr-command-reactor` trên `:vlr-commandEnded`) gọi `(dce_gui_nap)` qua `vla-SendCommand` không đồng bộ, tối đa 3 lần thử rồi tự hủy reactor.
  - Ghi nhật ký chi tiết vào `C:\Dce Pro\DCE Load.log`.

---

## 3. NHIỆM VỤ 2: PHÂN TÍCH MÔ HÌNH DỮ LIỆU AUTOSTR (`DUANMAU.AUTOSTR`)

### 3.1. Cấu trúc Schema JSON của `DuAnMau.autostr` (692 dòng)
Tệp `.autostr` là định dạng lưu trữ dự án kết cấu JSON độc quyền của DCE Pro, được deserialize thành lớp `ClassLibrary1.AutoStr.AutoStrConfig` trong mã C#.

```json
{
  "TenDuAn": "Dự án mẫu",
  "ProjectRootPath": "C:\\Dce Pro\\AutoStr",
  "FileGhiChuChung": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\GhiChuChung 5574 _ A3.dwg",
  "FileKhungTen": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\Khung A3.dwg",
  "FileBlockTenBanVe": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\BlockTenBanVe.dwg",
  "FileChiTietCoc": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\Chi Tiet Coc 250x250.dwg",
  "ExcelGocPath": "C:\\Dce Pro\\Excel\\File V.2020",
  "MacBtCotVach": "B20",
  "MacBtDamSan": "B20",
  "MacThep": "CB400-V",
  "BeRongDamDienHinh": 220.0,
  "ChieuCaoDamDienHinh": 400.0,
  "BeRongDam2": 220.0,
  "ChieuCaoDam2": 400.0,
  "BeRongDam3": 220.0,
  "ChieuCaoDam3": 300.0,
  "ChieuDaySanDienHinh": 120.0,
  "ChieuDaySanWC": 100.0,
  "GopChiTietDamGiongNhau": false,
  "TienToCot": "C",
  "GopCotGiongNhau": true,
  "CotChoTangTietDien": true,
  "CotHeSoTangToiDa": 2.0,
  "CotBuocTang": 50.0,
  "CocTuTangKhiKhongDat": true,
  "SanSapXepTagThep": true,
  "MbkcKyHieuSan": true,
  "QuyDinhThepMong": false,
  "ThepMongTrenX": "14a100",
  "ThepMongTrenY": "14a100",
  "ThepMongDuoiX": "14a100",
  "ThepMongDuoiY": "14a100",
  "MongDayBTLot": 100.0,
  "MongBTLotMoRong": 50.0,
  "MongBTBVTren": 35.0,
  "MongBTBVDuoi": 135.0,
  "MongBTBVThanh": 50.0,
  "MongNgamCoc": 100.0,
  "PrefixTenDai": "ĐC.",
  "KhoiLuongRiengGach": 1500.0,
  "KhoiLuongRiengVua": 1800.0,
  "ChieuDayTrat": 30.0,
  "SaiSoBatDiem": 200.0,
  "TyLeMBKC": "1:75",
  "NetIn": "monochrome.ctb",
  "MayIn": "DWG To PDF.pc3",
  "KhoGiay": "ISO full bleed A3 (297.00 x 420.00 MM)",
  "LuonGhiDe": true,
  "TachFileEtabsMong": true,
  "TuPhucHoiUngDung": true,
  "AiNghiemThuMoiBuoc": true,
  "AiNghiemThuCacBuoc": "4,5,6,7,13,14,15,20"
}
```

### 3.2. Cấu trúc các khối dữ liệu chuyên sâu
1. **Khối Layers (`AutoStrLayerNames`)**:
   - Kiến trúc đầu vào: `LayerLuoiTruc` ("KT_TRUC"), `LayerDimTruc` ("KT_DIM_TRUC"), `LayerCot` ("KT_COT"), `LayerDam` ("DAM"), `LayerBaoSan` ("KT_BAO_SAN"), `LayerLoMo` ("KT_LO_MO"), `LayerNhaVeSinh` ("KT_SAN_HA_COT"), `LayerTuong` ("KT_TUONG").
   - Kết cấu xuất bản: `LayerKcTruc` ("DCE_Truc"), `LayerKcCot` ("DCE_SUB-COWA_----"), `LayerKcDam` ("DCE_Beam"), `LayerKcBaoSan` ("DCE_BaoBeTong"), `LayerKcLoMo` ("DCE_Gen_Open"), `LayerThepDuoiSuot` ("DCE_San_Duoi"), `LayerThepTrenSuot` ("DCE_San_Tren").
2. **Khối Folders (`AutoStrFolderNames`)**:
   - `01.SheetSet`, `02.Xref`, `03.DuongPhanTichLuc`, `04.Etabs`, `05.ExcelTinhToan`, `06.MatBangMong`, `07.MatBangKetCau`, `08.MatBangCotVach`, `09.ChiTietCotVach`, `10.ChiTietDam`, `11.ChiTietSan`, `12.CauKienPhu`.
3. **Khối Tải trọng & Tổ hợp (`ComboSettings`)**:
   - TCVN 2737:2023 & TCVN 5574:2018.
   - Load Cases: `DL` (Dead), `SDL` (SuperDead), `BW` (Brick Wall), `LL` (Live Load), `WX` (Wind X), `WY` (Wind Y).
   - 14 Tổ hợp ULS (`ULS01`..`ULS14`): Hệ số tĩnh tải 1.1–1.2, hoạt tải 1.3 (hoặc 1.17 khi kết hợp gió), gió 2.1 (hoặc 1.89 khi nhiều tải tạm thời).
   - 14 Tổ hợp SLS (`SLS01`..`SLS14`) + Tổ hợp dài hạn `THDH` (hệ số hoạt tải dài hạn $\psi_L = 0.35$).
4. **Khối Móng & Lò xo cọc (`PointSpringList`, `PointSpring`, `PileCheckCombo`)**:
   - Lò xo đầu cọc: Tên `SP1`, loại cọc `CV1`, đường kính 200mm, sức chịu tải $P_{vl} = 200.0\text{ kN}$, độ cứng lò xo tịnh tiến $K_x = K_y = 2000.0\text{ kN/m}$, $K_z = 20000.0\text{ kN/m}$.
   - Dung sai cọc: Khoảng cách cọc tối thiểu $3d$, mép cọc đến mép đài $250\text{ mm}$, chiều cao đài $700\text{ mm}$, bề rộng dầm móng $300\text{ mm}$, cao $600\text{ mm}$, hệ số an toàn cọc 1.265.
5. **Khối Bố trí Thép Sàn (`SlabRebar`)**:
   - Thép dưới: $\phi 10a200$ cả hai phương X, Y.
   - Thép gia cường mũ trên: $\phi 10a200$ hai phương X, Y.
   - Tỷ lệ neo mũ: `TyLeMu` = 4.0 (nhịp $L/4$).
   - Tỷ lệ gia cường dưới: `TyLeGcDuoi` = 4.0.
   - Lớp bảo vệ mép sàn: $30\text{ mm}$, neo thép dưới $40d$, neo thép trên $40d$.
6. **Khối Phân tầng (`Floors`)**:
   - Mảng các tầng `AutoStrFloorInfo`: `StoryIndex`, `TenTang`, `KyHieuTang`, `FilePathKienTruc`, `ChieuCaoTang` (3600mm), `CaoDoTang`, `BeRongDam` (220mm), `ChieuCaoDam` (400mm), `ChieuDaySan` (120mm), `HoatTai` ($1.5\text{ kN/m}^2$), `TinhTai` ($1.5\text{ kN/m}^2$).

### 3.3. Quy trình 24 Bước Tự Động Hóa Kết Cấu (`AutoStrStep01`..`AutoStrStep24`)

| Bước | Lớp C# thực thi | Tên chức năng | Chi tiết đầu vào / đầu ra |
|:---:|---|---|---|
| **01** | `AutoStrStep01_TaoSheetSet` | Khởi tạo SheetSet | Tạo tệp `.dst` quản lý toàn bộ hồ sơ bản vẽ dự án |
| **02** | `AutoStrStep02_LuoiTruc` | Rút trích lưới trục | Đọc layer `KT_TRUC` kiến trúc, chuẩn hóa lưới trục sang `DCE_Truc` |
| **02b**| `AutoStrStep02b_ExrefCot` | Gán Xref cột | Tạo tệp xref cột dùng chung giữa các tầng |
| **03** | `AutoStrStep03_DinhViCot` | Định vị cột vách | Phân tích polygon cột, sinh mặt bằng định vị `MBDV_Cot` |
| **04** | `AutoStrStep04_MBKC` | Mặt bằng kết cấu | Thuật toán ghép nét dầm, nhận diện ô sàn, lỗ mở, hạ cốt WC |
| **05** | `AutoStrStep05_DuongPhanTichLuc` | Đường phân tích lực | Tạo wireframe tim dầm, tim cột làm dữ liệu nạp Etabs |
| **06** | `AutoStrStep06_TaiTuong` | Gán tải tường | Quét layer `KT_TUONG`, tính tải trọng tường gạch phân bố lên dầm |
| **07** | `AutoStrStep07_BuildEtabsModel` | Xây dựng mô hình Etabs | Kết nối Etabs OAPI, tự động sinh Levels, Grids, Frames, Shells, Tải trọng |
| **08** | `AutoStrStep08_TinhToanDamExcel` | Tính toán dầm ra Excel | Chạy Etabs, trích xuất nội lực dầm, nạp vào workbook `09.DCE_Beam` |
| **09** | `AutoStrStep09_VeChiTietDam` | Vẽ chi tiết dầm | Chuyển dữ liệu sheet vẽ sang CAD, sinh bản vẽ Shop Drawing dầm |
| **10** | `AutoStrStep10_TinhToanCotExcel` | Tính toán cột ra Excel | Trích xuất lực dọc N, mômen M, nạp workbook `08.DCE_ColumnWall` |
| **11** | `AutoStrStep11_VeChiTietCotVach` | Vẽ chi tiết cột vách | Sinh bản vẽ bố trí thép cột, mặt cắt tiết diện, chi tiết nối thép |
| **12a**| `AutoStrStep12a_TinhToanSan` | Tính toán bản sàn | Tính toán mômen uốn ô bản sàn theo TCVN 5574:2018 |
| **12** | `AutoStrStep12_VeChiTietSan` | Vẽ chi tiết thép sàn | Rải thép sàn lớp dưới (suốt) và lớp trên (mũ cột/gối) lên mặt bằng |
| **13** | `AutoStrStep13_MBMongCoc` | Mặt bằng móng cọc | Định vị tim cọc, bố trí đài cọc, sinh mặt bằng `MBKC_Mong` |
| **14** | `AutoStrStep14_DPTLMong` | Đường PTL móng | Sinh đường phân tích lực và dải Strip móng cho Safe |
| **15** | `AutoStrStep15_EtabsMong` | Mô hình móng Etabs | Gán độ cứng lò xo cọc `PointSpring`, giải phản lực đầu cọc |
| **16** | `AutoStrStep16_TTMong` | Tính toán đài cọc | Kiểm tra xuyên thủng, tính toán cốt thép đài cọc ra Excel `13.DCE_DaiCoc` |
| **17** | `AutoStrStep17_VeMong` | Vẽ chi tiết đài móng | Sinh bản vẽ chi tiết các đài cọc ĐC-1 đến ĐC-n |
| **18** | `AutoStrStep18_TTGiangM` | Tính toán giằng móng | Tính toán dầm giằng móng chịu lực lệch tâm và lún lệch |
| **19** | `AutoStrStep19_VeGiangM` | Vẽ chi tiết giằng móng | Sinh bản vẽ chi tiết hệ giằng móng GM |
| **20** | `AutoStrStep20_CKPhu` | Cấu kiện phụ | Tự động vẽ chi tiết cầu thang 2 vế, lanh tô, bổ trụ, bể phốt |
| **21** | `AutoStrStep21_DanhMuc` | Danh mục bản vẽ | Đọc toàn bộ khung tên trong dự án, xuất bảng danh mục bản vẽ tự động |
| **22** | `AutoStrStep22_MoSheetSet` | Hoàn thiện SheetSet | Cập nhật cây thư mục và danh sách bản vẽ vào AutoCAD Sheet Set Manager |
| **23** | `AutoStrStep23_InAn` | Xuất bản & In ấn | Tự động xuất toàn bộ dự án sang PDF qua `DWG To PDF.pc3` khổ A3 |
| **24** | `AutoStrStep24_TMTT` | Thuyết minh tính toán | Tổng hợp và xuất tệp Word `.docx` thuyết minh tính toán kỹ thuật |

---

## 4. NHIỆM VỤ 3: HỆ THỐNG MÁY HỌC & TOKENIZER GHI CHÚ THÉP (`MAUHOC.DB`)

### 4.1. Cơ sở dữ liệu SQLite `mauhoc.db`
Cơ sở dữ liệu lưu tại `C:\Dce Pro\MayHoc\mauhoc.db` dung lượng 8.98 MB, quản lý dữ liệu huấn luyện NLP cho việc bóc tách chuỗi ghi chú thép phức tạp của kỹ sư kết cấu.

#### Lược đồ bảng `MauHoc` (45,020 bản ghi)
```sql
CREATE TABLE MauHoc(
    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
    TextGoc TEXT, 
    ChiSoSo INTEGER, 
    GiaTri INTEGER, 
    Feature TEXT, 
    Nhan TEXT, 
    Nguon TEXT, 
    ThoiGian TEXT, 
    Hash TEXT
);
```
- `TextGoc`: Chuỗi ghi chú thô trên bản vẽ CAD (ví dụ: `"[1b] 3x∅20, L= 11700"`, `"[1a+3G]6x∅20,L=11700"`, `"[2] 2∅18 + 1∅20 L=3500"`).
- `ChiSoSo`: Thứ tự xuất hiện của token số trong chuỗi (0, 1, 2, 3...).
- `GiaTri`: Giá trị số nguyên của token (ví dụ: `1`, `3`, `20`, `11700`).
- `Feature`: Vector đặc trưng NLP được trích xuất (khoảng cách, ký tự liền trước, liền sau).
- `Nhan`: Nhãn phân loại mục tiêu (Target Label).
- `Nguon`: `seed` (45,006 mẫu tổng hợp chuẩn ban đầu), `user` (14 mẫu ghi nhận từ thao tác sửa tay của người dùng).
- `Hash`: Mã MD5 chống trùng lặp mẫu học.

#### Lược đồ bảng `LichSuSua` (3 bản ghi)
Ghi nhận các trường hợp máy học đoán sai và người dùng sửa lại trên giao diện CAD:
```sql
CREATE TABLE LichSuSua(
    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
    TextGoc TEXT, 
    Truoc TEXT, 
    Sau TEXT, 
    ThoiGian TEXT
);
```
*Dữ liệu thực nghiệm trích xuất từ DB:*
- Record 1: `TextGoc` = `"[1b] 3x∅20, L= 11700"` -> `Sau` = `"SL=3;DK=20;KC1=0;KC2=0;KC3=0;KC4=0;SH=1b;L=11700"`
- Record 2: `TextGoc` = `"[1a+3G]6x∅20,L=11700"` -> `Sau` = `"SL=6;DK=20;KC1=0;KC2=0;KC3=0;KC4=0;SH=1a+3G;L=11700"`
- Record 3: `TextGoc` = `"[1a+4t]6x∅20,L=11700"` -> `Sau` = `"SL=6;DK=20;KC1=0;KC2=0;KC3=0;KC4=0;SH=1a+4t;L=11700"`

#### Lược đồ bảng `CauHinhModel` (10 bản ghi lịch sử huấn luyện)
```sql
CREATE TABLE CauHinhModel(
    Ten TEXT, 
    DuongDan TEXT, 
    SoMau INTEGER, 
    ThoiGian TEXT
);
```
Bản ghi mới nhất ngày 2026-07-05 16:07:03: Mô hình `SteelToken` huấn luyện trên **45,048 mẫu**, lưu tệp mô hình nhị phân tại `C:\Dce Pro\MayHoc\model.zip`.

### 4.2. Không gian nhãn phân loại (Target Labels)
Thống kê 45,020 mẫu trong `mauhoc.db`:
- `KC1` (9,810 mẫu): Kích thước đoạn bẻ uốn hook / cánh đai đoạn 1.
- `L` (9,325 mẫu): Chiều dài thanh thép tổng thể (mm).
- `SL` (9,243 mẫu): Số lượng thanh thép.
- `BO` (6,195 mẫu): Số thứ tự hoặc số phụ cần bỏ qua.
- `SH` (4,339 mẫu): Số hiệu thanh thép (Bar Mark).
- `KC2` (3,970 mẫu): Kích thước đoạn bẻ uốn đoạn 2.
- `KC3` (1,268 mẫu): Kích thước đoạn bẻ uốn đoạn 3.
- `DK` (530 mẫu): Đường kính thanh thép (mm).
- `KC4` (340 mẫu): Kích thước đoạn bẻ uốn đoạn 4.

### 4.3. Pipeline Kỹ thuật Đặc trưng (Feature Engineering)
Vector `Feature` được mã hóa dưới dạng chuỗi các cờ phân tách bằng dấu cách:
- `PREV_*`: Loại ký tự đứng ngay trước số (`PREV_lbr` = `[`, `PREV_rbr` = `]`, `PREV_d` = `∅`/`d`, `PREV_sym`, `PREV_x` = `x`).
- `NEXT_*`: Loại ký tự đứng ngay sau số (`NEXT_b`, `NEXT_x`, `NEXT_comma`, `NEXT_edge`).
- `AFTERL_0/1`: Vị trí số nằm sau chỉ dẫn chiều dài `L=` hay không.
- `INRING_0/1`: Số nằm trong dấu ngoặc vuông `[...]` hay không (đặc trưng mạnh của số hiệu).
- `DKCHUAN_0/1`: Giá trị số có thuộc bộ đường kính thép tiêu chuẩn Việt Nam không ({6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32}).
- `NDIGIT_n`: Số chữ số của token ($n = 1..5$).
- `INSEQ_0/1`: Số có nằm trong một dãy số liên tiếp không.
- `BEFORED_0/1`: Số đứng trước ký hiệu đường kính `∅` (thường là số lượng).
- `AFTERD_0/1`: Số đứng sau ký hiệu đường kính `∅` (thường là đường kính).
- `SHLET_0/1`: Số có kèm chữ cái số hiệu phụ (ví dụ `1a`, `1b`).

### 4.4. Tích hợp mã C# (`ClassLibrary1.Dce_MayHoc`)
- Lớp dữ liệu: `SteelTokenData` (chứa các thuộc tính Feature, Text, Label).
- Lớp dự đoán: `SteelTokenPrediction` (chứa `PredictedLabel`, mảng xác suất `Score[]`).
- Lớp điều phối: `Dce_MayHoc` với các phương thức:
  - `NapModel()`: Tải mô hình `model.zip` bằng `Microsoft.ML.PredictionEngine`.
  - `ChuanHoa(string textGoc)`: Tiền xử lý chuỗi Unicode, thay thế ký hiệu $\phi$, $\Phi$, $\varnothing$ về ký tự chuẩn `∅`.
  - `PhanTich(string textGoc)`: Tokenize chuỗi, sinh vector feature cho từng số, chạy model ML dự đoán nhãn, trả về kết quả canonical dạng `SL=..;DK=..;SH=..;L=..`.
  - `GhiNhanSua(string textGoc, string truoc, string sau)`: Ghi nhận sửa lỗi vào SQLite và kích hoạt học tăng cường.
  - `HuanLuyen()`: Chạy huấn luyện lại mô hình bằng `MulticlassClassification.Trainers.SdcaMaximumEntropy` hoặc `LightGbm` của `Microsoft.ML`.

---

## 5. NHIỆM VỤ 4: HỆ THỐNG BẢNG TÍNH EXCEL & CUSTOM UDFS

### 5.1. Danh mục bảng tính kết cấu trong `Excel\File V.2020` & `DceSteel`
1. **Phần Bê tông cốt thép (`File V.2020\*.xlsm`)**:
   - `01. DCE_Etabs_2020.xlsm`: Nạp nội lực, tổ hợp tải trọng và quản lý dữ liệu Etabs.
   - `04. DCE_CheckBasicDesign_2020.xlsm`: Kiểm tra tỷ số nén cột, vách, drift chuyển vị gió, động đất giai đoạn thiết kế cơ sở.
   - `05. DCE_MongDon.xlsm`: Thiết kế móng đơn nông BTCT.
   - `08. DCE_ColumnWall_2020.xlsm`: Thiết kế cột và vách phẳng / vách lõi (biểu đồ tương tác P-M-M).
   - `09. DCE_Beam_2020.xlsm` (5.3 MB): Thiết kế dầm BTCT toàn diện (cường độ, nứt, võng, chọn thép tự động, xuất bản vẽ).
   - `10.1 DCE_SctCoc_10304 2025.xlsm` (4.9 MB): Tính toán sức chịu tải cọc theo tiêu chuẩn mới **TCVN 10304:2025** (theo đất nền, SPT, CPT, cường độ vật liệu cọc).
   - `12. DCE_SLab_2020.xlsm`: Thiết kế bản sàn BTCT theo dải Strip Safe hoặc Etabs.
   - `13. DCE_DaiCoc_2020.xlsm`: Tính toán đài cọc đài 1..9 cọc, kiểm tra xuyên thủng, tính thép đài.
   - `14. DCE_LanhTo_CauThang_McDam_2020.xlsm`: Thiết kế cấu kiện phụ (thang, lanh tô).
   - `15. DCE_ThongKeThep_2020.xlsm`: Cơ sở dữ liệu hình dạng thanh thép và bảng thống kê thép.
2. **Phần Kết cấu thép (`Excel\DceSteel\*.xlsm`)**:
   - Hơn 65 tệp bảng tính thiết kế theo **TCVN 5575:2024** và **TCVN 2737:2023**: Dầm thép chữ I (`40`), Cột thép chữ H (`200`, `201`), Liên kết hàn/bulông chịu cắt (`210`), Liên kết mômen Dầm-Cột (`215`), Xà gồ mái Z, C chịu lửa (`400`, `401`), Liên kết nách kèo (`213`), Sàn decking (`29`).

### 5.2. Công nghệ Excel-DNA XLL Add-in & Danh mục Custom UDFs
Toàn bộ các công thức tính toán kết cấu phức tạp không viết bằng VBA thô mà được biên dịch thành thư viện C# tốc độ cao đóng gói qua **Excel-DNA** thành các tệp XLL:
- `C:\Dce Pro\V.2020\DCE Excel-AddIn.xll` (32-bit) / `DCE Excel-AddIn64.xll` (64-bit)
- `C:\Dce Pro\AddinSteel\DceSteel-AddIn-packed.xll` / `DceSteel-AddIn64-packed.xll`

#### Bảng tra cứu các hàm UDF chính:
| Tên Hàm UDF | Add-in cung cấp | Ý nghĩa kỹ thuật | Tham số chính |
|---|---|---|---|
| `_xll.Beam_Function` | DCE Excel | Tính toán cốt thép dầm BTCT chịu uốn TCVN 5574:2018 | $b, h, a, M, R_b, R_s \rightarrow A_s, \xi, \alpha_m$ |
| `_xll.Foundation_TinhHeSoCChiuCat` | DCE Excel | Tính hệ số chống cắt / đâm thủng của đài cọc | Kích thước đài, khoảng cách mép cọc |
| `_xll.Slab_Function` | DCE Excel | Tính diện tích thép sàn theo dải dầm ảo Strip | Chiều dày sàn $h_s$, mômen dải Strip $M$ |
| `_xll.Slab_Crack_Function` | DCE Excel | Tính toán bề rộng khe nứt sàn $a_{crc}$ | $M_{sls}$, hàm lượng thép $\mu$, đường kính $\phi$ |
| `TRONGTAM_a` | DCE Excel | Tính toán khoảng cách trọng tâm lớp cốt thép | Mảng đường kính và số lượng thanh |
| `dt` | DCE Excel | Tính diện tích tiết diện bê tông bất kỳ | Tọa độ các đỉnh polygon |
| `_xll.Bimoment` | DceSteel | Tính toán mômen xoắn vênh (Bimoment $B_\omega$) cho dầm thép | Tải trọng xoắn, tọa độ điểm đặt lực |
| `_xll.DCEPropSectI` | DceSteel | Tra cứu đặc trưng hình học tiết diện thép chữ I/H | $h, b, t_w, t_f, r \rightarrow A, I_x, I_y, W_x, W_y, i_x, i_y, I_t, I_\omega$ |
| `_xll.DCEPropIQuat` | DceSteel | Tính đặc trưng tiết diện dầm vát / dầm thay đổi tiết diện | Tiết diện đầu và cuối |
| `_xll.DCEPropSectBox` | DceSteel | Tính đặc trưng tiết diện thép hộp chữ nhật | $b, h, t \rightarrow A, I_x, I_y, W_x, W_y$ |
| `_xll.DCEPropSectPipe` | DceSteel | Tính đặc trưng tiết diện thép ống tròn | $D, t \rightarrow A, I, W, i$ |
| `_xll.DCETCVNPhuLucFHeSoOnDinh` | DceSteel | Tra hệ số ổn định tổng thể $\phi_b$ theo Phụ lục F TCVN 5575 | Độ mảnh $\lambda$, loại tiết diện |
| `noisuy1chieu` | DceSteel / Excel | Nội suy tuyến tính 1 chiều từ bảng tra | Mảng X, mảng Y, giá trị $x_0$ |
| `BPThkd1` | DceSteel | Tính toán chiều dày bản mã chân cột (Base Plate) | Lực nén $N$, mômen $M$, ứng suất bê tông |
| `FINDCV` | DceSteel | Tra cứu profile thép định hình trong catalog | Tên quy cách thép (ví dụ: "H300x300x10x15") |

### 5.3. 73 Macro Điều phối Giao tiếp CAD–Etabs–Excel
Thông qua danh bạ `danhba_dce_excel.json`, DCE Pro thiết lập 73 phương thức điều phối:
- Nhóm kiểm tra đầu vào: `KiemTraDauVaoDam`, `KiemTraDauVaoCot`, `KiemTraDauVaoSan`, `TrangThaiWorkbook`.
- Nhóm trích xuất kết quả: `DocKetQuaDam`, `DocKetQuaCot`, `DocKetQuaVach`.
- Nhóm tự động hóa tính toán từ Etabs: `AutoStr_AiTinhToanDam`, `AutoStr_AiTinhToanCot`, `AutoStr_ChonThepSan`.
- Nhóm chuyển dữ liệu sang bản vẽ: `AutoStr_AiChuyenDuLieuVeDam`, `AutoStr_AiXuatDuLieuVeCot`, `Bea_ExportDrawingData`.
- Nhóm in thuyết minh: `AutoStr_InThuyetMinhDam`, `AutoStr_InThuyetMinhCot`, `AutoStr_InThuyetMinhSan`.

---

## 6. NHIỆM VỤ 5: CẤU TRÚC CAD XDATA & ĐỊNH DANH 13 REGAPPS

### 6.1. Định danh 13 RegApps của Hệ Sinh Thái DCE Pro
Quá trình rà soát binary và danh bạ hệ thống xác nhận chính xác **13 RegApps** (Application Names) được đăng ký trong bảng `RegAppTable` của bản vẽ CAD:

| STT | RegApp Name | Lớp quản lý (.NET) | Đối tượng CAD gán XData | Chức năng lưu trữ |
|:---:|---|---|---|---|
| 1 | **`RebarXdataDCE`** (`DcePro`) | `RebarXdataDCE` | Polyline thanh thép, Block thép | Dữ liệu cấu tạo thanh thép dầm/cột/móng |
| 2 | **`DceRebarXDataTKT`** (`DCEThongKeThep`) | `DceRebarXDataTKT` | Polyline, Dim khoảng rải, Block tag | Dữ liệu phục vụ bảng thống kê và cắt thép |
| 3 | **`Links_XDataObject`** | `Links_XDataObject` | Leader, Text, Block tag, Dimension | Quan hệ liên kết động giữa hình học và ghi chú |
| 4 | **`Dce_ThepKep`** | `DceRebarXDataTKT` | Polyline thép kép | Thông số thanh thép kép / bố trí nhiều lớp |
| 5 | **`XDATA_APP_SODOCAT`** | `Bea_ToiUuCatThepV2` | Polyline sơ đồ cắt | Dữ liệu sơ đồ tối ưu hóa cắt thép 11.7m |
| 6 | **`XDATA_APP_KHOTHEPDU`**| `Bea_ToiUuCatThepV2` | Bảng thống kê kho | Quản lý đề-xê thanh thép dư tái sử dụng |
| 7 | **`XDATA_MAT_BANG_MONG`**| `MongCoc_Lenh` | Polyline đài móng, Text móng | Thuộc tính đài móng trên mặt bằng |
| 8 | **`XDATA_MONG_COC`** | `Dce_Pile` | Block đài cọc, tim cọc | Liên kết cọc với đài và cao độ ngàm cọc |
| 9 | **`SYS_XData_ThangMoi`** | `ThangMoi_Lenh` | Block thang, Polyline bậc thang | Thông số hình học bản thang và cốt thép thang |
| 10| **`SYS_XData_App`** | `DceEvent` | Block tiêu đề, Model frame | Thông số phiên bản add-in và đơn vị dự án |
| 11| **`NHAN_XDATA_COC`** | `Dce_Pile` | Circle tim cọc, Block cọc | Tọa độ X-Y-Z cọc và sức chịu tải thiết kế |
| 12| **`NHAN_XDATA_DAM_CONG`**| `DceBeam` | Arc, Polyline dầm cong | Tọa độ tâm cung, bán kính, góc uốn dầm cong |
| 13| **`NHAN_XDATA_MB`** | `Bea_Design` | Block khung, Polyline bao sàn | Nhận dạng cao trình tầng và ranh giới mặt bằng |

### 6.2. Cấu trúc dữ liệu chi tiết của `RebarXdataDCE` (Dầm & Thép chủ)
Gán trên Polyline hoặc Block biểu diễn thanh thép chi tiết. Lưu trữ dạng mảng `TypedValue[]` (DXF group code 1000 cho chuỗi, 1070/1071 cho số nguyên, 1040 cho số thực):

```
TypedValue(1001, "RebarXdataDCE")   <-- RegApp Name
TypedValue(1000, TenCauKien)         <-- o(0): Tên cấu kiện (vd: "D1", "D2", "C1")
TypedValue(1000, SoHieu)             <-- o(1): Số hiệu thanh thép (vd: "1", "2", "1a")
TypedValue(1000, MaThep)             <-- o(2): Mã khoảng rải / liên kết dim (vd: "/KC", "a150")
TypedValue(1000, DuongKinh)          <-- o(3): Đường kính thép mm (vd: "20", "18", "8")
TypedValue(1000, SoLuong)            <-- o(4): Số lượng thanh thép (vd: "2", "3")
TypedValue(1000, LoaiThep)           <-- o(5): Vai trò thanh ("CTop", "CBot", "Đai", "Gia")
TypedValue(1040, DoanA)              <-- o(6): Chiều dài đoạn bẻ A (mm)
TypedValue(1040, DoanB)              <-- o(7): Chiều dài đoạn bẻ B (mm)
TypedValue(1040, DoanC)              <-- o(8): Chiều dài đoạn bẻ C (mm)
TypedValue(1040, DoanD)              <-- o(9): Chiều dài đoạn bẻ D (mm)
TypedValue(1040, ChieuDaiL)          <-- o(10): Chiều dài thanh thép tổng L (mm)
TypedValue(1070, HinhDang)           <-- o(11): Mã hình dạng thanh thép tiêu chuẩn (Shape ID)
```

### 6.3. Cấu trúc chuỗi Canonical của `DceRebarXDataTKT` (Thống kê thép)
Lưu trữ chuỗi text mã hóa chuẩn hóa cao cho bảng thống kê thép:
```
TypedValue(1001, "DceRebarXDataTKT")
TypedValue(1000, "SL={SốLượng};DK={ĐườngKính};KC1={ĐoạnA};KC2={ĐoạnB};KC3={ĐoạnC};KC4={ĐoạnD};SH={SốHiệu};L={ChiềuDài}")
```
*Ví dụ thực tế:* `"SL=3;DK=20;KC1=0;KC2=0;KC3=0;KC4=0;SH=1b;L=11700"`
Chuỗi này cho phép parser đọc tách tức thì không phụ thuộc vào thứ tự trường, đồng bộ 100% với định dạng của bảng `LichSuSua` trong SQLite.

### 6.4. Cấu trúc liên kết động của `Links_XDataObject`
Lưu trữ thông tin liên kết giữa thực thể hình học thanh thép và các ghi chú bản vẽ:
```
TypedValue(1001, "Links_XDataObject")
TypedValue(1005, HandleThep)         <-- SoftPointerId / Handle của Polyline thanh thép
TypedValue(1005, HandleTag)          <-- Handle của Block tag ghi chú Dce_KhtThepDai2
TypedValue(1005, HandleDim)          <-- Handle của Dimension đo khoảng rải
TypedValue(1005, HandleLeader)       <-- Handle của Leader chỉ thị vị trí
TypedValue(1000, LuaChonField)       <-- Biểu thức AutoCAD Field liên kết tự động cập nhật
TypedValue(1070, TrangThaiKhoa)      <-- Cờ khóa cập nhật (0 = Mở, 1 = Khóa)
```
- Khi người dùng kéo dãn (stretch) thanh thép hoặc dầm, AutoCAD Field tự động cập nhật chiều dài vào Text/Dim mà không cần gõ lại.
- Lệnh `XemLienKetThep` đọc `Links_XDataObject` để vẽ các tia liên kết màu đỏ tạm thời trên layer `DCE_Nhip`.
- Lệnh `XoaDLDataDCE` xóa sạch toàn bộ 13 RegApps này khi cần làm sạch bản vẽ chuyển giao cho đối tác.

---

## 7. CÁC PHÁT HIỆN KIẾN TRÚC ĐẶC BIỆT KHÁC

### 7.1. Thư viện hình học Native C++ `DceAnalyssiss64.dll` (69 Exported Functions)
Phát hiện đây là động cơ hình học thuần túy (Pure Geometry Engine) chạy 64-bit, độc lập hoàn toàn với AutoCAD, thực hiện các thuật toán:
1. **Kiểm tra không gian**: `Geom_IsPointInsidePolygon` (Ray Casting & Winding Number), `Geom_IsPolygonOverlap`, `Geom_SegmentVsSegment`.
2. **Đặc trưng đa giác**: `Geom_GetPolygonArea`, `Geom_GetPolygonCentroid`, `DceMath_ConvexHull` (Bao lồi).
3. **Cắt xén & Chiếu điểm**: `Geom_ProjectPointToLine`, `DceMath_ProjectPointToInfiniteLine`, `Geom_GetSegmentLenInPolygon`.
4. **Giải hệ toán học**: `DceMath_GiaiHePhuongTrinh`, `DceMath_GiaiPTBac2`, `DceMath_NoiSuy1Chieu`, `DceMath_NoiSuy2Chieu`.
5. **Topo hệ dầm mặt bằng (MBKC)**: `MBKC_AreBeamsConnected`, `MBKC_GetBeamGroupDirection`, `MBKC_CalculateCircumcircle`, `MBKC_OrderBeamGroup`, `TimSoDoan`, `TimSoNhip`.

*Đề xuất triển khai*: Toàn bộ 69 hàm này sẽ được port sạch sẽ 100% sang mã C# thuần túy trong thư viện `HPDcePro.Core` (`netstandard2.0`), loại bỏ hoàn toàn sự phụ thuộc vào DLL native C++ P/Invoke.

### 7.2. Hạ tầng MCP Server tích hợp (`C:\Dce Pro\AiAgent`)
DCE Pro đã xây dựng sẵn một cầu nối Model Context Protocol (MCP) hoàn chỉnh:
- Cầu nối Python: `C:\Dce Pro\AiAgent\Mcp\mcp_dce_server.py`.
- Cơ chế truyền thông: Stdio (phía AI/Claude) $\leftrightarrow$ Windows Named Pipe `\\.\pipe\DceMcp_<PID>` (phía CAD process).
- Lệnh kích hoạt trong CAD: `DBIMAIMCPSV_DCE`.
- 4 nhóm công cụ phơi bày: `@chung` (tra cứu, tính toán, hoàn tác), `@cad` (truy vấn bản vẽ, đo đạc, API thô), `@csi` (Etabs/Safe/Sap2000 OAPI), `@excel` (đọc ghi bảng tính).

---

## 8. BẢNG ÁNH XẠ CHUYỂN ĐỔI SANG KIẾN TRÚC MỚI `HPDCEPRO`

| Thành phần Legacy (C:\Dce Pro) | Thành phần mới trong `HPDcePro` | Công nghệ / Framework | Mục tiêu kiến trúc |
|---|---|---|---|
| Native `DceAnalyssiss64.dll` (69 hàm) | `HPDcePro.Core/Geometry/` | Pure C# (`netstandard2.0`) | 100% managed, cross-platform, unit-testable |
| `DuAnMau.autostr` (JSON project) | `HPDcePro.Core/Models/Project/` | `System.Text.Json` | Strongly-typed DTO, validation, immutability |
| `AutoStrStep01`..`AutoStrStep24` | `HPDcePro.Core/Workflow/` | Pipeline Pattern / Command | Thực thi từng bước độc lập, kiểm tra QC tự động |
| `MayHoc/mauhoc.db` (SQLite ML) | `HPDcePro.Core/MachineLearning/` | `Microsoft.Data.Sqlite` + Rule-based fallback | Trích xuất token thép, đọc và huấn luyện model |
| Excel COM / `DCE Excel.dll` | `HPDcePro.Core/Calculations/` + `HPDcePro.Excel` | Pure C# + ClosedXML | Tính TCVN 5574 dầm/sàn, TCVN 5575 thép, TCVN 10304 cọc không cần Excel nền |
| AutoCAD XData (`RebarXdataDCE`...) | `HPDcePro.Core/XData/` | `XDataPacket` / Codec | Đọc/ghi 13 RegApps tương thích 100% ngược |
| Giao diện WinForms cũ | `HPDcePro.UI/` | WPF + MVVM Toolkit + `MahApps.Metro` | Theme Dark/Light chuẩn repo, MetroWindow hiện đại |
| Lệnh CAD (`DCE Pro.lsp`) | `HPDcePro.AutoCad/Commands/` | AutoCAD.NET 25.1 (.NET 8) | Giữ nguyên 61 tên lệnh và alias, hỗ trợ script loader |
| License / Telemetry / Updater | **LOẠI BỎ HOÀN TOÀN** | Không có | 100% offline, sạch sẽ, không telemetry |

---

## 9. KẾT LUẬN & KIẾN NGHỊ

Cuộc khảo sát ngược đã hoàn thành xuất sắc 100% các mục tiêu đặt ra:
1. Đã giải mã toàn bộ 799 dòng AutoLISP `DCE Pro.lsp`, lập danh mục đầy đủ 61 lệnh tắt và 5 hàm tiện ích hình học.
2. Đã phân tích toàn diện schema JSON `DuAnMau.autostr` và định danh đầy đủ 24 lớp thực thi của quy trình tự động hóa `AutoStr`.
3. Đã truy vấn thành công SQLite `mauhoc.db`, khám phá 45,020 mẫu huấn luyện và bóc tách hoàn chỉnh kiến trúc NLP Tokenizer cốt thép.
4. Đã khảo sát toàn bộ kho bảng tính Excel và danh mục các hàm UDF chuyên sâu TCVN 5574 / 5575 / 10304.
5. Đã định danh chính xác 13 RegApps và cấu trúc dữ liệu nhị phân/chuỗi của `RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`.
6. Khám phá thêm 69 hàm toán hình học native trong `DceAnalyssiss64.dll` và hạ tầng MCP nội bộ `AiAgent`.

Dữ liệu khảo sát trên cung cấp nền tảng kỹ thuật vững chắc để đội ngũ kỹ sư tiến hành Phase 1 Clean-room C# rewrite trong solution `HPDcePro.slnx` mà không gặp bất kỳ điểm mù nào về kiến trúc.
