# Changes Report — Milestone M2 Remediation

**Worker:** `worker_m2_2` (HPRobot M2 Remediation Worker)  
**Parent:** `orchestrator_7` (`b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Date:** 2026-09-21  

---

## 1. Summary of Changes

Milestone M2 Remediation resolved two architectural defects in `HPRobot.McpBridge` identified by `reviewer_m2_2`:
1. **Theming Dictionary Scoping Defect**: Fixed dictionary lookup in `MaterialThemeBridge.cs` to fall back to `Application.Current.Resources`.
2. **COM Apartment Boundary Defect**: Updated `HandleAttach` and `HandleDetach` in `RobotDispatcher.cs` to dispatch operations through the dedicated STA worker's control lane via `_executor.StaWorker.RunOnControlLaneAsync`.

---

## 2. Detailed Modifications

### 2.1 `HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialThemeBridge.cs`
- **Location:** Line 39-53 (`Apply` method).
- **Issue:** `FindThemeDictionary(window.Resources)` evaluated to `null` because `MainWindow.xaml` does not merge `MaterialBridge.xaml` in `Window.Resources` (it is merged in `App.xaml` at the `Application` level). As a consequence, `seed` was null, skipping the creation of custom palette theme tokens (`Theme.Create(...)`) and dynamic dark/light theme switching.
- **Fix Applied:**
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
- **Rationale:** Ensures fallback to application-level resources, maintaining alignment with the theme scoping architecture in sibling bridges (`HPExcel`, `HPPowerBi`).

---

### 2.2 `HPRobot/HPRobot.McpBridge/Host/RobotDispatcher.cs`
- **Location:** Lines 53-97 (`HandleAttach` and `HandleDetach`).
- **Issue:** Named pipe JSON-RPC requests are dispatched on threadpool threads (MTA apartment). Invoking `_executor.Attachment.Attach()` or `_executor.Attachment.Detach()` directly on MTA threads creates COM RCWs within MTA, which subsequently triggers `RPC_E_WRONG_THREAD` (0x8001010E) when accessed by `RobotBridgeExecutor` running on the STA thread.
- **Fix Applied:**
  ```csharp
  private async Task<JsonRpcEnvelope?> HandleAttach(long id)
  {
      try
      {
          await _executor.StaWorker.RunOnControlLaneAsync("pipe-attach", () =>
          {
              _executor.Attachment.Attach();
          }).ConfigureAwait(false);

          var result = new
          {
              success = true,
              isAttached = _executor.Attachment.IsAttached,
              pid = _executor.Attachment.AttachedPid,
              version = _executor.Attachment.RobotVersion,
              activeModel = _executor.Attachment.ActiveModelFileName
          };

          return JsonRpcEnvelope.Success(id, result);
      }
      catch (Exception ex)
      {
          Log.Warning(ex, "Error handling robot.attach");
          return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
      }
  }

  private async Task<JsonRpcEnvelope?> HandleDetach(long id)
  {
      try
      {
          await _executor.StaWorker.RunOnControlLaneAsync("pipe-detach", () =>
          {
              _executor.Attachment.Detach("Client requested detach");
          }).ConfigureAwait(false);

          var result = new
          {
              success = true,
              isAttached = false
          };

          return JsonRpcEnvelope.Success(id, result);
      }
      catch (Exception ex)
      {
          Log.Warning(ex, "Error handling robot.detach");
          return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
      }
  }
  ```
- **Rationale:** Offloads COM attachment lifecycle operations onto `RobotStaWorker`'s high-priority control lane (`RunOnControlLaneAsync`), guaranteeing that all COM interaction occurs strictly inside the designated STA apartment.

---

## 3. Build & Test Verification Results

### 3.1 Solution Builds
1. **Debug Build:**
   ```powershell
   dotnet build HPRobot/HPRobot.slnx -c Debug
   ```
   - **Result:** Succeeded. 0 Warnings, 0 Errors.
2. **Release Build:**
   ```powershell
   dotnet build HPRobot/HPRobot.slnx -c Release
   ```
   - **Result:** Succeeded. 0 Warnings, 0 Errors.

### 3.2 Test Suite Execution
1. **Bridge Tests (Debug):**
   ```powershell
   dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj
   ```
   - **Result:** Passed! total: 137, failed: 0, succeeded: 137, skipped: 0.
2. **Bridge Tests (Release):**
   ```powershell
   dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj -c Release
   ```
   - **Result:** Passed! total: 137, failed: 0, succeeded: 137, skipped: 0.
3. **McpShared Server Core Tests Regression Check:**
   ```powershell
   dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
   ```
   - **Result:** Passed! total: 613, failed: 0, succeeded: 613, skipped: 0.
4. **McpShared Net48 Tests Regression Check:**
   ```powershell
   dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
   ```
   - **Result:** Passed! total: 72, failed: 0, succeeded: 72, skipped: 0.
