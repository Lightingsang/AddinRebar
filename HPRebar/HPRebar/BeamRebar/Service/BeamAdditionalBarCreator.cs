using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Places additional negative-moment top bars over supports and additional positive-moment bottom bars in midspans.
/// </summary>
public static class BeamAdditionalBarCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamAdditionalBarSpec spec,
        double stirrupDiameterMm,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var created = new List<Rebar>();

        // 1. Support Top Bars
        var topBars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack.ContinuousStack, spec, stirrupDiameterMm);
        foreach (var bar in topBars)
        {
            var barType = catalog.FindBarType(bar.BarTypeName, bar.Diameter);
            if (barType is null) continue;

            var rebar = BeamMainBarCreator.CreateBarFromPolyline(
                document, stack, bar, barType.BarType, partitionName);
            created.Add(rebar);
            onBarCreated?.Invoke();
        }

        // 2. Span Bottom Bars
        var bottomBars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(stack.ContinuousStack, spec, stirrupDiameterMm);
        foreach (var bar in bottomBars)
        {
            var barType = catalog.FindBarType(bar.BarTypeName, bar.Diameter);
            if (barType is null) continue;

            var rebar = BeamMainBarCreator.CreateBarFromPolyline(
                document, stack, bar, barType.BarType, partitionName);
            created.Add(rebar);
            onBarCreated?.Invoke();
        }

        return created;
    }
}
