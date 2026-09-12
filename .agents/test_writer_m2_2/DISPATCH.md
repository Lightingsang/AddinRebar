## 2026-09-07T15:58:56Z
You are test_writer_m2_2, an expert unit test author.
Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\test_writer_m2_2\
Authoritative User Request: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\ORIGINAL_REQUEST.md (Refer to '## Follow-up — 2026-09-07T15:37:30Z')
Scope Document: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\SCOPE.md
Domain Implementation: Inspect all files in `HPRebar/HPRebar.Core/FoundationRebar/`

Your Mission:
Author the comprehensive pure domain unit test suite for Milestone M2 in `HPRebar/HPRebar.Core.Tests/FoundationRebar/`.

Write Ownership:
You exclusively own all files created in `HPRebar/HPRebar.Core.Tests/FoundationRebar/`.
Do NOT modify implementation code in `HPRebar.Core/` or existing tests in `BeamRebar/` or `ColumnRebar/`.

Test Framework & Rules:
- xUnit v3 (`using Xunit;`)
- Namespace: `namespace HPRebar.Core.Tests.FoundationRebar;`
- Runner: `Microsoft.Testing.Platform` (via `dotnet test HPRebar/HPRebar.Core.Tests`)
- Zero dependencies on `Autodesk.Revit.*`.

Mandatory Test Suites & Scenarios:
1. `FoundationBoundaryCalculatorTests.cs`:
   - Effective boundary calculation ($[c_{side}, L - c_{side}] \times [c_{side}, W - c_{side}]$)
   - Effective span lengths $L_{eff} = L - 2 c_{side}$, $W_{eff} = W - 2 c_{side}$
   - Edge cases: $L \le 2 c_{side}$, $W \le 2 c_{side}$, negative covers
2. `FoundationValidationCalculatorTests.cs`:
   - Spacing $\le 0$ (zero and negative) -> invalid
   - Insufficient slab thickness: $H < c_{bot} + c_{top} + \sum d_{active}$ -> invalid
   - Sufficient slab thickness ($H \ge H_{min}$) -> valid
   - Top mat disabled: checks only bottom mat thickness requirement
   - Excessive bar count ($N > 1002$) -> invalid
   - Dimensions $\le 2 c_{side}$ -> invalid
3. `FoundationMeshCalculatorTests.cs`:
   - Spacing divisibility:
     * When length is exactly divisible by spacing ($L_{eff} \pmod s == 0$)
     * When length is not divisible: equal centering margins on both sides ($\delta = \text{slack}/2$)
     * Single bar when $Span_{eff} < s$
   - 4-layer vertical stacking:
     * Layer 1 (Bottom X): $z_1 = c_{bot} + d_{BX}/2$
     * Layer 2 (Bottom Y): $z_2 = c_{bot} + d_{BX} + d_{BY}/2$
     * Layer 3 (Top Y): $z_3 = H - c_{top} - d_{TX} - d_{TY}/2$
     * Layer 4 (Top X): $z_4 = H - c_{top} - d_{TX}/2$
     * Verify $z_1 < z_2 < z_3 < z_4$ and positive clearance gap
   - Arbitrary rotation in plan:
     * Rotations by 0°, 30°, 45°, 90°, 137°
     * Bar counts, individual bar lengths, and total steel length are invariant
     * All bar curves are coplanar in world space ($(\mathbf{p} - \mathbf{p}_0) \cdot \mathbf{n} == 0$)
   - Anchorage hooks:
     * Bottom bars bend UP (+Z)
     * Top bars bend DOWN (-Z)
     * Oversized hooks are clamped safely, never breaching opposite cover
   - Top mat disabled vs enabled:
     * Top mat disabled -> only Layer 1 & 2 generated, TopBarsX & TopBarsY are empty
4. `FoundationGeometrySnapshotTests.cs`:
   - Orthonormal basis vectors ($LocalX \cdot LocalY == 0$, unit lengths, right-handed cross product)
   - `ToWorld` and `ToLocal` round-trip accuracy within floating-point tolerance ($10^{-6}$)

Verification:
Execute:
```bash
dotnet test HPRebar/HPRebar.Core.Tests
```
Verify that all 241 existing tests PLUS all new tests pass 100% (0 failed, 0 skipped).
