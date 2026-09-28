using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// Reads the stirrup section of a span from sheet 'Dam' (rows 25-44). The column pair support | span holds a
/// stirrup type on the left ("Đai □", "Đai U", "Đai C", picked from the list in A25:A27) and the top bars it
/// wraps on the right ("3-4", "2"). Kata draws an entry only when the right cell is filled, so a type without
/// bars is a leftover of the template, not a stirrup.
/// </summary>
public static class KataStirrupSectionParser
{
    private const int FirstRow = 25;
    private const int LastRow = 44;

    /// <param name="leftColumn">1-based column of the pair's type cells (the support column left of the span).</param>
    public static IReadOnlyList<KataStirrupBranchSpec> ParsePair(IKataDamCellAccessor accessor, int leftColumn)
    {
        var list = new List<KataStirrupBranchSpec>();
        if (leftColumn < 1) return list;

        for (int row = FirstRow; row <= LastRow; row++)
        {
            string? legs = accessor.GetText(row, leftColumn + 1)?.Trim();
            if (string.IsNullOrEmpty(legs)) continue;

            string type = accessor.GetText(row, leftColumn)?.Trim() ?? "";
            list.Add(new KataStirrupBranchSpec(ShapeOf(type), legs!, KataDamCellAccessorExtensions.ToAddress(row, leftColumn)));
        }

        return list;
    }

    /// <summary>Kata reads the last character of the type: "U" cap stirrup, "C" cross tie, anything else closed.</summary>
    internal static KataStirrupShapeType ShapeOf(string type)
    {
        if (type.Length == 0) return KataStirrupShapeType.ClosedHoop;

        return char.ToUpperInvariant(type[type.Length - 1]) switch
        {
            'U' => KataStirrupShapeType.CapStirrup,
            'C' => KataStirrupShapeType.CrossTie,
            _ => KataStirrupShapeType.ClosedHoop
        };
    }
}
