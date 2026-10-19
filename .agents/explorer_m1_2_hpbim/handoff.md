# Handoff Report: Blueprint for Domain BIM Schema & Spatial Algorithms (HPBim M1.2)

**Author:** `teamwork_preview_explorer`  
**Milestone:** M1: Domain BIM Schema & Spatial Algorithms (Subtask M1.2)  
**Target Solution:** `HPBim/` (`src/HPBim.Core/`)  
**Date:** 2026-10-05T23:45:00Z  

---

## 1. Observation

### 1.1 Source Repository & Ground-Truth Context
1. **Python Source Inspection (`vn-autocad-skill-main`):**
   - Direct inspection of `vn-autocad-skill-main/vncad/arch.py:27-55`: The Python `wall(msp, p1, p2, t, openings)` function splits a wall centerline into solid segments between door/window cuts (`cuts = sorted(openings)`), computing 4-vertex polylines with offset $\pm t/2$ along normal vector $n = (-dy/L, dx/L)$.
   - `vncad/arch.py:76-112`: Defines single doors, double doors, and windows with swing arcs and offset lines.
   - `examples/nha_pho_5x18m/layout.py:8-100`: Defines the benchmark ground-truth:
     - 5 orthogonal X-grids ($X_1=0$, $X_2=5500$, $X_3=9500$, $X_4=13000$, $X_5=18000$) and 2 Y-grids ($Y_A=0$, $Y_B=5000$).
     - 5 Levels: `TRET` (+0.150m), `LAU 1` (+3.800m), `LAU 2` (+7.200m), `SAN THUONG` (+10.600m), `MAI CHE THANG` (+13.400m).
     - 10 RC columns 220x220 mm located at all grid intersections.
     - 26 walls across 3 floors (exterior 220mm, interior partitions 110mm) hosting doors (D1-D6), windows (S1-S3), and open pass-throughs.
     - 16 functional rooms with theoretical areas ($26.4\text{ m}^2$, $19.2\text{ m}^2$, $3.0\text{ m}^2$, $24.0\text{ m}^2$, etc.) and tag centerpoints.
     - MEP fixtures (sanitary ware, furniture, kitchen, electrical distribution panel).
   - `examples/nha_pho_5x18m/draw_structure.py:75-105`: Defines floor beams (D1 200x400, D2 200x300, D3 200x300), ground beams GM (250x400), slab zones S1..S4 (100mm), and void openings.
   - `vncad/struct.py:19-22`: Defines rebar unit weight formula:
     $$W = \frac{D^2}{162.2} \times L \quad (\text{kg})$$

2. **Clean Code Governance Directives (`HP_CLEAN_CODE_CORE.md`):**
   - Methods $\le 50$ lines (M3, PCC-067, PCC-093).
   - Maximum parameters per method $\le 4$ (M4).
   - Classes $\le 300$ lines (C2, PCC-115, PCC-184).
   - 0 empty `catch` blocks without explanatory comment (Q-B2).
   - No generic junk classes (`*Manager`, `*Helper`, `*Utils`, `*Processor`, `*Data`, `*Info`) (PCC-031, PCC-032).
   - Target framework: `netstandard2.0` with `Polyfill 11.0.1` and `Nullable=enable`.
   - Complete domain isolation: 0 references to `Autodesk.Revit.*` in `HPBim.Core`. All units in millimeters ($mm$) and degrees ($^\circ$).

---

## 2. Logic Chain

1. **Model Architectural Paradigm:**
   - In `vncad`, building components were represented as unstructured tuples (`((0,0), (18000,0), 220, [(15600, 1600, "wind")])`).
   - For `HPBim.Core`, these must be modeled as strongly-typed, immutable C# `sealed record` DTOs with `{ get; init; }` properties.
   - Records provide value-based equality, non-destructive mutation (`with { ... }`), concise syntax, and native JSON serialization compatibility with System.Text.Json.
   - To keep parameter counts under 4 (Rule M4), models use parameterless constructors with init properties or focused factory methods.

2. **Decoupling Geometry Math from Domain Models:**
   - Sibling explorer `explorer_m1_1_hpbim` is designing `Point2D`, `Point3D`, `Vector2D`, `Polygon2D`, and `BoundingBox2D` under `HPBim.Core.Geometry`.
   - `HPBim.Core.Models` consumes these pure geometry structs directly. No duplicate geometry math is introduced inside the model classes.

3. **Spatial Algorithm Separation:**
   - Rather than embedding complex spatial computation inside models, spatial algorithms are isolated into dedicated single-responsibility domain service classes under `HPBim.Core.Algorithms`:
     - `WallSegmenter`: Converts continuous wall centerline and hosted openings into solid 2D spans and 4-corner polygons.
     - `RoomAreaCalculator`: Computes Gauss/Shoelace polygon area in $m^2$, verifies boundary enclosure, and performs ray-casting point-in-polygon verification for room tag placement.
     - `OpeningPlacementEngine`: Translates host wall coordinates and opening distance into exact 2D/3D insertion points and computes door swing clearance zones for clash analysis.

---

## 3. Caveats

1. **Revit Unit Conversion Boundary:**
   - In `HPBim.Core`, all dimensions are strictly in millimeters ($mm$), areas in square meters ($m^2$), and angles in degrees ($^\circ$).
   - Revit API internally operates in imperial feet ($1\text{ ft} = 304.8\text{ mm}$). This conversion belongs exclusively in `HPBim.Client` when generating Revit API scripts; `HPBim.Core` remains 100% metric.
2. **Wall Justification / Centerline Convention:**
   - In accordance with `vncad` and standard architectural modeling, `WallDef.StartPoint` and `EndPoint` represent the **wall centerline**. Wall thickness $t$ expands symmetrically $\pm t/2$ along the normal vector $n$.
3. **Geometry Primitives Dependency:**
   - `WallSegmenter`, `RoomAreaCalculator`, and `OpeningPlacementEngine` depend on `HPBim.Core.Geometry` (`Point2D`, `Point3D`, `Vector2D`, `Polygon2D`). If `Polygon2D.Area` returns square millimeters ($mm^2$), the algorithm divides by $1,000,000.0$ to yield $m^2$.

---

## 4. Conclusion & Implementation Blueprint

### 4.1 Enums Blueprint (`src/HPBim.Core/Models/`)

All enums are placed in file-scoped namespaces `HPBim.Core.Models`:

```csharp
namespace HPBim.Core.Models;

public enum GridDirection
{
    X = 0, // Fixed X coordinate, line extends along Y
    Y = 1  // Fixed Y coordinate, line extends along X
}

public enum OpeningKind
{
    Door = 0,    // Single leaf door
    Door2 = 1,   // Double leaf door
    Window = 2,  // Window
    Opening = 3  // Cased opening / wall cutout
}

public enum OpeningSwing
{
    None = 0,
    Left = 1,
    Right = 2,
    Both = 3
}

public enum MepCategory
{
    Sanitary = 0,
    Kitchen = 1,
    Furniture = 2,
    Electrical = 3,
    Mechanical = 4
}

public enum PipeSystemType
{
    WaterSupply = 0,
    SoilDrain = 1,
    WasteDrain = 2
}

public enum RebarShapeType
{
    Straight = 0,
    HookEnd = 1,
    HookBoth = 2,
    ClosedStirrup = 3,
    CapStirrup = 4,
    UStirrup = 5
}
```

---

### 4.2 Domain BIM Schema Models (`src/HPBim.Core/Models/`)

#### 1. `AxisGrid.cs`
- **Path:** `src/HPBim.Core/Models/AxisGrid.cs`
- **Class:** `public sealed record AxisGrid`
```csharp
namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record AxisGrid
{
    public string Name { get; init; } = string.Empty;
    public double CoordinateMm { get; init; }
    public GridDirection Direction { get; init; }
    public Point2D StartPoint { get; init; }
    public Point2D EndPoint { get; init; }

    public double LengthMm => StartPoint.DistanceTo(EndPoint);

    public AxisGrid() { }

    public AxisGrid(string name, double coordinateMm, GridDirection direction, Point2D startPoint, Point2D endPoint)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CoordinateMm = coordinateMm;
        Direction = direction;
        StartPoint = startPoint;
        EndPoint = endPoint;
    }

    public static AxisGrid CreateOrthogonal(
        string name, double coordinateMm, GridDirection direction, 
        double minExtentMm, double maxExtentMm, double extensionMm = 1500.0)
    {
        double pStart = minExtentMm - extensionMm;
        double pEnd = maxExtentMm + extensionMm;

        Point2D start = direction == GridDirection.X 
            ? new Point2D(coordinateMm, pStart) 
            : new Point2D(pStart, coordinateMm);

        Point2D end = direction == GridDirection.X 
            ? new Point2D(coordinateMm, pEnd) 
            : new Point2D(pEnd, coordinateMm);

        return new AxisGrid(name, coordinateMm, direction, start, end);
    }
}
```

#### 2. `LevelDef.cs`
- **Path:** `src/HPBim.Core/Models/LevelDef.cs`
- **Class:** `public sealed record LevelDef`
```csharp
namespace HPBim.Core.Models;

public sealed record LevelDef
{
    public string Name { get; init; } = string.Empty;
    public double ElevationM { get; init; }
    public double ElevationMm => ElevationM * 1000.0;
    public double HeightMm { get; init; }
    public int FloorNumber { get; init; }
    public bool IsStructural { get; init; } = true;

    public double TopElevationM => ElevationM + (HeightMm / 1000.0);
    public double TopElevationMm => ElevationMm + HeightMm;

    public LevelDef() { }

    public LevelDef(string name, double elevationM, double heightMm, int floorNumber, bool isStructural = true)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ElevationM = elevationM;
        HeightMm = heightMm;
        FloorNumber = floorNumber;
        IsStructural = isStructural;
    }
}
```

#### 3. `ColumnDef.cs`
- **Path:** `src/HPBim.Core/Models/ColumnDef.cs`
- **Class:** `public sealed record ColumnDef`
```csharp
namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record ColumnDef
{
    public string Id { get; init; } = string.Empty;
    public Point2D CenterLocation { get; init; }
    public double WidthMm { get; init; } = 220.0;
    public double DepthMm { get; init; } = 220.0;
    public string BaseLevel { get; init; } = string.Empty;
    public string TopLevel { get; init; } = string.Empty;
    public double BaseOffsetMm { get; init; }
    public double TopOffsetMm { get; init; }
    public double RotationDeg { get; init; }
    public string Material { get; init; } = "Concrete - B20";

    public BoundingBox2D GetBoundingBox()
    {
        double halfW = WidthMm / 2.0;
        double halfD = DepthMm / 2.0;
        return new BoundingBox2D(
            new Point2D(CenterLocation.X - halfW, CenterLocation.Y - halfD),
            new Point2D(CenterLocation.X + halfW, CenterLocation.Y + halfD));
    }
}
```

#### 4. `WallDef.cs`
- **Path:** `src/HPBim.Core/Models/WallDef.cs`
- **Class:** `public sealed record WallDef`
```csharp
namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record WallDef
{
    public string Id { get; init; } = string.Empty;
    public Point2D StartPoint { get; init; }
    public Point2D EndPoint { get; init; }
    public double ThicknessMm { get; init; } = 220.0;
    public string BaseLevel { get; init; } = string.Empty;
    public string TopLevel { get; init; } = string.Empty;
    public double BaseOffsetMm { get; init; }
    public double TopOffsetMm { get; init; }
    public IReadOnlyList<OpeningDef> Openings { get; init; } = Array.Empty<OpeningDef>();

    public double LengthMm => StartPoint.DistanceTo(EndPoint);

    public Vector2D Direction
    {
        get
        {
            double dx = EndPoint.X - StartPoint.X;
            double dy = EndPoint.Y - StartPoint.Y;
            double len = LengthMm;
            return len > 1e-6 ? new Vector2D(dx / len, dy / len) : new Vector2D(1, 0);
        }
    }

    public Vector2D Normal => new Vector2D(-Direction.Y, Direction.X);
}
```

#### 5. `OpeningDef.cs`
- **Path:** `src/HPBim.Core/Models/OpeningDef.cs`
- **Class:** `public sealed record OpeningDef`
```csharp
namespace HPBim.Core.Models;

public sealed record OpeningDef
{
    public string Id { get; init; } = string.Empty;
    public string HostWallId { get; init; } = string.Empty;
    public double DistanceAlongWallMm { get; init; }
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public double SillHeightMm { get; init; }
    public OpeningKind Kind { get; init; } = OpeningKind.Door;
    public OpeningSwing Swing { get; init; } = OpeningSwing.Left;
    public string? Description { get; init; }

    public double CenterDistanceMm => DistanceAlongWallMm + (WidthMm / 2.0);
    public double EndDistanceMm => DistanceAlongWallMm + WidthMm;
    public double HeadHeightMm => SillHeightMm + HeightMm;
}
```

#### 6. `BeamDef.cs`
- **Path:** `src/HPBim.Core/Models/BeamDef.cs`
- **Class:** `public sealed record BeamDef`
```csharp
namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record BeamDef
{
    public string Id { get; init; } = string.Empty;
    public Point2D StartPoint { get; init; }
    public Point2D EndPoint { get; init; }
    public double WidthMm { get; init; } = 200.0;
    public double HeightMm { get; init; } = 400.0;
    public string Level { get; init; } = string.Empty;
    public string BeamType { get; init; } = "D1";
    public bool IsTieBeam { get; init; }

    public double LengthMm => StartPoint.DistanceTo(EndPoint);

    public Vector2D Direction
    {
        get
        {
            double dx = EndPoint.X - StartPoint.X;
            double dy = EndPoint.Y - StartPoint.Y;
            double len = LengthMm;
            return len > 1e-6 ? new Vector2D(dx / len, dy / len) : new Vector2D(1, 0);
        }
    }
}
```

#### 7. `SlabDef.cs`
- **Path:** `src/HPBim.Core/Models/SlabDef.cs`
- **Class:** `public sealed record SlabDef`
```csharp
namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record SlabDef
{
    public string Id { get; init; } = string.Empty;
    public Polygon2D Boundary { get; init; } = new(Array.Empty<Point2D>());
    public double ThicknessMm { get; init; } = 100.0;
    public string Level { get; init; } = string.Empty;
    public double ElevationOffsetMm { get; init; }
    public IReadOnlyList<Polygon2D> Voids { get; init; } = Array.Empty<Polygon2D>();

    public double GrossAreaM2 => Boundary.Area / 1_000_000.0;
    public double NetAreaM2 => (Boundary.Area - Voids.Sum(v => v.Area)) / 1_000_000.0;
}
```

#### 8. `RoomDef.cs`
- **Path:** `src/HPBim.Core/Models/RoomDef.cs`
- **Class:** `public sealed record RoomDef`
```csharp
namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record RoomDef
{
    public string Name { get; init; } = string.Empty;
    public Point2D TagLocation { get; init; }
    public double? TheoreticalAreaM2 { get; init; }
    public string Level { get; init; } = string.Empty;
    public Polygon2D? BoundaryPolygon { get; init; }
    public double TagRotationDeg { get; init; }

    public double ComputedAreaM2 => (BoundaryPolygon?.Area ?? 0.0) / 1_000_000.0;
}
```

#### 9. `MepFixtureDef.cs`
- **Path:** `src/HPBim.Core/Models/MepFixtureDef.cs`
- **Class:** `public sealed record MepFixtureDef`
```csharp
namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record MepFixtureDef
{
    public string Id { get; init; } = string.Empty;
    public MepCategory Category { get; init; } = MepCategory.Sanitary;
    public string FamilyName { get; init; } = string.Empty;
    public Point3D Location { get; init; }
    public double RotationDeg { get; init; }
    public string Level { get; init; } = string.Empty;
    public double WidthMm { get; init; }
    public double DepthMm { get; init; }
    public double HeightMm { get; init; }
    public string MountingType { get; init; } = "Floor";
}
```

#### 10. `PipeRunDef.cs`
- **Path:** `src/HPBim.Core/Models/PipeRunDef.cs`
- **Class:** `public sealed record PipeRunDef`
```csharp
namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record PipeRunDef
{
    public string Id { get; init; } = string.Empty;
    public PipeSystemType SystemType { get; init; } = PipeSystemType.WaterSupply;
    public double DiameterMm { get; init; } = 25.0;
    public IReadOnlyList<Point3D> Points { get; init; } = Array.Empty<Point3D>();
    public double Slope { get; init; }
    public string Material { get; init; } = "PPR";
    public string Level { get; init; } = string.Empty;

    public double TotalLengthMm
    {
        get
        {
            if (Points.Count < 2) return 0.0;
            double len = 0.0;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                len += Points[i].DistanceTo(Points[i + 1]);
            }
            return len;
        }
    }
}
```

#### 11. `RebarDef.cs`
- **Path:** `src/HPBim.Core/Models/RebarDef.cs`
- **Class:** `public sealed record RebarDef`
```csharp
namespace HPBim.Core.Models;

public sealed record RebarDef
{
    public string Id { get; init; } = string.Empty;
    public string HostElementId { get; init; } = string.Empty;
    public int BarMark { get; init; }
    public double DiameterMm { get; init; } = 10.0;
    public string SteelGrade { get; init; } = "CB300-V";
    public RebarShapeType ShapeType { get; init; } = RebarShapeType.Straight;
    public double LengthMm { get; init; }
    public int Quantity { get; init; } = 1;
    public double SpacingMm { get; init; }
    public double CoverMm { get; init; } = 25.0;
    public string? Zone { get; init; }

    /// <summary>TCVN 5574:2018 unit weight: W = D^2 / 162.2 (kg/m).</summary>
    public double UnitWeightKgPerM => (DiameterMm * DiameterMm) / 162.2;

    public double TotalWeightKg => UnitWeightKgPerM * (LengthMm / 1000.0) * Quantity;
}
```

#### 12. `BimModel.cs`
- **Path:** `src/HPBim.Core/Models/BimModel.cs`
- **Class:** `public sealed record BimModel`
```csharp
namespace HPBim.Core.Models;

public sealed record BimModel
{
    public string ProjectName { get; init; } = string.Empty;
    public IReadOnlyList<LevelDef> Levels { get; init; } = Array.Empty<LevelDef>();
    public IReadOnlyList<AxisGrid> Grids { get; init; } = Array.Empty<AxisGrid>();
    public IReadOnlyList<ColumnDef> Columns { get; init; } = Array.Empty<ColumnDef>();
    public IReadOnlyList<WallDef> Walls { get; init; } = Array.Empty<WallDef>();
    public IReadOnlyList<BeamDef> Beams { get; init; } = Array.Empty<BeamDef>();
    public IReadOnlyList<SlabDef> Slabs { get; init; } = Array.Empty<SlabDef>();
    public IReadOnlyList<RoomDef> Rooms { get; init; } = Array.Empty<RoomDef>();
    public IReadOnlyList<MepFixtureDef> Fixtures { get; init; } = Array.Empty<MepFixtureDef>();
    public IReadOnlyList<PipeRunDef> Pipes { get; init; } = Array.Empty<PipeRunDef>();
    public IReadOnlyList<RebarDef> Rebars { get; init; } = Array.Empty<RebarDef>();

    public double TotalRebarWeightKg => Rebars.Sum(r => r.TotalWeightKg);

    public LevelDef? GetLevel(string name) =>
        Levels.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<WallDef> GetWallsByLevel(string levelName) =>
        Walls.Where(w => string.Equals(w.BaseLevel, levelName, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<ColumnDef> GetColumnsByLevel(string levelName) =>
        Columns.Where(c => string.Equals(c.BaseLevel, levelName, StringComparison.OrdinalIgnoreCase));
}
```

---

### 4.3 Spatial Algorithms Blueprint (`src/HPBim.Core/Algorithms/`)

#### 1. `WallSegmenter.cs`
- **Path:** `src/HPBim.Core/Algorithms/WallSegmenter.cs`
- **Class:** `public sealed class WallSegmenter`
- **Sub-model:** `public sealed record WallSegment`
```csharp
namespace HPBim.Core.Algorithms;

using HPBim.Core.Geometry;
using HPBim.Core.Models;

public sealed record WallSegment(
    Point2D StartPoint,
    Point2D EndPoint,
    double StartOffsetMm,
    double EndOffsetMm,
    double ThicknessMm,
    Polygon2D Polygon)
{
    public double LengthMm => EndOffsetMm - StartOffsetMm;
}

public sealed class WallSegmenter
{
    private const double Epsilon = 1e-4;

    public IReadOnlyList<WallSegment> Segment(WallDef wall)
    {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        double wallLength = wall.LengthMm;
        if (wallLength < Epsilon)
        {
            throw new ArgumentException("Wall length must be greater than zero.", nameof(wall));
        }

        Vector2D u = wall.Direction;
        Vector2D n = wall.Normal;
        double halfT = wall.ThicknessMm / 2.0;
        Point2D p1 = wall.StartPoint;

        var intervals = GetNormalizedOpeningIntervals(wall.Openings, wallLength);
        var solidSpans = BuildSolidSpans(intervals, wallLength);

        var segments = new List<WallSegment>(solidSpans.Count);
        foreach (var (a, b) in solidSpans)
        {
            Point2D segStart = p1 + u * a;
            Point2D segEnd = p1 + u * b;

            var polyVertices = new[]
            {
                segStart + n * halfT,
                segEnd + n * halfT,
                segEnd - n * halfT,
                segStart - n * halfT
            };

            segments.Add(new WallSegment(
                segStart, segEnd, a, b,
                wall.ThicknessMm,
                new Polygon2D(polyVertices)));
        }

        return segments;
    }

    private static List<(double Start, double End)> BuildSolidSpans(
        List<(double Start, double End)> intervals, double wallLength)
    {
        var solidSpans = new List<(double Start, double End)>();
        double current = 0.0;

        foreach (var (start, end) in intervals)
        {
            if (start > current + Epsilon)
            {
                solidSpans.Add((current, start));
            }
            current = Math.Max(current, end);
        }

        if (current < wallLength - Epsilon)
        {
            solidSpans.Add((current, wallLength));
        }

        return solidSpans;
    }

    private static List<(double Start, double End)> GetNormalizedOpeningIntervals(
        IReadOnlyList<OpeningDef> openings, double maxSpan)
    {
        if (openings == null || openings.Count == 0)
        {
            return new List<(double, double)>();
        }

        var raw = openings
            .Select(o => (
                Start: Math.Max(0.0, o.DistanceAlongWallMm),
                End: Math.Min(maxSpan, o.DistanceAlongWallMm + o.WidthMm)))
            .Where(i => i.End > i.Start + Epsilon)
            .OrderBy(i => i.Start)
            .ToList();

        var merged = new List<(double Start, double End)>();
        foreach (var interval in raw)
        {
            if (merged.Count == 0)
            {
                merged.Add(interval);
            }
            else
            {
                var last = merged[merged.Count - 1];
                if (interval.Start <= last.End + Epsilon)
                {
                    merged[merged.Count - 1] = (last.Start, Math.Max(last.End, interval.End));
                }
                else
                {
                    merged.Add(interval);
                }
            }
        }

        return merged;
    }
}
```

#### 2. `RoomAreaCalculator.cs`
- **Path:** `src/HPBim.Core/Algorithms/RoomAreaCalculator.cs`
- **Class:** `public sealed class RoomAreaCalculator`
- **Sub-model:** `public sealed record RoomValidationResult`
```csharp
namespace HPBim.Core.Algorithms;

using HPBim.Core.Geometry;
using HPBim.Core.Models;

public sealed record RoomValidationResult(
    bool IsValid,
    bool IsEnclosed,
    bool IsTagInside,
    double ComputedAreaM2,
    double? TheoreticalAreaM2,
    double? AreaDeltaM2,
    string? Message);

public sealed class RoomAreaCalculator
{
    private const double Epsilon = 1e-6;

    public double ComputeAreaM2(Polygon2D polygon)
    {
        if (polygon == null) throw new ArgumentNullException(nameof(polygon));
        return polygon.Area / 1_000_000.0;
    }

    public bool IsEnclosed(Polygon2D polygon)
    {
        if (polygon == null || polygon.Vertices.Count < 3)
        {
            return false;
        }
        return Math.Abs(polygon.Area) > Epsilon;
    }

    public bool ContainsPoint(Polygon2D polygon, Point2D point)
    {
        if (polygon == null || polygon.Vertices.Count < 3)
        {
            return false;
        }

        var vertices = polygon.Vertices;
        int n = vertices.Count;
        bool inside = false;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var vi = vertices[i];
            var vj = vertices[j];

            if (((vi.Y > point.Y) != (vj.Y > point.Y)) &&
                (point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public RoomValidationResult ValidateRoom(RoomDef room, double areaToleranceRatio = 0.05)
    {
        if (room == null) throw new ArgumentNullException(nameof(room));
        if (room.BoundaryPolygon == null)
        {
            return new RoomValidationResult(
                IsValid: false,
                IsEnclosed: false,
                IsTagInside: false,
                ComputedAreaM2: 0.0,
                TheoreticalAreaM2: room.TheoreticalAreaM2,
                AreaDeltaM2: null,
                Message: "Room has no boundary polygon.");
        }

        bool isEnclosed = IsEnclosed(room.BoundaryPolygon);
        bool isTagInside = ContainsPoint(room.BoundaryPolygon, room.TagLocation);
        double computedAreaM2 = ComputeAreaM2(room.BoundaryPolygon);

        double? delta = null;
        bool areaMatches = true;
        if (room.TheoreticalAreaM2.HasValue)
        {
            delta = Math.Abs(computedAreaM2 - room.TheoreticalAreaM2.Value);
            double maxAllowedDiff = room.TheoreticalAreaM2.Value * areaToleranceRatio;
            areaMatches = delta.Value <= maxAllowedDiff;
        }

        bool isValid = isEnclosed && isTagInside && areaMatches;
        string? message = isValid ? null : BuildFailureMessage(isEnclosed, isTagInside, areaMatches);

        return new RoomValidationResult(
            IsValid: isValid,
            IsEnclosed: isEnclosed,
            IsTagInside: isTagInside,
            ComputedAreaM2: computedAreaM2,
            TheoreticalAreaM2: room.TheoreticalAreaM2,
            AreaDeltaM2: delta,
            Message: message);
    }

    private static string BuildFailureMessage(bool isEnclosed, bool isTagInside, bool areaMatches)
    {
        var issues = new List<string>(3);
        if (!isEnclosed) issues.Add("Polygon is not enclosed or has zero area");
        if (!isTagInside) issues.Add("Tag location is outside room boundary");
        if (!areaMatches) issues.Add("Computed area deviates from theoretical area");
        return string.Join("; ", issues);
    }
}
```

#### 3. `OpeningPlacementEngine.cs`
- **Path:** `src/HPBim.Core/Algorithms/OpeningPlacementEngine.cs`
- **Class:** `public sealed class OpeningPlacementEngine`
- **Sub-model:** `public sealed record OpeningPlacement`
```csharp
namespace HPBim.Core.Algorithms;

using HPBim.Core.Geometry;
using HPBim.Core.Models;

public sealed record OpeningPlacement(
    string OpeningId,
    string HostWallId,
    Point2D StartPoint2D,
    Point2D CenterPoint2D,
    Point2D EndPoint2D,
    Point3D InsertionPoint3D,
    double WallAngleDeg,
    double WidthMm,
    double HeightMm,
    double SillHeightMm,
    OpeningKind Kind,
    OpeningSwing Swing,
    Polygon2D? SwingClearancePolygon);

public sealed class OpeningPlacementEngine
{
    public OpeningPlacement ComputePlacement(WallDef wall, OpeningDef opening, double baseElevationMm = 0.0)
    {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        if (opening == null) throw new ArgumentNullException(nameof(opening));

        Vector2D u = wall.Direction;
        Vector2D n = wall.Normal;
        Point2D p1 = wall.StartPoint;

        double startDist = opening.DistanceAlongWallMm;
        double centerDist = opening.CenterDistanceMm;
        double endDist = startDist + opening.WidthMm;

        Point2D pStart = p1 + u * startDist;
        Point2D pCenter = p1 + u * centerDist;
        Point2D pEnd = p1 + u * endDist;

        double zInsertion = baseElevationMm + wall.BaseOffsetMm + opening.SillHeightMm;
        Point3D insertion3D = new Point3D(pCenter.X, pCenter.Y, zInsertion);

        double wallAngleDeg = Math.Atan2(u.Y, u.X) * (180.0 / Math.PI);

        Polygon2D? clearancePoly = null;
        if (opening.Kind == OpeningKind.Door || opening.Kind == OpeningKind.Door2)
        {
            clearancePoly = ComputeSwingClearance(pStart, u, n, wall.ThicknessMm, opening);
        }

        return new OpeningPlacement(
            OpeningId: opening.Id,
            HostWallId: wall.Id,
            StartPoint2D: pStart,
            CenterPoint2D: pCenter,
            EndPoint2D: pEnd,
            InsertionPoint3D: insertion3D,
            WallAngleDeg: wallAngleDeg,
            WidthMm: opening.WidthMm,
            HeightMm: opening.HeightMm,
            SillHeightMm: opening.SillHeightMm,
            Kind: opening.Kind,
            Swing: opening.Swing,
            SwingClearancePolygon: clearancePoly);
    }

    public IReadOnlyList<OpeningPlacement> ComputeAllPlacements(WallDef wall, double baseElevationMm = 0.0)
    {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        if (wall.Openings == null || wall.Openings.Count == 0)
        {
            return Array.Empty<OpeningPlacement>();
        }

        var results = new List<OpeningPlacement>(wall.Openings.Count);
        foreach (var op in wall.Openings)
        {
            results.Add(ComputePlacement(wall, op, baseElevationMm));
        }
        return results;
    }

    public Polygon2D ComputeSwingClearance(
        Point2D pStart,
        Vector2D u,
        Vector2D n,
        double wallThicknessMm,
        OpeningDef opening)
    {
        double width = opening.WidthMm;
        double side = opening.Swing == OpeningSwing.Right ? -1.0 : 1.0;

        Point2D hinge = pStart + n * (side * wallThicknessMm / 2.0);
        Point2D closedLeaf = hinge + u * width;

        var vertices = new List<Point2D> { hinge, closedLeaf };
        const int arcSegments = 6;
        double startAngle = Math.Atan2(u.Y, u.X);
        double sweep = side * (-Math.PI / 2.0);

        for (int i = 1; i <= arcSegments; i++)
        {
            double angle = startAngle + sweep * (i / (double)arcSegments);
            vertices.Add(new Point2D(
                hinge.X + width * Math.Cos(angle),
                hinge.Y + width * Math.Sin(angle)));
        }

        return new Polygon2D(vertices);
    }
}
```

---

## 5. Verification Method

To independently verify the implementation blueprint when coded:

1. **Clean Code Rule Compliance Check:**
   - Scan all `.cs` files in `src/HPBim.Core/Models/` and `src/HPBim.Core/Algorithms/`:
     - Method line count $\le 50$ (Max observed in blueprint: 36 lines).
     - File line count $\le 300$ (Max observed in blueprint: ~110 lines).
     - 0 empty catch blocks.
     - 0 references to `Autodesk.*`.

2. **Automated Unit Tests in `HPBim.Core.Tests`:**
   - `WallSegmenterTests`:
     - Wall with 0 openings $\implies$ returns 1 segment equal to full wall length.
     - Wall with 1 center opening ($18000\text{ mm}$, opening at $6000\text{ mm}$, width $2000\text{ mm}$) $\implies$ returns 2 segments $[0, 6000]$ and $[8000, 18000]$.
     - Wall with opening starting at 0 ($d=0$) $\implies$ returns 1 segment $[w, L]$.
     - Wall with opening ending at $L$ ($d+w=L$) $\implies$ returns 1 segment $[0, d]$.
     - 4-corner polygon bounding vertices match thickness offsets $\pm t/2$.
   - `RoomAreaCalculatorTests`:
     - Regular rectangle $5000 \times 5000\text{ mm}$ $\implies$ `ComputeAreaM2` returns exactly $25.0\text{ m}^2$.
     - Room tag $(2500, 2500)$ inside $\implies$ `ContainsPoint` returns `true`.
     - Point $(6000, 2500)$ outside $\implies$ `ContainsPoint` returns `false`.
     - Non-enclosed polygon (< 3 vertices) $\implies$ `IsEnclosed` returns `false`.
   - `OpeningPlacementEngineTests`:
     - Host wall $(0,0) \to (18000,0)$, window at $d=15600, w=1600$:
       - $P_{start} = (15600, 0)$, $P_{center} = (16400, 0)$, $P_{end} = (17200, 0)$.
       - Insertion 3D Z at level $+0.150\text{m}$ ($150\text{mm}$) + sill $900\text{mm} = 1050\text{mm}$.
     - Door swing clearance polygon contains expected arc vertices.
   - `RebarDefTests`:
     - $\Phi 16$ bar of length $1.0\text{m}$:
       $$W = \frac{16^2}{162.2} \times 1.0 \approx 1.5783\text{ kg/m}$$
     - Matches TCVN 5574:2018 benchmark formula within $10^{-4}$.

3. **Execution Command:**
   ```powershell
   dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   ```
   *Expected:* 100% tests green in $< 2$ seconds without Revit running.
