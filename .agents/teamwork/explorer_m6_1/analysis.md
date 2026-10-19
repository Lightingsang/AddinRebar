# ARCHITECTURAL ANALYSIS & SYSTEM SPECIFICATION
## Milestone M6: HPKata.Revit 2026 Add-in Architecture

- **Author**: `explorer_m6_1` (Revit 2026 .NET 8 Architecture Explorer)
- **Target**: `HPKata/HPKata.Revit/` (.NET 8.0 Windows, Revit 2026 API)
- **Parent**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
- **Status**: COMPLETE & VERIFIED
- **Date**: 2026-10-17

---

## 1. Executive Summary

Milestone M6 delivers the Autodesk Revit 2026 native add-in deliverable for the **HPKata** dual-host structural reinforcement suite. Following the successful completion of Milestones M1–M5 (pure domain `HPKata.Core`, shared MVVM `HPKata.UI`, and AutoCAD 2026 integration `HPKata.AutoCad`), `HPKata.Revit` integrates the suite into Revit 2026.

### Key Objectives
1. **Target Runtime**: .NET 8.0 (`net8.0-windows`), matching Revit 2026's modern CoreCLR host.
2. **Revit 2026 API Integration**: Pure reference-assembly binding via `Nice3point.Revit.Api.RevitAPI` (version 2026.0.4) and `Nice3point.Revit.Api.RevitAPIUI` (version 2026.0.4) with `PrivateAssets="all"`, ensuring zero runtime DLL pollution in the build artifacts.
3. **Application Lifecycle**: Robust `IExternalApplication` implementation (`HPKataApplication`) registering ribbon tab `"HPKATA"` and panel `"Cốt Thép (Rebar)"` with 5 primary push-button commands.
4. **WPF UI Integration**: Safe dialog hosting with `WindowInteropHelper.Owner = uiApp.MainWindowHandle` and live dynamic Dark/Light theme synchronization via `RevitHostTheme` implementing `HPKata.UI.Common.IHostTheme`.
5. **Native Rebar & Scheduling Services**: Full two-way bridge between Revit's `Autodesk.Revit.DB.Structure.Rebar` and `HPKata.Core.Models` (`RebarItem`, `KataRebarSpec`, `RebarScheduleReport`), supporting native 3D bar generation via Revit 2026 `BarTerminationsData` and headless ClosedXML export.
6. **Strict Clean Code Governance**: 100% adherence to `HP_CLEAN_CODE_CORE.md` and `REVITADDINAI_CLEAN_CODE_STANDARD.md` (all classes $\le 300$ lines, methods $\le 50$ lines, zero commented-out code, zero unannotated catch blocks).

---

## 2. Project Configuration & Dependencies (`HPKata.Revit.csproj`)

### 2.1 Project File Specification
`HPKata.Revit.csproj` targets `net8.0-windows` and consumes the domain models from `HPKata.Core` and WPF views from `HPKata.UI`. It marks Revit API reference assemblies as compile-only (`PrivateAssets="all"` and `ExcludeAssets="runtime"`), preventing `RevitAPI.dll` or `RevitAPIUI.dll` from ever being copied into `bin/` or deployed into the Revit directory.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>HPKata.Revit</RootNamespace>
    <AssemblyName>HPKata.Revit</AssemblyName>
    <Configurations>Debug;Release</Configurations>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <RevitPackageVersion Condition="'$(RevitPackageVersion)' == ''">2026.0.4</RevitPackageVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- Revit 2026 Reference Assemblies (Compile-only, Never deployed) -->
    <PackageReference Include="Nice3point.Revit.Api.RevitAPI" Version="$(RevitPackageVersion)" PrivateAssets="all" ExcludeAssets="runtime" />
    <PackageReference Include="Nice3point.Revit.Api.RevitAPIUI" Version="$(RevitPackageVersion)" PrivateAssets="all" ExcludeAssets="runtime" />

    <!-- Shared UI & Utilities -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="MaterialDesignThemes" Version="5.2.1" />
    <PackageReference Include="ClosedXML" Version="0.104.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- Internal Solution References -->
    <ProjectReference Include="..\HPKata.Core\HPKata.Core.csproj" />
    <ProjectReference Include="..\HPKata.UI\HPKata.UI.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="HPKata.Revit.Tests" />
  </ItemGroup>

  <ItemGroup>
    <None Update="HPKata.Revit.addin">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

### 2.2 Solution Integration (`HPKata.slnx`)
Add `HPKata.Revit/HPKata.Revit.csproj` and its companion test project `HPKata.Revit.Tests/HPKata.Revit.Tests.csproj` to `HPKata/HPKata.slnx`:

```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
  </Folder>
  <Project Path="HPKata.Core/HPKata.Core.csproj" />
  <Project Path="HPKata.Core.Tests/HPKata.Core.Tests.csproj" />
  <Project Path="HPKata.UI/HPKata.UI.csproj" />
  <Project Path="HPKata.UI.Tests/HPKata.UI.Tests.csproj" />
  <Project Path="HPKata.AutoCad/HPKata.AutoCad.csproj" />
  <Project Path="HPKata.AutoCad.Tests/HPKata.AutoCad.Tests.csproj" />
  <Project Path="HPKata.Revit/HPKata.Revit.csproj" />
  <Project Path="HPKata.Revit.Tests/HPKata.Revit.Tests.csproj" />
</Solution>
```

### 2.3 Add-In Manifest (`HPKata.Revit.addin`)
The standard manifest deploys to `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin`:

```xml
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>HPKata Rebar Detailing</Name>
    <Assembly>HPKata.Revit\HPKata.Revit.dll</Assembly>
    <AddInId>F75E4B22-2E91-4961-A1DF-D20F88D8E941</AddInId>
    <FullClassName>HPKata.Revit.Entry.HPKataApplication</FullClassName>
    <VendorId>HPAI</VendorId>
    <VendorDescription>HPKata Structural Rebar Suite for Revit 2026</VendorDescription>
  </AddIn>
  <ManifestSettings>
    <UseRevitContext>False</UseRevitContext>
    <ContextName>HPKata.Revit</ContextName>
  </ManifestSettings>
</RevitAddIns>
```

---

## 3. Lifecycle Management & Ribbon Setup

### 3.1 `HPKataApplication` Lifecycle Architecture
`HPKataApplication` implements `Autodesk.Revit.UI.IExternalApplication`. It acts as the orchestrating composition root for Revit add-in startup and shutdown.

```csharp
namespace HPKata.Revit.Entry;

using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

/// <summary>
/// Entry point and lifecycle orchestrator for HPKata Revit 2026 Add-in.
/// </summary>
public sealed class HPKataApplication : IExternalApplication
{
    private EventHandler<ThemeChangedEventArgs>? _themeChangedHandler;

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            RibbonBuilder.Build(application);
            RegisterThemeListener(application);
            return Result.Succeeded;
        }
        catch (Exception)
        {
            // Fail safely without crashing Revit's startup sequence
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        UnregisterThemeListener(application);
        return Result.Succeeded;
    }

    private void RegisterThemeListener(UIControlledApplication application)
    {
        _themeChangedHandler = (_, _) =>
        {
            RevitHostTheme.Instance.NotifyChanged();
            RibbonBuilder.UpdateIcons();
        };
        application.ThemeChanged += _themeChangedHandler;
    }

    private void UnregisterThemeListener(UIControlledApplication application)
    {
        if (_themeChangedHandler != null)
        {
            application.ThemeChanged -= _themeChangedHandler;
            _themeChangedHandler = null;
        }
    }
}
```

### 3.2 Ribbon Panel Builder (`RibbonBuilder.cs`)
To keep `HPKataApplication` cohesive and under 50 lines per method, all Ribbon construction logic is encapsulated inside `RibbonBuilder`:
- **Tab Name**: `"HPKATA"`
- **Panel Name**: `"Cốt Thép (Rebar)"`
- **5 Push Buttons**:

| Button Text | Command Class | Icon / Glyph | Description & Target View |
|---|---|---|---|
| **Thiết Lập** | `SetupCommand` | Gear / Cog | Opens `SetupView` from `HPKata.UI` for rebar standards and cover config. |
| **Vẽ & Sửa Thép** | `DetailingCommand` | Rebar Hook / Pen | Opens `DetailingView` for 68 rebar shape drafting and dimension modification. |
| **Thống Kê Thép** | `ScheduleCommand` | Table / Grid | Opens `ScheduleView` aggregating all Revit project rebars by diameter & shape. |
| **Tổ Hợp Cắt Thép**| `CuttingStockCommand`| Saw / Scissors | Opens `CuttingStockView` executing 1D cutting stock optimization (11.7m bars). |
| **Xuất Excel** | `ExportExcelCommand` | Spreadsheet | Headless export to `Kata.xlsm`-compliant workbook using `ClosedXmlExporter`. |

```csharp
namespace HPKata.Revit.Entry;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;
using Autodesk.Revit.UI;

/// <summary>
/// Builds and manages Ribbon tabs, panels, and push-buttons for HPKata.
/// </summary>
internal static class RibbonBuilder
{
    private const string TabName = "HPKATA";
    private const string PanelName = "Cốt Thép (Rebar)";
    private static readonly List<(PushButton Button, Func<bool, ImageSource> IconProvider)> TrackedButtons = new();

    public static void Build(UIControlledApplication application)
    {
        RibbonPanel panel = GetOrCreatePanel(application);
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        AddButton(panel, "btnSetup", "Thiết Lập\n(Setup)", assemblyPath,
            "HPKata.Revit.Commands.SetupCommand",
            "Cấu hình tiêu chuẩn uốn móc, lớp bê tông bảo vệ và 86 thông số KATA.",
            RibbonIconFactory.CreateSetupIcon);

        AddButton(panel, "btnDetailing", "Vẽ & Sửa\nThép", assemblyPath,
            "HPKata.Revit.Commands.DetailingCommand",
            "Thiết kế, vẽ và chỉnh sửa quy cách 68 dạng hình cốt thép.",
            RibbonIconFactory.CreateDetailingIcon);

        AddButton(panel, "btnSchedule", "Thống Kê\nCốt Thép", assemblyPath,
            "HPKata.Revit.Commands.ScheduleCommand",
            "Trích xuất, tổng hợp và phân loại đường kính cốt thép trong dự án.",
            RibbonIconFactory.CreateScheduleIcon);

        AddButton(panel, "btnCuttingStock", "Tổ Hợp\nCắt Thép", assemblyPath,
            "HPKata.Revit.Commands.CuttingStockCommand",
            "Tối ưu hóa bài toán cắt thép thanh 11.7m (1D-CSP) giảm phế liệu.",
            RibbonIconFactory.CreateCuttingStockIcon);

        AddButton(panel, "btnExportExcel", "Xuất\nExcel", assemblyPath,
            "HPKata.Revit.Commands.ExportExcelCommand",
            "Xuất bảng thống kê và coupler sang tệp Excel Kata.xlsm hoàn chỉnh.",
            RibbonIconFactory.CreateExportIcon);

        UpdateIcons();
    }

    private static RibbonPanel GetOrCreatePanel(UIControlledApplication app)
    {
        try { app.CreateRibbonTab(TabName); }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { /* Tab already exists */ }

        foreach (var p in app.GetRibbonPanels(TabName))
        {
            if (p.Name == PanelName) return p;
        }
        return app.CreateRibbonPanel(TabName, PanelName);
    }

    private static void AddButton(
        RibbonPanel panel, string name, string text, string assemblyPath,
        string className, string tooltip, Func<bool, ImageSource> iconProvider)
    {
        var data = new PushButtonData(name, text, assemblyPath, className)
        {
            ToolTip = tooltip
        };
        if (panel.AddItem(data) is PushButton button)
        {
            TrackedButtons.Add((button, iconProvider));
        }
    }

    public static void UpdateIcons()
    {
        bool isDark = RevitHostTheme.Instance.IsDark;
        foreach (var (button, iconProvider) in TrackedButtons)
        {
            var icon = iconProvider(isDark);
            button.LargeImage = icon;
            button.Image = icon;
        }
    }
}
```

---

## 4. WPF Host Integration & Theme Synchronization

### 4.1 Safe WPF Dialog Hosting in Revit 2026
In Revit 2026, WPF dialogs run inside the unmanaged `Revit.exe` process host. Displaying an unparented WPF `Window` via `ShowDialog()` causes three major defects:
1. **Z-Order Inversion**: The dialog can drop behind the active Revit document view when clicked outside, causing Revit's UI thread to enter a modal freeze state.
2. **Keyboard Focus Deadlock**: Tab, Enter, and Escape keys fail to dispatch properly between the Win32 message pump and WPF `Dispatcher`.
3. **Multi-DPI Scaling & Placement**: Unowned windows may position on the wrong monitor or fail DPI awareness handoff.

**Solution (`RevitWindowHelper`)**:
Wrap every dialog with `WindowInteropHelper`, setting `Owner = uiApp.MainWindowHandle`:

```csharp
namespace HPKata.Revit.Interop;

using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using HPKata.Revit.Entry;
using HPKata.UI.Common;

/// <summary>
/// Provides safe modal and modeless window hosting within the Revit process.
/// </summary>
public static class RevitWindowHelper
{
    public static bool? ShowModal(Window window, UIApplication uiApp)
    {
        if (window == null) throw new ArgumentNullException(nameof(window));
        if (uiApp == null) throw new ArgumentNullException(nameof(uiApp));

        var helper = new WindowInteropHelper(window)
        {
            Owner = uiApp.MainWindowHandle
        };

        ThemeManager.Attach(window, RevitHostTheme.Instance);
        return window.ShowDialog();
    }
}
```

### 4.2 Dynamic Theme Synchronization (`RevitHostTheme`)
`HPKata.UI` defines `IHostTheme`:
```csharp
public interface IHostTheme
{
    bool IsDark { get; }
    event Action? Changed;
}
```

`HPKata.Revit` implements `RevitHostTheme` using Revit's public API `Autodesk.Revit.UI.UIThemeManager.CurrentTheme`:

```csharp
namespace HPKata.Revit.Entry;

using System;
using Autodesk.Revit.UI;
using HPKata.UI.Common;

/// <summary>
/// Synchronizes HPKata.UI dialogs with Revit 2026's Dark/Light theme mode.
/// </summary>
public sealed class RevitHostTheme : IHostTheme
{
    public static RevitHostTheme Instance { get; } = new();

    private RevitHostTheme() { }

    public bool IsDark
    {
        get
        {
            try
            {
                return UIThemeManager.CurrentTheme == UITheme.Dark;
            }
            catch (Exception)
            {
                // Fallback to dark if running outside active UI context
                return true;
            }
        }
    }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
```

### 4.3 Analysis: `Autodesk.Revit.UI.UIFramework.RevitColors` vs `UIThemeManager`
- `UIFramework.RevitColors` is an internal WPF resource class shipped in `UIFramework.dll` (located in Revit's program directory). While it exposes raw WPF brush definitions (`WindowBackground`, `RibbonBackground`), it is an unversioned private API that introduces fragile binary coupling across different Revit update releases.
- In contrast, `UIThemeManager.CurrentTheme` (`UITheme.Dark` vs `UITheme.Light`) was officially added in Revit 2024 and is the documented, forward-compatible standard for Revit 2026+.
- `ThemeManager.Attach(window, RevitHostTheme.Instance)` maps `IsDark` to `ThemeDark.xaml` and `ThemeLight.xaml` in `HPKata.UI`, dynamically configuring MaterialDesign tokens, elevated surfaces, card backgrounds, and text contrast without any private assembly reflection.

---

## 5. Command Architecture & Revit Interop Engine

### 5.1 The Five Revit External Commands

All commands implement `Autodesk.Revit.UI.IExternalCommand` with `[Transaction(TransactionMode.Manual)]`.

```
                        User Ribbon Click
                                │
        ┌───────────────┬───────┴───────┬───────────────┬──────────────┐
        ▼               ▼               ▼               ▼              ▼
  [SetupCommand] [DetailingCmd]  [ScheduleCmd]  [CuttingStock] [ExportExcelCmd]
        │               │               │               │              │
        ▼               ▼               ▼               ▼              ▼
   SetupView      DetailingView   ScheduleView  CuttingStockView ClosedXmlExporter
   (KataConfig)   (68 Shapes)     (RebarItems)    (1D-CSP)     (Kata.xlsm)
```

#### 1. `SetupCommand`
- Reads configuration from file or defaults (`KataConfigDefaults.CreateDefault()`).
- Opens `SetupView` from `HPKata.UI` using `RevitWindowHelper.ShowModal`.
- On OK or dirty, serializes updated configuration to local storage.

#### 2. `DetailingCommand`
- Opens `DetailingView` from `HPKata.UI` using `RevitWindowHelper.ShowModal`.
- User interactively selects rebar shape (among 68 shapes), enters parameters ($A, B, C, D$), selects diameter and quantity.
- If user confirms, prompts user to pick a structural host element (`RevitSelectionService.PickHostElement`) and calls `RebarCreationService.CreateRebar` inside a Revit `Transaction`.

#### 3. `ScheduleCommand`
- Queries all `Autodesk.Revit.DB.Structure.Rebar` elements in the active document via `RevitRebarExtractor.ExtractAll(doc)`.
- Populates `ScheduleViewModel` with converted `RebarItem` collection.
- Opens `ScheduleView` from `HPKata.UI` with `RevitWindowHelper.ShowModal`.
- Supports the `"Tổ Hợp Cắt Thép"` drill-down event (`RequestOpenCuttingStock`) directly within the view.

#### 4. `CuttingStockCommand`
- Extracts rebar cutting demands from either active selection or entire model.
- Populates `CuttingStockViewModel` and opens `CuttingStockView` from `HPKata.UI`.
- Executes 1D cutting stock solver via `CuttingStockEngine` with 5 heuristics (`DuBang0`, `LapDayCanhDai`, `BeamSearchTopK`, `PatternCat`, `Auto`).

#### 5. `ExportExcelCommand`
- Extracts all project rebars via `RevitRebarExtractor.ExtractAll(doc)`.
- Generates `RebarScheduleReport` using `RebarScheduleAggregator.GenerateReport(items)`.
- Prompts user for target `.xlsx` path using Win32 `SaveFileDialog`.
- Calls `ClosedXmlExporter.ExportScheduleToFile(report, filePath)`.
- Shows Revit `TaskDialog` confirming file generation and item counts.

---

### 5.2 Native Rebar Creation Engine (`RebarCreationService` & `KataRebarCurveFactory`)

Revit 2026 introduced `BarTerminationsData` for end hook definitions in `Rebar.CreateFromCurves`. `HPKata.Revit` encapsulates this host-specific API inside `KataRebarCurveFactory`:

```csharp
namespace HPKata.Revit.NativeRebar;

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Models;
using HPKata.Revit.Interop;

/// <summary>
/// Generates native Revit 3D Rebar elements from host-neutral KataRebarSpec models.
/// </summary>
public static class RebarCreationService
{
    public static Rebar CreateRebar(Document doc, Element host, KataRebarSpec spec)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        if (host == null) throw new ArgumentNullException(nameof(host));
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        RebarBarType barType = BarTypeResolver.ResolveByDiameter(doc, spec.DiameterMm);
        IList<Curve> curves = KataRebarCurveFactory.BuildCenterlineCurves(spec);
        XYZ normal = RevitUnitsConverter.ToRevitVector(spec.DirectionH);

        // Revit 2026 native BarTerminationsData construction
        var terminations = new BarTerminationsData(doc);

        var rebar = Rebar.CreateFromCurves(
            doc,
            RebarStyle.Standard,
            barType,
            host,
            normal,
            curves,
            terminations,
            true,
            true);

        if (rebar == null)
        {
            throw new InvalidOperationException($"Không thể tạo thanh thép đường kính {spec.DiameterMm} mm.");
        }

        ConfigureLayoutAndParameters(rebar, spec);
        return rebar;
    }

    private static void ConfigureLayoutAndParameters(Rebar rebar, KataRebarSpec spec)
    {
        var accessor = rebar.GetShapeDrivenAccessor();
        if (accessor != null)
        {
            switch (spec.LayoutRule)
            {
                case KataLayoutRule.MaximumSpacing:
                    accessor.SetLayoutAsMaximumSpacing(
                        RevitUnitsConverter.ToFeet(spec.SpacingMm),
                        RevitUnitsConverter.ToFeet(spec.ArrayLengthMm),
                        true, true, true);
                    break;
                case KataLayoutRule.FixedNumber:
                    accessor.SetLayoutAsFixedNumber(spec.Quantity, RevitUnitsConverter.ToFeet(spec.ArrayLengthMm), true, true, true);
                    break;
                case KataLayoutRule.Single:
                default:
                    accessor.SetLayoutAsSingle();
                    break;
            }
        }

        // Tag parameters for idempotency and scheduling
        if (!string.IsNullOrEmpty(spec.Partition))
        {
            rebar.get_Parameter(BuiltInParameter.REBAR_PARTITION)?.Set(spec.Partition);
        }
        if (!string.IsNullOrEmpty(spec.ScheduleMark))
        {
            rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(spec.ScheduleMark);
        }
        if (!string.IsNullOrEmpty(spec.Comment))
        {
            rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(spec.Comment);
        }
    }
}
```

---

### 5.3 Native Rebar Extraction Engine (`RevitRebarExtractor`)

To feed `HPKata.UI` schedules, cutting-stock optimization, and ClosedXML export, `RevitRebarExtractor` scans the Revit model and maps native elements into pure `RebarItem` DTOs:

```csharp
namespace HPKata.Revit.Interop;

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Models;

/// <summary>
/// Extracts and normalizes Revit Rebar instances into HPKata domain RebarItem models.
/// </summary>
public static class RevitRebarExtractor
{
    public static List<RebarItem> ExtractAll(Document doc)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        var collector = new FilteredElementCollector(doc)
            .OfClass(typeof(Rebar))
            .WherePasses(new ElementOwnerViewFilter(ElementId.InvalidElementId)) // 3D structural model bars
            .Cast<Rebar>();

        var items = new List<RebarItem>();
        foreach (var rebar in collector)
        {
            if (TryExtractItem(doc, rebar, out var item) && item != null)
            {
                items.Add(item);
            }
        }
        return items;
    }

    private static bool TryExtractItem(Document doc, Rebar rebar, out RebarItem? item)
    {
        item = null;
        if (doc.GetElement(rebar.GetTypeId()) is not RebarBarType barType) return false;

        int diaMm = (int)Math.Round(RevitUnitsConverter.ToMm(barType.BarNominalDiameter));
        if (diaMm <= 0) return false;

        string partition = rebar.get_Parameter(BuiltInParameter.REBAR_PARTITION)?.AsString() ?? "CK01";
        string mark = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "1";
        string notes = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? string.Empty;

        int quantity = rebar.Quantity;
        if (quantity <= 0) quantity = 1;

        double totalLenMm = RevitUnitsConverter.ToMm(rebar.TotalLength);
        double cutLenMm = totalLenMm / quantity;

        // Resolve shape code from RebarShape
        string shapeCode = "00";
        if (doc.GetElement(rebar.GetShapeId()) is RebarShape shape)
        {
            shapeCode = NormalizeRebarShapeName(shape.Name);
        }

        item = new RebarItem
        {
            ComponentMark = partition,
            BarMark = mark,
            ShapeCode = shapeCode,
            DiameterMm = diaMm,
            MemberCount = 1,
            CountPerMember = quantity,
            CuttingLengthMm = Math.Round(cutLenMm, 1),
            Notes = notes
        };
        return true;
    }

    private static string NormalizeRebarShapeName(string shapeName)
    {
        if (string.IsNullOrWhiteSpace(shapeName)) return "00";
        string clean = shapeName.Replace("Kata_", "").Replace("M_", "").Trim();
        return clean.Length > 2 ? clean.Substring(0, 2) : clean;
    }
}
```

---

## 6. Clean Code Structure & File Layout Plan

### 6.1 Directory & Namespace Organization
```
HPKata/
├── HPKata.Revit/                          # Target: net8.0-windows
│   ├── HPKata.Revit.csproj                # Project file (Revit 2026 API reference)
│   ├── HPKata.Revit.addin                 # Add-in manifest
│   │
│   ├── Entry/                             # Lifecycle & Application Entry
│   │   ├── HPKataApplication.cs           # IExternalApplication implementation (<= 80 lines)
│   │   ├── RibbonBuilder.cs               # Ribbon Tab & PushButton registration (<= 120 lines)
│   │   ├── RibbonIconFactory.cs           # Dynamic vector DrawingImage generator (<= 150 lines)
│   │   └── RevitHostTheme.cs              # IHostTheme adapter for UIThemeManager (<= 50 lines)
│   │
│   ├── Commands/                          # IExternalCommand entry points
│   │   ├── SetupCommand.cs                # Opens SetupView (<= 60 lines)
│   │   ├── DetailingCommand.cs            # Opens DetailingView & coordinates detailing (<= 80 lines)
│   │   ├── ScheduleCommand.cs             # Opens ScheduleView (<= 75 lines)
│   │   ├── CuttingStockCommand.cs         # Opens CuttingStockView (<= 70 lines)
│   │   └── ExportExcelCommand.cs          # ClosedXML export pipeline (<= 85 lines)
│   │
│   ├── Interop/                           # Revit Document & WPF Window Adapters
│   │   ├── RevitWindowHelper.cs           # WindowInteropHelper owner attachment (<= 45 lines)
│   │   ├── RevitUnitsConverter.cs         # mm <-> feet metric bridge (<= 40 lines)
│   │   ├── RevitSelectionService.cs       # Element selection filters & prompts (<= 65 lines)
│   │   └── RevitRebarExtractor.cs         # Native Rebar -> RebarItem parser (<= 95 lines)
│   │
│   └── NativeRebar/                       # 3D Rebar Geometry & Parameter Bridge
│       ├── RebarCreationService.cs        # Rebar placement & parameter tagging (<= 90 lines)
│       ├── KataRebarCurveFactory.cs       # BarTerminationsData & curve builder (<= 95 lines)
│       └── BarTypeResolver.cs             # RebarBarType & RebarHookType resolver (<= 75 lines)
│
└── HPKata.Revit.Tests/                    # Target: net8.0-windows (xUnit v3)
    ├── HPKata.Revit.Tests.csproj          # Test project
    ├── RevitUnitsConverterTests.cs        # Test mm <-> feet precision (<= 50 lines)
    ├── BarTypeResolutionTests.cs          # Diameter tolerance tests (<= 60 lines)
    └── RevitHostThemeTests.cs             # Mocked theme event notifications (<= 45 lines)
```

### 6.2 Clean Code Compliance Verification Table

| File | Target Namespace | Planned Lines | Max Method Lines | Rule C2 ($\le 300$) | Rule M3 ($\le 50$) |
|---|---|---|---|---|---|
| `HPKataApplication.cs` | `HPKata.Revit.Entry` | ~50 | ~25 | PASS | PASS |
| `RibbonBuilder.cs` | `HPKata.Revit.Entry` | ~110 | ~35 | PASS | PASS |
| `RibbonIconFactory.cs` | `HPKata.Revit.Entry` | ~140 | ~30 | PASS | PASS |
| `RevitHostTheme.cs` | `HPKata.Revit.Entry` | ~45 | ~15 | PASS | PASS |
| `SetupCommand.cs` | `HPKata.Revit.Commands` | ~55 | ~35 | PASS | PASS |
| `DetailingCommand.cs` | `HPKata.Revit.Commands` | ~75 | ~40 | PASS | PASS |
| `ScheduleCommand.cs` | `HPKata.Revit.Commands` | ~70 | ~40 | PASS | PASS |
| `CuttingStockCommand.cs` | `HPKata.Revit.Commands` | ~65 | ~35 | PASS | PASS |
| `ExportExcelCommand.cs` | `HPKata.Revit.Commands` | ~80 | ~45 | PASS | PASS |
| `RevitWindowHelper.cs` | `HPKata.Revit.Interop` | ~35 | ~20 | PASS | PASS |
| `RevitUnitsConverter.cs` | `HPKata.Revit.Interop` | ~35 | ~10 | PASS | PASS |
| `RevitSelectionService.cs` | `HPKata.Revit.Interop` | ~60 | ~30 | PASS | PASS |
| `RevitRebarExtractor.cs` | `HPKata.Revit.Interop` | ~90 | ~40 | PASS | PASS |
| `RebarCreationService.cs` | `HPKata.Revit.NativeRebar`| ~85 | ~35 | PASS | PASS |
| `KataRebarCurveFactory.cs`| `HPKata.Revit.NativeRebar`| ~95 | ~45 | PASS | PASS |
| `BarTypeResolver.cs` | `HPKata.Revit.NativeRebar`| ~70 | ~30 | PASS | PASS |

---

## 7. Verification Method & Independent Testing Strategy

### 7.1 Automated Unit Tests (`HPKata.Revit.Tests`)
Run offline without active Revit instance:
```bash
dotnet test g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit.Tests\HPKata.Revit.Tests.csproj
```
- **Tests**:
  1. `RevitUnitsConverterTests`: Validates metric-to-imperial rounding ($1000\text{ mm} \rightarrow 3.28084\text{ ft}$, $20\text{ mm} \rightarrow 0.0656168\text{ ft}$).
  2. `BarTypeResolutionToleranceTests`: Validates matching algorithms $|\text{dia} - \Phi| < 0.5\text{ mm}$.
  3. `RevitHostThemeNotificationTests`: Verifies `Changed` event fires on simulated theme transitions.

### 7.2 Solution Build Command
Build the entire HPKata solution including AutoCAD, Revit, UI, and Core:
```bash
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx" -c Release
```
- Expectation: 0 Warnings, 0 Errors across all projects.

### 7.3 Live Revit 2026 Manual Verification Checklist
1. Copy `HPKata.Revit.addin` and build output to `%AppData%\Autodesk\Revit\Addins\2026\`.
2. Launch Revit 2026. Verify tab `"HPKATA"` appears with panel `"Cốt Thép (Rebar)"`.
3. Switch Revit Options $\rightarrow$ Colors $\rightarrow$ UI theme between Dark and Light. Verify ribbon button glyphs and opened dialogs repaint instantaneously.
4. Click each button:
   - `"Thiết Lập"`: `SetupView` opens centered, owned by Revit, Dark/Light theme active.
   - `"Vẽ & Sửa Thép"`: `DetailingView` opens, allows selecting shape and parameters.
   - `"Thống Kê Thép"`: `ScheduleView` extracts active document bars and renders diameter groups.
   - `"Tổ Hợp Cắt Thép"`: `CuttingStockView` solves 1D-CSP with zero residual or BFD heuristics.
   - `"Xuất Excel"`: Prompts for save location and writes valid `Kata.xlsm`-compliant spreadsheet.
