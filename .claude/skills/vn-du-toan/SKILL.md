---
name: vn-du-toan
description: >-
  Bóc khối lượng và lập dự toán chi phí xây dựng chuẩn Việt Nam, xuất Excel 100% phông Times New
  Roman bằng vncad.estimate, trong đó khối lượng LẤY TỪ CHÍNH BẢNG THỐNG KÊ TRÊN BẢN VẼ chứ không
  gõ tay. Dùng khi lập hoặc sửa dự toán, bóc khối lượng thép, bê tông, ván khuôn, xây tô, ốp lát,
  ước tính nhanh theo m² sàn, hoặc khi dự toán và bản vẽ lệch nhau.
---

# Dự toán gắn với bản vẽ

## Nguyên tắc số một: khối lượng không gõ tay

Bộ mẫu từng có dự toán gõ tay 13.700 kg thép khung trong khi bản vẽ chỉ ra 7.217,6 kg; thép móng
3.200 so với 748,9 kg; bê tông móng 38 m³ so với khoảng 12 m³. Dự toán là con số người dùng mang
đi vay vốn và ký hợp đồng, nên:

- Dòng nào có bảng thống kê trên bản vẽ thì **lấy số từ bảng bằng code**, và ghi nguồn ngay trong
  nội dung dòng ("theo bảng thống kê KC-02 x 3 tầng").
- Dòng nào chưa có căn cứ trên bản vẽ thì ghi **"SỐ ƯỚC"** trong nội dung dòng và nêu giả thiết.
- Không bao giờ nối thẳng một con số từ bảng chưa đầy đủ: soát bảng trước (skill `vn-ket-cau`).

## Lấy thép từ bản vẽ

Mẫu `rebar_quantities.py`:

```python
from vncad.struct import bar_weight

SO_TANG_DIEN_HINH = 3      # san tang 2, tang 3, san thuong cung mat bang ket cau
SO_DOAN_THANG = 3
D_RANH_GIOI = 10           # tach hai nhom dinh muc: D <= 10 va D > 10

def theo_duong_kinh(rows, he_so=1):
    nho = lon = 0.0
    for _, _, _, d, dai, sl, so in rows:
        kg = bar_weight(d, dai / 1000.0 * sl * so) * he_so
        nho, lon = (nho + kg, lon) if d <= D_RANH_GIOI else (nho, lon + kg)
    return nho, lon
```

Trong `build_dutoan.py`:

```python
THEP_NHO, THEP_LON = thep_than_nha()
("AF.622", "Cot thep dam cot san CB300-V (D lon hon 10mm), " + NGUON, "kg", round(THEP_LON, 1), 18500),
("AF.623", "Cot thep san va thep dai CB240-T (D tu 10mm tro xuong), " + NGUON, "kg", round(THEP_NHO, 1), 18000),
```

Chú ý nhãn nhóm đường kính: "D nhỏ hơn 10mm" bỏ sót đúng loại Ø10 dùng nhiều nhất; ghi
"D từ 10mm trở xuống".

## Xuất Excel

```python
from vncad.estimate import build_estimate
rows = [("SECTION", "PHAN I: PHAN NGAM VA MONG"),
        ("AB.111", "Ep coc BTCT 250x250, chieu sau ep 25m", "m dai", 750, 280000), ...]
build_estimate("out/DT-01.xlsx", project, rows, vat=0.08, overhead=0.06, contingency=0.05)
```

Module tự lo: phông Times New Roman toàn bộ, định dạng `#,##0`, công thức `=E*F` và `=SUM`, chi phí
chung, dự phòng, thuế GTGT. Không tự viết lại openpyxl; không gõ số vào ô thành tiền.

## Soát trước khi giao

- Đọc ngược file Excel bằng openpyxl: dòng thép bằng đúng số của `rebar_quantities`.
- Hàm lượng: thép khung 20 đến 45 kg/m² sàn; thép móng 50 đến 100 kg/m³ bê tông móng.
- Soát chéo bê tông, ván khuôn với kích thước cấu kiện trên bản vẽ; lệch lớn thì báo người dùng.
- Còn dòng nào gõ tay thì liệt kê trong báo cáo.

## Hai cách ước tính

1. **Bóc chi tiết theo đầu việc** khi đã có bản vẽ: khung đầu việc ở
   [references/danh-muc-cong-tac.md](references/danh-muc-cong-tac.md).
2. **Suất vốn trên m² sàn** khi mới có ý tưởng: hệ số quy đổi và đơn giá ở
   [references/don-gia-thi-truong.md](references/don-gia-thi-truong.md). Nói rõ sai số 10 đến 15%.

## Trung thực về con số

- Báo ba mức: thấp, giữa, cao. Một con số duy nhất luôn sai.
- Ghi mốc thời gian của đơn giá; nhắc hỏi báo giá nhà thầu trước khi ký hợp đồng.
- Khi số mới làm tổng giảm hay tăng mạnh, nói rõ chênh bao nhiêu và vì sao, trước khi người dùng
  tự phát hiện.

Căn cứ pháp lý thường dùng: Nghị định 10/2021/NĐ-CP về quản lý chi phí đầu tư xây dựng, Thông tư
12/2021/TT-BXD ban hành định mức. Kiểm tra văn bản còn hiệu lực tại thời điểm lập dự toán.
