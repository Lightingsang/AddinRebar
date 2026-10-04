---
name: hp-clean-code-scope-all-mcp-hosts
description: Since ADR-0007 (2026-10-04) every MCP folder is reviewed against HP_CLEAN_CODE_CORE.md + one host appendix, with Q-rules for runtime tools and seeds
metadata:
  type: project
---

Reviews of diffs in `McpShared/`, `HPAutoCad/`, `HPCivil3d/`, `HPNavis/`, `HPEtabs/`, `HPSap2000/`, `HPPowerBi/`, `HPExcel/`, `HPRobot/`, `HPTekla/` cite rule ids from `docs/clean-code/HP_CLEAN_CODE_CORE.md` (N, M, FM, C, S, D2/3/5/6, K1-5, CM, T1/3/4/7, P5-P9, Q-B1..3, Q-W1..5) and the folder's appendix in `docs/clean-code/host-appendix/` (A1-A16 autocad-civil, CO1-CO14 com-standalone, NI1-NI12 net48-inprocess, PB1-PB8 powerbi, RV1-RV7 revit). `HPRebar/` still uses REVITADDINAI_CLEAN_CODE_STANDARD.md for D1, D4, K6, T2, T5, T6, R1-R13, P1-P4. Only new or changed lines are in scope; no refactoring waves outside HPRebar.

**Why:** user approved plan 261004-1005 (ADR-0007) 2026-10-04; AI writes most new C# in the MCP folders.

**How to apply:** check P9 (contracts additive, `tools/list` byte-identical unless a tool changes); runtime tools and seeds must have no Q-B finding; host defects found in review are logged as H-xx in CLEAN_CODE_AUDIT.md §2a, not fixed in the same diff. Known H-01..H-08 (Power BI cloud opt-in/credentials/error bodies, Robot snapshot, SAP MTA, Tekla transaction + snapshot, Excel tests touching live registry).
