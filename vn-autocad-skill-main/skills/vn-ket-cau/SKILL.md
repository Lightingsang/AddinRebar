---
name: vn-ket-cau
description: >-
  Vẽ phần kết cấu hồ sơ thi công nhà ở chuẩn TCVN 5574:2018 bằng thư viện vncad: mặt bằng cọc ép,
  đài móng, giằng móng, dầm sàn, chi tiết bố trí thép dầm, cột, sàn, nút khung, nối thép, kết cấu
  cầu thang, và bảng thống kê cốt thép SINH TỪ KÍCH THƯỚC cấu kiện. Dùng khi vẽ hoặc sửa tờ KC,
  lập bảng thống kê thép, hoặc cần khối lượng thép cho dự toán.
---

# Vẽ kết cấu và thống kê cốt thép

Dùng chung luật bố cục và ghi chú với skill `vn-kien-truc`. Ví dụ đầy đủ:
`draw_structure.py`, `foundation_rebar.py`, `draw_rebar_detail.py`, `draw_struct_details.py`,
`draw_stair_structure.py` trong `${CLAUDE_PLUGIN_ROOT}/examples/nha_pho_5x18m`.

## Giới hạn phải nói trước

Thư viện dựng bản vẽ và thống kê thép **theo cấu tạo**, không tính nội lực. Tiết diện, thép chủ,
thông số giằng móng mà bạn tự đề xuất phải ghi vào báo cáo với nhãn "cần kỹ sư kết cấu duyệt".
Công trình nhịp lớn, có tầng hầm, nền yếu đặc biệt: nói thẳng là cần tính toán bằng phần mềm phân tích.

## Trình tự

1. Nhận kiến trúc và **địa chất** (hỏi nếu chưa có): ven sông yếu thì cọc ép 250x250.
2. Móng: đài theo số cọc, giằng móng liên hoàn khoá cứng chống lún lệch.
3. Dầm sàn từng tầng; dầm phụ **không được chạy xuyên giữa một ô sàn**: gặp thì tách ô.
4. Chi tiết thép: dầm 1/50 theo chiều dài và 1/25 mặt cắt; nút khung, nối thép 1/20 đến 1/25.
5. Bảng thống kê cốt thép sinh bằng hàm từ kích thước.

## Vẽ cấu kiện

```python
from vncad import rebar, struct

struct.pile_cap(msp, (x, y), 600, 1400, [(0, -450), (0, 450)], label="M1-CC1", label_goc=(1, 1))
struct.tie_beam(msp, (0, 0), (18000, 0), width=250, label="GM1 250x400", label_pos=giua_nhip(XS, 1))
struct.beam(msp, (x, 0), (x, 5000), width=200, label="D1 200x400", label_pos=0.72)
struct.slab_zone(msp, pts, "S1", level=3.800, thickness=100)
struct.rebar_line(msp, [(x0, y), (x1, y)], dia=10, spacing=150)   # chu dat o TRUNG DIEM tuyen
```

Luật đặt nhãn (đã gói trong hàm, nhưng phải chọn đúng tham số):

- Nhãn dầm tự đẩy ra khỏi lòng dầm theo phương vuông góc. Dầm chạy suốt nhiều nhịp: đặt nhãn
  **giữa một nhịp** (`label_pos=giua_nhip(XS, i)`), đặt giữa chiều dài là rơi đúng vào trục ngang.
- Mã hiệu đài đặt ở **góc** đài (`label_goc`), không đặt trên đỉnh: giằng chạy qua đỉnh đài.

## Bảng thống kê cốt thép: sinh từ kích thước, không gõ tay

```python
def _hang_thep_san(stt0):
    """Moi loai o san: lop duoi phuong X, phuong Y, thep mu bon canh."""
    for ten, lx, ly, so_ck in LOAI_SAN:
        nx, ny = ly // BUOC + 1, lx // BUOC + 1            # so thanh moi phuong
        yield (ten, str(stt0), "Lop duoi phuong X", 10, lx + 2 * NEO, nx, so_ck)
        ...
tong_kg, _ = struct.rebar_schedule(lay, (24, 140), REBAR_ROWS, "1/1", row_h=6.0)
```

Mỗi dòng: `(cấu kiện, hiệu thanh, hình dạng, D, dài 1 thanh mm, số thanh mỗi CK, số CK)`.
Hiệu thanh đánh số liên tục. Cấu kiện giống hệt nhau gộp một dòng và tăng số CK.

Quy tắc chiều dài dùng trong bộ mẫu:

| Việc | Quy tắc |
|---|---|
| Số thanh theo bước a | `floor(bề rộng / a) + 1` |
| Neo vào dầm, đài | 30D mỗi đầu |
| Nối chồng | 40D; thanh dài hơn cây thép thương phẩm 11,7m thì cộng mối nối |
| Móc thanh đài cọc | 10D mỗi đầu |
| Thép mũ gối sàn | vươn L/4 tính từ MÉP dầm, cộng nửa bề rộng dầm và chân móc |
| Đai dầm, giằng | chu vi trong lớp bảo vệ + 2 móc 10D; chỉ tính trong khoảng trống giữa gối |

Bảng dài hơn khoảng 14 dòng thì hạ `row_h` xuống 6mm để vẫn nằm trên mép giấy.

## Chi tiết thép

```python
rebar.beam_elevation(msp, doc, x0, x1, z_top, h=400, scale="1/50",
                     top=(3, 18), bot=(3, 18), stir=(8, 100, 200), label="D1")
rebar.beam_section(msp, doc, (cx, cy), 200, 400, top=(5, 18), bot=(3, 18),
                   stir=(8, 100), label="1-1", scale="1/25")
rebar.beam_section(msp, doc, (cx, cy), 250, 400, top=(2, 16), bot=(2, 16),
                   stir=(8, 200), label="GM", scale="1/20", cover=50)   # mong: lop bao ve 50
rebar.column_section(msp, doc, (cx, cy), 220, 220, bars=(4, 18), stir=(8, 100), scale="1/25")
```

Chú ý khi tự vẽ tuyến thép trên hình nghiêng (bản thang): đo khoảng cách **từ mặt trên** cho rõ.
Thép chịu lực chạy suốt ở **lớp dưới**; thép mũ ngắn ở **lớp trên** tại hai gối. Bộ mẫu từng vẽ
ngược hai lớp này và bộ soát tự động không bắt được.

Mặt cắt dài và mảnh (sàn dày 100, dài vài mét): cắt ngắn, đánh ký hiệu cắt hai đầu, ghi chiều
dài thật trong chú giải. Hai thanh nối chồng áp sát nhau: ở 1/25 vẽ tách ra cho đọc được và ghi
rõ "hình vẽ tách ra cho dễ đọc" trong chú giải.

## Ký hiệu cấu kiện và cách ghi thép

Đài `M1` đến `M4`, cụm cọc `CC1` đến `CC7`, cột `C1` đến `C4`, giằng `GM1` đến `GM18`,
đà kiềng `DK1`, dầm `D1`, `D2`, ô sàn `S1` đến `S6`. Ghi thép `Ø8a150`, `2Ø16`, `Ø6a100-200`;
trong DWG ký tự Ø là `%%C`.

## Cấu tạo theo TCVN 5574:2018 dùng trong bộ mẫu

- Bê tông móng B25 (M300); dầm cột sàn B20 (M250); lót đá 4x6 B7.5 dày 100.
- Thép chịu lực CB300-V; thép đai CB240-T.
- Lớp bảo vệ: móng 50mm, dầm cột 25mm, sàn 15mm, bản thang 20mm.
- Nối so le không quá 50% số thanh tại một mặt cắt; không nối trong nút khung và vùng mô men lớn.
- Đai a100 trong phạm vi nút và 2h hai đầu dầm, a200 giữa nhịp.
- Trọng lượng: W = D² / 162,2 × L (kg), trong `struct.bar_weight`.

## Soát kỹ thuật trước khi giao

- Số thanh khớp bước ghi trên hình; thép đúng lớp; lớp bảo vệ khớp ghi chú.
- Hàm lượng thép khung 20 đến 45 kg/m² sàn; thép móng 50 đến 100 kg/m³ bê tông móng.
- Cộng khối lượng toàn nhà bằng `rebar_quantities.py` (bảng một tầng điển hình nhân số tầng lặp).

## Tra cứu

- [Chi tiết cấu tạo thực chiến](../vn-kien-truc/references/chi-tiet-cau-tao-thuc-te.md)
- Dự toán lấy thép từ bảng: skill `vn-du-toan`.
