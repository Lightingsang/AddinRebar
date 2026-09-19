---
name: review-report-shape
description: How the lead wants code reviews in this repo delivered — read-only, acceptance-criteria table, findings ranked with file:line, score /10, verdict, Status footer
metadata:
  type: feedback
---

Reviews here are read-only and evidence-first: never launch AutoCAD/Revit/Navisworks or run the `*.ps1` harnesses yourself — the controller/tester supplies build + live numbers; you may read logs (`%LocalAppData%\HP*\McpBridge\logs\loader.log`) to cross-check them.

**Why:** the live harnesses take minutes, lock DLLs and can leave the host app in a changed state (workspace/theme are profile-persisted); a reviewer running them in parallel with the tester causes "acad already running" refusals and stale results.

**How to apply:** report file goes to `plans/reports/code-review-<date>-<slug>.md` when the lead names it; structure = scope table → acceptance-criteria table (one row per lettered criterion, verdict + `file:line` evidence) → findings ranked Critical/High/Medium/Low/Info with `file:line` + concrete fix → positives → score /10 + verdict. End the chat reply with `**Status:** DONE | DONE_WITH_CONCERNS`, `**Summary:**`, `**Concerns/Blockers:**`. Previous review reports live beside the plan (`plans/<plan>/reports/*-code-review.md`) — check which earlier findings the diff closes and say so.
