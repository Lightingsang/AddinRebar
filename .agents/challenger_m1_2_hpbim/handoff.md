# Handoff Report: Challenger M1-2 — TCVN Detailing & QC Engine Adversarial Verification

**Date:** 2026-10-06T00:12:00Z  
**Agent:** `teamwork_preview_challenger` (critic, specialist)  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m1_2_hpbim\`  
**Target Milestone:** Milestone 1 (M1: TCVN Detailing & QC Engine Adversarial Verification)  
**Verdict:** **`REQUEST_CHANGES`**

---

## 1. Observation

An empirical test harness was constructed and executed at:  
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m1_2_hpbim\runner\Program.cs`  
compiled against `HPBim.Core.csproj` (`netstandard2.0`) with .NET 10.0 runtime.

### 1.1 Empirical Verification Test Results (95 Total Assertions Executed)

#### A. `TcvnRebarCalculator` Unit Weight & vncad Ground-Truth Comparison: **VERIFIED PASS**
- **Test execution**: Checked formula $W = \frac{D^2}{162.2} \times L$ for all standard bar diameters ($D = 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32$) and compared with `vncad/struct.py:UNIT_WEIGHT` and `tests/test_thep_va_du_toan.py`.
- **Results**:
  * $D=6$: Calculated = $0.22195\text{ kg/m}$, vncad nominal = $0.222\text{ kg/m}$, diff = $0.00005 < 0.001$. [PASS]
  * $D=8$: Calculated = $0.39457\text{ kg/m}$, vncad nominal = $0.395\text{ kg/m}$, diff = $0.00043 < 0.001$. [PASS]
  * $D=10$: Calculated = $0.61652\text{ kg/m}$, vncad nominal = $0.617\text{ kg/m}$, diff = $0.00048 < 0.001$. [PASS]
  * $D=12$: Calculated = $0.88779\text{ kg/m}$, vncad nominal = $0.888\text{ kg/m}$, diff = $0.00021 < 0.001$. [PASS]
  * $D=14$: Calculated = $1.20838\text{ kg/m}$, vncad nominal = $1.208\text{ kg/m}$, diff = $0.00038 < 0.001$. [PASS]
  * $D=16$: Calculated = $1.57830\text{ kg/m}$, vncad nominal = $1.578\text{ kg/m}$, diff = $0.00030 < 0.001$. [PASS]
  * $D=18$: Calculated = $1.99753\text{ kg/m}$, vncad nominal = $1.998\text{ kg/m}$, diff = $0.00047 < 0.001$. [PASS]
  * $D=20$: Calculated = $2.46609\text{ kg/m}$, vncad nominal = $2.466\text{ kg/m}$, diff = $0.00009 < 0.001$. [PASS]
  * $D=22$: Calculated = $2.98397\text{ kg/m}$, vncad nominal = $2.984\text{ kg/m}$, diff = $0.00003 < 0.001$. [PASS]
  * $D=25$: Calculated = $3.85327\text{ kg/m}$, vncad nominal = $3.853\text{ kg/m}$, diff = $0.00027 < 0.001$. [PASS]
  * $D=28$: Calculated = $4.83354\text{ kg/m}$ (formula fallback). [PASS]
  * $D=32$: Calculated = $6.31319\text{ kg/m}$ (formula fallback). [PASS]
  * Diameter partition ($\le 10\text{mm}$ small vs $> 10\text{mm}$ large) matches exact weight sums. [PASS]

#### B. Commercial Bar Splice Calculator: **VERIFIED PASS**
- **Test execution**: Checked `CalculateCommercialBarSplices(totalSpanLengthMm, diameterMm, zone)`.
- **Results**:
  * $L = 5.0\text{ m} (< 11.7\text{ m})$: 0 splices, developed length = 5000mm. [PASS]
  * $L = 10.0\text{ m} (< 11.7\text{ m})$: 0 splices, developed length = 10000mm. [PASS]
  * $L = 11.7\text{ m}$ (exact boundary): 0 splices, developed length = 11700mm. [PASS]
  * $L = 15.0\text{ m}$ Tension ($D=20\text{mm}$): 1 splice, lap = $40D = 800\text{mm}$, developed length = $15000 + 800 = 15800\text{mm}$. [PASS]
  * $L = 15.0\text{ m}$ Compression ($D=20\text{mm}$): 1 splice, lap = $30D = 600\text{mm}$, developed length = $15000 + 600 = 15600\text{mm}$. [PASS]
  * $L = 25.0\text{ m}$ Tension ($D=20\text{mm}$): 2 splices, single lap = 800mm, developed length = $25000 + 2 \times 800 = 26600\text{mm}$. [PASS]

#### C. Stirrup Zoning ($a100$ dense / $a200$ sparse): **VERIFIED PASS**
- **Test execution**: Checked `CalculateBeamStirrupZones` and `CalculateColumnStirrupZones`.
- **Results**:
  * Typical beam ($L = 4000\text{mm}, H = 400\text{mm}$): dense length = 800mm ($2H$), start count = 8, mid sparse count = 12, end count = 8. [PASS]
  * Short beam ($L = 1500\text{mm}, H = 400\text{mm}$): dense length = 500mm ($L/3$ governs over $2H$), start count = 5, mid count = 2. [PASS]
  * Column ($H = 3600\text{mm}$): dense length = 900mm ($H/4$), bottom count = 9, middle sparse count = 9, top count = 9. [PASS]
  * Stirrup hooks ($10D \ge 75\text{mm}$): D6 clamps to 75mm, D8 = 80mm, D10 = 100mm. [PASS]

#### D. `QcClashDetector` Column vs Opening Clashes: **VERIFIED PASS**
- **Test execution**: Checked `DetectColumnOpeningClashes`.
- **Results**:
  * Positive clash: Column C1 ($220 \times 220$) vs Door D1 at $X = 50\text{mm}$ (width 900mm): detected 1 clash, penetration depth = $60.0\text{mm}$. [PASS]
  * Clear non-clash: Column C1 vs Door D2 at $X = 1500\text{mm}$: 0 clashes. [PASS]
  * Negative spatial hash coordinate handling at $(-3000, -2000)$: 1 clash detected correctly. [PASS]

#### E. `ClearanceChecker` Opening to Column Clearance: **VERIFIED PASS**
- **Test execution**: Checked `CheckOpeningToColumnClearances`.
- **Results**:
  * Clearance = 99mm ($< 100\text{mm}$): triggers violation `CLR-001` (Severity: Warning). [PASS]
  * Clearance = 100mm ($= 100\text{mm}$): 0 violations (strictly compliant threshold). [PASS]
  * Clearance = 150mm ($> 100\text{mm}$): 0 violations. [PASS]
  * Wall thickness check: 110mm and 220mm pass; 150mm triggers violation `TCVN-WALL-001`. [PASS]

---

### 1.2 Observed Defects & Failures

#### [DEFECT 1 - CRITICAL / BLOCKER] `Vector2D` Infinite Property Cycle Crashes JSON Serialization
- **Location**: `HPBim/src/HPBim.Core/Geometry/Vector2D.cs` lines 56 & 61; `HPBim/src/HPBim.Core/Models/WallDef.cs` lines 21 & 32; `HPBim/src/HPBim.Core/Models/BeamDef.cs` line 19.
- **Verbatim Error**:
  ```
  JsonException: A possible object cycle was detected. This can either be due to a cycle or if the object depth is larger than the maximum allowed depth of 64. Consider using ReferenceHandler.Preserve on JsonSerializerOptions to support cycles.
  Path: $.Walls.Direction.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.X.
  ```
- **Direct Replication**:
  ```csharp
  System.Text.Json.JsonSerializer.Serialize(new Vector2D(1, 0));
  // Immediately throws JsonException at depth 64!
  ```
- **Code Quote** (`Vector2D.cs`):
  ```csharp
  56: public Vector2D LeftNormal => new(-Y, X);
  ...
  61: public Vector2D RightNormal => new(Y, -X);
  ```

#### [DEFECT 2 - CRITICAL / BLOCKER] `Polygon2D` Deserialization Fails with `InvalidOperationException`
- **Location**: `HPBim/src/HPBim.Core/Geometry/Polygon2D.cs` lines 16 & 37.
- **Verbatim Error**:
  ```
  InvalidOperationException: Each parameter in the deserialization constructor on type 'HPBim.Core.Geometry.Polygon2D' must bind to an object property or field on deserialization. Each parameter name and type must match with a property or field on the object. Fields are only considered when 'JsonSerializerOptions.IncludeFields' is enabled. The name match can be case-insensitive.
  ```
- **Direct Replication**:
  ```csharp
  var poly = new Polygon2D(new[] { new Point2D(0, 0), new Point2D(100, 0), new Point2D(100, 100), new Point2D(0, 100) });
  string json = JsonSerializer.Serialize(poly, BimModelSerializer.CreateOptions(true));
  var desPoly = JsonSerializer.Deserialize<Polygon2D>(json, BimModelSerializer.CreateOptions(true));
  // Throws InvalidOperationException!
  ```
- **Code Quote** (`Polygon2D.cs`):
  ```csharp
  16: public Polygon2D(IEnumerable<Point2D> vertices)
  ...
  37: public IReadOnlyList<Point2D> Vertices => _vertices;
  ```
  The constructor parameter type `IEnumerable<Point2D>` does not match property type `IReadOnlyList<Point2D>`.

#### [DEFECT 3 - MEDIUM / BEHAVIORAL BUG] `QcClashDetector` False Positive: Flags Perimeter Beams on Slab Boundary
- **Location**: `HPBim/src/HPBim.Core/Qc/QcClashDetector.cs` line 91.
- **Code Quote**:
  ```csharp
  89: foreach (var slab in slabs)
  90: {
  91:     if (slab.Boundary != null && slab.Boundary.Contains(beamMid))
  92:     {
  93:         clashes.Add(new QcClash(beam.Id, slab.Id, "SecondaryBeamSlicesUndividedSlab", ...));
  94:     }
  95: }
  ```
- **Direct Replication**:
  For an exterior supporting beam running from $(0, 0)$ to $(4000, 0)$ on the edge of slab $(0, 0)-(4000, 4000)$, `beamMid` = $(2000, 0)$.
  Because `Polygon2D.Contains(pt, includeBoundary: true)` defaults to `includeBoundary = true`, `slab.Boundary.Contains(beamMid)` evaluates to `true`.
  `DetectBeamSlabClashes` returns a false positive:
  `[CLASH] SecondaryBeamSlicesUndividedSlab: B_PERIMETER vs S1 tại (2000, 0) - Dầm phụ B_PERIMETER chạy xuyên qua giữa ô sàn S1 mà chưa được chia tách ô sàn!`

---

## 2. Logic Chain

1. **Step 1 (TCVN Calculations & Clearance Thresholds)**:
   - Evaluated `TcvnRebarCalculator.CalculateUnitWeight(d)` against theoretical formula $W = D^2 / 162.2$ and against vncad nominal lookup table `UNIT_WEIGHT`. All 12 standard diameters ($D=6..32$) passed within $0.0005\text{ kg/m}$ difference.
   - Evaluated commercial splices for spans $5\text{m}, 10\text{m}, 11.7\text{m}, 15\text{m}, 25\text{m}$. Verified splice count ceiling logic, tension lap ($40D$) and compression lap ($30D$) lengths.
   - Evaluated beam and column stirrup zoning. Verified dense $a100$ and sparse $a200$ zones, and short-span $L/3$ governing length.
   - Evaluated `ClearanceChecker`: verified that 99mm distance triggers warning `CLR-001`, while 100mm and 150mm do not trigger warnings.
   - *Inference*: The mathematical formulas and rules in `TcvnRebarCalculator`, `TcvnStandards`, and `ClearanceChecker` are mathematically robust.

2. **Step 2 (Clash Detection Boundary Logic)**:
   - Evaluated `QcClashDetector.DetectColumnOpeningClashes`: verified positive clash (door penetrating column) and negative non-clash (door far from column).
   - Evaluated `QcClashDetector.DetectBeamSlabClashes`: secondary beams through the interior of the slab are flagged as expected; tie beams are skipped as expected.
   - However, when testing a perimeter beam situated on the slab edge, `Polygon2D.Contains(beamMid)` evaluates to `true` because the default argument is `includeBoundary = true`.
   - *Inference*: Normal edge perimeter beams supporting slabs will be erroneously flagged as invalid interior slices unless `includeBoundary: false` is passed.

3. **Step 3 (Serialization Round-Trip Disruption)**:
   - When attempting `BimModelSerializer.Serialize(model)`, `System.Text.Json` traverses `WallDef.Direction` and `WallDef.Normal`, which are of type `Vector2D`.
   - `Vector2D` exposes `public Vector2D LeftNormal => new(-Y, X);` and `RightNormal`. Because getter properties of the same type return new instances, the serializer enters infinite recursion until depth 64, crashing with `JsonException`.
   - When attempting `BimModelSerializer.Deserialize(json)`, `Polygon2D` cannot be deserialized because its constructor parameter is `IEnumerable<Point2D> vertices` while its property is `IReadOnlyList<Point2D> Vertices`, causing `InvalidOperationException`.
   - *Inference*: Any `BimModel` containing walls, beams, slabs, or rooms cannot be serialized or deserialized using `BimModelSerializer`. This directly violates Acceptance Criteria and blocks Milestone 3 (`HPBim.Client` pipe payload transmission) and Milestone 4 (CLI `validate --input model.json`).

4. **Step 4 (Proof-of-Fix)**:
   - Implementing custom converters (`Vector2DJsonConverter` serializing only `x, y`, and `Polygon2DJsonConverter` serializing `vertices`) and applying them to `JsonSerializerOptions` enabled 100% successful round-trip serialization and deserialization of the complete complex benchmark model across all 11 collections (`Levels`, `Grids`, `Columns`, `Walls`, `Beams`, `Slabs`, `Rooms`, `Fixtures`, `Pipes`, `Rebars`).

---

## 3. Caveats

- **No Caveats.**
- The empirical verification harness was executed with real objects, real geometry calculations, and real JSON parsing using the exact runtime pinned in `global.json` (.NET SDK `10.0.300`).
- The challenger did not modify any production code in `HPBim/src/HPBim.Core/`, adhering strictly to the Review-Only constraint.

---

## 4. Conclusion

**Verdict: `REQUEST_CHANGES`**

While the core mathematical calculations (`TcvnRebarCalculator`), clash detection physics (`DetectColumnOpeningClashes`), and clearance rules (`ClearanceChecker`) are mathematically sound and meet TCVN requirements, `HPBim.Core` cannot be approved in its current state due to two critical blockers and one behavioral bug:

1. **[BLOCKER 1]** `Vector2D.cs`: Annotate `[System.Text.Json.Serialization.JsonIgnore]` on `LeftNormal` and `RightNormal` (or convert them to methods `GetLeftNormal()`, `GetRightNormal()`). Similarly annotate `[JsonIgnore]` on computed properties `Direction` and `Normal` in `WallDef.cs` and `BeamDef.cs`.
2. **[BLOCKER 2]** `Polygon2D.cs`: Annotate `[System.Text.Json.Serialization.JsonConstructor]` on constructor and align parameter type to match property: `public Polygon2D(IReadOnlyList<Point2D> vertices)`. Annotate `[JsonIgnore]` on computed properties (`Perimeter`, `SignedArea`, `Area`, `Centroid`, `BoundingBox`, `IsClockwise`).
3. **[BEHAVIORAL BUG]** `QcClashDetector.cs` line 91: Change `slab.Boundary.Contains(beamMid)` to `slab.Boundary.Contains(beamMid, includeBoundary: false)` to prevent false positive clash warnings on perimeter edge beams.

---

## 5. Verification Method

To reproduce the findings independently:

```powershell
# 1. Run the empirical adversarial harness
dotnet run --project "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m1_2_hpbim\runner\Runner.csproj" -c Release

# 2. Reproduce Vector2D cycle directly
dotnet exec --depsfile ... (or run runner standalone)

# 3. Invalidation condition:
# All tests in the runner pass with 0 failures when the fixes above are applied.
```
