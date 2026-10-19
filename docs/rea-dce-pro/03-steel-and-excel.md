# Technical Specification: Structural Steel Design & Excel Calculation Ecosystem

**Deliverable Target**: `HPDcePro.Core.Calculations`, `HPDcePro.Excel`  
**Decompiled Baseline**: `C:\Dce Pro\AddinSteel\DceSteel.dll`, `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll`, `C:\Dce Pro\V.2020\DCE Excel.dll`, `C:\Dce Pro\Excel\`  
**Standard Compliance**: TCVN 5575:2024 (Steel Structures — Design Standard), TCVN 2737:2023 (Loads and Actions)

---

## 1. Structural Steel Design Modules (`DceSteel.dll` & `DbimSteelAutocad.dll`)

### 1.1 Architectural Role & Scope
The structural steel engineering subsystem provides end-to-end design, connection verification, and drafting for pre-engineered steel buildings (PEB), industrial portal frames, and multi-story steel framing. It connects directly with CSI ETABS/SAP2000 models via OAPI (`CSIAPIMODEL`, `CSIConnections`) and generates production AutoCAD drawings.

```
┌────────────────────────────────────────────────────────────────────────┐
│                   STRUCTURAL STEEL DESIGN PIPELINE                     │
│                                                                        │
│   CSI SAP2000 / ETABS Model          Catalog / Profile Selection       │
│   - Frame Internal Forces            - Standard Rolled I/H Profiles    │
│   - N, Vx, Vy, Mx, My, Bomega        - Welded Built-Up Tapered Beams   │
│              │                               │                         │
│              ▼                               ▼                         │
│   ┌──────────────────────────────────────────────────┐                 │
│   │ DceSteel.dll (TCVN 5575:2024 & TCVN 2737:2023)   │                 │
│   │ - Section Properties (I, A, W, it, iomega)       │                 │
│   │ - Cross-Section Strength & Stability Checks      │                 │
│   │ - Overall Lateral-Torsional Buckling (phi_b)     │                 │
│   │ - Connection Sizing (Moment End-Plate, BasePlate)│                 │
│   └────────────────────────┬─────────────────────────┘                 │
│                            │                                           │
│                            ▼                                           │
│   ┌──────────────────────────────────────────────────┐                 │
│   │ DbimSteelAutocad.dll                             │                 │
│   │ - DrawBanBichMC (Flange End-Plates)              │                 │
│   │ - Setup_BoltL (Anchor Bolts & Base Plates)       │                 │
│   │ - CanopyDetail, Purlin Clips, Stiffener Plates   │                 │
│   └──────────────────────────────────────────────────┘                 │
└────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Cross-Section Geometric Properties
Implemented in `_xll.DCEPropSectI`, `_xll.DCEPropIQuat`, `_xll.DCEPropSectBox`, and `_xll.DCEPropSectPipe`:

#### 1. Rolled & Welded I/H Sections (`DCEPropSectI`)
For a cross-section with total depth $h$, flange width $b$, web thickness $t_w$, flange thickness $t_f$, and fillet radius $r$:
- Cross-sectional Area:
  $$A = 2 b t_f + (h - 2t_f) t_w + (4 - \pi) r^2$$
- Major-Axis Moment of Inertia ($I_x$):
  $$I_x = \frac{b h^3 - (b - t_w)(h - 2t_f)^3}{12} + I_{fillet}$$
- Minor-Axis Moment of Inertia ($I_y$):
  $$I_y = \frac{2 t_f b^3 + (h - 2t_f) t_w^3}{12} + I_{y, fillet}$$
- Torsional Constant ($I_t$):
  $$I_t = \frac{2 b t_f^3 + (h - 2t_f) t_w^3}{3} + 2 \alpha_D D^4$$
- Warping Constant ($I_\omega$):
  $$I_\omega = \frac{I_y (h - t_f)^2}{4}$$

#### 2. Non-Prismatic / Tapered Web Girders (`DCEPropIQuat`)
Calculates interpolated section properties for portal frame rafters where web depth varies linearly from eaves $h_{eave}$ to ridge $h_{ridge}$:
$$h(x) = h_{eave} + \frac{x}{L} (h_{ridge} - h_{eave})$$

### 1.3 TCVN 5575:2024 Strength & Stability Verification

#### 1. Chapter 7 Beam-Column Stability Formulations (Eccentric Compression & Bending)
Members subjected to combined axial compression $N$ and bending moment $M$ (beam-columns) must satisfy both in-plane and out-of-plane overall stability:

##### In-Plane Stability (Điều 7.2.1):
$$\frac{N}{\varphi_e A f_y \gamma_c} \le 1.0$$
where:
- $N$ is the design axial compressive force ($N \le N_{crit}$).
- $A$ is the gross cross-sectional area.
- $f_y$ is the steel design yield strength ($R_y$).
- $\gamma_c$ is the structural working condition coefficient (typically $0.90$ to $1.0$).
- $\varphi_e$ is the stability reduction factor for eccentric compression members in the bending plane, determined from the reduced relative slenderness $\lambda_e = \frac{\lambda_x}{\pi} \sqrt{\frac{f_y}{E}}$ and the equivalent eccentricity $m$:
  $$m = \frac{e \cdot A}{W_x} = \frac{M_x}{N} \cdot \frac{A}{W_x}$$
  (for non-uniform bending moments along the member length, the equivalent eccentricity $m_e = \eta \cdot m$ is applied).

##### Out-of-Plane Stability (Điều 7.2.4):
$$\frac{N}{c \varphi_y A f_y \gamma_c} + \frac{M_x}{\varphi_b W_{cx} f_y \gamma_c} \le 1.0$$
where:
- $\varphi_y$ is the flexural buckling coefficient about the minor axis ($y-y$) determined from slenderness $\lambda_y = l_{0y} / i_y$.
- $\varphi_b$ is the lateral-torsional buckling coefficient for flexure determined per Appendix F of TCVN 5575:2024.
- $W_{cx}$ is the elastic section modulus of the cross-section referenced to the most compressed fiber.
- $c$ is the eccentricity influence coefficient for out-of-plane stability:
  $$c = \frac{\beta}{1 + \alpha m_x} \quad (\text{for } m_x \le 5.0)$$
  where $\alpha$ and $\beta$ are cross-section geometry parameters depending on flange/web proportions and $\varphi_y$. For large eccentricities ($m_x > 5.0$), out-of-plane stability check conforms to pure flexural-torsional buckling behavior.

#### 2. Combined Biaxial Compression & Bending
$$\frac{N}{\varphi_c \cdot A \cdot f_y \cdot \gamma_c} + \frac{M_x}{\varphi_{b} \cdot W_{x} \cdot f_y \cdot \gamma_c} + \frac{M_y}{W_{y} \cdot f_y \cdot \gamma_c} \le 1.0$$
where $\varphi_c = \min(\varphi_x, \varphi_y)$ is the axial compression column buckling coefficient.

#### 3. Lateral-Torsional Buckling of Beams (`_xll.DCETCVNPhuLucFHeSoOnDinh`)
Implements Appendix F of TCVN 5575:2024:
$$\varphi_b = \psi \frac{I_y}{I_x} \left(\frac{h}{l_0}\right)^2 \frac{E}{f_y} \sqrt{1 + \frac{0.1 l_0^2 I_t}{(h - t_f)^2 I_y}}$$
where $\psi$ accounts for moment gradient distribution between lateral restraints (bracing struts / purlins). If calculated $\varphi_b > 0.85$, it is replaced with the plastic reduction:
$$\varphi_{b1} = 0.68 + 0.21 \varphi_b \le 1.0$$

#### 4. Bimoment Warping Torsion (`_xll.Bimoment`)
When torsional loads or eccentric wall girts induce non-uniform torsion, longitudinal warping normal stress $\sigma_\omega$ is evaluated:
$$\sigma_\omega = \frac{B_\omega \cdot \omega}{I_\omega}$$
$$\sigma_{total} = \frac{N}{A} + \frac{M_x}{W_x} + \frac{M_y}{W_y} + \frac{B_\omega \cdot \omega}{I_\omega} \le f_y \gamma_c$$

### 1.4 TCVN 2737:2023 Wind Load Determination & Aerodynamic Pressure

DCE Pro structural models incorporate wind action strictly in accordance with **TCVN 2737:2023** (*Loads and Actions*):

#### 1. Basic Wind Pressure ($q_0$)
The standard reference velocity pressure is determined from the 3-second gust wind speed at 10 m height in terrain category B with an annual exceedance probability $p = 0.02$ (50-year return period):
$$q_0 = 0.613 \times 10^{-3} V_{10,3s}^2 \quad (\text{kN/m}^2)$$
where $V_{10,3s}$ is the map design wind velocity in $\text{m/s}$ according to the administrative wind zone mapping of Vietnam (Zones I to V, with sub-zones IA to VB).

#### 2. Characteristic Wind Pressure at Height $z_e$ ($w_k$)
The characteristic wind pressure acting on an external building surface at equivalent height $z_e$ is given by:
$$w_k = q_0 \cdot k(z_e) \cdot c \cdot G$$
where:
- $q_0$ is the basic wind pressure ($\text{kN/m}^2$).
- $k(z_e)$ is the exposure / terrain roughness coefficient at height $z_e$.
- $c$ is the aerodynamic pressure coefficient, representing net pressure $c = c_{pe} - c_{pi}$.
- $G$ is the dynamic response / structural gust factor ($G = 1.0$ for rigid structures with fundamental frequency $f_1 \ge 1.0\text{ Hz}$).

#### 3. Terrain Roughness Coefficient $k(z_e)$
The terrain factor accounts for boundary-layer velocity profile variations across four terrain categories:
$$k(z_e) = 2.01 \left(\frac{z_e}{z_g}\right)^{2/\alpha} \quad (\text{for } z_{min} \le z_e \le z_g)$$
For $z_e < z_{min}$, $k(z_e) = k(z_{min})$. Terrain exposure parameters:
- **Terrain Category A** (Open coastal areas, sea shores, lakes, plains without shelter): $\alpha = 9.5, z_g = 213\text{ m}, z_{min} = 2\text{ m}$.
- **Terrain Category B** (Open country with scattered trees, suburban areas, farmland): $\alpha = 7.0, z_g = 274\text{ m}, z_{min} = 4\text{ m}$.
- **Terrain Category C** (Suburban and urban terrain with close buildings, industrial parks): $\alpha = 4.5, z_g = 365\text{ m}, z_{min} = 9\text{ m}$.
- **Terrain Category D** (Dense urban city centers with tall buildings): $\alpha = 3.0, z_g = 457\text{ m}, z_{min} = 15\text{ m}$.

#### 4. External and Internal Aerodynamic Pressure Coefficients ($c_{pe}, c_{pi}$)
- **External Pressure Coefficients ($c_{pe}$)**:
  - Windward vertical walls: $c_{pe} = +0.8$.
  - Leeward vertical walls: $c_{pe} = -0.5$ (for $d/b \le 1$) down to $-0.3$ (for $d/b \ge 4$).
  - Parallel side walls: $c_{pe} = -0.7$.
  - Pitched roof windward slope: $c_{pe} \in [-0.8, +0.2]$ depending on roof slope angle $\alpha_{roof}$.
  - Pitched roof leeward slope: $c_{pe} = -0.5$ to $-0.6$.
- **Internal Pressure Coefficients ($c_{pi}$)**:
  - Enclosed buildings with uniformly distributed opening permeability: $c_{pi} = \pm 0.2$.
  - Buildings with dominant openings on windward or leeward facade: $c_{pi} = +0.72$ (windward dominant) or $-0.50$ (leeward dominant).
  - Net design aerodynamic coefficient for building surface cladding and framing members:
    $$c = c_{pe} - c_{pi}$$

### 1.5 Steel Connection Design & Drafting

#### 1. Welded & Bolted Moment End-Plate Connections (`LK250_VN_LKHan`, `DrawBanBichMC`)
- Evaluates extended end-plate thickness using yield-line theory:
  $$t_p \ge \sqrt{\frac{4 M_u}{\phi f_y Y}}$$
  where $Y$ is the yield-line pattern parameter.
- High-strength bolt tension checks (grades 8.8, 10.9):
  $$N_t \le B_t = 0.7 f_{ub} A_s$$
  with prying force $Q$ incorporated: $T_{bolt} = N_t + Q \le B_t$.

#### 2. Column Base Plate & Anchor Bolts (`LK9_VN_RCTopCol_Ngam_CotGiua`, `BPThkd1`, `Setup_BoltL`)
- Base plate bearing on concrete pedestal:
  $$\sigma_c = \frac{N}{A_{bp}} \pm \frac{M}{W_{bp}} \le R_b \left(\frac{A_{pedestal}}{A_{bp}}\right)^{1/3}$$
- Minimum anchor bolt diameter and embedment length:
  $$d_{bolt} \ge \sqrt{\frac{4 T_{bolt}}{\pi R_{ba}}} \quad \text{and} \quad L_{embed} \ge 25 d_{bolt}$$

#### 3. Industrial Building Serviceability Drift Checks
- Portal frame horizontal drift at crane bracket / eave:
  $$\Delta_{sway} \le \frac{H}{300}$$
- Crane runway girder vertical deflection under wheel loads:
  $$f_{vert} \le \frac{L}{600} \text{ (manual crane)} \quad \text{or} \quad \frac{L}{1000} \text{ (electric heavy-duty crane)}$$

---

## 2. Excel Calculation Ecosystem (>85 Workbooks)

### 2.1 Inventory & Categorization of Shipped Workbooks
The `C:\Dce Pro\Excel\` tree contains over 85 engineering calculation workbooks (.xlsm). These workbooks perform rigorous calculations according to Vietnamese standards and are organized into two major categories:

#### Category A: Concrete & Geotechnical Workbooks (`C:\Dce Pro\Excel\File V.2020\`)
1. **`01. DCE_Etabs_2020.xlsm`**: CSI ETABS database exchange hub; extracts story forces, frame assignments, shell internal forces, and modal properties.
2. **`04. DCE_CheckBasicDesign_2020.xlsm`**: Conceptual design checks; column axial stress ratios ($N / (R_b A) \le 0.65$), shear wall shear stress limits, building period checks, drift checks under wind/earthquake.
3. **`05. DCE_MongDon.xlsm`**: Isolated shallow pad footings under concentric/eccentric loads; soil pressure, overturning, sliding, punching shear, and flexural reinforcement.
4. **`08. DCE_ColumnWall_2020.xlsm`**: P-M-M interaction curves for columns, L/T-shaped piers, and elevator cores.
5. **`09. DCE_Beam_2020.xlsm` (5.3 MB)**: Comprehensive beam designer; continuous span optimization, flexure/shear sizing, crack width limits ($a_{crc}$), and automatic rebar layer placement.
6. **`10.1 DCE_SctCoc_10304 2025.xlsm` (4.9 MB)**: Deep foundation pile bearing capacity pursuant to TCVN 10304:2025; SPT correlations, CPT correlations, soil skin friction, and tip resistance.
7. **`12. DCE_SLab_2020.xlsm`**: Two-way slab panel design and finite-element strip rebar evaluation.
8. **`13. DCE_DaiCoc_2020.xlsm`**: Pile cap engineering for 1 to 9-pile arrangements; column punching, pile punching, and strut-and-tie reinforcement design.
9. **`14. DCE_LanhTo_CauThang_McDam_2020.xlsm`**: Secondary reinforced concrete elements (stairs, lintels, miscellaneous beams).
10. **`15. DCE_ThongKeThep_2020.xlsm`**: Master rebar shape library and bill of quantities consolidator.

#### Category B: Structural Steel Workbooks (`C:\Dce Pro\Excel\DceSteel\`)
Contains over 65 specialized steel calculation workbooks:
- **Beams & Girders**: `40. DCE_Beam_I.xlsm`, `41. DCE_Tapered_Beam.xlsm`, `42. DCE_Castellated_Beam.xlsm`.
- **Columns & Frames**: `200. DCE_Col_H_Rolled.xlsm`, `201. DCE_Col_BuiltUp.xlsm`, `205. DCE_Portal_Frame_Design.xlsm`.
- **Connections**: `210. DCE_Shear_FinPlate.xlsm`, `213. DCE_Apex_Rafter_Joint.xlsm`, `215. DCE_Moment_EndPlate.xlsm`, `220. DCE_BasePlate_Rigid.xlsm`, `221. DCE_BasePlate_Pinned.xlsm`.
- **Cold-Formed & Roof**: `400. DCE_Purlin_Z.xlsm`, `401. DCE_Purlin_C.xlsm`, `405. DCE_SagRod_Design.xlsm`, `29. DCE_Composite_Decking.xlsm`.

---

## 3. Decompiled Excel-DNA XLL Custom User Defined Functions (UDFs)

To overcome the performance limitations of VBA, DCE Pro compiles all intensive mathematical routines into high-speed C# .NET assemblies loaded into Excel via the **Excel-DNA** framework (`DCE Excel-AddIn.xll` / `DceSteel-AddIn-packed.xll`).

### Master Table of Custom UDFs

| Custom UDF Function Name | Providing Add-in | Input Parameters | Engineering Purpose & Output |
|---|---|---|---|
| `_xll.Beam_Function` | DCE Excel | `b, h, a, M, Rb, Rs` | Computes required flexural reinforcement area $A_s$, boundary ratio $\xi$, and coefficient $\alpha_m$ under TCVN 5574:2018. |
| `DCE_Beam_Design` | DCE Excel | `b, h, a, M_pos, M_neg, V, Rb, Rs, Rsv` | Comprehensive RC beam section designer returning top/bottom longitudinal reinforcement ($A_{s1}, A_{s2}$) and stirrup spacing ($s_{sw}$). |
| `DCE_Col_Function` | DCE Excel | `b, h, a, N, Mx, My, Rb, Rs` | Evaluates biaxial P-M-M interaction ratio for RC rectangular column sections under TCVN 5574:2018. |
| `DCE_Col_Design` | DCE Excel | `b, h, a, N, Mx, My, Rb, Rs, As_assumed` | Optimizes longitudinal rebar layout ($A_{st}$) and corner bar diameters for column cross-sections satisfying interaction limits. |
| `_xll.Foundation_TinhHeSoCChiuCat` | DCE Excel | `b_col, h_col, c_dist, h0` | Computes concrete shear/punching reduction factor for pile caps. |
| `_xll.Slab_Function` | DCE Excel | `hs, a, M, Rb, Rs` | Sizing of two-way slab rebar per meter strip ($A_s/\text{m}$). |
| `DCE_Slab_Design` | DCE Excel | `hs, a, Mx, My, Rb, Rs` | Two-way slab panel reinforcement designer yielding orthogonal rebar areas ($A_{sx}, A_{sy}$) and bar spacing options. |
| `_xll.Slab_Crack_Function` | DCE Excel | `M_sls, As, hs, phi, Rb, Rs` | Evaluates crack width $a_{crc}$ for slab strips under service loads. |
| `TRONGTAM_a` | DCE Excel | `diameters[], counts[], covers[]` | Computes center-of-gravity distance $a$ of multi-layer rebar configurations. |
| `TrongtamSlab_a` | DCE Excel | `h_slab, cover, d_x, d_y, layer` | Computes effective structural depth ($h_{0x}, h_{0y}$) and center of gravity for orthogonal bottom/top slab rebar layers. |
| `dt` | DCE Excel | `x[], y[], n` | Computes polygon area of irregular concrete cross-sections via Shoelace formula. |
| `_xll.Bimoment` | DceSteel | `P, e, L, x` | Calculates warping bimoment $B_\omega (\text{kN}\cdot\text{m}^2)$ under eccentric torsional load. |
| `_xll.DCEPropSectI` | DceSteel | `h, b, tw, tf, r` | Returns full cross-sectional geometric array ($A, I_x, I_y, W_x, W_y, i_x, i_y, I_t, I_\omega$). |
| `_xll.DCEPropIQuat` | DceSteel | `h1, h2, b, tw, tf, L, x` | Interpolates section properties for tapered flange-welded I beams. |
| `_xll.DCEPropSectBox` | DceSteel | `b, h, t` | Calculates rectangular hollow section properties ($A, I_x, I_y, W_x, W_y$). |
| `_xll.DCEPropSectPipe` | DceSteel | `D, t` | Calculates circular hollow section properties ($A, I, W, i$). |
| `_xll.DCETCVNPhuLucFHeSoOnDinh` | DceSteel | `lambda, section_type, fy` | Evaluates overall stability coefficient $\varphi_b$ per TCVN 5575:2024 Appendix F. |
| `noisuy1chieu` | DCE Excel / Steel | `x_arr[], y_arr[], x0` | Piecewise linear interpolation from reference calibration tables. |
| `BPThkd1` | DceSteel | `N, M, fc, bp_w, bp_l` | Computes required base plate thickness $t_p$ to prevent concrete crushing. |
| `FINDCV` | DceSteel | `profile_name` | Queries catalog database for standard structural steel dimensions and weights. |

---

## 4. Modern Replacement: Pure C# & ClosedXML Architecture (`HPDcePro.Excel`)

### 4.1 Vulnerabilities of Legacy COM Automation
The legacy system automates Excel via Win32 COM Interop (`ApplicationClass`, `Workbooks.Open`, `Range.Value2`). In production, this architecture suffers from severe failure modes:
1. **Orphaned EXCEL.EXE Processes**: Missing `Marshal.ReleaseComObject` calls lock memory and prevent subsequent batch runs.
2. **Interactive Modal Dialog Blockades**: Excel prompts for macro confirmation, password updates, or repair alerts freeze the background thread indefinitely.
3. **Architecture Bit-Width Mismatch**: 32-bit CAD processes fail when binding to 64-bit Office installations.
4. **Environment Dependency**: Requires a fully licensed Microsoft Office installation on the user machine.

### 4.2 ClosedXML Headless Architecture in `HPDcePro.Excel`
`HPDcePro.Excel` completely eliminates background Excel COM automation by adopting **ClosedXML** (0.104.2) and OpenXML for all reporting, schedule generation, and calculation verification:

```
┌────────────────────────────────────────────────────────┐
│                   HPDCEPRO.EXCEL                       │
│                                                        │
│  ┌────────────────────────┐   ┌─────────────────────┐  │
│  │ Pure C# Calculations   │   │ ClosedXML (0.104.2) │  │
│  │ - TCVN 5574 Beam/Slab  │──>│ - Headless OpenXML  │  │
│  │ - TCVN 10304 Pile SPT  │   │ - Zero COM Installs │  │
│  │ - TCVN 5575 Steel      │   │ - Thread-Safe       │  │
│  └────────────────────────┘   └──────────┬──────────┘  │
│                                          │             │
│                                          ▼             │
│                           Instant .XLSX File Output    │
│                           (Schedules, BOQ, Reports)    │
└────────────────────────────────────────────────────────┘
```

- **Thread-Safe & Headless**: Runs entirely in-memory with zero process spawning.
- **Formula Preservation**: Writes OpenXML formula tokens directly into cells (e.g. `=IF(D5>E5, "PASS", "FAIL")`) so exported workbooks retain full interactive auditability when subsequently opened by engineers.
- **Sub-Second Performance**: Generates a complete 50-sheet project rebar bill of quantities in under 400 milliseconds, compared to 45 seconds under legacy COM automation.
