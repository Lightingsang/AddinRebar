# Handoff Report — explorer_m1_it2_3

**Agent**: `explorer_m1_it2_3`  
**Role**: M1 Special Bar, Layer 2 & Polyline Remediation Explorer  
**Type**: Hard Handoff (Task Complete)  
**Deliverable Document**: `remediation_plan.md`  
**Target Subsystem**: `HPRebar/HPRebar.Core/BeamRebar/`  

---

## 1. Observation

Direct code inspections and adversarial audit reports revealed three specific defect mechanisms in the M1 domain calculators:

### 1.1 `BeamSpecialBarCalculator.cs` Support Column Penetration
- **Observation Path**: `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs:20-47, 77-105, 128-162`
- In `ComputeHangingStirrupStations` (lines 35–44):
  ```csharp
  for (int k = countPerSide; k >= 1; k--) stations.Add(xSecL - (k * spacingMm));
  for (int k = 1; k <= countPerSide; k++) stations.Add(xSecR + (k * spacingMm));
  ```
  Stations are computed without reference to `hostSpan.StartX` or `hostSpan.EndX`. For a secondary beam with $X_{center} = 350\text{ mm}$, $b_s = 250\text{ mm}$ in a span with $StartX = 200\text{ mm}$, stations are placed at $X \in \{75, 125, 175\}\text{ mm}$, penetrating into the support column.
- In `ComputeDiagonalTiePolyline` (lines 98–103):
  ```csharp
  new(xSecL - deltaX - anchorLength, 0.0, zTopBar),
  new(xSecL - deltaX, 0.0, zTopBar),
  ...
  new(xSecR + deltaX + anchorLength, 0.0, zTopBar)
  ```
  With $\Delta X = 536\text{ mm}$ and $L_{anchor} = 420\text{ mm}$, horizontal projection is $956\text{ mm}$. If $X_{center} = 800\text{ mm}$, $xSecL = 675\text{ mm}$, the start vertex is $X = 675 - 956 = -281.0\text{ mm}$, projecting 281 mm outside the continuous beam assembly into negative space.

### 1.2 `BeamAdditionalBarCalculator.cs` Dropped Layer 2 Top Bars on Exterior Supports
- **Observation Path**: `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs:40-89, 92-140`
- In exterior support blocks (`isExteriorStart` at line 41 and `isExteriorEnd` at line 92), only `config.Layer1Count` is processed. Both blocks terminate with `continue;` (lines 88 and 139).
- Any configuration specifying `config.Layer2Count > 0` for Support 0 or Support N is discarded without generating Layer 2 reinforcement.
- In `BeamAdditionalBarCalculatorTests.cs:253-262`, `HighBarCountAutomaticallyDistributesExcessIntoSecondLayer` configured `SupportIndex = 0` with `Layer2Count = 2`, but asserted only `Assert.Equal(3, bars.Count(b => b.Layer == 1));`, masking this omission.

### 1.3 `BeamMainBarCalculator.cs` Hairpin 180° Turn Culling & Long Beam Splicing
- **Observation Path**: `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs:424-433`
- In `SimplifyPolyline`:
  ```csharp
  var v1 = (pCurr - pPrev).Normalize();
  var v2 = (pNext - pCurr).Normalize();
  var cross = v1.Cross(v2);
  if (cross.Length > Tolerance.CollinearToleranceMm) simplified.Add(pCurr);
  ```
  For a 180° hairpin turn (e.g. $(0,0,0) \to (100,0,0) \to (50,0,0)$), vectors $\vec{v}_1 = (1,0,0)$ and $\vec{v}_2 = (-1,0,0)$ have $\|\vec{v}_1 \times \vec{v}_2\| = \sin 180^\circ = 0 \le \text{Tolerance.CollinearToleranceMm}$. The turnaround apex $(100,0,0)$ is culled, collapsing the polyline into $(0,0,0) \to (50,0,0)$.
- In `ComputeTopMainBars` (lines 123–181) and `ComputeBottomMainBars` (lines 337–393), bars exceeding stock length ($11.7\text{ m}$) are divided into exactly 2 segments around a single target midspan or support. Beams longer than $22.5\text{ m}$ produce segments exceeding commercial stock length.

---

## 2. Logic Chain

1. **Special Bar Clamping**:
   - Support nodes are physical structural columns containing vertical reinforcement; placing beam stirrups inside column nodes creates physical clashes in BIM and invalidates material takeoff.
   - For hanging stirrups, filtering stations to $[hostSpan.StartX + Cover, hostSpan.EndX - Cover]$ removes illegal positions without causing duplicate stirrup collisions at the face.
   - For diagonal bent ties, a 45° incline requires $xSecL - \Delta X \ge StartX + Cover$. If this geometric clearance is unavailable, a 45° bent bar cannot be formed. When clearance exists, clamping the horizontal anchor leg to $[StartX + Cover, EndX - Cover]$ guarantees all vertices remain strictly within the host beam envelope.

2. **Exterior Support Layer 2 Top Bars**:
   - Negative bending moments at cantilever or rigid exterior column joints frequently require multi-layer tensile reinforcement.
   - Mirroring the intermediate support Layer 2 implementation ($z_2 = z_1 - gap$, $L_{ext} = r_2 \times L_n$) and anchoring into the exterior column with a 90° downward hook (bounded by available depth $z_2 - z_{botFloor}$) restores structural integrity and honors user specifications for Support 0 and Support N.

3. **Polyline Simplification**:
   - Collinear vertex culling is mathematically defined as removing intermediate points along a straight line in the *same forward direction* ($\theta = 0 \implies \sin \theta = 0, \cos \theta = 1$).
   - Direction reversals ($\theta = \pi \implies \sin \theta = 0, \cos \theta = -1$) are geometric apexes, not straight lines. Adding the condition $\vec{v}_1 \cdot \vec{v}_2 > 0$ strictly distinguishes collinear continuation from 180° turns, preserving hairpin hooks while removing redundant collinear points.

4. **Long Beam Splicing**:
   - Commercial rebar has a strict manufacturing limit of 11.7 m. Beams exceeding $2 \times 11.7\text{ m} \approx 22.5\text{ m}$ cannot be fabricated with a single lap joint.
   - Adding a defensive validation failure in `BeamContinuousStack.Validate()` protects downstream processes, while defining the multi-segment partitioning algorithm prepares the codebase for $N$-point splicing.

---

## 3. Caveats

1. **Revit API Decoupling**: All calculations remain 100% pure standard C# in `HPRebar.Core` (`netstandard2.0`). No Revit API references were introduced.
2. **Multi-Splice Scope**: The full multi-splice algorithm for continuous beams $> 22.5\text{ m}$ is designed and specified in `remediation_plan.md`, with defensive validation planned for Phase 1 to prevent illegal lengths while multi-point partitioning is scheduled.
3. **No Code Mutations**: Per the Teamwork explorer archetype, no source files were mutated. All deliverables are structured plans and code specifications located within `.agents/explorer_m1_it2_3/`.

---

## 4. Conclusion

The root causes of all three targeted defects have been traced to exact lines of code, and detailed mathematical solutions and replacement code chunks have been specified in `remediation_plan.md`:
1. `BeamSpecialBarCalculator.cs` is remediated with boundary filtering and anchor clamping within $[hostSpan.StartX + Cover, hostSpan.EndX - Cover]$.
2. `BeamAdditionalBarCalculator.cs` is remediated with complete Layer 2 generation for Support 0 and Support N.
3. `BeamMainBarCalculator.cs` is remediated with codirectional vector dot-product verification ($\vec{v}_1 \cdot \vec{v}_2 > 0$) in `SimplifyPolyline` to preserve 180° hairpin hooks, and defensive validation for beams $> 22.5\text{ m}$.

---

## 5. Verification Method

To independently verify the implementation after code application:

1. **Test Execution Command**:
   ```bash
   dotnet test HPRebar.Core.Tests --filter "FullyQualifiedName~BeamSpecialBarCalculatorTests|FullyQualifiedName~BeamAdditionalBarCalculatorTests|FullyQualifiedName~BeamMainBarCalculatorTests"
   ```
2. **Specific Test Invalidation Conditions**:
   - Any hanging stirrup station with $X < hostSpan.StartX + hostSpan.Cover$ or $X > hostSpan.EndX - hostSpan.Cover$ fails verification.
   - Any diagonal tie vertex with $X < hostSpan.StartX$ or $X > hostSpan.EndX$ fails verification.
   - An exterior support configuration with `Layer2Count = 2` producing `bars.Count(b => b.Layer == 2) != 2` fails verification.
   - `SimplifyPolyline` applied to `[(0,0,0), (100,0,0), (50,0,0)]` producing anything other than 3 points with apex `(100,0,0)` fails verification.
   - Running the full test suite (`dotnet test HPRebar.Core.Tests`) must yield 100% passing tests with zero tautological assertions.
