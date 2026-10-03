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

        // 1. Run Pre-flight Engineering Validation
        var validation = FoundationValidationCalculator.Validate(snapshot, spec);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Foundation rebar validation failed: {validation.ErrorMessage}");
        }

        // 2. Compute Effective Placement Boundaries
        var bounds = FoundationBoundaryCalculator.Calculate(snapshot, spec.CoverSide);
        double xMin = bounds.XMin;
        double xMax = bounds.XMax;
        double yMin = bounds.YMin;
        double yMax = bounds.YMax;

        // 3. Exact 4-Layer Vertical Elevations in Local Z Frame
        // Layer 1 (Bottom X, outermost bottom)
        double z1 = spec.CoverBottom + (spec.DiameterBottomX / 2.0);

        // Layer 2 (Bottom Y, resting directly on Layer 1)
        double z2 = spec.CoverBottom + spec.DiameterBottomX + (spec.DiameterBottomY / 2.0);

        // Top Mat Elevations (under top cover)
        double z3 = 0.0;
        double z4 = 0.0;
        if (spec.IsTopMatEnabled)
        {
            // Layer 3 (Top Y, beneath Layer 4)
            z3 = snapshot.Thickness - spec.CoverTop - spec.DiameterTopX - (spec.DiameterTopY / 2.0);

            // Layer 4 (Top X, outermost top)
            z4 = snapshot.Thickness - spec.CoverTop - (spec.DiameterTopX / 2.0);
        }

        // 4. Hook Lengths with Safety Clamping to Prevent Breaching Opposite Cover
        bool hasHooks = spec.HookType == FoundationHookType.Hook90Degrees;

        // Bottom hooks bend UP (+Z)
        double hookLenB1 = 0.0;
        double hookLenB2 = 0.0;
        if (hasHooks)
        {
            double reqB1 = spec.GetHookLength(spec.DiameterBottomX);
            double maxRiseB1 = Math.Max(0.0, snapshot.Thickness - z1 - spec.CoverTop);
            hookLenB1 = Math.Min(reqB1, maxRiseB1);

            double reqB2 = spec.GetHookLength(spec.DiameterBottomY);
            double maxRiseB2 = Math.Max(0.0, snapshot.Thickness - z2 - spec.CoverTop);
            hookLenB2 = Math.Min(reqB2, maxRiseB2);
        }

        // Top hooks bend DOWN (-Z)
        double hookLenT3 = 0.0;
        double hookLenT4 = 0.0;
        if (hasHooks && spec.IsTopMatEnabled)
        {
            double reqT3 = spec.GetHookLength(spec.DiameterTopY);
            double maxDropT3 = Math.Max(0.0, z3 - spec.CoverBottom);
            hookLenT3 = Math.Min(reqT3, maxDropT3);

            double reqT4 = spec.GetHookLength(spec.DiameterTopX);
            double maxDropT4 = Math.Max(0.0, z4 - spec.CoverBottom);
            hookLenT4 = Math.Min(reqT4, maxDropT4);
        }

        // 5. Generate Rebar Curves for Each Active Layer
        var bottomXPolylines = new List<Polyline3>();
        var bottomYPolylines = new List<Polyline3>();
        var topXPolylines = new List<Polyline3>();
        var topYPolylines = new List<Polyline3>();
        var allBars = new List<FoundationBar>();

        int barCounter = 1;

        // --- Layer 1: Bottom Direction X (runs along X, distributed along Y) ---
        var yPositionsB1 = CalculateBarPositions(yMin, yMax, spec.SpacingBottomX, equalSpacing);
        for (int i = 0; i < yPositionsB1.Count; i++)
        {
            double y = yPositionsB1[i];
            var (localPoly, worldPoly) = BuildBarPolyline(
                snapshot,
                isDirX: true,
                coordAlongStart: xMin,
                coordAlongEnd: xMax,
                transverseCoord: y,
                localZ: z1,
                hasHooks: hasHooks,
                hookLength: hookLenB1,
                isHookUp: true);

            bottomXPolylines.Add(worldPoly);
            allBars.Add(new FoundationBar
            {
                BarIndex = barCounter++,
                Layer = FoundationBarLayer.BottomX,
                LayerName = "BottomX",
                Diameter = spec.DiameterBottomX,
                Polyline = worldPoly,
                LocalPolyline = localPoly,
                HookType = spec.HookType,
                HookLength = hookLenB1
            });
        }

        // --- Layer 2: Bottom Direction Y (runs along Y, distributed along X) ---
        var xPositionsB2 = CalculateBarPositions(xMin, xMax, spec.SpacingBottomY, equalSpacing);
        for (int i = 0; i < xPositionsB2.Count; i++)
        {
            double x = xPositionsB2[i];
            var (localPoly, worldPoly) = BuildBarPolyline(
                snapshot,
                isDirX: false,
                coordAlongStart: yMin,
                coordAlongEnd: yMax,
                transverseCoord: x,
                localZ: z2,
                hasHooks: hasHooks,
                hookLength: hookLenB2,
                isHookUp: true);

            bottomYPolylines.Add(worldPoly);
            allBars.Add(new FoundationBar
            {
                BarIndex = barCounter++,
                Layer = FoundationBarLayer.BottomY,
                LayerName = "BottomY",
                Diameter = spec.DiameterBottomY,
                Polyline = worldPoly,
                LocalPolyline = localPoly,
                HookType = spec.HookType,
                HookLength = hookLenB2
            });
        }

        // --- Top Mat (Layers 3 & 4) ---
        if (spec.IsTopMatEnabled)
        {
            // --- Layer 3: Top Direction Y (runs along Y, distributed along X) ---
            var xPositionsT3 = CalculateBarPositions(xMin, xMax, spec.SpacingTopY, equalSpacing);
            for (int i = 0; i < xPositionsT3.Count; i++)
            {
                double x = xPositionsT3[i];
                var (localPoly, worldPoly) = BuildBarPolyline(
                    snapshot,
                    isDirX: false,
                    coordAlongStart: yMin,
                    coordAlongEnd: yMax,
                    transverseCoord: x,
                    localZ: z3,
                    hasHooks: hasHooks,
                    hookLength: hookLenT3,
                    isHookUp: false);

                topYPolylines.Add(worldPoly);
                allBars.Add(new FoundationBar
                {
                    BarIndex = barCounter++,
                    Layer = FoundationBarLayer.TopY,
                    LayerName = "TopY",
                    Diameter = spec.DiameterTopY,
                    Polyline = worldPoly,
                    LocalPolyline = localPoly,
                    HookType = spec.HookType,
                    HookLength = hookLenT3
                });
            }

            // --- Layer 4: Top Direction X (runs along X, distributed along Y) ---
            var yPositionsT4 = CalculateBarPositions(yMin, yMax, spec.SpacingTopX, equalSpacing);
            for (int i = 0; i < yPositionsT4.Count; i++)
            {
                double y = yPositionsT4[i];
                var (localPoly, worldPoly) = BuildBarPolyline(
                    snapshot,
                    isDirX: true,
                    coordAlongStart: xMin,
                    coordAlongEnd: xMax,
                    transverseCoord: y,
                    localZ: z4,
                    hasHooks: hasHooks,
                    hookLength: hookLenT4,
                    isHookUp: false);

                topXPolylines.Add(worldPoly);
                allBars.Add(new FoundationBar
                {
                    BarIndex = barCounter++,
                    Layer = FoundationBarLayer.TopX,
                    LayerName = "TopX",
                    Diameter = spec.DiameterTopX,
                    Polyline = worldPoly,
                    LocalPolyline = localPoly,
                    HookType = spec.HookType,
                    HookLength = hookLenT4
                });
            }
        }

        // 6. Calculate Summary Metrics & Statistics
        double lenBx = 0.0, lenBy = 0.0, lenTx = 0.0, lenTy = 0.0;
        foreach (var p in bottomXPolylines) lenBx += p.TotalLength;
        foreach (var p in bottomYPolylines) lenBy += p.TotalLength;
        foreach (var p in topXPolylines) lenTx += p.TotalLength;
        foreach (var p in topYPolylines) lenTy += p.TotalLength;

        double totalLenMm = lenBx + lenBy + lenTx + lenTy;

        double weightKg = 0.0;
        foreach (var bar in allBars)
        {
            // Unit nominal weight formula: 0.006165 * d^2 * L(m)
            weightKg += 0.006165 * bar.Diameter * bar.Diameter * (bar.LengthMm / 1000.0);
        }

        var stats = new FoundationMeshStatistics
        {
            TotalBarCount = allBars.Count,
            BottomBarCountX = bottomXPolylines.Count,
            BottomBarCountY = bottomYPolylines.Count,
            TopBarCountX = topXPolylines.Count,
            TopBarCountY = topYPolylines.Count,
            BottomLengthMmX = lenBx,
            BottomLengthMmY = lenBy,
            TopLengthMmX = lenTx,
            TopLengthMmY = lenTy,
            TotalLengthMm = totalLenMm,
            EstimatedWeightKg = weightKg
        };

        return new FoundationMeshResult
        {
            BottomBarsX = bottomXPolylines,
            BottomBarsY = bottomYPolylines,
            TopBarsX = topXPolylines,
            TopBarsY = topYPolylines,
            Bars = allBars,
            Statistics = stats
        };
    }

    /// <summary>
    /// Builds planar local and world polyline curves for a single reinforcement bar.
    /// Strictly maintains coplanarity: all points of an X-bar lie in a constant transverse plane Y = const,
    /// and all points of a Y-bar lie in a constant transverse plane X = const.
    /// </summary>
    private static (Polyline3 Local, Polyline3 World) BuildBarPolyline(
        FoundationGeometrySnapshot snapshot,
        bool isDirX,
        double coordAlongStart,
        double coordAlongEnd,
        double transverseCoord,
        double localZ,
        bool hasHooks,
        double hookLength,
        bool isHookUp)
    {
        var localPoints = new List<Point3>();
        double hookZOffset = isHookUp ? hookLength : -hookLength;

        if (isDirX)
        {
            // Bar runs along X at constant transverse coordinate Y = transverseCoord
            if (hasHooks && hookLength > 0.0)
            {
                localPoints.Add(new Point3(coordAlongStart, transverseCoord, localZ + hookZOffset)); // Hook start tip
                localPoints.Add(new Point3(coordAlongStart, transverseCoord, localZ));               // Corner 1
                localPoints.Add(new Point3(coordAlongEnd, transverseCoord, localZ));                 // Corner 2
                localPoints.Add(new Point3(coordAlongEnd, transverseCoord, localZ + hookZOffset));   // Hook end tip
            }
            else
            {
                localPoints.Add(new Point3(coordAlongStart, transverseCoord, localZ));
                localPoints.Add(new Point3(coordAlongEnd, transverseCoord, localZ));
            }
        }
        else
        {
            // Bar runs along Y at constant transverse coordinate X = transverseCoord
            if (hasHooks && hookLength > 0.0)
            {
                localPoints.Add(new Point3(transverseCoord, coordAlongStart, localZ + hookZOffset)); // Hook start tip
                localPoints.Add(new Point3(transverseCoord, coordAlongStart, localZ));               // Corner 1
                localPoints.Add(new Point3(transverseCoord, coordAlongEnd, localZ));                 // Corner 2
                localPoints.Add(new Point3(transverseCoord, coordAlongEnd, localZ + hookZOffset));   // Hook end tip
            }
            else
            {
                localPoints.Add(new Point3(transverseCoord, coordAlongStart, localZ));
                localPoints.Add(new Point3(transverseCoord, coordAlongEnd, localZ));
            }
        }

        // Map every local point to World coordinates via orthonormal frame transformation
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
