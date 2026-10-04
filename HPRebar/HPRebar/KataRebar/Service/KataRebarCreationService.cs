using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>Bars and Revit elements created for the longitudinal bars of a plan.</summary>
/// <param name="FallbackSets">Sets Revit could not lay out, drawn as single bars instead.</param>
public readonly record struct KataLongitudinalOutcome(int Bars, int Elements, int Sets, int FallbackSets);

/// <summary>
/// Creates the longitudinal bars of a plan — continuous top and bottom bars, additional bars and side bars —
/// drawn with their anchorage bends in the curves and hosted on the framing element under the middle of the
/// bar. Identical, evenly spaced bars of one cell are one Rebar laid out as a fixed number across the beam
/// (<see cref="KataLongitudinalSetGrouping"/>); a lone bar is a single Rebar. A set Revit cannot lay out falls
/// back to single bars at the same positions. Revit may snap a new bar's plane, or the first bar of a set, to the
/// host's cover (a bar 29 mm off the centre line moved 4 mm, measured in Revit 2026): a single bar is moved back
/// across the beam to its planned offset, a set's two ends get their constraint distances corrected; a bar or set
/// pulled up or down is moved back to its planned level (<see cref="KataRebarSectionFit"/>).
/// </summary>
public static class KataRebarCreationService
{
    /// <summary>A set's first and last bar may sit this far across the beam from their planned offsets (mm).</summary>
    private const double PositionToleranceMm = 0.5;

    /// <summary>A bar further than this from its planned offset is moved back (mm).</summary>
    private const double AlignToleranceMm = 0.05;

    public static KataLongitudinalOutcome CreateLongitudinalBars(
        Document doc,
        KataRebarPlan plan,
        KataBeamPlacement placement,
        IReadOnlyDictionary<double, RebarBarType> barTypes)
    {
        int bars = 0, elements = 0, sets = 0, fallbacks = 0;
        foreach (var set in KataLongitudinalSetGrouping.Group(plan.Layout.LongitudinalBars))
        {
            if (!set.IsSingle)
            {
                if (TryCreateSet(doc, plan, placement, barTypes, set))
                {
                    bars += set.Count;
                    elements++;
                    sets++;
                    continue;
                }

                fallbacks++;
            }

            foreach (var bar in set.Bars)
            {
                CreateSingle(doc, plan, placement, barTypes, bar);
                bars++;
                elements++;
            }
        }

        return new KataLongitudinalOutcome(bars, elements, sets, fallbacks);
    }

    private static Rebar CreateSingle(Document doc, KataRebarPlan plan, KataBeamPlacement placement, IReadOnlyDictionary<double, RebarBarType> barTypes, KataRebarCurve bar)
    {
        var host = HostOf(placement, bar);
        Rebar rebar;
        try
        {
            rebar = KataRebarCurveFactory.Create(
                doc,
                RebarStyle.Standard,
                barTypes[bar.Diameter],
                host,
                placement.Mapper.AxisY,
                KataRebarCurveFactory.Curves(bar.Polyline, placement.Mapper));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // Name the bar: the layout has hundreds and Revit only says it refused one.
            throw new InvalidOperationException($"{ex.Message} Thanh {bar.BarDescription} số {bar.BarNumber}: {Describe(bar.Polyline)}", ex);
        }

        KataRebarStamp.Apply(rebar, host, plan.Spec.BeamName, KataRebarStamp.Mark(bar.BarNumber, bar.BarMark));
        AlignAcross(doc, rebar, bar.TransverseY, placement);
        var (a, b) = KataRebarSectionFit.LongestLevelSegment(bar.Polyline);
        KataRebarSectionFit.Fit(doc, rebar, placement.Mapper, a, b, acrossToo: false);
        return rebar;
    }

    private static string Describe(Polyline3 polyline) =>
        string.Join(" ", polyline.Points.Select(p => FormattableString.Invariant($"({p.X:0},{p.Y:0},{p.Z:0})")));

    /// <summary>
    /// Moves the bar (bar 0 of a set) back to its planned transverse offset when Revit snapped its plane to the
    /// host's cover on creation or layout; the move holds through later regenerations.
    /// </summary>
    private static void AlignAcross(Document doc, Rebar rebar, double plannedY, KataBeamPlacement placement)
    {
        doc.Regenerate();
        double y = placement.Mapper.ToLocal(FirstPoint(rebar)).Y;
        double shift = plannedY - y;
        if (Math.Abs(shift) <= AlignToleranceMm) return;

        ElementTransformUtils.MoveElement(doc, rebar.Id, placement.Mapper.AxisY * (shift / 304.8));
        doc.Regenerate();
    }

    /// <summary>
    /// Brings bar <paramref name="index"/> of a set back to its planned offset by correcting the constraint of the
    /// distribution end that carries it (cover or host face). Which way the distance runs depends on the face, so
    /// the correction is tried one way, measured, and reversed when it made things worse.
    /// </summary>
    private static void FitEnd(Document doc, Rebar rebar, RebarHandleType end, int index, double plannedY, KataBeamPlacement placement)
    {
        var manager = rebar.GetRebarConstraintsManager();
        var handle = manager.GetAllHandles().FirstOrDefault(h => h.GetHandleType() == end);
        if (handle is null) return;

        double error = BarY(rebar, index, placement) - plannedY;
        if (Math.Abs(error) <= AlignToleranceMm) return;

        var original = manager.GetCurrentConstraintOnHandle(handle);
        if (original is null) return;
        bool toCover = original.IsToCover();
        // Only a cover or host-face distance can be corrected; any other constraint is left to the position check.
        if (!toCover && original.GetConstraintType() != RebarConstraintType.FixedDistanceToHostFace) return;
        double distance = toCover ? original.GetDistanceToTargetCover() : original.GetDistanceToTargetHostFace();

        foreach (double sign in new[] { 1.0, -1.0 })
        {
            var constraint = manager.GetCurrentConstraintOnHandle(handle);
            double target = distance + sign * error / 304.8;
            if (toCover) constraint.SetDistanceToTargetCover(target);
            else constraint.SetDistanceToTargetHostFace(target);
            // Multi-version: preferred rebar constraint — SetPreferredConstraint replaced SetPreferredConstraintForHandle in Revit 2025.
#if REVIT2025_OR_GREATER
            manager.SetPreferredConstraint(constraint);
#else
            manager.SetPreferredConstraintForHandle(handle, constraint);
#endif
            doc.Regenerate();
            if (Math.Abs(BarY(rebar, index, placement) - plannedY) <= AlignToleranceMm) return;
        }
    }

    private static double BarY(Rebar rebar, int index, KataBeamPlacement placement) =>
        placement.Mapper.ToLocal(rebar.GetShapeDrivenAccessor().GetBarPositionTransform(index).OfPoint(FirstPoint(rebar))).Y;

    private static XYZ FirstPoint(Rebar rebar) =>
        rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0)[0].GetEndPoint(0);

    private static bool TryCreateSet(Document doc, KataRebarPlan plan, KataBeamPlacement placement, IReadOnlyDictionary<double, RebarBarType> barTypes, KataLongitudinalSet set)
    {
        using var sub = new SubTransaction(doc);
        sub.Start();
        try
        {
            var rebar = CreateSingle(doc, plan, placement, barTypes, set.Base);
            var accessor = rebar.GetShapeDrivenAccessor();

            // The base bar is the one with the smallest offset, so the set grows towards +Y: on the normal's side
            // unless Revit turned the bar's normal around.
            bool onNormalSide = accessor.Normal.DotProduct(placement.Mapper.AxisY) > 0.0;
            accessor.SetLayoutAsFixedNumber(set.Count, set.ArrayLength / 304.8, onNormalSide, true, true);
            doc.Regenerate();
            FitEnd(doc, rebar, RebarHandleType.RebarPlane, 0, set.Bars[0].TransverseY, placement);
            FitEnd(doc, rebar, RebarHandleType.OutOfPlaneExtent, set.Count - 1, set.Bars[set.Count - 1].TransverseY, placement);
            var (a, b) = KataRebarSectionFit.LongestLevelSegment(set.Base.Polyline);
            double left = KataRebarSectionFit.Fit(doc, rebar, placement.Mapper, a, b, acrossToo: false);
            if (left > PositionToleranceMm)
                throw new InvalidOperationException($"bộ lệch cao độ {left:0.#} mm so với thiết kế");

            CheckPositions(rebar, set, placement);
            sub.Commit();
            return true;
        }
        catch (Exception ex) when (ex is Autodesk.Revit.Exceptions.ApplicationException or InvalidOperationException or ArgumentException)
        {
            sub.RollBack();
            Log.Warning(ex, "Kata Rebar: fixed-number set {Mark} ({Count}Ø{Dia}) failed ({Reason}); drawing single bars", set.Base.BarMark, set.Count, set.Base.Diameter, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// The set's first and last bar must sit at their planned offsets across the beam; otherwise the set is
    /// rolled back and drawn as single bars. Bar positions are read through the position transforms:
    /// <c>GetCenterlineCurves(…, index)</c> returns the first bar's geometry whatever the index.
    /// </summary>
    private static void CheckPositions(Rebar rebar, KataLongitudinalSet set, KataBeamPlacement placement)
    {
        if (rebar.NumberOfBarPositions != set.Count)
            throw new InvalidOperationException($"bộ có {rebar.NumberOfBarPositions} vị trí thay vì {set.Count}");

        foreach (int index in new[] { 0, set.Count - 1 })
        {
            double y = BarY(rebar, index, placement);
            double planned = set.Bars[index].TransverseY;
            if (Math.Abs(y - planned) > PositionToleranceMm)
                throw new InvalidOperationException($"thanh {index + 1} nằm ở y = {y:0.#} thay vì {planned:0.#}");
        }
    }

    private static Element HostOf(KataBeamPlacement placement, KataRebarCurve bar)
    {
        var points = bar.Polyline.Points;
        return placement.HostAt((points[0].X + points[points.Count - 1].X) / 2.0);
    }
}
