---
phase: 4
title: "Live-verify harness --phase full (automatable vs manual-only) + registry loop + hồi quy 3 host + docs/CLAUDE.md/AGENTS.md"
status: pending
priority: P2
effort: "4h"
dependencies: [2, 3]
---

# Phase 4: Chứng minh end-to-end với ETABS 22 thật, lặp lại được; docs

## Context Links
- [ADR-02 §1–5](adr/adr-02-no-transaction-snapshot-tiers-and-opt-ins.md) · [ADR-04 §2–3](adr/adr-04-sta-worker-busy-cancel-units-guard-globals.md) · [ADR-05](adr/adr-05-identity-registry-packaging.md) · [red-team](reports/red-team-2026-09-16.md) hàng 1, 4, 5, 8, 9, 13, 14d
- Mẫu: `HPNavis/tools/harness/{README.md, run-live-verify.ps1, live-verify.py, harness-common.ps1}`; `McpShared/tools/{mcp-session.py, harness_common.py, README.md}`; Navis phase 5 (`../260915-0824-navisworks-mcp-2026/phase-05-…md`); `plans/260915-0824-navisworks-mcp-2026/reports/snapshot-tools-list.ps1`.
- CLAUDE.md mục "HPAutoCad MCP Bridge" + "HPNavis MCP Bridge" (khuôn docs).

## Overview
`live-verify.py --phase full` (cùng file đã lớn qua `spike`/`bridge`/`seeds`) chứng minh chuỗi Claude Code → `HPEtabs.Mcp.Server.exe` → pipe → `HPEtabs.McpBridge.exe` → COM → ETABS 22 trên máy dev, unattended tới mức có thể; manual-only tách rõ. Registry HIT/MISS→approve/quarantine→restore; **registry root giữ giữa các bước OFF/ON** (red-team #5). 3 exe kia không đổi. Docs. **Chỉ sau phase này mới được nói Verified.**

## Requirements
- Functional: `run-live-verify.ps1 -Phase full` (Windows PowerShell 5.1) + `live-verify.py --phase full` một phiên stdio (`mcp-session.py`) trên registry root cách ly (`HPEtabs/output/live-verify/registry`, env `HPETABS_MCP_Registry__LibraryPath/DbPath`) — **không wipe** giữa nhóm; start **bridge từ thư mục publish** (E16 lặp: self-check OK trên publish folder); UIA tick 2 checkbox + Attach; không start/kill ETABS; không `OpenFile/New*` trừ `-AllowModelCreate` (👤 cung cấp `model.EDB`).
- Ma trận scenario:

| Nhóm | Tự động? | Số (≈) | Nội dung |
|---|---|---|---|
| A — attach/context/publish | auto | 5 | bridge publish folder start → self-check OK (log) · `get_etabs_context` `isAttached=false` → Attach (UIA) → `isAttached, attachedPid, presentUnits` · `etabs://model/info` · `inspect_type ETABSv1.cSapModel` · `-32001` text "(a separate app, not inside ETABS)" trước tick |
| E — execute matrix | auto | 20 | R read · R + `SetSection` → **`isError` + `PREVIEW`** · dryRun W → `PREVIEW` (count không đổi) · W `AddByCoord` → `prerun\` file + `snapshot` = tên file (không `\`) + `Changed.Added=1` · W throw sau ghi → `rolledBack:false` + `snapshot` · label `../x` → tên file sanitized, không thoát bucket · D `SetModelIsLocked` OFF → JSON-RPC `-32001` · D ON → round-trip · guard ×6 (`ApplicationExit`, `new Helper()`, `Marshal`, `Expression.Call`, `System.IO.File`, `HPEtabs.McpBridge.BridgeEntry`) · compile error · cancel đua · timeout 5 s vòng lặp → `TimedOut` + text `TimeoutSemanticsHint` · **ETABS closed (👤 hoặc `-SkipClose`) → `-32003` ngay; mở lại → Attach không restart → OK** · not-attached (Detach UIA) → `-32003` · no-model (👤 hoặc `-SkipNoModel`) → `-32003` · units restore (`GetPresentUnits` sau = trước) · audit `started` trước forced save, `[destructive]` · UNC model (👤 `-UncModelPath`) → W `-32003` |
| S — seeds | auto | 12 + 1 | R1–R8 `run_tool`; W1–W3 `test_tool realRun=true` + `run_tool`; D1 `test_tool` → `PREVIEW`; ON `run_tool`; **OFF ×5 → `-32001` ×5, tool vẫn published** (registry giữ) |
| R — registry loop | auto | 10 | MISS ("đếm frame theo section") → ad-hoc → `get_run` → `toolify_run` → `propose_tool count_frames_by_section` (R) → `test_tool` → `publish_tool` → CLI `HPEtabs.Mcp.Server.exe registry approve … --by harness` → `tools/list_changed` ≤ 0.5 s → gọi theo tên · fragile tool (`InvalidOperationException`) 5 × fail → quarantined → `manage_tool restore` + `newVersion` (`ArgumentException`) → re-approve → 5 × fail vẫn published · `propose_tool` chứa `RunAnalysis` → từ chối · `propose_tool` `transaction:none` + `SetSection` → `PREVIEW` từ chối |
| X — hồi quy | auto | 6 | Revit/AutoCAD/Navis exe rebuild Release → `tools/list` byte-identical với snapshot phase 0 (số = `reports/phase-00-baseline.md`); nếu Revit 2026 mở: `get_revit_context` thật; 5 suite = baseline + HPEtabs tests |
| M — manual-only | 👤 | 5 | modal dialog trong ETABS → `-32002` (không SendKeys vào ETABS) · kill ETABS **giữa call** → bridge báo not-attached sau khi call trả/timeout, Attach lại OK · licence seat (ETABS thứ hai) · restore snapshot: mở `presave`/`prerun` trong ETABS, so mắt · `run_analysis` trên project thật |

- Docs: CLAUDE.md (hàng `HPEtabs/` Repository Layout + mục "HPEtabs MCP Bridge (Dynamic ETABS MCP Server)" theo khuôn Navis: app WPF độc lập + COM, 24 tool, 3 tier allow-list + snapshot vô điều kiện + forced save + 2 opt-in + `-32001`/`PREVIEW`, seed contract, harness, install/remove, **known gaps**: pid picker, `AutoLaunchBridge`, receiver-scoped guard carve-out (`GetProperty`/`File`), `-presave` policy (mtime/size heuristic), modal auto, ETABS 21/23, `export_table`/`restore_model_snapshot`, licence seat, UNC model; sửa câu chiều phụ thuộc liệt kê `HPEtabs/`), `AGENTS.md` regen bằng lệnh `_TO_PORTABLE` trong CLAUDE.md, `docs/codebase-summary.md`, `docs/system-architecture.md`, `docs/project-changelog.md` (`docs/development-roadmap.md` không tồn tại — bỏ qua), `HPEtabs/README.md` (cần ETABS cài để build bridge + bridge tests; publish folder; start bridge trước), `McpShared/README.md`; memory note.

## Architecture
```
run-live-verify.ps1 -Phase full ──▶ harness-common.ps1 (start HPEtabs.McpBridge.exe từ output/HPEtabs.McpBridge guarded; UIA Attach + 2 checkbox; kill own pids; never touches ETABS.exe)
        └─▶ python live-verify.py --phase full --server-exe … --bridge-dir … --model HPEtabs/output/live-verify/model.EDB [--revit-exe] [--autocad-exe] [--navis-exe] [--skip-no-model] [--skip-close] [--unc-model-path] [--allow-model-create]
                 └─▶ ../../../McpShared/tools/mcp-session.py + harness_common.Checklist (PASS/FAIL/SKIP + JSON)
```

## Related Code Files
- Modify: `HPEtabs/tools/harness/{live-verify.py, run-live-verify.ps1, harness-common.ps1, README.md}` (thêm `full`); `CLAUDE.md`, `AGENTS.md` (generated), `docs/{codebase-summary, system-architecture, project-changelog}.md`, `McpShared/README.md`, `HPEtabs/README.md`; memory.
- **Không** đổi `HPRebar/`, `HPAutoCad/`, `HPNavis/`.

## Implementation Steps
1. `--phase full` = A+E+S+R+X theo bảng; chạy **3 lần**; lỗi bridge/server → sửa + rerun; lỗi engine → `McpShared` additive + hồi quy 3 host + cập nhật bảng phase 0.
2. Hồi quy X (rebuild Release 3 exe kia; so snapshot phase 0).
3. Nhóm M: checklist 👤 trong `reports/phase-04-live-verify.md`.
4. Docs + memory; regen `AGENTS.md`; `python scripts/sync-agent-skills.py check` (không chạy được nếu thiếu `.skill-sync/config.json` — ghi).
5. `reports/phase-04-live-verify.md` + `plan.md` status.

## Todo List
- [ ] `--phase full` ≈ 53 scenario pass ×3 👤
- [ ] X hồi quy byte-identical + 5 suite = baseline
- [ ] M checklist 👤
- [ ] Docs/CLAUDE.md/AGENTS.md/README/memory
- [ ] Report + plan status

## Success Criteria
- [ ] `powershell.exe -File HPEtabs/tools/harness/run-live-verify.ps1 -Phase full -Runs 3` → **≈ 53 pass, 0 fail**, SKIP chỉ với `-SkipNoModel`/`-SkipClose`/không `-UncModelPath` (ghi số); mỗi run < 10 phút; bridge tự thoát; ETABS còn chạy.
- [ ] `tools/list_changed` ≤ 0.5 s sau approve; quarantine sau đúng 5 fail; restore + newVersion → 5 `ArgumentException` vẫn published; **5× D-off → `-32001`, `run_analysis` vẫn published** (registry không wipe).
- [ ] Revit/AutoCAD/Navis `tools/list` byte-identical với snapshot phase 0; 5 suite = `reports/phase-00-baseline.md`.
- [ ] `git diff --stat HPRebar/ HPAutoCad/ HPNavis/` rỗng; `McpShared/` chỉ diff bảng phase 0.
- [ ] Snapshot: sau chuỗi W có `prerun\` ≥ 2, `presave\` = số lần user save (0–1 trong run auto); sau 11 W `prerun\` = 10.
- [ ] Bridge chạy từ **publish folder**: log `MCP scripting self-check OK` (`Assembly.Location` khác rỗng).
- [ ] `AGENTS.md` regen qua `_TO_PORTABLE`; diff chỉ mục ETABS. Nhóm M: 5 ô có kết quả 👤 hoặc "chưa làm" — không ghi Verified cho ô chưa làm.

## Risk Assessment
- UIA trên cửa sổ WPF của ta: đã chứng minh với Navis/AutoCAD — thấp.
- ETABS bật dialog bất ngờ → busy; harness không đóng dialog của ETABS → README, rerun.
- `run_analysis` > 600 s → model bỏ đi nhỏ; đo phase 3.
- Model bỏ đi bị ghi (W + `realRun`): harness không restore tự động → user dùng `presave`; README.
- ETABS-closed scenario cần 👤 đóng/mở ETABS → `-SkipClose` mặc định **on** trong unattended.

## Security Considerations
- Harness không `ApplicationExit`, không `CreateObject`, không UNC (trừ `-UncModelPath` do user cấp); registry cách ly; `.mcp.json` không đụng.

## Next Steps
- Known gaps → CLAUDE.md (danh sách trên); Ribbon không áp dụng — cửa sổ bridge là bề mặt duy nhất.
