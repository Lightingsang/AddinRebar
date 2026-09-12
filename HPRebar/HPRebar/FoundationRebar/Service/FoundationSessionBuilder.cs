using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.FoundationRebar.Models;
using HPRebar.FoundationRebar.Model;
using Serilog;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
///     Reads everything the window opens with: the slab's geometry snapshot, the bar types the document
///     actually has, and a default spec. All read-only queries, so the command does this before handing
///     over to the modeless window.
/// </summary>
public static class FoundationSessionBuilder
{
    /// <summary>Returns null when the document holds no rebar bar types to build from.</summary>
    public static FoundationSession? Build(Document document, Floor floor)
    {
        var snapshot = FoundationSolidFaceReader.Read(floor);

        Log.Information(
            "Foundation Rebar loaded snapshot: L={Length:F0}mm, W={Width:F0}mm, H={Thickness:F0}mm, TopZ={TopZ:F0}mm, BotZ={BotZ:F0}mm",
            snapshot.Length, snapshot.Width, snapshot.Thickness, snapshot.TopZ, snapshot.BottomZ);

        var barTypes = new FilteredElementCollector(document)
            .OfClass(typeof(RebarBarType))
            .Cast<RebarBarType>()
            .OrderBy(barType => barType.BarNominalDiameter)
            .ToList();

        if (barTypes.Count == 0) return null;

        return new FoundationSession(document, floor, snapshot, barTypes, new FoundationRebarSpec());
    }
}
