using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.KataExport.Model;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Header values that are not section sizes: the slab on top of the beam (B7) and every instance parameter
/// of the first element, so the window can switch the Name/Count parameters without calling Revit.
/// </summary>
public static class KataHeaderReader
{
    private const double SlabTopToleranceMm = 50.0;
    private const double SlabSideReachMm = 1000.0;

    /// <summary>Thickest floor whose top is at the top of the run; null when none touches it.</summary>
    public static (double? ThicknessMm, IReadOnlyList<string> Warnings) SlabThickness(Document doc, RevitView view, KataRunGeometry run)
    {
        var warnings = new List<string>();
        double tolerance = RevitUnits.MmToFt(SlabTopToleranceMm);
        double reach = RevitUnits.MmToFt(SlabSideReachMm);

        // The floor's own box top is its top face for the flat slabs Kata draws; no geometry is needed.
        var thicknesses = KataCandidateCollector.Near(doc, view, run, reach, 0.0, tolerance, BuiltInCategory.OST_Floors)
            .Where(floor => floor.get_BoundingBox(null) is { } box
                            && run.Pieces.Any(p => Math.Abs(box.Max.Z - p.TopFt) <= tolerance))
            .Select(floor => floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM)?.AsDouble())
            .Where(t => t is > 0)
            .Select(t => Math.Round(RevitUnits.FtToMm(t!.Value), 1))
            .Distinct()
            .ToList();

        if (thicknesses.Count > 1)
            warnings.Add($"Floors of {string.Join(", ", thicknesses)} mm meet the beam top; B7 takes the thickest.");

        return (thicknesses.Count == 0 ? null : thicknesses.Max(), warnings);
    }

    /// <summary>Instance parameter values by name, typed as Kata should receive them (text, whole number or number).</summary>
    public static IReadOnlyDictionary<string, object?> Parameters(Element element)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (Parameter parameter in element.Parameters)
        {
            string name = parameter.Definition?.Name ?? string.Empty;
            if (name.Length == 0 || values.ContainsKey(name)) continue;
            values[name] = Value(parameter);
        }

        return values;
    }

    private static object? Value(Parameter parameter)
    {
        if (!parameter.HasValue) return null;
        switch (parameter.StorageType)
        {
            case StorageType.String:
                return parameter.AsString();
            case StorageType.Integer:
                return parameter.AsInteger();
            case StorageType.Double:
                try
                {
                    return UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), parameter.GetUnitTypeId());
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException)
                {
                    return parameter.AsDouble(); // a unitless number has no unit type to convert from
                }
            default:
                return parameter.AsValueString();
        }
    }
}
