---
phase: D
title: "QA/QC — CAD standards, drawing audit aggregate, issue markup"
status: completed
priority: P2
effort: "14h"
dependencies: [A, B, C]
---

# Phase D: QA/QC — CAD standards, drawing audit aggregate, issue markup

## Context Links
- [architecture.md](architecture.md) · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)

## Tools
| Tool | Category | Notes |
|---|---|---|
| `cad_standards_check` | Audit | `CadStandardsService` + rule config JSON (layer naming regex, entity→layer expectations, allowed colors/linetypes/lineweights, text/dim styles, text heights, block naming); issues `STD-nnnn` |
| `audit_aec_drawing` | Audit | aggregates geometry + standards (+ structural/arch/MEP/clash when those phases exist) → summary {critical, warning, info} + issues[] with issueId, category, severity, description, handles, locationMm, suggestedAction; sections selectable |
| `create_issue_markup` | Annotation | from issue objects (passed back by the AI) → circle/rectangle/revcloud/MLeader + issue id on a dedicated layer; never touches original geometry |

## Success Criteria
- [x] Standards rules are data; audit output is stable-ordered and pageable; markup layer created if missing, dryRun supported — live 63/63 + 67/67 (2026-09-16), [report](reports/phase-D-qaqc-live.md)

## Implementation notes (as built)
- `Standards/` (rule set JSON + pure checker over records and `DrawingTables`), `Issues/AuditIssue` (shared record, stable order), `Cad/AuditService` (one query, sections), `Cad/IssueMarkupService` (two-phase, layer on demand, colour by severity), `AecTools.Audit`; `RuleFileLocator` shared with the classification rules.
- `unused_layer` needs the whole drawing (empty filter → space all, not truncated); otherwise skipped with a warning.
- Structural/architecture/MEP/clash sections join the audit when phases E–H land (the section list is data).

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
