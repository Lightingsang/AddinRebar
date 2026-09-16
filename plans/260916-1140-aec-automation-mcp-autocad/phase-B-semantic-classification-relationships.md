---
phase: B
title: "Semantic — AEC classification + entity relationships"
status: completed
priority: P2
effort: "16h"
dependencies: [A]
---

# Phase B: Semantic — AEC classification + entity relationships

## Context Links
- [architecture.md](architecture.md) · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)

## Overview
`classify_aec_entities` turns records into AEC objects with confidence + evidence; `get_entity_relationships` derives connected / intersect / near /
inside / contains / touching / aligned / parallel / perpendicular between classified or raw entities.

## Architecture
- `HPAutoCad.Aec/Classification/`: `AecType` enum (Structural*, Architectural*, Door, Window, Room, Stair, Furniture, Pipe, Duct, CableTray, Equipment,
  Fixture, Terminal, Fitting, Unknown), `ClassificationRule` (match on layer wildcard(s), entity types, block name wildcard, closed/open, size range
  mm, aspect ratio, nearby text regex, weight), `ClassificationRuleSet` loaded from JSON (`Rules/aec-classification.default.json` embedded + optional
  override file `%AppData%\HPAutoCad\McpServer\rules\aec-classification.json`), `AecClassifier.Classify(records, ruleSet, spatialIndex)` →
  `AecObject { handle, aecType, confidence, evidence[], properties{width, depth, length, …} }`.
- `Relationships/`: `RelationshipDetector` over `AecObject`/records using `SpatialPredicates` + direction maths (aligned / parallel / perpendicular via
  `ParallelAngle`; connected = endpoint within `EndpointConnection` of the other's shape).

## Tools
| Tool | Category | Inputs | Output |
|---|---|---|---|
| `classify_aec_entities` | Aec | `filter`/`handles`, `disciplines[]`, `minConfidence`, `ruleSet` (name/path), `limit` | items = AecObject[]; summary by aecType |
| `get_entity_relationships` | Aec | `filter`/`handles` (source), `target` filter, `relations[]`, `tolerance`, `limit` | relationships [{source, target, relation, confidence, valueMm?}] |

## Success Criteria
- [x] Rule set is data (JSON, embedded default + user file); the harness scene (S-COL/S-BEAM/A-WALL/M-PIPE/door block) classifies with evidence strings — [report](reports/phase-B-semantic-live.md)
- [x] Unit tests: rule matching (wildcards, size ranges, aspect, rotated rectangles), confidence + nearby text, alternatives, discipline/minConfidence, rule-file validation, relationship predicates (16 tests)
- [x] Live: beam ↔ column `connected` (6 pairs, b2 → c4 gap 7 mm at confidence 0.65), pipe × beam intersect, self-set parallel de-duplicated, empty source refused

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
