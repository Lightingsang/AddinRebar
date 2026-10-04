# Review — phase 1 (ADR-0007, core standard, appendices, agent rules)

Scope: uncommitted docs diff (24 files changed, 7 new). Read-only review; nothing edited except this report.
Score: **8/10** — the id move is clean and AGENTS.md is safe; one id collision blocks commit, three Mediums are cheap to fix.

## Checks run
| Check | Result |
|---|---|
| Rule-id gate (script over HEAD standard vs core + Revit standard) | 83 old ids, 0 missing, 0 duplicates; added P9 + Q-B1..B3 + Q-W1..W5 |
| Ids whose text changed | 19: the 13 intended (N5/N6/N7/N9, M6/M7, C5/C7, D2/D5, CM2/CM5, T1, FM1 — FM1 counted) + S1, S5, K1, CM3, P5 (see L1) |
| Contract fidelity vs brief §2 + D1–D6 | holds: 3 blocking / 5 warnings, "not analysed" + publish allowed under every policy (D3a: review file or publish message), scope list ×11 + HPGeo excluded, no refactor, hash-pinned allowlist, D4a interpretations in core §13 |
| `git diff AGENTS.md` | one hunk, byte-identical to CLAUDE.md except "Claude Code" → "the host coding agent"; nothing else lost |
| Relative links (ADR, core, standard, checklist, workflow, plan, audit, README, 5 appendices) | 0 broken; every backticked repo path exists |
| `path:line` citations sampled (24) | 20 exact, 4 weak (L3), 0 wrong. H-01..H-06 all confirmed in source |
| `sync-agent-skills.py check` | only pre-existing state: `hp-mcp-excel` conflict, 4 `equivalent-dual-drift`, etabs portable drift, 3 new-portable skills |
| Checklist §G-Revit / workflow for HPRebar | all R lines of HEAD kept (R11 absent before too); steps 4/10/11/13 keep the Revit commands verbatim |

## High
**H1 — appendix ids `C1…C14` collide with core `C1…C7` (Classes).**
[com-standalone.md:7-20](../../../docs/clean-code/host-appendix/com-standalone.md#L7-L20) defines C1 "one host attachment" … C7 "tier allow-list" while [HP_CLEAN_CODE_CORE.md:69-75](../../../docs/clean-code/HP_CLEAN_CODE_CORE.md#L69-L75) defines C1 "one responsibility" … C7 "inheritance". [CODE_REVIEW_CHECKLIST.md:31](../../../docs/clean-code/CODE_REVIEW_CHECKLIST.md#L31) cites "C1–C7" and [:63](../../../docs/clean-code/CODE_REVIEW_CHECKLIST.md#L63) "com-standalone C1–C14"; [powerbi.md:3](../../../docs/clean-code/host-appendix/powerbi.md#L3) "C1/C12". A finding "C5" is now ambiguous (presentation vs snapshot) — breaks core's own invariant "an id is defined in exactly one file" ([core:5](../../../docs/clean-code/HP_CLEAN_CODE_CORE.md#L5)); the gate script missed it because it only compared core + Revit standard.
Fix: rename to `CO1…CO14` (or `CX`) in com-standalone.md, powerbi.md:3, checklist:63 and the known-gap table; extend the id gate to all seven files (prefixes A, RV, NI, PB, CO, Q, plus core/Revit) and assert global uniqueness.

## Medium
**M1 — runtime check stated as current behaviour before phases 2–3 exist.**
[CLAUDE.md:21](../../../CLAUDE.md#L21) / AGENTS.md:21 ("`propose_tool` rejects…", "Every `*.Mcp.Server.Tests` project checks its embedded seeds"), the 10 skill blocks (e.g. [hp-mcp-revit SKILL.md:125](../../../.claude/skills/hp-mcp-revit/SKILL.md#L125) "`propose_tool` chạy bộ kiểm tra…"), [core:150](../../../docs/clean-code/HP_CLEAN_CODE_CORE.md#L150), [powerbi.md:17](../../../docs/clean-code/host-appendix/powerbi.md#L17). Today `ToolValidator` has no quality branch ([ToolValidator.cs:62-82](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs#L62-L82)), so an agent reading the skill trusts a gate that does not run and will never see the "quality not analysed" line it is told to expect (P8).
Fix: either land commit 2 (CLAUDE/AGENTS/skills) together with phase 2, or add one clause in each place: "enforced by servers/bridges built after the quality analyzer ships; until then self-check the code against core §13". Core §13 / ADR may stay normative ("must"), not descriptive ("is checked").

**M2 — workflow Definition of Done still Revit-only.**
[TOOL_DEVELOPMENT_WORKFLOW.md:67](../../../docs/clean-code/TOOL_DEVELOPMENT_WORKFLOW.md#L67) "every targeted Revit configuration", [:74-75](../../../docs/clean-code/TOOL_DEVELOPMENT_WORKFLOW.md#L74-L75) D1/D4 (HPRebar-only now), [:79](../../../docs/clean-code/TOOL_DEVELOPMENT_WORKFLOW.md#L79) R1–R4/R9, [:80](../../../docs/clean-code/TOOL_DEVELOPMENT_WORKFLOW.md#L80) T1–T4 (T2 HPRebar-only), [:82](../../../docs/clean-code/TOOL_DEVELOPMENT_WORKFLOW.md#L82) "Revit safety review (G)". Step 16 makes this list the acceptance gate, so a non-Revit tool cannot tick it honestly. Same for "before code" Q6 (P2, T2) and warning-sign row 10.
Fix: mirror the step 10/13 wording — "host build (Revit: every targeted configuration)", "host safety review (G + appendix)", "host boundaries respected (Revit: P2, P4, R1–R4, R9)", "T1, T3, T4 (+T2 HPRebar)", "D2/D3/D5 (+D1/D4 HPRebar)".

**M3 — Tekla snapshot failure only warned, cited as the rule's source.**
[net48-inprocess.md:28](../../../docs/clean-code/host-appendix/net48-inprocess.md#L28) NI12 cites [TeklaBridgeExecutor.cs:151-161](../../../HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs#L151-L161), where a failed `CreateSnapshot` is `Log.Warning` and the write proceeds — the defect class logged as H-03 for Robot and the opposite of com-standalone C5 ("a failed save or copy fails the run"). Per CLAUDE.md "logged, not silently fixed": add H-07 to [CLEAN_CODE_AUDIT.md §2a](../../../docs/clean-code/CLEAN_CODE_AUDIT.md) and a known-gap line under NI12; state NI12 as "a failed snapshot fails the run" so new code follows ETABS.

## Low
- **L1 wording drift outside the intended list:** S1 dropped "(bar shapes, request kinds that multiply)", S5 added "services", CM3 added the Q-B1 note, P5 source moved, §1 bullet "protecting Core" → "host-free code". Harmless, except **K1** lost the HPRebar anchors "stock length 11 700, bar-position limit 1002, bar-type matching tolerance" ([core:104](../../../docs/clean-code/HP_CLEAN_CODE_CORE.md#L104)) — add them to the HPRebar note at [REVITADDINAI…:17](../../../docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md#L17) or §8 line 30, like M6/N7.
- **L2 stale "binding standard" pointer:** [PRAGMATIC_CLEAN_CODE_RULES.md:5](../../../docs/clean-code/PRAGMATIC_CLEAN_CODE_RULES.md#L5) still says the binding standard "for this repository" is REVITADDINAI… → point to core + appendix. ADR-0001 is historical; optionally note "scope widened by 0007" in [adr/README.md](../../../docs/architecture/adr/README.md) row 0001.
- **L3 weak citations:** C1 → `EtabsExecutor.cs:65` is the worker thread, not single-attachment/mutex; A5 → `ScriptUnits.cs:11` is the class line; A11 → `SeedLibraryTests.cs:103-144` covers the AEC shim tests only (schema default = fallback is Civil's `Schema_defaults_equal_the_code_fallbacks`); NI11 → `TeklaBridgeExecutor.cs:22` is a `using` alias (use 175-206). C2 claims Robot/Excel use `MainThreadQueue(expireWithoutTicks)` + control lane — `RobotStaWorker.cs:30`/`ExcelStaWorker.cs:30` prove STA only (GIẢ ĐỊNH CHƯA XÁC MINH for the rest).
- **L4 PB4 "known gap"** ([powerbi.md:10](../../../docs/clean-code/host-appendix/powerbi.md#L10); raw service body echoed at `PowerBiCloudClient.cs:142,285`, verified) is not in audit §2a — log as H-08 or drop the label.
- **L5 FM1** ([core:59](../../../docs/clean-code/HP_CLEAN_CODE_CORE.md#L59)) says `.editorconfig` at each solution root — true only after phase 4 (10 solutions lack one); same present-tense issue as M1, minor.
- **L6** workflow step 5 mixes DEPENDENCY_RULES `S1–S4` with standard `S5` and says "§1 of the standard" (now core §1) — pre-existing ambiguity, fix while touching the line.
- **L7** `hp-mcp-etabs` `.claude` copy has no block (documented in plan "Phase 1 notes") — keep as follow-up.

## Positive
- Move, not copy: Revit standard is a thin pointer file with every Revit/HPRebar id verbatim and a §13 supersession row; ids stable.
- AGENTS.md hand-insert done exactly per D5a; workflow step 15 + Non-negotiable now forbid regeneration.
- Appendices are short, rule + `path:line`, with known gaps tied to H-ids; audit §2a separates H-xx from HPRebar B-xx and marks "inference" honestly (H-02, H-06).

## Recommended actions (order)
1. H1 rename `C*` → `CO*` + global id gate. 2. M1 hold commit 2 until phase 2 or add the "enforced from…" clause. 3. M2 genericise DoD. 4. M3 log H-07 + NI12 wording. 5. L1–L4 in the same docs commit.

## Plan follow-up (lead decides)
Phase-1 todos appear done except: success criterion "id-list diff empty" passes only for core + Revit (H1 shows appendices need the same gate); commit split item 2 timing depends on M1.

**Status:** DONE_WITH_CONCERNS
**Summary:** Id move and AGENTS.md insert are correct and links resolve; appendix ids C1–C14 collide with core C1–C7 and must be renamed before commit.
**Concerns/Blockers:** H1 (blocking for commit); M1 timing of the agent-facing commit relative to phase 2.
