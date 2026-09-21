# Dispatch for Worker Milestone 2 (Iteration 2 Fix): HPTekla.McpBridge

## 2026-09-21T18:12:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m2_gen2
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (header ## 2026-09-21T17:20:33Z)
- Challenger 1 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m2_1\handoff.md
- Challenger 2 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m2_2\handoff.md
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Ownership:
You have exclusive write access to:
- `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs`
- `HPTekla/HPTekla.McpBridge/PluginAssemblyResolver.cs`
- `HPTekla/HPTekla.McpBridge.Tests/**`

Objective:
Remediate the 3 items identified by Reviewer 2, Challenger 1, and Challenger 2:
1. `TeklaBridgeExecutor.cs`: Add timeout `CancellationTokenSource`:
   ```csharp
   var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.TeklaHeavyMaxTimeoutSeconds);
   using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
   using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
   _currentCancel = linkedCts;
   ```
2. `TeklaBridgeExecutor.cs`: In `ExecuteAsync`, catch and rethrow `BridgeRequestException`:
   ```csharp
   catch (BridgeRequestException)
   {
       throw;
   }
   ```
3. `PluginAssemblyResolver.cs`: Differentiate reflective vs in-plugin requests:
   When `requester is not null && IsInFolder(requester, folder)` (i.e. our own plugin), allow binding forward to higher available major versions (`requested <= available`).
4. Build & Test:
   - Run `dotnet test HPTekla/HPTekla.McpBridge.Tests/HPTekla.McpBridge.Tests.csproj`
   - Run `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release`
   Ensure all tests pass 100% and build succeeds with 0 errors.
5. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m2_gen2\report.md` and handoff to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m2_gen2\handoff.md`.
