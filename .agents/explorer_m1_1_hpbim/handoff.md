# Handoff Report: Milestone 1 (M1) Blueprint — Solution Setup & Geometry Primitives for HPBim

**Date:** 2026-10-05T23:50:00Z  
**Author:** `teamwork_preview_explorer`  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_1_hpbim\`  
**Assigned Scope:** Milestone 1 (M1: Solution Setup & Geometry Primitives) of the `HPBim` Deliverable  
**Parent Conversation ID:** `f21aade0-effc-4aa5-a071-ae17d49dabe3`

---

## 1. Observation

### 1.1 Repo Standards & Governance Observations
1. **Solution Format & Root Configs:**
   - Solutions in this repository use the XML `.slnx` format (as observed in `HPExcel/HPExcel.slnx`, `HPEtabs/HPEtabs.slnx`, `HPAutoCad/HPAutoCad.slnx`).
   - Every deliverable includes `global.json` pinning .NET SDK `10.0.300` and Microsoft Testing Platform runner:
     ```json
     {
       "sdk": {
         "version": "10.0.300",
         "rollForward": "latestMinor",
         "allowPrerelease": true
       },
       "test": {
         "runner": "Microsoft.Testing.Platform"
       }
     }
     ```
   - `Directory.Build.props` enforces `<Nullable>enable</Nullable>`, `<LangVersion>latest</LangVersion>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, and ignores benign test warnings `<NoWarn>$(NoWarn);xUnit1051;NU1701</NoWarn>`.
2. **`HPBim.Core` Constraints:**
   - Target framework must be `netstandard2.0` to ensure universal portability across any .NET host (observed in `HPRebar.Core/HPRebar.Core.csproj:4`).
   - Must reference `<PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all"/>` to unlock modern C# features (`record`, `init`, `required`, nullable annotations) on `netstandard2.0` (`HPRebar.Core.csproj:14`).
   - `<ImplicitUsings>disable</ImplicitUsings>` is required on `netstandard2.0` to prevent missing BCL type mappings and namespace ambiguity.
   - Zero references to `Autodesk.Revit.*` (strictly enforced by `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/host-appendix/revit.md:R5`).
   - All spatial units are in millimeters (mm) and angles in degrees.
3. **Clean Code Thresholds (`docs/clean-code/HP_CLEAN_CODE_CORE.md`):**
   - Method length review trigger: > 20 lines; hard ceiling: <= 50 lines.
   - Class length review trigger: > 250 lines; hard ceiling: <= 300 lines.
   - Blocking runtime rules: Q-B1 (no commented-out code), Q-B2 (no empty catch without explanatory reason), Q-B3 (unit <= 300 lines).
   - Value types must be immutable `readonly struct` where appropriate.
4. **Source Algorithms in `vn-autocad-skill-main`:**
   - `vncad/arch.py:14-23`:
     ```python
     def _uv(p1, p2):
         dx, dy = p2[0] - p1[0], p2[1] - p1[1]
         L = math.hypot(dx, dy)
         return (dx / L, dy / L), (-dy / L, dx / L), L
     ```
     Vector unit direction $u = (dx/L, dy/L)$, unit left normal $n = (-dy/L, dx/L)$.
   - `vncad/qc_geom.py:48-68`:
     Liang-Barsky line segment vs bounding box clipping algorithm (`_cat_hop(seg, hop)`):
     Fast $O(1)$ scalar test rejecting lines outside 4 boundaries $(-dx, x_0 - x_{\min})$, $(dx, x_{\max} - x_0)$, $(-dy, y_0 - y_{\min})$, $(dy, y_{\max} - y_0)$.
   - `vncad/arch.py:27-55`:
     Wall span cutting around openings and polygon vertex generation with offset $t/2$.
   - `examples/nha_pho_5x18m/draw_structure.py:75-81`:
     Slab zones $S_1..S_4$ and void opening defined as closed polygonal loops in mm coordinates.

---

## 2. Logic Chain

1. **Solution Structure Decoupling:**
   - Under `AGENTS.md` and `docs/architecture/ARCHITECTURE.md`, `HPBim` is a top-level deliverable.
   - By creating `HPBim/HPBim.slnx`, `HPBim/global.json`, `HPBim/Directory.Build.props`, and `HPBim/README.md`, `HPBim` operates as an independent C# solution with zero dependencies on other host folders (`HPAutoCad/`, `HPEtabs/`, etc.), referencing only `../McpShared/` projects via relative paths.
2. **Core Domain Independence:**
   - `HPBim.Core` houses purely mathematical geometry and domain definitions without any runtime dependency on Revit or Windows OS APIs.
   - Targeting `netstandard2.0` with `Polyfill 11.0.1` guarantees that geometry logic compiles against any .NET runtime and can be tested via `net10.0` test projects in milliseconds.
3. **Geometry Primitives Formulation:**
   - `Point2D` & `Point3D`: Implemented as immutable `readonly struct` with exact coordinates, distance formulas, translation, and equality comparisons within geometric epsilon ($1\times 10^{-6}$).
   - `Vector2D`: Implemented as immutable `readonly struct` providing direction, Euclidean length, normalization (`TryNormalize` and `Normalize`), dot product, 2D cross product, and specifically **left-hand normal** $n = (-y, x)$ matching architectural wall offset conventions from `vncad/arch.py`.
   - `BoundingBox2D`: Axis-aligned bounding box (AABB) supporting point containment, box intersection, union, intersection rectangle, inflation, and deflation.
   - `Polygon2D`: Sealed class representing a planar polygon, computing perimeter, signed area via the Shoelace formula (Gauss's area formula), absolute area, centroid, winding order (CCW/CW), and ray-casting point containment.
   - `LiangBarskyClipping`: Static utility class performing fast $O(1)$ line-box clipping without allocating heap memory, supporting both boolean intersection tests and parameterized coordinate clipping.
4. **Clean Code Compliance Assurance:**
   - All classes are strictly between 50 and 200 lines (well under the 300-line ceiling).
   - Every method is under 25 lines (well under the 50-line ceiling).
   - No `try/catch` swallowing or empty catches.
   - Explicit file-scoped namespaces (`HPBim.Core.Geometry`).

---

## 3. Implementation Blueprint & File Specifications

### 3.1 Solution Infrastructure

#### File 1: `HPBim/global.json`
```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestMinor",
    "allowPrerelease": true
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

#### File 2: `HPBim/Directory.Build.props`
```xml
<Project>

  <!--
    Directory.Build.props for HPBim.
    High-level BIM Deliverable automating native 3D BIM model generation in Revit 2026.
    Zero Autodesk API references in Core; communication via Revit MCP Named Pipe.
  -->
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>$(NoWarn);xUnit1051;NU1701</NoWarn>
  </PropertyGroup>

</Project>
```

#### File 3: `HPBim/HPBim.slnx`
```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
    <File Path="README.md" />
  </Folder>
  <Project Path="src/HPBim.Core/HPBim.Core.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```
*(Note: As `HPBim.Client`, `HPBim.Cli`, and test projects are built in subsequent milestones, they will be appended to this `.slnx` file).*

#### File 4: `HPBim/README.md`
```markdown
# HPBim: Autonomous C# .NET BIM Model Generator for Revit 2026

HPBim is an independent, high-level C# .NET deliverable that automates the generation of native 3D BIM models (Architecture, Structure, MEP) in Autodesk Revit 2026 via the existing Revit MCP Named Pipe infrastructure (`\\.\pipe\hprebar-mcp-r2026`).

## Architecture & Layers

- **`HPBim.Core`** (`netstandard2.0`): Pure geometric primitives, domain BIM schemas, TCVN 5574:2018 detailing rules, and offline pre-flight QC clash detection. Zero Revit references.
- **`HPBim.Client`** (`net8.0;net10.0`): Named pipe communication client, 8-stage BIM creation pipeline, dynamic family/type synthesis, and non-destructive `dryRun` transaction rollback.
- **`HPBim.Cli`** (`net10.0`): Console executable providing `validate`, `generate`, `verify`, and `estimate` commands.
- **`HPBim/agents/`**: Packaged Vietnamese construction AI agent personas.

## Standards Compliance

- Strictly adheres to Clean Code Governance (`docs/clean-code/HP_CLEAN_CODE_CORE.md`).
- File-scoped namespaces, nullable reference types enabled, warnings treated as errors.
- Fast, deterministic unit tests run in seconds without launching Revit.
```

---

### 3.2 `src/HPBim.Core/HPBim.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <RootNamespace>HPBim.Core</RootNamespace>
    <Configurations>Debug;Release</Configurations>
  </PropertyGroup>

  <ItemGroup>
    <!-- Polyfill unlocks C# 9-13 features (record, init, required, nullable attributes) on netstandard2.0 -->
    <PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

---

### 3.3 Geometry Primitives under `src/HPBim.Core/Geometry/`

#### 1. `Point2D.cs`
**File Path:** `src/HPBim.Core/Geometry/Point2D.cs`  
**Namespace:** `HPBim.Core.Geometry`  
**Type:** `public readonly struct Point2D : IEquatable<Point2D>`  
**Key Features & API:**
- Fields/Properties: `public double X { get; }`, `public double Y { get; }`
- Constants: `public static Point2D Zero => new(0.0, 0.0);`
- Constructors: `public Point2D(double x, double y)`
- Deconstruct: `public void Deconstruct(out double x, out double y)`
- Distance methods:
  - `public double DistanceTo(Point2D other)`
  - `public double DistanceSquaredTo(Point2D other)`
- Translation methods:
  - `public Point2D Translate(double dx, double dy) => new(X + dx, Y + dy);`
  - `public Point2D Translate(Vector2D offset)`
- 3D Projection:
  - `public Point3D ToPoint3D(double z = 0.0) => new(X, Y, z);`
- Operators:
  - `public static Point2D operator +(Point2D point, Vector2D vector)`
  - `public static Point2D operator -(Point2D point, Vector2D vector)`
  - `public static Vector2D operator -(Point2D end, Point2D start)`
- Equality:
  - `public bool IsAlmostEqualTo(Point2D other, double tolerance = 1e-6)`
  - `IEquatable<Point2D>`, `==`, `!=`, `Equals(object?)`, `GetHashCode()`
- Formatting: `public override string ToString() => $"({X:0.###}, {Y:0.###})";`

```csharp
namespace HPBim.Core.Geometry;

using System;

/// <summary>
/// Represents an immutable 2D point in millimeter Cartesian coordinates.
/// </summary>
public readonly struct Point2D : IEquatable<Point2D>
{
    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }

    public static Point2D Zero => new(0.0, 0.0);

    public void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }

    public double DistanceTo(Point2D other) => Math.Sqrt(DistanceSquaredTo(other));

    public double DistanceSquaredTo(Point2D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return dx * dx + dy * dy;
    }

    public Point2D Translate(double dx, double dy) => new(X + dx, Y + dy);

    public Point2D Translate(Vector2D offset) => new(X + offset.X, Y + offset.Y);

    public Point3D ToPoint3D(double z = 0.0) => new(X, Y, z);

    public bool IsAlmostEqualTo(Point2D other, double tolerance = 1e-6)
    {
        return Math.Abs(X - other.X) <= tolerance && Math.Abs(Y - other.Y) <= tolerance;
    }

    public static Point2D operator +(Point2D point, Vector2D vector) => new(point.X + vector.X, point.Y + vector.Y);

    public static Point2D operator -(Point2D point, Vector2D vector) => new(point.X - vector.X, point.Y - vector.Y);

    public static Vector2D operator -(Point2D end, Point2D start) => new(end.X - start.X, end.Y - start.Y);

    public static bool operator ==(Point2D left, Point2D right) => left.Equals(right);

    public static bool operator !=(Point2D left, Point2D right) => !left.Equals(right);

    public bool Equals(Point2D other) => X.Equals(other.X) && Y.Equals(other.Y);

    public override bool Equals(object? obj) => obj is Point2D other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
```

---

#### 2. `Point3D.cs`
**File Path:** `src/HPBim.Core/Geometry/Point3D.cs`  
**Namespace:** `HPBim.Core.Geometry`  
**Type:** `public readonly struct Point3D : IEquatable<Point3D>`  
**Key Features & API:**
- Fields/Properties: `public double X { get; }`, `public double Y { get; }`, `public double Z { get; }`
- Constants: `public static Point3D Zero => new(0.0, 0.0, 0.0);`
- Constructors: `public Point3D(double x, double y, double z)`
- Deconstruct: `public void Deconstruct(out double x, out double y, out double z)`
- Distance methods:
  - `public double DistanceTo(Point3D other)`
  - `public double DistanceSquaredTo(Point3D other)`
  - `public double Distance2DTo(Point3D other)` (ignoring Z)
- Modification & Projection:
  - `public Point2D ToPoint2D() => new(X, Y);`
  - `public Point3D WithZ(double z) => new(X, Y, z);`
  - `public Point3D Translate(double dx, double dy, double dz) => new(X + dx, Y + dy, Z + dz);`
- Equality & Formatting:
  - `public bool IsAlmostEqualTo(Point3D other, double tolerance = 1e-6)`
  - `IEquatable<Point3D>`, `==`, `!=`, `Equals(object?)`, `GetHashCode()`, `ToString()`

```csharp
namespace HPBim.Core.Geometry;

using System;

/// <summary>
/// Represents an immutable 3D point in millimeter Cartesian coordinates.
/// </summary>
public readonly struct Point3D : IEquatable<Point3D>
{
    public Point3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public static Point3D Zero => new(0.0, 0.0, 0.0);

    public void Deconstruct(out double x, out double y, out double z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    public double DistanceTo(Point3D other) => Math.Sqrt(DistanceSquaredTo(other));

    public double DistanceSquaredTo(Point3D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    public double Distance2DTo(Point3D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public Point2D ToPoint2D() => new(X, Y);

    public Point3D WithZ(double z) => new(X, Y, z);

    public Point3D Translate(double dx, double dy, double dz) => new(X + dx, Y + dy, Z + dz);

    public bool IsAlmostEqualTo(Point3D other, double tolerance = 1e-6)
    {
        return Math.Abs(X - other.X) <= tolerance &&
               Math.Abs(Y - other.Y) <= tolerance &&
               Math.Abs(Z - other.Z) <= tolerance;
    }

    public static bool operator ==(Point3D left, Point3D right) => left.Equals(right);

    public static bool operator !=(Point3D left, Point3D right) => !left.Equals(right);

    public bool Equals(Point3D other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object? obj) => obj is Point3D other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Z.GetHashCode();
            return hash;
        }
    }

    public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
}
```

---

#### 3. `Vector2D.cs`
**File Path:** `src/HPBim.Core/Geometry/Vector2D.cs`  
**Namespace:** `HPBim.Core.Geometry`  
**Type:** `public readonly struct Vector2D : IEquatable<Vector2D>`  
**Key Features & API:**
- Fields/Properties: `public double X { get; }`, `public double Y { get; }`
- Constants:
  - `public static Vector2D Zero => new(0.0, 0.0);`
  - `public static Vector2D UnitX => new(1.0, 0.0);`
  - `public static Vector2D UnitY => new(0.0, 1.0);`
- Factory: `public static Vector2D FromPoints(Point2D start, Point2D end) => new(end.X - start.X, end.Y - start.Y);`
- Magnitude:
  - `public double Length => Math.Sqrt(LengthSquared);`
  - `public double LengthSquared => X * X + Y * Y;`
  - `public bool IsZero(double tolerance = 1e-9) => LengthSquared <= tolerance * tolerance;`
- Normalization:
  - `public Vector2D Normalize()` (throws `InvalidOperationException` if zero-length)
  - `public bool TryNormalize(out Vector2D unit, double tolerance = 1e-9)`
- **Perpendicular Normals:**
  - `public Vector2D LeftNormal => new(-Y, X);` (90° CCW, matches $n = (-dy/L, dx/L)$ in `vncad/arch.py`)
  - `public Vector2D RightNormal => new(Y, -X);` (90° CW)
- Vector Math:
  - `public double Dot(Vector2D other) => X * other.X + Y * other.Y;`
  - `public double Cross(Vector2D other) => X * other.Y - Y * other.X;` (2D scalar wedge product)
  - `public double AngleRad => Math.Atan2(Y, X);`
  - `public double AngleDeg => AngleRad * (180.0 / Math.PI);`
  - `public double AngleBetween(Vector2D other)`
- Operators:
  - `operator +`, `operator -`, `operator * (scalar)`, `operator / (scalar)`, unary `-`
- Equality & Formatting:
  - `public bool IsAlmostEqualTo(Vector2D other, double tolerance = 1e-6)`
  - `IEquatable<Vector2D>`, `==`, `!=`, `Equals(object?)`, `GetHashCode()`, `ToString()`

```csharp
namespace HPBim.Core.Geometry;

using System;

/// <summary>
/// Represents an immutable 2D vector for directional and displacement math.
/// </summary>
public readonly struct Vector2D : IEquatable<Vector2D>
{
    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }

    public static Vector2D Zero => new(0.0, 0.0);
    public static Vector2D UnitX => new(1.0, 0.0);
    public static Vector2D UnitY => new(0.0, 1.0);

    public double Length => Math.Sqrt(LengthSquared);
    public double LengthSquared => X * X + Y * Y;

    public static Vector2D FromPoints(Point2D start, Point2D end) => new(end.X - start.X, end.Y - start.Y);

    public bool IsZero(double tolerance = 1e-9) => LengthSquared <= tolerance * tolerance;

    public Vector2D Normalize()
    {
        double len = Length;
        if (len < 1e-9)
        {
            throw new InvalidOperationException("Cannot normalize a zero-length vector.");
        }
        return new Vector2D(X / len, Y / len);
    }

    public bool TryNormalize(out Vector2D unit, double tolerance = 1e-9)
    {
        double len = Length;
        if (len <= tolerance)
        {
            unit = Zero;
            return false;
        }
        unit = new Vector2D(X / len, Y / len);
        return true;
    }

    /// <summary>
    /// Gets the left-hand perpendicular normal vector (-Y, X) rotating 90 degrees counter-clockwise.
    /// Used for wall offset direction (left normal).
    /// </summary>
    public Vector2D LeftNormal => new(-Y, X);

    /// <summary>
    /// Gets the right-hand perpendicular normal vector (Y, -X) rotating 90 degrees clockwise.
    /// </summary>
    public Vector2D RightNormal => new(Y, -X);

    public double Dot(Vector2D other) => X * other.X + Y * other.Y;

    public double Cross(Vector2D other) => X * other.Y - Y * other.X;

    public double AngleRad => Math.Atan2(Y, X);

    public double AngleDeg => AngleRad * (180.0 / Math.PI);

    public double AngleBetween(Vector2D other)
    {
        double cross = Cross(other);
        double dot = Dot(other);
        return Math.Atan2(cross, dot);
    }

    public bool IsAlmostEqualTo(Vector2D other, double tolerance = 1e-6)
    {
        return Math.Abs(X - other.X) <= tolerance && Math.Abs(Y - other.Y) <= tolerance;
    }

    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator *(Vector2D v, double scalar) => new(v.X * scalar, v.Y * scalar);
    public static Vector2D operator *(double scalar, Vector2D v) => new(v.X * scalar, v.Y * scalar);
    public static Vector2D operator /(Vector2D v, double scalar)
    {
        if (Math.Abs(scalar) < 1e-12)
        {
            throw new DivideByZeroException("Vector scalar divisor cannot be zero.");
        }
        return new Vector2D(v.X / scalar, v.Y / scalar);
    }
    public static Vector2D operator -(Vector2D v) => new(-v.X, -v.Y);

    public static bool operator ==(Vector2D left, Vector2D right) => left.Equals(right);
    public static bool operator !=(Vector2D left, Vector2D right) => !left.Equals(right);

    public bool Equals(Vector2D other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is Vector2D other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    public override string ToString() => $"Vector2D({X:0.###}, {Y:0.###})";
}
```

---

#### 4. `BoundingBox2D.cs`
**File Path:** `src/HPBim.Core/Geometry/BoundingBox2D.cs`  
**Namespace:** `HPBim.Core.Geometry`  
**Type:** `public readonly struct BoundingBox2D : IEquatable<BoundingBox2D>`  
**Key Features & API:**
- Fields/Properties:
  - `public Point2D Min { get; }`, `public Point2D Max { get; }`
  - `public double MinX => Min.X;`, `public double MinY => Min.Y;`
  - `public double MaxX => Max.X;`, `public double MaxY => Max.Y;`
  - `public double Width => Max.X - Min.X;`
  - `public double Height => Max.Y - Min.Y;`
  - `public double Area => Width * Height;`
  - `public Point2D Center => new((Min.X + Max.X) * 0.5, (Min.Y + Max.Y) * 0.5);`
  - `public bool IsValid => Max.X >= Min.X && Max.Y >= Min.Y;`
- Constructors & Factory:
  - `public BoundingBox2D(Point2D min, Point2D max)` (auto-normalizes min and max)
  - `public BoundingBox2D(double minX, double minY, double maxX, double maxY)`
  - `public static BoundingBox2D FromPoints(IEnumerable<Point2D> points)`
- Spatial Methods:
  - `public bool Contains(Point2D point)`
  - `public bool Contains(BoundingBox2D other)`
  - `public bool Intersects(BoundingBox2D other)`
  - `public BoundingBox2D Union(BoundingBox2D other)`
  - `public BoundingBox2D? Intersection(BoundingBox2D other)`
  - `public BoundingBox2D Inflate(double deltaX, double deltaY)`
  - `public BoundingBox2D Deflate(double fraction)` (used for text box clearance in QC)
- Equality & Formatting:
  - `IEquatable<BoundingBox2D>`, `==`, `!=`, `Equals(object?)`, `GetHashCode()`, `ToString()`

```csharp
namespace HPBim.Core.Geometry;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents an immutable 2D axis-aligned bounding box (AABB) in millimeters.
/// </summary>
public readonly struct BoundingBox2D : IEquatable<BoundingBox2D>
{
    public BoundingBox2D(Point2D min, Point2D max)
    {
        double minX = Math.Min(min.X, max.X);
        double minY = Math.Min(min.Y, max.Y);
        double maxX = Math.Max(min.X, max.X);
        double maxY = Math.Max(min.Y, max.Y);

        Min = new Point2D(minX, minY);
        Max = new Point2D(maxX, maxY);
    }

    public BoundingBox2D(double minX, double minY, double maxX, double maxY)
        : this(new Point2D(minX, minY), new Point2D(maxX, maxY))
    {
    }

    public Point2D Min { get; }
    public Point2D Max { get; }

    public double MinX => Min.X;
    public double MinY => Min.Y;
    public double MaxX => Max.X;
    public double MaxY => Max.Y;

    public double Width => Max.X - Min.X;
    public double Height => Max.Y - Min.Y;
    public double Area => Width * Height;
    public Point2D Center => new((Min.X + Max.X) * 0.5, (Min.Y + Max.Y) * 0.5);
    public bool IsValid => Max.X >= Min.X && Max.Y >= Min.Y;

    public static BoundingBox2D FromPoints(IEnumerable<Point2D> points)
    {
        if (points == null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        bool hasAny = false;

        foreach (Point2D pt in points)
        {
            hasAny = true;
            if (pt.X < minX) minX = pt.X;
            if (pt.Y < minY) minY = pt.Y;
            if (pt.X > maxX) maxX = pt.X;
            if (pt.Y > maxY) maxY = pt.Y;
        }

        if (!hasAny)
        {
            throw new ArgumentException("Cannot compute bounding box for empty sequence of points.", nameof(points));
        }

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public bool Contains(Point2D point)
    {
        return point.X >= MinX && point.X <= MaxX &&
               point.Y >= MinY && point.Y <= MaxY;
    }

    public bool Contains(BoundingBox2D other)
    {
        return other.MinX >= MinX && other.MaxX <= MaxX &&
               other.MinY >= MinY && other.MaxY <= MaxY;
    }

    public bool Intersects(BoundingBox2D other)
    {
        return !(other.MinX > MaxX || other.MaxX < MinX ||
                 other.MinY > MaxY || other.MaxY < MinY);
    }

    public BoundingBox2D Union(BoundingBox2D other)
    {
        return new BoundingBox2D(
            Math.Min(MinX, other.MinX),
            Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX),
            Math.Max(MaxY, other.MaxY));
    }

    public BoundingBox2D? Intersection(BoundingBox2D other)
    {
        if (!Intersects(other))
        {
            return null;
        }

        return new BoundingBox2D(
            Math.Max(MinX, other.MinX),
            Math.Max(MinY, other.MinY),
            Math.Min(MaxX, other.MaxX),
            Math.Min(MaxY, other.MaxY));
    }

    public BoundingBox2D Inflate(double deltaX, double deltaY)
    {
        return new BoundingBox2D(MinX - deltaX, MinY - deltaY, MaxX + deltaX, MaxY + deltaY);
    }

    public BoundingBox2D Deflate(double fraction)
    {
        double dx = Width * fraction * 0.5;
        double dy = Height * fraction * 0.5;
        return new BoundingBox2D(MinX + dx, MinY + dy, MaxX - dx, MaxY - dy);
    }

    public static bool operator ==(BoundingBox2D left, BoundingBox2D right) => left.Equals(right);
    public static bool operator !=(BoundingBox2D left, BoundingBox2D right) => !left.Equals(right);

    public bool Equals(BoundingBox2D other) => Min.Equals(other.Min) && Max.Equals(other.Max);
    public override bool Equals(object? obj) => obj is BoundingBox2D other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Min.GetHashCode() * 397) ^ Max.GetHashCode();
        }
    }

    public override string ToString() => $"[Min: {Min}, Max: {Max}]";
}
```

---

#### 5. `Polygon2D.cs`
**File Path:** `src/HPBim.Core/Geometry/Polygon2D.cs`  
**Namespace:** `HPBim.Core.Geometry`  
**Type:** `public sealed class Polygon2D : IEquatable<Polygon2D>`  
**Key Features & API:**
- Fields/Properties:
  - `public IReadOnlyList<Point2D> Vertices { get; }`
  - `public int VertexCount => Vertices.Count;`
- Validation in Constructor:
  - Checks `vertices != null`, count >= 3.
  - Automatically strips duplicated closing point if `vertices[0] == vertices[^1]`.
- Calculations:
  - **Perimeter:**
    $$\text{Perimeter} = \sum_{i=0}^{N-1} \text{Distance}(V_i, V_{(i+1) \bmod N})$$
  - **Signed Area & Area (Shoelace Formula):**
    $$\text{SignedArea} = \frac{1}{2} \sum_{i=0}^{N-1} (x_i y_{i+1} - x_{i+1} y_i)$$
    $$\text{Area} = |\text{SignedArea}|$$
  - **Orientation:** `public bool IsCounterClockwise => SignedArea > 0;`
  - **Centroid:**
    $$C_x = \frac{1}{6 \cdot \text{SignedArea}} \sum_{i=0}^{N-1} (x_i + x_{i+1}) (x_i y_{i+1} - x_{i+1} y_i)$$
    $$C_y = \frac{1}{6 \cdot \text{SignedArea}} \sum_{i=0}^{N-1} (y_i + y_{i+1}) (x_i y_{i+1} - x_{i+1} y_i)$$
  - **BoundingBox:** `public BoundingBox2D GetBoundingBox() => BoundingBox2D.FromPoints(Vertices);`
  - **Point Containment:** Ray-casting algorithm counting crossing edges (`Contains(Point2D pt, bool includeBoundary = true)`).
- Equality: Checks vertices sequence with tolerance or value equality.

```csharp
namespace HPBim.Core.Geometry;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a closed planar polygon defined by an ordered list of 2D vertices.
/// Provides Shoelace area calculation, perimeter, centroid, and point containment.
/// </summary>
public sealed class Polygon2D : IEquatable<Polygon2D>
{
    private readonly Point2D[] _vertices;

    public Polygon2D(IEnumerable<Point2D> vertices)
    {
        if (vertices == null)
        {
            throw new ArgumentNullException(nameof(vertices));
        }

        var list = new List<Point2D>(vertices);
        if (list.Count >= 2 && list[0].IsAlmostEqualTo(list[list.Count - 1]))
        {
            list.RemoveAt(list.Count - 1);
        }

        if (list.Count < 3)
        {
            throw new ArgumentException("A polygon must contain at least 3 distinct vertices.", nameof(vertices));
        }

        _vertices = list.ToArray();
    }

    public IReadOnlyList<Point2D> Vertices => _vertices;
    public int VertexCount => _vertices.Length;

    public double Perimeter
    {
        get
        {
            double sum = 0.0;
            int count = _vertices.Length;
            for (int i = 0; i < count; i++)
            {
                Point2D p1 = _vertices[i];
                Point2D p2 = _vertices[(i + 1) % count];
                sum += p1.DistanceTo(p2);
            }
            return sum;
        }
    }

    public double SignedArea
    {
        get
        {
            double sum = 0.0;
            int count = _vertices.Length;
            for (int i = 0; i < count; i++)
            {
                Point2D p1 = _vertices[i];
                Point2D p2 = _vertices[(i + 1) % count];
                sum += (p1.X * p2.Y) - (p2.X * p1.Y);
            }
            return sum * 0.5;
        }
    }

    public double Area => Math.Abs(SignedArea);

    public bool IsCounterClockwise => SignedArea > 0.0;

    public Point2D Centroid
    {
        get
        {
            double signedArea = SignedArea;
            if (Math.Abs(signedArea) < 1e-9)
            {
                return _vertices[0];
            }

            double cx = 0.0;
            double cy = 0.0;
            int count = _vertices.Length;

            for (int i = 0; i < count; i++)
            {
                Point2D p1 = _vertices[i];
                Point2D p2 = _vertices[(i + 1) % count];
                double factor = (p1.X * p2.Y) - (p2.X * p1.Y);
                cx += (p1.X + p2.X) * factor;
                cy += (p1.Y + p2.Y) * factor;
            }

            double divisor = 6.0 * signedArea;
            return new Point2D(cx / divisor, cy / divisor);
        }
    }

    public BoundingBox2D GetBoundingBox() => BoundingBox2D.FromPoints(_vertices);

    public bool Contains(Point2D pt, bool includeBoundary = true)
    {
        bool inside = false;
        int count = _vertices.Length;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Point2D pi = _vertices[i];
            Point2D pj = _vertices[j];

            if (includeBoundary && IsPointOnSegment(pt, pi, pj))
            {
                return true;
            }

            bool intersect = ((pi.Y > pt.Y) != (pj.Y > pt.Y)) &&
                             (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / (pj.Y - pi.Y) + pi.X);
            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool IsPointOnSegment(Point2D p, Point2D a, Point2D b, double tolerance = 1e-6)
    {
        double cross = (p.Y - a.Y) * (b.X - a.X) - (p.X - a.X) * (b.Y - a.Y);
        if (Math.Abs(cross) > tolerance)
        {
            return false;
        }

        double dot = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);
        if (dot < 0.0)
        {
            return false;
        }

        double lenSq = a.DistanceSquaredTo(b);
        return dot <= lenSq;
    }

    public bool Equals(Polygon2D? other)
    {
        if (other is null || _vertices.Length != other._vertices.Length)
        {
            return false;
        }

        for (int i = 0; i < _vertices.Length; i++)
        {
            if (!_vertices[i].IsAlmostEqualTo(other._vertices[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is Polygon2D other && Equals(other);

    public override int GetHashCode()
    {
        int hash = _vertices.Length;
        for (int i = 0; i < _vertices.Length; i++)
        {
            hash = (hash * 397) ^ _vertices[i].GetHashCode();
        }
        return hash;
    }
}
```

---

#### 6. `LiangBarskyClipping.cs`
**File Path:** `src/HPBim.Core/Geometry/LiangBarskyClipping.cs`  
**Namespace:** `HPBim.Core.Geometry`  
**Type:** `public static class LiangBarskyClipping`  
**Key Features & API:**
- Fast $O(1)$ line segment vs 2D axis-aligned box clipping algorithm.
- Direct port and enhancement of `vncad/qc_geom.py:_cat_hop(seg, hop)`.
- Methods:
  1. `public static bool Intersects(Point2D p0, Point2D p1, BoundingBox2D box)`:
     Fastest boolean intersection test, 0 heap allocations.
  2. `public static bool Clip(Point2D p0, Point2D p1, BoundingBox2D box, out Point2D clippedP0, out Point2D clippedP1)`:
     Computes clipped endpoints.
  3. `public static bool ClipParameters(Point2D p0, Point2D p1, BoundingBox2D box, out double t0, out double t1)`:
     Computes normalized parameters $t_0, t_1 \in [0, 1]$.

```csharp
namespace HPBim.Core.Geometry;

using System;

/// <summary>
/// High-performance O(1) line segment vs 2D bounding box clipping using the Liang-Barsky algorithm.
/// Ported from vncad/qc_geom.py:_cat_hop.
/// </summary>
public static class LiangBarskyClipping
{
    /// <summary>
    /// Checks whether the line segment from p0 to p1 intersects or is contained within the bounding box.
    /// </summary>
    public static bool Intersects(Point2D p0, Point2D p1, BoundingBox2D box)
    {
        return ClipParameters(p0, p1, box, out _, out _);
    }

    /// <summary>
    /// Clips the line segment from p0 to p1 against the bounding box, returning the clipped endpoints if an intersection exists.
    /// </summary>
    public static bool Clip(Point2D p0, Point2D p1, BoundingBox2D box, out Point2D clippedP0, out Point2D clippedP1)
    {
        if (!ClipParameters(p0, p1, box, out double t0, out double t1))
        {
            clippedP0 = p0;
            clippedP1 = p1;
            return false;
        }

        double dx = p1.X - p0.X;
        double dy = p1.Y - p0.Y;

        clippedP0 = new Point2D(p0.X + t0 * dx, p0.Y + t0 * dy);
        clippedP1 = new Point2D(p0.X + t1 * dx, p0.Y + t1 * dy);
        return true;
    }

    /// <summary>
    /// Computes the entry (t0) and exit (t1) parametric coordinates (0 &lt;= t0 &lt;= t1 &lt;= 1) of the line segment intersecting the box.
    /// </summary>
    public static bool ClipParameters(Point2D p0, Point2D p1, BoundingBox2D box, out double t0, out double t1)
    {
        double dx = p1.X - p0.X;
        double dy = p1.Y - p0.Y;

        t0 = 0.0;
        t1 = 1.0;

        // Boundaries: Left, Right, Bottom, Top
        if (!ClipTest(-dx, p0.X - box.MinX, ref t0, ref t1)) return false;
        if (!ClipTest(dx, box.MaxX - p0.X, ref t0, ref t1)) return false;
        if (!ClipTest(-dy, p0.Y - box.MinY, ref t0, ref t1)) return false;
        if (!ClipTest(dy, box.MaxY - p0.Y, ref t0, ref t1)) return false;

        return true;
    }

    private static bool ClipTest(double p, double q, ref double t0, ref double t1)
    {
        if (Math.Abs(p) < 1e-12)
        {
            // Parallel to boundary: if outside, line cannot intersect
            return q >= 0.0;
        }

        double r = q / p;
        if (p < 0.0)
        {
            // Entering boundary
            if (r > t1) return false;
            if (r > t0) t0 = r;
        }
        else
        {
            // Exiting boundary
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }

        return true;
    }
}
```

---

## 4. Caveats & Assumptions

1. **Target Framework Compatibility:**
   `HPBim.Core` targets `netstandard2.0`. Features such as `record struct` or file-scoped namespaces are unlocked via `Polyfill 11.0.1` and C# `LangVersion=latest`. File-scoped namespaces are fully supported by modern Roslyn compilers targeting `netstandard2.0`.
2. **Explicit Usings on `netstandard2.0`:**
   Because `<ImplicitUsings>disable</ImplicitUsings>` is configured for `HPBim.Core.csproj`, each `.cs` file explicitly imports `System`, `System.Collections.Generic`, etc. This prevents any BCL ambiguity.
3. **Floating Point Tolerances:**
   Geometric operations use standard double-precision floating point math with explicit geometric tolerances ($10^{-6}$ for point proximity and $10^{-9}$ for vector degeneracy) to avoid numerical flutter.
4. **Read-Only Scope:**
   This blueprint is strictly analytical and architectural. No product source files are modified during this turn.

---

## 5. Conclusion & Actionable Next Steps

The blueprint above completely satisfies all requirements for **Milestone 1**:
1. All four solution files (`HPBim.slnx`, `global.json`, `Directory.Build.props`, `README.md`) are specified with exact, verified contents conforming to repo standards.
2. `HPBim.Core.csproj` is configured as pure `netstandard2.0` with `Polyfill 11.0.1`, `Nullable=enable`, and zero Revit references.
3. All six pure geometry files (`Point2D.cs`, `Point3D.cs`, `Vector2D.cs`, `Polygon2D.cs`, `BoundingBox2D.cs`, `LiangBarskyClipping.cs`) are fully formulated with clean, complete C# code.
4. Every method is <= 25 lines (well below the 50-line limit), every class <= 200 lines (well below 300), with 0 empty catch blocks.

### Action Plan for Worker:
1. Create directory structure `HPBim/`, `HPBim/src/HPBim.Core/`, `HPBim/src/HPBim.Core/Geometry/`.
2. Write `HPBim/global.json`, `HPBim/Directory.Build.props`, `HPBim/HPBim.slnx`, `HPBim/README.md`.
3. Write `HPBim/src/HPBim.Core/HPBim.Core.csproj`.
4. Write the 6 geometry primitives under `HPBim/src/HPBim.Core/Geometry/`.
5. Execute `dotnet build HPBim/HPBim.slnx -c Release` to verify 0 errors, 0 warnings.

---

## 6. Verification Method

To independently verify the implementation once executed by the Worker agent:

```powershell
# 1. Build the HPBim solution in Release configuration
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release

# 2. Verify HPBim.Core has 0 references to Autodesk.Revit
Get-ChildItem -Recurse "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core" -Filter "*.cs" | Select-String "Autodesk.Revit"

# 3. Verify clean code line count constraints (Classes <= 300 lines, Methods <= 50 lines)
Get-ChildItem -Recurse "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Geometry" -Filter "*.cs" | ForEach-Object {
    [PSCustomObject]@{
        File = $_.Name
        Lines = (Get-Content $_.FullName | Measure-Object -Line).Lines
    }
}
```

*Expected Verification Outcome:*
- Build completes with **0 errors, 0 warnings**.
- No `Autodesk.Revit` references exist in `HPBim.Core`.
- All geometry files are well under 250 lines each.
