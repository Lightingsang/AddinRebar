# Map 05 — HPPowerBi (script-quality walker impact)

Read-only scout, 2026-10-04. Paths relative to repo root. Inference marked GIẢ ĐỊNH CHƯA XÁC MINH.

## 0. Headline findings

| # | Finding | Evidence |
|---|---|---|
| H1 | **Zero embedded seeds.** No `HPPowerBi.Mcp.Server/Registry/` folder exists; the csproj embeds `Registry\SeedLibrary\**` only `Condition="Exists(...)"`. The 12 domain tools are **native C# MCP tool classes** + custom bridge JSON-RPC methods, not `tool.json + code.cs` seeds | [HPPowerBi.Mcp.Server.csproj:28-29](HPPowerBi/HPPowerBi.Mcp.Server/HPPowerBi.Mcp.Server.csproj#L28-L29); `ls` → "No such file or directory" |
| H2 | Analyze path = shared engine unchanged → a walker added inside `ScriptAnalyzer.Run` reaches Power BI with **no Power BI source change** (bridge exe rebuild/redeploy only) | [PowerBiBridgeExecutor.cs:293-296](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L293-L296) |
| H3 | Seed-quality test must pass on an **empty library** (vacuous, or assert "no SeedLibrary dir") — a "≥ 1 seed" assertion would fail here | H1 |
| H4 | Server test project already references `HPRebar.McpBridge.Core` (Roslyn) → walker callable from the test without a new reference (P7 ok) | [HPPowerBi.Mcp.Server.Tests.csproj:22](HPPowerBi/HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj#L22) |

## 1. Bridge shape

- **Standalone WPF desktop app**, own process, `WinExe` `net8.0-windows`, `UseWPF` — not net48, nothing loads into PBIDesktop.exe: [HPPowerBi.McpBridge.csproj:4-5](HPPowerBi/HPPowerBi.McpBridge/HPPowerBi.McpBridge.csproj#L4-L5).
- Packages: `Microsoft.AnalysisServices` (AMO-TOM), `Microsoft.AnalysisServices.AdomdClient`, `Microsoft.Identity.Client` (MSAL), MaterialDesignThemes 5.3.2, System.Management (WMI discovery): [csproj:17-30](HPPowerBi/HPPowerBi.McpBridge/HPPowerBi.McpBridge.csproj#L17-L30). Refs McpShared Contracts + McpBridge.Core only: [csproj:34-35](HPPowerBi/HPPowerBi.McpBridge/HPPowerBi.McpBridge.csproj#L34-L35).
- Local connection: TOM `Server` + ADOMD `AdomdConnection("Data Source=localhost:{port};Initial Catalog={db}")` to PBIDesktop's msmdsrv.exe port: [PbiConnectionManager.cs:101-103](HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiConnectionManager.cs#L101-L103).
- Cloud: MSAL **client credentials only** (`ConfidentialClientApplicationBuilder` + secret, scope `analysis.windows.net/powerbi/api/.default`), REST via one `HttpClient` to `api.powerbi.com/v1.0/myorg/`: [PowerBiCloudClient.cs:49-50](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L49-L50), [:102-108](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L102-L108). XML doc claims Interactive/Device Code too ([:45](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L45)) — not implemented.
- Pipe `hppowerbi-mcp-2026`, host `McpBridgeHost` reused with custom handler: [BridgeEntry.cs:73-75](HPPowerBi/HPPowerBi.McpBridge/BridgeEntry.cs#L73-L75).
- **Guard profile** `GuardProfile.PowerBi`: identifier `MessageBox`; members `Disconnect`, `Dispose` **denied on every receiver**; `server.{Disconnect,Dispose}`, `adomd.{Close,Dispose}`; namespaces `System.Windows.Forms`, `HPPowerBi.McpBridge`, `HPRebar.McpBridge.Core.Host`: [GuardProfile.cs:110-119](McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs#L110-L119). `AnalyzerProfile.PowerBi` = no transaction names: [AnalyzerProfile.cs:38-40](McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs#L38-L40).
- **Analyze** = `ScriptAnalyzer.Run(_compiler, code, GuardProfile.PowerBi, AnalyzerProfile.PowerBi)`; `request.Transaction` ignored: [PowerBiBridgeExecutor.cs:293-296](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L293-L296). `AnalyzeResult` built at [ScriptAnalyzer.cs:46](McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs#L46), guard+compile added [:21-35](McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs#L21-L35). Routed by base `RequestDispatcher` (custom handler returns null for non-Power-BI suffixes): [PowerBiDispatcher.cs:101-114](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs#L101-L114).
- **Execute** does NOT call ScriptAnalyzer: opt-in → `ScriptGuard.Check` → regex mutation detect → compile → dryRun static preview → snapshot → run: [PowerBiBridgeExecutor.cs:94-185](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L94-L185).

## 2. TMSL snapshot interplay

- Script path: snapshot only when `isMutation` = regex `SaveChanges|Measures.Add|…|Columns.Remove` **or** `transaction == auto`: [PowerBiBridgeExecutor.cs:111-112](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L111-L112), regex [PbiSafetyGuard.cs:164-171](HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSafetyGuard.cs#L164-L171); snapshot taken after connect check, failure aborts the run: [PowerBiBridgeExecutor.cs:150-162](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L150-L162); name returned in `ExecuteResult.Snapshot` [:216](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L216).
- Native write tools snapshot before mutation (measure upsert/delete, relationship): [PowerBiDispatcher.cs:221-226](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs#L221-L226), [:267-271](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs#L267-L271), [:309-313](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs#L309-L313).
- Snapshot = `JsonSerializer.SerializeDatabase(db, IncludeRestrictedInformation=false)` to `%LocalAppData%\HPPowerBi\Snapshots\`, prune 50: [PbiSnapshotManager.cs:48-56](HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSnapshotManager.cs#L48-L56). Restore only deserialises (no apply-back API wired): [:64-78](HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSnapshotManager.cs#L64-L78).
- **Quality findings are propose-time only** (`*.analyze` → `ToolValidator`), never on the execute path → no interplay with snapshots. In the analyze answer they sit beside `GuardViolations` (code `GUARD` when surfaced by execute, [PowerBiBridgeExecutor.cs:101](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L101)) and compiler `Diagnostics`; the new `QualityFindings{RuleId,Severity,Line,Column,Message}` is a separate list — do not merge into `Diagnostics` (would make `Compiles`-style consumers see errors).
- Note: walker runs regardless of guard verdict? `Run` only compiles when guard is clean ([ScriptAnalyzer.cs:26](McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs#L26)); quality walker is syntax-only → run it unconditionally like `Analyze` facts.

## 3. Seeds

| Item | Value |
|---|---|
| Embedded seeds | **0** (H1). `appsettings.json` still has `InstallSeeds: true` (no-op) |
| "powerbi_*" tools (native) | 8 local: `get_powerbi_context`, `execute_powerbi_code`, `powerbi_get_schema`, `powerbi_evaluate_dax`, `powerbi_create_or_update_measure`, `powerbi_delete_measure`, `powerbi_manage_relationship`, `powerbi_format_dax`; 4 cloud: `powerbi_cloud_list_workspaces/_list_datasets/_trigger_refresh/_execute_dax` — [PowerBiHostProfile.cs:38-45](HPPowerBi/HPPowerBi.Mcp.Server/Hosts/PowerBi/PowerBiHostProfile.cs#L38-L45) |
| `tools/list` count | 22 (12 + `inspect_type` + `cancel_execution` + 8 registry), pinned [PowerBiToolCatalogTests.cs:54](HPPowerBi/HPPowerBi.Mcp.Server.Tests/PowerBiToolCatalogTests.cs#L54) (AGENTS.md says 20 — stale) |
| Largest `code.cs` | n/a. Largest native tool class 95 lines `PowerBiManageRelationshipTool.cs`; largest bridge file `PowerBiDispatcher.cs` 418 (> 300-line review trigger, existing — Boy Scout only) |
| Generator script | none |
| Existing seed tests | none. Server tests 51 `[Fact/Theory]` in 5 files (profile, catalog, pipe round trips with linked `FakeRevitExecutor`); bridge tests 133 in 15 files. No `SeedLibrary*Tests` |
| Test → McpBridge.Core | yes, both test projects ([server tests csproj:22](HPPowerBi/HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj#L22), [bridge tests csproj:21](HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj#L21)) |

## 4. Patterns the rules would hit

Walker scope = scripts/seeds → **0 hits** (no seeds). Same rules applied by reviewers to native bridge code (checklist, not walker), rough counts:

| Rule | Hits | Where |
|---|---|---|
| Empty catch, no reason (blocking class) | 3 | [PowerBiBridgeExecutor.cs:341](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L341), [PbiConnectionManager.cs:132-133](HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiConnectionManager.cs#L132-L133) |
| Bare catch with reason comment (passes) | 3 | [PowerBiBridgeExecutor.cs:199-201](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L199-L201), [WindowsHostTheme.cs:28-31](HPPowerBi/HPPowerBi.McpBridge/Resources/Themes/WindowsHostTheme.cs#L28-L31), [:50-53](HPPowerBi/HPPowerBi.McpBridge/Resources/Themes/WindowsHostTheme.cs#L50-L53) |
| `catch (Exception)` swallow (log + continue) | ~4 | snapshot prune [PbiSnapshotManager.cs:108-111](HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSnapshotManager.cs#L108-L111), [:119-123](HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSnapshotManager.cs#L119-L123); disconnect [PbiConnectionManager.cs:169-171](HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiConnectionManager.cs#L169-L171). Dispatcher's 12 `catch (Exception)` **return** an error envelope → not swallowing |
| MSAL token cache catch | 0 | no try/catch around `AcquireTokenForClient` ([PowerBiCloudClient.cs:108](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L108)) |
| Vague identifiers | ~20 (inference) | `p` ×7 params in dispatcher, `ro/cb/ir/tsm/t` in cloud JSON parse ([PowerBiCloudClient.cs:153-194](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L153-L194)), `doc`, `json`, `kw` |
| Block > 50 lines / nesting > 3 | 1 / 0-1 | `ExecuteAsync` ~160 lines [PowerBiBridgeExecutor.cs:78-240](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L78-L240) |

AI-proposed scripts likely to trip warnings (GIẢ ĐỊNH CHƯA XÁC MINH): `var res = …ExecuteReader()`, `catch (Exception) { }` around `SaveChanges`, `data` row arrays.

## 5. Appendix `powerbi` — rule material

| Topic | Rule to write | Source |
|---|---|---|
| Connection lifecycle | Scripts never close/dispose the bridge connections; **any** `.Dispose()` call is denied (global member deny) → use `using var cmd = adomd.CreateCommand()` / `using var reader = …` declarations (not member access, so allowed); `reader.Close()` allowed. Bridge owns teardown | [GuardProfile.cs:113-118](McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs#L113-L118); pattern [PbiDaxExecutor.cs:53-54](HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiDaxExecutor.cs#L53-L54) |
| Mutation detection is textual | Write through `model.SaveChanges()` / named collection ops so the regex sees it; otherwise set `transaction:"auto"` or no snapshot is taken | [PbiSafetyGuard.cs:170](HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSafetyGuard.cs#L170) |
| Credentials / tokens | Never log/return tokens or secrets; current log prints only expiry ([PowerBiCloudClient.cs:113](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L113)); token cached in-memory only; snapshots exclude restricted info. Scripts have no cloud client global (globals list) — keep it so | [HostScriptContracts.cs:160](McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs#L160) |
| Cloud error text | HTTP body echoed verbatim into exceptions/results — sanitise (may carry tenant/request ids) | [PowerBiCloudClient.cs:142](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L142), [:285](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L285) |
| Cloud calls | REST via injected `HttpClient`, `using` request/response, `ct` passed, `$top` clamped 1..1000; scripts cannot reach network (base guard denies `System.Net`) | [PowerBiCloudClient.cs:132-137](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L132-L137) |
| DAX caps | `maxRows` clamp 1..10 000, default TopN 100; DAX validator blocks XMLA `<`, TMSL `{`, `KILL SPID`, `DISCOVER_TRACE` | [PbiDaxExecutor.cs:48](HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiDaxExecutor.cs#L48), [PowerBiDispatcher.cs:21](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs#L21), [PbiSafetyGuard.cs:136-156](HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSafetyGuard.cs#L136-L156) |
| Measure idempotence | Upsert = `Measures.Find` → update-or-add, one `SaveChanges`; result says `Created`/`Updated`. Re-run with same args is state-idempotent, but MCP annotation says `Idempotent=false` and each call writes a new snapshot | [PbiMeasureService.cs:45-83](HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiMeasureService.cs#L45-L83), [PowerBiCreateOrUpdateMeasureTool.cs:18](HPPowerBi/HPPowerBi.Mcp.Server/Tools/PowerBiCreateOrUpdateMeasureTool.cs#L18) |
| Refresh triggers | `POST …/refreshes` fire-and-forget (`Accepted` + requestId), no polling, `Destructive=true` | [PowerBiCloudClient.cs:206-243](HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs#L206-L243) |
| Timeouts | script 5..600 s clamp; native tools fixed 30 s | [PowerBiBridgeExecutor.cs:165](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L165), [PowerBiCreateOrUpdateMeasureTool.cs:69](HPPowerBi/HPPowerBi.Mcp.Server/Tools/PowerBiCreateOrUpdateMeasureTool.cs#L69) |

## 6. Risks

1. **Empty seed library** → seed test must not require ≥ 1 seed; baseline allowlist empty for Power BI.
2. **Cloud handlers bypass both opt-ins** — workspaces/datasets/refresh/cloud DAX never call `EnsureExecutionAllowed`/`EnsureMutationAllowed` (local handlers do) → `trigger_refresh` writes to the Service with checkboxes off: [PowerBiDispatcher.cs:338-401](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs#L338-L401) vs [:121](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs#L121). Behaviour defect → log (B-xx), not fix in this plan.
3. **Cloud client never gets credentials** — `new PowerBiCloudClient()` with no args; env `POWERBI_*` read only for the status label → cloud tools likely always throw "TenantId and ClientId must be configured": [BridgeEntry.cs:68](HPPowerBi/HPPowerBi.McpBridge/BridgeEntry.cs#L68), [StatusViewModel.cs:395-397](HPPowerBi/HPPowerBi.McpBridge/ViewModels/StatusViewModel.cs#L395-L397). GIẢ ĐỊNH CHƯA XÁC MINH live. Log.
4. **`test_tool` cannot test** — dryRun returns a static preview `IsError=false` before connecting, so any compiling script "passes": [PowerBiBridgeExecutor.cs:132-140](HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiBridgeExecutor.cs#L132-L140). Quality walker is the only real signal pre-publish.
5. **No transaction check at propose** — analyze ignores `Transaction`, so a `none` tool calling `SaveChanges` is not refused (ETABS does refuse). Out of scope; note in appendix.
6. Secrets in seeds: none (no seeds); `ClientSecret` only from properties. Seed rule for future: no literal GUID tenant/client ids or secrets in `code.cs` (walker could flag long hex/GUID literals — not in contract, skip).
7. Test skips without Power BI: none found (`Skip*` grep empty); tests use fakes/`HttpMessageHandler` stubs, so the new seed test runs anywhere.
8. Old bridge → `quality not analysed` until `HPPowerBi.McpBridge.exe` is rebuilt/republished (standalone exe, user starts it).

**Status:** DONE_WITH_CONCERNS
**Summary:** HPPowerBi has no embedded seeds; the walker reaches it through the unchanged shared `ScriptAnalyzer.Run` call and the seed test must tolerate an empty library.
**Concerns:** cloud handlers skip both opt-ins and the cloud client is never given credentials — log as behaviour defects, do not fix here.
