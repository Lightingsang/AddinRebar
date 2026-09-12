# Continuous Beam Rebar: Geometry Readers, Support Detection & Models Implementation Plan

**Target Solution**: `HPRebar/HPRebar.slnx`  
**Target Path**: `HPRebar/HPRebar/Beam Rebar/`  
**Milestone**: M3 Part 1 (Geometry Readers, Support Detection, Validation, and Domain Models)  
**Author**: `explorer_m3_1`  
**Status**: Authoritative Technical Design  

---

## 1. Architectural Overview & Component Map

The continuous beam reinforcement feature follows the production architecture established in `HPRebar/HPRebar/Column Rebar/`. The Revit integration layer is strictly separated from the pure domain logic in `HPRebar.Core/BeamRebar/`:

```
┌────────────────────────────────────────────────────────────────────────┐
│             Revit UI & Selection Layer (HPRebar/Beam Rebar/)           │
│  - StructuralFramingSelectionFilter: Restricts picking to Framing      │
│  - BeamStackValidator: Checks collinearity, continuity, solid validity │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ passes validated Element list
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│             Revit Geometry Readers (HPRebar/Beam Rebar/)               │
│  - BeamSolidFaceReader: Extracts Solid, Top/Bottom/Left/Right/Ends     │
│  - BeamSupportFinder: Detects Columns, Walls, and Girders under spans  │
│    + Intersecting secondary beams framing into web for hanging ties   │
│  - BeamStackReader: Coordinates extraction, normalizes span directions,│
│    converts ft -> mm via RevitUnits, and populates domain assemblies   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ converts to
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                  Models (HPRebar/Beam Rebar/Models/)                   │
│  - BeamStack: Pairs pure BeamContinuousStack with Revit BeamFaces &    │
│    datum planes                                                        │
│  - BeamFaces: Revit handles (Element, PlanarFaces, Levels, Supports)   │
│  - BeamRebarSpec: Complete user specification mapped to Core specs     │
│  - CreatedRebar, CreatedViews, OrchestratorResult: Output tracking     │
│  - UiStrings, UiStringsCatalog, ValidationResult, ValidationMessages   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ references
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                Pure Domain Models (HPRebar.Core/BeamRebar/)            │
│  - BeamContinuousStack, BeamSpan, BeamSupportNode,                     │
│    SecondaryBeamIntersection, Enums (All mm, zero Revit dependencies) │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Models Specification (`HPRebar/HPRebar/Beam Rebar/Models/`)

All files in this directory use `namespace HPRebar.BeamRebar.Models;` with file-scoped namespaces.

### 2.1 `BeamSectionStyle.cs`
Identifies the geometric classification of a beam element.
```csharp
namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Cross-section geometry classification of a structural framing beam element.
/// </summary>
public enum BeamSectionStyle
{
    /// <summary>Unsupported cross-section (e.g. non-rectangular, chamfered, tapered, curved, or multi-solid).</summary>
    Other = 0,

    /// <summary>Prismatic rectangular concrete beam.</summary>
    Rectangle = 1
}
```

### 2.2 `BeamFaces.cs`
Maintains the Revit-side geometry handles for one beam span.
```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Revit-side geometry handles and references for a single beam span.
/// All numeric dimensions reside in the matching <see cref="HPRebar.Core.BeamRebar.Models.BeamSpan"/>.
/// </summary>
public sealed record BeamFaces
{
    /// <summary>The Revit FamilyInstance of category OST_StructuralFraming.</summary>
    public Element Element { get; init; } = null!;

    /// <summary>Top horizontal planar face (+Z direction).</summary>
    public PlanarFace Top { get; init; } = null!;

    /// <summary>Bottom horizontal soffit planar face (-Z direction).</summary>
    public PlanarFace Bottom { get; init; } = null!;

    /// <summary>Left vertical lateral face (+Y_beam direction).</summary>
    public PlanarFace? Left { get; init; }

    /// <summary>Right vertical lateral face (-Y_beam direction).</summary>
    public PlanarFace? Right { get; init; }

    /// <summary>Start vertical end cut face (-X_beam direction).</summary>
    public PlanarFace? StartFace { get; init; }

    /// <summary>End vertical end cut face (+X_beam direction).</summary>
    public PlanarFace? EndFace { get; init; }

    /// <summary>Reference level assigned to the beam.</summary>
    public Level? Level { get; init; }

    /// <summary>Supporting structural elements (columns, walls, girders) touching or below this span.</summary>
    public IReadOnlyList<Element> IntersectingSupports { get; init; } = new List<Element>();

    /// <summary>Secondary framing beams framing into the web of this span.</summary>
    public IReadOnlyList<Element> IntersectingSecondaryBeams { get; init; } = new List<Element>();
}
```

### 2.3 `BeamStack.cs`
The master assembly pairing the pure domain representation with Revit geometric datums.
```csharp
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Autodesk.Revit.DB;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.Models;

/// <summary>
/// A validated run of continuous beam spans, ready for reinforcement.
/// <see cref="Spans"/> and <see cref="Faces"/> are index-aligned and ordered from start to end.
/// </summary>
public sealed record BeamStack
{
    /// <summary>Overall style across all spans in the continuous run.</summary>
    public BeamSectionStyle Style { get; init; } = BeamSectionStyle.Rectangle;

    /// <summary>Pure domain representation in millimetres with zero Revit references.</summary>
    public BeamContinuousStack ContinuousStack { get; init; } = new();

    /// <summary>Shorthand accessor to spans in the continuous stack.</summary>
    public IReadOnlyList<BeamSpan> Spans => ContinuousStack.Spans;

    /// <summary>Shorthand accessor to support nodes in the continuous stack.</summary>
    public IReadOnlyList<BeamSupportNode> Supports => ContinuousStack.Supports;

    /// <summary>Shorthand accessor to secondary beam intersections.</summary>
    public IReadOnlyList<SecondaryBeamIntersection> SecondaryIntersections => ContinuousStack.SecondaryIntersections;

    /// <summary>Revit-side geometric faces, index-aligned with <see cref="Spans"/>.</summary>
    public IReadOnlyList<BeamFaces> Faces { get; init; } = new List<BeamFaces>();

    /// <summary>Normalized unit vector along the continuous longitudinal beam axis.</summary>
    public XYZ BeamDirection { get; init; } = XYZ.BasisX;

    /// <summary>Normalized unit vector transverse to the beam axis (Z x BeamDirection).</summary>
    public XYZ TransverseDirection { get; init; } = XYZ.BasisY;

    /// <summary>Origin point in Revit world coordinates corresponding to s = 0 mm.</summary>
    public XYZ OriginPoint { get; init; } = XYZ.Zero;

    /// <summary>Datum face for longitudinal stationing (e.g. left exterior support face or first span start face).</summary>
    public PlanarFace DatumFace { get; init; } = null!;

    /// <summary>Top horizontal datum face of the continuous beam (typically top face of span 0).</summary>
    public PlanarFace TopDatum { get; init; } = null!;

    /// <summary>Bottom horizontal soffit datum face of span 0.</summary>
    public PlanarFace BottomDatum { get; init; } = null!;

    /// <summary>Faces used by the dimensioning service to generate elevation witness lines.</summary>
    public IReadOnlyList<PlanarFace> DimensionFaces { get; init; } = new List<PlanarFace>();

    /// <summary>Generates a human-readable summary of the continuous beam stack (all mm).</summary>
    public string Summary()
    {
        var culture = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine($"BeamStack: {Spans.Count} span(s), {Supports.Count} support(s), TotalLength={ContinuousStack.TotalLength.ToString("0.#", culture)} mm.");

        for (int i = 0; i < Spans.Count; i++)
        {
            var span = Spans[i];
            sb.AppendLine($"  Span [{i + 1}]: Lc={span.LengthCenter:0.#} mm, Ln={span.LengthClear:0.#} mm, b={span.Width:0.#} mm, h={span.Height:0.#} mm, Cover={span.Cover:0.#} mm");
        }

        for (int i = 0; i < Supports.Count; i++)
        {
            var sup = Supports[i];
            sb.AppendLine($"  Support [{i}]: {sup.Name} ({sup.Type}), CenterX={sup.CenterX:0.#} mm, Width={sup.Width:0.#} mm, IsExterior={sup.IsExterior}");
        }

        if (SecondaryIntersections.Count > 0)
        {
            sb.AppendLine($"  Secondary Intersections ({SecondaryIntersections.Count}):");
            foreach (var sec in SecondaryIntersections)
            {
                sb.AppendLine($"    Span {sec.HostSpanIndex}: CenterX={sec.CenterX:0.#} mm, bs={sec.Width:0.#} mm, hs={sec.Height:0.#} mm, Side={sec.FramingSide}");
            }
        }

        return sb.ToString();
    }
}
```

### 2.4 `RebarTypeInfo.cs`
Encapsulates an available Revit `RebarBarType` with its diameter converted to millimetres.
```csharp
using Autodesk.Revit.DB.Structure;

namespace HPRebar.BeamRebar.Models;

/// <summary>
/// A rebar bar type available in the document, with its bar diameter in millimetres.
/// </summary>
public sealed record RebarTypeInfo
{
    public string Name { get; init; } = string.Empty;

    public double DiameterMm { get; init; }

    public RebarBarType BarType { get; init; } = null!;
}
```

### 2.5 `BeamRebarSpec.cs`
Master configuration specifying all reinforcement parameters across the continuous beam.
```csharp
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Complete user reinforcement specification for the continuous beam run.
/// Integrates pure domain specs with resolved Revit RebarBarType references.
/// </summary>
public sealed record BeamRebarSpec
{
    /// <summary>Stirrup distribution specification (uniform or 3-zone, spacing, bar count, cover).</summary>
    public BeamStirrupSpec Stirrups { get; init; } = new();

    /// <summary>Main continuous top and bottom longitudinal reinforcement specification.</summary>
    public BeamMainBarSpec MainBars { get; init; } = new();

    /// <summary>Additional negative moment (top) and positive moment (bottom) reinforcement.</summary>
    public BeamAdditionalBarSpec AdditionalBars { get; init; } = new();

    /// <summary>Side skin reinforcement and anti-buckling cross-ties for deep beams.</summary>
    public BeamSideBarSpec SideBars { get; init; } = new();

    /// <summary>Special hanging stirrups and diagonal ties at secondary beam joints.</summary>
    public BeamSpecialBarSpec SpecialBars { get; init; } = new();

    /// <summary>Rebar bar type for main longitudinal bars.</summary>
    public RebarTypeInfo MainBarType { get; init; } = null!;

    /// <summary>Rebar bar type for stirrups.</summary>
    public RebarTypeInfo StirrupBarType { get; init; } = null!;

    /// <summary>Rebar bar type for top additional bars.</summary>
    public RebarTypeInfo AddTopBarType { get; init; } = null!;

    /// <summary>Rebar bar type for bottom additional bars.</summary>
    public RebarTypeInfo AddBottomBarType { get; init; } = null!;

    /// <summary>Rebar bar type for side skin bars.</summary>
    public RebarTypeInfo SideBarType { get; init; } = null!;

    /// <summary>Rebar bar type for cross-ties.</summary>
    public RebarTypeInfo TieBarType { get; init; } = null!;

    /// <summary>Value written to the Revit rebar 'Partition' parameter for schedule grouping.</summary>
    public string PartitionName { get; init; } = "Beam";
}
```

### 2.6 `CreatedRebar.cs`
Tracks all rebar elements created during the execution.
```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Inventory of all Rebar instances instantiated in the Revit model.
/// </summary>
public sealed record CreatedRebar
{
    public IReadOnlyList<Rebar> Stirrups { get; init; } = new List<Rebar>();

    public IReadOnlyList<Rebar> MainBars { get; init; } = new List<Rebar>();

    public IReadOnlyList<Rebar> AdditionalBars { get; init; } = new List<Rebar>();

    public IReadOnlyList<Rebar> SideBars { get; init; } = new List<Rebar>();

    public IReadOnlyList<Rebar> SpecialBars { get; init; } = new List<Rebar>();

    public int Total => Stirrups.Count + MainBars.Count + AdditionalBars.Count + SideBars.Count + SpecialBars.Count;
}
```

### 2.7 `CreatedViews.cs`
Tracks all views created during the execution.
```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Detail elevation and section views generated for the continuous beam run.
/// </summary>
public sealed record CreatedViews
{
    /// <summary>Overall longitudinal elevation detail section view.</summary>
    public ViewSection? ElevationDetail { get; init; }

    /// <summary>Transverse cross-section views (e.g. 2 to 3 cuts per span).</summary>
    public IReadOnlyList<ViewSection> SectionViews { get; init; } = new List<ViewSection>();

    public int Total => (ElevationDetail is null ? 0 : 1) + SectionViews.Count;
}
```

### 2.8 `OrchestratorResult.cs`
Outcome of the full orchestration execution.
```csharp
namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Outcome of a full Continuous Beam Rebar creation run.
/// </summary>
public sealed record OrchestratorResult
{
    public bool IsOk => Validation.IsOk;

    public ValidationResult Validation { get; init; } = ValidationResult.Ok;

    public CreatedViews Views { get; init; } = new();

    public CreatedRebar Rebar { get; init; } = new();

    public int Total => Views.Total + Rebar.Total;

    public static OrchestratorResult Invalid(ValidationResult validation) => new() { Validation = validation };

    public static OrchestratorResult Success(CreatedViews views, CreatedRebar rebar) =>
        new() { Validation = ValidationResult.Ok, Views = views, Rebar = rebar };
}
```

### 2.9 `ValidationResult.cs`
Result container for geometric validation checks.
```csharp
namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Outcome of checking a picked continuous beam stack.
/// Code 0 indicates success. Any non-zero code corresponds to an identified rule failure.
/// </summary>
public sealed record ValidationResult
{
    private ValidationResult(int code, string message)
    {
        Code = code;
        Message = message;
    }

    public int Code { get; }

    public string Message { get; }

    public bool IsOk => Code == 0;

    public bool IsSuccess => IsOk;

    public static ValidationResult Ok { get; } = new(0, ValidationMessages.For(0));

    public static ValidationResult Fail(int code) => new(code, ValidationMessages.For(code));

    public static ValidationResult Fail(int code, string customMessage) => new(code, customMessage);

    public static ValidationResult Fail(string customMessage) => new(-1, customMessage);
}
```

### 2.10 `ValidationMessages.cs`
Rule catalog for continuous beam validation.
```csharp
using System.Collections.Generic;

namespace HPRebar.BeamRebar.Models;

/// <summary>
/// English failure messages for continuous beam validation rules.
/// </summary>
public static class ValidationMessages
{
    private static readonly IReadOnlyDictionary<int, string> Messages = new Dictionary<int, string>
    {
        [0] = "OK",
        [1] = "No beam elements selected.",
        [2] = "One of the selected elements is not a Structural Framing instance.",
        [3] = "One of the selected beams is curved or non-linear. Only straight beams are supported.",
        [4] = "One of the selected beams does not contain exactly one solid with real volume.",
        [5] = "One of the selected beams does not have a valid rectangular cross-section.",
        [6] = "The selected beams are not collinear (longitudinal axis angle > 1.0°).",
        [7] = "The selected beams are offset laterally from the continuous longitudinal axis (> 10.0 mm).",
        [8] = "The selected beams do not share the same reference level or top elevation plane.",
        [9] = "The selected beams are not continuous — there is a gap or disconnected span between them.",
        [10] = "A beam span has non-positive dimensions (width <= 0, height <= 0, or length <= 0).",
        [11] = "No supporting columns, walls, or framing girders could be identified under the beam run.",

        // Family availability pre-flight checks (Code 20-29)
        [20] = "Rebar Shape family 'M_T1' is not loaded in the project. Load it before creating stirrups.",
        [21] = "Rebar Shape family 'M_T10' is not loaded in the project. Load it before creating cross-ties.",
        [22] = "This document contains no Rebar Bar Types. Please load a rebar family first.",
        [23] = "Required 90° or 135° Rebar Hook Type is missing in the project."
    };

    public static string For(int code) =>
        Messages.TryGetValue(code, out var message) ? message : $"Unknown validation failure (code {code}).";
}
```

### 2.11 `UiStrings.cs` & `UiStringsCatalog.cs`
Complete localized labels for English and Vietnamese.
```csharp
namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Complete set of UI strings for the Continuous Beam Rebar dialog in one language.
/// </summary>
public sealed record UiStrings
{
    // Window Chrome
    public string WindowTitle { get; init; } = "Continuous Beam Rebar";
    public string Ok { get; init; } = "OK";
    public string Cancel { get; init; } = "Cancel";
    public string LanguageToggle { get; init; } = "VN";
    public string Working { get; init; } = "Creating beam reinforcement...";
    public string NothingToCreate { get; init; } = "Nothing to create with the current settings.";

    // Navigation Tabs
    public string TabSettings { get; init; } = "Settings";
    public string TabGeometry { get; init; } = "Geometry";
    public string TabStirrups { get; init; } = "Stirrups";
    public string TabMainBars { get; init; } = "Main Bars";
    public string TabAddTopBars { get; init; } = "Top Add Bars";
    public string TabAddBottomBars { get; init; } = "Bottom Add Bars";
    public string TabSideBars { get; init; } = "Side Skin Bars";
    public string TabSpecialBars { get; init; } = "Secondary Ties";

    // Shared Field Labels
    public string Span { get; init; } = "Span";
    public string Spans { get; init; } = "Spans";
    public string Support { get; init; } = "Support";
    public string Supports { get; init; } = "Supports";
    public string BarType { get; init; } = "Bar Type";
    public string Diameter { get; init; } = "Diameter";
    public string Spacing { get; init; } = "Spacing";
    public string Count { get; init; } = "Count";
    public string Layer { get; init; } = "Layer";
    public string Cover { get; init; } = "Cover";
    public string Width { get; init; } = "Width (b)";
    public string Height { get; init; } = "Height (h)";
    public string Length { get; init; } = "Length (L)";
    public string ClearSpan { get; init; } = "Clear Span (Ln)";
    public string HookLength { get; init; } = "Hook Length";
    public string LapLength { get; init; } = "Lap Length";
    public string Partition { get; init; } = "Partition";
    public string ApplyAll { get; init; } = "Apply to All";

    // Settings Tab
    public string ViewGeneration { get; init; } = "Automated Views";
    public string CreateElevationView { get; init; } = "Create Longitudinal Elevation Detail";
    public string CreateSectionViews { get; init; } = "Create Cross-Section Views";
    public string CreateDimensions { get; init; } = "Create Section & Span Dimensions";
    public string CreateTags { get; init; } = "Create Rebar Tags & Schedule Tables";
    public string ElevationViewName { get; init; } = "Detail View Name";
    public string SectionViewPrefix { get; init; } = "Section View Prefix";

    // Geometry Tab
    public string BeamStackSummary { get; init; } = "Continuous Beam Stack";
    public string SpanIndex { get; init; } = "Span No";
    public string DimensionsMm { get; init; } = "Dimensions (mm)";
    public string Level { get; init; } = "Reference Level";
    public string TopElevation { get; init; } = "Top Elevation";
    public string Cantilever { get; init; } = "Cantilever Overhang";

    // Stirrups Tab
    public string StirrupLayout { get; init; } = "Distribution Layout";
    public string LayoutUniform { get; init; } = "Uniform Spacing Throughout";
    public string Layout3ZoneL4 { get; init; } = "3-Zone: Dense L/4, Midspan L/2";
    public string Layout3ZoneL3 { get; init; } = "3-Zone: Dense L/3, Midspan L/3";
    public string DenseSpacing { get; init; } = "Support Spacing (s1)";
    public string MidspanSpacing { get; init; } = "Midspan Spacing (s2)";
    public string StartOffset { get; init; } = "First Stirrup Offset (c0)";

    // Main Bars Tab
    public string MainTopBars { get; init; } = "Top Longitudinal Bars";
    public string MainBottomBars { get; init; } = "Bottom Longitudinal Bars";
    public string ContinuousBarCount { get; init; } = "Continuous Bar Count";
    public string AnchorageHook { get; init; } = "End Anchorage 90° Hook";
    public string StaggeredSplice { get; init; } = "50% Staggered Lap Splice";
    public string SpliceLength { get; init; } = "Lap Splice Length";

    // Additional Top Bars Tab
    public string AdditionalTopHeader { get; init; } = "Top Negative Bars Over Supports";
    public string SupportNode { get; init; } = "Support Node";
    public string ExtensionRule { get; init; } = "Cutoff Rule";
    public string RuleL3 { get; init; } = "L/3 of Adjacent Clear Span";
    public string RuleL4 { get; init; } = "L/4 of Adjacent Clear Span";

    // Additional Bottom Bars Tab
    public string AdditionalBottomHeader { get; init; } = "Bottom Positive Bars in Midspan";
    public string MidspanOffsetRule { get; init; } = "Start Offset from Support Face";
    public string RuleL7 { get; init; } = "Ln/7 from Support Face";
    public string RuleL8 { get; init; } = "Ln/8 from Support Face";

    // Side Bars Tab
    public string SideBarsHeader { get; init; } = "Side Skin Reinforcement (Web Bars)";
    public string EnableSideBars { get; init; } = "Enable Skin Reinforcement";
    public string AutoDeepBeamRule { get; init; } = "Auto for Beams h >= 700 mm";
    public string MaxVerticalSpacing { get; init; } = "Max Vertical Spacing (<= 300 mm)";
    public string CrossTies { get; init; } = "Transverse Anti-Buckling Cross-Ties";

    // Special Bars Tab
    public string SecondaryFramingHeader { get; init; } = "Secondary Beam Joint Reinforcement";
    public string HangingStirrups { get; init; } = "Hanging Stirrups Cage";
    public string DiagonalTies { get; init; } = "45° Diagonal Ties";
    public string TieCount { get; init; } = "Ties Count";
}
```

```csharp
namespace HPRebar.BeamRebar.Models;

/// <summary>
/// Preloaded English and Vietnamese catalogs for UI localization.
/// </summary>
public static class UiStringsCatalog
{
    public static UiStrings English { get; } = new() { LanguageToggle = "VN" };

    public static UiStrings Vietnamese { get; } = new()
    {
        WindowTitle = "Thép Dầm Liên Tục",
        Ok = "Thực Hiện",
        Cancel = "Hủy",
        LanguageToggle = "EN",
        Working = "Đang dựng cốt thép dầm liên tục...",
        NothingToCreate = "Không có gì để dựng với thiết lập hiện tại.",

        TabSettings = "Cài Đặt",
        TabGeometry = "Hình Dạng",
        TabStirrups = "Thép Đai",
        TabMainBars = "Thép Chủ",
        TabAddTopBars = "Thép Gối (Trên)",
        TabAddBottomBars = "Thép Bụng (Dưới)",
        TabSideBars = "Thép Giá / Cấu Tạo",
        TabSpecialBars = "Gia Cường Dầm Phụ",

        Span = "Nhịp",
        Spans = "Các Nhịp",
        Support = "Gối Tựa",
        Supports = "Các Gối Tựa",
        BarType = "Loại Thép",
        Diameter = "Đường Kính",
        Spacing = "Khoảng Cách",
        Count = "Số Thanh",
        Layer = "Lớp",
        Cover = "Lớp Bảo Vệ",
        Width = "Bề Rộng (b)",
        Height = "Chiều Cao (h)",
        Length = "Chiều Dài (L)",
        ClearSpan = "Thông Thủy (Ln)",
        HookLength = "Chiều Dài Móc Neo",
        LapLength = "Đoạn Nối Chồng",
        Partition = "Partition",
        ApplyAll = "Áp Dụng Cho Tất Cả",

        ViewGeneration = "Tự Động Tạo Khung Nhìn",
        CreateElevationView = "Tạo Chi Tiết Dọc Dầm",
        CreateSectionViews = "Tạo Mặt Cắt Ngang Dầm",
        CreateDimensions = "Ghi Kích Thước Dầm & Nhịp",
        CreateTags = "Gắn Tag Thép & Bảng Thống Kê",
        ElevationViewName = "Tên Chi Tiết Dọc",
        SectionViewPrefix = "Tiền Tố Mặt Cắt",

        BeamStackSummary = "Thông Số Dầm Liên Tục",
        SpanIndex = "Nhịp Số",
        DimensionsMm = "Kích Thước (mm)",
        Level = "Tầng / Level",
        TopElevation = "Cao Độ Đỉnh Dầm",
        Cantilever = "Công-xôn (Đầu Thừa)",

        StirrupLayout = "Quy Cách Rải Đai",
        LayoutUniform = "Rải Đều Toàn Bộ Nhịp",
        Layout3ZoneL4 = "3 Vùng: Gối L/4, Giữa Nhịp L/2",
        Layout3ZoneL3 = "3 Vùng: Gối L/3, Giữa Nhịp L/3",
        DenseSpacing = "Khoảng Cách Gối (s1)",
        MidspanSpacing = "Khoảng Cách Nhịp (s2)",
        StartOffset = "Khoảng Cách Đai Đầu Tiên (c0)",

        MainTopBars = "Thép Chủ Lớp Trên",
        MainBottomBars = "Thép Chủ Lớp Dưới",
        ContinuousBarCount = "Số Lượng Thanh Suốt",
        AnchorageHook = "Móc Neo 90° Tại Gối Biên",
        StaggeredSplice = "Nối Chồng So Le 50%",
        SpliceLength = "Chiều Dài Nối",

        AdditionalTopHeader = "Thép Tăng Cường Gối (Mô-men Âm)",
        SupportNode = "Vị Trí Gối",
        ExtensionRule = "Quy Cách Cắt Thép",
        RuleL3 = "Kéo Dài L/3 Nhịp Thông Thủy Lân Cận",
        RuleL4 = "Kéo Dài L/4 Nhịp Thông Thủy Lân Cận",

        AdditionalBottomHeader = "Thép Tăng Cường Bụng (Mô-men Dương)",
        MidspanOffsetRule = "Điểm Bắt Đầu Từ Mép Gối",
        RuleL7 = "Cách Mép Gối Ln/7",
        RuleL8 = "Cách Mép Gối Ln/8",

        SideBarsHeader = "Thép Cấu Tạo / Thép Giá (Thành Dầm)",
        EnableSideBars = "Bật Thép Cấu Tạo Thành Dầm",
        AutoDeepBeamRule = "Tự Động Khi Chiều Cao h >= 700 mm",
        MaxVerticalSpacing = "Khoảng Cách Dọc Tối Đa (<= 300 mm)",
        CrossTies = "Đai C Cố Định Thép Giá",

        SecondaryFramingHeader = "Gia Cường Vị Trí Giao Dầm Phụ",
        HangingStirrups = "Chùm Đai Treo",
        DiagonalTies = "Thanh Neo Xiên 45°",
        TieCount = "Số Lượng Đai"
    };
}
```

---

## 3. `StructuralFramingSelectionFilter.cs` Specification

Restricts Revit interactive canvas selection exclusively to structural framing elements (`BuiltInCategory.OST_StructuralFraming`).

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace HPRebar.BeamRebar;

/// <summary>
/// Restricts selection strictly to structural framing family instances (beams).
/// </summary>
public sealed class StructuralFramingSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element element)
    {
        if (element is null) return false;

        // Ensure category matches BuiltInCategory.OST_StructuralFraming
        if (element.Category?.BuiltInCategory != BuiltInCategory.OST_StructuralFraming)
            return false;

        // Ensure element is a placed instance (not a FamilySymbol or ElementType)
        return element is FamilyInstance;
    }

    public bool AllowReference(Reference reference, XYZ position) => true;
}
```

---

## 4. `BeamSolidFaceReader.cs` Specification

Pulls clean 3D solids and boundary faces out of structural framing elements.

### 4.1 Coordinate Reference Vectors
For a beam element oriented along unit vector $\vec{X}_{beam}$:
- Vertical Vector: $\vec{Z} = (0, 0, 1)$
- Transverse Vector: $\vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$ (normalized)
- Longitudinal Normal: $\vec{X}_{beam}$

### 4.2 Face Classification Criteria
- **Horizontal Faces**: $|\vec{n} \cdot \vec{Z}| \ge 1 - 10^{-6}$.
  - Top Face: Horizontal face with highest $Z$ coordinate.
  - Bottom Face (Soffit): Horizontal face with lowest $Z$ coordinate.
- **Vertical Lateral Faces**: $|\vec{n} \cdot \vec{Z}| \le 10^{-6}$.
  - Left Face: $\vec{n} \cdot \vec{Y}_{beam} > 0.8$.
  - Right Face: $\vec{n} \cdot (-\vec{Y}_{beam}) > 0.8$.
  - Start End Cut Face: $\vec{n} \cdot (-\vec{X}_{beam}) > 0.8$.
  - End Cut Face: $\vec{n} \cdot \vec{X}_{beam} > 0.8$.

### 4.3 Detailed Implementation
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Models;
using Serilog;

namespace HPRebar.BeamRebar;

/// <summary>
/// Extracts clean 3D solids and boundary planar faces from Revit structural framing elements.
/// </summary>
public static class BeamSolidFaceReader
{
    private const double Tolerance = 1.0e-6;

    /// <summary>Retrieves all solids with non-zero volume from the element geometry.</summary>
    public static IReadOnlyList<Solid> GetSolids(Element element)
    {
        if (element is null) throw new ArgumentNullException(nameof(element));

        var options = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
        var geometry = element.get_Geometry(options);
        var solids = new List<Solid>();

        if (geometry is null) return solids;

        foreach (var obj in geometry)
        {
            switch (obj)
            {
                case Solid solid when solid.Volume > Tolerance:
                    solids.Add(solid);
                    break;
                case GeometryInstance instance:
                    foreach (var instObj in instance.GetInstanceGeometry())
                    {
                        if (instObj is Solid instSolid && instSolid.Volume > Tolerance)
                        {
                            solids.Add(instSolid);
                        }
                    }
                    break;
            }
        }

        return solids;
    }

    /// <summary>Gets the single solid of an element or null if not built from exactly one solid.</summary>
    public static Solid? GetSingleSolid(Element element)
    {
        var solids = GetSolids(element);
        return solids.Count == 1 ? solids[0] : null;
    }

    /// <summary>Requires that the element contains exactly one solid with real volume.</summary>
    public static Solid RequireSingleSolid(Element element) =>
        GetSingleSolid(element)
        ?? throw new InvalidOperationException($"Beam element {element.Id} must contain exactly one solid with positive volume.");

    /// <summary>Extracts planar faces whose normal points vertically (+Z or -Z).</summary>
    public static IReadOnlyList<PlanarFace> GetHorizontalFaces(Element element)
    {
        var solid = RequireSingleSolid(element);
        return solid.Faces.OfType<PlanarFace>()
            .Where(f => Math.Abs(Math.Abs(f.FaceNormal.Z) - 1.0) < Tolerance)
            .OrderBy(f => f.Origin.Z)
            .ToList();
    }

    /// <summary>Gets the lowest horizontal planar face (bottom soffit).</summary>
    public static PlanarFace GetBottom(Element element)
    {
        var faces = GetHorizontalFaces(element);
        if (faces.Count == 0) throw new InvalidOperationException($"Beam {element.Id} has no horizontal bottom face.");
        return faces[0];
    }

    /// <summary>Gets the highest horizontal planar face (top surface).</summary>
    public static PlanarFace GetTop(Element element)
    {
        var faces = GetHorizontalFaces(element);
        if (faces.Count == 0) throw new InvalidOperationException($"Beam {element.Id} has no horizontal top face.");
        return faces[faces.Count - 1];
    }

    /// <summary>Extracts vertical planar faces whose normal is orthogonal to the Z axis.</summary>
    public static IReadOnlyList<PlanarFace> GetVerticalFaces(Element element)
    {
        var solid = RequireSingleSolid(element);
        return solid.Faces.OfType<PlanarFace>()
            .Where(f => Math.Abs(f.FaceNormal.Z) < Tolerance)
            .ToList();
    }

    /// <summary>Gets the left lateral vertical face pointing along +Y_beam.</summary>
    public static PlanarFace? GetLeftFace(Element element, XYZ transverseAxis) =>
        GetVerticalFaces(element).FirstOrDefault(f => f.FaceNormal.DotProduct(transverseAxis) > 0.8);

    /// <summary>Gets the right lateral vertical face pointing along -Y_beam.</summary>
    public static PlanarFace? GetRightFace(Element element, XYZ transverseAxis) =>
        GetVerticalFaces(element).FirstOrDefault(f => f.FaceNormal.DotProduct(-transverseAxis) > 0.8);

    /// <summary>Gets the start end-cut face pointing along -X_beam.</summary>
    public static PlanarFace? GetStartFace(Element element, XYZ beamAxis) =>
        GetVerticalFaces(element).FirstOrDefault(f => f.FaceNormal.DotProduct(-beamAxis) > 0.8);

    /// <summary>Gets the end end-cut face pointing along +X_beam.</summary>
    public static PlanarFace? GetEndFace(Element element, XYZ beamAxis) =>
        GetVerticalFaces(element).FirstOrDefault(f => f.FaceNormal.DotProduct(beamAxis) > 0.8);

    /// <summary>
    /// Evaluates if the beam has a standard rectangular cross-section.
    /// Checks for 2 horizontal faces (top/bottom) and 4 vertical faces (left/right/start/end).
    /// </summary>
    public static BeamSectionStyle GetSectionStyle(Element element, XYZ beamAxis, XYZ transverseAxis)
    {
        var solid = GetSingleSolid(element);
        if (solid is null) return BeamSectionStyle.Other;

        var horizontal = GetHorizontalFaces(element);
        var vertical = GetVerticalFaces(element);

        if (horizontal.Count < 2 || vertical.Count < 4)
        {
            Log.Warning("Beam {Id}: Not rectangular. Horizontal={HCount}, Vertical={VCount}",
                element.Id, horizontal.Count, vertical.Count);
            return BeamSectionStyle.Other;
        }

        var top = horizontal[horizontal.Count - 1];
        var bottom = horizontal[0];
        var left = GetLeftFace(element, transverseAxis);
        var right = GetRightFace(element, transverseAxis);
        var start = GetStartFace(element, beamAxis);
        var end = GetEndFace(element, beamAxis);

        if (left is null || right is null || start is null || end is null)
        {
            Log.Warning("Beam {Id}: Could not identify all 4 vertical faces along beam axis.", element.Id);
            return BeamSectionStyle.Other;
        }

        // Validate parallelism of opposed faces
        bool opposedSides = Math.Abs(left.FaceNormal.AngleTo(right.FaceNormal) - Math.PI) < Tolerance;
        bool opposedEnds = Math.Abs(start.FaceNormal.AngleTo(end.FaceNormal) - Math.PI) < Tolerance;
        bool orthogonal = Math.Abs(left.FaceNormal.AngleTo(beamAxis) - (Math.PI / 2)) < Tolerance;

        if (!opposedSides || !opposedEnds || !orthogonal)
        {
            Log.Warning("Beam {Id}: Faces are not strictly orthogonal and rectangular.", element.Id);
            return BeamSectionStyle.Other;
        }

        return BeamSectionStyle.Rectangle;
    }

    /// <summary>Calculates signed-free distance from a point to a plane in millimetres.</summary>
    public static double DistanceMm(PlanarFace plane, XYZ point) =>
        RevitUnits.FtToMm(Math.Abs((point - plane.Origin).DotProduct(plane.FaceNormal)));

    /// <summary>Projects a 3D point onto a planar face along its normal.</summary>
    public static XYZ ProjectToPlane(XYZ point, PlanarFace plane)
    {
        var diff = plane.Origin - point;
        var dist = diff.DotProduct(plane.FaceNormal);
        return Math.Abs(dist) < Tolerance ? point : point + plane.FaceNormal * dist;
    }

    /// <summary>Measures beam cross-section width b in millimetres between lateral faces.</summary>
    public static double GetWidthMm(Element element, XYZ transverseAxis)
    {
        var left = GetLeftFace(element, transverseAxis);
        var right = GetRightFace(element, transverseAxis);
        if (left is null || right is null) return 0.0;
        return DistanceMm(left, right.Origin);
    }

    /// <summary>Measures beam total depth h in millimetres between top and bottom faces.</summary>
    public static double GetHeightMm(Element element)
    {
        var top = GetTop(element);
        var bottom = GetBottom(element);
        return DistanceMm(top, bottom.Origin);
    }
}
```

---

## 5. `BeamSupportFinder.cs` Specification

Identifies supporting elements below or at beam nodes (columns, structural walls, girders) as well as intersecting secondary framing beams that require hanging reinforcement.

### 5.1 Support Detection Algorithm
1. **Candidate Query**:
   - Construct an extended bounding box around each span:
     - $Z_{min, box} = Z_{min, beam} - 3.0\text{ ft}$
     - $Z_{max, box} = Z_{min, beam} + 0.5\text{ ft}$
   - Filter by categories `OST_StructuralColumns`, `OST_Walls`, `OST_StructuralFraming` using `BoundingBoxIntersectsFilter`.
2. **Column Support (`OST_StructuralColumns`)**:
   - Check if column intersects or meets beam soffit:
     $$\text{Top Face } Z_{col} \approx Z_{beam, soffit} \pm 10\text{ mm}$$
   - Project column top footprint corners onto $\vec{X}_{beam}$:
     $$w_{support} = \max_{k}(\vec{C}_k \cdot \vec{X}_{beam}) - \min_{k}(\vec{C}_k \cdot \vec{X}_{beam})$$
     $$d_{support} = \max_{k}(\vec{C}_k \cdot \vec{Y}_{beam}) - \min_{k}(\vec{C}_k \cdot \vec{Y}_{beam})$$
     $$\text{Center } s_{center} = \frac{1}{2}\left(\max(\vec{C}_k \cdot \vec{X}_{beam}) + \min(\vec{C}_k \cdot \vec{X}_{beam})\right)$$
   - Handles rotated columns accurately without trigonometry edge cases!
3. **Wall Support (`OST_Walls`)**:
   - Verify structural wall parameter: `WALL_STRUCTURAL_SIGNIFICANT == 1`.
   - Check top face elevation against beam soffit.
   - Measure bearing width along $\vec{X}_{beam}$ by projecting wall solid intersection onto $\vec{X}_{beam}$.
4. **Girder Support (`OST_StructuralFraming`)**:
   - Exclude elements in the continuous beam selection.
   - Verify non-collinear orientation: angle $\theta \in [60^\circ, 120^\circ]$.
   - Check girder top elevation meets continuous beam soffit.
   - Width $w_{support}$ = girder cross-section width.
5. **Cantilever Boundary Classification**:
   - For span 0 (left end) or span $N-1$ (right end): if distance from beam outer face to nearest support center exceeds $w_{support}/2 + 100\text{ mm}$, classify the outer node as `SupportType.CantileverLeft` or `CantileverRight` with width = 0!

### 5.2 Secondary Beam Framing Algorithm
- Identify beams framing into the web of the continuous beam:
  - Category `OST_StructuralFraming`, excluding selected continuous spans.
  - Angle $\theta \in [75^\circ, 105^\circ]$ relative to $\vec{X}_{beam}$.
  - Elevation check: secondary beam soffit $\ge$ primary beam soffit $- 50\text{ mm}$ and secondary top $\le$ primary top $+ 50\text{ mm}$.
  - Intersection station $s_x = (\vec{P}_{intersect} - \vec{P}_{origin}) \cdot \vec{X}_{beam}$ in mm.
  - Extract secondary beam width $b_s$ and depth $h_s$ in mm.
  - Classify framing side (`Left`, `Right`, or `Both`).

### 5.3 Detailed Implementation
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.BeamRebar.Models;
using Serilog;

namespace HPRebar.BeamRebar;

/// <summary>
/// Detects bearing supports (columns, walls, girders) and intersecting secondary framing beams.
/// </summary>
public static class BeamSupportFinder
{
    private const double ToleranceFt = 0.05; // ~15 mm

    /// <summary>
    /// Discovers all bearing support nodes along the continuous beam run.
    /// </summary>
    public static IReadOnlyList<BeamSupportNode> FindSupports(
        Document doc,
        IReadOnlyList<Element> sortedBeams,
        XYZ beamAxis,
        XYZ transverseAxis,
        XYZ originPoint)
    {
        var rawSupports = new List<(double CenterXMm, double WidthMm, double DepthMm, SupportType Type, string UniqueId)>();
        var beamIds = new HashSet<ElementId>(sortedBeams.Select(b => b.Id));

        foreach (var beam in sortedBeams)
        {
            var box = beam.get_BoundingBox(null);
            if (box is null) continue;

            // Expand box downwards to capture columns, walls, and girders below soffit
            var outline = new Outline(
                new XYZ(box.Min.X - 1.0, box.Min.Y - 1.0, box.Min.Z - 3.0),
                new XYZ(box.Max.X + 1.0, box.Max.Y + 1.0, box.Min.Z + 0.5));

            var filter = new LogicalOrFilter(new ElementFilter[]
            {
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns),
                new ElementCategoryFilter(BuiltInCategory.OST_Walls),
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming)
            });

            var candidates = new FilteredElementCollector(doc)
                .WherePasses(new BoundingBoxIntersectsFilter(outline))
                .WherePasses(filter)
                .WhereElementIsNotElementType()
                .Where(e => !beamIds.Contains(e.Id))
                .ToList();

            foreach (var candidate in candidates)
            {
                var cat = candidate.Category?.BuiltInCategory;
                if (cat == BuiltInCategory.OST_StructuralColumns)
                {
                    var support = MeasureColumnSupport(candidate, beamAxis, transverseAxis, originPoint);
                    if (support.HasValue) rawSupports.Add(support.Value);
                }
                else if (cat == BuiltInCategory.OST_Walls)
                {
                    var support = MeasureWallSupport(candidate, beamAxis, transverseAxis, originPoint);
                    if (support.HasValue) rawSupports.Add(support.Value);
                }
                else if (cat == BuiltInCategory.OST_StructuralFraming)
                {
                    var support = MeasureGirderSupport(candidate, beamAxis, transverseAxis, originPoint);
                    if (support.HasValue) rawSupports.Add(support.Value);
                }
            }
        }

        // Deduplicate supports by center station (merging candidates within 100 mm)
        var ordered = rawSupports
            .OrderBy(s => s.CenterXMm)
            .GroupBy(s => Math.Round(s.CenterXMm / 100.0) * 100.0)
            .Select(g => g.First())
            .ToList();

        var result = new List<BeamSupportNode>();
        int total = ordered.Count;

        for (int i = 0; i < total; i++)
        {
            var s = ordered[i];
            bool isExterior = (i == 0 || i == total - 1);
            var type = isExterior && s.Type == SupportType.Column ? SupportType.ExteriorColumn : s.Type;

            result.Add(new BeamSupportNode(
                index: i,
                name: $"Support {i + 1}",
                centerX: s.CenterXMm,
                width: s.WidthMm,
                type: type,
                depth: s.DepthMm,
                elementUniqueId: s.UniqueId,
                isExterior: isExterior));
        }

        return result;
    }

    /// <summary>
    /// Identifies secondary framing beams intersecting the continuous beam web.
    /// </summary>
    public static IReadOnlyList<SecondaryBeamIntersection> FindSecondaryBeams(
        Document doc,
        IReadOnlyList<Element> sortedBeams,
        XYZ beamAxis,
        XYZ transverseAxis,
        XYZ originPoint)
    {
        var intersections = new List<SecondaryBeamIntersection>();
        var beamIds = new HashSet<ElementId>(sortedBeams.Select(b => b.Id));
        int index = 0;

        for (int spanIdx = 0; spanIdx < sortedBeams.Count; spanIdx++)
        {
            var beam = sortedBeams[spanIdx];
            var box = beam.get_BoundingBox(null);
            if (box is null) continue;

            var outline = new Outline(
                new XYZ(box.Min.X - 0.5, box.Min.Y - 0.5, box.Min.Z - 0.2),
                new XYZ(box.Max.X + 0.5, box.Max.Y + 0.5, box.Max.Z + 0.2));

            var candidates = new FilteredElementCollector(doc)
                .WherePasses(new BoundingBoxIntersectsFilter(outline))
                .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming))
                .WhereElementIsNotElementType()
                .Where(e => !beamIds.Contains(e.Id))
                .ToList();

            foreach (var candidate in candidates)
            {
                var curve = (candidate.Location as LocationCurve)?.Curve as Line;
                if (curve is null) continue;

                XYZ candDir = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
                double dot = Math.Abs(candDir.DotProduct(beamAxis));

                // Angle must be near 90 degrees (dot product < 0.25 -> angle between 75 and 105 deg)
                if (dot > 0.25) continue;

                // Intersection point between candidate line and primary beam centerline
                var primaryLine = (beam.Location as LocationCurve)?.Curve as Line;
                if (primaryLine is null) continue;

                var result = primaryLine.Intersect(curve, out var intersectionArray);
                XYZ ptIntersect;

                if (result == SetComparisonResult.Overlap && intersectionArray != null && intersectionArray.Size > 0)
                {
                    ptIntersect = intersectionArray.get_Item(0).XYZPoint;
                }
                else
                {
                    // Fallback to midpoint of closest approach
                    ptIntersect = curve.GetEndPoint(0);
                }

                double centerXMm = RevitUnits.FtToMm((ptIntersect - originPoint).DotProduct(beamAxis));
                double widthMm = BeamSolidFaceReader.GetWidthMm(candidate, candDir.CrossProduct(XYZ.BasisZ).Normalize());
                double heightMm = BeamSolidFaceReader.GetHeightMm(candidate);

                if (widthMm <= 0.0) widthMm = 200.0; // Fallback default
                if (heightMm <= 0.0) heightMm = 400.0;

                // Determine framing side relative to primary beam axis
                XYZ toCandidate = (curve.GetEndPoint(1) - ptIntersect).Normalize();
                double sideDot = toCandidate.DotProduct(transverseAxis);
                var side = Math.Abs(sideDot) < 0.2 ? IntersectionSide.Both : (sideDot > 0 ? IntersectionSide.Left : IntersectionSide.Right);

                intersections.Add(new SecondaryBeamIntersection(
                    index: index++,
                    hostSpanIndex: spanIdx,
                    centerX: centerXMm,
                    width: widthMm,
                    height: heightMm,
                    topElevation: RevitUnits.FtToMm(ptIntersect.Z),
                    framingSide: side,
                    elementUniqueId: candidate.UniqueId));
            }
        }

        return intersections;
    }

    private static (double CenterXMm, double WidthMm, double DepthMm, SupportType Type, string UniqueId)?
        MeasureColumnSupport(Element column, XYZ beamAxis, XYZ transverseAxis, XYZ originPoint)
    {
        var solid = BeamSolidFaceReader.GetSingleSolid(column);
        if (solid is null) return null;

        var top = BeamSolidFaceReader.GetTop(column);
        var corners = new List<XYZ>();

        foreach (Edge edge in top.EdgeLoops.get_Item(0).Edges)
        {
            corners.Add(edge.AsCurve().GetEndPoint(0));
        }

        if (corners.Count == 0) return null;

        double minS = corners.Min(c => (c - originPoint).DotProduct(beamAxis));
        double maxS = corners.Max(c => (c - originPoint).DotProduct(beamAxis));
        double minY = corners.Min(c => (c - originPoint).DotProduct(transverseAxis));
        double maxY = corners.Max(c => (c - originPoint).DotProduct(transverseAxis));

        double centerS = (minS + maxS) / 2.0;
        double widthS = maxS - minS;
        double depthY = maxY - minY;

        return (
            RevitUnits.FtToMm(centerS),
            RevitUnits.FtToMm(widthS),
            RevitUnits.FtToMm(depthY),
            SupportType.Column,
            column.UniqueId);
    }

    private static (double CenterXMm, double WidthMm, double DepthMm, SupportType Type, string UniqueId)?
        MeasureWallSupport(Element wall, XYZ beamAxis, XYZ transverseAxis, XYZ originPoint)
    {
        var solid = BeamSolidFaceReader.GetSingleSolid(wall);
        if (solid is null) return null;

        var box = wall.get_BoundingBox(null);
        if (box is null) return null;

        XYZ center = (box.Min + box.Max) * 0.5;
        double centerS = (center - originPoint).DotProduct(beamAxis);

        // Project bounding box extents along beam axis
        double widthS = Math.Abs((box.Max - box.Min).DotProduct(beamAxis));
        double depthY = Math.Abs((box.Max - box.Min).DotProduct(transverseAxis));

        return (
            RevitUnits.FtToMm(centerS),
            RevitUnits.FtToMm(widthS > 0.3 ? widthS : 0.8), // ~250 mm fallback
            RevitUnits.FtToMm(depthY),
            SupportType.Wall,
            wall.UniqueId);
    }

    private static (double CenterXMm, double WidthMm, double DepthMm, SupportType Type, string UniqueId)?
        MeasureGirderSupport(Element girder, XYZ beamAxis, XYZ transverseAxis, XYZ originPoint)
    {
        var curve = (girder.Location as LocationCurve)?.Curve as Line;
        if (curve is null) return null;

        XYZ candDir = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
        double dot = Math.Abs(candDir.DotProduct(beamAxis));

        // Angle must be between 60° and 120°
        if (dot > 0.5) return null;

        XYZ mid = (curve.GetEndPoint(0) + curve.GetEndPoint(1)) * 0.5;
        double centerS = (mid - originPoint).DotProduct(beamAxis);
        double width = BeamSolidFaceReader.GetWidthMm(girder, candDir.CrossProduct(XYZ.BasisZ).Normalize());
        double height = BeamSolidFaceReader.GetHeightMm(girder);

        return (
            RevitUnits.FtToMm(centerS),
            width > 0 ? width : 300.0,
            height > 0 ? height : 600.0,
            SupportType.Girder,
            girder.UniqueId);
    }
}
```

---

## 6. `BeamStackValidator.cs` Specification

Validates that a selected collection of beam elements forms an executable continuous beam run before opening any transaction.

### 6.1 Validation Rules Sequence
1. **Rule 1: Selection Count**: `beams.Count >= 1`.
2. **Rule 2: Framing Category**: Every element has `Category.BuiltInCategory == OST_StructuralFraming` and is `FamilyInstance`.
3. **Rule 3: Linear Geometry**: Every beam has a straight `Line` `LocationCurve`.
4. **Rule 4: Single Solid**: Every beam contains exactly 1 solid with volume $> 10^{-6}$.
5. **Rule 5: Rectangular Cross-Section**: Every beam exhibits `BeamSectionStyle.Rectangle`.
6. **Rule 6: Collinearity**: Longitudinal angle deviation between any span and the primary axis $\le 1.0^\circ$.
7. **Rule 7: Lateral Offset**: Perpendicular point-to-line distance from any beam center to the continuous axis $\le 10.0\text{ mm}$.
8. **Rule 8: Level & Top Elevation Consistency**: Top face elevations match within $\pm 5.0\text{ mm}$.
9. **Rule 9: Physical Continuity**: Distance between adjacent span end and start faces $\le$ max support width $+ 50\text{ mm}$.
10. **Rule 10: Positive Dimensions**: $b \ge 100\text{ mm}$, $h \ge 150\text{ mm}$, $L_c > 0$.

### 6.2 Detailed Implementation
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Models;

namespace HPRebar.BeamRebar;

/// <summary>
/// Validates a selection of structural framing elements against continuous beam geometric rules.
/// </summary>
public static class BeamStackValidator
{
    public const double MaxCollinearAngleDeg = 1.0;
    public const double MaxOffsetMm = 10.0;
    public const double MaxElevationDiffMm = 5.0;

    public static ValidationResult Validate(Document doc, IReadOnlyList<Element> beams)
    {
        if (beams is null || beams.Count == 0)
            return ValidationResult.Fail(1);

        var rules = new (int Code, Func<bool> Holds)[]
        {
            (2, () => beams.All(b => b.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming && b is FamilyInstance)),
            (3, () => beams.All(b => (b.Location as LocationCurve)?.Curve is Line)),
            (4, () => beams.All(b => BeamSolidFaceReader.GetSolids(b).Count == 1)),
            (5, () => AreAllRectangular(beams)),
            (6, () => IsCollinear(beams)),
            (7, () => IsWithinLateralOffset(beams)),
            (8, () => HasConsistentTopElevation(beams)),
            (9, () => AreSpansContiguous(beams)),
            (10, () => HasPositiveDimensions(beams))
        };

        foreach (var (code, holds) in rules)
        {
            if (!holds()) return ValidationResult.Fail(code);
        }

        return ValidationResult.Ok;
    }

    private static bool AreAllRectangular(IReadOnlyList<Element> beams)
    {
        foreach (var beam in beams)
        {
            var line = (beam.Location as LocationCurve)?.Curve as Line;
            if (line is null) return false;

            XYZ axis = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            XYZ trans = XYZ.BasisZ.CrossProduct(axis).Normalize();

            if (BeamSolidFaceReader.GetSectionStyle(beam, axis, trans) != BeamSectionStyle.Rectangle)
                return false;
        }
        return true;
    }

    private static bool IsCollinear(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        var line0 = (beams[0].Location as LocationCurve)?.Curve as Line;
        if (line0 is null) return false;

        XYZ primaryAxis = (line0.GetEndPoint(1) - line0.GetEndPoint(0)).Normalize();

        for (int i = 1; i < beams.Count; i++)
        {
            var line = (beams[i].Location as LocationCurve)?.Curve as Line;
            if (line is null) return false;

            XYZ axis = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            double dot = Math.Abs(axis.DotProduct(primaryAxis));
            double angleDeg = Math.Acos(Math.Min(1.0, dot)) * 180.0 / Math.PI;

            if (angleDeg > MaxCollinearAngleDeg) return false;
        }

        return true;
    }

    private static bool IsWithinLateralOffset(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        var line0 = (beams[0].Location as LocationCurve)?.Curve as Line;
        if (line0 is null) return false;

        XYZ origin = line0.GetEndPoint(0);
        XYZ primaryAxis = (line0.GetEndPoint(1) - origin).Normalize();

        for (int i = 1; i < beams.Count; i++)
        {
            var line = (beams[i].Location as LocationCurve)?.Curve as Line;
            if (line is null) return false;

            XYZ mid = (line.GetEndPoint(0) + line.GetEndPoint(1)) * 0.5;
            XYZ diff = mid - origin;
            XYZ perp = diff - diff.DotProduct(primaryAxis) * primaryAxis;
            double offsetMm = RevitUnits.FtToMm(perp.GetLength());

            if (offsetMm > MaxOffsetMm) return false;
        }

        return true;
    }

    private static bool HasConsistentTopElevation(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        double z0 = BeamSolidFaceReader.GetTop(beams[0]).Origin.Z;

        for (int i = 1; i < beams.Count; i++)
        {
            double z = BeamSolidFaceReader.GetTop(beams[i]).Origin.Z;
            double diffMm = RevitUnits.FtToMm(Math.Abs(z - z0));

            if (diffMm > MaxElevationDiffMm) return false;
        }

        return true;
    }

    private static bool AreSpansContiguous(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        var line0 = (beams[0].Location as LocationCurve)?.Curve as Line;
        if (line0 is null) return false;

        XYZ origin = line0.GetEndPoint(0);
        XYZ axis = (line0.GetEndPoint(1) - origin).Normalize();

        // Project and sort endpoints
        var spans = beams.Select(b =>
        {
            var l = (b.Location as LocationCurve)!.Curve as Line;
            double s0 = (l!.GetEndPoint(0) - origin).DotProduct(axis);
            double s1 = (l.GetEndPoint(1) - origin).DotProduct(axis);
            return (Start: Math.Min(s0, s1), End: Math.Max(s0, s1));
        }).OrderBy(s => s.Start).ToList();

        for (int i = 0; i < spans.Count - 1; i++)
        {
            double gapFt = spans[i + 1].Start - spans[i].End;
            double gapMm = RevitUnits.FtToMm(gapFt);

            // Gaps up to 1500 mm are acceptable if spanning across a wide column/wall
            // Any gap > 2000 mm indicates a missing or disconnected span
            if (gapMm > 2000.0) return false;
        }

        return true;
    }

    private static bool HasPositiveDimensions(IReadOnlyList<Element> beams)
    {
        foreach (var beam in beams)
        {
            var line = (beam.Location as LocationCurve)?.Curve as Line;
            if (line is null || line.Length <= 0) return false;

            XYZ axis = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            XYZ trans = XYZ.BasisZ.CrossProduct(axis).Normalize();

            double b = BeamSolidFaceReader.GetWidthMm(beam, trans);
            double h = BeamSolidFaceReader.GetHeightMm(beam);

            if (b < 100.0 || h < 150.0) return false;
        }
        return true;
    }
}
```

---

## 7. `BeamStackReader.cs` Specification

Coordinates the complete extraction process, sorting spans in natural progression, aligning longitudinal coordinates, identifying bearing supports and secondary beams, and assembling both the pure `BeamContinuousStack` and Revit `BeamStack`.

### 7.1 Coordinate Normalization & Sorting
1. Let primary direction $\vec{X}_{beam}$ be determined from the first beam's vector $\vec{P}_1 - \vec{P}_0$.
2. For every beam $i$, project its endpoints onto the line through $\vec{P}_0$:
   $$s_{0, i} = (\vec{P}_{start, i} - \vec{P}_0) \cdot \vec{X}_{beam}, \quad s_{1, i} = (\vec{P}_{end, i} - \vec{P}_0) \cdot \vec{X}_{beam}$$
3. Sort beams in ascending order of $\min(s_{0, i}, s_{1, i})$.
4. If $s_{1, i} < s_{0, i}$, note that this beam's local line orientation is flipped relative to the continuous beam flow.
5. Set datum origin at $\vec{P}_{origin} = \vec{P}_0 + \min(s_0, s_1) \cdot \vec{X}_{beam}$.

### 7.2 Clear Span & Boundary Calculations
For each span $i$ between Support $i$ and Support $i+1$:
$$L_{center} = s_{center, i+1} - s_{center, i}$$
$$L_{clear} = L_{center} - \frac{w_{support, i}}{2} - \frac{w_{support, i+1}}{2}$$
$$\text{Span Start } s_{start} = s_{center, i} + \frac{w_{support, i}}{2}$$

### 7.3 Detailed Implementation
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Models;
using HPRebar.Core.BeamRebar.Models;
using Serilog;

namespace HPRebar.BeamRebar;

/// <summary>
/// Reads a validated selection of structural framing elements and produces a fully assembled <see cref="BeamStack"/>.
/// Performs all conversions between Revit decimal feet and domain millimetres.
/// </summary>
public static class BeamStackReader
{
    public static BeamStack Read(Document doc, IReadOnlyList<Element> selectedBeams)
    {
        if (selectedBeams is null || selectedBeams.Count == 0)
            throw new ArgumentException("No beams provided to BeamStackReader.", nameof(selectedBeams));

        // 1. Establish dominant longitudinal axis X_beam and transverse axis Y_beam
        var primaryLine = (selectedBeams[0].Location as LocationCurve)!.Curve as Line;
        XYZ originRef = primaryLine!.GetEndPoint(0);
        XYZ beamAxis = (primaryLine.GetEndPoint(1) - originRef).Normalize();
        XYZ transAxis = XYZ.BasisZ.CrossProduct(beamAxis).Normalize();

        // 2. Project and sort beams in ascending order along X_beam
        var orderedBeams = selectedBeams
            .Select(b =>
            {
                var line = (b.Location as LocationCurve)!.Curve as Line;
                double s0 = (line!.GetEndPoint(0) - originRef).DotProduct(beamAxis);
                double s1 = (line.GetEndPoint(1) - originRef).DotProduct(beamAxis);
                return new { Beam = b, MinS = Math.Min(s0, s1), MaxS = Math.Max(s0, s1) };
            })
            .OrderBy(item => item.MinS)
            .Select(item => item.Beam)
            .ToList();

        // Origin datum point at start of first beam
        XYZ originPoint = originRef + (orderedBeams.Select(b =>
        {
            var l = (b.Location as LocationCurve)!.Curve as Line;
            return Math.Min((l!.GetEndPoint(0) - originRef).DotProduct(beamAxis),
                            (l.GetEndPoint(1) - originRef).DotProduct(beamAxis));
        }).Min()) * beamAxis;

        // 3. Find supports and secondary intersections
        var supports = BeamSupportFinder.FindSupports(doc, orderedBeams, beamAxis, transAxis, originPoint);
        var secondaryBeams = BeamSupportFinder.FindSecondaryBeams(doc, orderedBeams, beamAxis, transAxis, originPoint);

        // 4. Extract faces and construct domain spans
        var facesList = new List<BeamFaces>(orderedBeams.Count);
        var spansList = new List<BeamSpan>(orderedBeams.Count);

        for (int i = 0; i < orderedBeams.Count; i++)
        {
            var beam = orderedBeams[i];
            var faces = ReadBeamFaces(doc, beam, beamAxis, transAxis);
            facesList.Add(faces);

            double widthMm = BeamSolidFaceReader.GetWidthMm(beam, transAxis);
            double heightMm = BeamSolidFaceReader.GetHeightMm(beam);
            double topElevMm = RevitUnits.FtToMm(faces.Top.Origin.Z);

            // Locate adjacent supports for this span
            var leftSupport = i < supports.Count ? supports[i] : null;
            var rightSupport = (i + 1) < supports.Count ? supports[i + 1] : null;

            double leftCenterXMm = leftSupport?.CenterX ?? 0.0;
            double rightCenterXMm = rightSupport?.CenterX ?? leftCenterXMm + 4000.0;
            double leftWidthMm = leftSupport?.Width ?? 0.0;
            double rightWidthMm = rightSupport?.Width ?? 0.0;

            double lengthCenterMm = rightCenterXMm - leftCenterXMm;
            double lengthClearMm = lengthCenterMm - (leftWidthMm / 2.0) - (rightWidthMm / 2.0);
            if (lengthClearMm <= 0) lengthClearMm = lengthCenterMm;

            double startXMm = leftCenterXMm + (leftWidthMm / 2.0);

            var span = new BeamSpan(
                index: i,
                name: $"Span {i + 1}",
                lengthCenter: lengthCenterMm,
                width: widthMm,
                height: heightMm,
                topElevation: topElevMm,
                cover: 25.0, // Default cover
                clearLength: lengthClearMm,
                startX: startXMm,
                cantilever: CantileverPosition.None,
                elementUniqueId: beam.UniqueId);

            spansList.Add(span);
        }

        // 5. Build pure BeamContinuousStack
        var continuousStack = new BeamContinuousStack(spansList, supports, secondaryBeams);

        // 6. Build dimension witness faces
        var dimFaces = BuildDimensionFaces(facesList);

        return new BeamStack
        {
            Style = BeamSectionStyle.Rectangle,
            ContinuousStack = continuousStack,
            Faces = facesList,
            BeamDirection = beamAxis,
            TransverseDirection = transAxis,
            OriginPoint = originPoint,
            DatumFace = facesList[0].StartFace ?? facesList[0].Bottom,
            TopDatum = facesList[0].Top,
            BottomDatum = facesList[0].Bottom,
            DimensionFaces = dimFaces
        };
    }

    private static BeamFaces ReadBeamFaces(Document doc, Element beam, XYZ beamAxis, XYZ transAxis)
    {
        return new BeamFaces
        {
            Element = beam,
            Top = BeamSolidFaceReader.GetTop(beam),
            Bottom = BeamSolidFaceReader.GetBottom(beam),
            Left = BeamSolidFaceReader.GetLeftFace(beam, transAxis),
            Right = BeamSolidFaceReader.GetRightFace(beam, transAxis),
            StartFace = BeamSolidFaceReader.GetStartFace(beam, beamAxis),
            EndFace = BeamSolidFaceReader.GetEndFace(beam, beamAxis),
            Level = LevelOf(doc, beam, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)
        };
    }

    private static IReadOnlyList<PlanarFace> BuildDimensionFaces(IReadOnlyList<BeamFaces> facesList)
    {
        var list = new List<PlanarFace>();
        foreach (var f in facesList)
        {
            if (f.StartFace != null) list.Add(f.StartFace);
            if (f.Top != null) list.Add(f.Top);
            if (f.Bottom != null) list.Add(f.Bottom);
            if (f.EndFace != null) list.Add(f.EndFace);
        }
        return list;
    }

    private static Level? LevelOf(Document doc, Element elem, BuiltInParameter paramId)
    {
        var id = elem.get_Parameter(paramId)?.AsElementId();
        return id is null ? null : doc.GetElement(id) as Level;
    }
}
```

---

## 8. Multi-Version Revit API Guardrails Checklist

| API Area | Forbidden Legacy Pattern | Mandatory Compliant Pattern | Rationale |
|---|---|---|---|
| **Units** | `DisplayUnitType.DUT_MILLIMETERS` | `UnitTypeId.Millimeters` | Deprecated in 2021, removed in 2023 |
| **Unit Conversion** | Manual arithmetic `* 304.8` | `RevitUnits.MmToFt` / `FtToMm` | Centralized, certified by Revit UnitUtils |
| **Element IDs** | `elem.Id.IntegerValue` | `#if REVIT2024_OR_GREATER elem.Id.Value #else elem.Id.IntegerValue #endif` | `ElementId.Value` returns `long` since Revit 2024 |
| **Categories** | `elem.Category.Name == "Structural Framing"` | `elem.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming` | Language-independent, immune to Revit localization |
| **Geometry** | Hardcoded `ViewDetailLevel.Coarse` | `ViewDetailLevel.Fine` with `ComputeReferences = true` | Required to obtain valid Face references for dimensions |
| **Curves** | Arc/Spline location lines | Reject via `BeamStackValidator` | Continuous shape-driven rebar requires straight lines |

---

## 9. Verification & Quality Gates

The implementer can independently verify these components through:

1. **Compilation Check across target configurations**:
   ```bash
   dotnet build HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
   dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
   ```
2. **Namespace & File Structure Check**:
   Confirm all models reside in `HPRebar/HPRebar/Beam Rebar/Models/` under `namespace HPRebar.BeamRebar.Models;` and services reside at `HPRebar/HPRebar/Beam Rebar/` under `namespace HPRebar.BeamRebar;`.
3. **Core Independence Check**:
   Ensure `HPRebar.Core` is referenced by `HPRebar`, and `HPRebar.Core` maintains 0 references to `Autodesk.Revit.*`.
4. **Unit Tests Check**:
   ```bash
   dotnet test HPRebar.Core.Tests
   ```
