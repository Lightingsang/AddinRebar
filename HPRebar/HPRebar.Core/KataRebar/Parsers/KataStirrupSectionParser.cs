using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// Reads the stirrup sections of sheet 'Dam' (rows 25-44). Each column pair (C/D, E/F...) holds a stirrup
/// type on the left ("Đai □", "Đai U", "Đai C", picked from the list in A25:A27) and the main bars it wraps
/// on the right ("3-4", "2"). Kata draws an entry only when the right cell is filled, so a type without bars
/// is a leftover of the template, not a stirrup.
/// </summary>
public static class KataStirrupSectionParser
{
    private const int FirstRow = 25;
    private const int LastRow = 44;

    /// <summary>The outer closed hoop, which every section has, followed by the inner stirrups found.</summary>
    public static IReadOnlyList<KataStirrupBranchSpec> Parse(
        IKataDamCellAccessor accessor,
        int firstColumn,
        int lastColumn,
        List<KataCellNote> notes)
    {
        var list = new List<KataStirrupBranchSpec> { KataStirrupBranchSpec.Outer };

        for (int col = firstColumn; col < lastColumn; col += 2)
        {
            for (int row = FirstRow; row <= LastRow; row++)
            {
                string? legs = accessor.GetText(row, col + 1)?.Trim();
                if (string.IsNullOrEmpty(legs))
                    continue;

                string type = accessor.GetText(row, col)?.Trim() ?? "";
                string address = KataDamCellAccessorExtensions.ToAddress(row, col);
                list.Add(new KataStirrupBranchSpec(ShapeOf(type), legs!, address));
                notes.Add(new KataCellNote(address, $"{type} {legs}".Trim(), "đai trong"));
            }
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
