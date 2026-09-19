---
name: phase-review-report-conventions
description: How the lead expects per-phase code-review reports for the MCP bridge plans (path, sections, score, status footer, read-only while a tester runs builds)
metadata:
  type: feedback
---

Phase reviews go to `plans/<plan>/reports/code-review-phase-XX.md` (the Navis plan uses this order; older plans used `phase-XX-code-review.md` — match the sibling files), read-only on source; never run `dotnet build`/`dotnet test`/harness when a tester agent runs them in parallel (when the lead's task text explicitly allows the gates, run them and say so in the Mode line) — take live/test numbers from the lead's `phase-XX-*.md` report and say so in the Mode line.

**Why:** the lead runs reviewer and tester side by side (2026-09-14, phases 4 and 5); a second build locks DLLs and duplicates work.

**How to apply:** sections in this order — Scope (with an explicit `git show --stat <sha> -- HPRebar/HPRebar/ McpShared/HPRebar.Mcp.Contracts/ …` parity proof), "Checks run (static)" table, Findings table (`# | Sev | File:line | Problem | Fix`, concrete fix text), per-item design/parity/standards verdicts, "Plan follow-ups (report only)", `**Score: X / 10**` with a one-paragraph justification, one `**What is good:**` line, then `**Status:**` / `**Summary:**` / `**Majors:**`. The lead appends a "Resolution" table under the report afterwards — keep finding numbers stable. Findings the lead deferred ("phase 5 decides") come back as design questions in the next phase; re-check them first — and fact-check the phase report's headline claims with a probe when the unit test cannot observe the property it is named after (phase-2 Navis serializer: "bounded while writing" was false, test and live run both green).
