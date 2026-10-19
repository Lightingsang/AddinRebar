# Orchestrator 11 Context - Multi-Agent Structural & BIM Coordination (HPBim)

## User Request & Mission
Tái cấu trúc và thiết lập lại toàn diện Đội ngũ Multi-Agent Kết Cấu & BIM (Kỹ sư Chủ trì Điều phối, Kỹ sư Kết cấu CAD, Kỹ sư BIM Revit, Kỹ sư QA/QC Độc lập) cùng hạ tầng Sổ cái Ledger và bộ công cụ CLI C# Fail-Fast, hiện thực hóa triệt để Nguyên Tắc Vàng: "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Repo Root: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development
Orchestrator Workspace: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_11
Authoritative Request: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-07T02:05:11Z)

## Normative Documents & Standards
- Quy chế phối hợp chuẩn Kỹ sư Kết cấu: `C:\Users\STR-HP03\.gemini\antigravity\brain\087c0df3-fff1-40c1-83ce-c0d4c6cb6acd\quy_che_phoi_hop_cad_revit_chuan_ky_su_ket_cau.md`
- Quy chuẩn Clean Code kho mã nguồn: `docs/clean-code/HP_CLEAN_CODE_CORE.md`
- Hạ tầng kết nối trực tiếp: Named Pipe AutoCAD (`\\.\pipe\hpautocad-mcp-2026`) và Named Pipe Revit (`\\.\pipe\hprebar-mcp-r2026`).

## Requirements
- R1. Tái Lập Hệ Thống Hồ Sơ & Quyền Hạn Bộ Tứ Agent (`coordinator_assistant`, `cad_assistant`, `revit_assistant`, `qaqc_assistant`), khắc cốt Nguyên Tắc Vàng tại ĐIỀU 0.
- R2. Thiết Lập Sổ Cái Điều Phối (`state/bim_coordination_ledger.json`) qua 7 bước vòng đời & Cơ Chế Fail-Fast Bằng Code C# trong `HPBim.Cli`.
- R3. Thực Thi Dứt Điểm Phân Đoạn Phần Móng (Cọc, Đài Móng, Giằng Móng) theo 5 bước tuần tự:
  1. Làm sạch mô hình: Purge 100% 211 dầm ma trong Revit, bảo tồn 27 Grids và 10 Levels.
  2. Hạ cao độ cọc: 196 cọc D400 ngàm 100mm vào đáy đài (COTE -2.500m cho ĐC-1..3', -2.700m cho ĐC-4..5, -2.800m cho ĐC-6).
  3. Trích xuất & Bàn giao Nhóm 1: 72 đài móng từ block mm (đỉnh -1.600m), 22 giằng móng GM-1..22 (đỉnh -1.600m).
  4. Dựng mô hình Revit: Đài móng và giằng móng với cơ chế tự kiểm tra 2 pha.
  5. Kiểm toán độc lập: QA/QC kiểm toán 8 bước đối soát DWG vs Revit, xuất Biên bản nghiệm thu Markdown và JSON.

## Key Constraints
- NEVER write or modify code directly as orchestrator — dispatch specialists (Explorer, Worker, Reviewer, Challenger, Auditor).
- Strict adherence to Clean Code and Die-Hard Golden Rule (ĐIỀU 0).
