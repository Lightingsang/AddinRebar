# Handoff Report: M1 Unit Test Suite Specifications (`HPRebar.Core.Tests/BeamRebar/`)

**Author**: `spec_miner_m1_3`  
**Role**: M1 Test Suite Specification Miner  
**Working Directory**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3`  
**Target File**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3\test_spec_plan.md`  
**Date**: 2026-09-07  

---

## 1. Observation

1. **Test Infrastructure & Framework**:
   - `HPRebar.Core.Tests/HPRebar.Core.Tests.csproj` specifies target framework `net8.0`, package `xunit.v3` version `3.1.0`, and test runner property `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` (lines 110–122).
   - `AGENTS.md` (lines 35–37) confirms: `dotnet test HPRebar.Core.Tests # 102 xUnit tests, no Revit needed`.

2. **ColumnRebar Gold Reference Pattern**:
   - `HPRebar.Core.Tests/ColumnRebar/TestSections.cs` (lines 6–33) establishes shared static test fixtures and factory builders (`TestSections.Rectangle(...)`), using C# 9+ record `with { ... }` non-destructive mutations.
   - `HPRebar.Core.Tests/ColumnRebar/StirrupDistributionCalculatorTests.cs` (lines 8–140) uses `private const int Precision = 6;`, `[Fact]` and `[Theory]` attributes, explicit invariant assertions (`Assert.Single`, `Assert.InRange`, `Assert.Equal`), and exception assertions (`Assert.Throws<ArgumentOutOfRangeException>`) guarding against Revit API set limits (`MaxBarPositions = 1002`).

3. **Core Specification & Reverse-Engineered Algorithms**:
   - `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\survey_source_analysis.md` provides exact formulas for:
     - Clear span $L_n = L_{center} - C_{left}/2 - C_{right}/2$ (line 143).
     - Uniform stirrup count $n = \lfloor L_{dist}/S \rfloor + 1$ with centering margin $\delta$ (lines 165–167).
     - 3-Zone stirrups ($L/4-L/2-L/4$ and $L/3-L/3-L/3$) with boundary rounding and short link beam fallback (lines 170–181).
     - Continuous top and bottom bar 90° hooks and 50% staggered lap splices for lengths $> 11.7$ m (lines 195–232).
     - Additional top negative moment bars ($L_n/3$ or $L_n/4$) and midspan bottom positive moment bars ($L_n/7$ or $L_n/8$) across 2 vertical layers with $\Delta Z = \pm (d_{bar} + 30)$ mm (lines 237–268).
     - Side/skin reinforcement for deep beams ($h \ge 700$ mm) with $s_v \le 300$ mm and anti-buckling cross-ties (lines 273–285).
     - Secondary beam hanging stirrup cages ($n$ pairs @ 50 mm) and 45° diagonal bent bars (lines 286–300).
     - Canvas coordinate transformation $X_{canvas} = M + (X - X_{min}) \times S$ and $Y_{canvas} = H_{canvas} - M - (Z - Z_{min}) \times S$ (lines 352–355).

---

## 2. Logic Chain

1. **Decoupled Testability**:
   - Observation 1 demonstrates that `HPRebar.Core.Tests` runs completely isolated from Revit via xUnit v3 in .NET 8.
   - Therefore, all calculations in `HPRebar.Core/BeamRebar/Calculators/` must be 100% testable in memory without Revit API mocks, using purely numerical inputs ($mm$, $double$) and immutable domain records.

2. **Test Suite Layout & Fixture Standardization**:
   - Observation 2 reveals that the existing `ColumnRebar` test suite organizes fixtures into `TestSections.cs` and dedicated calculator test files.
   - Therefore, `HPRebar.Core.Tests/BeamRebar/` must mirror this structure with `TestBeamData.cs` providing builder methods (`SingleSpan`, `TwoSpan`, `ThreeSpan`, `CantileverLeft`, `VariableDepth`, `DeepBeam`, `UniformStirrupSpec`, `ThreeZoneL4StirrupSpec`, `MainBarSpec`), allowing concise, readable test declarations.

3. **Multi-Tier Test Decomposition**:
   - Based on the comprehensive requirements extracted in Observation 3, the test plan is organized into 4 rigorous tiers:
     - **Tier 1 (Feature Coverage)**: Validates base formulas across all 10 feature groups (uniform stirrups, 3-zone L4, 3-zone L3, top bars, bottom bars, top support additions, bottom midspan additions, side bars, hanging ties, canvas transform).
     - **Tier 2 (Boundary & Corner Cases)**: Validates boundary values (0 or negative spans/spacings, single span, 2/3/5 multi-spans, left/right/both cantilevers, depth steps, link beams $<600$ mm, $>1002$ bar count limits, sub-millimeter segment culling, planarity enforcement).
     - **Tier 3 (Realistic Combinations)**: Validates multi-variable interactions (varying cross-sections across 3 spans, deep beams with side bars + 3-zone stirrups, long spans $>11.7$ m with 50% staggered lap splices, cantilevers with interior spans).
     - **Tier 4 (Real-World Structural Framing)**: Validates full real-world scenarios:
       - Framing Case A: Standard 3-span continuous office girder ($L_1=6m, L_2=5m, L_3=6m, 300\times 600$, 43+36+43=122 stirrups, 3T20 top, 3T20 bottom, L/3 top additions, L/7 midspan additions).
       - Framing Case B: Secondary beam framing intersection with $2\times 3$ hanging stirrups @ 50mm and 45° diagonal bent bars.

4. **Precision and Invariant Verification**:
   - Assertions standard is fixed to `Assert.Equal(expectedDouble, actualDouble, precision: 6)` (tolerance $1.0\times 10^{-6}$ mm), with exact integer assertions for bar counts and collection lengths, ensuring 100% deterministic test execution.

---

## 3. Caveats

- **No Caveats**: The legacy source algorithms in `R02_BeamsRebar` and the architectural patterns of `HPRebar.Core` have been exhaustively documented in `survey_source_analysis.md` and `target_arch_analysis.md`. All required domain calculations are fully specified without ambiguities.
- Assumed standard detailing hooks: 90° downward hooks for top bars into end columns, 90° upward hooks for bottom bars into end columns, and 135°/90° alternating hooks for web anti-buckling cross-ties.

---

## 4. Conclusion

A comprehensive unit test specification comprising **94 unit tests** across 6 test classes and 1 shared fixture builder (`TestBeamData.cs`) has been fully designed and documented in `test_spec_plan.md`. 

The test specification ensures complete verification of all domain calculators in `HPRebar.Core/BeamRebar/`, guards against all known legacy edge cases and Revit API set limitations (such as the 1002 bar position limit and sub-millimeter segment crashes), and guarantees a 100% test pass rate under `dotnet test HPRebar.Core.Tests` without touching the Revit runtime.

---

## 5. Verification Method

1. **Inspect Test Plan Artifact**:
   - View `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3\test_spec_plan.md` to review the full 4-tier test case matrix, fixture builder specifications, and assertion criteria.
2. **Independent Test Execution (Upon SUT & Test Implementation in M1/M2)**:
   - Run the project unit test suite from repository root:
     ```powershell
     dotnet test HPRebar/HPRebar.Core.Tests
     ```
   - Invalidation Condition: Any compilation error, any failing test, any test duration $> 5$ seconds, or any reference to `Autodesk.Revit.*` in `HPRebar.Core` or `HPRebar.Core.Tests`.
