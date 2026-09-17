---
title: "HPEtabs MCP 2026 (ETABS 22 host for the shared HP MCP engine)"
description: "AI → MCP → ETABS 22 (v22.7.0.4095, OAPI 2.10) trong folder top-level riêng HPEtabs/; bridge = WPF app độc lập giữ 1 COM attachment; không transaction → snapshot vô điều kiện + 3 tier allow-list R/W/D + 2 opt-in; engine McpShared/ chỉ thêm hằng/profile/hint"
status: completed
priority: P2
effort: 28h
branch: RebarVersion1
tags: [etabs, mcp, roslyn, com, named-pipe, registry, mcpshared, snapshot]
created: 2026-09-16
revised: 2026-09-16
blockedBy: []
blocks: []
---

# HPEtabs MCP 2026 — Plan

**Ngày:** 2026-09-16 · **Status:** completed — phase 0 **done 2026-09-16**, phase 1 **done 2026-09-17** (scaffold cả 2 exe + spike E9–E20 với ETABS 22 thật; [spike report](reports/phase-01-spike.md)); phase 2 **done 2026-09-17** (tier semantic từ fixture, snapshot vô điều kiện, fingerprint, path policy; live `-Phase bridge -Runs 2` 48 ×2, review round fixed; [report](reports/phase-02-bridge-runtime.md)); phase 3 **done 2026-09-17** (12 seed, `tools/list` 24, live `-Phase seeds` incl. `run_analysis` thật + E10 đóng; [report](reports/phase-03-seeds.md)); phase 4 **done 2026-09-17** (`-Phase full -Publish -Runs 3` → 3 × 102 PASS từ publish folder, registry loop live, hồi quy 3 host; [report](reports/phase-04-live-verify.md)) — **PLAN COMPLETE** · revised sau red-team cùng ngày · mọi khẳng định OAPI/runtime có nguồn — [evidence E1–E8](research/evidence-on-machine-2026-09-16.md) + CHM › topic; còn lại `[chưa xác minh]` · ck CLI không có → file plan viết trực tiếp, như các plan trước · Template: Stack-Aware (phase 0 = engine chung; server gộp vào phase 1; WPF gộp vào 2)

## Executive summary
- **Host thứ tư, kiểu thứ ba:** ETABS.exe = COM LocalServer out-of-process (E3), API = `ETABSv1.dll` netstandard2.0 (E2), không add-in nạp lúc khởi động (E4) → bridge = **WPF app độc lập** `HPEtabs.McpBridge` (net8.0-windows, publish **folder**) giữ **một** attachment `Helper.GetObject` tới instance user đang mở (không pid picker; > 1 ETABS → chỉ dẫn "Tools › Active Instance for API"); pipe `hpetabs-mcp-22` + `McpBridgeHost`/`RequestDispatcher`/`BridgeClient` không đổi ([ADR-01](adr/adr-01-standalone-bridge-app-vs-in-process-server.md)). Server `HPEtabs.Mcp.Server` (net10, single-file) hình dạng `HPAutoCad.Mcp.Server`, **không** tham chiếu `ETABSv1.dll`.
- **Không transaction/undo** (CHM › "Information for Plugin Developers"): 3 tier từ **bảng allow-list** sinh từ CHM index (fixture `etabs-oapi-tiers.txt`; R = `Get*/Is*/Has*/Count/RefreshView/AnalysisResults*` không path; D = path-taking + `Start/Modify/Merge/Reset/Clear/Rename/Show/Export/Import/Replicate*` + lock/analysis/file/delete; còn lại W; không rõ = D). W/D: **snapshot `.EDB` vô điều kiện** (forced `File.Save()` + copy, trong budget, UNC refused, `-presave` có điều kiện) — `snapshot` = tên file; preview (`none`/dryRun trên W/D) = `isError` + `PREVIEW`, không chạy; D với checkbox "Allow destructive operations" OFF = JSON-RPC `-32001`; R drift = cảnh báo, không lỗi — [ADR-02](adr/adr-02-no-transaction-snapshot-tiers-and-opt-ins.md).
- **Version = 22** (CSI đánh số; E1): pipe/env/`DefaultVersion`/`ValidVersions [22]`; "2026" trong tên folder chỉ là quy ước. 👤 **User xác nhận "22" là đích** (có lần nói "ETABS 2022").
- **API từ thư mục cài** (không NuGet, E5): props dò env → CLSID `LocalServer32` (E3, giá trị path thuần) → `%ProgramW6432%`; runtime `AssemblyLoadContext.Resolving`; **bridge + bridge tests cần ETABS cài**; server + server tests build mọi máy, seed compile-check `Assert.SkipWhen` quan sát được — [ADR-03](adr/adr-03-etabsv1-reference-and-test-without-etabs.md).
- **Thread/đơn vị/guard:** STA foreground worker, control lane Attach/Detach, liveness `Process.Exited` → `-32003` ngay; `MainThreadQueue` tick = vòng lặp; `ct` không ngắt call OAPI (`RunAnalysis` — CHM); ép `kN_mm_C`; guard gọn (`Helper`, `MessageBox`, 7 member `cOAPI`, namespace bridge/`Core.Host`); hint engine thay text add-in — [ADR-04](adr/adr-04-sta-worker-busy-cancel-units-guard-globals.md).
- **Định danh:** HostId `etabs`, "HPEtabs MCP", `%AppData%\HPEtabs\McpServer\`, `.mcp.json` `hprebar-etabs` (👤); **12 seed** = 8 R + 3 W + 1 D; 24 tool — [ADR-05](adr/adr-05-identity-registry-packaging.md).
- **Ràng buộc cứng:** không đổi hành vi HPRebar/HPAutoCad/HPNavis; `McpShared` chỉ additive (bảng phase 0); `HostProfile.Revit` mặc định; `tools/list` + schema 3 host byte-identical; không secret/path máy; wording nghiêm — **Verified chỉ sau phase 4 với ETABS thật**.

## Design of record
[architecture.md](architecture.md) · ADR [01](adr/adr-01-standalone-bridge-app-vs-in-process-server.md) · [02](adr/adr-02-no-transaction-snapshot-tiers-and-opt-ins.md) · [03](adr/adr-03-etabsv1-reference-and-test-without-etabs.md) · [04](adr/adr-04-sta-worker-busy-cancel-units-guard-globals.md) · [05](adr/adr-05-identity-registry-packaging.md) · Research: [evidence](research/evidence-on-machine-2026-09-16.md) · [researcher-01](research/researcher-01-etabs-oapi-facts.md) (sửa: #13 save-as → `[chưa xác minh]`; unlock-xoá-kết-quả → `[chưa xác minh trong CHM]`; namespace `ETABSv1`) · [researcher-02](research/researcher-02-mcpshared-seam-and-autocad-mirror.md) (sửa: globals theo user; option A bị loại; props key sai) · Red team: [reports/red-team-2026-09-16.md](reports/red-team-2026-09-16.md) · Kế thừa: [Navis plan](../260915-0824-navisworks-mcp-2026/plan.md), [AutoCAD plan](../260913-0000-autocad-mcp-bridge-2026/plan.md).

## Phases
| # | File | Status | Depends | Effort |
|---|---|---|---|---|
| 0 | [phase-00](phase-00-mcpshared-additive-etabs-contracts-and-engine-tests.md) — 13 sửa engine additive (hằng/profile/`EtabsInfo` 10 field/`Snapshot` tên file/`AnalyzeRequest.Transaction`/2 hint/`executionDisabledMessage`/`HostVersion` từ profile) + test #14 `-32001` không thành run; gate byte-identical (+ #15 `global::` guard, #16 `Snapshot` strip sau review) | **completed 2026-09-16** — [report](reports/phase-00-report.md) · [review 9/10](reports/code-review-phase-00.md) · [tests](reports/test-report-phase-00.md) | — | 4h |
| 1 | [phase-01](phase-01-hpetabs-scaffold-props-bridge-skeleton-com-attach-spike.md) — scaffold **cả 2 exe + 2 test project**, props CLSID, `EtabsHostProfile` + 4 tool, spike E9…E20 qua stdio `live-verify.py --phase spike` (gate) 👤 | **completed 2026-09-17** — [spike](reports/phase-01-spike.md) · [review](reports/code-review-phase-01.md) · [tests](reports/test-report-phase-01.md) | 0 | 8h |
| 2 | [phase-02](phase-02-etabs-bridge-runtime-executor-tiers-snapshot-window.md) — runtime: fixture tier (semantic), snapshot, fingerprint, path policy, `PREVIEW`/`-32001`, liveness, cửa sổ 2 checkbox; bridge tests 184 (cần ETABS) | **done 2026-09-17** | 0, 1 | 8h |
| 3 | [phase-03](phase-03-etabs-seed-library-and-registry-per-host.md) — 12 seed, compile-check trong server tests (skip quan sát được), structure test, `test_tool realRun=true` cho W | **done 2026-09-17** | 0, 2 | 4h |
| 4 | [phase-04](phase-04-live-verify-harness-registry-loop-and-docs.md) — `--phase full` ≈ 53 scenario, registry loop (registry giữ qua OFF/ON), hồi quy 3 host, docs/CLAUDE.md/AGENTS.md | **done 2026-09-17** | 2, 3 | 4h |

## Key dependencies / constraints
- `HPEtabs/` chỉ `ProjectReference ../McpShared/*`; không `HPRebar/`, `HPAutoCad/`, `HPNavis/`, `HPCivil3D/` (không tồn tại hôm nay); prefix `HPEtabs.*`. Máy dev: ETABS 22 v22.7.0.4095, `ETABSv1.dll` 2.10.0.0 (E1). Spike cần 👤 mở ETABS với model bỏ đi + duyệt 2 probe ghi.
- Licensing: attach vào instance đang chạy giả định không tốn seat `[chưa xác minh]`; `CreateObject` cấm. **User cần biết** (red-team): UNC model bị từ chối cho W/D; forced save không có checkbox riêng; bridge tests cần ETABS cài.
- **Cross-plan:** `260916-1140-aec-automation-mcp-autocad` (in-progress) đang sửa `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs` (uncommitted) và số test/tool AutoCAD còn đổi → phase 0 append cuối file, số baseline đo lúc chạy (`reports/phase-00-baseline.md`); merge caution, không block.

## Top risks
| Risk | L×I | Mitigation |
|---|---|---|
| Call COM kẹt khi modal / `RunAnalysis` dài; `ct` không ngắt | H×M | `IsWindowEnabled` pre-check; grace 8 s; `TimeoutSemanticsHint`; D 600 s; E15 |
| Forced `File.Save()` ghi đè file user; save-as chưa rõ | M×H | `-presave` khi user đã save; UNC refused; snapshot trong budget; E9/E11 |
| Fixture tier sai ở member biên | M×M | sinh từ CHM + review tay + test pin member red-team #2; fingerprint add/delete; unbound → D |
| Bridge tests cần ETABS → không chạy trên máy khác | L×L | server tests + seed compile-check skip quan sát được; README |

## Red Team Review
### Session — 2026-09-16
Findings: 36 raw → 14 (14 accepted incl. 3 partial; 3 sub-points rejected) · severity 3 Critical / 7 High / 4 Medium · [reports/red-team-2026-09-16.md](reports/red-team-2026-09-16.md)

| # | Finding | Severity | Disposition | Applied To |
|---|---|---|---|---|
| 1 | Hwnd chết + attach qua queue → kẹt `-32002` | Critical | Accept | ADR-04 §1–2, ADR-02 §5, phase 1 E19, phase 2, phase 4 |
| 2 | Tier prefix bỏ sót member ghi; `Count()` sai | Critical | Accept | ADR-02 §1–2, phase 2 fixture/tests, phase 3 |
| 3 | `ExecuteRequest.Snapshot` không set được; full path lên wire | Critical | Accept | ADR-02 §3, phase 0 #5 (tên file), phase 1 tool |
| 4 | Preview `isError=false` đánh `tested` | High | Accept | ADR-02 §1 (`PREVIEW`, `realRun=true`, `analyze`), phase 0 #6–7, phase 3 |
| 5 | D-off/drift thành fail → quarantine oan | High | Accept | ADR-02 §1 (`-32001`), phase 0 #14, phase 3/4 "5× OFF" |
| 6 | Snapshot ngoài budget; text timeout sai | High | Accept | ADR-02 §3, ADR-04 §3, phase 3 D1 mô tả |
| 7 | 3 text engine hard-code add-in | High | Accept | phase 0 #10–12, ADR-04 §3, ADR-05 §1 |
| 8 | Forced save đè UNC; `-presave` trùng/đẩy | High | Accept (partial) | ADR-02 §3, phase 2 snapshot tests, phase 4 |
| 9 | Bridge single-file → Roslyn chết | High | Accept | ADR-05 §3, ADR-03 §2, phase 1 E16 |
| 10 | Stub + `Assert.Skip` trên project không build | High | Accept (modified) | ADR-03 §3–4, phase 2/3 tests |
| 11 | Guard lặp base; namespace bridge/Host lọt; `GetProperty` | Medium | Accept (partial) | ADR-04 §5, phase 0 #8–9, phase 3 R3 |
| 12 | `EtabsInfo` 19 field trùng | Medium | Accept | phase 0 #4 (10 field) |
| 13 | Gold plating: `AutoLaunchBridge`, pid picker, 9 script, phase 3 riêng, 56h | Medium | Accept | ADR-01 §4, ADR-05, phases 0–4 (28h), 1 harness |
| 14 | Lặt vặt: baseline, `HostVersion` profile, audit `started`, label, `args` path, text | Medium | Accept | phase 0 #13, ADR-02 §3–4, ADR-04 §1, phase 2, phase 4 |

### Whole-Plan Consistency Sweep
Files reread (13): `plan.md`, `architecture.md`, `adr/adr-01…05`, `phase-00…04`, `reports/red-team-2026-09-16.md` (authoritative, không sửa). Decision deltas checked: 14 accepted (bảng trên) → 14 áp dụng; 3 sub-point rejected → xác nhận **không** áp dụng (presave giữ có điều kiện, không checkbox thứ ba, guard ở Core). Reconciled stale references: 70 chỗ sửa (gộp/đổi số phase + mọi link; `56h`→28h; baseline cứng→`reports/phase-00-baseline.md`; bỏ `snapshot:false`/`ExecuteRequest.Snapshot`/pid picker/`AutoLaunchBridge`/9 script harness/stub/`Assert.Skip`/`modified=1`/text "inside ETABS"; `-32003` cho no-path/UNC; `HPCivil3D/` ghi "không tồn tại hôm nay"). Grep sweep cuối: mọi hit còn lại chỉ ở nghĩa "đã bỏ/known gap/rejected". **Unresolved contradictions: 0.**
