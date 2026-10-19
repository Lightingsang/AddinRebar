using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;

namespace HPKata.Core.Shapes
{
    public static partial class RebarShapeCatalog
    {
        private static void RegisterDefinitions00To29(Dictionary<RebarShapeType, RebarShapeDefinition> d)
        {
            RegisterGroup00To14(d);
            RegisterGroup15To29(d);
        }

        private static void RegisterGroup00To14(Dictionary<RebarShapeType, RebarShapeDefinition> d)
        {
            d[RebarShapeType.Dang00] = new(RebarShapeType.Dang00, "00", "Generic Freeform Rebar", "Freeform Rebar", 6, false, "Arbitrary N-vertex polyline");
            d[RebarShapeType.Dang01] = new(RebarShapeType.Dang01, "01", "Straight Main Bar", "RebarShape 00", 1, false, "Single straight segment");
            d[RebarShapeType.Dang02] = new(RebarShapeType.Dang02, "02", "Symmetric U-Bar", "RebarShape 21", 2, false, "3 segments, two 90 deg bends");
            d[RebarShapeType.Dang02a] = new(RebarShapeType.Dang02a, "02a", "Symmetric U-Bar with 135 Hooks", "RebarShape 21", 3, false, "U-bar with two 135 deg seismic hooks");
            d[RebarShapeType.Dang03] = new(RebarShapeType.Dang03, "03", "L-Bar Corner Bar", "RebarShape 11", 2, false, "2 segments, one 90 deg bend");
            d[RebarShapeType.Dang04] = new(RebarShapeType.Dang04, "04", "Asymmetric U-Bar", "RebarShape 21", 3, false, "3 segments, unequal legs");
            d[RebarShapeType.Dang05] = new(RebarShapeType.Dang05, "05", "Z-Bar Step Bar", "RebarShape 26", 3, false, "3 segments, opposing 90 deg bends");
            d[RebarShapeType.Dang06] = new(RebarShapeType.Dang06, "06", "Cranked Bar", "RebarShape 25", 3, true, "3 segments, 2 bends angle alpha");
            d[RebarShapeType.Dang07] = new(RebarShapeType.Dang07, "07", "Chamfered Corner Bar", "RebarShape 12", 2, true, "2 segments, angle alpha");
            d[RebarShapeType.Dang08] = new(RebarShapeType.Dang08, "08", "Column Offset Crank Bar", "RebarShape 25", 4, false, "Column section step 1:6");
            d[RebarShapeType.Dang08a] = new(RebarShapeType.Dang08a, "08a", "Inverted Column Offset Crank", "RebarShape 25", 4, false, "Opposite face offset crank");
            d[RebarShapeType.Dang09] = new(RebarShapeType.Dang09, "09", "U-Bar with 90 Outward Cogs", "RebarShape 23", 2, false, "U-bar with two 90 deg anchor cogs");
            d[RebarShapeType.Dang09a] = new(RebarShapeType.Dang09a, "09a", "U-Bar with 180 Circular Hooks", "RebarShape 21", 2, false, "Plain bar footing edge U-bar");
            d[RebarShapeType.Dang10] = new(RebarShapeType.Dang10, "10", "J-Bar with Cranked Leg", "RebarShape 31", 3, true, "Retaining wall toe bar");
            d[RebarShapeType.Dang11] = new(RebarShapeType.Dang11, "11", "Open C-Stirrup", "RebarShape 32", 4, false, "Thin-wall beam stirrup");
            d[RebarShapeType.Dang12] = new(RebarShapeType.Dang12, "12", "Stair Flight Crank Bar 1", "RebarShape 33", 4, true, "Stair soffit bar");
            d[RebarShapeType.Dang12a] = new(RebarShapeType.Dang12a, "12a", "Stair Flight Crank Inverted", "RebarShape 33", 4, true, "Stair landing top bar");
            d[RebarShapeType.Dang13] = new(RebarShapeType.Dang13, "13", "Open Trapezoidal Stirrup", "RebarShape 41", 5, true, "Tapered beam stirrup");
            d[RebarShapeType.Dang13a] = new(RebarShapeType.Dang13a, "13a", "Symmetric Trapezoidal Stirrup", "RebarShape 41", 3, true, "Symmetrical gutter tie");
            d[RebarShapeType.Dang14] = new(RebarShapeType.Dang14, "14", "Closed Rectangular Ring Welded", "RebarShape 51", 2, false, "Welded confining ring");
        }

        private static void RegisterGroup15To29(Dictionary<RebarShapeType, RebarShapeDefinition> d)
        {
            d[RebarShapeType.Dang15] = new(RebarShapeType.Dang15, "15", "C-Bar Same-Direction Cogs", "RebarShape 21", 2, false, "Slab border trimming");
            d[RebarShapeType.Dang16] = new(RebarShapeType.Dang16, "16", "Multi-Span Continuous Stepped", "RebarShape 55", 6, false, "Continuous beam step");
            d[RebarShapeType.Dang17] = new(RebarShapeType.Dang17, "17", "Nested Column Stirrup Box", "RebarShape 52", 3, false, "Column anti-bursting tie");
            d[RebarShapeType.Dang18] = new(RebarShapeType.Dang18, "18", "Z-Bar Opposing 90 Cogs", "RebarShape 26", 2, false, "Inter-wall shear dowel");
            d[RebarShapeType.Dang19] = new(RebarShapeType.Dang19, "19", "Semi-Circular U-Bar", "RebarShape 21", 2, false, "Plain round tension tie");
            d[RebarShapeType.Dang20] = new(RebarShapeType.Dang20, "20", "S-Tie Double Z-Bar", "RebarShape 42", 4, false, "Deep beam web tie");
            d[RebarShapeType.Dang21] = new(RebarShapeType.Dang21, "21", "Polygon Chamfered Stirrup", "RebarShape 43", 4, false, "Chamfered drainage trench");
            d[RebarShapeType.Dang22] = new(RebarShapeType.Dang22, "22", "Standard Closed Stirrup 135", "RebarShape 51", 3, false, "Primary beam/column tie");
            d[RebarShapeType.Dang23] = new(RebarShapeType.Dang23, "23", "Spiral Overlap Closed Stirrup", "RebarShape 53", 3, false, "Torsion-resisting stirrup");
            d[RebarShapeType.Dang23a] = new(RebarShapeType.Dang23a, "23a", "Overlap Closed Stirrup Short", "RebarShape 53", 3, false, "High-shear column tie");
            d[RebarShapeType.Dang24] = new(RebarShapeType.Dang24, "24", "Diamond Stirrup Rhombus", "RebarShape 61", 3, false, "8-bar column inner tie");
            d[RebarShapeType.Dang24a] = new(RebarShapeType.Dang24a, "24a", "Asymmetric Diamond Stirrup", "RebarShape 61", 3, false, "Rectangular column diamond");
            d[RebarShapeType.Dang25] = new(RebarShapeType.Dang25, "25", "Octagonal Stirrup", "RebarShape 62", 4, false, "Octagonal/round column");
            d[RebarShapeType.Dang26] = new(RebarShapeType.Dang26, "26", "6-Leg Multi-Cell Beam Stirrup", "RebarShape 63", 6, false, "Transfer beam mega stirrup");
            d[RebarShapeType.Dang27] = new(RebarShapeType.Dang27, "27", "Closed Trapezoidal Stirrup", "RebarShape 64", 4, false, "Sloped beam / parapet");
            d[RebarShapeType.Dang28] = new(RebarShapeType.Dang28, "28", "Triangular Stirrup", "RebarShape 65", 4, false, "Triangular precast pile");
            d[RebarShapeType.Dang29] = new(RebarShapeType.Dang29, "29", "5-Leg Stirrup Internal Tie", "RebarShape 66", 5, false, "Wide beam stirrup");
        }

        private static double CalculateLength00To29(
            RebarShapeType shape,
            double[] p,
            double angleDeg,
            int diameterMm,
            bool applyBendDeduction)
        {
            double l1 = GetParam(p, 0);
            double l2 = GetParam(p, 1);
            double l3 = GetParam(p, 2);
            double l4 = GetParam(p, 3);
            double l5 = GetParam(p, 4);
            double l6 = GetParam(p, 5);

            if (shape <= RebarShapeType.Dang14)
            {
                return EvalShapes00To14(shape, l1, l2, l3, l4, l5, angleDeg, diameterMm, applyBendDeduction);
            }
            return EvalShapes15To29(shape, l1, l2, l3, l4, l5, l6, diameterMm, applyBendDeduction);
        }

        private static double EvalShapes00To14(
            RebarShapeType shape,
            double l1, double l2, double l3, double l4, double l5,
            double angleDeg, int d, bool ded)
        {
            switch (shape)
            {
                case RebarShapeType.Dang00:
                    return l1 + l2 + l3 + l4 + l5;
                case RebarShapeType.Dang01:
                    return l1;
                case RebarShapeType.Dang02:
                    return 2.0 * l1 + l2 - (ded ? 2.0 * RebarCalculator.BendDeduction(d, 90.0, true) : 0.0);
                case RebarShapeType.Dang02a:
                    double hook2a = l3 > 0 ? l3 : RebarCalculator.Hook135Length(d);
                    return 2.0 * l1 + l2 + 2.0 * hook2a;
                case RebarShapeType.Dang03:
                    return l1 + l2 - (ded ? RebarCalculator.BendDeduction(d, 90.0, true) : 0.0);
                case RebarShapeType.Dang04:
                case RebarShapeType.Dang05:
                    return l1 + l2 + l3 - (ded ? 2.0 * RebarCalculator.BendDeduction(d, 90.0, true) : 0.0);
                case RebarShapeType.Dang06:
                    double s6 = SafeSin(angleDeg);
                    double mid6 = Math.Abs(s6) > 1e-4 ? l2 / Math.Abs(s6) : l2;
                    return l1 + mid6 + l3;
                case RebarShapeType.Dang07:
                    return l1 + l2;
                case RebarShapeType.Dang08:
                case RebarShapeType.Dang08a:
                    return l1 + Math.Sqrt(l2 * l2 + l3 * l3) + l4;
                case RebarShapeType.Dang09:
                    double cog9 = l3 > 0 ? l3 : RebarCalculator.Hook90Length(d);
                    return 2.0 * l1 + l2 + 2.0 * cog9;
                case RebarShapeType.Dang09a:
                    double hook9a = l3 > 0 ? l3 : RebarCalculator.Hook180Length(d);
                    return 2.0 * l1 + l2 + 2.0 * hook9a;
                case RebarShapeType.Dang10:
                    return l1 + l2 + l3;
                case RebarShapeType.Dang11:
                case RebarShapeType.Dang12:
                case RebarShapeType.Dang12a:
                    return l1 + l2 + l3 + l4;
                case RebarShapeType.Dang13:
                    return l1 + l2 + l3 + l4 + l5;
                case RebarShapeType.Dang13a:
                    return 2.0 * l1 + 2.0 * l2 + l3;
                case RebarShapeType.Dang14:
                    return 2.0 * (l1 + l2);
                default:
                    return l1;
            }
        }

        private static double EvalShapes15To29(
            RebarShapeType shape,
            double l1, double l2, double l3, double l4, double l5, double l6,
            int d, bool ded)
        {
            switch (shape)
            {
                case RebarShapeType.Dang15:
                case RebarShapeType.Dang18:
                    return 2.0 * l1 + l2;
                case RebarShapeType.Dang16:
                    return l1 + l2 + l3 + l4 + l5 + l6;
                case RebarShapeType.Dang17:
                    return 2.0 * (l1 + l2) + l3;
                case RebarShapeType.Dang19:
                    return 2.0 * l1 + Math.PI * l2;
                case RebarShapeType.Dang20:
                case RebarShapeType.Dang21:
                    return l1 + l2 + l3 + l4;
                case RebarShapeType.Dang22:
                    double hook22 = l3 > 0 ? l3 : RebarCalculator.Hook135Length(d);
                    double len22 = 2.0 * (l1 + l2) + 2.0 * hook22;
                    return ded ? len22 - 4.0 * RebarCalculator.BendDeduction(d, 90.0, true) : len22;
                case RebarShapeType.Dang23:
                case RebarShapeType.Dang23a:
                    double lap23 = l3 > 0 ? l3 : RebarCalculator.LapSpliceLength(d, 30.0);
                    return 2.0 * (l1 + l2) + lap23;
                case RebarShapeType.Dang24:
                    double hook24 = l3 > 0 ? l3 : RebarCalculator.Hook135Length(d);
                    return 4.0 * l1 + 2.0 * hook24;
                case RebarShapeType.Dang24a:
                    double hook24a = l3 > 0 ? l3 : RebarCalculator.Hook135Length(d);
                    return 2.0 * (l1 + l2) + 2.0 * hook24a;
                case RebarShapeType.Dang25:
                    double hook25 = l4 > 0 ? l4 : RebarCalculator.Hook135Length(d);
                    return 2.0 * (l1 + l2) + 4.0 * l3 + 2.0 * hook25;
                case RebarShapeType.Dang26:
                    return l1 + l2 + l3 + l4 + l5 + l6;
                case RebarShapeType.Dang27:
                    double hook27 = l4 > 0 ? l4 : RebarCalculator.Hook135Length(d);
                    return l1 + 2.0 * l2 + l3 + 2.0 * hook27;
                case RebarShapeType.Dang28:
                    double hook28 = l4 > 0 ? l4 : RebarCalculator.Hook135Length(d);
                    return l1 + l2 + l3 + 2.0 * hook28;
                case RebarShapeType.Dang29:
                    return 2.0 * (l1 + l2) + l5;
                default:
                    return l1;
            }
        }
    }
}
