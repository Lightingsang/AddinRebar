using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Finds the closed rectangular stirrup shape of the project by the names offices use for it (Kata "41",
/// Autodesk "M_T1"/"T1"...). A candidate counts only if it really is a stirrup/tie with at least four
/// segments, so a straight or L shape that happens to share a name is never scaled into a stirrup.
/// </summary>
public static class KataRebarShapeResolver
{
    public static readonly IReadOnlyList<string> ClosedStirrupNames = new[]
    {
        "41", "M_41", "M_T1", "T1", "Rebar Shape 41", "Shape 41", "01", "M_01", "Rebar Shape 1", "Shape 1"
    };

    public static RebarShape? ClosedStirrup(Document doc)
    {
        var shapes = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarShape))
            .Cast<RebarShape>()
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var name in ClosedStirrupNames)
        {
            if (shapes.TryGetValue(name, out var shape) && IsClosedStirrup(shape))
                return shape;
        }

        return null;
    }

    private static bool IsClosedStirrup(RebarShape shape) =>
        shape.RebarStyle == RebarStyle.StirrupTie
        && shape.GetRebarShapeDefinition() is RebarShapeDefinitionBySegments { NumberOfSegments: >= 4 };
}
