---
title: "HPNavis MCP Bridge 2026 (Navisworks Manage 2026, .NET Framework 4.8)"
description: "AI → MCP → Navisworks 2026 trong folder top-level riêng HPNavis/, engine chung ở McpShared/ đa mục tiêu net8.0;net48; tool surface nghiêng truy vấn/phân tích/báo cáo; dryRun = commit-then-Rollback có điều kiện"
status: in-progress
priority: P2
effort: 56h
branch: RebarVersion1
tags: [navisworks, mcp, roslyn, net48, named-pipe, registry, mcpshared]
created: 2026-09-15
revised: 2026-09-15
blockedBy: []
blocks: []
---

# HPNavis MCP Bridge 2026 — Plan

**Ngày:** 2026-09-15 · **Status:** in-progress — phase 0–3 committed; phase 4 done (12 seeds live, 24 tools); phase 5 planned; red-team 4 lens cùng ngày (36 finding → 17 dedup, 14 accept + 2 partial + 1 user) · mọi khẳng định API/runtime đã kiểm trên máy dev — [research/evidence-on-machine-2026-09-15.md](research/evidence-on-machine-2026-09-15.md) · Template: Stack-Aware 6-phase (phase 0 = engine chung; WPF gộp vào 2)

## Executive summary
- **Host khác hẳn hai host cũ:** Roamer.exe = .NET Framework 4.8 (E1). `McpShared/HPRebar.McpBridge.Core` (net8.0) **đa mục tiêu `net8.0;net48`** — probe: 2 shim `#if NET48` + 4 property type + Polyfill; Roslyn 5.9 scripting **đã chạy** trên 4.8.9181 với `AssemblyResolve` hẹp ([ADR-01](adr/adr-01-net48-host-multitarget-mcpbridge-core.md), E12–E13). Automation API chỉ open/append/save/print → ngoài tiến trình **loại** (E7).
- **Navisworks là công cụ rà soát:** geometry chỉ đọc. Ghi được (W1): selection set, viewpoint, comment, appearance override, hidden/required, clash test + status, TimeLiner task; ghép/lưu/xuất file + chạy clash = **heavy** (W2: không undo, chạy lâu → checkbox opt-in thứ hai host-side, pre-pass `HEAVY`, timeout 600 s qua profile, audit `started`+`[heavy]`); SQL nhúng/ComApi/transaction riêng cấm (W3) ([ADR-02](adr/adr-02-navis-transaction-dryrun-and-writable-surface.md), [ADR-04](adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md)). **12 seed** = 8 read-only + 3 ghi nhẹ + 1 heavy.
- **Transaction:** `BeginTransaction` có, **không rollback in-flight**; `Document.Rollback()` undo transaction **vừa commit của toàn document** → chỉ gọi khi `NextUndo == "MCP: <label>"` (rỗng → không gọi, không đụng undo của user); `manual` ≡ `auto`; `none` = wrap + fingerprint (E6; red-team Critical).
- **Thread:** `Application.Idle` + `MainThreadQueue` (Core; **`expireWithoutTicks` opt-in cho Navis** vì Idle im khi modal native — S-07) + `PostMessage(WM_NULL)`; quiescence = depth counter Progress + `!IsWindowEnabled(main)` + `IsActiveTransaction`; `context` trả busy ngay khi script đang chạy (E5).
- **Folder:** `HPNavis/` (`HPNavis.slnx`, `global.json`, `Directory.Build.props` dò thư mục cài qua registry `Navisworks API Runtime\23`, `HPNavis.McpBridge` net48, `HPNavis.McpBridge.Tests` net48, `HPNavis.Mcp.Server` net10, `HPNavis.Mcp.Server.Tests` net10, `tools/harness/`, `output/`); chỉ `ProjectReference ../McpShared/*` ([ADR-03](adr/adr-03-navisworks-api-reference-and-test-without-navisworks.md), [ADR-05](adr/adr-05-navis-plugin-packaging-deploy-identity.md)). Pipe `hpnavis-mcp-2026`, prefix `navis.`, env `HPNAVIS_MCP_`, `.mcp.json` `hprebar-navis`, registry root `%AppData%\HPNavis\McpServer\`. MVP không Ribbon (Add-ins menu mở cửa sổ).

## Design of record
[architecture.md](architecture.md) · ADR: [01 net48 multi-target](adr/adr-01-net48-host-multitarget-mcpbridge-core.md) · [02 transaction/dryRun/writable](adr/adr-02-navis-transaction-dryrun-and-writable-surface.md) · [03 API reference + test net48](adr/adr-03-navisworks-api-reference-and-test-without-navisworks.md) · [04 main thread/busy/heavy/guard/globals/trust](adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) · [05 packaging/deploy/identity](adr/adr-05-navis-plugin-packaging-deploy-identity.md) · Research: [evidence on-machine](research/evidence-on-machine-2026-09-15.md) · [researcher-01 web facts](research/researcher-01-navisworks-api-plugin-facts.md) · Red team: [reports/red-team-2026-09-15.md](reports/red-team-2026-09-15.md) · Kế thừa: [AutoCAD plan](../260913-0000-autocad-mcp-bridge-2026/plan.md), [Revit ADRs](../260912-1521-dynamic-revit-mcp-server-2026/adr/).

## Phases
| # | File | Status | Depends | Effort |
|---|---|---|---|---|
| 0 | [phase-00](phase-00-mcpshared-net48-multitarget-and-navis-contracts.md) — `McpShared` đa mục tiêu net48 (**bảng sửa engine authoritative, 14 mục**), `Net48Tests`, hằng/profile Navis, `MaxTimeoutSeconds` qua 3 site; gate byte-identical trên exe rebuild | **built + tested + reviewed + committed (2026-09-15)** — `fb65f25` (phase 0) + 0b deny-list gốc; 126 + 58 + 109 + 58 test; `tools/list` 33/24 identical; review 8.5/10 → 7/8 fix; Core `.cs` additions only (`reports/phase-00-report.md`) | — | 8h (≈5h) |
| 1 | [phase-01](phase-01-hpnavis-scaffold-plugin-spike-with-gate.md) — scaffold `HPNavis/`, plugin net48 + resolver hẹp + self-check + cửa sổ tối giản, **spike S-01…S-11 (gate)**: nạp/prompt, Roslyn, Idle/wake, `RollbackOwn` (kể cả transaction rỗng), modal/append/clash, plugin lạ, pre-pass | **built + verified live 2/2 (2026-09-15)** — plugin nạp Roamer không prompt, self-check OK, S-01…S-11 pass/kết luận (`reports/phase-01-spike.md`); 4 fix engine additive (Contracts net48, `expireWithoutTicks` + dequeue lock, VM) — 128/60/109/58 test, `tools/list` identical; review 7/10 → 12/15 finding fixed cùng ngày (`reports/code-review-phase-01.md`); commit `63d9454` | 0 | 10h (≈6h) |
| 2 | [phase-02](phase-02-navis-bridge-runtime-transactions-context-window.md) — runner (ma trận có điều kiện), fingerprint, serializer, context, heavy gate host-side, cửa sổ 2 checkbox, `HPNavis.McpBridge.Tests` net48; harness pipe ≥ 22 | **built + tested + verified live 2/2 (2026-09-15)** — `NavisUndoDecision`, bounded serializer, `HPNavis.McpBridge.Tests` 62, harness 43 check/run + no-doc (`reports/phase-02-bridge-runtime.md`); commit `a1b1b0b` | 0, 1 | 12h (≈5h) |
| 3 | [phase-03](phase-03-hpnavis-mcp-server-exe-profile-tools-tests.md) — exe `HPNavis.Mcp.Server` + `NavisHostProfile` (600 s) + 4 tool/prompt/resource + tests; `.mcp.json`; lần đầu client net10 ↔ bridge net48 thật | **built + tested + verified live (2026-09-15)** — exe published, 12 tests, stdio smoke 8/8 qua bridge thật (net10 ↔ net48 pipe ACL OK), `.mcp.json` `hprebar-navis` (local) (`reports/phase-03-server.md`); commit `3ef5c36` | 0 | 6h (≈2h) |
| 4 | [phase-04](phase-04-navis-seed-library-and-registry-per-host.md) — 12 seed, compile-check net48, structure test net10, registry live (heavy qua `run_tool`) | **built + tested + verified live (2026-09-15)** — 12 seed, compile-check net48 qua compiler của bridge (124), structure net10 (49), live 18/18 + heavy 2/2, `tools/list` 24; review 8/10 → Hi1, M1–M4 fixed (`reports/phase-04-seeds.md`) | 0, 2, 3 | 10h (≈3h) |
| 5 | [phase-05](phase-05-live-verify-harness-registry-loop-and-docs.md) — `McpShared/tools/` canonical, harness ≈ 62 scenario ×3, registry loop, hồi quy, docs/CLAUDE.md/AGENTS.md | planned | 2, 3, 4 | 10h |

## Key decisions (Claude tự chốt — đổi được)
| # | Vấn đề | Chốt | Lý do |
|---|---|---|---|
| 1 | ADR-01 | **A** — đa mục tiêu Core, additive (`#if NET48` + member mới) | E12/E13; B lặp ~2 000 dòng; C chết |
| 2 | dryRun / lỗi / timeout | `Commit()` rồi `Rollback()` **chỉ khi** `NextUndo == "MCP: <label>"`; W2 không dryRun; `manual` ≡ `auto` | E6 + red-team Critical |
| 3 | Heavy | cờ in-memory trên executor (không `BridgeSettings`), pre-pass `HEAVY` host-side, `MaxTimeoutSeconds` additive qua 3 site engine (Revit/AutoCAD = 120), audit `started` + `[heavy]`, marker `tags:["heavy"]`, heavy seed-only MVP | ADR-04 §3 |
| 4 | Globals / Guard Navis | `doc, app, units, ct, log, progress, args` (không `state`); guard + `System.Data`/`Document.Database`, `System.Linq.Expressions`/`Delegate`, `BeginTransaction`/`Transaction` | ADR-04 §4–5 |
| 5 | Plugin | 1 assembly net48 phẳng; `EventWatcherPlugin` + `AddInPlugin` (menu); **không** Ribbon/theme trong MVP | E2–E4, YAGNI |
| 6 | API ref & test | `Directory.Build.props` env → registry → ProgramW6432; seed compile-check trong test **net48**; không build được khi không cài (nói thẳng) | ADR-03 |
| 7 | Seeds / harness | 12 = 8 RO + 3 W1 + 1 heavy (bỏ `get_model_tree`, `append_model_file`, `export_clash_report`); `mcp-call.py`/`mcp-session.py` canonical ở `McpShared/tools/` | user 8–12; E8; chiều phụ thuộc |
| 8 | Version | Navisworks Manage 2026 duy nhất; Simulate → `HasClashModule=false`; Freedom không hỗ trợ | E1 |

## Top risks
| Risk | L×I | Mitigation |
|---|---|---|
| ~~Roslyn bind lỗi **trong Roamer**~~ — **đã qua** (S-02/S-10: 5 resolve, plugin lạ bật, không xung đột) | — | resolver allow-list + RequestingAssembly; **+ Contracts net48** (discovery bind trước resolver — D1) |
| `Rollback()` hoàn lại nhầm / không hoàn lại 1 loại edit | M×H | điều kiện `NextUndo`; S-05/S-05b/S-05c; hàng "W1?" |
| ~~`Idle` không bắn khi modal~~ — **xác nhận & xử lý** (S-07: timer expiry; S-08: depth về 0) | — | `expireWithoutTicks` + depth counter + `!IsWindowEnabled`; clash từ GUI chưa tự động hoá |
| Clash run > 600 s, không ngắt được; UI đóng băng | M×M | heavy gate, audit `started`, mô tả "save first", harness sample nhỏ |
| Guard vượt bằng reflection-by-expression ở Revit/AutoCAD (base list) | M×M | Navis profile đã cấm; base list = user quyết |
| ~~Prompt bảo mật khi nạp plugin unsigned~~ — **không có prompt (S-01)**; Roamer elevated ↔ server không elevated còn để phase 3 | L×M | harness kiểm `IsElevated`; docs |

## Quyết định user đã xác nhận (2026-09-15, theo khuyến nghị)
1. **Deny-list gốc `ScriptGuard`** (Revit/AutoCAD): thêm `System.Linq.Expressions`, `Expression`, `Delegate`, `CreateDelegate`, `Compile` — **làm, commit riêng sau phase 0**, có test guard + snapshot `tools/list` (tool surface không đổi; guard từ chối thêm mẫu). Ghi là bước **0b** trong phase 0 (không gộp vào gate byte-identical của phase 0).
2. **Ribbon tab "MCP Navis" bỏ khỏi MVP** — Add-ins menu; Ribbon sau phase 5 như AutoCAD 0.2.0.
3. **Bước tiếp theo:** `/bs:cook` phase 0.
Các "dừng và hỏi" còn lại chỉ phát sinh khi spike: S-01 chặn plugin unsigned; S-05 `Rollback()` không hoàn lại metadata; phase 1/5 đụng plugin lạ trong profile user.

## Red Team Review
**Session 2026-09-15** — 4 lens, 36 finding thô → 17 dedup: 1 Critical (`Rollback()` không scope), 8 High (`manual`/transaction lọt, heavy gate vượt bằng Expression, 600 s không tới registry, `Document.Database` SQL, cancel id-less, `none` không enforce, `BridgeSettings` persist, `Net48Tests`/seed-test infeasible, spike thiếu opt-in, danh sách sửa mâu thuẫn), 8 Medium. **14 accept, 2 partial, 1 user decision, 5 đề xuất reject có lý do.** Bảng đầy đủ + evidence + sweep: [reports/red-team-2026-09-15.md](reports/red-team-2026-09-15.md). **Whole-plan consistency sweep:** grep từ khoá cũ = 0 ngoài mục lịch sử; số seed 12 / tool 24 / 4 project nhất quán; bảng sửa engine chỉ ở phase 0 — **0 mâu thuẫn chưa giải quyết.**
