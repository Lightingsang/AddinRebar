using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Service;

/// <summary>Places the perimeter ties of one column segment, one Revit element per evenly spaced group.</summary>
public static class StirrupCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        ColumnFaces faces,
        ColumnSection section,
        RebarShape shape,
        RebarBarType barType,
        double coverMm,
        IReadOnlyList<StirrupRun> runs,
        string partitionName)
    {
        var created = new List<Rebar>(runs.Count);

        foreach (var run in runs)
        {
            var placement = section.Shape == SectionShape.Rectangle
                ? StirrupGeometry.Rectangle(faces, coverMm, section.B, section.H, 0, 0, run.StartOffset)
                : StirrupGeometry.Circle(faces, section.D, coverMm, run.StartOffset);

            created.Add(Place(document, faces.Element, shape, barType, placement, run, partitionName));
        }

        return created;
    }

    /// <summary>
    ///     Creates one tie and spreads it up the column. The three flags tell Revit to include the bar it
    ///     was created from and to keep both ends of the run anchored as the spacing is applied.
    /// </summary>
    internal static Rebar Place(
        Document document,
        Element host,
        RebarShape shape,
        RebarBarType barType,
        StirrupPlacement placement,
        StirrupRun run,
        string partitionName)
    {
        var rebar = Rebar.CreateFromRebarShape(
            document, shape, barType, host, placement.Origin, placement.XVector, placement.YVector);

        var accessor = rebar.GetShapeDrivenAccessor();

        accessor.ScaleToBox(placement.Origin, placement.Width, placement.Height);
        accessor.SetLayoutAsNumberWithSpacing(run.Count, RevitUnits.MmToFt(run.Spacing), true, true, true);

        MainBarCreator.SetPartition(rebar, partitionName);

        return rebar;
    }
}
