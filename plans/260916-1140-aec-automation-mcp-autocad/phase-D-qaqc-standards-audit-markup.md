---
phase: D
title: "QA/QC — CAD standards, drawing audit aggregate, issue markup"
status: pending
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
- [ ] Standards rules are data; audit output is stable-ordered and pageable; markup layer created if missing, dryRun supported

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
