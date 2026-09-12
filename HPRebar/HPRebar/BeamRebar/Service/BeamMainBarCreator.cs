using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Places continuous top and bottom longitudinal reinforcement bars with anchorage hooks and staggered splices.
/// </summary>
public static class BeamMainBarCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamMainBarSpec spec,
        double stirrupDiameterMm,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var created = new List<Rebar>();

        // 1. Top Main Bars
        var topBars = BeamMainBarCalculator.ComputeTopMainBars(stack.ContinuousStack, spec, stirrupDiameterMm);
        var topBarType = catalog.FindBarType(spec.TopBarTypeName, spec.TopDiameter);
        if (topBarType is null)
            throw new InvalidOperationException($"RebarBarType for top main bars ({spec.TopDiameter} mm) not found.");

        foreach (var bar in topBars)
        {
            var rebar = CreateBarFromPolyline(
                document, stack, bar, topBarType.BarType, partitionName);
            created.Add(rebar);
            onBarCreated?.Invoke();
        }

        // 2. Bottom Main Bars
        var bottomBars = BeamMainBarCalculator.ComputeBottomMainBars(stack.ContinuousStack, spec, stirrupDiameterMm);
        var bottomBarType = catalog.FindBarType(spec.BottomBarTypeName, spec.BottomDiameter);
        if (bottomBarType is null)
            throw new InvalidOperationException($"RebarBarType for bottom main bars ({spec.BottomDiameter} mm) not found.");

        foreach (var bar in bottomBars)
        {
            var rebar = CreateBarFromPolyline(
                document, stack, bar, bottomBarType.BarType, partitionName);
            created.Add(rebar);
            onBarCreated?.Invoke();
        }

        return created;
    }

    internal static Rebar CreateBarFromPolyline(
        Document document,
        BeamStack stack,
        BarPolyline bar,
        RebarBarType defaultBarType,
        string partitionName)
    {
        int hostIdx = bar.HostSpanIndex >= 0 && bar.HostSpanIndex < stack.Faces.Count
            ? bar.HostSpanIndex
            : 0;

        var hostElement = stack.Faces[hostIdx].Element;
        var curves = BuildCurves(bar.Polyline, stack.PointMapper);

#pragma warning disable CS0618 // Multi-version: Rebar.CreateFromCurves / RebarHookOrientation deprecated in Revit 2026, required for Revit 2023-2025 compatibility
        var rebar = Rebar.CreateFromCurves(
            document,
            RebarStyle.Standard,
            defaultBarType,
            startHook: null,
            endHook: null,
            host: hostElement,
            norm: stack.NormalDirection,
            curves: curves,
            startHookOrient: RebarHookOrientation.Right,
            endHookOrient: RebarHookOrientation.Right,
            useExistingShapeIfPossible: true,
            createNewShape: true);
#pragma warning restore CS0618

        BeamStirrupCreator.SetPartition(rebar, partitionName);
        return rebar;
    }

    public static IList<Curve> BuildCurves(Polyline3 polyline, PointMapper mapper)
    {
        var simplified = polyline.Simplify(1.0); // 1.0 mm minimum segment limit
        if (simplified.Points.Count < 2)
            throw new InvalidOperationException("Polyline collapsed to fewer than 2 points.");

        var curves = new List<Curve>(simplified.Points.Count);
        for (int i = 1; i < simplified.Points.Count; i++)
        {
            var p0 = mapper.ToXyz(simplified.Points[i - 1]);
            var p1 = mapper.ToXyz(simplified.Points[i]);
            curves.Add(Line.CreateBound(p0, p1));
        }

        if (simplified.IsClosed && simplified.Points.Count > 2)
        {
            var pLast = mapper.ToXyz(simplified.Points[simplified.Points.Count - 1]);
            var pFirst = mapper.ToXyz(simplified.Points[0]);
            curves.Add(Line.CreateBound(pLast, pFirst));
        }

        return curves;
    }
}
