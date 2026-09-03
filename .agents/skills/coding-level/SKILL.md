---
name: bs:coding-level
description: "Set coding experience level for tailored output. Use for adjusting explanation depth, code complexity, and response format to user expertise. Invoke when response depth should match user expertise. Keywords: experience, level, explanation, format."
user-invocable: true
when_to_use: "Invoke when response depth should match user expertise."
category: utilities
keywords: [experience, level, explanation, format]
argument-hint: "[0-5]"
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

# Coding Level

Set your coding experience level for tailored explanations and output format.

## Usage

`/bs:coding-level [0-5]`

## Levels

| Level | Name | Description |
|-------|------|-------------|
| 0 | ELI5 | Zero coding experience - analogies, no jargon, step-by-step |
| 1 | Junior | 0-2 years - concepts explained, WHY not just HOW |
| 2 | Mid-Level | 3-5 years - design patterns, system thinking |
| 3 | Senior | 5-8 years - trade-offs, business context, architecture |
| 4 | Tech Lead | 8-10 years - risk assessment, business impact, strategy |
| 5 | God Mode | Expert - default behavior, maximum efficiency (default) |

## How It Works

1. Set `codingLevel` in `.claude/.ck.json`
2. Guidelines are **automatically injected** on every session start
3. No manual activation needed - it just works!

## Example

Set level 1 in `.claude/.ck.json`:
```json
{
  "codingLevel": 1,
  ...
}
```

Next session, Claude will automatically:
- Explain concepts and techniques clearly
- Always explain WHY, not just HOW
- Point out common mistakes
- Add "Key Takeaways" after implementations

## Optional: Manual Output Styles

For finer control, you can also use `/output-style` with these styles:
- `coding-level-0-eli5`
- `coding-level-1-junior`
- `coding-level-2-mid`
- `coding-level-3-senior`
- `coding-level-4-lead`
- `coding-level-5-god`
