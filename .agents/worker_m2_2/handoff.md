# Handoff Report — Milestone M2 Remediation

**Worker:** `worker_m2_2` (HPRobot M2 Remediation Worker)  
**Parent:** `orchestrator_7` (conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Type:** Hard Handoff (Task complete)  
**Date:** 2026-09-21  

---

## 1. Observation

### 1.1 Pre-existing Defects Identified by Reviewer
- Reviewer `reviewer_m2_2` evaluated `HPRobot.McpBridge` in `.agents/reviewer_m2_2/handoff.md` and returned `REQUEST_CHANGES` with two specific findings:
  1. `HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialThemeBridge.cs` lines 39–51 searched only `window.Resources`. Because `MainWindow.xaml` does not define `MaterialBridge.xaml` in its window resources, `FindThemeDictionary(window.Resources)` was `null`, disabling MaterialDesign palette updates on theme changes.
  2. `HPRobot/HPRobot.McpBridge/Host/RobotDispatcher.cs` lines 53–94 invoked `_executor.Attachment.Attach()` and `Detach()` synchronously on MTA threadpool threads during named pipe dispatching, exposing COM objects to cross-apartment violations (`RPC_E_WRONG_THREAD` 0x8001010E).

### 1.2 Implemented Changes
- In `HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialThemeBridge.cs`:
  ```csharp
  var seed = FindThemeDictionary(window.Resources)
             ?? (Application.Current is not null ? FindThemeDictionary(Application.Current.Resources) : null);
  if (seed is not null)
  {
      var seedTheme = seed.GetTheme();
      var theme = Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, seedTheme.PrimaryMid.Color, seedTheme.SecondaryMid.Color);
      theme.Background = Token(palette, "Color.Background", theme.Background);
      theme.Foreground = Token(palette, "Color.Foreground.Primary", theme.Foreground);
      theme.ForegroundLight = Token(palette, "Color.Foreground.Secondary", theme.ForegroundLight);
      theme.ValidationError = Token(palette, "Color.Danger", theme.ValidationError);
      theme.Cards.Background = Token(palette, "Color.SurfaceElevated", theme.Cards.Background);
      theme.Cards.Border = Token(palette, "Color.Border", theme.Cards.Border);
      theme.ToolTips.Background = Token(palette, "Color.SurfaceElevated", theme.ToolTips.Background);
      overlay.SetTheme(theme);
  }
  ```
- In `HPRobot/HPRobot.McpBridge/Host/RobotDispatcher.cs`:
  ```csharp
  private async Task<JsonRpcEnvelope?> HandleAttach(long id)
  {
      try
      {
          await _executor.StaWorker.RunOnControlLaneAsync("pipe-attach", () =>
          {
              _executor.Attachment.Attach();
          }).ConfigureAwait(false);
          ...
      }
      ...
  }

  private async Task<JsonRpcEnvelope?> HandleDetach(long id)
  {
      try
      {
          await _executor.StaWorker.RunOnControlLaneAsync("pipe-detach", () =>
          {
              _executor.Attachment.Detach("Client requested detach");
          }).ConfigureAwait(false);
          ...
      }
      ...
  }
  ```

### 1.3 Direct Tool Outputs & Test Invocations
- `dotnet build HPRobot/HPRobot.slnx -c Debug`
  - Output: `Build succeeded. 0 Warning(s), 0 Error(s)`.
- `dotnet build HPRobot/HPRobot.slnx -c Release`
  - Output: `Build succeeded. 0 Warning(s), 0 Error(s)`.
- `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`
  - Output: `Test run summary: Passed! - total: 137, failed: 0, succeeded: 137, skipped: 0. Duration: 2s 765ms`.
- `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj -c Release`
  - Output: `Test run summary: Passed! - total: 137, failed: 0, succeeded: 137, skipped: 0. Duration: 2s 691ms`.
- `dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`
  - Output: `Passed! total: 613, failed: 0, succeeded: 613, skipped: 0`.
- `dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
  - Output: `Passed! total: 72, failed: 0, succeeded: 72, skipped: 0`.

---

## 2. Logic Chain

1. **Theming Dictionary Resolution**:
   - `MaterialBridge.xaml` is registered in `App.xaml` at the application resource dictionary level.
   - `FindThemeDictionary(window.Resources)` was falling back to null when window-level resources did not contain it.
   - Adding `?? (Application.Current is not null ? FindThemeDictionary(Application.Current.Resources) : null)` successfully captures the application's `IMaterialDesignThemeDictionary`.
   - Once resolved, `seed.GetTheme()` executes, and `overlay.SetTheme(theme)` correctly applies the active Dark/Light palette to MaterialDesign components without requiring duplicate dictionary merging in `MainWindow.xaml`.

2. **Apartment State Consistency for COM Operations**:
   - Robot COM automation (`RobotOM.dll`) requires single-threaded apartment access.
   - Incoming pipe requests arrive via `PipeListener` and `RequestDispatcher` on MTA threadpool worker threads.
   - Dispatching `Attach()` and `Detach()` to `_executor.StaWorker.RunOnControlLaneAsync(...)` guarantees that `GetActiveObject`, `_robot.Project`, `RefreshContext()`, and COM release logic execute exclusively on the STA thread (`HPRobot-STA-Worker`).
   - This prevents race conditions and eliminates the risk of `RPC_E_WRONG_THREAD (0x8001010E)`.

3. **Non-regression**:
   - The test suite `HPRobot.McpBridge.Tests` exercises `robot.attach` and `robot.detach` over a real Named Pipe in `RobotDispatcherWireChallengerTests.cs`.
   - All 137 tests continue to pass 100% in both Debug and Release configurations.
   - McpShared regression suites (613 server core tests and 72 Net48 tests) continue to pass 100%.

---

## 3. Caveats

- Milestone M2 Remediation scope is strictly limited to the two assigned files (`MaterialThemeBridge.cs` and `RobotDispatcher.cs`).
- Milestone M3 (Server & Seed Tools) will implement the client-side tooling that communicates with this bridge.

---

## 4. Conclusion

The M2 architectural remediation is complete and verified:
1. `MaterialThemeBridge.cs` now properly scopes and resolves theme dictionaries from `Application.Current.Resources`.
2. `RobotDispatcher.cs` dispatches all COM attachment operations through the dedicated STA worker control lane.
3. Solution builds cleanly with 0 warnings and 0 errors in both Debug and Release modes.
4. All 137 tests in `HPRobot.McpBridge.Tests` pass with zero failures.

---

## 5. Verification Method

To independently reproduce and verify:
1. Build both Debug and Release configurations:
   ```powershell
   dotnet build "HPRobot/HPRobot.slnx" -c Debug
   dotnet build "HPRobot/HPRobot.slnx" -c Release
   ```
   Confirm 0 warnings and 0 errors.

2. Run the bridge unit test suite:
   ```powershell
   dotnet run --project "HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj"
   dotnet run --project "HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj" -c Release
   ```
   Confirm 137 tests pass, 0 failed, 0 skipped.

3. Inspect modified source files:
   - `HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialThemeBridge.cs` (line 39)
   - `HPRobot/HPRobot.McpBridge/Host/RobotDispatcher.cs` (lines 57-60 and 84-87)
