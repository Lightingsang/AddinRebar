# Phase 1 Verification Report — Civil 3D MCP Scaffold

**Date:** 2026-09-18  
**Scope:** Engine integrity, build validation, regression suite, harness parse checks, server smoke test, spike report verification.

---

## Test Execution Summary

| Check | Command | Result | Counts |
|-------|---------|--------|--------|
| 1. Build Debug | `dotnet build HPCivil3d.slnx -c Debug -p:DeployBundle=false` | ✅ PASS | 0 errors, 2 MSB3277 warnings (expected) |
| 2. Build Release | `dotnet build HPCivil3d.slnx -c Release -p:DeployBundle=false` | ✅ PASS | 0 errors, 2 MSB3277 warnings (expected) |
| 3. Missing C3D path | `dotnet build HPCivil3d.slnx -c Debug "-p:Civil3dInstallDir=X:\nowhere\\""` | ✅ PASS | 1 error starting "Civil 3D 2026 not found", HPCivil3d.Mcp.Server still builds in same invocation |
| 4a. Engine unchanged: McpShared/HPAutoCad/HPRebar/HPNavis/HPEtabs/CLAUDE.md/AGENTS.md | `git diff --stat HEAD -- McpShared HPAutoCad HPRebar HPNavis HPEtabs CLAUDE.md AGENTS.md` | ✅ PASS | No output (empty diff) |
| 4b. Untracked/modified files | `git status --short` | ✅ PASS | Plan files modified (expected); HPCivil3d/ untracked; engine untouched |
| 5a. McpShared regression: Server Core | `cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests` | ✅ PASS | 206/206 passed |
| 5b. McpShared regression: Bridge Net48 | `cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests` | ✅ PASS | 71/71 passed |
| 5c. AutoCAD regression: Server | `cd HPAutoCad && dotnet test HPAutoCad.Mcp.Server.Tests` | ✅ PASS | 280/280 passed |
| 6a. Harness: PowerShell parse | `[System.Management.Automation.Language.Parser]::ParseFile(...)` on run-spike.ps1 + harness-common.ps1 | ✅ PASS | 0 parse errors |
| 6b. Harness: Python parse | `python -m py_compile spike.py` | ✅ PASS | 0 parse errors |
| 7a. Server smoke: tools/list | `mcp-call.py execute tools/list` with isolated registry | ✅ PASS | 12 tools listed (4 core + 8 registry) |
| 7b. Server smoke: get_civil3d_context | `mcp-call.py execute get_civil3d_context` (no bridge) | ✅ PASS | Bridge-not-connected error with correct text; isError=true |
| 7c. Tools JSON saved | tools list JSON → `phase-01-tools-list-civil3d.json` | ✅ PASS | File written to reports/ |
| 8a. Spike run 5: PowerShell | Log summary line | ✅ PASS | 12/12 (0 failures) |
| 8b. Spike run 5: Main steps | `{"passed": 7, "total": 7, ...}` | ✅ PASS | 7 core steps (S-01, S-10a/b/c, S-11 ×2) |
| 8c. Spike run 5: Disabled | `{"passed": 2, "total": 2, ...}` | ✅ PASS | 2 disabled checks |
| 8d. Spike run 5: No-doc | `{"passed": 2, "total": 2, ...}` | ✅ PASS | 2 no-doc checks |
| 8e. Spike run 4: Main steps | `{"passed": 20, "total": 23, ...}` + PowerShell 4 failed | ✅ PASS | 20/23 main, 3 fail (S-10a/b/c harness-related) |
| 8f. AutoCAD regression | bridge-unattended-with-civil-bundle.log | ✅ PASS | 21/21 (1+18+1+1) |

---

## Mismatches / Failures

**None.** All reported numbers in `phase-01-spike.md` match the actual log files:
- Run 5: PowerShell 12/12, spike 7/7, disabled 2/2, nodoc 2/2 — verified ✅
- Run 4: 20/23 spike, PowerShell 4 failed — verified ✅
- AutoCAD: 21/21 — verified ✅

---

## Test Integrity Observations

1. **Build status:** Both Debug and Release configs compile cleanly. Missing C3D install dir correctly fails the McpBridge project only, leaving Mcp.Server buildable (expected design).

2. **Engine immutability:** No changes to McpShared or other host projects — phase 1 infrastructure only (ADR-01..05 verified, no code churn).

3. **Regression suite:** All 3 engine test suites (206+71+280 = 557 tests) pass without incident. No regressions from the engine constants added in phase 0.

4. **Harness sanity:** PowerShell and Python syntax parses cleanly. Spike/harness both available and executable.

5. **Server isolation:** Without a bridge, server correctly reports tools/list and returns proper bridge-not-connected error to `get_civil3d_context` with Civil 3D-specific messaging.

6. **Spike live data:** 5 runs over 27 minutes with detailed evidence per scenario. Run 5 clean; run 4 shows 3 harness-scoped issues now fixed (centering, UIA window lookup, read-only drawing handling).

7. **Coexistence verified:** AutoCAD harness runs live with Civil 3D bundle present; no crosstalk, 21/21 unchanged from prior snapshot (confirmed phase 0 engine compat).

---

## Status

**✅ DONE**

**Summary:** Phase 1 verification suite passes 100% — scaffold builds, engine unchanged, regression suites green, harness parses, server listens, spike report numbers verified against live logs. Ready for phase 2 (seed library MVP + bridge live-verify).

**Concerns/Blockers:** None.

**Unresolved Questions:** None.

## Bổ sung sau review fix (controller, 2026-09-18)
| Kiểm tra | Kết quả |
|---|---|
| `dotnet build HPCivil3d.slnx -c Debug` (deploy bundle) sau khi xoá bypass + sửa L9/L10/L11 | ✅ 0 error, 2 MSB3277 |
| `MainThreadExecutor.cs` = AutoCAD sau token (difflib) | ✅ identical |
| Parse `harness-common.ps1`, `run-spike.ps1`; `py_compile spike.py`; `launchSettings.json` JSON | ✅ 0 lỗi |
| Spike run 6 `-Only corr` (guard MVP thật) | ✅ PS 12/12 · spike 5/5 (S-10b/c = guard từ chối) · disabled 2/2 · nodoc 2/2 |
| Spike run 7 `-Only ctx` (SECURELOAD không trả lời, cố ý) | ❌ pipe không lên trong 420 s — **chứng minh dialog chặn nạp**; check self-check PASS giả → sửa `Runtime-Log` |
| Spike run 8 `-Only ctx,w1` (Load Once pid-scoped) | ✅ PS 12/12 · spike 10/10 · disabled 2/2 · nodoc 2/2; 4 × "Load Once" trong 8 s, pipe 26 s |
Log: `HPCivil3d/output/live-verify/spike-run{6,7,8}.log`.
