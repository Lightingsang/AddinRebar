# HPKata M5-2: DWG Rebar Geometry, 2-Way XData & Dynamic Handle-Linking Architecture

**Document ID**: `HPKATA-M5-2-ANALYSIS`  
**Agent**: `explorer_m5_2` (teamwork_preview_explorer)  
**Parent**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Date**: 2026-10-17  
**Status**: APPROVED ARCHITECTURAL SPECIFICATION  
**Scope**: AutoCAD 2026 (.NET 8) DWG database interop for rebar geometry, 2-way XData streams, block attribute mapping, and dynamic handle linking.

---

## 1. Executive Summary & Problem Boundary

Milestone M5 of HPKata delivers the production-ready AutoCAD 2026 (.NET 8) rebar detailing add-in (`HPKata.AutoCad`). A core capability of KataPro is its **Dynamic Rebar $\leftrightarrow$ Tag Linking Engine**:
1. Rebar entities in DWG are drawn as 2D `Polyline` objects with fillet arc bulges and global width.
2. Every rebar polyline carries a structured 12-item XData stream under RegApp `"KATA"`.
3. Rebar polylines and tag blocks (`thep`, `kata_block_KHT`, `kata_block_KHTCT`) are bi-directionally cross-referenced via DWG database `Handle` values (DxfCode 1005).
4. When rebar polylines are stretched, moved, or copied, commands `ud` (`update_thep`) and `cl` (`copy_tao_lien_ket`) dynamically recalculate cutting lengths and update tag block attributes without manual re-typing.

This document specifies the exact mathematical geometry, AutoCAD database transactions, XData wire streams, block attribute dictionaries, and clean code service decomposition adhering strictly to repository Clean Code Governance (`docs/clean-code/HP_CLEAN_CODE_CORE.md`, Rule C2 $\le 300$ lines, Rule M3 $\le 50$ lines, host appendix `autocad-civil.md`).

---

## 2. Mathematical & Algorithmic Geometry Engine (Fillet Arc Bulge & Polyline Generation)

### 2.1 Rebar Polyline Geometry Fundamentals
In AutoCAD, a 2D lightweight polyline (`Autodesk.AutoCAD.DatabaseServices.Polyline`) consists of a sequential list of 2D vertices $(X_i, Y_i)$ in the Object Coordinate System (OCS), where each vertex $i$ may optionally carry:
- A starting width and ending width (or a global `ConstantWidth`).
- A **bulge** value defining an arc segment spanning from vertex $i$ to vertex $i+1$.

### 2.2 Mathematical Derivation of Fillet Arc Bulge
In AutoCAD polyline geometry, the bulge $b$ of an arc segment is defined as:
$$b = \tan\left(\frac{\theta}{4}\right)$$
where $\theta$ is the included angle (sweep angle) of the arc.
- $\theta > 0$ denotes a **Counter-Clockwise (CCW)** arc ($b > 0$).
- $\theta < 0$ denotes a **Clockwise (CW)** arc ($b < 0$).
- $|b| = 0$ denotes a straight line segment.

#### Tangent Distance and Corner Fillet Construction
Given three consecutive vertices $P_{\text{prev}}, P_{\text{corner}}, P_{\text{next}}$ defining two intersecting straight segments:
1. Direction vectors:
   $$\vec{v}_1 = P_{\text{corner}} - P_{\text{prev}}, \quad \vec{v}_2 = P_{\text{next}} - P_{\text{corner}}$$
   Unit direction vectors:
   $$\hat{u}_1 = \frac{\vec{v}_1}{\|\vec{v}_1\|}, \quad \hat{u}_2 = \frac{\vec{v}_2}{\|\vec{v}_2\|}$$
2. Vector products:
   - Dot product: $D = \hat{u}_1 \cdot \hat{u}_2 = u_{1x} u_{2x} + u_{1y} u_{2y}$
   - 2D Cross product (z-component): $C = u_{1x} u_{2y} - u_{1y} u_{2x}$
3. Deflection / turn angle $\phi$:
   $$\phi = \text{atan2}(C, D)$$
   - $|\phi| = 0$: Collinear straight continuation.
   - $|\phi| = \pi$: 180° complete hairpin bend.
   - $C > 0$: Left turn (CCW).
   - $C < 0$: Right turn (CW).
4. Bend radius $R$:
   Per TCVN 5574:2018 and `HPKata.Core.Calculators.RebarCalculator`:
   $$R = r_{\text{inner}} + \frac{d}{2}$$
   where $d$ is bar diameter and $r_{\text{inner}}$ is the inner mandrel radius:
   - For $90^\circ$ bend: $r_{\text{inner}} = 2.0d$ ($d \le 16\text{ mm}$), $r_{\text{inner}} = 3.5d$ ($d \ge 18\text{ mm}$).
   - For $135^\circ$ bend: $r_{\text{inner}} = 1.5d$ ($d \le 10\text{ mm}$), $r_{\text{inner}} = 2.0d$ ($d > 10\text{ mm}$).
5. Tangent distance $t$ from corner $P_{\text{corner}}$:
   $$t = R \cdot \tan\left(\frac{|\phi|}{2}\right)$$
6. Clipping & anti-overlap guard:
   To prevent self-intersection when rebar legs are short:
   $$t_{\text{actual}} = \min\left(t, 0.45 \cdot \|\vec{v}_1\|, 0.45 \cdot \|\vec{v}_2\|\right)$$
   If $t$ was clamped, the effective bend radius is:
   $$R_{\text{eff}} = \frac{t_{\text{actual}}}{\tan\left(\frac{|\phi|}{2}\right)}$$
7. Tangent points:
   $$T_1 = P_{\text{corner}} - \hat{u}_1 \cdot t_{\text{actual}}$$
   $$T_2 = P_{\text{corner}} + \hat{u}_2 \cdot t_{\text{actual}}$$
8. Vertex insertion:
   - Replace $P_{\text{corner}}$ with two vertices: $T_1$ and $T_2$.
   - Vertex at $T_1$ carries bulge:
     $$\text{bulge} = \text{sign}(C) \cdot \tan\left(\frac{|\phi|}{4}\right)$$
   - Vertex at $T_2$ carries bulge $= 0.0$ (or continues with the next segment's bulge).

#### Standard Angular Bulge Constants
| Turn Angle $|\phi|$ | Interior Angle $\alpha = 180^\circ - |\phi|$ | Bulge $|b| = \tan(|\phi|/4)$ | Engineering Application |
|---|---|---|---|
| **$45^\circ$** | $135^\circ$ | $\tan(11.25^\circ) \approx 0.19891237$ | Haunch bars (`dang07`), chamfered corners |
| **$90^\circ$** | $90^\circ$ | $\tan(22.5^\circ) = \sqrt{2} - 1 \approx 0.41421356$ | Square cogs, L-bars (`dang03`), U-bars (`dang02`) |
| **$135^\circ$** | $45^\circ$ | $\tan(33.75^\circ) \approx 0.66817864$ | Seismic stirrup hooks (`dang22`, `dang02a`) |
| **$180^\circ$** | $0^\circ$ | $\tan(45.0^\circ) = 1.00000000$ | Semi-circular plain bar hooks (`dang09a`, `dang19`) |

### 2.3 Global Width & Lineweight Conventions
In AutoCAD:
- `Layer`:
  - Longitudinal / main bars: `"kata_thep chu"` (ACI 1 Red, Lineweight 0.50 mm).
  - Stirrups / ties / links: `"kata_thep dai"` (ACI 220 Magenta, Lineweight 0.35 mm).
- `Polyline.ConstantWidth`:
  - When `KataConfig.DrawCenterlines == 1`: `ConstantWidth = 0.0` (centerline axis representation).
  - When drawing physical rebar outline: `ConstantWidth = diameterMm / scale` or fixed drawing width (e.g. 0.35 mm to 0.50 mm in model space).
- `Polyline.Elevation`: Set to $Z$ coordinate from component level or 0.0 for 2D detailing sheets.

---

## 3. AutoCAD XData RegApp "KATA" 12-Item Stream Interop

### 3.1 RegApp Table Registration
Before attaching XData with RegApp `"KATA"`, the application ID must exist in `db.RegAppTableId`. `XDataService.EnsureRegApp` inspects `RegAppTable` and registers `"KATA"` idempotently.

### 3.2 12-Item XData Stream Specification
Every rebar polyline carries a 12-item XData stream serialized as:

```
[1001: "KATA"]
  ├── 1000: Dang              -> Rebar shape code string ("01", "02", "22", "dang02a")
  ├── 1070: STT               -> Bar mark sequence number (short: 1, 2, 3...)
  ├── 1070: phi               -> Nominal bar diameter in mm (short: 10, 16, 20, 25)
  ├── 1000: kc                -> Spacing string ("@150", "a200", or "" for main bars)
  ├── 1000: sl                -> Bar count expression string ("4", "8", "2x4", "24")
  ├── 1070: sl_ck             -> Member count multiplier (short: 1, 2...)
  ├── 1000: L                 -> Semicolon-delimited segment lengths in mm ("400;1200;400")
  ├── 1000: goc_uon           -> Special bending angle in degrees ("45", "135", or "")
  ├── 1000: ten_ck            -> Structural member mark ("D1-TANG 2", "C1", "M1")
  ├── 1000: stt_thep_chinh    -> Bar sub-layer position ("T" for top, "D" for bottom, "G" for side)
  ├── 1000: chu_thich         -> Fabrication / thread / coupler note ("Coupler 1 đầu", "Đai kín")
  └── 1000: Z                 -> Absolute floor elevation string ("+3.600")
```

### 3.3 Dynamic Handle-Linking Fields
Following the 12 standard items, optional dynamic link entries are appended:
```
  ├── 1005: LinkedHandle      -> Database Handle string of linked tag block (e.g. "3F2A")
  ├── 1000: LinkType          -> Link category ("THEP_DON", "THEP_BO", "THEP_DAI")
  └── 1005: LinkedDimHandle   -> Database Handle string of associated distribution dimension (e.g. "3F2C")
```

### 3.4 Decoupled Host-Neutral Serialization
To guarantee that business logic is 100% unit-testable without AutoCAD:
- `HPKata.Core.Serialization.KataXDataSerializer` maps `KataXDataRecord` to/from `IReadOnlyList<KataTypedValue>`.
- `HPKata.AutoCad.Interop.XDataService` simply iterates over AutoCAD `ResultBuffer` and translates:
  `TypedValue(tv.TypeCode, tv.Value) <-> KataTypedValue(ktv.DxfCode, ktv.Value)`.

---

## 4. Structural Block Attribute Mapping Engine

### 4.1 Master Block Attribute Specifications
KataPro relies on standard structural blocks in `Iso_kata.dwg`. The attribute tag schema for each block is:

#### 1. `kata_block_KHT` (Standard 2-Bar Mark Tag Block)
| Tag | Description | Type | Example |
|---|---|---|---|
| `SH1` | Bar Mark 1 | String | `"1"` |
| `DK1` | Diameter 1 | String | `"18"` |
| `KC1` | Spacing 1 | String | `"a200"` |
| `DKKC1` | Formatted Callout 1 | String | `"Ø18"` or `"Ø10a150"` |
| `SH2` | Bar Mark 2 | String | `"2"` |
| `DK2` | Diameter 2 | String | `"16"` |
| `KC2` | Spacing 2 | String | `""` |
| `DKKC2` | Formatted Callout 2 | String | `"Ø16"` |

#### 2. `thep` (Single Rebar Tag Callout Block)
| Tag | Description | Type | Example |
|---|---|---|---|
| `SH` | Bar Mark | String | `"5"` |
| `SL` | Total Bar Quantity | String | `"24"` |
| `DK` | Bar Diameter | String | `"10"` |
| `KC` | Spacing | String | `"a150"` |
| `CD1T` | Cutting Length for 1 Bar | String | `"2450"` (mm) |
| `DKKC` | Combined Callout | String | `"24Ø10a150"` |

#### 3. `kata_block_KHTCT` (Detailed 4-Bar Mark Tag Block)
Carries `SH1`..`SH4` and `DKKC1`..`DKKC4` for congested column/beam callouts.

#### 4. `Kata_Block_Beam_Rebar` (Beam Cross-Section Rebar Model)
| Tag | Description | Example |
|---|---|---|
| `U_NGOAI` | Stirrup closure form | `"Closed"` / `"Open"` |
| `THEP_T` | Top main bars | `"3f20"` |
| `THEP_D` | Bottom main bars | `"3f20"` |
| `THEP_GC` | Side skin reinforcement | `"2f12"` |
| `CD` | Cantilever / anchorage length | `"600"` |
| `A_DAI` | Stirrup spacing specification | `"a100/a200"` |

#### 5. `Kata_Block_Column&Wall`
Carries `TEN_COT`, `B`, `H`, `THEP_CHINH` (`"8f22"`), `THEP_DAI` (`"f10a100/200"`).

#### 6. `dangCoupler`
Carries `L` (sleeve length), `DK` (bar diameter), `SCK` (member count), `SL1` (count/member), `SL` (total count), `GHI_CHU` (thread note).

### 4.2 Attribute Update Workflow
When updating block attributes in AutoCAD .NET:
1. Open `BlockReference` for read.
2. Iterate `blockRef.AttributeCollection`.
3. For each `ObjectId attId`, open `AttributeReference` for read:
   - Check if tag matches target key (case-insensitive).
   - If matched and value changed: upgrade open to `ForWrite`, update `TextString`, and call `AdjustAlignment(db)`.

---

## 5. Dynamic 2-Way Handle-Linking Engine & Commands

### 5.1 Architecture State Machine
The referential state between a rebar polyline and its tag block is:

```
[Unlinked Polyline]                     [Unlinked Tag Block]
        |                                       |
        +------------> [ Command: link ] <-----+
                              |
                     (Writes Handle 1005)
                              v
                 [ Bi-directionally Linked Pair ]
                 (H_bar <=============> H_tag)
                              |
       +----------------------+----------------------+
       |                      |                      |
[ Command: ud ]        [ Command: cl ]        [ Command: dl ]
       |                      |                      |
  (Updates CD1T          (Deep Clone &          (Clears 1005
   from vertices)         Re-bind New            XData links)
                          Handles)                   v
                                              [ Severed Pair ]
```

### 5.2 Command Implementation Details

#### 1. `link` (`tao_lien_ket`)
- **User Prompt**: Prompts for rebar polyline, then prompts for tag block.
- **Validation**: Ensures polyline has valid geometry and tag block has attribute `SH` or `SH1`.
- **Database Transaction**:
  - Sets polyline XData `LinkedHandle = tagBlock.Handle.ToString()`.
  - Sets tag block XData `LinkedHandle = polyline.Handle.ToString()`.
  - Pushes initial values from polyline XData and geometry into tag block attributes (`SH`, `DK`, `KC`, `CD1T`, `DKKC`).
- **Feedback**: Prints confirmation with handles to AutoCAD command prompt.

#### 2. `cl` (`copy_tao_lien_ket`)
- **Problem Solved**: Standard AutoCAD `COPY` command duplicates entities with their existing XData intact, causing the new cloned polyline and tag block to point back to the *old* original entities.
- **Workflow**:
  1. User selects entities and specifies displacement vector $\vec{d}$.
  2. Within transaction:
     - Clone each entity: `var clone = ent.Clone() as Entity; clone.TransformBy(displacementMatrix);`
     - Append clones to `BlockTableRecord`: `btr.AppendEntity(clone); tr.AddNewlyCreatedDBObject(clone, true);`
     - Map `originalHandle -> clonedEntity`.
  3. **Re-binding Step**:
     - Inspect each cloned polyline's XData `LinkedHandle`.
     - Look up `clonedMap[oldTagHandle]`.
     - Update cloned polyline's `LinkedHandle = newClonedTag.Handle.ToString()`.
     - Update cloned tag's `LinkedHandle = newClonedPoly.Handle.ToString()`.
     - If polyline was copied without its tag, set `LinkedHandle = string.Empty` to prevent dangling link.

#### 3. `ud` (`update_thep`)
- **Workflow**:
  1. For each selected polyline (or all modified polylines):
     - Read vertices and bulges.
     - Decompose into straight segment lengths $L_1, L_2, \dots, L_k$.
     - Calculate cutting length $L_{\text{cut}} = \sum L_i - \Delta L$ (using `RebarShapeCatalog` or centerline sum).
     - Update polyline XData `ChieuDaiCacDoanMm = [L1, L2, ...]`.
     - Read `LinkedHandle`. If tag exists:
       - Open `BlockReference` for write.
       - Update attribute `CD1T = L_cut.ToString("F0")`.
       - Update attributes `SH`, `DK`, `KC`, `DKKC`.
     - Check `LinkedDimHandle`:
       - If dimension exists: read $L_{\text{dim}} = \text{dim.Measurement}$.
       - Calculate count $n = \lfloor L_{\text{dim}} / a \rfloor + 1$.
       - Update polyline XData `sl = n.ToString()` and tag block attribute `SL = n.ToString()`.
  2. If linked tag is missing (`IsErased == true`), warn user that polyline is an orphan.

#### 4. `dl` (`xoa_lien_ket`)
- Clears `LinkedHandle` on selected entity and its partner, restoring them to unlinked status.

#### 5. `tim` (`xem_lien_ket`) and `xoa` (`xoa_highlight`)
- `tim`: Resolves `LinkedHandle`, highlights both entities using `Entity.Highlight()`, computes combined bounding box, and calls `Editor.Zoom(extents)` to frame them in the active viewport.
- `xoa`: Calls `Entity.Unhighlight()` on all highlighted entities and issues `Editor.Regen()`.

#### 6. `dimthep`
- Iterates over polyline segments.
- Computes midpoint and normal offset direction $\hat{n} = (-v_y, v_x) / \|\vec{v}\|$.
- Creates `AlignedDimension` offset by 150 mm on layer `"kata_dim"` using dimension style `"kata_dim_iso"`.
- Appends dimensions to ModelSpace.

---

## 6. Clean Code Architecture & Modular Service Decomposition

To satisfy repository Clean Code Governance (`docs/clean-code/HP_CLEAN_CODE_CORE.md`), the AutoCAD interop and command infrastructure is partitioned into modular, highly cohesive classes:

```
HPKata.AutoCad/
├── Interop/
│   ├── IRebarGeometryService.cs        # Contract for polyline & vertex manipulation (~35 lines)
│   ├── RebarGeometryService.cs         # Polyline builder with fillet arc bulges & width (~240 lines)
│   ├── FilletArcCalculator.cs          # Pure math helper for fillet bulges & tangent points (~130 lines)
│   ├── IXDataService.cs                # Contract for XData read/write/registry (~40 lines)
│   ├── XDataService.cs                 # AutoCAD ResultBuffer adapter for KataXDataRecord (~190 lines)
│   ├── IBlockAttributeService.cs       # Contract for block attribute I/O (~45 lines)
│   ├── BlockAttributeService.cs        # Reads/updates BlockReference attributes (~210 lines)
│   ├── IHandleLinkService.cs           # Contract for 2-way handle linking & clone rebind (~50 lines)
│   ├── HandleLinkService.cs            # Orchestrates link, cl, dl, tim, xoa (~260 lines)
│   ├── RebarUpdateService.cs           # Implementation of ud & dimension count sync (~200 lines)
│   ├── DimensionRebarService.cs        # Automated dimension placement along segments (~180 lines)
│   └── TransientHighlightService.cs    # Transient graphics / viewport zoom framing (~130 lines)
│
├── Commands/
│   ├── RebarDrawingCommands.cs         # vt, taothep, smd, dai90, dai135, dai180, dai90135 (~220 lines)
│   ├── RebarLinkCommands.cs            # link, cl, ud, dl, tim, xoa (~210 lines)
│   ├── RebarEditCommands.cs            # sua, nhan, neo, cut, cattuyy, dmt, dao_thep (~240 lines)
│   └── RebarDimensionCommands.cs       # dimthep, timthep, check_lap (~190 lines)
```

### Clean Code Rule Compliance Table
| Rule ID | Rule Requirement | Implementation Guarantee in HPKata.AutoCad |
|---|---|---|
| **C2** | File $\le 300$ lines | Every file is strictly scoped $\le 260$ lines; largest class is `HandleLinkService` (~260 lines). |
| **M3** | Method $\le 50$ lines | Complex logic decomposed into step-down helpers (e.g. `RebindClonedPolyline`, `UpdateTagAttributes`). |
| **Q-B1** | Zero commented-out code | Zero dead or commented-out code in all source files. |
| **Q-B2** | Zero unannotated empty catches | Every catch block includes explanatory comment or logging. |
| **A1** | Transaction safety | Bridge/command level transaction wrapper with deterministic `using var tr = db.TransactionManager.StartTransaction()`. |
| **A2** | Deterministic wrapper disposal | All disposable AutoCAD wrappers (`Transaction`, `ResultBuffer`, `DocumentLock`) wrapped in `using var`. |
| **A3** | Document locking | Write commands acquire `using (doc.LockDocument())`. |
| **A7** | Handle addressing | Persistent cross-references use `Handle.ToString()`, never ephemeral `ObjectId`. |

---

## 7. Verification & Testing Strategy

1. **Host-Independent Geometry & Fillet Tests (`HPKata.Core.Tests`)**:
   - `FilletArcCalculatorTests`: Verifies exact bulge values for $45^\circ$, $90^\circ$, $135^\circ$, $180^\circ$ turns against mathematical constants.
   - `KataXDataSerializerTests`: Validates 100% roundtrip fidelity of 12-item stream and handle fields.
   - `KataBlockAttributeSerializerTests`: Validates tag, beam, and coupler block attribute mapping.
2. **Headless AutoCAD Contract Tests (`HPKata.AutoCad.Tests`)**:
   - Test data models and mocked transaction adapters without requiring live `acad.exe`.
3. **Live AutoCAD 2026 Verification**:
   - Verified via live AutoCAD 2026 MCP bridge (`hprebar-autocad`):
     - `link` creates bi-directional 1005 XData on rebar polyline and `kata_block_KHT`.
     - `ud` updates `CD1T` on stretched polyline.
     - `cl` clones rebar + tag and assigns new unique linked handles.
