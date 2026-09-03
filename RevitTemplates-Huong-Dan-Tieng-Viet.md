# Hướng Dẫn Toàn Tập: Nice3point RevitTemplates cho Người Việt

> **Nguồn gốc:** [github.com/Nice3point/RevitTemplates](https://github.com/Nice3point/RevitTemplates) — bộ template C#/.NET chính thức do cộng đồng tin dùng để phát triển Add-In cho Autodesk Revit.
> **Phiên bản tài liệu này tham chiếu:** 6.2.2 (phát hành 12/05/2026).
> **License:** MIT — dùng cho dự án thương mại thoải mái.

---

## 1. RevitTemplates Là Gì? Tại Sao Phải Dùng?

Khi bạn muốn viết một plugin (Add-In) cho Revit bằng C#, có hai con đường:

| Cách làm | Đặc điểm |
|---|---|
| **Tự tạo project từ đầu** | Phải tự cấu hình `.csproj`, viết tay `.addin` manifest, copy DLL sau mỗi lần build, debug bằng cách attach process thủ công, tự xử lý multi-version Revit, tự setup logging, MVVM, DI... → Mất 2–3 ngày chỉ để dựng khung. |
| **Dùng `Nice3point.Revit.Templates`** | Gõ 1 lệnh `dotnet new revit-addin`, có ngay project chạy được trong Revit, đủ MVVM + DI + Serilog + multi-version + auto-deploy + nút Ribbon. → 30 giây có project chạy. |

**Tóm tắt một câu:** RevitTemplates là "Spring Initializr cho Revit Add-In" — bộ scaffolder giúp bạn bỏ qua toàn bộ phần boilerplate khó chịu nhất khi làm Revit plugin.

### Các tính năng đã được tích hợp sẵn

- ✅ **Multi-target Revit:** Một code base, build được cho Revit 2023 → 2027 (cấu hình `Debug.R23` → `Debug.R27`).
- ✅ **WPF + MVVM:** Có sẵn `View`, `ViewModel`, dùng `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand).
- ✅ **Dependency Injection:** Hai chế độ — `Microsoft.Extensions.DependencyInjection` (nhẹ) hoặc `Microsoft.Extensions.Hosting` (đầy đủ vòng đời).
- ✅ **Serilog logging:** Cấu trúc log chuẩn enterprise.
- ✅ **Nice3point.Revit.Sdk:** MSBuild SDK tự custom — viết `.csproj` ngắn gọn, tự biết deploy DLL vào đâu, tự launch Revit khi F5 debug.
- ✅ **Assembly isolation cho .NET Core:** Tránh xung đột DLL giữa các add-in (vấn đề nan giải của Revit 2025+).
- ✅ **ILRepack:** Gộp các DLL phụ thuộc vào 1 file duy nhất → tránh "DLL hell".
- ✅ **CI/CD sẵn sàng:** GitHub Actions + Azure DevOps pipeline mẫu.
- ✅ **Installer + Autodesk Store bundle:** Build ra `.msi` và bundle để đẩy lên Autodesk App Store.

---

## 2. Cài Đặt Templates (Một Lần Duy Nhất)

### Bước 1: Cài .NET SDK

Tải [.NET SDK mới nhất](https://dotnet.microsoft.com/download) (khuyến nghị .NET 8 hoặc .NET 9 SDK). Kiểm tra:

```bash
dotnet --version
```

### Bước 2: Cài bộ templates từ NuGet

Mở Terminal / PowerShell / CMD và gõ:

```bash
dotnet new install Nice3point.Revit.Templates
```

> Lệnh này tải gói từ [nuget.org/packages/Nice3point.Revit.Templates](https://www.nuget.org/packages/Nice3point.Revit.Templates) và đăng ký vào .NET Template Engine.

### Bước 3: Kiểm tra cài đặt thành công

```bash
dotnet new list revit
```

Bạn sẽ thấy 6 templates:

| Short Name | Tên Đầy Đủ | Mục Đích |
|---|---|---|
| `revit-addin` | Revit AddIn | **Project đơn lẻ** — gọn nhẹ, làm 1 add-in nhỏ |
| `revit-application` | Revit AddIn Application | **Entry point** của kiến trúc modular (chia nhiều module) |
| `revit-module` | Revit AddIn Module | **Module business logic**, phải reference Application |
| `revit-solution` | Revit AddIn Solution | **Solution enterprise** — đủ CI/CD + installer + docs |
| `revit-benchmark` | Revit Benchmark | Test hiệu năng dùng BenchmarkDotNet chạy trong thread Revit |
| `revit-test` | Revit Test | Unit test dùng TUnit, chạy trong process Revit (có full Revit API) |

---

## 3. Tạo Add-In Đầu Tiên — 30 Giây Là Có Project Chạy

### Cách 1: Dùng CLI (Khuyên dùng cho người mới)

```bash
mkdir MyFirstRevitAddIn
cd MyFirstRevitAddIn
dotnet new revit-addin
```

Sau lệnh trên, bạn có ngay một project hoàn chỉnh.

### Cách 2: Dùng IDE (JetBrains Rider hoặc Visual Studio)

- **Visual Studio:** `File` → `New Project` → gõ "Revit" trong search box → chọn `Revit AddIn`.
- **JetBrains Rider:** `New Solution` → chọn `Revit` ở sidebar → chọn template.

### Các tùy chọn (parameters) khi tạo `revit-addin`

Template `revit-addin` có 4 tham số chính bạn nên biết:

| Tham số | Giá trị | Ý nghĩa |
|---|---|---|
| `--addinManifestType` | `application` (mặc định) / `dbApplication` / `command` | Loại add-in đăng ký trong `.addin` manifest |
| `--addinUiWpf` | `true` (mặc định) / `false` | Có sinh code WPF + MVVM hay không |
| `--addinDiMode` | `disabled` (mặc định) / `container` / `hosting` | Có dùng DI không, dùng loại nào |
| `--addinLogging` | `false` (mặc định) / `true` | Có dùng Serilog hay không |

**Ví dụ "đỉnh nóc" — full feature:**

```bash
dotnet new revit-addin \
  --addinManifestType application \
  --addinUiWpf true \
  --addinDiMode hosting \
  --addinLogging true \
  --name MyAdvancedAddIn
```

**Ví dụ siêu tối giản — chỉ là External Command:**

```bash
dotnet new revit-addin \
  --addinManifestType command \
  --addinUiWpf false \
  --addinDiMode disabled \
  --addinLogging false
```

### Giải thích 3 loại Add-In Manifest

| Loại | Class kế thừa | Khi nào dùng |
|---|---|---|
| **Application** | `ExternalApplication` | Add-in chạy ngay khi Revit khởi động → tạo Ribbon, button, sự kiện. **Mặc định**. |
| **DBApplication** | `ExternalDBApplication` | Add-in chạy headless (không UI), chỉ xử lý database — dùng cho automation/batch processing. |
| **Command** | `ExternalCommand` | Add-in chạy 1 lần khi user nhấn button → thực thi xong là thoát. |

---

## 4. Cấu Trúc Project Sinh Ra

Sau khi chạy `dotnet new revit-addin` với full features (`application + WPF + hosting + logging`), bạn sẽ có:

```
MyFirstRevitAddIn/
├── Application.cs                          ← Entry point - kế thừa ExternalApplication
├── Host.cs                                 ← Bootstrap DI container (Microsoft.Extensions.Hosting)
├── MyFirstRevitAddIn.csproj                ← Dùng SDK="Nice3point.Revit.Sdk"
├── MyFirstRevitAddIn.addin                 ← Revit manifest (XML)
├── Commands/
│   └── StartupCommand.cs                   ← External Command - logic chính của button
├── Configuration/
│   ├── HostingConfiguration.cs             ← Cấu hình DI host
│   └── LoggerConfiguration.cs              ← Cấu hình Serilog
├── Models/                                  ← (Trống) — nơi chứa data models
├── ViewModels/
│   └── MyFirstRevitAddInViewModel.cs       ← ViewModel kế thừa ObservableObject
├── Views/
│   ├── MyFirstRevitAddInView.xaml          ← Cửa sổ WPF
│   └── MyFirstRevitAddInView.xaml.cs       ← Code-behind
└── Resources/
    └── Icons/
        ├── RibbonIcon16.png                ← Icon nhỏ trên Ribbon
        └── RibbonIcon32.png                ← Icon lớn trên Ribbon
```

### Phân tích `Application.cs` — file quan trọng nhất

```csharp
[UsedImplicitly]
public class Application : AsyncExternalApplication
{
    public override async Task OnStartupAsync()
    {
        await Host.StartAsync();      // 1. Khởi động DI container
        CreateRibbon();               // 2. Tạo tab/panel/button trên Ribbon Revit
    }

    public override async Task OnShutdownAsync()
    {
        await Host.StopAsync();       // 3. Dọn dẹp khi Revit thoát
    }

    private void CreateRibbon()
    {
        var panel = Application.CreatePanel("Commands", "MyFirstRevitAddIn");
        panel.AddPushButton<StartupCommand>("Execute")
            .SetImage("/MyFirstRevitAddIn;component/Resources/Icons/RibbonIcon16.png")
            .SetLargeImage("/MyFirstRevitAddIn;component/Resources/Icons/RibbonIcon32.png");
    }
}
```

> **Điểm hay:** Method `CreatePanel`, `AddPushButton<T>()`, `SetImage()`, `SetLargeImage()` là **extension method từ `Nice3point.Revit.Toolkit`** — viết ngắn gọn hơn rất nhiều so với code Revit API gốc (vốn cần `Autodesk.Revit.UI.UIControlledApplication`, `RibbonPanel`, `PushButtonData`, lấy assembly path bằng `Assembly.GetExecutingAssembly()`...).

### Phân tích `StartupCommand.cs`

```csharp
[Transaction(TransactionMode.Manual)]
public class StartupCommand : ExternalCommand
{
    public override void Execute()
    {
        var view = Host.GetService<MyFirstRevitAddInView>();   // Lấy View từ DI
        view.ShowDialog();                                     // Show WPF dialog
    }
}
```

### Phân tích `MyFirstRevitAddIn.csproj`

```xml
<Project Sdk="Nice3point.Revit.Sdk">
  <PropertyGroup>
    <UseWPF>true</UseWPF>
    <DeployAddin>true</DeployAddin>     <!-- Tự copy DLL vào ProgramData/Autodesk/Revit/Addins -->
    <LaunchRevit>true</LaunchRevit>     <!-- Khi F5 → auto mở Revit -->
    <IsRepackable>true</IsRepackable>   <!-- ILRepack gộp DLL phụ thuộc -->
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Configurations>Debug.R23;Debug.R24;Debug.R25;Debug.R26;Debug.R27</Configurations>
    <Configurations>$(Configurations);Release.R23;Release.R24;Release.R25;Release.R26;Release.R27</Configurations>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Nice3point.Revit.Toolkit" Version="$(RevitVersion).*"/>
    <PackageReference Include="Nice3point.Revit.Extensions" Version="$(RevitVersion).*"/>
    <PackageReference Include="Nice3point.Revit.Api.RevitAPI" Version="$(RevitVersion).*"/>
    <PackageReference Include="Nice3point.Revit.Api.RevitAPIUI" Version="$(RevitVersion).*"/>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.7"/>
    <PackageReference Include="Serilog" Version="4.3.1"/>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2"/>
  </ItemGroup>
</Project>
```

> **Điểm thần thánh:** `Version="$(RevitVersion).*"` — chỉ một dòng, NuGet sẽ tự pull đúng RevitAPI.dll cho version đang build (chọn `Debug.R27` → kéo `Nice3point.Revit.Api.RevitAPI` version 27.*).

---

## 5. Workflow Phát Triển: Code → F5 → Debug Trong Revit

### Bước 1: Chọn configuration tương ứng với Revit đã cài

Trong IDE (Rider/Visual Studio), ở dropdown cấu hình:

- Bạn có Revit 2024 → chọn `Debug.R24`
- Bạn có Revit 2027 → chọn `Debug.R27`

Khi chọn xong, MSBuild tự động:
1. Set `TargetFramework` đúng (Revit 2025+ dùng `.NET 8`, Revit 2024 dùng `.NET Framework 4.8`).
2. Pull đúng RevitAPI.dll cho version.
3. Define preprocessing constants: `REVIT2027`, `REVIT2027_OR_GREATER`, etc.

### Bước 2: Nhấn F5

Vì `<LaunchRevit>true</LaunchRevit>` đã được set, IDE sẽ:
1. Build project.
2. Copy DLL vào folder `%ProgramData%\Autodesk\Revit\Addins\<version>\`.
3. Copy `.addin` manifest vào cùng chỗ.
4. **Launch `Revit.exe`** với debugger attach sẵn.
5. Bạn có thể đặt breakpoint trong `Application.cs` hoặc `StartupCommand.cs` → khi Revit gọi tới, debugger dừng đúng dòng.

### Bước 3: Test add-in trong Revit

- Khi Revit khởi động xong, vào tab Ribbon → bạn sẽ thấy panel **"Commands"** với nút **"Execute"**.
- Bấm nút → cửa sổ WPF (View của bạn) hiện ra.

---

## 6. Quản Lý Tương Thích Nhiều Phiên Bản Revit (2019–2027)

Đây là điểm **mạnh nhất** của RevitTemplates. Revit API thay đổi giữa các version (rất nhiều method bị deprecated, đổi tên, đổi signature).

### Cách dùng preprocessor directives

Khi cần code khác nhau cho version khác nhau:

```csharp
#if REVIT2021_OR_GREATER
    // API mới (Revit 2021+): dùng ForgeTypeId
    var mm = UnitUtils.ConvertFromInternalUnits(69, UnitTypeId.Millimeters);
#else
    // API cũ: dùng DisplayUnitType (đã deprecated)
    var mm = UnitUtils.ConvertFromInternalUnits(69, DisplayUnitType.DUT_MILLIMETERS);
#endif
```

Hoặc loại trừ API đã bị xóa:

```csharp
#if !REVIT2023_OR_GREATER
    // ElementId.IntegerValue chỉ tồn tại ở Revit < 2023
    var builtinCategory = (BuiltInCategory)category.Id.IntegerValue;
#endif
```

### Danh sách constants có sẵn

| Constant | Đúng khi |
|---|---|
| `REVIT2023` | Đang build cho đúng Revit 2023 |
| `REVIT2024` | Đang build cho đúng Revit 2024 |
| `REVIT2025` | Đang build cho đúng Revit 2025 |
| `REVIT2026` | Đang build cho đúng Revit 2026 |
| `REVIT2027` | Đang build cho đúng Revit 2027 |
| `REVIT2023_OR_GREATER` | Revit ≥ 2023 |
| `REVIT2024_OR_GREATER` | Revit ≥ 2024 |
| `REVIT2025_OR_GREATER` | Revit ≥ 2025 |
| `REVIT2026_OR_GREATER` | Revit ≥ 2026 |
| `REVIT2027_OR_GREATER` | Revit ≥ 2027 |

---

## 7. Kiến Trúc DI + MVVM Trong Project

### Khi `addinDiMode = hosting` (recommended cho project lớn)

```
┌─────────────────────────────────────────────┐
│         Revit khởi động                      │
│              ↓                               │
│  Application.OnStartupAsync()               │
│              ↓                               │
│         Host.StartAsync()                    │
│              ↓                               │
│  HostApplicationBuilder                      │
│    ├── Logging: Serilog                      │
│    ├── Configuration: ConfigureHosting()     │
│    └── Services:                             │
│          ├── ViewModel (Transient)           │
│          └── View (Transient)                │
│              ↓                               │
│         CreateRibbon() → Add button         │
└─────────────────────────────────────────────┘

User nhấn button trên Ribbon
              ↓
StartupCommand.Execute()
              ↓
var view = Host.GetService<View>();   ← Resolved từ DI
view.ShowDialog();
              ↓
View được inject ViewModel qua constructor
ViewModel có ILogger inject sẵn
```

### Code DI trong `Host.cs`

```csharp
public static async Task StartAsync()
{
    var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
    {
        ContentRootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
        DisableDefaults = true
    });

    // Logging
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    // Configuration
    builder.ConfigureHosting();

    // MVVM
    builder.Services.AddTransient<MyFirstRevitAddInViewModel>();
    builder.Services.AddTransient<MyFirstRevitAddInView>();

    _host = builder.Build();
    await _host.StartAsync();
}
```

### ViewModel với DI

```csharp
public sealed class MyFirstRevitAddInViewModel(
    ILogger<MyFirstRevitAddInViewModel> logger
) : ObservableObject
{
    // ObservableObject từ CommunityToolkit.Mvvm
    // → Tự sinh INotifyPropertyChanged
    // logger được inject tự động từ Host
}
```

---

## 8. Deploy & Phát Hành

### Deploy local (cho mình dùng)

`<DeployAddin>true</DeployAddin>` đã tự copy DLL + `.addin` vào:

- **Windows:** `C:\ProgramData\Autodesk\Revit\Addins\<version>\`

Không cần làm gì thêm.

### Build Release để chia sẻ

```bash
dotnet build -c Release.R27
```

Output sẽ ở `bin/Release.R27/` — copy folder này cho khách hàng để họ paste vào folder Addins của họ.

### Tạo Installer `.msi` (dùng template `revit-solution`)

Template `revit-solution` đã có sẵn:
- Project `Installer` (dùng WixSharp).
- Script `build/` với ModularPipelines.
- Output: file `.msi` cài đặt với 1 click.

### Đẩy lên Autodesk App Store

Template `revit-solution` đi kèm cấu trúc Autodesk Store Bundle — đúng format Autodesk yêu cầu:
- Folder `Contents/` chứa DLL + `.addin`.
- File `PackageContents.xml` (Autodesk standard).
- Đóng gói → upload lên [apps.autodesk.com](https://apps.autodesk.com).

---

## 9. Các Template Còn Lại — Khi Nào Dùng?

### `revit-application` + `revit-module` (Kiến trúc Modular)

Khi dự án lớn, bạn không nên nhồi mọi command vào 1 DLL. Tách thành:

```
MyCompany.Revit.AddIn/                       ← revit-application (Entry point)
├── Application.cs                           ← Tạo Ribbon, load các module
└── MyCompany.Revit.AddIn.addin

MyCompany.Revit.AddIn.Walls/                 ← revit-module 1
├── Commands/                                 ← Logic về tường
└── Views/

MyCompany.Revit.AddIn.Doors/                 ← revit-module 2
├── Commands/                                 ← Logic về cửa
└── Views/

MyCompany.Revit.AddIn.Reports/               ← revit-module 3
├── Commands/                                 ← Logic xuất báo cáo
└── Views/
```

→ Mỗi module độc lập, dễ giao cho thành viên team khác nhau, dễ unit test.

### `revit-benchmark`

Khi bạn cần đo hiệu năng code Revit API. Vì BenchmarkDotNet thông thường không chạy được trong process Revit (cần Revit context để gọi API), template này đã cấu hình sẵn để benchmark **chạy trong main thread của Revit**.

```bash
dotnet new revit-benchmark --name MyAddIn.Benchmarks
```

### `revit-test`

Test với access đầy đủ vào Revit API (transaction, document, element...). Dùng [TUnit](https://github.com/thomhurst/TUnit) — framework testing modern, fast.

```bash
dotnet new revit-test --name MyAddIn.Tests
```

---

## 10. Các Thư Viện Bundled Quan Trọng

| Thư viện | Mục đích | Link |
|---|---|---|
| **Nice3point.Revit.Sdk** | Custom MSBuild SDK — cấu hình build tự động | [GitHub](https://github.com/Nice3point/RevitTemplates) |
| **Nice3point.Revit.Toolkit** | Extension methods cho UIControlledApplication, Ribbon, etc. — viết code ngắn gọn | NuGet |
| **Nice3point.Revit.Extensions** | Extension methods cho Element, Document — như LINQ cho Revit | NuGet |
| **Nice3point.Revit.Api.RevitAPI** | RevitAPI.dll đóng gói NuGet, multi-version | NuGet |
| **CommunityToolkit.Mvvm** | `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` — MVVM no-boilerplate | [Microsoft](https://github.com/CommunityToolkit/dotnet) |
| **Microsoft.Extensions.Hosting** | DI + lifecycle management chuẩn .NET | Microsoft |
| **Serilog** | Structured logging | [serilog.net](https://serilog.net) |
| **ILRepack** | Gộp các DLL phụ thuộc thành 1 file | NuGet |
| **JetBrains.Annotations** | `[UsedImplicitly]`, suppress false-positive warning | NuGet |

---

## 11. Troubleshooting — Lỗi Thường Gặp

| Triệu chứng | Nguyên nhân | Cách fix |
|---|---|---|
| F5 không mở Revit | `<LaunchRevit>true</LaunchRevit>` chưa có hoặc Revit chưa cài đúng version | Kiểm tra `.csproj` + chọn đúng `Debug.R<XX>` |
| Build fail "RevitAPI.dll not found" | Chọn nhầm configuration (vd. build Debug.R27 nhưng máy chỉ có Revit 2024) | Đổi sang `Debug.R24` |
| Add-in không hiện trong Revit | `.addin` manifest chưa copy đúng folder | Verify `<DeployAddin>true</DeployAddin>` + check `%ProgramData%\Autodesk\Revit\Addins\` |
| `FileLoadException` khi load DLL | DLL phụ thuộc xung đột version với add-in khác | Bật `<IsRepackable>true</IsRepackable>` (ILRepack gộp DLL) |
| Lỗi compile sau khi tạo template | `dotnet new install` cài bản cũ | `dotnet new uninstall Nice3point.Revit.Templates` rồi cài lại |

---

## 12. Tài Liệu Tham Khảo

- **GitHub:** [github.com/Nice3point/RevitTemplates](https://github.com/Nice3point/RevitTemplates)
- **Wiki:** [github.com/Nice3point/RevitTemplates/wiki](https://github.com/Nice3point/RevitTemplates/wiki) — gồm các trang:
  - Installation
  - Templates
  - Step-by-step Guide
  - Managing API Compatibility
  - Third-party Files
  - Publishing the Release
  - Autodesk Store Bundle
  - MsBuild Sdk
- **NuGet:** [nuget.org/packages/Nice3point.Revit.Templates](https://www.nuget.org/packages/Nice3point.Revit.Templates)
- **Sample real-world project:** [RevitLookup](https://github.com/jeremytammik/RevitLookup) — dùng bộ template này.

---

## TL;DR Cho Người Bận

```bash
# 1. Cài 1 lần
dotnet new install Nice3point.Revit.Templates

# 2. Tạo project mới
mkdir MyAddIn && cd MyAddIn
dotnet new revit-addin

# 3. Mở bằng Rider hoặc Visual Studio
# 4. Chọn Debug.R27 (hoặc version Revit của bạn)
# 5. Nhấn F5 → Revit tự khởi động → debug ngay
```

Xong. Bạn vừa tiết kiệm 2 ngày setup.
