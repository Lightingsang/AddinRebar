using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Calculations.Beam
{
    /// <summary>
    /// Material properties catalog for concrete and reinforcing steel per TCVN 5574:2018.
    /// </summary>
    public static class MaterialLibrary
    {
        private static readonly Dictionary<ConcreteGrade, ConcreteProperties> Concretes = new()
        {
            { ConcreteGrade.B15, new ConcreteProperties(ConcreteGrade.B15, 8.5, 0.75, 24000.0) },
            { ConcreteGrade.B20, new ConcreteProperties(ConcreteGrade.B20, 11.5, 0.90, 27500.0) },
            { ConcreteGrade.B25, new ConcreteProperties(ConcreteGrade.B25, 14.5, 1.05, 30000.0) },
            { ConcreteGrade.B30, new ConcreteProperties(ConcreteGrade.B30, 17.0, 1.15, 32500.0) },
            { ConcreteGrade.B35, new ConcreteProperties(ConcreteGrade.B35, 19.5, 1.30, 34500.0) },
            { ConcreteGrade.B40, new ConcreteProperties(ConcreteGrade.B40, 22.0, 1.40, 36000.0) }
        };

        private static readonly Dictionary<RebarGrade, RebarProperties> Rebars = new()
        {
            { RebarGrade.CB240T, new RebarProperties(RebarGrade.CB240T, 210.0, 210.0, 170.0) },
            { RebarGrade.CB300V, new RebarProperties(RebarGrade.CB300V, 260.0, 260.0, 210.0) },
            { RebarGrade.CB400V, new RebarProperties(RebarGrade.CB400V, 350.0, 350.0, 280.0) },
            { RebarGrade.CB500V, new RebarProperties(RebarGrade.CB500V, 435.0, 400.0, 300.0) }
        };

        public static ConcreteProperties GetConcrete(ConcreteGrade grade)
        {
            if (Concretes.TryGetValue(grade, out var props))
            {
                return props;
            }
            throw new ArgumentOutOfRangeException(nameof(grade), $"Unknown concrete grade: {grade}");
        }

        public static RebarProperties GetRebar(RebarGrade grade)
        {
            if (Rebars.TryGetValue(grade, out var props))
            {
                return props;
            }
            throw new ArgumentOutOfRangeException(nameof(grade), $"Unknown rebar grade: {grade}");
        }

        public static bool TryGetConcrete(string name, out ConcreteProperties? props)
        {
            props = null;
            if (string.IsNullOrWhiteSpace(name)) return false;

            var cleaned = name.Trim().ToUpperInvariant();
            foreach (var kvp in Concretes)
            {
                if (kvp.Key.ToString().Equals(cleaned, StringComparison.OrdinalIgnoreCase))
                {
                    props = kvp.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetRebar(string name, out RebarProperties? props)
        {
            props = null;
            if (string.IsNullOrWhiteSpace(name)) return false;

            var cleaned = name.Trim().ToUpperInvariant().Replace("-", string.Empty);
            foreach (var kvp in Rebars)
            {
                var keyStr = kvp.Key.ToString().ToUpperInvariant();
                var altName = kvp.Value.Name.ToUpperInvariant().Replace("-", string.Empty);
                if (keyStr.Equals(cleaned, StringComparison.OrdinalIgnoreCase) ||
                    altName.Equals(cleaned, StringComparison.OrdinalIgnoreCase))
                {
                    props = kvp.Value;
                    return true;
                }
            }
            return false;
        }
    }
}
