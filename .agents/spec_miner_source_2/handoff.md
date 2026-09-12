# Handoff Report: Specification Mining for Foundation Rebar (R03_FoundationRebar & Phương Án A)

**Agent**: `spec_miner_source_2`  
**Working Directory**: `F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_2`  
**Deliverable**: `handoff.md`  
**Handoff Type**: Hard (Task Complete)  
**Authoritative Request**: `ORIGINAL_REQUEST.md` (§ Follow-up — 2026-09-07T15:37:30Z)  
**Target Solution**: `HPRebar/HPRebar.slnx` (Revit 2023–2027, .NET 8 / .NET Framework 4.8 / .NET 10)  

---

## 1. Observation

Direct observations from codebase inspection, repository file catalogs, legacy patterns, and structural domain engineering rules:

### 1.1 Source Context & Existing Target Architecture
1. **Target Solution Structure**:
   - `HPRebar/HPRebar.Core/` (`netstandard2.0`): Pure mathematical algorithms and geometry calculators, 100% free of `Autodesk.Revit.*` references. Existing modules: `ColumnRebar/` and `BeamRebar/`.
   - `HPRebar/HPRebar.Core.Tests/` (`net8.0` with xUnit v3): 241 baseline tests currently passing (102 ColumnRebar + 139 BeamRebar).
   - `HPRebar/HPRebar/` (Multi-targeting .NET 8, .NET 4.8, .NET 10): Feature folders `Column Rebar/` and `Beam Rebar/`.
   - Ribbon entry in `Application.cs` already hosts "Column Rebar" and "Beam Rebar" buttons on panel `"Rebar"`.

2. **Legacy R03_FoundationRebar Context**:
   - Developed as part of the monolithic suite `RebarAddin-master` alongside `R01_ColumnsRebar` and `R02_BeamsRebar` (targeting Revit 2019–2021 on .NET Framework 4.7.2/4.8).
   - File organization in legacy module:
     - `Command/`: `FoundationRebarCmd.cs` (IExternalCommand entry), `ModifyFoundationCmd.cs`.
     - `Model/`: `FoundationModel.cs`, `FoundationGeometryModel.cs`, `MeshRebarModel.cs`, `RebarModel.cs`, `HookModel.cs`, `LayerModel.cs`.
     - `ViewModel/`: `FoundationRebarViewModel.cs`, `FoundationGeometryViewModel.cs`, `FoundationSettingViewModel.cs`.
     - `View/`: `FoundationRebarView.xaml`, `FoundationGeometryView.xaml`, `FoundationSettingView.xaml`.
     - `Library/`: `Filter/FoundationSelectionFilter.cs`, `Geometry/SolidFace.cs`, `Geometry/ProcessInfoFoundation.cs`, `Create/CreateRebar.cs`, `Error/ErrorFoundation.cs`, `Draw/DrawMainCanvas.cs`.

3. **Domain Scope for 'Phương án A'**:
   - Target Revit element: `Floor` (`BuiltInCategory.OST_Floors`), representing mat/slab foundations (bản móng / móng bè / đài móng dạng bản).
   - Rebar system: 2 mats (Bottom Mat & Top Mat), each mat containing 2 orthogonal directions (Direction X - Primary, Direction Y - Secondary).
   - Hook anchorage: Straight or 90° standard hooks bending into the foundation core (+Z for bottom mat, -Z for top mat).

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Execution Pipeline | `FoundationRebarCommand` | External command entry point; handles user interactive selection of Floor, runs pre-flight validation, and launches orchestrator. | `ExternalCommandData`, user selection pick | `Result.Succeeded` or `Cancelled` | Aborts cleanly on Escape or selection of non-Floor elements. | Legacy `Command/FoundationRebarCmd.cs` |
| 2 | Selection Filter | `FoundationSelectionFilter` | Restricts interactive viewport selection exclusively to `Floor` elements (`BuiltInCategory.OST_Floors`). | `Reference`, `Element` | `bool` (`true` if element is `Floor`) | Ignores/rejects non-Floor elements during `PickObject`. | Legacy `Library/Filter/FoundationSelectionFilter.cs` |
| 3 | Geometric Validation | Horizontal Floor Verification | Checks that the selected floor is strictly horizontal (top and bottom faces have normals collinear with $\pm \vec{Z}$). | `Element` floor | `ValidationResult` (is valid, error message) | Rejects sloped or warped floors with explicit error message. | Legacy `Library/Error/ErrorFoundation.cs` |
| 4 | Geometry Extraction | `FoundationSolidFaceReader` | Extracts non-zero volume `Solid`, identifies Top PlanarFace (normal $(0,0,1)$), Bottom PlanarFace (normal $(0,0,-1)$), vertical boundary faces, and thickness $H$. | `Element` floor | `Solid`, Top/Bottom `PlanarFace`, thickness $H$, boundary edges | Throws if solid is empty, null, or has non-horizontal faces. | Legacy `Library/Geometry/SolidFace.cs` |
| 5 | Orientation Discovery | Local Axis Alignment | Determines primary orientation vector $\vec{U}_X$ from dominant boundary edge or oriented bounding box; computes orthogonal $\vec{U}_Y = \vec{U}_Z \times \vec{U}_X$. | Top face boundary loops | $\vec{U}_X$, $\vec{U}_Y$, local origin $\mathcal{O}$ | Falls back to Project North $(1,0,0)$ if boundary has zero length. | Legacy `Library/Geometry/ProcessInfoFoundation.cs` |
| 6 | Boundary Modeling | Effective Rebar Bounds | Offsets outer boundary polygon inward by side cover $c_{side}$ to establish the legal layout zone $[u_{min} + c_{side}, u_{max} - c_{side}] \times [v_{min} + c_{side}, v_{max} - c_{side}]$. | $L, W$, side cover $c_{side}$ | Effective span lengths $L_{eff}, W_{eff}$ | Rejects if $L \le 2 c_{side}$ or $W \le 2 c_{side}$. | Pure domain requirement R1 |
| 7 | Bottom Mat X Rebar | Bottom Primary Direction | Calculates quantity, spacing, and 3D curve coordinates for bottom bars running parallel to $\vec{U}_X$, distributed across $\vec{U}_Y$. | $L, W, s_{bx}, \Phi_{bx}, c_{side}, c_{bottom}$ | `IReadOnlyList<BarPolyline>` for Layer 1 | Throws if spacing $s_{bx} \le 0$ or bar count $> 1002$. | Legacy `Model/MeshRebarModel.cs` |
| 8 | Bottom Mat Y Rebar | Bottom Secondary Direction | Calculates quantity, spacing, and 3D curve coordinates for bottom bars running parallel to $\vec{U}_Y$, distributed across $\vec{U}_X$. Centerline sits on top of X bars. | $L, W, s_{by}, \Phi_{by}, c_{side}, c_{bottom}, \Phi_{bx}$ | `IReadOnlyList<BarPolyline>` for Layer 2 | Throws if spacing $s_{by} \le 0$ or bar count $> 1002$. | Legacy `Model/MeshRebarModel.cs` |
| 9 | Top Mat X Rebar | Top Primary Direction | Calculates quantity, spacing, and 3D curve coordinates for top bars running parallel to $\vec{U}_X$, distributed across $\vec{U}_Y$ at elevation $Z_{top} - c_{top} - \Phi_{tx}/2$. | $L, W, s_{tx}, \Phi_{tx}, c_{side}, c_{top}$ | `IReadOnlyList<BarPolyline>` for Layer 4 | Omitted if `EnableTopMat` is `false`. | Legacy `Model/MeshRebarModel.cs` |
| 10 | Top Mat Y Rebar | Top Secondary Direction | Calculates quantity, spacing, and 3D curve coordinates for top bars running parallel to $\vec{U}_Y$, distributed across $\vec{U}_X$ at elevation $Z_{top} - c_{top} - \Phi_{tx} - \Phi_{ty}/2$. | $L, W, s_{ty}, \Phi_{ty}, c_{side}, c_{top}, \Phi_{tx}$ | `IReadOnlyList<BarPolyline>` for Layer 3 | Omitted if `EnableTopMat` is `false`. | Legacy `Model/MeshRebarModel.cs` |
| 11 | Anchorage Hooks | 90° Upward/Downward Bends | Generates 90° vertical hook legs at both ends of each bar. Bottom bars bend UPWARDS (+Z); top bars bend DOWNWARDS (-Z). | Hook type, requested length $L_{h}$, thickness $H$, covers | 4-point `Polyline3` per hooked bar | Clamps hook length to $H - c_{bottom} - c_{top} - \Phi$ to avoid punching through opposite face. | Legacy `Model/HookModel.cs` |
| 12 | Remainder Centering | Equal Edge Margin ($\delta$) | Centers bar distribution across the distribution span so left and right margin offsets are equal: $\delta = (L_{eff} - 2 o_{start} - N \cdot s) / 2$. | $L_{eff}$, spacing $s$, start offset $o_{start}$ | Offset delta $\delta$, start coordinate | Guarantees symmetrical rebar layout. | Standard structural detailing rule |
| 13 | Clearance Guardrail | Thickness vs. Layer Check | Validates that slab thickness $H \ge c_{bottom} + c_{top} + \Phi_{bx} + \Phi_{by} + \Phi_{tx} + \Phi_{ty}$. | Thickness $H$, covers, diameters | `ValidationResult` (passes or fails) | Blocks generation with diagnostic error if thickness is insufficient. | Core requirement R2 |
| 14 | Revit Creation Engine | `Rebar.CreateFromCurves` | Creates native Revit `Rebar` elements from planar polyline curve sets using current Revit API. | `Document`, `Floor`, `IList<Curve>`, `RebarBarType` | `Rebar` instances added to model | Rolls back transaction on failure; logs warning if curve too short. | Legacy `Library/Create/CreateRebar.cs` |
| 15 | Multi-Version ID | `ElementId` Compatibility | Gates `.Value` (Revit 2024+) vs `.IntegerValue` (Revit 2023) using `// Multi-version: ElementId`. | `ElementId` | `long` / `int` | Prevents compilation errors across R23–R27. | Target architecture convention |
| 16 | Atomic Orchestration | `FoundationRebarOrchestrator` | Sole owner of `TransactionGroup("Foundation Rebar")`. Manages UI modal dialog and commits/rolls back. | Session snapshot, UI inputs | Succeeded / Rolled back | 100% clean rollback on user cancel or unhandled exception. | Target architecture convention |
| 17 | WPF MVVM Interface | Dynamic Themed Dialog | Tabbed UI: Geometry (dimensions, thickness, level), Bottom Mat (diameters, spacings, hooks), Top Mat (toggle, diameters, spacings, hooks), Settings (covers, offsets). | `FoundationRebarSession` | Configured `FoundationRebarSpec` | Disables OK button when validation fails. | WPF MVVM convention |
| 18 | Ribbon Registration | Panel Rebar Button | Adds "Foundation Rebar" push button with 16px and 32px icons to "Rebar" ribbon panel in `Application.cs`. | Ribbon panel `Rebar` | Registered `PushButton` | Caught during startup if icon URI invalid. | `Application.cs` |

---

## 3. Edge Cases Discovered & Algorithmic Solutions

| # | Feature | Input / Scenario | Observed / Anticipated Behavior | Algorithmic Handling in Target |
|---|---------|------------------|---------------------------------|--------------------------------|
| 1 | Rotated Foundation | Floor is rotated at an arbitrary angle in plan (e.g. 30°, 45°, or skewed). | Standard Axis-Aligned Bounding Box (AABB) inflates, creating erroneous dimensions and rebar protruding outside the slab. | Extract the dominant edge vector as local axis $\vec{U}_X$. Project all boundary vertices onto $(\vec{U}_X, \vec{U}_Y)$ to obtain the exact Oriented Bounding Box (OBB) dimensions. |
| 2 | Insufficient Thickness | Foundation thickness $H$ is too thin for 2 mats (e.g., $H = 150$ mm, covers = 50 mm top & bottom, $\sum \Phi = 60$ mm $\to$ total needed = 160 mm). | Top and bottom bars collide or invert in Z elevation ($Z_{top} < Z_{bottom}$). | `FoundationValidationCalculator` rejects input with code `ErrInsufficientThickness`: *"Slab thickness (150 mm) is less than required minimum clearance (160 mm)"*. |
| 3 | Spacing Divisibility | Span length is exactly divisible by spacing ($L_{eff} \pmod s = 0$) vs. has remainder ($L_{eff} \pmod s \ne 0$). | Off-by-one errors or uneven edge margins if simple integer division is used. | Compute $N_{intervals} = \lfloor (L_{eff} - 2 o_{start}) / s \rfloor$, $n = N_{intervals} + 1$, and center the remainder: $\delta = (L_{eff} - 2 o_{start} - N_{intervals} \cdot s) / 2$. |
| 4 | Zero or Negative Spacing | User enters $s \le 0$ or spacing so small that $L_{eff} / s > 1002$. | Division by zero crashes calculator; bar count $> 1002$ crashes Revit API `Rebar` layout. | Core calculator throws `ArgumentOutOfRangeException` when $s \le 0$ or $(L_{eff} / s) > 1002$. UI validates numeric range $[50, 1000]$ mm. |
| 5 | Hook Punching | User requests hook length $L_h = 300$ mm in a slab with thickness $H = 250$ mm. | Hook leg extends beyond the opposite face cover and punches through concrete exterior. | Clamp hook length dynamically: $L_{h, actual} = \min(L_h, H - c_{bottom} - c_{top} - 10.0)$. |
| 6 | Sub-Millimeter Curve | Hook geometry or corner fillet creates segment length $< 0.8$ mm. | Revit throws `ArgumentException: Curve is too short` (Application.ShortCurveTolerance is ~0.78 mm). | Run `Polyline3.Simplify(1.0)` to merge any vertices closer than 1.0 mm before generating Revit `Line` elements. |
| 7 | Non-Planar Polyline | Minor rounding error causes out-of-plane variation in $Z$ or $Y$. | Revit throws `ArgumentException: Curves must be planar`. | Enforce exact planar coordinate assignment: all points of an X-bar must have identical $Y = v_i$, and hook points must strictly vary only in $X$ and $Z$. |
| 8 | Single Mat Foundation | User disables Top Mat (`EnableTopMat = false`). | Code must generate only Bottom Mat (2 directions) without attempting to calculate Top Mat. | Core calculator skips Top X and Top Y passes completely; checks thickness only against bottom mat requirements: $H \ge c_{bottom} + c_{top} + \Phi_{bx} + \Phi_{by}$. |
| 9 | High Bar Count (>1002) | Huge mat foundation (e.g. $60 \text{ m} \times 60 \text{ m}$ @ 100 mm spacing $\to 601$ bars per direction). | Under Revit's 1002 limit, but total bars across 4 layers = $2404$ bars. | Process bar generation with transaction batching or single robust transaction with progress reporting. |
| 10 | Culture / Locale Crash | Machine locale uses comma `,` as decimal separator (e.g. `1200,5` mm). | `double.Parse(text)` throws `FormatException` or parses scale incorrectly. | Use `CultureInfo.InvariantCulture` for all numeric conversions; use `RevitUnits.MmToFt()` without string roundtripping. |

---

## 3. Detailed Technical Analysis

### 3.1 Core Domain Logic: Foundation Geometry Calculation

#### 1. Target Element Extraction
The foundation is modeled as a Revit `Floor` (`OST_Floors`). Geometry extraction proceeds through the following steps:
1. `element.get_Geometry(new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine })`.
2. Extract all `Solid` objects with `Volume > 1.0e-6`. Ensure exactly one valid solid is found.
3. Identify horizontal planar faces by checking normal vector alignment with the vertical axis:
   - $\text{FaceNormal} \cdot (0, 0, 1) > 1 - 10^{-6} \implies$ **Top Face** ($F_{top}$). Highest $Z$ is $Z_{top}$.
   - $\text{FaceNormal} \cdot (0, 0, -1) > 1 - 10^{-6} \implies$ **Bottom Face** ($F_{bottom}$). Lowest $Z$ is $Z_{bottom}$.
4. Foundation thickness:
   $$H = Z_{top} - Z_{bottom}$$

#### 2. Local Coordinate System & Oriented Bounding Box (OBB)
To handle foundations at any arbitrary plan rotation angle $\theta$:
1. Extract outer boundary loop curves of $F_{top}$.
2. Identify the longest straight boundary edge $E_{max}$ with start point $P_0$ and end point $P_1$.
3. Define the local orthonormal coordinate basis:
   $$\vec{U}_X = \frac{P_1 - P_0}{\|P_1 - P_0\|}$$
   $$\vec{U}_Z = (0, 0, 1)$$
   $$\vec{U}_Y = \vec{U}_Z \times \vec{U}_X = (-U_{X,y}, U_{X,x}, 0)$$
4. Transform all boundary vertices $V_k$ into local 2D coordinates $(u_k, v_k)$:
   $$u_k = (V_k - P_0) \cdot \vec{U}_X, \quad v_k = (V_k - P_0) \cdot \vec{U}_Y$$
5. Compute local dimensions:
   $$u_{min} = \min(u_k), \quad u_{max} = \max(u_k) \implies L = u_{max} - u_{min}$$
   $$v_{min} = \min(v_k), \quad v_{max} = \max(v_k) \implies W = v_{max} - v_{min}$$
6. Set Local Coordinate Origin:
   $$\mathcal{O} = P_0 + u_{min}\vec{U}_X + v_{min}\vec{U}_Y + Z_{bottom}\vec{U}_Z$$
   In this local coordinate system, the foundation volume occupies $[0, L] \times [0, W] \times [0, H]$.

---

### 3.2 Mesh Rebar Distribution (Lưới Thép 2 Lớp 2 Phương)

#### 1. Effective Distribution Boundary
After subtracting side cover $c_{side}$:
- Range along X: $[c_{side}, L - c_{side}]$, effective length $L_{eff} = L - 2 c_{side}$.
- Range along Y: $[c_{side}, W - c_{side}]$, effective width $W_{eff} = W - 2 c_{side}$.

#### 2. Vertical Stacking Hierarchy (Thứ Tự Lớp Thép)
Physical reinforcement bars cannot occupy the same elevation. The 4 layers are stacked vertically in local $Z$ coordinates (relative to $Z = 0$ at bottom soffit):

| Layer Index | Layer Name | Running Direction | Spacing Axis | Centerline Elevation $Z_{local}$ | Physical Role |
|:---:|:---:|:---:|:---:|:---:|:---|
| **Layer 1** | Bottom Mat X | $\vec{U}_X$ | Along $\vec{U}_Y$ | $Z_1 = c_{bottom} + \frac{\Phi_{bx}}{2}$ | Bottom-most outer layer |
| **Layer 2** | Bottom Mat Y | $\vec{U}_Y$ | Along $\vec{U}_X$ | $Z_2 = c_{bottom} + \Phi_{bx} + \frac{\Phi_{by}}{2}$ | Bottom secondary layer (rests on Layer 1) |
| **Layer 3** | Top Mat Y | $\vec{U}_Y$ | Along $\vec{U}_X$ | $Z_3 = H - c_{top} - \Phi_{tx} - \frac{\Phi_{ty}}{2}$ | Top secondary layer (hung under Layer 4) |
| **Layer 4** | Top Mat X | $\vec{U}_X$ | Along $\vec{U}_Y$ | $Z_4 = H - c_{top} - \frac{\Phi_{tx}}{2}$ | Top-most outer layer |

*Note on clear vertical spacing*: Between Layer 2 and Layer 3, the available clear height is:
$$H_{clear} = Z_3 - Z_2 - \frac{\Phi_{by} + \Phi_{ty}}{2} = H - (c_{bottom} + c_{top} + \Phi_{bx} + \Phi_{by} + \Phi_{tx} + \Phi_{ty})$$
The pre-flight validator must enforce $H_{clear} > 0$.

#### 3. Mathematical Distribution Formulas

##### Direction X Bars (Thanh phương X):
- Run along X: from $u_{start} = c_{side}$ to $u_{end} = L - c_{side}$.
- Distributed along Y over $W_{eff}$:
  - Start offset: $o_{start}$ (default $s_x / 2$ or 50 mm).
  - Number of intervals: $N_y = \lfloor \frac{W_{eff} - 2 o_{start}}{s_x} \rfloor$
  - Number of bars: $n_x = N_y + 1$
  - Centering offset: $\delta_y = \frac{(W_{eff} - 2 o_{start}) - (N_y \cdot s_x)}{2}$
  - Discrete positions:
    $$v_i = c_{side} + o_{start} + \delta_y + i \cdot s_x \quad (i = 0, \dots, n_x - 1)$$

##### Direction Y Bars (Thanh phương Y):
- Run along Y: from $v_{start} = c_{side}$ to $v_{end} = W - c_{side}$.
- Distributed along X over $L_{eff}$:
  - Start offset: $o_{start}$ (default $s_y / 2$ or 50 mm).
  - Number of intervals: $N_x = \lfloor \frac{L_{eff} - 2 o_{start}}{s_y} \rfloor$
  - Number of bars: $n_y = N_x + 1$
  - Centering offset: $\delta_x = \frac{(L_{eff} - 2 o_{start}) - (N_x \cdot s_y)}{2}$
  - Discrete positions:
    $$u_j = c_{side} + o_{start} + \delta_x + j \cdot s_y \quad (j = 0, \dots, n_y - 1)$$

#### 4. Hook / Anchorage Geometry (Móc Neo)
1. **Straight Bars (`HookType.None`)**:
   - Single segment line between endpoints:
     - X-Bar: $(c_{side}, v_i, Z) \to (L - c_{side}, v_i, Z)$
     - Y-Bar: $(u_j, c_{side}, Z) \to (u_j, W - c_{side}, Z)$
2. **90° Hook Bars (`HookType.Hook90`)**:
   - Bottom Mat (Hooks bend **UPWARDS** $+Z$):
     - Effective hook length: $L_{hb} = \min(L_{h\_spec}, H - c_{bottom} - c_{top} - \Phi - 10.0)$.
     - 4 polyline vertices for X-bar at $v = v_i$:
       $$P_1 = (c_{side}, v_i, Z_1 + L_{hb})$$
       $$P_2 = (c_{side}, v_i, Z_1)$$
       $$P_3 = (L - c_{side}, v_i, Z_1)$$
       $$P_4 = (L - c_{side}, v_i, Z_1 + L_{hb})$$
   - Top Mat (Hooks bend **DOWNWARDS** $-Z$):
     - Effective hook length: $L_{ht} = \min(L_{h\_spec}, H - c_{bottom} - c_{top} - \Phi - 10.0)$.
     - 4 polyline vertices for X-bar at $v = v_i$:
       $$P_1 = (c_{side}, v_i, Z_4 - L_{ht})$$
       $$P_2 = (c_{side}, v_i, Z_4)$$
       $$P_3 = (L - c_{side}, v_i, Z_4)$$
       $$P_4 = (L - c_{side}, v_i, Z_4 - L_{ht})$$

---

### 3.3 Legacy / Obsolete / Bad Patterns in R03_FoundationRebar

| Legacy Antipattern | Where Found in Legacy Code | Why It Is Harmful | Target Production Replacement (`HPRebar`) |
|---|---|---|---|
| **Tight Coupling to Revit API** | `Model/MeshRebarModel.cs`, `Model/FoundationGeometryModel.cs` | Directly instantiating `XYZ`, `Curve`, `Document` in domain models prevents all automated unit testing. | Pure C# records (`Point3`, `Polyline3`, `FoundationGeometrySnapshot`, `FoundationRebarSpec`) in `HPRebar.Core` (`netstandard2.0`). |
| **Deprecated `DisplayUnitType`** | `Library/Geometry/ProcessInfoFoundation.cs` | `DisplayUnitType.DUT_MILLIMETERS` was deprecated in Revit 2021 and removed in Revit 2022+. Crashes modern builds. | Use `UnitTypeId.Millimeters` and `RevitUnits.MmToFt` / `FtToMm`. |
| **Obsolete `CreateFromCurves` signatures** | `Library/Create/CreateRebar.cs` | Legacy calls passing obsolete parameters or using `CreateFreeForm` fail in Revit 2025/2026. | Standard `Rebar.CreateFromCurves` with `RebarStyle.Standard`, explicit normal, and shape-matching flags. |
| **Unsafe `ElementId` integer casting** | Throughout legacy codebase (`(int)elem.Id.IntegerValue`) | In Revit 2024+, `ElementId` value changed from `int` to `long` (`Int64`). Direct integer casting causes truncation or compiler warnings. | Use multi-version conditional compilation tagged `// Multi-version: ElementId` (`#if REVIT2024_OR_GREATER ... id.Value ... #else ... id.IntegerValue ... #endif`). |
| **Culture-Sensitive String Parsing** | `ViewModel/FoundationRebarViewModel.cs` | `double.Parse(txtSpacing.Text)` crashes on European/Vietnamese Windows locales where `,` is decimal separator. | Parse with `CultureInfo.InvariantCulture` or numeric `double.TryParse(..., NumberStyles.Float, CultureInfo.InvariantCulture, ...)`. |
| **Missing `TransactionGroup` Rollback** | `Command/FoundationRebarCmd.cs` | Used isolated `Transaction` calls. If creation failed halfway or user cancelled, orphan rebars were left in the project. | `FoundationRebarOrchestrator` owns a single atomic `TransactionGroup("Foundation Rebar")`. Commits only on complete success; cleanly rolls back on any error or cancel. |
| **No Failure Preprocessor** | `Library/Create/CreateRebar.cs` | Revit popup warnings (e.g. slight cover collision or rebar shape matching warnings) stall automated execution. | Attach `RebarFailureHandling.Apply(transaction)` using `IFailuresPreprocessor` to automatically swallow warnings. |
| **Hardcoded UI Colors & Styles** | `View/FoundationRebarView.xaml` | Hardcoded `Background="White"`, `Foreground="Black"` renders UI unreadable in Revit Dark Theme. | Bind all styling to dynamic theme keys (`{DynamicResource Brush.TextPrimary}`, `{DynamicResource Brush.SurfaceBackground}`, etc.). |
| **No Clearance / Clash Pre-Flight** | `Library/Error/ErrorFoundation.cs` | Only checked if element was not null. Did not check if $H < c_{bottom} + c_{top} + \sum \Phi$, causing inverted rebar geometries. | `FoundationValidationCalculator` executes rigorous pre-flight arithmetic validation before any Revit transaction opens. |

---

### 3.4 Target Specification Mapping for 'Phương án A'

#### 1. Architecture Layering
```
HPRebar/
├── HPRebar.Core/
│   └── FoundationRebar/
│       ├── Models/
│       │   ├── FoundationGeometrySnapshot.cs   (Dimensions, elevations, origin, local axes)
│       │   ├── FoundationRebarSpec.cs          (Diameters, spacings, covers, hooks, toggles)
│       │   ├── FoundationLayerSpec.cs          (Diameter, Spacing, HookType, HookLength)
│       │   ├── FoundationMeshLayer.cs          (Enum: BottomX, BottomY, TopX, TopY)
│       │   ├── FoundationHookType.cs           (Enum: Straight, Hook90, Hook135, Hook180)
│       │   └── FoundationValidationResult.cs   (IsValid, ErrorCode, ErrorMessage)
│       └── Calculators/
│           ├── FoundationBoundaryCalculator.cs  (Effective spans, margins, offsets)
│           ├── FoundationMeshCalculator.cs      (Pure 3D polyline generation for 4 layers)
│           └── FoundationValidationCalculator.cs(Pre-flight validation rules)
│
├── HPRebar.Core.Tests/
│   └── FoundationRebar/
│       ├── FoundationBoundaryCalculatorTests.cs
│       ├── FoundationMeshCalculatorTests.cs
│       └── FoundationValidationCalculatorTests.cs
│
└── HPRebar/
    ├── Foundation Rebar/
    │   ├── FoundationRebarCommand.cs           (ExternalCommand entry point)
    │   ├── FoundationSelectionFilter.cs         (ISelectionFilter for Floor)
    │   ├── FoundationSolidFaceReader.cs         (Extracts Solid, Top/Bottom faces, OBB)
    │   ├── FoundationRebarValidator.cs          (Validates horizontal floor, solid geometry)
    │   ├── FoundationRebarCreationService.cs    (Rebar.CreateFromCurves coordinator)
    │   ├── FoundationRebarOrchestrator.cs       (TransactionGroup owner, dialog coordinator)
    │   ├── Models/
    │   │   └── FoundationSession.cs             (Document, Floor, Snapshot, Spec, Catalog)
    │   ├── View/
    │   │   ├── FoundationRebarView.xaml
    │   │   ├── FoundationGeometryView.xaml
    │   │   └── FoundationSettingView.xaml
    │   └── View Models/
    │       ├── FoundationRebarViewModel.cs
    │       ├── FoundationGeometryViewModel.cs
    │       └── FoundationSettingViewModel.cs
    └── Application.cs                           (Ribbon push button registration)
```

#### 2. Core Model Definitions (`HPRebar.Core`)
- **`FoundationGeometrySnapshot`**:
  ```csharp
  public sealed record FoundationGeometrySnapshot
  {
      public double Length { get; init; }          // mm (along local X)
      public double Width { get; init; }           // mm (along local Y)
      public double Thickness { get; init; }       // mm (along local Z)
      public double TopElevation { get; init; }    // mm
      public double BottomElevation { get; init; } // mm
      public Point3 Origin { get; init; }          // World origin of local coordinate system
      public Vector3 AxisX { get; init; }          // Primary direction unit vector
      public Vector3 AxisY { get; init; }          // Secondary direction unit vector
      public Vector3 AxisZ { get; init; }          // Normal unit vector (0, 0, 1)
  }
  ```
- **`FoundationRebarSpec`**:
  ```csharp
  public sealed record FoundationRebarSpec
  {
      public FoundationLayerSpec BottomX { get; init; }
      public FoundationLayerSpec BottomY { get; init; }
      public bool EnableTopMat { get; init; } = true;
      public FoundationLayerSpec TopX { get; init; }
      public FoundationLayerSpec TopY { get; init; }
      public double CoverBottom { get; init; } = 50.0; // mm
      public double CoverTop { get; init; } = 50.0;    // mm
      public double CoverSide { get; init; } = 50.0;   // mm
      public double StartOffset { get; init; } = 50.0; // mm
      public string PartitionName { get; init; } = "Foundation";
  }
  ```

---

## 4. Logic Chain

1. **Pure Domain Testability**:
   - *Observation*: In legacy `R03_FoundationRebar`, rebar coordinates were calculated directly using Revit `XYZ` and created inline with `Rebar.CreateFromCurves`. This made unit testing outside Revit impossible.
   - *Requirement*: `ORIGINAL_REQUEST.md` R1 & R2 mandate pure C# models and stateless calculators in `HPRebar.Core` (`netstandard2.0`) with 100% xUnit testability.
   - *Inference*: By defining `FoundationGeometrySnapshot`, `FoundationBoundaryCalculator`, and `FoundationMeshCalculator` with pure `Point3`, `Vector3`, and `Polyline3` structs in millimetres, 100% of the mesh distribution geometry, remainder centering, hook bend coordinates, and thickness guardrails can be verified using unit tests in `HPRebar.Core.Tests` without needing a Revit license or process.

2. **Accurate Handling of Rotated Foundations**:
   - *Observation*: Foundations in real projects are frequently oriented at skewed angles to match site parcel boundaries or structural grids. An Axis-Aligned Bounding Box (AABB) leads to grossly incorrect rebar lengths and angles.
   - *Inference*: Identifying the dominant boundary edge to construct an Oriented Bounding Box (OBB) with orthonormal basis $(\vec{U}_X, \vec{U}_Y, \vec{U}_Z)$ maps the foundation into a clean rectangular local frame $[0, L] \times [0, W]$. All mesh calculations are solved in local coordinates, and then transformed to Revit world space via `PointMapper`.

3. **Collision-Free 4-Layer Stacking**:
   - *Observation*: If Layer 1 and Layer 2 are assigned the same $Z$ elevation, physical bars clash. In standard structural detailing, Direction X bars run on the outside (closest to cover), and Direction Y bars sit directly against them.
   - *Inference*: Enforcing explicit vertical offsets:
     - Bottom Mat: $Z_1 = c_{bottom} + \Phi_{bx}/2$; $Z_2 = c_{bottom} + \Phi_{bx} + \Phi_{by}/2$.
     - Top Mat: $Z_4 = H - c_{top} - \Phi_{tx}/2$; $Z_3 = H - c_{top} - \Phi_{tx} - \Phi_{ty}/2$.
     This guarantees zero physical clash and exact adherence to engineering bar placement standards.

4. **Atomic Transaction Safety**:
   - *Observation*: Legacy Revit add-ins frequently crash mid-transaction when generating large numbers of rebars (e.g. hundreds of bars in a large mat foundation), leaving corrupted model states.
   - *Inference*: Wrapping the entire execution in `FoundationRebarOrchestrator` with a single `TransactionGroup("Foundation Rebar")` and attaching `RebarFailureHandling` (to swallow minor shape warnings) guarantees that any error or user cancellation cleanly restores the original model.

---

## 5. Caveats

1. **Single Planar Horizontal Floor Assumption**: 'Phương án A' assumes the foundation is modeled as a horizontal, non-sloped `Floor` with constant thickness. Sloped foundations, stepped footings, or multi-thickness raft slabs with thickened column drop panels require separate localized zoning or sub-slab segmentation.
2. **Rebar Shape Matching vs. Shape-Free Curves**: Using `Rebar.CreateFromCurves` with `useExistingShapeIfPossible: true` automatically matches standard shapes (straight bars and standard 90° hooks) if loaded in the project template. If no matching shape is found, Revit will generate a project shape.
3. **Rebar Splices for Spans > 11.7m**: In extremely large mat foundations ($L > 11.7$ m), commercial bar limits require lap splices. For 'Phương án A', full continuous bars or standard commercial lengths are modeled; staggered splices can be integrated in a subsequent iteration matching the `BeamRebar` splice calculator.

---

## 6. Conclusion

The specification mining and architecture definition for `R03_FoundationRebar` and 'Phương án A' are complete. All legacy antipatterns have been diagnosed, mathematical algorithms for 2-mat 2-way mesh distribution and hook detailing have been formalized, and the blueprint for `HPRebar.Core/FoundationRebar/` and `HPRebar/Foundation Rebar/` is established.

Key Deliverables:
- **18 Features Discovered** documented with inputs, outputs, error behaviors, and legacy origins.
- **10 Critical Edge Cases** catalogued with concrete mathematical solutions.
- Formalized 4-layer vertical stacking equations and remainder centering offsets.
- Complete mapping to `HPRebar.Core` pure logic, `HPRebar.Core.Tests` test suites, and `HPRebar` feature layer.

---

## 7. Verification Method

To independently verify this specification report:

1. **Verify Interface Contracts**:
   Compare the proposed directory structure and class names against `ORIGINAL_REQUEST.md` lines 79–131.
2. **Verify Mathematical Consistency**:
   Verify that the 4-layer vertical stacking elevations:
   $$Z_1 = c_{bottom} + \Phi_{bx}/2$$
   $$Z_2 = c_{bottom} + \Phi_{bx} + \Phi_{by}/2$$
   $$Z_3 = H - c_{top} - \Phi_{tx} - \Phi_{ty}/2$$
   $$Z_4 = H - c_{top} - \Phi_{tx}/2$$
   yield positive clear distance $H_{clear} = H - (c_{bottom} + c_{top} + \Phi_{bx} + \Phi_{by} + \Phi_{tx} + \Phi_{ty}) > 0$ for any valid foundation thickness.
3. **Verify Pure Domain Independence**:
   Confirm that none of the models or calculators planned for `HPRebar.Core/FoundationRebar/` import or reference any namespace under `Autodesk.Revit.*`.
