using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;

namespace HPKata.Core.Shapes
{
    public static partial class RebarShapeCatalog
    {
        private static void RegisterDefinitions60ToCoupler(Dictionary<RebarShapeType, RebarShapeDefinition> d)
        {
            d[RebarShapeType.Dang60] = new(RebarShapeType.Dang60, "60", "Precast Pile Splice Ring", "RebarShape 79", 4, false, "Spun pile joint ring");
            d[RebarShapeType.Dang61] = new(RebarShapeType.Dang61, "61", "Deep Basement Wall Counterfort", "Freeform Rebar", 6, true, "Diaphragm wall counterfort");
            d[RebarShapeType.Dang62] = new(RebarShapeType.Dang62, "62", "Water Tank Corner Haunch Bar", "RebarShape 98", 5, true, "Water tank corner haunch");
            d[RebarShapeType.Dang62a] = new(RebarShapeType.Dang62a, "62a", "Tank Haunch Bar Anchor Wings", "RebarShape 98", 5, true, "Hydrostatic pressure tank");
            d[RebarShapeType.Dang63] = new(RebarShapeType.Dang63, "63", "Masonry Wall Dowel Pin", "RebarShape 11", 2, false, "Brick wall masonry tie");
            d[RebarShapeType.Dang64] = new(RebarShapeType.Dang64, "64", "Bridge Pier Cap Conical Cage", "Freeform Rebar", 5, false, "Bridge pier cap cage");
            d[RebarShapeType.Dang65] = new(RebarShapeType.Dang65, "65", "Precast Beam End Bursting Mesh", "RebarShape 99", 6, true, "Post-tensioned anchorage");
            d[RebarShapeType.Dang65a] = new(RebarShapeType.Dang65a, "65a", "Extended Beam End Bursting Mesh", "RebarShape 99", 6, true, "Bulb-tee girder mesh");
            d[RebarShapeType.Dang66] = new(RebarShapeType.Dang66, "66", "Mat Foundation High-Chair Wave", "Freeform Rebar", 6, false, "Raft foundation high-chair");
            d[RebarShapeType.Dang67] = new(RebarShapeType.Dang67, "67", "Continuous Helical Spiral Rebar", "RebarShape 77", 3, false, "Bored pile spiral tie");
            d[RebarShapeType.DangCoupler] = new(RebarShapeType.DangCoupler, "coupler", "Mechanical Coupler Sleeve", "Coupler", 2, false, "Rebar mechanical splice sleeve");
        }

        private static double CalculateLength60ToCoupler(
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

            switch (shape)
            {
                case RebarShapeType.Dang60:
                    return l1 + l2 + l3 + l4;
                case RebarShapeType.Dang61:
                case RebarShapeType.Dang65:
                case RebarShapeType.Dang65a:
                case RebarShapeType.Dang66:
                    return l1 + l2 + l3 + l4 + l5 + l6;
                case RebarShapeType.Dang62:
                case RebarShapeType.Dang62a:
                case RebarShapeType.Dang64:
                    return l1 + l2 + l3 + l4 + l5;
                case RebarShapeType.Dang63:
                    return l1 + l2;
                case RebarShapeType.Dang67:
                    return EvalSpiral67(l1, l2, l3, diameterMm);
                case RebarShapeType.DangCoupler:
                    return l1 > 0 ? l1 : 80.0; // Standard coupler sleeve length
                default:
                    return l1;
            }
        }

        private static double EvalSpiral67(double diameterMm, double pitchMm, double heightMm, int barDiaMm)
        {
            double d = diameterMm;
            double p = pitchMm > 0 ? pitchMm : 100.0;
            double h = heightMm > 0 ? heightMm : 1000.0;
            double n = Math.Ceiling(h / p);
            double turnLen = Math.Sqrt((Math.PI * d) * (Math.PI * d) + p * p);
            double hooks = 2.0 * RebarCalculator.Hook135Length(barDiaMm);
            return n * turnLen + hooks;
        }
    }
}
