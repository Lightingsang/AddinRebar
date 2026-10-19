# DISPATCH — worker_m1_remediate

## Task
You are worker_m1_remediate. Your task is to apply the exact remediation items requested by Reviewer 2, Challenger 1, and Challenger 2 to the technical reverse-engineering dossier in `docs/rea-dce-pro/`:

### 1. `docs/rea-dce-pro/README.md`:
- Update physical byte length of `C:\Dce Pro\MayHoc\mauhoc.db` to `8,982,528 bytes` (8.57 MB / 8.98 decimal MB).
- Update auxiliary `DceUpdate\DceUpdate.exe` SHA-256 to `a422598bf5208d57dbf2eb6fb586d8dbabbf29ae4ee59301bfe10043944f4489`.

### 2. `docs/rea-dce-pro/01-domain-architecture.md`:
- Update Section 8.1.2 header from `DceMath_* — 24 Functions` to `DceMath_* — 34 Functions`.
- Add the 3 missing exported functions to Table 2:
  - `DceMath_KhoangCach2Diem` -> `Point2D.DistanceTo` / `Point3D.DistanceTo` (Euclidean distance)
  - `DceMath_LocalToGlobal` -> `CoordinateTransform.LocalToGlobal` (3D local-to-WCS affine transform)
  - `DceMath_MidPoint` -> `Point2D.MidPoint` / `Point3D.MidPoint` (midpoint calculation)
- Unbundle collapsed functions into individual rows:
  - `DceMath_Offset_x`, `DceMath_Offset_y`, `DceMath_Offset_xy` as 3 separate rows.
  - `CalculateExpression`, `CalculateExpression2`, `CalculateExpression3` as 3 separate rows.
  - `CalculateRatio`, `CalculateRatio2` as 2 separate rows.
- Ensure the sum of all categories ($12 + 34 + 9 + 10 + 4$) equals exactly **69 functions**, each with its own entry.

### 3. `docs/rea-dce-pro/02-autostr-and-mayhoc.md`:
- Add a technical note in Section 1 on handling the UTF-8 BOM (`EF BB BF`) in `DuAnMau.autostr` for the `AutoStrProjectSerializer` in `HPDcePro.Core`.

### 4. `docs/rea-dce-pro/03-steel-and-excel.md`:
- Add the exact mathematical formulations for TCVN 2737:2023 Wind Load:
  - Basic wind pressure: $q_0 = 0.613 \times 10^{-3} V_{10,3s}^2$ ($kN/m^2$).
  - Characteristic wind pressure at height $z_e$: $w_k = q_0 \cdot k(z_e) \cdot c \cdot G$.
  - Terrain roughness coefficient $k(z_e)$ for zones A, B, C, D ($k(z_e) = 2.01 (z_e/z_g)^{2/\alpha}$).
  - External and internal pressure coefficients $c_{pe}, c_{pi}$.
- Add official Chapter 7 formulations for TCVN 5575:2024 steel beam-column stability:
  - In-plane stability: $\frac{N}{\varphi_e A f_y \gamma_c} \le 1.0$ (where $\varphi_e$ is determined from reduced slenderness $\lambda_e$ and eccentricity $m$).
  - Out-of-plane stability: $\frac{N}{c \varphi_y A f_y \gamma_c} + \frac{M_x}{\varphi_b W_{cx} f_y \gamma_c} \le 1.0$ (where $c$ is the influence factor and $\varphi_b$ is lateral-torsional buckling coefficient).
- Add the missing Excel UDF entries to Table 2: `DCE_Col_Function`, `DCE_Col_Design`, `DCE_Beam_Design`, `DCE_Slab_Design`, `TrongtamSlab_a`.

### 5. `docs/rea-dce-pro/04-command-mapping-and-roadmap.md`:
- In Table 1, append rows 62–69 for the 8 .NET commands from lines 76–85 of `C:\Dce Pro\DCE Pro.lsp`:
  - 62 | `TextDaiMong` | Label footing elevations & dimensions | `HPDcePro.AutoCad.Commands.FootingCommands.TextDaiMong`
  - 63 | `VeMongCoc_DCE` (alias `VeMongCoc`) | Draw pile foundation detail & rebar | `HPDcePro.AutoCad.Commands.FootingCommands.VeMongCoc`
  - 64 | `VeMongDon_DCE` (alias `VeMongDon`) | Draw isolated pad footing detail | `HPDcePro.AutoCad.Commands.FootingCommands.VeMongDon`
  - 65 | `TMCoc` | Pile numbering & coordinate tag | `HPDcePro.AutoCad.Commands.FootingCommands.TMCoc`
  - 66 | `RaiDaiCoc` (alias `rdc`) | Array pile reinforcement stirrups | `HPDcePro.AutoCad.Commands.FootingCommands.RaiDaiCoc`
  - 67 | `VeBlockLanhTo` | Draw opening lintel block | `HPDcePro.AutoCad.Commands.LintelCommands.VeBlockLanhTo`
  - 68 | `VeMatCatLanhTo` | Draw lintel cross-section detail | `HPDcePro.AutoCad.Commands.LintelCommands.VeMatCatLanhTo`
  - 69 | `VeThang_DCE` | Draw reinforced concrete staircase & landing | `HPDcePro.AutoCad.Commands.StairCommands.VeThang`
- In Section 2, add pure LISP utility `c:DCENAP` (line 349).

### Mandatory Integrity Warning:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_remediate\handoff.md`.
