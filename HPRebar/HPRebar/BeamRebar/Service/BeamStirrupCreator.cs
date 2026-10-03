using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Shared;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Places standard rectangular closed stirrups (shear ties) across continuous beam clear spans and support nodes.
/// </summary>
public static class BeamStirrupCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamStirrupSpec spec,
        RebarShapeResolver shapes,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var shape = shapes.MainStirrup();
        if (shape is null)
            throw new InvalidOperationException("Rectangular stirrup shape (M_T1/T1) not loaded in document.");

        var barType = catalog.FindBarType(spec.BarTypeName, spec.Diameter);
        if (barType is null)
            throw new InvalidOperationException($"RebarBarType for stirrups ({spec.Diameter} mm) not found.");

        var created = new List<Rebar>();

        // 1. Spans (Clear Spans)
        for (int i = 0; i < stack.Spans.Count; i++)
        {
            var span = stack.Spans[i];
            var hostElement = stack.Faces[i].Element;
            var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(span.LengthClear, spec, span.IsCantilever);

            foreach (var run in runs)
            {
                if (run.Count <= 0) continue;

                var rebar = PlaceStirrupRun(
                    document, hostElement, shape, barType.BarType, stack, span, run, partitionName);

                created.Add(rebar);
                onBarCreated?.Invoke();
            }
        }

        // 2. Interior Support Nodes (if enabled)
        if (spec.IncludeStirrupsInNodes && stack.Supports.Count > 2)
        {
            for (int k = 1; k < stack.Supports.Count - 1; k++)
            {
                var support = stack.Supports[k];
                var nodeRun = BeamStirrupDistributionCalculator.ComputeNodeRun(support.Width, spec.Cover, spec.NodeSpacing);
                if (nodeRun.Count <= 0) continue;

                // Host on the adjacent span element
                var hostSpan = stack.Spans[k - 1];
                var hostElement = stack.Faces[k - 1].Element;

                var rebar = PlaceNodeStirrupRun(
                    document, hostElement, shape, barType.BarType, stack, hostSpan, support, nodeRun, spec.Cover, partitionName);

                created.Add(rebar);
                onBarCreated?.Invoke();
            }
        }

        return created;
    }

    private static Rebar PlaceStirrupRun(
        Document doc,
        Element host,
        RebarShape shape,
        RebarBarType barType,
        BeamStack stack,
        BeamSpan span,
        StirrupRun run,
        string partitionName)
    {
        double widthMm = span.Width - (2.0 * span.Cover);
        double heightMm = span.Height - (2.0 * span.Cover);

        // Lower-left corner of first stirrup
        var localOrigin = new Point3(
            span.StartX + run.StartX,
            -(span.Width / 2.0) + span.Cover,
            span.BottomElevation + span.Cover);

        XYZ originXyz = stack.PointMapper.ToXyz(localOrigin);
        XYZ xVec = stack.TransverseDirection; // Y_beam
        XYZ yVec = XYZ.BasisZ;            // Z

        var rebar = Rebar.CreateFromRebarShape(doc, shape, barType, host, originXyz, xVec, yVec);
        var accessor = rebar.GetShapeDrivenAccessor();

        accessor.ScaleToBox(originXyz, xVec * RevitUnits.MmToFt(widthMm), yVec * RevitUnits.MmToFt(heightMm));
        if (run.Count == 1)
        {
            accessor.SetLayoutAsSingle();
        }
        else
        {
            accessor.SetLayoutAsNumberWithSpacing(
                Math.Clamp(run.Count, 2, RevitRebarLimits.MaxBarPositions), RevitUnits.MmToFt(run.Spacing), true, true, true);
        }

        SetPartition(rebar, partitionName);
        return rebar;
    }

    private static Rebar PlaceNodeStirrupRun(
        Document doc,
        Element host,
        RebarShape shape,
        RebarBarType barType,
        BeamStack stack,
        BeamSpan refSpan,
        BeamSupportNode support,
        StirrupRun run,
        double coverMm,
        string partitionName)
    {
        double widthMm = refSpan.Width - (2.0 * coverMm);
        double heightMm = refSpan.Height - (2.0 * coverMm);

        double stationX = support.LeftFaceX + run.StartOffset;
        var localOrigin = new Point3(
            stationX,
            -(refSpan.Width / 2.0) + coverMm,
            refSpan.BottomElevation + coverMm);

        XYZ originXyz = stack.PointMapper.ToXyz(localOrigin);
        XYZ xVec = stack.TransverseDirection;
        XYZ yVec = XYZ.BasisZ;

        var rebar = Rebar.CreateFromRebarShape(doc, shape, barType, host, originXyz, xVec, yVec);
        var accessor = rebar.GetShapeDrivenAccessor();

        accessor.ScaleToBox(originXyz, xVec * RevitUnits.MmToFt(widthMm), yVec * RevitUnits.MmToFt(heightMm));
        if (run.Count == 1)
        {
            accessor.SetLayoutAsSingle();
        }
        else
        {
            accessor.SetLayoutAsNumberWithSpacing(
                Math.Clamp(run.Count, 2, RevitRebarLimits.MaxBarPositions), RevitUnits.MmToFt(run.Spacing), true, true, true);
        }

        SetPartition(rebar, partitionName);
        return rebar;
    }

    internal static void SetPartition(Element rebar, string partitionName)
    {
        if (string.IsNullOrWhiteSpace(partitionName)) return;
        var param = rebar.LookupParameter("Partition");
        if (param is { IsReadOnly: false })
            param.Set(partitionName);
    }
}
