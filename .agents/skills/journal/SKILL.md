---
name: bs:journal
description: "Write technical journal entries analyzing recent changes. Use for session reflections, change analysis, decision documentation. Invoke for technical session reflection or decision records. Keywords: journal, reflection, changes, session."
user-invocable: true
when_to_use: "Invoke for technical session reflection or decision records."
category: utilities
keywords: [journal, reflection, changes, session]
argument-hint: "[topic or reflection]"
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

# Journal

Use the `journal-writer` subagent to explore the memories and recent code changes, and write some journal entries.
Journal entries should be concise and focused on the most important events, key changes, impacts, and decisions.
Keep journal entries in the `./docs/journals/` directory.

**IMPORTANT:** Invoke "/bs:project-organization" skill to organize the outputs.

## Workflow Position

**Typically follows:** `/bs:ship` (journal after shipping), `/bs:cook` (journal after implementation), `/bs:fix` (journal after bug fix)
**Terminal skill** — no typical successor.
