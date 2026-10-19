# BIÊN BẢN NGHIỆM THU ĐỘC LẬP PHÂN ĐOẠN PHẦN MÓNG
**Dự án:** Hội trường Tân Đông Hiệp (KC-HT-90-TLTT-16.dwg)  
**Mô hình Revit:** TemplateAIModel.rvt  
**Thời gian kiểm toán:** 2026-10-07 15:44:34 UTC  
**Đơn vị kiểm toán:** Kỹ sư QA/QC Độc lập (100% C# .NET Engine)  
**Nguyên tắc áp dụng:** ĐIỀU 0 — Tuyệt đối không phỏng đoán, kiểm toán hình học chân thực.

---

## 1. Kết Quả Kiểm Toán 8 Bước

| STT | Hạng mục kiểm toán | Tiêu chuẩn dung sai | Kết quả thực tế | Đánh giá |
| :--- | :--- | :--- | :--- | :--- |
| Bước 1 | Hệ trục & Cao trình | 27 trục, 10 cao trình, ghim 100%, sai số <= 1.0mm | Hệ trục/cao độ có 2 lỗi sai lệch vượt dung sai 1.0mm. | SAI, CẦN SỬA |
| Bước 2 | Số lượng cấu kiện & Dầm ma | 196 cọc, 72 đài móng, 188-192 giằng, 0 dầm ma | Sai lệch số lượng cấu kiện: Số giằng móng thực tế 477 ngoài phạm vi 188-192. | SAI, CẦN SỬA |
| Bước 3 | Vị trí & Tọa độ mặt bằng | Sai số tim cọc, trọng tâm đài <= 2.0mm | 100% tọa độ tim cọc và trọng tâm đài móng khớp CAD, sai số <= 2.0mm. | ĐÚNG |
| Bước 4 | Kích thước tiết diện | Sai số kích thước b x h = 0.0mm | 100% kích thước tiết diện đài móng và giằng móng khớp thiết kế, sai số 0.0mm. | ĐÚNG |
| Bước 5 | Cao độ & Cao trình ngàm | Đỉnh đài/dầm -1600mm, đầu cọc ngàm 100mm | Có 289 cấu kiện sai lệch cao trình / độ ngàm cọc. | SAI, CẦN SỬA |
| Bước 6 | Quan hệ kết cấu (Bao hàm & Tựa) | 100% cọc trong đài (khoảng hở >= 150mm), dầm liên tục | Quan hệ kết cấu không đạt: 0 lỗi cọc, 14 lỗi gác dầm. | SAI, CẦN SỬA |
| Bước 7 | Đối soát chi tiết CAD | Khớp chi tiết bản vẽ móng ĐC-1 đến ĐC-6 | Đối soát chéo chi tiết mặt cắt CAD: Khớp 100% chi tiết đài ĐC-1 đến ĐC-6. | ĐÚNG |
| Bước 8 | Cập nhật sổ cái điều phối | Đồng bộ 100% vào bim_coordination_ledger.json | Sẵn sàng ghi nhận vào Sổ cái và xuất Biên bản. | ĐÚNG |

---

## 2. Kết Luận & Quyết Định Nghiệm Thu
- **Trạng thái:** **YÊU CẦU KHẮC PHỤC (REQUEST CHANGES)**
- **Đóng băng phân đoạn (Freeze Zone):** Tạm dừng, chưa đóng băng phân đoạn.