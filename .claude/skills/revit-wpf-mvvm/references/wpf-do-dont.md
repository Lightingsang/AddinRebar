# WPF Do/Don't — Revit Add-In

## XAML Layout

| ✅ DO | ❌ DON'T |
|---|---|
| `Grid` với `RowDefinitions`/`ColumnDefinitions` + `*`/`Auto` | `Margin="50,30,20,10"` hack vị trí |
| `StackPanel` cho hàng đơn giản (≤ 4 child) | Nested 5+ levels StackPanel |
| `DockPanel` cho layout tool window | `Canvas` (trừ khi vẽ overlay) |
| Spacing = multiples of 4 (`4`, `8`, `16`, `24`, `32`) | `Margin="7,3,5,2"` random |
| `MinWidth`/`MinHeight` cho responsive | Hardcode `Width="487"` |

## Binding

| ✅ DO | ❌ DON'T |
|---|---|
| `{Binding PropertyName}` (default OneWay/TwoWay) | `Source={x:Static ...}` cho data app |
| `UpdateSourceTrigger=PropertyChanged` cho TextBox cần realtime | Để default `LostFocus` khi cần live filter |
| `Mode=OneTime` cho data tĩnh | TwoWay khi không cần (waste cycle) |
| `Converter={StaticResource BoolToVisibilityConverter}` | Convert trong code-behind |
| `IsAsync=True` cho heavy property | Block UI thread |
| `FallbackValue=""` cho safety | Crash khi DataContext null |

## DataContext

| ✅ DO | ❌ DON'T |
|---|---|
| Set `DataContext` trong code-behind constructor (DI inject ViewModel) | `<Window.DataContext><vm:XxxViewModel/></Window.DataContext>` |
| `d:DataContext="{d:DesignInstance vm:XxxViewModel}"` cho IntelliSense design-time | Không set design-time → mất autocomplete XAML |

## Window / Modal

| ✅ DO | ❌ DON'T |
|---|---|
| `WindowStartupLocation="CenterOwner"` | `Manual` không set position |
| `WindowInteropHelper.Owner = UiApplication.MainWindowHandle` | Show window standalone (lạc khỏi Revit) |
| `ShowInTaskbar="False"` cho modal | Hiện ngoài taskbar gây nhầm |
| `ResizeMode="CanResizeWithGrip"` cho window có grid/list | `NoResize` trừ khi nhỏ |
| Min size 400×300 trở lên | < 200×150 (không đọc được) |

## Resources

| ✅ DO | ❌ DON'T |
|---|---|
| `{DynamicResource Color.Background}` (theme-swappable) | `{StaticResource ...}` cho color/brush |
| `pabs://application:,,,/MyAddIn;component/Resources/Themes/Theme.xaml` | Relative path `../Themes/Theme.xaml` |
| Merge `Theme.xaml` ở `App.xaml` HOẶC từng `Window.Resources` | Merge ở nhiều cấp (xung đột) |

## Code-behind

| ✅ DO | ❌ DON'T |
|---|---|
| `InitializeComponent()` + `DataContext = vm` | Logic business trong code-behind |
| Subscribe `Loaded` để init async | Init trong constructor (chưa rendered) |
| Override `OnClosing` để confirm save | Dùng `Closing` event handler trong code-behind nếu logic phức tạp |
| `(DataContext as IDisposable)?.Dispose()` khi Window đóng | Leak event handlers |

## Threading

| ✅ DO | ❌ DON'T |
|---|---|
| `async Task` + `await` từ UI thread | `Task.Wait()` / `.Result` (deadlock) |
| `Dispatcher.InvokeAsync(() => ...)` khi update UI từ thread khác | Truy cập control từ background thread |
| Revit API call qua `ExternalEvent` (modeless) | Gọi `Document.NewTransaction` từ ViewModel trực tiếp (modeless) |
| `CancellationToken` cho mọi async > 1s | Long-running không cancel |

## Performance

| ✅ DO | ❌ DON'T |
|---|---|
| `VirtualizingStackPanel.IsVirtualizing="True"` cho DataGrid/ListBox lớn | Render full 10k items |
| `ItemsSource="{Binding Items}"` (ObservableCollection) | `Items.Add()` thủ công từ code-behind |
| `Freezable` (Brush, Geometry) → `Freeze()` | Để mutable (recreate liên tục) |
| `BindingMode=OneTime` cho dữ liệu không đổi | Default Two-Way mode lãng phí |

## Revit-specific

| ✅ DO | ❌ DON'T |
|---|---|
| Modal `ShowDialog` cho dialog ngắn (< 30s task) | Modal cho task dài (block Revit, UX tệ) |
| Modeless `Show` + `ExternalEvent` cho panel/picker | Modeless không có ExternalEvent (crash) |
| Đóng window trước khi finish transaction | Để window mở khi user cần làm tiếp Revit |
| Test UI ở cả Revit dark + light theme | Chỉ test 1 theme |
