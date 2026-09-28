using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>Additional bottom bars of the spans (rows 17-18), cut at a fixed L/7 from the support faces.</summary>
public static partial class KataAdditionalBarLayout
{
    private static List<KataRebarCurve> BuildBottom(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        ref int barId)
    {
        int spanCount = st.SpanCount;
        double[] spanStart = st.SpanStart;
        double[] spanEnd = st.SpanEnd;
        double coverStirrup = rules.StirrupCover;
        double stirrupDia = rules.StirrupDiameter;
        double beamWidth = spec.Width;
        double beamHeight = spec.Height;
        double zBot = -beamHeight;
        var extraBottomBars = new List<KataRebarCurve>();

        // 5. Span Bottom Additional Bars (Midspan Positive Moment)
        double zBotCont = spec.BottomContinuous.IsEmpty
            ? zBot + coverStirrup + stirrupDia + 10.0
            : zBot + rules.BottomBarCentreDepth;

        for (int s = 0; s < spanCount; s++)
        {
            var span = spec.Spans[s];
            double ln = span.Length;
            if (ln <= 0.0)
                continue;

            double rCut = 1.0 / 7.0; // standard L/7 cutoff
            double dCut = ln * rCut;
            double xStart = spanStart[s] + dCut;
            double xEnd = spanEnd[s] - dCut;

            if (xEnd <= xStart)
                continue;

            double currentBotZ = zBotCont;

            for (int layerIdx = 1; layerIdx <= 2; layerIdx++)
            {
                var layerItems = span.AllBottomExtraLayers[layerIdx - 1];
                if (layerItems.Count == 0)
                    continue;

                double maxDia = 0.0;
                foreach (var it in layerItems)
                {
                    if (it.Diameter > maxDia) maxDia = it.Diameter;
                }
                double gap = Math.Max(maxDia + 30.0, 50.0);
                currentBotZ += gap;
                double zLayer = currentBotZ;

                foreach (var item in layerItems)
                {
                    if (item.IsEmpty)
                        continue;

                    int count = item.Count;
                    double dia = item.Diameter;

                    var yPositions = KataRebarCalculator.ComputeTransverseYPositions(beamWidth, coverStirrup, stirrupDia, dia, count);

                    double dimA = xEnd - xStart;

                    foreach (double y in yPositions)
                    {
                        var pts = new List<Point3>
                        {
                            new(xStart, y, zLayer),
                            new(xEnd, y, zLayer)
                        };

                        extraBottomBars.Add(new KataRebarCurve
                        {
                            BarId = barId++,
                            Role = KataBarRole.ExtraBottom,
                            Diameter = dia,
                            Layer = layerIdx,
                            Polyline = new Polyline3(pts).Simplify(1.0),
                            StartHookAngle = HookAngle.None,
                            EndHookAngle = HookAngle.None,
                            StartHookLength = 0.0,
                            EndHookLength = 0.0,
                            TransverseY = y,
                            HostSpanIndex = s,
                            HostSupportIndex = -1,
                            ShapeCode = "00",
                            BarMark = $"4.{s + 1}.{layerIdx}",
                            BarDescription = $"Gia cường nhịp {s + 1} L{layerIdx}",
                            DimA = dimA,
                            DimB = 0.0,
                            DimC = 0.0,
                            DimR = 0.0,
                            SttCad = 4
                        });
                    }
                }
            }
        }

        return extraBottomBars;
    }
}
