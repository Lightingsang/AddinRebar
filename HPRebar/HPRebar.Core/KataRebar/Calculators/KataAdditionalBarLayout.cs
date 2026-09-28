using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Additional top bars over the supports (rows 13-16, cut at the H3/H5 ratios of the longer adjacent span)
/// and additional bottom bars in the spans (rows 17-18). Not drawn in Revit yet: the cut-off rules still
/// have to be checked against Kata's own drawings.
/// </summary>
public static partial class KataAdditionalBarLayout
{
    public static (List<KataRebarCurve> Top, List<KataRebarCurve> Bottom) Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        ref int barId)
    {
        var top = BuildTop(spec, rules, st, ref barId);
        var bottom = BuildBottom(spec, rules, st, ref barId);
        return (top, bottom);
    }

    private static List<KataRebarCurve> BuildTop(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        ref int barId)
    {
        int spanCount = st.SpanCount;
        int suppCount = spec.Supports.Count;
        double[] suppLeft = st.SupportStart;
        double[] suppRight = st.SupportEnd;
        double coverStirrup = rules.StirrupCover;
        double stirrupDia = rules.StirrupDiameter;
        double beamWidth = spec.Width;
        double beamHeight = spec.Height;
        double zTop = 0.0;
        var suppCenter = new double[suppLeft.Length];
        for (int i = 0; i < suppCenter.Length; i++) suppCenter[i] = st.SupportCentre(i);
        var extraTopBars = new List<KataRebarCurve>();

        // 4. Support Top Additional Bars (Negative Moment)
        double zTopCont = spec.TopContinuous.IsEmpty
            ? zTop - coverStirrup - stirrupDia - 10.0
            : -rules.TopBarCentreDepth;

        for (int k = 0; k < Math.Min(suppCount, spanCount + 1); k++)
        {
            var supp = spec.Supports[k];
            if (supp.IsCantilever || supp.ColumnWidth <= 0.0)
                continue;

            bool isExteriorStart = (k == 0);
            bool isExteriorEnd = (k == suppCount - 1);
            double currentTopZ = zTopCont;

            for (int layerIdx = 1; layerIdx <= 4; layerIdx++)
            {
                var layerItems = supp.AllTopExtraLayers[layerIdx - 1];
                if (layerItems.Count == 0)
                    continue;

                double maxDia = 0.0;
                foreach (var it in layerItems)
                {
                    if (it.Diameter > maxDia) maxDia = it.Diameter;
                }
                double gap = Math.Max(maxDia + 30.0, 50.0);
                currentTopZ -= gap;
                double zLayer = currentTopZ;

                double ratio = layerIdx == 1
                    ? (spec.TopCutoffRatioLayer1 > 0.0 ? spec.TopCutoffRatioLayer1 : 0.25)
                    : (spec.TopCutoffRatioLayer2 > 0.0 ? spec.TopCutoffRatioLayer2 : 0.20);

                var origin = layerIdx == 1 ? spec.CutoffOriginLayer1 : spec.CutoffOriginLayer2;

                foreach (var item in layerItems)
                {
                    if (item.IsEmpty)
                        continue;

                    int count = item.Count;
                    double dia = item.Diameter;

                    double availDrop = Math.Max(0.0, beamHeight - (2.0 * coverStirrup) - (2.0 * stirrupDia));
                    double hookLen = Math.Min(availDrop, Math.Max(spec.CompressionLapMultiplier * dia, 200.0));

                    var yPositions = KataRebarCalculator.ComputeTransverseYPositions(beamWidth, coverStirrup, stirrupDia, dia, count);

                    if (isExteriorStart)
                    {
                        double lnRight = spec.Spans[0].Length;
                        double lCutoff = lnRight * ratio;
                        double xStart = suppLeft[0] + coverStirrup;
                        double xEnd = (origin == KataCutoffOrigin.FromColumnFace)
                            ? suppRight[0] + lCutoff
                            : suppCenter[0] + lCutoff;
                        double dimA = xEnd - xStart;
                        double dimB = hookLen;

                        foreach (double y in yPositions)
                        {
                            var pts = new List<Point3>
                            {
                                new(xStart, y, zLayer - hookLen),
                                new(xStart, y, zLayer),
                                new(xEnd, y, zLayer)
                            };

                            extraTopBars.Add(new KataRebarCurve
                            {
                                BarId = barId++,
                                Role = KataBarRole.ExtraTop,
                                Diameter = dia,
                                Layer = layerIdx,
                                Polyline = new Polyline3(pts).Simplify(1.0),
                                StartHookAngle = HookAngle.Hook90,
                                EndHookAngle = HookAngle.None,
                                StartHookLength = hookLen,
                                EndHookLength = 0.0,
                                TransverseY = y,
                                HostSpanIndex = 0,
                                HostSupportIndex = k,
                                ShapeCode = hookLen > 0.0 ? "05a" : "00",
                                BarMark = $"3.{k + 1}.{layerIdx}",
                                BarDescription = $"Gia cường gối {k + 1} L{layerIdx}",
                                DimA = dimA,
                                DimB = dimB,
                                DimC = 0.0,
                                DimR = 2.0 * dia,
                                SttCad = 3
                            });
                        }
                    }
                    else if (isExteriorEnd)
                    {
                        double lnLeft = spec.Spans[spanCount - 1].Length;
                        double lCutoff = lnLeft * ratio;
                        double xStart = (origin == KataCutoffOrigin.FromColumnFace)
                            ? suppLeft[k] - lCutoff
                            : suppCenter[k] - lCutoff;
                        double xEnd = suppRight[k] - coverStirrup;
                        double dimA = xEnd - xStart;
                        double dimB = hookLen;

                        foreach (double y in yPositions)
                        {
                            var pts = new List<Point3>
                            {
                                new(xStart, y, zLayer),
                                new(xEnd, y, zLayer),
                                new(xEnd, y, zLayer - hookLen)
                            };

                            extraTopBars.Add(new KataRebarCurve
                            {
                                BarId = barId++,
                                Role = KataBarRole.ExtraTop,
                                Diameter = dia,
                                Layer = layerIdx,
                                Polyline = new Polyline3(pts).Simplify(1.0),
                                StartHookAngle = HookAngle.None,
                                EndHookAngle = HookAngle.Hook90,
                                StartHookLength = 0.0,
                                EndHookLength = hookLen,
                                TransverseY = y,
                                HostSpanIndex = spanCount - 1,
                                HostSupportIndex = k,
                                ShapeCode = hookLen > 0.0 ? "05a" : "00",
                                BarMark = $"3.{k + 1}.{layerIdx}",
                                BarDescription = $"Gia cường gối {k + 1} L{layerIdx}",
                                DimA = dimA,
                                DimB = dimB,
                                DimC = 0.0,
                                DimR = 2.0 * dia,
                                SttCad = 3
                            });
                        }
                    }
                    else
                    {
                        // Interior support: Lcutoff = max(Lleft, Lright) * ratio
                        double lnLeft = (k - 1 >= 0 && k - 1 < spanCount) ? spec.Spans[k - 1].Length : 0.0;
                        double lnRight = (k < spanCount) ? spec.Spans[k].Length : 0.0;
                        double maxLn = Math.Max(lnLeft, lnRight);
                        double lCutoff = maxLn * ratio;

                        double xStart = (origin == KataCutoffOrigin.FromColumnFace)
                            ? suppLeft[k] - lCutoff
                            : suppCenter[k] - lCutoff;

                        double xEnd = (origin == KataCutoffOrigin.FromColumnFace)
                            ? suppRight[k] + lCutoff
                            : suppCenter[k] + lCutoff;
                        double dimA = xEnd - xStart;

                        foreach (double y in yPositions)
                        {
                            var pts = new List<Point3>
                            {
                                new(xStart, y, zLayer),
                                new(xEnd, y, zLayer)
                            };

                            extraTopBars.Add(new KataRebarCurve
                            {
                                BarId = barId++,
                                Role = KataBarRole.ExtraTop,
                                Diameter = dia,
                                Layer = layerIdx,
                                Polyline = new Polyline3(pts).Simplify(1.0),
                                StartHookAngle = HookAngle.None,
                                EndHookAngle = HookAngle.None,
                                StartHookLength = 0.0,
                                EndHookLength = 0.0,
                                TransverseY = y,
                                HostSpanIndex = -1,
                                HostSupportIndex = k,
                                ShapeCode = "00",
                                BarMark = $"3.{k + 1}.{layerIdx}",
                                BarDescription = $"Gia cường gối {k + 1} L{layerIdx}",
                                DimA = dimA,
                                DimB = 0.0,
                                DimC = 0.0,
                                DimR = 0.0,
                                SttCad = 3
                            });
                        }
                    }
                }
            }
        }

        return extraTopBars;
    }
}
