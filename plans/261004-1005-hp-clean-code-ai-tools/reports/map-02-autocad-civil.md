# Map 02 — HPAutoCad + HPCivil3d (+ HPGeo status)

Scout 2026-10-04, read-only. No build, no deploy, no source edit. Inferences marked **[inference]**.
Heuristic scan script lived in the session scratchpad only (regex, not Roslyn) — counts in §6 are approximate.

## 0. HPGeo/ folder status

- Retired as CLAUDE.md says (CLAUDE.md:224 "Legacy standalone `HPGeo/` retired in Milestone M5"). On disk `HPGeo/HPGeo.{AutoCad,AutoCad.Loader,Core,Tests,TileFetch}` hold **only `bin/` + `obj/`** — 0 `.cs`, 0 `.csproj`, no solution. `HPGeo/output/` = old acceptance/spike artefacts.
- Git tracks **258 files, all `HPGeo/tools/node_modules/**`** (`mgrs` package), added by commit 040d2d2 "HPGeo". Leftover, not live code.
- Live successor: `HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs` (panel `HPGEOLINK_PANEL`).
- Stale reference: `.claude/rules/development-rules.md:88` still names `HPGeo/HPGeo.AutoCad.Loader/Ribbon/HPGeoRibbonTab.cs` as the ribbon template (file gone). → fix while writing the `autocad-civil` appendix (point at `HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs`).
- Recommendation: no `.editorconfig` / seed test for HPGeo; exclude it from the "9 solutions" count. Removing the tracked node_modules is out of scope (irreversible; user decision).

## 1. Seed layout, counts, sizes, generators

| Server | Seeds | Files | Categories | Largest `code.cs` |
|---|---|---|---|---|
| `HPAutoCad/HPAutoCad.Mcp.Server/Registry/SeedLibrary/<Category>/<name>/` | **50** | 150 (tool.json + code.cs + examples.json each) | 15: Aec Annotation Architecture Audit Block ChangeSet Coordination Data Drawing Generic Geometry Layer Layout MEP Structural | `Block/insert_block` 51, `Data/get_entities` 47, `Data/get_drawing_info` 42; total 732 lines |
| `HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/<Category>/<name>/` | **12** | 36 | 8: Alignment Corridor Document Parcel Pipe Point Profile Surface | `Pipe/list_pipe_networks` 76, `Alignment/get_alignment_geometry` 73, `Point/list_cogo_points` 55; total 572 lines |

- Embedding: `HPAutoCad/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.csproj:34-35` — `<Compile Remove="Registry\SeedLibrary\**\*.cs"/>` + `<EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)…"/>`. Civil csproj is a mirrored copy (`mirror-tokens.json` mirroredFiles), same lines. **Every file dropped under `SeedLibrary/` is embedded into the exe** → do not put an allowlist file there.
- 300-line blocking rule: no seed comes close (max 76). AEC seeds are thin shims (≤ 12 / ≤ 18 lines, see §2); real logic is engine code in `HPAutoCad/HPAutoCad.Aec/` (97 `.cs`, 12 520 lines, largest file 295 — `Cad/EntityShapeReader.cs`; 0 files > 300). That engine falls under the repository-code core standard, not the runtime walker.
- Generators:
  - **Civil: `HPCivil3d/tools/generate-seed-library.py`** (769 lines). `seed()` writes all three files (`:18-27`, `code.cs` at `:25`). Shared script fragments spliced into several seeds: `LIKE` (`:37-39`, local fn `bool Like(string, string)`), `ERR` (`:41-44`, `Fail(...)` + `Classify(Exception)`), `RESOLVE` (`:46-56`). Docstring `:1-11` mandates "run it after editing a seed body here, then `dotnet test HPCivil3d.Mcp.Server.Tests`". → Any Civil seed fix (or allowlist removal) must be made in the generator and regenerated; a hand edit of `code.cs` is overwritten next run. A fragment fix touches every seed that splices it.
  - **AutoCAD: no generator.** Seeds are hand-written (no script under `HPAutoCad/tools/` writes `SeedLibrary`). Edit `code.cs` directly.

## 2. Existing seed tests and where a quality test fits

AutoCAD — `HPAutoCad/HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs` (439 lines):
- Loader from embedded resources `:36-60` (`LoadSeedsCore`, cached `Lazy`), `MemberData(Seeds)`.
- `All_fifty_seeds_are_embedded` `:66`; `Aec_seed_is_a_thin_shim_over_the_engine` `:103` (24 names, `AssertThinShim(maxLines: 12)` `:108`); `Aec_write_seed_is_a_thin_shim_that_documents_its_side_effects` `:127` (14 names, `maxLines: 18` `:133`); helper `AssertThinShim` `:140-144` (line count only); size cap `MaxCodeBytes` 32 KB `:24`, asserted `:215`.
- `Seed_record_passes_the_registry_validator_for_the_autocad_profile` `:238` → `ToolValidator.Validate(record, null, [], false, AutocadHostProfile.Instance)` — **analysis = null**.
- `Seed_code_passes_the_autocad_guard_and_reads_only_declared_args` `:252` → `ScriptGuard.Check` + `ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Autocad)`.
- `Seed_code_compiles_against_the_autocad_api` `:270` (AutoCAD.NET 25.1.0 from NuGet cache; `Assert.SkipWhen` when absent).
Civil — `HPCivil3d/HPCivil3d.Mcp.Server.Tests/SeedLibraryTests.cs` (292 lines): same loader `:40-64`; `MaxCodeBytes` 32 KB `:21`, **`MaxCodeLines` 120** `:22` asserted `:108` (already stricter than the 300 rule); `All_twelve_seeds_are_embedded` `:70`; validator `:133` (analysis null); guard + `ScriptAnalyzer.Analyze(…, AnalyzerProfile.Civil3d)` `:147-154`; `Schema_defaults_equal_the_code_fallbacks` `:230`; compile `:268` (skips via `SeedCompileProbe.SkipReason()` `SeedCompileProbe.cs:18` when Civil 3D/AutoCAD.NET absent).

Roslyn availability: both test projects reference `McpShared/HPRebar.McpBridge.Core` (`HPAutoCad.Mcp.Server.Tests.csproj:33`, `HPCivil3d.Mcp.Server.Tests.csproj:31`) and already call `ScriptAnalyzer` → **the new walker is callable with no csproj change** (no P7 issue). TFMs: AutoCAD tests `net10.0-windows`, Civil tests `net10.0`. `HPCivil3d.McpBridge.Tests` (mirror tests, net10.0) also references Bridge.Core — not the place for seed tests.

Fit: one new `[Theory, MemberData(nameof(Seeds))] Seed_code_meets_the_script_quality_rules` per server, reusing the existing `LoadSeeds()` (no second loader, K1). Either a method in the existing `SeedLibraryTests.cs` (AutoCAD file is 439 lines — already over the 300 review trigger) or a new `SeedQualityTests.cs` + a small `internal static` accessor to `LoadSeeds()` **[recommend new file; avoids growing the 439-line file]**. Allowlist file in the tests project (e.g. `seed-quality-baseline.json` copied to output or a C# table).

Watch-out: the two validator tests pass `analysis: null`. If `ToolValidator` emits the "quality not analysed" line as a **warning**, the 62 tests stay green; if it ever became an error they all fail. Acceptance criterion 3 already says warning → consistent.

## 3. Mirror test impact (E10)

`HPCivil3d/tools/mirror-tokens.json` keys: `tokens` (50), `versionPatterns` (2), `mirroredFiles` (**29** pairs — CLAUDE.md says 24, stale since the MaterialDesign phase added 5 theme files), `civilOwnedFiles` (13), `renames`, `ownedCounterparts` (10, each with `autocadSha256`).
- Mirrored C# relevant here: `HPAutoCad.McpBridge/MainThreadExecutor.cs` ↔ Civil copy (holds the `Analyze` call, §4), `Model/AutocadScriptGlobals.cs`, `Service/AutocadScriptRunner.cs`, `AutocadResultSerializer.cs`, `AutocadContextReader.cs`, `DatabaseChangeCounter.cs`, loader + ribbon files, server `Program.cs`, `appsettings.json`, server csproj.
- Sha-pinned (ownedCounterparts): `HPAutoCad.McpBridge/BridgeEntry.cs`, `Service/ScriptingSelfCheck.cs`, `HPAutoCad.McpBridge.csproj`, `Bundle/PackageContents.xml`, `launchSettings.json`, server `Hosts/AutocadHostProfile.cs`, `Tools/ExecuteAutocadCodeTool.cs`, `Tools/AutocadContextTool.cs`, `Prompts/AutocadScriptPrompts.cs`, `Resources/AutocadDocumentResources.cs`.

Scan scope (`HPCivil3d/HPCivil3d.McpBridge.Tests/MirrorTokenTable.cs:70-75`): only projects `*.McpBridge`, `*.McpBridge.Loader`, `*.Mcp.Server`; excludes `bin/`, `obj/`, **`Registry/SeedLibrary/`** (`:73-74` "seed tools are written per host … never mirrored") and `.png/.ico/.dll/.pdb/.user/.cache/.md`. Coverage facts enforced by `MirrorTests.cs:61-74` (every Civil file mirrored or civil-owned) and `:77-93` (every AutoCAD file mirrored or an owned counterpart).

| Change in this plan | Breaks Civil tests? | Evidence |
|---|---|---|
| Edit / add seed `code.cs` (either host) | No | SeedLibrary excluded `MirrorTokenTable.cs:74` |
| New test file in `HPAutoCad.Mcp.Server.Tests` or `HPCivil3d.Mcp.Server.Tests` | No | test projects not in the scanned list `MirrorTests.cs:68,87` |
| Edit McpShared walker / `AnalyzeResult` / `ToolValidator` | No | outside both folders |
| Any edit to `MainThreadExecutor.cs` / `AutocadScriptRunner.cs` (mirrored) | Yes unless the same edit lands in the Civil copy | `MirrorTests.cs:25-40` |
| Any edit to `BridgeEntry.cs`, `ScriptingSelfCheck.cs`, `AutocadHostProfile.cs`, `ExecuteAutocadCodeTool.cs` … | Yes (sha pin) until ported + re-pinned | `MirrorTests.cs:104-116` |
| New file inside `HPAutoCad.Mcp.Server/` (outside SeedLibrary), e.g. allowlist JSON or per-project `.editorconfig` | Yes — "no Civil decision" | `MirrorTests.cs:87-92`; `.editorconfig` extension is not excluded `MirrorTokenTable.cs:75` |
| `.editorconfig` at `HPAutoCad/` / `HPCivil3d/` solution root | No | root is not a scanned project dir |

Mirror-safe recipe: (1) no bridge-file edit is needed at all (§4); (2) put the quality test + allowlist in the `*.Mcp.Server.Tests` projects; (3) `.editorconfig` only at solution root; (4) if an engine change ever needs a bridge edit, edit both copies in the same commit and re-pin any `autocadSha256` touched. No `mirror-tokens.json` change required by the plan as scoped.

## 4. How the AutoCAD/Civil bridges build `AnalyzeResult`

- `HPAutoCad/HPAutoCad.McpBridge/MainThreadExecutor.cs:152` → `ScriptAnalyzer.Run(_compiler, request.Code, GuardProfile.Autocad, AnalyzerProfile.Autocad)`.
- `HPCivil3d/HPCivil3d.McpBridge/MainThreadExecutor.cs:152` → same with `GuardProfile.Civil3d, AnalyzerProfile.Civil3d`.
- Dispatched by the shared `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:127` (`_executor.Analyze(parameters)`); `ScriptAnalyzer.Run` `McpShared/…/Scripting/ScriptAnalyzer.cs:21-35` calls `Analyze` `:40-53`, which builds the `AnalyzeResult` → a walker hooked into `Analyze` reaches both bridges with **zero host code change** (E1 verified for these two hosts).
- Delivery = rebuild + redeploy the bundle with the new `HPRebar.McpBridge.Core.dll` in `Contents\Bridge\`: AutoCAD unified `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\` (deployed by `HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj:15-17`; `HPAutoCad/Directory.Build.props:3` stops the legacy `HPAutoCad.McpBridge.bundle` deploy); Civil `HPCivil3d.McpBridge.bundle` (`HPCivil3d.McpBridge.Loader.csproj:17`). AutoCAD / Civil 3D must be closed; SECURELOAD re-prompts per unsigned DLL hash (Civil ×4, CLAUDE.md:220). Until redeployed: old bridge answers without the new fields → "quality not analysed" (acceptance 3). Live AutoCAD/Civil = `CHƯA TEST` unless run.

## 5. `autocad-civil` appendix — rules with sources

| # | Rule | Source |
|---|---|---|
| A1 | Scripts never open, commit, abort or dispose a transaction or lock: `tr` is the bridge's; outer group + inner `tr`, `dryRun`/`none` abort the outer. Guard denies `StartTransaction`, `StartOpenCloseTransaction`, `TopTransaction`, `LockDocument`, `tr.Commit/Abort/Dispose`. | `GuardProfile.cs:14-42` (summary: a leaked wrapper finalised on GC thread takes acad.exe down, "verified live"); `AutocadScriptRunner.cs:61,70,90-92,130-149`; CLAUDE.md:154 |
| A2 | Wrapper disposal is correctness: every AutoCAD .NET wrapper an add-in creates (Transaction, DocumentLock, DBObject opened outside `tr`) is disposed deterministically (`using var`). Undisposed wrapper = crash, not leak. | `docs/journals/2026-09-14-phase-2-autocad-bridge-runtime.md:47`; `plans/260913-0000-autocad-mcp-bridge-2026/reports/phase-02-code-review.md:32` |
| A3 | Document lock is the bridge's (`DocumentLockMode.ProtectedAutoWrite`, undo name "HPMCP"); add-in commands that write lock their own document. | `AutocadScriptRunner.cs:70`; CLAUDE.md:154 |
| A4 | No interactive prompts / command-context escapes / modal UI from scripts (`ed.Get*`, `Select*`, `SendStringToExecute`, `Command*`, `ShowModal*`). | `GuardProfile.cs:27-34` |
| A5 | mm at the tool boundary via `units` (`ScriptUnits`), whatever INSUNITS is. | `McpShared/…/Scripting/ScriptUnits.cs:11`; CLAUDE.md:154, :164 (ADR-02 of AEC plan) |
| A6 | Civil: plan x/y in mm via `units`, but stations/elevations/areas in the **Civil drawing unit** with `drawingUnit` in every envelope; `units` follows Civil settings (Meters 1000 / Feet 304.8), INSUNITS only without Civil settings. | `HPCivil3d/HPCivil3d.McpBridge/Service/Civil3dUnitTable.cs:8-20,36`; CLAUDE.md:212, :214 |
| A7 | Entities addressed by **handle**, resolved through one resolver with INVALID_HANDLE / ERASED / NOT_AN_ENTITY codes; never `ObjectId` across calls. | CLAUDE.md:164 (ADR-02 AEC) |
| A8 | Tolerances from `GeometryTolerance` (no literals in services). | `HPAutoCad/HPAutoCad.Aec/Geometry/GeometryTolerance.cs:11-24`; CLAUDE.md:164 |
| A9 | Writes are two-phase: validate everything opened for read, then upgrade + apply; atomic batch = nothing written on a refused item. | `HPAutoCad.Aec/Cad/EditContext.cs:97-131` (`OpenForEdit`, `Upgrade`); CLAUDE.md:164 (phase-C review) |
| A10 | Results stay under the 64 KB cap: every list paged (`limit` ≤ engine cap + `offset`), caps pinned by seed tests. | `McpShared/…/Model/BridgeSettings.cs:22` (`MaxOutputBytes = 64*1024`); `HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs:168`; Civil `SeedLibraryTests.cs:181` |
| A11 | Seed contract: body ends in `return`; `args.X("literal", default)` for every schema key; schema default = code fallback (Civil); caller errors `ArgumentException`; AEC seeds = one `AecTools.*` call. | CLAUDE.md:156, :164, :214; `SeedLibraryTests.cs:103-144`, Civil `:230` |
| A12 | Civil guard additions: `Rebuild*`, `DataShortcuts`, `SurveyProject*`, `ExportTo*`, `CreateFrom*`, `ImportPoints/ExportPoints`, `CreateSolids*ToFile`, namespaces `Autodesk.Civil.DataShortcuts/AeccUiMgd`, `Autodesk.AECC.Interop`. Civil `Entity`/`DBObject` via aliases, never `using Autodesk.Civil.DatabaseServices`. | `GuardProfile.cs:176-192`; CLAUDE.md:210, :212 |
| A13 | .NET 8 AutoCAD add-in = Loader in default ALC + real code in its own `AssemblyLoadContext`; host prefixes `Ac`/`Ad`/`Autodesk.` resolved from the host. | `.claude/rules/development-rules.md:89`; `HPAutoCad.McpBridge.Loader/BridgeLoadContext.cs:17,26` |
| A14 | One shared ribbon tab `HPAutoCad` (`HPAUTOCAD_MCP_TAB`), one panel per tool, FindTab-or-create, own panel only, rebuild panel on COLORTHEME, remove tab only when empty, log `ribbon panel … added`. Civil = own tab `HPCIVIL3D_MCP_TAB`, same protocol. | `.claude/rules/development-rules.md:82-88`; `McpRibbonTab.cs:24,26,69-90,128,136` |
| A15 | Drawing-writing commands never use `CommandFlags.NoUndoMarker`; WPF TwoWay props need setters; `EnterContextualReflection` around `InitializeComponent`. | `.claude/rules/development-rules.md:91-92` |
| A16 | Civil folder is a mirror: AutoCAD bridge edits must be ported (tokens / `civil-only` blocks / re-pin). | CLAUDE.md:208; `MirrorTests.cs:6-10` |

## 6. Seed violation scan (heuristic, regex — **[inference]**, Roslyn walker will differ)

| Rule | AutoCAD (50) | Civil (12) |
|---|---|---|
| > 300 lines (block) | 0 | 0 |
| empty `catch { }` (block) | 0 | 0 |
| commented-out code (block) | 0 (4 `//` lines in all seeds, all prose) | 0 |
| block / local fn > 50 lines | 0 | 0 |
| nesting > 3 | 0 | **10 hits in 2 seeds**: `Pipe/list_pipe_networks` (foreach→if→foreach→try, L37-61), `Surface/list_surfaces` L26-27 |
| vague identifier | 1 borderline: `var result` `Layout/list_layouts/code.cs:8` (only if `result` is on the list) | 0 |
| `bool` param on local fn | 0 | 0 (`Like(string,string)`) |
| `catch (Exception)` no rethrow / no return | 0 (only catch: `Data/get_entities/code.cs:38` = `Autodesk.AutoCAD.Runtime.Exception` with a reason comment only) | **14 in 10 seeds**, all of form `catch (Exception ex) { Fail(Classify(ex), ex.Message, handle); }` (from generator `ERR` fragment) |

Implications: AutoCAD baseline allowlist likely empty. Civil: 0 blocking findings, warnings depend on two walker decisions — (a) does `try` count as a nesting level; (b) does a catch that **uses the exception variable to record an error** (`Fail(..., ex.Message, ...)` → `errors[]` envelope) count as "returns an error". Recommend (b) = yes (reporting, not swallowing) → Civil drops to ~2 nesting warnings. Since warnings never block, allowlist is only needed if the seed test asserts zero warnings. Test-case gold: `get_entities/code.cs:38-41` (catch whose body is only a reason comment → must NOT be flagged empty); `list_cogo_points/code.cs:25` and `get_drawing_info/code.cs:29` (prose comments containing `(…)` / `/` → must NOT be flagged commented-out code).

## 7. Risks

1. **Civil generator ownership** — fixing a Civil seed by hand is lost on regeneration; allowlist removal must go through `generate-seed-library.py`; fragment edits (`ERR`, `LIKE`) ripple into ~10 seeds and change their `_seeds.json` checksums → installed seeds upgrade on user machines (if unedited). Keep Civil seeds on the baseline unless the user wants the churn.
2. **Mirror trip-wire** — any "while we are here" edit to `MainThreadExecutor.cs`, `AutocadScriptRunner.cs`, `BridgeEntry.cs`, `AutocadHostProfile.cs`… turns `HPCivil3d.McpBridge.Tests` red; a new file in `HPAutoCad.Mcp.Server/` (allowlist, per-project `.editorconfig`) fails `Every_AutoCAD_bridge_source_file_is_either_mirrored_or_an_owned_counterpart`. Mitigation §3 recipe.
3. **Allowlist inside SeedLibrary** gets embedded into the exe (`csproj:35`) and may be treated as a seed by `SeedInstaller` **[inference]** — keep it in the tests project.
4. **Validator tests with `analysis: null`** (AutoCAD `:238`, Civil `:133`) — "not analysed" must stay a warning or 62 tests break.
5. **Redeploy friction** — AutoCAD may be in use; SECURELOAD per DLL hash; live propose→publish on AutoCAD/Civil stays `CHƯA TEST` in this plan.
6. **False positives**: nesting via `try`; catch-and-record pattern; prose comments with parentheses/semicolons; anonymous-object braces mistaken for blocks (a syntax walker avoids this, regex does not).
7. **Doc drift found**: CLAUDE.md "24 mirrored files" vs 29 in json; development-rules.md:88 dangling HPGeo path; tracked `HPGeo/tools/node_modules` (258 files).

**Status:** DONE
**Summary:** AutoCAD (50 hand-written seeds) and Civil (12 generated seeds) both reach the walker through the shared `ScriptAnalyzer.Run` with no host edit; tests projects already reference Bridge.Core; quality tests + allowlist belong in the `*.Mcp.Server.Tests` projects, which the mirror test never scans.
**Concerns:** Civil warnings hinge on two walker decisions (`try` as nesting; catch that records `ex` = reporting); Civil fixes must go through the generator; HPGeo/ is a husk (bin/obj + tracked node_modules) with one stale rule reference.
