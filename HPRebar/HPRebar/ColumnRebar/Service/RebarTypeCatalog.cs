using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.ColumnRebar.Model;

namespace HPRebar.ColumnRebar.Service;

/// <summary>Rebar products and cover settings the document offers, ordered so the UI can list them.</summary>
public static class RebarTypeCatalog
{
    /// <summary>Every rebar bar type, thinnest first.</summary>
    public static IReadOnlyList<RebarTypeInfo> BarTypes(Document document) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(RebarBarType))
            .Cast<RebarBarType>()
            .Select(barType => new RebarTypeInfo
            {
                Name = barType.Name,
                DiameterMm = RevitUnits.FtToMm(barType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble()),
                BarType = barType
            })
            .OrderBy(info => info.DiameterMm)
            .ToList();

    /// <summary>Every cover setting, thinnest first.</summary>
    public static IReadOnlyList<RebarCoverType> CoverTypes(Document document) =>
        new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfClass(typeof(RebarCoverType))
            .Cast<RebarCoverType>()
            .OrderBy(cover => cover.CoverDistance)
            .ToList();

    /// <summary>
    ///     Cover the tool starts from: the second thinnest, matching the original tool's default, falling
    ///     back to the only one available in a document that defines just one.
    /// </summary>
    public static double DefaultCoverMm(Document document)
    {
        var covers = CoverTypes(document);

        if (covers.Count == 0) return 0d;

        var chosen = covers.Count > 1 ? covers[1] : covers[0];

        return RevitUnits.FtToMm(chosen.CoverDistance);
    }

    /// <summary>
    ///     Bar the tool starts from: the fourth thinnest, matching the original tool's default, falling back
    ///     to the thickest available when the document offers fewer.
    /// </summary>
    public static RebarTypeInfo? DefaultBarType(IReadOnlyList<RebarTypeInfo> barTypes)
    {
        if (barTypes.Count == 0) return null;

        return barTypes.Count > 3 ? barTypes[3] : barTypes[barTypes.Count - 1];
    }
}
