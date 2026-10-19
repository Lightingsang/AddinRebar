namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// Explicit Revit family and symbol definition resolved ahead-of-time.
/// Eliminates runtime guessing in Revit scripts.
/// </summary>
public sealed record ResourceResolutionSpec
{
    public string FamilyName { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public bool RequiresProceduralCreation { get; init; }
    public string? BaseTypeName { get; init; }
    public IReadOnlyDictionary<string, double> DimensionParametersMm { get; init; } = new Dictionary<string, double>();

    /// <summary>
    /// Computes deterministic canonical representation covering family, symbol/type,
    /// base type, and all dimension parameters in invariant culture and key-sorted order.
    /// </summary>
    public string GetCanonicalString()
    {
        var sb = new StringBuilder();
        sb.Append(FamilyName).Append(':').Append(TypeName);
        if (!string.IsNullOrEmpty(BaseTypeName))
        {
            sb.Append("(base:").Append(BaseTypeName).Append(')');
        }
        if (DimensionParametersMm != null && DimensionParametersMm.Count > 0)
        {
            sb.Append('[');
            bool first = true;
            foreach (var kvp in DimensionParametersMm.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (!first) sb.Append(',');
                sb.Append(kvp.Key).Append('=').Append(kvp.Value.ToString("F2", CultureInfo.InvariantCulture));
                first = false;
            }
            sb.Append(']');
        }
        return sb.ToString();
    }
}
