# Code Standards — Revit Add-In Project

> Stabs: C# / .NET 8 (R25+) hoặc .NET Framework 4.8 (R22–R24) / Revit API 2022–2027 / WPF + CommunityToolkit.Mvvm / Nice3point.Revit.Sdk

## 1. File Naming

| Loại file | Convention | Ví dụ |
|---|---|---|
| `.cs` | PascalCase | `WallReportViewModel.cs`, `StartupCommand.cs` |
| `.xaml` | PascalCase | `WallReportView.xaml`, `Theme.xaml`, `ThemeDark.xaml` |
| `.csproj` | PascalCase match folder | `MyAddIn.csproj` |
| `.addin` manifest | PascalCase match assembly | `MyAddIn.addin` |
| `.md`, `.txt`, plain config | kebab-case | `code-standards.md`, `multi-version-strategy.md` |
| `.sh`, `.js`, `.py` | kebab-case | `build-release.sh` |
| `appsettings.json`, `launchSettings.json` | camelCase | (.NET convention) |

## 2. File Size

| Loại | Soft limit | Action khi vượt |
|---|---|---|
| ViewModel (`.cs`) | 250 dòng | Tách Service layer |
| Service (`.cs`) | 300 dòng | Tách thành multiple service theo concern |
| View code-behind (`.xaml.cs`) | 50 dòng | Move logic vào ViewModel |
| XAML (`.xaml`) | 500 dòng | Tách `UserControl` riêng |
| Markdown / config | No limit | — |

## 3. C# / .NET

- `nullable enable` ở project level.
- `sealed class` cho mọi ViewModel/Service nếu không extend.
- `record` cho DTO/Message immutable.
- `using` declaration thay blobs:
  ```csharp
  using var transaction = doc.NewTransaction("Update wall");
  transaction.Start();
  // ...
  transaction.Commit();
  ```
- `async Task` cho async method, `async void` chỉ cho event handler.
- File-scoped namespace: `namespace MyAddIn.ViewModels;`
- LINQ over loop khi readable.

## 4. WPF + MVVM (CommunityToolkit.Mvvm)

**MUST:**
- `sealed partial class XxxViewModel : ObservableObject`
- `[ObservableProperty]` trên private field
- `[RelayCommand]` trên method
- `[NotifyCanExecuteChangedFor(nameof(XxxCommand))]` khi cần
- Constructor inject `ILogger<T>` + services
- Long-running command: nhận `CancellationToken`, guard `IsBusy`
- Code-behind chỉ `InitializeComponent()` + `DataContext = vm`

**DON'T:**
- ❌ Tự viết `INotifyPropertyChanged` / `RelayCommand`
- ❌ Set `DataContext` trong XAML
- ❌ Logic business trong code-behind
- ❌ Truy cập control bằng `x:Name` từ code-behind

Reference: skill `revit-wpf-mvvm` (SKILL.md + references/mvvm-toolkit-patterns.md + wpf-do-dont.md).

## 5. XAML Styles & Material Design (MANDATORY)

> **QUY TẮC BẮT BUỘC:** Mọi add-in hoặc công cụ mới có giao diện WPF (Revit, AutoCAD, Civil 3D, standalone bridge...) **bắt buộc 100% sử dụng `MaterialDesignInXamlToolkit`** (v5.2.1 hoặc v5.3.2).

**MUST:**
- Merge `MaterialBridge.xaml` (chứa `md:CustomColorTheme` + `MaterialDesign2.Defaults.xaml`) đầu tiên trong `Theme.xaml`.
- **Font**: Bắt buộc ghi đè `<FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>` (tránh Pack URI của Roboto gây crash sau khi ILRepack merge).
- **Theme switch**: Mọi color/brush dùng `{DynamicResource Brush.X}` hoặc `{DynamicResource ...}` để tự động đồng bộ Dark/Light theo Host thông qua `MaterialThemeBridge`.
- **Containers**: Dùng `md:Card` hoặc `Card.Panel` phân tầng độ cao (elevation) thay vì Border trần.
- **Input Controls**: Dạng Outlined (`MaterialDesignOutlinedTextBox`, `MaterialDesignOutlinedComboBox`) kèm `md:HintAssist.Hint` nổi (floating hint).
- **Icons**: Dùng `md:PackIcon` nhất quán (16x16 / 20x20 / 24x24).
- **Buttons**: Phân cấp rõ ràng — Raised Primary Button cho hành động chính, Outlined Secondary Button cho hành động phụ, IconButton cho thao tác nhanh.
- **Packaging (ILRepack)**: Cấu hình `<IsRepackable>true</IsRepackable>` (Nice3point SDK) hoặc MSBuild target merge `MaterialDesignThemes.Wpf.dll` & `MaterialDesignColors.dll` (AutoCAD/Standalone) để không bị thiếu DLL runtime.
- Mọi spacing dùng `{DynamicResource Spacing.X}` (bội số của 4: 4/8/16/24/32).

**DON'T:**
- ❌ Dùng font Roboto mặc định của MaterialDesign (`{md:MaterialDesignFont}`) trong XAML.
- ❌ Hardcode màu tĩnh `Background="#1E1E1E"` → dùng `{DynamicResource Brush.Background}`.
- ❌ Dùng `{StaticResource ...}` cho color/brush (gây mất khả năng đổi theme runtime).
- ❌ Implicit style `<Style TargetType="Button">` global mà không base trên MaterialDesign (gây xung đột với theme host).
- ❌ Để sót DLL MaterialDesign ngoài thư mục output khi host chạy in-process.

Reference: skill `revit-xaml-styles` (SKILL.md + references/boilerplate/*).

## 6. Revit API

**MUST:**
- `[Transaction(TransactionMode.Manual)]` trên ExternalCommand
- Mọi document modify wrap `using var t = doc.NewTransaction(...)` + `t.Start()` + `t.Commit()`
- Modeless UI: `ExternalEvent` để gọi Revit API từ ViewModel
- Multi-version code: `#if REVIT<XX>_OR_GREATER` + comment `// Multi-version: <topic>`
- ElementId access đúng version:
  ```csharp
  #if REVIT2024_OR_GREATER
      long id = elementId.Value;
  #else
      int id = elementId.IntegerValue;
  #endif
  ```

Reference: skill `revit-addin` (SKILL.md + references/multi-version-strategy.md + nice3point-toolkit.md).

## 7. Project Configuration (`.csproj`)

**MUST:**
```xml
<Project Sdk="Nice3point.Revit.Sdk">
  <PropertyGroup>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <DeployAddin>true</DeployAddin>
    <LaunchRevit>true</LaunchRevit>
    <IsRepackable>true</IsRepackable>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Configurations>Debug.R22;Debug.R23;Debug.R24;Debug.R25;Debug.R26;Debug.R27</Configurations>
    <Configurations>$(Configurations);Release.R22;Release.R23;Release.R24;Release.R25;Release.R26;Release.R27</Configurations>
  </PropertyGroup>
</Project>
```

## 8. Test

Reference: skill `revit-test` (SKILL.md + references/test-setup-rider.md + projects-to-track.md).

| Loại | Framework | Khi nào |
|---|---|---|
| Pure logic (không cần Revit) | xUnit | Calculator, parser, geometry math |
| In-process (cần Revit context) | TUnit (Nice3point) | Test với `Document`, `Transaction`, `Element` |
| Alternative in-process | ricaun-io RevitTest (NUnit) | Khi cần VS Test Adapter UI |

**MUST:** Tách layer pure logic ra khỏi Revit API để test xUnit. Không mock `Document` (sealed).

## 9. Commits + Git

- Conventional commits: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`
- Không AI references trong commit messages.
- KHÔNG commit `.env`, `appsettings.local.json`, API keys.
- KHÔNG commit `bin/`, `obj/`, `*.user`, `.vs/`.

## 10. Logging (Serilog)

- Log path: `%LocalAppData%\<AddinName>\logs\addin-YYYY-MM-DD.log`
- Daily rolling, retain 7 days.
- Format: `[{Timestamp:HH:mm:ss.fff} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}`
- Setup trong `Configuration/LoggerConfiguration.cs` (Nice3point template tạo sẵn).

## 11. References

| Topic | Location |
|---|---|
| MVVM patterns | `.claude/skills/revit-wpf-mvvm/` |
| XAML styles | `.claude/skills/revit-xaml-styles/` |
| Scaffold + Nice3point | `.claude/skills/revit-addin/` |
| F5 debug + troubleshoot | `.claude/skills/revit-debug/` |
| Test setup | `.claude/skills/revit-test/` |
| Multi-version compat | `.claude/skills/revit-addin/references/multi-version-strategy.md` |
| Nice3point templates source | https://github.com/Nice3point/RevitTemplates |
| Revit API docs | https://www.revitapidocs.com/2027/ |
| CommunityToolkit.Mvvm | https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/ |

## 12. AutoCAD Feature-Folder Architecture & Standards (`HPAutoCad`)

Following the unified `HPAutoCad` architecture (established with `HPAutoCad.HPGeoLink`, retiring legacy standalone `HPGeo`):

**MUST:**
- **Feature Folder Convention:** Every new AutoCAD tool or feature MUST be created inside `HPAutoCad/` as a dedicated feature folder:
  - `HPAutoCad.Core/<ToolName>/`: Pure domain algorithms, models, calculators, and parsers (.NET 8.0, ZERO references to AutoCAD APIs, 100% testable via xUnit).
  - `HPAutoCad/<ToolName>/`: UI, Commands, ViewModels, Views, and CAD readers/writers (.NET 8.0-windows, WPF + CommunityToolkit.Mvvm).
  - `HPAutoCad.Tests/<ToolName>/`: Unit tests covering domain logic and ViewModel interactions.
- **ALC Isolation:** New commands and views must be loaded through `HPAutoCad.Loader` in an isolated `AssemblyLoadContext` (`AppLoadContext`), never directly polluting AutoCAD's Default ALC.
- **Theme Consistency:** UI views must merge `Resources/Themes/Theme.xaml` via `{DynamicResource ...}`, re-basing onto MaterialDesignThemes keys and using `MaterialThemeBridge.Attach` to track `COLORTHEME` runtime changes.
- **Closed-Loop Verification:** Never declare an AutoCAD tool implementation or bug fix complete without verifying live in AutoCAD 2026 via the unattended MCP harness (`run-geolink-verify.ps1` or host-specific harness).
