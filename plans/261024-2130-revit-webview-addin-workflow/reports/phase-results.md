# Kết quả (2026-10-24)

Quyết định user: tách nhẹ phần test được (giữ 2 project, không đổi folder đang chạy); tên skill `revit-webview-addin`.

| Phase | Kết quả |
|---|---|
| 1 Domain | `Core/Domain/Dashboard/ShowElementsRules` (nguồn: selection / ids ≤ 10 000 / category; BAD_REQUEST) + 6 test. Validate chạy trong `DashboardModule` trước queue → request sai không chờ API thread (BAD_REQUEST thắng NO_DOCUMENT cho request hỏng). `ElementNavigator` nhận nguồn đã validate. |
| 2 Application | `Core/Application/SingleFlightRun<TKey>` (join cùng key, BUSY key khác — ném đồng bộ, bỏ lượt có token đã huỷ) + 4 test; `DashboardModule` dùng nó thay tuple tự quản. |
| Không tách | quy tắc lọc category (`IsCounted`) và `CanHoldElements`: đầu vào là kiểu Revit (`Category`, `ViewType`), logic 1 dòng → tách chỉ thêm lớp ánh xạ. |
| Kiểm tra | Core.Tests 82/82; `HPRebar.slnx -c Debug.R26` 0 lỗi; live Revit 2026.5 thứ hai (bản sao): protocol 22/22, events 13/13 (modal → TIMEOUT → phục hồi, đổi view, xoá/undo → stale), lifecycle 5/5 (không `--theme`; theme đã verify ở plan 261024-1914), Revit responding. |
| 4 Skill | `.claude/skills/revit-webview-addin/` SKILL.md + 6 references (architecture-layers, bridge-protocol, revit-threading-and-transactions, testing-and-live-verify, packaging-and-versions, gotchas); link ra ngoài skill đổi thành đường dẫn dạng code (engine báo "reference escapes provider root"); `sync-agent-skills.py apply` → `.agents/skills/revit-webview-addin/`, check: drift none, findings none. Routing: `.claude/rules/skill-domain-routing.md`. |

## Review round (code-reviewer, DONE_WITH_CONCERNS → fixed)
- Code: không lệch hành vi ngoài thay đổi chủ ý (BAD_REQUEST trước NO_DOCUMENT); giả định "chỉ UI thread" đúng.
- High (skill): pipeline ghi giữ `TransactionGroup` mở qua nhiều `RunAsync` → trái luật "group mở/đóng trong một Execute". Sửa: A = một Execute (1 Undo, không tiến độ vì UI thread bận), B = Transaction mỗi lượt (nhiều Undo, có tiến độ/huỷ); ghi GIẢ ĐỊNH CHƯA XÁC MINH.
- Medium đã sửa: `CANCELLED` ghi là dự trữ (chưa có lệnh huỷ); `dotnet test` phải chạy trong `HPRebar/` (gốc repo = 0 test); điều kiện model ở view mặt bằng cho events harness; add-in web mới cần duyệt + ADR mới (ADR-0008 chỉ cho toolkit).
- Low đã sửa: câu dẫn ADR-0008 tách thành hướng dẫn; thứ tự `modules` không quan trọng; `npm ci` sau `pack` lỡ chạy trên cây dev; enum `ShowElementsSource` file riêng; test thêm (BUSY không start, hủy + key khác, cùng key sau khi xong, CurrentSelection với > 10 000 ids, category không ids) → 86/86; comment joiner dùng chung deadline; lifecycle thêm check BAD_REQUEST khi không có tài liệu.
- Low để lại: `ElementNavigator.Show` public tin `source` khớp `request` (đề xuất `ShowElementsTarget`); commit nên tách (di chuyển code / đổi thứ tự lỗi / skill).
