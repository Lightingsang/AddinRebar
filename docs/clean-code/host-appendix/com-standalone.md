# Host appendix — COM standalone bridges (ETABS, SAP2000, Robot, Excel; Power BI app shape)

> Read with [HP_CLEAN_CODE_CORE.md](../HP_CLEAN_CODE_CORE.md). Scope: `HPEtabs/`, `HPSap2000/`, `HPRobot/`, `HPExcel/`; the bridge-app rules (CO1, CO12) also apply to `HPPowerBi/` (see [powerbi.md](powerbi.md) for its API). Shape: the bridge is a user-started WPF desktop app holding the one host attachment and the pipe; the net10 server never references the host wrapper. Sources as of 2026-10-04 (`plans/261004-1005-hp-clean-code-ai-tools/reports/map-03-com-standalone.md`).

| Id | Rule | Source |
|---|---|---|
| CO1 | One host attachment per bridge process, owned by one worker; single instance per user | `HPEtabs/HPEtabs.McpBridge/EtabsExecutor.cs:65`; Robot/Excel `Program.cs` mutex |
| CO2 | One dedicated STA worker owns every host call, behind a queue, with a control lane for attach/detach | ETABS `HPEtabs/HPEtabs.McpBridge/EtabsExecutor.Worker.cs:8-33` (`MainThreadQueue(expireWithoutTicks)`); Robot `RobotStaWorker.cs:30`, Excel `ExcelStaWorker.cs:30` (own STA workers) |
| CO3 | A COM message filter where the host rejects calls (RETRYLATER) | Robot/Excel `ComInteropHelper.cs` |
| CO4 | Units forced for the run and restored in `finally`; a failed restore is a warning | `HPEtabs/HPEtabs.McpBridge/Service/EtabsUnitsPolicy.cs:13-35`; `SapUnitsPolicy.cs`; `RobotUnitsPolicy.cs` |
| CO5 | A snapshot before every write/destructive run; a failed save or copy **fails the run** | `HPEtabs/HPEtabs.McpBridge/Service/EtabsSnapshotManager.cs:52-56,93` |
| CO6 | Run-time path policy for every file-taking member (absolute, local, outside the bridge's own state folders) | `HPEtabs/HPEtabs.McpBridge/Service/EtabsPathPolicy.cs:13`; `HPSap2000/HPSap2000.McpBridge/Service/SapPathPolicy.cs:9` |
| CO7 | Tier allow-list R/W/D, generated and reviewed, fail-closed for unknown members | `HPEtabs/HPEtabs.McpBridge/Resources/etabs-oapi-tiers.txt` + `HPEtabs/HPEtabs.McpBridge/Service/EtabsTierAnalyzer.cs:29-36` |
| CO8 | The tier verdict is also given at analyze time, so `propose_tool` refuses a destructive tool or a `none` tool that writes; tier diagnostics stay in `GuardViolations`, quality findings in `QualityFindings` | `EtabsExecutor.cs:212-232`; `SapExecutor.cs:177-197` |
| CO9 | Every host return code is checked → `InvalidOperationException("… returned {ret} from X")` | ETABS/SAP seed contract (CLAUDE.md "HPEtabs MCP Bridge") |
| CO10 | Caller mistakes → `ArgumentException` (excluded from the stability window) | seeds of all four hosts |
| CO11 | Output fields carry units (`fzKN`, `lengthMm`, `eMPa`) | ETABS/SAP seeds |
| CO12 | The bridge is published as a folder (`PublishSingleFile=false`): Roslyn needs `Assembly.Location` | bridge csproj files |
| CO13 | The host wrapper DLL is referenced with `Private=false` and resolved from the install at run time, before any method naming its types is JIT-compiled | `HPEtabs.McpBridge` csproj; `HPRobot/HPRobot.McpBridge/Com/RobotAssemblyResolver.cs:73` |
| CO14 | A COM probe that may fail (lookup by name, optional property) catches the narrowest exception type and says why in a comment; never a bare `catch { }` | core Q-B2; `HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Data/read_table/code.cs:13,25` (allowlisted baseline) |

## Known gaps (logged, not fixed — [CLEAN_CODE_AUDIT.md](../CLEAN_CODE_AUDIT.md) §2a)
| Host | Gap | Rule |
|---|---|---|
| SAP2000 | worker thread never set to STA | CO2 |
| Robot, Excel | no run-time path policy | CO6 |
| Robot, Excel | tiering by name/prefix (Excel: unknown member = read-only) and only at execute time | CO7, CO8 |
| Robot | failed `Project.Save()` swallowed before the snapshot copy (H-03) | CO5 |
| Robot | unlabelled output fields (`fx`, `fz`) | CO11 |

Tests: `<Host>.Mcp.Server.Tests` (compile checks skip without the installed host), `HPEtabs.McpBridge.Tests`. ETABS/Excel seeds come from `tools/generate-seed-library.py`; edit the generator, regenerate, retest.
