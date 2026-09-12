# ADR-06 — Tool Registry: files là sự thật, SQLite là chỉ mục, publish phải qua người

**Ngày:** 2026-09-12 · **Status:** Accepted (user-confirmed: policy `manual` mặc định) · **Owner:** HPRebar

## Context

- Yêu cầu: registry lưu tên, mô tả, input, category, code, ví dụ, lịch sử chạy + độ ổn định; AI tìm registry trước; không tự đưa mọi code vào thư viện — cần bước kiểm/duyệt.
- Nhiều phiên Claude = nhiều tiến trình server cùng lúc; người dùng muốn review/chia sẻ tool giữa máy.
- SDK 2.2.0: `McpServerOptions.ToolCollection` + `Changed` → server tự phát `notifications/tools/list_changed`; `McpServerTool.Create(AIFunction)` cho schema tuỳ ý.

## Decision

| Vấn đề | Chốt | Lý do |
|---|---|---|
| Nguồn sự thật | **Files** `tools-library/<Category>/<name>/{tool.json, code.cs, examples.json}` (`%AppData%\HPRebar\McpServer\`, đổi qua `Registry:LibraryPath`) | git-review được, chia sẻ được, sửa tay được, sống sót khi cài lại |
| Chỉ mục + bộ nhớ | **SQLite** `registry.db` (WAL, FTS5): `tools`, `tool_versions`, `runs`, `registry_events` | nhiều server ghi chung; lịch sử chạy là thứ duy nhất không tái tạo từ file |
| Đồng bộ | `FileSystemWatcher` (debounce 400 ms) → reload → `DynamicToolRegistrar.Sync()` trong `DeferChangedEvents` | approve bằng CLI/sửa file có hiệu lực ở mọi server đang chạy, không restart |
| Vòng đời | `draft → tested → pending_approval → published → quarantined \| deprecated` | mỗi bước = ghi file + `registry_events` |
| Cổng publish | **`manual`** mặc định: AI chỉ tới `pending_approval` + `_review/<name>.md`; người `HPRebar.Mcp.Server.exe registry approve <name> --by <ai>` hoặc sửa `status` trong tool.json. `auto`: opt-in trong config, cần `tested` | yêu cầu 5 của user; AI không có đường nào tự publish khi manual |
| Kiểm trước khi nhận | `ToolValidator`: slug/reserved, category, schema subset, args ↔ schema 2 chiều, guard + compile qua `revit.analyze`, transaction khớp, ví dụ ≥ 1 (khuyến nghị ≥ 2, khác args), literal nghi ngờ → warning | cơ học, rẻ, không đoán "hữu ích hay không" |
| Test | `test_tool` chạy ví dụ với **dryRun** (rollback) → `tested`; `realRun` chỉ khi được yêu cầu | không đổi model của user khi kiểm |
| Ai tổng quát hoá | client LLM (prompt `toolify_run` + `get_run` literal) | không API key, không chi phí server |
| Ổn định | `stability = successRate(50 run) × min(1, runs/10)`; quarantine khi ≥ 5 run và lỗi > 40 %; test và lỗi hạ tầng (bridge vắng/opt-in tắt) **không** tính | tool hỏng tự rút khỏi `tools/list`, có ghi chú lý do |
| Tìm kiếm | FTS5 `bm25 × (0.5 + 0.5·stability) × statusWeight` (published 1 · tested 0.6 · pending 0.4 · draft 0.2 · khác 0); fallback token-match nếu thiếu FTS5 | offline, đủ tới vài trăm tool; embedding để sau |

## Consequences

- Người dùng có 3 cách duyệt: CLI, sửa `tool.json`, (tương lai) cửa sổ bridge. Review file chứa đủ code/schema/ví dụ/test runs.
- Bảo mật không đổi (ADR-04): tool lưu sẵn vẫn qua guard + opt-in + timeout + audit khi chạy; `args` không bao giờ ghép vào code.
- Chi phí: `Microsoft.Data.Sqlite` + native `e_sqlite3` trong single-file publish (`IncludeNativeLibrariesForSelfExtract`), ~2 MB.
- Đã verify live 2026-09-12: 3 kịch bản (HIT · MISS→propose→test→approve→HIT · HỎNG→quarantine→restore) — `reports/phase-09-live-verify.md`.
