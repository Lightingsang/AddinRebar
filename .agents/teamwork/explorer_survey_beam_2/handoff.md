# Survey Report: TCVN 5574:2018 Calculation Engine, Force Import (Excel/ETABS), and Domain Architecture

**Author:** explorer_survey_beam_2 (teamwork_preview_explorer)  
**Parent:** orchestrator_3 (`4db7c650-146a-4a05-a1e1-c0fb57e2733b`)  
**Working Directory:** `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_2\`  
**Date:** 2026-10-07  
**Scope:** Survey Focus Area 2 (Calculation & Engineering Domain Logic, TCVN 5574:2018 Formulation, Force Import via ClosedXML/ETABS, xUnit v3 Test Suite)

---

## 1. Observation

### 1.1 Existing Models and Engines in `HPRebar.Core`
- **Target Framework & Dependencies (`HPRebar/HPRebar.Core/HPRebar.Core.csproj:4-16`):**
  - Targets `netstandard2.0` with `<LangVersion>latest</LangVersion>` and `<Nullable>enable</Nullable>`.
  - Only one dependency: `<PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all"/>`.
  - **Zero references to `Autodesk.Revit.*`** or UI/COM libraries (strictly compliant with Clean Code Governance Rule D1).
- **Existing Beam Modeling Structure (`HPRebar/HPRebar.Core/BeamRebar/Models/`):**
  - `BeamSpan.cs`: Record defining cross-section geometry: `Width` ($b$), `Height` ($h$), `TopElevation` ($Z_{top}$), `BottomElevation` ($Z_{bot} = Z_{top} - h$), `Cover` ($c$), `LengthClear` ($L_n$), `LengthCenter` ($L_c$), `StartX`, `EndX`. Contains helper `EffectiveDepth(barDiameter, stirrupDiameter) => Height - Cover - stirrupDiameter - (barDiameter / 2.0)`.
  - `BeamMainBarSpec.cs`: Longitudinal continuous bars: `TopCount`, `TopDiameter`, `TopCover`, `BottomCount`, `BottomDiameter`, `BottomCover`, `MaxStockLength` (default 11700 mm), `LapFactor` (default 40.0), `EnableStagger`, `TopStartAnchorage`, `TopEndAnchorage`.
  - `BeamAdditionalBarSpec.cs`: Non-continuous reinforcement: `SupportTopBars` with `SupportAdditionalTopBarConfig` (`Layer1Count`, `Layer1Diameter`, `Layer1ExtensionRatio`, `Layer2Count`, `Layer2Diameter`, `Layer2ExtensionRatio`, `LayerGap`), and `SpanBottomBars` with `SpanAdditionalBottomBarConfig` (`Layer1Count`, `Layer1Diameter`, `CutoffRatio`, `Layer2Count`, `Layer2Diameter`, `LayerGap`).
  - `BeamStirrupSpec.cs`: Transverse ties: `Layout` (`Uniform`, `ThreeZoneL4`, `ThreeZoneL3`), `Diameter`, `Cover`, `SpacingDense` ($s_1$), `SpacingSparse` ($s_2$), `StartOffset`, `IncludeStirrupsInNodes`, `NodeSpacing`, `HookAngle`.
  - `BeamSideBarSpec.cs`: Skin bars for deep beams: `AutoSkinBars`, `DepthThreshold`, `MaxVerticalSpacing`, `Diameter`, `IncludeCrossTies`, `CrossTieSpacing`.
  - `BeamSpecialBarSpec.cs`: Hanging stirrups at intersections with secondary beams: `EnableHangingStirrups`, `HangingStirrupSpacing`, `HangingStirrupDiameter`, `HangingStirrupCount`.
- **Existing Engineering/Calculation Logic Check:**
  - `BeamRebar/Calculators/`: Contains 12 geometric layout calculators (`BeamMainBarCalculator`, `BeamAdditionalBarCalculator`, `BeamStirrupDistributionCalculator`, `BeamSideBarCalculator`, `BeamSpecialBarCalculator`, `BeamSpecRules`, `BeamSupportLayout`, `BeamSpanAssembly`, etc.).
  - Search across `HPRebar.Core` for structural resistance terms (`alpha_m`, `xi_R`, `Rb`, `Rs`, `Asw`, `5574`): **No structural capacity, resistance, or TCVN 5574:2018 calculation engine exists in `HPRebar.Core`**.
  - Current calculators only perform pure geometric drafting (turning already-known bar counts, diameters, and spacings into 3D polyline coordinates).
  - In `HPBim/src/HPBim.Core/Tcvn/`, preliminary constants exist (`TcvnStandards.cs`, `TcvnRebarCalculator.cs`, `ConcreteCoverRules.cs`), but HPRebar is strictly forbidden from referencing HPBim. A clean, dedicated, self-contained TCVN 5574:2018 structural engine must be designed directly in `HPRebar.Core`.

### 1.2 Excel Import Infrastructure (`ClosedXML`)
- In `HPRebar/HPRebar/HPRebar.csproj:40`:
  `<PackageReference Include="ClosedXML" Version="0.104.2"/>` is already present in the add-in project.
  `ClosedXML` supports .NET 8 / .NET Framework and is ILRepack-merged into `HPRebar.dll`.
- In `HPRebar.Core`: Does NOT (and MUST NOT) reference `ClosedXML` to preserve Rule D1 (pure BCL + Polyfill).
- Reference pattern: In `HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs`, the parsing engine accepts `IKataDamCellAccessor` (defined in `IKataDamCellAccessor.cs`) or in-memory tables (`KataCellTable.cs`). The physical file reader (`ClosedXML` or COM) lives exclusively in the outer layer (`HPRebar`), while the parsing and domain validation logic stays in `HPRebar.Core`.

### 1.3 ETABS 22 Infrastructure (`HPEtabs/`)
- In `HPEtabs/HPEtabs.Mcp.Server/Registry/SeedLibrary/Results/get_frame_forces/code.cs:1-55`:
  - Connects to ETABS via OAPI: `sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput()`, `sapModel.Results.Setup.SetComboSelectedForOutput(caseOrCombo)`.
  - Queries frame forces: `sapModel.Results.FrameForce(frame, eItemTypeElm.ObjectElm, ref n, ref obj, ref objSta, ref elm, ref elmSta, ref loadCase, ref stepType, ref stepNum, ref p, ref v2, ref v3, ref t, ref m2, ref m3)`.
  - Returns: `stationMm`, `loadCase`, `pKN` (axial), `v2KN` (major shear $V_2$), `v3KN`, `tKNm`, `m2KNm`, `m3KNm` (major bending $M_3$).
- The ETABS MCP bridge listens on Named Pipe: `\\.\pipe\hpetabs-mcp-22`.
- Direct COM access: Out-of-process COM connection via `cOAPI` / `SapModel` using `ETABSv1.dll`.

### 1.4 Test Suite in `HPRebar.Core.Tests`
- `HPRebar/HPRebar.Core.Tests/HPRebar.Core.Tests.csproj`:
  - Target: `net8.0`, `<OutputType>Exe</OutputType>`, `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`.
  - Packages: `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5).
  - Reference: Only `..\HPRebar.Core\HPRebar.Core.csproj`.
- Command Execution Verification:
  `dotnet test HPRebar.Core.Tests` executed in 3.6 seconds:
  ```text
  Test run summary: Passed!
    total: 1530
    failed: 0
    succeeded: 1530
    skipped: 0
    duration: 3s 595ms
  ```

---

## 2. Logic Chain

### 2.1 Domain Separation & Architecture Strategy
1. **From Observation 1.1 & 1.2:** `HPRebar.Core` is strictly `netstandard2.0` with zero external dependencies beyond `Polyfill`.
2. **Inference:** The TCVN 5574:2018 calculation engine, material database, flexural/shear formulas, and rebar selection optimization must be implemented entirely within `HPRebar.Core` in a dedicated namespace (e.g. `HPRebar.Core.Tcvn5574` or `HPRebar.Core.BeamRebar.Design`).
3. **Data Ingestion Interface:** Force tables from Excel (`ClosedXML`) or ETABS must be mapped to pure domain records/interfaces in `HPRebar.Core` (`BeamInternalForceEnvelope`, `BeamDesignSectionInput`, `IBeamForceTableAccessor`). The file reading (`ClosedXML`) and COM queries (`HPEtabs`) belong in the service layer of `HPRebar/HPRebar/BeamRebar/Service/`.

### 2.2 TCVN 5574:2018 Engineering Formulation (Rigorous Math & Standards)

#### A. Concrete Material Properties (TCVN 5574:2018 Bảng 6, Bảng 7, Bảng 10)
For Limit State 1 (ULS), short-term static loading condition ($\gamma_{b1} = 1.0$):

| Concrete Grade | $R_b$ (MPa) | $R_{bt}$ (MPa) | $E_b$ (MPa) |
|---|---|---|---|
| **B15** (M200) | 8.5 | 0.75 | 24,000 |
| **B20** (M250) | 11.5 | 0.90 | 27,500 |
| **B25** (M350) | 14.5 | 1.05 | 30,000 |
| **B30** (M400) | 17.0 | 1.15 | 32,500 |
| **B35** (M450) | 19.5 | 1.30 | 34,500 |
| **B40** (M500) | 22.0 | 1.40 | 36,000 |

#### B. Reinforcing Steel Properties (TCVN 5574:2018 Bảng 13, Bảng 14, Bảng 15)

| Steel Grade | Type | $R_s$ (MPa) | $R_{sc}$ (MPa) | $R_{sw}$ (MPa) | $E_s$ (MPa) |
|---|---|---|---|---|---|
| **CB240-T** | Smooth round ($\phi \le 10$, stirrups) | 210 | 210 | 170 | 200,000 |
| **CB300-V** | Ribbed longitudinal ($\phi \ge 10$) | 260 | 260 | 210 | 200,000 |
| **CB400-V** | High-strength ribbed | 350 | 350 | 280 | 200,000 |
| **CB500-V** | High-strength ribbed | 435 | 400* | 300 | 200,000 |

*\*Note on $R_{sc}$ for CB500-V:* Clause 8.1.2.3.1 limits compression steel design yield strength $R_{sc} \le 400\text{ MPa}$ due to maximum concrete ultimate compressive strain $\varepsilon_{b2} = 0.0035$ ($\varepsilon_{b2} E_s = 0.0035 \times 200000 = 700\text{ MPa}$, but limited to 400 MPa per Table 14).

#### C. Relative Boundary Depth of Compression Zone ($\xi_R$ & $\alpha_R$)
Per TCVN 5574:2018 Clause 8.1.2.3.1:
$$\xi_R = \frac{0.8}{1 + \frac{\varepsilon_{s,el}}{\varepsilon_{b,u}}} = \frac{0.8}{1 + \frac{R_s / E_s}{0.0035}} = \frac{0.8}{1 + \frac{R_s}{700}}$$
$$\alpha_R = \xi_R \cdot (1 - 0.5 \xi_R)$$

Pre-computed boundary values:
- **CB240-T:** $\xi_R = \frac{0.8}{1 + 210/700} = 0.6154$, $\alpha_R = 0.426$
- **CB300-V:** $\xi_R = \frac{0.8}{1 + 260/700} = 0.5833$, $\alpha_R = 0.413$
- **CB400-V:** $\xi_R = \frac{0.8}{1 + 350/700} = 0.5333$, $\alpha_R = 0.391$
- **CB500-V:** $\xi_R = \frac{0.8}{1 + 435/700} = 0.4934$, $\alpha_R = 0.372$

#### D. Flexural Reinforcement Design Algorithm (Clause 8.1.2.3)
Given: Section width $b$ (mm), total height $h$ (mm), bending moment $|M|$ (N·mm), concrete grade ($R_b$), steel grade ($R_s, R_{sc}$), concrete cover $c$, assumed bar centroid distance $a \approx c + d_{đai} + d_{thanh}/2$ (typically $35\text{ mm}$ for 1 layer, $55\text{ mm}$ for 2 layers).
1. Effective depth: $h_0 = h - a$.
2. Dimensionless moment ratio:
   $$\alpha_m = \frac{|M|}{R_b \cdot b \cdot h_0^2}$$
3. Case evaluation:
   - **Case 1: $\alpha_m \le \alpha_R$ (Singly reinforced - Cốt đơn):**
     $$\xi = 1 - \sqrt{1 - 2 \alpha_m}$$
     $$\zeta = 1 - 0.5 \xi = \frac{1 + \sqrt{1 - 2 \alpha_m}}{2}$$
     Required tension steel area:
     $$A_s = \frac{|M|}{R_s \cdot \zeta \cdot h_0} = \frac{\xi \cdot R_b \cdot b \cdot h_0}{R_s}$$
     Compression steel: $A_s' = 0$ (constructive only, minimum 2 bars to hang stirrups).
   - **Case 2: $\alpha_m > \alpha_R$ (Doubly reinforced - Cốt kép):**
     Compression zone concrete is overstressed. Set $\xi = \xi_R$.
     Maximum moment resisted by concrete:
     $$M_b = \alpha_R \cdot R_b \cdot b \cdot h_0^2$$
     Residual moment resisted by compression steel $A_s'$ (at distance $a'$ from compression face):
     $$\Delta M = |M| - M_b$$
     Required compression steel area:
     $$A_s' = \frac{\Delta M}{R_{sc} \cdot (h_0 - a')}$$
     Required tension steel area:
     $$A_s = \frac{\xi_R \cdot R_b \cdot b \cdot h_0}{R_s} + \frac{R_{sc}}{R_s} A_s'$$
     *Check:* Ensure $x = \xi_R h_0 \ge 2a'$. If $x < 2a'$, compression steel does not reach yield strength; take $A_s = \frac{|M|}{R_s (h_0 - a')}$.
4. Reinforcement ratio check (Clause 10.3.1.1):
   - Tension steel ratio: $\mu = \frac{A_s}{b \cdot h_0} \times 100\%$.
   - Minimum ratio: $\mu_{min} = 0.1\%$. If $A_s < 0.001 b h_0$, set $A_s = 0.001 b h_0$.
   - Maximum recommended ratio: $\mu_{max} = \xi_R \frac{R_b}{R_s} \times 100\%$ (typically $2.0\% \sim 2.5\%$). If $\mu > 3.0\%$, flag section as undersized.

#### E. Shear Reinforcement Design Algorithm (Clause 8.1.3.2 & 8.1.3.3)
Given: Section width $b$, height $h$, effective depth $h_0$, design shear force $Q$ (N), concrete tensile strength $R_{bt}$, stirrup steel strength $R_{sw}$, stirrup branch count $n_w$ (typically 2), trial stirrup diameter $d_{sw}$ (e.g. 8 mm or 10 mm).
1. Stirrup area per cross-section: $A_{sw} = n_w \cdot \frac{\pi d_{sw}^2}{4}$.
2. **Check 1: Inclined concrete compressive crush limit (Clause 8.1.3.2.1):**
   $$Q \le Q_{b,max} = \varphi_{b1} \cdot R_b \cdot b \cdot h_0 = 0.3 \cdot R_b \cdot b \cdot h_0$$
   If $Q > Q_{b,max}$, concrete Web crushing occurs; beam cross-section must be enlarged!
3. **Check 2: Concrete tensile shear capacity on inclined section (Clause 8.1.3.3):**
   Minimum shear carried by concrete alone:
   $$Q_{b,min} = \varphi_{b3} \cdot R_{bt} \cdot b \cdot h_0 = 0.5 \cdot R_{bt} \cdot b \cdot h_0$$
   If $Q \le Q_{b,min}$, stirrups are not needed by calculation; spacing is determined purely by detailing rules.
4. **Stirrup calculation when $Q > Q_{b,min}$:**
   The required linear shear tie capacity $q_{sw} = \frac{R_{sw} A_{sw}}{s}$:
   In inclined crack equilibrium, $Q \le Q_b + Q_{sw}$.
   With $Q_b = \frac{1.5 R_{bt} b h_0^2}{C}$ and $Q_{sw} = q_{sw} C_0$, the critical crack length minimizing resistance is:
   $$C_0 = \sqrt{\frac{1.5 R_{bt} b h_0^2}{q_{sw}}} \quad \text{clamped to } [h_0, 2h_0]$$
   Solving for required $q_{sw,req}$:
   $$q_{sw,req} = \frac{(Q - Q_{b,min})^2}{4 \cdot 1.5 R_{bt} b h_0^2} \ge \frac{Q - 0.75 R_{bt} b h_0}{2 h_0}$$
   Calculate required spacing:
   $$s_{calc} = \frac{R_{sw} \cdot A_{sw}}{q_{sw,req}}$$
5. **Constructive and Maximum Stirrup Spacing Limits (Clause 10.3.2.1):**
   - Maximum spacing to prevent unreinforced diagonal crack:
     $$s_{max} = \frac{\varphi_{b4} R_{bt} b h_0^2}{Q} = \frac{1.5 R_{bt} b h_0^2}{Q}$$
   - Support zone ($L/4$ dense zone):
     - If $h \le 450\text{ mm}$: $s \le \min(h/2, 150\text{ mm})$.
     - If $h > 450\text{ mm}$: $s \le \min(h/3, 500\text{ mm}, 150\text{ mm})$.
     - Practical standard spacing: $s_1 = 100\text{ mm}$ or $150\text{ mm}$.
   - Midspan zone ($L/2$ sparse zone):
     - If $h > 150\text{ mm}$: $s \le \min(3h/4, 500\text{ mm})$.
     - Practical standard spacing: $s_2 = 200\text{ mm}$.
   - Minimum stirrup ratio limit: $q_{sw} \ge 0.25 R_{bt} b \Rightarrow s \le \frac{R_{sw} A_{sw}}{0.25 R_{bt} b}$.

#### F. Optimal Rebar Selection Algorithm & Clear Spacing Verification (Clause 10.3.1.2)
1. **Selection Optimization Procedure:**
   - Available bar diameters: $\mathcal{D} = \{14, 16, 18, 20, 22, 25, 28, 32\}$ mm.
   - For a given section width $b$:
     - Max bars in 1 layer: $n_{max} = \lfloor \frac{b - 2(c + d_{sw}) + t_{min}}{d + t_{min}} \rfloor$.
     - Candidate combinations:
       - Single diameter: $n \phi d$ ($n \in [2, n_{max}]$).
       - Dual diameter (adjacent sizes only, e.g. $2\phi 20 + 1\phi 18$ or $2\phi 22 + 2\phi 20$): $n_1 \phi d_1 + n_2 \phi d_2$ with $|d_1 - d_2| \le 4\text{ mm}$ and symmetric layout.
   - Scoring / Objective Function:
     - Feasibility: $A_{s,prov} \ge A_{s,req}$.
     - Waste penalty: Minimizes over-design ratio $\frac{A_{s,prov} - A_{s,req}}{A_{s,req}}$ (ideal $0\% \le \Delta \le 12\%$).
     - Prefer standard diameters ($\phi 18, \phi 20, \phi 22$).
     - Favor single-layer solutions over double-layer for concrete pourability.
2. **Clear Spacing Validation ($t$):**
   - Transverse clear spacing between longitudinal bars:
     $$t = \frac{b - 2c - 2d_{sw} - \sum_{i=1}^n d_i}{n - 1}$$
   - Clause 10.3.1.2 Compliance Check:
     - **Bottom bars (positive moment in span):**
       $$t \ge \max(25\text{ mm}, d_{max})$$
     - **Top bars (negative moment at supports):**
       $$t \ge \max(30\text{ mm}, d_{max})$$
       (accounting for aggregate size $D_{max} \approx 20\text{ mm}$ during top casting).
     - **Vertical gap between 2 layers:**
       $$t_v \ge \max(25\text{ mm}, d_{max}) \quad (\text{typically default } 30\text{ mm} \sim 50\text{ mm})$$

### 2.3 Force Import Mechanisms

#### A. Excel (.xlsx) Import via ClosedXML
- Input Data Model in `HPRebar.Core`:
  ```csharp
  public sealed record BeamForceEnvelope
  {
      public string BeamMark { get; init; } = "";
      public int SpanIndex { get; init; }
      public double SpanLengthMm { get; init; }
      public double MomentSupportLeftKnm { get; init; }   // Negative M- top
      public double MomentMidSpanKnm { get; init; }        // Positive M+ bottom
      public double MomentSupportRightKnm { get; init; }  // Negative M- top
      public double ShearSupportLeftKn { get; init; }     // Max Q left
      public double ShearMidSpanKn { get; init; }         // Max Q mid
      public double ShearSupportRightKn { get; init; }    // Max Q right
  }
  ```
- Two Excel sheet formats supported:
  1. *Format 1 (Summary Beam Schedule Sheet):* Columns: `Beam Mark`, `Story`, `b (mm)`, `h (mm)`, `Position` (Gối trái / Nhịp / Gối phải), `M+ (kNm)`, `M- (kNm)`, `Q (kN)`.
  2. *Format 2 (ETABS Raw Element Forces Export):* Columns: `Story`, `Beam`, `Station`, `Output Case`, `P`, `V2`, `M3`. Parser automatically groups by beam and station ($x=0, x=L/2, x=L$) and finds max envelopes across load combinations (`COMB*`, `BAO`, `ENV`).
- Architecture:
  `ClosedXmlBeamForceReader` resides in `HPRebar/BeamRebar/Service/` (which references `ClosedXML`), reads the workbook, converts cells into `IReadOnlyList<BeamForceEnvelope>`, and passes them to the pure `HPRebar.Core` design engine.

#### B. ETABS 22 Integration via `HPEtabs`
- Connection options:
  1. *Named Pipe IPC (Recommended):* Connect to `\\.\pipe\hpetabs-mcp-22` using JSON-RPC client. Call tool `get_frame_forces` with `caseOrCombo = "BAO"` and `frameNames = [beamMark]`. Zero COM dependencies inside Revit process!
  2. *Direct Out-of-Process COM:* Query `cOAPI` / `SapModel` using `sapModel.Results.FrameForce(...)`.
- Coordinate & Span Mapping:
  - Match Revit continuous beam spans to ETABS Frame Objects by matching:
    - Beam Mark / Comments parameter $\leftrightarrow$ ETABS Frame Label/Name.
    - Start/End coordinates $(X_1, Y_1, Z_1) \rightarrow (X_2, Y_2, Z_2)$ with tolerance $\pm 50\text{ mm}$.
  - Extract Station Forces:
    - Station $0.0\text{ m}$: $M_{left} = M_3$, $Q_{left} = |V_2|$.
    - Station $L/2$: $M_{mid} = M_3$, $Q_{mid} = |V_2|$.
    - Station $L$: $M_{right} = M_3$, $Q_{right} = |V_2|$.

---

## 3. Caveats
1. **Dynamic Load Combinations:** The current formulation focuses on the critical envelope combinations ($M_{max}, M_{min}, Q_{max}$) derived from dead load, live load, and wind/seismic combinations (TCVN 2737:2023 / TCVN 9386:2012). Torsion ($T$) combined with flexure and shear ($M + Q + T$) is not included in the baseline rectangular beam formula and can be addressed in future expansions.
2. **Deflection & Crack Width (SLS):** The primary focus of R4 is Ultimate Limit State (ULS) flexural and shear reinforcement calculation ($A_s, A_s', A_{sw}/s$) and detailing rules (clause 10.3). Crack width verification ($\Delta a_{crc} \le 0.3\text{ mm}$) and deflection verification ($f \le L/200$) can be layered on top as an advisory check.
3. **ClosedXML Dependency Boundary:** `HPRebar.Core.csproj` must not reference `ClosedXML`. `ClosedXML` is solely used in `HPRebar` (add-in project). Any data feeding into `HPRebar.Core` must pass via pure BCL collections/DTOs.

---

## 4. Conclusion

1. **Architecture Viability:** The TCVN 5574:2018 calculation and rebar optimization engine can be cleanly housed in `HPRebar.Core` without violating Rule D1. It will take geometric beam dimensions from `BeamSpan`, material grades (`ConcreteGrade`, `SteelGrade`), and force inputs (`BeamForceEnvelope`), outputting fully detailed `BeamMainBarSpec`, `BeamAdditionalBarSpec`, and `BeamStirrupSpec`.
2. **End-to-End Pipeline Cohesion:**
   - Input: Excel `.xlsx` (via `ClosedXmlBeamForceReader`) OR ETABS 22 (via `HPEtabs` named pipe / OAPI).
   - Core Processing: `TcvnBeamCalculator` calculates $A_s, A_s', A_{sw}/s$, runs `BeamRebarOptimizer` to select optimal bar sizes, and validates clear spacing $t \ge \max(25, d)$.
   - UI Integration: Populates `BeamRebarSession`, updates 2D preview canvases and 3D Helix viewport in real-time.
   - Revit Execution: User clicks "Create Rebar", `RebarCreationService` stages 5 transactions to generate native 3D `Rebar` elements.
3. **Design Proposal for `HPRebar.Core` Namespace Structure:**
   - `HPRebar.Core/Tcvn5574/Materials/ConcreteMaterial.cs` (B15–B40 properties, $R_b, R_{bt}, E_b$)
   - `HPRebar.Core/Tcvn5574/Materials/SteelMaterial.cs` (CB240-T, CB300-V, CB400-V, CB500-V, $R_s, R_{sc}, R_{sw}, \xi_R, \alpha_R$)
   - `HPRebar.Core/Tcvn5574/Calculators/TcvnBeamFlexureCalculator.cs` (Singly/doubly reinforced rectangular beam design)
   - `HPRebar.Core/Tcvn5574/Calculators/TcvnBeamShearCalculator.cs` (Concrete shear capacity $Q_b$, stirrup spacing $s$, crushing check)
   - `HPRebar.Core/Tcvn5574/Calculators/TcvnBeamRebarOptimizer.cs` (Optimal diameter selection, spacing validation $t \ge \max(25, d)$)
   - `HPRebar.Core/Tcvn5574/Models/BeamForceEnvelope.cs` (Station, $M^+, M^-, Q$)

---

## 5. Verification Method

### 5.1 Verification Commands
- Run pure core tests (no Revit, runs in 3.6s):
  ```powershell
  dotnet test HPRebar/HPRebar.Core.Tests
  ```
- Build full add-in with multi-version configuration:
  ```powershell
  dotnet build HPRebar/HPRebar.slnx -c Debug.R26
  ```

### 5.2 Proposed Test Suite for TCVN 5574:2018 in `HPRebar.Core.Tests`
Create test class `HPRebar.Core.Tests/Tcvn5574/TcvnBeamCalculatorTests.cs`:
1. `ConcreteMaterial_Properties_MatchTcvn5574Table6And7`: Verify $R_b, R_{bt}, E_b$ for B15, B20, B25, B30, B35, B40.
2. `SteelMaterial_BoundaryValues_MatchTcvn5574Formula`: Verify $\xi_R = 0.533$ and $\alpha_R = 0.391$ for CB400-V.
3. `FlexuralDesign_SinglyReinforced_CalculatesAccurateAs`:
   - Beam $b=300\text{ mm}, h=600\text{ mm}, a=40\text{ mm} \Rightarrow h_0=560\text{ mm}$. Concrete B25 ($R_b=14.5\text{ MPa}$), Steel CB400-V ($R_s=350\text{ MPa}$).
   - For $M = 150\text{ kN}\cdot\text{m}$:
     $\alpha_m = \frac{150 \times 10^6}{14.5 \times 300 \times 560^2} = 0.1099 < \alpha_R (0.391)$.
     $\zeta = \frac{1 + \sqrt{1 - 2(0.1099)}}{2} = 0.9419$.
     $A_s = \frac{150 \times 10^6}{350 \times 0.9419 \times 560} = 812\text{ mm}^2$.
     Assert calculated $A_s \approx 812\text{ mm}^2 \pm 2\text{ mm}^2$.
4. `FlexuralDesign_DoublyReinforced_WhenAlphaMExceedsAlphaR_CalculatesCompressionSteel`:
   - For $M = 600\text{ kN}\cdot\text{m}$, $\alpha_m = 0.4397 > \alpha_R (0.391)$.
   - Verify $A_s' > 0$ and $A_s$ properly includes tension component balancing $A_s'$.
5. `ShearDesign_CrushLimit_ThrowsWhenShearExceedsLimit`:
   - $Q > 0.3 R_b b h_0 \Rightarrow$ throws validation error with message indicating concrete web crush failure.
6. `ShearDesign_ModerateShear_CalculatesCorrectStirrupSpacing`:
   - Verify calculated spacing matches $s_{calc} = \frac{R_{sw} A_{sw}}{q_{sw,req}}$ clamped to $s_{max}$ and constructive limits.
7. `ClearSpacingValidation_WhenBarsAreCrowded_FailsWithDescriptiveMessage`:
   - Beam width $b=200\text{ mm}$, cover $c=25\text{ mm}$, stirrup $\phi 8$, 4 bars $\phi 25$:
     Clear spacing $t = \frac{200 - 50 - 16 - 100}{3} = 11.33\text{ mm} < 25\text{ mm} \Rightarrow$ Returns non-compliant result.

### 5.3 Invalidation Conditions
- Any introduction of `Autodesk.Revit.*` in `HPRebar.Core` immediately invalidates the solution (violates Rule D1).
- Any class exceeding 300 lines or method exceeding 50 lines violates Clean Code Governance.
- Hardcoding calculation numbers without citing TCVN 5574:2018 clauses or tables.
