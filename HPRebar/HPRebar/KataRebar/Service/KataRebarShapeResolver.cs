using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Resolves RebarShape families loaded in the Revit document for Kata beam reinforcement.
/// Supports both Kata standard shape codes ("00", "05a", "15a", "41", "45", "24a", "42")
/// and standard Autodesk Revit shape names ("M_00", "M_02", "M_T1", "T1", "M_T10", "T10", etc.).
/// </summary>
public sealed class KataRebarShapeResolver
{
    private readonly IReadOnlyDictionary<string, RebarShape> _shapes;

    public KataRebarShapeResolver(IReadOnlyDictionary<string, RebarShape> shapes)
    {
        _shapes = shapes ?? throw new ArgumentNullException(nameof(shapes));
    }

    /// <summary>
    /// Loads all RebarShape elements from the active Revit document.
    /// </summary>
    public static KataRebarShapeResolver Load(Document document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        var shapes = new FilteredElementCollector(document)
            .OfClass(typeof(RebarShape))
            .Cast<RebarShape>()
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return new KataRebarShapeResolver(shapes);
    }

    /// <summary>
    /// Finds a RebarShape by its exact name or standard alias.
    /// </summary>
    public RebarShape? Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _shapes.TryGetValue(name, out var shape) ? shape : null;
    }

    /// <summary>
    /// Finds the first matching RebarShape from a list of candidate names.
    /// </summary>
    public RebarShape? FindFirst(params string[] candidateNames)
    {
        foreach (var name in candidateNames)
        {
            if (_shapes.TryGetValue(name, out var shape))
                return shape;
        }
        return null;
    }

    /// <summary>
    /// Finds a RebarShape using Kata standard shape code ("41", "45", "24a", "00", "05a", "15a", etc.).
    /// Maps to known Revit family naming conventions automatically.
    /// </summary>
    public RebarShape? FindByKataCode(string shapeCode)
    {
        if (string.IsNullOrWhiteSpace(shapeCode)) return null;

        var candidates = GetCandidatesForKataCode(shapeCode);
        return FindFirst(candidates.ToArray());
    }

    /// <summary>
    /// Closed rectangular stirrup (Kata 41, M_T1, T1, 01, M_01, Rebar Shape 1).
    /// </summary>
    public RebarShape? MainStirrup() => FindByKataCode("41");

    /// <summary>
    /// Cap stirrup / U-stirrup (Kata 45, M_T2, T2, 45, M_45, Rebar Shape 45).
    /// </summary>
    public RebarShape? CapStirrup() => FindByKataCode("45");

    /// <summary>
    /// Cross tie / C-hook (Kata 24a, 42, M_T10, T10, 10, M_10).
    /// </summary>
    public RebarShape? CrossTie() => FindByKataCode("24a");

    /// <summary>
    /// Straight bar (Kata 00, M_00, 00, Straight, Rebar Shape 00).
    /// </summary>
    public RebarShape? StraightBar() => FindByKataCode("00");

    /// <summary>
    /// Single hook L bar (Kata 05a, M_02, 02, 05a, M_05a).
    /// </summary>
    public RebarShape? SingleHookBar() => FindByKataCode("05a");

    /// <summary>
    /// Double hook bar (Kata 15a, M_11, 11, 15a, M_15a).
    /// </summary>
    public RebarShape? DoubleHookBar() => FindByKataCode("15a");

    /// <summary>
    /// Returns aliases / candidate family names for a given Kata shape code.
    /// </summary>
    public static IReadOnlyList<string> GetCandidatesForKataCode(string shapeCode)
    {
        return shapeCode.Trim().ToLowerInvariant() switch
        {
            "41" => new[] { "41", "M_41", "M_T1", "T1", "01", "M_01", "Rebar Shape 1", "Shape 1", "M_51", "51" },
            "45" => new[] { "45", "M_45", "M_T2", "T2", "Rebar Shape 45", "Shape 45", "M_11", "11" },
            "24a" or "42" => new[] { "24a", "M_24a", "42", "M_42", "M_T10", "T10", "M_T10B", "M_T10C", "10", "M_10" },
            "00" => new[] { "00", "M_00", "Shape 00", "Rebar Shape 00", "Straight" },
            "05a" or "02" => new[] { "05a", "M_05a", "02", "M_02", "Shape 02", "Rebar Shape 2", "M_T3" },
            "15a" or "11" => new[] { "15a", "M_15a", "11", "M_11", "Shape 11", "Rebar Shape 11" },
            _ => new[] { shapeCode, $"M_{shapeCode}", $"Shape {shapeCode}" }
        };
    }
}
