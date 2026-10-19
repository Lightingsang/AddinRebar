namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
///     Pure C# service to generate beam spans from Support-to-Support (Column-to-Column, Column-to-Wall).
///     Guarantees 100% collinear alignment along each axis -- zero jagged stepping.
/// </summary>
public static class Story1BeamTopologyService
{
    public static List<BeamSegmentModel> GenerateXBeams(
        List<SupportNode> allSupports,
        GridSystem grids,
        List<CadLineItem> xLines,
        List<CadLineItem> yLines,
        List<CadTextItem> texts,
        ref int index)
    {
        var result = new List<BeamSegmentModel>();
        var sortedXGrids = grids.XGrids.OrderBy(g => g.Coord).ToList();

        foreach (var yGrid in grids.YGrids)
        {
            double gy = yGrid.Coord;
            var matchingLines = xLines.Where(l => Math.Abs(l.Pos - gy) <= 350.0).ToList();
            if (matchingLines.Count == 0) continue;

            var validX = FindValidSupportsOnY(gy, sortedXGrids, allSupports, yLines);
            if (validX.Count < 2) continue;

            var axisRule = Story1BeamAlignmentHelper.ResolveXAxisRule(gy, matchingLines);

            for (int i = 0; i < validX.Count - 1; i++)
            {
                var gx1 = validX[i];
                var gx2 = validX[i + 1];
                double xLeft = Math.Min(gx1.Coord, gx2.Coord);
                double xRight = Math.Max(gx1.Coord, gx2.Coord);
                double spanLength = xRight - xLeft;

                if (spanLength < 300.0) continue;

                var spanLines = matchingLines.Where(l =>
                    Math.Max(l.Start, xLeft) < Math.Min(l.End, xRight) - 200.0).ToList();

                if (!HasContinuousCadCoverage(spanLines, xLeft, xRight, spanLength))
                    continue;

                var beam = CreateXBeam(spanLines, gx1, gx2, xLeft, xRight, gy, axisRule, texts, ref index);
                result.Add(beam);
            }
        }

        return result;
    }

    public static List<BeamSegmentModel> GenerateYBeams(
        List<SupportNode> allSupports,
        GridSystem grids,
        List<CadLineItem> yLines,
        List<CadLineItem> xLines,
        List<CadTextItem> texts,
        ref int index)
    {
        var result = new List<BeamSegmentModel>();
        var sortedYGrids = grids.YGrids.OrderBy(g => g.Coord).ToList();

        foreach (var xGrid in grids.XGrids)
        {
            double gx = xGrid.Coord;
            var matchingLines = yLines.Where(l => Math.Abs(l.Pos - gx) <= 350.0).ToList();
            if (matchingLines.Count == 0) continue;

            var validY = FindValidSupportsOnX(gx, sortedYGrids, allSupports, xLines);
            if (validY.Count < 2) continue;

            var axisRule = Story1BeamAlignmentHelper.ResolveYAxisRule(gx, matchingLines);

            for (int j = 0; j < validY.Count - 1; j++)
            {
                var gy1 = validY[j];
                var gy2 = validY[j + 1];
                double yBot = Math.Min(gy1.Coord, gy2.Coord);
                double yTop = Math.Max(gy1.Coord, gy2.Coord);
                double spanLength = yTop - yBot;

                if (spanLength < 300.0) continue;

                var spanLines = matchingLines.Where(l =>
                    Math.Max(l.Start, yBot) < Math.Min(l.End, yTop) - 200.0).ToList();

                if (!HasContinuousCadCoverage(spanLines, yBot, yTop, spanLength))
                    continue;

                var beam = CreateYBeam(spanLines, gy1, gy2, gx, yBot, yTop, axisRule, texts, ref index);
                result.Add(beam);
            }
        }

        return result;
    }

    private static List<GridItem> FindValidSupportsOnY(
        double gy, List<GridItem> sortedXGrids, List<SupportNode> supports, List<CadLineItem> yLines)
    {
        var valid = new List<GridItem>();
        foreach (var gx in sortedXGrids)
        {
            bool hasCol = supports.Any(s => s.Type == "Column" &&
                Math.Abs(s.X - gx.Coord) <= 400.0 && Math.Abs(s.Y - gy) <= 400.0);
            bool hasWall = supports.Any(s => s.Type == "Wall" &&
                Math.Abs(s.X - gx.Coord) <= 300.0 && Math.Abs(s.Y - gy) <= 300.0);
            bool hasTransverse = yLines.Any(l =>
                Math.Abs(l.Pos - gx.Coord) <= 350.0 && l.Start <= gy + 400.0 && l.End >= gy - 400.0);

            if (hasCol || hasWall || hasTransverse)
                valid.Add(gx);
        }
        return valid;
    }

    private static List<GridItem> FindValidSupportsOnX(
        double gx, List<GridItem> sortedYGrids, List<SupportNode> supports, List<CadLineItem> xLines)
    {
        var valid = new List<GridItem>();
        foreach (var gy in sortedYGrids)
        {
            bool hasCol = supports.Any(s => s.Type == "Column" &&
                Math.Abs(s.X - gx) <= 400.0 && Math.Abs(s.Y - gy.Coord) <= 400.0);
            bool hasWall = supports.Any(s => s.Type == "Wall" &&
                Math.Abs(s.X - gx) <= 300.0 && Math.Abs(s.Y - gy.Coord) <= 300.0);
            bool hasTransverse = xLines.Any(l =>
                Math.Abs(l.Pos - gy.Coord) <= 350.0 && l.Start <= gx + 400.0 && l.End >= gx - 400.0);

            if (hasCol || hasWall || hasTransverse)
                valid.Add(gy);
        }
        return valid;
    }

    private static bool HasContinuousCadCoverage(
        List<CadLineItem> lines, double start, double end, double spanLength)
    {
        if (lines.Count < 2) return false;

        bool startsNear = lines.Any(l => l.Start <= start + 650.0);
        bool endsNear = lines.Any(l => l.End >= end - 650.0);
        if (!startsNear || !endsNear) return false;

        double covered = CalculateCoveredLength(lines, start, end);
        return covered >= spanLength * 0.60;
    }

    private static double CalculateCoveredLength(List<CadLineItem> lines, double minVal, double maxVal)
    {
        var ivs = lines.Select(l => (S: Math.Max(l.Start, minVal), E: Math.Min(l.End, maxVal)))
                       .Where(iv => iv.E > iv.S).OrderBy(iv => iv.S).ToList();
        if (ivs.Count == 0) return 0.0;
        double total = 0.0, curS = ivs[0].S, curE = ivs[0].E;
        for (int i = 1; i < ivs.Count; i++)
        {
            if (ivs[i].S <= curE + 100.0) curE = Math.Max(curE, ivs[i].E);
            else { total += curE - curS; curS = ivs[i].S; curE = ivs[i].E; }
        }
        return total + (curE - curS);
    }

    private static (double center, int width) FindBestParallelCenter(List<CadLineItem> lines, double fallback)
    {
        double bestCenter = fallback, bestDiff = double.MaxValue;
        int bestW = 200;
        int[] stdW = [100, 200, 300, 400, 500];
        for (int i = 0; i < lines.Count; i++)
        {
            for (int j = i + 1; j < lines.Count; j++)
            {
                double d = Math.Abs(lines[i].Pos - lines[j].Pos);
                foreach (var sw in stdW)
                {
                    double diff = Math.Abs(d - sw);
                    if (diff <= 30.0 && diff < bestDiff)
                    {
                        bestDiff = diff; bestW = sw;
                        bestCenter = Math.Round((lines[i].Pos + lines[j].Pos) / 2.0, 1);
                    }
                }
            }
        }
        return (bestCenter, bestW);
    }

    private static BeamSegmentModel CreateXBeam(
        List<CadLineItem> spanLines, GridItem gx1, GridItem gx2,
        double xLeft, double xRight, double gy, AxisAlignmentRule rule, List<CadTextItem> texts, ref int index)
    {
        var (yMid, detectedW) = FindBestParallelCenter(spanLines, gy);
        var (name, w, h) = ResolveBeamInfo(texts, (xLeft + xRight) / 2.0, yMid, detectedW);
        if (w == 0 || h == 0) { w = detectedW; h = 400; }
        double sX = rule.InvertDirection ? xRight : xLeft, eX = rule.InvertDirection ? xLeft : xRight;
        index++;
        return new BeamSegmentModel
        {
            Index = index, Name = name, Mark = $"{name}_{gx1.Name}-{gx2.Name}", TypeName = $"B_{w}x{h}", Axis = "X",
            WidthMm = w, HeightMm = h, StartX = sX, StartY = yMid, EndX = eX, EndY = yMid,
            LengthMm = xRight - xLeft, YJustification = 2, YJustificationName = "Origin", YOffsetMm = 0.0,
            ZJustification = 0, ZJustificationName = "Top", ZOffsetMm = 0.0, LevelName = "Tầng 1", TopElevationMm = -100.0
        };
    }

    private static BeamSegmentModel CreateYBeam(
        List<CadLineItem> spanLines, GridItem gy1, GridItem gy2,
        double gx, double yBot, double yTop, AxisAlignmentRule rule, List<CadTextItem> texts, ref int index)
    {
        var (xMid, detectedW) = FindBestParallelCenter(spanLines, gx);
        var (name, w, h) = ResolveBeamInfo(texts, xMid, (yBot + yTop) / 2.0, detectedW);
        if (w == 0 || h == 0) { w = detectedW; h = 400; }
        double sY = rule.InvertDirection ? yTop : yBot, eY = rule.InvertDirection ? yBot : yTop;
        index++;
        return new BeamSegmentModel
        {
            Index = index, Name = name, Mark = $"{name}_{gy1.Name}-{gy2.Name}", TypeName = $"B_{w}x{h}", Axis = "Y",
            WidthMm = w, HeightMm = h, StartX = xMid, StartY = sY, EndX = xMid, EndY = eY,
            LengthMm = yTop - yBot, YJustification = 2, YJustificationName = "Origin", YOffsetMm = 0.0,
            ZJustification = 0, ZJustificationName = "Top", ZOffsetMm = 0.0, LevelName = "Tầng 1", TopElevationMm = -100.0
        };
    }

    private static (string name, int w, int h) ResolveBeamInfo(List<CadTextItem> texts, double x, double y, double measuredW)
    {
        string bestName = "DK";
        int w = 0, h = 0;
        double minDist = double.MaxValue;

        foreach (var t in texts)
        {
            double dist = Math.Sqrt(Math.Pow(t.Rx - x, 2) + Math.Pow(t.Ry - y, 2));
            if (dist < minDist && dist <= 2500.0)
            {
                var matchDim = Regex.Match(t.Text, @"\(?(\d{3})x(\d{3})\)?");
                if (matchDim.Success)
                {
                    w = int.Parse(matchDim.Groups[1].Value);
                    h = int.Parse(matchDim.Groups[2].Value);
                    bestName = t.Text;
                    minDist = dist;
                }
                else if (PrepareStory1GradeBeamsModelCommand.StandardSections.TryGetValue(t.Text, out var sec))
                {
                    bestName = t.Text;
                    w = sec.W;
                    h = sec.H;
                    minDist = dist;
                }
            }
        }
        if (w == 0 && PrepareStory1GradeBeamsModelCommand.StandardSections.TryGetValue(bestName, out var s))
        {
            w = s.W;
            h = s.H;
        }
        return (bestName, w, h);
    }
}
