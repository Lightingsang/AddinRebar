using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Longitudinal bars on the beam sides: row 20 of a span, the G4/G5 layers, or two Ø12 per 300 mm when the
/// beam is 700 mm deep or more. Not drawn in Revit yet: row 20 of a span is Kata's "cốt giá" override and
/// its meaning still has to be settled.
/// </summary>
public static class KataSideBarLayout
{
    public static List<KataRebarCurve> Build(
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
        double zTop = 0.0;
        double zBot = -beamHeight;
        var sideBars = new List<KataRebarCurve>();

        // 6. Side Bars / Web Skin Reinforcement (h >= 700 mm or specified)
        bool deepBeamRequired = beamHeight >= 700.0;
        bool hasGlobalSideBars = spec.GlobalSideBars.Count > 0;

        for (int s = 0; s < spanCount; s++)
        {
            var span = spec.Spans[s];
            IReadOnlyList<KataBarItem> activeSideBars;

            if (span.SideBars.Count > 0)
            {
                // If explicitly set to count 0 (e.g. 0f12), suppress side bars in this span
                if (span.SideBars[0].Count == 0)
                    continue;

                activeSideBars = span.SideBars;
            }
            else if (hasGlobalSideBars)
            {
                activeSideBars = spec.GlobalSideBars;
            }
            else if (deepBeamRequired)
            {
                // Auto-generate based on <= 300mm vertical spacing
                double mainDia = !spec.TopContinuous.IsEmpty ? spec.TopContinuous.Diameter : 20.0;
                double zOffset = coverStirrup + stirrupDia + (mainDia / 2.0);
                double clearVerticalSpanMm = beamHeight - (2.0 * zOffset);
                int spaces = (int)Math.Ceiling(clearVerticalSpanMm / 300.0);
                int nRows = Math.Max(1, spaces - 1);

                var generated = new List<KataBarItem>(nRows);
                for (int r = 1; r <= nRows; r++)
                {
                    generated.Add(new KataBarItem(2, 12.0, r, 0.0, "2f12"));
                }
                activeSideBars = generated;
            }
            else
            {
                continue;
            }

            int rowCount = activeSideBars.Count;
            if (rowCount == 0)
                continue;

            double zBotMain = spec.BottomContinuous.IsEmpty
                ? zBot + coverStirrup + stirrupDia + 10.0
                : zBot + rules.BottomBarCentreDepth;
            double zTopMain = spec.TopContinuous.IsEmpty
                ? zTop - coverStirrup - stirrupDia - 10.0
                : -rules.TopBarCentreDepth;
            double deltaZ = (zTopMain - zBotMain) / (rowCount + 1);

            double xStart = spanStart[s];
            double xEnd = spanEnd[s];

            for (int r = 1; r <= rowCount; r++)
            {
                var item = activeSideBars[r - 1];
                double dia = item.Diameter > 0.0 ? item.Diameter : 12.0;
                double zRow = zBotMain + (r * deltaZ);

                double yLeft = -(beamWidth / 2.0) + coverStirrup + stirrupDia + (dia / 2.0);
                double yRight = +(beamWidth / 2.0) - coverStirrup - stirrupDia - (dia / 2.0);
                double dimA = xEnd - xStart;

                // Left lateral face bar
                var ptsLeft = new List<Point3>
                {
                    new(xStart, yLeft, zRow),
                    new(xEnd, yLeft, zRow)
                };
                sideBars.Add(new KataRebarCurve
                {
                    BarId = barId++,
                    Role = KataBarRole.SideBar,
                    Diameter = dia,
                    Layer = r,
                    Polyline = new Polyline3(ptsLeft).Simplify(1.0),
                    StartHookAngle = HookAngle.None,
                    EndHookAngle = HookAngle.None,
                    TransverseY = yLeft,
                    HostSpanIndex = s,
                    HostSupportIndex = -1,
                    ShapeCode = "00",
                    BarMark = $"5.{r}",
                    BarDescription = $"Cốt sườn H{r}",
                    DimA = dimA,
                    DimB = 0.0,
                    DimC = 0.0,
                    DimR = 0.0,
                    SttCad = 5
                });

                // Right lateral face bar
                var ptsRight = new List<Point3>
                {
                    new(xStart, yRight, zRow),
                    new(xEnd, yRight, zRow)
                };
                sideBars.Add(new KataRebarCurve
                {
                    BarId = barId++,
                    Role = KataBarRole.SideBar,
                    Diameter = dia,
                    Layer = r,
                    Polyline = new Polyline3(ptsRight).Simplify(1.0),
                    StartHookAngle = HookAngle.None,
                    EndHookAngle = HookAngle.None,
                    TransverseY = yRight,
                    HostSpanIndex = s,
                    HostSupportIndex = -1,
                    ShapeCode = "00",
                    BarMark = $"5.{r}",
                    BarDescription = $"Cốt sườn H{r}",
                    DimA = dimA,
                    DimB = 0.0,
                    DimC = 0.0,
                    DimR = 0.0,
                    SttCad = 5
                });
            }
        }

        return sideBars;
    }
}
