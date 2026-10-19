namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///     Pure C# service to generate all secondary, perimeter, corridor, wing, and diagonal beams.
///     Uses collinear line merging and greedy best-pair matching to eliminate duplicate and missing beams.
/// </summary>
public static class Story1SecondaryBeamService
{
    public static List<BeamSegmentModel> GenerateAllSecondaryBeams(
        GridSystem grids,
        (List<CadLineItem> xLines, List<CadLineItem> yLines, List<CadDiagLineItem> diagLines) lines,
        List<CadTextItem> texts,
        ref int index)
    {
        var result = new List<BeamSegmentModel>();

        // 1. Off-grid and perimeter X beams (North corridor, South entrance, WC, interior secondary)
        var offGridX = ProcessOffGridXBeams(grids, lines.xLines, texts, ref index);
        result.AddRange(offGridX);

        // 2. Off-grid and perimeter Y beams (West wing DK23-Y, East wing DK.a', intermediate Y)
        var offGridY = ProcessOffGridYBeams(grids, lines.yLines, texts, ref index);
        result.AddRange(offGridY);

        // 3. Diagonal beams (DX-1 entrance chamfers)
        var diagBeams = ProcessDiagonalBeams(lines.diagLines, texts, ref index);
        result.AddRange(diagBeams);

        return result;
    }

    private static List<BeamSegmentModel> ProcessOffGridXBeams(
        GridSystem grids, List<CadLineItem> xLines, List<CadTextItem> texts, ref int index)
    {
        var result = new List<BeamSegmentModel>();
        var offLines = xLines.Where(l => !grids.YGrids.Any(g => Math.Abs(g.Coord - l.Pos) <= 80.0)).ToList();
        var merged = MergeCollinear(offLines);
        var used = new HashSet<int>();

        for (int i = 0; i < merged.Count; i++)
        {
            if (used.Contains(i)) continue;
            var l1 = merged[i];

            int bestJ = -1;
            double maxOverlap = 0.0;
            double bestDist = 0.0;

            for (int j = i + 1; j < merged.Count; j++)
            {
                if (used.Contains(j)) continue;
                var l2 = merged[j];
                double dist = Math.Abs(l1.Pos - l2.Pos);
                if (dist < 80.0 || dist > 520.0) continue;

                double s = Math.Max(l1.Start, l2.Start);
                double e = Math.Min(l1.End, l2.End);
                double overlap = e - s;

                if (overlap >= 400.0 && overlap > maxOverlap)
                {
                    maxOverlap = overlap;
                    bestJ = j;
                    bestDist = dist;
                }
            }

            if (bestJ >= 0)
            {
                used.Add(i);
                used.Add(bestJ);
                var l2 = merged[bestJ];
                double s = Math.Max(l1.Start, l2.Start);
                double e = Math.Min(l1.End, l2.End);
                double midY = Math.Round((l1.Pos + l2.Pos) / 2.0, 1);
                int w = bestDist <= 150 ? 100 : bestDist <= 250 ? 200 : bestDist <= 350 ? 300 : bestDist <= 450 ? 400 : 500;

                var (snapS, snapE) = SnapXEndpoints(s, e, grids);
                var (name, secW, secH) = ResolveSecBeamInfo(texts, (snapS + snapE) / 2.0, midY, w);

                index++;
                result.Add(CreateSecBeam(index, name, $"B_{secW}x{secH}", "X", secW, secH,
                    snapS, midY, snapE, midY, snapE - snapS, $"{name}_SEC_X_{index:D3}"));
            }
        }

        return result;
    }

    private static List<BeamSegmentModel> ProcessOffGridYBeams(
        GridSystem grids, List<CadLineItem> yLines, List<CadTextItem> texts, ref int index)
    {
        var result = new List<BeamSegmentModel>();
        var offLines = yLines.Where(l => !grids.XGrids.Any(g => Math.Abs(g.Coord - l.Pos) <= 80.0)).ToList();
        var merged = MergeCollinear(offLines);
        var used = new HashSet<int>();

        for (int i = 0; i < merged.Count; i++)
        {
            if (used.Contains(i)) continue;
            var l1 = merged[i];

            int bestJ = -1;
            double maxOverlap = 0.0;
            double bestDist = 0.0;

            for (int j = i + 1; j < merged.Count; j++)
            {
                if (used.Contains(j)) continue;
                var l2 = merged[j];
                double dist = Math.Abs(l1.Pos - l2.Pos);
                if (dist < 80.0 || dist > 520.0) continue;

                double s = Math.Max(l1.Start, l2.Start);
                double e = Math.Min(l1.End, l2.End);
                double overlap = e - s;

                if (overlap >= 400.0 && overlap > maxOverlap)
                {
                    maxOverlap = overlap;
                    bestJ = j;
                    bestDist = dist;
                }
            }

            if (bestJ >= 0)
            {
                used.Add(i);
                used.Add(bestJ);
                var l2 = merged[bestJ];
                double s = Math.Max(l1.Start, l2.Start);
                double e = Math.Min(l1.End, l2.End);
                double midX = Math.Round((l1.Pos + l2.Pos) / 2.0, 1);
                int w = bestDist <= 150 ? 100 : bestDist <= 250 ? 200 : bestDist <= 350 ? 300 : bestDist <= 450 ? 400 : 500;

                var (snapS, snapE) = SnapYEndpoints(s, e, grids);
                var (name, secW, secH) = ResolveSecBeamInfo(texts, midX, (snapS + snapE) / 2.0, w);

                index++;
                result.Add(CreateSecBeam(index, name, $"B_{secW}x{secH}", "Y", secW, secH,
                    midX, snapS, midX, snapE, snapE - snapS, $"{name}_SEC_Y_{index:D3}"));
            }
        }

        return result;
    }

    private static List<CadLineItem> MergeCollinear(List<CadLineItem> lines)
    {
        var result = new List<CadLineItem>();
        var groups = lines.GroupBy(l => Math.Round(l.Pos / 25.0) * 25.0);

        foreach (var grp in groups)
        {
            double pos = grp.Key;
            var ordered = grp.OrderBy(l => l.Start).ToList();
            if (ordered.Count == 0) continue;

            double curS = ordered[0].Start;
            double curE = ordered[0].End;
            string handle = ordered[0].Handle;

            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].Start <= curE + 100.0)
                {
                    curE = Math.Max(curE, ordered[i].End);
                }
                else
                {
                    if (curE - curS >= 300.0)
                    {
                        result.Add(new CadLineItem(handle, pos, curS, curE, curE - curS));
                    }
                    curS = ordered[i].Start;
                    curE = ordered[i].End;
                    handle = ordered[i].Handle;
                }
            }
            if (curE - curS >= 300.0)
            {
                result.Add(new CadLineItem(handle, pos, curS, curE, curE - curS));
            }
        }
        return result;
    }

    private static List<BeamSegmentModel> ProcessDiagonalBeams(
        List<CadDiagLineItem> diagLines, List<CadTextItem> texts, ref int index)
    {
        var result = new List<BeamSegmentModel>();
        var processed = new HashSet<int>();

        for (int i = 0; i < diagLines.Count; i++)
        {
            if (processed.Contains(i)) continue;
            var d1 = diagLines[i];

            for (int j = i + 1; j < diagLines.Count; j++)
            {
                if (processed.Contains(j)) continue;
                var d2 = diagLines[j];

                double midX1 = (d1.X1 + d2.X1) / 2.0;
                double midY1 = (d1.Y1 + d2.Y1) / 2.0;
                double midX2 = (d1.X2 + d2.X2) / 2.0;
                double midY2 = (d1.Y2 + d2.Y2) / 2.0;
                double len = Math.Sqrt(Math.Pow(midX2 - midX1, 2) + Math.Pow(midY2 - midY1, 2));

                if (len >= 250.0 && Math.Abs(d1.Len - d2.Len) <= 200.0)
                {
                    processed.Add(i);
                    processed.Add(j);
                    index++;

                    result.Add(CreateSecBeam(index, "DX-1", "B_200x300", "Diag", 200, 300,
                        midX1, midY1, midX2, midY2, len, $"DX-1_{index:D3}"));
                    break;
                }
            }
        }

        return result;
    }

    private static (double S, double E) SnapXEndpoints(double s, double e, GridSystem grids)
    {
        var nearS = grids.XGrids.FirstOrDefault(g => Math.Abs(g.Coord - s) <= 350.0);
        var nearE = grids.XGrids.FirstOrDefault(g => Math.Abs(g.Coord - e) <= 350.0);
        double snapS = nearS != null ? nearS.Coord : s;
        double snapE = nearE != null ? nearE.Coord : e;
        return snapS < snapE ? (snapS, snapE) : (s, e);
    }

    private static (double S, double E) SnapYEndpoints(double s, double e, GridSystem grids)
    {
        var nearS = grids.YGrids.FirstOrDefault(g => Math.Abs(g.Coord - s) <= 350.0);
        var nearE = grids.YGrids.FirstOrDefault(g => Math.Abs(g.Coord - e) <= 350.0);
        double snapS = nearS != null ? nearS.Coord : s;
        double snapE = nearE != null ? nearE.Coord : e;
        return snapS < snapE ? (snapS, snapE) : (s, e);
    }

    private static (string Name, int W, int H) ResolveSecBeamInfo(
        List<CadTextItem> texts, double x, double y, int measuredW)
    {
        var nearby = texts.Where(t => Math.Sqrt(Math.Pow(t.Rx - x, 2) + Math.Pow(t.Ry - y, 2)) <= 1500.0)
                          .OrderBy(t => Math.Sqrt(Math.Pow(t.Rx - x, 2) + Math.Pow(t.Ry - y, 2))).ToList();

        string name = "DK_PHU";
        int w = measuredW;
        int h = measuredW == 100 ? 300 : 400;

        foreach (var t in nearby)
        {
            if (t.Text.Contains("200x300")) { w = 200; h = 300; }
            else if (t.Text.Contains("200x400")) { w = 200; h = 400; }
            else if (t.Text.Contains("200x500")) { w = 200; h = 500; }
            else if (t.Text.Contains("300x400")) { w = 300; h = 400; }
            else if (t.Text.Contains("100x300")) { w = 100; h = 300; }

            if (t.Text.StartsWith("DK", StringComparison.OrdinalIgnoreCase) ||
                t.Text.StartsWith("DX", StringComparison.OrdinalIgnoreCase) ||
                t.Text.StartsWith("BT", StringComparison.OrdinalIgnoreCase))
            {
                name = t.Text;
                if (PrepareStory1GradeBeamsModelCommand.StandardSections.TryGetValue(name, out var sec))
                {
                    w = sec.W;
                    h = sec.H;
                }
            }
        }

        return (name, w, h);
    }

    private static BeamSegmentModel CreateSecBeam(
        int idx, string name, string typeName, string axis, double w, double h,
        double sX, double sY, double eX, double eY, double len, string mark) => new()
    {
        Index = idx,
        Name = name,
        Mark = mark,
        TypeName = typeName,
        Axis = axis,
        WidthMm = w,
        HeightMm = h,
        StartX = sX,
        StartY = sY,
        EndX = eX,
        EndY = eY,
        LengthMm = Math.Round(len, 1),
        YJustification = 2,
        YJustificationName = "Origin",
        YOffsetMm = 0.0,
        ZJustification = 0,
        ZJustificationName = "Top",
        ZOffsetMm = 0.0,
        LevelName = "Tầng 1",
        TopElevationMm = -100.0,
        IsPerimeter = false,
        IsSecondary = true
    };
}
