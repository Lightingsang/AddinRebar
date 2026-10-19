# DISPATCH — worker_m2

## 2026-10-17T16:59:37Z

## Task
You are worker_m2. Your task is to implement Milestone M2: the clean-room solution setup and pure C# domain core in `HPDcePro/`:

### 1. Solution & Build Infrastructure (`HPDcePro/`):
- Create `HPDcePro/HPDcePro.slnx` using .NET 10 XML format.
- Create `HPDcePro/global.json` pinning SDK 10.0.300 (`rollForward: latestMinor`, runner `Microsoft.Testing.Platform`).
- Create `HPDcePro/Directory.Build.props` with `LangVersion=latest`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `NoWarn=NU1701;CS1591`.

### 2. `HPDcePro.Core` (`src/HPDcePro.Core/HPDcePro.Core.csproj`):
- Target frameworks: `netstandard2.0;net8.0`.
- Dependencies: `Microsoft.Data.Sqlite` 10.0.12, `System.Text.Json` (for netstandard2.0). 0 host references (no Revit, no AutoCAD, no Excel COM).
- Modules:
  1. **Geometry (`HPDcePro.Core.Geometry`)**:
     - `Point2D.cs`, `Vector2D.cs`, `Segment2D.cs`, `Polygon2D.cs`
     - `PolygonOperations.cs` (Shoelace area, centroid, ray-casting point-in-polygon, winding number)
     - `LineIntersection.cs` (segment-segment, infinite line-line intersection)
     - `ConvexHull2D.cs` (Monotone chain or Graham scan 2D convex hull)
     - `SpatialGrid2D.cs` (spatial indexing for fast geometric point/segment lookups)
     - Full replacement for all 69 native C++ exports of `DceAnalyssiss64.dll` as documented in `docs/rea-dce-pro/01-domain-architecture.md`.
  2. **Serialization (`HPDcePro.Core.Serialization`)**:
     - `AutoStrConfig.cs`, `AutoStrFloorInfo.cs`, `PointSpring.cs`, `ComboSettings.cs`, `SlabRebarConfig.cs`
     - `AutoStrProjectSerializer.cs` with UTF-8 BOM (`EF BB BF`) trimming and resilient JSON deserialization compatible with `C:\Dce Pro\AutoStr\DuAnMau.autostr`.
  3. **CAD XData (`HPDcePro.Core.XData`)**:
     - `XDataPacket.cs`, `DxfCodeConstants.cs`, `IXDataCodec<T>.cs`
     - `RebarXDataCodec.cs` (decodes/encodes 12-element `RebarXdataDCE` array: element name, bar mark, diameter, quantity, bar type, hooks A..D, length L, shape ID)
     - `ScheduleXDataCodec.cs` (decodes/encodes canonical `DceRebarXDataTKT` string: `SL=..;DK=..;KC1=..;KC2=..;KC3=..;KC4=..;SH=..;L=..`)
     - `LinksXDataCodec.cs` (`Links_XDataObject` dynamic field handle linking)
     - `XDataRegistry.cs` (catalogue of 13 RegApps with malformed packet recovery)
  4. **Machine Learning (`HPDcePro.Core.MachineLearning`)**:
     - `RebarToken.cs`, `RebarTokenType.cs` (`SH`, `SL`, `DK`, `KC`, `CD`)
     - `RebarTextTokenizer.cs` (pure C# tokenizer for Vietnamese rebar callout strings, e.g., `2\u220518`, `3d20`, `\u22058a150`, extracting bar quantity, diameter, spacing, shape)
     - `MauHocDatabaseReader.cs` (SQLite query service for `C:\Dce Pro\MayHoc\mauhoc.db` reading `MauHoc` 45,020 samples, `LichSuSua`, `CauHinhModel`)
  5. **DRM Exclusion**:
     - 0 references to legacy licensing, DRM, hardware locking, or telemetry (`LicenseMoi`, `LicenseCheckAPI`, `DceSerinumber`, `DbimTaiKhoanApi`, `DceUpdate`, `DceGuard`).

### 3. Clean Code Standards:
- File length <= 300 lines (Rule C2).
- Method length <= 50 lines (Rule M3).
- 0 commented-out code (Rule Q-B1).
- 0 unannotated empty catch blocks (Rule Q-B2).
- Run `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` to verify 0 errors and 0 warnings.

### Mandatory Integrity Warning:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2\handoff.md`.
