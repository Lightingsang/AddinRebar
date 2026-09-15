---
phase: 5
title: "Live verify harness (Navisworks 2026 thật) + registry loop + hồi quy Revit/AutoCAD + docs"
status: pending
priority: P1
effort: "10h"
dependencies: [2, 3, 4]
---

# Phase 5: Harness `HPNavis/tools/harness/` + vòng lặp registry live + docs

## Context Links
- [ADR-05 §2](adr/adr-05-navis-plugin-packaging-deploy-identity.md) (plugin lạ, gỡ) · [ADR-04 §3, §6](adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) (heavy qua `run_tool`, trust model) · [ADR-02](adr/adr-02-navis-transaction-dryrun-and-writable-surface.md)
- Mẫu: `HPAutoCad/tools/harness/{README.md, run-live-verify.ps1, live-verify.py, mcp-session.py, mcp-call.py, harness-common.ps1, run-server-smoke.ps1}`.
- Kết quả AutoCAD phase 5: `plans/260913-0000-autocad-mcp-bridge-2026/reports/phase-05-live-verify.md` (65 scenario) — chuẩn để so.

## Overview
Chứng minh toàn chuỗi Claude Code → `HPNavis.Mcp.Server.exe` → pipe → plugin → Navisworks 2026 trên máy dev, unattended, lặp lại được; registry HIT/MISS→approve/quarantine→restore; hai exe kia không đổi; docs. **Script host-neutral (`mcp-call.py`, `mcp-session.py`) chuyển thành bản canonical ở `McpShared/tools/`** — HPNavis import theo đường tương đối (MCP → McpShared, đúng chiều phụ thuộc); bản của AutoCAD **giữ nguyên** (không đụng `HPAutoCad/`), repoint là follow-up. Chỉ sau phase này mới được nói **Verified**.

## Requirements
- Functional: `run-live-verify.ps1` một phiên stdio (`McpShared/tools/mcp-session.py`) qua registry root cách ly (`HPNavis/output/live-verify/registry`, env `HPNAVIS_MCP_Registry__LibraryPath/DbPath`); mở Roamer với `Samples\gatehouse\gatehouse_pub.nwd`; UIA tick opt-in trên cửa sổ HPNavis (execution; heavy chỉ trong nhóm H, tắt lại sau); kill chỉ process mình mở; không lưu file mẫu (không `SaveFile`; đóng bằng kill sau khi `Undo` hết edit của harness).
- Scenario (≈ 62):

| Nhóm | Số | Nội dung |
|---|---|---|
| E — execute matrix | 18 | none/dryRun/commit · `auto` **rỗng** dryRun (undo user còn) · exception · none + modify (→ lỗi + `RolledBack=true`) · manual (≡ auto, log) · guard `MessageBox`/`Undo`/`BeginTransaction`/`Expression.Call`/`NavisworksCommand` · pre-pass `AppendFile` heavy OFF (`HEAVY`) · compile error · cancel đua · timeout 5 s · busy (Options modal qua UIA → `-32002` → đóng → retry OK) · `context` giữa execute dài → `-32002` < 1 s · `IsClear` → `-32003` · opt-in OFF → `-32001` · audit dòng cuối khớp |
| S — seeds | 12 + 1 | R1–R8, W1–W3 mỗi seed `run_tool` thật hoặc `test_tool` dryRun trên gatehouse; R6/R7 trên `Getting Started` sau H1 + 1 task tạo bằng script; H1: `test_tool` → từ chối rõ; heavy ON → `run_tool` chạy thật (Architecture vs MEP, tolerance 0; thời gian, không bị cắt 120 s); heavy OFF → `HEAVY` |
| R — registry loop | 10 | MISS ("đếm item theo `Item.Source File`") → ad-hoc → `get_run` → `propose_tool count_items_by_source_file` → `test_tool` → `publish_tool` (pending) → CLI `HPNavis.Mcp.Server.exe registry approve … --by harness` → `tools/list_changed` ≤ 0.5 s → gọi theo tên · fragile tool (`KeyNotFoundException`) 5 × fail → quarantined → `manage_tool restore` + `propose_tool newVersion` (`ArgumentException`) → re-approve → 5 × fail vẫn published · `propose_tool` chứa `TestsRunTest` → bị từ chối (chứng minh heavy seed-only) |
| C — context/resources/prompts | 5 | `get_navis_context` sau append (`IsBusy=false`, `HeavyOperationsEnabled`); `navis://models`; `registry://tools`; prompt `navis_query_template`; `inspect_type Autodesk.Navisworks.Api.Search` |
| X — hồi quy | 6 | Revit exe + AutoCAD exe **rebuild Release** → `tools/list` 34/24 byte-identical với snapshot phase 0; nếu Revit 2026 mở: `get_revit_context` thật; 3 suite cũ + `Net48Tests` + HPNavis tests: 96+109+58 nguyên số, HPNavis ≥ 40 + 11 |
| I — isolation (`-IncludeIsolation`) | 4 | Roamer thứ hai → pipe in use, log nêu "Navisworks 2026"; plugin lạ `NavisworksMCPPlugin` **bật** → self-check vẫn OK, resolve log không trả lời requester ngoài thư mục (S-10 lặp); tắt (`.disabled`) → khôi phục `finally`; gỡ plugin (xoá thư mục) → Roamer mở không log HPNavis |

- Docs: CLAUDE.md (hàng `HPNavis/` trong Repository Layout + mục "HPNavis MCP Bridge (Dynamic Navisworks MCP Server)" theo khuôn mục AutoCAD: runtime **.NET Framework 4.8**, tool surface 24, seed contract, heavy gate + trust model, harness, install/remove, known gaps; sửa câu chiều phụ thuộc liệt kê `HPNavis/`; ghi `McpShared/tools/` là script harness dùng chung), `AGENTS.md` regen bằng lệnh Python trong CLAUDE.md, `docs/codebase-summary.md`, `docs/system-architecture.md`, `docs/project-roadmap.md`, `docs/deployment-guide.md`, `McpShared/README.md` (net48 target + `tools/`), `HPNavis/README.md`, memory file.

## Architecture
```
run-live-verify.ps1 ──▶ harness-common.ps1 (start Roamer guarded, UIA opt-in trên cửa sổ HPNavis, kill own pids, toggle plugin lạ với finally)
        └─▶ python live-verify.py --exe Roamer.exe --model Samples\gatehouse\gatehouse_pub.nwd [--revit-exe] [--autocad-exe] [--include-isolation]
                 └─▶ ../../../McpShared/tools/mcp-session.py (một stdio session tới HPNavis.Mcp.Server.exe; env registry cách ly)
```
H1 clash trên `Getting Started` là bước nặng nhất — đo ở phase 1 S-08; nếu > 90 s → selection nhỏ (2 category) để harness < 12 phút; hospital 30 MB **không** dùng.

## Related Code Files
- Create: `McpShared/tools/{mcp-call.py, mcp-session.py, README.md}` (bản canonical; nội dung = AutoCAD hiện tại, docstring host-neutral); `HPNavis/tools/harness/{README.md, harness-common.ps1, run-live-verify.ps1, run-server-smoke.ps1, live-verify.py, pipe-scenarios.py (phase 1–2), run-bridge-unattended.ps1 (phase 2)}`.
- Modify: `CLAUDE.md`, `AGENTS.md` (generated), `docs/*.md`, `McpShared/README.md`, `HPNavis/README.md`; memory `navisworks-mcp-bridge-plan.md` (đã có — cập nhật trạng thái).
- **Không** đổi `HPAutoCad/tools/harness/*` (follow-up: repoint sang `McpShared/tools/`).

## Implementation Steps
1. Tạo `McpShared/tools/` (copy 2 script host-neutral); HPNavis harness import theo đường tương đối; copy `live-verify.py`/`harness-common.ps1` từ AutoCAD và sửa host (pipe `hpnavis-mcp-2026`, exe, env; bỏ SECURELOAD; opt-in UIA nhắm cửa sổ HPNavis; modal busy = mở Options qua UIA menu → Esc sau; **[chưa xác minh]** phím tắt Options — fallback dialog "Append" rồi Esc).
2. Scenario theo bảng; chạy 3 lần; sửa bridge/server theo lỗi; lỗi engine (nếu có) fix ở `McpShared` + hồi quy 3 host + cập nhật bảng phase 0.
3. Hồi quy X (rebuild Release hai exe kia; snapshot phase 0).
4. Docs + memory; regen `AGENTS.md`; `python scripts/sync-agent-skills.py check`.
5. `reports/phase-05-live-verify.md` (bảng scenario, thời gian, RAM, known gaps) + `plan.md` status.

## Todo List
- [ ] `McpShared/tools/` canonical + HPNavis harness
- [ ] ≈ 62 scenario pass ×3
- [ ] Hồi quy X
- [ ] Docs/CLAUDE.md/AGENTS.md/memory
- [ ] Report + plan status

## Success Criteria
- [ ] `pwsh HPNavis/tools/harness/run-live-verify.ps1 -IncludeIsolation` → **≥ 60 pass, ≤ 2 skip có lý do, 0 fail**, 3 lần liên tiếp; < 12 phút; Roamer tự thoát, không dialog treo.
- [ ] `tools/list_changed` sau `registry approve` ≤ 0.5 s; tool mới gọi được theo tên trong cùng session; `propose_tool` chứa W2 bị từ chối với lý do guard/heavy.
- [ ] Quarantine sau đúng 5 fail; restore + newVersion → không quarantine lại.
- [ ] Revit 34 / AutoCAD 24 `tools/list` byte-identical với snapshot phase 0 (exe rebuild, SHA mới); 3 suite cũ pass nguyên số.
- [ ] `git diff --stat HPRebar/ HPAutoCad/` = rỗng cho toàn plan (ngoại trừ CLAUDE.md/docs ở gốc); `git status McpShared/tools/` chỉ có file mới.
- [ ] `python scripts/sync-agent-skills.py check` không drift; `AGENTS.md` khớp CLAUDE.md qua `_TO_PORTABLE`.
- [ ] Không có `[heavy]`/`started` audit line ở host khác (grep audit Revit/AutoCAD nếu có chạy).

## Risk Assessment
- Navisworks hỏi "Save changes?" khi kill → harness `Undo`/`Rollback` hết edit rồi `taskkill /f` (file mẫu không bị ghi vì không `SaveFile`).
- H1 quá lâu → thu hẹp selection; > 600 s → known gap, `-IncludeHeavy` tuỳ chọn.
- Plugin lạ: bật/tắt trong harness → **hỏi user trước** (👤); khôi phục `finally`.
- UIA với cửa sổ WPF của ta trong Roamer — thấp.
- Roamer chạy elevated (license) → client không elevated bị từ chối kết nối (owner SID) → harness kiểm `IsElevated` trước và fail rõ.
