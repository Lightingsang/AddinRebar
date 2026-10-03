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

        foreach (var config in spec.SupportTopBars)
        {
            var node = SupportTopNode.At(stack, config.SupportIndex, stirrupDiameterMm);
            if (node is null)
            {
                continue;
            }

            if (config.Layer1Count > 0)
            {
                result.AddRange(PlaceSupportTopLayer(node, config, layer: 1, stirrupDiameterMm, firstBarIndex: result.Count));
            }

            if (config.Layer2Count > 0)
            {
                result.AddRange(PlaceSupportTopLayer(node, config, layer: 2, stirrupDiameterMm, firstBarIndex: result.Count));
            }
        }

        return result;
    }

    private enum SupportEnd
    {
        /// <summary>Support 0: the bar hooks down at the beam's start and runs into the first span.</summary>
        Start,

        /// <summary>Last support: the bar runs in from the last span and hooks down at the beam's end.</summary>
        End,

        /// <summary>Between two spans: a straight bar reaching into both.</summary>
        Interior
    }

    /// <summary>
    /// The section the top bars over one support are set out in: the adjacent span for an end support, the
    /// shallower top, larger cover and narrower width of the two spans for an interior one.
    /// </summary>
    private sealed record SupportTopNode(
        int SupportIndex,
        BeamSupportNode Support,
        SupportEnd End,
        int HostSpanIndex,
        double ZTop,
        double Cover,
        double Width,
        double ClearLengthLeft,
        double ClearLengthRight,
        double ZHookFloor)
    {
        /// <summary>Null when the support index is out of range, or an end support has no span.</summary>
        public static SupportTopNode? At(BeamContinuousStack stack, int supportIndex, double stirrupDiameterMm)
        {
            if (supportIndex < 0 || supportIndex >= stack.Supports.Count)
            {
                return null;
            }

            var support = stack.Supports[supportIndex];
            bool isStart = supportIndex == 0;
            bool isEnd = supportIndex == stack.Supports.Count - 1;

            if (isStart || isEnd)
            {
                if (stack.Spans.Count == 0)
                {
                    return null;
                }

                int spanIndex = isStart ? 0 : stack.Spans.Count - 1;
                var span = stack.Spans[spanIndex];
                double ln = span.LengthClear;

                return new SupportTopNode(
                    supportIndex,
                    support,
                    isStart ? SupportEnd.Start : SupportEnd.End,
                    spanIndex,
                    span.TopElevation,
                    span.Cover,
                    span.Width,
                    ClearLengthLeft: isStart ? 0.0 : ln,
                    ClearLengthRight: isStart ? ln : 0.0,
                    ZHookFloor: span.BottomElevation + span.Cover + stirrupDiameterMm);
            }

            var leftSpan = stack.Spans[supportIndex - 1];
            var rightSpan = stack.Spans[supportIndex];

            return new SupportTopNode(
                supportIndex,
                support,
                SupportEnd.Interior,
                HostSpanIndex: -1,
                Math.Min(leftSpan.TopElevation, rightSpan.TopElevation),
                Math.Max(leftSpan.Cover, rightSpan.Cover),
                Math.Min(leftSpan.Width, rightSpan.Width),
                leftSpan.LengthClear,
                rightSpan.LengthClear,
                ZHookFloor: 0.0);
        }
    }

    /// <summary>
    /// One layer of top bars over a support. Layer 2 sits one layer gap below layer 1; an end support's bars
    /// hook down, the hook cut so it stops above the bottom cover and stirrup.
    /// </summary>
    private static IEnumerable<BarPolyline> PlaceSupportTopLayer(
        SupportTopNode node,
        SupportAdditionalTopBarConfig config,
        int layer,
        double stirrupDiameterMm,
        int firstBarIndex)
    {
        int count = layer == 1 ? config.Layer1Count : config.Layer2Count;
        double diameter = layer == 1 ? config.Layer1Diameter : config.Layer2Diameter;
        double ratio = layer == 1
            ? (config.Layer1ExtensionRatio > 0 ? config.Layer1ExtensionRatio : DefaultTopCutoffRatioLayer1)
            : (config.Layer2ExtensionRatio > 0 ? config.Layer2ExtensionRatio : DefaultTopCutoffRatioLayer2);

        double z = node.ZTop - node.Cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
        if (layer == 2)
        {
            z -= config.LayerGap > 0 ? config.LayerGap : (config.Layer1Diameter + MinimumClearVerticalGapMm);
        }

        var support = node.Support;
        double extensionLeft = ratio * node.ClearLengthLeft;
        double extensionRight = ratio * node.ClearLengthRight;
        double hook = node.End == SupportEnd.Interior ? 0.0 : ExteriorHook(node, config, diameter, z);

        var yPositions = BeamMainBarCalculator.ComputeTransverseYPositions(
            node.Width, node.Cover, stirrupDiameterMm, diameter, count);

        for (int i = 0; i < yPositions.Count; i++)
        {
            double y = yPositions[i];
            var bar = new BarPolyline
            {
                BarIndex = firstBarIndex + i,
                Type = BarType.AdditionalTop,
                Diameter = diameter,
                Layer = layer,
                HostSupportIndex = node.SupportIndex,
                TransverseY = y,
                BarTypeName = config.BarTypeName
            };

            yield return node.End switch
            {
                SupportEnd.Start => bar with
                {
                    HostSpanIndex = node.HostSpanIndex,
                    Polyline = new Polyline3(new List<Point3>
                    {
                        new(support.LeftFaceX + node.Cover, y, z - hook),
                        new(support.LeftFaceX + node.Cover, y, z),
                        new(support.RightFaceX + extensionRight, y, z)
                    }),
                    StartHookAngle = HookAngle.Hook90,
                    EndHookAngle = HookAngle.None,
                    StartHookLength = hook,
                    LeftExtension = support.Width,
                    RightExtension = extensionRight
                },
                SupportEnd.End => bar with
                {
                    HostSpanIndex = node.HostSpanIndex,
                    Polyline = new Polyline3(new List<Point3>
                    {
                        new(support.LeftFaceX - extensionLeft, y, z),
                        new(support.RightFaceX - node.Cover, y, z),
                        new(support.RightFaceX - node.Cover, y, z - hook)
                    }),
                    StartHookAngle = HookAngle.None,
                    EndHookAngle = HookAngle.Hook90,
                    EndHookLength = hook,
                    LeftExtension = extensionLeft,
                    RightExtension = support.Width
                },
                _ => bar with
                {
                    Polyline = new Polyline3(new List<Point3>
                    {
                        new(support.LeftFaceX - extensionLeft, y, z),
                        new(support.RightFaceX + extensionRight, y, z)
                    }),
                    LeftExtension = extensionLeft,
                    RightExtension = extensionRight
                }
            };
        }
    }

    /// <summary>The exterior hook: the configured length, or the default leg, cut to the room above the bottom cover.</summary>
    private static double ExteriorHook(SupportTopNode node, SupportAdditionalTopBarConfig config, double diameter, double z)
    {
        double availableDrop = Math.Max(0.0, z - node.ZHookFloor);
        return config.ExteriorHookLength > 0.0
            ? Math.Min(availableDrop, config.ExteriorHookLength)
            : Math.Min(availableDrop, BeamHookLength.Default(diameter));
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
