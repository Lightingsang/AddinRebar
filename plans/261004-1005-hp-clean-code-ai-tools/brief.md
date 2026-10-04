# Brief 261004-1005 — Clean code for AI-created tools across the whole repository

> **Audience:** Claude Code. **Mode:** analyse → evaluate → plan. **No production code, no `.csproj`/`.slnx`/manifest edits, no commits** in this brief. Stop at §8 and wait for the user.
> **Language:** this brief and every file you write are in English; the final chat report to the user is in Vietnamese.

## Tóm tắt (tiếng Việt)

Mục tiêu: áp dụng quy trình clean code cho **mọi code C# do AI tạo** trong toàn repo, qua hai kênh:
1. **Mã nguồn repo** (AI agent viết feature/command/MCP tool/seed) — mở rộng standard hiện chỉ binding cho `HPRebar/` sang `McpShared/` + 9 folder MCP còn lại, dạng **lõi host-neutral + phụ lục theo nhóm host**. Chỉ áp cho code mới/sửa; code cũ không refactor.
2. **Tool runtime** (AI gọi `propose_tool → test_tool → publish_tool`) — thêm phân tích chất lượng vào `ScriptAnalyzer` của bridge, trả qua field additive của `AnalyzeResult`, `ToolValidator` phân loại lỗi chặn / cảnh báo; seed nhúng có test chất lượng với baseline allowlist.

Việc của Claude Code: kiểm chứng hợp đồng bên dưới bằng code, chạy baseline (không sửa code), viết báo cáo map + đánh giá, lập `plan.md` + 4 phase file, rồi **dừng chờ duyệt**. Được phản biện, nhưng mọi đề xuất lệch hợp đồng ghi riêng ở mục "Contract deviation proposals".

---

## 1. Mandatory reading (before anything else)

1. `CLAUDE.md` — "Clean Code Governance — RevitAddinAI (MANDATORY)", "Repository Layout", the per-host MCP sections.
2. `docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md`, `TOOL_DEVELOPMENT_WORKFLOW.md`, `CODE_REVIEW_CHECKLIST.md`, `REFACTORING_PLAN.md` (§7 "Out of scope" is what this work changes).
3. `docs/architecture/ARCHITECTURE.md`, `DEPENDENCY_RULES.md`, `adr/0001…0006`.
4. Prior governance plan for format and depth: `plans/261003-2133-pragmatic-clean-code-governance/plan.md` and its `reports/map-0X-*.md`.
5. Look-up only: `docs/clean-code/PRAGMATIC_CLEAN_CODE_RULES.md` (PCC-001…293).

## 2. Requirement contract (agreed with the user 2026-10-04 — binding)

| Field | Content |
|---|---|
| **Expected output** | (a) ADR-0007 extending the governance scope; (b) `docs/clean-code/HP_CLEAN_CODE_CORE.md` (host-neutral rules) + `docs/clean-code/host-appendix/` with five appendices: `revit`, `autocad-civil`, `com-standalone` (ETABS, SAP2000, Robot, Excel, Power BI bridge app shape), `net48-inprocess` (Navisworks, Tekla), `powerbi` (AMO-TOM/ADOMD/MSAL specifics); (c) a script-quality analyzer in the bridge + additive contract fields + `ToolValidator` mapping + quality section in the review file; (d) a seed-quality test in all 10 `*.Mcp.Server.Tests` projects with a baseline allowlist; (e) `.editorconfig` (suggestion-only, no reformat) for the 9 solutions that lack one; (f) CLAUDE.md → AGENTS.md, the 10 `hp-mcp-*` skills and the code-reviewer agent memory updated. |
| **Acceptance criteria** | 1. `propose_tool` **rejects** (error) a script with: commented-out code; an empty `catch` with no comment giving the reason; more than 300 lines. 2. **Warnings only** (never block): a block or local function > 50 lines; nesting > 3 levels; vague identifiers (`data`, `tmp`, `obj`, `res`, …); a `bool` parameter on a local function; `catch (Exception)` that neither rethrows nor returns an error. 3. Old/offline bridge → report says `quality not analysed`, publish still allowed under every policy including `auto`, and the review file shows the same line. 4. Seed-quality test green in all 10 servers; existing violations live in a baseline allowlist (e.g. two `catch { }` in HPExcel seed `read_table`); new or edited seeds must be clean. 5. `HPRebar.Mcp.Server.Core.Tests` and `HPRebar.McpBridge.Core.Net48Tests` green. 6. `tools/list` of every server exe **byte-identical** before/after. 7. Live propose → test → publish on Revit 2026; other hosts reported `CHƯA TEST` unless run live. |
| **In scope** | All **new or changed** C# in `HPRebar/`, `McpShared/`, `HPAutoCad/`, `HPCivil3d/`, `HPNavis/`, `HPEtabs/`, `HPSap2000/`, `HPPowerBi/`, `HPExcel/`, `HPRobot/`, `HPTekla/`; AI-proposed runtime tools; embedded seeds. |
| **Out of scope** | Refactoring existing code (Boy Scout only on lines a diff already touches, PCC-028); third-party analyzer NuGet; `Directory.Build.props`/`.csproj` analyzer switches; Python/TypeScript/HTML; changing any MCP tool description. |
| **Constraints** | Contract changes strictly additive (deployed bridges); existing file names and rule ids (N1, R1, …) unchanged; numbers are review triggers except the three blocking rules; P6 (commit per kind of change); P7 (no new project/package without approval); plan approved before code. |
| **Chosen approach** | **A** — a new syntax walker beside the existing `ScriptAnalyzer` in `McpShared/HPRebar.McpBridge.Core/Scripting/`; `AnalyzeResult` gains `QualityAnalysed` + `QualityFindings{RuleId, Severity, Line, Column, Message}`; `ToolValidator` maps severity → errors/warnings; seed tests call the same walker. Rejected: B (rules/thresholds per host in `AnalyzerProfile` — no present need, K4), C (new shared netstandard project — P7, `.slnx` edits). |
| **Accepted assumptions** | Non-Revit bridges report "not analysed" until redeployed; seed code lives in `<Host>.Mcp.Server/Registry/SeedLibrary/**/code.cs`. |

## 3. Codebase evidence already gathered (verify, do not trust)

| # | Fact | Where |
|---|---|---|
| E1 | Every bridge (Revit, AutoCAD, Civil 3D, Navis, ETABS, SAP2000, Robot, Excel, Power BI, Tekla) builds its `AnalyzeResult` through the one shared `ScriptAnalyzer` | `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs` (`return new AnalyzeResult` ~L46), `AnalyzerProfile.cs`; executors e.g. `HPEtabs/HPEtabs.McpBridge/EtabsExecutor.cs` |
| E2 | `HPRebar.Mcp.Server.Core` has **no** Roslyn reference (packages: ModelContextProtocol, Hosting, Sqlite); Roslyn is in `HPRebar.McpBridge.Core` (`Microsoft.CodeAnalysis.CSharp.Scripting` 5.9.0) | the two `.csproj` |
| E3 | `AnalyzeResult` today: `Compiles`, `GuardViolations`, `Diagnostics`, `Literals`, `ArgKeys`, `LineCount`, `HasLoops`, `UsesTransaction`, `CacheHit` | `McpShared/HPRebar.Mcp.Contracts/Messages/AnalyzeMessages.cs` |
| E4 | `ToolValidator.Validate` is called once, in `ProposeAsync`; errors block the draft, warnings are returned; `analysis == null` (bridge offline) → one warning, draft still saved | `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs`, `ToolLifecycleService.cs` L91–94 |
| E5 | `Publish` does not re-validate; `PolicyAuto` publishes directly, otherwise `WriteReview` writes a markdown review file | `ToolLifecycleService.cs` L147–183, L239+ |
| E6 | Seeds are installed by `SeedInstaller` straight from embedded resources — they never pass `ToolValidator` | `McpShared/HPRebar.Mcp.Server.Core/Registry/SeedInstaller.cs` |
| E7 | 465 files under `Registry/SeedLibrary/` in the 10 servers; largest `code.cs` = 137 lines (Revit `create_line_based_element`) | measured 2026-10-04 |
| E8 | HPExcel seed `Data/read_table/code.cs` has `catch { }` at L13 and L25 (COM lookup by name) | grep |
| E9 | HPExcel already has `SeedLibraryQualityVerificationTests.cs` — check what it asserts before designing a new seed test (reuse vs duplicate, K1) | `HPExcel/HPExcel.Mcp.Server.Tests/` |
| E10 | Civil 3D mirror test compares 24 files against their AutoCAD source (`HPCivil3d/tools/mirror-tokens.json`); editing an AutoCAD seed or bridge file alone breaks it | `HPCivil3d.McpBridge.Tests` |
| E11 | Existing `tools/list` snapshot scripts to reuse for the byte-identical gate | `plans/260915-0824-navisworks-mcp-2026/reports/snapshot-tools-list.ps1` (+ ETABS, Civil 3D copies) |
| E12 | AGENTS.md carries ~123 lines CLAUDE.md lacks (SAP2000/PowerBi/Excel/Robot/Tekla sections) — regenerating AGENTS.md from CLAUDE.md would delete them | `plans/261003-2133-…/plan.md` "Issues found outside scope" |
| E13 | Only `HPRebar/.editorconfig` exists (suggestions only, Wave 0.4) | `HPRebar/.editorconfig` |
| E14 | net48 hosts (Navisworks, Tekla) consume the `net48` asset of Contracts/Bridge.Core; System.Text.Json binding on .NET Framework has bitten before | CLAUDE.md `McpShared/` row |

## 4. Workflow to use

1. Use the repo's `/bs:plan` skill (Stack-Aware 6-phase) and follow `TOOL_DEVELOPMENT_WORKFLOW.md` steps 1–8; steps 9–16 become the phase files.
2. Run **5 parallel scout subagents**, one per host group, each writing one report into `reports/`:

| Report | Group | Must answer |
|---|---|---|
| `map-01-revit-mcpshared.md` | HPRebar + McpShared | Exact insertion point in `ScriptAnalyzer`; how `AnalyzeResult` crosses the pipe (serializer options, unknown-field tolerance on old servers/bridges); `ToolValidator`/`WriteReview` change surface; which existing standard sections are already host-neutral |
| `map-02-autocad-civil.md` | HPAutoCad, HPCivil3d | Seed layout and seed tests; mirror-test impact (E10); AutoCAD/Civil-specific rules for the appendix (transactions, document lock, units) |
| `map-03-com-standalone.md` | HPEtabs, HPSap2000, HPRobot, HPExcel | Seed tests that compile against installed DLLs (skip behaviour); R/W/D tiering interplay with new findings; COM release patterns that the "empty catch" rule will hit (E8, E9); appendix rules |
| `map-04-net48-inprocess.md` | HPNavis, HPTekla | net48 asset behaviour of new contract fields (E14); Net48Tests coverage; appendix rules (main-thread queue, undo/savepoint) |
| `map-05-powerbi.md` | HPPowerBi | Bridge shape, seeds, TMSL snapshot interplay; appendix rules |

3. Synthesize into the evaluation and plan (§6).

## 5. Baseline you may run (read-only for source)

Allowed: `dotnet test` for `McpShared/HPRebar.Mcp.Server.Core.Tests`, `McpShared/HPRebar.McpBridge.Core.Net48Tests` (do **not** pass `--nologo`), and the 10 `*.Mcp.Server.Tests` projects; the `tools/list` snapshot of each server exe (E11); a throw-away script under `reports/` that counts seed violations against the §2 rules.
Not allowed: building the Revit add-in or any `Debug.R2x` configuration (a Kata session and Revit may hold locks), deploying bridges, editing source, committing.
Record exact commands, counts and hashes in `reports/baseline.md`. A test that cannot run (host not installed) is listed as skipped with the reason, never omitted.

## 6. Deliverables (all inside `plans/261004-1005-hp-clean-code-ai-tools/`)

1. `reports/map-01…05-*.md` (§4).
2. `reports/baseline.md` (§5): test counts per project, `tools/list` hash per exe, seed violation counts per rule per host.
3. `reports/evaluation.md`:
   - **Assumption check** — table: contract item → verified / refuted / partial → evidence (`path:line`).
   - **Risks** — at least: false positives of the commented-out-code detector; mirror drift (E10); net48 deserialization (E14); AGENTS.md regeneration loss (E12); Excel seed baseline (E8/E9).
   - **Rule fit** — for each of the 8 runtime rules: how it is detected syntactically, expected false-positive cases, test cases needed.
   - **Contract deviation proposals** — anything you believe should change, each with evidence, consequence and a recommended option. Do **not** apply them in the plan; mark the affected phase steps "pending user decision".
4. `plan.md` — status table, phase list, verification per phase, open decisions; same shape as `plans/261003-2133-…/plan.md`.
5. `phase-01-adr-docs-agent-rules.md` — ADR-0007; `HP_CLEAN_CODE_CORE.md` + 5 appendices (rule ids kept; which REVITADDINAI sections move to core vs stay Revit); CODE_REVIEW_CHECKLIST + TOOL_DEVELOPMENT_WORKFLOW pointing at core + appendix; `hp-mcp-*` skills; how CLAUDE.md/AGENTS.md are updated without losing E12 content.
6. `phase-02-bridge-analyzer-validator.md` — walker design, contract fields, `ToolValidator` mapping, review-file section, unit tests (net10 + net48), redeploy notes per bridge.
7. `phase-03-seed-quality-tests.md` — shared test helper vs per-server copies (decide with K1/K3 and E9), baseline allowlist format, mirror-safe handling for Civil 3D.
8. `phase-04-editorconfig-docs-sync.md` — `.editorconfig` for the 9 solutions (mirror HPRebar's, suggestions only), docs/changelog/codebase-summary, skill_sync run.

Each phase file lists: files touched, steps, build/test commands, acceptance checks mapped to §2 criteria, commit split (P6), rollback.

## 7. Rules while you work

- Every claim cites `path:line` or a command output; mark inferences as such.
- Findings and design choices cite rule ids (standard ids or PCC ids).
- Prefer the simplest design that meets §2 (K5); every new type gets a one-sentence responsibility (C1); no new interface without a stated reason (S5).
- Do not touch Kata files (active development elsewhere).
- Status words exactly: Planned / Implemented / Built / Tested / Verified (P8). This brief ends at **Planned**.

## 8. Stop point

When §6 is complete, stop and report to the user **in Vietnamese**:
1. Kết quả baseline (số test, hash `tools/list`, vi phạm seed).
2. Bảng kiểm chứng giả định — chỉ các mục refuted/partial.
3. Đề xuất lệch hợp đồng (nếu có), mỗi mục kèm lựa chọn khuyến nghị.
4. Danh sách phase + ước lượng số file chạm mỗi phase.
5. Các quyết định anh cần đưa ra trước khi bắt đầu Phase 1.

Do not start Phase 1 until the user approves the plan explicitly.
