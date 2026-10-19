using System;
using System.Globalization;
using ClosedXML.Excel;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Excel
{
    public static class DceUdfFunctions
    {
        // TCVN 5574:2018 Materials & Mechanics
        public static double TCVN_Rb(string macBt)
        {
            var conc = MaterialLibrary.TryGetConcrete(macBt, out var p) && p != null
                ? p
                : MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            return conc.Rb;
        }

        public static double TCVN_Rbt(string macBt)
        {
            var conc = MaterialLibrary.TryGetConcrete(macBt, out var p) && p != null
                ? p
                : MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            return conc.Rbt;
        }

        public static double TCVN_Eb(string macBt)
        {
            var conc = MaterialLibrary.TryGetConcrete(macBt, out var p) && p != null
                ? p
                : MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            return conc.Eb;
        }

        public static double TCVN_Rs(string macThep)
        {
            var reb = MaterialLibrary.TryGetRebar(macThep, out var p) && p != null
                ? p
                : MaterialLibrary.GetRebar(RebarGrade.CB400V);
            return reb.Rs;
        }

        // TCVN 5574:2018 Beam Reinforcement & Crack Width
        public static double TCVN_As_Beam(
            double b, double h, double momentKnm, string macBt = "B25", string macThep = "CB400-V", double a = 40.0)
            => CalculationUdfBridge.TinhThepDam_M(b, h, momentKnm, macBt, macThep, a);

        public static double TCVN_Beam_CrackWidth(
            double b, double h, double serviceMomentKnm, double asTensionMm2,
            double phiBarMm = 20.0, string macBt = "B25", string macThep = "CB400-V", double a = 40.0, bool isLongTerm = true)
            => RcGeotechUdfBridge.BeRongVetNutDam(b, h, serviceMomentKnm, asTensionMm2, phiBarMm, macBt, macThep, a, isLongTerm);

        // TCVN 10304:2025 Pile Bearing Capacity
        public static double TCVN_Pile_Rc_SPT(
            double diameterM, double lengthM, double n60Shaft, double n60Tip, bool isDriven = true)
            => CalculationUdfBridge.SucChiuTaiCoc_SPT(diameterM, lengthM, n60Shaft, n60Tip, isDriven);

        public static double TCVN_Pile_SkinFriction(
            double diameterM, double lengthM, double n60Shaft, bool isDriven = true, bool isCohesive = true)
            => RcGeotechUdfBridge.SucChiuTaiCoc_MaSat(diameterM, lengthM, n60Shaft, isDriven, isCohesive, isDesign: true);

        // TCVN 5575:2024 Steel Member Stability
        public static double TCVN_Steel_BeamCheck(
            double hMm, double bMm, double twMm, double tfMm, double momentMxKnm,
            double unbracedLengthL0M = 6.0, double shearVKn = 0.0, double yieldStrengthFyMpa = 235.0)
            => SteelWindUdfBridge.TinhDamThep_TCVN5575(hMm, bMm, twMm, tfMm, momentMxKnm, unbracedLengthL0M, shearVKn, yieldStrengthFyMpa);

        public static double TCVN_Steel_ColumnCheck(
            double hMm, double bMm, double twMm, double tfMm,
            double axialNKn, double momentMxKnm, double effectiveLengthLxM = 3.0, double effectiveLengthLyM = 3.0,
            double yieldStrengthFyMpa = 235.0)
            => SteelWindUdfBridge.TinhCotThep_TCVN5575(hMm, bMm, twMm, tfMm, axialNKn, momentMxKnm, effectiveLengthLxM, effectiveLengthLyM, yieldStrengthFyMpa);

        // TCVN 2737:2023 Wind Load
        public static double TCVN_Wind_Pressure(
            string vungGio, string diaHinh, double caoDoZeM, double cpe = 0.8, double cpi = 0.0, bool isDesign = true)
            => SteelWindUdfBridge.TinhTaiTrongGio_TCVN2737(vungGio, diaHinh, caoDoZeM, cpe, cpi, isDesign);

        /// <summary>
        /// Registers TCVN UDF evaluation hook for the ClosedXML workbook.
        /// </summary>
        public static void RegisterAll(IXLWorkbook workbook)
        {
            if (workbook == null) throw new ArgumentNullException(nameof(workbook));
            workbook.CustomProperties.Add("HPDcePro_TCVN_UDF_Registered", true);
        }

        public static object? EvaluateFunction(string functionName, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(functionName)) return null;

            string name = functionName.Trim().ToUpperInvariant();
            switch (name)
            {
                case "TCVN_RB":
                    return TCVN_Rb(args.Length > 0 ? args[0]?.ToString() ?? "B25" : "B25");
                case "TCVN_RBT":
                    return TCVN_Rbt(args.Length > 0 ? args[0]?.ToString() ?? "B25" : "B25");
                case "TCVN_EB":
                    return TCVN_Eb(args.Length > 0 ? args[0]?.ToString() ?? "B25" : "B25");
                case "TCVN_RS":
                    return TCVN_Rs(args.Length > 0 ? args[0]?.ToString() ?? "CB400-V" : "CB400-V");
                case "TCVN_AS_BEAM":
                    double b = args.Length > 0 && double.TryParse(args[0]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var bv) ? bv : 300.0;
                    double h = args.Length > 1 && double.TryParse(args[1]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var hv) ? hv : 600.0;
                    double m = args.Length > 2 && double.TryParse(args[2]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var mv) ? mv : 150.0;
                    return TCVN_As_Beam(b, h, m);
                case "TCVN_PILE_RC_SPT":
                    double d = args.Length > 0 && double.TryParse(args[0]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dv) ? dv : 0.4;
                    double l = args.Length > 1 && double.TryParse(args[1]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lv) ? lv : 20.0;
                    double nShaft = args.Length > 2 && double.TryParse(args[2]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var nsv) ? nsv : 15.0;
                    double nTip = args.Length > 3 && double.TryParse(args[3]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ntv) ? ntv : 25.0;
                    return TCVN_Pile_Rc_SPT(d, l, nShaft, nTip);
                default:
                    return null;
            }
        }
    }
}
