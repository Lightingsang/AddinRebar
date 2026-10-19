# MẪU PHIẾU YÊU CẦU LÀM RÕ THIẾT KẾ (RFI TEMPLATE)
## HỆ THỐNG MULTI-AGENT BIM KẾT CẤU HPBIM

---

## ĐIỀU 0: NGUYÊN TẮC VÀNG TỐI THƯỢNG (PRIME DIRECTIVE)
> **"Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."**
>
> - **Triệt tiêu hoàn toàn phỏng đoán**: Tuyệt đối cấm tự ý nội suy, đoán mò hoặc dùng thuật toán ghép nét tự do để bù đắp các thông tin còn thiếu trên hồ sơ thiết kế.
> - **Cơ chế Fail-Fast bằng code**: Nếu thiếu bất kỳ thông số bắt buộc nào (cao độ, kích thước tiết diện, điểm tựa liên kết), hệ thống code ném ngoại lệ dừng ngay đối tượng đó, đưa vào danh sách *"Chưa đủ cơ sở để kết luận"*.
> - **Không che giấu lỗi**: Mọi sai lệch, mâu thuẫn hay thiếu sót hình học/cao độ đều phải được bảo lưu ở mức cảnh báo và công khai minh bạch lên Kỹ sư Trưởng, cấm tự ý hợp thức hóa để chạy tiến độ.

---

# PHIẾU YÊU CẦU LÀM RÕ THIẾT KẾ (RFI)
### REQUEST FOR INFORMATION — STRUCTURAL BIM COORDINATION

| MÃ SỐ RFI | **RFI-HPBIM-[PHAN_DOAN]-[SỐ_THỨ_TỰ]** |
|---|---|
| **Dự án** | Hội Trường Tân Đông Hiệp (KC-HT-90-TLTT-16) |
| **Phân đoạn công trình** | [ ] Phần Móng &nbsp;&nbsp;&nbsp;&nbsp; [ ] Thân Tầng 1 &nbsp;&nbsp;&nbsp;&nbsp; [ ] Thân Tầng 2 &nbsp;&nbsp;&nbsp;&nbsp; [ ] Mái &nbsp;&nbsp;&nbsp;&nbsp; [ ] Khác |
| **Ngày phát hành** | YYYY-MM-DD (UTC ISO 8601) |
| **Người lập phiếu** | [ ] `coordinator_assistant` &nbsp;&nbsp;&nbsp;&nbsp; [ ] `cad_assistant` |
| **Người phê duyệt** | **Kỹ sư Trưởng (Chủ nhiệm Dự án / Project Chief Engineer)** |
| **Mức độ ưu tiên** | [ ] Khẩn cấp (Đóng băng toàn phân đoạn) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Cao (Đóng băng cụm cấu kiện) &nbsp;&nbsp;&nbsp;&nbsp; [ ] Trung bình |
| **Trạng thái hiện tại** | [ ] `PENDING_CHIEF_ENGINEER` &nbsp;&nbsp;&nbsp;&nbsp; [ ] `APPROVED` &nbsp;&nbsp;&nbsp;&nbsp; [ ] `REJECTED` |

---

## 1. DANH SÁCH CẤU KIỆN LIÊN QUAN

| STT | Mã Cấu Kiện (`elementId`) | Ký Hiệu (`Mark`) | Phân Loại Cấu Kiện | Nhóm Phân Loại | Trạng Thái Hiện Tại |
|:---:|---|---|---|:---:|:---:|
| 1 | `GB-GM-05-C-3-4` | `GM-5` | Giằng Móng | **Nhóm 2** (Mâu thuẫn) | `FROZEN` |
| 2 | `FT-DC-03-3-C` | `ĐC-3` | Đài Móng | **Nhóm 1** (Tạm dừng liên đới) | `FROZEN` |

---

## 2. VỊ TRÍ KHÔNG GIAN & HỆ TRỤC LIÊN ĐỚI

- **Trục định vị**: Giao điểm giữa Trục [X] và Trục [Y] (ví dụ: Trục 3 đến Trục 4, dọc Trục C).
- **Phạm vi tọa độ dự án (World Coordinates)**:
  - $X$: Từ `[X1]` mm đến `[X2]` mm
  - $Y$: Từ `[Y1]` mm đến `[Y2]` mm
  - $Z$ (Cao độ): COTE `[Z]` m
- **Tầng / Cao trình tham chiếu**: Level `[Tên Level]` (ví dụ: Level `Tầng Móng`, COTE $-1.600\text{m}$).

---

## 3. MÔ TẢ CHI TIẾT MÂU THUẪN HOẶC THIẾU SÓT (ĐỐI SOÁT 6 NGUỒN)

### 3.1. Phân loại vấn đề:
- [ ] **Nhóm 2: Mâu thuẫn thông tin giữa các bản vẽ**:
  - [ ] Mâu thuẫn giữa Mặt bằng và Mặt cắt.
  - [ ] Mâu thuẫn giữa Mặt bằng và Bảng thống kê.
  - [ ] Mâu thuẫn giữa Chi tiết gia công và Ghi chú chung.
  - [ ] Mâu thuẫn giữa Kiến trúc và Kết cấu.
- [ ] **Nhóm 3: Thiếu thông tin thiết kế**:
  - [ ] Thiếu kích thước tiết diện $b \times h$ hoặc đường kính $D$.
  - [ ] Thiếu cao độ đỉnh/đáy cấu kiện hoặc không rõ COTE giật cấp.
  - [ ] Không rõ điểm tựa liên kết 2 đầu (dầm lơ lửng, cọc không có đài).
  - [ ] Nét vẽ hình học dở dang, không khép kín polyline.

### 3.2. Bảng đối chiếu số liệu mâu thuẫn (Số liệu thực tế):

| Nguồn Dữ Liệu | Số Hiệu Tờ Bản Vẽ | Vị Trí / Handle / Block CAD | Thông Số Ghi Nhận | Nhận Xét Kỹ Thuật |
|---|---|---|---|---|
| **Nguồn A (Mặt bằng)** | Bản vẽ `KC-03` | Handle: `132A2D9`, Text: `GM-5` | Tiết diện: $250 \times 500\text{mm}$, Đỉnh $-1.600\text{m}$ | Nét vẽ thể hiện bề rộng 250mm |
| **Nguồn B (Mặt cắt/Chi tiết)** | Bản vẽ `KC-12` | Khung chi tiết dầm, Khung tên `GM-5` | Tiết diện: $250 \times 600\text{mm}$, Đỉnh $-1.600\text{m}$ | Mặt cắt ghi chiều cao h = 600mm |
| **Nguồn C (Bảng thống kê)** | Bản vẽ `KC-15` | Bảng thống kê bê tông cốt thép | Bê tông tính theo dầm $250 \times 500\text{mm}$ | Khối lượng khớp với Mặt bằng |

---

## 4. ĐÁNH GIÁ RỦI RO KỸ THUẬT & LÝ DO ÁP DỤNG ĐIỀU 0

- **Căn cứ ĐIỀU 0 (Nguyên Tắc Vàng Tối Thượng)**:
  > *"Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."*
- **Rủi ro nếu tự ý phỏng đoán phương án**:
  - Nếu tự chọn $h = 500\text{mm}$: Có thể thiếu khả năng chịu lực uốn/cắt theo tính toán kết cấu của Kỹ sư thiết kế.
  - Nếu tự chọn $h = 600\text{mm}$: Có thể va chạm với hệ thống đường ống kỹ thuật ngầm hoặc sai lệch khối lượng bê tông/ván khuôn.
  - Do đó, việc dừng dựng hình và đóng băng cụm cấu kiện là bắt buộc để đảm bảo an toàn tuyệt đối.

---

## 5. ĐỀ XUẤT PHƯƠNG ÁN XỬ LÝ (KIẾN NGHỊ CỦA BỘ PHẬN KỸ THUẬT)

Bộ phận Kỹ thuật CAD & Điều phối đề xuất 2 phương án để Kỹ sư Trưởng xem xét:

- **Phương án 1**: Áp dụng tiết diện $250 \times 500\text{mm}$ theo Mặt bằng và Bảng thống kê; hiệu chỉnh bản vẽ chi tiết `KC-12`.
- **Phương án 2**: Áp dụng tiết diện $250 \times 600\text{mm}$ theo Bản vẽ chi tiết `KC-12`; cập nhật lại Mặt bằng `KC-03` và khối lượng thống kê `KC-15`.
- **Kiến nghị chọn**: Kỹ sư CAD kiến nghị chọn **Phương án 1** vì phù hợp với mặt bằng chung của hệ giằng trục ngang $250 \times 500$.

---

## 6. Ý KIẾN PHÊ DUYỆT CỦA KỸ SƯ TRƯỞNG (PROJECT CHIEF ENGINEER)

*(Phần dành riêng cho Kỹ sư Trưởng ghi nhận chỉ đạo và ký duyệt)*

- [ ] **CHẤP THUẬN PHƯƠNG ÁN ĐỀ XUẤT**: Chấp thuận Phương án số: `[.....]`
- [ ] **CHỈ ĐỊNH PHƯƠNG ÁN KHÁC**:
  ```text
  ....................................................................................................
  ....................................................................................................
  ....................................................................................................
  ```
- [ ] **YÊU CẦU ĐƠN VỊ THIẾT KẾ PHÁT HÀNH BẢN VẼ SỬA ĐỔI (REVISION)**: Mã bản vẽ mới: `[...........]`

**Chữ ký Kỹ sư Trưởng**:  
*Họ và tên*: ............................................................  
*Ngày phê duyệt*: ....................................................  

---

## 7. CẬP NHẬT SỔ CÁI ĐIỀU PHỐI (LEDGER SYNC)

Sau khi nhận được phê duyệt chính thức của Kỹ sư Trưởng:
1. Kỹ sư Chủ trì cập nhật trường `rfis` trong `state/bim_coordination_ledger.json` sang `status: "APPROVED"`.
2. Chuyển cấu kiện từ `Group2_ConflictNeedRfi` sang `Group1_ReadyToModel`.
3. Mở khóa phân đoạn (chuyển trạng thái từ `FROZEN` sang `IN_PROGRESS`).
4. Giao lệnh cho `revit_assistant` tiến hành dựng hình theo đúng phương án đã duyệt.
