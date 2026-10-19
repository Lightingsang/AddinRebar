# HPKata.Revit Headless Testing & Scheduling Runner Architecture Analysis

**Agent**: `explorer_m6_3` (Revit Headless Testing & Scheduling Runner Explorer)  
**Milestone**: M6 (HPKata.Revit 2026 Add-in)  
**Date**: 2026-10-17  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_3\`  
**Target Projects**: `HPKata/HPKata.Revit/`, `HPKata/HPKata.Revit.Tests/`  
**Governance**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (§7, §8, §9)

---

## 1. Executive Summary

Milestone M6 initiates the **HPKata.Revit 2026 Add-in**, extending the dual-host structural rebar suite from AutoCAD 2026 into Autodesk Revit 2026 (.NET 8).
The critical technical hurdle in Revit add-in development is that `Autodesk.Revit.DB.Document` is a sealed native C++ wrapper with internal constructors that **cannot be instantiated or mocked** outside an active `Revit.exe` process. Without proper architectural decoupling, scheduling and cutting stock commands would be untestable headlessly, creating slow feedback loops and risky deployments.

This investigation delivers the complete architectural design for:
1. **Decoupled Document Context (`IRevitDocumentContext`) & Lightweight DTOs (`RevitRebarDto`, `RevitScheduleTableDto`)**: Completely decouples business orchestration from the Revit API.
2. **In-Memory Mock Context (`MockRevitDocumentContext`)**: Enables 100% in-memory headless testing with zero Revit dependencies at test time.
3. **Three Core Command Runners (`RevitScheduleRunner`, `RevitCuttingStockRunner`, `RevitExportExcelRunner`)**: Pure static orchestration classes accepting `IRevitDocumentContext` and driving `HPKata.Core` aggregation, optimization, and export engines.
4. **Headless Unit Test Suite (`HPKata.Revit.Tests.csproj`)**: High-performance test suite targeting `net8.0-windows`, `xunit.v3`, and Microsoft Testing Platform (`Microsoft.Testing.Platform`) with **33 comprehensive test cases** spanning unit conversions, shape adapters, parameter bridge, scheduling aggregation, 1D-CSP optimization, and ClosedXML export.
5. **Clean Code Compliance**: All classes $\le 300$ lines (Rule C2), methods $\le 50$ lines (Rule M3), 0 dead code (Rule Q-B1), 0 unannotated catches (Rule Q-B2), and zero conversion factors outside `HPKata.Revit`.

---

## 2. Decoupled Document Context Architecture

### 2.1 The Host Decoupling Problem
In Revit 2026:
- `Autodesk.Revit.DB.Document` and `Autodesk.Revit.DB.Structure.Rebar` are sealed classes with private/internal constructors.
- Direct invocation of `rebar.TotalLength`, `rebar.get_Parameter(...)`, or `FilteredElementCollector` outside `Revit.exe` throws `TypeInitializationException` or `InvalidOperationException`.
- In Milestone M5 (`HPKata.AutoCad`), `ICadDocumentContext` and `MockCadDocumentContext` solved this by isolating AutoCAD database handles and polyline entities into `CadEntityDto`.
- In Milestone M6, we establish the identical architectural symmetry for Revit via `IRevitDocumentContext`.

### 2.2 Domain DTOs (`HPKata.Revit/Abstractions/RevitDtos.cs`)

```csharp
namespace HPKata.Revit.Abstractions
{
    /// <summary>
    /// Decoupled DTO representing a Revit Structural Rebar instance.
    /// Can be populated from live Autodesk.Revit.DB.Structure.Rebar or constructed in memory for tests.
    /// All physical dimensions are strictly in metric millimeters (mm).
    /// </summary>
    public class RevitRebarDto
    {
        public long Id { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public string Partition { get; set; } = string.Empty;     // BuiltInParameter.REBAR_PARTITION (ten_ck)
        public string ScheduleMark { get; set; } = string.Empty;  // BuiltInParameter.ALL_MODEL_MARK / REBAR_NUMBER (stt)
        public string ShapeName { get; set; } = "M_00";           // e.g. "M_00", "M_T1", "M_21", "Kata_22"
        public int DiameterMm { get; set; }                       // Metric nominal diameter (e.g. 10, 16, 20)
        public int Quantity { get; set; } = 1;                    // rebar.Quantity (sl)
        public int MemberCount { get; set; } = 1;                 // Member repetition multiplier (default 1)
        public double TotalLengthMm { get; set; }                 // Total cutting length in mm
        public double SpacingMm { get; set; }                     // Center-to-center spacing in mm
        public string Comments { get; set; } = string.Empty;      // ALL_MODEL_INSTANCE_COMMENTS (notes/couplers)
        public string HookStartName { get; set; } = string.Empty; // e.g. "Standard 90 deg"
        public string HookEndName { get; set; } = string.Empty;   // e.g. "Standard 135 deg"
        public double[] SegmentLengthsMm { get; set; } = [];      // Segment parameters A, B, C, D, E, F in mm
        public double BendingAngleDeg { get; set; }               // Special bend angle if any
        public string HostCategory { get; set; } = string.Empty;  // e.g. "Structural Framing"
        public string HostMark { get; set; } = string.Empty;      // e.g. "B101", "C1"
    }

    /// <summary>
    /// Tabular DTO representing schedule data to be displayed in WPF views, inserted into Revit, or exported.
    /// </summary>
    public class RevitScheduleTableDto
    {
        public string Title { get; set; } = string.Empty;
        public List<string> ColumnHeaders { get; } = new();
        public List<RevitScheduleRowDto> Rows { get; } = new();

        public void AddRow(params string[] cells) => Rows.Add(new RevitScheduleRowDto(cells));
    }

    public class RevitScheduleRowDto
    {
        public List<string> Cells { get; } = new();
        public RevitScheduleRowDto() { }
        public RevitScheduleRowDto(IEnumerable<string> cells) => Cells.AddRange(cells);
    }
}
```

### 2.3 The Context Interface (`HPKata.Revit/Abstractions/IRevitDocumentContext.cs`)

```csharp
using System.Collections.Generic;

namespace HPKata.Revit.Abstractions
{
    /// <summary>
    /// Decoupled interface representing active Revit document state, rebar queries, and notifications.
    /// Enables 100% headless execution of scheduling and cutting stock commands.
    /// </summary>
    public interface IRevitDocumentContext
    {
        string DocumentTitle { get; }
        string DocumentPath { get; }
        string ActiveViewName { get; }

        IReadOnlyList<RevitRebarDto> GetAllRebars();
        IReadOnlyList<RevitRebarDto> GetSelectedRebars();

        void WriteNotification(string message);
        void ShowError(string title, string message);

        void CreateOrUpdateScheduleTable(RevitScheduleTableDto scheduleTable);
        void HighlightRebars(IEnumerable<long> elementIds);
        void ClearHighlights();
    }
}
```

### 2.4 The In-Memory Mock (`HPKata.Revit.Tests/Mocks/MockRevitDocumentContext.cs`)

```csharp
using System;
using System.Collections.Generic;
using HPKata.Revit.Abstractions;

namespace HPKata.Revit.Tests.Mocks
{
    /// <summary>
    /// In-memory mock implementing IRevitDocumentContext for fast, isolated, headless unit testing.
    /// Zero dependencies on RevitAPI.dll or live Revit processes.
    /// </summary>
    public class MockRevitDocumentContext : IRevitDocumentContext
    {
        public string DocumentTitle { get; set; } = "Floor2_Structure.rvt";
        public string DocumentPath { get; set; } = @"C:\Projects\BIM\Floor2_Structure.rvt";
        public string ActiveViewName { get; set; } = "{3D - Rebar View}";

        public List<RevitRebarDto> Rebars { get; } = new();
        public List<RevitRebarDto> SelectedRebars { get; } = new();
        public List<string> Notifications { get; } = new();
        public List<(string Title, string Message)> Errors { get; } = new();
        public List<RevitScheduleTableDto> InsertedTables { get; } = new();
        public HashSet<long> HighlightedElementIds { get; } = new();

        public IReadOnlyList<RevitRebarDto> GetAllRebars() => Rebars;
        public IReadOnlyList<RevitRebarDto> GetSelectedRebars() =>
            SelectedRebars.Count > 0 ? SelectedRebars : Rebars;

        public void WriteNotification(string message) => Notifications.Add(message);
        public void ShowError(string title, string message) => Errors.Add((title, message));

        public void CreateOrUpdateScheduleTable(RevitScheduleTableDto scheduleTable)
        {
            if (scheduleTable == null) throw new ArgumentNullException(nameof(scheduleTable));
            InsertedTables.Add(scheduleTable);
        }

        public void HighlightRebars(IEnumerable<long> elementIds)
        {
            if (elementIds == null) return;
            foreach (var id in elementIds) HighlightedElementIds.Add(id);
        }

        public void ClearHighlights() => HighlightedElementIds.Clear();
    }
}
```

### 2.5 The Live Adapter (`HPKata.Revit/Interop/RevitDocumentContext.cs`)
Inside `HPKata.Revit.Interop`, `RevitDocumentContext` wraps `Autodesk.Revit.UI.UIDocument` and `Autodesk.Revit.DB.Document`:
- `GetAllRebars()`: Calls `FilteredElementCollector` for class `Rebar`, converts each using `RevitParameterBridge.ToRebarDto(rebar)`.
- `GetSelectedRebars()`: Reads `uidoc.Selection.GetElementIds()`, filters by `Rebar`, converts.
- `WriteNotification()`: Uses `Autodesk.Revit.UI.TaskDialog` or Serilog status bar.
- `HighlightRebars()`: Calls `uidoc.Selection.SetElementIds(...)`.

---

## 3. Revit Command Runners Design

All runners are implemented as pure static orchestration classes under `HPKata.Revit.Commands`. They depend only on `IRevitDocumentContext` and `HPKata.Core`, maintaining complete host-neutrality in domain decisions.

### 3.1 Schedule Command Runner (`RevitScheduleRunner.cs`)

```csharp
using System;
using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Interop;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// Pure command runner for Revit rebar scheduling ('tkt' equivalent).
    /// Collects rebars from IRevitDocumentContext, aggregates by Partition and Mark,
    /// and formats tabular schedule reports.
    /// </summary>
    public static class RevitScheduleRunner
    {
        public static RevitScheduleResult ExecuteSchedule(
            IRevitDocumentContext context,
            bool applyBendDeduction = false,
            bool filterSelectedOnly = false)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<RevitRebarDto> dtos = filterSelectedOnly
                ? context.GetSelectedRebars()
                : context.GetAllRebars();

            List<RebarItem> items = ExtractRebarItems(dtos);
            RebarScheduleReport report = RebarScheduleAggregator.GenerateReport(items, applyBendDeduction);

            RevitScheduleTableDto table = BuildScheduleTable(report);
            context.CreateOrUpdateScheduleTable(table);
            context.WriteNotification(
                $"[HPKata] Rebar schedule generated: {report.Items.Count} items, {report.Bins.TotalProjectWeightKg:F1} kg.");

            return new RevitScheduleResult(report, table);
        }

        public static List<RebarItem> ExtractRebarItems(IEnumerable<RevitRebarDto> dtos)
        {
            var items = new List<RebarItem>();
            if (dtos == null) return items;

            foreach (var dto in dtos)
            {
                if (dto.DiameterMm <= 0) continue;
                items.Add(RevitParameterBridge.ToRebarItem(dto));
            }
            return items;
        }

        private static RevitScheduleTableDto BuildScheduleTable(RebarScheduleReport report)
        {
            var table = new RevitScheduleTableDto { Title = "BẢNG THỐNG KÊ CỐT THÉP (REVIT)" };
            table.ColumnHeaders.AddRange(new[]
            {
                "STT", "CẤU KIỆN", "SỐ HIỆU", "HÌNH DẠNG", "ĐK Ø", "DÀI 1 THANH",
                "SỐ CK", "SL 1 CK", "TỔNG SL", "TỔNG DÀI (m)", "TRỌNG LƯỢNG (kg)"
            });

            int rowIdx = 1;
            foreach (RebarItem item in report.Items)
            {
                table.AddRow(
                    rowIdx++.ToString(),
                    item.ComponentMark,
                    item.BarMark,
                    item.ShapeCode,
                    $"Ø{item.DiameterMm}",
                    item.CuttingLengthMm.ToString("F0"),
                    item.MemberCount.ToString(),
                    item.CountPerMember.ToString(),
                    item.TotalCount.ToString(),
                    item.TotalLengthM.ToString("F2"),
                    item.TotalWeightKg.ToString("F1")
                );
            }
            return table;
        }
    }

    public readonly record struct RevitScheduleResult(
        RebarScheduleReport Report,
        RevitScheduleTableDto Table);
}
```

### 3.2 1D Cutting-Stock Runner (`RevitCuttingStockRunner.cs`)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Revit.Abstractions;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// Pure command runner for 1D Cutting-Stock Optimization ('tohopthep' equivalent in Revit).
    /// Extracts bar demands from IRevitDocumentContext and optimizes cuts against 11,700 mm stock bars.
    /// </summary>
    public static class RevitCuttingStockRunner
    {
        public static RevitCuttingStockResult ExecuteCuttingStock(
            IRevitDocumentContext context,
            CuttingStockEngine? customEngine = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<RevitRebarDto> dtos = context.GetAllRebars();
            List<RebarItem> items = RevitScheduleRunner.ExtractRebarItems(dtos);

            var eligibleItems = items
                .Where(i => i.DiameterMm >= 10 && i.CuttingLengthMm > 0.0)
                .GroupBy(i => i.DiameterMm)
                .OrderBy(g => g.Key);

            var engine = customEngine ?? new CuttingStockEngine { StandardStockLengthMm = 11700.0, MinRemnantLengthMm = 500.0 };
            var results = new List<CuttingStockResult>();

            foreach (var group in eligibleItems)
            {
                var demands = group.Select(i => new CuttingDemand(
                    i.DiameterMm,
                    i.CuttingLengthMm,
                    i.TotalCount,
                    i.BarMark,
                    i.ComponentMark)).ToList();

                CuttingStockResult res = engine.Solve(group.Key, demands);
                results.Add(res);
            }

            if (results.Count == 0)
            {
                context.WriteNotification("[HPKata] Không tìm thấy thanh thép >= Ø10 để tổ hợp cắt.");
                return new RevitCuttingStockResult(results, new RevitScheduleTableDto());
            }

            RevitScheduleTableDto table = BuildCuttingTable(results);
            context.CreateOrUpdateScheduleTable(table);

            int totalBars = results.Sum(r => r.NewStandardBarsCount);
            double avgWaste = results.Count > 0 ? results.Average(r => r.GrossWastePercent) : 0.0;
            context.WriteNotification($"[HPKata] Tổ hợp cắt thép: {totalBars} cây 11.7m, hao hụt TB: {avgWaste:F2}%.");

            return new RevitCuttingStockResult(results, table);
        }

        private static RevitScheduleTableDto BuildCuttingTable(IReadOnlyList<CuttingStockResult> results)
        {
            var table = new RevitScheduleTableDto { Title = "TỔ HỢP CẮT THÉP (1D-CSP 11,700 mm)" };
            table.ColumnHeaders.AddRange(new[]
            {
                "Mẫu số", "Đường kính Ø", "Chiều dài phôi", "Các đoạn cắt (mm)", "Dài sử dụng", "Đoạn thừa", "Số thanh"
            });

            int patternIdx = 1;
            foreach (var res in results)
            {
                var groupedPatterns = res.Patterns
                    .GroupBy(p => string.Join("+", p.CutsMm.OrderByDescending(c => c).Select(c => c.ToString("F0"))));

                foreach (var g in groupedPatterns)
                {
                    var sample = g.First();
                    string cutsStr = string.Join(" + ", sample.CutsMm.Select(c => c.ToString("F0")));
                    table.AddRow(
                        patternIdx++.ToString(),
                        $"Ø{res.DiameterMm}",
                        $"{sample.StockLengthMm:F0}",
                        cutsStr,
                        $"{sample.UsedLengthMm:F0}",
                        $"{sample.RemnantLengthMm:F0}",
                        g.Count().ToString()
                    );
                }
            }
            return table;
        }
    }

    public readonly record struct RevitCuttingStockResult(
        IReadOnlyList<CuttingStockResult> Results,
        RevitScheduleTableDto Table);
}
```

### 3.3 ClosedXML Headless Excel Export Runner (`RevitExportExcelRunner.cs`)

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Revit.Abstractions;

namespace HPKata.Revit.Commands
{
    /// <summary>
    /// Pure command runner for headless ClosedXML Excel schedule export ('toexcel' equivalent in Revit).
    /// </summary>
    public static class RevitExportExcelRunner
    {
        public static string ExecuteExcelExport(
            IRevitDocumentContext context,
            string? overridePath = null,
            bool applyBendDeduction = false)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<RevitRebarDto> dtos = context.GetAllRebars();
            List<RebarItem> items = RevitScheduleRunner.ExtractRebarItems(dtos);
            RebarScheduleReport report = RebarScheduleAggregator.GenerateReport(items, applyBendDeduction);

            string targetPath = overridePath ?? ResolveDefaultExportPath(context);
            ClosedXmlExporter.ExportScheduleToFile(report, targetPath);

            context.WriteNotification($"[HPKata] Xuất bảng thống kê thành công: {targetPath}");
            return targetPath;
        }

        private static string ResolveDefaultExportPath(IRevitDocumentContext context)
        {
            string dir = Path.GetDirectoryName(context.DocumentPath) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                dir = Path.GetTempPath();
            }

            string baseName = Path.GetFileNameWithoutExtension(context.DocumentTitle);
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "RevitModel";

            return Path.Combine(dir, $"{baseName}_ThongKeThep.xlsx");
        }
    }
}
```

---

## 4. Parameter Bridge, Shape Adapters & Unit Converter Alignment

To ensure seamless integration with the artifacts designed by `explorer_m6_2`:

### 4.1 Unit Converter Quarantine (`HPKata.Revit/Interop/RevitUnitConverter.cs`)
- Enforces Rule: **ZERO feet-to-mm conversion factors outside `HPKata.Revit`**.
- Scale factor: $1\text{ ft} = 304.8\text{ mm}$ exactly.
- Methods:
  - `FeetToMm(double feet) => feet * 304.8`
  - `MmToFeet(double mm) => mm / 304.8`
  - `Points/Vectors conversion`:
    - `ToRevitXyz(double xMm, double yMm, double zMm)`
    - `FromRevitXyz(XYZ xyz) => (xyz.X * 304.8, xyz.Y * 304.8, xyz.Z * 304.8)`
  - Tolerances: epsilon $\varepsilon = 10^{-5}\text{ ft}$ ($\approx 0.003\text{ mm}$).

### 4.2 Parameter Bridge (`HPKata.Revit/Interop/RevitParameterBridge.cs`)
Maps bidirectional parameters between `RevitRebarDto` and `HPKata.Core.Models.RebarItem`:
- Member / Partition: `dto.Partition` (or fallback to `dto.HostMark`) $\leftrightarrow$ `RebarItem.ComponentMark`
- Rebar Mark: `dto.ScheduleMark` $\leftrightarrow$ `RebarItem.BarMark`
- Shape Code: `RevitShapeAdapter.ResolveKataShapeCode(dto.ShapeName)` $\leftrightarrow$ `RebarItem.ShapeCode`
- Diameter: `dto.DiameterMm` $\leftrightarrow$ `RebarItem.DiameterMm`
- Quantity: `dto.Quantity` $\leftrightarrow$ `RebarItem.CountPerMember`
- Member Count: `dto.MemberCount` $\leftrightarrow$ `RebarItem.MemberCount`
- Cutting Length: `dto.TotalLengthMm` $\leftrightarrow$ `RebarItem.CuttingLengthMm`
- Spacing: `dto.SpacingMm > 0 ? $"@{dto.SpacingMm:F0}" : ""` $\leftrightarrow$ `RebarItem.Spacing`
- Comments / Coupler Notes: `dto.Comments` $\leftrightarrow$ `RebarItem.Notes`
- Segments: `dto.SegmentLengthsMm` $\leftrightarrow$ `RebarItem.ParametersMm`
- Unit Weight: `RebarCalculator.GetUnitMass(dto.DiameterMm)` $\leftrightarrow$ `RebarItem.UnitMassKgPerM`

### 4.3 Rebar Shape Adapter (`HPKata.Revit/Interop/RevitShapeAdapter.cs`)
Bidirectional lookup between Revit standard shape names and Kata 85 shapes:
- `M_00` / `00` / `RebarShape 00` $\leftrightarrow$ `dang01` (Straight Main Bar)
- `M_02` / `11` $\leftrightarrow$ `dang03` (L-Bar / Square Corner)
- `M_11` / `21` $\leftrightarrow$ `dang02` (Symmetric U-Bar)
- `M_21` + Hooks $\leftrightarrow$ `dang02a` (Symmetric U-Bar with 135° Hooks)
- `M_26` / `26` $\leftrightarrow$ `dang05` (Z-Bar / Step Bar)
- `M_T1` / `51` $\leftrightarrow$ `dang22` (Standard Closed Stirrup 135°)
- `M_53` / `53` $\leftrightarrow$ `dang23` (Spiral Overlap Stirrup)
- `M_61` / `61` $\leftrightarrow$ `dang24` (Diamond Stirrup)
- `M_77` / `79` $\leftrightarrow$ `dang40` (Circular Hoop Rebar)
- `M_77_Spiral` $\leftrightarrow$ `dang67` (Helical Spiral)
- Default / Unknown $\leftrightarrow$ `dang00` (Generic Freeform)

---

## 5. Headless Test Architecture (`HPKata.Revit.Tests.csproj`)

### 5.1 Project Specification (`HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>HPKata.Revit.Tests</RootNamespace>
    <AssemblyName>HPKata.Revit.Tests</AssemblyName>
    <Configurations>Debug;Release</Configurations>
    <IsPackable>false</IsPackable>
    <!-- global.json pins test.runner = Microsoft.Testing.Platform; xunit.v3 speaks it natively -->
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.1.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="ClosedXML" Version="0.104.2" />
    <PackageReference Include="QRCoder" Version="1.6.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HPKata.Core\HPKata.Core.csproj" />
    <ProjectReference Include="..\HPKata.UI\HPKata.UI.csproj" />
    <ProjectReference Include="..\HPKata.Revit\HPKata.Revit.csproj" />
  </ItemGroup>

</Project>
```

### 5.2 Test Inventory (33 Comprehensive Unit Tests)

The test suite is structured into 6 focused test classes:

| # | Test Class | Test Method Name | Scenario & Assertion | Target Quality Metric |
|---|---|---|---|---|
| **1** | `RevitUnitConverterTests` | `FeetToMm_StandardValues_ConvertsAccurately` | Converts 1 ft, 10 ft, 0.5 ft to 304.8, 3048, 152.4 mm | Accurate within $10^{-6}$ |
| **2** | `RevitUnitConverterTests` | `MmToFeet_StandardValues_ConvertsAccurately` | Converts 304.8 mm, 11700 mm to feet | Accurate within $10^{-6}$ |
| **3** | `RevitUnitConverterTests` | `RoundTrip_FeetToMmToFeet_PreservesPrecision` | Round trip feet -> mm -> feet across 50 random values | Delta $< 10^{-9}$ |
| **4** | `RevitUnitConverterTests` | `ConvertArray_SegmentLengths_ConvertsAllElements` | Converts double[] array of feet to mm | Array length & values match |
| **5** | `RevitUnitConverterTests` | `ZeroAndNegative_Values_HandledCorrectly` | Zero returns zero, negative feet handled or clamped | No unhandled exception |
| **6** | `RevitUnitConverterTests` | `ToleranceEpsilon_Comparison_WorksWithinThreshold` | Compares close lengths within 0.001 mm | Assert tolerance passes |
| **7** | `RevitShapeAdapterTests` | `ResolveKataShapeCode_StandardRevitNames_MapsExact` | `M_00`->dang01, `M_02`->dang03, `M_11`->dang02, `M_T1`->dang22 | Exact 1:1 shape mapping |
| **8** | `RevitShapeAdapterTests` | `ResolveKataShapeCode_CaseInsensitiveAndPrefixVariants`| `"m_00"`, `"Kata_22"`, `"RebarShape 51"` resolve correctly | Case-insensitive robust |
| **9** | `RevitShapeAdapterTests` | `ResolveKataShapeCode_UnknownName_FallsBackToDang00` | `"Custom_Shape_X"` returns `"dang00"` | Safe fallback, 0 crash |
| **10**| `RevitShapeAdapterTests` | `ReverseMap_KataDangToRevitShapeName_ReturnsStandard` | `dang01`->M_00, `dang22`->M_T1 | Bidirectional parity |
| **11**| `RevitShapeAdapterTests` | `ExtractSegmentParametersMm_FromDimensions_MapsArray`| Maps segment parameter dictionary (A, B, C) to double[] | Exact order & mm conversion |
| **12**| `RevitShapeAdapterTests` | `CalculateCuttingLength_ClosedStirrup_MatchesFormula`| Compares Revit shape dims with `dang22` cutting length | Matches catalog within 1mm |
| **13**| `RevitParameterBridgeTests`| `ToRebarItem_FullDto_MapsAllPropertiesAccurately` | Full DTO with Partition, Mark, Dia, Spacing, Notes | 100% property equality |
| **14**| `RevitParameterBridgeTests`| `ToRebarItem_EmptyPartition_FallsBackToHostMark` | Partition is empty -> uses HostMark ("B101") | Fallback works |
| **15**| `RevitParameterBridgeTests`| `ToRebarItem_ZeroLength_CalculatesFromSegments` | TotalLengthMm is 0 -> calculates via shape catalog | Computed length $> 0$ |
| **16**| `RevitParameterBridgeTests`| `ToRebarItem_NegativeQuantity_ClampsToOne` | Quantity is -5 -> clamped to 1 | Safe boundary clamp |
| **17**| `RevitParameterBridgeTests`| `ToRebarItem_CouplerKeywordInComments_Identified` | Comments contain "Coupler ren 1 đầu" -> preserved in Notes | Coupler filter matches |
| **18**| `RevitParameterBridgeTests`| `ToRebarDto_FromRebarItem_RoundTripsEquality` | Round-trip conversion RebarItem -> Dto -> RebarItem | Equality preserved |
| **19**| `RevitScheduleRunnerTests`| `ExecuteSchedule_WithRebars_BuildsTableAndInserts` | `MockRevitDocumentContext` with 3 rebars -> generates table | 3 rows, 11 cols, 1 table |
| **20**| `RevitScheduleRunnerTests`| `ExecuteSchedule_SelectedOnly_FiltersToSelectedRebars` | Context has 5 rebars, 2 selected -> processes only 2 | Table has 2 rows |
| **21**| `RevitScheduleRunnerTests`| `ExecuteSchedule_EmptyContext_ReturnsEmptyTableGracefully`| Context has 0 rebars -> returns empty table, no exception | 0 rows, 0 crash |
| **22**| `RevitScheduleRunnerTests`| `ExecuteSchedule_MultiplePartitions_AggregatesCorrectly`| Rebars in partitions "D1", "D2", "C1" | Grouped & sorted properly |
| **23**| `RevitScheduleRunnerTests`| `ExecuteSchedule_TcvnBins_CalculatesMassBins` | Verifies $\le 10$, $10..18$, $> 18$ weight sums | Correct TCVN bin totals |
| **24**| `RevitScheduleRunnerTests`| `ExecuteSchedule_WritesNotificationToContext` | Context notifications contain item count and kg | Context.Notifications updated |
| **25**| `RevitCuttingStockRunnerTests`| `ExecuteCuttingStock_WithEligibleBars_Solves1DCSP` | Rebars $\Phi 16$ (3500mm, 5200mm) -> solves on 11.7m | Results contain patterns |
| **26**| `RevitCuttingStockRunnerTests`| `ExecuteCuttingStock_IgnoresWireCoilBars_DiaUnder10` | Rebars $\Phi 6$ and $\Phi 8$ are excluded from 11.7m cuts | Excluded from cutting stock |
| **27**| `RevitCuttingStockRunnerTests`| `ExecuteCuttingStock_NoEligibleBars_ReturnsEmptyWithNotice`| Only $\Phi 8$ in document -> returns notification | 0 patterns, notification logged |
| **28**| `RevitCuttingStockRunnerTests`| `ExecuteCuttingStock_ConservationOfMass_BalanceHolds` | Cuts + remnants == StockLength * BarCount | Zero material leak |
| **29**| `RevitCuttingStockRunnerTests`| `ExecuteCuttingStock_BuildsCuttingPatternTable` | Verifies columns and rows in pattern table | Column headers & rows valid |
| **30**| `RevitExportExcelRunnerTests` | `ExecuteExcelExport_WithRebars_GeneratesValidXlsxFile` | Exports schedule to temporary file via ClosedXML | File exists, size $> 0$ bytes |
| **31**| `RevitExportExcelRunnerTests` | `ExecuteExcelExport_CustomPath_ExportsToSpecifiedPath` | Explicit path specified -> saves to that exact path | Target path matches |
| **32**| `RevitExportExcelRunnerTests` | `ExecuteExcelExport_EmptyPath_FallsBackToTempPath` | Context has empty path -> saves to temp directory | Temp file created cleanly |
| **33**| `RevitExportExcelRunnerTests` | `ExecuteExcelExport_SpreadsheetStructure_ValidatesContent` | Opens generated xlsx with ClosedXML; checks sheets | Sheet "Thong_ke" exists, header row matches |

---

## 6. Clean Code Governance & Rule Verification

All proposed designs strictly adhere to repository rules:

| Rule ID | Rule Description | Architectural Enforcement |
|---|---|---|
| **C1** | Single Responsibility Principle | Each runner has one reason to change: `RevitScheduleRunner` (schedule), `RevitCuttingStockRunner` (1D-CSP), `RevitExportExcelRunner` (ClosedXML export). |
| **C2** | Class Length $\le 300$ lines | `IRevitDocumentContext`: ~35 lines, `RevitDtos`: ~75 lines, `MockRevitDocumentContext`: ~85 lines, `RevitScheduleRunner`: ~120 lines, `RevitCuttingStockRunner`: ~110 lines, `RevitExportExcelRunner`: ~90 lines. |
| **M3** | Method Length $\le 50$ lines | High-level orchestrating methods sequence 3–5 line private helpers (`ExtractRebarItems`, `BuildScheduleTable`, `BuildCuttingTable`, `ResolveDefaultExportPath`). |
| **M6** | Pure Functions in Domain | `HPKata.Core` engines (`RebarScheduleAggregator`, `CuttingStockEngine`, `ClosedXmlExporter`) are pure functions with 0 mutations of caller state. |
| **S5** | Dependency Inversion Principle | High-level runners depend on `IRevitDocumentContext` abstraction, never on `Autodesk.Revit.DB.Document`. |
| **D1 / D4** | Zero Host Dependencies in Core | `HPKata.Core` targets `netstandard2.0;net8.0` with 0 references to AutoCAD or Revit. Unit conversions are quarantined inside `HPKata.Revit.Interop`. |
| **Q-B1** | Zero commented-out code | Enforced across all new files. |
| **Q-B2** | Zero unannotated empty catch blocks | Enforced across all new files. |
| **T1 / T3** | Isolated, repeatable AAA tests | All 33 unit tests execute in-memory via `MockRevitDocumentContext` in $< 2$ seconds. |

---

## 7. Solution Integration Plan (`HPKata.slnx`)

When implementation proceeds in Milestone M6, `HPKata.slnx` will register the two new projects alongside M1–M5 deliverables:

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

---

## 8. Verification & Next Steps

1. **Independent Verification**:
   - The test plan in §5 defines 33 distinct unit test cases.
   - Run `dotnet test HPKata.Revit.Tests` upon implementation — expected duration $\le 3.0\text{ seconds}$ with 100% green tests.
2. **Handoff Report**:
   - Detailed in `handoff.md` with complete 5-component structure ready for implementation.
