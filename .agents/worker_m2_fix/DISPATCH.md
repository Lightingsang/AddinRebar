## 2026-09-21T07:10:05Z
You are worker_m2_fix, a teamwork_preview_worker.
Your working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_fix
Project root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Authoritative user request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically section ## 2026-09-21T06:10:48Z)
Reviewer 2 handoff report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\handoff.md
Project Blueprint: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_5\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Objective:
Remediate the two defects identified by Reviewer 2 for Milestone 2:
1. Wire PowerBiDispatcher into the Named Pipe listener so custom Power BI methods (powerbi.dax, powerbi.schema, etc.) are genuinely routed instead of being rejected by RequestDispatcher.
2. Fix the theme dictionary lookup scope in MaterialThemeBridge so it falls back to Application.Current.Resources.

Detailed Steps:
1. Additive Extension in McpShared (non-breaking):
   - In McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:
     Add an optional constructor parameter:
     `Func<long, JsonRpcEnvelope, NdjsonPipeWriter, CancellationToken, Task<JsonRpcEnvelope?>>? customHandler = null`
     In `DispatchAsync`, inside the `default:` case of `switch (JsonRpcMethods.Suffix(method))`:
     ```csharp
     if (_customHandler is not null)
     {
         var customResponse = await _customHandler(id, request, writer, cancellationToken).ConfigureAwait(false);
         if (customResponse is not null)
             return customResponse;
     }
     ```
   - In McpShared/HPRebar.McpBridge.Core/Host/McpBridgeHost.cs:
     Add optional parameter `Func<long, JsonRpcEnvelope, NdjsonPipeWriter, CancellationToken, Task<JsonRpcEnvelope?>>? customHandler = null` to the `McpBridgeHost` constructor (defaulting to `null`), and pass it to `new RequestDispatcher(executor, settings, hostVersion, hostName, executionDisabledMessage, customHandler)`.
2. Connect PowerBiDispatcher in HPPowerBi.McpBridge:
   - In HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs:
     Ensure it exposes a method matching the delegate signature `Task<JsonRpcEnvelope?> DispatchCustomAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)`.
   - In HPPowerBi/HPPowerBi.McpBridge/BridgeEntry.cs:
     Pass `dispatcher.DispatchCustomAsync` to `new McpBridgeHost(...)`.
3. Fix Theming Dictionary Scope in HPPowerBi.McpBridge:
   - In HPPowerBi/HPPowerBi.McpBridge/Resources/Themes/MaterialThemeBridge.cs:
     Update `Apply`:
     ```csharp
     var seed = FindThemeDictionary(window.Resources)
                ?? (Application.Current is not null ? FindThemeDictionary(Application.Current.Resources) : null);
     if (seed is not null)
     {
         var seedTheme = seed.GetTheme();
         var theme = Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, seedTheme.PrimaryMid.Color, seedTheme.SecondaryMid.Color);
         overlay.SetTheme(theme);
     }
     ```
4. Verification:
   Run:
   dotnet build HPPowerBi/HPPowerBi.slnx -c Debug
   dotnet run --project HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj
   dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
   Ensure 100% of tests pass, 0 warnings, 0 errors.
   Write handoff report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_fix\handoff.md
   Send completion message via send_message to orchestrator_5 (conversation ID: 4d88b310-8910-4f85-b5a8-50216392bc6b) when done.
