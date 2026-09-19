---
name: mcp-live-harness-review-gotchas
description: Non-obvious checks when reviewing the PowerShell + Python live-verify harnesses of the HP MCP bridges (Navis/AutoCAD) — get_tool publishedAt makes substring asserts tautological, Start-* helpers that throw before returning leave the host running, shared daily log tails need a time filter, PS 5.1 decodes python stdout as OEM, git `M` with empty diff = EOL only
metadata:
  type: project
---

Checks that found real issues in the Navis phase-5 harness review (2026-09-15) and will recur for every `run-live-verify.ps1` / `live-verify.py` pair:

- **`get_tool` output always carries `publishedAt` once approved** (`ToolRegistryQueryTools.cs`, `WhenWritingNull` hides it only before). Any `"published" in json.dumps(detail)` assertion is tautological after the first approve; demand `detail.get("status") == "published"`. Same for `"quarantined"` (today only matches `notes`, fragile).
- **Wrapper failure path:** a helper that starts the host and then throws before `return $p` (window not found, pipe never appeared) leaves the caller's `$proc` null → `finally { Stop-Host $proc }` is a no-op and the host keeps running. The older wrappers assign `$proc = Start-…` first; composed helpers (`Start-BridgeOn`) must `try { … } catch { Stop-… $p; throw }`.
- **Safety invariants must fail the run, not WARN**: plugin-folder restore, opt-in untick. Check `$allOk = $false` is set where the WARNING is printed.
- **Shared daily bridge log (`shared: true`)**: `Get-BridgeLogTail N | Where-Object {*text*}` counts lines from earlier same-day runs; require the `[datetime]::ParseExact(...) -ge $logStart` filter the wrapper already uses elsewhere.
- **PS 5.1 + python UTF-8**: `PYTHONIOENCODING=utf-8` fixes the Python side only; powershell.exe decodes native stdout with `[Console]::OutputEncoding` (OEM) → `→ … ≡` mojibake in logs/run-summary. Fix is `[Console]::OutputEncoding = [Text.Encoding]::UTF8` after the pwsh→powershell relaunch guard. Python-written `summary-*.json` are unaffected, which is why live reports still show clean arrows.
- **`git status` shows `M` but `git diff` is empty** on this machine (`core.autocrlf=true`, worktree LF): EOL-only change. Verify with `git show HEAD:<file> | diff - <file>` before believing a report line that says the file changed.
- **Counting checks**: count `check(` per `scenario_*` with a regex split — the lead's tables were off by one between groups (S/R 14/18 vs 15/17) while totals matched; docs, README and CLAUDE.md inherit the wrong split.
- **Isolated registry root for the CLI**: `McpServerHost.RunAsync` builds the `registry …` CLI from the same `CreateBuilder` (`AddEnvironmentVariables(profile.EnvPrefix)`), so `os.environ.update` + `subprocess.run([exe, "registry", …])` is enough — no extra flag needed; cite this instead of asking.
- **AGENTS.md drift**: regenerate CLAUDE.md through `pm._replace(pm._normalize_newlines(s), pm._TO_PORTABLE)` and compare — one late CLAUDE.md edit left AGENTS.md one token stale even though the lead had regenerated it.

**Why:** these harnesses are the only runtime proof the bridges get; a tautological check or a wrapper that leaves Roamer/acad.exe running silently weakens the "verified live" claim the docs then repeat.

**How to apply:** run these before the (a)–(f) checklist; the string/field greps (`ExecuteResult`, `ToolLifecycleTools`, `RunHistoryTools`, `PipeListener`, `BridgeRequestException`, host `*UndoDecision`/`*HeavyGate`) resolve every asserted literal in one pass.
