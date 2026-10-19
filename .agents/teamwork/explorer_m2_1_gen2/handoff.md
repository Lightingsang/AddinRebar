# Handoff Report: Nha Pho 5x18m Benchmark Survey & Specification

**Agent**: `explorer_m2_1_gen2`  
**Milestone**: M2 (Benchmark Survey & Core Tests)  
**Target Solution**: `HPBim/HPBim.slnx` (`HPBim.Core` / `HPBim.Core.Tests`)  
**Date**: 2026-10-06  

---

## 1. Observation

### 1.1 Source Benchmark Dataset
The ground truth benchmark for the Vietnamese modern townhouse project `nha_pho_5x18m` was located in `vn-autocad-skill-main/examples/nha_pho_5x18m/`:
- `layout.py`: Geometric definitions of grids, levels, columns, walls with openings, rooms, furniture/fixtures, doors schedule.
- `draw_structure.py`: Pile caps, tie beams, beam-slab structural plan (D1, D2, D3, S1..S4, stair opening), typical floor rebar schedule `REBAR_ROWS`.
- `draw_stair_structure.py`: Structural stair layout, rebar schedule `REBAR_ROWS_THANG` (flights, landing, DCT/DCN beams).
- `foundation_rebar.py`: Foundation rebar schedule `REBAR_ROWS_MONG` (pile caps M1/M2, tie beams GM1..GM7).
- `rebar_quantities.py`: Aggregation formulas for rebar tonnage across 3 typical floors (`SO_TANG_DIEN_HINH = 3`) and 3 stair flights (`SO_DOAN_THANG = 3`).
- `build_dutoan.py`: Bill of quantities (BOQ) with references `NGUON_MONG` and `NGUON`.
- `draw_plumbing.py`: Water supply, drainage pipes, risers, and sanitary fixtures across 3 floors.

### 1.2 Verbatim Extracted Data Points

#### Levels (5 Levels) — `layout.py:15-16`
```python
LEVELS = [("TRET", 0.150), ("LAU 1", 3.800), ("LAU 2", 7.200),
          ("SAN THUONG", 10.600), ("MAI CHE THANG", 13.400)]
```
1. `TRET` (Floor 1 / Ground): Elevation `+0.150m` (150 mm), Storey Height: 3650 mm, FloorNumber: 1
2. `LAU 1` (Floor 2): Elevation `+3.800m` (3800 mm), Storey Height: 3400 mm, FloorNumber: 2
3. `LAU 2` (Floor 3): Elevation `+7.200m` (7200 mm), Storey Height: 3400 mm, FloorNumber: 3
4. `SAN THUONG` (Roof terrace): Elevation `+10.600m` (10600 mm), Storey Height: 2800 mm, FloorNumber: 4
5. `MAI CHE THANG` (Stair roof): Elevation `+13.400m` (13400 mm), Storey Height: 0 mm, FloorNumber: 5

#### Grids (7 Grids) — `layout.py:9-10`
```python
AXIS_X = [("1", 0.0), ("2", 5500.0), ("3", 9500.0), ("4", 13000.0), ("5", 18000.0)]
AXIS_Y = [("A", 0.0), ("B", 5000.0)]
```
- Transverse (X-Direction, extends along Y from -1500 to 6500 mm):
  - Grid `1`: X = 0.0 mm
  - Grid `2`: X = 5500.0 mm
  - Grid `3`: X = 9500.0 mm
  - Grid `4`: X = 13000.0 mm
  - Grid `5`: X = 18000.0 mm
- Longitudinal (Y-Direction, extends along X from -1500 to 19500 mm):
  - Grid `A`: Y = 0.0 mm
  - Grid `B`: Y = 5000.0 mm
Total: 5 (X) + 2 (Y) = 7 Grids.

#### Columns (10 Columns) — `layout.py:20-21`
```python
COLUMNS = [(x, y, (220, 220)) for _, x in AXIS_X for _, y in AXIS_Y]
```
10 columns located at all grid intersections `(X, Y)`:
- (0, 0), (0, 5000)
- (5500, 0), (5500, 5000)
- (9500, 0), (9500, 5000)
- (13000, 0), (13000, 5000)
- (18000, 0), (18000, 5000)
Section: 220 x 220 mm. Material: Concrete B20 (M250).

#### Walls (26 Walls) & Openings (23 Openings) — `layout.py:25-58`
- **Floor 1 (8 walls)**:
  1. `(0, 0) -> (18000, 0)`, t=220mm, Window `(15600, w=1600, h=1400, sill=900, "S1")`
  2. `(0, 5000) -> (18000, 5000)`, t=220mm, Window `(6200, w=1800, h=1400, sill=900, "S1")`
  3. `(0, 0) -> (0, 5000)`, t=220mm, Garage Door Opening `(700, w=3600, h=2600, sill=0, "D1")`
  4. `(18000, 0) -> (18000, 5000)`, t=220mm, Backyard Door `(1800, w=900, h=2200, sill=0, "D5")`
  5. `(5500, 0) -> (5500, 5000)`, t=110mm, Cased Opening `(1400, w=2200, h=2400, sill=0)`
  6. `(9500, 0) -> (9500, 1900)`, t=110mm, WC1 Door `(500, w=800, h=2100, sill=0, "D4")`
  7. `(9500, 1900) -> (11400, 1900)`, t=110mm, No openings
  8. `(13000, 0) -> (13000, 5000)`, t=110mm, Cased Opening `(2000, w=2600, h=2400, sill=0)`
- **Floor 2 (9 walls)**:
  9. `(0, 0) -> (18000, 0)`, t=220mm, Window `(4200, w=1600, h=1400, sill=900, "S1")`
  10. `(0, 5000) -> (18000, 5000)`, t=220mm, Window `(4200, w=1600, h=1400, sill=900, "S1")`
  11. `(0, 0) -> (0, 5000)`, t=220mm, Balcony Glass `(1200, w=2600, h=2300, sill=0, "S3")`
  12. `(18000, 0) -> (18000, 5000)`, t=220mm, Window `(1500, w=1600, h=1400, sill=900, "S1")`
  13. `(1500, 0) -> (1500, 5000)`, t=220mm, Balcony Access Door `(2100, w=900, h=2200, sill=0, "D6")`
  14. `(7800, 0) -> (7800, 5000)`, t=110mm, Master Bed 1 Door `(2000, w=900, h=2200, sill=0, "D2")`
  15. `(13000, 0) -> (13000, 5000)`, t=110mm, Master Bed 2 Door `(2000, w=900, h=2200, sill=0, "D2")`
  16. `(10500, 0) -> (10500, 2400)`, t=110mm, WC2 Door `(900, w=800, h=2100, sill=0, "D4")`
  17. `(10500, 2400) -> (13000, 2400)`, t=110mm, No openings
- **Floor 3 (9 walls)**:
  18. `(0, 0) -> (18000, 0)`, t=220mm, Window `(4200, w=1600, h=1400, sill=900, "S1")`
  19. `(0, 5000) -> (18000, 5000)`, t=220mm, Window `(4200, w=1600, h=1400, sill=900, "S1")`
  20. `(0, 0) -> (0, 5000)`, t=220mm, Balcony Glass `(1200, w=2600, h=2300, sill=0, "S3")`
  21. `(18000, 0) -> (18000, 5000)`, t=220mm, Window `(1600, w=1400, h=1400, sill=900, "S1")`
  22. `(1500, 0) -> (1500, 5000)`, t=220mm, Balcony Access Door `(2100, w=900, h=2200, sill=0, "D6")`
  23. `(7800, 0) -> (7800, 5000)`, t=110mm, Altar Room Double Door `(1900, w=1600, h=2200, sill=0, "D3")`
  24. `(13000, 0) -> (13000, 5000)`, t=110mm, Bed 3 Door `(2000, w=900, h=2200, sill=0, "D2")`
  25. `(15600, 0) -> (15600, 2200)`, t=110mm, WC3 Door `(700, w=800, h=2100, sill=0, "D4")`
  26. `(15600, 2200) -> (18000, 2200)`, t=110mm, No openings
Total: 8 + 9 + 9 = 26 walls.

#### Rooms (16 Rooms) — `layout.py:64-82`
- **Floor 1 (5 rooms)**:
  1. Gara o to: (2750, 2500), 26.4 m²
  2. Phong khach: (7500, 3300), 19.2 m²
  3. Gieng troi + cau thang: (11250, 3600), 13.5 m² (nominal)
  4. WC 1: (10600, 1700), 3.0 m²
  5. Bep + phong an: (15500, 2500), 24.0 m²
- **Floor 2 (5 rooms)**:
  6. Ban cong: (750, 2500), 3.6 m², rotation 90°
  7. Phong ngu master 1: (4650, 2500), 30.5 m²
  8. Sanh + thong tang: (9150, 3600), 14.5 m² (nominal)
  9. WC 2: (11750, 2100), 5.6 m²
  10. Phong ngu master 2: (15500, 3500), 24.0 m²
- **Floor 3 (6 rooms)**:
  11. Ban cong: (750, 2500), 3.6 m², rotation 90°
  12. Phong tho gia tien: (4650, 2500), 30.5 m²
  13. Sanh + thong tang: (9150, 3600), 14.5 m² (nominal)
  14. Phong ngu master 3: (14400, 3500), 17.0 m²
  15. WC 3: (16800, 1900), 5.3 m²
  16. San phoi: (16850, 4100), 5.8 m² (nominal)
Total: 5 + 5 + 6 = 16 rooms.

#### Beams & Slabs — `draw_structure.py:75-96`
- **Beams per typical floor (8 beams/floor)**:
  - 5 transverse beams D1 (200x400): at X = 0, 5500, 9500, 13000, 18000 from Y=0 to Y=5000.
  - 2 longitudinal continuous beams D2 (200x300): at Y = 0, 5000 from X=0 to X=18000.
  - 1 secondary beam D3 (200x300): at Y = 2500 from X=0 to X=5500.
  Total across 3 suspended floors (LAU 1, LAU 2, SAN THUONG) = 24 beams.
  (Plus 7 foundation tie beams GM1..GM7, 250x400).
- **Slabs per typical floor (4 slabs/floor, thickness 100 mm)**:
  - S1: `(0, 0) -> (5500, 0) -> (5500, 2500) -> (0, 2500)` (13.75 m²)
  - S2: `(0, 2500) -> (5500, 2500) -> (5500, 5000) -> (0, 5000)` (13.75 m²)
  - S3: `(5500, 0) -> (9500, 0) -> (9500, 5000) -> (5500, 5000)` (20.0 m²)
  - S4: `(13000, 0) -> (18000, 0) -> (18000, 5000) -> (13000, 5000)` (25.0 m²)
  - Stair Opening (Void): `(9500, 0) -> (13000, 0) -> (13000, 5000) -> (9500, 5000)` (17.5 m²)
  Total across 3 suspended floors = 12 slab panels (217.5 m² gross slab area).

#### MEP & Furniture Fixtures — `layout.py:85-99` & `draw_plumbing.py`
26 fixtures total (11 sanitary/plumbing fixtures, 3 kitchen, 12 furniture/electrical):
- Sanitary: BON-CAU (3), LAVABO (3), VOI-SEN (2), PHEU-THU (2), CHAU-RUA-CHEN (1) = 11 fixtures.
- Water supply pipes: PPR D25 main riser, PPR D20 floor branches.
- Soil & waste pipes: PVC D114 (blackwater to septic tank), PVC D60 (greywater).

#### Rebar Quantities & Weight Verification (7966.5 kg)
Executing the exact formula $W = \frac{D^2}{162.2} \times L \times Q$ yields:
- **Foundation (`REBAR_ROWS_MONG`)**:
  - Pile caps M1/M2 (40 bars D16x1.62m + 80 bars D16x0.82m): 205.81 kg
  - Tie beams GM1, GM2 (8 bars D16x19.6m + 162 stirrups D8x1.06m): 315.24 kg
  - Tie beams GM3-GM7 (20 bars D16x5.96m + 95 stirrups D8x1.06m): 227.86 kg
  - **Subtotal Foundation**: **748.91 kg**
- **Superstructure (3 Typical Floors $\times$ `REBAR_ROWS`)**:
  - D1 beams (top, bot, stirrups): 1125.42 kg
  - D2 beams (top, bot, stirrups): 1259.11 kg
  - D3 beams (main, stirrups): 148.10 kg
  - S1..S4 slabs (bottom X/Y + top perimeter caps): 3161.42 kg
  - C1 columns (main D18 + stirrups D8): 1152.40 kg
  - **Subtotal 3 Floors**: **6846.45 kg**
- **Staircase (3 Flights $\times$ `REBAR_ROWS_THANG`)**:
  - Flight slabs BT1, BT2, CN: 219.06 kg
  - Beams DCT, DCN (main D16 + stirrups D8): 152.06 kg
  - **Subtotal 3 Flights**: **371.12 kg**
- **Grand Total Rebar**:
  $$W_{\text{total}} = 748.91 + 6846.45 + 371.12 = \mathbf{7966.47\text{ kg}} \longrightarrow \mathbf{7966.5\text{ kg}}$$
  - Small diameter ($D \le 10\text{ mm}$): $4120.88\text{ kg}$ (than nha) $+ 107.49\text{ kg}$ (mong) $= 4228.37\text{ kg}$
  - Large diameter ($D > 10\text{ mm}$): $3096.68\text{ kg}$ (than nha) $+ 641.42\text{ kg}$ (mong) $= 3738.10\text{ kg}$
  - Exactly matching `build_dutoan.py`: `THEP_NHO = 4120.9 kg`, `THEP_LON = 3096.7 kg`, `THEP_MONG = 748.9 kg`.

---

## 2. Logic Chain

1. **Step 1: Ground Truth Identification**  
   From Section 1.1, `vn-autocad-skill-main/examples/nha_pho_5x18m` contains the complete architectural, structural, MEP, and quantity dataset.
2. **Step 2: 1:1 Mapping to Domain DTOs (`HPBim.Core.Models`)**  
   `HPBim.Core` provides:
   - `LevelDef`: Name, ElevationM, HeightMm, FloorNumber.
   - `AxisGrid`: Name, CoordinateMm, Direction, StartPoint, EndPoint.
   - `ColumnDef`: CenterLocation, WidthMm (220), DepthMm (220), BaseLevel, TopLevel.
   - `WallDef`: StartPoint, EndPoint, ThicknessMm (110 or 220), BaseLevel, TopLevel, Openings.
   - `OpeningDef`: DistanceAlongWallMm, WidthMm, HeightMm, SillHeightMm, Kind (`Door`, `Door2`, `Window`, `Opening`).
   - `RoomDef`: Name, TagLocation, TheoreticalAreaM2, Level, TagRotationDeg.
   - `BeamDef`: StartPoint, EndPoint, WidthMm, HeightMm, Level, BeamType.
   - `SlabDef`: Boundary, ThicknessMm (100), Level, Voids.
   - `MepFixtureDef` & `PipeRunDef`: Location, Category, SystemType, DiameterMm.
   - `RebarDef`: BarMark, DiameterMm, LengthMm, Quantity, SteelGrade, ShapeType.
   All extracted data can be mapped directly with 0 loss of information.
3. **Step 3: Verification of Clean Code Constraints**  
   Per `HP_CLEAN_CODE_CORE.md`, a single class cannot exceed 300 lines, and methods cannot exceed 50 lines. Populating 26 walls with 23 openings and 33 rebar specifications in a single file would exceed 400 lines. Therefore, `NhaPho5x18mFixture` is designed as a modular partial class across 4 files (Core/Setup, Architecture, Structure, MEP) in `HPBim.Core.Tests/Fixtures/`.
4. **Step 4: Verification of Pre-flight QC Compatibility**  
   Examining `QcEngine`, `QcClashDetector`, and `ClearanceChecker`:
   - All wall openings are offset $\ge 500\text{ mm}$ from column intersections, avoiding `ColumnVsOpening` clashes.
   - Secondary beam D3 aligns with the boundary between S1 and S2, preventing `SecondaryBeamSlicesUndividedSlab` clashes.
   - Door swings clear room tag locations (specifically WC1 tag at `(10600, 1700)`).
   - Rebar concrete covers (15mm slab, 25mm beam/column, 50mm footing) all satisfy TCVN $\ge 15\text{ mm}$.
   Therefore, `QcEngine.Validate(model)` will return `IsValid == true` with 0 errors.

---

## 3. Caveats

1. **Nominal vs. Exact Room Boundaries**:
   In `layout.py`, hallways (`Sanh + thong tang`, `Gieng troi`, `San phoi`) have `None` for theoretical area. In `RoomDef`, `TheoreticalAreaM2` is nullable (`double?`), which accommodates this cleanly.
2. **Furniture vs. Sanitary Categorization**:
   `layout.py` defines 26 total fixtures including vehicles, sofas, and dining tables. In `NhaPho5x18mFixture`, we include all 26 fixtures using appropriate `MepCategory` (`Sanitary`, `Kitchen`, `Furniture`, `Electrical`).
3. **Model Space vs. World Coordinates**:
   In `layout.py`, $X$ represents the depth of the house (0 to 18000 mm), and $Y$ represents the frontage width (0 to 5000 mm). In Revit, standard orientation maps $X$ to length and $Y$ to width, matching the benchmark model space exactly.

---

## 4. Conclusion & Implementation Specification

### 4.1 Architecture of `NhaPho5x18mFixture`
Located in `HPBim/tests/HPBim.Core.Tests/Fixtures/`:
```
Fixtures/
├── NhaPho5x18mFixture.cs              # Entry point: CreateModel(), Levels, Grids, Columns (~85 lines)
├── NhaPho5x18mFixture.Architecture.cs # Walls, Openings, Rooms (~140 lines)
├── NhaPho5x18mFixture.Structure.cs    # Beams, Slabs, 33 Rebar rows (~160 lines)
└── NhaPho5x18mFixture.Mep.cs          # Sanitary/Furniture fixtures, Pipe runs (~95 lines)
```

#### File 1: `NhaPho5x18mFixture.cs` (Core & Grid/Columns)
```csharp
namespace HPBim.Core.Tests.Fixtures;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public static partial class NhaPho5x18mFixture
{
    public const string ProjectName = "Nha Pho 5x18m (3 Tang)";

    public static BimModel CreateModel()
    {
        return new BimModel
        {
            ProjectName = ProjectName,
            Levels = CreateLevels(),
            Grids = CreateGrids(),
            Columns = CreateColumns(),
            Walls = CreateWalls(),
            Rooms = CreateRooms(),
            Beams = CreateBeams(),
            Slabs = CreateSlabs(),
            Rebars = CreateRebars(),
            Fixtures = CreateFixtures(),
            Pipes = CreatePipes()
        };
    }

    public static IReadOnlyList<LevelDef> CreateLevels() =>
    [
        new("TRET", 0.150, 3650.0, 1, true),
        new("LAU 1", 3.800, 3400.0, 2, true),
        new("LAU 2", 7.200, 3400.0, 3, true),
        new("SAN THUONG", 10.600, 2800.0, 4, true),
        new("MAI CHE THANG", 13.400, 0.0, 5, false)
    ];

    public static IReadOnlyList<AxisGrid> CreateGrids()
    {
        double[] xCoords = [0.0, 5500.0, 9500.0, 13000.0, 18000.0];
        string[] xNames = ["1", "2", "3", "4", "5"];
        var grids = new List<AxisGrid>(7);

        for (int i = 0; i < xCoords.Length; i++)
        {
            grids.Add(AxisGrid.CreateOrthogonal(xNames[i], xCoords[i], GridDirection.X, 0.0, 5000.0));
        }

        grids.Add(AxisGrid.CreateOrthogonal("A", 0.0, GridDirection.Y, 0.0, 18000.0));
        grids.Add(AxisGrid.CreateOrthogonal("B", 5000.0, GridDirection.Y, 0.0, 18000.0));
        return grids;
    }

    public static IReadOnlyList<ColumnDef> CreateColumns()
    {
        double[] xCoords = [0.0, 5500.0, 9500.0, 13000.0, 18000.0];
        string[] xNames = ["1", "2", "3", "4", "5"];
        double[] yCoords = [0.0, 5000.0];
        string[] yNames = ["A", "B"];
        var cols = new List<ColumnDef>(10);

        for (int i = 0; i < xCoords.Length; i++)
        {
            for (int j = 0; j < yCoords.Length; j++)
            {
                cols.Add(new ColumnDef
                {
                    Id = $"C_{xNames[i]}_{yNames[j]}",
                    CenterLocation = new Point2D(xCoords[i], yCoords[j]),
                    WidthMm = 220.0,
                    DepthMm = 220.0,
                    BaseLevel = "TRET",
                    TopLevel = "SAN THUONG",
                    Material = "Concrete - B20"
                });
            }
        }
        return cols;
    }
}
```

#### File 2: `NhaPho5x18mFixture.Architecture.cs` (Walls & Rooms)
Contains `CreateWalls()` (returns 26 `WallDef`s with 23 `OpeningDef`s) and `CreateRooms()` (returns 16 `RoomDef`s).

#### File 3: `NhaPho5x18mFixture.Structure.cs` (Beams, Slabs & Rebar)
Contains `CreateBeams()` (returns 24 beams), `CreateSlabs()` (returns 12 slabs), and `CreateRebars()` (returns 33 rebar definitions totaling 7966.5 kg).

#### File 4: `NhaPho5x18mFixture.Mep.cs` (Fixtures & Pipes)
Contains `CreateFixtures()` (returns 26 fixtures) and `CreatePipes()` (returns water supply, soil, and waste runs).

### 4.2 Test Suite Specification: `NhaPho5x18mTests.cs`
Located in `HPBim/tests/HPBim.Core.Tests/NhaPho5x18mTests.cs`:
1. `Levels_ShouldMatchBenchmark_ExactElevationsAndCount`: Verifies 5 levels, elevations (+0.15m, +3.8m, +7.2m, +10.6m, +13.4m).
2. `Grids_ShouldMatchBenchmark_ExactCoordinatesAndCount`: Verifies 7 grids (5 X, 2 Y).
3. `Columns_ShouldMatchBenchmark_ExactGridIntersectionsAndDimensions`: Verifies 10 columns, all 220x220 mm.
4. `Walls_ShouldMatchBenchmark_ExactCountAndThicknesses`: Verifies 26 walls (8 T1, 9 T2, 9 T3).
5. `Openings_ShouldMatchBenchmark_ExactCountAndDimensions`: Verifies 23 openings.
6. `Rooms_ShouldMatchBenchmark_ExactCountAndAreas`: Verifies 16 rooms.
7. `BeamsAndSlabs_ShouldMatchBenchmark_StructuralGrid`: Verifies 8 beams/floor, 4 slabs/floor, stairwell void.
8. `MepFixtures_ShouldMatchBenchmark_CountsAndCategories`: Verifies fixtures and pipe networks.
9. `Rebar_ShouldMatchBenchmark_ExactTotalWeightAndCategories`:
   - `Assert.Equal(7966.5, Math.Round(model.TotalRebarWeightKg, 1));`
   - Foundation = 748.9 kg, Superstructure = 7217.6 kg.
10. `QcEngine_ShouldValidate_ModelHasZeroCriticalErrors`:
    - `var report = new QcEngine().Validate(model);`
    - `Assert.True(report.IsValid);`
    - `Assert.Empty(report.Errors);`
11. `BimModelSerializer_ShouldRoundTripModelPreservingExactWeight`: Verifies JSON serialization round-trip fidelity.

---

## 5. Verification Method

### 5.1 Verification Commands
Once the implementer creates the fixture and tests:
```bash
# Build HPBim.slnx
dotnet build HPBim/HPBim.slnx -c Debug

# Run the full unit test suite including the NhaPho5x18m benchmark tests
dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
```

### 5.2 Independent Invalidation Conditions
- Any discrepancy in total rebar weight deviating from $7966.5\text{ kg} \pm 0.1\text{ kg}$.
- Wall count $\ne 26$ or column count $\ne 10$ or grid count $\ne 7$ or level count $\ne 5$.
- `QcEngine.Validate()` failing or reporting critical clashes.
- Violating Clean Code rules ($> 300$ lines per file, $> 50$ lines per method).
