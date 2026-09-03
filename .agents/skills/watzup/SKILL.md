---
name: bs:watzup
description: "Generate session hand-off summaries from recent changes — what shipped, what's in flight, what's next. Use at end-of-day, before context handoff to a teammate or new session, or to produce a quick progress-tracking report on any active project. Invoke for end-of-session handoffs or progress summaries. Keywords: session, wrap-up, changes, review."
user-invocable: true
when_to_use: "Invoke for end-of-session handoffs or progress summaries."
category: utilities
keywords: [session, wrap-up, changes, review]
metadata:
  author: claudekit
  version: "1.0.0"
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# Wrap Up

Review my current branch and the most recent commits.
Provide a detailed summary of all changes, including what was modified, added, or removed.
Analyze the overall impact and quality of the changes.

**IMPORTANT**: **Do not** start implementing.
