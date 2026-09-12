# Phase 03 — Bridge Status Window (WPF, modeless)

## Context Links
- [architecture.md §1 UI node, §7 lifecycle bridge](architecture.md) · [ADR-04 lớp 1 opt-in, bổ sung UX "script cuối + Undo hint"](adr/adr-04-execute-code-security-model.md) · [ADR-03 cache & "Restart bridge" >500 compile](adr/adr-03-roslyn-in-process-execution.md)
- Phase 2: `ViewModel/IMcpBridgeRunner.cs`, `Service/McpBridgeHost.Current`, `Model/BridgeStatus.cs`, `Model/LastRunInfo.cs`
- Verified pattern: `HPRebar/HPRebar/ColumnRebar/ColumnRebarCommand.cs:25-38, 100-114` (static `_window`, `Activate()`, `WindowInteropHelper.Owner`, `Show()`, `Closed`); `ColumnRebar/View/ColumnRebarView.xaml.cs:9-18` (code-behind = `InitializeComponent` + `DataContext` + `ThemeSwitcher` + `CloseRequested += Close`); `ColumnRebar/Service/ThemeSwitcher.cs:14-15, 68-73` (pack URI hardcode `HPRebar;component`, `UIThemeManager` gate)
- Verified: `HPRebar/HPRebar/Resources/Themes/Theme.xaml:7-14` là file duy nhất chứa `pack://` (7 chỗ); 8 file còn lại (`ThemeDark/Light`, `Typography`, `Spacing`, `Buttons`, `TextBoxes`, `Controls`, `ThemeIcons`) không có pack URI → link được
- `CLAUDE.md` §"Windows are modeless" · `.claude/rules/development-rules.md` §WPF/MVVM
- Skills: `/bs:revit-wpf-mvvm`, `/bs:revit-xaml-styles`

## Overview
- **Status (thực tế 2026-09-12):** built + verified trong Revit 2026 — cửa sổ modeless mở từ ribbon, checkbox opt-in bật thật (log "code execution enabled"), stop/start listener từ cửa sổ. Theme link 8 file từ HPRebar (resource key `resources/themes/*.baml` verified), `Theme.xaml` + `ThemeSwitcher` bản bridge. Review: 2🟡 (quote path explorer, try/catch) đã sửa. Chưa kiểm dark/light swap bằng mắt.
- **Priority:** P2 · **Status:** pending · **Effort:** 6h
- Một cửa sổ modeless `McpBridgeStatusView`: bật/tắt listener, opt-in "Allow AI code execution" (OFF mặc định), trạng thái kết nối, script cuối + kết quả, đường dẫn audit log, "Restart bridge", Undo hint. Chạy song song phase 4; compile được ngay sau phase 2, F5 test sau phase 4.

## Key Insights
- Listener sống độc lập cửa sổ (có option auto-start, ADR §7) → **cửa sổ không sở hữu handler/ExternalEvent**; `Closed` chỉ `_window = null`. Khác `ColumnRebarCommand.cs:104-108` (dispose handler khi đóng) — chủ đích, vì host là process-level singleton, dispose ở `Application.OnShutdown` (phase 4).
- `StatusChanged` bắn từ pipe thread → ViewModel bắt `Dispatcher.CurrentDispatcher` lúc khởi tạo (command chạy trên Revit UI thread = STA có Dispatcher) và `InvokeAsync` khi cập nhật property. Không đưa logic vào code-behind.
- Theme: **quyết định** — link 8 file XAML không pack URI từ `HPRebar/HPRebar/Resources/Themes/` bằng `<Page Include="..\HPRebar\Resources\Themes\*.xaml" Exclude="..\HPRebar\Resources\Themes\Theme.xaml" Link="Resources\Themes\%(Filename)%(Extension)"/>`; bridge có `Theme.xaml` riêng (merge cùng 7 dictionary nhưng URI `HPRebar.McpBridge;component`) và `Service/ThemeSwitcher.cs` riêng (~70 dòng, URI bridge). Lý do không reference `HPRebar.csproj`: kéo cả add-in rebar + ILRepack. Phương án lâu dài `HPRebar.Ui` shared lib — YAGNI v1, ghi Next Steps. `[DECISION — confirm với user nếu không muốn duplicate ThemeSwitcher]`.
- Không `DialogResult` (throw trên modeless); VM `event Action? CloseRequested`.
- VM < 250 dòng, XAML < 500 dòng; mọi màu/spacing `{DynamicResource Brush.*|Spacing.*}`; kiểm bằng grep `#[0-9A-Fa-f]{6}` trong XAML view = 0.

## Requirements
**Functional**
- Toggle listener (Start/Stop) phản ánh `BridgeStatus` (Stopped/Listening/Connected/Busy/Error + message).
- Checkbox "Allow AI code execution" bind `ExecutionEnabled`; mặc định OFF; tooltip nêu rủi ro (ADR-04 residual risk).
- Hiển thị `PipeName`, `LastRun` (label, thời điểm, transaction mode, dryRun, ok/error/timeout, duration, changed counts, 20 dòng đầu source), `CompiledScriptCount` (cảnh báo màu khi >500), `AuditDirectory` + nút mở folder, nút "Copy last script".
- "Restart bridge" = `RestartAsync()` (stop listener, dispose pipe, start lại; không xoá cache Roslyn — assembly không unload, ADR-03 → hint "restart Revit để giải phóng").
- Undo hint text: "Mỗi lần chạy = 1 nhóm Undo `MCP: <label>` — Ctrl+Z trong Revit".
- Nút Close → `CloseRequested`.
**Non-functional**
- Dark/Light theo Revit (`ThemeSwitcher.ApplyFromRevit`); click ribbon lần 2 → `Activate()`.

## Architecture
```
McpBridgeCommand (ExternalCommand, static _window)
  └─ McpBridgeHost.Current (IMcpBridgeRunner) ──► McpBridgeStatusViewModel : ObservableObject
                                                    [ObservableProperty] IsListening, IsExecutionEnabled, StatusText, StatusKind,
                                                       PipeName, LastRunSummary, LastScriptPreview, CompiledScriptCount, AuditDirectory, IsBusy
                                                    [RelayCommand] ToggleListenerAsync, RestartBridgeAsync, OpenAuditFolder, CopyLastScript, Close
                                                    event CloseRequested; subscribes runner.StatusChanged → Dispatcher.InvokeAsync(Refresh)
  └─ McpBridgeStatusView : Window  (code-behind: InitializeComponent; DataContext = vm; ThemeSwitcher.ApplyFromRevit(this); vm.CloseRequested += Close)
XAML: x:Class="HPRebar.McpBridge.View.McpBridgeStatusView", xmlns:vm="clr-namespace:HPRebar.McpBridge.ViewModel", MergedDictionaries → pack://application:,,,/HPRebar.McpBridge;component/Resources/Themes/Theme.xaml
```
Data flow: user click → `[RelayCommand]` → `IMcpBridgeRunner` (pipe thread work) → `StatusChanged` → VM refresh (UI thread) → bindings. Không có Revit API call nào từ VM.

## Related Code Files
**To create (proposed)**
- `HPRebar/HPRebar.McpBridge/View/McpBridgeStatusView.xaml`, `View/McpBridgeStatusView.xaml.cs` (namespace `HPRebar.McpBridge.View`)
- `HPRebar/HPRebar.McpBridge/ViewModel/McpBridgeStatusViewModel.cs` (`sealed partial class … : ObservableObject`)
- `HPRebar/HPRebar.McpBridge/Service/ThemeSwitcher.cs` (bản bridge, URI `HPRebar.McpBridge;component`)
- `HPRebar/HPRebar.McpBridge/Resources/Themes/Theme.xaml` (merge 7 dictionary link)
**To modify (proposed)**
- `HPRebar/HPRebar.McpBridge/McpBridgeCommand.cs` — thay stub phase 0 bằng mở cửa sổ
- `HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj` — `<Page Include … Link>` 8 file theme (phase 4 không sửa csproj → không xung đột)
**To delete:** none (template Views/ViewModels đã prune phase 0)

## Implementation Steps
1. Activate `/bs:revit-wpf-mvvm` + `/bs:revit-xaml-styles` — đọc token names hiện có: `grep -o 'x:Key="[^"]*"' HPRebar/HPRebar/Resources/Themes/ThemeDark.xaml Spacing.xaml Typography.xaml Buttons.xaml` để dùng đúng `Brush.*`, `Spacing.*`, style key nút/checkbox.
2. csproj: thêm ItemGroup `<Page Include="..\HPRebar\Resources\Themes\*.xaml" Exclude="..\HPRebar\Resources\Themes\Theme.xaml" Link="Resources\Themes\%(Filename)%(Extension)"/>`. `[VERIFY]` Nice3point SDK không auto-include `**/*.xaml` gây duplicate (nếu có → `EnableDefaultPageItems=false` chỉ cho pattern này hoặc liệt kê 8 file tường minh).
3. `Resources/Themes/Theme.xaml` bridge: copy `HPRebar/HPRebar/Resources/Themes/Theme.xaml`, đổi 7 URI sang `HPRebar.McpBridge;component/Resources/Themes/…`. Giữ thứ tự merge (ThemeDark trước).
4. `Service/ThemeSwitcher.cs` bridge: copy `ColumnRebar/Service/ThemeSwitcher.cs`, đổi namespace `HPRebar.McpBridge.Service`, đổi 2 URI. Giữ `#if REVIT2024_OR_GREATER` + `// Multi-version:` (R25/R26 luôn true nhưng giữ để đồng bộ nguồn copy).
5. `ViewModel/McpBridgeStatusViewModel.cs`: ctor `(IMcpBridgeRunner runner)`; `_dispatcher = Dispatcher.CurrentDispatcher`; `runner.StatusChanged += _ => _dispatcher.InvokeAsync(Refresh)`; `Refresh()` copy các giá trị từ runner sang `[ObservableProperty]`; `partial void OnIsExecutionEnabledChanged(bool value) => runner.ExecutionEnabled = value;` `[RelayCommand(CanExecute = nameof(CanToggle))] async Task ToggleListenerAsync()` (`IsBusy` guard, try/catch → `StatusText`); `RestartBridgeAsync`; `OpenAuditFolder` (`Process.Start("explorer.exe", dir)` — chỉ mở folder, không mở file); `CopyLastScript` (`Clipboard.SetText`); `Close() => CloseRequested?.Invoke()`. Khi runner status Busy → disable Restart.
6. `View/McpBridgeStatusView.xaml`: `Window` `Width=520 Height=560 WindowStartupLocation=CenterOwner ResizeMode=CanResize`, `Background="{DynamicResource Brush.Window.Background}"` (tên thật từ bước 1). Layout Grid 4 hàng: (a) header status dot + text + toggle button; (b) GroupBox "Safety": CheckBox opt-in + Undo hint TextBlock + RequireLocalApproval (ẩn v1 — không render, YAGNI); (c) GroupBox "Last run": TextBox read-only monospace 20 dòng preview + summary + Copy; (d) footer: CompiledScriptCount, AuditDirectory (TextBlock + nút Open), Restart, Close. Mọi Margin/Padding qua `{DynamicResource Spacing.*}`; font qua `Typography.xaml` keys.
7. `View/McpBridgeStatusView.xaml.cs`: đúng 4 câu lệnh như `ColumnRebarView.xaml.cs:11-17`.
8. `McpBridgeCommand.cs`: `private static McpBridgeStatusView? _window;` `Execute()`: `_window?.Activate(); return;` nếu có; `McpBridgeHost.Current is null` → `TaskDialog.Show("MCP Bridge", "Bridge did not initialise; check %LocalAppData%\HPRebar\McpBridge\logs.")`; else `new McpBridgeStatusView(new McpBridgeStatusViewModel(host))`, `new WindowInteropHelper(view).Owner = Application.MainWindowHandle` (`Application` = `ExternalCommand.Application` UIApplication, comment như `ColumnRebarCommand.cs:31`), `view.Closed += (_, _) => _window = null;`, `_window = view; view.Show();`. Không dispose host.
9. Build gate `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`. XAML lint: `grep -nE '#[0-9A-Fa-f]{6}|StaticResource' HPRebar/HPRebar.McpBridge/View/*.xaml` → 0 dòng.
10. Sau phase 4 xong: F5 (`/bs:revit-debug`) → ribbon "MCP Bridge" → cửa sổ mở, đổi theme Revit Dark/Light → màu đổi không mở lại cửa sổ; click ribbon lần 2 → focus.

## Todo List
- [ ] csproj `<Page Link>` 8 theme + verify không duplicate
- [ ] `Resources/Themes/Theme.xaml` bridge + `Service/ThemeSwitcher.cs` bridge
- [ ] `McpBridgeStatusViewModel` (< 250 dòng) với dispatcher marshal
- [ ] `McpBridgeStatusView.xaml(.cs)` toàn DynamicResource
- [ ] `McpBridgeCommand` static window + Activate + Owner + Show
- [ ] Build gate + XAML grep + (sau phase 4) F5 smoke theme swap

## Success Criteria
- `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` → exit 0.
- `grep -rnE '#[0-9A-Fa-f]{3,8}"|StaticResource' HPRebar/HPRebar.McpBridge/View/` → 0 kết quả.
- `grep -n "DialogResult\|DataContext=" HPRebar/HPRebar.McpBridge/View/*.xaml` → 0; `View/McpBridgeStatusView.xaml.cs` ≤ 20 dòng.
- `wc -l ViewModel/McpBridgeStatusViewModel.cs` < 250.
- F5 (sau phase 4): cửa sổ mở modeless — Revit vẫn pan/zoom được khi cửa sổ mở; checkbox opt-in mặc định unchecked mỗi lần mở Revit; toggle listener → status "Listening on hprebar-mcp-r2026".

## Risk Assessment
| Risk | L×I | Mitigation |
|---|---|---|
| Linked XAML `Page` bị Nice3point/WPF SDK include trùng → MC lỗi duplicate | M×M | Bước 2 verify; fallback liệt kê 8 `<Page Include>` tường minh |
| Token key trong theme HPRebar đổi tên → bridge vỡ binding runtime (không lỗi compile) | M×M | Cùng nguồn file (link) nên đổi tên đồng bộ; F5 smoke |
| `StatusChanged` trước khi Dispatcher sẵn sàng / sau khi cửa sổ đóng | M×L | Unsubscribe trong `Closed` (qua VM `Detach()` gọi từ command `Closed` lambda — vẫn không logic trong code-behind) |
| Duplicate `ThemeSwitcher` drift giữa 2 add-in | M×L | Comment nguồn gốc; Next Steps `HPRebar.Ui` |

## Security Considerations
- Opt-in state chỉ trong bộ nhớ, reset mỗi lần Revit khởi động (ADR-04 lớp 1) — VM không persist.
- Preview script cuối hiển thị nguồn AI gửi → user thấy chính xác gì đã chạy; không hiển thị đường dẫn tài liệu hash.
- `OpenAuditFolder` chỉ `explorer.exe <dir>` cố định, không nhận input ngoài.

## Next Steps
- Phase 5: F5 smoke checklist + Dynamo coexistence dùng cửa sổ này để bật listener.
- Backlog: `HPRebar.Ui` shared theme lib để bỏ duplicate `ThemeSwitcher`/`Theme.xaml`; tuỳ chọn `RequireLocalApproval` UI (ADR-04 alt 3).
