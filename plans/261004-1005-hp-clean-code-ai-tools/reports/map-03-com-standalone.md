# Map 03 — COM standalone group (HPEtabs, HPSap2000, HPRobot, HPExcel)

Scout 2026-10-04, read-only. No build/test run, no host started. "inference" = grep/heuristic, not a walker run.
Sources of truth: CLAUDE.md (ETABS only, L186–202), AGENTS.md L227–235 (SAP2000), L263–297 (Excel), L37 (Robot, layout row only — no Robot section exists), host READMEs.

## 1. Bridge shape and analyze path

All four bridges are standalone WPF apps, not add-ins: OutputType WinExe, net8.0-windows, UseWPF, ProjectReference to McpShared Contracts + McpBridge.Core.

| Host | Shape evidence | COM attach | Worker | Host analyzer |
|---|---|---|---|---|
| ETABS | HPEtabs/HPEtabs.McpBridge/HPEtabs.McpBridge.csproj:6-11; ETABSv1 ref Private=false :32-34 | cHelper.GetObject — HPEtabs/HPEtabs.McpBridge/Service/EtabsAttachment.cs:465 | STA, foreground — HPEtabs/HPEtabs.McpBridge/EtabsExecutor.cs:65-67; MainThreadQueue(expireWithoutTicks) :61 | EtabsTierAnalyzer (semantic, fail-closed) — Service/EtabsTierAnalyzer.cs:37 |
| SAP2000 | HPSap2000/HPSap2000.McpBridge/HPSap2000.McpBridge.csproj:6-11; SAP2000v1 Private=false :32-34 | helper.GetObject + direct oleaut32 GetActiveObject fallback — Service/SapAttachment.cs:63,79,222 | **no SetApartmentState → MTA** — HPSap2000/HPSap2000.McpBridge/SapExecutor.cs:57-58 (log line still prints "STA=" — SapExecutor.Worker.cs:49) | SapTierAnalyzer (copy of ETABS, semantic) — Service/SapTierAnalyzer.cs:28 |
| Robot | HPRobot/HPRobot.McpBridge/HPRobot.McpBridge.csproj:4-9; Interop.RobotOM Private=false :27-29 | Marshal2.GetActiveObject("Robot.Application") — Com/RobotAttachment.cs:92, Com/Marshal2.cs:23 | STA — Com/RobotStaWorker.cs:30; IOleMessageFilter — Com/ComInteropHelper.cs:17 | RobotTierAnalyzer (syntactic name table) — Safety/RobotTierAnalyzer.cs:20 |
| Excel | HPExcel/HPExcel.McpBridge/HPExcel.McpBridge.csproj:4-6; Interop.Excel + ClosedXML NuGet :24-27 | ComInteropHelper GetActiveObject — Com/ComInteropHelper.cs:21,38 | STA — Com/ExcelStaWorker.cs:30; message filter — Com/ComInteropHelper.cs:24 | ExcelTierAnalyzer (syntactic name table) — Safety/ExcelTierAnalyzer.cs:18 |

### How `*.analyze` is answered (E1 verified for this group)

- ETABS: HPEtabs/HPEtabs.McpBridge/EtabsExecutor.cs:212-232 — `ScriptAnalyzer.Run(_compiler, code, GuardProfile.Etabs, AnalyzerProfile.Etabs)` (:214); early return if `!Compiles` (:215); then `_analyzer.Inspect(compiled.Script)` (:220); tier refusals + `DESTRUCTIVE` (:223-225) + `PREVIEW` "declared transaction: none … writes" (:227-229) are **prepended to `result.GuardViolations`** (:231). Never builds a new AnalyzeResult — mutates the shared one.
- SAP2000: identical shape — HPSap2000/HPSap2000.McpBridge/SapExecutor.cs:177-197 (Run :179, merge :196).
- Robot: plain pass-through — HPRobot/HPRobot.McpBridge/Host/RobotBridgeExecutor.cs:344-347. Tier analysis runs **only on execute** (:114), not on analyze.
- Excel: plain pass-through — HPExcel/HPExcel.McpBridge/Host/ExcelBridgeExecutor.cs:341-344. Tier analysis only on execute (:119).
- Shared builder: McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs:21-35 (Run = Analyze + guard + compile), :40-54 (`new AnalyzeResult` :46).

### Where quality findings sit (design consequence)

1. Compute `QualityAnalysed`/`QualityFindings` inside `ScriptAnalyzer.Analyze(code, profile)` (ScriptAnalyzer.cs:40-54), not in `Run` after the guard. Reason: ETABS/SAP return early when the script does not compile (EtabsExecutor.cs:215, SapExecutor.cs:180); Analyze always runs first (ScriptAnalyzer.cs:23), so findings survive every path.
2. All four hosts inherit the new fields with **zero host source edits** — ETABS/SAP only reassign `GuardViolations`, Robot/Excel return the shared object. Only a rebuild + republish of each bridge folder is needed (redeploy note for phase 2).
3. Findings must stay in their own field, never in `GuardViolations`/`Diagnostics`: ToolValidator turns every guard violation into an error (McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs:68-69), and bridge tests assert `Assert.Empty(fine.GuardViolations)` / `Assert.Contains(... d.Id == "PREVIEW")` (HPEtabs/HPEtabs.McpBridge.Tests/EtabsExecutorRefusalTests.cs:139-150; SAP copy SapExecutorRefusalTests.cs). Mixing would break those and turn warnings into blockers.
4. Rule ids must not collide with host diagnostic ids `PREVIEW`, `PATH`, `DESTRUCTIVE` (EtabsTierAnalyzer.cs:40, :78; EtabsExecutor.cs:225) nor Navis `HEAVY`. Suggest a prefix such as `Q-…`.
5. Line numbers: ScriptAnalyzer parses raw code as `SourceCodeKind.Script` (ScriptAnalyzer.cs:42); tier hits use the compiled script's tree (EtabsTierAnalyzer.cs:50-58). Same text → same lines; the seed test must parse raw code.cs the same way, not the method wrapper SeedLibraryCompileTests builds.

## 2. Seeds

| Host | Seeds (code.cs) | Files | Total code.cs lines | Largest code.cs | Generator |
|---|---|---|---|---|---|
| ETABS | 12 | 36 | 545 | 77 Geometry/get_structural_objects | HPEtabs/tools/generate-seed-library.py (751 lines) |
| SAP2000 | 12 | 36 | 558 | 88 Geometry/get_structural_objects | **none** (only generate-oapi-tier-fixture.ps1) — hand-maintained |
| Robot | 12 | 36 | 499 | 64 Geometry/get_structural_objects | **none** — hand-maintained |
| Excel | 12 | 36 | 720 | 86 Data/read_table | HPExcel/tools/generate-seed-library.py (1108 lines) |

Path: `<Host>/<Host>.Mcp.Server/Registry/SeedLibrary/<Category>/<name>/{tool.json,code.cs,examples.json}` — accepted assumption confirmed. No seed > 300 lines (blocking rule never fires on baseline).

Seed edits for ETABS/Excel must go through the generator (edit, regenerate, retest — CLAUDE.md L196); editing code.cs alone drifts from the generator. Any code.cs change also changes the `_seeds.json` checksum → upgrade path on users' machines (SeedInstaller, CLAUDE.md registry notes).

### Seed tests in the server test projects

| Project | Seed tests | Installed-DLL compile | Skip |
|---|---|---|---|
| HPEtabs.Mcp.Server.Tests | SeedLibraryStructureTests.cs (own loader :60-75; validator :162-172; guard + args ⇔ schema :176-190), SeedLibraryCompileTests.cs | ETABSv1.dll via env/CLSID LocalServer32/Program Files — SeedLibraryCompileTests.cs:139-165 | `Assert.SkipWhen(compiled is null, "ETABS 22 not installed …")` :30,:41,:63 |
| HPSap2000.Mcp.Server.Tests | same pair (copy): SeedLibraryStructureTests.cs :159,:173; SeedLibraryCompileTests.cs | SAP2000v1.dll :136-161 | `Assert.SkipWhen` :30,:41,:63 |
| HPRobot.Mcp.Server.Tests | SeedCatalogTests.cs (:208 guard/schema, :231 validator), SeedCompilationTests.cs, SeedExecutionTests.cs | Interop.RobotOM.dll via CLSID :180-214 | `Assert.SkipWhen` :70,:110 |
| HPExcel.Mcp.Server.Tests | SeedLibraryQualityVerificationTests.cs, ExcelSeedScriptRoslynCompilationTests.cs, catalog/round-trip/adversarial | Interop.Excel from **NuGet** (csproj :16; test :56) | never skips |

- All four test projects already ProjectReference HPRebar.McpBridge.Core (Roslyn): HPEtabs…Tests.csproj:29, HPSap2000…:29, HPRobot…:29, HPExcel…:24. A walker placed in McpBridge.Core is callable from every seed test **without any csproj edit**.
- Precedent for sharing test source: each links FakeRevitExecutor.cs via `<Compile Include … Link>` (e.g. HPEtabs.Mcp.Server.Tests.csproj:35) — but adding a second link is a csproj edit (P7/brief constraint). Prefer a public helper inside McpBridge.Core (production assembly, already referenced) so each per-server test is ~20 lines.
- Every existing seed-validator call passes `analysis: null` — HPEtabs…/SeedLibraryStructureTests.cs:168, HPRobot…/SeedCatalogTests.cs:238, HPExcel…/SeedLibraryQualityVerificationTests.cs:38 — and asserts only `IsValid`. Phase-2 rule: `analysis == null` ⇒ one "quality not analysed" **warning**, never an error, or 4×12 existing tests turn red.

## 3. E8 / E9 — HPExcel

- E8 verified: HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Data/read_table/code.cs:13 `try { targetTable = targetSheet.ListObjects[tableName]; } catch { }` and :19-25 (multi-line try inside `foreach (var ws in wb.Worksheets)`, `catch { }` at :25). Source in generator: HPExcel/tools/generate-seed-library.py:330, :342. Not-found is handled after the loop by `throw new ArgumentException` (read_table/code.cs:29-32) — so semantically "try-get by name", not a lost error.
- E9 — SeedLibraryQualityVerificationTests.cs (71 lines) asserts:
  - :16-24 twelve seeds load from embedded resources (`SeedInstaller.LoadSeeds`), 7 expected categories;
  - :26-51 per seed: `ToolValidator.Validate(record, null, …)` IsValid (:38-39), `ScriptGuard.Check(code, GuardProfile.Excel)` empty (:41-42), `ScriptAnalyzer.Analyze` → every schema arg is read (:44-49; one direction only — unlike ETABS which checks both, SeedLibraryStructureTests.cs:187-188);
  - :53-70 host profile metadata.
  - It asserts **nothing about code quality** despite its name. Same content is duplicated in ETABS/SAP (StructureTests) and Robot (SeedCatalogTests) and in HPExcel itself (ExcelCatalogCompletenessTests.cs:122, ExcelSeedLibraryAdversarialChallengeTests.cs:31 also count 12 seeds).
- Reuse vs duplicate (K1): do not extend this file per host. Recommended: one public static entry in McpBridge.Core (e.g. `ScriptQuality.Check(code)` = the same walker the bridge runs) + one small per-server test that loads seeds (`SeedInstaller.LoadSeeds(assembly)` exists — Excel test :19; ETABS/SAP use their own loader) and compares findings to a baseline allowlist. The Excel test can host the new `[Fact]` (natural home, name fits) or a sibling file; either way a one-file change per server.

## 4. Empty-catch / swallowing-catch exposure

### Seeds (what the walker will see) — inference by grep `catch`

| Host | empty `catch { }` | `catch (Exception)` swallow | Verdict |
|---|---|---|---|
| ETABS | 0 | 0 | clean |
| SAP2000 | 0 | 0 | clean |
| Robot | 0 | 0 | clean |
| Excel | 2 (read_table :13, :25) | 0 | legitimate try-get; baseline-allowlist now; real fix = catch `COMException` + reason comment, via generator L330/L342 |

No seed calls Marshal.ReleaseComObject (grep `Release` = 0 in all 48 seeds) — release discipline lives in the bridges only.

### Bridges (governed by the core standard for new/changed lines; walker never runs on them)

Single-line empty catches (grep): ETABS 2, SAP 2, Robot 5, Excel 14; `catch (Exception` occurrences: 30 / 21 / 29 / 24 (inference, includes logging ones).

| Location | Pattern | Classification |
|---|---|---|
| HPEtabs/HPEtabs.McpBridge/Service/EtabsAttachment.cs:434, :443 | GetVersion / GetModelFilename best-effort context | legitimate, needs reason comment |
| HPSap2000/HPSap2000.McpBridge/SapExecutor.cs:219 | `catch (ObjectDisposedException) { }` on cancel | legitimate (narrow type) |
| HPSap2000/HPSap2000.McpBridge/Service/SapAttachment.cs:238 | best-effort | legitimate, comment |
| HPRobot/HPRobot.McpBridge/Com/RobotAttachment.cs:180; Host/RobotBridgeExecutor.cs:162, :195, :391 | detach cleanup, try-get app/units, cancel | legitimate, comment; :195 swallows units-manager failure → policy silently skipped (RobotUnitsPolicy.Run null branch, Units/RobotUnitsPolicy.cs:31-35) — borderline |
| HPRobot/HPRobot.McpBridge/Safety/RobotSnapshotManager.cs:102 | `try { robot.Project.Save(); } catch { }` then File.Copy of the disk file (:103) | **real defect candidate** (log as B-xx, do not fix here): a failed save yields a stale snapshot labelled pre-run; ETABS fails the run on Save ret≠0 (HPEtabs/HPEtabs.McpBridge/Service/EtabsSnapshotManager.cs:93) |
| HPExcel/HPExcel.McpBridge/Com/ExcelAttachment.cs:130-267 (8×), Host/ExcelBridgeExecutor.cs:172, :202-203, :390, Discovery/ExcelProcessDetector.cs:35, Safety/ExcelSnapshotManager.cs:86 | COM property probes (ActiveWorkbook, Selection, Name), process lookup | legitimate try-get, should narrow to COMException/ArgumentException + comment |
| HPExcel/HPExcel.McpBridge/Com/ComInteropHelper.cs:63-92; HPRobot/…/Com/ComInteropHelper.cs:33-62 | `catch (Exception ex) { Log.Debug(...) }` around ReleaseComObject | legitimate (release must not throw); would trip the script rule "catch(Exception) neither rethrows nor returns error" if it appeared in a script → the rule should treat a logging call as insufficient but it is a **warning** only, acceptable |
| HPExcel/…/Safety/ExcelSnapshotManager.cs:105-113 | SaveCopyAs failure → warning + disk-copy fallback | logged, but same stale-snapshot risk as Robot (inference) |

## 5. Appendix `com-standalone` — candidate rules with sources

| # | Rule | ETABS | SAP2000 | Robot | Excel |
|---|---|---|---|---|---|
| C1 | One COM attachment per bridge process, owned by one worker; bridge is a user-started desktop app, server never references the wrapper | CLAUDE.md L190 (ADR-01); EtabsExecutor.cs:65 | AGENTS.md L231 | Program.cs:14,35 mutex; AGENTS.md L37 | Program.cs:13,26 mutex; AGENTS.md L267 |
| C2 | Dedicated STA worker + queue (`MainThreadQueue(expireWithoutTicks)`); control lane for attach/detach | EtabsExecutor.cs:61-67, EtabsExecutor.Worker.cs:8-33 | **violates: MTA** SapExecutor.cs:57 (works per ETABS spike "STA is a design choice", CLAUDE.md L192 — record as deviation) | RobotStaWorker.cs:30 | ExcelStaWorker.cs:30 |
| C3 | Message filter for RETRYLATER where the host rejects calls | n/a (ETABS serves under modal, CLAUDE.md L192) | n/a | ComInteropHelper.cs:17 | ComInteropHelper.cs:24 |
| C4 | Units forced for the run and restored in `finally`; restore failure = warning | EtabsUnitsPolicy.cs:13,23-35 (kN_mm_C) | SapUnitsPolicy.cs:12,26 (kN_m_C) | RobotUnitsPolicy.cs:13-35 (Metric; skipped when manager null) | n/a (no units) |
| C5 | Snapshot before any W/D run; a failed save/copy **fails the run** | EtabsSnapshotManager.cs:52-56,93,134 | SapSnapshotManager.cs:19; AGENTS.md L232 | RobotSnapshotManager.cs:16 (save failure swallowed :102 — gap) | ExcelSnapshotManager.cs:14,125 |
| C6 | Run-time path policy for file-taking members (absolute, local, not inside bridge state dirs) | EtabsPathPolicy.cs:13; EtabsScriptRunner.cs:195-210 | SapPathPolicy.cs:9; SapScriptRunner.cs:176-191 | **none found** | **none found** |
| C7 | Tier allow-list R/W/D, fail-closed for unknown members; generated, reviewed table | etabs-oapi-tiers.txt (1344 lines) + semantic EtabsTierAnalyzer.cs:29-36 | sap2000-oapi-tiers.txt (2123 lines) | RobotTierTable name/prefix match, syntactic (RobotTierAnalyzer.cs:39-60) | ExcelTierTable.cs:82-110 name/prefix, unknown → ReadOnly (fail-open, inference) |
| C8 | Tier verdict also at analyze time (so propose_tool refuses D / `none`+W) | EtabsExecutor.cs:220-231 | SapExecutor.cs:185-196 | **only at execute** RobotBridgeExecutor.cs:114 vs :346 | **only at execute** ExcelBridgeExecutor.cs:119 vs :343 |
| C9 | Every OAPI `ret` checked → `InvalidOperationException("… returned {ret} from X")` | 39 IOE / 11 `ret != 0` in seeds; CLAUDE.md L196 seed contract | 35 / 9 | 4 / 1 | 0 / 0 (Excel COM throws instead of ret codes) |
| C10 | Caller mistakes → `ArgumentException` (excluded from stability window) | 22 in seeds | 22 | 7 (e.g. get_node_reactions/code.cs:23) | 4 (read_table/code.cs:31) |
| C11 | Unit-labelled output fields (`fzKN`, `lengthMm`, `eMPa`) | yes (`fxKN`, `coordinatesMm`, `eMPa`) | yes (`fxKN`, `coordinatesM`) | **no** (`fx`, `fz` — get_node_reactions/code.cs:35-36) | n/a |
| C12 | Publish bridge as a folder (`PublishSingleFile=false`; Roslyn needs `Assembly.Location`) | csproj:19; README.md:56-60 | csproj:19; README.md:54-58 | csproj:15 | csproj:12 |
| C13 | Wrapper DLL referenced `Private=false`, resolved from the install at run time | csproj:32-34; CLAUDE.md L194 | csproj:32-34 | csproj:27-29; RobotAssemblyResolver.cs:73 | NuGet interop (copied) |

Recommendation: write C1, C4, C5, C9–C13 as appendix rules; C2/C6/C7/C8 as rules with a "known gaps" table (SAP MTA, Robot/Excel no path policy, syntactic fail-open tiering, no analyze-time tier verdict) — logged, not fixed (refactor out of scope).

## 6. Risks

| # | Risk | Evidence | Mitigation |
|---|---|---|---|
| R1 | ETABS/SAP/Robot compile tests skip without the product; seed-quality test must not depend on them | SkipWhen sites §2 | quality test = syntax only (raw code.cs), no wrapper DLL → runs everywhere, never skips |
| R2 | ETABS/SAP bridge tests need the product installed (CLAUDE.md: "needs ETABS 22 installed") | CLAUDE.md L32 (HPEtabs layout row) | net10/net48 walker tests live in McpShared tests; host bridge tests only re-run for regression |
| R3 | Findings merged into GuardViolations would block every proposal and break PREVIEW/DESTRUCTIVE tests | ToolValidator.cs:68; EtabsExecutorRefusalTests.cs:139-150 | separate field (contract already says so); id prefix distinct from PREVIEW/PATH/DESTRUCTIVE |
| R4 | Commented-out-code false positives on prose with code tokens | ETABS seed comments: "(status 4 = finished)", "`.$et` beside the `.EDB`" (Results/get_joint_reactions, Results/get_frame_forces, Model/get_model_info); ETABS 6 comment lines, SAP 1, Robot/Excel 0 | detector must require the comment body to parse as a statement/declaration with ≥1 token beyond prose (e.g. ends with `;`/`{`/`}`), add these two lines as negative test cases |
| R5 | "Block > 50 lines" applied to the top-level script body would warn on 21 seeds (ETABS 4, SAP 5, Robot 3, Excel 9 have > 50 lines) | wc §2 | count only nested blocks/local functions, not the compilation unit |
| R6 | Vague-identifier list scope: Robot `data`×2, `obj`×1 (Property/get_materials_and_sections:12,:32; Geometry/get_structural_objects:49), Excel `val`×2 (run_macro:21, write_range:35), `result` (run_macro:10), ETABS/SAP `x` (geometry seeds) | grep inference | fix the exact word list in phase 2; `x`/`y` coordinates should be exempt |
| R7 | Nesting > 3 candidates: Excel write_range (indent depth 5), run_macro (4), Robot get_load_definitions (4) | indent heuristic, inference | warnings only; baseline allowlist covers |
| R8 | Fixing Excel read_table changes the seed checksum + generator; SAP/Robot have no generator so fixes would be hand edits | §2 | allowlist now; any fix is a separate commit through the generator |
| R9 | Old deployed COM bridges (user-started exes, often running and locking files) report `quality not analysed` until republished | C12; CLAUDE.md L194 | phase-2 redeploy note per bridge folder; republish only with the bridge app closed |
| R10 | Robot/Excel analyze returns no tier verdict, so quality warnings appear on a destructive proposal while ETABS shows DESTRUCTIVE errors — inconsistent UX across hosts | C8 | document in appendix known gaps; not in this brief's scope |

## Contract notes (for evaluation.md, not applied)

- Contract fits this group unchanged: findings ride the shared AnalyzeResult with no host code edits. No deviation needed.
- Observed defects to log (not fix): SAP worker MTA vs STA log text (SapExecutor.cs:57 vs .Worker.cs:49); Robot snapshot Save swallowed (RobotSnapshotManager.cs:102).

**Status:** DONE
**Summary:** COM bridges (4 standalone WPF apps) all build AnalyzeResult through ScriptAnalyzer; ETABS/SAP only prepend tier diagnostics to GuardViolations, Robot/Excel pass through, so quality fields need zero host edits — compute them in `Analyze`, keep them out of GuardViolations. Seeds: 4×12, max 88 lines, only Excel read_table has empty catches (legit try-get, allowlist); seed tests can call a McpBridge.Core helper without csproj edits.
**Concerns:** Robot snapshot swallows Save failure; SAP worker is MTA; Robot/Excel lack analyze-time tiering and path policy — appendix known gaps.
