namespace HPKata.Revit.Bridge;

/// <summary>
/// Two-way adapter converting between Revit RebarShape definitions and Kata 85 shapes (dang00..dang67).
/// Handles disambiguation of symmetric vs asymmetric U-bars (|A - C| &lt; 1.0 mm).
/// </summary>
public static class RevitShapeAdapter
{
    private const double SymmetricToleranceMm = 1.0;

    /// <summary>
    /// Resolves a Revit shape name and parameter array into a standardized Kata shape code (e.g. dang01).
    /// </summary>
    public static string ResolveKataShape(string? revitShapeName, double[]? parametersMm = null)
    {
        if (string.IsNullOrWhiteSpace(revitShapeName))
        {
            return "dang01";
        }

        string trimmed = revitShapeName.Trim();

        // Direct Kata format pass-through (e.g. dang01 or 01)
        if (trimmed.StartsWith("dang", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.ToLowerInvariant();
        }

        // Try standard mapping table
        if (RevitShapeMappingTable.TryGetKataShape(trimmed, out string kataShape))
        {
            // Disambiguate U-bar: M_21 / dang02 vs dang04 (symmetric vs asymmetric)
            if ((kataShape == "dang02" || kataShape == "dang04") &&
                parametersMm != null && parametersMm.Length >= 3)
            {
                double a = parametersMm[0];
                double c = parametersMm[2];
                return Math.Abs(a - c) < SymmetricToleranceMm ? "dang02" : "dang04";
            }
            return kataShape;
        }

        return "dang00"; // Fallback to freeform
    }

    /// <summary>
    /// Maps a Kata shape code (dang01..dang67) back to the standard Revit shape name (e.g. M_00).
    /// </summary>
    public static string ResolveRevitShape(string? kataShapeCode)
    {
        if (string.IsNullOrWhiteSpace(kataShapeCode))
        {
            return "M_00";
        }

        string trimmed = kataShapeCode.Trim();
        if (RevitShapeMappingTable.TryGetRevitShape(trimmed, out string revitShape))
        {
            return revitShape;
        }

        return "M_00";
    }
}
