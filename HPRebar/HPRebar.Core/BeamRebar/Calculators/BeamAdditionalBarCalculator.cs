using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// Calculates additional top negative-moment reinforcement over support nodes and
/// additional bottom positive-moment reinforcement in span midspans.
/// Supports multi-layer vertical offsets and custom cutoff ratios.
/// </summary>
public static class BeamAdditionalBarCalculator
{
    public const double DefaultTopCutoffRatioLayer1 = 1.0 / 3.0;
    public const double DefaultTopCutoffRatioLayer2 = 1.0 / 4.0;
    public const double DefaultBottomCutoffRatio = 1.0 / 7.0;
    public const double MinimumClearVerticalGapMm = 30.0;

    /// <summary>
    /// Computes additional top negative-moment bars over intermediate and exterior supports.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeSupportTopBars(
        BeamContinuousStack stack,
        BeamAdditionalTopBarSpec spec,
        double stirrupDiameterMm)
    {
        var result = new List<BarPolyline>();
        int barId = 0;

        foreach (var config in spec.SupportTopBars)
        {
            int sIdx = config.SupportIndex;
            if (sIdx < 0 || sIdx >= stack.Supports.Count)
                continue;

            var support = stack.Supports[sIdx];
            bool isExteriorStart = (sIdx == 0);
            bool isExteriorEnd = (sIdx == stack.Supports.Count - 1);

            // Exterior Support 0 (Left End)
            if (isExteriorStart)
            {
                if (stack.Spans.Count == 0) continue;
                var span = stack.Spans[0];
                double ln = span.LengthClear;
                double zTop = span.TopElevation;
                double cover = span.Cover;
                double zBotFloor = span.BottomElevation + cover + stirrupDiameterMm;

                // Layer 1
                if (config.Layer1Count > 0)
                {
                    double r1 = config.Layer1ExtensionRatio > 0 ? config.Layer1ExtensionRatio : DefaultTopCutoffRatioLayer1;
                    double xStart = support.LeftFaceX + cover;
                    double xEnd = support.RightFaceX + (r1 * ln);
                    double z1 = zTop - cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                    double availDrop1 = Math.Max(0.0, z1 - zBotFloor);
                    double hookLen1 = config.ExteriorHookLength > 0.0
                        ? Math.Min(availDrop1, config.ExteriorHookLength)
                        : Math.Min(availDrop1, Math.Max(30.0 * config.Layer1Diameter, 200.0));

                    var yPositions1 = BeamMainBarCalculator.ComputeTransverseYPositions(
                        span.Width, cover, stirrupDiameterMm, config.Layer1Diameter, config.Layer1Count);

                    for (int i = 0; i < yPositions1.Count; i++)
                    {
                        double y = yPositions1[i];
                        var pts = new List<Point3>
                        {
                            new(xStart, y, z1 - hookLen1),
                            new(xStart, y, z1),
                            new(xEnd, y, z1)
                        };

                        result.Add(new BarPolyline
                        {
                            BarIndex = barId++,
                            Type = BarType.AdditionalTop,
                            Diameter = config.Layer1Diameter,
                            Layer = 1,
                            HostSupportIndex = sIdx,
                            HostSpanIndex = 0,
                            Polyline = new Polyline3(pts),
                            StartHookAngle = HookAngle.Hook90,
                            EndHookAngle = HookAngle.None,
                            StartHookLength = hookLen1,
                            TransverseY = y,
                            LeftExtension = support.Width,
                            RightExtension = r1 * ln,
                            BarTypeName = config.BarTypeName
                        });
                    }
                }

                // Layer 2
                if (config.Layer2Count > 0)
                {
                    double r2 = config.Layer2ExtensionRatio > 0 ? config.Layer2ExtensionRatio : DefaultTopCutoffRatioLayer2;
                    double xStart2 = support.LeftFaceX + cover;
                    double xEnd2 = support.RightFaceX + (r2 * ln);

                    double gap = config.LayerGap > 0 ? config.LayerGap : (config.Layer1Diameter + MinimumClearVerticalGapMm);
                    double z1 = zTop - cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                    double z2 = z1 - gap;

                    double availDrop2 = Math.Max(0.0, z2 - zBotFloor);
                    double hookLen2 = config.ExteriorHookLength > 0.0
                        ? Math.Min(availDrop2, config.ExteriorHookLength)
                        : Math.Min(availDrop2, Math.Max(30.0 * config.Layer2Diameter, 200.0));

                    var yPositions2 = BeamMainBarCalculator.ComputeTransverseYPositions(
                        span.Width, cover, stirrupDiameterMm, config.Layer2Diameter, config.Layer2Count);

                    for (int i = 0; i < yPositions2.Count; i++)
                    {
                        double y = yPositions2[i];
                        var pts = new List<Point3>
                        {
                            new(xStart2, y, z2 - hookLen2),
                            new(xStart2, y, z2),
                            new(xEnd2, y, z2)
                        };

                        result.Add(new BarPolyline
                        {
                            BarIndex = barId++,
                            Type = BarType.AdditionalTop,
                            Diameter = config.Layer2Diameter,
                            Layer = 2,
                            HostSupportIndex = sIdx,
                            HostSpanIndex = 0,
                            Polyline = new Polyline3(pts),
                            StartHookAngle = HookAngle.Hook90,
                            EndHookAngle = HookAngle.None,
                            StartHookLength = hookLen2,
                            TransverseY = y,
                            LeftExtension = support.Width,
                            RightExtension = r2 * ln,
                            BarTypeName = config.BarTypeName
                        });
                    }
                }

                continue;
            }

            // Exterior Support N (Right End)
            if (isExteriorEnd)
            {
                if (stack.Spans.Count == 0) continue;
                var span = stack.Spans[stack.Spans.Count - 1];
                double ln = span.LengthClear;
                double zTop = span.TopElevation;
                double cover = span.Cover;
                double zBotFloor = span.BottomElevation + cover + stirrupDiameterMm;

                // Layer 1
                if (config.Layer1Count > 0)
                {
                    double r1 = config.Layer1ExtensionRatio > 0 ? config.Layer1ExtensionRatio : DefaultTopCutoffRatioLayer1;
                    double xStart = support.LeftFaceX - (r1 * ln);
                    double xEnd = support.RightFaceX - cover;
                    double z1 = zTop - cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                    double availDrop1 = Math.Max(0.0, z1 - zBotFloor);
                    double hookLen1 = config.ExteriorHookLength > 0.0
                        ? Math.Min(availDrop1, config.ExteriorHookLength)
                        : Math.Min(availDrop1, Math.Max(30.0 * config.Layer1Diameter, 200.0));

                    var yPositions1 = BeamMainBarCalculator.ComputeTransverseYPositions(
                        span.Width, cover, stirrupDiameterMm, config.Layer1Diameter, config.Layer1Count);

                    for (int i = 0; i < yPositions1.Count; i++)
                    {
                        double y = yPositions1[i];
                        var pts = new List<Point3>
                        {
                            new(xStart, y, z1),
                            new(xEnd, y, z1),
                            new(xEnd, y, z1 - hookLen1)
                        };

                        result.Add(new BarPolyline
                        {
                            BarIndex = barId++,
                            Type = BarType.AdditionalTop,
                            Diameter = config.Layer1Diameter,
                            Layer = 1,
                            HostSupportIndex = sIdx,
                            HostSpanIndex = stack.Spans.Count - 1,
                            Polyline = new Polyline3(pts),
                            StartHookAngle = HookAngle.None,
                            EndHookAngle = HookAngle.Hook90,
                            EndHookLength = hookLen1,
                            TransverseY = y,
                            LeftExtension = r1 * ln,
                            RightExtension = support.Width,
                            BarTypeName = config.BarTypeName
                        });
                    }
                }

                // Layer 2
                if (config.Layer2Count > 0)
                {
                    double r2 = config.Layer2ExtensionRatio > 0 ? config.Layer2ExtensionRatio : DefaultTopCutoffRatioLayer2;
                    double xStart2 = support.LeftFaceX - (r2 * ln);
                    double xEnd2 = support.RightFaceX - cover;

                    double gap = config.LayerGap > 0 ? config.LayerGap : (config.Layer1Diameter + MinimumClearVerticalGapMm);
                    double z1 = zTop - cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                    double z2 = z1 - gap;

                    double availDrop2 = Math.Max(0.0, z2 - zBotFloor);
                    double hookLen2 = config.ExteriorHookLength > 0.0
                        ? Math.Min(availDrop2, config.ExteriorHookLength)
                        : Math.Min(availDrop2, Math.Max(30.0 * config.Layer2Diameter, 200.0));

                    var yPositions2 = BeamMainBarCalculator.ComputeTransverseYPositions(
                        span.Width, cover, stirrupDiameterMm, config.Layer2Diameter, config.Layer2Count);

                    for (int i = 0; i < yPositions2.Count; i++)
                    {
                        double y = yPositions2[i];
                        var pts = new List<Point3>
                        {
                            new(xStart2, y, z2),
                            new(xEnd2, y, z2),
                            new(xEnd2, y, z2 - hookLen2)
                        };

                        result.Add(new BarPolyline
                        {
                            BarIndex = barId++,
                            Type = BarType.AdditionalTop,
                            Diameter = config.Layer2Diameter,
                            Layer = 2,
                            HostSupportIndex = sIdx,
                            HostSpanIndex = stack.Spans.Count - 1,
                            Polyline = new Polyline3(pts),
                            StartHookAngle = HookAngle.None,
                            EndHookAngle = HookAngle.Hook90,
                            EndHookLength = hookLen2,
                            TransverseY = y,
                            LeftExtension = r2 * ln,
                            RightExtension = support.Width,
                            BarTypeName = config.BarTypeName
                        });
                    }
                }

                continue;
            }

            // Intermediate Support Node (Centered between Span sIdx-1 and Span sIdx)
            var leftSpan = stack.Spans[sIdx - 1];
            var rightSpan = stack.Spans[sIdx];
            double lnLeft = leftSpan.LengthClear;
            double lnRight = rightSpan.LengthClear;
            double zTopInter = Math.Min(leftSpan.TopElevation, rightSpan.TopElevation);
            double coverInter = Math.Max(leftSpan.Cover, rightSpan.Cover);
            double widthInter = Math.Min(leftSpan.Width, rightSpan.Width);

            // Layer 1
            if (config.Layer1Count > 0)
            {
                double r1 = config.Layer1ExtensionRatio > 0 ? config.Layer1ExtensionRatio : DefaultTopCutoffRatioLayer1;
                double lExtLeft1 = r1 * lnLeft;
                double lExtRight1 = r1 * lnRight;
                double xStart1 = support.LeftFaceX - lExtLeft1;
                double xEnd1 = support.RightFaceX + lExtRight1;
                double z1 = zTopInter - coverInter - stirrupDiameterMm - (config.Layer1Diameter / 2.0);

                var yPositions1 = BeamMainBarCalculator.ComputeTransverseYPositions(
                    widthInter, coverInter, stirrupDiameterMm, config.Layer1Diameter, config.Layer1Count);

                for (int i = 0; i < yPositions1.Count; i++)
                {
                    double y = yPositions1[i];
                    var pts = new List<Point3>
                    {
                        new(xStart1, y, z1),
                        new(xEnd1, y, z1)
                    };

                    result.Add(new BarPolyline
                    {
                        BarIndex = barId++,
                        Type = BarType.AdditionalTop,
                        Diameter = config.Layer1Diameter,
                        Layer = 1,
                        HostSupportIndex = sIdx,
                        Polyline = new Polyline3(pts),
                        TransverseY = y,
                        LeftExtension = lExtLeft1,
                        RightExtension = lExtRight1,
                        BarTypeName = config.BarTypeName
                    });
                }
            }

            // Layer 2
            if (config.Layer2Count > 0)
            {
                double r2 = config.Layer2ExtensionRatio > 0 ? config.Layer2ExtensionRatio : DefaultTopCutoffRatioLayer2;
                double lExtLeft2 = r2 * lnLeft;
                double lExtRight2 = r2 * lnRight;
                double xStart2 = support.LeftFaceX - lExtLeft2;
                double xEnd2 = support.RightFaceX + lExtRight2;

                double gap = config.LayerGap > 0 ? config.LayerGap : (config.Layer1Diameter + MinimumClearVerticalGapMm);
                double z1 = zTopInter - coverInter - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                double z2 = z1 - gap;

                var yPositions2 = BeamMainBarCalculator.ComputeTransverseYPositions(
                    widthInter, coverInter, stirrupDiameterMm, config.Layer2Diameter, config.Layer2Count);

                for (int i = 0; i < yPositions2.Count; i++)
                {
                    double y = yPositions2[i];
                    var pts = new List<Point3>
                    {
                        new(xStart2, y, z2),
                        new(xEnd2, y, z2)
                    };

                    result.Add(new BarPolyline
                    {
                        BarIndex = barId++,
                        Type = BarType.AdditionalTop,
                        Diameter = config.Layer2Diameter,
                        Layer = 2,
                        HostSupportIndex = sIdx,
                        Polyline = new Polyline3(pts),
                        TransverseY = y,
                        LeftExtension = lExtLeft2,
                        RightExtension = lExtRight2,
                        BarTypeName = config.BarTypeName
                    });
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Overload taking general BeamAdditionalBarSpec.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeSupportTopBars(
        BeamContinuousStack stack,
        BeamAdditionalBarSpec spec,
        double stirrupDiameterMm)
    {
        return ComputeSupportTopBars(stack, new BeamAdditionalTopBarSpec { SupportTopBars = spec.SupportTopBars }, stirrupDiameterMm);
    }

    /// <summary>
    /// Computes additional bottom positive-moment bars at span midspans.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeSpanBottomBars(
        BeamContinuousStack stack,
        BeamAdditionalBottomBarSpec spec,
        double stirrupDiameterMm)
    {
        var result = new List<BarPolyline>();
        int barId = 0;

        foreach (var config in spec.SpanBottomBars)
        {
            int sIdx = config.SpanIndex;
            if (sIdx < 0 || sIdx >= stack.Spans.Count)
                continue;

            var span = stack.Spans[sIdx];
            double ln = span.LengthClear;
            double rCut = config.CutoffRatio > 0 ? config.CutoffRatio : DefaultBottomCutoffRatio;
            double dCut = rCut * ln;

            double leftFaceX = (stack.Supports.Count > sIdx) ? stack.Supports[sIdx].RightFaceX : span.StartX;
            double rightFaceX = (stack.Supports.Count > sIdx + 1) ? stack.Supports[sIdx + 1].LeftFaceX : span.EndX;

            double xStart = leftFaceX + dCut;
            double xEnd = rightFaceX - dCut;
            double zBot = span.BottomElevation;

            // Layer 1
            if (config.Layer1Count > 0)
            {
                double z1 = zBot + span.Cover + stirrupDiameterMm + (config.Layer1Diameter / 2.0);
                var yPositions1 = BeamMainBarCalculator.ComputeTransverseYPositions(
                    span.Width, span.Cover, stirrupDiameterMm, config.Layer1Diameter, config.Layer1Count);

                for (int i = 0; i < yPositions1.Count; i++)
                {
                    double y = yPositions1[i];
                    var pts = new List<Point3>
                    {
                        new(xStart, y, z1),
                        new(xEnd, y, z1)
                    };

                    result.Add(new BarPolyline
                    {
                        BarIndex = barId++,
                        Type = BarType.AdditionalBottom,
                        Diameter = config.Layer1Diameter,
                        Layer = 1,
                        HostSpanIndex = sIdx,
                        Polyline = new Polyline3(pts),
                        TransverseY = y,
                        BarTypeName = config.BarTypeName
                    });
                }
            }

            // Layer 2
            if (config.Layer2Count > 0)
            {
                double gap = config.LayerGap > 0 ? config.LayerGap : (config.Layer1Diameter + MinimumClearVerticalGapMm);
                double z1 = zBot + span.Cover + stirrupDiameterMm + (config.Layer1Diameter / 2.0);
                double z2 = z1 + gap;

                var yPositions2 = BeamMainBarCalculator.ComputeTransverseYPositions(
                    span.Width, span.Cover, stirrupDiameterMm, config.Layer2Diameter, config.Layer2Count);

                for (int i = 0; i < yPositions2.Count; i++)
                {
                    double y = yPositions2[i];
                    var pts = new List<Point3>
                    {
                        new(xStart, y, z2),
                        new(xEnd, y, z2)
                    };

                    result.Add(new BarPolyline
                    {
                        BarIndex = barId++,
                        Type = BarType.AdditionalBottom,
                        Diameter = config.Layer2Diameter,
                        Layer = 2,
                        HostSpanIndex = sIdx,
                        Polyline = new Polyline3(pts),
                        TransverseY = y,
                        BarTypeName = config.BarTypeName
                    });
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Overload taking general BeamAdditionalBarSpec.
    /// </summary>
    public static IReadOnlyList<BarPolyline> ComputeSpanBottomBars(
        BeamContinuousStack stack,
        BeamAdditionalBarSpec spec,
        double stirrupDiameterMm)
    {
        return ComputeSpanBottomBars(stack, new BeamAdditionalBottomBarSpec { SpanBottomBars = spec.SpanBottomBars }, stirrupDiameterMm);
    }
}
