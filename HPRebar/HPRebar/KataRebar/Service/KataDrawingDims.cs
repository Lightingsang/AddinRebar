using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.KataRebar.Calculators;
using Serilog;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Kata's dimension chains (<see cref="KataDimChains"/>) as Revit dimensions on a view: at every point a chain measures
/// to, a short tick in &lt;Invisible lines&gt; across the measured direction, and one dimension through the chain's
/// ticks on Kata's dimension line. Ticks and dimensions carry the run's drafting tag, so a re-run replaces them.
/// </summary>
internal static class KataDrawingDims
{
    /// <summary>Half length of a tick (mm, model).</summary>
    private const double TickHalfMm = 5.0;

    /// <param name="toModel">A point of the drawing (its X right, Z up, mm) in the model, on the view's plane.</param>
    /// <returns>Dimensions created and chains Revit refused (logged).</returns>
    public static (int Created, int Refused) Create(Document doc, RevitView view, string runKey, IReadOnlyList<KataDimChain> chains, Func<double, double, XYZ> toModel)
    {
        var invisible = KataLineStyles.Invisible(doc);
        var type = doc.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType);
        int created = 0, refused = 0;
        foreach (var chain in chains.Where(c => c.Stations.Count >= 2))
        {
            XYZ Point(double along, double across) => chain.Vertical ? toModel(across, along) : toModel(along, across);
            var tickDirection = (chain.Vertical ? view.RightDirection : view.UpDirection).Normalize();
            var references = new ReferenceArray();
            var ticks = new List<ElementId>();
            foreach (var station in chain.Stations)
            {
                var p = Point(station.Along, station.Across);
                var half = tickDirection.Multiply(RevitUnits.MmToFt(TickHalfMm));
                var tick = doc.Create.NewDetailCurve(view, Line.CreateBound(p - half, p + half));
                tick.LineStyle = invisible;
                KataDraftingStorage.Write(tick, runKey, KataDraftingStorage.DraftingKind);
                references.Append(tick.GeometryCurve.Reference);
                ticks.Add(tick.Id);
            }

            var first = chain.Stations[0].Along;
            var last = chain.Stations[chain.Stations.Count - 1].Along;
            var line = Line.CreateBound(Point(first, chain.LineAt), Point(last, chain.LineAt));
            try
            {
                var dimension = type != ElementId.InvalidElementId && doc.GetElement(type) is DimensionType dimensionType
                    ? doc.Create.NewDimension(view, line, references, dimensionType)
                    : doc.Create.NewDimension(view, line, references);
                KataDraftingStorage.Write(dimension, runKey, KataDraftingStorage.DraftingKind);
                created++;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException ex)
            {
                // A chain Revit cannot dimension (ticks it finds parallel or coincident) is skipped, the others drawn.
                doc.Delete(ticks);
                Log.Warning("Kata Rebar: dimension chain on {View} at {Line:0} ({Count} points) refused: {Reason}", view.Name, chain.LineAt, chain.Stations.Count, ex.Message);
                refused++;
            }
        }

        return (created, refused);
    }
}
