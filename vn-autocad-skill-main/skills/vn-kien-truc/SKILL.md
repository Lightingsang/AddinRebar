---
name: vn-kien-truc
description: >-
  Vẽ phần kiến trúc của hồ sơ thi công nhà ở chuẩn TCVN bằng thư viện vncad: mặt bằng các tầng,
  mặt đứng, mặt cắt, khai triển WC và cầu thang, lát gạch, trần, chi tiết chống thấm, lan can,
  tổng mặt bằng định vị, bảng thống kê cửa. Dùng khi vẽ mới hoặc sửa bất kỳ hình kiến trúc nào,
  hoặc khi nhập, sửa dữ liệu công trình trong layout.py.
---

# Vẽ kiến trúc chuẩn Việt Nam

Quy chuẩn đồ hoạ (21 lớp, cỡ chữ, tỷ lệ, khung tên) đã nạp sẵn trong `vncad`, rút từ 34 bộ bản vẽ
thi công thật. Không tự chế lại lớp, cỡ chữ hay khung tên; không vẽ thiết bị bằng nét rời rạc.
Ví dụ đầy đủ: `${CLAUDE_PLUGIN_ROOT}/examples/nha_pho_5x18m/draw_*.py`.

## Sáu thứ bản vẽ thật luôn có

1. **Bộ nhiều tờ**, mỗi tờ một chủ đề.
2. **Block** cho cửa, thiết bị vệ sinh, nội thất, ký hiệu cao độ, bong bóng trục.
3. **Nhiều tỷ lệ**: mặt bằng 1/100, khai triển 1/50, chi tiết 1/25 đến 1/10, tổng mặt bằng 1/200.
4. **Cao độ 3 chữ số thập phân**: `+3.800`, không làm tròn thành `+3.8`.
5. **Ghi chú kỹ thuật** bằng câu lệnh thi công cụ thể.
6. **Khung tên đủ trường** kèm lưới chữ ký 4 vai trò.

## Dữ liệu trước, vẽ sau

Mọi số liệu nằm trong `layout.py`; file vẽ chỉ đọc số từ đó.

```python
AXIS_X = [("1", 0.0), ("2", 5500.0), ("3", 9500.0), ("4", 13000.0), ("5", 18000.0)]
AXIS_Y = [("A", 0.0), ("B", 5000.0)]
FLOOR_LEVEL = {1: 0.150, 2: 3.800, 3: 7.200}
# tuong: (p1, p2, day, [(vi_tri_tu_p1, be_rong, loai)]), loai: door, door2, wind, open
FLOOR_WALLS = {1: [((0, 0), (18000, 0), 220, [(15600, 1600, "wind")]), ...]}
# phong: (ten, tam_nhan, dien_tich_m2, goc_xoay_nhan); phong hep dai thi xoay 90
FLOOR_ROOMS = {2: [("Ban cong", (750, 2500), 3.6, 90), ...]}
```

## Mặt bằng

```python
from vncad import annotate as an, arch, fixtures as fx, standards as S

ops = arch.wall(msp, (0, 0), (18000, 0), 220, openings=[(3000, 900), (9000, 1800)])
s, e, u, n, t = ops[0]; arch.door(msp, s, e, u, n, t)          # cua 1 canh + cung mo
s, e, u, n, t = ops[1]; arch.window(msp, s, e, n, t)           # cua so
arch.column(msp, (0, 0), (220, 220))       # LUON ve cot SAU tuong de cot de len tuong
fx.place(msp, doc, "BON-CAU", (15000, 1500), rotation=90)
an.room_label(msp, doc, (7500, 3300), "Phong khach", 19.2, 0.150, "1/100")
an.room_label(msp, doc, (750, 2500), "Ban cong", 3.6, 3.800, "1/100", rotation=90)
an.view_title(msp, (9000, y_tieu_de), "Mat bang kien truc tang 1", "1/100")
```

Kích thước 3 lớp từ trong ra ngoài (lỗ mở, tim trục, phủ bì) rồi mới tới bong bóng trục:
`an.dim_tiers(...)` trả về vị trí đặt `an.grid_bubbles(...)`, nên dim không bao giờ cắt bong bóng.
Xếp hai tầng trên một tờ: bong bóng trục số chỉ đặt ở đáy tầng dưới và đỉnh tầng trên.

## Đặt nhãn cho khỏi bị nét cắt (lỗi hay gặp nhất)

- **Nhãn phòng** không đặt ở tâm hình học nếu cung quét cánh cửa đi qua đó. Dời lên góc xa cửa.
- **Phòng hẹp** hơn bề rộng dòng chữ (ban công, hành lang): `rotation=90`. Đo trước bằng
  `S.rong_chu("BAN CONG", S.th("room", "1/100"))`.
- Sau khi vẽ luôn chạy `tools/soat-ban-ve-khong-render.py`; chỗ nào báo lỗi thì
  `tools/de-xuat-cho-dat-chu.py "<chu>"` cho độ dời nhỏ nhất.

## Mặt đứng, mặt cắt

```python
from vncad import elevation as el, place, section as sec

el.storey_grid(msp, doc, [("TRET", 0.150), ("LAU 1", 3.800)], x0, x1, "1/100")   # cao do bang MET
el.opening(msp, x=1200, z_bot=3.85, w=2600, h=2300, kind="glass", panes=3)
el.balcony(msp, x0 + 300, x1 - 300, z_floor=3.800, h_rail=900, kind="kinh")
sec.slab(msp, x0, x1, z_top=3.800, thk=100)          # san cat, tu hatch be tong
sec.beam(msp, x_tam, z_top=3.800, w=200, h=400, label="D1")
sec.stair(msp, x_dau, z_bot=0.150, z_top=3.800, steps=10, tread=270)
```

Hàm mặt cắt dùng **cao độ tuyệt đối**, bỏ qua gốc đặt theo trục đứng. Muốn dời cả cụm xuống ô
lưới riêng thì bọc: `with place.Placed(msp, 0.0, oy): draw_section(...)`.

## Lát gạch và trần

```python
from vncad import tiles
# chua cho MOI nhan cua ca tang: phong lon (sanh) trai gach de len ca phong nho long trong no
tiles.floor_tiles(msp, x0, y0, x1, y1, tile=800, chua=[(cx0, cy0, cx1, cy1), ...])
```

Mạch gạch ngắt quanh chữ, đúng cách người vẽ tay làm. Mã vật liệu `G1 400x400` đặt giữa phòng;
phòng không đủ rộng thì xoay dọc hoặc hạ cỡ chữ (xem `_co_chu` trong `draw_finishes.py`).

## Chi tiết cấu tạo (chống thấm, lan can, bậc thang)

- Tỷ lệ 1/20 cho nút chống thấm, 1/10 cho hình mảnh (lan can kính), 1/20 cho bậc thang.
- **Đánh số lớp trên hình, diễn giải trong bảng chú giải trên giấy**: ghi chú đường dẫn dài ở tỷ lệ
  lớn chiếm chỗ hơn cả hình.

```python
an.so_lop(msp, 2, (ox + 480, oy + 560), chi_toi=(ox + 130, oy + 330), r=70.0, scale="1/20")
# tren giay: table.draw(lay, (158, 272), [("SO", 11, "c"), ("LOP VAT LIEU", 101, "l")], rows, "CHU GIAI CT1")
```

## Tổng mặt bằng định vị (1/200)

Lớp `A-SITE` (ranh lô, nét gạch chấm), `A-ROAD` (vỉa hè, lòng đường). Vẽ ranh lô, bao nhà, sân,
vỉa hè, một đoạn lòng đường có nét gãy (ghi bề rộng thật bằng chữ), nhà lân cận nét mảnh, cốt
cao độ, hướng Bắc (`blocks.insert(msp, doc, "HUONG-BAC", pt, "1/200")`), kích thước định vị từ
ranh lô. Bảng chỉ tiêu quy hoạch (diện tích lô, xây dựng, mật độ, tổng sàn, hệ số sử dụng đất)
vẽ trên giấy. Mẫu: `draw_site_plan.py`.

## Block có sẵn

Thiết bị: `BON-CAU`, `LAVABO`, `VOI-SEN`, `BON-TAM`, `PHEU-THU`, `BEP-NAU`, `CHAU-RUA-CHEN`,
`TU-LANH`, `GIUONG-DOI`, `GIUONG-DON`, `SOFA`, `BAN-TRA`, `BAN-AN-6`, `TU-QUAN-AO`, `TIVI`,
`XE-OTO`, `XE-MAY`. Ký hiệu: `BONGBONG-TRUC`, `CAO-DO`, `CAO-DO-MB`, `KY-HIEU-MAT-CAT`,
`BONGBONG-CHI-TIET`, `HUONG-BAC`.

## Phong thuỷ và an toàn nhà ở

- Phòng thờ tầng cao nhất, quay ra mặt tiền; không dựa lưng tường WC, không có WC tầng trên đè lên.
- WC tầng trên cùng đưa về phía sau, cửa sổ mở ra giếng trời.
- Lỗ thông tầng, cầu thang, ban công: lan can cao từ 900mm tính từ mặt sàn hoặc mũi bậc.

## Tra cứu

- [Quy chuẩn lớp, cỡ chữ, dimstyle, khung tên](references/quy-chuan-ban-ve-tcvn.md)
- [Chi tiết cấu tạo thực chiến](references/chi-tiet-cau-tao-thuc-te.md)
- Bố cục tờ giấy: skill `vn-bo-cuc-to-giay`. Soát: skill `vn-soat-ban-ve`.
