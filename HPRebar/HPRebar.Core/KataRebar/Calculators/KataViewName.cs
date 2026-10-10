using System;
using System.Linq;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// A view name Revit accepts made from a beam name (cell B3): the characters Revit refuses in names become "-",
/// surrounding blanks go, and an empty result falls back to the given name.
/// </summary>
public static class KataViewName
{
    /// <summary>Characters Revit does not allow in a view name.</summary>
    private const string Refused = "\\:{}[]|;<>?`~";

    public static string Clean(string? name, string fallback)
    {
        if (fallback is null) throw new ArgumentNullException(nameof(fallback));
        string cleaned = new string((name ?? "").Select(c => Refused.IndexOf(c) >= 0 || char.IsControl(c) ? '-' : c).ToArray()).Trim();
        return cleaned.Length == 0 ? fallback : cleaned;
    }
}
