# 0001 — Adopt Pragmatic Clean Code governance

- **Status:** Proposed (2026-10-03)
- **Tags:** [PROJECT]

## Context
The Revit product line (HPRebar add-in, HPRebar.Core, Revit MCP) grew to ~37 k production lines across five features built in quick succession, much of it AI-generated. The audit ([CLEAN_CODE_AUDIT.md](../../clean-code/CLEAN_CODE_AUDIT.md)) found copied infrastructure, cross-feature cycles, Revit types in ViewModels and UI settings that never reach the model. Project rules existed (CLAUDE.md, development-rules.md, code-standards.md) but covered structure and style, not design judgement, and nothing tied review findings to a shared vocabulary.

## Decision
1. *Pragmatic Clean Code* (Ślusarczyk, 2026) is the design reference; its 293 rules are catalogued as PCC-001…PCC-293 in [PRAGMATIC_CLEAN_CODE_RULES.md](../../clean-code/PRAGMATIC_CLEAN_CODE_RULES.md).
2. The binding standard is [REVITADDINAI_CLEAN_CODE_STANDARD.md](../../clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md): selected PCC rules plus Revit [REVIT] and project [PROJECT] rules, with thresholds as review triggers, not limits.
3. Every new tool follows [TOOL_DEVELOPMENT_WORKFLOW.md](../../clean-code/TOOL_DEVELOPMENT_WORKFLOW.md); every change is reviewed with [CODE_REVIEW_CHECKLIST.md](../../clean-code/CODE_REVIEW_CHECKLIST.md); refactoring follows the waves of [REFACTORING_PLAN.md](../../clean-code/REFACTORING_PLAN.md) and is logged in [REFACTORING_LOG.md](../../clean-code/REFACTORING_LOG.md).
4. AI-generated code is unreviewed code (PCC-017) until the checklist is passed.
5. CLAUDE.md makes the four core documents mandatory reading for every session that changes `HPRebar/`.

## Consequences
- \+ Findings cite stable rule ids; reviews stop re-arguing taste. The repo, not session memory, holds the rules.
- − More reading per change; mitigated by the condensed standard and the checklist. Rules can be misapplied mechanically; the standard keeps the book's exceptions next to each rule.

## Alternatives
Keep only the existing project rules (rejected: no design vocabulary, already drifted from code). Linter-only (rejected: most findings are design-level).

## Rules
PCC-001, PCC-002, PCC-017, PCC-104, PCC-217.
