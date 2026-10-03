using System;
using System.Collections.Generic;
using HPRebar.Core.FoundationRebar.Models;

namespace HPRebar.Core.FoundationRebar.Calculators;

/// <summary>
/// Core 3D geometry engine generating continuous reinforcement curves for 2-mat 2-way foundation slabs.
/// Pure C# mathematical calculations in millimetres with zero Revit API dependencies.
/// </summary>
public static class FoundationMeshCalculator
{
    /// <summary>
    /// Nominal mass of steel bar: 0.006165 kg per metre of length per mm² of diameter (π/4 × 7850 kg/m³).
    /// </summary>
    private const double BarKgPerMetrePerSquareMm = 0.006165;

    /// <summary>
    /// Calculates discrete distribution positions along a span according to nominal spacing.
    /// Default mode centers remaining slack equally on both sides:
    /// delta = (Span_eff - N * s) / 2, count = N + 1 where N = floor(Span_eff / s).
    /// If equalSpacing is true, subdivides span evenly with actual spacing &lt;= nominalSpacing.
    /// </summary>
    public static IReadOnlyList<double> CalculateBarPositions(
        double spanStart,
        double spanEnd,
        double spacing,
        bool equalSpacing = false)
    {
        double span = spanEnd - spanStart;
        if (span <= 0 || spacing <= 0)
            return Array.Empty<double>();

        var positions = new List<double>();

        if (!equalSpacing)
        {
            // Symmetrical centered margin mode: delta = (Span_eff - N * s) / 2
            int intervals = (int)Math.Floor(span / spacing);
            if (intervals == 0)
            {
                // Span smaller than spacing: place 1 centered bar
                positions.Add(spanStart + span / 2.0);
            }
            else
            {
                double slack = span - (intervals * spacing);
                double delta = slack / 2.0;
                for (int i = 0; i <= intervals; i++)
                {
                    positions.Add(spanStart + delta + (i * spacing));
                }
            }
        }
        else
        {
            // Equal subdivisions mode (max spacing constraint)
            int intervals = Math.Max(1, (int)Math.Ceiling(span / spacing));
            double actualSpacing = span / intervals;
            for (int i = 0; i <= intervals; i++)
            {
                positions.Add(spanStart + (i * actualSpacing));
            }
        }

        return positions;
    }

    /// <summary>
    /// Computes full 3D mesh reinforcement result for the given geometry snapshot and specification.
    /// Throws <see cref="InvalidOperationException"/> if pre-flight validation fails.
    /// </summary>
    public static FoundationMeshResult Calculate(
        FoundationGeometrySnapshot snapshot,
        FoundationRebarSpec spec,
        bool equalSpacing = false)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        var validation = FoundationValidationCalculator.Validate(snapshot, spec);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Foundation rebar validation failed: {validation.ErrorMessage}");
        }

        var bounds = FoundationBoundaryCalculator.Calculate(snapshot, spec.CoverSide);
        var bars = new List<FoundationBar>();
        foreach (var layer in PlanLayers(snapshot, spec))
        {
            bars.AddRange(PlaceLayer(snapshot, bounds, layer, equalSpacing, firstBarIndex: bars.Count + 1));
        }

        return Summarise(bars);
    }

    /// <summary>One layer of the mesh: which way its bars run, how they are spaced, their height and hook.</summary>
    /// <param name="HookRise">
    /// Signed hook leg: positive bends up (bottom mat), negative bends down (top mat), 0 = no hook.
    /// </param>
    private sealed record MeshLayer(
        FoundationBarLayer Layer,
        bool RunsAlongX,
        double Spacing,
        double Diameter,
        double Z,
        double HookRise,
        FoundationHookType HookType);

    /// <summary>
    /// The active layers in placing order: bottom X (outermost), bottom Y resting on it, then top Y hung under
    /// top X (outermost top). Hook legs are clamped so they never cross into the opposite cover.
    /// </summary>
    private static IEnumerable<MeshLayer> PlanLayers(FoundationGeometrySnapshot snapshot, FoundationRebarSpec spec)
    {
        bool hasHooks = spec.HookType == FoundationHookType.Hook90Degrees;

        double zBottomX = spec.CoverBottom + (spec.DiameterBottomX / 2.0);
        double zBottomY = spec.CoverBottom + spec.DiameterBottomX + (spec.DiameterBottomY / 2.0);

        yield return new MeshLayer(
            FoundationBarLayer.BottomX, RunsAlongX: true, spec.SpacingBottomX, spec.DiameterBottomX,
            zBottomX, hasHooks ? RisingHook(snapshot, spec, spec.DiameterBottomX, zBottomX) : 0.0, spec.HookType);
        yield return new MeshLayer(
            FoundationBarLayer.BottomY, RunsAlongX: false, spec.SpacingBottomY, spec.DiameterBottomY,
            zBottomY, hasHooks ? RisingHook(snapshot, spec, spec.DiameterBottomY, zBottomY) : 0.0, spec.HookType);

        if (!spec.IsTopMatEnabled)
        {
            yield break;
        }

        double zTopY = snapshot.Thickness - spec.CoverTop - spec.DiameterTopX - (spec.DiameterTopY / 2.0);
        double zTopX = snapshot.Thickness - spec.CoverTop - (spec.DiameterTopX / 2.0);

        yield return new MeshLayer(FoundationBarLayer.TopY, RunsAlongX: false, spec.SpacingTopY, spec.DiameterTopY,
            zTopY, hasHooks ? -FallingHook(spec, spec.DiameterTopY, zTopY) : 0.0, spec.HookType);
        yield return new MeshLayer(FoundationBarLayer.TopX, RunsAlongX: true, spec.SpacingTopX, spec.DiameterTopX,
            zTopX, hasHooks ? -FallingHook(spec, spec.DiameterTopX, zTopX) : 0.0, spec.HookType);
    }

    /// <summary>Bottom-mat hook: the requested leg, cut so it stops under the top cover.</summary>
    private static double RisingHook(
        FoundationGeometrySnapshot snapshot, FoundationRebarSpec spec, double diameter, double z) =>
        Math.Min(spec.GetHookLength(diameter), Math.Max(0.0, snapshot.Thickness - z - spec.CoverTop));

    /// <summary>Top-mat hook: the requested leg, cut so it stops above the bottom cover.</summary>
    private static double FallingHook(FoundationRebarSpec spec, double diameter, double z) =>
        Math.Min(spec.GetHookLength(diameter), Math.Max(0.0, z - spec.CoverBottom));

    /// <summary>
    /// The bars of one layer: a bar running along X is repeated across Y within the effective boundary, and the
    /// other way round. Bars are numbered from <paramref name="firstBarIndex"/>.
    /// </summary>
    private static IEnumerable<FoundationBar> PlaceLayer(
        FoundationGeometrySnapshot snapshot,
        FoundationEffectiveBoundary bounds,
        MeshLayer layer,
        bool equalSpacing,
        int firstBarIndex)
    {
        var across = layer.RunsAlongX
            ? CalculateBarPositions(bounds.YMin, bounds.YMax, layer.Spacing, equalSpacing)
            : CalculateBarPositions(bounds.XMin, bounds.XMax, layer.Spacing, equalSpacing);
        double alongStart = layer.RunsAlongX ? bounds.XMin : bounds.YMin;
        double alongEnd = layer.RunsAlongX ? bounds.XMax : bounds.YMax;

        for (int i = 0; i < across.Count; i++)
        {
            var (localPoly, worldPoly) = BuildBarPolyline(snapshot, layer, alongStart, alongEnd, across[i]);

            yield return new FoundationBar
            {
                BarIndex = firstBarIndex + i,
                Layer = layer.Layer,
                LayerName = layer.Layer.ToString(),
                Diameter = layer.Diameter,
                Polyline = worldPoly,
                LocalPolyline = localPoly,
                HookType = layer.HookType,
                HookLength = Math.Abs(layer.HookRise)
            };
        }
    }

    private static FoundationMeshResult Summarise(IReadOnlyList<FoundationBar> bars)
    {
        var bottomX = PolylinesOf(bars, FoundationBarLayer.BottomX);
        var bottomY = PolylinesOf(bars, FoundationBarLayer.BottomY);
        var topX = PolylinesOf(bars, FoundationBarLayer.TopX);
        var topY = PolylinesOf(bars, FoundationBarLayer.TopY);

        double lenBx = TotalLength(bottomX);
        double lenBy = TotalLength(bottomY);
        double lenTx = TotalLength(topX);
        double lenTy = TotalLength(topY);

        double weightKg = 0.0;
        foreach (var bar in bars)
        {
            weightKg += BarKgPerMetrePerSquareMm * bar.Diameter * bar.Diameter * (bar.LengthMm / 1000.0);
        }

        return new FoundationMeshResult
        {
            BottomBarsX = bottomX,
            BottomBarsY = bottomY,
            TopBarsX = topX,
            TopBarsY = topY,
            Bars = bars,
            Statistics = new FoundationMeshStatistics
            {
                TotalBarCount = bars.Count,
                BottomBarCountX = bottomX.Count,
                BottomBarCountY = bottomY.Count,
                TopBarCountX = topX.Count,
                TopBarCountY = topY.Count,
                BottomLengthMmX = lenBx,
                BottomLengthMmY = lenBy,
                TopLengthMmX = lenTx,
                TopLengthMmY = lenTy,
                TotalLengthMm = lenBx + lenBy + lenTx + lenTy,
                EstimatedWeightKg = weightKg
            }
        };
    }

    private static List<Polyline3> PolylinesOf(IEnumerable<FoundationBar> bars, FoundationBarLayer layer)
    {
        var polylines = new List<Polyline3>();
        foreach (var bar in bars)
        {
            if (bar.Layer == layer)
            {
                polylines.Add(bar.Polyline);
            }
        }

        return polylines;
    }

    private static double TotalLength(IEnumerable<Polyline3> polylines)
    {
        double total = 0.0;
        foreach (var polyline in polylines)
        {
            total += polyline.TotalLength;
        }

        return total;
    }

    /// <summary>
    /// Builds planar local and world polyline curves for a single reinforcement bar.
    /// Strictly maintains coplanarity: all points of an X-bar lie in a constant transverse plane Y = const,
    /// and all points of a Y-bar lie in a constant transverse plane X = const.
    /// </summary>
    private static (Polyline3 Local, Polyline3 World) BuildBarPolyline(
        FoundationGeometrySnapshot snapshot,
        MeshLayer layer,
        double alongStart,
        double alongEnd,
        double across)
    {
        Point3 At(double along, double z) =>
            layer.RunsAlongX ? new Point3(along, across, z) : new Point3(across, along, z);

        var localPoints = new List<Point3>();
        if (Math.Abs(layer.HookRise) > 0.0)
        {
            localPoints.Add(At(alongStart, layer.Z + layer.HookRise)); // hook start tip
            localPoints.Add(At(alongStart, layer.Z));
            localPoints.Add(At(alongEnd, layer.Z));
            localPoints.Add(At(alongEnd, layer.Z + layer.HookRise));   // hook end tip
        }
        else
        {
            localPoints.Add(At(alongStart, layer.Z));
            localPoints.Add(At(alongEnd, layer.Z));
        }

        var worldPoints = new Point3[localPoints.Count];
        for (int i = 0; i < localPoints.Count; i++)
        {
            worldPoints[i] = snapshot.ToWorld(localPoints[i].X, localPoints[i].Y, localPoints[i].Z);
        }

        // Simplify both to guarantee no micro-segments below Revit tolerance
        var localPoly = new Polyline3(localPoints).Simplify(1.0);
        var worldPoly = new Polyline3(worldPoints).Simplify(1.0);

        return (localPoly, worldPoly);
    }
}
