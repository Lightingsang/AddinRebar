using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;

namespace HPKata.Core.Shapes
{
    public static partial class RebarShapeCatalog
    {
        private static void RegisterDefinitions30To59(Dictionary<RebarShapeType, RebarShapeDefinition> d)
        {
            RegisterGroup30To44(d);
            RegisterGroup45To59(d);
        }

        private static void RegisterGroup30To44(Dictionary<RebarShapeType, RebarShapeDefinition> d)
        {
            d[RebarShapeType.Dang30] = new(RebarShapeType.Dang30, "30", "Stepped Footing Edge Bar", "RebarShape 54", 5, false, "Stepped boundary footing");
            d[RebarShapeType.Dang31] = new(RebarShapeType.Dang31, "31", "Circular Arch Bar with Tangents", "RebarShape 71", 4, true, "Arc radius R + two tangents");
            d[RebarShapeType.Dang32] = new(RebarShapeType.Dang32, "32", "Pile-to-Cap Flared Dowel", "RebarShape 72", 4, false, "Bored pile head breakout");
            d[RebarShapeType.Dang33] = new(RebarShapeType.Dang33, "33", "Spatial Foundation Cage Basket", "Freeform Rebar", 5, false, "3D folded footing rebar");
            d[RebarShapeType.Dang34] = new(RebarShapeType.Dang34, "34", "Continuous Wave Bar", "RebarShape 73", 3, false, "Continuous periodic shear key");
            d[RebarShapeType.Dang35] = new(RebarShapeType.Dang35, "35", "Slab Chair Bar", "RebarShape 74", 3, false, "Slab top mesh chair bar");
            d[RebarShapeType.Dang35a] = new(RebarShapeType.Dang35a, "35a", "Simplified Slab Chair Bar", "RebarShape 74", 3, false, "Standard chair bar variant");
            d[RebarShapeType.Dang36] = new(RebarShapeType.Dang36, "36", "Mat Foundation Chair Wings", "RebarShape 75", 5, false, "Heavy raft foundation chair");
            d[RebarShapeType.Dang37] = new(RebarShapeType.Dang37, "37", "Curved Vault Bar Anchors", "RebarShape 76", 5, true, "Circular water tank wall");
            d[RebarShapeType.Dang38] = new(RebarShapeType.Dang38, "38", "Parabolic U-Bar", "RebarShape 77", 3, false, "Drainage channel invert");
            d[RebarShapeType.Dang39] = new(RebarShapeType.Dang39, "39", "Spiral-to-Straight Transition", "RebarShape 78", 4, false, "Bored pile top connection");
            d[RebarShapeType.Dang40] = new(RebarShapeType.Dang40, "40", "Circular Hoop Rebar Lapped", "RebarShape 79", 2, false, "Circular column / pile tie");
            d[RebarShapeType.Dang40a] = new(RebarShapeType.Dang40a, "40a", "Circular Hoop 135 Hooks", "RebarShape 79", 2, false, "Seismic round column tie");
            d[RebarShapeType.Dang41] = new(RebarShapeType.Dang41, "41", "Flat U-Bar Hairpin", "RebarShape 11", 2, false, "Slab edge hairpins");
            d[RebarShapeType.Dang42] = new(RebarShapeType.Dang42, "42", "Chamfered Hairpin", "RebarShape 13", 3, false, "Skewed slab boundary");
            d[RebarShapeType.Dang43] = new(RebarShapeType.Dang43, "43", "Basement Retaining Wall Dowel", "RebarShape 81", 6, true, "Cantilever basement wall");
            d[RebarShapeType.Dang43a] = new(RebarShapeType.Dang43a, "43a", "Basement Wall Inverted Toe", "RebarShape 81", 6, true, "Property boundary wall");
            d[RebarShapeType.Dang44] = new(RebarShapeType.Dang44, "44", "Cantilever Balcony Loop", "RebarShape 82", 4, true, "Balcony / canopy slab");
        }

        private static void RegisterGroup45To59(Dictionary<RebarShapeType, RebarShapeDefinition> d)
        {
            d[RebarShapeType.Dang45] = new(RebarShapeType.Dang45, "45", "Sloped Column Neck Bar", "RebarShape 83", 4, true, "Column section reduction 1:6");
            d[RebarShapeType.Dang45a] = new(RebarShapeType.Dang45a, "45a", "Right-Angle Column Neck Bar", "RebarShape 83", 3, false, "Right-angle column shift");
            d[RebarShapeType.Dang46] = new(RebarShapeType.Dang46, "46", "Transfer Girder Double Step", "RebarShape 84", 5, false, "Stepped transfer girder");
            d[RebarShapeType.Dang47] = new(RebarShapeType.Dang47, "47", "Corbel Loop", "RebarShape 85", 4, false, "Crane bracket corbel");
            d[RebarShapeType.Dang48] = new(RebarShapeType.Dang48, "48", "Top Negative Moment Slab Hat", "RebarShape 86", 3, false, "Slab support negative hat");
            d[RebarShapeType.Dang49] = new(RebarShapeType.Dang49, "49", "Inclined Shear Truss Bar", "RebarShape 87", 3, true, "High shear diagonal bar");
            d[RebarShapeType.Dang49a] = new(RebarShapeType.Dang49a, "49a", "Double Inclined Shear Truss", "RebarShape 87", 5, true, "Short-span heavy load beam");
            d[RebarShapeType.Dang50] = new(RebarShapeType.Dang50, "50", "Restroom Slab Drop", "RebarShape 88", 4, false, "Sunken restroom slab");
            d[RebarShapeType.Dang51] = new(RebarShapeType.Dang51, "51", "Seismic Cap Tie 135-135", "RebarShape 89", 3, false, "Wall boundary element");
            d[RebarShapeType.Dang51a] = new(RebarShapeType.Dang51a, "51a", "Open Cap Tie 90-135", "RebarShape 89", 3, false, "Easy-installation cap tie");
            d[RebarShapeType.Dang52] = new(RebarShapeType.Dang52, "52", "Single Cross Tie", "RebarShape 91", 2, false, "Column/beam cross-tie");
            d[RebarShapeType.Dang53] = new(RebarShapeType.Dang53, "53", "Secondary Beam Hanger U-Stirrup", "RebarShape 92", 3, false, "Secondary beam hanger");
            d[RebarShapeType.Dang53a] = new(RebarShapeType.Dang53a, "53a", "Hanger Stirrup Outward Wings", "RebarShape 92", 4, false, "Heavy junction hanger");
            d[RebarShapeType.Dang54] = new(RebarShapeType.Dang54, "54", "Skewed Batter Pile Cap Anchor", "RebarShape 93", 4, true, "Batter pile cap anchor");
            d[RebarShapeType.Dang55] = new(RebarShapeType.Dang55, "55", "Box Culvert Frame Rebar", "RebarShape 94", 6, true, "Highway box culvert");
            d[RebarShapeType.Dang56] = new(RebarShapeType.Dang56, "56", "Stair Landing Transition Bar", "RebarShape 95", 4, true, "Stair flight landing knee");
            d[RebarShapeType.Dang57] = new(RebarShapeType.Dang57, "57", "Skewed Torsion Stirrup", "RebarShape 96", 3, true, "Curved bridge deck torsion");
            d[RebarShapeType.Dang58] = new(RebarShapeType.Dang58, "58", "Opening Corner Diagonal Bar", "RebarShape 97", 2, true, "Slab opening anti-crack");
            d[RebarShapeType.Dang59] = new(RebarShapeType.Dang59, "59", "Slab Edge Torsion U-Bar", "RebarShape 21", 2, false, "Flat plate slab edge");
        }

        private static double CalculateLength30To59(
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

            if (shape <= RebarShapeType.Dang44)
            {
                return EvalShapes30To44(shape, l1, l2, l3, l4, l5, l6, angleDeg, diameterMm);
            }
            return EvalShapes45To59(shape, l1, l2, l3, l4, l5, l6, angleDeg, diameterMm);
        }

        private static double EvalShapes30To44(
            RebarShapeType shape,
            double l1, double l2, double l3, double l4, double l5, double l6,
            double angleDeg, int d)
        {
            switch (shape)
            {
                case RebarShapeType.Dang30:
                    return l1 + l2 + l3 + l4 + l5;
                case RebarShapeType.Dang31:
                    double theta31 = angleDeg > 0 ? angleDeg : 90.0;
                    return 2.0 * l1 + Math.PI * l2 * (theta31 / 180.0);
                case RebarShapeType.Dang32:
                    return l1 + l2 + l3 + l4;
                case RebarShapeType.Dang33:
                    return l1 + l2 + l3 + l4 + l5;
                case RebarShapeType.Dang34:
                    double n34 = l3 > 0 ? l3 : 1.0;
                    return 2.0 * n34 * Math.Sqrt(l1 * l1 + l2 * l2);
                case RebarShapeType.Dang35:
                case RebarShapeType.Dang35a:
                    return 2.0 * l1 + 2.0 * l2 + l3;
                case RebarShapeType.Dang36:
                    return l1 + l2 + l3 + l4 + l5;
                case RebarShapeType.Dang37:
                    double theta37 = angleDeg > 0 ? angleDeg : 90.0;
                    return l1 + Math.PI * l2 * (theta37 / 180.0) + (l5 > 0 ? l5 : l1);
                case RebarShapeType.Dang38:
                    return 2.0 * l1 + Math.PI * l2;
                case RebarShapeType.Dang39:
                    double n39 = l4 > 0 ? l4 : 1.0;
                    return l1 + l2 + n39 * Math.Sqrt(Math.PI * Math.PI * l3 * l3 + 10000.0);
                case RebarShapeType.Dang40:
                    double lap40 = l2 > 0 ? l2 : RebarCalculator.LapSpliceLength(d, 30.0);
                    return Math.PI * l1 + lap40;
                case RebarShapeType.Dang40a:
                    double hook40a = l2 > 0 ? l2 : RebarCalculator.Hook135Length(d);
                    return Math.PI * l1 + 2.0 * hook40a;
                case RebarShapeType.Dang41:
                    return l1 + l2;
                case RebarShapeType.Dang42:
                    return l1 + l2 + l3;
                case RebarShapeType.Dang43:
                case RebarShapeType.Dang43a:
                    return l1 + l2 + l3 + l4 + l5 + l6;
                case RebarShapeType.Dang44:
                    return l1 + l2 + l3 + l4;
                default:
                    return l1;
            }
        }

        private static double EvalShapes45To59(
            RebarShapeType shape,
            double l1, double l2, double l3, double l4, double l5, double l6,
            double angleDeg, int d)
        {
            switch (shape)
            {
                case RebarShapeType.Dang45:
                    return l1 + Math.Sqrt(l2 * l2 + l3 * l3) + l4;
                case RebarShapeType.Dang45a:
                    return l1 + l2 + l3;
                case RebarShapeType.Dang46:
                    return l1 + l2 + l3 + l4 + l5;
                case RebarShapeType.Dang47:
                    return l1 + l2 + l3 + l4;
                case RebarShapeType.Dang48:
                    return l1 + l2 + l3;
                case RebarShapeType.Dang49:
                    double s49 = SafeSin(angleDeg);
                    double mid49 = Math.Abs(s49) > 1e-4 ? l2 / Math.Abs(s49) : l2;
                    return l1 + mid49 + l3;
                case RebarShapeType.Dang49a:
                    return l1 + l2 + l3 + l4 + l5;
                case RebarShapeType.Dang50:
                    return l1 + l2 + l3 + l4;
                case RebarShapeType.Dang51:
                    double h51 = RebarCalculator.Hook135Length(d);
                    return l1 + l2 + l3 + 2.0 * h51;
                case RebarShapeType.Dang51a:
                    return l1 + l2 + l3 + RebarCalculator.Hook90Length(d) + RebarCalculator.Hook135Length(d);
                case RebarShapeType.Dang52:
                    double h135 = l2 > 0 ? l2 : RebarCalculator.Hook135Length(d);
                    return l1 + h135 + RebarCalculator.Hook90Length(d);
                case RebarShapeType.Dang53:
                    return 2.0 * l1 + 2.0 * l2 + l3;
                case RebarShapeType.Dang53a:
                    return 2.0 * l1 + 2.0 * l2 + l3 + 2.0 * l4;
                case RebarShapeType.Dang54:
                    return l1 + l2 + l3 + l4;
                case RebarShapeType.Dang55:
                    return l1 + l2 + l3 + l4 + l5 + l6;
                case RebarShapeType.Dang56:
                    return l1 + l2 + l3 + l4;
                case RebarShapeType.Dang57:
                    return l1 + l2 + l3;
                case RebarShapeType.Dang58:
                    return l1 + 2.0 * l2;
                case RebarShapeType.Dang59:
                    return 2.0 * l1 + l2;
                default:
                    return l1;
            }
        }
    }
}
