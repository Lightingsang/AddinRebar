# KIẾN TRÚC HỆ THỐNG MULTI-AGENT BIM KẾT CẤU HPBIM
## TÀI LIỆU QUẢN TRỊ KỸ THUẬT & HỒ SƠ PHỐI HỢP BỘ TỨ AGENT

---

## ĐIỀU 0: NGUYÊN TẮC VÀNG TỐI THƯỢNG (PRIME DIRECTIVE)
> **"Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."**
>
> - **Triệt tiêu hoàn toàn phỏng đoán**: Tuyệt đối cấm tự ý nội suy, đoán mò hoặc dùng thuật toán ghép nét tự do để bù đắp các thông tin còn thiếu trên hồ sơ thiết kế.
> - **Cơ chế Fail-Fast bằng code**: Nếu thiếu bất kỳ thông số bắt buộc nào (cao độ, kích thước tiết diện, điểm tựa liên kết), hệ thống code ném ngoại lệ dừng ngay đối tượng đó, đưa vào danh sách *"Chưa đủ cơ sở để kết luận"*.
> - **Không che giấu lỗi**: Mọi sai lệch, mâu thuẫn hay thiếu sót hình học/cao độ đều phải được bảo lưu ở mức cảnh báo và công khai minh bạch lên Kỹ sư Trưởng, cấm tự ý hợp thức hóa để chạy tiến độ.

---

## 1. TỔNG QUAN HỆ SINH THÁI 4 AGENT CHUYÊN TRÁCH

Hệ thống Multi-Agent HPBim được thiết kế mô phỏng chính xác phòng kỹ thuật kết cấu và BIM chuẩn mực của một tổng thầu / đơn vị tư vấn thiết kế hàng đầu, bao gồm 4 Agent chuyên trách độc lập:

```
                               ┌────────────────────────────────────────┐
                               │       KỸ SƯ TRƯỞNG (CHỦ NHIỆM)         │
                               │        (PROJECT CHIEF ENGINEER)        │
                               └──────────────────┬─────────────────────┘
                                                  │ Phê duyệt RFI / Nghiệm thu
                                                  ▼
                               ┌────────────────────────────────────────┐
                               │   KỸ SƯ CHỦ TRÌ ĐIỀU PHỐI (M1)         │
                               │       (coordinator_assistant)          │
                               │  - Quản trị Vòng đời 7 bước            │
                               │  - Sổ cái bim_coordination_ledger.json │
                               │  - Phân đoạn & Freeze Zone             │
                               └──────┬──────────────────────────┬──────┘
                                      │                          │
                 Phân công đọc DWG    │                          │ Phân công dựng Revit (Nhóm 1)
                                      ▼                          ▼
       ┌────────────────────────────────────────┐      ┌────────────────────────────────────────┐
       │     KỸ SƯ KẾT CẤU CAD (M1)             │      │       KỸ SƯ BIM REVIT (M1)             │
       │         (cad_assistant)                │      │        (revit_assistant)               │
       │  - Rà soát 2 giai đoạn                 │      │  - Chỉ dựng Nhóm 1 duy nhất            │
       │  - Đối soát chéo 6 nguồn dữ liệu       │      │  - Tự kiểm tra 2 pha tự động           │
       │  - Phân loại 3 nhóm (1, 2, 3)          │      │  - Rollback khi sai số > 1.0mm         │
       └──────────────────┬─────────────────────┘      └──────────────────┬─────────────────────┘
                          │                                               │
                          │ Dữ liệu DWG gốc                               │ Dữ liệu Revit thực tế
                          └───────────────────────┬───────────────────────┘
                                                  │
                                                  ▼
                               ┌────────────────────────────────────────┐
                               │      KỸ SƯ QA/QC ĐỘC LẬP (M1)          │
                               │          (qaqc_assistant)              │
                               │  - Kiểm toán độc lập 8 bước            │
                               │  - 3 trạng thái kết luận bắt buộc      │
                               │  - Định tuyến lỗi tự động              │
                               └────────────────────────────────────────┘
```

---

## 2. MA TRẬN PHÂN ĐỊNH TRÁCH NHIỆM (RACI MATRIX)

| Giai đoạn & Nhiệm vụ Kỹ thuật | Kỹ sư Chủ trì (`coordinator`) | Kỹ sư Kết cấu CAD (`cad`) | Kỹ sư BIM Revit (`revit`) | Kỹ sư QA/QC (`qaqc`) | Kỹ sư Trưởng (Dự án) |
|---|:---:|:---:|:---:|:---:|:---:|
| **Quản trị Sổ cái Ledger** (`bim_coordination_ledger.json`) | **A / R** | C | C | C | I |
| **Phân đoạn & Kích hoạt Freeze Zone** | **A / R** | C | C | C | I |
| **Đọc DWG & Đối soát chéo 6 nguồn** | I | **A / R** | I | C | I |
| **Phân loại 3 Nhóm Cấu kiện** (Nhóm 1, 2, 3) | A | **R** | I | C | I |
| **Phát hành Phiếu Yêu Cầu Làm Rõ** (`rfi_template.md`) | **A / R** | R | I | C | **Phê duyệt** |
| **Purge sạch 211 dầm ma trong Revit** | A | I | **R** | C | I |
| **Hạ cao độ 196 cọc D400 ngàm 100mm** | A | C | **R** | C | I |
| **Dựng hình Revit & Tự kiểm tra 2 pha** | A | I | **A / R** | I | I |
| **Kiểm toán Độc lập 8 bước (DWG vs Revit)** | I | C | C | **A / R** | I |
| **Định tuyến Sửa lỗi (CadFix / RevitFix)** | **A** | R (nếu CAD) | R (nếu Revit) | **R (định tuyến)** | I |
| **Nghiệm thu hoàn thành Phân đoạn** | R | C | C | **A / R** | **Xác nhận** |

*(Ghi chú: **R** = Responsible - Người thực hiện chính; **A** = Accountable - Người chịu trách nhiệm phê duyệt; **C** = Consulted - Người được tham vấn; **I** = Informed - Người được thông báo).*

---

## 3. VÒNG ĐỜI 7 BƯỚC CỦA MỖI CẤU KIỆN KẾT CẤU

Mọi cấu kiện kết cấu từ móng đến mái bắt buộc phải trải qua đầy đủ 7 bước trạng thái:

1. **Bước 1 (`Step1_ReadCad`)**: Đọc thực thể hình học từ bản vẽ AutoCAD DWG (Handle, layer, tọa độ, kích thước).
2. **Bước 2 (`Step2_VerifiedInfo`)**: Đối soát chéo 6 nguồn dữ liệu. Phân loại cấu kiện:
   - **Nhóm 1**: Đủ thông tin duy nhất $\rightarrow$ Chuyển giao sang Revit.
   - **Nhóm 2**: Mâu thuẫn thông tin $\rightarrow$ CẤM DỰNG. Đóng băng, lập RFI.
   - **Nhóm 3**: Thiếu thông tin $\rightarrow$ CẤM DỰNG. Bảo lưu danh sách "Chưa đủ cơ sở để kết luận", lập RFI.
3. **Bước 3 (`Step3_ModelRevit`)**: Dựng đối tượng trong Revit và tự kiểm tra 2 pha ngay sau khi dựng.
4. **Bước 4 (`Step4_QaqcAudited`)**: QA/QC kiểm toán độc lập 8 bước qua API.
5. **Bước 5 (`Step5_DefectFixing`)**: Định tuyến sửa lỗi tự động nếu QA/QC kết luận `Sai, cần sửa`.
6. **Bước 6 (`Step6_ReAudited`)**: QA/QC tái kiểm toán đối tượng sau khi sửa lỗi.
7. **Bước 7 (`Step7_ConfirmedPass`)**: Xác nhận nghiệm thu ĐẠT $100\%$, đóng băng cấu kiện hoàn thành.

---

## 4. QUY TRÌNH ĐỐI SOÁT CHÉO 6 NGUỒN CỦA AGENT CAD

Trước khi chuyển giao cấu kiện sang Revit, Agent CAD bắt buộc phải đối soát chéo trên 6 nguồn:
1. **Mặt bằng (Plan Layout)**: Vị trí trục, tên cấu kiện, kích thước mặt bằng.
2. **Mặt cắt (Cross-Sections)**: Kích thước $b \times h$, chiều dày sàn, các lớp cấu tạo.
3. **Chi tiết (Detail Drawings)**: Cấu tạo chi tiết, giật cấp, vát mép, độ ngàm, thép neo.
4. **Bảng thống kê (Schedules / BOQ)**: Bảng kê kích thước danh định và khối lượng.
5. **Ghi chú chung (General Notes)**: Cấp độ bền bê tông, lớp bảo vệ, COTE quy ước, độ ngàm cọc.
6. **Tầng liên đới (Adjacent Levels)**: Tính liên tục của kết cấu chịu lực giữa các tầng.

> **Quy tắc bất di bất dịch**: Nếu có bất kỳ mâu thuẫn nào giữa 2 trong 6 nguồn (ví dụ: Mặt bằng ghi $250 \times 500$ nhưng Mặt cắt ghi $250 \times 600$), **TUYỆT ĐỐI KHÔNG TỰ CHỌN MỘT PHƯƠNG ÁN**. Chuyển ngay sang **Nhóm 2** để phát hành RFI.

---

## 5. HỆ THỐNG PHÂN LOẠI 3 NHÓM CẤU KIỆN

| Nhóm | Tên Phân Loại | Tiêu Chuẩn Kỹ Thuật | Quyền Hạn Đối Với Revit |
|:---:|---|---|---|
| **Nhóm 1** | `Group1_ReadyToModel` | Đủ thông tin duy nhất, rõ ràng $100\%$, đối soát chéo 6 nguồn khớp hoàn toàn. | **ĐƯỢC PHÉP BÀN GIAO CHO AGENT REVIT DỰNG MÔ HÌNH.** |
| **Nhóm 2** | `Group2_ConflictNeedRfi` | Có thông tin mâu thuẫn giữa các bản vẽ (mặt bằng vs mặt cắt, chi tiết vs thống kê). | **CẤM DỰNG MÔ HÌNH.** Đóng băng phân đoạn, phát hành RFI. |
| **Nhóm 3** | `Group3_MissingInfoHalt` | Thiếu thông tin bắt buộc (thiếu tiết diện, thiếu cao độ, không rõ điểm tựa). | **CẤM DỰNG MÔ HÌNH.** Đưa vào danh sách "Chưa đủ cơ sở để kết luận", phát hành RFI bổ sung. |

---

## 6. CƠ CHẾ TỰ KIỂM TRA 2 PHA TỰ ĐỘNG CỦA AGENT REVIT

Mọi thao tác dựng hình của Agent Revit bắt buộc phải trải qua 2 pha khép kín:
- **Pha 1 (Tạo đối tượng)**: Mở Transaction tạo FamilyInstance / Structural Framing / Floor.
- **Pha 2 (Đo đạc lại ngay lập tức qua API)**:
  - Truy vấn lại chính phần tử vừa tạo qua `ElementId`.
  - Đo đạc: Tiết diện thực tế ($b, h, D$), cao độ đỉnh/đáy ($Z_{\text{top}}, Z_{\text{bottom}}$), tọa độ điểm đầu/cuối, góc xoay.
  - **Dung sai bắt buộc**:
    - Tiết diện: $\Delta b = 0.0\text{mm}, \Delta h = 0.0\text{mm}$ (Chính xác $100\%$ tuyệt đối).
    - Cao độ: $|\Delta z| \le 1.0\text{mm}$.
    - Vị trí mặt bằng: $|\Delta x| \le 1.0\text{mm}, |\Delta y| \le 1.0\text{mm}$ (Cọc $\le 2.0\text{mm}$).
  - Nếu bất kỳ thông số nào vượt dung sai: **LẬP TỨC ROLLBACK TRANSACTION**, không để lại mô hình rác, báo lỗi về Chủ trì.

---

## 7. QUY TRÌNH KIỂM TOÁN ĐỘC LẬP 8 BƯỚC CỦA AGENT QA/QC

Agent QA/QC kiểm toán độc lập theo 8 bước tuần tự:
1. **Bước 1: Hệ trục và Cao độ** (Sai số nhịp $\le 1.0\text{mm}$. NẾU SAI $\rightarrow$ **DỪNG TOÀN BỘ VÀ ĐÓNG BĂNG MÔ HÌNH**).
2. **Bước 2: Số lượng cấu kiện** (Rà soát thiếu, dư, trùng lặp, dầm ma).
3. **Bước 3: Vị trí mặt bằng** (Đúng trục, đúng khoảng cách; cọc $\le 2.0\text{mm}$, khác $\le 1.0\text{mm}$).
4. **Bước 4: Kích thước tiết diện** (Sai số $b \times h = 0.0\text{mm}$ tuyệt đối).
5. **Bước 5: Cao độ kết cấu** (Đo đỉnh/đáy đài móng, giằng móng, cọc ngàm; sai số $\le 1.0\text{mm}$).
6. **Bước 6: Quan hệ kết cấu** (Dầm gác đài/cột, dầm phụ gác dầm chính, cọc ngàm đáy đài $100\text{mm}$).
7. **Bước 7: Kiểm tra chéo Mặt bằng vs Mặt cắt** (Đúng mặt bằng nhưng sai mặt cắt $\rightarrow$ **KẾT LUẬN LÀ SAI**).
8. **Bước 8: Vị trí bất thường & Va chạm** (Dầm lệch trục, dầm chuyển, lỗ mở lớn, sàn giật cấp, clash).

### 3 Trạng thái kết luận bắt buộc:
- **Đúng** (`Pass`): Thỏa mãn $100\%$ tiêu chuẩn.
- **Sai, cần sửa** (`Fail_NeedsFix`): Ghi rõ định lượng và định tuyến sửa lỗi (CadFix vs RevitFix).
- **Chưa đủ cơ sở để kết luận** (`Inconclusive_NeedsInfo`): Nêu rõ thiếu thông tin gì từ hồ sơ gốc.
- **CẤM TUYỆT ĐỐI**: Không sử dụng các từ ngữ mơ hồ ("Có vẻ đúng", "Gần đúng", "Không thấy vấn đề", "Khả năng cao là đúng").

---

## 8. CƠ CHẾ KỸ THUẬT FAIL-FAST GATE (FF-1 ĐẾN FF-6)

| Mã | Tên Quy Tắc | Điều Kiện Thất Bại | Ngoại Lệ & Xử Lý |
|:---:|---|---|---|
| **FF-1** | Cấm Thuật Toán Phỏng Đoán | Dùng gom cụm khoảng cách, line-pairing, dictionary kích thước tự chế. | Ném `SpeculativeAlgorithmProhibitedException` $\rightarrow$ Nhóm 3. |
| **FF-2** | Bắt Buộc Tọa Độ Thực | Tọa độ rỗng, NaN, hoặc dầm có chiều dài bằng 0. | Ném `InvalidGeometrySpecificationException` $\rightarrow$ Nhóm 3. |
| **FF-3** | Bắt Buộc Cao Độ Đỉnh/Đáy | Thiếu cao độ hoặc cao độ lệch thiết kế (ví dụ giằng móng khác $-1.600\text{m}$). | Ném `MissingElevationSpecificationException` $\rightarrow$ Nhóm 3. |
| **FF-4** | Bắt Buộc Tiết Diện Cụ Thể | Thiếu $b$ hoặc $h$ ($b \le 0, h \le 0$), thiếu đường kính cọc $D \le 0$. | Ném `MissingCrossSectionSpecificationException` $\rightarrow$ Nhóm 3. |
| **FF-5** | Bắt Buộc Điểm Tựa Liên Kết | Dầm không có điểm tựa 2 đầu (dầm lơ lửng), cọc không có đài móng bao quanh. | Ném `FloatingStructuralElementException` $\rightarrow$ Nhóm 3. |
| **FF-6** | Đối Soát Chéo Không Mâu Thuẫn | Mâu thuẫn giữa 2 trong 6 nguồn dữ liệu thiết kế. | Ném `ConflictingDesignInformationException` $\rightarrow$ Nhóm 2 (RFI). |

---

## 9. MỤC TIÊU VÀ KẾ HOẠCH HÀNH ĐỘNG PHÂN ĐOẠN PHẦN MÓNG

Áp dụng cho dự án thực tế: **Hội Trường Tân Đông Hiệp (KC-HT-90-TLTT-16)**:
1. **Làm sạch mô hình**: Purge sạch $100\%$ toàn bộ 211 dầm ma hiện có trong Revit. Bảo tồn nguyên vẹn 27 Grids và 10 Levels.
2. **Hạ cao độ cọc**: Điều chỉnh 196 cọc D400 ngàm $100\text{mm}$ vào đáy đài tương ứng (đỉnh cọc: $-2.500\text{m}$ cho `ĐC-1`..`3'`, $-2.700\text{m}$ cho `ĐC-4`..`5`, $-2.800\text{m}$ cho `ĐC-6`).
3. **Trích xuất & Bàn giao Nhóm 1**: Trích xuất 72 đài móng từ Block `mm` (đỉnh $-1.600\text{m}$) và 22 loại giằng móng `GM-1` đến `GM-22` (đỉnh $-1.600\text{m}$).
4. **Dựng hình Revit 2 pha**: Agent Revit dựng đài và giằng móng với cơ chế tự kiểm tra 2 pha.
5. **Kiểm toán độc lập 8 bước**: Agent QA/QC kiểm toán, xuất biên bản nghiệm thu ĐẠT $100\%$ và cập nhật Sổ cái `bim_coordination_ledger.json`.

---

## 10. DANH MỤC HỒ SƠ TÁC TỬ & BIỂU MẪU

1. `coordinator_assistant.md` — Hồ sơ Kỹ sư Chủ trì Điều phối.
2. `cad_assistant.md` — Hồ sơ Kỹ sư Kết cấu CAD.
3. `revit_assistant.md` — Hồ sơ Kỹ sư BIM Revit.
4. `qaqc_assistant.md` — Hồ sơ Kỹ sư QA/QC Độc lập.
5. `rfi_template.md` — Mẫu Phiếu Yêu Cầu Làm Rõ Thiết Kế (RFI).
6. `README.md` — Tài liệu Kiến trúc Hệ thống Multi-Agent (Tài liệu này).
