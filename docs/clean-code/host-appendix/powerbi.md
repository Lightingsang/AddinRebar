# Host appendix — Power BI (AMO/TOM, ADOMD, MSAL)

> Read with [HP_CLEAN_CODE_CORE.md](../HP_CLEAN_CODE_CORE.md) and the bridge-app rules CO1/CO12 of [com-standalone.md](com-standalone.md). Scope: `HPPowerBi/`. Shape: standalone WPF bridge (net8.0-windows) talking to Power BI Desktop's local Analysis Services (AMO/TOM `Server` + `AdomdConnection`) and to the Power BI Service over REST with MSAL. The 12 domain tools are compiled classes in the server, not seeds. Sources as of 2026-10-04 (`plans/261004-1005-hp-clean-code-ai-tools/reports/map-05-powerbi.md`).

| Id | Rule | Source |
|---|---|---|
| PB1 | Scripts never close or dispose the bridge's connections; any `.Dispose()` call is denied, so scripts dispose their own objects with `using var` declarations; the bridge owns teardown | `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs:110-119`; `HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiDaxExecutor.cs:53-54` |
| PB2 | Mutation detection is textual: write through `model.SaveChanges()` / named collection operations so the snapshot regex sees it, or declare `transaction: "auto"` | `HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSafetyGuard.cs:170` |
| PB3 | Never log or return tokens or secrets; tokens stay in memory, logs show only the expiry; scripts get no cloud client global | `HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs:113`; `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs:160` |
| PB4 | Service error bodies are sanitised before they reach a result (they may carry tenant or request ids) | `HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs:142, 285` (known gap H-08) |
| PB5 | REST calls go through the injected `HttpClient`, dispose request/response, pass `ct`, clamp `$top` 1..1000 | `HPPowerBi/HPPowerBi.McpBridge/Cloud/PowerBiCloudClient.cs:132-137` |
| PB6 | DAX results capped (`maxRows` 1..10 000, default 100); the DAX validator refuses XMLA/TMSL text and `KILL SPID` | `HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiDaxExecutor.cs:48`; `HPPowerBi/HPPowerBi.McpBridge/Safety/PbiSafetyGuard.cs:136-156` |
| PB7 | Upserts find-then-update-or-add with one `SaveChanges` and say `Created`/`Updated` | `HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiMeasureService.cs:45-83` |
| PB8 | Every call that writes to the Service checks the bridge's opt-in, like local writes do | expected rule; violated today (H-01) |

Known gaps (H-01, H-02): cloud handlers check neither opt-in (`HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs:338-401`); the cloud client is created without credentials (`HPPowerBi/HPPowerBi.McpBridge/BridgeEntry.cs:68`).
Tests: `HPPowerBi.Mcp.Server.Tests` (tool catalog pinned at 22). The seed-quality test accepts an empty library and checks any future seed.
