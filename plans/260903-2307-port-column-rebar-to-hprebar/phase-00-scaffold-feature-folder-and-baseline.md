---
phase: 0
title: "Scaffold feature folder + Core project + baseline build"
status: completed
priority: P1
effort: "0.5d"
dependencies: []
---

# Phase 0: Scaffold feature folder + Core project + baseline build

## Overview
Dựng khung: feature folder `Column Rebar/` theo convention, project `HPRebar.Core` (pure logic, không Revit), 2 test project skeleton, thêm `CommunityToolkit.Mvvm`, Theme.xaml, ribbon button. Kết thúc phase: build pass R27 + R23, bấm button trong Revit hiện TaskDialog "Column Rebar — coming soon".

## Requirements
- Functional: ribbon panel `HPRebar` có button `Column Rebar` gọi `ColumnRebarCommand`.
- Non-functional: `HPRebar.Core` **không** reference `Nice3point.Revit.Api.*`; ILRepack merge Core vào `HPRebar.dll` (`IsRepackable=true` đã bật); solution build được cả 10 config.

## Architecture
```
HPRebar/
├── HPRebar.slnx                      ← thêm 3 project + BuildType mapping
├── HPRebar/                          ← add-in (đã có)
│   ├── HPRebar.csproj                ← + CommunityToolkit.Mvvm, + ProjectReference Core
│   ├── Application.cs                ← + panel/button
│   ├── Resources/Themes/             ← Theme.xaml, ThemeDark.xaml, ThemeLight.xaml (shared)
│   └── Column Rebar/
│       ├── ColumnRebarCommand.cs
│       ├── Models/        View/        View Models/     (rỗng, giữ .gitkeep)
├── HPRebar.Core/                     ← MỚI: netstandard2.0, pure logic
│   ├── HPRebar.Core.csproj
│   └── ColumnRebar/                  ← namespace HPRebar.Core.ColumnRebar
├── HPRebar.Core.Tests/               ← MỚI: xUnit net8.0
└── HPRebar.Tests/                    ← Phase 3 (TUnit), chỉ ghi chú ở đây
```

`.slnx` mapping cho project không có config R##:
```xml
<Project Path="HPRebar.Core/HPRebar.Core.csproj">
  <BuildType Solution="Debug.R23|*" Project="Debug" /> … (10 dòng, giống build/Build.csproj)
</Project>
```

## Related Code Files
- Create: `HPRebar/HPRebar.Core/HPRebar.Core.csproj`, `HPRebar/HPRebar.Core/ColumnRebar/.gitkeep`
- Create: `HPRebar/HPRebar.Core.Tests/HPRebar.Core.Tests.csproj`, `…/ColumnRebar/SmokeTests.cs` (1 test `Assert.True(true)` để pipeline chạy)
- Create: `HPRebar/HPRebar/Column Rebar/ColumnRebarCommand.cs`
- Create: `HPRebar/HPRebar/Resources/Themes/Theme.xaml`, `ThemeDark.xaml`, `ThemeLight.xaml` — **extract** từ fenced XAML block trong `.claude/skills/revit-xaml-styles/references/styles/*.md` (là file Markdown, không phải `.xaml`)
- Modify: `HPRebar/HPRebar/HPRebar.csproj` (packages + ProjectReference + `<Page Include="Resources\Themes\*.xaml"/>` nếu SDK không auto-include)
- Modify: `HPRebar/HPRebar/Application.cs` (`CreateRibbon`: panel `"Rebar"`, button `Column Rebar`; giữ `StartupCommand` nguyên)
- Modify: `HPRebar/HPRebar.slnx`

## Implementation Steps
1. Chạy baseline trước khi sửa gì: `cd HPRebar && dotnet build HPRebar.slnx -c Debug.R26` → ghi lại kết quả (pass/fail, thời gian restore). Solution **chưa từng restore** (không có `obj/project.assets.json`) nên lần đầu sẽ tải nhiều package. R27 để Phase 1 xử lý riêng (package 2027 chưa có trong cache).
2. Tạo `HPRebar.Core.csproj`:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>netstandard2.0</TargetFramework>
       <LangVersion>latest</LangVersion>
       <Nullable>enable</Nullable>
       <ImplicitUsings>disable</ImplicitUsings>
       <RootNamespace>HPRebar.Core</RootNamespace>
       <Configurations>Debug;Release</Configurations>
     </PropertyGroup>
     <ItemGroup>
       <PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all"/>
     </ItemGroup>
   </Project>
   ```
   Polyfill cho `record`/`init`/`required` trên netstandard2.0.
3. Tạo `HPRebar.Core.Tests.csproj` (xUnit, `net8.0`, ref Core, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`). `global.json` đã set `test.runner = Microsoft.Testing.Platform` — nếu xUnit 2.x không chạy được với runner này, dùng `xunit.v3` + `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`. Verify bằng `dotnet test HPRebar.Core.Tests`.
4. `HPRebar.csproj`: thêm `<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0"/>` + `<ProjectReference Include="..\HPRebar.Core\HPRebar.Core.csproj"/>`.
5. Feature folder + `ColumnRebarCommand.cs`:
   ```csharp
   namespace HPRebar.ColumnRebar;

   [UsedImplicitly]
   [Transaction(TransactionMode.Manual)]
   public sealed class ColumnRebarCommand : ExternalCommand
   {
       public override void Execute()
       {
           TaskDialog.Show("Column Rebar", "Scaffold OK");
       }
   }
   ```
6. `Application.CreateRibbon`: `var rebarPanel = Application.CreatePanel("Rebar", "HPRebar"); rebarPanel.AddPushButton<ColumnRebarCommand>("Column Rebar").SetImage(...)`. Tạm dùng icon có sẵn `RibbonIcon16/32.png`.
7. Theme: **extract** XAML từ fenced block trong `.claude/skills/revit-xaml-styles/references/styles/*.md` — `theme-dark-sample.md` (1 block) → `ThemeDark.xaml`, `theme-light-sample.md` (1) → `ThemeLight.xaml`, `controls-sample.md` (4) + `spacing-typography-sample.md` (3) → gộp vào `Theme.xaml` qua MergedDictionaries. Đổi `pack://…/MyAddIn;component/…` → `HPRebar`. Chưa merge vào window nào (Phase 6).
   Key có sẵn (dùng đúng tên này ở Phase 6/7, **không** bịa tên mới): brush `Brush.Accent(.Hover/.Pressed)`, `Brush.Background`, `Brush.Surface(Elevated/Hover/Pressed)`, `Brush.Border(.Focus/.Disabled)`, `Brush.Foreground.Primary/Secondary/Tertiary/Disabled/OnAccent`, `Brush.Danger/Warning/Success/Info`; style `PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton`, `LinkButton`, `StandardTextBox`, `NumberTextBox`, `SearchTextBox`, `Card`, `Separator`, `Badge`, `BadgeText`, `Tag`; token `Spacing.Small/Medium/Large(+.Value/Horizontal/Vertical)`, `Font.Family.Default/Mono`, `Font.Size.Caption/Body/BodyStrong/Subheading/Heading/Title`.
   Canvas (Phase 7) cần thêm key mới vào cả ThemeDark + ThemeLight: `Brush.Canvas.Fill`, `Brush.Canvas.MainBar`, `Brush.Canvas.MainBar.Selected`, `Brush.Canvas.Stirrup`, `Brush.Canvas.Bound`, `Brush.Canvas.Tag`.
8. Logging: thêm `<PackageReference Include="Serilog.Sinks.File" Version="7.0.0"/>`; `Application.CreateLogger()` thêm `.WriteTo.File(Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), "HPRebar", "logs", "hprebar-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)`. Cần cho Phase 4/5 debug khi `CreateFreeForm` fail lúc F5 (Debug sink chỉ thấy khi attach debugger).
9. `.slnx`: thêm 2 project với BuildType mapping (Core, Core.Tests). `Core.Tests` cần mapping để `TestProjectModule` (`dotnet test` solution theo `Release.R*`) tìm thấy.
10. Build gate: `dotnet build HPRebar.slnx -c Debug.R26` (chính — máy có Revit 2026) và `-c Debug.R23` (net48). `dotnet test HPRebar.Core.Tests`.
11. F5 (hoặc copy addin thủ công) → Revit 2026 → button hiện dialog + file log xuất hiện ở `%LocalAppData%\HPRebar\logs\`.

## Success Criteria
- [x] `dotnet build HPRebar.slnx -c Debug.R26` pass, `-c Debug.R23` pass
- [x] `dotnet test HPRebar.Core.Tests` pass (1 smoke test) — xunit.v3 3.1.0 + MTP runner
- [x] `HPRebar.Core.csproj` không có bất kỳ `Nice3point.Revit.*` / `RevitAPI` reference (grep)
- [ ] (F5, chưa chạy) Ribbon có button `Column Rebar`, click hiện TaskDialog
- [ ] (F5, chưa chạy) File log ghi được ở `%LocalAppData%\HPRebar\logs\`
- [x] `bin/Debug.R26/…/HPRebar.dll` là 1 file (Core đã merge) — kiểm tra không có `HPRebar.Core.dll` rời trong addin folder

## Risk Assessment
- **ILRepack không merge ProjectReference** → nếu `HPRebar.Core.dll` xuất hiện rời, chấp nhận (Revit vẫn load từ cùng folder nhờ `EnableDynamicLoading`), ghi chú trong Phase 8.
- **xUnit vs Microsoft.Testing.Platform** trong `global.json` → fallback xunit.v3 (bước 3).
- **`.slnx` sửa tay sai cú pháp** → `dotnet sln HPRebar.slnx list` để verify.

## Cook notes (2026-09-03)

- Baseline build R26 truoc khi sua: **pass**, 30.9s (restore lan dau, 23 warning ILRepack `Method reference is used with definition return type` — pre-existing).
- Theme XAML tu dong duoc include as `Page` boi WPF SDK — **khong can** `<Page Include>` thu cong. 8/8 file compile ra `.baml` trong `obj/Debug.R26/Resources/Themes/`.
- Test runner: xUnit 2.x trong cache (2.9.2 + runner.visualstudio 2.8.2) **khong** noi duoc Microsoft.Testing.Platform (can runner.visualstudio >= 3.0). Dung thang `xunit.v3` 3.1.0 + `xunit.runner.visualstudio` 3.1.5 + `OutputType=Exe` + `UseMicrosoftTestingPlatformRunner=true`. Test pass 1/1.
- ILRepack **co** merge ProjectReference: `publish/HPRebar/` chi co `HPRebar.dll` (1.46 MB), khong co `HPRebar.Core.dll` roi. Dung ca R26 (net8) lan R23 (net48).
- **Deploy path thuc te la `%AppData%\Autodesk\Revit\Addins\<ver>\HPRebar\`** (per-user), khong phai `%ProgramData%\...` nhu `CLAUDE.md` ghi. Sua CLAUDE.md o Phase 8.
- `Theme.xaml` merge bang absolute `pack://application:,,,/HPRebar;component/...` URI (khong dung `x:Key` tren item trong `MergedDictionaries` nhu sample skill — `MergedDictionaries` la `Collection<ResourceDictionary>`, khong phai IDictionary). ThemeSwitcher (Phase 6) tim dict theo `Source.OriginalString`.
- Canvas brush key da them vao **ca** ThemeDark + ThemeLight (6 Color + 6 SolidColorBrush moi ben, tong 52 key/file).
