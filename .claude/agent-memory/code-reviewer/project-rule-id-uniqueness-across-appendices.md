---
name: project-rule-id-uniqueness-across-appendices
description: HP clean-code docs — rule ids must be unique across core, Revit standard AND the five host appendices; phase-1 gate only checked two files
metadata:
  type: project
---

Rule ids (N, M, FM, C, S, D, K, CM, T, P, Q, R + appendix prefixes A, RV, NI, PB, com-standalone) must be globally unique across `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md` and `docs/clean-code/host-appendix/*.md`. Phase-1 review (2026-10-04) found com-standalone C1–C14 colliding with core C1–C7; the lead's id gate compared only core + Revit standard.

**Why:** findings cite bare ids (`path:line — C5 — …`), so a duplicate prefix makes review output ambiguous.

**How to apply:** on any diff to these docs, run a regex over all seven files for `^\| <Id> \|` and assert no id appears twice; also check DEPENDENCY_RULES S1–S4 vs standard S1–S5 (pre-existing overlap). Related: present-tense claims about the runtime quality check before the analyzer ships ([[project-hp-clean-code-scope-all-mcp-hosts]]).
