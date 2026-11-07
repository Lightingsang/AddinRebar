# SPEC_VeMong: tính năng Vẽ thép móng của Kata Pro

Trạng thái: **BẢN NHÁP, CHƯA ĐỦ ĐỂ XÂY LẠI.** Ngày thu thập: 2026-11-04.

Bản này chỉ ghi điều đọc được từ phần **không bị bảo vệ**. Hành vi vẽ thép móng nằm trong DLL có metadata cố ý làm hỏng và chuỗi bị mã hóa, nên chưa được quan sát.

| Nhóm quy tắc | Số quy tắc | Dùng để xây lại |
|---|---|---|
| Đã quan sát, có giá trị kiểm chứng | 12 | Được |
| Chỉ suy từ tên, chưa thấy hành vi | 9 | Không, phải kiểm chứng trước |
| Chưa có bằng chứng | 13 | Không |

## 1. Quy ước

- **Quan sát:** đọc trực tiếp từ file. Với công thức, giá trị Excel lưu sẵn khớp kết quả tính lại.
- **Suy từ tên:** chỉ thấy tên định danh. Tên không chứng minh hành vi.
- **Chưa có bằng chứng:** cần điều tra tiếp theo mục 7.
- Cột "Kiểm duyệt" chờ người kiểm duyệt, chưa ai duyệt.

## 2. Nguồn bằng chứng

| Mã | Nguồn | Định danh | Cách đọc |
|---|---|---|---|
| E1 | `C:\kata_pro\Goi lenh.lsp` | sha256 `7f1dd7b1…df34` | Đọc văn bản |
| E2 | `C:\kata_pro\menukata.cui` | sha256 `b030507b…090b` | Đọc XML |
| E3 | Bản sao `Kata.xlsm`, sheet `SLAB DESIGN` | sha256 bản sao `93036043…bb96` | Đọc XML trong gói |
| E4 | Bản sao `Kata.xlsm`, sheet ẩn `Data` | như E3 | Đọc XML trong gói |
| E5 | `Kata_pro64_Cad2025.dll` | sha256 `a1560540…7673` | Liệt kê chuỗi ASCII, không chạy |
| E6 | `Revit_Thong2025.dll` | sha256 `6bdf008e…f0b8` | Liệt kê chuỗi ASCII, không chạy |
| E7 | REA `inspect_managed_artifact` trên E6 | evidence `ev_f40ef243…31d6` | Metadata hỏng: `Duplicate metadata stream #GUID` |
| E8 | REA `inspect_pe_resources` trên E5 | evidence `ev_9290c7b9…4c34` | Một resource version, không có form hay icon |
| E9 | Bản sao `Kata.xlsm`, `vbaProject.bin` | như E3 | Chỉ đọc tên module |

E5 và E6 là tên định danh, không phải code. E3 ghi "Thực hiện: Nguyễn Khả Tam". E9 có 5 module (`save_info`, `Ve_dam`, `to_hop`, `Tinh_dam`, `Module1`), không module nào mang tên móng; nội dung VBA chưa đọc.

## 3. Lệnh và điểm vào

| Mã | Quy tắc | Bằng chứng | Mức | Kiểm duyệt |
|---|---|---|---|---|
| VM-001 | Menu `Foundation`, mô tả "Móng", chạy lệnh `ve_mong` | E2 `MMU_0026` | Quan sát | Chờ |
| VM-002 | Alias `vmbm` → `ve_mat_bang_mong` | E1 | Quan sát | Chờ |
| VM-003 | Alias `rm` → `rai_mong` | E1 | Quan sát | Chờ |
| VM-004 | Alias `vmd` → `ve_mong_don` | E1 | Quan sát | Chờ |
| VM-005 | Alias `vmc` → `ve_mong_coc` | E1 | Quan sát | Chờ |
| VM-006 | Alias `vdm` → `ve_dam_mong` | E1 | Quan sát | Chờ |
| VM-007 | Alias `vgm` → `ve_giang_mong` | E1 | Quan sát | Chờ |

## 4. Tính thép móng theo dải 1 m

Sheet `SLAB DESIGN`, tiêu đề "TÍNH TOÁN THÉP MÓNG". Đây là bảng tính, **chưa có bằng chứng lệnh vẽ dùng bảng này**.

| Mã | Quy tắc | Bằng chứng | Mức | Kiểm duyệt |
|---|---|---|---|---|
| VM-008 | Vật liệu tra theo tên: `Rb`, `Eb` từ bảng bê tông; `Rs`, `Es` từ bảng thép (mục 4.2) | E3 G9:G12, E4 | Quan sát | Chờ |
| VM-009 | Mỗi móng có 4 dải: lớp dưới `X1-max`, `Y1-max`; lớp trên `X1-min`, `Y1-min` | E3 C21:C28 | Quan sát | Chờ |
| VM-010 | Chiều cao làm việc `ho = h − a`; `a` nhập riêng từng dải | E3 G24 | Quan sát | Chờ |
| VM-011 | Diện tích thép theo công thức 4.1 | E3 R24, S24, H24 | Quan sát | Chờ |
| VM-012 | Thép chọn theo công thức 4.1, tối đa 2 lớp `φ/a`; kiểm tra `As chọn ≥ As` cho `OK`, ngược lại `False` | E3 K24, L24 | Quan sát | Chờ |

### 4.1 Công thức

```text
αm = |M| / Rb / ho² × 10³      M kNm/m; Rb MPa; ho mm
ξ  = 1 − √(1 − 2·αm)
As = ξ · Rb / Rs · ho · 10³    mm²/m

As chọn = 1000/a₁ · π·φ₁²/4 + (nếu a₂ > 0) 1000/a₂ · π·φ₂²/4
```

Ca kiểm chứng lấy từ giá trị lưu trong sheet (B40, CB300-V: Rb 22, Rs 260):

| Dải | M | h | a | ho | αm | ξ | As | Bố trí | As chọn | Kết quả |
|---|---|---|---|---|---|---|---|---|---|---|
| X1-max | 250 | 1000 | 100 | 900 | 0,014029 | 0,014129 | 1075,98 | F16a200 + F14a200 | 1775,00 | OK |
| Y1-max | 200 | 1000 | 115 | 885 | 0,011607 | 0,011675 | 874,29 | F16a200 | 1005,31 | OK |
| X1-min | −150 | 1000 | 60 | 940 | 0,007716 | 0,007746 | 616,13 | F14a200 | 769,69 | OK |
| Y1-min | −140 | 1000 | 75 | 925 | 0,007437 | 0,007465 | 584,30 | F14a200 | 769,69 | OK |

Sheet còn so `h` của lớp dưới với lớp trên (`T24`, `T25`), trả `ok` khi bằng. Không suy ra `a` từ ca này: `a` là số nhập.

### 4.2 Vật liệu

Bảng ghi "TCVN 5574-2018".

| Bê tông | B10 | B15 | B20 | B22.5 | B25 | B30 | B35 | B40 | B45 | B50 | B55 | B60 | B70 | B80 | B90 | B100 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Rb (MPa) | 6 | 8,5 | 11,5 | 13 | 14,5 | 17 | 19,5 | 22 | 25 | 27,5 | 30 | 33 | 37 | 41 | 44 | 47,5 |
| Eb (MPa) | 19000 | 24000 | 27000 | 28750 | 30000 | 32500 | 34500 | 36000 | 37000 | 38000 | 39000 | 39500 | 41000 | 42000 | 42500 | 43000 |

| Thép | CB240-T | CB300-T | CB300-V | CB400-V | CB500-V | CB600-V | SD295 | SD345 | SD390 | SD490 |
|---|---|---|---|---|---|---|---|---|---|---|
| Rs (MPa) | 210 | 260 | 260 | 350 | 435 | 520 | 256,52 | 300 | 339,13 | 426,09 |

`Es` = 200 000 MPa cho mọi mác thép.

## 5. Quy tắc chỉ suy từ tên

Chưa có giá trị, điều kiện hay thứ tự. **Không được cài đặt từ bảng này.**

| Mã | Giả thuyết | Tên quan sát | Bằng chứng | Mức | Kiểm duyệt |
|---|---|---|---|---|---|
| VM-101 | Có ba loại móng: đơn, băng, cọc | `MongDon`, `MongBang`, `MongCoc` | E5 | Suy từ tên | Chờ |
| VM-102 | Có bước vẽ mặt bằng móng và rải móng riêng | `Form_Ve_Mat_Bang_Mong`, `Form_Rai_Mong`, `RaiMongJig` | E5 | Suy từ tên | Chờ |
| VM-103 | Móng cọc kiểm tra khoảng cách cọc | `checkPileSpacing` | E5 | Suy từ tên | Chờ |
| VM-104 | Có tính thép dọc và ngang móng cọc | `TinhToanThepDocMongCoc`, `TinhToanThepNgangMongCoc` | E5 | Suy từ tên | Chờ |
| VM-105 | Có tự chọn thép móng | `Form_Chon_Thep_Mong_Tu_Dong`, `KetQuaTinhThepMongTuDong` | E5 | Suy từ tên | Chờ |
| VM-106 | Chiều dày móng có thể phụ thuộc tải cọc | `GetChieuDayMongByChiuTaiCoc` | E5 | Suy từ tên | Chờ |
| VM-107 | Dữ liệu vào gồm tên, cao độ đáy, chiều cao, lớp bảo vệ, bê tông lót, vát | `tenMong`, `caoTrinhDayMong`, `hMong`, `Cover_mong`, `HBatDayMong`, `isVacMong` | E5 | Suy từ tên | Chờ |
| VM-108 | Thép móng tách theo hai phương | `Thep_mong_X`, `Thep_mong_Y` | E5, E6 | Suy từ tên | Chờ |
| VM-109 | Revit có đường vẽ móng riêng | `Ve_mong_don_Update`, `Ve_mong_bang`, `Ve_mong_coc_tong_hop_Update`, `ve_thep_mong_bang` | E6 | Suy từ tên | Chờ |

## 6. Chưa có bằng chứng

| Mã | Câu hỏi | Cách kiểm chứng đề xuất |
|---|---|---|
| VM-201 | Thứ tự hộp thoại và lựa chọn người dùng của từng lệnh | Chạy lệnh, chụp màn hình |
| VM-202 | Giá trị mặc định của mọi ô nhập | Chụp form |
| VM-203 | Hình học đài, móng băng, cổ móng | Đo DWG xuất ra |
| VM-204 | Số thanh và cách chia thép theo phương X, Y | Đo DWG với ca đổi kích thước |
| VM-205 | Móc neo, đoạn bẻ, chiều dài neo | Đo DWG |
| VM-206 | Xử lý lớp bảo vệ, lớp thép trên và dưới | Đo DWG |
| VM-207 | Thép chờ cột, giằng, dầm móng | Đo DWG |
| VM-208 | Bố trí và ghi chú mặt cắt | Đo DWG |
| VM-209 | Dim, tag, ký hiệu thép, layer, block | Đọc DWG qua AutoCAD MCP |
| VM-210 | Bảng thống kê thép móng | Đọc DWG, đọc Excel |
| VM-211 | Thông báo lỗi và điều kiện từ chối | Thử ca lỗi |
| VM-212 | Quy tắc riêng của móng băng, móng cọc | Bộ ca thử theo từng loại |
| VM-213 | Lệnh vẽ có dùng sheet `SLAB DESIGN` không | So kết quả khi đổi sheet |

## 7. Giới hạn của bản này

- **Không đọc:** thân hàm, chuỗi mã hóa, nội dung VBA, code licence, chuỗi kết nối trong DLL.
- **Không làm:** chạy Kata, sửa `C:\kata_pro`, vượt metadata hỏng hoặc mã hóa.
- **REA không decompile được:** REA báo metadata hỏng (E7), mức này không ghi được quy tắc.
- **Tra cứu web:** một lượt tìm không thấy tài liệu công khai về các lệnh này.
- **Ghi chú môi trường:** `Kata.xlsm` đang mở trên máy lúc thu thập, nên đã đọc bản sao trong thư mục tạm.

Muốn hoàn thiện cần **một trong hai**: văn bản cho phép của tác giả Kata để phân tích DLL, hoặc chạy lệnh trên bộ ca thử với license hợp lệ rồi đo DWG xuất ra.
