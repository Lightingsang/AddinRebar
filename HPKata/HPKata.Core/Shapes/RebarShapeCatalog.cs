using System;
using System.Collections.Generic;

namespace HPKata.Core.Shapes
{
    /// <summary>
    /// Master catalog and mathematical calculation engine for all 85 standard rebar shapes.
    /// </summary>
    public static partial class RebarShapeCatalog
    {
        private static readonly Dictionary<RebarShapeType, RebarShapeDefinition> Definitions;
        private static readonly Dictionary<string, RebarShapeType> CodeLookup;

        static RebarShapeCatalog()
        {
            Definitions = new Dictionary<RebarShapeType, RebarShapeDefinition>(85);
            CodeLookup = new Dictionary<string, RebarShapeType>(StringComparer.OrdinalIgnoreCase);

            RegisterDefinitions00To29(Definitions);
            RegisterDefinitions30To59(Definitions);
            RegisterDefinitions60ToCoupler(Definitions);

            foreach (var kvp in Definitions)
            {
                CodeLookup[kvp.Value.ShapeCode] = kvp.Key;
                CodeLookup["dang" + kvp.Value.ShapeCode] = kvp.Key;
                CodeLookup["shape" + kvp.Value.ShapeCode] = kvp.Key;
            }
        }

        public static RebarShapeDefinition GetDefinition(RebarShapeType shapeType)
        {
            if (Definitions.TryGetValue(shapeType, out var def))
            {
                return def;
            }
            return Definitions[RebarShapeType.Dang01];
        }

        public static bool TryParseShapeCode(string? code, out RebarShapeType shapeType)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                shapeType = RebarShapeType.Dang01;
                return false;
            }

            string clean = code!.Trim();
            if (CodeLookup.TryGetValue(clean, out shapeType))
            {
                return true;
            }

            if (int.TryParse(clean, out int num) && num >= 0 && num <= 67)
            {
                string padded = num.ToString("D2");
                if (CodeLookup.TryGetValue(padded, out shapeType))
                {
                    return true;
                }
            }

            shapeType = RebarShapeType.Dang01;
            return false;
        }

        public static RebarShapeType NormalizeShapeCode(string? code)
        {
            TryParseShapeCode(code, out var shapeType);
            return shapeType;
        }

        public static double CalculateCuttingLength(
            RebarShapeType shape,
            double[] parametersMm,
            double angleDeg = 0.0,
            int diameterMm = 0,
            bool applyBendDeduction = false)
        {
            parametersMm ??= Array.Empty<double>();

            if (shape <= RebarShapeType.Dang29)
            {
                return CalculateLength00To29(shape, parametersMm, angleDeg, diameterMm, applyBendDeduction);
            }
            if (shape <= RebarShapeType.Dang59)
            {
                return CalculateLength30To59(shape, parametersMm, angleDeg, diameterMm, applyBendDeduction);
            }
            return CalculateLength60ToCoupler(shape, parametersMm, angleDeg, diameterMm, applyBendDeduction);
        }

        public static double SafeSin(double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double s = Math.Sin(rad);
            return Math.Abs(s) >= 1e-4 ? s : 0.0;
        }

        public static double GetParam(double[] p, int index)
        {
            return index >= 0 && index < p.Length ? p[index] : 0.0;
        }
    }
}
