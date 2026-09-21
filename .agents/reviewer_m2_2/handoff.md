# Review & Adversarial Challenge Report — Milestone M2 (HPRobot McpBridge)

**Reviewer:** `reviewer_m2_2` (M2 UI and Architecture Reviewer)  
**Parent:** `orchestrator_7` (conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Verdict:** **`REQUEST_CHANGES`**  
**Date:** 2026-09-21  

---

## 1. Observation

### 1.1 Independent Build Verification
- Executed: `dotnet build HPRobot/HPRobot.slnx -c Debug`
  - Output:
    ```text
    HPRebar.Mcp.Contracts -> ...\netstandard2.0\HPRebar.Mcp.Contracts.dll
    HPRebar.Mcp.Contracts -> ...\net48\HPRebar.Mcp.Contracts.dll
    HPRebar.McpBridge.Core -> ...\net48\HPRebar.McpBridge.Core.dll
    HPRebar.McpBridge.Core -> ...\net8.0\HPRebar.McpBridge.Core.dll
    HPRebar.Mcp.Server.Core -> ...\net10.0\HPRebar.Mcp.Server.Core.dll
    HPRobot.McpBridge -> ...\HPRobot.McpBridge\bin\Debug\net8.0-windows\HPRobot.McpBridge.dll

    Build succeeded.
        0 Warning(s)
        0 Error(s)
    Time Elapsed 00:00:02.87
    ```
- Executed: `dotnet build HPRobot/HPRobot.slnx -c Release`
  - Output: `Build succeeded. 0 Warning(s), 0 Error(s)`.
- Executed: `dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`
  - Output: `Passed! total: 613, failed: 0, succeeded: 613, skipped: 0`.
- Executed: `dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
  - Output: `Passed! total: 72, failed: 0, succeeded: 72, skipped: 0`.

### 1.2 Architectural Isolation & Dependency Verification
- Inspected: `HPRobot/HPRobot.slnx`, `HPRobot/Directory.Build.props`, and `HPRobot/HPRobot.McpBridge/HPRobot.McpBridge.csproj`.
- `HPRobot/HPRobot.McpBridge/HPRobot.McpBridge.csproj` lines 40-42:
  ```xml
  <ItemGroup>
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
  </ItemGroup>
  ```
- Grep scan across `HPRobot/` for sibling names (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`, `HPExcel`) returned zero cross-project references.

### 1.3 MaterialDesign 5.3.2 Theming & Resource Lookup
- Inspected: `HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialThemeBridge.cs`, lines 39-51:
  ```csharp
  public static void Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null)
  {
      using var reflectionScope = AssemblyLoadContext.GetLoadContext(typeof(MaterialThemeBridge).Assembly)?.EnterContextualReflection();
      if (icon is not null) window.Icon = icon(dark);

      var palette = new ResourceDictionary { Source = new Uri(dark ? DarkUri : LightUri) };
      var overlay = new ResourceDictionary { [OverlayMarker] = true };
      overlay.MergedDictionaries.Add(palette);

      if (FindThemeDictionary(window.Resources) is { } seed)
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

      var merged = window.Resources.MergedDictionaries;
      var existing = IndexOfOverlay(merged);
      if (existing >= 0) merged[existing] = overlay;
      else merged.Add(overlay);
  }
  ```
- Inspected: `HPRobot/HPRobot.McpBridge/App.xaml`, lines 5-13:
  ```xml
  <Application.Resources>
      <ResourceDictionary>
          <ResourceDictionary.MergedDictionaries>
              <ResourceDictionary Source="/HPRobot.McpBridge;component/Resources/Themes/MaterialBridge.xaml"/>
              <ResourceDictionary Source="/HPRobot.McpBridge;component/Resources/Themes/ThemeLight.xaml"/>
              <ResourceDictionary Source="/HPRobot.McpBridge;component/Resources/Themes/RobotTheme.xaml"/>
          </ResourceDictionary.MergedDictionaries>
      </ResourceDictionary>
  </Application.Resources>
  ```
- Inspected: `HPRobot/HPRobot.McpBridge/Views/MainWindow.xaml`, lines 18-41:
  - `<Window.Resources>` contains ONLY `<Style x:Key="StatusDot" TargetType="Ellipse">`.
  - `MainWindow.xaml` does NOT merge `MaterialBridge.xaml`.

### 1.4 STA Threading and COM Apartment Dispatch in `RobotDispatcher`
- Inspected: `HPRobot/HPRobot.McpBridge/Host/RobotDispatcher.cs`, lines 53-74 & 76-94:
  ```csharp
  private Task<JsonRpcEnvelope?> HandleAttach(long id)
  {
      try
      {
          _executor.Attachment.Attach();
          var result = new
          {
              success = true,
              isAttached = _executor.Attachment.IsAttached,
              pid = _executor.Attachment.AttachedPid,
              version = _executor.Attachment.RobotVersion,
              activeModel = _executor.Attachment.ActiveModelFileName
          };

          return Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, result));
      }
      catch (Exception ex)
      {
          Log.Warning(ex, "Error handling robot.attach");
          return Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message));
      }
  }
  ```
- In contrast, inspected: `HPRobot/HPRobot.McpBridge/ViewModels/MainWindowViewModel.cs`, lines 126-140:
  ```csharp
  [RelayCommand]
  private async Task AttachAsync()
  {
      try
      {
          await _executor.StaWorker.RunOnControlLaneAsync("ui-attach", () =>
          {
              _attachment.Attach();
          });
      }
      catch (Exception ex)
      {
          Log.Warning(ex, "Attach button failed");
      }
  }
  ```

---

## 2. Logic Chain

### 2.1 Major Finding 1: Theming Dictionary Scoping Defect
1. `MaterialBridge.xaml` declares `<md:CustomColorTheme .../>`, which implements `IMaterialDesignThemeDictionary`.
2. `MaterialBridge.xaml` is merged into `Application.Current.Resources` in `App.xaml:8`.
3. `MainWindow.xaml` does not merge `MaterialBridge.xaml` into `Window.Resources`.
4. In `MaterialThemeBridge.cs:39`, `FindThemeDictionary(window.Resources)` is evaluated.
5. Because `window.Resources` contains only `StatusDot`, `FindThemeDictionary` traverses only `window.Resources` and returns `null`.
6. Therefore, `if (FindThemeDictionary(window.Resources) is { } seed)` evaluates to `false`.
7. Lines 41–50 (`seed.GetTheme()`, `Theme.Create(...)`, and `overlay.SetTheme(theme)`) are never executed.
8. As a result, MaterialDesign theme tokens (primary/secondary mid, cards background, tooltips, validation errors) are never merged into `overlay`, and dynamic OS theme switching between light and dark mode fails to update MaterialDesign controls.
9. Verified fix from sibling bridge implementations (`HPPowerBi` / `HPExcel`):
   ```csharp
   var seed = FindThemeDictionary(window.Resources)
              ?? (Application.Current is not null ? FindThemeDictionary(Application.Current.Resources) : null);
   ```

### 2.2 Major Finding 2: COM Apartment Boundary Violation in `RobotDispatcher`
1. `McpBridgeHost` invokes `RobotDispatcher.DispatchCustomAsync` when handling incoming named pipe JSON-RPC requests (`robot.attach`).
2. This invocation occurs on a thread-pool background thread (MTA apartment).
3. `HandleAttach` directly invokes `_executor.Attachment.Attach()`.
4. Inside `Attach()`, `Marshal2.GetActiveObject` retrieves the `Robot.Application` COM pointer and assigns it to `_robot`, followed by calling `RefreshContext()` which queries COM properties (`_robot.Project`, `Structure`, `Nodes.GetAll().Count`).
5. Because this occurs on an MTA thread-pool thread, the COM RCW is initialized within the MTA apartment.
6. When a script execution request (`robot.execute`) arrives later, `RobotBridgeExecutor.ExecuteAsync` executes on `_staWorker` (the dedicated STA thread).
7. Accessing an unmarshaled COM pointer created in an MTA apartment from an STA thread violates COM apartment rules and risks `RPC_E_WRONG_THREAD (0x8001010E)`.
8. In `MainWindowViewModel.cs:131`, UI attachment is correctly wrapped with `await _executor.StaWorker.RunOnControlLaneAsync(...)`. `RobotDispatcher.HandleAttach` (and `HandleDetach`) must follow the exact same STA dispatching pattern:
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
   ```

### 2.3 Integrity Assessment
- Checked for hardcoded test results, facade implementations, or bypassed logic.
- Found no evidence of cheating: `RobotAttachment`, `RobotStaWorker`, `ComInteropHelper`, `RobotUnitsPolicy`, `RobotSafetyGuard`, `RobotSnapshotManager`, and `RobotTierAnalyzer` are fully and genuinely implemented.

---

## 3. Caveats

- Milestone M2 does not include test projects (`HPRobot.McpBridge.Tests` and `HPRobot.Mcp.Server.Tests`), which are scheduled for Milestone M4.
- Live verification against an active `robot.exe` session requires an interactive desktop session; testing conducted here verifies compilation, AST classification, COM architecture, and engine regression suites.

---

## 4. Conclusion

**Verdict: `REQUEST_CHANGES`**

Milestone M2 delivers substantial, high-quality, host-isolated work, but contains two major defects that must be corrected before advancing to Milestone M3:
1. **Theming Scoping Defect**: `MaterialThemeBridge.cs:39` must inspect `Application.Current.Resources` when `window.Resources` does not yield `IMaterialDesignThemeDictionary`.
2. **STA Apartment Boundary Defect**: `RobotDispatcher.cs:53-94` must dispatch `Attach()` and `Detach()` to `_executor.StaWorker.RunOnControlLaneAsync` rather than invoking them synchronously on the thread-pool thread.

---

## 5. Verification Method

To verify the required fixes:
1. **Theming Verification**:
   Inspect `HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialThemeBridge.cs`. Verify that line 39 incorporates fallback to `Application.Current.Resources`:
   ```csharp
   var seed = FindThemeDictionary(window.Resources)
              ?? (Application.Current is not null ? FindThemeDictionary(Application.Current.Resources) : null);
   ```
2. **Dispatcher Threading Verification**:
   Inspect `HPRobot/HPRobot.McpBridge/Host/RobotDispatcher.cs`. Verify that `HandleAttach` and `HandleDetach` are `async` and await `_executor.StaWorker.RunOnControlLaneAsync(...)`.
3. **Build Verification**:
   ```powershell
   dotnet build "HPRobot/HPRobot.slnx" -c Debug
   dotnet build "HPRobot/HPRobot.slnx" -c Release
   ```
   Both builds must exit with code 0 and 0 warnings.
