---
title: "HP clean code for AI-created tools (whole repository)"
description: "Extend the clean-code governance to every MCP folder and add a script-quality check to propose_tool and to the embedded seeds"
status: in-progress
priority: P1
branch: RebarVersion1
tags: [clean-code, governance, mcp, roslyn, seeds, editorconfig]
created: 2026-10-04
---

# Plan 261004-1005 — clean code for AI-created tools

Brief: [brief.md](brief.md) (contract §2 binding). Status: phases 1–4 **Implemented**, built and tested; phase 2 **Verified** live on Revit 2026 (5/5); other hosts' bridges not redeployed (CHƯA TEST). User approved the plan and D1–D6 recommendations 2026-10-04.
Two channels: (1) repository code AI agents write → one host-neutral core standard + 5 host appendices; (2) runtime tools AI proposes → a quality walker in the shared bridge analyzer, mapped by `ToolValidator`, and the same walker over every embedded seed.

## Evidence
| Report | Content |
|---|---|
| [map-01](reports/map-01-revit-mcpshared.md) | insertion point `ScriptAnalyzer.Analyze`, pipe serialization, validator/review surface, docs split |
| [map-02](reports/map-02-autocad-civil.md) | 62 seeds, mirror-safe placement, appendix A1–A16 |
| [map-03](reports/map-03-com-standalone.md) | 48 seeds, tier diagnostics beside findings, Excel baseline, appendix C1–C13 |
| [map-04](reports/map-04-net48-inprocess.md) | net48 contract safety, 24 seeds, appendix Navis/Tekla |
| [map-05](reports/map-05-powerbi.md) | no seeds, appendix Power BI |
| [baseline](reports/baseline.md) | 1 901 pass / 0 fail / 38 skip; `tools/list` hashes ×10 (Debug bins); seed scan: 2 blocking hits (Excel) |
| [evaluation](reports/evaluation.md) | assumption check, risks K1–K12, rule fit Q-B1..B3 / Q-W1..W5, deviations D1–D6, design decisions |

## Phases
| # | Phase | Files (est.) | Depends | Status |
|---|---|---|---|---|
| 1 | [ADR-0007, core standard + appendices, agent rules](phase-01-adr-docs-agent-rules.md) | ~30 (docs, skills ×10 + mirrors) | approved | done — review 8/10, findings fixed |
| 2 | [Bridge quality walker + validator + review](phase-02-bridge-analyzer-validator.md) | ~10 (4 prod, 1 new prod, ~5 tests) | approved (D3a, D4a) | done — review 7.5/10, findings fixed; live 5/5 |
| 3 | [Seed quality tests + baseline allowlist](phase-03-seed-quality-tests.md) | 10 (tests only) | 2, D1a | done |
| 4 | [.editorconfig + docs sync](phase-04-editorconfig-docs-sync.md) | ~14 (10 `.editorconfig` + docs) | 1–3, D2a | done |

Order 2 → 3 can run before 1 (code first, docs cite the final rule ids); proposed order 1 → 2 → 3 → 4 keeps the ADR ahead of the code it governs.

## Verification per phase
| Phase | Gate |
|---|---|
| 1 | links resolve; rule ids unchanged (diff of id lists); `python scripts/sync-agent-skills.py check` no new drift; no AGENTS.md regeneration |
| 2 | `McpShared` tests (Server.Core + Net48) green; all 10 server tests green; `tools/list` ×10 byte-identical vs `reports/tools-list-baseline-debug/`; `HPCivil3d.McpBridge.Tests` green; live propose → test → publish on Revit 2026 |
| 3 | 10 server tests green; mutation check (add `// var x = 1;` to a scratch copy → test red) |
| 4 | every solution builds with 0 new warnings-as-errors; no file reformatted (`git diff --stat` shows only `.editorconfig` + docs) |

## Decisions (user, 2026-10-04)
All recommendations of [evaluation §4](reports/evaluation.md) accepted: D1a Power BI test accepts an empty library · D2a `.editorconfig` ×10 incl. McpShared · D3a auto policy → line in the publish message · D4a rule interpretations of evaluation §3 · D5a hand-insert into CLAUDE.md and AGENTS.md, never regenerate · D6a skills edited where they live. Out-of-scope host defects logged as H-01…H-06 in `CLEAN_CODE_AUDIT.md` §2a. Revit test instance may be closed for the phase-2 bridge redeploy.

## Phase 1 notes
- `hp-mcp-etabs`: block added on the portable side only — the `.claude` copy predates commit 37c538b (`connect_etabs`) and editing both sides turns the existing portable drift into a sync conflict; port the portable copy back first, then the block follows.
- `sync-agent-skills.py apply` stays blocked by the pre-existing `hp-mcp-excel` conflict; the four dual-edited skills show as `equivalent-dual-drift` until it is resolved.
- `git checkout` rewrites skill files with CRLF and the sync engine then reports a conflict — restore LF after a checkout.

## Out of scope
Refactoring existing code (Boy Scout on touched lines only); analyzer NuGet; `.csproj`/`Directory.Build.props` switches; Python/TS/HTML; tool descriptions; fixing the defects of evaluation §6; Kata files; `HPGeo/` (retired).

## Results (2026-10-04)
| Gate | Result |
|---|---|
| `McpShared` engine tests | 793/793 (743 + 50 new) |
| `HPRebar.McpBridge.Core.Net48Tests` | 115/115 (+2: walker on .NET Framework, JSON with/without the new fields) |
| 10 server tests | Revit 118, AutoCAD 283, Civil 3D 109, Navis 52, ETABS 84, SAP2000 57 + 25 skip, Robot 100, Excel 93, Power BI 98, Tekla 86 + 13 skip — 0 failed |
| Civil 3D mirror | 60/60 |
| `tools/list` ×10 Debug exes | byte-identical to `reports/tools-list-baseline-debug/` (before-snapshot taken at HEAD ffad05b) |
| Live Revit 2026 | [live-verify-quality.md](reports/live-verify-quality.md) 5/5, re-run after the review fixes 5/5 |
| Reviews | [phase 1](reports/review-phase-01.md) 8/10 · [phases 2–3](reports/review-phase-02-03.md) 7.5/10 — High/Medium fixed |

Review fixes: appendix ids `CO1…CO14` (collided with core C1–C7); DoD host-generic; H-07 Tekla snapshot, H-08 Power BI error bodies logged; seed baseline hash on LF-normalised code (`95f812195e94`); Q-B1 treats `Example:` / `e.g.` / `Usage` / labelled lines as prose; quality record stamped with the code hash, review says "stale" after a hand edit or import; walker failure → "not analysed" instead of a failed analyze; severity constants on `QualityFinding`; seed libraries must be non-empty (Power BI excepted); catch-variable names checked by Q-W3.
Known gaps: a tool proposed while the bridge was offline is not quality-checked later (test_tool does not re-analyse); Q-W1 warns when a whole script sits in one `try`; the 9 non-Revit bridges report "quality not analysed" until redeployed; seed warnings are not printed by the seed tests (review triggers only).
