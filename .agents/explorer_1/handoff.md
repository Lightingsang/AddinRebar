# Handoff Report: Core Logic & Test Architecture for Smart Plot Pro

- **Author**: Explorer 1
- **Date**: 2026-09-20T22:26:00Z
- **Target Feature**: Smart Plot Pro (`HPAutoCad.Core/SmartPlot/` and `HPAutoCad.Tests/SmartPlot/`)
- **Status**: Hard Handoff (Investigation & Architectural Design Complete)

---

## 1. Observation

### 1.1 Project Structure & Target Frameworks
Direct observation from project files in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\`:
1. `HPAutoCad.Core/HPAutoCad.Core.csproj`:
   - Line 7: `<TargetFramework>net8.0</TargetFramework>`
   - Line 8: `<LangVersion>latest</LangVersion>`
   - Line 9: `<Nullable>enable</Nullable>`
   - Line 10: `<ImplicitUsings>enable</ImplicitUsings>`
   - Line 11: `<RootNamespace>HPAutoCad.Core</RootNamespace>`
   - Line 23: `<InternalsVisibleTo Include="HPAutoCad.Tests" />`
   - Package references: Zero. Zero references to AutoCAD APIs (`Autodesk.AutoCAD.*`), Nice3point, WPF, or native libraries. It is a pure, host-free C# library.
2. `HPAutoCad.Tests/HPAutoCad.Tests.csproj`:
   - Line 8: `<TargetFramework>net10.0-windows</TargetFramework>`
   - Line 17-19: `<OutputType>Exe</OutputType>`, `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`
   - Line 23: `<PackageReference Include="xunit.v3" Version="3.1.0" />`
   - Line 24: `<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />`
   - Line 35: `<ProjectReference Include="..\HPAutoCad.Core\HPAutoCad.Core.csproj" />`
   - Line 36: `<ProjectReference Include="..\HPAutoCad\HPAutoCad.csproj" />`
3. `HPAutoCad/global.json`:
   - Lines 7-9: `"test": { "runner": "Microsoft.Testing.Platform" }`
4. `HPAutoCad/HPAutoCad.slnx`:
   - References `HPAutoCad.Core/HPAutoCad.Core.csproj` and `HPAutoCad.Tests/HPAutoCad.Tests.csproj`.
   - Solution builds cleanly in `Debug` configuration (`dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` succeeded with 0 errors, 1 harmless ILRepack warning).
5. Existing test baseline:
   - Command `dotnet test HPAutoCad.Tests` runs 241 tests: 238 succeeded, 0 failed, 3 skipped (live provider tests requiring `HPGEO_LIVE_TILES=1`).
6. Existing settings persistence pattern:
   - Observed in `HPAutoCad/HPAutoCad/HPGeoLink/Cad/UserSettingsStore.cs`:
     - Line 16: `System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ...)`
     - Lines 18-23: `JsonSerializerOptions` with `WriteIndented = true`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, `Converters = { new JsonStringEnumConverter() }`.

---

## 2. Logic Chain

### 2.1 Separation of Concerns
1. From Observation 1.1 (`HPAutoCad.Core.csproj` is net8.0 with zero AutoCAD dependencies), all domain calculations, models, range parsing, spatial ordering algorithms, file naming logic, and preset serialization must reside exclusively inside `HPAutoCad.Core/SmartPlot/`.
2. CAD entity interaction (accessing `BlockReference`, `Polyline`, `Layout`, `PlotEngine`) will reside in `HPAutoCad/SmartPlot/Cad/`, referencing `HPAutoCad.Core.SmartPlot` models.
3. PDF merging via `PdfSharp` belongs in `HPAutoCad/SmartPlot/Pdf/` (or a dedicated PDF service in `HPAutoCad`), not in `HPAutoCad.Core`, preserving `HPAutoCad.Core` as zero-dependency.

### 2.2 Domain Models & Enums Architecture
From requirements R1 (ORIGINAL_REQUEST.md lines 205–216):
- **Enums**:
  - `FrameSourceType`: `Block`, `Layer`, `Layout`
  - `OutputMode`: `SingleFiles`, `MergedPdf`
  - `OrientationMode`: `Auto`, `Portrait`, `Landscape`
- **Models**:
  - `PlotBounds`: Dedicated readonly record struct holding `(double MinX, double MinY, double MaxX, double MaxY)`. Provides calculated properties `Width`, `Height`, `CenterX`, `CenterY`, `IsLandscape`, `IsValid`, and helper methods `VerticalOverlap(other)` and `OverlapsVertically(other, threshold)`.
  - `PlotItem`: Sealed record representing an individual printable frame. Contains `Id`, `Bounds`, `LayoutName`, `DisplayName`, `AttributeValue`, `SheetNumber`, `SheetTitle`, `Order`, `Rotation`, `IsSelected`, `SourceHandle`, `OutputFileName`.
  - `PlotConfiguration`: Parameters for plot operation (printer, paper size, plot style, orientation, output mode, tokens, etc.).
  - `PlotPreset`: Named preset wrapper for `PlotConfiguration`.
  - `PlotPresetCollection`: Root document structure serialized to JSON.
  - `PlotResult`: Immutable execution outcome with counts, file paths, error messages, and elapsed time.

### 2.3 `PlotOrderService` & Spatial Tolerance Band Algorithm
1. **Problem**: Real-world CAD drawings often have drawing frames on the same visual row that are slightly offset in the Y axis (e.g. 5mm or 50mm offset due to manual placement or title block variations), or arranged in zic-zac / staggered patterns. A naive sort `OrderByDescending(y).ThenBy(x)` scrambles the order because any minute Y difference splits frames into different levels.
2. **Algorithm**:
   - Primary Sort: Sort frames by `MaxY` descending.
   - Dynamic Row Clustering: Group frames into row bands using vertical overlap. Two frames (or a frame and a row band) belong to the same row if their vertical intersection `[Max(MinY1, MinY2), Min(MaxY1, MaxY2)]` covers at least `overlapRatioThreshold` (default `50%` or `0.5`) of the smaller frame's height:
     $$\text{Overlap} = \max(0, \min(Y_{\max 1}, Y_{\max 2}) - \max(Y_{\min 1}, Y_{\min 2}))$$
     $$\text{OverlapRatio} = \frac{\text{Overlap}}{\min(H_1, H_2)} \ge 0.5$$
   - Row Sorting: Sort row clusters Top to Bottom by their cluster `CenterY` (or `MaxY`) descending.
   - In-Row Sorting: Within each row cluster, sort items Left to Right by `MinX` ascending (with tie-breaker `MaxY` descending).
   - Re-indexing: Assign sequential 1-based `Order` to each `PlotItem`.

### 2.4 `LayoutRangeParser` Safe Parsing & Error Immunity
1. **Inputs**: Supports `"All"`, `"*"` (full count), comma/semicolon separation (`"1,3,5"`, `"1;3;5"`), range spans (`"1-5"`), inverted ranges (`"5-1"` normalized to `1..5`), and combined syntax (`"1-3,5,8-10"`).
2. **Fault Tolerance**:
   - Zero exceptions: Uses `int.TryParse` exclusively; never calls `int.Parse`.
   - Malformed tokens (`"abc"`, `",,,,"`, `"1--5"`, `"-5"`, `"!@#"`) are silently skipped without disrupting valid parsed segments.
   - Bounded execution: Hard cap (e.g. 10,000 items) prevents memory exhaustion from malicious ranges like `"1-2000000000"`.
   - Distinct and sorted: Returns unique, ascending 1-based integers clamped to `[1, maxCount]`.

### 2.5 `FileNameService` Sanitization & Template Tokens
1. **Tokens**: Replaces `{Prefix}`, `{Layout}`, `{Title}`, `{SheetNo}`, `{Order}`, `{DwgName}`, `{Date}`.
2. **Sanitization**:
   - Replaces characters from `Path.GetInvalidFileNameChars()` (`\ / : * ? " < > |` and control chars) with `_`.
   - Trims whitespace, periods, and leading/trailing underscores.
   - Guard against Windows reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`).
   - If empty after sanitization, falls back to `Plot_Sheet_{Order}`.

### 2.6 `PresetService` JSON Storage & Resilience
1. **Location**: `%AppData%\HPAutoCad\SmartPlot\presets.json`.
2. **Testability**: Constructor `PresetService(string? customFilePath = null)` allows passing an isolated file path for unit testing without mutating real user data.
3. **Resilience**:
   - Creates directory if missing.
   - If file missing or corrupted: catches `JsonException` / `IOException` and returns default built-in presets (e.g. "A1 Monochrome PDF", "A1 Monochrome Merged", "A3 Color PDF") rather than crashing AutoCAD.
   - Saves atomically to prevent partial writes during unexpected exits.

---

## 3. Caveats

1. **AutoCAD & WPF Layers are Out of Scope for Explorer 1**: CAD-specific implementations (`BlockFrameProvider`, `LayerFrameProvider`, `LayoutFrameProvider`, `AutoCadPlotEngine`, `SmartPlotWindow`, `SmartPlotViewModel`) belong to downstream feature developers. The designs here ensure that those layers have complete, decoupled domain contracts to consume.
2. **PdfSharp v6.x Package**: Must be added to `HPAutoCad/HPAutoCad.csproj` by the CAD/PDF implementer; it is intentionally **not** added to `HPAutoCad.Core.csproj` to maintain pure host-free architecture.
3. **Drawing Scale**: CAD coordinates in ModelSpace are in drawing units (mm, m, inches). The `PlotOrderService` algorithm operates purely on geometric coordinate ratios (relative overlap), making it completely unit-agnostic.

---

## 4. Conclusion & Complete Architectural Specifications

The following C# files are fully designed and ready for the implementer to create.

### 4.1 Target File Layout in `HPAutoCad.Core/SmartPlot/`

```
HPAutoCad.Core/SmartPlot/
├── Models/
│   ├── FrameSourceType.cs
│   ├── OutputMode.cs
│   ├── OrientationMode.cs
│   ├── PlotBounds.cs
│   ├── PlotItem.cs
│   ├── PlotConfiguration.cs
│   ├── PlotPreset.cs
│   ├── PlotPresetCollection.cs
│   └── PlotResult.cs
└── Services/
    ├── IPlotOrderService.cs
    ├── PlotOrderService.cs
    ├── LayoutRangeParser.cs
    ├── IFileNameService.cs
    ├── FileNameService.cs
    ├── IPresetService.cs
    └── PresetService.cs
```

---

### 4.2 C# Source Specifications for Core Components

#### `Models/FrameSourceType.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Specifies the mechanism used to detect and extract plot frames from the drawing.
/// </summary>
public enum FrameSourceType
{
    /// <summary>Detect frames by block reference insertion (including dynamic blocks and attribute evaluation).</summary>
    Block,

    /// <summary>Detect frames by closed polylines residing on a specified CAD layer.</summary>
    Layer,

    /// <summary>Detect frames from PaperSpace Layout tabs according to tab order and range filters.</summary>
    Layout
}
```

#### `Models/OutputMode.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Specifies how plotted PDF pages are output.
/// </summary>
public enum OutputMode
{
    /// <summary>Each frame is exported to an individual PDF file.</summary>
    SingleFiles,

    /// <summary>All plotted frames are merged into a single multi-page PDF document.</summary>
    MergedPdf
}
```

#### `Models/OrientationMode.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Specifies plot page orientation mode.
/// </summary>
public enum OrientationMode
{
    /// <summary>Automatically detect Portrait or Landscape based on the frame aspect ratio (width vs height).</summary>
    Auto,

    /// <summary>Force Portrait orientation.</summary>
    Portrait,

    /// <summary>Force Landscape orientation.</summary>
    Landscape
}
```

#### `Models/PlotBounds.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// 2D axis-aligned bounding box of a frame in CAD coordinates.
/// </summary>
public readonly record struct PlotBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => Math.Max(0.0, MaxX - MinX);
    public double Height => Math.Max(0.0, MaxY - MinY);
    public double CenterX => (MinX + MaxX) * 0.5;
    public double CenterY => (MinY + MaxY) * 0.5;
    public bool IsLandscape => Width >= Height;

    public bool IsValid =>
        double.IsFinite(MinX) && double.IsFinite(MinY) &&
        double.IsFinite(MaxX) && double.IsFinite(MaxY) &&
        MaxX >= MinX && MaxY >= MinY;

    /// <summary>
    /// Computes the vertical overlap height between this boundary and another boundary.
    /// </summary>
    public double VerticalOverlap(PlotBounds other)
    {
        var top = Math.Min(MaxY, other.MaxY);
        var bottom = Math.Max(MinY, other.MinY);
        return Math.Max(0.0, top - bottom);
    }

    /// <summary>
    /// Determines whether this boundary shares a significant vertical overlap band with another boundary.
    /// </summary>
    public bool OverlapsVertically(PlotBounds other, double ratioThreshold = 0.5)
    {
        var overlap = VerticalOverlap(other);
        var minHeight = Math.Min(Height, other.Height);
        if (minHeight <= 1e-6) return Math.Abs(CenterY - other.CenterY) < 1.0;
        return (overlap / minHeight) >= ratioThreshold;
    }
}
```

#### `Models/PlotItem.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Represents an individual plot frame detected in a drawing.
/// </summary>
public sealed record PlotItem
{
    public required string Id { get; init; }
    public required PlotBounds Bounds { get; init; }
    public string LayoutName { get; init; } = "Model";
    public string DisplayName { get; init; } = string.Empty;
    public string? AttributeValue { get; init; }
    public string? SheetNumber { get; init; }
    public string? SheetTitle { get; init; }
    public int Order { get; init; } = 1;
    public double Rotation { get; init; } = 0.0;
    public bool IsSelected { get; init; } = true;
    public string? SourceHandle { get; init; }
    public string? OutputFileName { get; init; }

    // Convenience delegates to Bounds
    public double MinX => Bounds.MinX;
    public double MinY => Bounds.MinY;
    public double MaxX => Bounds.MaxX;
    public double MaxY => Bounds.MaxY;
    public double Width => Bounds.Width;
    public double Height => Bounds.Height;
    public bool IsLandscape => Bounds.IsLandscape;
}
```

#### `Models/PlotConfiguration.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Models;

/// <summary>
/// Full parameter configuration for a plot operation or session.
/// </summary>
public sealed record PlotConfiguration
{
    public string DeviceName { get; init; } = "AutoCAD PDF (General Documentation).pc3";
    public string MediaName { get; init; } = "ISO_full_bleed_A1_(841.00_x_594.00_MM)";
    public string PlotStyle { get; init; } = "monochrome.ctb";
    public OrientationMode Orientation { get; init; } = OrientationMode.Auto;
    public OutputMode OutputMode { get; init; } = OutputMode.SingleFiles;
    public string OutputFolder { get; init; } = string.Empty;
    public string FileNamePattern { get; init; } = "{Prefix}_{Layout}_{SheetNo}_{Title}";
    public string FileNamePrefix { get; init; } = "HP";
    public string MergedFileName { get; init; } = "MergedPlot.pdf";
    public FrameSourceType FrameSource { get; init; } = FrameSourceType.Block;
    public string BlockName { get; init; } = string.Empty;
    public string SheetNumberAttribute { get; init; } = "SOHIEU";
    public string SheetTitleAttribute { get; init; } = "TENTIEUDE";
    public string LayerName { get; init; } = string.Empty;
    public string LayoutRange { get; init; } = "All";
    public bool CenterPlot { get; init; } = true;
    public bool FitToPaper { get; init; } = true;
    public double CustomScaleNumerator { get; init; } = 1.0;
    public double CustomScaleDenominator { get; init; } = 1.0;
    public double ToleranceBandYRatio { get; init; } = 0.5;
}
```

#### `Models/PlotPreset.cs` & `PlotPresetCollection.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Models;

public sealed record PlotPreset
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public PlotConfiguration Config { get; init; } = new();
}

public sealed record PlotPresetCollection
{
    public int Version { get; init; } = 1;
    public string? DefaultPresetName { get; init; }
    public List<PlotPreset> Presets { get; init; } = [];
}
```

#### `Models/PlotResult.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Models;

public sealed record PlotResult
{
    public bool Success { get; init; }
    public int TotalSheets { get; init; }
    public int PlottedSheets { get; init; }
    public int FailedSheets { get; init; }
    public IReadOnlyList<string> OutputFilePaths { get; init; } = [];
    public string? MergedPdfPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public TimeSpan ElapsedTime { get; init; } = TimeSpan.Zero;

    public static PlotResult Succeeded(IReadOnlyList<string> files, string? merged = null, TimeSpan elapsed = default) =>
        new()
        {
            Success = true,
            TotalSheets = files.Count,
            PlottedSheets = files.Count,
            FailedSheets = 0,
            OutputFilePaths = files,
            MergedPdfPath = merged,
            ElapsedTime = elapsed
        };

    public static PlotResult Failed(string error, int total = 0, int plotted = 0) =>
        new()
        {
            Success = false,
            TotalSheets = total,
            PlottedSheets = plotted,
            FailedSheets = Math.Max(0, total - plotted),
            Errors = [error]
        };
}
```

---

### 4.3 C# Source Specifications for Services

#### `Services/IPlotOrderService.cs` & `PlotOrderService.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Services;

using HPAutoCad.Core.SmartPlot.Models;

public interface IPlotOrderService
{
    /// <summary>
    /// Sorts plot frames in natural reading order: Top to Bottom, Left to Right,
    /// clustering items into row bands based on a vertical overlap tolerance ratio.
    /// </summary>
    /// <param name="items">The collection of detected plot items.</param>
    /// <param name="overlapRatioThreshold">Vertical overlap fraction (0.0 to 1.0) required to consider two frames in the same row. Default is 0.5.</param>
    /// <returns>A new ordered list of items with sequential 1-based Order values.</returns>
    IReadOnlyList<PlotItem> OrderFrames(IEnumerable<PlotItem> items, double overlapRatioThreshold = 0.5);
}

public sealed class PlotOrderService : IPlotOrderService
{
    public IReadOnlyList<PlotItem> OrderFrames(IEnumerable<PlotItem> items, double overlapRatioThreshold = 0.5)
    {
        if (items is null) return Array.Empty<PlotItem>();

        var validItems = items.Where(i => i.Bounds.IsValid).ToList();
        if (validItems.Count <= 1)
        {
            return validItems.Select((item, idx) => item with { Order = idx + 1 }).ToList();
        }

        var threshold = Math.Clamp(overlapRatioThreshold, 0.05, 0.95);

        // Sort descending by MaxY (highest top edge first), tie-breaker MinX ascending
        var sortedByTop = validItems
            .OrderByDescending(i => i.MaxY)
            .ThenBy(i => i.MinX)
            .ToList();

        var rows = new List<PlotRow>();

        foreach (var item in sortedByTop)
        {
            PlotRow? bestMatch = null;
            var bestOverlap = 0.0;

            foreach (var row in rows)
            {
                if (row.Matches(item, threshold, out var overlapRatio) && overlapRatio > bestOverlap)
                {
                    bestMatch = row;
                    bestOverlap = overlapRatio;
                }
            }

            if (bestMatch != null)
            {
                bestMatch.Add(item);
            }
            else
            {
                rows.Add(new PlotRow(item));
            }
        }

        // Sort rows Top to Bottom (descending CenterY)
        var orderedRows = rows.OrderByDescending(r => r.CenterY).ToList();

        var result = new List<PlotItem>(validItems.Count);
        var currentOrder = 1;

        foreach (var row in orderedRows)
        {
            // Within each row, sort Left to Right (ascending MinX)
            var sortedInRow = row.Items
                .OrderBy(i => i.MinX)
                .ThenByDescending(i => i.MaxY);

            foreach (var item in sortedInRow)
            {
                result.Add(item with { Order = currentOrder++ });
            }
        }

        return result;
    }

    private sealed class PlotRow
    {
        public double MinY { get; private set; }
        public double MaxY { get; private set; }
        public List<PlotItem> Items { get; } = [];

        public PlotRow(PlotItem firstItem)
        {
            MinY = firstItem.MinY;
            MaxY = firstItem.MaxY;
            Items.Add(firstItem);
        }

        public double Height => Math.Max(0.0, MaxY - MinY);
        public double CenterY => (MinY + MaxY) * 0.5;

        public bool Matches(PlotItem item, double threshold, out double overlapRatio)
        {
            var overlap = Math.Max(0.0, Math.Min(MaxY, item.MaxY) - Math.Max(MinY, item.MinY));
            var minHeight = Math.Min(Height, item.Height);

            if (minHeight <= 1e-6)
            {
                overlapRatio = Math.Abs(CenterY - item.Bounds.CenterY) < 1.0 ? 1.0 : 0.0;
                return overlapRatio > 0;
            }

            overlapRatio = overlap / minHeight;
            return overlapRatio >= threshold;
        }

        public void Add(PlotItem item)
        {
            Items.Add(item);
            MinY = Math.Min(MinY, item.MinY);
            MaxY = Math.Max(MaxY, item.MaxY);
        }
    }
}
```

#### `Services/LayoutRangeParser.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Services;

/// <summary>
/// Robust parser for sheet and layout range expressions ("All", "1-5", "1,3,5", "1-3,5,8-10").
/// Guarantees zero unhandled exceptions on malformed or garbage inputs.
/// </summary>
public static class LayoutRangeParser
{
    private const int HardCap = 10000;

    /// <summary>
    /// Parses a range text into a sorted, distinct list of 1-based indices.
    /// </summary>
    /// <param name="rangeText">The input range string, e.g. "All", "1-5", "1,3,5", "1-3,5,8-10".</param>
    /// <param name="maxCount">Maximum available count to bound "All" or open ranges.</param>
    /// <returns>A sorted list of valid 1-based indices.</returns>
    public static IReadOnlyList<int> Parse(string? rangeText, int maxCount = int.MaxValue)
    {
        if (maxCount <= 0) return Array.Empty<int>();

        var text = rangeText?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return maxCount < int.MaxValue
                ? Enumerable.Range(1, Math.Min(maxCount, HardCap)).ToList()
                : Array.Empty<int>();
        }

        if (text.Equals("all", StringComparison.OrdinalIgnoreCase) || text == "*")
        {
            return maxCount < int.MaxValue
                ? Enumerable.Range(1, Math.Min(maxCount, HardCap)).ToList()
                : Array.Empty<int>();
        }

        var result = new HashSet<int>();
        var tokens = text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            var trimmed = token.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (trimmed.Contains('-'))
            {
                var parts = trimmed.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out var start) &&
                    int.TryParse(parts[1].Trim(), out var end))
                {
                    if (start <= 0 && end <= 0) continue;

                    var min = Math.Max(1, Math.Min(start, end));
                    var max = Math.Min(Math.Max(start, end), Math.Min(maxCount, HardCap));

                    for (var i = min; i <= max; i++)
                    {
                        result.Add(i);
                    }
                }
            }
            else if (int.TryParse(trimmed, out var single) && single >= 1 && single <= maxCount)
            {
                result.Add(single);
            }
        }

        return result.OrderBy(x => x).ToList();
    }
}
```

#### `Services/IFileNameService.cs` & `FileNameService.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Services;

using System.IO;
using System.Text.RegularExpressions;
using HPAutoCad.Core.SmartPlot.Models;

public interface IFileNameService
{
    /// <summary>
    /// Formats a file name by substituting template tokens ({Prefix}, {Layout}, {SheetNo}, {Title}, {Order}, {DwgName}, {Date})
    /// and sanitizing invalid characters.
    /// </summary>
    string FormatFileName(string template, PlotItem item, string? prefix = null, string? dwgName = null);

    /// <summary>
    /// Strips or replaces characters disallowed by Windows file systems.
    /// </summary>
    string SanitizeFileName(string rawName, char replacementChar = '_');

    /// <summary>
    /// Combines an output folder and file name, ensuring valid extension.
    /// </summary>
    string BuildFullFilePath(string outputFolder, string fileNameWithoutExt, string extension = ".pdf");
}

public sealed class FileNameService : IFileNameService
{
    private static readonly HashSet<char> InvalidChars = Path.GetInvalidFileNameChars().ToHashSet();

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public string FormatFileName(string template, PlotItem item, string? prefix = null, string? dwgName = null)
    {
        var pattern = string.IsNullOrWhiteSpace(template)
            ? "{Prefix}_{Layout}_{SheetNo}_{Title}"
            : template.Trim();

        var prefixVal = prefix ?? string.Empty;
        var layoutVal = item.LayoutName ?? string.Empty;
        var sheetNoVal = item.SheetNumber ?? item.AttributeValue ?? item.Order.ToString();
        var titleVal = item.SheetTitle ?? item.DisplayName ?? string.Empty;
        var orderVal = item.Order.ToString("D2");
        var dwgVal = Path.GetFileNameWithoutExtension(dwgName) ?? string.Empty;
        var dateVal = DateTime.Now.ToString("yyyyMMdd");

        var raw = pattern
            .Replace("{Prefix}", prefixVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{Layout}", layoutVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{SheetNo}", sheetNoVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{Title}", titleVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{Order}", orderVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{DwgName}", dwgVal, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", dateVal, StringComparison.OrdinalIgnoreCase);

        var sanitized = SanitizeFileName(raw);

        // Collapse multiple consecutive underscores
        sanitized = Regex.Replace(sanitized, @"_+", "_").Trim('_', ' ', '.');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"Plot_Sheet_{item.Order}";
        }

        return sanitized;
    }

    public string SanitizeFileName(string rawName, char replacementChar = '_')
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "Plot_Sheet";

        var sb = new System.Text.StringBuilder(rawName.Length);
        foreach (var ch in rawName)
        {
            if (InvalidChars.Contains(ch) || char.IsControl(ch))
            {
                sb.Append(replacementChar);
            }
            else
            {
                sb.Append(ch);
            }
        }

        var result = sb.ToString().Trim(' ', '.');
        if (ReservedNames.Contains(result))
        {
            result = $"{replacementChar}{result}";
        }

        return string.IsNullOrWhiteSpace(result) ? "Plot_Sheet" : result;
    }

    public string BuildFullFilePath(string outputFolder, string fileNameWithoutExt, string extension = ".pdf")
    {
        var ext = extension.StartsWith('.') ? extension : $".{extension}";
        var cleanBase = SanitizeFileName(fileNameWithoutExt);
        var fullName = cleanBase.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
            ? cleanBase
            : $"{cleanBase}{ext}";

        return string.IsNullOrWhiteSpace(outputFolder)
            ? fullName
            : Path.Combine(outputFolder, fullName);
    }
}
```

#### `Services/IPresetService.cs` & `PresetService.cs`
```csharp
namespace HPAutoCad.Core.SmartPlot.Services;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Core.SmartPlot.Models;

public interface IPresetService
{
    string FilePath { get; }
    IReadOnlyList<PlotPreset> LoadPresets();
    PlotPreset GetDefaultPreset();
    PlotPreset? GetPreset(string name);
    void SavePresets(IEnumerable<PlotPreset> presets, string? defaultPresetName = null);
    void SavePreset(PlotPreset preset, bool setAsDefault = false);
    bool DeletePreset(string name);
}

public sealed class PresetService : IPresetService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; }

    public PresetService(string? customFilePath = null)
    {
        FilePath = customFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HPAutoCad",
            "SmartPlot",
            "presets.json");
    }

    public IReadOnlyList<PlotPreset> LoadPresets()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                var defaults = CreateDefaultPresets();
                SavePresets(defaults.Presets, defaults.DefaultPresetName);
                return defaults.Presets;
            }

            var json = File.ReadAllText(FilePath);
            var collection = JsonSerializer.Deserialize<PlotPresetCollection>(json, JsonOptions);

            if (collection?.Presets is { Count: > 0 } list)
            {
                return list;
            }

            return CreateDefaultPresets().Presets;
        }
        catch
        {
            // Graceful fallback on corrupted or unreadable JSON
            return CreateDefaultPresets().Presets;
        }
    }

    public PlotPreset GetDefaultPreset()
    {
        var presets = LoadPresets();
        return presets.FirstOrDefault(p => p.IsDefault) ?? presets.First();
    }

    public PlotPreset? GetPreset(string name)
    {
        return LoadPresets().FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void SavePresets(IEnumerable<PlotPreset> presets, string? defaultPresetName = null)
    {
        var list = presets.ToList();
        var defName = defaultPresetName ?? list.FirstOrDefault(p => p.IsDefault)?.Name ?? list.FirstOrDefault()?.Name;

        // Ensure default flag consistency
        var normalized = list.Select(p => p with { IsDefault = string.Equals(p.Name, defName, StringComparison.OrdinalIgnoreCase) }).ToList();

        var collection = new PlotPresetCollection
        {
            Version = 1,
            DefaultPresetName = defName,
            Presets = normalized
        };

        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(collection, JsonOptions);

        // Safe atomic write pattern
        var tempFile = $"{FilePath}.tmp";
        File.WriteAllText(tempFile, json);
        File.Move(tempFile, FilePath, overwrite: true);
    }

    public void SavePreset(PlotPreset preset, bool setAsDefault = false)
    {
        var current = LoadPresets().ToList();
        var index = current.FindIndex(p => p.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase));

        var updatedPreset = preset with { IsDefault = setAsDefault };

        if (index >= 0)
        {
            current[index] = updatedPreset;
        }
        else
        {
            current.Add(updatedPreset);
        }

        SavePresets(current, setAsDefault ? preset.Name : null);
    }

    public bool DeletePreset(string name)
    {
        var current = LoadPresets().ToList();
        var removed = current.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed)
        {
            SavePresets(current);
        }
        return removed;
    }

    private static PlotPresetCollection CreateDefaultPresets()
    {
        return new PlotPresetCollection
        {
            Version = 1,
            DefaultPresetName = "A1 Monochrome PDF",
            Presets =
            [
                new PlotPreset
                {
                    Name = "A1 Monochrome PDF",
                    Description = "Single-sheet PDF export with monochrome plot style on A1 full bleed paper.",
                    IsDefault = true,
                    Config = new PlotConfiguration
                    {
                        DeviceName = "AutoCAD PDF (General Documentation).pc3",
                        MediaName = "ISO_full_bleed_A1_(841.00_x_594.00_MM)",
                        PlotStyle = "monochrome.ctb",
                        Orientation = OrientationMode.Auto,
                        OutputMode = OutputMode.SingleFiles,
                        FitToPaper = true,
                        CenterPlot = true
                    }
                },
                new PlotPreset
                {
                    Name = "A1 Monochrome Merged",
                    Description = "Multi-page merged PDF document with monochrome plot style on A1 paper.",
                    IsDefault = false,
                    Config = new PlotConfiguration
                    {
                        DeviceName = "AutoCAD PDF (General Documentation).pc3",
                        MediaName = "ISO_full_bleed_A1_(841.00_x_594.00_MM)",
                        PlotStyle = "monochrome.ctb",
                        Orientation = OrientationMode.Auto,
                        OutputMode = OutputMode.MergedPdf,
                        MergedFileName = "PlotSet_A1.pdf",
                        FitToPaper = true,
                        CenterPlot = true
                    }
                },
                new PlotPreset
                {
                    Name = "A3 Color PDF",
                    Description = "Color PDF plotting on A3 paper for presentations and approval sets.",
                    IsDefault = false,
                    Config = new PlotConfiguration
                    {
                        DeviceName = "AutoCAD PDF (General Documentation).pc3",
                        MediaName = "ISO_full_bleed_A3_(420.00_x_297.00_MM)",
                        PlotStyle = "acad.ctb",
                        Orientation = OrientationMode.Auto,
                        OutputMode = OutputMode.SingleFiles,
                        FitToPaper = true,
                        CenterPlot = true
                    }
                }
            ]
        };
    }
}
```

---

### 4.4 Target Test Suites in `HPAutoCad.Tests/SmartPlot/`

```
HPAutoCad.Tests/SmartPlot/
├── PlotBoundsAndModelTests.cs
├── PlotOrderServiceTests.cs
├── LayoutRangeParserTests.cs
├── FileNameServiceTests.cs
└── PresetServiceTests.cs
```

#### Test Outline:
1. **`PlotOrderServiceTests.cs`**:
   - `Sort_NullOrEmpty_ReturnsEmpty()`
   - `Sort_SingleItem_ReturnsItemWithOrder1()`
   - `Sort_Grid_3x3_TopToBottomLeftToRight()`: Perfectly aligned 3x3 grid (9 frames) -> verify exact Order 1..9 matches top row L->R, middle row L->R, bottom row L->R.
   - `Sort_MisalignedRow_WithinToleranceBand_GroupsInSameRow()`: Frame 1 (Y=1000..1500), Frame 2 (Y=960..1460, X=900) -> 92% overlap, verified sorted by X into Orders 1 and 2.
   - `Sort_ZicZacStaggered_CorrectlyClustersDistinctRows()`: Row 1 staggered by 40mm (height 594mm), Row 2 separated by 300mm -> verified 2 distinct rows.
   - `Sort_VerticalColumn_OrdersTopToBottom()`: Vertical stack of frames -> verified descending Y order.
   - `Sort_HorizontalRow_OrdersLeftToRight()`: Single horizontal row -> verified ascending X order.
   - `Sort_PreservesItemProperties()`: Asserts `Id`, `DisplayName`, `SourceHandle` intact.

2. **`LayoutRangeParserTests.cs`**:
   - `Parse_EmptyOrNull_ReturnsExpected()`: `null`, `""`, `"   "`.
   - `Parse_AllKeyword_ReturnsSequential()`: `"All"`, `"all"`, `"*"` with maxCount=10 -> `[1..10]`.
   - `Parse_CommaSeparated_ReturnsDistinctSorted()`: `"5, 1, 3"` -> `[1, 3, 5]`.
   - `Parse_DashRange_ReturnsConsecutive()`: `"1-5"` -> `[1, 2, 3, 4, 5]`.
   - `Parse_InvertedRange_ReturnsAscending()`: `"5-1"` -> `[1, 2, 3, 4, 5]`.
   - `Parse_ComplexRange()`: `"1-3,5,8-10"` -> `[1, 2, 3, 5, 8, 9, 10]`.
   - `Parse_MalformedInput_ImmuneToExceptions()`: `"abc"`, `",,,,"`, `"1--5"`, `"!@#"`, `"-5"` -> returns valid parts or empty without throwing.
   - `Parse_ClampedToMaxCount()`: `"1-100"` with maxCount=5 -> `[1, 2, 3, 4, 5]`.
   - `Parse_HugeRange_ProtectedByHardCap()`: `"1-2000000000"` -> bounded without OOM.

3. **`FileNameServiceTests.cs`**:
   - `SanitizeFileName_RemovesInvalidWindowsCharacters()`: Replaces `: * ? " < > | \ /`.
   - `SanitizeFileName_ReservedNames_PrefixesUnderscore()`: `"CON"` -> `"_CON"`.
   - `FormatFileName_SubstitutesAllTokens()`: Tests `{Prefix}_{Layout}_{SheetNo}_{Title}`.
   - `FormatFileName_EmptyOrMissingTokens_CleansConsecutiveUnderscores()`: No dangling `___`.
   - `BuildFullFilePath_ValidPathWithExtension()`: Properly appends directory and `.pdf`.

4. **`PresetServiceTests.cs`**:
   - `LoadPresets_WhenFileMissing_CreatesAndReturnsDefaults()`: Verifies default 3 presets created.
   - `SavePresets_And_LoadPresets_Roundtrip()`: Custom presets persist and reload identically.
   - `SavePreset_UpdatesExistingByName()`: Updates config without duplicate entries.
   - `DeletePreset_RemovesTargetPreset()`: Verifies deletion.
   - `CorruptedJson_FallbacksGracefully()`: Malformed JSON string on disk returns defaults instead of throwing.

5. **`PlotBoundsAndModelTests.cs`**:
   - `PlotBounds_Properties_ComputedCorrectly()`: Width, Height, CenterX, CenterY, IsLandscape.
   - `PlotBounds_VerticalOverlap_CalculatesAccurately()`: Checks partial overlap, full containment, disjoint intervals.
   - `PlotResult_FactoryMethods_SetAppropriateStatus()`: Tests `Succeeded` and `Failed`.

---

## 5. Verification Method

To verify the architecture and readiness:
1. **Compilation Check**:
   ```bash
   dotnet build "HPAutoCad/HPAutoCad.slnx" -c Debug
   ```
   Must succeed with 0 errors.
2. **Unit Test Execution**:
   ```bash
   dotnet test "HPAutoCad/HPAutoCad.Tests"
   ```
   Must succeed with 0 failures (baseline 238 passing tests preserved).
3. **Downstream Implementation Verification**:
   When the coder implements the above classes and test files:
   ```bash
   dotnet test "HPAutoCad/HPAutoCad.Tests" --filter "FullyQualifiedName~SmartPlot"
   ```
   All newly created SmartPlot unit tests must pass 100%.
4. **Zero AutoCAD References in Core**:
   Inspect `HPAutoCad.Core/SmartPlot/**/*.cs` to confirm zero `using Autodesk.AutoCAD.*` statements.

---

**End of Handoff Report.**
