---
name: vn-soat-ban-ve
description: >-
  Soát lỗi bộ hồ sơ bản vẽ trước khi giao: bốn phép soát tự động (cửa sổ nhìn, chữ đè chữ, chữ bị
  nét vẽ cắt, hình tràn ô lưới), soát bằng mắt trên ảnh render, và soát kỹ thuật (thép đúng lớp,
  số thanh khớp bước, hàm lượng thép hợp lý). Dùng sau mỗi lần vẽ hoặc sửa tờ, trước khi báo xong,
  hoặc khi người dùng nói bản vẽ lộn xộn, chữ chồng, hình bị cắt.
---

# Soát bản vẽ

Ba tầng, làm đủ cả ba. Tầng tự động không thay được hai tầng sau: có những lỗi nó không nhìn thấy.

## Tầng 1: bốn phép soát tự động (vài giây, không render)

```bash
python ".claude/skills/_shared/vn-autocad/tools/soat-ban-ve-khong-render.py" --du-an du_an.<ten>
```

| Phép | Bắt lỗi gì | Hàm |
|---|---|---|
| Cửa sổ nhìn | chồng nhau, lấn khung tên, lọt khung | `sheet.check_views` |
| Chữ đè chữ | hai ô chữ giao nhau từ 18% diện tích ô nhỏ | `qc.check_text_overlap` |
| Chữ bị nét cắt | đoạn thẳng đi xuyên ô chữ (đã thu ô vào 14% để bỏ chạm viền) | `qc.check_text_on_lines` |
| Tràn ô lưới | đối tượng lòi ra ngoài ô chứa tâm của nó | `qc.check_cell_bleed` |

Bề rộng chữ đo trên chính file phông Times New Roman, có tính góc xoay. Máy không có phông này
thì phép đo sai; cài phông trước (trên Linux: gói `ttf-mscorefonts-installer`).

Cảnh báo in kèm tên lớp thủ phạm, ví dụ `LINE tren lop A-DOOR cat qua chu "WC 1"`. Hai công cụ đi kèm:

```bash
python ".claude/skills/_shared/vn-autocad/tools/chi-ra-net-cat-qua-chu.py" "GM1 250x400" --du-an du_an.<ten>
python ".claude/skills/_shared/vn-autocad/tools/de-xuat-cho-dat-chu.py" "Thoat san D60" --du-an du_an.<ten>
```

### Cách sửa theo loại lỗi (rút từ 66 chỗ đã sửa trong bộ mẫu)

| Lớp thủ phạm | Nguyên nhân thường gặp | Cách sửa |
|---|---|---|
| `S-FOUND`, `S-BEAM` | nhãn nằm trên trục cấu kiện, hoặc giữa dầm trùng trục ngang | nhãn giữa một nhịp; mã đài ở góc |
| `A-FLOR` | mạch gạch qua mã vật liệu | `tiles.floor_tiles(chua=[mọi nhãn của tầng])` |
| `A-DOOR` | cung quét cánh cửa qua nhãn phòng | dời nhãn lên góc xa cửa |
| `A-WIND`, `A-WALL` | chữ rộng hơn phòng | xoay 90 độ hoặc hạ cỡ chữ theo `S.rong_chu` |
| `M-WALL` | nhãn ống rơi lên tường | `label_at` ở chỗ trống, dùng công cụ đề xuất |
| `A-SYMBOL` | nét chỉ xuất phát từ tâm vòng số | `annotate.so_lop` (xuất phát từ viền) |
| chữ đè chữ "D3 đè S1" | dầm phụ chạy xuyên giữa ô sàn | lỗi thiết kế: tách ô sàn |

## Tầng 2: nhìn ảnh

```python
from vncad import render
render.render_sheet(doc, "KC-04", "out/_soat/KC-04.png", "plot", 150, "A3")   # anh toan to
render.render_sheet(doc, "KC-04", "out/_soat/KC-04_hi.png", "plot", 400, "A3") # roi cat chi tiet
```

Nhìn mọi tờ mới hoặc vừa sửa. Những lỗi bộ soát **không** bắt được, phải tự tìm:

- Chữ trong đường kích thước (nằm trong block DIMENSION) đè lên chữ khác.
- Hai chữ chạm mép nhau nhưng dưới ngưỡng 18%, đọc thành một chuỗi.
- Chữ, bảng biểu trên giấy (paper space) đè nhau hoặc đè hình.
- Hình bị cắt ở mép cửa sổ nhìn; tiêu đề hình nằm lọt trong vùng hình khác.
- Hình quá mảnh, quá nhỏ ở tỷ lệ đã chọn: đổi tỷ lệ hoặc cắt ngắn.

## Tầng 3: soát kỹ thuật

- **Thép đúng lớp:** sàn và bản thang: thép chịu lực chạy suốt ở lớp dưới, thép mũ ngắn ở lớp trên
  tại gối. Phóng ảnh 400 dpi lên xem.
- **Số thanh khớp bước:** bề rộng / bước + 1 phải bằng số thanh trong bảng.
- **Lớp bảo vệ trên hình khớp ghi chú:** móng 50, dầm cột 25, sàn 15.
- **Bảng thép sàn có đủ** hai phương lớp dưới và thép mũ.
- **Hàm lượng hợp lý:** thép khung 20 đến 45 kg/m² sàn; thép móng 50 đến 100 kg/m³ bê tông móng.
  Ngoài khoảng này thì hoặc bảng thiếu, hoặc có số gõ tay.
- **Khối lượng dự toán khớp bản vẽ:** soát chéo bê tông, ván khuôn với kích thước trên tờ.

## Báo cáo soát

```
**Soát tự động:** SACH | <n> cảnh báo (gom theo tờ)
**Đã nhìn:** <danh sách tờ, dpi>
**Lỗi kỹ thuật:** <mô tả, tờ, cách sửa>
**Chưa soát được:** <nếu có, lý do>
```
