---
title: "Phase 5 — Verify live trong AutoCAD 2026: ma trận execute, 3 kịch bản registry (HIT · MISS→approve · HỎNG→quarantine→restore), hồi quy Revit, docs"
status: planned
priority: P1
effort: 10h
depends_on: [phase-02, phase-03, phase-04]
created: 2026-09-13
revised: 2026-09-14 (ADR-06 — hai exe, hai solution)
---

# Phase 5 — Live verification & docs

> Revised 2026-09-14: hai exe riêng (`HPRebar/output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe`, `HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe`), không có env chọn host; CLI `registry approve` gọi trên exe AutoCAD (ADR-06).

## Context
- Mẫu: [`../260912-1521-dynamic-revit-mcp-server-2026/phase-09-verify-live-and-docs.md`](../260912-1521-dynamic-revit-mcp-server-2026/phase-09-verify-live-and-docs.md) + `reports/phase-09-live-verify.md` (3 kịch bản Revit đã pass).
- Harness: `mcp_call.py` (stdio, scratchpad) — cùng exe Claude Code dùng; env chọn host.
- Máy dev: AutoCAD 2026 R25.1.74 (.NET 8), Revit 2026 với bridge Revit đã deploy (commit 9b83aee).

## Overview
Chứng minh vòng lặp tự sinh & ghi nhớ tool chạy trên AutoCAD **giống hệt** Revit, và Revit không hồi quy khi hai host cùng chạy trong một phiên. Sau đó cập nhật docs (CLAUDE.md § MCP bridge, AGENTS.md regen bằng engine, `docs/codebase-summary.md`, `docs/system-architecture.md`), memory note. Chỉ ở phase này mới đụng `CLAUDE.md`/`docs/` (ngoài scope plan hiện tại — thực hiện khi `/bs:cook` phase 5).

## Requirements
- Bridge AutoCAD deploy bản cuối (AutoCAD đóng khi build Debug); server exe publish với `IncludeNativeLibrariesForSelfExtract` (SQLite) như hiện tại.
- `.mcp.json` có cả `hprebar-revit` (không đổi) và `hprebar-autocad` → `HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe` (User Action).
- Drawing test: `Drawing1.dwg` trống (metric template `acadiso.dwt`, `INSUNITS=4`) + một DWG có block/dimension style (user chọn hoặc tạo bằng seed `create_layer`/`draw_polyline`/`insert_block`).

## Scenarios
A. **Execute matrix (qua `execute_autocad_code`, stdio):** 12 kịch bản phase 2 lặp lại qua server: none-read · dryRun · commit + Undo · exception · none+modify · manual OK · manual để mở · guard (`ed.GetPoint`, `tr.Commit()`, `SendStringToExecute`) · compile error · cancel · timeout (5 s) · busy (`LINE` dở → `-32002` sau grace, rồi ESC → chạy lại OK) · opt-in OFF → `-32001` · không drawing → `-32003`. Kiểm `changed`, `rolledBack`, `timedOut`, `runId`, `hint`, audit line.
B. **HIT** — `search_tools "liệt kê layer"` → `list_layers` (seed) → gọi theo tên → kết quả + `runs` row + `stability` tăng; `get_drawing_info`, `get_entities type=LWPOLYLINE`, `draw_polyline` dryRun rồi thật (Undo 1 bước), `create_layer`, `add_text`, `add_linear_dimension`, `insert_block` (drawing có block), `list_layouts`, `get_selected_entities` (chọn tay trước) — **mọi seed ít nhất dryRun** (như Revit 21 seed smoke).
C. **MISS → memory** — task "đổi mọi text trên layer X sang layer Y" không có seed → `execute_autocad_code` dryRun rồi thật → `runId` + hint → `get_run` (literals `"X"`, `"Y"` → args) → `toolify_run` → `propose_tool move_text_between_layers` (category `Annotation`, `host` tự điền) → `test_tool` (2 case, dryRun) → `publish_tool` → `pending_approval` + `_review/move_text_between_layers.md` (có `Host: autocad`) → `HPAutoCad.Mcp.Server.exe registry approve move_text_between_layers --by <user>` → server AutoCAD đang chạy phát `tools/list_changed` (không restart) → `search_tools` HIT → gọi theo tên → `runs`.
D. **HỎNG → quarantine → restore** — publish tool cố ý mong manh (cần block `NONEXISTENT`) → chạy 5 lần trên drawing trống → ≥ 3 fail → `quarantined`, biến khỏi `tools/list`, `_review` ghi lý do; sửa code (fallback) + `test_tool` → `manage_tool restore` → trở lại `tools/list`.
E. **Hồi quy Revit + hai exe song song** — cùng phiên: `hprebar-revit` `get_revit_context` + `analyze_model_statistics` (seed Revit) + `execute_revit_code` dryRun; `tools/list` revit = 34 không đổi; `%AppData%\HPRebar\McpServer\tools-library\` hash không đổi; 2 pipe tồn tại (`Get-ChildItem \\.\pipe\ | ? { $_.Name -like 'hprebar-mcp-*' -or $_.Name -like 'hpautocad-mcp-*' }`); hai registry root khác nhau (`%AppData%\HPRebar\McpServer`, `%AppData%\HPAutoCad\McpServer`).
F. **Xung đột/cách ly** — mở Civil 3D 2026 song song (cùng R25.1): bundle `Platform="AutoCAD"` **không** nạp vào Civil 3D (kiểm log) → không tranh pipe; mở AutoCAD 2026 instance thứ hai → bridge thứ hai báo "pipe already in use" trong status window, instance đầu vẫn phục vụ.

## Implementation steps
1. Đóng AutoCAD → `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` (deploy bundle AutoCAD) → `dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server`. Revit: **không** redeploy bridge (wire-compat); publish lại `HPRebar.Mcp.Server` (đã mỏng đi ở phase 0) → user thêm entry `.mcp.json` → mở AutoCAD 2026 → `HPMCPBRIDGE` → bật listener + opt-in.
2. Chạy A–F bằng `mcp_call.py`; lưu output vào `reports/phase-05-live-verify.md` (bảng kịch bản → kết quả → ghi chú, kèm số dòng audit, tên tool, runId).
3. Lỗi phát hiện → sửa ngay (như phase 9 Revit đã sửa 3 lỗi), chạy lại kịch bản liên quan, ghi vào report.
4. Docs: `CLAUDE.md` § Repository Layout (đã thêm `HPAutoCad/`, `McpShared/` ở phase 0) + § mới "HPAutoCad MCP Bridge" (tool surface, pipe, commands `HPMCPBRIDGE`, bundle path, settings/audit path, `.mcp.json` entry, lệnh build/test/publish, giới hạn); § "HPRebar MCP Bridge" ghi rõ phần dùng chung ở `McpShared/`; `AGENTS.md` regen bằng lệnh engine trong CLAUDE.md; `docs/codebase-summary.md`, `docs/system-architecture.md` (diagram 2 host); cập nhật `plan.md` status + `Status` các ADR → Accepted (kèm "verified by reports/phase-05-live-verify.md").
5. Memory: cập nhật `dynamic-revit-mcp-server-plan` hoặc thêm `autocad-mcp-bridge-plan` (trạng thái verified, leftovers).

## Todo
- [ ] 1 deploy/publish/.mcp.json · [ ] 2 A · [ ] 2 B · [ ] 2 C · [ ] 2 D · [ ] 2 E · [ ] 2 F · [ ] 3 fixes · [ ] 4 docs · [ ] 5 memory

## Success criteria
- A: 14/14 kịch bản đúng kỳ vọng; mỗi run ghi audit; Undo gỡ đúng một run.
- B: 12/12 seed chạy ít nhất dryRun không lỗi trên drawing phù hợp (seed cần block: có block).
- C: tool mới xuất hiện trong `tools/list` của server **đang chạy** ≤ 1 s sau `approve`, gọi được theo tên, `runs` ghi.
- D: quarantine tự động đúng ngưỡng (≥ 5 run, > 40 %), restore hoạt động.
- E: Revit `tools/list` = 34, 3 lệnh Revit OK, library Revit không đổi.
- F: không nạp vào Civil 3D; instance 2 fail-fast rõ ràng.
- Docs cập nhật; `python -m unittest discover -s tests/skill-sync …` không liên quan (không chạm skill-sync). `AGENTS.md` = output engine.

## Risks
| Risk | Mitigation |
|---|---|
| Autoloader không nạp bundle (SECURELOAD/TRUSTEDPATHS) | thử `NETLOAD Contents\Loader.dll` tay; nếu OK → thêm docs thêm bundle path vào `TRUSTEDPATHS`; fallback registry demand-load (ADR-05) |
| Update 1.2 (.NET 10) tự cài qua Autodesk Access trước phase 5 | gate: kiểm `acdbmgd.runtimeconfig.json` trước khi bắt đầu; nếu net10 → phase 1 bổ sung TFM (ADR-05 §4) |
| Redeploy bridge Revit gây prompt "publisher could not be verified" | *Always Load*; hoặc không redeploy (server tương thích wire với bridge cũ) |
| Test D tạo nhiều run rác | drawing riêng; `registry` giữ `runs` — chấp nhận, đó là bộ nhớ |

## Security
Toàn bộ chạy trên máy dev, drawing test; opt-in bật tay; audit giữ nguyên.

## Next steps (ngoài scope plan này, ghi để mở đường)
- AutoCAD 2025 (`AutocadVersion=2025`, `AutoCAD.NET [25.0.1]`, `SeriesMin R25.0`) build-only; 2026 Update 1.2 / 2027 (.NET 10) khi máy dev nâng cấp.
- `HPAutoCad/build/` (ModularPipelines, Sourcy root riêng) cho `pack`; hoặc script publish/bundle đơn giản.
- Mở `Platform="AutoCAD*"` cho Civil 3D sau khi verify cách ly pipe.
