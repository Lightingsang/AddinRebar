# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Role & Responsibilities

Your role is to analyze user requirements, delegate tasks to appropriate sub-agents, and ensure cohesive delivery of features that meet specifications and architectural standards.

## Repository Layout

This repo bundles **three deliverables** for a Revit API 2022–2027 course; treat them as separate concerns — do not cross-wire them:

| Path | What it is | Stack |
|---|---|---|
| `RevitAIApp/` | Working Revit Add-In (the "real" code). Multi-version (R23–R27). | C# / .NET Framework 4.8 (R23–R24) or .NET 8 (R25–R27) / WPF / Nice3point.Revit.Sdk |
| `course-website/` | Static lesson site (`index.html`, `lesson-01..04.html`) deployed via `vercel.json` | Plain HTML |
| `scripts/generate_revit_api_infographics.py` | One-off generator for course infographics (output → `output/`) | Python + `google-genai` (run via `.claude/skills/.venv`) |
| `docs/` | Project docs — `system-architecture.md`, `code-standards.md` (Vietnamese). **Authoritative** for stack rules. |
| `plans/` | Stack-Aware 6-phase plans produced by `/bs:plan`; phase files live under timestamped subfolders. |
| `RevitTemplates-Huong-Dan-Tieng-Viet.md`, `RevitTemplates-Infographic-Prompt.md` | Reference material on Nice3point templates (Vietnamese). |

**Inside `RevitAIApp/`** (mirrors Nice3point `revit-solution` template):
- `RevitAIApp.sln` — solution; configurations are `Debug.R23..R27` / `Release.R23..R27` (suffix = Revit version).
- `MyRevitAIApp/` — the actual add-in project. Entry `Application.cs` registers ribbon → `Commands/StartupCommand.cs` opens `Views/MyRevitAIAppView.xaml` bound to `ViewModels/MyRevitAIAppViewModel.cs` (`ObservableObject` from CommunityToolkit.Mvvm).
- `MyRevitAIApp/MyRevitAIApp.addin` — Revit manifest (AddInId GUID, FullClassName).
- `build/` — ModularPipelines (.NET 10 console) automation: `Program.cs` registers `CompileProjectModule` (default) and `CreateInstallerModule` (when `pack` arg given).
- `install/` — WixSharp installer (`Installer.Generator.cs`, `Installer.Versioning.cs`) invoked implicitly by the build pipeline; outputs to `RevitAIApp/output/` (folder created on first build).
- `source/` — empty placeholder folder; treat as reserved, do not dump files here unless the template convention says so.

## Feature Folder Convention (MANDATORY)

Mọi feature mới trong `RevitAIApp/MyRevitAIApp/` MUST có folder riêng theo template:

```
MyRevitAIApp/
└── <Feature Name With Spaces>/         ← tên feature có space cho readability (vd "View Sheet Creator")
    ├── <FeatureName>Command.cs         ← ExternalCommand entry, ở root feature folder
    ├── <FeatureName>Service.cs         ← Services ở root feature folder (không subfolder Services/)
    ├── Models/
    ├── View/
    └── View Models/                    ← đúng tên có space
```

**Quy tắc bắt buộc:**
- Tên folder feature có thể (và nên) dùng space (vd `View Sheet Creator`) cho readability
- 3 subfolder bắt buộc: `Models`, `View`, `View Models` (đúng chính tả, View Models có space)
- ExternalCommand entry + Services nằm ở **root** của feature folder — KHÔNG tạo subfolder `Commands/`, `Services/` bên trong feature folder
- C# namespace MUST khai báo **explicit** trong mỗi `.cs` file, strip space + PascalCase. Ví dụ folder `View Sheet Creator/Models/` → `namespace MyRevitAIApp.ViewSheetCreator.Models;`. KHÔNG dựa vào auto-namespace của IDE (sẽ propose `View_Sheet_Creator.View_Models` với underscore — phải sửa thủ công).
- XAML files cũng update `x:Class="MyRevitAIApp.<FeatureNameNoSpace>.Views.XxxView"` và `xmlns:vm="clr-namespace:MyRevitAIApp.<FeatureNameNoSpace>.ViewModels"`.
- `Resources/` (Theme.xaml, icons, fonts) là **shared cross-feature** → NẰM Ở root project (`MyRevitAIApp/Resources/`), KHÔNG nhân bản trong từng feature folder.
- KHÔNG tạo folder phẳng `Commands/`, `Services/`, `ViewModels/`, `Views/` ở root project nữa cho feature mới (legacy structure từ Nice3point template, không migrate).
- Template gốc `MyRevitAIApp/Commands/StartupCommand.cs` + `Views/MyRevitAIAppView.xaml` + `ViewModels/MyRevitAIAppViewModel.cs` giữ nguyên (không trong scope migrate, chỉ áp dụng convention cho feature MỚI).

**Lý do:**
1. Feature-based organization giúp dễ tìm code khi project có nhiều feature (vd 20+ command/dialog)
2. Cô lập dependencies (Models, ViewModels, Services của 1 feature gói chung)
3. Dễ remove/disable 1 feature (chỉ cần exclude folder)
4. Khớp với cách user phân tích domain — mỗi business workflow = 1 feature folder

**Reference implementation:** `RevitAIApp/MyRevitAIApp/View Sheet Creator/` (đầu tiên áp dụng convention này, plan tại `C:\Users\NC\.claude\plans\t-i-c-n-t-o-m-t-crispy-hennessy.md`).

## Build, Run, Debug

All commands run from `RevitAIApp/` unless noted. **Always pick the Revit-version-suffixed configuration** — plain `Debug` / `Release` exists in the .sln but is not what you want for an add-in build.

```bash
# Restore + build for a specific Revit version (replace R27 with R23..R27)
dotnet build RevitAIApp.sln -c Debug.R27
dotnet build RevitAIApp.sln -c Release.R27

# Build the single MyRevitAIApp project only
dotnet build MyRevitAIApp/MyRevitAIApp.csproj -c Debug.R27

# Compile everything via the ModularPipelines build (default action)
cd build && dotnet run

# Produce the MSI installer (cleans first, then runs CreateInstallerModule)
cd build && dotnet run -- pack
```

Debug-build outputs auto-deploy to `%ProgramData%\Autodesk\Revit\Addins\<version>\` (driven by `<DeployAddin>true</DeployAddin>` + `<LaunchRevit>true</LaunchRevit>` in `MyRevitAIApp.csproj`). F5 from Rider/VS launches Revit and attaches the debugger. The HARD-GATE-BUILD-VERIFY rule in `/bs:cook` requires `dotnet build -c Debug.R<active-version>` to pass after every `.cs`/`.xaml` change — do not skip it.

**Multi-version conditional compilation** uses constants emitted by the Nice3point MSBuild SDK based on configuration name:
```csharp
#if REVIT2024_OR_GREATER
    long id = elementId.Value;      // .Value is long since 2024
#else
    int id = elementId.IntegerValue; // legacy
#endif
```
Constants: `REVIT2023`, `REVIT2024_OR_GREATER`, etc. Use `!REVIT<XX>_OR_GREATER` to gate code removed in newer versions.

**Tests:** no test project exists yet. When adding one, follow the framework decision tree in `.claude/skills/revit-test/` — TUnit for in-process (needs Revit context), xUnit for pure logic, ricaun-io `RevitTest` if the VS Test Adapter UI is required. Pure-logic code MUST be in a layer that does not touch the Revit API so it remains xUnit-testable (`Document` is sealed, cannot be mocked).

## Architecture Cheatsheet (Nice3point + MVVM)

Read `docs/system-architecture.md` for the full diagram; the load-bearing facts are:

1. **Entry**: Revit loads `MyRevitAIApp.addin` → instantiates `Application : ExternalApplication` → `OnStartup()` configures Serilog + `CreateRibbon()` registers buttons via `Application.CreatePanel(...).AddPushButton<T>(...)`.
2. **Command pattern**: every button is a class deriving `ExternalCommand` (Nice3point.Revit.Toolkit) with `[Transaction(TransactionMode.Manual)]`. `Execute()` constructs ViewModel → View → `ShowDialog()`.
3. **MVVM**: `sealed partial class XxxViewModel : ObservableObject`; `[ObservableProperty]` on private fields, `[RelayCommand]` on methods. Code-behind is `InitializeComponent()` + `DataContext = vm` only — never set DataContext in XAML, never put logic in `*.xaml.cs`.
4. **Revit API access**: wrap every document mutation in `using var t = doc.NewTransaction("..."); t.Start(); ... t.Commit();`. Modeless windows must marshal API calls through `ExternalEvent` (cannot call Revit API from arbitrary threads).
5. **Theme/styles**: every color, spacing, font-size goes through `{DynamicResource Brush.X}` / `{DynamicResource Spacing.X}` so dark/light swap works. Hardcoded values break the runtime theme switch — see `/bs:revit-xaml-styles`.
6. **Logging**: Serilog, daily rolling file at `%LocalAppData%\<AddinName>\logs\addin-YYYY-MM-DD.log`. Always log via DI-injected `ILogger<T>`, not `Log.Logger` directly (except in `Application.cs` bootstrap).

## Workflows

- Primary workflow: `./.claude/rules/primary-workflow.md`
- Development rules: `./.claude/rules/development-rules.md`
- Orchestration protocols: `./.claude/rules/orchestration-protocol.md`
- Documentation management: `./.claude/rules/documentation-management.md`
- And other workflows: `./.claude/rules/*`

**IMPORTANT:** Analyze the skills catalog and activate the skills that are needed for the task during the process.
**IMPORTANT:** DO NOT modify skills in `~/.claude/skills` directory directly. **MUST** modify skills in this current working directory. Unless you are asked to do so.
**IMPORTANT:** You must follow strictly the development rules in `./.claude/rules/development-rules.md` file.
**IMPORTANT:** Before you plan or proceed any implementation, always read the `./README.md` file first to get context.
**IMPORTANT:** Sacrifice grammar for the sake of concision when writing reports.
**IMPORTANT:** In reports, list any unresolved questions at the end, if any.

## Git

**DO NOT** use `chore` and `docs` in commit messages of file changes in `.claude` directory.

## Hook Response Protocol

### Privacy Block Hook (`@@PRIVACY_PROMPT@@`)

When a tool call is blocked by the privacy-block hook, the output contains a JSON marker between `@@PRIVACY_PROMPT_START@@` and `@@PRIVACY_PROMPT_END@@`. **You MUST use the `AskUserQuestion` tool** to get proper user approval.

**Required Flow:**

1. Parse the JSON from the hook output
2. Use `AskUserQuestion` with the question data from the JSON
3. Based on user's selection:
   - **"Yes, approve access"** → Use `bash cat "filepath"` to read the file (bash is auto-approved)
   - **"No, skip this file"** → Continue without accessing the file

**Example AskUserQuestion call:**
```json
{
  "questions": [{
    "question": "I need to read \".env\" which may contain sensitive data. Do you approve?",
    "header": "File Access",
    "options": [
      { "label": "Yes, approve access", "description": "Allow reading .env this time" },
      { "label": "No, skip this file", "description": "Continue without accessing this file" }
    ],
    "multiSelect": false
  }]
}
```

**IMPORTANT:** Always ask the user via `AskUserQuestion` first. Never try to work around the privacy block without explicit user approval.

## Python Scripts (Skills)

When running Python scripts from `.claude/skills/`, use the venv Python interpreter:
- **Linux/macOS:** `.claude/skills/.venv/bin/python3 scripts/xxx.py`
- **Windows:** `.claude\skills\.venv\Scripts\python.exe scripts\xxx.py`

This ensures packages installed by `install.sh` (google-genai, pypdf, etc.) are available.

**IMPORTANT:** When scripts of skills failed, don't stop, try to fix them directly.

## [IMPORTANT] Consider Modularization
- If a code file exceeds 200 lines of code, consider modularizing it
- Check existing modules before creating new
- Analyze logical separation boundaries (functions, classes, concerns)
- Use kebab-case naming with long descriptive names, it's fine if the file name is long because this ensures file names are self-documenting for LLM tools (Grep, Glob, Search)
- Write descriptive code comments
- After modularization, continue with main task
- When not to modularize: Markdown files, plain text files, bash scripts, configuration files, environment variables files, etc.

## Documentation Management

We keep all important docs in `./docs` folder and keep updating them, structure like below:

```
./docs
├── project-overview-pdr.md
├── code-standards.md
├── codebase-summary.md
├── design-guidelines.md
├── deployment-guide.md
├── system-architecture.md
└── project-roadmap.md
```

**IMPORTANT:** *MUST READ* and *MUST COMPLY* all *INSTRUCTIONS* in project `./CLAUDE.md`, especially *WORKFLOWS* section is *CRITICALLY IMPORTANT*, this rule is *MANDATORY. NON-NEGOTIABLE. NO EXCEPTIONS. MUST REMEMBER AT ALL TIMES!!!*
