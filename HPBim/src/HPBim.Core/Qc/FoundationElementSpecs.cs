namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;

/// <summary>
/// Specification parameters for structural foundation elements based on CAD drawing details.
/// </summary>
public sealed record FootingSpec(
    string TypeName,
    int PileCount,
    double WidthMm,
    double LengthMm,
    double HeightMm,
    double TopElevationMm,
    double BottomElevationMm,
    double PileTopElevationMm,
    int ConcreteGrade);

public static class FoundationElementSpecs
{
    public const double DefaultTopElevationMm = -1600.0;
    public const double MinPileEdgeClearanceMm = 150.0;
    public const double PileEmbedmentMm = 100.0;
    public const double PileDiameterMm = 400.0;

    private static readonly Dictionary<string, FootingSpec> Specs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ĐC-1"]  = new("ĐC-1",  1,  700.0,  700.0, 1000.0, -1600.0, -2600.0, -2500.0, 300),
        ["ĐC-2"]  = new("ĐC-2",  2,  900.0, 1600.0, 1000.0, -1600.0, -2600.0, -2500.0, 300),
        ["ĐC-2'"] = new("ĐC-2'", 2,  900.0, 1600.0, 1000.0, -1600.0, -2600.0, -2500.0, 300),
        ["ĐC-3"]  = new("ĐC-3",  3, 1000.0, 2200.0, 1000.0, -1600.0, -2600.0, -2500.0, 300),
        ["ĐC-3'"] = new("ĐC-3'", 3, 1000.0, 2200.0, 1000.0, -1600.0, -2600.0, -2500.0, 400),
        ["ĐC-4"]  = new("ĐC-4",  4, 1600.0, 1600.0, 1200.0, -1600.0, -2800.0, -2700.0, 300),
        ["ĐC-5"]  = new("ĐC-5",  5, 1600.0, 2500.0, 1200.0, -1600.0, -2800.0, -2700.0, 300),
        ["ĐC-6"]  = new("ĐC-6",  6, 1600.0, 2500.0, 1300.0, -1600.0, -2900.0, -2800.0, 400),
    };

    public static bool TryGetSpec(string typeName, out FootingSpec spec)
    {
        string norm = NormalizeTypeName(typeName);
        return Specs.TryGetValue(norm, out spec!);
    }

    public static string NormalizeTypeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        string s = raw.Trim();
        if (s.StartsWith("ĐC-", StringComparison.OrdinalIgnoreCase) || s.StartsWith("DC-", StringComparison.OrdinalIgnoreCase))
        {
            // e.g. ĐC-2-04 -> ĐC-2, ĐC-3'-02 -> ĐC-3'
            string[] parts = s.Split('-');
            if (parts.Length >= 2)
            {
                string baseType = "ĐC-" + parts[1];
                return baseType.Replace("'", "'");
            }
        }
        return s;
    }
}
