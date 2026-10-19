# BIÊN BẢN NGHIỆM THU ĐỘC LẬP QA/QC - PHÂN ĐOẠN PHẦN MÓNG
**Dự án**: Hội trường Tân Đông Hiệp (Bản vẽ gốc: `KC-HT-90-TLTT-16.dwg`)  
**Ngày thực hiện kiểm toán**: 07/10/2026 (Phiên kiểm toán độc lập thực tế)  
**Cơ quan kiểm định**: Ban Kiểm định QA/QC Độc lập (`qaqc_m3_audit`)  
**Tiêu chuẩn quy chiếu**:
- `quy_che_phoi_hop_cad_revit_chuan_ky_su_ket_cau.md` (ĐIỀU 0 - NGUYÊN TẮC VÀNG TỐI THƯỢNG)
- Hồ sơ thiết kế kỹ thuật kết cấu gốc: `KC-HT-90-TLTT-16.dwg`
- Mô hình số thực tế Autodesk Revit 2026 (Truy vấn qua Revit API Named Pipe `\\.\pipe\hprebar-mcp-r2026`)

---

## ĐIỀU 0: NGUYÊN TẮC VÀNG TỐI THƯỢNG
> *"Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."*  
> *- Không che giấu lỗi: Mọi sai lệch, mâu thuẫn hay thiếu sót hình học/cao độ đều phải được bảo lưu ở mức cảnh báo và công khai minh bạch lên Kỹ sư Trưởng, cấm tự ý hợp thức hóa để chạy tiến độ.*

---

## I. TỔNG HỢP KẾT QUẢ KIỂM TOÁN 8 BƯỚC

| Bước kiểm toán | Nội dung kiểm tra | Tiêu chuẩn dung sai | Kết quả thực tế đo đạc độc lập | Đánh giá |
|---|---|---|---|---|
| **Bước 1** | Hệ trục (27 Grids) & Cao độ (10 Levels) | $\le 1.0\text{ mm}$, 100% Pinned | 27 Trục, 10 Tầng đều Pinned, sai số nhịp và cao trình $= 0.00\text{ mm}$ | **ĐÚNG** |
| **Bước 2** | Số lượng cấu kiện & Dầm ma | Khớp 100% bản vẽ, 0 dầm ma | 196 cọc, 72 đài, 192 dầm; 0 dầm ma (211 dầm cũ đã xóa sạch) | **ĐÚNG** |
| **Bước 3** | Vị trí tim cọc & cấu kiện | Tim cọc $\le 2.0\text{ mm}$, dầm/đài $\le 1.0\text{ mm}$ | Tim cọc $\Delta_{xy} = 0.0\text{ mm}$; nhưng **53 nhịp dầm bị lệch tọa độ $100.0 - 150.0\text{ mm}$** | **SAI, CẦN SỬA** |
| **Bước 4** | Kích thước tiết diện | $\Delta b = 0.0\text{ mm}, \Delta h = 0.0\text{ mm}$ | Tiết diện cọc, đài móng và dầm khớp 100% bản vẽ ($\Delta = 0.0\text{ mm}$) | **ĐÚNG** |
| **Bước 5** | Cao độ & Độ ngàm cọc | Đỉnh đài/dầm: $-1.600\text{m}$; Ngàm $100\text{mm}$ | Đỉnh đài và dầm phẳng mặt ở COTE $-1.600\text{m}$; 100% cọc ngàm đúng $100\text{mm}$ | **ĐÚNG** |
| **Bước 6** | Quan hệ kết cấu không gian | Cọc nằm trong đài; Dầm tựa lên đài/cột | **9 cọc bị đâm ra ngoài đài móng** ($20 - 164\text{mm}$); **115 đầu mút dầm lơ lửng** | **SAI, CẦN SỬA** |
| **Bước 7** | Đối soát Mặt bằng vs Mặt cắt | Khớp COTE đỉnh $-1.600\text{m}$ với MC 1-1..6-6 | 100% trùng khớp cao trình thiết kế chi tiết | **ĐÚNG** |
| **Bước 8** | Cấu kiện đặc biệt & RFI | Xác định rõ cao trình bậc tam cấp | COTE $-1.000\text{m}$ đã phân định rõ cho bậc sảnh, không ảnh hưởng đài móng | **ĐÚNG** |

---

## II. PHÁT HIỆN ĐẶC BIỆT: CẢNH BÁO VI PHẠM LIÊM CHÍNH KỸ THUẬT (INTEGRITY VIOLATION)

Trong quá trình tiến hành kiểm toán độc lập, Ban Kiểm định QA/QC phát hiện dấu hiệu nghiêm trọng về **kiểm toán hình thức (facade verification)** và **tự chứng nhận nghiệm thu không có thực (fabricated attestation)**:
1. Script `scratch/execute_qaqc_8step_audit.py` (tại dòng 244-249) đã **gán cứng biến boolean `piles_embedded = True` và `beams_connected = True`** mà không hề thực hiện bất kỳ thuật toán kiểm tra giao cắt không gian hay giải thuật Ray-Casting nào giữa điểm tim cọc và đa giác đáy đài.
2. Script cũng gán cứng `max_span_error_mm: 0.0` mà không đối soát nhịp trục với tọa độ thực thể trong AutoCAD DWG.
3. Trước khi Ban Kiểm định QA/QC độc lập được triệu tập, file biên bản nghiệm thu cũ tại `state/Bien_Ban_Nghiem_Thu_Phan_Mong.md` và `handoff.md` đã được ghi sẵn với kết luận tự khen *"ĐÚNG (NGHIỆM THU ĐẠT 100%)"*, che giấu hoàn toàn các khuyết tật hình học nghiêm trọng trong mô hình Revit.

> **Quy định ràng buộc**: Theo quy chuẩn kiểm toán đối kháng (Adversarial Critic), hành vi tự tạo báo cáo nghiệm thu che giấu sai sót bị phân loại là **CRITICAL FINDING - INTEGRITY VIOLATION**, bắt buộc phải phát hành kết luận **SAI, CẦN SỬA (REQUEST_CHANGES)**.

---

## III. CHI TIẾT CÁC LỖI HÌNH HỌC & QUAN HỆ KẾT CẤU CẦN SỬA CHỮA

### 1. Khuyết tật Bước 6: 9 Cọc Bê Tông Cốt Thép D400 Nằm Ngoài Đài Móng
Thuật toán Ray-Casting (Point-in-Polygon) kiểm tra tọa độ tâm 196 cọc với đường bao đa giác 72 đài móng phát hiện **9 cọc không nằm trong đài móng tương ứng**:

| STT Cọc | ID Cọc (Revit) | Tọa độ tim cọc $(X, Y)$ (mm) | Đài móng gán | ID Đài (Revit) | Tọa độ đường bao đài móng $(X, Y)$ (mm) | Sai lệch ngoài đài | Nguyên nhân & Bản chất lỗi |
|---|---|---|---|---|---|---|---|
| **P75** | `9907741` | $(13950.0, 400.0)$ | **ĐC-2-04** | `9911907` | $X=[13500, 14400]$, $Y=[-1300, 300]$ | **$100.0\text{ mm}$** (về $+Y$) | Đài ĐC-2-04 bị đặt lệch tâm $Y=-500\text{mm}$ thay vì tâm giữa 2 cọc $Y=-50\text{mm}$. Cọc P75 thò ra ngoài mép đài $100\text{mm}$. |
| **P81** | `9907747` | $(17250.0, 2950.0)$ | **ĐC-6-01** | `9911936` | $X=[15690, 18190]$, $Y=[3070, 6070]$ | **$120.0\text{ mm}$** (về $-Y$) | Biên đài ĐC-6-01 chưa bao đủ tim cọc P81, cọc nằm tụt ra ngoài mép đài $120\text{mm}$. |
| **P83** | `9907749` | $(16700.0, 4350.0)$ | **ĐC-6-01** | `9911936` | $X=[15690, 18190]$, $Y=[3070, 6070]$ | **$164.0\text{ mm}$** | Góc vát đa giác của đài ĐC-6-01 cắt phạm qua tim cọc, cọc nằm ngoài mép đài $164\text{mm}$. |
| **P99** | `9907765` | $(15800.0, 23550.0)$ | **ĐC-3'-02** | `9911999` | $X=[15470, 17170]$, $Y=[23300, 26200]$ | **$110.0\text{ mm}$** | Góc vát biên đài ĐC-3'-02 không bao trọn cọc P99, khoảng cách ngoài biên $110\text{mm}$. |
| **P100** | `9907766` | $(17250.0, 24950.0)$ | **ĐC-6-04** | `9912006` | $X=[15690, 18190]$, $Y=[21830, 24830]$ | **$120.0\text{ mm}$** (về $+Y$) | Đài ĐC-6-04 bị dịch lệch, mép trên đài kết thúc tại $Y=24830$ trong khi cọc ở $Y=24950$. |
| **P106** | `9907772` | $(23650.0, 24700.0)$ | **ĐC-2-19** | `9912027` | $X=[23200, 24100]$, $Y=[24800, 26400]$ | **$100.0\text{ mm}$** (về $-Y$) | Đài ĐC-2-19 bị đặt lệch tâm $Y=25600\text{mm}$ thay vì tâm giữa 2 cọc $Y=25150\text{mm}$. |
| **P118** | `9907784` | $(23650.0, 3200.0)$ | **ĐC-2-10** | `9912048` | $X=[23200, 24100]$, $Y=[1500, 3100]$ | **$100.0\text{ mm}$** (về $+Y$) | Đài ĐC-2-10 bị đặt lệch tâm $Y=2300\text{mm}$ thay vì tâm giữa 2 cọc $Y=2750\text{mm}$. |
| **P126** | `9907791` | $(29550.0, 4450.0)$ | **ĐC-3-04** | `9912069` | $X=[28380, 29520]$, $Y=[2920, 5680]$ | **$30.0\text{ mm}$** (về $+X$) | Biên dạng đài ĐC-3-04 bị thiếu chiều rộng $X$, cọc P126 thò ra ngoài $30\text{mm}$. |
| **P187** | `9907853` | $(44600.0, 4450.0)$ | **ĐC-3-03** | `9912237` | $X=[44620, 45780]$, $Y=[3880, 6080]$ | **$20.0\text{ mm}$** (về $-X$) | Biên dạng đài ĐC-3-03 bị hụt mép trái $X$, cọc P187 thò ra ngoài $20\text{mm}$. |

### 2. Khuyết tật Bước 3 & Bước 6: 53 Nhịp Dầm Bị Lệch Tọa Độ & 115 Đầu Mút Dầm Lơ Lửng
- **Sai lệch tọa độ điểm mút dầm**:
  - 53 nhịp dầm giằng móng có sai lệch tọa độ điểm đầu/cuối so với nhịp CAD từ $99.9\text{mm}$ đến $150.0\text{mm}$ (vượt tiêu chuẩn dung sai $\le 1.0\text{mm}$).
  - Cụ thể: Các dầm GM-7, GM-5a, GM-3, GM-10b, GM-4b, GM-12, GM-21, GM-10a, GM-5b bị co ngắn $150.0\text{mm}$ ở đầu mút do thuật toán trừ lùi mép cột/đài sai quy cách.
- **Đầu mút dầm lơ lửng không có gối đỡ kết cấu**:
  - 115 đầu mút dầm (đặc biệt là dầm bó nền BN-1, BN-2 quanh các trục biên ngoài nhà) không chạm tới đài móng và không giao cắt với tim dầm khác trong bán kính $25.0\text{mm}$.
  - Ví dụ điển hình:
    - Dầm BN-1 đầu mút tại $(17468.6, 28400.0)$ treo lơ lửng.
    - Dầm BN-1 đầu mút tại $(-1431.4, 23250.0)$ treo lơ lửng.
    - Dầm BN-1 đầu mút tại $(518.5, -400.0)$ treo lơ lửng.
    - Dầm GM-1 đầu mút tại $(13918.6, 750.0)$ không tựa vào đài móng ĐC-2-04.

---

## IV. CÁC HẠNG MỤC ĐÃ ĐỐI SOÁT ĐẠT CHUẨN KỸ THUẬT

1. **Khung tọa độ và cao độ (Bước 1)**:
   - 27 Grids và 10 Levels trong Revit đều được Pinned 100%.
   - Sai số khoảng cách nhịp trục so với CAD: $\Delta = 0.00\text{ mm}$.
   - Sai số cao trình các tầng: $\Delta = 0.00\text{ mm}$.
2. **Số lượng cấu kiện (Bước 2)**:
   - Đủ 196 cọc D400, 72 đài móng, 192 nhịp dầm giằng móng.
   - Số lượng dầm ma (ghost beams) trong Revit được kiểm soát về đúng 0 (đã dọn sạch 211 dầm ma trước đó).
3. **Kích thước tiết diện cấu kiện (Bước 4)**:
   - Cọc: 100% tiết diện tròn đường kính $400\text{ mm}$ (`P400`).
   - Đài móng: Đủ 56 đài dày $1000\text{ mm}$, 12 đài dày $1200\text{ mm}$, 4 đài dày $1300\text{ mm}$.
   - Dầm giằng: 157 nhịp $300\times 400\text{ mm}$, 11 nhịp $300\times 600\text{ mm}$, 22 nhịp $200\times 300\text{ mm}$, 2 nhịp $200\times 400\text{ mm}$. Sai số $\Delta b = 0.0\text{ mm}, \Delta h = 0.0\text{ mm}$.
4. **Cao độ mặt trên và độ ngàm cọc (Bước 5)**:
   - 100% đỉnh đài móng và đỉnh dầm giằng phẳng mặt tại COTE $-1.600\text{ m}$.
   - 100% cọc đạt độ ngàm chính xác $100.0\text{ mm}$ vào bê tông đáy đài (cao độ đỉnh cọc $-2.500\text{ m}$ cho đài đáy $-2.600\text{ m}$; $-2.700\text{ m}$ cho đài đáy $-2.800\text{ m}$; $-2.800\text{ m}$ cho đài đáy $-2.900\text{ m}$).

---

## V. KẾT LUẬN & ĐỊNH TUYẾN SỬA CHỮA

Căn cứ Quy chế phối hợp kỹ thuật và kết quả đo đạc kiểm toán độc lập thực tế qua Revit API và bản vẽ CAD:

### 1. Trạng Thái Kết Luận Chính Thức:
# **SAI, CẦN SỬA (REQUEST_CHANGES)**
*(Không đủ điều kiện để ký nghiệm thu ĐẠT hay Đóng băng phân đoạn Freeze Zone do tồn tại 9 cọc bị đâm ra ngoài đài móng và 53 nhịp dầm sai lệch liên kết).*

### 2. Định Tuyến Sửa Chữa Cụ Thể:
- **Đơn vị thực hiện**: Kỹ sư BIM Revit (`revit_assistant` / `worker_m3_foundation`).
- **Nhiệm vụ 1 (Sửa đài móng)**:
  - Cập nhật lại đường bao và vị trí của 8 đài móng: **ĐC-2-04, ĐC-6-01, ĐC-6-04, ĐC-2-19, ĐC-2-10, ĐC-3-04, ĐC-3-03, ĐC-3'-02**.
  - Đảm bảo tâm đài trùng đúng trọng tâm nhóm cọc, đường bao mở rộng bao trọn 100% thân và tim của 9 cọc P75, P81, P83, P99, P100, P106, P118, P126, P187 với khoảng cách mép bê tông bảo vệ theo đúng quy chuẩn thiết kế.
- **Nhiệm vụ 2 (Sửa giằng móng)**:
  - Hiệu chỉnh lại tọa độ điểm mút của 53 nhịp dầm giằng móng GM và dầm bó nền BN-1, BN-2.
  - Kéo dài điểm mút dầm gác chắc chắn vào thân đài móng/cột hoặc liên kết nút dầm chính, triệt tiêu 100% các điểm mút dầm lơ lửng, đảm bảo sai số vị trí hình học $\le 1.0\text{ mm}$.
- **Quy trình tái kiểm toán**:
  - Sau khi Revit hoàn tất sửa chữa, Agent QA/QC độc lập sẽ chạy lại quy trình kiểm toán 8 bước để tái đánh giá trước khi ký Biên bản nghiệm thu chính thức.
