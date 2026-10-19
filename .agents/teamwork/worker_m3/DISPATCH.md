# DISPATCH — worker_m3

## Task
You are worker_m3. Your task is to implement Milestone M3 in `HPDcePro.Core` (Features 11, 12, 13, 14, 15, 16):

### 1. Beam Detailing & Calculation Engine (TCVN 5574:2018)
In `src/HPDcePro.Core/Calculations/Beam/`:
- `ConcreteGrade.cs`, `RebarGrade.cs`, `MaterialLibrary.cs`: Standard design strengths ($R_b, R_{bt}, R_s, R_{sc}, R_{sw}, E_b, E_s$) for B15..B40 and CB240-T..CB500-V per TCVN 5574:2018.
- `BeamFlexureCalculator.cs`: Calculates required tensile steel $A_s$ and compressive steel $A_s'$ for rectangular sections ($b \times h$). Handles single reinforcement ($\alpha_m \le \alpha_R$) and double reinforcement ($\alpha_m > \alpha_R$), checking $\mu_{min} = 0.1\%$ and $\mu_{max} = 3.0\%$.
- `BeamShearCalculator.cs`: Calculates stirrup capacity $V_{sw}$ and spacing $s$ under shear force $V$, ensuring concrete crushing limit $V \le 0.3 \varphi_{w1} \varphi_{b1} R_b b h_0$ and diagonal tension condition.
- `BeamCrackCalculator.cs`: Evaluates short-term and long-term crack width $a_{crc} \le a_{crc,ult}$ (typically 0.3 mm / 0.4 mm).

### 2. 1D Rebar Cutting Stock Optimizer & Schedule Aggregator
In `src/HPDcePro.Core/Rebar/`:
- `RebarCuttingOptimizer.cs`: 1D Cutting Stock Optimization (`ToiUuCatThep`) with commercial bar length 11,700 mm. Automatically inserts lap splices ($30d..40d$) for continuous bars exceeding 11.7 m. Employs Best-Fit Decreasing / First-Fit Decreasing pattern optimization to minimize scrap offcut percentage while conserving rebar mass.
- `RebarScheduleAggregator.cs`: Aggregates bar schedules (`ThongKeThep`) by structure element, diameter, and shape; computes individual and total bar lengths, and calculates unit mass (kg/m) and total steel weight (kg) per TCVN 1651:2018.

### 3. Deep Foundation SPT Capacity (TCVN 10304:2025)
In `src/HPDcePro.Core/Calculations/Pile/`:
- `PileCapacityCalculator.cs`: Pure C# calculation of bored / driven pile axial bearing capacity ($R_u, R_c$) based on soil SPT $N_{60}$, calculating unit shaft resistance $f_i$ across soil layers and unit tip resistance $q_b$.

### 4. Structural Steel Design (TCVN 5575:2024)
In `src/HPDcePro.Core/Calculations/Steel/`:
- `SteelSectionProperties.cs`: Computes cross-sectional area, moments of inertia ($I_x, I_y$), elastic section moduli ($W_x, W_y$), and radius of gyration ($i_x, i_y$) for I, H, box, and pipe shapes.
- `SteelStabilityCalculator.cs`: Implements Chapter 7 beam-column in-plane stability $\frac{N}{\varphi_e A f_y \gamma_c} \le 1.0$ and out-of-plane stability $\frac{N}{c \varphi_y A f_y \gamma_c} + \frac{M_x}{\varphi_b W_{cx} f_y \gamma_c} \le 1.0$.

### 5. Aerodynamic Wind Load (TCVN 2737:2023)
In `src/HPDcePro.Core/Calculations/Wind/`:
- `WindLoadCalculator.cs`: Calculates basic velocity pressure $q_0 = 0.613 \times 10^{-3} V_{10,3s}^2$ ($kN/m^2$), terrain roughness exposure coefficient $k(z_e)$ for categories A, B, C, D, external/internal pressure coefficients $c_{pe}, c_{pi}$, and total wind load $w_k$.

### 6. Clean Code & Verification:
- All new files <= 300 lines (Rule C2).
- All new methods <= 50 lines (Rule M3).
- 0 commented-out code (Rule Q-B1).
- 0 unannotated catch blocks (Rule Q-B2).
- Build with `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` (0 warnings, 0 errors).

### Mandatory Integrity Warning:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3\handoff.md`.
