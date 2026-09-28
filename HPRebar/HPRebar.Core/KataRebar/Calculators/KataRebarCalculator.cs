using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Core rebar geometry and 3-zone stirrup distribution calculator for Kata beam rebar specifications.
/// Pure logic layer with 0 dependencies on Autodesk.Revit.* and 100% netstandard2.0 purity.
/// </summary>
public static class KataRebarCalculator
{
    private const double DefaultStartOffsetMm = 50.0;
    private const double SteelDensityKgPerM3 = 7850.0;

    /// <summary>
    /// Computes the transverse Y positions centered across the beam width.
    /// Centered at Y = 0.
    /// </summary>
    public static IReadOnlyList<double> ComputeTransverseYPositions(
        double widthMm,
        double coverMm,
        double stirrupDiameterMm,
        double barDiameterMm,
        int count)
    {
        if (count <= 1)
            return new[] { 0.0 };

        double y0 = -(widthMm / 2.0) + coverMm + stirrupDiameterMm + (barDiameterMm / 2.0);
        double yn = +(widthMm / 2.0) - coverMm - stirrupDiameterMm - (barDiameterMm / 2.0);

        if (y0 >= yn)
        {
            var centered = new List<double>(count);
            for (int i = 0; i < count; i++) centered.Add(0.0);
            return centered;
        }

        if (count == 2)
            return new[] { y0, yn };

        double deltaY = (yn - y0) / (count - 1);
        var positions = new List<double>(count);
        for (int i = 0; i < count; i++)
        {
            positions.Add(y0 + (i * deltaY));
        }

        return positions;
    }

    /// <summary>
    /// Main entry point: transforms a <see cref="KataBeamRebarSpec"/> into explicit 3D rebar curves,
    /// hook anchorages, multi-layer cutoffs, web side bars, and 3-zone stirrup distributions.
    /// </summary>
    /// <param name="spec">The structural beam reinforcement specification.</param>
    /// <returns>A fully computed <see cref="KataRebarLayoutResult"/>.</returns>
    public static KataRebarLayoutResult Calculate(KataBeamRebarSpec spec)
    {
        if (spec is null)
            throw new ArgumentNullException(nameof(spec));

        var warnings = new List<string>();

        if (spec.Spans.Count == 0)
        {
            return new KataRebarLayoutResult
            {
                BeamName = spec.BeamName,
                Warnings = new[] { "Beam specification contains no spans." }
            };
        }

        if (spec.Width <= 0.0 || spec.Height <= 0.0)
        {
            return new KataRebarLayoutResult
            {
                BeamName = spec.BeamName,
                Warnings = new[] { "Beam dimensions width and height must be strictly positive." }
            };
        }

        int spanCount = spec.Spans.Count;
        int suppCount = spec.Supports.Count;

        // 1. Establish longitudinal stations along X-axis
        double curX = 0.0;
        var suppLeft = new double[Math.Max(suppCount, spanCount + 1)];
        var suppRight = new double[Math.Max(suppCount, spanCount + 1)];
        var suppCenter = new double[Math.Max(suppCount, spanCount + 1)];
        var spanStart = new double[spanCount];
        var spanEnd = new double[spanCount];

        for (int i = 0; i < spanCount; i++)
        {
            double colWidth = (i < suppCount) ? Math.Max(0.0, spec.Supports[i].ColumnWidth) : 0.0;
            suppLeft[i] = curX;
            suppRight[i] = curX + colWidth;
            suppCenter[i] = curX + (colWidth / 2.0);
            curX += colWidth;

            double spanLen = Math.Max(0.0, spec.Spans[i].Length);
            spanStart[i] = curX;
            spanEnd[i] = curX + spanLen;
            curX += spanLen;
        }

        // Final support
        int lastSuppIdx = spanCount;
        double lastColWidth = (lastSuppIdx < suppCount) ? Math.Max(0.0, spec.Supports[lastSuppIdx].ColumnWidth) : 0.0;
        suppLeft[lastSuppIdx] = curX;
        suppRight[lastSuppIdx] = curX + lastColWidth;
        suppCenter[lastSuppIdx] = curX + (lastColWidth / 2.0);
        curX += lastColWidth;

        double totalBeamLength = curX;

        // Cantilever checks
        bool isLeftCantilever = suppCount > 0 && (spec.Supports[0].ColumnWidth <= 0.0 || spec.Supports[0].IsCantilever);
        bool isRightCantilever = suppCount > spanCount && (spec.Supports[spanCount].ColumnWidth <= 0.0 || spec.Supports[spanCount].IsCantilever);

        double coverStirrup = spec.CoverStirrup > 0.0 ? spec.CoverStirrup : 25.0;
        double stirrupDia = spec.GlobalStirrup.Diameter > 0.0 ? spec.GlobalStirrup.Diameter : 10.0;
        double beamWidth = spec.Width;
        double beamHeight = spec.Height;

        double zTop = 0.0;
        double zBot = -beamHeight;

        int barId = 1;
        var mainTopBars = new List<KataRebarCurve>();
        var mainBottomBars = new List<KataRebarCurve>();
        var extraTopBars = new List<KataRebarCurve>();
        var extraBottomBars = new List<KataRebarCurve>();
        var sideBars = new List<KataRebarCurve>();
        var stirrupZones = new List<KataStirrupZoneResult>();
        var individualStirrups = new List<KataRebarCurve>();

        // 2. Continuous Top Main Bars
        if (!spec.TopContinuous.IsEmpty)
        {
            int count = spec.TopContinuous.Count;
            double dia = spec.TopContinuous.Diameter;
            double zTopBar = zTop - coverStirrup - stirrupDia - (dia / 2.0);

            double availHeightStart = Math.Max(0.0, beamHeight - (2.0 * coverStirrup) - (2.0 * stirrupDia));
            double hookStart = Math.Min(availHeightStart, Math.Max(spec.CompressionLapMultiplier * dia, 200.0));

            double availHeightEnd = Math.Max(0.0, beamHeight - (2.0 * coverStirrup) - (2.0 * stirrupDia));
            double hookEnd = Math.Min(availHeightEnd, Math.Max(spec.CompressionLapMultiplier * dia, 200.0));

            double xStart = 0.0 + coverStirrup;
            double xEnd = totalBeamLength - coverStirrup;

            string shapeCode = (hookStart > 0.0 && hookEnd > 0.0) ? "15a" : ((hookStart > 0.0 || hookEnd > 0.0) ? "05a" : "00");
            double dimA = xEnd - xStart;
            double dimB = hookStart;
            double dimC = hookEnd;

            var yPositions = ComputeTransverseYPositions(beamWidth, coverStirrup, stirrupDia, dia, count);

            foreach (double y in yPositions)
            {
                var pts = new List<Point3>
                {
                    new(xStart, y, zTopBar - hookStart),
                    new(xStart, y, zTopBar),
                    new(xEnd, y, zTopBar),
                    new(xEnd, y, zTopBar - hookEnd)
                };

                mainTopBars.Add(new KataRebarCurve
                {
                    BarId = barId++,
                    Role = KataBarRole.MainTop,
                    Diameter = dia,
                    Layer = 1,
                    Polyline = new Polyline3(pts).Simplify(1.0),
                    StartHookAngle = HookAngle.Hook90,
                    EndHookAngle = HookAngle.Hook90,
                    StartHookLength = hookStart,
                    EndHookLength = hookEnd,
                    TransverseY = y,
                    HostSpanIndex = -1,
                    HostSupportIndex = -1,
                    ShapeCode = shapeCode,
                    BarMark = "1",
                    BarDescription = "Thép chủ trên",
                    DimA = dimA,
                    DimB = dimB,
                    DimC = dimC,
                    DimR = 2.0 * dia,
                    SttCad = 1
                });
            }
        }

        // 3. Continuous Bottom Main Bars
        if (!spec.BottomContinuous.IsEmpty)
        {
            int count = spec.BottomContinuous.Count;
            double dia = spec.BottomContinuous.Diameter;
            double zBotBar = zBot + coverStirrup + stirrupDia + (dia / 2.0);

            double availHeightStart = Math.Max(0.0, beamHeight - (2.0 * coverStirrup) - (2.0 * stirrupDia));
            double hookStart = Math.Min(availHeightStart, Math.Max(spec.CompressionLapMultiplier * dia, 200.0));

            double availHeightEnd = Math.Max(0.0, beamHeight - (2.0 * coverStirrup) - (2.0 * stirrupDia));
            double hookEnd = Math.Min(availHeightEnd, Math.Max(spec.CompressionLapMultiplier * dia, 200.0));

            double xStart;
            HookAngle startHookAngle;
            if (isLeftCantilever && suppCount > 1)
            {
                xStart = suppLeft[1];
                hookStart = 0.0;
                startHookAngle = HookAngle.None;
            }
            else
            {
                xStart = 0.0 + coverStirrup;
                startHookAngle = HookAngle.Hook90;
            }

            double xEnd;
            HookAngle endHookAngle;
            if (isRightCantilever && suppCount > spanCount)
            {
                xEnd = suppRight[spanCount - 1];
                hookEnd = 0.0;
                endHookAngle = HookAngle.None;
            }
            else
            {
                xEnd = totalBeamLength - coverStirrup;
                endHookAngle = HookAngle.Hook90;
            }

            string shapeCode = (hookStart > 0.0 && hookEnd > 0.0) ? "15a" : ((hookStart > 0.0 || hookEnd > 0.0) ? "05a" : "00");
            double dimA = xEnd - xStart;
            double dimB = hookStart;
            double dimC = hookEnd;

            var yPositions = ComputeTransverseYPositions(beamWidth, coverStirrup, stirrupDia, dia, count);

            foreach (double y in yPositions)
            {
                var pts = new List<Point3>();
                if (hookStart > 0.0)
                    pts.Add(new Point3(xStart, y, zBotBar + hookStart));

                pts.Add(new Point3(xStart, y, zBotBar));
                pts.Add(new Point3(xEnd, y, zBotBar));

                if (hookEnd > 0.0)
                    pts.Add(new Point3(xEnd, y, zBotBar + hookEnd));

                mainBottomBars.Add(new KataRebarCurve
                {
                    BarId = barId++,
                    Role = KataBarRole.MainBottom,
                    Diameter = dia,
                    Layer = 1,
                    Polyline = new Polyline3(pts).Simplify(1.0),
                    StartHookAngle = startHookAngle,
                    EndHookAngle = endHookAngle,
                    StartHookLength = hookStart,
                    EndHookLength = hookEnd,
                    TransverseY = y,
                    HostSpanIndex = -1,
                    HostSupportIndex = -1,
                    ShapeCode = shapeCode,
                    BarMark = "2",
                    BarDescription = "Thép chủ dưới",
                    DimA = dimA,
                    DimB = dimB,
                    DimC = dimC,
                    DimR = 2.0 * dia,
                    SttCad = 2
                });
            }
        }

        // 4. Support Top Additional Bars (Negative Moment)
        double topContDia = !spec.TopContinuous.IsEmpty ? spec.TopContinuous.Diameter : 20.0;
        double zTopCont = zTop - coverStirrup - stirrupDia - (topContDia / 2.0);

        for (int k = 0; k < suppCount; k++)
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

                    var yPositions = ComputeTransverseYPositions(beamWidth, coverStirrup, stirrupDia, dia, count);

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

        // 5. Span Bottom Additional Bars (Midspan Positive Moment)
        double botContDia = !spec.BottomContinuous.IsEmpty ? spec.BottomContinuous.Diameter : 20.0;
        double zBotCont = zBot + coverStirrup + stirrupDia + (botContDia / 2.0);

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

                    var yPositions = ComputeTransverseYPositions(beamWidth, coverStirrup, stirrupDia, dia, count);

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

            double zBotMain = zBot + coverStirrup + stirrupDia + (botContDia / 2.0);
            double zTopMain = zTop - coverStirrup - stirrupDia - (topContDia / 2.0);
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

        // 7. 3-Zone Stirrups Distribution (Dense L/4, Sparse L/2, Dense L/4)
        double outToOutWidth = Math.Max(0.0, beamWidth - (2.0 * coverStirrup));
        double outToOutHeight = Math.Max(0.0, beamHeight - (2.0 * coverStirrup));

        double yMinStirrup = -(beamWidth / 2.0) + coverStirrup + (stirrupDia / 2.0);
        double yMaxStirrup = +(beamWidth / 2.0) - coverStirrup - (stirrupDia / 2.0);
        double zTopStirrup = zTop - coverStirrup - (stirrupDia / 2.0);
        double zBotStirrup = zBot + coverStirrup + (stirrupDia / 2.0);

        for (int s = 0; s < spanCount; s++)
        {
            var span = spec.Spans[s];
            double ln = span.Length;
            if (ln <= 0.0)
                continue;

            var stSpec = span.StirrupOverride ?? spec.GlobalStirrup;
            double sDense = stSpec.SupportSpacing > 0.0 ? stSpec.SupportSpacing : 150.0;
            double sSparse = stSpec.MidspanSpacing > 0.0 ? stSpec.MidspanSpacing : 200.0;
            double sEnd = stSpec.EndSupportSpacing ?? sDense;
            double sCant = stSpec.CantileverSpacing > 0.0 ? stSpec.CantileverSpacing : 150.0;

            bool isCant = (s == 0 && isLeftCantilever) || (s == spanCount - 1 && isRightCantilever);

            var branches = stSpec.Branches.Count > 0
                ? stSpec.Branches
                : new[] { new KataStirrupBranchSpec(KataStirrupShapeType.ClosedHoop, "Outer") };

            if (isCant)
            {
                // Cantilever: uniform spacing
                double lDist = ln - DefaultStartOffsetMm - coverStirrup;
                var cantStations = new List<double>();
                if (lDist > 0.0)
                {
                    int intervals = (int)Math.Floor(lDist / sCant);
                    int count = intervals + 1;
                    double startX = (s == 0 && isLeftCantilever)
                        ? spanStart[s] + coverStirrup
                        : spanStart[s] + DefaultStartOffsetMm;

                    for (int i = 0; i < count; i++)
                    {
                        cantStations.Add(startX + (i * sCant));
                    }
                }

                foreach (var branch in branches)
                {
                    stirrupZones.Add(new KataStirrupZoneResult
                    {
                        SpanIndex = s,
                        ZoneIndex = 0,
                        ZoneName = "Console",
                        StartStationX = cantStations.Count > 0 ? cantStations[0] : spanStart[s],
                        EndStationX = cantStations.Count > 0 ? cantStations[cantStations.Count - 1] : spanEnd[s],
                        Spacing = sCant,
                        Count = cantStations.Count,
                        Stations = cantStations,
                        OutToOutWidth = outToOutWidth,
                        OutToOutHeight = outToOutHeight,
                        StirrupType = branch.ShapeType,
                        BarMark = branch.ShapeType switch
                        {
                            KataStirrupShapeType.ClosedHoop => "d1",
                            KataStirrupShapeType.CapStirrup => "d2",
                            KataStirrupShapeType.CrossTie => "d3",
                            _ => "d1"
                        }
                    });

                    // Generate individual curves for this branch
                    foreach (double x in cantStations)
                    {
                        var curve = CreateStirrupCurve(branch.ShapeType, x, yMinStirrup, yMaxStirrup, zTopStirrup, zBotStirrup, stirrupDia, barId++, s);
                        individualStirrups.Add(curve);
                    }
                }
            }
            else
            {
                // Standard 3-Zone: Zone 1 (Left Dense), Zone 3 (Right Dense), Zone 2 (Midspan Sparse)
                double l1 = ln / 4.0;
                double l3 = ln / 4.0;

                // Zone 1
                double lDist1 = Math.Max(0.0, l1 - DefaultStartOffsetMm);
                int intervals1 = (int)Math.Floor(lDist1 / sDense);
                int count1 = intervals1 + 1;
                var stations1 = new List<double>(count1);
                for (int i = 0; i < count1; i++)
                {
                    stations1.Add(spanStart[s] + DefaultStartOffsetMm + (i * sDense));
                }

                // Zone 3
                double lDist3 = Math.Max(0.0, l3 - DefaultStartOffsetMm);
                int intervals3 = (int)Math.Floor(lDist3 / sEnd);
                int count3 = intervals3 + 1;
                double lastX3 = spanEnd[s] - DefaultStartOffsetMm;
                double firstX3 = lastX3 - (intervals3 * sEnd);
                var stations3 = new List<double>(count3);
                for (int i = 0; i < count3; i++)
                {
                    stations3.Add(firstX3 + (i * sEnd));
                }

                // Zone 2 (Symmetrically spaced within the physical gap)
                double lastX1 = stations1[stations1.Count - 1];
                double gap = firstX3 - lastX1;
                var stations2 = new List<double>();

                if (gap > 2.0 * Math.Min(sDense, sSparse))
                {
                    double y2 = (gap / sSparse) - 2.0;
                    int intervals2 = (int)Math.Ceiling(y2 - 1e-9);
                    if (intervals2 < 0) intervals2 = 0;
                    int count2 = intervals2 + 1;

                    double delta2 = (gap - (intervals2 * sSparse)) / 2.0;
                    double startX2 = lastX1 + delta2;
                    for (int i = 0; i < count2; i++)
                    {
                        stations2.Add(startX2 + (i * sSparse));
                    }
                }

                foreach (var branch in branches)
                {
                    // Zone 1
                    stirrupZones.Add(new KataStirrupZoneResult
                    {
                        SpanIndex = s,
                        ZoneIndex = 0,
                        ZoneName = "Gối trái",
                        StartStationX = stations1[0],
                        EndStationX = stations1[stations1.Count - 1],
                        Spacing = sDense,
                        Count = stations1.Count,
                        Stations = stations1,
                        OutToOutWidth = outToOutWidth,
                        OutToOutHeight = outToOutHeight,
                        StirrupType = branch.ShapeType,
                        BarMark = branch.ShapeType switch
                        {
                            KataStirrupShapeType.ClosedHoop => "d1",
                            KataStirrupShapeType.CapStirrup => "d2",
                            KataStirrupShapeType.CrossTie => "d3",
                            _ => "d1"
                        }
                    });

                    // Zone 2
                    if (stations2.Count > 0)
                    {
                        stirrupZones.Add(new KataStirrupZoneResult
                        {
                            SpanIndex = s,
                            ZoneIndex = 1,
                            ZoneName = "Giữa nhịp",
                            StartStationX = stations2[0],
                            EndStationX = stations2[stations2.Count - 1],
                            Spacing = sSparse,
                            Count = stations2.Count,
                            Stations = stations2,
                            OutToOutWidth = outToOutWidth,
                            OutToOutHeight = outToOutHeight,
                            StirrupType = branch.ShapeType,
                            BarMark = branch.ShapeType switch
                            {
                                KataStirrupShapeType.ClosedHoop => "d1",
                                KataStirrupShapeType.CapStirrup => "d2",
                                KataStirrupShapeType.CrossTie => "d3",
                                _ => "d1"
                            }
                        });
                    }

                    // Zone 3
                    stirrupZones.Add(new KataStirrupZoneResult
                    {
                        SpanIndex = s,
                        ZoneIndex = 2,
                        ZoneName = "Gối phải",
                        StartStationX = stations3[0],
                        EndStationX = stations3[stations3.Count - 1],
                        Spacing = sEnd,
                        Count = stations3.Count,
                        Stations = stations3,
                        OutToOutWidth = outToOutWidth,
                        OutToOutHeight = outToOutHeight,
                        StirrupType = branch.ShapeType,
                        BarMark = branch.ShapeType switch
                        {
                            KataStirrupShapeType.ClosedHoop => "d1",
                            KataStirrupShapeType.CapStirrup => "d2",
                            KataStirrupShapeType.CrossTie => "d3",
                            _ => "d1"
                        }
                    });

                    // Generate curves
                    foreach (double x in stations1)
                        individualStirrups.Add(CreateStirrupCurve(branch.ShapeType, x, yMinStirrup, yMaxStirrup, zTopStirrup, zBotStirrup, stirrupDia, barId++, s));

                    foreach (double x in stations2)
                        individualStirrups.Add(CreateStirrupCurve(branch.ShapeType, x, yMinStirrup, yMaxStirrup, zTopStirrup, zBotStirrup, stirrupDia, barId++, s));

                    foreach (double x in stations3)
                        individualStirrups.Add(CreateStirrupCurve(branch.ShapeType, x, yMinStirrup, yMaxStirrup, zTopStirrup, zBotStirrup, stirrupDia, barId++, s));
                }
            }
        }

        // 8. Total Steel Weight Calculation
        double totalWeightKg = 0.0;
        Action<IReadOnlyList<KataRebarCurve>> sumWeight = list =>
        {
            foreach (var bar in list)
            {
                double lengthM = bar.Polyline.TotalLength / 1000.0;
                double radiusM = (bar.Diameter / 2.0) / 1000.0;
                double volumeM3 = Math.PI * radiusM * radiusM * lengthM;
                totalWeightKg += volumeM3 * SteelDensityKgPerM3;
            }
        };

        sumWeight(mainTopBars);
        sumWeight(mainBottomBars);
        sumWeight(extraTopBars);
        sumWeight(extraBottomBars);
        sumWeight(sideBars);
        sumWeight(individualStirrups);

        return new KataRebarLayoutResult
        {
            BeamName = spec.BeamName,
            MainTopBars = mainTopBars,
            MainBottomBars = mainBottomBars,
            ExtraTopBars = extraTopBars,
            ExtraBottomBars = extraBottomBars,
            SideBars = sideBars,
            StirrupZones = stirrupZones,
            IndividualStirrups = individualStirrups,
            Warnings = warnings,
            TotalSteelWeightKg = Math.Round(totalWeightKg, 2)
        };
    }

    private static KataRebarCurve CreateStirrupCurve(
        KataStirrupShapeType shape,
        double x,
        double yMin,
        double yMax,
        double zTop,
        double zBot,
        double dia,
        int id,
        int spanIndex)
    {
        double outWidth = Math.Abs(yMax - yMin) + dia;
        double outHeight = Math.Abs(zTop - zBot) + dia;

        switch (shape)
        {
            case KataStirrupShapeType.CapStirrup:
            {
                var pts = new List<Point3>
                {
                    new(x, yMin, zTop),
                    new(x, yMin, zBot),
                    new(x, yMax, zBot),
                    new(x, yMax, zTop)
                };
                return new KataRebarCurve
                {
                    BarId = id,
                    Role = KataBarRole.StirrupCap,
                    Diameter = dia,
                    Polyline = new Polyline3(pts, isClosed: false).Simplify(1.0),
                    TransverseY = 0.0,
                    HostSpanIndex = spanIndex,
                    ShapeCode = "45",
                    BarMark = "d2",
                    BarDescription = "Đai nắp chữ U",
                    DimA = outWidth,
                    DimB = outHeight,
                    DimC = 0.0,
                    DimR = 2.0 * dia,
                    SttCad = 7
                };
            }
            case KataStirrupShapeType.CrossTie:
            {
                double zMid = (zTop + zBot) / 2.0;
                var pts = new List<Point3>
                {
                    new(x, yMin, zMid),
                    new(x, yMax, zMid)
                };
                return new KataRebarCurve
                {
                    BarId = id,
                    Role = KataBarRole.CrossTie,
                    Diameter = dia,
                    Polyline = new Polyline3(pts, isClosed: false).Simplify(1.0),
                    TransverseY = 0.0,
                    HostSpanIndex = spanIndex,
                    ShapeCode = "24a",
                    BarMark = "d3",
                    BarDescription = "Đai C / móc đan",
                    DimA = outWidth,
                    DimB = 10.0 * dia,
                    DimC = 10.0 * dia,
                    DimR = 2.0 * dia,
                    SttCad = 8
                };
            }
            default: // ClosedHoop
            {
                var pts = new List<Point3>
                {
                    new(x, yMin, zTop),
                    new(x, yMax, zTop),
                    new(x, yMax, zBot),
                    new(x, yMin, zBot),
                    new(x, yMin, zTop)
                };
                return new KataRebarCurve
                {
                    BarId = id,
                    Role = KataBarRole.StirrupClosed,
                    Diameter = dia,
                    Polyline = new Polyline3(pts, isClosed: true).Simplify(1.0),
                    TransverseY = 0.0,
                    HostSpanIndex = spanIndex,
                    ShapeCode = "41",
                    BarMark = "d1",
                    BarDescription = "Đai chữ nhật kín",
                    DimA = outWidth,
                    DimB = outHeight,
                    DimC = 0.0,
                    DimR = 2.0 * dia,
                    SttCad = 6
                };
            }
        }
    }
}
