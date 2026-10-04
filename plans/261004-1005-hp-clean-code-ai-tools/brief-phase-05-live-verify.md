# Brief 261004-1225 — Phase 5: redeploy the bridges and live-verify the script-quality gate on the other hosts

> **Audience:** Claude Code. **Parent plan:** [plan.md](plan.md) (phases 1–4 done; Revit Verified 5/5). This brief adds **phase 5**.
> **Language:** files in English; the final chat report to the user in Vietnamese.

## Tóm tắt (tiếng Việt)

Phase 1–4 đã xong, nhưng chỉ bridge Revit được deploy lại nên chỉ Revit được kiểm tra chất lượng khi chạy; 9 host còn lại đang báo "quality not analysed". Việc của phase này:
1. Tổng quát hoá `reports/live-verify-quality.py` thành harness dùng chung `McpShared/tools/live-verify-quality.py --host <id>`.
2. Build Debug, deploy lại bridge cho 7 host có trên máy (AutoCAD, Civil 3D, Navisworks, ETABS, Robot nếu khởi động được, Excel, Power BI) rồi chạy harness trên từng host, chỉ với file nháp/tạo mới.
3. Ghi báo cáo theo host, cập nhật plan.md và CLAUDE.md, commit (không push). Lỗi phát hiện chỉ ghi log H-xx, không sửa. SAP2000 và Tekla ghi CHƯA TEST.

---

## 1. Read first

1. [plan.md](plan.md), [reports/live-verify-quality.md](reports/live-verify-quality.md), [reports/live-verify-quality.py](reports/live-verify-quality.py) — the Revit run this phase repeats.
2. `CLAUDE.md` sections of each host ("HPAutoCad …", "HPCivil3d MCP Bridge", "HPNavis MCP Bridge", "HPEtabs MCP Bridge", Robot, Excel, Power BI): build, deploy and launch rules.
3. Existing harness READMEs: `HPNavis/tools/harness/`, `HPAutoCad/tools/harness/`, `HPCivil3d/tools/harness/`, `HPEtabs/tools/harness/`, `HPRobot/tools/harness/`; shared helpers `McpShared/tools/{mcp-session.py, harness_common.py, README.md}`.
4. `docs/clean-code/HP_CLEAN_CODE_CORE.md` (rule ids Q-B1…Q-B3, Q-W1…Q-W5).

## 2. Requirement contract (agreed with the user 2026-10-04 — binding)

| Field | Content |
|---|---|
| **Expected output** | (a) `McpShared/tools/live-verify-quality.py --host <id>`: one host-neutral harness replacing the Revit-only script; (b) Debug bridges rebuilt and redeployed for the hosts below; (c) `reports/live-verify-quality-<host>.md` per host; (d) `plan.md` phase 5 row + results, CLAUDE.md "CHƯA TEST" notes removed for hosts that pass; (e) defects found logged as `H-09…` in `docs/clean-code/CLEAN_CODE_AUDIT.md` §2a. |
| **Hosts** | Run: **AutoCAD 2026, Civil 3D 2026, Navisworks Manage 2026, ETABS 22, Excel, Power BI Desktop**; **Robot 2026** only if it starts (COM `Robot.Application` is registered, but no exe at `…\Robot Structural Analysis Professional 2026\robot.exe` — find the real path or report why not). **SAP2000 27, Tekla 2025**: not installed → `CHƯA TEST`, no work. **Revit**: already Verified — do not touch the running Revit instance. |
| **Acceptance per host** (same as the Revit run) | 1. Context tool answers, execution enabled, the active document is a **new/scratch** document (never a user model). 2. `propose_tool` with commented-out code + `catch { }` → refused with `quality Q-B1` **and** `quality Q-B2`. 3. `propose_tool` with `var data = …` → accepted with `quality Q-W3` warning. 4. `test_tool` → 2/2 pass (dryRun; if the host's dryRun semantics differ, document the variant used and why — the script never touches the model). 5. `publish_tool` under the manual policy → `pending_approval`, review file contains `## Code quality` and `analysed: 0 error(s), 1 warning(s)`. Pass = 5/5. |
| **Harness rules** | Host table inside the harness (exe path under `bin/Debug/<tfm>/`, `EnvPrefix`, `ContextToolName`, a category that exists in that host's profile, version env var) — values taken from each `*HostProfile.cs`, not guessed; isolated registry per host under a temp folder (never the user's `%AppData%` registry), deleted after the run; JSON summary + exit code via `harness_common.Checklist`; `--host revit` reproduces the current Revit run. The old `reports/live-verify-quality.py` becomes a thin call or is removed in the same commit. |
| **Application handling** | Ask the user to save their work, then the agent may close and relaunch hosts and bridge apps. AutoCAD (and Excel) were open at 12:20 — **before closing any running host, list its open documents and stop to ask the user if any is not a scratch file or has unsaved changes**. Open only new/blank documents (or files under a scratch folder in the repo's `output/`). |
| **In scope** | Debug builds of bridges/servers, deploy per each host's documented mechanism, the shared harness, reports, plan/CLAUDE.md/audit updates. |
| **Out of scope** | Any production code change (walker, validator, bridges); fixing defects found (log only); Release packaging/installers; SAP2000/Tekla; the known gaps already in plan.md (offline-propose re-analysis, Q-W1 whole-`try` false positive). |
| **Commit policy** | Conventional commits, no AI reference, **no push**: (1) `test(mcp): …` harness; (2) `docs(plan): …` reports + plan/CLAUDE.md/audit. Generated registries, logs with machine paths and secrets are not committed. |

## 3. Facts already gathered (verify)

| # | Fact |
|---|---|
| F1 | Installed (path check 12:20): AutoCAD 2026, Civil 3D 2026 (`AutoCAD 2026\C3D\AeccDbMgd.dll`), Navisworks Manage 2026, ETABS 22, Excel (Office16), Power BI Desktop. Not found: SAP2000 27 (no COM registration), Tekla 2025. Robot: COM registered, exe path unknown. |
| F2 | Running at 12:20: `acad` (PID 28688 — AutoCAD or Civil 3D, check), `EXCEL`, `Revit` (leave alone). |
| F3 | Every server profile exposes `HostId`, `EnvPrefix` (`HPREBAR_MCP_`, `HPNAVIS_MCP_`, `HPEXCEL_MCP_`, `HPPOWERBI_MCP_`, `HPROBOT_MCP_`, …), `ContextToolName` (`get_<host>_context`), `Categories`, `CliExecutable` — see `McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs` and `HP*/**/Hosts/*HostProfile.cs`. |
| F4 | The Revit script sets `HPREBAR_MCP_Bridge__RevitVersion`, `…Registry__LibraryPath`, `…Registry__DbPath`; check the equivalent keys per host (`HostVersion` seeding, ETABS uses `22`). |
| F5 | Unattended harnesses exist for Navis, AutoCAD, Civil 3D, ETABS, Robot (`run-live-verify.ps1` / `run-bridge-unattended.ps1` / `launch-live-cad.ps1`); Excel and Power BI have none — launch by hand-written steps in the report. |
| F6 | Power BI has no seeds; the quality gate still applies to `propose_tool`. Its bridge needs a PBIDesktop instance — use a blank report. |
| F7 | ETABS/Robot/Excel snapshot before writes; the verify scripts are read-only (`transaction: "none"`) — confirm no snapshot is produced. |
| F8 | Civil 3D bridge files mirror AutoCAD (`HPCivil3d/tools/mirror-tokens.json`) — no source edits are expected in this phase; if any becomes necessary, stop (out of scope). |

## 4. Steps

1. Write the shared harness; run `--host revit` against the already-deployed Revit bridge **only if** the user confirms the Revit instance holds a scratch document; otherwise unit-check the host table and skip.
2. Per host, in this order (unattended first): Navisworks → AutoCAD → Civil 3D → ETABS → Robot → Excel → Power BI:
   build Debug → close host/bridge (§2 application handling) → deploy → launch on a blank/scratch document → run the harness → write `reports/live-verify-quality-<host>.md` (env, versions, deploy time, commands, 5-row table, notes) → close what you opened.
3. A failing check: capture the exact output, decide whether it is an environment issue (retry once) or a defect (log `H-xx` with path:line evidence); continue with the next host.
4. Update `plan.md` (phase 5 row, Results table, Known gaps), CLAUDE.md per-host notes, audit §2a; commit per §2.

## 5. Stop and report (Vietnamese)

1. Bảng kết quả theo host: PASS x/5, FAIL, CHƯA TEST (lý do).
2. Lỗi mới ghi log (H-xx) và đề xuất xử lý.
3. Các commit đã tạo (hash + message), xác nhận chưa push.
4. Ứng dụng nào agent đã đóng/mở, và trạng thái hiện tại của chúng.
