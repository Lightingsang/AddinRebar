---
phase: 4
title: "Live-verify harness HPCivil3d/tools/harness/: execute matrix, 12 seed trên scene tutorial, registry loop MISS → propose → test → publish → CLI approve → quarantine/restore, isolation hai chiều với AutoCAD 2026, hồi quy 4 host"
status: pending
priority: P1
effort: "8h"
dependencies: [2, 3]
---

# Phase 4: Chứng minh end-to-end với Civil 3D 2026 thật, lặp lại được, unattended

## Context Links
- [ADR-02 §Consequences](adr/adr-02-bundle-platform-civil3d-pipe-isolation.md) (isolation hai chiều) · [ADR-04](adr/adr-04-transactions-rebuilds-guard-civil3d.md) · [ADR-05 §Tests live](adr/adr-05-seed-library-mvp-12.md) · [phase-02](phase-02-civil3d-bridge-runtime-ribbon-mirror-tests.md) (harness pipe/ribbon đã có) · [phase-03](phase-03-server-profile-seeds-tests.md) (exe publish, smoke)
- Mẫu (tồn tại): `HPAutoCad/tools/harness/{run-live-verify.ps1 (params :9–17; isolation :96–132), live-verify.py (:493–497 args; 65 scenario), harness-common.ps1, README.md}`; `HPNavis/tools/harness/README.md`; `HPEtabs/tools/harness/live-verify.py` (`--phase`, registry loop 19 check); `McpShared/tools/{mcp-session.py, mcp-call.py, harness_common.py}` (canonical — import tương đối `../../../McpShared/tools/`); gate 4 host: `reports/snapshot-tools-list.ps1` (phase 0) + `plans/260916-2152-etabs-mcp-2026/reports/regression-tools-list-phase-04.py` (khuôn `KNOWN_CHANGED`).
- Scene: [E6](research/evidence-on-machine-2026-09-17.md) tutorial drawings (copy vào `HPCivil3d/output/live-verify/scene/`), `_Autodesk Civil 3D Corridor Template (Metric).dwt`.

## Overview
Một phiên stdio (`mcp-session.py`) trên **registry root cách ly** (`HPCivil3d/output/live-verify/registry`, env `HPCIVIL3D_MCP_Registry__LibraryPath/DbPath`) qua exe **publish** → pipe → bridge trong Civil 3D 2026 (start bằng dòng lệnh shortcut Autodesk E13 + `/b`), scene copy từ tutorial. Ma trận ≈ 70 scenario: execute matrix, 12 seed (đọc trên drawing đúng họ; W1/W2 dryRun → thật → `U`), registry loop đầy đủ, isolation **hai chiều** (Civil bundle không nạp AutoCAD/ADVS; AutoCAD bundle không nạp Civil — hồi quy; hai product cùng chạy → hai pipe), hồi quy `tools/list` 4 host. **Chỉ sau phase này mới nói Verified.** Windows PowerShell 5.1 (UIA), kills only what it started, không đụng bundle lạ của user.

## Requirements
- Functional: `run-live-verify.ps1 [-Exe] [-AutocadExe] [-OutDir] [-IncludeIsolation] [-OnlyIsolation] [-SkipAutocad] [-UseLiveRegistry] [-Runs n]` + `live-verify.py --exe --autocad-exe --scene-dir --out --only a,busy,disabled,nodoc,s,r,c,x --reset-verify-tools`; scene copy tự động (Program Files read-only; kiểm tên file bằng `ls` lúc chạy; nếu thiếu file → SKIP có lý do); mở drawing bằng script `.scr` (`_open "<path>"`), đóng không lưu (`_close _y`? → dùng COM `ActiveDocument.Close(False)` như AutoCAD harness).

| Nhóm | Tự động? | Số (≈) | Nội dung |
|---|---|---|---|
| A — khởi động/context | auto | 6 | Civil 3D start (E13 args) → pipe ≤ 420 s → self-check log · `get_civil3d_context` `civil3d.isCivilDocument`, `drawingUnit`, đếm alignments trên `Align-1.dwg` · `civil3d://document/info` · `inspect_type Autodesk.Civil.DatabaseServices.Alignment` · `-32001` trước tick · tick UIA |
| E — execute matrix | auto | 20 | read (alignment count) · W1 dryRun (`changed.added=3`, `rolledBack`) · commit · `U` qua COM → Count về cũ · same-label 2 run · exception (`throw`) → rollback · `none` + Add → lỗi + rollback · `manual` ≡ auto + log · guard ×6 (`Rebuild`, `DataShortcuts.SetWorkingFolder`, `ExportToDEM`, `ed.GetPoint`, `tr.Commit()`, `System.IO.File`) · compile error · `cancel_execution` đua · timeout 5 s vòng lặp → `TimedOut` + rollback · context khi script chạy < 1.5 s · busy (`LINE` COM → `-32002` sau 8 s → ESC posted → retry OK) · no-doc `-32003` · units Feet trên `Align-1.dwg` / Meters trên drawing từ template Metric (`/t`) · `Autodesk.Civil.*Exception` message · audit file có dòng |
| S — 12 seed | auto | 18 | R1 (`Align-1.dwg`) · R2 (≥ 1 alignment, `startStationLabel`) · R3 (entities + samples, `sampleStepMm`) · R4 (`Profile-1.dwg`) · R5 + R6 (`Surface-1.dwg`: 3 điểm trong, 1 ngoài → `outsideCount=1`) · R7 (`Corridor-1.dwg`: baselines/surfaces, không rebuild) · R8 (`Pipe Networks-1.dwg`: `includeParts`, `slopePercent`, `innerDiameterMm`) · R9 (`Parcel-1.dwg`: `area:null` + warning) · R10 (`Points-1.dwg`: `limit/offset`, filter) · W1 `test_tool` (dryRun) → `run_tool` thật → `U` · W2 `test_tool` → `run_tool` (polyline vẽ trước bằng execute) → `list_alignments` thấy → `U` · mọi seed `drawingUnit` có mặt · envelope < 64 KB |
| R — registry loop | auto | 18 | `search_tools` MISS ("đếm cogo point theo description") → ad-hoc `execute` → `get_run` → `toolify_run` → `propose_tool count_cogo_points_by_description` (Point, none) → `test_tool` 2/2 → `publish_tool` pending + `_review/*.md` nêu `HPCivil3d.Mcp.Server.exe registry approve` → `run_tool` refused pending → CLI approve `--by harness` → `tools/list_changed` ≤ 0.5 s → gọi theo tên · fragile tool (`InvalidOperationException` khi tên alignment lạ) ×5 fail → quarantined → `run_tool` refused → `manage_tool restore` + `propose_tool newVersion` (`ArgumentException`) → re-approve → 5 caller error vẫn published · `propose_tool` chứa `corridor.Rebuild()` → guard refused · `propose_tool` `transaction:none` + `CogoPoints.Add` → analyzer/validator refused |
| I — isolation hai chiều | `-IncludeIsolation` | 6 | (1) Civil 3D thứ hai → cửa sổ "already in use — another Civil 3D 2026 instance", bridge đầu vẫn phục vụ · (2) AutoCAD 2026 (`/product ACAD`) start → `loader.log` Civil **không** tăng, pipe `hpcivil3d-mcp-2026` không xuất hiện từ nó; pipe `hpautocad-mcp-2026` **có** (bundle AutoCAD nạp) · (3) Advance Steel (`/product "ADVS" /p "<<ADVS>>"`) → không nạp cả hai · (4) **coexist**: AutoCAD 2026 + Civil 3D 2026 cùng chạy → `get_autocad_context` (`host=autocad`) và `get_civil3d_context` (`host=civil3d`) cùng OK, W1 trên Civil không đụng AutoCAD (`get_drawing_info` AutoCAD không đổi) · (5) hồi quy AutoCAD harness isolation 2 (Civil không nạp bundle AutoCAD) — gọi `HPAutoCad/tools/harness/run-live-verify.ps1 -OnlyIsolation` sau khi bundle Civil deploy |
| X — hồi quy | auto | 6 | 4 exe rebuild Release → `tools/list` byte-identical với `reports/phase-00-tools-list-after-*.json` (`reports/regression-tools-list-phase-04.py` copy ETABS, 4 host, `KNOWN_CHANGED` rỗng trừ khi plan khác đã đổi) · 7 suite cũ = baseline · HPCivil3d 3 suite xanh |
| M — manual-only | 👤 | 3 | dialog modal Civil (vd. Toolspace panorama) khi script chờ → `-32002` (không SendKeys vào Civil) · corridor thật của user: `list_corridors` + `Rebuild` bị chặn thông báo rõ · mở drawing từ data shortcut project → `list_*` đọc reference object OK (không ghi) |

- Non-functional: ≈ 70 auto pass ×3 (`-Runs 3`) trong ≤ 15 phút/run; 0 skip trừ file scene thiếu (ghi lý do); harness không sửa file trong Program Files, không đụng `%AppData%\HPCivil3d\McpServer\` thật (trừ `-UseLiveRegistry`), không xoá bundle lạ; log `reports/phase-04-live-verify-run{1,2,3}.log`.

## Architecture
```
run-live-verify.ps1 ──▶ harness-common.ps1 (Start-AcadWithBridge -Product C3D -Profile '<<C3D_Metric>>' /ld AecBase.dbx /b bridge.scr; Answer-SecureLoad; Set-OptIn UIA trên cửa sổ HPCivil3d; COM ESC/U/Close(False) chỉ với pid của mình)
        ├─▶ python live-verify.py --exe output\HPCivil3d.Mcp.Server\HPCivil3d.Mcp.Server.exe --scene-dir output\live-verify\scene --autocad-exe ..\HPAutoCad\output\...\HPAutoCad.Mcp.Server.exe
        │        └─▶ ../../../McpShared/tools/mcp-session.py + harness_common.Checklist (PASS/FAIL/SKIP + JSON + exit code)
        ├─▶ [-IncludeIsolation] acad.exe /product ACAD · /product "ADVS" /p "<<ADVS>>" · Civil thứ hai · HPAutoCad run-live-verify.ps1 -OnlyIsolation
        └─▶ python reports/regression-tools-list-phase-04.py (4 host)
```

## Related Code Files
- Create: `HPCivil3d/tools/harness/{run-live-verify.ps1, live-verify.py, README.md}`; `reports/regression-tools-list-phase-04.py` (copy ETABS + `etabs` host + rỗng `KNOWN_CHANGED`); `reports/phase-04-live-verify.md`, `reports/phase-04-tools-list-{revit,autocad,navis,etabs}.json`, log ×3.
- Modify: `HPCivil3d/tools/harness/harness-common.ps1` (`-Product/-Profile`, `Open-Drawing`, `Undo-Last`, `Close-NoSave` qua COM với pid check), `bridge.scr`.
- Không sửa: `McpShared/tools/*` (canonical; nếu thiếu tính năng → thêm **additive** và ghi rõ, như Navis phase 5).

## Implementation Steps
1. Copy AutoCAD `run-live-verify.ps1`/`live-verify.py` → token Civil; scene copy step; `--scene-dir`; nhóm A/E.
2. Nhóm S (18) trên 6 drawing; W1/W2 vòng dryRun → thật → `U` (COM `SendCommand("_U ")` trên pid mình, như AutoCAD).
3. Nhóm R (18) — copy khối registry loop AutoCAD/ETABS; tên tool `count_cogo_points_by_description`; fragile tool `alignment_length_by_name`.
4. Nhóm I (6) — `-IncludeIsolation`; gọi harness AutoCAD `-OnlyIsolation` (kiểm exe AutoCAD publish tồn tại, nếu không → SKIP ghi rõ).
5. Nhóm X — `regression-tools-list-phase-04.py` 4 host; 7 suite + 3 suite Civil.
6. `-Runs 3` → `reports/phase-04-live-verify.md` (bảng nhóm × run, số pass/skip/fail, thời gian, ghi chú M); code review → fix → chạy lại.

## Todo List
- [ ] A/E (26) · [ ] S (18) · [ ] R (18) · [ ] I (6, `-IncludeIsolation`) · [ ] X (6) · [ ] 3 run pass · [ ] M ghi nhận · [ ] Report + review

## Success Criteria
- [ ] `powershell -File HPCivil3d/tools/harness/run-live-verify.ps1 -IncludeIsolation -Runs 3` → mỗi run ≈ 70 PASS, 0 FAIL, SKIP chỉ khi scene thiếu (0 trên máy này); Civil 3D/AutoCAD/ADVS tự thoát; không process mồ côi.
- [ ] `python plans/260917-1633-civil3d-mcp-2026/reports/regression-tools-list-phase-04.py` → Revit/AutoCAD/Navis/ETABS byte-identical với phase-0 after (số tool ghi trong `reports/phase-00-baseline.md`).
- [ ] `HPAutoCad/tools/harness/run-live-verify.ps1 -OnlyIsolation` → 4/4 (isolation AutoCAD giữ nguyên sau khi bundle Civil tồn tại).
- [ ] Registry cách ly: `HPCivil3d/output/live-verify/registry/tools-library/Point/count_cogo_points_by_description/` tồn tại sau run; `%AppData%\HPCivil3d\McpServer\` **không** thay đổi mtime trong run (trừ `-UseLiveRegistry`).
- [ ] `reports/phase-04-live-verify.md` có bảng 3 run + nhóm M ghi "chưa làm"/kết quả; ADR-02/03/04 → **Accepted (verified live)**.
- [ ] 10 suite (7 cũ + 3 Civil) = baseline/xanh.

## Risk Assessment
| Risk | Mitigation |
|---|---|
| Civil 3D khởi động chậm (nạp 30+ DLL, plugin lạ) > 420 s | timeout tham số; log thời gian; `Answer-SecureLoad` mỗi 5 s |
| Tutorial drawing Imperial → số mm lớn nhưng đúng; đơn vị test phải theo `drawingUnit` | harness đọc `drawingUnit` từ R1 và kiểm theo đó (không hard-code Feet) |
| Plugin `Civil3dMcp`/`AutoCadMcp` cũ bật dialog khi mở Civil | harness log window titles lạ; nếu chặn → user disable bundle cũ (**không** do harness làm) — ghi README |
| `U` qua COM undo cả run trước (undo gộp) | mỗi W chạy sau `REGEN` như AutoCAD harness đã học (CLAUDE.md: "sau REGEN thì U gỡ đúng 1") |
| Isolation (2)/(3) chờ 180–300 s mỗi product | chỉ với `-IncludeIsolation`; chạy sau nhóm chính |

## Security Considerations
- Harness không bật gì ngoài checkbox opt-in trên cửa sổ của ta (UIA theo pid); không ghi `%AppData%\HPCivil3d\McpServer` thật; không sửa `.mcp.json`; không xoá bundle của user; scene chỉ là copy.

## Next Steps
- Phase 5 docs + publish + `.mcp.json` note; ghi known gaps (M chưa làm, S-09 area, rebuild policy).
