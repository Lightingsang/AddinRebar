using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Serilog;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Places special reinforcement at secondary framing beam intersections:
/// Concentrated hanging stirrup cages and 45° diagonal bent ties.
/// </summary>
public static class BeamSpecialBarCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamSpecialBarSpec spec,
        RebarShapeResolver shapes,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var created = new List<Rebar>();

        // 1. Concentrated Hanging Stirrups
        if (spec.EnableHangingStirrups && stack.ContinuousStack.SecondaryIntersections.Count > 0)
        {
            foreach (var sec in stack.ContinuousStack.SecondaryIntersections)
            {
                if (stack.ContinuousStack.FindSpanAt(sec.CenterX) == null)
                {
                    Log.Warning("Secondary beam {Id} at station {CenterX:0.#} mm frames into support joint zone; skipping hanging stirrups.",
                        sec.ElementUniqueId, sec.CenterX);
                }
            }

            var hangingBars = BeamSpecialBarCalculator.ComputeHangingStirrups(stack.ContinuousStack, spec);
            var barType = catalog.FindBarType(spec.HangingStirrupTypeName, spec.HangingStirrupDiameter);

            if (barType is not null)
            {
                foreach (var bar in hangingBars)
                {
                    int hostIdx = bar.HostSpanIndex >= 0 && bar.HostSpanIndex < stack.Faces.Count
                        ? bar.HostSpanIndex
                        : 0;
                    var hostElement = stack.Faces[hostIdx].Element;
                    var curves = BeamMainBarCreator.BuildCurves(bar.Polyline, stack.PointMapper);

                    // Hanging stirrups lie in transverse Y-Z plane -> normal is BeamDirection (X_beam)
#pragma warning disable CS0618 // Multi-version: Rebar.CreateFromCurves / RebarHookOrientation deprecated in Revit 2026, required for Revit 2023-2025 compatibility
                    var rebar = Rebar.CreateFromCurves(
                        document,
                        RebarStyle.StirrupTie,
                        barType.BarType,
                        startHook: null,
                        endHook: null,
                        host: hostElement,
                        norm: stack.BeamDirection,
                        curves: curves,
                        startHookOrient: RebarHookOrientation.Right,
                        endHookOrient: RebarHookOrientation.Right,
                        useExistingShapeIfPossible: true,
                        createNewShape: true);
#pragma warning restore CS0618

                    BeamStirrupCreator.SetPartition(rebar, partitionName);
                    created.Add(rebar);
                    onBarCreated?.Invoke();
                }
            }
        }

        // 2. 45° Diagonal Bent Ties
        if (spec.EnableDiagonalTies && stack.ContinuousStack.SecondaryIntersections.Count > 0)
        {
            var diagBars = BeamSpecialBarCalculator.ComputeDiagonalTies(stack.ContinuousStack, spec);
            var barType = catalog.FindBarType(spec.DiagonalTieTypeName, spec.DiagonalTieDiameter);

            if (barType is not null)
            {
                foreach (var bar in diagBars)
                {
                    var rebar = BeamMainBarCreator.CreateBarFromPolyline(
                        document, stack, bar, barType.BarType, partitionName);
                    created.Add(rebar);
                    onBarCreated?.Invoke();
                }
            }
        }

        return created;
    }
}
