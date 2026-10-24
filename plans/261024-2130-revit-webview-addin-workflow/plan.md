# Quy trình phát triển Revit add-in WebView2 + React (từ HP BIM Toolkit) → skill

2026-10-24. Nền: HP BIM Toolkit (commit 9098fba, e83ee24), [đề xuất kiến trúc](../reports/architect-2026-10-24-revit-addin-modern-stack-direction.md).

## Đánh giá kiến trúc hiện có (scout 2026-10-24)
| Lớp yêu cầu | Đang ở đâu | Đánh giá |
|---|---|---|
| Frontend | `HPBimToolkit.Frontend` (React/TS/Vite/Tailwind, mock bridge, vitest 28) | ✅ tách rời, kiểu TS sinh từ C# |
| Bridge | Core `Contracts/`, `Protocol/` (parse, dispatcher, timeout, mã lỗi) + add-in `Bridge/` (transport, event publisher) + `ExternalEvents/RevitRequestQueue` | ✅ một ExternalEvent, có test 72 |
| Domain | Core `Statistics/` (CategoryTally, ModelChangeTally) | 🟡 quy tắc nghiệp vụ còn nằm trong adapter Revit: lọc category đếm được (`ModelStatisticsReader.IsCounted`), view nhận phần tử (`ElementNavigator.CanHoldElements`), giới hạn/validate `showElements` → không unit-test được |
| Application | add-in `Modules/` (DashboardModule, AppModule) | 🟡 điều phối thống kê (join cùng scope / BUSY / bỏ lượt đã huỷ — từng là lỗi High) nằm trong add-in, gọi static service với `UIApplication` → không test được |
| Infrastructure | add-in `Services/` (đọc Revit → DTO), `Host/`, `Infrastructure/` | ✅ đúng chỗ, file nhỏ (≤ 185 dòng) |
| Transaction | chưa có (Module 1 chỉ đọc + selection) | ❌ chưa có chuẩn ghi; cần trước Parameter Manager |
| Backend | không | ✅ đúng: chưa có nhu cầu dùng chung |

## Đề xuất (tránh overengineering)
- Giữ 2 project C# (Core thuần .NET + add-in). Không tách thêm project Domain/Application/Infrastructure.
- Core thêm `Domain/<Module>/` (quy tắc thuần) và `Application/` (điều phối không phụ thuộc Revit); add-in giữ vai trò Infrastructure + Host. Chỉ chuyển phần có giá trị test, không đổi tên folder đang chạy.
- Chuẩn ghi (TransactionGroup + Transaction, 1 Undo, preview → apply, chia lô + huỷ) ghi vào quy trình; code `RevitWriteRunner` khi module ghi đầu tiên cần (YAGNI).
- Quy trình viết thành skill (SKILL.md + references), mirror sang `.agents/` bằng engine sync.

## Phases
| # | Phase | Gate |
|---|---|---|
| 1 ✅ | Domain: `ShowElementsRules` (giới hạn id, chọn nguồn) + test; `CategoryCountingRule` hoãn có chủ ý (đầu vào kiểu Revit, 1 dòng) | Core.Tests xanh |
| 2 ✅ | Application: `SingleFlightRuns` (join/BUSY/bỏ lượt huỷ) dùng trong DashboardModule + test | Core.Tests xanh, build R26 |
| 3 ✅ | Live regression (Revit thứ hai, bản sao): protocol 22 + events 13 + lifecycle 5 | toàn PASS |
| 4 ✅ | Skill: quy trình + references (layers, bridge, transaction, test/live, gotchas) | skill-sync check sạch |
| 5 ✅ | Review + docs | không còn High |

Kết quả: [reports/phase-results.md](reports/phase-results.md).
