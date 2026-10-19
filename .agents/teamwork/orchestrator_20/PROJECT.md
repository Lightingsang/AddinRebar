# Project: HPDcePro Structural Engineering Clean-Room Rewrite & REA Reverse Engineering Dossier

## Architecture

The `HPDcePro` ecosystem is a clean-room C# rewrite of the legacy `C:\Dce Pro` structural engineering software. It replaces native C++ binaries (`DceAnalyssiss64.dll`), Excel COM automation, and legacy obfuscated code with high-performance, modular, testable .NET libraries adhering strictly to HP Clean Code Governance.

```
HPDcePro/
├── HPDcePro.slnx                      # .NET solution (SDK 10.0.400, MTP runner)
├── global.json                        # SDK pin 10.0.300 rollForward: latestMinor, MTP runner
├── Directory.Build.props              # TreatWarningsAsErrors=true, LangVersion=latest, Nullable=enable
├── src/
│   ├── HPDcePro.Core/                 # Pure C# domain core (netstandard2.0;net8.0)
│   │   ├── Geometry/                  # Pure C# 2D geometry replacing native DceAnalyssiss64.dll
│   │   ├── Serialization/             # .autostr JSON serializer & AutoStrConfig models
│   │   ├── XData/                     # CAD XData codecs for 13 RegApps (RebarXdataDCE, etc.)
│   │   ├── MachineLearning/           # SQLite mauhoc.db reader & Tokenizer for rebar callouts
│   │   ├── Calculations/              # TCVN 5574:2018 (Beam), TCVN 10304:2025 (Pile), TCVN 5575:2024 (Steel), TCVN 2737:2023 (Wind)
│   │   └── Rebar/                     # 1D Cutting Stock Optimizer (11.7m) & Schedule Aggregator
│   ├── HPDcePro.UI/                   # WPF MVVM UI library (net8.0-windows) with MahApps.Metro 2.4.11
│   │   ├── Common/                    # ThemeManager, MetroWindow base, RelayCommand, converters
│   │   ├── ViewModels/                # BeamDetailingViewModel, CuttingOptimizationViewModel, AutoStrConfigViewModel
│   │   └── Views/                     # MetroWindow views with Segoe UI and Dark/Light support
│   ├── HPDcePro.AutoCad/              # AutoCAD 2026 command adapter (net8.0-windows)
│   │   ├── Commands/                  # [CommandMethod] handlers for 61 legacy commands
│   │   └── Lisp/                      # Clean HPDcePro.lsp loader & geometric utility LISPs
│   ├── HPDcePro.Excel/                # ClosedXML headless schedule & calculation exporter (net8.0)
│   └── HPDcePro.Revit/                # Revit 2026 data exchange adapter skeleton (net8.0-windows)
└── tests/
    └── HPDcePro.Core.Tests/           # xUnit v3 / MTP unit & integration test suite (net10.0)
        ├── Geometry/                  # Polygon2D, Ray-casting, clipping tests
        ├── Serialization/             # Real DuAnMau.autostr round-trip tests
        ├── XData/                     # 13 RegApps encode/decode & corruption recovery tests
        ├── MachineLearning/           # Real mauhoc.db SQLite inspection & token prediction tests
        ├── Calculations/              # TCVN 5574, 10304, 5575, 2737 formula tests
        └── Rebar/                     # 1D 11.7m Cutting stock optimizer & schedule aggregation tests
```

---

## Feature Inventory

Every feature identified during the survey phase is mapped to a specific milestone:

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Binary Inventory & SHA-256 Catalog | Document 6 target binaries + auxiliaries with exact SHA-256 and REA evidence IDs in `docs/rea-dce-pro/README.md` | M1 | Survey 1, R1 |
| 2 | Decompiled Architecture & Algorithm Docs | Document RC Beam, Column/Wall, Slab, Foundation, Stairs, Rebar schedule, AutoStr 24 steps, MayHoc ML, Steel, and Excel UDFs in `docs/rea-dce-pro/` | M1 | Survey 1, 2, R1 |
| 3 | Command & LISP Mapping Table | Document all 61 AutoCAD commands, 5 LISP utilities, and 24 AutoStr steps with legacy-to-HPDcePro mapping | M1 | Survey 2, R1 |
| 4 | Staged 6-Phase Technical Roadmap | Outline 6-phase technical roadmap from Phase 1 (Core Foundation) to Phase 6 (Ecosystem Integration) | M1 | Survey 1, 2, 3, R1 |
| 5 | Solution Scaffold & Governance | Create `HPDcePro/HPDcePro.slnx`, `global.json`, `Directory.Build.props` with zero warnings/errors | M2 | Survey 3, R2 |
| 6 | Pure C# 2D Geometry Engine | Implement `Point2D`, `Vector2D`, `Segment2D`, `Polygon2D`, ray-casting point-in-polygon, polygon area/centroid, line intersection, segment clipping, convex hull, replacing 69 C++ functions in `DceAnalyssiss64.dll` | M2 | Survey 1, 2, R2 |
| 7 | `.autostr` Project Serialization | Strongly-typed JSON serializer/deserializer for `.autostr` compatible with `DuAnMau.autostr` (`AutoStrConfig`, combos, springs, floors, materials) | M2 | Survey 2, R2 |
| 8 | CAD XData Codec (13 RegApps) | Full encoder/decoder for `RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`, `Dce_ThepKep`, `XDATA_APP_SODOCAT`, etc., with malformed packet recovery | M2 | Survey 2, R2 |
| 9 | SQLite `mauhoc.db` ML Tokenizer | SQLite reader and rebar text tokenizer for `mauhoc.db` (45,020 samples) extracting `SH`, `SL`, `DK`, `KC`, `CD` tokens | M2 | Survey 2, 3, R2 |
| 10 | Zero Legacy DRM & Telemetry Invariant | Ensure 0 references to `LicenseMoi`, `LicenseCheckAPI`, `DceSerinumber`, `DbimTaiKhoanApi`, `DceUpdate`, `DceGuard` | M2 | Survey 1, R2 |
| 11 | TCVN 5574:2018 RC Beam Engine | Flexure ($M_u$, single/double reinforcement), shear ($V_u$, stirrups), crack width ($a_{crc}$), rebar detailing rules | M3 | Survey 1, 2, R2 |
| 12 | 1D Rebar Cutting Stock Optimizer | 1D cutting stock optimization (`ToiUuCatThep`) for 11,700 mm commercial bars, lap splice rules, leftover scrap tracking | M3 | Survey 1, 2, R2 |
| 13 | Rebar Schedule Aggregation | Structural rebar schedule aggregator (`ThongKeThep`) by element, diameter, shape, mass calculation | M3 | Survey 1, 2, R2 |
| 14 | TCVN 10304:2025 Pile Capacity Model | Pure C# calculation of soil SPT pile bearing capacity ($R_c, R_u$) | M3 | Survey 2, R2 |
| 15 | TCVN 5575:2024 Steel Design Model | Pure C# section properties and flexural/buckling checks for I/H sections | M3 | Survey 2, R2 |
| 16 | TCVN 2737:2023 Wind Load Model | Pure C# aerodynamic wind load calculation and load combo generator | M3 | Survey 2, R2 |
| 17 | `HPDcePro.UI` MahApps.Metro MVVM | `MetroWindow` views with Dark/Light theme switching for Beam Detailing, Cutting Stock, AutoStr Config | M4 | Survey 3, R3 |
| 18 | `HPDcePro.AutoCad` Command Adapter | [CommandMethod] adapters for legacy commands (`ShopDrawingBeam`, `VeDam_DCE`, `ToiUuCatThep`, `tkt_dce`, etc.) and clean `HPDcePro.lsp` loader | M4 | Survey 2, 3, R3 |
| 19 | `HPDcePro.Excel` Headless Export | ClosedXML schedule export and calculation report generator without Excel COM automation | M4 | Survey 2, 3, R3 |
| 20 | `HPDcePro.Revit` Exchange Skeleton | Structural framing and rebar data exchange adapter skeleton compatible with repo Revit architecture | M4 | Survey 3, R3 |
| 21 | Automated Test Suite (>= 80 tests) | Comprehensive xUnit v3 / MTP test suite in `HPDcePro.Core.Tests` covering all core modules, passing 100% | M5 | Survey 3, R4 |
| 22 | Clean Code & Forensic Audit Verification | Strict compliance with file <= 300 lines, method <= 50 lines, 0 warnings, clean forensic audit verdict | M5 | Governance |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | REA Reverse-Engineering Dossier | Features 1, 2, 3, 4 (`docs/rea-dce-pro/` complete reverse engineering documentation) | Survey done | DONE (5 documents, 69 exports, 69 commands, CLEAN audit) |
| M2 | Solution Setup & Pure C# Domain Core | Features 5, 6, 7, 8, 9, 10 (`HPDcePro.slnx`, 2D Geometry, `.autostr` I/O, XData, SQLite ML, zero DRM) | M1 contracts | DONE (22 tests pass, 0 warnings, 0 errors) |
| M3 | Structural Calculation Engines & Optimization | Features 11, 12, 13, 14, 15, 16 (TCVN 5574, Cutting Stock 11.7m, Schedule, TCVN 10304/5575/2737) | M2 | DONE (64 tests pass, 100% genuine, 0 warnings/errors) |
| M4 | Multi-Host Adapters & MahApps.Metro UI | Features 17, 18, 19, 20 (`HPDcePro.UI`, `HPDcePro.AutoCad`, `HPDcePro.Excel`, `HPDcePro.Revit`) | M2, M3 | DONE (4 adapter projects, 70 AutoCAD commands, MahApps.Metro UI, ClosedXML, Revit exchange, 77 tests pass, 0 warnings/errors) |
| M5 | Automated Test Suite & Final Verification | Features 21, 22 (`HPDcePro.Core.Tests` >= 80 tests, build & test clean, audit PASS) | M2, M3, M4 | DONE (118 tests pass across solution, 100% genuine, 0 warnings/errors) |

---

## Interface Contracts

### 1. Geometry ↔ Structural Detailing (`HPDcePro.Core.Geometry` ↔ `HPDcePro.Core.Rebar`)
```csharp
namespace HPDcePro.Core.Geometry;

public readonly struct Point2D(double x, double y)
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public double DistanceTo(Point2D other);
}

public readonly struct Vector2D(double x, double y)
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Length { get; }
    public Vector2D Normalize();
    public double Dot(Vector2D other);
    public double Cross(Vector2D other);
}

public class Polygon2D
{
    public IReadOnlyList<Point2D> Vertices { get; }
    public bool ContainsPoint(Point2D pt);
    public double Area { get; }
    public Point2D Centroid { get; }
    public Polygon2D Offset(double distance);
    public static Polygon2D ConvexHull(IEnumerable<Point2D> points);
}
```

### 2. Project Serialization (`HPDcePro.Core.Serialization`)
```csharp
namespace HPDcePro.Core.Serialization;

public class AutoStrProjectSerializer
{
    public static AutoStrConfig LoadFromFile(string filePath);
    public static AutoStrConfig Deserialize(string json);
    public static string Serialize(AutoStrConfig config);
    public static void SaveToFile(string filePath, AutoStrConfig config);
}
```

### 3. CAD XData Codec (`HPDcePro.Core.XData`)
```csharp
namespace HPDcePro.Core.XData;

public interface IXDataCodec<T>
{
    string RegAppName { get; }
    XDataPacket Encode(T model);
    T Decode(XDataPacket packet);
    bool TryDecode(XDataPacket packet, out T? result);
}
```

### 4. RC Beam Calculation & Detailing (`HPDcePro.Core.Calculations`)
```csharp
namespace HPDcePro.Core.Calculations;

public class BeamSectionDesignService
{
    public BeamFlexureResult CalculateFlexureReinforcement(BeamDesignInput input);
    public BeamShearResult CalculateShearReinforcement(BeamShearInput input);
    public BeamCrackResult CheckCrackWidth(BeamCrackInput input);
}
```

### 5. 1D Cutting Stock Optimization (`HPDcePro.Core.Rebar`)
```csharp
namespace HPDcePro.Core.Rebar;

public class RebarCuttingOptimizer
{
    public const double CommercialBarLengthMm = 11700.0;
    public CuttingOptimizationResult Optimize(IReadOnlyList<RebarDemandItem> demands, CuttingStockOptions options);
}
```

---

## Code Layout

- Every project in `HPDcePro/` follows the isolated workspace standard:
  - `HPDcePro.Core`: Target `netstandard2.0;net8.0`, references 0 host assemblies.
  - `HPDcePro.Core.Tests`: Target `net10.0`, references `HPDcePro.Core`, `xunit.v3`, `Microsoft.Testing.Platform`.
  - `HPDcePro.UI`: Target `net8.0-windows`, references `HPDcePro.Core`, `MahApps.Metro` 2.4.11, `CommunityToolkit.Mvvm` 8.4.0.
  - `HPDcePro.AutoCad`: Target `net8.0-windows`, references `HPDcePro.Core`, `HPDcePro.UI`, `AutoCAD.NET` 25.1.0.
  - `HPDcePro.Excel`: Target `net8.0`, references `HPDcePro.Core`, `ClosedXML` 0.104.2.
  - `HPDcePro.Revit`: Target `net8.0-windows`, references `HPDcePro.Core`, `Nice3point.Revit.Api` 2026.0.4.
- Clean Code rules enforced:
  - File length $\le 300$ lines (Rule C2).
  - Method length $\le 50$ lines (Rule M3).
  - Pure functions in Core; no `ref` accumulators (Rule M6).
  - 0 commented-out code (Rule Q-B1), 0 unannotated catch blocks (Rule Q-B2).
  - Compiler warnings treated as errors (`TreatWarningsAsErrors=true`).
  - Builds executed with `--disable-build-servers -m:1`.
