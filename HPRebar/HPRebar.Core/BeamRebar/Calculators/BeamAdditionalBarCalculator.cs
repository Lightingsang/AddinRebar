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
            var section = SupportTopSection.At(stack, config.SupportIndex, stirrupDiameterMm);
            if (section is null)
            {
                continue;
            }

            if (config.Layer1Count > 0)
            {
                result.AddRange(
                    PlaceSupportTopLayer(section, config, layer: 1, stirrupDiameterMm, firstBarIndex: result.Count));
            }

            if (config.Layer2Count > 0)
            {
                result.AddRange(
                    PlaceSupportTopLayer(section, config, layer: 2, stirrupDiameterMm, firstBarIndex: result.Count));
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

    private enum SupportPosition
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
    /// <param name="HostSpanIndex">The adjacent span of an end support; -1 (no host span) for an interior one.</param>
    /// <param name="ZHookFloor">Lowest level a hook may reach (bottom cover + stirrup); not used at an interior support.</param>
    private sealed record SupportTopSection(
        int SupportIndex,
        BeamSupportNode Support,
        SupportPosition Position,
        int HostSpanIndex,
        double ZTop,
        double Cover,
        double Width,
        double ClearLengthLeft,
        double ClearLengthRight,
        double ZHookFloor)
    {
        /// <summary>Null when the support index is out of range, or an end support has no span.</summary>
        public static SupportTopSection? At(BeamContinuousStack stack, int supportIndex, double stirrupDiameterMm)
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

                return new SupportTopSection(
                    supportIndex,
                    support,
                    isStart ? SupportPosition.Start : SupportPosition.End,
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

            return new SupportTopSection(
                supportIndex,
                support,
                SupportPosition.Interior,
                HostSpanIndex: -1,
                Math.Min(leftSpan.TopElevation, rightSpan.TopElevation),
                Math.Max(leftSpan.Cover, rightSpan.Cover),
                Math.Min(leftSpan.Width, rightSpan.Width),
                leftSpan.LengthClear,
                rightSpan.LengthClear,
                ZHookFloor: 0.0);
        }
    }

    /// <summary>What one layer of a support's top bars uses: bar count, diameter, cutoff ratio and bar height.</summary>
    private readonly record struct TopLayerSetOut(int Count, double Diameter, double Ratio, double Z);

    /// <summary>
    /// Layer 1 sits under the top cover and stirrup; layer 2 one layer gap below it. Both heights are taken
    /// from the layer-1 diameter, as the gap default is.
    /// </summary>
    private static TopLayerSetOut SetOutTopLayer(
        SupportTopSection section,
        SupportAdditionalTopBarConfig config,
        int layer,
        double stirrupDiameterMm)
    {
        double z = section.ZTop - section.Cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);

        if (layer == 1)
        {
            double ratio1 = config.Layer1ExtensionRatio > 0 ? config.Layer1ExtensionRatio : DefaultTopCutoffRatioLayer1;
            return new TopLayerSetOut(config.Layer1Count, config.Layer1Diameter, ratio1, z);
        }

        z -= config.LayerGap > 0 ? config.LayerGap : (config.Layer1Diameter + MinimumClearVerticalGapMm);
        double ratio2 = config.Layer2ExtensionRatio > 0 ? config.Layer2ExtensionRatio : DefaultTopCutoffRatioLayer2;
        return new TopLayerSetOut(config.Layer2Count, config.Layer2Diameter, ratio2, z);
    }

    /// <summary>
    /// One layer of top bars over a support: a straight bar at an interior support, a bar hooked down at the
    /// beam's end at an end support, the hook cut so it stops above the bottom cover and stirrup.
    /// </summary>
    private static IEnumerable<BarPolyline> PlaceSupportTopLayer(
        SupportTopSection section,
        SupportAdditionalTopBarConfig config,
        int layer,
        double stirrupDiameterMm,
        int firstBarIndex)
    {
        var setOut = SetOutTopLayer(section, config, layer, stirrupDiameterMm);
        double z = setOut.Z;
        var support = section.Support;
        double extensionLeft = setOut.Ratio * section.ClearLengthLeft;
        double extensionRight = setOut.Ratio * section.ClearLengthRight;
        double hook = section.Position == SupportPosition.Interior ? 0.0 : ExteriorHook(section, config, setOut.Diameter, z);

        var yPositions = BeamMainBarCalculator.ComputeTransverseYPositions(
            section.Width, section.Cover, stirrupDiameterMm, setOut.Diameter, setOut.Count);

        for (int i = 0; i < yPositions.Count; i++)
        {
            double y = yPositions[i];
            var bar = new BarPolyline
            {
                BarIndex = firstBarIndex + i,
                Type = BarType.AdditionalTop,
                Diameter = setOut.Diameter,
                Layer = layer,
                HostSupportIndex = section.SupportIndex,
                HostSpanIndex = section.HostSpanIndex,
                TransverseY = y,
                BarTypeName = config.BarTypeName
            };

            yield return section.Position switch
            {
                SupportPosition.Start => bar with
                {
                    Polyline = new Polyline3(new List<Point3>
                    {
                        new(support.LeftFaceX + section.Cover, y, z - hook),
                        new(support.LeftFaceX + section.Cover, y, z),
                        new(support.RightFaceX + extensionRight, y, z)
                    }),
                    StartHookAngle = HookAngle.Hook90,
                    EndHookAngle = HookAngle.None,
                    StartHookLength = hook,
                    LeftExtension = support.Width,
                    RightExtension = extensionRight
                },
                SupportPosition.End => bar with
                {
                    Polyline = new Polyline3(new List<Point3>
                    {
                        new(support.LeftFaceX - extensionLeft, y, z),
                        new(support.RightFaceX - section.Cover, y, z),
                        new(support.RightFaceX - section.Cover, y, z - hook)
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

    /// <summary>
    /// The exterior hook: the configured length, or the default leg, cut to the room above the bottom cover.
    /// </summary>
    private static double ExteriorHook(
        SupportTopSection section, SupportAdditionalTopBarConfig config, double diameter, double z)
    {
        double availableDrop = Math.Max(0.0, z - section.ZHookFloor);
        return config.ExteriorHookLength > 0.0
            ? Math.Min(availableDrop, config.ExteriorHookLength)
            : Math.Min(availableDrop, BeamHookLength.Default(diameter));
    }
}
