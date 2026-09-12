# Technical Implementation Specification: Views, Dimensions, Annotations, Orchestration & Command
## Milestone M3 Part 3 — Continuous Beam Rebar (HPRebar / Revit Add-In)

- **Author**: `explorer_m3_3`
- **Target Solution**: `HPRebar/HPRebar.slnx`
- **Target Assembly**: `HPRebar/HPRebar/Beam Rebar/`
- **Target Frameworks**: `net8.0-windows7.0` (Revit 2025/2026), multi-version compatible with `net48` (Revit 2023/2024) and `net10.0-windows7.0` (Revit 2027)
- **Status**: Complete Design Specification
- **Date**: 2026-09-07

---

## 1. Executive Architecture & Responsibility Matrix

The continuous beam reinforcement feature in `HPRebar` strictly follows the production architecture established by the Golden Reference `HPRebar/HPRebar/Column Rebar/`. Milestone M3 Part 3 encompasses all view generation, dimensioning, tabular scheduling, atomic transaction orchestration, execution runner, command entry point, and shared Revit integration utilities.

```
┌────────────────────────────────────────────────────────────────────────┐
│                        WPF MVVM Presentation                           │
│  HPRebar/HPRebar/Beam Rebar/View/ & View Models/                       │
│  - BeamRebarViewModel : ObservableObject                               │
│  - BeamRebarView : Window (Modal, DynamicResource Theming)             │
│  - IBeamRebarRunner (Decoupling Interface)                             │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ calls runner.Run(spec, progress)
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                   Revit Add-In Integration Layer                       │
│  HPRebar/HPRebar/Beam Rebar/                                           │
│  - BeamRebarCommand : ExternalCommand (Selection, Filter, Modal Show)  │
│  - RevitRebarRunner : IBeamRebarRunner                                 │
│  - BeamRebarOrchestrator (Sole Owner of TransactionGroup("Beam Rebar"))│
│    ├── DetailViewCreator (Longitudinal Elevation ViewSection)          │
│    ├── SectionViewCreator (Transverse Cross-Section ViewSections)      │
│    ├── DimensionCreator (SURFACE -> LINEAR Reference Rewriting)        │
│    ├── RebarTableTagCreator (Detail Curves & TextNotes Schedule Table) │
│    └── RebarCreationService (Stirrups, Main, Additional, Side, Special)│
│  - Supporting:                                                         │
│    ├── RebarFailureHandling (SwallowWarnings preprocessor)             │
│    ├── RevitUnits (Strict Millimetres <-> Decimal Feet Boundary)       │
│    ├── LocalizationService & UiStrings (EN / VN Language Switching)    │
│    ├── ThemeSwitcher (Runtime DynamicResource Dark/Light Theme Swap)   │
│    └── RevitDialogs (TaskDialog wrappers without WinForms)             │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ consumes domain records & math
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                      Pure Domain Logic & Geometry                      │
│  HPRebar.Core/BeamRebar/ (netstandard2.0, Zero Revit References)       │
│  - BeamContinuousStack, BeamSpan, BeamSupportNode                      │
│  - BeamStirrupDistributionCalculator, BeamMainBarCalculator...         │
└────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibility Table

| Class | Primary Responsibility | Revit API Dependencies |
|---|---|---|
| `DetailViewCreator` | Creates overall continuous beam elevation section view (`ViewSection.CreateDetail` / `CreateSection`). | `ViewSection`, `ViewFamilyType`, `BoundingBoxXYZ`, `Transform` |
| `SectionViewCreator` | Creates transverse cross-section views at supports and midspans (`ViewSection.CreateSection`). | `ViewSection`, `ViewFamilyType`, `BoundingBoxXYZ`, `Transform` |
| `DimensionCreator` | Generates dimension chains on elevations (levels, spans) and sections ($B, H$) with `SURFACE` -> `LINEAR` reference rewriting. | `Document.Create.NewDimension`, `Reference`, `ReferenceArray`, `PlanarFace` |
| `RebarTableTagCreator` | Draws tabular rebar schedule blocks beside cross-sections and places elevation rebar tags. | `Document.Create.NewDetailCurve`, `TextNote.Create`, `IndependentTag.Create` |
| `BeamRebarOrchestrator` | Sole owner of `TransactionGroup("Beam Rebar")`; manages pre-flight checks, sub-transactions, progress, assimilation, and rollback. | `TransactionGroup`, `Transaction`, `Document` |
| `RevitRebarRunner` | Adapts orchestrator to `IBeamRebarRunner`, keeping ViewModel clean of Revit document references. | None directly; mediates domain input to orchestrator |
| `BeamRebarCommand` | `ExternalCommand` entry point; handles element selection (`StructuralFramingSelectionFilter`), validation, session initialization, and view launch. | `ExternalCommand`, `UIDocument.Selection`, `WindowInteropHelper` |
| `RebarFailureHandling` | Suppresses non-fatal Revit warnings (`SwallowWarnings` : `IFailuresPreprocessor`) to prevent UI lockups. | `IFailuresPreprocessor`, `FailuresAccessor`, `FailureSeverity` |
| `RevitUnits` | Universal unit conversion boundary between millimetres and decimal feet using `UnitTypeId.Millimeters`. | `UnitUtils`, `UnitTypeId`, `SpecTypeId` |
| `LocalizationService` | Provides reactive runtime language switching (English / Vietnamese) for all UI labels. | `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` |
| `ThemeSwitcher` | Synchronizes WPF window styling with Revit's Light/Dark mode via resource dictionary replacement. | `ResourceDictionary`, `UIThemeManager` (`REVIT2024_OR_GREATER`) |
| `RevitDialogs` | TaskDialog wrappers replacing WinForms `MessageBox`. | `Autodesk.Revit.UI.TaskDialog` |

---

## 2. Longitudinal Elevation Detail Views (`DetailViewCreator.cs`)

### 2.1 Mission & Architectural Intent
The continuous beam spans across multiple columns, walls, and openings. The longitudinal elevation view captures the entire continuous assembly from exterior support to exterior support in a single drawing, showing:
- Structural framing profiles, joints, and stepped soffits.
- Continuous top and bottom longitudinal reinforcement with end anchorage hooks and lap splices.
- Stirrup layout zones (dense at supports, sparse in midspan, and hanging clusters).
- Additional reinforcement bars (support top bars, midspan bottom bars).
- Level markers, grids, and longitudinal dimension lines.

### 2.2 View Type Resolution
Detail views prefer `ViewFamily.Detail` (Detail Section), falling back to `ViewFamily.Section` (Building Section) if the template lacks detail section types:

```csharp
namespace HPRebar.BeamRebar;

public static class DetailViewCreator
{
    private const string ViewTypeName = "@BeamDetail";

    public static ViewFamilyType? ResolveViewType(Document document, string name)
    {
        var sectionTypes = new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .Where(type => type.ViewFamily == ViewFamily.Detail || type.ViewFamily == ViewFamily.Section)
            .ToList();

        // 1. Exact name match
        var existing = sectionTypes.FirstOrDefault(type => type.Name == name);
        if (existing is not null) return existing;

        // 2. Prefer Detail family template, fallback to Section
        var template = sectionTypes.FirstOrDefault(type => type.ViewFamily == ViewFamily.Detail)
                       ?? sectionTypes.FirstOrDefault(type => type.ViewFamily == ViewFamily.Section);

        return template?.Duplicate(name) as ViewFamilyType;
    }
}
```

### 2.3 Coordinate System & Section Box Geometry
Let:
- $\vec{X}_{beam}$ be the unit direction vector along the continuous beam axis (from start of first span to end of last span, in the horizontal plane $Z = 0$).
- $\vec{Z} = (0, 0, 1)$ be the world vertical unit vector.
- $\vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$ be the transverse horizontal unit vector pointing towards the side face.

The view's `BoundingBoxXYZ` is constructed in a local coordinate frame:
- **Origin**: 3D centroid of the continuous beam assembly:
  $$P_{origin} = P_{start} + \frac{L_{total}}{2} \cdot \vec{X}_{beam} + (Z_{datum} - \frac{H_{max}}{2}) \cdot \vec{Z}$$
- **BasisX**: $\vec{X}_{beam}$ (horizontal axis in the section view, along the beam length).
- **BasisY**: $\vec{Z}$ (vertical axis in the section view, pointing upwards).
- **BasisZ**: $\vec{Y}_{beam}$ (perpendicular to the view plane; view camera looks in direction $-\vec{Y}_{beam}$ or $+\vec{Y}_{beam}$).
- **Crop Box Dimensions**:
  - `Min.X` = $-L_{total} / 2 - \text{margin}$
  - `Max.X` = $+L_{total} / 2 + \text{margin}$
  - `Min.Y` = $-H_{max} / 2 - \text{margin}$
  - `Max.Y` = $+H_{max} / 2 + \text{margin}$
  - `Min.Z` = $-B_{max} / 2 - \text{margin}$
  - `Max.Z` = $+B_{max} / 2 + \text{margin}$

```csharp
    public static ViewSection? Create(
        Document document,
        BeamContinuousStack stack,
        BeamFaces faces,
        BeamAnnotationSettings settings)
    {
        var viewType = ResolveViewType(document, ViewTypeName);
        if (viewType is null)
        {
            Log.Warning("The document has no detail or section view family type; elevation view was skipped.");
            return null;
        }

        var totalLengthFt = RevitUnits.MmToFt(stack.TotalLength);
        var maxHeightFt = RevitUnits.MmToFt(stack.MaxHeight);
        var maxWidthFt = RevitUnits.MmToFt(faces.MaxWidthMm);
        var marginFt = RevitUnits.MmToFt(settings.ViewMargin);

        XYZ axisDir = faces.BeamAxis; // Unit vector along beam length
        XYZ sideDir = faces.SideNormal; // Unit vector perpendicular to beam side (Z x axisDir)

        // Centerpoint of continuous beam assembly
        XYZ startPoint = faces.StartPoint;
        XYZ centerPoint = startPoint 
            + (totalLengthFt * 0.5) * axisDir 
            + (faces.TopElevationFt - maxHeightFt * 0.5) * XYZ.BasisZ;

        var transform = Transform.Identity;
        transform.Origin = centerPoint;
        transform.BasisX = axisDir;
        transform.BasisY = XYZ.BasisZ;
        transform.BasisZ = sideDir;

        var box = new BoundingBoxXYZ
        {
            Transform = transform,
            Min = new XYZ(-totalLengthFt * 0.5 - marginFt, -maxHeightFt * 0.5 - marginFt, -maxWidthFt * 0.5 - marginFt),
            Max = new XYZ(totalLengthFt * 0.5 + marginFt, maxHeightFt * 0.5 + marginFt, maxWidthFt * 0.5 + marginFt)
        };

        ViewSection view;
        if (viewType.ViewFamily == ViewFamily.Detail)
        {
            view = ViewSection.CreateDetail(document, viewType.Id, box);
        }
        else
        {
            view = ViewSection.CreateSection(document, viewType.Id, box);
        }

        Rename(view, settings.DetailViewName);

        // Hide crop boundary box
        view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE)?.Set(0);

        if (settings.DetailTemplate is not null)
        {
            view.ViewTemplateId = settings.DetailTemplate.Id;
        }

        return view;
    }
```

### 2.4 Duplicate Name Handling (`Rename`)
Revit enforces document-wide unique view names. Attempting to set `view.Name` to an existing view name throws `ArgumentException: Name must be unique`. DetailViewCreator implements safe fallback suffixing:

```csharp
    internal static void Rename(View view, string name)
    {
        try
        {
            view.Name = name;
        }
        catch (Exception)
        {
            var fallback = name + "A";
            Log.Warning("A view named {Name} already exists; new view renamed to {Fallback}", name, fallback);
            try
            {
                view.Name = fallback;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not name view {Fallback}; retaining default Revit name {Default}", fallback, view.Name);
            }
        }
    }
```

---

## 3. Transverse Cross-Section Views (`SectionViewCreator.cs`)

### 3.1 Mission & Cross-Section Cut Locations
For each span in the continuous beam, transverse cross-section views must be cut to show rebar placement in critical zones:
1. **Section 1-1 (Left Support Zone / Gối Trái)**:
   - Cut at $X = X_{left\_support\_face} + L_{clear} / 6$.
   - Shows: Dense stirrups, top main bars, negative moment additional top bars, side bars, bottom main bars.
2. **Section 2-2 (Midspan Zone / Nhịp)**:
   - Cut at $X = X_{left\_support\_face} + L_{clear} / 2$.
   - Shows: Sparse stirrups, top main bars, side bars, bottom main bars, positive moment additional bottom bars.
3. **Section 3-3 (Right Support Zone / Gối Phải)**:
   - Cut at $X = X_{right\_support\_face} - L_{clear} / 6$.
   - Used when spans have asymmetric additional top bars or differing support sizes.
4. **Cantilever Section**:
   - For cantilever spans, cut at mid-length of the overhang ($L_{cantilever} / 2$).

### 3.2 Coordinate System & Box Sizing
- **Origin**: Point on beam axis at cut station $s$, at mid-height of the span cross-section:
  $$P_{origin} = P_{start} + s \cdot \vec{X}_{beam} + (Z_{top} - H_{span}/2) \cdot \vec{Z}$$
- **BasisX**: $\vec{Y}_{beam}$ (horizontal axis in section view, across the beam width $b$).
- **BasisY**: $\vec{Z}$ (vertical axis in section view, pointing upwards).
- **BasisZ**: $\vec{X}_{beam}$ (view direction looking along the beam longitudinal axis).
- **Bounding Box Crop**:
  - `Min.X` = $-b / 2 - \text{margin}$
  - `Max.X` = $+b / 2 + 2.5 \cdot \text{margin}$ *(extra space on the right reserved for the bar table!)*
  - `Min.Y` = $-h / 2 - \text{margin}$
  - `Max.Y` = $+h / 2 + \text{margin}$
  - `Min.Z` = $-\text{margin} / 2$
  - `Max.Z` = $+\text{margin} / 2$ (narrow far-clip plane to prevent showing background beams)

```csharp
namespace HPRebar.BeamRebar;

public static class SectionViewCreator
{
    private const string ViewTypeName = "@BeamSection";

    public static IReadOnlyList<ViewSection> Create(
        Document document,
        BeamContinuousStack stack,
        BeamFaces faces,
        BeamAnnotationSettings settings)
    {
        var views = new List<ViewSection>();
        var viewType = DetailViewCreator.ResolveViewType(document, ViewTypeName);

        if (viewType is null)
        {
            Log.Warning("The document has no section view family type; cross-section views were skipped.");
            return views;
        }

        for (int spanIndex = 0; spanIndex < stack.Spans.Count; spanIndex++)
        {
            var span = stack.Spans[spanIndex];
            var cutStations = ComputeCutStations(span, settings.SectionsPerSpan);

            for (int cutIndex = 0; cutIndex < cutStations.Count; cutIndex++)
            {
                double stationX = cutStations[cutIndex];
                var sectionView = CreateSectionAtStation(
                    document, viewType, settings, stack, faces, spanIndex, cutIndex, stationX);

                if (sectionView is not null)
                {
                    views.Add(sectionView);
                }
            }
        }

        return views;
    }

    public static IReadOnlyList<double> ComputeCutStations(BeamSpan span, int sectionsPerSpan)
    {
        var stations = new List<double>();
        if (span.IsCantilever || sectionsPerSpan <= 1)
        {
            stations.Add(span.StartX + span.LengthClear * 0.5);
            return stations;
        }

        if (sectionsPerSpan == 2)
        {
            stations.Add(span.StartX + span.LengthClear / 6.0); // Support zone
            stations.Add(span.StartX + span.LengthClear * 0.5); // Midspan
        }
        else // 3 sections per span (Standard detailing)
        {
            stations.Add(span.StartX + span.LengthClear / 6.0);       // Left Support
            stations.Add(span.StartX + span.LengthClear * 0.5);       // Midspan
            stations.Add(span.StartX + span.LengthClear * 5.0 / 6.0); // Right Support
        }

        return stations;
    }

    private static ViewSection CreateSectionAtStation(
        Document document,
        ViewFamilyType viewType,
        BeamAnnotationSettings settings,
        BeamContinuousStack stack,
        BeamFaces faces,
        int spanIndex,
        int cutIndex,
        double stationXMm)
    {
        var span = stack.Spans[spanIndex];
        var marginFt = RevitUnits.MmToFt(settings.ViewMargin);
        var widthFt = RevitUnits.MmToFt(span.Width);
        var heightFt = RevitUnits.MmToFt(span.Height);
        var stationFt = RevitUnits.MmToFt(stationXMm);

        XYZ axisDir = faces.BeamAxis;
        XYZ sideDir = faces.SideNormal;

        XYZ cutCenter = faces.StartPoint 
            + stationFt * axisDir 
            + (RevitUnits.MmToFt(span.TopElevation) - heightFt * 0.5) * XYZ.BasisZ;

        var transform = Transform.Identity;
        transform.Origin = cutCenter;
        transform.BasisX = sideDir;
        transform.BasisY = XYZ.BasisZ;
        transform.BasisZ = axisDir; // Camera looks along beam axis

        var box = new BoundingBoxXYZ
        {
            Transform = transform,
            // Right-side margin scaled +2.5x to accommodate the bar schedule table
            Min = new XYZ(-widthFt * 0.5 - marginFt, -heightFt * 0.5 - marginFt, -marginFt * 0.5),
            Max = new XYZ(widthFt * 0.5 + 2.5 * marginFt, heightFt * 0.5 + marginFt, marginFt * 0.5)
        };

        var view = ViewSection.CreateSection(document, viewType.Id, box);
        view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE)?.Set(0);

        if (settings.SectionTemplate is not null)
        {
            view.ViewTemplateId = settings.SectionTemplate.Id;
        }

        string sectionName = settings.SectionViewName(spanIndex + 1, cutIndex + 1);
        DetailViewCreator.Rename(view, sectionName);

        return view;
    }
}
```

---

## 4. Dimension Generation (`DimensionCreator.cs`)

### 4.1 The Stable Reference Rewriting Mechanism
Inside a `ViewSection` (whether elevation or cross-section), Revit's `doc.Create.NewDimension` method requires references to **curves / edges** (`LINEAR`). However, querying geometry faces via `PlanarFace.Reference` returns references categorized as `SURFACE`.

If a `SURFACE` reference is passed directly to `NewDimension` in a `ViewSection`, Revit fails with:
`Autodesk.Revit.Exceptions.ArgumentException: Invalid reference for dimensioning in this view.`

To solve this deterministically without executing an expensive `ReferenceIntersector` ray trace across the view, the stable representation string token is converted:

```csharp
    /// <summary>
    /// Converts a 3D planar face reference (SURFACE) to an edge reference (LINEAR)
    /// required by Revit NewDimension in ViewSection.
    /// </summary>
    internal static Reference ToLinearReference(Document document, PlanarFace face)
    {
        var surface = face.Reference.ConvertToStableRepresentation(document);
        var linear = surface.Replace("SURFACE", "LINEAR");
        return Reference.ParseFromStableRepresentation(document, linear);
    }
```

### 4.2 Elevation Dimensions
On the longitudinal elevation view, two distinct dimension chains are created:
1. **Span & Support Chain** (Horizontal):
   - Runs below the beam soffit.
   - References: Support left faces, support right faces, span clear endpoints, beam overall ends.
   - Dimension line: Parallel to beam axis, offset downward by `DimensionOffsetV`.
2. **Level & Depth Chain** (Vertical):
   - Runs at the exterior end of the continuous beam.
   - References: Top surface planar face, bottom soffit planar face, and level datums.
   - Dimension line: Vertical, offset horizontally from the beam end by `DimensionOffsetH`.

### 4.3 Section Dimensions
On each cross-section view:
1. **Width Dimension ($B$)**:
   - References: Left side vertical face and right side vertical face.
   - Dimension line: Horizontal, offset above the beam top face by `DimensionOffsetV`.
2. **Height Dimension ($H$)**:
   - References: Top horizontal face and bottom horizontal soffit face.
   - Dimension line: Vertical, offset to the left of the beam left face by `DimensionOffsetH`.

```csharp
namespace HPRebar.BeamRebar;

public static class DimensionCreator
{
    public static int CreateOnElevation(
        Document document,
        ViewSection view,
        BeamContinuousStack stack,
        BeamFaces faces,
        BeamAnnotationSettings settings)
    {
        if (settings.DimensionType is null || faces.SupportFaces.Count < 2) return 0;

        int created = 0;
        try
        {
            // 1. Span Dimension Chain
            var spanRefs = new ReferenceArray();
            foreach (var face in faces.SupportFaces)
            {
                if (face.Reference is not null)
                {
                    spanRefs.Append(ToLinearReference(document, face));
                }
            }

            if (spanRefs.Size >= 2)
            {
                var line = ElevationSpanLine(view, faces, settings);
                document.Create.NewDimension(view, line, spanRefs, settings.DimensionType);
                created++;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not create span dimensions on elevation view {View}; skipping.", view.Name);
        }

        try
        {
            // 2. Height / Level Dimension
            var heightRefs = new ReferenceArray();
            if (faces.TopFace?.Reference is not null && faces.BottomFace?.Reference is not null)
            {
                heightRefs.Append(ToLinearReference(document, faces.TopFace));
                heightRefs.Append(ToLinearReference(document, faces.BottomFace));

                if (heightRefs.Size == 2)
                {
                    var line = ElevationHeightLine(view, faces, settings);
                    document.Create.NewDimension(view, line, heightRefs, settings.DimensionType);
                    created++;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not create height dimension on elevation view {View}; skipping.", view.Name);
        }

        return created;
    }

    public static int CreateOnSection(
        Document document,
        ViewSection view,
        SpanFaces faces,
        BeamSpan span,
        BeamAnnotationSettings settings)
    {
        if (settings.DimensionType is null) return 0;
        int created = 0;

        // 1. Width Dimension B
        try
        {
            if (faces.LeftVertical?.Reference is not null && faces.RightVertical?.Reference is not null)
            {
                var refs = new ReferenceArray();
                refs.Append(ToLinearReference(document, faces.LeftVertical));
                refs.Append(ToLinearReference(document, faces.RightVertical));

                var line = SectionWidthLine(view, faces, settings);
                document.Create.NewDimension(view, line, refs, settings.DimensionType);
                created++;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not create width dimension on section view {View}; skipping.", view.Name);
        }

        // 2. Height Dimension H
        try
        {
            if (faces.TopHorizontal?.Reference is not null && faces.BottomHorizontal?.Reference is not null)
            {
                var refs = new ReferenceArray();
                refs.Append(ToLinearReference(document, faces.TopHorizontal));
                refs.Append(ToLinearReference(document, faces.BottomHorizontal));

                var line = SectionHeightLine(view, faces, settings);
                document.Create.NewDimension(view, line, refs, settings.DimensionType);
                created++;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not create height dimension on section view {View}; skipping.", view.Name);
        }

        return created;
    }

    private static Line ElevationSpanLine(ViewSection view, BeamFaces faces, BeamAnnotationSettings settings)
    {
        double offsetFt = RevitUnits.MmToFt(settings.DimensionOffsetV);
        XYZ start = faces.StartPoint - offsetFt * XYZ.BasisZ;
        XYZ end = faces.EndPoint - offsetFt * XYZ.BasisZ;
        return Line.CreateBound(start, end);
    }

    private static Line ElevationHeightLine(ViewSection view, BeamFaces faces, BeamAnnotationSettings settings)
    {
        double offsetFt = RevitUnits.MmToFt(settings.DimensionOffsetH);
        XYZ start = faces.StartPoint - offsetFt * faces.BeamAxis;
        XYZ end = start + RevitUnits.MmToFt(faces.MaxHeightMm) * XYZ.BasisZ;
        return Line.CreateBound(start, end);
    }

    private static Line SectionWidthLine(ViewSection view, SpanFaces faces, BeamAnnotationSettings settings)
    {
        double offsetFt = RevitUnits.MmToFt(settings.DimensionOffsetV);
        XYZ center = view.Origin;
        XYZ left = center - (RevitUnits.MmToFt(faces.WidthMm) * 0.5) * view.RightDirection + offsetFt * view.UpDirection;
        XYZ right = center + (RevitUnits.MmToFt(faces.WidthMm) * 0.5) * view.RightDirection + offsetFt * view.UpDirection;
        return Line.CreateBound(left, right);
    }

    private static Line SectionHeightLine(ViewSection view, SpanFaces faces, BeamAnnotationSettings settings)
    {
        double offsetFt = RevitUnits.MmToFt(settings.DimensionOffsetH);
        XYZ center = view.Origin;
        XYZ bottom = center - (RevitUnits.MmToFt(faces.WidthMm) * 0.5 + offsetFt) * view.RightDirection - (RevitUnits.MmToFt(faces.HeightMm) * 0.5) * view.UpDirection;
        XYZ top = bottom + RevitUnits.MmToFt(faces.HeightMm) * view.UpDirection;
        return Line.CreateBound(bottom, top);
    }

    public static int PlannedCount(BeamContinuousStack stack) => 2 + stack.Spans.Count * 2 * 2;
}
```

---

## 5. Rebar Schedule Annotations & Tags (`RebarTableTagCreator.cs`)

### 5.1 Tabular Schedule Layout beside Cross-Sections
To produce consistent, professional shop drawings matching the original tool without requiring complex sheet scheduling views, `RebarTableTagCreator` draws a structured table beside each cross-section view using `document.Create.NewDetailCurve` and `TextNote.Create`.

#### Row Definitions for Beam Sections
Each table consists of:
- **Title Row**: Section Mark and Dimensions (e.g. `Section 1-1 (300 x 600)`)
- **Row 1 - Main Top**: e.g. `Main Top: 3-T20`
- **Row 2 - Main Bottom**: e.g. `Main Bot: 3-T20`
- **Row 3 - Additional Top**: (if present) e.g. `Add Top: 2-T18`
- **Row 4 - Additional Bottom**: (if present) e.g. `Add Bot: 2-T16`
- **Row 5 - Side Bars**: (if $h \ge 700$) e.g. `Side Bars: 2x2-T12`
- **Row 6 - Stirrups**: e.g. `Stirrups: T10 @ 100` (or `@ 200` for midspan)

```csharp
namespace HPRebar.BeamRebar;

public static class RebarTableTagCreator
{
    private const double RowHeightFactor = 12.0;
    private const double LabelColumns = 6.0;
    private const double ValueColumns = 6.0;

    public static int Create(
        Document document,
        ViewSection view,
        BeamSpan span,
        int spanIndex,
        int cutIndex,
        BeamRebarSpec spec,
        BeamAnnotationSettings settings)
    {
        if (settings.TextNoteType is null)
        {
            Log.Warning("The document has no text note type; bar table was skipped.");
            return 0;
        }

        try
        {
            double rowHeight = CalculateRowHeight(document, settings);
            XYZ origin = CalculateTableOrigin(view, span, settings);
            var rows = BuildRows(span, cutIndex, spec);

            XYZ rightDir = view.RightDirection;
            XYZ upDir = view.UpDirection;

            for (int r = 0; r < rows.Count; r++)
            {
                XYZ rowTopLeft = origin - (r * rowHeight) * upDir;
                WriteRow(document, view, settings, rowTopLeft, rowHeight, rows[r].Label, rows[r].Value, rightDir, upDir);
            }

            return rows.Count;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not write rebar table in {View}; skipped.", view.Name);
            return 0;
        }
    }

    private static IReadOnlyList<(string Label, string Value)> BuildRows(
        BeamSpan span, 
        int cutIndex, 
        BeamRebarSpec spec)
    {
        var list = new List<(string, string)>();
        list.Add(("Section", $"{span.Width:0}x{span.Height:0}"));
        list.Add(("Main Top", $"{spec.MainBars.TopBarCount}-{spec.MainBars.TopBarType.Name}"));
        list.Add(("Main Bot", $"{spec.MainBars.BottomBarCount}-{spec.MainBars.BottomBarType.Name}"));

        if (cutIndex == 0 && spec.AdditionalTopBars.Count > 0)
        {
            list.Add(("Add Top", $"{spec.AdditionalTopBars.BarCount}-{spec.AdditionalTopBars.BarType.Name}"));
        }
        else if (cutIndex == 1 && spec.AdditionalBottomBars.Count > 0)
        {
            list.Add(("Add Bot", $"{spec.AdditionalBottomBars.BarCount}-{spec.AdditionalBottomBars.BarType.Name}"));
        }

        if (span.Height >= 700 && spec.SideBars.TotalBarCount > 0)
        {
            list.Add(("Side Bars", $"{spec.SideBars.RowsPerSide * 2}-{spec.SideBars.BarType.Name}"));
        }

        double spacing = cutIndex == 1 ? spec.Stirrups.MidspanSpacing : spec.Stirrups.SupportSpacing;
        list.Add(("Stirrup", $"{spec.Stirrups.BarType.Name} @ {spacing:0}"));

        return list;
    }

    private static void WriteRow(
        Document document,
        ViewSection view,
        BeamAnnotationSettings settings,
        XYZ topLeft,
        double rowHeight,
        string label,
        string value,
        XYZ rightDir,
        XYZ upDir)
    {
        double labelWidth = LabelColumns * rowHeight;
        double valueWidth = ValueColumns * rowHeight;

        XYZ topMid = topLeft + labelWidth * rightDir;
        XYZ topRight = topMid + valueWidth * rightDir;
        XYZ bottomLeft = topLeft - rowHeight * upDir;
        XYZ bottomMid = topMid - rowHeight * upDir;
        XYZ bottomRight = topRight - rowHeight * upDir;

        // Draw 5 bounding line segments for the 2-column box
        var lines = new[]
        {
            Line.CreateBound(topLeft, topRight),
            Line.CreateBound(bottomLeft, bottomRight),
            Line.CreateBound(topLeft, bottomLeft),
            Line.CreateBound(topMid, bottomMid),
            Line.CreateBound(topRight, bottomRight)
        };

        foreach (var line in lines)
        {
            document.Create.NewDetailCurve(view, line);
        }

        TextNote.Create(document, view.Id, topLeft, label, settings.TextNoteType!.Id);
        TextNote.Create(document, view.Id, topMid, value, settings.TextNoteType.Id);
    }

    private static double CalculateRowHeight(Document document, BeamAnnotationSettings settings)
    {
        double textSize = settings.TextNoteType?.get_Parameter(BuiltInParameter.TEXT_SIZE)?.AsDouble() ?? 0.0;
        double baseHeight = textSize > 0 ? RowHeightFactor * textSize : RevitUnits.MmToFt(60.0);

        int scale = settings.SectionTemplate?.get_Parameter(BuiltInParameter.VIEW_SCALE)?.AsInteger() ?? 0;
        return scale > 0 ? baseHeight * (100.0 / scale) : baseHeight;
    }

    private static XYZ CalculateTableOrigin(ViewSection view, BeamSpan span, BeamAnnotationSettings settings)
    {
        double widthFt = RevitUnits.MmToFt(span.Width);
        double heightFt = RevitUnits.MmToFt(span.Height);
        double offsetFt = RevitUnits.MmToFt(settings.TableOffset);

        // Position clear of right face of section, at top elevation of section
        return view.Origin 
            + (widthFt * 0.5 + offsetFt) * view.RightDirection 
            + (heightFt * 0.5) * view.UpDirection;
    }
}
```

### 5.2 Native Rebar Tagging on Elevation (`TagRebarOnElevation`)
For native Revit schedule integration, generated rebar elements (stirrup sets, main bars, additional bars) can optionally receive native `IndependentTag` elements:

```csharp
    public static void TagRebarOnElevation(
        Document document,
        ViewSection elevationView,
        Rebar rebar,
        XYZ headPosition)
    {
        try
        {
            IndependentTag.Create(
                document,
                elevationView.Id,
                new Reference(rebar),
                addLeader: true,
                TagMode.TM_ADDBY_CATEGORY,
                TagOrientation.Horizontal,
                headPosition);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not place rebar tag on rebar {RebarId}; skipping.", rebar.Id);
        }
    }
```

---

## 6. Atomic Transaction Management (`BeamRebarOrchestrator.cs`)

### 6.1 Transaction Architecture & Lifecycle
`BeamRebarOrchestrator` is the **sole owner** of the master `TransactionGroup("Beam Rebar")`.
- **Pre-condition check**: Before opening any transaction, verify that all required rebar shapes, bar types, and cover types exist in the document via `BeamRebarCreationService.CanCreate(...)`.
- **Atomic Rollback**: If an unhandled exception occurs at any point during view, dimension, rebar, or annotation creation, the entire `TransactionGroup` is rolled back (`group.RollBack()`). This guarantees zero orphaned views or partial rebar elements.
- **Single Undo Step**: Upon successful completion, `group.Assimilate()` combines all internal transactions into a single entry named `"Beam Rebar"` in Revit's undo queue.
- **Warning Suppression**: Every internal transaction invokes `RebarFailureHandling.Apply(transaction)` immediately after `transaction.Start()`.

### 6.2 Internal Transactions Sequence
```
┌───────────────────────────────────────────────────────────┐
│ TransactionGroup("Beam Rebar")                            │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 1. Transaction("Create Detail View")                  │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 2. Transaction("Create Section Views")                │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 3. Transaction("Create Elevation Dimensions")         │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 4. Transaction("Create Section Dimensions")           │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 5. Transaction("Create Beam Stirrups")                │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 6. Transaction("Create Main Bars")                    │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 7. Transaction("Create Additional Bars")              │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 8. Transaction("Create Side Bars")                    │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 9. Transaction("Create Special Hanging Bars")         │ │
│ └───────────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ 10. Transaction("Create Rebar Tables & Tags")         │ │
│ └───────────────────────────────────────────────────────┘ │
│ group.Assimilate()                                        │
└───────────────────────────────────────────────────────────┘
```

### 6.3 Implementation Structure

```csharp
namespace HPRebar.BeamRebar;

public sealed class BeamRebarOrchestrator
{
    private readonly Document _document;
    private readonly BeamContinuousStack _stack;
    private readonly BeamFaces _faces;
    private readonly RebarShapeResolver _shapes;
    private readonly BeamAnnotationSettings _settings;

    public BeamRebarOrchestrator(
        Document document,
        BeamContinuousStack stack,
        BeamFaces faces,
        RebarShapeResolver shapes,
        BeamAnnotationSettings settings)
    {
        _document = document;
        _stack = stack;
        _faces = faces;
        _shapes = shapes;
        _settings = settings;
    }

    public int PlannedCount(BeamRebarSpec spec) =>
        1                                               // Elevation detail view
        + _stack.Spans.Count * _settings.SectionsPerSpan // Cross section views
        + DimensionCreator.PlannedCount(_stack)
        + _stack.Spans.Count * _settings.SectionsPerSpan // Tables
        + BeamRebarCreationService.PlannedCount(_stack, spec);

    public BeamOrchestratorResult Run(BeamRebarSpec spec, IProgress<int>? progress = null)
    {
        var ready = BeamRebarCreationService.CanCreate(_shapes, _stack, spec);
        if (!ready.IsOk)
        {
            return BeamOrchestratorResult.Invalid(ready);
        }

        using var group = new TransactionGroup(_document, "Beam Rebar");
        group.Start();

        try
        {
            int done = 0;

            // 1. Create Views
            var views = CreateViews(progress, ref done);

            // 2. Create Dimensions
            CreateDimensions(views, progress, ref done);

            // 3. Create Reinforcement Elements
            var rebar = BeamRebarCreationService.Create(
                _document, _stack, _faces, spec, _shapes,
                new Progress<int>(v => progress?.Report(done + v)));

            done += rebar.Total;

            // 4. Create Schedule Tables
            CreateTables(views, spec, progress, ref done);

            group.Assimilate();

            Log.Information("Beam Rebar completed successfully: {Views} view(s), {Rebar} rebar element(s).",
                views.Total, rebar.Total);

            return new BeamOrchestratorResult
            {
                Views = views,
                Rebar = rebar,
                Validation = ValidationResult.Ok()
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Beam Rebar creation failed; rolling back all document mutations.");
            group.RollBack();
            throw;
        }
    }

    private CreatedBeamViews CreateViews(IProgress<int>? progress, ref int done)
    {
        ViewSection? detailView = null;
        using (var t = new Transaction(_document, "Create Detail View"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            detailView = DetailViewCreator.Create(_document, _stack, _faces, _settings);
            t.Commit();
        }
        progress?.Report(++done);

        IReadOnlyList<ViewSection> sectionViews;
        using (var t = new Transaction(_document, "Create Section Views"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            sectionViews = SectionViewCreator.Create(_document, _stack, _faces, _settings);
            t.Commit();
        }
        done += sectionViews.Count;
        progress?.Report(done);

        return new CreatedBeamViews { DetailView = detailView, SectionViews = sectionViews };
    }

    private void CreateDimensions(CreatedBeamViews views, IProgress<int>? progress, ref int done)
    {
        if (views.DetailView is not null)
        {
            using var t = new Transaction(_document, "Create Elevation Dimensions");
            t.Start();
            RebarFailureHandling.Apply(t);
            done += DimensionCreator.CreateOnElevation(_document, views.DetailView, _stack, _faces, _settings);
            t.Commit();
            progress?.Report(done);
        }

        using (var t = new Transaction(_document, "Create Section Dimensions"))
        {
            t.Start();
            RebarFailureHandling.Apply(t);
            for (int i = 0; i < views.SectionViews.Count && i < _faces.SpanFaceList.Count; i++)
            {
                int spanIdx = i / _settings.SectionsPerSpan;
                done += DimensionCreator.CreateOnSection(
                    _document, views.SectionViews[i], _faces.SpanFaceList[spanIdx], _stack.Spans[spanIdx], _settings);
            }
            t.Commit();
        }
        progress?.Report(done);
    }

    private void CreateTables(CreatedBeamViews views, BeamRebarSpec spec, IProgress<int>? progress, ref int done)
    {
        using var t = new Transaction(_document, "Create Beam Tables");
        t.Start();
        RebarFailureHandling.Apply(t);

        int viewIndex = 0;
        for (int spanIndex = 0; spanIndex < _stack.Spans.Count; spanIndex++)
        {
            for (int cutIndex = 0; cutIndex < _settings.SectionsPerSpan && viewIndex < views.SectionViews.Count; cutIndex++)
            {
                RebarTableTagCreator.Create(
                    _document, views.SectionViews[viewIndex], _stack.Spans[spanIndex], spanIndex, cutIndex, spec, _settings);
                viewIndex++;
                done++;
            }
        }

        t.Commit();
        progress?.Report(done);
    }
}
```

---

## 7. Decoupled Runner Bridge (`RevitRebarRunner.cs`)

### 7.1 Interface Contract (`IBeamRebarRunner`)
The runner pattern insulates the MVVM layer from direct Revit API classes (`Document`, `TransactionGroup`), enabling clean mock testing without Revit:

```csharp
namespace HPRebar.BeamRebar.ViewModels;

public interface IBeamRebarRunner
{
    int PlannedCount(BeamRebarSpec spec);
    int Run(BeamRebarSpec spec, IProgress<int> progress);
    BeamOrchestratorResult? LastResult { get; }
}
```

### 7.2 Implementation (`RevitRebarRunner`)

```csharp
namespace HPRebar.BeamRebar;

public sealed class RevitRebarRunner : IBeamRebarRunner
{
    private readonly BeamRebarOrchestrator _orchestrator;

    public RevitRebarRunner(BeamRebarOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public BeamOrchestratorResult? LastResult { get; private set; }

    public int PlannedCount(BeamRebarSpec spec) => _orchestrator.PlannedCount(spec);

    public int Run(BeamRebarSpec spec, IProgress<int> progress)
    {
        var result = _orchestrator.Run(spec, progress);
        if (!result.IsOk)
        {
            throw new InvalidOperationException(result.Validation.Message);
        }

        LastResult = result;
        return result.Total;
    }
}
```

---

## 8. ExternalCommand Entry Point (`BeamRebarCommand.cs`)

### 8.1 Workflow Execution Flow
1. **Interactive Selection**: User prompts `PickObjects` filtered to `BuiltInCategory.OST_StructuralFraming` using `StructuralFramingSelectionFilter`.
2. **User Cancellation Guard**: Catches `Autodesk.Revit.Exceptions.OperationCanceledException` cleanly if the user presses `Esc`.
3. **Geometric Validation**: Validates collinear axis, continuity, valid bounding boxes, and cross-section parameters via `BeamStackValidator.Validate(...)`.
4. **Stack & Face Reading**: Extracts ordered spans, support nodes, secondary intersections, and boundary planar faces via `BeamStackReader.Read(...)` and `BeamSolidFaceReader.Read(...)`.
5. **Spec & Resource Initialization**:
   - Builds default reinforcement parameters via `DefaultBeamRebarSpecBuilder.Build(...)`.
   - Resolves available rebar shapes via `RebarShapeResolver.Load(...)`.
   - Resolves annotation types and view templates via `BeamAnnotationSettings.Load(...)`.
6. **MVVM View Instantiation & Display**:
   - Instantiates `BeamRebarOrchestrator` and `RevitRebarRunner`.
   - Constructs `BeamRebarViewModel` and `BeamRebarView`.
   - Sets Revit main window owner handle via `WindowInteropHelper`.
   - Applies active Revit UI theme (Dark/Light) via `ThemeSwitcher.ApplyFromRevit(view)`.
   - Opens modal dialog: `view.ShowDialog()`.
7. **Success Feedback**: Shows `RevitDialogs.Info` detailing the number of views and rebar elements generated.
8. **Top-Level Fault Containment**: Catches any unexpected exception, logs via `Log.Error`, and displays user-friendly `RevitDialogs.Error`.

```csharp
namespace HPRebar.BeamRebar;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class BeamRebarCommand : ExternalCommand
{
    public override void Execute()
    {
        var uiDocument = Application.ActiveUIDocument;
        var document = uiDocument.Document;

        IList<Reference> references;
        try
        {
            references = uiDocument.Selection.PickObjects(
                ObjectType.Element,
                new StructuralFramingSelectionFilter(),
                "Select continuous structural beam spans in order from left to right");
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return; // User pressed Escape
        }

        if (references.Count == 0) return;

        try
        {
            var selectedBeams = references
                .Select(r => document.GetElement(r))
                .Where(e => e is not null)
                .ToList();

            var validation = BeamStackValidator.Validate(document, selectedBeams);
            if (!validation.IsOk)
            {
                Log.Warning("Beam Rebar validation failed: {Message}", validation.Message);
                RevitDialogs.Error("Beam Rebar", validation.Message);
                return;
            }

            var (stack, faces) = BeamStackReader.Read(document, selectedBeams);
            Log.Information("Beam Rebar loaded continuous stack with {Spans} span(s) and {Supports} support(s).",
                stack.Spans.Count, stack.Supports.Count);

            var spec = DefaultBeamRebarSpecBuilder.Build(document, stack);
            var shapes = RebarShapeResolver.Load(document);

            var ready = BeamRebarCreationService.CanCreate(shapes, stack, spec);
            if (!ready.IsOk)
            {
                RevitDialogs.Error("Beam Rebar", ready.Message);
                return;
            }

            var annotation = BeamAnnotationSettings.Load(document, stack);
            var orchestrator = new BeamRebarOrchestrator(document, stack, faces, shapes, annotation);
            var runner = new RevitRebarRunner(orchestrator);

            var barTypes = RebarTypeCatalog.BarTypes(document);
            var session = new BeamRebarSession(stack, faces, spec, barTypes);
            var viewModel = new BeamRebarViewModel(session, new LocalizationService(), runner);
            var view = new BeamRebarView(viewModel);

            new WindowInteropHelper(view).Owner = Application.MainWindowHandle;
            ThemeSwitcher.ApplyFromRevit(view);

            if (view.ShowDialog() != true || runner.LastResult is null) return;

            var created = runner.LastResult;
            RevitDialogs.Info(
                "Beam Rebar",
                $"Created {created.Views.Total} view(s) and {created.Rebar.Total} rebar element(s):\n" +
                $"- {created.Rebar.Stirrups.Count} stirrup set(s)\n" +
                $"- {created.Rebar.MainTopBars.Count + created.Rebar.MainBottomBars.Count} main bar(s)\n" +
                $"- {created.Rebar.AdditionalTopBars.Count + created.Rebar.AdditionalBottomBars.Count} additional bar(s)\n" +
                $"- {created.Rebar.SideBars.Count} side bar(s)\n" +
                $"- {created.Rebar.SpecialBars.Count} special hanging bar(s)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Beam Rebar command failed during execution.");
            RevitDialogs.Error("Beam Rebar", $"An unexpected error occurred:\n{ex.Message}");
        }
    }
}
```

---

## 9. Supporting Infrastructure Services

### 9.1 Warning Suppression Preprocessor (`RebarFailureHandling.cs`)
Revit raises non-fatal geometry overlap warnings during rebar generation (e.g. rebar slightly outside host, bar curve touches stirrup). If unhandled, modal dialogs halt background execution.

```csharp
namespace HPRebar.BeamRebar;

internal static class RebarFailureHandling
{
    public static void Apply(Transaction transaction)
    {
        var options = transaction.GetFailureHandlingOptions();
        options = options.SetFailuresPreprocessor(new SwallowWarnings());
        options = options.SetClearAfterRollback(true);
        transaction.SetFailureHandlingOptions(options);
    }

    private sealed class SwallowWarnings : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            foreach (var failure in accessor.GetFailureMessages())
            {
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    Log.Warning("Revit non-fatal warning swallowed: {Warning}", failure.GetDescriptionText());
                    accessor.DeleteWarning(failure);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
```

### 9.2 Universal Unit Conversion (`RevitUnits.cs`)
Enforces the boundary between `HPRebar.Core` (millimetres) and Revit internal representation (decimal feet). Adheres to zero-deprecation rules by using `UnitTypeId.Millimeters` and `SpecTypeId.Length`:

```csharp
namespace HPRebar.BeamRebar;

internal static class RevitUnits
{
    public static double MmToFt(double mm) => 
        UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

    public static double FtToMm(double ft) => 
        UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);

    public static string Display(Document doc, double ft) => 
        UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, ft, false);
}
```

### 9.3 Dynamic Localization Service (`LocalizationService.cs`)
Provides instant, reactive switching between English and Vietnamese UI strings without window recreation:

```csharp
namespace HPRebar.BeamRebar;

public sealed partial class LocalizationService : ObservableObject
{
    [ObservableProperty]
    private UiStrings _strings = UiStringsCatalog.English;

    public bool IsVietnamese => ReferenceEquals(Strings, UiStringsCatalog.Vietnamese);

    public void Toggle() =>
        Strings = IsVietnamese ? UiStringsCatalog.English : UiStringsCatalog.Vietnamese;
}
```

### 9.4 Revit UI Theme Switching (`ThemeSwitcher.cs`)
Swaps merged resource dictionaries between `ThemeDark.xaml` and `ThemeLight.xaml`. Includes multi-version compilation for Revit 2024+ (`UIThemeManager`):

```csharp
namespace HPRebar.BeamRebar;

public static class ThemeSwitcher
{
    private const string DarkUri = "pack://application:,,,/HPRebar;component/Resources/Themes/ThemeDark.xaml";
    private const string LightUri = "pack://application:,,,/HPRebar;component/Resources/Themes/ThemeLight.xaml";

    public static void ApplyFromRevit(FrameworkElement target) => 
        Apply(target, RevitPrefersDark());

    public static void Apply(FrameworkElement target, bool dark)
    {
        var wanted = new Uri(dark ? DarkUri : LightUri);
        var merged = target.Resources.MergedDictionaries;

        for (int i = 0; i < merged.Count; i++)
        {
            if (!IsColourDictionary(merged[i])) continue;
            if (merged[i].Source == wanted) return;
            merged[i] = new ResourceDictionary { Source = wanted };
            return;
        }

        foreach (var dict in merged)
        {
            for (int i = 0; i < dict.MergedDictionaries.Count; i++)
            {
                if (!IsColourDictionary(dict.MergedDictionaries[i])) continue;
                if (dict.MergedDictionaries[i].Source == wanted) return;
                dict.MergedDictionaries[i] = new ResourceDictionary { Source = wanted };
                return;
            }
        }
    }

    private static bool IsColourDictionary(ResourceDictionary dict)
    {
        var src = dict.Source?.OriginalString;
        return src is not null && (src.EndsWith("ThemeDark.xaml", StringComparison.OrdinalIgnoreCase)
                                   || src.EndsWith("ThemeLight.xaml", StringComparison.OrdinalIgnoreCase));
    }

    private static bool RevitPrefersDark()
    {
#if REVIT2024_OR_GREATER
        return Autodesk.Revit.UI.UIThemeManager.CurrentTheme == Autodesk.Revit.UI.UITheme.Dark;
#else
        return true;
#endif
    }
}
```

### 9.5 TaskDialog Wrappers (`RevitDialogs.cs`)
Replaces Windows Forms message boxes with native Revit TaskDialogs:

```csharp
namespace HPRebar.BeamRebar;

internal static class RevitDialogs
{
    public static void Error(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconError, MainInstruction = title, MainContent = message }.Show();

    public static void Warning(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconWarning, MainInstruction = title, MainContent = message }.Show();

    public static void Info(string title, string message) =>
        new TaskDialog(title) { MainIcon = TaskDialogIcon.TaskDialogIconInformation, MainInstruction = title, MainContent = message }.Show();

    public static bool Confirm(string title, string message)
    {
        var dialog = new TaskDialog(title)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconWarning,
            MainInstruction = title,
            MainContent = message,
            CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
            DefaultButton = TaskDialogResult.No
        };
        return dialog.Show() == TaskDialogResult.Yes;
    }
}
```

---

## 10. Data Models for Views & Orchestrator (`Models/`)

### 10.1 `CreatedBeamViews.cs`
```csharp
namespace HPRebar.BeamRebar.Models;

public sealed record CreatedBeamViews
{
    public ViewSection? DetailView { get; init; }
    public IReadOnlyList<ViewSection> SectionViews { get; init; } = Array.Empty<ViewSection>();
    public int Total => (DetailView is null ? 0 : 1) + SectionViews.Count;
}
```

### 10.2 `CreatedBeamRebar.cs`
```csharp
namespace HPRebar.BeamRebar.Models;

public sealed record CreatedBeamRebar
{
    public IReadOnlyList<Rebar> Stirrups { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> MainTopBars { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> MainBottomBars { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> AdditionalTopBars { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> AdditionalBottomBars { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> SideBars { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> SpecialBars { get; init; } = Array.Empty<Rebar>();

    public int Total => Stirrups.Count 
        + MainTopBars.Count 
        + MainBottomBars.Count 
        + AdditionalTopBars.Count 
        + AdditionalBottomBars.Count 
        + SideBars.Count 
        + SpecialBars.Count;
}
```

### 10.3 `BeamOrchestratorResult.cs`
```csharp
namespace HPRebar.BeamRebar.Models;

public sealed record BeamOrchestratorResult
{
    public CreatedBeamViews Views { get; init; } = new();
    public CreatedBeamRebar Rebar { get; init; } = new();
    public ValidationResult Validation { get; init; } = ValidationResult.Ok();

    public bool IsOk => Validation.IsOk;
    public int Total => Views.Total + Rebar.Total;

    public static BeamOrchestratorResult Invalid(ValidationResult validation) =>
        new() { Validation = validation };
}
```

### 10.4 `BeamAnnotationSettings.cs`
```csharp
namespace HPRebar.BeamRebar.Models;

public sealed class BeamAnnotationSettings
{
    public View? DetailTemplate { get; set; }
    public View? SectionTemplate { get; set; }
    public DimensionType? DimensionType { get; set; }
    public ElementType? TextNoteType { get; set; }

    public double DimensionOffsetH { get; set; }
    public double DimensionOffsetV { get; set; }
    public double TableOffset { get; set; }
    public double ViewMargin { get; set; }

    public string DetailViewName { get; set; } = "Beam Detail";
    public string SectionPrefix { get; set; } = "Sec";
    public int SectionsPerSpan { get; set; } = 3; // 2 or 3 sections per span

    public static BeamAnnotationSettings Load(Document document, BeamContinuousStack stack)
    {
        double maxSizeMm = stack.MaxHeight;

        var templates = new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .WhereElementIsNotElementType()
            .Cast<View>()
            .Where(v => v.IsTemplate)
            .ToList();

        var structural = templates.FirstOrDefault(v => 
            v.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE)?.AsValueString() == "Structural")
            ?? templates.FirstOrDefault();

        var dimensionTypes = new FilteredElementCollector(document)
            .OfClass(typeof(DimensionType))
            .Cast<DimensionType>()
            .ToList();

        var textTypes = new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfClass(typeof(TextNoteType))
            .Cast<ElementType>()
            .ToList();

        return new BeamAnnotationSettings
        {
            DetailTemplate = structural,
            SectionTemplate = structural,
            DimensionType = dimensionTypes.FirstOrDefault(d => d.FamilyName == "Linear Dimension Style") 
                            ?? dimensionTypes.FirstOrDefault(),
            TextNoteType = textTypes.FirstOrDefault(),
            DimensionOffsetH = maxSizeMm * 0.5,
            DimensionOffsetV = maxSizeMm * 0.5,
            TableOffset = maxSizeMm * 0.5,
            ViewMargin = maxSizeMm * 0.8,
            SectionsPerSpan = 3
        };
    }

    public string SectionViewName(int spanIndex, int cutIndex) => 
        $"{DetailViewName} - Span {spanIndex} - {SectionPrefix} {cutIndex}";
}
```

---

## 11. Verification & Testing Strategy

### 11.1 Multi-Version Build Verification
The implementation must build cleanly without warnings or errors across the configured Revit SDK targets:
```bash
dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
```

### 11.2 Deprecation Guardrail Verification
Grep codebase to verify zero deprecated Revit API usages:
- No `DisplayUnitType` (use `UnitTypeId.Millimeters`).
- No `UnitType` (use `SpecTypeId.Length`).
- No unqualified `ElementId.IntegerValue` without `#if !REVIT2024_OR_GREATER`.
- Direct enum comparison for `elem.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming`.

### 11.3 Architectural Cleanliness Verification
- `HPRebar.Core` contains zero references to `Autodesk.Revit.*`.
- All files reside strictly within `HPRebar/HPRebar/Beam Rebar/` and subfolders `Models/`, `View/`, `View Models/`.
- All C# namespaces use file-scoped declarations: `namespace HPRebar.BeamRebar;`.
