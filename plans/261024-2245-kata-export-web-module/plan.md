# Kata Export — module web trong HP BIM Toolkit (Export + tạo thép)

> **Bỏ (2026-10-24, người dùng):** Kata ở lại HPRebar — thay bằng `plans/261024-2345-kata-export-web-ui-hprebar/` (ADR-0010). Phase 1 của plan này cất ở `git stash` "toolkit Kata phase 1 (superseded…)".

2026-10-24. Quy trình: skill `/revit-webview-addin`. Nguồn: tool WPF `HPRebar/HPRebar/KataExport` (66 file, 7,5k dòng) + `KataRebar` (46 file, 3,8k dòng), engine `HPRebar.Core/KataExport` + `KataRebar` (~12k dòng, thuần).

## Quyết định người dùng (2026-10-24)
1. Module web mới trong HP BIM Toolkit; tool WPF chạy song song tới khi đủ tính năng, gỡ quyết định sau.
2. Phạm vi: Export (Revit → Kata Pro) **và** tạo thép (Excel → Revit).
3. Engine: tham chiếu project `HPRebar.Core` (một nguồn quy tắc Kata) → ADR-0009.
4. Adapter Revit + Excel COM: port vào `HPBimToolkit/Services/Kata/` (R26), không sửa tool WPF; trùng code tạm thời tới khi gỡ WPF.

## Luồng
```text
Trang Kata (React) ──kata.*──► KataModule ──► RevitRequestQueue ──► Services/Kata (Revit) ─┐
      ▲  bảng nhịp/gối, mặt đứng SVG, kế hoạch thép               HPRebar.Core (Kata engine) ◄┘
      └── kata.excel.* ──► Services/Kata/Excel (COM, Kata Pro đang mở, sheet Dam)
```

## Phases (lát dọc, mỗi phase có cổng của skill)
| # | Phase | Type | Gate | File |
|---|---|---|---|---|
| 0 ✅ | ADR-0009 + tham chiếu HPRebar.Core + spike: engine chạy trong load context toolkit, COM Excel từ toolkit | HITL nhẹ | build R26, spike live | [phase-00](phase-00-adr-and-spike.md) |
| 1 | Đọc dầm: chọn run (selection hiện tại / pick), đọc nhịp-gối-lưới-header → bảng xem trước | AFK | test + live trên bản sao | [phase-01](phase-01-read-run.md) |
| 2 | Mặt đứng: model `KataElevationBuilder` → DTO → SVG (focus nhịp, chú thích) | AFK | vitest + ảnh trong Revit | [phase-02](phase-02-elevation.md) |
| 3 | Ghi Excel: dò workbook, xác nhận đúng file, ghi sheet `Dam` | HITL (workbook nháp) | so ô với hợp đồng ô | [phase-03](phase-03-excel-write.md) |
| 4 | Đọc lại + kế hoạch thép: đọc `Dam` → parse → khớp dầm → xem trước thép/mặt cắt, bỏ nhóm thép | AFK | test + live | [phase-04](phase-04-read-back-plan.md) |
| 5 | Tạo thép: một Undo "HP: Kata Rebar – <dầm>", xoá lượt cũ, bar type, đánh số; long section, mặt cắt, sheet | HITL | so với tool WPF trên cùng bản sao | [phase-05](phase-05-generate.md) |
| 6 | Cài đặt Kata (3 tab) đọc/ghi `%AppData%\HPRebar\KataSettings.json` chung với WPF | AFK | test round trip file | [phase-06](phase-06-settings.md) |
| 7 | Parity + review + docs + gói: checklist so WPF, harness, CLAUDE.md/AGENTS.md, install guide, pack | AFK | không còn High | [phase-07](phase-07-parity-review.md) |

## Rủi ro chính
- Ghi nhầm workbook thật (tool WPF ghi workbook **đang mở**) → bắt buộc xác nhận tên file trước khi ghi; live chỉ với bản nháp.
- Tạo thép là module ghi đầu tiên → kiểm chứng luật Transaction của skill (giả định chưa xác minh).
- Hai bản adapter lệch nhau khi WPF còn được sửa → mọi quy tắc mới đặt trong HPRebar.Core; ghi danh sách file port + commit nguồn để đối chiếu.
- Khối lượng lớn, nhiều phiên làm việc → mỗi phase commit riêng sau khi qua cổng.

## Không làm
- Sửa `HPRebar/HPRebar/KataExport|KataRebar` (tool WPF), Wave 6, gỡ nút ribbon WPF.
- R23–R25/R27 cho module web.
