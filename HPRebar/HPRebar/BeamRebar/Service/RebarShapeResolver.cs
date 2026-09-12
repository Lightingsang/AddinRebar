using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Resolves required standard RebarShape families loaded in the Revit document,
/// supporting multiple naming conventions and national standard aliases.
/// </summary>
public sealed class RebarShapeResolver
{
    private static readonly string[] RectangularStirrupNames = { "M_T1", "T1", "01", "M_01", "Rebar Shape 1", "Shape 1" };
    private static readonly string[] CrossTieNames = { "M_T10", "T10", "M_T10B", "M_T10C", "10", "M_10" };

    private readonly IReadOnlyDictionary<string, RebarShape> _shapes;

    private RebarShapeResolver(IReadOnlyDictionary<string, RebarShape> shapes) => _shapes = shapes;

    public static RebarShapeResolver Load(Document document)
    {
        var shapes = new FilteredElementCollector(document)
            .OfClass(typeof(RebarShape))
            .Cast<RebarShape>()
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return new RebarShapeResolver(shapes);
    }

    /// <summary>Finds standard closed rectangular stirrup shape (e.g. M_T1, T1).</summary>
    public RebarShape? MainStirrup() => FindFirst(RectangularStirrupNames);

    /// <summary>Finds transverse anti-buckling cross-tie shape (e.g. M_T10, T10).</summary>
    public RebarShape? CrossTie(int tieType = 0)
    {
        string specific = tieType switch
        {
            1 => "M_T10B",
            2 => "M_T10",
            3 => "M_T10C",
            _ => "M_T10"
        };
        return Find(specific) ?? FindFirst(CrossTieNames);
    }

    /// <summary>
    /// Validates that necessary shapes are loaded in the document before starting rebar placement.
    /// </summary>
    public ValidationResult Require(bool needsCrossTies = false, bool needsSpecialStirrups = false)
    {
        if (MainStirrup() is null)
            return ValidationResult.Fail(20, "Rectangular stirrup shape (M_T1 or T1) is not loaded in the project. Load it before creating stirrups.");

        if (needsCrossTies && CrossTie() is null)
            return ValidationResult.Fail(21, "Cross-tie shape (M_T10 or T10) is not loaded in the project. Load it before creating cross-ties.");

        return ValidationResult.Ok;
    }

    public RebarShape? Find(string name) => _shapes.TryGetValue(name, out var shape) ? shape : null;

    public RebarShape? FindFirst(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (_shapes.TryGetValue(name, out var shape))
                return shape;
        }
        return null;
    }
}
