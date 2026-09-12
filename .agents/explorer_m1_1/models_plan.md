# Technical Architecture & Domain Models Specification: HPRebar.Core.BeamRebar

**Component**: `HPRebar.Core/BeamRebar/Models/`  
**Target Framework**: `netstandard2.0` (C# 12 / latest via Polyfill 11.0.1)  
**Author**: `explorer_m1_1` (M1 Domain Models Architect)  
**Date**: 2026-09-07  
**Status**: Authoritative Technical Specification  

---

## 1. Executive Summary & Architectural Scope

This specification defines the complete domain model architecture for the Continuous Beam Reinforcement module (`BeamRebar`) in `HPRebar.Core`. In alignment with the core tenets established by `HPRebar`'s reference implementation (`ColumnRebar`), the domain layer is completely decoupled from Autodesk Revit, UI frameworks, and external dependencies.

### Key Architectural Tenets:
1. **Zero-Revit Dependency**: Absolute prohibition of `Autodesk.Revit.*` references in `HPRebar.Core`. Models are pure, deterministic C# records and readonly structs.
2. **Standard Units**: All physical dimensions, coordinates, lengths, clearances, spacings, and diameters are strictly represented as `double` values in **millimetres** ($mm$).
3. **Data Immutability**: All model classes are implemented as `public sealed record` with `{ get; init; }` or `public readonly struct`, eliminating shared mutable state, concurrency side-effects, and accidental state corruption.
4. **Clean Decoupling**: Acts as the shared lingua-franca connecting upstream Revit geometry readers (`BeamStackReader`), pure business logic calculators (`HPRebar.Core.BeamRebar.Calculators`), downstream native Revit creators (`BeamRebarCreationService`), and the WPF MVVM preview canvas.
5. **Runtime Compliance**: 100% compliant with `netstandard2.0` under .NET SDK 10.0.300 and `Polyfill 11.0.1`.

---

## 2. Assembly & Target Namespace Configuration

### 2.1 Project Settings (`HPRebar.Core.csproj`)
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

### 2.2 Namespace Hierarchy
All domain models defined herein reside strictly in:
```csharp
namespace HPRebar.Core.BeamRebar.Models;
```
Calculators and geometry algorithms reside in:
```csharp
namespace HPRebar.Core.BeamRebar.Calculators;
```
Core numerical tolerance utilities reside in:
```csharp
namespace HPRebar.Core.BeamRebar;
```

---

## 3. Coordinate System & Continuous Datum Specification

Continuous beams in Autodesk Revit frequently have inconsistent local coordinate orientations depending on the order and direction in which framing elements were drawn by modelers (e.g., Span 1 modeled West-to-East, Span 2 modeled East-to-West). 

To ensure pure, deterministic geometric calculations, `HPRebar.Core` introduces the **Unified Continuous Beam Datum**:

```
 Elevation View (X - Z plane):
 
 Z_top  +-----------------------+-----------------------+-----------------------+
        |        Span 0         |        Span 1         |        Span 2         |
        |       (b0 x h0)       |       (b1 x h1)       |       (b2 x h2)       |
 Z_bot  +-----------+-----------+-----------+-----------+-----------+-----------+
                    |                       |                       |
               [Support 0]             [Support 1]             [Support 2]   [Support 3]
               Center X=0              Center X=L0             Center X=L0+L1
               Width = C0              Width = C1              Width = C2    Width = C3
               
 Station X:    0 ──────────────> X_1 ─────────────────> X_2 ───────────────> X_total
```

### 3.1 Datum Axes Definition
1. **Longitudinal Axis ($\vec{U}_X$)**: Runs continuously from the start face of the first span/support along the centerline of the continuous beam stack towards the end face of the final span/support.
   - $X = 0$: Located at the centerline or exterior face of Support 0.
   - Stations $X$ are monotonically increasing along the chain.
2. **Vertical Axis ($\vec{U}_Z$)**: Global vertical unit vector $(0, 0, 1)$.
   - Elevations $Z$ represent heights in millimetres.
   - $Z_{top, i}$: Top elevation of Span $i$.
   - $Z_{bot, i} = Z_{top, i} - h_i$: Soffit elevation of Span $i$.
3. **Transverse Axis ($\vec{U}_Y$)**: Normal horizontal unit vector:
   $$\vec{U}_Y = \vec{U}_Z \times \vec{U}_X$$
   - $Y = 0$: Centerline plane of the beam web.
   - $Y \in [-b/2, +b/2]$: Transverse bounds across the beam width $b$.

---

## 4. Comprehensive Class & Struct Specifications

### 4.1 Geometry Primitives

#### `Point3.cs`
A lightweight, immutable 3D coordinate struct in continuous beam local space.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A 3D Cartesian point in the continuous beam local millimetre frame:
/// X longitudinal along beam axis, Y transverse across beam width, Z vertical.
/// </summary>
public readonly struct Point3 : System.IEquatable<Point3>
{
    public Point3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Longitudinal coordinate along the continuous beam axis (mm).</summary>
    public double X { get; }

    /// <summary>Transverse coordinate perpendicular to beam axis (mm, centered at 0).</summary>
    public double Y { get; }

    /// <summary>Vertical elevation coordinate (mm).</summary>
    public double Z { get; }

    public static readonly Point3 Zero = new(0, 0, 0);

    public static Point3 operator +(Point3 p, Vector3 v) => new(p.X + v.X, p.Y + v.Y, p.Z + v.Z);
    public static Point3 operator -(Point3 p, Vector3 v) => new(p.X - v.X, p.Y - v.Y, p.Z - v.Z);
    public static Vector3 operator -(Point3 a, Point3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public double DistanceTo(Point3 other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public bool IsAlmostEqualTo(Point3 other, double tolerance = 1.0e-9) =>
        System.Math.Abs(X - other.X) <= tolerance &&
        System.Math.Abs(Y - other.Y) <= tolerance &&
        System.Math.Abs(Z - other.Z) <= tolerance;

    public void Deconstruct(out double x, out double y, out double z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    public bool Equals(Point3 other) => IsAlmostEqualTo(other);
    public override bool Equals(object? obj) => obj is Point3 other && Equals(other);
    public override int GetHashCode() => (X, Y, Z).GetHashCode();
    public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";

    public static bool operator ==(Point3 left, Point3 right) => left.Equals(right);
    public static bool operator !=(Point3 left, Point3 right) => !left.Equals(right);
}
```

#### `Vector3.cs`
A lightweight, immutable 3D directional vector struct supporting standard Euclidean vector algebra.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A 3D Cartesian vector in millimetres or unit space.
/// </summary>
public readonly struct Vector3 : System.IEquatable<Vector3>
{
    public Vector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public static readonly Vector3 Zero = new(0, 0, 0);
    public static readonly Vector3 UnitX = new(1, 0, 0);
    public static readonly Vector3 UnitY = new(0, 1, 0);
    public static readonly Vector3 UnitZ = new(0, 0, 1);

    public double Length => System.Math.Sqrt(X * X + Y * Y + Z * Z);
    public double LengthSquared => X * X + Y * Y + Z * Z;

    public Vector3 Normalize()
    {
        double len = Length;
        return len < 1.0e-9 ? Zero : new Vector3(X / len, Y / len, Z / len);
    }

    public double Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3 Cross(Vector3 other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);
    public static Vector3 operator *(Vector3 v, double scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);
    public static Vector3 operator *(double scalar, Vector3 v) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);
    public static Vector3 operator /(Vector3 v, double scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

    public bool Equals(Vector3 other) =>
        System.Math.Abs(X - other.X) <= 1.0e-9 &&
        System.Math.Abs(Y - other.Y) <= 1.0e-9 &&
        System.Math.Abs(Z - other.Z) <= 1.0e-9;

    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override int GetHashCode() => (X, Y, Z).GetHashCode();
    public override string ToString() => $"[{X:0.###}, {Y:0.###}, {Z:0.###}]";

    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);
    public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);
}
```

#### `Polyline3.cs`
An immutable 3D polyline curve container representing rebar centerlines.
```csharp
using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// An ordered sequence of 3D points forming a continuous rebar centerline curve.
/// All coordinates are in local millimetres.
/// </summary>
public sealed record Polyline3
{
    public IReadOnlyList<Point3> Points { get; init; } = Array.Empty<Point3>();

    public bool IsClosed { get; init; }

    /// <summary>Total cumulative length of all polyline segments in millimetres.</summary>
    public double TotalLength
    {
        get
        {
            if (Points.Count < 2) return 0.0;
            double sum = 0.0;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                sum += Points[i].DistanceTo(Points[i + 1]);
            }
            if (IsClosed && Points.Count > 2)
            {
                sum += Points[Points.Count - 1].DistanceTo(Points[0]);
            }
            return sum;
        }
    }

    /// <summary>
    /// Simplifies the polyline by merging consecutive vertices closer than <paramref name="minSegmentLength"/> mm.
    /// Crucial for preventing Revit CreateFromCurves exceptions on Application.ShortCurveTolerance (~0.78 mm).
    /// </summary>
    public Polyline3 Simplify(double minSegmentLength = 1.0)
    {
        if (Points.Count < 2) return this;

        var result = new List<Point3>(Points.Count) { Points[0] };
        for (int i = 1; i < Points.Count; i++)
        {
            if (Points[i].DistanceTo(result[result.Count - 1]) >= minSegmentLength)
            {
                result.Add(Points[i]);
            }
        }

        // Check closed polyline tail
        if (IsClosed && result.Count > 2 && result[result.Count - 1].DistanceTo(result[0]) < minSegmentLength)
        {
            result.RemoveAt(result.Count - 1);
        }

        return this with { Points = result };
    }

    /// <summary>Translates all vertices by the specified vector.</summary>
    public Polyline3 Translate(Vector3 offset)
    {
        var translated = new Point3[Points.Count];
        for (int i = 0; i < Points.Count; i++)
        {
            translated[i] = Points[i] + offset;
        }
        return this with { Points = translated };
    }
}
```

#### `BarPolyline.cs`
The primary output of bar calculators and input to native Revit creators.
```csharp
using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A fully calculated physical reinforcing bar centerline curve and associated detailing metadata.
/// Coordinates are in millimetres in the continuous beam datum.
/// </summary>
public sealed record BarPolyline
{
    /// <summary>Sequential bar identifier in the generation run.</summary>
    public int BarIndex { get; init; }

    /// <summary>Structural role of this bar.</summary>
    public BarType Type { get; init; }

    /// <summary>Bar nominal diameter in millimetres.</summary>
    public double Diameter { get; init; }

    /// <summary>Vertical layer index (1 = outer layer, 2 = inner secondary layer).</summary>
    public int Layer { get; init; } = 1;

    /// <summary>3D polyline geometry defining the bar centerline.</summary>
    public Polyline3 Polyline { get; init; } = new();

    /// <summary>Start hook bend angle.</summary>
    public HookAngle StartHookAngle { get; init; } = HookAngle.None;

    /// <summary>End hook bend angle.</summary>
    public HookAngle EndHookAngle { get; init; } = HookAngle.None;

    /// <summary>Start hook length in millimetres (if modeled parametrically).</summary>
    public double StartHookLength { get; init; }

    /// <summary>End hook length in millimetres (if modeled parametrically).</summary>
    public double EndHookLength { get; init; }

    /// <summary>Transverse position Y across the beam width (mm, centered at 0).</summary>
    public double TransverseY { get; init; }

    /// <summary>Span index hosting this bar (-1 if continuous across multiple spans).</summary>
    public int HostSpanIndex { get; init; } = -1;

    /// <summary>Support index hosting this bar (-1 if not associated with a support node).</summary>
    public int HostSupportIndex { get; init; } = -1;

    /// <summary>Revit RebarBarType name matched in project document.</summary>
    public string BarTypeName { get; init; } = string.Empty;

    /// <summary>Total cut length of this bar including polyline and hooks (mm).</summary>
    public double TotalLength => Polyline.TotalLength + StartHookLength + EndHookLength;
}
```

---

### 4.2 Continuous Beam Assembly & Support Models

#### `BeamSpan.cs`
Represents an individual structural beam span in the continuous assembly.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// An individual span segment in a continuous beam assembly.
/// All spatial coordinates and dimensions are in millimetres.
/// </summary>
public sealed record BeamSpan
{
    /// <summary>Zero-based index of the span in the continuous chain (0, 1, ... N-1).</summary>
    public int Index { get; init; }

    /// <summary>User-friendly identifier (e.g., "Span 1", "D1").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Revit Element UniqueId of the underlying Structural Framing instance.</summary>
    public string ElementUniqueId { get; init; } = string.Empty;

    /// <summary>Center-to-center span length Lc between adjacent support centerlines (mm).</summary>
    public double LengthCenter { get; init; }

    /// <summary>Clear span length Ln between adjacent support inner faces (mm).</summary>
    public double LengthClear { get; init; }

    /// <summary>Cross-section width b (mm).</summary>
    public double Width { get; init; }

    /// <summary>Cross-section total height h (mm).</summary>
    public double Height { get; init; }

    /// <summary>Top surface elevation Z_top (mm).</summary>
    public double TopElevation { get; init; }

    /// <summary>Bottom soffit elevation Z_bot = Z_top - Height (mm).</summary>
    public double BottomElevation => TopElevation - Height;

    /// <summary>Longitudinal coordinate X of the clear span start face (mm).</summary>
    public double StartX { get; init; }

    /// <summary>Longitudinal coordinate X of the clear span end face (mm).</summary>
    public double EndX => StartX + LengthClear;

    /// <summary>Longitudinal coordinate X of the left support centerline (mm).</summary>
    public double CenterStartX { get; init; }

    /// <summary>Longitudinal coordinate X of the right support centerline (mm).</summary>
    public double CenterEndX => CenterStartX + LengthCenter;

    /// <summary>Specified concrete cover thickness c (mm).</summary>
    public double Cover { get; init; } = 25.0;

    /// <summary>Cantilever classification if this span overhangs an exterior support.</summary>
    public CantileverPosition Cantilever { get; init; } = CantileverPosition.None;

    /// <summary>True if this span is an overhang / cantilever.</summary>
    public bool IsCantilever => Cantilever != CantileverPosition.None;

    /// <summary>Effective depth d = h - Cover - stirrupDiameter - barDiameter / 2.</summary>
    public double EffectiveDepth(double barDiameter, double stirrupDiameter) =>
        Height - Cover - stirrupDiameter - (barDiameter / 2.0);
}
```

#### `BeamSupportNode.cs`
Represents an intermediate or terminal bearing support (column, wall, or girder).
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A bearing support node supporting the continuous beam assembly.
/// For N spans, there are exactly N + 1 support nodes (Index 0 to N).
/// </summary>
public sealed record BeamSupportNode
{
    /// <summary>Zero-based index of the support node along the beam chain (0, 1, ... N).</summary>
    public int Index { get; init; }

    /// <summary>User-friendly identifier (e.g., "Support 0", "C1", "W1").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Revit Element UniqueId of the supporting column, wall, or girder.</summary>
    public string ElementUniqueId { get; init; } = string.Empty;

    /// <summary>Longitudinal coordinate X of the support centerline (mm).</summary>
    public double CenterX { get; init; }

    /// <summary>Support dimension C along the beam longitudinal axis (mm).</summary>
    public double Width { get; init; }

    /// <summary>Support dimension B transverse to the beam longitudinal axis (mm).</summary>
    public double Depth { get; init; }

    /// <summary>Support classification.</summary>
    public SupportType Type { get; init; } = SupportType.Column;

    /// <summary>Coordinate X of the support left face entering the adjacent left span (mm).</summary>
    public double LeftFaceX => CenterX - (Width / 2.0);

    /// <summary>Coordinate X of the support right face entering the adjacent right span (mm).</summary>
    public double RightFaceX => CenterX + (Width / 2.0);

    /// <summary>True if this support is an exterior terminal support.</summary>
    public bool IsExterior { get; init; }
}
```

#### `SecondaryBeamIntersection.cs`
Represents an intersecting secondary framing beam introducing concentrated shear loads into the continuous beam web.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Geometric representation of a secondary framing beam framing into the continuous primary beam.
/// Used to calculate hanging stirrup cages and diagonal ties.
/// </summary>
public sealed record SecondaryBeamIntersection
{
    /// <summary>Zero-based index of the intersection along the beam run.</summary>
    public int Index { get; init; }

    /// <summary>Index of the primary beam span containing this intersection.</summary>
    public int HostSpanIndex { get; init; }

    /// <summary>Longitudinal coordinate X of the secondary beam centerline (mm).</summary>
    public double CenterX { get; init; }

    /// <summary>Cross-section width bs of the incoming secondary beam (mm).</summary>
    public double Width { get; init; }

    /// <summary>Cross-section depth hs of the incoming secondary beam (mm).</summary>
    public double Height { get; init; }

    /// <summary>Top elevation of the secondary beam (mm).</summary>
    public double TopElevation { get; init; }

    /// <summary>Bottom soffit elevation of the secondary beam (mm).</summary>
    public double SoffitElevation => TopElevation - Height;

    /// <summary>Side(s) of the primary beam where the secondary beam frames in.</summary>
    public IntersectionSide FramingSide { get; init; } = IntersectionSide.Both;

    /// <summary>Revit Element UniqueId of the secondary beam.</summary>
    public string ElementUniqueId { get; init; } = string.Empty;

    /// <summary>Left face coordinate X of the secondary beam joint (mm).</summary>
    public double LeftFaceX => CenterX - (Width / 2.0);

    /// <summary>Right face coordinate X of the secondary beam joint (mm).</summary>
    public double RightFaceX => CenterX + (Width / 2.0);
}
```

#### `BeamContinuousStack.cs`
The master geometric assembly aggregating all spans, supports, and intersections.
```csharp
using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Master geometric assembly representing a complete continuous multi-span beam.
/// Immutable container holding ordered spans, support nodes, and secondary intersections.
/// </summary>
public sealed record BeamContinuousStack
{
    public IReadOnlyList<BeamSpan> Spans { get; init; } = Array.Empty<BeamSpan>();

    public IReadOnlyList<BeamSupportNode> Supports { get; init; } = Array.Empty<BeamSupportNode>();

    public IReadOnlyList<SecondaryBeamIntersection> SecondaryIntersections { get; init; } = Array.Empty<SecondaryBeamIntersection>();

    public int SpanCount => Spans.Count;
    public int SupportCount => Supports.Count;

    /// <summary>Overall continuous length from the start face of Support 0 to end face of Support N (mm).</summary>
    public double TotalLength => OverallEndX - OverallStartX;

    /// <summary>Left-most coordinate of the entire beam run (mm).</summary>
    public double OverallStartX => Supports.Count > 0 ? Supports[0].LeftFaceX : 0.0;

    /// <summary>Right-most coordinate of the entire beam run (mm).</summary>
    public double OverallEndX => Supports.Count > 0 ? Supports[Supports.Count - 1].RightFaceX : 0.0;

    /// <summary>Maximum cross-section height across all spans (mm).</summary>
    public double MaxHeight
    {
        get
        {
            double max = 0.0;
            for (int i = 0; i < Spans.Count; i++)
            {
                if (Spans[i].Height > max) max = Spans[i].Height;
            }
            return max;
        }
    }

    /// <summary>Minimum top elevation across all spans (mm).</summary>
    public double MinTopElevation
    {
        get
        {
            if (Spans.Count == 0) return 0.0;
            double min = Spans[0].TopElevation;
            for (int i = 1; i < Spans.Count; i++)
            {
                if (Spans[i].TopElevation < min) min = Spans[i].TopElevation;
            }
            return min;
        }
    }

    /// <summary>Maximum top elevation across all spans (mm).</summary>
    public double MaxTopElevation
    {
        get
        {
            if (Spans.Count == 0) return 0.0;
            double max = Spans[0].TopElevation;
            for (int i = 1; i < Spans.Count; i++)
            {
                if (Spans[i].TopElevation > max) max = Spans[i].TopElevation;
            }
            return max;
        }
    }

    /// <summary>
    /// Performs geometric validation of the continuous beam stack.
    /// Checks span count, support alignment, contiguity, and positive dimensions.
    /// </summary>
    public ValidationResult Validate()
    {
        if (Spans.Count == 0)
            return ValidationResult.Fail("Beam continuous stack must contain at least 1 span.");

        if (Supports.Count != Spans.Count + 1)
            return ValidationResult.Fail($"Support count ({Supports.Count}) must equal Span count ({Spans.Count}) + 1.");

        for (int i = 0; i < Spans.Count; i++)
        {
            var span = Spans[i];
            if (span.Width <= 0.0) return ValidationResult.Fail($"Span {i} has invalid width: {span.Width} mm.");
            if (span.Height <= 0.0) return ValidationResult.Fail($"Span {i} has invalid height: {span.Height} mm.");
            if (span.LengthClear <= 0.0) return ValidationResult.Fail($"Span {i} has non-positive clear span: {span.LengthClear} mm.");
            if (span.Cover <= 0.0) return ValidationResult.Fail($"Span {i} has non-positive cover: {span.Cover} mm.");
        }

        // Validate joint contiguity between spans and supports
        const double tolerance = 1.0; // 1.0 mm tolerance for physical join
        for (int i = 0; i < Spans.Count; i++)
        {
            var leftSupport = Supports[i];
            var span = Spans[i];
            var rightSupport = Supports[i + 1];

            if (System.Math.Abs(leftSupport.RightFaceX - span.StartX) > tolerance)
                return ValidationResult.Fail($"Discontinuity detected at Span {i} start: Support {i} right face ({leftSupport.RightFaceX:0.#}) != Span start ({span.StartX:0.#}).");

            if (System.Math.Abs(span.EndX - rightSupport.LeftFaceX) > tolerance)
                return ValidationResult.Fail($"Discontinuity detected at Span {i} end: Span end ({span.EndX:0.#}) != Support {i+1} left face ({rightSupport.LeftFaceX:0.#}).");
        }

        return ValidationResult.Ok();
    }

    /// <summary>Finds the span enclosing the given station coordinate X.</summary>
    public BeamSpan? FindSpanAt(double x)
    {
        for (int i = 0; i < Spans.Count; i++)
        {
            if (x >= Spans[i].StartX && x <= Spans[i].EndX)
                return Spans[i];
        }
        return null;
    }
}
```

---

### 4.3 Reinforcement Specification Records

#### `BeamStirrupSpec.cs`
Specification for transverse shear reinforcement across continuous spans.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Specification parameters for beam stirrup (shear tie) distribution.
/// All lengths and spacings are in millimetres.
/// </summary>
public sealed record BeamStirrupSpec
{
    /// <summary>Distribution layout algorithm.</summary>
    public StirrupLayout Layout { get; init; } = StirrupLayout.ThreeZoneL4;

    /// <summary>Stirrup bar diameter in millimetres (e.g. 8, 10).</summary>
    public double Diameter { get; init; } = 8.0;

    /// <summary>Concrete cover thickness to the outer edge of the stirrup (mm).</summary>
    public double Cover { get; init; } = 25.0;

    /// <summary>Dense spacing S1 in support zones (mm, e.g. 100).</summary>
    public double SpacingDense { get; init; } = 100.0;

    /// <summary>Sparse spacing S2 in midspan zone (mm, e.g. 200).</summary>
    public double SpacingSparse { get; init; } = 200.0;

    /// <summary>Offset from support inside face to the first stirrup (mm, default: 50).</summary>
    public double StartOffset { get; init; } = 50.0;

    /// <summary>True to carry stirrups through interior support column/wall nodes.</summary>
    public bool IncludeStirrupsInNodes { get; init; }

    /// <summary>Stirrup spacing across support node widths (mm, default: 150).</summary>
    public double NodeSpacing { get; init; } = 150.0;

    /// <summary>Hook bend angle on stirrup hoop (default: 135° seismic hook).</summary>
    public HookAngle HookAngle { get; init; } = HookAngle.Hook135;

    /// <summary>Rebar shape family name (e.g. "M_T1" or "T1").</summary>
    public string RebarShapeName { get; init; } = "M_T1";

    /// <summary>Revit RebarBarType name matched in project document.</summary>
    public string BarTypeName { get; init; } = string.Empty;
}
```

#### `StirrupZone.cs`
Represents a discrete calculated zone of stirrups.
```csharp
using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A calculated contiguous run of stirrups with uniform spacing.
/// Coordinates are in local millimetres along the continuous beam axis.
/// </summary>
public sealed record StirrupZone
{
    /// <summary>Zone sequence index within the span (0 = Left, 1 = Mid, 2 = Right).</summary>
    public int ZoneIndex { get; init; }

    /// <summary>Parent span index (-1 if support node zone).</summary>
    public int HostSpanIndex { get; init; } = -1;

    /// <summary>Zone classification label (e.g., "Dense Support", "Midspan").</summary>
    public string ZoneName { get; init; } = string.Empty;

    /// <summary>Longitudinal coordinate X of the zone start station (mm).</summary>
    public double StartX { get; init; }

    /// <summary>Longitudinal coordinate X of the zone end station (mm).</summary>
    public double EndX { get; init; }

    /// <summary>Zone longitudinal length (mm).</summary>
    public double Length => EndX - StartX;

    /// <summary>Uniform spacing between stirrups in this zone (mm).</summary>
    public double Spacing { get; init; }

    /// <summary>Number of stirrup positions in this zone.</summary>
    public int Count { get; init; }

    /// <summary>Exact longitudinal coordinates X for each individual stirrup (mm).</summary>
    public IReadOnlyList<double> Positions { get; init; } = Array.Empty<double>();

    /// <summary>Out-to-out stirrup width (b - 2*Cover) (mm).</summary>
    public double Width { get; init; }

    /// <summary>Out-to-out stirrup height (h - 2*Cover) (mm).</summary>
    public double Height { get; init; }

    /// <summary>Top elevation of stirrup top outer bar edge (Z_top - Cover) (mm).</summary>
    public double TopElevation { get; init; }
}
```

#### `StirrupRun.cs`
A compact record designed to feed directly into Revit's `RebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing`.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Parameters for a single RebarShape-driven stirrup array.
/// Compatible with Revit's SetLayoutAsNumberWithSpacing API method.
/// </summary>
public sealed record StirrupRun
{
    /// <summary>Total number of stirrups in the array.</summary>
    public int Count { get; init; }

    /// <summary>Center-to-center spacing in millimetres.</summary>
    public double Spacing { get; init; }

    /// <summary>Origin point (lower-left corner of the first stirrup) in local coordinates (mm).</summary>
    public Point3 Origin { get; init; }

    /// <summary>Out-to-out width for ScaleToBox (mm).</summary>
    public double Width { get; init; }

    /// <summary>Out-to-out height for ScaleToBox (mm).</summary>
    public double Height { get; init; }
}
```

#### `BeamMainBarSpec.cs`
Specification for top and bottom continuous longitudinal reinforcement, anchorages, and staggered lap splicing.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Detailing specification for continuous top and bottom longitudinal reinforcement.
/// All lengths and spacings are in millimetres.
/// </summary>
public sealed record BeamMainBarSpec
{
    // --- Top Main Bars ---
    /// <summary>Number of top continuous bars (minimum 2 to engage stirrup top corners).</summary>
    public int TopCount { get; init; } = 2;

    /// <summary>Top bar diameter in millimetres (e.g. 18, 20, 22).</summary>
    public double TopDiameter { get; init; } = 20.0;

    /// <summary>Top concrete cover in millimetres.</summary>
    public double TopCover { get; init; } = 25.0;

    /// <summary>Exterior start anchorage type for top bars.</summary>
    public EndAnchorageType TopStartAnchorage { get; init; } = EndAnchorageType.Hook90Down;

    /// <summary>Hook length at start exterior support (mm, 0 for auto h - 2*Cover).</summary>
    public double TopStartHookLength { get; init; }

    /// <summary>Exterior end anchorage type for top bars.</summary>
    public EndAnchorageType TopEndAnchorage { get; init; } = EndAnchorageType.Hook90Down;

    /// <summary>Hook length at end exterior support (mm, 0 for auto h - 2*Cover).</summary>
    public double TopEndHookLength { get; init; }

    /// <summary>Revit RebarBarType name for top bars.</summary>
    public string TopBarTypeName { get; init; } = string.Empty;

    // --- Bottom Main Bars ---
    /// <summary>Number of bottom continuous bars (minimum 2 to engage stirrup bottom corners).</summary>
    public int BottomCount { get; init; } = 2;

    /// <summary>Bottom bar diameter in millimetres (e.g. 18, 20, 22).</summary>
    public double BottomDiameter { get; init; } = 20.0;

    /// <summary>Bottom concrete cover in millimetres.</summary>
    public double BottomCover { get; init; } = 25.0;

    /// <summary>Exterior start anchorage type for bottom bars.</summary>
    public EndAnchorageType BottomStartAnchorage { get; init; } = EndAnchorageType.Hook90Up;

    /// <summary>Hook length at start exterior support (mm, 0 for auto h - 2*Cover).</summary>
    public double BottomStartHookLength { get; init; }

    /// <summary>Exterior end anchorage type for bottom bars.</summary>
    public EndAnchorageType BottomEndAnchorage { get; init; } = EndAnchorageType.Hook90Up;

    /// <summary>Hook length at end exterior support (mm, 0 for auto h - 2*Cover).</summary>
    public double BottomEndHookLength { get; init; }

    /// <summary>Revit RebarBarType name for bottom bars.</summary>
    public string BottomBarTypeName { get; init; } = string.Empty;

    // --- Splicing & Division Rules ---
    /// <summary>Maximum stock/commercial bar length (mm, default: 11700 mm = 11.7 m).</summary>
    public double MaxStockLength { get; init; } = 11700.0;

    /// <summary>Lap splice length multiplier in bar diameters (e.g. 40 -> 40 * d).</summary>
    public double LapFactor { get; init; } = 40.0;

    /// <summary>True to stagger lap splices by 50% between adjacent bar lines.</summary>
    public bool EnableStagger { get; init; } = true;

    /// <summary>Stagger offset multiplier (default: 1.3 * LapLength).</summary>
    public double StaggerOffsetRatio { get; init; } = 1.3;
}
```

#### `BeamAdditionalBarSpec.cs`
Specification for top reinforcement over intermediate supports and bottom reinforcement at midspans.
```csharp
using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Detailing specification for additional reinforcement:
/// Negative moment top bars over supports and positive moment bottom bars in midspans.
/// </summary>
public sealed record BeamAdditionalBarSpec
{
    /// <summary>Per-support additional top bar configurations.</summary>
    public IReadOnlyList<SupportAdditionalTopBarConfig> SupportTopBars { get; init; } = Array.Empty<SupportAdditionalTopBarConfig>();

    /// <summary>Per-span additional bottom bar configurations.</summary>
    public IReadOnlyList<SpanAdditionalBottomBarConfig> SpanBottomBars { get; init; } = Array.Empty<SpanAdditionalBottomBarConfig>();
}

/// <summary>
/// Configuration for additional top bars centered over a specific support node.
/// </summary>
public sealed record SupportAdditionalTopBarConfig
{
    /// <summary>Index of the support node where bars are centered (0 to N).</summary>
    public int SupportIndex { get; init; }

    // --- Layer 1 ---
    /// <summary>Bar count in Layer 1 (placed at same elevation as top main bars).</summary>
    public int Layer1Count { get; init; }

    /// <summary>Bar diameter in Layer 1 (mm).</summary>
    public double Layer1Diameter { get; init; }

    /// <summary>Extension ratio into adjacent clear spans for Layer 1 (default: 1/3 = L/3).</summary>
    public double Layer1ExtensionRatio { get; init; } = 1.0 / 3.0;

    // --- Layer 2 ---
    /// <summary>Bar count in Layer 2 (placed underneath Layer 1; 0 if single layer).</summary>
    public int Layer2Count { get; init; }

    /// <summary>Bar diameter in Layer 2 (mm).</summary>
    public double Layer2Diameter { get; init; }

    /// <summary>Extension ratio for Layer 2 (default: 1/4 = L/4, cut shorter than Layer 1).</summary>
    public double Layer2ExtensionRatio { get; init; } = 1.0 / 4.0;

    /// <summary>Vertical gap DeltaZ between Layer 1 and Layer 2 (mm, default: 50 mm).</summary>
    public double LayerGap { get; init; } = 50.0;

    // --- Exterior Anchorage ---
    /// <summary>Exterior anchorage hook type if this is an exterior support (Support 0 or N).</summary>
    public EndAnchorageType ExteriorEndAnchorage { get; init; } = EndAnchorageType.Hook90Down;

    /// <summary>Exterior hook length (mm, 0 for auto).</summary>
    public double ExteriorHookLength { get; init; }

    /// <summary>Revit RebarBarType name.</summary>
    public string BarTypeName { get; init; } = string.Empty;
}

/// <summary>
/// Configuration for additional bottom bars placed in the midspan region of a span.
/// </summary>
public sealed record SpanAdditionalBottomBarConfig
{
    /// <summary>Index of the span where bars are placed (0 to N-1).</summary>
    public int SpanIndex { get; init; }

    // --- Layer 1 ---
    /// <summary>Bar count in Layer 1 (placed in line with main bottom bars).</summary>
    public int Layer1Count { get; init; }

    /// <summary>Bar diameter in Layer 1 (mm).</summary>
    public double Layer1Diameter { get; init; }

    /// <summary>Cutoff distance ratio from support inner face (default: 1/7 = L/7).</summary>
    public double CutoffRatio { get; init; } = 1.0 / 7.0;

    // --- Layer 2 ---
    /// <summary>Bar count in Layer 2 (placed above Layer 1; 0 if single layer).</summary>
    public int Layer2Count { get; init; }

    /// <summary>Bar diameter in Layer 2 (mm).</summary>
    public double Layer2Diameter { get; init; }

    /// <summary>Vertical gap DeltaZ between Layer 1 and Layer 2 (mm, default: 50 mm).</summary>
    public double LayerGap { get; init; } = 50.0;

    /// <summary>Revit RebarBarType name.</summary>
    public string BarTypeName { get; init; } = string.Empty;
}
```

#### `BeamSideBarSpec.cs`
Specification for longitudinal skin/web reinforcement in deep beams ($h \ge 700$ mm) and anti-buckling cross-ties.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Detailing specification for longitudinal side (skin) reinforcement and transverse cross-ties in deep beams.
/// Complies with TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3.
/// </summary>
public sealed record BeamSideBarSpec
{
    /// <summary>True to automatically generate side bars when beam height h >= DepthThreshold.</summary>
    public bool AutoSkinBars { get; init; } = true;

    /// <summary>Beam height threshold triggering skin reinforcement (mm, default: 700 mm).</summary>
    public double DepthThreshold { get; init; } = 700.0;

    /// <summary>Diameter of longitudinal side bars (mm, e.g. 12 or 14).</summary>
    public double Diameter { get; init; } = 12.0;

    /// <summary>Maximum vertical center-to-center spacing between side bar pairs (mm, default: 300 mm).</summary>
    public double MaxVerticalSpacing { get; init; } = 300.0;

    /// <summary>Concrete cover from beam lateral vertical faces to the outer edge of side bars (mm).</summary>
    public double Cover { get; init; } = 25.0;

    /// <summary>True to generate transverse anti-buckling cross-ties connecting opposite side bars.</summary>
    public bool IncludeCrossTies { get; init; } = true;

    /// <summary>Diameter of cross-ties (mm, e.g. 6 or 8).</summary>
    public double CrossTieDiameter { get; init; } = 8.0;

    /// <summary>Longitudinal spacing along beam axis between cross-ties (mm, default: 400 mm).</summary>
    public double CrossTieSpacing { get; init; } = 400.0;

    /// <summary>Hook configuration for cross-ties.</summary>
    public CrossTieHookType CrossTieHook { get; init; } = CrossTieHookType.Hook90And135;

    /// <summary>Revit RebarBarType name for longitudinal side bars.</summary>
    public string SideBarTypeName { get; init; } = string.Empty;

    /// <summary>Revit RebarBarType name for transverse cross-ties.</summary>
    public string CrossTieBarTypeName { get; init; } = string.Empty;
}
```

#### `BeamSpecialBarSpec.cs`
Specification for secondary beam intersection reinforcement (shear hanging stirrups and diagonal ties).
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Detailing specification for secondary framing beam intersection reinforcement.
/// Handles concentrated shear hanging stirrups ("cốt treo") and diagonal bent ties ("thép vai bò").
/// </summary>
public sealed record BeamSpecialBarSpec
{
    // --- Hanging Stirrups (Cốt treo) ---
    /// <summary>True to generate concentrated hanging stirrups flanking secondary beam joints.</summary>
    public bool EnableHangingStirrups { get; init; } = true;

    /// <summary>Number of stirrup pairs on EACH side of the incoming secondary beam (default: 3 pairs).</summary>
    public int HangingStirrupsPerSide { get; init; } = 3;

    /// <summary>Diameter of hanging stirrups (mm, e.g. 8 or 10).</summary>
    public double HangingStirrupDiameter { get; init; } = 8.0;

    /// <summary>Close spacing between hanging stirrups (mm, default: 50 mm).</summary>
    public double HangingStirrupSpacing { get; init; } = 50.0;

    // --- Diagonal Ties (Thép vai bò) ---
    /// <summary>True to generate 45° diagonal bent bars under the secondary beam soffit.</summary>
    public bool EnableDiagonalTies { get; init; }

    /// <summary>Number of diagonal bars across the beam width (default: 2).</summary>
    public int DiagonalTieCount { get; init; } = 2;

    /// <summary>Diameter of diagonal bent bars (mm, e.g. 14 or 16).</summary>
    public double DiagonalTieDiameter { get; init; } = 14.0;

    /// <summary>Angle of diagonal inclination in degrees (default: 45.0°).</summary>
    public double DiagonalAngleDegrees { get; init; } = 45.0;

    // --- Revit Types ---
    /// <summary>Revit RebarBarType name for hanging stirrups.</summary>
    public string HangingStirrupTypeName { get; init; } = string.Empty;

    /// <summary>Revit RebarBarType name for diagonal ties.</summary>
    public string DiagonalTieTypeName { get; init; } = string.Empty;
}
```

---

### 4.4 Enums & Validation Types

#### `Enums.cs`
Comprehensive enumerations capturing structural detailing classifications.
```csharp
namespace HPRebar.Core.BeamRebar.Models;

/// <summary>Classification of structural bearing supports under a continuous beam.</summary>
public enum SupportType
{
    None = 0,
    Column = 1,
    Wall = 2,
    Girder = 3,
    CantileverLeft = 4,
    CantileverRight = 5
}

/// <summary>Stirrup distribution layout algorithm across a clear span.</summary>
public enum StirrupLayout
{
    /// <summary>Uniform spacing throughout the clear span.</summary>
    Uniform = 0,

    /// <summary>Dense support zones (Ln/4) and sparse midspan zone (Ln/2).</summary>
    ThreeZoneL4 = 1,

    /// <summary>Dense support zones (Ln/3) and sparse midspan zone (Ln/3).</summary>
    ThreeZoneL3 = 2
}

/// <summary>End anchorage hook bend type for longitudinal reinforcing bars.</summary>
public enum EndAnchorageType
{
    None = 0,
    Hook90Down = 1,
    Hook90Up = 2,
    Hook135 = 3,
    Hook180 = 4
}

/// <summary>Structural role and classification of reinforcing bars.</summary>
public enum BarType
{
    MainTop = 1,
    MainBottom = 2,
    AdditionalTop = 3,
    AdditionalBottom = 4,
    SideSkin = 5,
    CrossTie = 6,
    HangingStirrup = 7,
    DiagonalTie = 8
}

/// <summary>Standard rebar hook bend angles in degrees.</summary>
public enum HookAngle
{
    None = 0,
    Hook90 = 90,
    Hook135 = 135,
    Hook180 = 180
}

/// <summary>Overhang cantilever placement on a continuous beam assembly.</summary>
public enum CantileverPosition
{
    None = 0,
    Left = 1,
    Right = 2,
    Both = 3
}

/// <summary>Side where a secondary framing beam intersects the primary beam.</summary>
public enum IntersectionSide
{
    Both = 0,
    Left = 1,
    Right = 2
}

/// <summary>Hook configurations for transverse anti-buckling cross-ties.</summary>
public enum CrossTieHookType
{
    /// <summary>90° hook at one end, 135° hook at the other end (recommended for ease of placement).</summary>
    Hook90And135 = 0,

    /// <summary>135° seismic hook at both ends.</summary>
    Hook135And135 = 1,

    /// <summary>180° hook at both ends.</summary>
    Hook180And180 = 2
}
```

#### `ValidationResult.cs`
Lightweight immutable result record for domain validations.
```csharp
using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Result of a geometric or parameter validation check.
/// </summary>
public sealed record ValidationResult
{
    public bool IsSuccess { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static ValidationResult Ok() => new() { IsSuccess = true };

    public static ValidationResult Fail(string error) => new()
    {
        IsSuccess = false,
        ErrorMessage = error
    };

    public static ValidationResult WithWarnings(IReadOnlyList<string> warnings) => new()
    {
        IsSuccess = true,
        Warnings = warnings
    };
}
```

---

## 5. End-to-End Data Flow & Interface Contracts

The domain models defined above serve as the contract at every phase of the continuous beam reinforcement workflow:

```
┌────────────────────────────────────────────────────────────────────────┐
│                        Phase 1: Revit Reading                          │
│  Selected Framing Elements -> BeamStackReader -> BeamSupportFinder     │
│  Output: BeamContinuousStack (Immutable Spans & Support Nodes in mm)   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        Phase 2: UI Presentation                        │
│  BeamContinuousStack -> BeamRebarViewModel -> BeamRebarView (Modal)    │
│  User configures: StirrupSpec, MainBarSpec, AddBarSpec, SideBarSpec    │
│  Preview: BeamElevationCanvas rendering directly from Domain Models    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        Phase 3: Pure Calculations                      │
│  HPRebar.Core.BeamRebar.Calculators:                                   │
│  - BeamStirrupDistributionCalculator -> IReadOnlyList<StirrupZone>    │
│  - BeamMainBarCalculator             -> IReadOnlyList<BarPolyline>     │
│  - BeamAdditionalBarCalculator       -> IReadOnlyList<BarPolyline>     │
│  - BeamSideBarCalculator             -> IReadOnlyList<BarPolyline>     │
│  - BeamSpecialBarCalculator          -> IReadOnlyList<BarPolyline>     │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        Phase 4: Revit Generation                       │
│  BeamRebarOrchestrator wraps atomic TransactionGroup("Beam Rebar"):    │
│  - BeamStirrupCreator: Rebar.CreateFromRebarShape + SetLayout...       │
│  - BeamMainBarCreator: Rebar.CreateFromCurves (from BarPolyline)       │
│  - BeamDetailViewCreator & DimensionCreator                            │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Edge Cases & Numerical Guardrails Handled by Domain Models

| Edge Case | Domain Model Mechanism | Benefit to System |
|-----------|------------------------|-------------------|
| **Sub-millimeter segments (< 1.0 mm)** | `Polyline3.Simplify(minSegmentLength = 1.0)` | Merges close points to guarantee segments exceed Revit's `Application.ShortCurveTolerance` (~0.78 mm), preventing `ArgumentException: Curve is too short`. |
| **Max Stirrup Positions (> 1002)** | `StirrupZone.Count` & `StirrupRun.Count` validated against guardrail | Prevents fatal Revit runtime crash in `RebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing`. |
| **Multi-layer Crowding** | `SupportAdditionalTopBarConfig.LayerGap`, `SpanAdditionalBottomBarConfig.LayerGap`, and `BarPolyline.Layer` | Provides structured $\Delta Z$ separation ($d_{bar} + 30$ mm) between bar layers to satisfy building code clear spacing rules. |
| **Reversed Draw Direction** | `BeamSpan.StartX`, `EndX` and `Point3` along monotonic datum $\vec{U}_X$ | Eliminates inverted vector bugs; guarantees consistent left-to-right calculation regardless of Revit beam draw order. |
| **Variable Depth Steps** | `BeamSpan.Height`, `TopElevation`, `BottomElevation` distinct per span | Enables `BeamMainBarCalculator` to detect vertical soffit steps and insert 90° upward anchorage hooks at transitions. |
| **Cantilevers (Exterior Overhangs)** | `SupportType.CantileverLeft`, `CantileverRight`, `BeamSpan.IsCantilever` | Distinguishes overhang spans where top tension bars extend to the tip and turn down 90°, while bottom bars stop at the support. |
| **Secondary Framing Joint Clustering** | `SecondaryBeamIntersection.LeftFaceX`, `RightFaceX` | Enables detection of adjacent secondary beams (< 200 mm apart) and merging of overlapping hanging stirrup zones. |
| **Zero-Revit Unit Safety** | All properties in `HPRebar.Core` explicitly typed as `double` representing mm | Prevents culture-dependent string parsing errors (`double.Parse("300,5")`) by keeping pure numbers in the domain core. |

---

## 7. Testability & xUnit v3 Alignment (`TestBeamData.cs`)

The proposed domain models are 100% testable in `HPRebar.Core.Tests/BeamRebar/` without requiring Revit processes or mocking.

Example test fixture builder pattern for `TestBeamData.cs`:
```csharp
namespace HPRebar.Core.Tests.BeamRebar;

public static class TestBeamData
{
    /// <summary>Standard 2-span continuous beam: 2x 6000mm spans, 300x600mm section.</summary>
    public static BeamContinuousStack CreateTwoSpanBeam(
        double b = 300, 
        double h = 600, 
        double spanLength = 6000, 
        double columnWidth = 400)
    {
        var s0 = new BeamSupportNode { Index = 0, Name = "C0", CenterX = 0, Width = columnWidth, Depth = 400, Type = SupportType.Column };
        var s1 = new BeamSupportNode { Index = 1, Name = "C1", CenterX = spanLength, Width = columnWidth, Depth = 400, Type = SupportType.Column };
        var s2 = new BeamSupportNode { Index = 2, Name = "C2", CenterX = spanLength * 2, Width = columnWidth, Depth = 400, Type = SupportType.Column };

        var span0 = new BeamSpan
        {
            Index = 0,
            Name = "Span 1",
            Width = b,
            Height = h,
            TopElevation = 3600,
            LengthCenter = spanLength,
            LengthClear = spanLength - columnWidth,
            StartX = columnWidth / 2.0,
            CenterStartX = 0,
            Cover = 25.0
        };

        var span1 = new BeamSpan
        {
            Index = 1,
            Name = "Span 2",
            Width = b,
            Height = h,
            TopElevation = 3600,
            LengthCenter = spanLength,
            LengthClear = spanLength - columnWidth,
            StartX = spanLength + (columnWidth / 2.0),
            CenterStartX = spanLength,
            Cover = 25.0
        };

        return new BeamContinuousStack
        {
            Spans = new[] { span0, span1 },
            Supports = new[] { s0, s1, s2 }
        };
    }
}
```

---

## 8. Implementation Checklist & File Mapping

The following 17 C# source files will be created in `HPRebar.Core/BeamRebar/Models/`:

| # | File Name | Type | Core Responsibilities |
|---|-----------|------|-----------------------|
| 1 | `Point3.cs` | `readonly struct` | 3D Cartesian point in continuous beam local datum (mm). Vector addition/subtraction. |
| 2 | `Vector3.cs` | `readonly struct` | 3D vector math (dot, cross, normalize, length). |
| 3 | `Polyline3.cs` | `sealed record` | Ordered 3D points container, total length, vertex simplification for short curve tolerance. |
| 4 | `BarPolyline.cs` | `sealed record` | Rebar centerline curve, diameter, layer, hook angles, and Revit type metadata. |
| 5 | `BeamSpan.cs` | `sealed record` | Individual span geometry ($b, h, L_c, L_n, Z_{top}, Z_{bot}, c$), start/end stations. |
| 6 | `BeamSupportNode.cs` | `sealed record` | Physical support ($C, B, X_{center}$, `SupportType`), exterior flag, left/right faces. |
| 7 | `SecondaryBeamIntersection.cs` | `sealed record` | Secondary framing beam joint ($b_s, h_s, X_{center}$, framing side) for hanging ties. |
| 8 | `BeamContinuousStack.cs` | `sealed record` | Master assembly holding ordered spans, supports, secondary intersections, and validation. |
| 9 | `BeamStirrupSpec.cs` | `sealed record` | Stirrup layout parameters (layout type, diameter, dense/sparse spacing, cover, node ties). |
| 10 | `StirrupZone.cs` | `sealed record` | Calculated stirrup run zone (start, end, length, spacing, count, individual positions). |
| 11 | `StirrupRun.cs` | `sealed record` | Parameters for native `RebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing`. |
| 12 | `BeamMainBarSpec.cs` | `sealed record` | Longitudinal top & bottom bar specs, anchorages, commercial division (11.7m), stagger rules. |
| 13 | `BeamAdditionalBarSpec.cs` | `sealed record` | Support top bars ($L/3, L/4$, layers 1 & 2) and midspan bottom bars ($L/7$, layers 1 & 2). |
| 14 | `BeamSideBarSpec.cs` | `sealed record` | Deep beam skin bars ($h \ge 700$ mm, $s \le 300$ mm) and anti-buckling cross-ties. |
| 15 | `BeamSpecialBarSpec.cs` | `sealed record` | Secondary intersection hanging stirrup cages ($n \times 2$ @ 50mm) and 45° diagonal ties. |
| 16 | `Enums.cs` | `enum` definitions | `SupportType`, `StirrupLayout`, `EndAnchorageType`, `BarType`, `HookAngle`, `CantileverPosition`, `IntersectionSide`, `CrossTieHookType`. |
| 17 | `ValidationResult.cs` | `sealed record` | Immutable validation feedback holding success status, errors, and warnings. |

---

## 9. Conclusion

The domain model architecture specified herein provides a complete, robust, type-safe foundation for `HPRebar.Core/BeamRebar`. By enforcing absolute zero-Revit isolation, immutability, standard millimetre units, and strict numerical safeguards, this design guarantees seamless interoperability across the upcoming domain calculators (`explorer_m1_2`), unit tests (`spec_miner_m1_3`), and Revit execution services.
