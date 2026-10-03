using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Shared.Revit;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Places longitudinal skin/side reinforcement bars and transverse anti-buckling cross-ties for deep beams (h >= 700 mm).
/// </summary>
public static class BeamSideBarCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamSideBarSpec spec,
        double stirrupDiameterMm,
        double mainBarDiameterMm,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var created = new List<Rebar>();

        // 1. Longitudinal Side (Skin) Bars
        var sideBars = BeamSideBarCalculator.ComputeLongitudinalSideBars(
            stack.ContinuousStack, spec, stirrupDiameterMm, mainBarDiameterMm);

        var sideBarType = catalog.FindBarType(spec.SideBarTypeName, spec.Diameter);
        if (sideBarType is not null)
        {
            foreach (var bar in sideBars)
            {
                var rebar = BeamMainBarCreator.CreateBarFromPolyline(
                    document, stack, bar, sideBarType.BarType, partitionName);
                created.Add(rebar);
                onBarCreated?.Invoke();
            }
        }

        // 2. Transverse Cross-Ties
        if (spec.IncludeCrossTies)
        {
            var crossTies = BeamSideBarCalculator.ComputeCrossTies(
                stack.ContinuousStack, spec, stirrupDiameterMm, mainBarDiameterMm);

            var tieBarType = catalog.FindBarType(spec.CrossTieBarTypeName, spec.CrossTieDiameter);
            if (tieBarType is not null)
            {
                foreach (var tie in crossTies)
                {
                    int hostIdx = tie.HostSpanIndex >= 0 && tie.HostSpanIndex < stack.Faces.Count
                        ? tie.HostSpanIndex
                        : 0;
                    var hostElement = stack.Faces[hostIdx].Element;

                    var curves = BeamMainBarCreator.BuildCurves(tie.Polyline, stack.PointMapper);

                    // Cross ties lie in transverse Y-Z plane -> normal is BeamDirection (X_beam)
                    var rebar = RebarCurveFactory.CreateWithoutHooks(document, RebarStyle.StirrupTie, tieBarType.BarType, hostElement, stack.BeamDirection, curves);

                    BeamStirrupCreator.SetPartition(rebar, partitionName);
                    created.Add(rebar);
                    onBarCreated?.Invoke();
                }
            }
        }

        return created;
    }
}
