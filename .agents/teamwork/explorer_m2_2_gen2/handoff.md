# Milestone 2 Test Coverage & Architecture Handoff Report

**Agent**: `explorer_m2_2_gen2`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_2_gen2`  
**Parent (Orchestrator Gen 2)**: `18c3ad8c-1fc9-42cc-83e7-40ab9000587e`  
**Target Solution**: `HPBim/HPBim.slnx`  
**Target Projects**: `HPBim/src/HPBim.Core/` (`netstandard2.0`), `HPBim/tests/HPBim.Core.Tests/` (`net10.0`, xUnit v3 / MTP)

---

## 1. Observation

### 1.1 Existing Test Suite Status
Direct execution of `dotnet run --project HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj` on the dev environment yielded:
```text
xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET 10.0.11)
Test run summary: Passed! - HPBim.Core.Tests.dll (net10.0|x64)
  total: 87
  failed: 0
  succeeded: 87
  skipped: 0
  duration: 435ms
```
The existing 87 tests are distributed across 6 files in `HPBim/tests/HPBim.Core.Tests/`:
1. `AdvancedAdversarialAndTcvnStressTests.cs` (237 physical lines / 6 test methods: 12 diameter theories, 6 cover theories, 4 facts = 22 executions)
2. `CollinearWallTests.cs` (217 physical lines / 7 test methods: collinear segmenting, boundary conditions, edge clearance)
3. `JsonSerializationStressTests.cs` (271 physical lines / 5 test methods: full round-trip, camelCase enums, compact format, error handling)
4. `MultiLevelClashIsolationTests.cs` (278 physical lines / 6 test methods: 5-level clash isolation, level string matching, penetration depth, exact contact, beam vs slab, spatial grid)
5. `PolygonAndClippingStressTests.cs` (223 physical lines / 7 test methods: Liang-Barsky trivial accept/reject, piercing, polygon shoelace, centroid, ray casting, duplicate vertex pruning)
6. `QcOptionsBoundaryTests.cs` (154 physical lines / 8 test methods: struct constructor behavior, 6-check execution combinations, cover boundary)

### 1.2 Ground-Truth Benchmark Inspection (`vn-autocad-skill-main/examples/nha_pho_5x18m/`)
Direct empirical verification using `uv run` on the original Python benchmark source revealed the exact ground-truth parameters:
- **Levels (5)**:
  * Tầng 1 (Trệt): $+0.150$m (150mm), $H = 3650$mm, Floor 1
  * Tầng 2 (Lầu 1): $+3.800$m (3800mm), $H = 3400$mm, Floor 2
  * Tầng 3 (Lầu 2): $+7.200$m (7200mm), $H = 3400$mm, Floor 3
  * Sân Thượng: $+10.600$m (10600mm), $H = 2800$mm, Floor 4
  * Mái Che Thang: $+13.400$m (13400mm), $H = 3000$mm, Floor 5
- **Grids (7)**:
  * 5 Trục X: `1` ($0.0$mm), `2` ($5500.0$mm), `3` ($9500.0$mm), `4` ($13000.0$mm), `5` ($18000.0$mm)
  * 2 Trục Y: `A` ($0.0$mm), `B` ($5000.0$mm)
- **Columns (10)**:
  * $220 \times 220$mm reinforced concrete at every grid intersection $(X_i, Y_j)$
- **Walls (26 total)**:
  * Tầng 1: 8 tường (4 tường bao 220mm, 4 tường ngăn 110mm, 6 lỗ mở)
  * Tầng 2: 9 tường (5 tường bao 220mm, 4 tường ngăn 110mm, 9 lỗ mở)
  * Tầng 3: 9 tường (5 tường bao 220mm, 4 tường ngăn 110mm, 8 lỗ mở)
  * Tổng cộng: $8 + 9 + 9 = 26$ tường
- **Rooms (16 total)**:
  * Tầng 1 (5 phòng): Gara ô tô ($26.4\text{ m}^2$), Phòng khách ($19.2\text{ m}^2$), Giếng trời + cầu thang, WC 1 ($3.0\text{ m}^2$), Bếp + phòng ăn ($24.0\text{ m}^2$)
  * Tầng 2 (5 phòng): Ban công ($3.6\text{ m}^2$), Phòng ngủ master 1 ($30.5\text{ m}^2$), Sảnh + thông tầng, WC 2 ($5.6\text{ m}^2$), Phòng ngủ master 2 ($24.0\text{ m}^2$)
  * Tầng 3 (6 phòng): Ban công ($3.6\text{ m}^2$), Phòng thờ gia tiên ($30.5\text{ m}^2$), Sảnh + thông tầng, Phòng ngủ master 3 ($17.0\text{ m}^2$), WC 3 ($5.3\text{ m}^2$), Sân phơi
- **Beams & Slabs**:
  * Dầm chính: D1 $200 \times 400$ (5 dầm theo phương Y), D2 $200 \times 300$ (2 dầm theo phương X), D3 $200 \times 300$ (dầm phụ chia ô sàn)
  * Giằng móng: GM1..GM7 $250 \times 400$
  * Ô sàn: S1 ($5.5 \times 2.5$m, $100$mm), S2 ($5.5 \times 2.5$m, $100$mm), S3 ($4.0 \times 5.0$m, $100$mm), S4 ($5.0 \times 5.0$m, $100$mm), Ô thông tầng ($3.5 \times 5.0$m)
- **Rebar Weights & Schedule (Ground Truth)**:
  ```text
  Thân nhỏ (<= 10mm): 4120.88 kg
  Thân lớn (> 10mm) : 3096.68 kg
  Thép móng (KC-01) :  748.91 kg
  --------------------------------
  TỔNG CỘNG         : 7966.47 kg (chính xác 7966.5 kg)
  ```
  Total schedule rows: 33 items (8 dầm + 9 sàn + 2 cột = 19 rows thân $\times 3$ tầng = 57 rows; 8 thang $\times 3$ đợt = 24 rows; 6 móng = 6 rows $\rightarrow$ 87 rebar definitions).

### 1.3 Clean Code Metrics (`HP_CLEAN_CODE_CORE.md`)
- Limit: File/Class length $\le 300$ lines, Method length $\le 50$ lines.
- Current status in `src/HPBim.Core/`: All 31 files are strictly $\le 173$ lines.
- Current status in `tests/HPBim.Core.Tests/`:
  * `MultiLevelClashIsolationTests.cs`: 278 lines (approaching limit)
  * `JsonSerializationStressTests.cs`: 271 lines
- **Critical Risk**: Porting `NhaPho5x18mFixture` as a single monolithic class will result in $600\text{--}800$ lines, immediately violating clean code governance.

---

## 2. Logic Chain

1. **Premise 1**: Milestone 2 requires full verification of the benchmark model `NhaPho5x18mFixture`, core geometry calculations (`GeometryTests.cs`), TCVN 5574:2018 detailing rules (`TcvnRulesTests.cs`), and offline QC engine orchestration (`QcEngineTests.cs`).
2. **Premise 2**: The existing 87 tests in M1 were written under adversarial review to validate M1 features and fix the `QcOptions` parameterless struct default zeroing defect. They test specific low-level boundary conditions, but do NOT provide:
   - The official ground-truth fixture `NhaPho5x18mFixture.cs` (0% implemented).
   - Dedicated benchmark model validation tests in `NhaPho5x18mTests.cs` (0% implemented).
   - Complete TCVN detailing algorithm coverage: anchorage, hook 90°, tension/compression lap splices, commercial bar splices ($> 11.7$m), beam stirrup zoning, column stirrup zoning in `TcvnRulesTests.cs` (0% implemented).
   - Unified end-to-end QC engine pipeline validation in `QcEngineTests.cs` (0% implemented).
   - Standard geometry contract test suite in `GeometryTests.cs` (0% implemented).
3. **Premise 3**: Clean Code Governance mandates class length $\le 300$ lines. Writing a 5-story townhouse fixture with 5 levels, 7 grids, 10 columns, 26 walls, 23 openings, 16 rooms, beams, slabs, MEP, and 87 rebar rows in one file will violate the 300-line ceiling.
4. **Deduction 1**: `NhaPho5x18mFixture` MUST be decomposed into a modular architecture using C# `partial class` or modular element builders:
   - `Fixtures/NhaPho5x18mFixture.cs` (Entry point `CreateBenchmarkModel()`, expected summary constants) $\le 120$ lines.
   - `Fixtures/NhaPho5x18mFixture.Architecture.cs` (Levels, Grids, Columns, Walls, Openings, Rooms) $\le 250$ lines.
   - `Fixtures/NhaPho5x18mFixture.Structure.cs` (Beams, Slabs, Rebar rows with exact 7966.5 kg total) $\le 250$ lines.
   - `Fixtures/NhaPho5x18mFixture.Mep.cs` (Fixtures, Pipes) $\le 120$ lines.
5. **Deduction 2**: Four clean, focused test classes must be created, each strictly $\le 250$ lines and all methods $\le 50$ lines:
   - `NhaPho5x18mTests.cs` ($\approx 220$ lines, 8 tests)
   - `GeometryTests.cs` ($\approx 230$ lines, 8 tests / 24 theory runs)
   - `TcvnRulesTests.cs` ($\approx 220$ lines, 8 tests / 25 theory runs)
   - `QcEngineTests.cs` ($\approx 210$ lines, 8 tests)
6. **Deduction 3**: Adding these 32 tests (yielding $\approx 70\text{--}80$ test executions) to the existing 87 tests will expand the test suite to $\approx 160$ tests, completing Milestone 2 with 100% pass and 0 clean code violations.

---

## 3. Caveats

- **Existing Tests Coexistence**: The 6 existing test files from M1 (`PolygonAndClippingStressTests.cs`, `AdvancedAdversarialAndTcvnStressTests.cs`, etc.) should NOT be deleted. They provide adversarial regression safety. The 4 new test files specified in PROJECT.md (`NhaPho5x18mTests.cs`, `GeometryTests.cs`, `TcvnRulesTests.cs`, `QcEngineTests.cs`) add the formal domain verification required by M2.
- **Floating-Point Tolerances**:
  * Total rebar weight: Expected is $7966.5$ kg. Due to decimal rounding in schedule rows, assertions should use an absolute tolerance of $\pm 0.5$ kg.
  * Room area calculations: Wall thicknesses (110mm/220mm) slightly shift room inner boundaries compared to centerline theoretical areas; assertions should use a 5% relative tolerance or $\pm 0.5\text{ m}^2$ as specified in `RoomAreaCalculator`.
- **Level Name Matching**: `QcClashDetector` and `ClearanceChecker` match levels using case-insensitive strings (`BaseLevel`). The fixture must ensure element level references (`"Level 1"`, `"Level 2"`, etc.) match consistently.

---

## 4. Conclusion & Actionable Recommendations for Worker

The Worker agent should execute Milestone 2 following this exact plan:

### 4.1 Step 1 — Create `NhaPho5x18mFixture` in `HPBim/tests/HPBim.Core.Tests/Fixtures/`
Decompose the benchmark model into 4 partial class files under namespace `HPBim.Core.Tests.Fixtures`:

1. **`NhaPho5x18mFixture.cs`** ($\le 120$ lines):
   - Public constants:
     * `ExpectedLevelCount = 5`
     * `ExpectedGridCount = 7`
     * `ExpectedColumnCount = 10`
     * `ExpectedWallCount = 26`
     * `ExpectedRoomCount = 16`
     * `ExpectedFixtureCount = 26`
     * `ExpectedTotalRebarWeightKg = 7966.5`
     * `ExpectedSmallRebarWeightKg = 4120.9` ($\le 10$mm)
     * `ExpectedLargeRebarWeightKg = 3096.7` ($> 10$mm)
     * `ExpectedFoundationRebarWeightKg = 748.9`
   - Factory method `CreateBenchmarkModel()`:
     ```csharp
     return new BimModel
     {
         ProjectName = "NhaPho_5x18m_Benchmark",
         Levels = CreateLevels(),
         Grids = CreateGrids(),
         Columns = CreateColumns(),
         Walls = CreateWalls(),
         Beams = CreateBeams(),
         Slabs = CreateSlabs(),
         Rooms = CreateRooms(),
         Fixtures = CreateFixtures(),
         Pipes = CreatePipes(),
         Rebars = CreateRebars()
     };
     ```
2. **`NhaPho5x18mFixture.Architecture.cs`** ($\le 250$ lines):
   - `CreateLevels()`: 5 levels with elevations $0.150$m, $3.800$m, $7.200$m, $10.600$m, $13.400$m.
   - `CreateGrids()`: 5 X-grids ($0, 5500, 9500, 13000, 18000$) and 2 Y-grids ($0, 5000$).
   - `CreateColumns()`: 10 columns $220 \times 220$ at grid intersections.
   - `CreateWalls()`: 26 walls (Level 1: 8 walls, Level 2: 9 walls, Level 3: 9 walls) with attached openings.
   - `CreateRooms()`: 16 rooms with `TagLocation`, `TheoreticalAreaM2`, and polygonal boundaries.
3. **`NhaPho5x18mFixture.Structure.cs`** ($\le 250$ lines):
   - `CreateBeams()`: Main beams D1 $200 \times 400$, D2 $200 \times 300$, D3 $200 \times 300$, and tie beams GM1..GM7 $250 \times 400$.
   - `CreateSlabs()`: Slabs S1..S4 ($100$mm) and stair void.
   - `CreateRebars()`: Exactly 87 rebar entries based on the 33 ground-truth schedule rows:
     * Dầm D1, D2, D3: 8 entries $\times 3$ floors
     * Sàn S1..S4: 9 entries $\times 3$ floors
     * Cột C1: 2 entries $\times 3$ floors
     * Thang: 8 entries $\times 3$ flights
     * Móng: 6 entries $\times 1$ foundation
4. **`NhaPho5x18mFixture.Mep.cs`** ($\le 120$ lines):
   - `CreateFixtures()`: 26 fixtures across 3 floors (sanitary, kitchen, furniture).
   - `CreatePipes()`: 3 pipe runs (SoilDrain D110, WasteDrain D90, WaterSupply D25).

### 4.2 Step 2 — Implement `NhaPho5x18mTests.cs` ($\le 250$ lines)
Location: `HPBim/tests/HPBim.Core.Tests/NhaPho5x18mTests.cs`
Covering 8 essential tests:
1. `Benchmark_ElementCounts_MatchGroundTruth()`: Verify counts for levels, grids, columns, walls, rooms, fixtures, rebars.
2. `Benchmark_GridAndColumnCoordinates_AlignAtIntersections()`: Verify 10 columns intersect X/Y grids.
3. `Benchmark_RebarTotalWeight_Matches7966_5Kg()`: `Assert.Equal(7966.5, model.TotalRebarWeightKg, tolerance: 0.5);`.
4. `Benchmark_RebarPartition_MatchesSmallAndLargeWeights()`: Partition by $10$mm $\rightarrow$ Small $4120.9$ kg, Large $3096.7$ kg.
5. `Benchmark_WallSegmentation_All26WallsSegmentProperly()`: Ensure `WallSegmenter.Segment()` succeeds on all 26 walls with openings.
6. `Benchmark_RoomAreas_MatchTheoreticalValuesWithinTolerance()`: Verify `RoomAreaCalculator.ValidateRoom()` passes for all rooms with boundaries.
7. `Benchmark_OfflineQc_ProducesZeroErrorsAndZeroClashes()`: `var report = new QcEngine().Validate(model); Assert.True(report.IsValid); Assert.Empty(report.Errors); Assert.Empty(report.Clashes);`.
8. `Benchmark_JsonSerialization_RoundTripPreservesAllElements()`: Round-trip via `BimModelSerializer` preserves counts and total rebar weight.

### 4.3 Step 3 — Implement `GeometryTests.cs` ($\le 250$ lines)
Location: `HPBim/tests/HPBim.Core.Tests/GeometryTests.cs`
Covering 8 contract test methods:
1. `Shoelace_VariousPolygons_CalculatesExactArea()`: Theory for Rectangle, Right Triangle, Trapezoid, Hexagon.
2. `Polygon2D_SignedAreaAndOrientation()`: CCW (positive, IsCCW=true), CW (negative, IsCW=true).
3. `Polygon2D_Centroid_CalculatesAccurately()`: Symmetric and triangle centroids.
4. `Polygon2D_Containment_RayCasting()`: Inside, outside, boundary vertex/edge, concave notch.
5. `Polygon2D_Perimeter_CalculatesAccurately()`: Perimeter match.
6. `LiangBarsky_Clipping_AllScenarios()`: Inside, outside (4 cardinal + diagonals), piercing, edge collinear.
7. `Vector2D_And_Point2D_Operations()`: Dot product, Left/Right normals orthogonality, distance, angle.
8. `BoundingBox2D_IntersectionsAndUnions()`: Overlap, disjoint, inflate/deflate.

### 4.4 Step 4 — Implement `TcvnRulesTests.cs` ($\le 250$ lines)
Location: `HPBim/tests/HPBim.Core.Tests/TcvnRulesTests.cs`
Covering 8 contract test methods:
1. `Tcvn_UnitWeightFormula_StandardDiameters()`: $W = D^2 / 162.2$ for all standard diameters (6..32mm).
2. `Tcvn_TotalWeightCalculation_MatchesMath()`: Weight from mm length and quantity.
3. `Tcvn_WeightPartitioning_SeparatesAt10Mm()`: Rebar list split into small ($\le 10$mm) and large ($> 10$mm).
4. `Tcvn_ConcreteCoverRules_FollowsTable19()`: Minimum cover compliance per `StructuralElementKind`.
5. `Tcvn_DetailingMultipliers_AnchorageLapAndHook()`: Anchorage $30D$, 90° hook $12D$, tension lap $40D$, compression lap $30D$, stirrup hook $\max(10D, 75$mm$)$.
6. `Tcvn_CommercialBarSplices_HandlesLengthsBeyond11_7M()`: Commercial length cutoff $\le 11.7$m (0 splices), $18$m (1 splice), $24$m (2 splices).
7. `Tcvn_BeamStirrupZones_CalculatesDenseAndSparseDistributions()`: Dense zone $\min(2h, L/3)$, spacing $100/200$mm.
8. `Tcvn_ColumnStirrupZones_CalculatesBottomMiddleTopDistributions()`: Dense zone $H/4$ top and bottom, sparse middle.

### 4.5 Step 5 — Implement `QcEngineTests.cs` ($\le 250$ lines)
Location: `HPBim/tests/HPBim.Core.Tests/QcEngineTests.cs`
Covering 8 contract test methods:
1. `QcEngine_Validate_CleanModel_PassesWithZeroErrorsAndClashes()`: Valid model $\rightarrow$ `IsValid = true`.
2. `QcEngine_Validate_CoverViolations_FailsWithErrors()`: Cover $< 15$mm $\rightarrow$ `IsValid = false`, `TCVN-COVER-001`.
3. `QcEngine_Validate_ColumnOpeningClash_FailsWithClashes()`: Opening overlapping column $\rightarrow$ `IsValid = false`, `ColumnVsOpening`.
4. `QcEngine_Validate_BeamSlabClash_FlagsSecondaryBeamSlicingSlab()`: Secondary beam through undivided slab $\rightarrow$ clash; tie beam $\rightarrow$ no clash.
5. `QcEngine_Validate_ClearanceViolations_GeneratesWarnings()`: Door near column ($< 100$mm), door swing hitting room tag or fixture, non-standard wall thickness $\rightarrow$ warnings, `IsValid = true`.
6. `QcEngine_Validate_QcOptionsCombinations()`: Selective check execution count ($2, 3, 1, 5, 6$).
7. `QcEngine_Validate_FormatSummary_ProducesVietnameseOutput()`: Report string formatting.
8. `QcEngine_Validate_DefensiveNullAndEmptyInputs()`: Empty model, null collections handled gracefully.

---

## 5. Verification Method

To independently verify after implementation:

1. **Build Verification**:
   ```powershell
   dotnet build HPBim/HPBim.slnx -c Release
   ```
   *Expected*: 0 Errors, 0 Warnings across `HPBim.Core` and `HPBim.Core.Tests`.

2. **Test Execution Verification**:
   ```powershell
   dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   ```
   *Expected*: Total tests $\ge 150$, 100% green (0 failed, 0 skipped), execution time $< 1.5$ seconds.

3. **Clean Code Compliance Audit**:
   ```powershell
   Get-ChildItem -Path "HPBim/src/HPBim.Core", "HPBim/tests/HPBim.Core.Tests" -Recurse -Filter "*.cs" |
     Where-Object { $_.FullName -notmatch "obj|bin" } |
     ForEach-Object { [PSCustomObject]@{ File = $_.Name; Lines = (Get-Content $_.FullName | Measure-Object -Line).Lines } } |
     Where-Object { $_.Lines -gt 300 }
   ```
   *Expected*: Empty output (0 files exceeding 300 lines).
