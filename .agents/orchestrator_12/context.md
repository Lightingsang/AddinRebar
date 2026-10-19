# Orchestrator 12 Context — Generation 2 (Successor to orchestrator_11)

## Project Overview
Tái cấu trúc và thiết lập lại toàn diện Đội ngũ Multi-Agent Kết Cấu & BIM (Kỹ sư Chủ trì Điều phối, Kỹ sư Kết cấu CAD, Kỹ sư BIM Revit, Kỹ sư QA/QC Độc lập) cùng hạ tầng Sổ cái Ledger và bộ công cụ CLI C# Fail-Fast, hiện thực hóa triệt để Nguyên Tắc Vàng: "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."

## Inherited State & Hand-off from Generation 1
- **Predecessor Handoff**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_11\handoff.md`
- **Authoritative Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (section `## 2026-10-07T02:05:11Z`)
- **Normative Quy Chế**: `C:\Users\STR-HP03\.gemini\antigravity\brain\087c0df3-fff1-40c1-83ce-c0d4c6cb6acd\quy_che_phoi_hop_cad_revit_chuan_ky_su_ket_cau.md`
- **Global Project Scope**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_11\PROJECT.md`
- **Gate Status**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_11\GATE_STATUS.md`

## Milestone Progress So Far:
- **Phase 0 (Survey & Ground-Truth Extraction)**: 100% COMPLETE. 211 dầm ma định danh, 27 Grids & 10 Levels bảo toàn, 196 cọc D400 đối soát 6 nguồn khớp 100%, 72 đài móng và 22 giằng móng trích xuất từ CAD Block mm vào `verified_72_footings.json` và `verified_22_grade_beams.json`.
- **Milestone M1 (Agent Profiles & Golden Rule ĐIỀU 0)**: 100% COMPLETE (Gate PASS). 6 files trong `HPBim/agents/` ban hành chuẩn chỉ, khắc cốt ĐIỀU 0.
- **Milestone M2 (Ledger & Fail-Fast Gate)**: IN_PROGRESS (Iteration 2).
  - Iteration 1 bị Forensic Auditor VETO do lỗi IEEE 754 (NaN, Infinity), chuỗi trắng trong Supports, và cờ opt-in/bypass trong CLI.
  - `explorer_m2_fix` đã chuẩn bị trọn bộ giải pháp vá lỗi tại: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_fix/` (`remediation.patch`, `proposed_FailFastGate.cs`, `proposed_CadSourceSpec.cs`, `analysis.md`).

## Concrete Next Steps for Generation 2:
1. **Hoàn tất Milestone M2 (Iteration 2 Fix & Gate Closure)**:
   - Dispatch `worker_m2_fix` áp dụng bản vá của `explorer_m2_fix` vào `FailFastGate.cs`, `CadSourceSpec.cs` và các lệnh CLI (`BuildGradeBeamNetworkCommand.cs`, `AnalyzeFootingsCommand.cs`, `GenerateGradeBeamsCommand.cs`, `GenerateFoundationSlabsCommand.cs`).
   - Xác nhận 100% 314/314 tests trong `HPBim.slnx` chạy PASS.
   - Thẩm định và đóng Gate Milestone M2.
2. **Thực thi Dứt điểm Milestone M3 (Phân đoạn Phần Móng)**:
   - B1: Purge sạch 100% 211 dầm ma trong Revit, bảo tồn 27 Grids và 10 Levels.
   - B2: Điều chỉnh 196 cọc D400 ngàm 100mm vào đáy đài tương ứng (COTE -2.500m cho ĐC-1..3', -2.700m cho ĐC-4..5, -2.800m cho ĐC-6).
   - B3: Dựng 72 đài móng và 22 giằng móng GM-1..22 trong Revit bằng cơ chế tự kiểm tra 2 pha.
   - B4: QA/QC kiểm toán độc lập 8 bước DWG vs Revit, xuất Biên bản nghiệm thu chính thức Markdown và JSON.
   - B5: Đóng Gate M3, tuyên bố hoàn thành dự án lên Sentinel để thực hiện Victory Audit độc lập.
