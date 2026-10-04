# 0007 — Extend the clean-code governance to every MCP folder and to AI-proposed tools

- **Status:** Accepted (2026-10-04, by user)
- **Tags:** [PCC] [PROJECT]

## Context
ADR-0001 made the standard binding for `HPRebar/` only. Since then AI agents write most new C# in nine more MCP folders (`HPAutoCad/`, `HPCivil3d/`, `HPNavis/`, `HPEtabs/`, `HPSap2000/`, `HPPowerBi/`, `HPExcel/`, `HPRobot/`, `HPTekla/`) and in the shared engine `McpShared/`, with no written rules beyond CLAUDE.md conventions. A second channel exists at run time: an AI proposes a tool (`propose_tool → test_tool → publish_tool`), and the registry checks guard, compile and args ⇔ schema, but nothing about the readability of the script it will keep (`McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs:62-82`). The 155 embedded seeds never pass the validator at all (`SeedInstaller`). Evidence: `plans/261004-1005-hp-clean-code-ai-tools/reports/` (map-01…05, baseline, evaluation).

## Decision
1. **Scope.** The rules apply to every **new or changed** C# line in `HPRebar/`, `McpShared/`, `HPAutoCad/`, `HPCivil3d/`, `HPNavis/`, `HPEtabs/`, `HPSap2000/`, `HPPowerBi/`, `HPExcel/`, `HPRobot/`, `HPTekla/`, to AI-proposed runtime tools and to embedded seeds. Existing code is not refactored; the Boy Scout rule covers only lines a diff already touches (PCC-028). `HPGeo/` is retired and excluded.
2. **Documents.** Host-neutral rules live in [HP_CLEAN_CODE_CORE.md](../../clean-code/HP_CLEAN_CODE_CORE.md) with their existing ids; host rules live in [host-appendix/](../../clean-code/host-appendix/) — `revit`, `autocad-civil`, `com-standalone` (ETABS, SAP2000, Robot, Excel, Power BI bridge app shape), `net48-inprocess` (Navisworks, Tekla), `powerbi`. [REVITADDINAI_CLEAN_CODE_STANDARD.md](../../clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md) keeps its file name and its Revit/HPRebar ids and points to core for the rest.
3. **Runtime quality check.** The shared bridge analyzer reports script-quality findings; `propose_tool` **rejects** a script with commented-out code (Q-B1), an empty `catch` without a reason comment (Q-B2) or more than 300 lines (Q-B3), and only **warns** for Q-W1…Q-W5 (block or local function > 50 lines, nesting > 3, vague identifiers, `bool` parameter on a local function, swallowing `catch (Exception)`). A bridge that predates the check reports "quality not analysed"; publishing stays allowed under every policy.
4. **Seeds.** Every server's tests run the same check over its embedded seeds; existing violations sit in a hash-pinned allowlist, so a new or edited seed must be clean.
5. **Contracts stay additive** (deployed bridges); no analyzer NuGet, no `.csproj`/`Directory.Build.props` analyzer switches.

## Consequences
- \+ One vocabulary for review across all hosts; AI-proposed tools get the same minimum bar as repository code; seeds cannot regress.
- − More reading per change (core + one appendix); bridges must be redeployed before they analyse quality; a syntactic detector can misjudge prose comments — kept in check by a parse-based detector and a negative test corpus.

## Alternatives
Per-host thresholds in `AnalyzerProfile` (rejected: no present need, K4). A new shared netstandard project for the walker (rejected: new project, P7). Roslyn analyzer NuGet in every solution (rejected: out of scope, reformat churn).

## Rules
PCC-017, PCC-028, PCC-256, PCC-067, PCC-115, K4, K5, P7.
