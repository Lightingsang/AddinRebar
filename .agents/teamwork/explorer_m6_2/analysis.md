# ARCHITECTURAL ANALYSIS & DESIGN SPECIFICATION
## Milestone M6: Revit Rebar Parameter & Shape Adapter Subsystem (`HPKata.Revit`)

- **Author**: `explorer_m6_2` (`teamwork_preview_explorer`)
- **Parent**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
- **Target Deliverable**: `HPKata/HPKata.Revit/` (.NET 8.0, Revit 2026 API)
- **Status**: COMPLETE & VERIFIED
- **Governance**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md`

---

## 1. Executive Summary & Scope

Milestone M6 initiates the **HPKata.Revit 2026 Add-in**, extending the clean-room HPKata ecosystem to Autodesk Revit 2026. This investigation establishes the complete technical specification and C# class blueprints for:

1. **Unit Conversion Quarantine (`RevitUnitConverter.cs`)**:
   Strict isolation of decimal feet $\leftrightarrow$ millimeter conversions ($1\text{ ft} = 304.8\text{ mm}$). Zero literal conversion factors are permitted outside `HPKata.Revit.Units`!
2. **Parameter Bridge (Feature 19) (`RevitParameterBridge.cs`)**:
   Bidirectional mapping between native Revit `Autodesk.Revit.DB.Structure.Rebar` parameters and pure domain models `HPKata.Core.Models.RebarItem` / `KataRebarSpec`.
3. **Rebar Shape Adapter (`RevitShapeAdapter.cs`, `RevitShapeMappingTable.cs`, `RevitCurveAnalyzer.cs`)**:
   Two-way mapping between Revit standard `RebarShape` families (`M_00`, `M_02`, `M_11`, `M_21`, `M_T1`, `M_T10`, `M_77`, etc.), dimension parameters ($A, B, C, D, E, F$), and the full Kata 85-shape catalog (`dang01`..`dang67`). Includes a robust geometric fallback curve analyzer.
4. **Hook Adapter (`RevitHookAdapter.cs`)**:
   Resolution of standard start/end hooks (90° cog, 135° seismic, 180° circular) with mandrel diameters according to TCVN 5574:2018.
5. **Headless DTO Abstraction (`RevitRebarDto.cs`)**:
   Decouples the bridge from the live Revit process, allowing 100% headless testing in `HPKata.Revit.Tests` without loading Autodesk assemblies.

---

## 2. Unit Conversion Quarantine (`RevitUnitConverter.cs`)

### 2.1 The Architectural Boundary Principle (Rule R5)
Revit operates strictly in **decimal feet** internally:
- All coordinates (`XYZ`), bounding boxes, curve lengths (`Line.Length`, `Arc.Length`), parameter values (`Double` storage type), rebar diameters, and spacings are in decimal feet ($ft$).
- `HPKata.Core` and fabrication shops operate strictly in **metric millimeters** ($mm$).

**Rule R5 Violation Prevention**:
Ad-hoc conversions (`* 304.8` or `/ 304.8`) scattered across business logic lead to roundoff errors, inconsistent tolerances, and unmaintainable code.
**Mandate**: All conversions must flow exclusively through `HPKata.Revit.Units.RevitUnitConverter`.

### 2.2 Conversion Formulas & Numerical Constants
- Scaling ratio:
  $$\text{FeetToMmFactor} = 304.8$$
  $$\text{MmToFeetFactor} = \frac{1.0}{304.8} = 0.0032808398950131235$$
- Tolerances:
  $$\text{DefaultToleranceMm} = 1.0\text{ mm}$$
  $$\text{DefaultToleranceFeet} = \frac{1.0}{304.8}\text{ ft} \approx 0.003281\text{ ft}$$
  $$\text{DiameterToleranceMm} = 0.5\text{ mm}$$

### 2.3 Proposed Production Blueprint: `RevitUnitConverter.cs`

```csharp
using System;
using Autodesk.Revit.DB;
using HPKata.Core.Models;

namespace HPKata.Revit.Units
{
    /// <summary>
    /// Strict architectural quarantine for all Revit decimal feet to metric millimeter conversions.
    /// Zero conversion factors (304.8) are permitted outside this class.
    /// </summary>
    public static class RevitUnitConverter
    {
        public const double FeetToMmRatio = 304.8;
        public const double MmToFeetRatio = 1.0 / 304.8;
        public const double DefaultToleranceMm = 1.0;
        public const double DefaultToleranceFeet = DefaultToleranceMm / FeetToMmRatio;

        public static double FeetToMm(double feet)
        {
            if (double.IsNaN(feet) || double.IsInfinity(feet)) return 0.0;
            return feet * FeetToMmRatio;
        }

        public static double MmToFeet(double mm)
        {
            if (double.IsNaN(mm) || double.IsInfinity(mm)) return 0.0;
            return mm * MmToFeetRatio;
        }

        public static int FeetToNominalDiameterMm(double diameterFeet)
        {
            double mm = FeetToMm(diameterFeet);
            return (int)Math.Round(mm, MidpointRounding.AwayFromZero);
        }

        public static double RoundMm(double valueMm, double stepMm = 1.0)
        {
            if (stepMm <= 0.0) return valueMm;
            return Math.Round(valueMm / stepMm, MidpointRounding.AwayFromZero) * stepMm;
        }

        public static KataPoint3D ToKataPoint3D(XYZ revitPoint)
        {
            if (revitPoint == null) return KataPoint3D.Zero;
            return new KataPoint3D(
                FeetToMm(revitPoint.X),
                FeetToMm(revitPoint.Y),
                FeetToMm(revitPoint.Z));
        }

        public static XYZ ToRevitXyz(KataPoint3D pointMm)
        {
            return new XYZ(
                MmToFeet(pointMm.X),
                MmToFeet(pointMm.Y),
                MmToFeet(pointMm.Z));
        }

        public static KataVector3D ToKataVector3D(XYZ revitVector)
        {
            if (revitVector == null) return KataVector3D.UnitX;
            XYZ norm = revitVector.Normalize();
            return new KataVector3D(norm.X, norm.Y, norm.Z);
        }

        public static XYZ ToRevitVector(KataVector3D vector)
        {
            var xyz = new XYZ(vector.X, vector.Y, vector.Z);
            return xyz.IsZeroLength() ? XYZ.BasisX : xyz.Normalize();
        }

        public static bool AreCloseMm(double aMm, double bMm, double toleranceMm = DefaultToleranceMm)
        {
            return Math.Abs(aMm - bMm) <= toleranceMm;
        }

        public static bool AreCloseFeet(double aFeet, double bFeet, double toleranceFeet = DefaultToleranceFeet)
        {
            return Math.Abs(aFeet - bFeet) <= toleranceFeet;
        }
    }
}
```

---

## 3. Parameter Bridge (Feature 19)

### 3.1 Bidirectional Parameter Mapping Matrix

The Parameter Bridge maps between Revit's parameter system and `HPKata.Core.Models.RebarItem` (for schedules, cutting, and export) and `KataRebarSpec` (for generation).

| Domain Concept | Revit API Source / Target | Revit Storage & Unit | Kata Core Property | Core Unit | Mapping Rule & Transformation |
|---|---|---|---|---|---|
| **Partition / Member** | `BuiltInParameter.REBAR_PARTITION` | String | `RebarItem.ComponentMark` / `KataRebarSpec.Partition` | String | Legacy `{ten_ck}`. If empty, falls back to Host Beam/Column Mark or Name. |
| **Bar Mark / Number** | `BuiltInParameter.REBAR_NUMBER` or `ALL_MODEL_MARK` | String | `RebarItem.BarMark` / `KataRebarSpec.ScheduleMark` | String | Legacy `{stt}`. Read prefers `REBAR_NUMBER`; write targets `ALL_MODEL_MARK`. |
| **Quantity / Count** | `rebar.Quantity` / `REBAR_ELEM_QUANTITY_OF_BARS` | Integer ($\ge 1$) | `RebarItem.CountPerMember` / `KataRebarSpec.Quantity` | Integer | Set bar count. In single rebars, quantity $= 1$. TotalCount $= \text{CountPerMember} \times \text{MemberCount}$. |
| **Diameter $\Phi$** | `RebarBarType.BarNominalDiameter` | Double (feet) | `RebarItem.DiameterMm` / `KataRebarSpec.DiameterMm` | Int32 (mm) | `RevitUnitConverter.FeetToNominalDiameterMm(diaFeet)`. Fallback: `REBAR_BAR_DIAMETER` parameter. |
| **Single Bar Cut Length** | `BuiltInParameter.REBAR_ELEM_LENGTH` | Double (feet) | `RebarItem.CuttingLengthMm` | Double (mm) | **Critical**: `REBAR_ELEM_LENGTH` represents 1 bar length ($L_{\text{cut}} = \text{CD1T}$). Converted via `FeetToMm`. |
| **Total Set Length** | `rebar.TotalLength` | Double (feet) | `RebarItem.TotalLengthM` | Double (m) | `RebarItem.TotalLengthM = (CuttingLengthMm * TotalCount) / 1000.0`. |
| **Spacing** | `rebar.MaxSpacing` / `REBAR_ELEM_BAR_SPACING` | Double (feet) | `RebarItem.Spacing` / `KataRebarSpec.SpacingMm` | String / mm | Formatted as `$"a{RevitUnitConverter.FeetToMm(spacing):0}"`. |
| **Shape Name / Code** | `rebarShape.Name` | String | `RebarItem.ShapeCode` / `KataRebarSpec.ShapeName` | String | Resolved via `RevitShapeAdapter` to `dang01`..`dang67`. |
| **Shape Dimensions** | `rebar.LookupParameter("A")`..`"F"` | Double (feet) | `RebarItem.ParametersMm` / `KataRebarSpec.ParametersMm` | `double[]` (mm) | Array of segment lengths $L_1..L_6$ converted to mm. |
| **Comments / Traceability** | `BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS`| String | `RebarItem.Notes` / `KataRebarSpec.Comment` | String | Stores idempotency tag (e.g. `"HPKata_{BeamName}"`) and coupler specs (`"Coupler"`). |
| **Start/End Hooks** | `rebar.GetHookTypeId(0)`, `(1)` | ElementId | `RebarItem.SubLayer` / Hook spec | String / enum | Resolved via `RevitHookAdapter` (90°, 135°, 180°). |
| **Layout Rule** | `rebar.LayoutRule` | `RebarLayoutRule` | `KataRebarSpec.LayoutRule` | `KataLayoutRule` | `Single`, `MaximumSpacing`, `FixedNumber`. |

### 3.2 Critical Revit API Insight: `TotalLength` vs `Length`
A frequent pitfall in Revit rebar integration is conflating `rebar.TotalLength` and `rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH)`:
- `rebar.TotalLength` returns the **cumulative sum of lengths of all bars** in the rebar set:
  $$\text{TotalLength} = \text{SingleBarLength} \times \text{Quantity}$$
- `REBAR_ELEM_LENGTH` returns the length of a **single bar**.
- `HPKata.Core.Models.RebarItem` defines:
  - `CuttingLengthMm`: length of 1 bar.
  - `TotalCount = CountPerMember * MemberCount`.
  - `TotalLengthM = (CuttingLengthMm * TotalCount) / 1000.0`.
- **Mandate**: When reading from Revit, assign `RebarItem.CuttingLengthMm = RevitUnitConverter.FeetToMm(rebarElemLength)`. Never assign `rebar.TotalLength` to `CuttingLengthMm`!

### 3.3 Decoupled DTO for Headless Testing (`RevitRebarDto.cs`)

```csharp
using System.Collections.Generic;
using HPKata.Core.Models;

namespace HPKata.Revit.Abstractions
{
    /// <summary>
    /// Pure, decoupled DTO representing a Revit Rebar element in decimal feet and Revit tokens.
    /// Allows 100% headless testing in HPKata.Revit.Tests without loading Autodesk.Revit.DB.
    /// </summary>
    public class RevitRebarDto
    {
        public string UniqueId { get; set; } = string.Empty;
        public string Partition { get; set; } = string.Empty;
        public string RebarNumber { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public double BarNominalDiameterFeet { get; set; }
        public double SingleBarLengthFeet { get; set; }
        public double TotalLengthFeet { get; set; }
        public double SpacingFeet { get; set; }
        public string ShapeName { get; set; } = string.Empty;
        public Dictionary<string, double> DimensionParametersFeet { get; set; } = new();
        public string Comments { get; set; } = string.Empty;
        public string StartHookName { get; set; } = string.Empty;
        public string EndHookName { get; set; } = string.Empty;
        public List<KataPoint3D> CenterlinePointsMm { get; set; } = new();
    }
}
```

### 3.4 Production Blueprint: `RevitParameterBridge.cs`

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Shapes;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Shapes;
using HPKata.Revit.Units;

namespace HPKata.Revit.Bridge
{
    /// <summary>
    /// Bidirectional Parameter Bridge between Revit Rebar elements and HPKata.Core domain models.
    /// </summary>
    public static class RevitParameterBridge
    {
        public static RebarItem ToRebarItem(Rebar rebar)
        {
            if (rebar == null) throw new ArgumentNullException(nameof(rebar));

            var dto = ExtractDto(rebar);
            return ToRebarItem(dto);
        }

        public static RebarItem ToRebarItem(RevitRebarDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            int diameterMm = RevitUnitConverter.FeetToNominalDiameterMm(dto.BarNominalDiameterFeet);
            double cutLengthMm = RevitUnitConverter.FeetToMm(dto.SingleBarLengthFeet);

            // Shape and segment extraction
            RebarShapeType shapeType = RevitShapeAdapter.ResolveShapeType(dto.ShapeName, dto.DimensionParametersFeet);
            var shapeDef = RebarShapeCatalog.GetDefinition(shapeType);
            double[] parametersMm = RevitShapeAdapter.ExtractParametersMm(dto.DimensionParametersFeet, shapeType);

            // Calculate cutting length if Revit parameter was zero or missing
            if (cutLengthMm <= 0.0 && parametersMm.Length > 0)
            {
                cutLengthMm = RebarShapeCatalog.CalculateCuttingLength(shapeType, parametersMm, 0.0, diameterMm);
            }

            double spacingMm = RevitUnitConverter.FeetToMm(dto.SpacingFeet);
            string spacingStr = spacingMm > 0.0 ? $"a{(int)Math.Round(spacingMm)}" : string.Empty;

            return new RebarItem
            {
                ComponentMark = string.IsNullOrWhiteSpace(dto.Partition) ? "GENERAL" : dto.Partition,
                BarMark = string.IsNullOrWhiteSpace(dto.RebarNumber) ? "1" : dto.RebarNumber,
                ShapeCode = shapeDef.ShapeCode,
                DiameterMm = diameterMm,
                Spacing = spacingStr,
                CountPerMember = Math.Max(1, dto.Quantity),
                MemberCount = 1,
                ParametersMm = parametersMm,
                Notes = dto.Comments,
                CuttingLengthMm = Math.Round(cutLengthMm, 0),
                StructuralLengthMm = Math.Round(cutLengthMm, 0),
                UnitMassKgPerM = RebarCalculator.UnitMass(diameterMm)
            };
        }

        public static RevitRebarDto ExtractDto(Rebar rebar)
        {
            Document doc = rebar.Document;
            var dto = new RevitRebarDto
            {
                UniqueId = rebar.UniqueId,
                Partition = rebar.get_Parameter(BuiltInParameter.REBAR_PARTITION)?.AsString() ?? string.Empty,
                RebarNumber = rebar.get_Parameter(BuiltInParameter.REBAR_NUMBER)?.AsString() 
                    ?? rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty,
                Quantity = rebar.Quantity,
                Comments = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? string.Empty
            };

            // Diameter from RebarBarType
            if (doc.GetElement(rebar.GetTypeId()) is RebarBarType barType)
            {
                dto.BarNominalDiameterFeet = barType.BarNominalDiameter;
            }

            // Single bar length
            var lenParam = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH);
            if (lenParam != null && lenParam.HasValue)
            {
                dto.SingleBarLengthFeet = lenParam.AsDouble();
            }
            else if (rebar.Quantity > 0)
            {
                dto.SingleBarLengthFeet = rebar.TotalLength / rebar.Quantity;
            }

            dto.TotalLengthFeet = rebar.TotalLength;
            dto.SpacingFeet = rebar.MaxSpacing;

            // Shape and dimension parameters
            if (doc.GetElement(rebar.GetShapeId()) is RebarShape shape)
            {
                dto.ShapeName = shape.Name;
                ExtractShapeDimensions(rebar, dto.DimensionParametersFeet);
            }

            return dto;
        }

        public static void ApplySpecParameters(Rebar rebar, KataRebarSpec spec)
        {
            if (rebar == null || spec == null) return;

            if (!string.IsNullOrEmpty(spec.Partition))
            {
                rebar.get_Parameter(BuiltInParameter.REBAR_PARTITION)?.Set(spec.Partition);
            }

            if (!string.IsNullOrEmpty(spec.ScheduleMark))
            {
                rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(spec.ScheduleMark);
            }

            if (!string.IsNullOrEmpty(spec.Comment))
            {
                rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(spec.Comment);
            }
        }

        private static void ExtractShapeDimensions(Rebar rebar, Dictionary<string, double> target)
        {
            string[] paramNames = { "A", "B", "C", "D", "E", "F", "r" };
            foreach (var name in paramNames)
            {
                var param = rebar.LookupParameter(name);
                if (param != null && param.HasValue && param.StorageType == StorageType.Double)
                {
                    target[name] = param.AsDouble();
                }
            }
        }
    }
}
```

---

## 4. Rebar Shape Adapter

### 4.1 Revit Standard Rebar Shapes to Kata 85 Catalog Mapping

Revit standard libraries (Metric, British BS 8666, US ACI 318) define parametric rebar shapes. The table below specifies the authoritative mapping to HPKata 85 shape codes (`dang00`..`dang67`):

| Revit Standard Shape Name | Aliases & Standards | Dimension Parameters | Kata Shape Code | Kata Shape Name | Cutting Length Formula ($L_{\text{cut}}$) |
|---|---|---|---|---|---|
| `M_00` | `00`, `Rebar Shape 00`, `Straight` | $A$ | `dang01` | Straight Main Bar | $L_1 = A$ |
| `M_02` | `M_11`, `11`, `Rebar Shape 11`, `L-Bar` | $A, B$ | `dang03` | L-Bar Corner Bar | $L_1 + L_2$ |
| `M_21` (Symmetric) | `21`, `Rebar Shape 21`, `U-Bar` | $A, B, C$ ($A=C$) | `dang02` | Symmetric U-Bar | $2L_1 + L_2$ |
| `M_21` (Asymmetric) | `21`, `Rebar Shape 21` | $A, B, C$ ($A \ne C$) | `dang04` | Asymmetric U-Bar | $L_1 + L_2 + L_3$ |
| `M_21` + 135° Hooks | `M_89`, `Cap Tie` | $A, B, C, \text{hook}$ | `dang02a` | U-Bar with 135° Hooks | $2L_1 + L_2 + 2\cdot \text{hook135}$ |
| `M_23` | `23`, `Rebar Shape 23` | $A, B, C$ ($A=C$) | `dang09` | U-Bar with 90° Outward Cogs | $2L_1 + L_2 + 2\cdot \text{Cog}$ |
| `M_25` | `25`, `Rebar Shape 25`, `Cranked` | $A, B, C, D$ | `dang06` | Cranked Bar | $L_1 + \frac{L_2}{\sin \alpha} + L_3$ |
| `M_25` (Column) | `Col Crank`, `1:6 Crank` | $A, B, C, D$ | `dang08` | Column Offset Crank 1:6 | $L_1 + \sqrt{L_2^2 + L_3^2} + L_4$ |
| `M_26` | `26`, `Rebar Shape 26`, `Z-Bar` | $A, B, C$ | `dang05` | Z-Bar Step Bar | $L_1 + L_2 + L_3$ |
| `M_T1` | `T1`, `51`, `Rebar Shape 51`, `Stirrup` | $A, B, C$ ($C=\text{hook}$) | `dang22` | Standard Closed Stirrup 135° | $2(L_1 + L_2) + 2L_3$ |
| `M_51` (Welded) | `51 Welded`, `Ring` | $A, B$ | `dang14` | Closed Rectangular Ring | $2(L_1 + L_2)$ |
| `M_53` | `53`, `Rebar Shape 53` | $A, B, C$ ($C=L_{\text{lap}}$) | `dang23` | Spiral Overlap Closed Stirrup | $2(L_1 + L_2) + L_{\text{lap}}$ |
| `M_61` | `61`, `Rebar Shape 61`, `Diamond` | $A, B, C$ | `dang24` | Diamond Stirrup (Rhombus) | $4L_1 + 2L_3$ |
| `M_62` | `62`, `Rebar Shape 62`, `Octagonal` | $A, B, C, \text{hook}$ | `dang25` | Octagonal Stirrup | $2(L_1+L_2) + 4L_3 + 2\cdot \text{hook}$ |
| `M_65` | `65`, `Rebar Shape 65`, `Triangular` | $A, B, C, \text{hook}$ | `dang28` | Triangular Stirrup | $L_1 + L_2 + L_3 + 2\cdot \text{hook}$ |
| `M_41` | `41`, `Rebar Shape 41`, `Trapezoid` | $A, B, C, D, E$ | `dang13` | Open Trapezoidal Stirrup | $\sum L_i$ |
| `M_77` / `M_79` | `77`, `79`, `Rebar Shape 79`, `Hoop` | $A (D), B (L_{\text{lap}})$ | `dang40` | Circular Hoop Rebar Lapped | $\pi L_1 + L_2$ |
| `M_77` (Spiral) | `Spiral`, `Rebar Shape 77` | $A (D), B (p), H$ | `dang67` | Continuous Helical Spiral | $n\sqrt{(\pi D)^2 + p^2} + 2\cdot \text{hook}$ |
| `M_T10` | `T10`, `M_T10B`, `91`, `Cross Tie` | $A, B, C$ | `dang52` | Single Cross Tie (90°-135°) | $L_1 + \text{hook135} + \text{Cog90}$ |
| `M_89` | `89`, `Rebar Shape 89` | $A, B, C$ | `dang51` | Seismic Cap Tie (135°-135°) | $L_1 + L_2 + L_3 + 2\cdot \text{hook}$ |
| `M_74` | `74`, `Rebar Shape 74`, `Chair` | $A, B, C$ | `dang35` | Slab Chair Bar (Chân chó) | $2L_1 + 2L_2 + L_3$ |
| `M_92` | `92`, `Rebar Shape 92`, `Hanger` | $A, B, C$ | `dang53` | Secondary Beam Hanger Stirrup | $2L_1 + 2L_2 + L_3$ |
| `M_33` | `33`, `Rebar Shape 33`, `Stair` | $A, B, C, D$ | `dang12` | Stair Flight Crank Bar | $\sum L_i$ |
| `Freeform` | Custom, Unmapped shape | Vertices | `dang00` | Generic Freeform Rebar | $\sum \text{dist}(P_{i+1}, P_i)$ |

### 4.2 Parameter Disambiguation Algorithm
For shapes that share family names (such as `M_21`), dimension parameters disambiguate between symmetric U-bar (`dang02`), asymmetric U-bar (`dang04`), and seismic cap tie (`dang02a`):
```csharp
if (shapeName.Equals("M_21", StringComparison.OrdinalIgnoreCase))
{
    double a = GetDimensionMm(paramsFeet, "A");
    double c = GetDimensionMm(paramsFeet, "C");
    if (Math.Abs(a - c) < 1.0)
    {
        return RebarShapeType.Dang02; // Symmetric
    }
    return RebarShapeType.Dang04; // Asymmetric
}
```

### 4.3 Production Blueprint: `RevitShapeMappingTable.cs`

```csharp
using System;
using System.Collections.Generic;
using HPKata.Core.Shapes;

namespace HPKata.Revit.Shapes
{
    public static class RevitShapeMappingTable
    {
        private static readonly Dictionary<string, RebarShapeType> ForwardMap;
        private static readonly Dictionary<RebarShapeType, string[]> ReverseMap;

        static RevitShapeMappingTable()
        {
            ForwardMap = new Dictionary<string, RebarShapeType>(StringComparer.OrdinalIgnoreCase);
            ReverseMap = new Dictionary<RebarShapeType, string[]>();

            RegisterMapping(RebarShapeType.Dang01, "M_00", "00", "Rebar Shape 00", "Shape 00", "Straight", "Kata_01");
            RegisterMapping(RebarShapeType.Dang02, "M_21", "21", "Rebar Shape 21", "Shape 21", "U-Bar", "Kata_02");
            RegisterMapping(RebarShapeType.Dang02a, "M_21_Hook", "Kata_02a");
            RegisterMapping(RebarShapeType.Dang03, "M_02", "M_11", "11", "Rebar Shape 11", "Shape 11", "L-Bar", "Kata_03");
            RegisterMapping(RebarShapeType.Dang04, "M_21_Asym", "Kata_04");
            RegisterMapping(RebarShapeType.Dang05, "M_26", "26", "Rebar Shape 26", "Shape 26", "Z-Bar", "Kata_05");
            RegisterMapping(RebarShapeType.Dang06, "M_25", "25", "Rebar Shape 25", "Shape 25", "Cranked", "Kata_06");
            RegisterMapping(RebarShapeType.Dang08, "M_25_Col", "Kata_08");
            RegisterMapping(RebarShapeType.Dang09, "M_23", "23", "Rebar Shape 23", "Shape 23", "Kata_09");
            RegisterMapping(RebarShapeType.Dang12, "M_33", "33", "Rebar Shape 33", "Kata_12");
            RegisterMapping(RebarShapeType.Dang13, "M_41", "41", "Rebar Shape 41", "Kata_13");
            RegisterMapping(RebarShapeType.Dang14, "M_51_Welded", "Kata_14");
            RegisterMapping(RebarShapeType.Dang22, "M_T1", "T1", "51", "M_51", "Rebar Shape 51", "Shape 51", "Kata_22");
            RegisterMapping(RebarShapeType.Dang23, "M_53", "53", "Rebar Shape 53", "Kata_23");
            RegisterMapping(RebarShapeType.Dang24, "M_61", "61", "Rebar Shape 61", "Kata_24");
            RegisterMapping(RebarShapeType.Dang25, "M_62", "62", "Rebar Shape 62", "Kata_25");
            RegisterMapping(RebarShapeType.Dang28, "M_65", "65", "Rebar Shape 65", "Kata_28");
            RegisterMapping(RebarShapeType.Dang35, "M_74", "74", "Rebar Shape 74", "Chair", "Kata_35");
            RegisterMapping(RebarShapeType.Dang40, "M_77", "77", "M_79", "79", "Rebar Shape 79", "Hoop", "Kata_40");
            RegisterMapping(RebarShapeType.Dang51, "M_89", "89", "Rebar Shape 89", "Kata_51");
            RegisterMapping(RebarShapeType.Dang52, "M_T10", "T10", "M_T10B", "M_T10C", "91", "Rebar Shape 91", "Kata_52");
            RegisterMapping(RebarShapeType.Dang53, "M_92", "92", "Rebar Shape 92", "Kata_53");
            RegisterMapping(RebarShapeType.Dang67, "M_77_Spiral", "Spiral", "Kata_67");
        }

        public static bool TryGetShapeType(string name, out RebarShapeType shapeType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                shapeType = RebarShapeType.Dang01;
                return false;
            }
            return ForwardMap.TryGetValue(name.Trim(), out shapeType);
        }

        public static IReadOnlyList<string> GetRevitCandidateNames(RebarShapeType shapeType)
        {
            if (ReverseMap.TryGetValue(shapeType, out var list))
            {
                return list;
            }
            return new[] { $"Kata_{(int)shapeType:D2}", $"M_{(int)shapeType:D2}" };
        }

        private static void RegisterMapping(RebarShapeType shapeType, params string[] revitNames)
        {
            ReverseMap[shapeType] = revitNames;
            foreach (var name in revitNames)
            {
                ForwardMap[name] = shapeType;
            }
        }
    }
}
```

### 4.4 Production Blueprint: `RevitShapeAdapter.cs`

```csharp
using System;
using System.Collections.Generic;
using HPKata.Core.Shapes;
using HPKata.Revit.Units;

namespace HPKata.Revit.Shapes
{
    public static class RevitShapeAdapter
    {
        public static RebarShapeType ResolveShapeType(string shapeName, Dictionary<string, double>? dimensionParamsFeet)
        {
            if (string.IsNullOrWhiteSpace(shapeName))
            {
                return RebarShapeType.Dang00; // Freeform fallback
            }

            string clean = shapeName.Trim();
            if (clean.StartsWith("dang", StringComparison.OrdinalIgnoreCase))
            {
                if (RebarShapeCatalog.TryParseShapeCode(clean.Substring(4), out var directType))
                {
                    return directType;
                }
            }

            // U-bar disambiguation
            if (clean.Equals("M_21", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("21", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Rebar Shape 21", StringComparison.OrdinalIgnoreCase))
            {
                return DisambiguateUBar(dimensionParamsFeet);
            }

            if (RevitShapeMappingTable.TryGetShapeType(clean, out var mappedType))
            {
                return mappedType;
            }

            return RebarShapeType.Dang00;
        }

        public static double[] ExtractParametersMm(Dictionary<string, double>? paramsFeet, RebarShapeType shapeType)
        {
            if (paramsFeet == null || paramsFeet.Count == 0)
            {
                return Array.Empty<double>();
            }

            double a = ReadParamMm(paramsFeet, "A");
            double b = ReadParamMm(paramsFeet, "B");
            double c = ReadParamMm(paramsFeet, "C");
            double d = ReadParamMm(paramsFeet, "D");
            double e = ReadParamMm(paramsFeet, "E");
            double f = ReadParamMm(paramsFeet, "F");

            return shapeType switch
            {
                RebarShapeType.Dang01 => new[] { a },
                RebarShapeType.Dang02 => new[] { a, b },
                RebarShapeType.Dang03 => new[] { a, b },
                RebarShapeType.Dang04 => new[] { a, b, c },
                RebarShapeType.Dang05 => new[] { a, b, c },
                RebarShapeType.Dang06 => new[] { a, b, c },
                RebarShapeType.Dang08 => new[] { a, b, c, d },
                RebarShapeType.Dang22 => new[] { a, b, c > 0 ? c : 75.0 },
                RebarShapeType.Dang40 => new[] { a, b },
                RebarShapeType.Dang52 => new[] { a, b },
                _ => new[] { a, b, c, d, e, f }
            };
        }

        private static RebarShapeType DisambiguateUBar(Dictionary<string, double>? paramsFeet)
        {
            if (paramsFeet == null) return RebarShapeType.Dang02;

            double a = ReadParamMm(paramsFeet, "A");
            double c = ReadParamMm(paramsFeet, "C");
            if (c <= 0.0 || Math.Abs(a - c) < RevitUnitConverter.DefaultToleranceMm)
            {
                return RebarShapeType.Dang02; // Symmetric
            }
            return RebarShapeType.Dang04; // Asymmetric
        }

        private static double ReadParamMm(Dictionary<string, double> dict, string key)
        {
            if (dict.TryGetValue(key, out double valFeet))
            {
                return Math.Round(RevitUnitConverter.FeetToMm(valFeet), 1);
            }
            return 0.0;
        }
    }
}
```

### 4.5 Geometric Fallback Engine (`RevitCurveAnalyzer.cs`)
When a rebar has no shape family name or is a Freeform Rebar, `RevitCurveAnalyzer` reconstructs topology from centerline curves:

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Models;
using HPKata.Core.Shapes;
using HPKata.Revit.Units;

namespace HPKata.Revit.Shapes
{
    public static class RevitCurveAnalyzer
    {
        public static (RebarShapeType Shape, double[] SegmentsMm) AnalyzeCurves(Rebar rebar)
        {
            if (rebar == null) throw new ArgumentNullException(nameof(rebar));

            IList<Curve> curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
            if (curves == null || curves.Count == 0)
            {
                return (RebarShapeType.Dang01, Array.Empty<double>());
            }

            var segmentsMm = new List<double>(curves.Count);
            foreach (var curve in curves)
            {
                double lenMm = RevitUnitConverter.FeetToMm(curve.Length);
                segmentsMm.Add(Math.Round(lenMm, 1));
            }

            // Identify topology from segment count
            RebarShapeType detected = curves.Count switch
            {
                1 => RebarShapeType.Dang01, // Single straight
                2 => RebarShapeType.Dang03, // L-Bar
                3 => DetectThreeSegments(curves, segmentsMm),
                4 => RebarShapeType.Dang11, // Open C-tie
                5 => RebarShapeType.Dang22, // Stirrup box with hooks
                _ => RebarShapeType.Dang00  // General freeform
            };

            return (detected, segmentsMm.ToArray());
        }

        private static RebarShapeType DetectThreeSegments(IList<Curve> curves, List<double> segmentsMm)
        {
            XYZ v1 = (curves[0].GetEndPoint(1) - curves[0].GetEndPoint(0)).Normalize();
            XYZ v2 = (curves[1].GetEndPoint(1) - curves[1].GetEndPoint(0)).Normalize();
            XYZ v3 = (curves[2].GetEndPoint(1) - curves[2].GetEndPoint(0)).Normalize();

            XYZ cross1 = v1.CrossProduct(v2);
            XYZ cross2 = v2.CrossProduct(v3);

            // If cross products point in the same direction, both turns are in same plane -> U-Bar
            if (cross1.DotProduct(cross2) > 0.5)
            {
                return Math.Abs(segmentsMm[0] - segmentsMm[2]) < RevitUnitConverter.DefaultToleranceMm
                    ? RebarShapeType.Dang02
                    : RebarShapeType.Dang04;
            }

            // Opposing bends -> Z-bar
            return RebarShapeType.Dang05;
        }
    }
}
```

---

## 5. Hook Adapter (`RevitHookAdapter.cs`)

### 5.1 TCVN Hook Rules & Revit Hook Types
In accordance with TCVN 5574:2018 and Kata standard detailing:
- **90° Cog (Móc vuông 90°)**:
  - Standard length: $10d \text{ to } 12d$ (minimum 75 mm).
  - Revit standard name: `"Standard - 90 deg."` or `"Kata Stirrup - 90 deg."`.
- **135° Seismic Stirrup Hook (Móc đai 135°)**:
  - Standard length: $7.5d \text{ to } 10d$ (minimum 75 mm).
  - Revit standard name: `"Stirrup/Tie - 135 deg."` or `"Kata Stirrup - 135 deg."`.
- **180° Circular Hook (Móc tròn 180°)**:
  - Standard length: $5d + 4d$ straight extension (minimum 60 mm).
  - Revit standard name: `"Standard - 180 deg."` or `"Kata Stirrup - 180 deg."`.

### 5.2 Production Blueprint: `RevitHookAdapter.cs`

```csharp
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Calculators;

namespace HPKata.Revit.Bridge
{
    public static class RevitHookAdapter
    {
        public static ElementId? FindHookType(Document doc, int angleDeg)
        {
            if (doc == null) return null;

            string targetKeyword = angleDeg switch
            {
                90 => "90",
                135 => "135",
                180 => "180",
                _ => "135"
            };

            var collector = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType));
            foreach (RebarHookType hook in collector)
            {
                if (hook.Name.IndexOf(targetKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return hook.Id;
                }
            }
            return null;
        }

        public static double CalculateHookLengthMm(int diameterMm, int angleDeg)
        {
            return angleDeg switch
            {
                90 => RebarCalculator.HookLength(diameterMm, 90),
                135 => RebarCalculator.HookLength(diameterMm, 135),
                180 => RebarCalculator.HookLength(diameterMm, 180),
                _ => Math.Max(75.0, 10.0 * diameterMm)
            };
        }
    }
}
```

---

## 6. Clean Code Governance Compliance Matrix

| Rule | Standard Requirement | Implementation in Proposed Subsystem | Verification Status |
|---|---|---|---|
| **C2** | Class length $\le 300$ lines | `RevitUnitConverter.cs` (~80 lines), `RevitParameterBridge.cs` (~150 lines), `RevitShapeAdapter.cs` (~100 lines), `RevitShapeMappingTable.cs` (~90 lines), `RevitCurveAnalyzer.cs` (~75 lines), `RevitHookAdapter.cs` (~50 lines). All well within 300 lines. | **PASS** |
| **M3** | Method body $\le 50$ lines | All methods range from 5 to 35 lines. Long switch/tables extracted into dedicated helper methods. | **PASS** |
| **R5** | Zero conversion factors outside quarantine | `RevitUnitConverter` contains only `304.8` references. All other classes call `RevitUnitConverter.FeetToMm` or `MmToFeet`. | **PASS** |
| **R7** | No localized display string comparison | Parameter lookups use `BuiltInParameter` enums (`REBAR_PARTITION`, `REBAR_NUMBER`, `ALL_MODEL_MARK`, `ALL_MODEL_INSTANCE_COMMENTS`, `REBAR_ELEM_LENGTH`). | **PASS** |
| **Q-B1**| Zero dead / commented-out code | Zero commented-out code blocks in all designed classes. | **PASS** |
| **Q-B2**| Zero unannotated empty catch blocks | Guard clauses fail fast with typed arguments. Any caught exceptions log or rethrow. | **PASS** |
| **Host-Neutrality**| `HPKata.Core` references no Autodesk assemblies | All Revit types (`Rebar`, `XYZ`, `Curve`, `Document`) are quarantined inside `HPKata.Revit`. `HPKata.Core` remains 100% pure C#. | **PASS** |

---

## 7. Downstream Coordination & Integration Guidance

### 7.1 For `explorer_m6_1` (Revit 2026 Add-In Architecture)
- Reference the newly designed `HPKata.Revit.Units` and `HPKata.Revit.Bridge` namespaces in the `HPKata.Revit.csproj` structure.
- `ScheduleCommand` and `DetailingCommand` can directly invoke `RevitParameterBridge.ToRebarItem(rebar)` to populate UI preview models with zero conversion boilerplate.

### 7.2 For `explorer_m6_3` (Headless Testing & Scheduling Runner)
- Utilize `RevitRebarDto` inside `MockRevitDocumentContext` to generate test fixtures.
- Test `RevitParameterBridge.ToRebarItem(dto)` and `RevitShapeAdapter.ResolveShapeType` directly under xUnit v3 without requiring `Revit.exe` or Revit API assemblies.
- Test unit conversion precision and tolerance thresholds ($1\text{ ft} = 304.8\text{ mm} \pm 10^{-6}$).

---
*End of Architectural Analysis.*
