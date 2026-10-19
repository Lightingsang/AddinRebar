# Project: Multi-Agent Structural & BIM Coordination (HPBim)

## Architecture
- **Ecosystem**: HPBim (C# .NET) under `01_AddinRebar` repository.
- **Normative Foundation**: Quy chế phối hợp chuẩn Kỹ sư Kết cấu (`quy_che_phoi_hop_cad_revit_chuan_ky_su_ket_cau.md`).
- **Core Directive**: ĐIỀU 0 - NGUYÊN TẮC VÀNG: "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."
- **Communication & Storage**:
  - Central Coordination Ledger: `state/bim_coordination_ledger.json` tracking 7 lifecycle steps.
  - Named Pipe AutoCAD: `\\.\pipe\hpautocad-mcp-2026`
  - Named Pipe Revit: `\\.\pipe\hprebar-mcp-r2026`
  - Technical Enforcement: `HPBim.Cli` Fail-Fast Gate (zero line-pairing/guessing).

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Agent Profiles & System Prompts | Tái lập hồ sơ 4 Agent (`coordinator_assistant`, `cad_assistant`, `revit_assistant`, `qaqc_assistant`) khắc cốt ĐIỀU 0 Nguyên Tắc Vàng | M1 | ORIGINAL_REQUEST §R1 |
| 2 | Freeze Zone & RFI Mechanism | Quy định đóng băng phân đoạn và phát hành Phiếu yêu cầu làm rõ khi phát hiện mâu thuẫn | M1 | ORIGINAL_REQUEST §R1 |
| 3 | Central Coordination Ledger Schema & C# Models | Sổ cái `bim_coordination_ledger.json` theo dõi 7 bước vòng đời của từng cấu kiện | M2 | ORIGINAL_REQUEST §R2, Explorer 1 |
| 4 | C# Fail-Fast Technical Gate in CLI | Triệt tiêu mọi mã phỏng đoán/ghép nét, ném ngoại lệ dừng cấu kiện thiếu thông tin (FF-1 đến FF-6) | M2 | ORIGINAL_REQUEST §R2, Explorer 1 |
| 5 | Clean Ghost Beams from Revit | Purge sạch 100% 211 dầm ma trong Revit, bảo tồn nguyên vẹn 27 Grids và 10 Levels | M3 | ORIGINAL_REQUEST §R3, Explorer 2 |
| 6 | Pile Cut-off Elevation Adjustment | Điều chỉnh 196 cọc D400 ngàm 100mm vào đáy đài (-2.500m ĐC-1..3', -2.700m ĐC-4..5, -2.800m ĐC-6) | M3 | ORIGINAL_REQUEST §R3, Explorer 2, Explorer 3 |
| 7 | CAD Footing & Tie Beam Extraction (Nhóm 1) | Trích xuất tọa độ 72 đài móng từ block mm và 22 giằng móng GM-1..22 từ CAD, phân loại Nhóm 1 | M3 | ORIGINAL_REQUEST §R3, Explorer 3 |
| 8 | Revit 2-Phase Modeling & Verification | Dựng đài móng và giằng móng trong Revit kèm cơ chế tự đo ngược lại (sai số b x h = 0.0mm, z <= 1.0mm) | M3 | ORIGINAL_REQUEST §R3, Explorer 2 |
| 9 | QA/QC 8-Step Independent Audit | Kiểm toán độc lập 8 bước đối soát DWG vs Revit, xuất biên bản nghiệm thu Markdown & JSON | M3 | ORIGINAL_REQUEST §R3 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Agent Profiles & System Prompts | Tái lập hồ sơ 4 Agent, ban hành quy chế ĐIỀU 0, cơ chế Freeze Zone & RFI | none | DONE |
| M2 | Ledger Infrastructure & Fail-Fast CLI | Sổ cái `bim_coordination_ledger.json`, CLI Fail-Fast Gate không suy đoán | M1 | DONE |
| M3 | Foundation Phase Execution & QA/QC Audit | Purge dầm ma, chỉnh cọc, trích xuất CAD Nhóm 1, dựng Revit 2 pha, QA/QC 8 bước | M2 | PLANNED |

## Interface Contracts
### CAD $\leftrightarrow$ Revit
- Cấu kiện Nhóm 1: Có dữ liệu hình học, vị trí, kích thước, cao độ duy nhất được bàn giao sang Revit.
- Cấu kiện Nhóm 2 & Nhóm 3: Bị đóng băng tại Freeze Zone, phát hành RFI, cấm bàn giao sang Revit.

### Revit $\leftrightarrow$ QA/QC
- Dựng hình 2 pha: Sau khi tạo đối tượng, Revit Agent lập tức đo đạc kiểm tra lại b, h, z, tọa độ. Nếu sai số > 1.0mm thì rollback Transaction.
- QA/QC kiểm toán 8 bước trực tiếp trên Revit API và đối soát DWG qua Named Pipe.

### QA/QC $\leftrightarrow$ Ledger
- Mọi kết quả kiểm toán được cập nhật vào `state/bim_coordination_ledger.json` với 3 kết luận duy nhất: Đúng / Sai, cần sửa / Chưa đủ cơ sở để kết luận.
