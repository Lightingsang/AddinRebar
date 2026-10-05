using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;
using Serilog;
using RevitUnits = HPRebar.KataExport.Service.RevitUnits;

namespace HPRebar.KataRebar.Service;

/// <summary>Stirrups created for one beam: rebar sets, plus single bars drawn where a set could not be made.</summary>
public sealed record KataStirrupOutcome(int Sets, int SingleBars);

/// <summary>
/// One shape-driven rebar set per stirrup zone: the closed stirrup shape is scaled to the zone's
/// out-to-out box and laid out as "number with spacing" along the beam. A zone whose set fails is rolled
/// back on its own and drawn as single bars from the layout's curves instead, so nothing is duplicated.
/// </summary>
public static class KataStirrupSetCreator
{
    private const double PositionToleranceFt = 1.0e-3;

    public static KataStirrupOutcome Create(
        Document doc,
        KataRebarPlan plan,
        KataBeamPlacement placement,
        RebarShape? shape,
        RebarBarType barType)
    {
        int sets = 0, singles = 0;
        foreach (var zone in plan.Layout.StirrupZones)
        {
            if (zone.Count <= 0 || zone.OutToOutWidth <= 0.0 || zone.OutToOutHeight <= 0.0) continue;

            var host = placement.HostAt((zone.StartStationX + zone.EndStationX) / 2.0);
            if (shape is not null && zone.StirrupType == KataStirrupShapeType.ClosedHoop && TryCreateSet(doc, zone, placement, shape, barType, host, plan.Spec.BeamName, plan.Rules.StirrupCover))
            {
                sets++;
                continue;
            }

            singles += CreateSingles(doc, plan, zone, placement, barType, host);
        }

        return new KataStirrupOutcome(sets, singles);
    }

    private static bool TryCreateSet(
        Document doc,
        KataStirrupZoneResult zone,
        KataBeamPlacement placement,
        RebarShape shape,
        RebarBarType barType,
        Element host,
        string beamName,
        double stirrupCoverMm)
    {
        var mapper = placement.Mapper;
        double coverFt = RevitUnits.MmToFt(stirrupCoverMm);
        using var sub = new SubTransaction(doc);
        sub.Start();
        try
        {
            // Laid from the +Y face towards −Y: the shape's hooks land at the top on −Y, the top right of Kata's
            // section, which looks along the beam (B01 section 2-2).
            var origin = mapper.ToXyz(zone.StartStationX, zone.BoxMinY + zone.OutToOutWidth, zone.BoxMinZ);
            var across = -mapper.AxisY;
            var up = mapper.AxisZ;

            var rebar = Rebar.CreateFromRebarShape(doc, shape, barType, host, origin, across, up)
                        ?? throw new InvalidOperationException($"Revit không tạo được đai từ hình '{shape.Name}'.");
            var accessor = rebar.GetShapeDrivenAccessor();
            accessor.ScaleToBox(origin, across * RevitUnits.MmToFt(zone.OutToOutWidth), up * RevitUnits.MmToFt(zone.OutToOutHeight));
            Layout(doc, rebar, zone, barsOnNormalSide: true, coverFt);

            // The set must grow along +X; a shape whose normal points the other way is laid out again.
            if (zone.Count > 1 && !GrowsAlong(rebar, mapper.AxisX))
            {
                Layout(doc, rebar, zone, barsOnNormalSide: false, coverFt);
                if (!GrowsAlong(rebar, mapper.AxisX))
                    throw new InvalidOperationException("Bộ đai không rải theo trục dầm ở cả hai hướng.");
            }

            if (rebar.NumberOfBarPositions != zone.Count)
                throw new InvalidOperationException($"Bộ đai có {rebar.NumberOfBarPositions} vị trí thay vì {zone.Count}.");

            // ScaleToBox compromises instead of failing when a shape cannot fit; a wrong size falls back to single bars.
            CheckFirstStirrup(rebar, zone, mapper, RevitUnits.FtToMm(barType.BarNominalDiameter));

            KataRebarStamp.Apply(rebar, host, beamName, zone.BarNumber);
            sub.Commit();
            return true;
        }
        catch (Exception ex) when (ex is Autodesk.Revit.Exceptions.ApplicationException or InvalidOperationException or ArgumentException)
        {
            sub.RollBack();
            Log.Warning(ex, "Kata Rebar: stirrup set for zone {Zone} of span {Span} failed ({Reason}); drawing single bars", zone.ZoneName, zone.SpanIndex + 1, ex.Message);
            return false;
        }
    }

    /// <summary>Lays the set out along the beam, then puts its edges back at the plan's stirrup cover.</summary>
    private static void Layout(Document doc, Rebar rebar, KataStirrupZoneResult zone, bool barsOnNormalSide, double stirrupCoverFt)
    {
        var accessor = rebar.GetShapeDrivenAccessor();
        if (zone.Count <= 1)
            accessor.SetLayoutAsSingle();
        else
            accessor.SetLayoutAsNumberWithSpacing(zone.Count, RevitUnits.MmToFt(zone.Spacing), barsOnNormalSide, true, true);

        doc.Regenerate();
        KataStirrupCoverFit.Apply(doc, rebar, stirrupCoverFt);
    }

    /// <summary>
    /// The first stirrup must sit at the zone's first station with the out-to-out box of the layout. The real
    /// centreline is measured, bends and hooks included: with them suppressed Revit straightens the hook laps
    /// past the corners and the box reads one bar diameter too large.
    /// </summary>
    private static void CheckFirstStirrup(Rebar rebar, KataStirrupZoneResult zone, HPRebar.BeamRebar.Service.PointMapper mapper, double barDiameterMm)
    {
        var points = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0)
            .SelectMany(c => new[] { c.GetEndPoint(0), c.GetEndPoint(1) })
            .Select(mapper.ToLocal)
            .ToList();
        if (points.Count == 0) throw new InvalidOperationException("Bộ đai không có đường tim.");

        double width = points.Max(p => p.Y) - points.Min(p => p.Y) + barDiameterMm;
        double height = points.Max(p => p.Z) - points.Min(p => p.Z) + barDiameterMm;
        double minY = points.Min(p => p.Y) - barDiameterMm / 2.0;
        double minZ = points.Min(p => p.Z) - barDiameterMm / 2.0;
        double x = points.Average(p => p.X);

        if (Math.Abs(width - zone.OutToOutWidth) > BoxToleranceMm || Math.Abs(height - zone.OutToOutHeight) > BoxToleranceMm
            || Math.Abs(minY - zone.BoxMinY) > BoxToleranceMm || Math.Abs(minZ - zone.BoxMinZ) > BoxToleranceMm
            || Math.Abs(x - zone.StartStationX) > BoxToleranceMm)
        {
            throw new InvalidOperationException(
                $"Đai đầu tiên {width:0}×{height:0} tại ({x:0}, {minY:0}, {minZ:0}) thay vì {zone.OutToOutWidth:0}×{zone.OutToOutHeight:0} tại ({zone.StartStationX:0}, {zone.BoxMinY:0}, {zone.BoxMinZ:0}).");
        }
    }

    private const double BoxToleranceMm = 3.0;

    private static bool GrowsAlong(Rebar rebar, XYZ along)
    {
        var accessor = rebar.GetShapeDrivenAccessor();
        var first = accessor.GetBarPositionTransform(0).Origin;
        var last = accessor.GetBarPositionTransform(rebar.NumberOfBarPositions - 1).Origin;
        return (last - first).DotProduct(along) > PositionToleranceFt;
    }

    private static int CreateSingles(
        Document doc,
        KataRebarPlan plan,
        KataStirrupZoneResult zone,
        KataBeamPlacement placement,
        RebarBarType barType,
        Element host)
    {
        var stations = new HashSet<double>(zone.Stations.Select(s => Math.Round(s, 3)));
        var curves = plan.Layout.IndividualStirrups
            .Where(s => s.HostSpanIndex == zone.SpanIndex && s.BarMark == zone.BarMark)
            .Where(s => stations.Contains(Math.Round(s.Polyline.Points[0].X, 3)))
            .ToList();

        foreach (var stirrup in curves)
        {
            var rebar = KataRebarCurveFactory.Create(
                doc, RebarStyle.StirrupTie, barType, host, placement.Mapper.AxisX,
                KataRebarCurveFactory.Curves(stirrup.Polyline, placement.Mapper));
            KataRebarStamp.Apply(rebar, host, plan.Spec.BeamName, stirrup.BarNumber);
        }

        return curves.Count;
    }
}
