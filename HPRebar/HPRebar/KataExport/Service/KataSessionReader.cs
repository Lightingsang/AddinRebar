using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Models;
using HPRebar.KataExport.Model;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Reads everything the Kata sheet needs from the picked beams in one pass on the Revit API thread.
/// </summary>
public static class KataSessionReader
{
    public static KataExportSession Read(Document doc, RevitView view, IReadOnlyList<Element> beams)
    {
        var run = KataRunReader.Read(doc, beams);
        var (supports, supportWarnings) = KataSupportCollector.Collect(doc, view, run);
        var grids = KataGridReader.Read(doc, view, run);
        var (slab, slabWarnings) = KataHeaderReader.SlabThickness(doc, view, run);
        var first = run.Pieces[0];

        var warnings = supportWarnings.Concat(grids.Warnings).Concat(slabWarnings).ToList();
        if (first.ReferenceLevel is null) warnings.Add("The first beam has no reference level; B10 is +0.000.");

        return new KataExportSession
        {
            BeamIds = run.Pieces.Select(p => p.Element.Id).ToList(),
            Pieces = run.Pieces
                .Select(p => new KataBeamPiece(p.Stations, p.WidthMm, p.HeightMm, p.ZOffsetMm, p.Element.UniqueId))
                .ToList(),
            Supports = supports,
            Grids = grids.Crossings,
            HeightMm = first.HeightMm,
            WidthMm = first.WidthMm,
            LevelElevationMm = first.ReferenceLevel is { } level ? RevitUnits.FtToMm(level.ProjectElevation) : 0.0,
            SlabThicknessMm = slab,
            AxisGridName = grids.AxisGridName,
            AxisOffsetMm = grids.AxisOffsetMm,
            FirstBeamParameters = KataHeaderReader.Parameters(first.Element),
            Warnings = warnings
        };
    }
}
