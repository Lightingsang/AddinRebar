# Development Rules

**IMPORTANT:** Analyze the skills catalog and activate the skills that are needed for the task during the process.
**IMPORTANT:** You ALWAYS follow these principles: **YAGNI (You Aren't Gonna Need It) - KISS (Keep It Simple, Stupid) - DRY (Don't Repeat Yourself)**

## General
- **File Naming convention by language:**
  - **C# (`.cs`)** — PascalCase (`WallReportViewModel.cs`, `StartupCommand.cs`).
  - **XAML (`.xaml`)** — PascalCase (`WallReportView.xaml`, `Theme.xaml`, `ThemeDark.xaml`).
  - **Markdown / plain text / config** — kebab-case (`code-standards.md`, `multi-version-strategy.md`).
  - **Shell / JS / Python (`.sh`/`.js`/`.py`)** — kebab-case (`build-release.sh`, `extract-revit-versions.py`).
  - **.NET config (`launchSettings.json`, `appsettings.json`)** — camelCase (theo convention .NET).
  - LLM tools (Grep/Glob) ưu tiên tên dài descriptive hơn là tên ngắn cryptic. KHÔNG dùng `vm.cs` → dùng `WallReportViewModel.cs`.
- **File Size Management:**
  - C# code file < 300 dòng (riêng WPF code-behind có thể dài hơn 200 nếu chỉ chứa `InitializeComponent` + DataContext set).
  - XAML file < 500 dòng — tách `UserControl` riêng nếu lớn.
  - ViewModel < 250 dòng — tách Service nếu chứa business logic.
  - Markdown/config: không giới hạn.
- When looking for docs, activate `docs-seeker` skill (`context7` reference). Cho Revit/Nice3point, ưu tiên xem GitHub repo gốc và `RevitTemplates-Huong-Dan-Tieng-Viet.md` trong root project.
- Use `gh` bash command để interact GitHub.
- Use `dotnet` CLI (build/test/restore) cho mọi .NET operation. **KHÔNG dùng** `msbuild.exe` trực tiếp trừ khi cần feature Visual Studio MSBuild-specific.
- Use `sequential-thinking` và `/bs:debug` cho phân tích complex logic.
- **[IMPORTANT]** Follow codebase structure + code standards trong `./docs`.
- **[IMPORTANT]** KHÔNG simulate/mock implementation — luôn implement real code.

## C# / .NET / WPF Specific Rules

### C# coding standards
- `nullable enable` ở project level (`.csproj`) — bắt buộc khai báo `string?` nếu nullable.
- `sealed class` cho mọi ViewModel/Service nếu không cần extend (perf + design clarity).
- `record` cho DTO/Message (immutable + value equality).
- `using` declaration thay cho `using { ... }` block (C# 8+):
  ```csharp
  using var transaction = doc.NewTransaction("Update wall");
  ```
- LINQ over loop khi readable (filter/map/reduce). Loop khi cần early exit hoặc side effect phức tạp.
- `async Task` cho mọi async method, `async void` CHỈ cho event handler.
- File-scoped namespace (`namespace MyAddIn.ViewModels;` không có `{}`).

### WPF / MVVM rules
- **MANDATORY UI**: Mọi add-in hoặc công cụ mới có giao diện WPF (Revit, AutoCAD, Civil 3D, standalone bridge...) **bắt buộc 100% sử dụng `MaterialDesignInXamlToolkit`** (v5.2.1 hoặc v5.3.2).
- **MUST** dùng `CommunityToolkit.Mvvm` — KHÔNG tự viết `INotifyPropertyChanged`/`RelayCommand`.
- `sealed partial class XxxViewModel : ObservableObject` — partial bắt buộc cho source generator.
- `[ObservableProperty]` trên private field, `[RelayCommand]` trên method.
- Code-behind chỉ chứa `InitializeComponent()` + `DataContext = viewModel` (DI).
- KHÔNG set `DataContext` trong XAML.
- Mọi style XAML dùng `{DynamicResource ...}` — không `StaticResource` cho color/brush (hỗ trợ đổi Dark/Light qua `MaterialThemeBridge`).
- **Font**: Bắt buộc override `<FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>` (cấm dùng font Roboto mặc định của toolkit để tránh lỗi resolve Pack URI sau ILRepack).
- **Control Layout**: Dùng `md:Card` / `Card.Panel` phân tầng (elevation); Input controls dùng Outlined với `md:HintAssist.Hint`; Icon dùng `md:PackIcon`; Phân cấp Button (Raised Primary, Outlined Secondary, IconButton).
- **Packaging**: Bắt buộc cấu hình `<IsRepackable>true</IsRepackable>` (Nice3point SDK) hoặc MSBuild target merge `MaterialDesignThemes.Wpf.dll` & `MaterialDesignColors.dll` (AutoCAD/Standalone) để không thiếu DLL lúc runtime.
- Modal: set `Owner = UiApplication.MainWindowHandle` qua `WindowInteropHelper` (Revit) hoặc `ShowModalWindow(MainWindow.Handle, window, false)` (AutoCAD).
- Modeless: dùng `ExternalEvent` để gọi Revit API từ ViewModel.

### Revit API rules
- `[Transaction(TransactionMode.Manual)]` cho mọi `ExternalCommand`.
- Wrap mọi document modification:
  ```csharp
  using var t = doc.NewTransaction("Action name");
  t.Start();
  // ... modify ...
  t.Commit();
  ```
- Multi-version code: dùng preprocessor `#if REVIT<XX>_OR_GREATER`, kèm comment `// Multi-version: <topic>` để grep.
- `FilteredElementCollector` chain methods — không materialize giữa chừng:
  ```csharp
  var walls = new FilteredElementCollector(doc)
      .OfClass(typeof(Wall))
      .WhereElementIsNotElementType()
      .Cast<Wall>()
      .ToList();
  ```
- Element ID:
  ```csharp
  #if REVIT2024_OR_GREATER
      long id = elementId.Value;
  #else
      int id = elementId.IntegerValue;
  #endif
  ```

### AutoCAD Add-In rules (HPAutoCad, HPGeo, mọi tool AutoCAD sau này)
- **Một tab ribbon chung `HPAutoCad` — KHÔNG tạo tab mới.** Tab Id `HPAUTOCAD_MCP_TAB`, title `HPAutoCad` (giá trị do `HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs` định nghĩa; giữ nguyên). Mỗi tool thêm **đúng một panel** của mình với Id riêng (`HPAUTOCAD_MCP_PANEL` "MCP", `HPGEO_VN2000_PANEL` "VN2000", …). Giao thức bắt buộc:
  1. `ComponentManager.Ribbon.FindTab(TabId)` → chưa có thì tạo (`Id`, `Title`, `IsVisible`), có rồi thì dùng — ai load trước tạo, ai sau nối vào.
  2. Chỉ thêm panel khi `tab.Panels` chưa có `Source.Id` của mình (guard chống nhân đôi khi RIBBON/RIBBONCLOSE, đổi workspace).
  3. Đổi `COLORTHEME` → gỡ và tạo lại **panel** của mình (icon vector theo theme), không bao giờ gỡ tab.
  4. `Terminate` → gỡ panel của mình; gỡ tab chỉ khi `tab.Panels.Count == 0`.
  5. Log dòng `ribbon panel <PanelId> added to tab HPAUTOCAD_MCP_TAB (tab created|tab existing, …)` để harness chứng minh việc chia sẻ tab.
  Mẫu: `HPAutoCad/HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs` và `HPGeo/HPGeo.AutoCad.Loader/Ribbon/HPGeoRibbonTab.cs`. Civil 3D là sản phẩm khác, tab riêng `HPCivil3d` (`HPCIVIL3D_MCP_TAB`) — cùng giao thức.
- **Loader + `AssemblyLoadContext` riêng là bắt buộc** cho add-in .NET 8 của AutoCAD 2026: DLL AutoCAD load trực tiếp không được có NuGet dependency (chỉ AutoCAD.NET `ExcludeAssets=runtime` + framework); phần thật nằm trong `Contents\<App>\` với `EnableDynamicLoading=true`, resolver từ chối `Ac*/Ad*/Autodesk.*`. Lý do: default context không probe thư mục bundle, bản Serilog/Mvvm thứ hai = `FileLoadException` lúc JIT, `Initialize` không bao giờ chạy.
- Bundle `PackageContents.xml`: `Platform="AutoCAD"` (không `AutoCAD*`), `SeriesMin/Max="R25.1"`; deploy `%AppData%\Autodesk\ApplicationPlugins\<Name>.bundle\` bằng target `DeployBundle` (`-p:DeployBundle=false` khi AutoCAD đang mở). SECURELOAD hỏi lại sau mỗi build.
- WPF trong AutoCAD: `EnterContextualReflection` quanh `InitializeComponent`; mọi property bind TwoWay (`IsChecked`, `SelectedItem`, `Text`) phải có setter (thiếu → "AutoCAD Error Aborting"); `ShowModalWindow(MainWindow.Handle, window, false)`; UserControl dùng `DynamicResource`, không `StaticResource`; đặt `Background/Foreground` trực tiếp trên Window (implicit style không áp cho lớp kế thừa).
- Lệnh ghi bản vẽ (kể cả chỉ ghi Xrecord NOD) không dùng `CommandFlags.NoUndoMarker`.

## Code Quality Guidelines
- Read and follow codebase structure and code standards in `./docs`
- Don't be too harsh on code linting, but **make sure there are no syntax errors and code are compilable**
- Prioritize functionality and readability over strict style enforcement and code formatting
- Use reasonable code quality standards that enhance developer productivity
- Use try catch error handling & cover security standards
- Use `code-reviewer` agent to review code after every implementation

## Pre-commit/Push Rules
- Run linting before commit
- Run tests before push (DO NOT ignore failed tests just to pass the build or github actions)
- Keep commits focused on the actual code changes
- **DO NOT** commit and push any confidential information (such as dotenv files, API keys, database credentials, etc.) to git repository!
- Create clean, professional commit messages without AI references. Use conventional commit format.

## Code Implementation
- Write clean, readable, and maintainable code
- Follow established architectural patterns
- Implement features according to specifications
- Handle edge cases and error scenarios
- **DO NOT** create new enhanced files, update to the existing files directly.

## Visual Aids
- Use `/bs:preview --explain` when explaining unfamiliar code patterns or complex logic
- Use `/bs:preview --diagram` for architecture diagrams and data flow visualization
- Use `/bs:preview --slides` for step-by-step walkthroughs and presentations
- Use `/bs:preview --ascii` for terminal-friendly diagrams (no browser needed to understand)
- Add `--html` to any generation flag for self-contained HTML output (opens in browser, no server needed)
- **Plan context:** Active plan determined from `## Plan Context` in hook injection; visuals save to `{plan_dir}/visuals/`
- If no active plan, fallback to `plans/visuals/` directory
- For Mermaid diagrams, use `/mermaidjs-v11` skill for v11 syntax rules
- See `primary-workflow.md` → Step 6 for workflow integration