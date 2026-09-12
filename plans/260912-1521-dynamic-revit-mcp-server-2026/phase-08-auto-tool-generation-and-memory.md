---
title: "Phase 8 — Auto Tool Generation, review gate, tool memory"
status: built+tested (2026-09-12) — verified over stdio without Revit; in-Revit loop → phase 9
priority: P1
effort: 16h
depends_on: [phase-07]
created: 2026-09-12
---

# Phase 8 — Auto Tool Generation & Tool Memory

## Context
- Read side (search/get/run/dynamic tools) exists after phase 7. This phase adds the write side: turn a successful ad-hoc `execute_revit_code` run into a reviewed, published tool, and keep the library healthy.
- Bridge gains one Revit-free method in phase 6: `revit.analyze` (guard + compile + literal/arg-key extraction on the pipe thread, no Revit thread).
- Design §5.2 / §6 / §8 in [research/ai-bim-self-extending-tool-registry-design.md](research/ai-bim-self-extending-tool-registry-design.md).

## Overview
Lifecycle `draft → tested → pending_approval → published → (quarantined | deprecated)` implemented by `ToolManager` + four meta tools (`propose_tool`, `test_tool`, `publish_tool`, `manage_tool`), a `toolify_run` prompt, registry resources, an approval CLI inside the server exe, and automatic quarantine from run statistics.

## Key insights
- The **client LLM** does the generalisation (literals → `args`, naming, schema, examples). The server only (a) tells it when a run looks reusable, (b) hands it the literals and code, (c) validates hard rules. No server-side LLM call, no API key.
- "Success" of a run is known only in the server (`ExecuteResult.IsError == false`, `changed`, `value`), so the reusable-hint belongs in `execute_revit_code`'s result, not in a separate tool.
- Approval is a **human** act by default. The server process cannot ask the human; it writes a review file and waits. The CLI (`HPRebar.Mcp.Server.exe registry approve <name>`) edits `tool.json`; the phase-7 watcher publishes it live in every running server.
- Validation must be mechanical and cheap: name slug, JSON Schema subset, every schema property referenced in code as `args.<Kind>("<prop>")` (or `args.Has`), guard + compile via `revit.analyze`, near-duplicate check via FTS.

## Requirements
Functional
- `execute_revit_code` result gains `runId` and, when the run succeeded and looks reusable (≥ 12 code lines, or `changed.added > 0`, or a loop), `hint: "Reusable? get_run <id> then propose_tool …"`.
- `get_run(runId)` → code, result summary, analysis (`literals[]` with line/col/context, `argKeys[]`, `guardViolations[]`).
- `propose_tool(name, title, description, category, tags[], inputSchema, code, examples[], transaction, timeoutSeconds, sourceRunId?)` → validation report; on pass → `draft` v1 (or new version when the tool exists and `newVersion=true`).
- `test_tool(name, cases?[], realRun=false)` → each case `dryRun=true` through `run_tool`; all pass → `tested`; records runs with `kind=test`.
- `publish_tool(name)` → policy `manual`: status `pending_approval`, writes `tools-library/_review/<name>.md`; policy `auto`: `tested` + ≥ 2 dry-run passes + no guard violations → `published` immediately.
- CLI: `registry list [--status]`, `registry approve <name> [--by <who>]`, `registry reject <name> --reason`, `registry export <name> <dir>`, `registry import <dir>`, `registry stats`.
- `manage_tool(name, action: deprecate|restore|quarantine|bump_version, reason?)`.
- Prompt `toolify_run(runId)` → messages containing code, literals, rules, output contract. Resources `registry://tools` (list json) and `registry://tools/{name}`.
- Quarantine: after every run, if `runs ≥ QuarantineMinRuns (5)` and failure rate over window `> QuarantineMaxFailureRate (0.4)` → status `quarantined`, tool removed from `tools/list`, review file written.
Non-functional
- `propose_tool` validation < 2 s (analysis on bridge pipe thread, no Revit thread).
- Every state change logged to stderr + `registry_events` table (who/what/when).

## Architecture
```
execute_revit_code ──success──▶ runs(kind=adhoc, code kept) ──▶ result.runId + hint
AI ──get_run(runId)──▶ ToolManager.AnalyzeRun ──revit.analyze──▶ literals/argKeys
AI (prompt toolify_run) ──propose_tool──▶ ToolValidator: slug · schema subset · props↔args · duplicate(FTS≥0.9) · revit.analyze
        ──▶ draft (files + db)
AI ──test_tool──▶ run_tool(dryRun) × cases ──▶ tested
AI ──publish_tool──▶ PublishPolicy ──manual──▶ pending_approval + _review/<name>.md
                                     ──auto───▶ published ──▶ DynamicToolRegistrar.Sync ──▶ list_changed
human ──CLI approve──▶ tool.json status=published ──watcher──▶ every running server registers it
each run ──▶ StabilityScorer ──▶ quarantine? ──▶ Sync (remove) + review file
```
New/changed files:
```
HPRebar.Mcp.Contracts/Messages/AnalyzeMessages.cs      AnalyzeRequest(code) · AnalyzeResult(diagnostics, guardViolations, literals[], argKeys[], lineCount, usesTransaction)
HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs         + Analyze = "revit.analyze"          (phase 6 delivers these two)
HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs      Roslyn walker: literals, args keys   (phase 6)
HPRebar.Mcp.Server/Registry/ToolValidator.cs            hard rules → ValidationReport
HPRebar.Mcp.Server/Registry/PublishPolicy.cs            manual | auto
HPRebar.Mcp.Server/Registry/ReviewFileWriter.cs         _review/<name>.md (code, schema, examples, test runs, diff vs previous version)
HPRebar.Mcp.Server/Registry/RegistryCli.cs              subcommands; Program.cs dispatches when args[0]=="registry"
HPRebar.Mcp.Server/Tools/Registry/ToolLifecycleTools.cs propose_tool, test_tool, publish_tool, manage_tool
HPRebar.Mcp.Server/Tools/Registry/RunHistoryTools.cs    get_run
HPRebar.Mcp.Server/Prompts/ToolifyPrompts.cs            toolify_run
HPRebar.Mcp.Server/Resources/ToolRegistryResources.cs   registry://tools, registry://tools/{name}
HPRebar.Mcp.Server/Tools/ExecuteRevitCodeTool.cs        runId + hint; store code of successful adhoc runs (prune > 500)
```

## Implementation steps
1. `runs.code` column for adhoc successes; `ExecuteRevitCodeTool` records run, computes hint.
2. `get_run` + `ToolManager.AnalyzeRunAsync` (bridge `revit.analyze`).
3. `ToolValidator` + tests (slug, schema subset, props↔args, duplicates, guard).
4. `propose_tool` → draft files + db + `registry_events`.
5. `test_tool` (cases default = examples) → `tested`.
6. `PublishPolicy`, `ReviewFileWriter`, `publish_tool`, `manage_tool`.
7. `RegistryCli` + Program.cs dispatch; `approve` edits tool.json atomically; watcher (phase 7) does the rest.
8. Quarantine hook in `ToolManager.RecordRun`.
9. Prompt + resources.
10. Tests: full lifecycle over pipe with `FakeRevitExecutor` (propose → test → publish manual → CLI approve → registered), quarantine after failures, policy auto.

## Todo
- [x] 1 runs/hint · [x] 2 get_run/analyze · [x] 3 validator · [x] 4 propose · [x] 5 test · [x] 6 publish/manage · [x] 7 CLI (same exe, `registry …`) · [x] 8 quarantine (phase 7 hook) · [x] 9 prompt/resources · [x] 10 tests (158 xUnit)

## Success criteria
- Scenario "miss → propose → test → approve → hit" runs end-to-end with a fake executor in xUnit and live in Revit (phase 9).
- AI cannot reach `published` under policy `manual` without the CLI.
- A tool failing 3 of 5 runs disappears from `tools/list` and a review file explains why.

## Risks
| Risk | Mitigation |
|---|---|
| LLM leaves literals hard-coded | validator flags numeric literals ≥ 2 digits not bound to `const`; examples must differ in ≥ 1 arg |
| Schema subset too narrow for real tools | support string/number/integer/boolean/enum/array/object(nested 2 levels)/required/default/description; reject the rest with a message |
| Duplicate tools with different names | FTS similarity ≥ 0.9 → propose returns "update `<existing>` instead (newVersion=true)" |
| Review file ignored, drafts pile up | `registry list --status pending_approval` in CLI; bridge status window can show a count later |

## Security
Guard runs twice (bridge `revit.analyze` at propose, bridge execute at run). Policy `auto` is opt-in via config and logged. Review file includes the full code for human eyes. No network, no LLM calls from the server.

## Next steps
Phase 9 verifies live and documents. Later: approval UI in the bridge WPF window; embeddings search when the library passes ~200 tools.
