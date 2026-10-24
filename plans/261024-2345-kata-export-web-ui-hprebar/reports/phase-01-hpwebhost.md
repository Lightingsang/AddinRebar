# Phase 1 — HPWebHost shared library (2026-10-25)

## Moves (git mv, history kept)
| From | To |
|---|---|
| `HPBimToolkit.Core/Contracts/{BridgeEnvelopes,BridgeErrorCodes}.cs`, `Protocol/{BridgeJson,CommandDispatcher,PayloadReader,RequestParser}.cs` | `HPWebHost.Core/Protocol/` (ns `HPWebHost.Protocol`) |
| `HPBimToolkit.Core/Hosting/{AssetRoutes,PageOrigins}.cs` | `HPWebHost.Core/Hosting/` (ns `HPWebHost.Hosting`) |
| `HPBimToolkit/Host/{WebViewHost,EmbeddedAssetServer}.cs`, `Bridge/WebMessageTransport.cs` | `HPWebHost/Hosting/` |
| protocol/hosting tests | `HPWebHost.Core.Tests/` (dispatcher typed-payload test uses a local record instead of a dashboard DTO) |
- `DependencyPreloader` stays in the toolkit (JIT-compiling all of ILRepack-merged HPRebar.dll would cost seconds; HPRebar warms only WebView2).

## Generalised
- `PageOrigins.Resolve(virtualHost, devUrl, startPage = "index.html")`, `EmbeddedOrigin` instance property.
- `EmbeddedAssetServer(assembly, origins, productName, logger)`: placeholder for any missing embedded HTML page.
- `WebViewHost(WebViewHostOptions(UserDataFolder, DebugPortVariable, ProductName, LogFolder), navigate, logger)`.
- Projects: `HPWebHost.Core` net8.0 (pure), `HPWebHost` net8.0-windows (WebView2 compile-only, Serilog), `HPWebHost.Core.Tests`; slnx folder `/WebHost/`.

## Results
| Check | Result |
|---|---|
| `HPWebHost.Core.Tests` | ✅ 56/56 (+1 start-page test) |
| `HPBimToolkit.Core.Tests` | ✅ 31/31; `contracts.generated.ts` unchanged |
| Toolkit build + deploy (ships `HPWebHost.dll`, `HPWebHost.Core.dll`) | ✅ |
| `run-live-verify.ps1` (DynamoSampleModel copy) | ✅ 22/22, Revit responding |
| `live-verify-events.mjs` (prepared-site-plan copy) | ✅ 13/13 |
| `live-verify-lifecycle.mjs` (no `--theme`) | ✅ 6/6 |
