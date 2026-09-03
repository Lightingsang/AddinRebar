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
- **MUST** dùng `CommunityToolkit.Mvvm` — KHÔNG tự viết `INotifyPropertyChanged`/`RelayCommand`.
- `sealed partial class XxxViewModel : ObservableObject` — partial bắt buộc cho source generator.
- `[ObservableProperty]` trên private field, `[RelayCommand]` trên method.
- Code-behind chỉ chứa `InitializeComponent()` + `DataContext = viewModel` (DI).
- KHÔNG set `DataContext` trong XAML.
- Mọi style XAML dùng `{DynamicResource ...}` — không `StaticResource` cho color/brush.
- Modal: set `Owner = UiApplication.MainWindowHandle` qua `WindowInteropHelper`.
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