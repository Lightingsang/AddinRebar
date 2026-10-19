namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;

/// <summary>
///     Audits every single span between columns on CAD to classify:
///     1. REAL MAIN BEAMS (have clear parallel lines on CAD)
///     2. PHANTOM BEAMS (open space on CAD, no beam lines -> must omit)
///     3. REAL SECONDARY BEAMS (off-grid, corridor, toilet drop, elevator, skew)
/// </summary>
public sealed class Story1SpanAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public Story1SpanAuditService(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string surveyPath = args.GetOption("input", Path.Combine("scratch", "cad_da_kieng_survey_dossier.json"));
        string gridsPath = args.GetOption("grids", Path.Combine("scratch", "revit_grids_and_levels.json"));
        string colsPath = args.GetOption("columns", Path.Combine("scratch", "columns_story1_model.json"));
        string wallsPath = args.GetOption("walls", Path.Combine("scratch", "elevator_walls_model.json"));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_spans_classified.json"));
        string reportPath = args.GetOption("report", Path.Combine("scratch", "cad_spans_classified_report.md"));

        _formatter.WriteBanner("Span Audit & Classification", "Phân loại nhịp dầm thực tế vs Ô sàn mở (Loại trừ dầm ma)");

        using var surveyDoc = JsonDocument.Parse(await File.ReadAllTextAsync(surveyPath, ct).ConfigureAwait(false));
        var root = surveyDoc.RootElement;
        var segs = root.GetProperty("segments").EnumerateArray().ToList();
        var texts = root.GetProperty("texts").EnumerateArray().Select(ParseText).ToList();

        var grids = LoadGrids(gridsPath);
        var allSupports = Story1BeamSupportGraph.LoadAllSupports(colsPath, wallsPath, grids);

        var xLines = new List<Seg1D>();
        var yLines = new List<Seg1D>();
        var diagSegs = new List<SegDiag>();

        foreach (var s in segs)
        {
            var arr = s.EnumerateArray().Select(e => e.GetDouble()).ToArray();
            int type = (int)arr[0];
            if (type == 0) xLines.Add(new Seg1D(arr[1], arr[2], arr[3]));
            else if (type == 1) yLines.Add(new Seg1D(arr[1], arr[2], arr[3]));
            else diagSegs.Add(new SegDiag(arr[1], arr[2], arr[3], arr[4]));
        }

        var xMerged = MergeCollinear(xLines);
        var yMerged = MergeCollinear(yLines);

        // 1. Audit X Grid Spans (along Y grids A..N)
        var xSpanResults = AuditXGridSpans(grids, allSupports, xMerged, texts);
        // 2. Audit Y Grid Spans (along X grids 1..13)
        var ySpanResults = AuditYGridSpans(grids, allSupports, yMerged, texts);

        int confirmedX = xSpanResults.Count(s => s.HasBeam);
        int omittedX = xSpanResults.Count(s => !s.HasBeam);
        int confirmedY = ySpanResults.Count(s => s.HasBeam);
        int omittedY = ySpanResults.Count(s => !s.HasBeam);

        _formatter.WriteSuccess($"Phương X: {confirmedX} nhịp CÓ DẦM, {omittedX} nhịp Ô MỞ (Loại trừ dầm ma).");
        _formatter.WriteSuccess($"Phương Y: {confirmedY} nhịp CÓ DẦM, {omittedY} nhịp Ô MỞ (Loại trừ dầm ma).");

        // 3. Discover Off-grid Secondary & Perimeter Beams
        var offGridBeams = DiscoverOffGridBeams(grids, xMerged, yMerged, diagSegs, texts);
        _formatter.WriteSuccess($"Dầm phụ / Ngoại biên / Buồng thang / Vệ sinh: {offGridBeams.Count} dầm.");

        var finalModelBeams = AssembleFinalVerifiedBeams(xSpanResults, ySpanResults, offGridBeams);
        _formatter.WriteSuccess($"TỔNG CỘNG HỆ DẦM TẦNG 1 CHÍNH THỨC: {finalModelBeams.Count} dầm.");

        var resultObj = new
        {
            totalBeams = finalModelBeams.Count,
            totalMainX = confirmedX,
            totalMainY = confirmedY,
            totalOmittedSpans = omittedX + omittedY,
            totalSecondary = offGridBeams.Count,
            beams = finalModelBeams
        };

        File.WriteAllText(outputPath, JsonSerializer.Serialize(resultObj, JsonOptions));
        _formatter.WriteSuccess($"Đã lưu kết quả phân loại: {outputPath}");

        await GenerateReportAsync(xSpanResults, ySpanResults, offGridBeams, finalModelBeams, reportPath, ct).ConfigureAwait(false);
        _formatter.WriteSuccess($"Đã xuất Báo cáo Đối soát Mặt bằng chi tiết: {reportPath}");

        return 0;
    }

    private static List<SpanAuditItem> AuditXGridSpans(
        GridSystem grids, List<SupportNode> supports, List<Seg1D> xLines, List<CadTextItem> texts)
    {
        var result = new List<SpanAuditItem>();
        var sortedXGrids = grids.XGrids.OrderBy(g => g.Coord).ToList();

        foreach (var yg in grids.YGrids)
        {
            double gy = yg.Coord;
            var colsOnY = supports.Where(s => Math.Abs(s.Y - gy) <= 300.0).OrderBy(s => s.X).ToList();
            if (colsOnY.Count < 2) continue;

            for (int i = 0; i < colsOnY.Count - 1; i++)
            {
                var c1 = colsOnY[i];
                var c2 = colsOnY[i + 1];
                double span = c2.X - c1.X;
                if (span < 400.0) continue;

                double clearStart = c1.X + c1.WidthMm / 2.0;
                double clearEnd = c2.X - c2.WidthMm / 2.0;
                double clearLen = clearEnd - clearStart;

                var nearby = xLines.Where(l => Math.Abs(l.Pos - gy) <= 350.0 &&
                                              l.Start < clearEnd + 100.0 && l.End > clearStart - 100.0).ToList();

                var (hasPair, w, yMid) = CheckParallelCoverage(nearby, clearStart, clearEnd, clearLen);

                var (name, secW, secH) = MatchText(texts, (c1.X + c2.X) / 2.0, gy, w);
                if (secW == 0) secW = w > 0 ? w : 200;
                if (secH == 0) secH = 400;

                string g1 = c1.GridX.Length > 0 ? c1.GridX : $"{c1.X:F0}";
                string g2 = c2.GridX.Length > 0 ? c2.GridX : $"{c2.X:F0}";

                result.Add(new SpanAuditItem
                {
                    Axis = "X",
                    GridName = yg.Name,
                    GridCoord = gy,
                    SpanName = $"{g1}-{g2}",
                    Start = c1.X,
                    End = c2.X,
                    ClearLen = clearLen,
                    HasBeam = hasPair,
                    WidthMm = secW,
                    HeightMm = secH,
                    BeamName = name,
                    TypeName = $"B_{secW}x{secH}",
                    CoverageLines = nearby.Count
                });
            }
        }
        return result;
    }

    private static List<SpanAuditItem> AuditYGridSpans(
        GridSystem grids, List<SupportNode> supports, List<Seg1D> yLines, List<CadTextItem> texts)
    {
        var result = new List<SpanAuditItem>();
        var sortedYGrids = grids.YGrids.OrderBy(g => g.Coord).ToList();

        foreach (var xg in grids.XGrids)
        {
            double gx = xg.Coord;
            var colsOnX = supports.Where(s => Math.Abs(s.X - gx) <= 300.0).OrderBy(s => s.Y).ToList();
            if (colsOnX.Count < 2) continue;

            for (int j = 0; j < colsOnX.Count - 1; j++)
            {
                var c1 = colsOnX[j];
                var c2 = colsOnX[j + 1];
                double span = c2.Y - c1.Y;
                if (span < 400.0) continue;

                double clearStart = c1.Y + c1.HeightMm / 2.0;
                double clearEnd = c2.Y - c2.HeightMm / 2.0;
                double clearLen = clearEnd - clearStart;

                var nearby = yLines.Where(l => Math.Abs(l.Pos - gx) <= 350.0 &&
                                              l.Start < clearEnd + 100.0 && l.End > clearStart - 100.0).ToList();

                var (hasPair, w, xMid) = CheckParallelCoverage(nearby, clearStart, clearEnd, clearLen);

                var (name, secW, secH) = MatchText(texts, gx, (c1.Y + c2.Y) / 2.0, w);
                if (secW == 0) secW = w > 0 ? w : 200;
                if (secH == 0) secH = 400;

                string g1 = c1.GridY.Length > 0 ? c1.GridY : $"{c1.Y:F0}";
                string g2 = c2.GridY.Length > 0 ? c2.GridY : $"{c2.Y:F0}";

                result.Add(new SpanAuditItem
                {
                    Axis = "Y",
                    GridName = xg.Name,
                    GridCoord = gx,
                    SpanName = $"{g1}-{g2}",
                    Start = c1.Y,
                    End = c2.Y,
                    ClearLen = clearLen,
                    HasBeam = hasPair,
                    WidthMm = secW,
                    HeightMm = secH,
                    BeamName = name,
                    TypeName = $"B_{secW}x{secH}",
                    CoverageLines = nearby.Count
                });
            }
        }
        return result;
    }

    private static (bool HasPair, int Width, double MidPos) CheckParallelCoverage(
        List<Seg1D> lines, double clearStart, double clearEnd, double clearLen)
    {
        if (lines.Count < 2) return (false, 0, 0);

        for (int i = 0; i < lines.Count; i++)
        {
            for (int j = i + 1; j < lines.Count; j++)
            {
                var l1 = lines[i];
                var l2 = lines[j];
                double dist = Math.Abs(l1.Pos - l2.Pos);
                if (dist < 90.0 || dist > 520.0) continue;

                double s = Math.Max(Math.Max(l1.Start, l2.Start), clearStart);
                double e = Math.Min(Math.Min(l1.End, l2.End), clearEnd);
                double overlap = e - s;

                if (overlap >= clearLen * 0.40 || overlap >= 600.0)
                {
                    int w = dist <= 150 ? 100 : dist <= 250 ? 200 : dist <= 350 ? 300 : dist <= 450 ? 400 : 500;
                    return (true, w, (l1.Pos + l2.Pos) / 2.0);
                }
            }
        }
        return (false, 0, 0);
    }

    private static List<BeamSegmentModel> DiscoverOffGridBeams(
        GridSystem grids, List<Seg1D> xLines, List<Seg1D> yLines, List<SegDiag> diags, List<CadTextItem> texts)
    {
        var result = new List<BeamSegmentModel>();
        int idx = 1000;

        // Find parallel pairs off-grid
        var offX = xLines.Where(l => !grids.YGrids.Any(g => Math.Abs(g.Coord - l.Pos) <= 250.0)).ToList();
        var offY = yLines.Where(l => !grids.XGrids.Any(g => Math.Abs(g.Coord - l.Pos) <= 250.0)).ToList();

        // Off-grid X pairs
        for (int i = 0; i < offX.Count; i++)
        {
            for (int j = i + 1; j < offX.Count; j++)
            {
                var l1 = offX[i];
                var l2 = offX[j];
                double dist = Math.Abs(l1.Pos - l2.Pos);
                if (dist < 90.0 || dist > 520.0) continue;

                double s = Math.Max(l1.Start, l2.Start);
                double e = Math.Min(l1.End, l2.End);
                double overlap = e - s;

                if (overlap >= 500.0)
                {
                    double midY = (l1.Pos + l2.Pos) / 2.0;
                    bool dup = result.Any(b => b.Axis == "X" && Math.Abs(b.StartY - midY) <= 80.0 &&
                                              Math.Abs(b.StartX - s) <= 200.0 && Math.Abs(b.EndX - e) <= 200.0);
                    if (!dup)
                    {
                        idx++;
                        int measuredW = dist <= 150 ? 100 : dist <= 250 ? 200 : dist <= 350 ? 300 : dist <= 450 ? 400 : 500;
                        var (name, w, h) = MatchText(texts, (s + e) / 2.0, midY, measuredW);
                        if (w == 0) w = measuredW;
                        if (h == 0) h = 300;

                        result.Add(new BeamSegmentModel
                        {
                            Index = idx,
                            Name = name,
                            Mark = $"{name}_{idx:D4}",
                            TypeName = $"B_{w}x{h}",
                            Axis = "X",
                            WidthMm = w,
                            HeightMm = h,
                            StartX = s,
                            StartY = midY,
                            EndX = e,
                            EndY = midY,
                            LengthMm = overlap,
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
                        });
                    }
                }
            }
        }

        // Off-grid Y pairs
        for (int i = 0; i < offY.Count; i++)
        {
            for (int j = i + 1; j < offY.Count; j++)
            {
                var l1 = offY[i];
                var l2 = offY[j];
                double dist = Math.Abs(l1.Pos - l2.Pos);
                if (dist < 90.0 || dist > 520.0) continue;

                double s = Math.Max(l1.Start, l2.Start);
                double e = Math.Min(l1.End, l2.End);
                double overlap = e - s;

                if (overlap >= 500.0)
                {
                    double midX = (l1.Pos + l2.Pos) / 2.0;
                    bool dup = result.Any(b => b.Axis == "Y" && Math.Abs(b.StartX - midX) <= 80.0 &&
                                              Math.Abs(b.StartY - s) <= 200.0 && Math.Abs(b.EndY - e) <= 200.0);
                    if (!dup)
                    {
                        idx++;
                        int measuredW = dist <= 150 ? 100 : dist <= 250 ? 200 : dist <= 350 ? 300 : dist <= 450 ? 400 : 500;
                        var (name, w, h) = MatchText(texts, midX, (s + e) / 2.0, measuredW);
                        if (w == 0) w = measuredW;
                        if (h == 0) h = 300;

                        result.Add(new BeamSegmentModel
                        {
                            Index = idx,
                            Name = name,
                            Mark = $"{name}_{idx:D4}",
                            TypeName = $"B_{w}x{h}",
                            Axis = "Y",
                            WidthMm = w,
                            HeightMm = h,
                            StartX = midX,
                            StartY = s,
                            EndX = midX,
                            EndY = e,
                            LengthMm = overlap,
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
                        });
                    }
                }
            }
        }

        // Diagonal beams (DX-1)
        foreach (var d in diags)
        {
            double len = Math.Sqrt(Math.Pow(d.X2 - d.X1, 2) + Math.Pow(d.Y2 - d.Y1, 2));
            if (len >= 600.0)
            {
                idx++;
                result.Add(new BeamSegmentModel
                {
                    Index = idx,
                    Name = "DX-1",
                    Mark = $"DX-1_{idx:D4}",
                    TypeName = "B_200x300",
                    Axis = "Diag",
                    WidthMm = 200,
                    HeightMm = 300,
                    StartX = d.X1,
                    StartY = d.Y1,
                    EndX = d.X2,
                    EndY = d.Y2,
                    LengthMm = len,
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
                });
            }
        }

        return result;
    }

    private static List<BeamSegmentModel> AssembleFinalVerifiedBeams(
        List<SpanAuditItem> xSpans, List<SpanAuditItem> ySpans, List<BeamSegmentModel> offGrid)
    {
        var finalBeams = new List<BeamSegmentModel>();
        int idx = 0;

        foreach (var s in xSpans.Where(x => x.HasBeam))
        {
            idx++;
            finalBeams.Add(new BeamSegmentModel
            {
                Index = idx,
                Name = s.BeamName,
                Mark = $"{s.BeamName}_{s.SpanName}",
                TypeName = s.TypeName,
                Axis = "X",
                WidthMm = s.WidthMm,
                HeightMm = s.HeightMm,
                StartX = s.Start,
                StartY = s.GridCoord,
                EndX = s.End,
                EndY = s.GridCoord,
                LengthMm = s.End - s.Start,
                YJustification = s.GridName == "Trục N" || s.GridName == "Trục A" ? 0 : 2,
                YJustificationName = s.GridName == "Trục N" || s.GridName == "Trục A" ? "Left" : "Origin",
                YOffsetMm = s.GridName == "Trục N" ? 100.0 : s.GridName == "Trục A" ? 300.0 : 0.0,
                ZJustification = 0,
                ZJustificationName = "Top",
                ZOffsetMm = 0.0,
                LevelName = "Tầng 1",
                TopElevationMm = -100.0,
                IsPerimeter = s.GridName == "Trục N" || s.GridName == "Trục A",
                IsSecondary = false
            });
        }

        foreach (var s in ySpans.Where(y => y.HasBeam))
        {
            idx++;
            finalBeams.Add(new BeamSegmentModel
            {
                Index = idx,
                Name = s.BeamName,
                Mark = $"{s.BeamName}_{s.SpanName}",
                TypeName = s.TypeName,
                Axis = "Y",
                WidthMm = s.WidthMm,
                HeightMm = s.HeightMm,
                StartX = s.GridCoord,
                StartY = s.Start,
                EndX = s.GridCoord,
                EndY = s.End,
                LengthMm = s.End - s.Start,
                YJustification = s.GridName == "Trục 13" || s.GridName == "Trục 1" ? 0 : 2,
                YJustificationName = s.GridName == "Trục 13" || s.GridName == "Trục 1" ? "Left" : "Origin",
                YOffsetMm = 100.0,
                ZJustification = 0,
                ZJustificationName = "Top",
                ZOffsetMm = 0.0,
                LevelName = "Tầng 1",
                TopElevationMm = -100.0,
                IsPerimeter = s.GridName == "Trục 13" || s.GridName == "Trục 1",
                IsSecondary = false
            });
        }

        foreach (var b in offGrid)
        {
            idx++;
            b.Index = idx;
            finalBeams.Add(b);
        }

        return finalBeams;
    }

    private static (string Name, int W, int H) MatchText(List<CadTextItem> texts, double x, double y, int measuredW)
    {
        string name = "DK";
        int w = measuredW > 0 ? measuredW : 200;
        int h = 400;
        double minDist = double.MaxValue;

        foreach (var t in texts)
        {
            double d = Math.Sqrt(Math.Pow(t.Rx - x, 2) + Math.Pow(t.Ry - y, 2));
            if (d < minDist && d <= 2500.0)
            {
                var matchDim = Regex.Match(t.Text, @"\(?(\d{3})x(\d{3})\)?");
                if (matchDim.Success)
                {
                    w = int.Parse(matchDim.Groups[1].Value);
                    h = int.Parse(matchDim.Groups[2].Value);
                    name = t.Text;
                    minDist = d;
                }
                else if (PrepareStory1GradeBeamsModelCommand.StandardSections.TryGetValue(t.Text, out var sec))
                {
                    name = t.Text;
                    w = sec.W;
                    h = sec.H;
                    minDist = d;
                }
                else if (t.Text.StartsWith("DK", StringComparison.OrdinalIgnoreCase))
                {
                    name = t.Text;
                    minDist = d;
                }
            }
        }
        if (name.Contains("b", StringComparison.OrdinalIgnoreCase) || name.Contains("DX", StringComparison.OrdinalIgnoreCase) || w == 100)
            h = 300;

        return (name, w, h);
    }

    private static List<Seg1D> MergeCollinear(List<Seg1D> segs)
    {
        var result = new List<Seg1D>();
        var groups = segs.GroupBy(s => Math.Round(s.Pos / 5.0) * 5.0);

        foreach (var grp in groups)
        {
            double pos = grp.Key;
            var ordered = grp.OrderBy(s => s.Start).ToList();
            if (ordered.Count == 0) continue;

            double curS = ordered[0].Start;
            double curE = ordered[0].End;

            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].Start <= curE + 80.0)
                {
                    curE = Math.Max(curE, ordered[i].End);
                }
                else
                {
                    if (curE - curS >= 200.0) result.Add(new Seg1D(pos, curS, curE));
                    curS = ordered[i].Start;
                    curE = ordered[i].End;
                }
            }
            if (curE - curS >= 200.0) result.Add(new Seg1D(pos, curS, curE));
        }
        return result;
    }

    private static CadTextItem ParseText(JsonElement t)
    {
        var arr = t.EnumerateArray().ToList();
        return new CadTextItem(arr[0].GetString() ?? "", arr[1].GetString() ?? "",
            double.TryParse(arr[2].GetString(), out double rx) ? rx : 0,
            double.TryParse(arr[3].GetString(), out double ry) ? ry : 0);
    }

    private static GridSystem LoadGrids(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var list = doc.RootElement.GetProperty("grids").EnumerateArray().ToList();
        var xg = list.Where(g => Math.Abs(g.GetProperty("x0").GetDouble() - g.GetProperty("x1").GetDouble()) < 1.0)
                     .Select(g => new GridItem(g.GetProperty("name").GetString() ?? "", g.GetProperty("x0").GetDouble())).ToList();
        var yg = list.Where(g => Math.Abs(g.GetProperty("y0").GetDouble() - g.GetProperty("y1").GetDouble()) < 1.0)
                     .Select(g => new GridItem(g.GetProperty("name").GetString() ?? "", g.GetProperty("y0").GetDouble())).ToList();
        return new GridSystem(xg, yg);
    }

    private static async Task GenerateReportAsync(
        List<SpanAuditItem> xSpans, List<SpanAuditItem> ySpans, List<BeamSegmentModel> offGrid,
        List<BeamSegmentModel> finalBeams, string outPath, CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# BÁO CÁO PHÂN TÍCH TOÀN DIỆN MẶT BẰNG ĐÀ KIỀNG (TẦNG 1)");
        sb.AppendLine("## ĐỐI SOÁT TỪNG NHỊP CỘT -- LOẠI TRỪ DẦM MA & THU THẬP DẦM PHỤ");
        sb.AppendLine();
        sb.AppendLine($"* **Thời gian lập**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"* **Tổng số dầm chính phương X**: {xSpans.Count(x => x.HasBeam)} nhịp (đã loại trừ {xSpans.Count(x => !x.HasBeam)} nhịp ô mở)");
        sb.AppendLine($"* **Tổng số dầm chính phương Y**: {ySpans.Count(y => y.HasBeam)} nhịp (đã loại trừ {ySpans.Count(y => !y.HasBeam)} nhịp ô mở)");
        sb.AppendLine($"* **Tổng số dầm phụ / ngoại biên / ô thang**: {offGrid.Count} dầm");
        sb.AppendLine($"* **TỔNG SỐ DẦM CHÍNH THỨC**: **{finalBeams.Count} dầm**");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("### 1. BẢNG ĐỐI SOÁT NHỊP TRỤC PHƯƠNG X (DỌC THEO CÁC TRỤC A..N)");
        sb.AppendLine();
        sb.AppendLine("| Trục | Nhịp cột | Chiều dài | Nét CAD | Trạng thái | Tiết diện |");
        sb.AppendLine("|:---|:---:|:---:|:---:|:---:|:---:|");

        foreach (var s in xSpans)
        {
            string st = s.HasBeam ? "✅ CÓ DẦM" : "❌ Ô MỞ (KHÔNG CÓ DẦM)";
            sb.AppendLine($"| {s.GridName} | {s.SpanName} | {s.ClearLen:F0} | {s.CoverageLines} nét | {st} | `{s.TypeName}` |");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("### 2. BẢNG ĐỐI SOÁT NHỊP TRỤC PHƯƠNG Y (DỌC THEO CÁC TRỤC 1..13)");
        sb.AppendLine();
        sb.AppendLine("| Trục | Nhịp cột | Chiều dài | Nét CAD | Trạng thái | Tiết diện |");
        sb.AppendLine("|:---|:---:|:---:|:---:|:---:|:---:|");

        foreach (var s in ySpans)
        {
            string st = s.HasBeam ? "✅ CÓ DẦM" : "❌ Ô MỞ (KHÔNG CÓ DẦM)";
            sb.AppendLine($"| {s.GridName} | {s.SpanName} | {s.ClearLen:F0} | {s.CoverageLines} nét | {st} | `{s.TypeName}` |");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("### 3. DANH SÁCH DẦM PHỤ & DẦM NGOẠI BIÊN ĐƯỢC THU THẬP");
        sb.AppendLine();
        sb.AppendLine("| STT | Tên | Tiết diện | Phương | Chiều dài | Tọa độ Bắt đầu | Tọa độ Kết thúc |");
        sb.AppendLine("|:---:|:---|:---:|:---:|:---:|:---:|:---:|");

        int i = 0;
        foreach (var b in offGrid)
        {
            i++;
            sb.AppendLine($"| {i} | `{b.Name}` | `{b.TypeName}` | {b.Axis} | {b.LengthMm:F0} | ({b.StartX:F0}, {b.StartY:F0}) | ({b.EndX:F0}, {b.EndY:F0}) |");
        }

        await File.WriteAllTextAsync(outPath, sb.ToString(), System.Text.Encoding.UTF8, ct).ConfigureAwait(false);
    }
}

public sealed class SpanAuditItem
{
    public string Axis { get; set; } = "";
    public string GridName { get; set; } = "";
    public double GridCoord { get; set; }
    public string SpanName { get; set; } = "";
    public double Start { get; set; }
    public double End { get; set; }
    public double ClearLen { get; set; }
    public bool HasBeam { get; set; }
    public int WidthMm { get; set; }
    public int HeightMm { get; set; }
    public string BeamName { get; set; } = "";
    public string TypeName { get; set; } = "";
    public int CoverageLines { get; set; }
}
