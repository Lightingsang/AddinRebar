# Checklist bàn giao

Đánh dấu từng dòng trước khi báo "xong". Dòng nào không đạt thì hoặc sửa, hoặc ghi vào mục
"còn treo" của báo cáo. Không bỏ qua lặng lẽ.

## Bộ hồ sơ

- [ ] Một file DXF chứa cả bộ, mỗi tờ một Layout; không còn `Layout1`, `Layout2` mặc định
- [ ] Số tờ trong file khớp danh mục; AutoCAD đếm lại đúng số đó sau khi chuyển DWG
- [ ] Mỗi tờ có khung tên đủ trường, số hiệu, tên tờ, **tỷ lệ ghi đủ mọi tỷ lệ có trên tờ**
- [ ] Mặt bằng có kích thước 3 lớp, bong bóng trục, nhãn phòng kèm diện tích và cao độ
- [ ] Cao độ ghi 3 chữ số thập phân
- [ ] Ghi chú kỹ thuật là câu lệnh thi công cụ thể, không phải câu chung chung

## Soát

- [ ] `tools/soat-ban-ve-khong-render.py` báo SACH
- [ ] Đã render và nhìn mọi tờ mới hoặc vừa sửa (ảnh tổng 150 dpi, cắt chi tiết 400 dpi)
- [ ] Chữ trong đường kích thước không đè chữ khác (bộ soát không đọc được chữ này)
- [ ] Thép chịu lực đúng lớp (bản thang, sàn: lớp dưới; thép mũ: lớp trên tại gối)
- [ ] Số thanh trong bảng thống kê khớp bước thép ghi trên hình
- [ ] Lớp bảo vệ trên hình khớp ghi chú (móng 50, dầm cột 25, sàn 15)
- [ ] Hàm lượng thép khung 20 đến 45 kg/m² sàn; thép móng 50 đến 100 kg/m³ bê tông

## Dự toán

- [ ] Mọi dòng thép ghi nguồn "theo bảng thống kê ..."
- [ ] Dòng chưa có căn cứ trên bản vẽ có chữ "SỐ ƯỚC" và nêu giả thiết
- [ ] Toàn bộ phông Times New Roman; ô thành tiền là công thức, không gõ số
- [ ] Đã soát chéo khối lượng bê tông với kích thước trên bản vẽ

## Mẫu báo cáo bàn giao

```
**Status:** DONE | DONE_WITH_CONCERNS | BLOCKED | NEEDS_CONTEXT
**Bộ hồ sơ:** <số tờ> tờ (KT x, KC y, ME z)   **Soát:** SACH
**Thép:** khung <kg> (<kg/m² sàn>), móng <kg>
**Dự toán:** chi phí trực tiếp <triệu đồng>; các dòng SỐ ƯỚC: <liệt kê>
**File:** <đường dẫn tuyệt đối DXF, DWG, PDF, XLSX>
**Giả thiết cần kỹ sư duyệt:** <liệt kê từng thông số tự đề xuất>
**Còn treo:** <việc chưa làm và lý do>
```
