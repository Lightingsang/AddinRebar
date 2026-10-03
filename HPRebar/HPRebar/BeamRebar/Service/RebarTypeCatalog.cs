using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Catalog of the RebarBarType and RebarCoverType elements in the Revit document.
/// </summary>
public sealed class RebarTypeCatalog
{
    private readonly IReadOnlyList<RebarTypeInfo> _barTypes;
    private readonly IReadOnlyList<RebarCoverType> _coverTypes;

    public RebarTypeCatalog(Document doc)
    {
        _barTypes = LoadBarTypes(doc);

        _coverTypes = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .OfClass(typeof(RebarCoverType))
            .Cast<RebarCoverType>()
            .OrderBy(c => c.CoverDistance)
            .ToList();
    }

    public IReadOnlyList<RebarTypeInfo> BarTypes => _barTypes;

    public static IReadOnlyList<RebarTypeInfo> LoadBarTypes(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(RebarBarType))
            .Cast<RebarBarType>()
            .Select(b => new RebarTypeInfo
            {
                Name = b.Name,
                DiameterMm = RevitUnits.FtToMm(b.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble()),
                BarType = b
            })
            .OrderBy(b => b.DiameterMm)
            .ToList();
    }

    public RebarTypeInfo? FindBarType(string name, double targetDiameterMm)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var match = _barTypes.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }

        // Match closest diameter within 0.5 mm tolerance
        var closest = _barTypes.FirstOrDefault(b => Math.Abs(b.DiameterMm - targetDiameterMm) < 0.5);
        if (closest is not null) return closest;

        // Fallback to closest overall
        return _barTypes.OrderBy(b => Math.Abs(b.DiameterMm - targetDiameterMm)).FirstOrDefault();
    }

    public double DefaultCoverMm() => _coverTypes.Count > 0 ? RevitUnits.FtToMm(_coverTypes[0].CoverDistance) : 25.0;
}
