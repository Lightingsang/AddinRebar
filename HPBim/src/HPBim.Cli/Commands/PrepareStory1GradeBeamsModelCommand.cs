namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;

/// <summary>
///     Pure C# command to prepare Story 1 Grade Beams from Support to Support (Column-to-Column, Column-to-Wall).
///     Strictly complies with ETABS & BIM standards -- NO artificial cuts at empty grid intersections.
/// </summary>
public sealed class PrepareStory1GradeBeamsModelCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public PrepareStory1GradeBeamsModelCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public static readonly Dictionary<string, (int W, int H)> StandardSections = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DK1-X"] = (200, 400), ["DK2-X"] = (200, 400), ["DK3-X"] = (200, 400), ["DK4-X"] = (200, 400),
        ["DK5-X"] = (200, 400), ["DK5a-X"] = (200, 400), ["DK5'-X"] = (200, 400), ["DK6-X"] = (200, 300),
        ["DK7-X"] = (200, 400), ["DK8-X"] = (200, 400), ["DK9-X"] = (200, 500), ["DK10-X"] = (200, 400),
        ["DK11-X"] = (300, 400), ["DK12-X"] = (200, 300), ["DK13-X"] = (200, 400), ["DK14-X"] = (200, 400),
        ["DK15-X"] = (200, 400), ["DK16-X"] = (200, 300), ["DK17-X"] = (200, 300), ["DK18-X"] = (200, 300),
        ["DK19-X"] = (200, 300), ["DK20-X"] = (200, 300), ["DK21-X"] = (200, 300),
        ["DK1-Y"] = (300, 400), ["DK2-Y"] = (300, 400), ["DK3-Y"] = (300, 400), ["DK4-Y"] = (200, 400),
        ["DK5-Y"] = (200, 400), ["DK5'-Y"] = (200, 400), ["DK6-Y"] = (300, 400), ["DK7-Y"] = (300, 600),
        ["DK8-Y"] = (200, 400), ["DK9-Y"] = (200, 400), ["DK10-Y"] = (300, 600), ["DK11-Y"] = (200, 400),
        ["DK12-Y"] = (200, 400), ["DK13-Y"] = (200, 300), ["DK14-Y"] = (200, 300), ["DK15-Y"] = (200, 400),
        ["DK16-Y"] = (200, 300), ["DK17-Y"] = (200, 400), ["DK18-Y"] = (200, 400), ["DK19-Y"] = (200, 400),
        ["DK20-Y"] = (200, 400), ["DK21-Y"] = (200, 400), ["DK22-Y"] = (200, 300), ["DK23-Y"] = (200, 300),
        ["DK.a"] = (200, 400), ["DK.a'"] = (200, 400), ["DK.b"] = (200, 300), ["DX-1"] = (200, 300)
    };

    public Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string linesPath = args.GetOption("lines", Path.Combine("scratch", "cad_grade_beam_lines.json"));
        string textsPath = args.GetOption("texts", Path.Combine("scratch", "cad_grade_beams_analysis.json"));
        string gridsPath = args.GetOption("grids", Path.Combine("scratch", "revit_grids_and_levels.json"));
        string colsPath = args.GetOption("columns", Path.Combine("scratch", "columns_story1_model.json"));
        string wallsPath = args.GetOption("walls", Path.Combine("scratch", "elevator_walls_model.json"));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "story1_grade_beams_model.json"));

        _formatter.WriteBanner("Prepare Story 1 Grade Beams", "Chuẩn hóa Dầm Sàn Tầng 1: Từ Cột sang Cột/Vách (ETABS chuẩn)");

        if (!File.Exists(linesPath) || !File.Exists(textsPath) || !File.Exists(gridsPath))
        {
            _formatter.WriteError("Không tìm thấy đủ file dữ liệu CAD hoặc Grids!");
            return Task.FromResult(1);
        }

        var linesData = LoadLines(linesPath);
        var textsData = LoadTexts(textsPath);
        var gridsData = LoadGrids(gridsPath);
        var allSupports = Story1BeamSupportGraph.LoadAllSupports(colsPath, wallsPath, gridsData);

        _formatter.WriteInfo($"Đã nạp {gridsData.XGrids.Count} trục X, {gridsData.YGrids.Count} trục Y, {allSupports.Count} điểm tựa (Cột & Vách).");

        var allBeams = new List<BeamSegmentModel>();
        int beamIndex = 0;

        // 1. Process X Beams along Y Grids (A..N) from Support to Support
        var xBeams = Story1BeamTopologyService.GenerateXBeams(allSupports, gridsData, linesData.xLines, linesData.yLines, textsData, ref beamIndex);
        allBeams.AddRange(xBeams);

        // 2. Process Y Beams along X Grids (1..13) from Support to Support
        var yBeams = Story1BeamTopologyService.GenerateYBeams(allSupports, gridsData, linesData.yLines, linesData.xLines, textsData, ref beamIndex);
        allBeams.AddRange(yBeams);

        // 3. Process All Secondary & Off-grid Beams (DK.a, DK.b, DX-1, Elevator & Toilet drops)
        var secBeams = Story1SecondaryBeamService.GenerateAllSecondaryBeams(gridsData, linesData, textsData, ref beamIndex);
        allBeams.AddRange(secBeams);

        var deduplicatedBeams = DeduplicateBeams(allBeams);
        AlignToCadParallelPairs(deduplicatedBeams, linesData.xLines, linesData.yLines);
        _formatter.WriteSuccess($"Đã tạo {deduplicatedBeams.Count} dầm chuẩn (Đã khử trùng lặp): {deduplicatedBeams.Count(b => b.Axis == "X")} dầm X, {deduplicatedBeams.Count(b => b.Axis == "Y")} dầm Y, {deduplicatedBeams.Count(b => b.Axis == "Diag")} dầm xiên.");

        var resultObj = new
        {
            totalBeams = deduplicatedBeams.Count,
            totalX = deduplicatedBeams.Count(b => b.Axis == "X"),
            totalY = deduplicatedBeams.Count(b => b.Axis == "Y"),
            totalDiag = deduplicatedBeams.Count(b => b.Axis == "Diag"),
            targetLevel = "Tầng 1",
            topElevationMm = -100.0,
            beams = deduplicatedBeams
        };

        File.WriteAllText(outputPath, JsonSerializer.Serialize(resultObj, JsonOptions));
        _formatter.WriteSuccess($"Đã xuất mô hình chuẩn Điểm Tựa (Cột & Vách) vào: {outputPath}");

        foreach (var b in deduplicatedBeams.Take(20))
            Console.WriteLine($"  [{b.Axis}] {b.Mark,-15} {b.TypeName,-10} L={b.LengthMm,6:F0}mm | Origin | ({b.StartX,6:F0},{b.StartY,6:F0}) -> ({b.EndX,6:F0},{b.EndY,6:F0})");

        return Task.FromResult(0);
    }

    private static List<BeamSegmentModel> DeduplicateBeams(List<BeamSegmentModel> raw)
    {
        var clean = new List<BeamSegmentModel>();
        var ordered = raw.OrderBy(b => b.IsSecondary ? 1 : 0)
                         .ThenByDescending(b => b.LengthMm).ToList();

        foreach (var b in ordered)
        {
            double s1 = b.Axis == "X" ? Math.Min(b.StartX, b.EndX) : Math.Min(b.StartY, b.EndY);
            double e1 = b.Axis == "X" ? Math.Max(b.StartX, b.EndX) : Math.Max(b.StartY, b.EndY);
            double pos1 = b.Axis == "X" ? b.StartY : b.StartX;

            bool overlaps = false;
            foreach (var c in clean)
            {
                if (c.Axis != b.Axis) continue;
                double pos2 = c.Axis == "X" ? c.StartY : c.StartX;
                if (Math.Abs(pos1 - pos2) > 60.0) continue;

                double s2 = c.Axis == "X" ? Math.Min(c.StartX, c.EndX) : Math.Min(c.StartY, c.EndY);
                double e2 = c.Axis == "X" ? Math.Max(c.StartX, c.EndX) : Math.Max(c.StartY, c.EndY);

                double sOverlap = Math.Max(s1, s2);
                double eOverlap = Math.Min(e1, e2);
                double overlap = eOverlap - sOverlap;

                if (overlap >= 300.0 && (overlap >= (e1 - s1) * 0.40 || overlap >= (e2 - s2) * 0.40))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                clean.Add(b);
            }
        }

        for (int i = 0; i < clean.Count; i++)
        {
            clean[i].Index = i + 1;
        }

        return clean;
    }

    private static void AlignToCadParallelPairs(
        List<BeamSegmentModel> beams, List<CadLineItem> xLines, List<CadLineItem> yLines)
    {
        int[] stdW = [500, 400, 300, 200, 100];
        foreach (var b in beams)
        {
            if (b.Axis == "X")
            {
                var nearby = xLines.Where(l => Math.Abs(l.Pos - b.StartY) <= 250.0 &&
                    Math.Max(l.Start, Math.Min(b.StartX, b.EndX)) < Math.Min(l.End, Math.Max(b.StartX, b.EndX)) - 100.0).ToList();
                double bestScore = -1.0, bestMid = b.StartY;
                int bestW = (int)b.WidthMm;
                for (int i = 0; i < nearby.Count; i++)
                {
                    for (int j = i + 1; j < nearby.Count; j++)
                    {
                        double d = Math.Abs(nearby[i].Pos - nearby[j].Pos);
                        foreach (var sw in stdW)
                        {
                            if (Math.Abs(d - sw) <= 25.0)
                            {
                                double s = Math.Max(nearby[i].Start, nearby[j].Start);
                                double e = Math.Min(nearby[i].End, nearby[j].End);
                                double score = (e - s) - Math.Abs(d - b.WidthMm) * 10.0;
                                if (score > bestScore)
                                {
                                    bestScore = score; bestW = sw;
                                    bestMid = Math.Round((nearby[i].Pos + nearby[j].Pos) / 2.0, 1);
                                }
                            }
                        }
                    }
                }
                if (bestScore > 0)
                {
                    b.StartY = bestMid; b.EndY = bestMid; b.WidthMm = bestW;
                    b.TypeName = $"B_{bestW}x{b.HeightMm:F0}";
                }
            }
            else if (b.Axis == "Y")
            {
                var nearby = yLines.Where(l => Math.Abs(l.Pos - b.StartX) <= 250.0 &&
                    Math.Max(l.Start, Math.Min(b.StartY, b.EndY)) < Math.Min(l.End, Math.Max(b.StartY, b.EndY)) - 100.0).ToList();
                double bestScore = -1.0, bestMid = b.StartX;
                int bestW = (int)b.WidthMm;
                for (int i = 0; i < nearby.Count; i++)
                {
                    for (int j = i + 1; j < nearby.Count; j++)
                    {
                        double d = Math.Abs(nearby[i].Pos - nearby[j].Pos);
                        foreach (var sw in stdW)
                        {
                            if (Math.Abs(d - sw) <= 25.0)
                            {
                                double s = Math.Max(nearby[i].Start, nearby[j].Start);
                                double e = Math.Min(nearby[i].End, nearby[j].End);
                                double score = (e - s) - Math.Abs(d - b.WidthMm) * 10.0;
                                if (score > bestScore)
                                {
                                    bestScore = score; bestW = sw;
                                    bestMid = Math.Round((nearby[i].Pos + nearby[j].Pos) / 2.0, 1);
                                }
                            }
                        }
                    }
                }
                if (bestScore > 0)
                {
                    b.StartX = bestMid; b.EndX = bestMid; b.WidthMm = bestW;
                    b.TypeName = $"B_{bestW}x{b.HeightMm:F0}";
                }
            }
        }
    }

    private static (List<CadLineItem> xLines, List<CadLineItem> yLines, List<CadDiagLineItem> diagLines) LoadLines(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var xLines = root.GetProperty("xLines").EnumerateArray()
            .Select(l => new CadLineItem(l.GetProperty("handle").GetString() ?? "", l.GetProperty("y").GetDouble(), l.GetProperty("x1").GetDouble(), l.GetProperty("x2").GetDouble(), l.GetProperty("len").GetDouble())).ToList();
        var yLines = root.GetProperty("yLines").EnumerateArray()
            .Select(l => new CadLineItem(l.GetProperty("handle").GetString() ?? "", l.GetProperty("x").GetDouble(), l.GetProperty("y1").GetDouble(), l.GetProperty("y2").GetDouble(), l.GetProperty("len").GetDouble())).ToList();
        var diagLines = root.TryGetProperty("diagLines", out var dArr)
            ? dArr.EnumerateArray().Select(d => new CadDiagLineItem(
                d.GetProperty("handle").GetString() ?? "",
                d.GetProperty("x1").GetDouble(),
                d.GetProperty("y1").GetDouble(),
                d.GetProperty("x2").GetDouble(),
                d.GetProperty("y2").GetDouble(),
                d.GetProperty("len").GetDouble())).ToList()
            : new List<CadDiagLineItem>();
        return (xLines, yLines, diagLines);
    }

    private static List<CadTextItem> LoadTexts(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("dkTexts").EnumerateArray()
            .Select(t => new CadTextItem(t.GetProperty("text").GetString() ?? "", t.GetProperty("layer").GetString() ?? "", t.GetProperty("rx").GetDouble(), t.GetProperty("ry").GetDouble())).ToList();
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
}

public sealed record GridItem(string Name, double Coord);
public sealed record GridSystem(List<GridItem> XGrids, List<GridItem> YGrids);
public sealed record CadLineItem(string Handle, double Pos, double Start, double End, double Len);
public sealed record CadDiagLineItem(string Handle, double X1, double Y1, double X2, double Y2, double Len);
public sealed record CadTextItem(string Text, string Layer, double Rx, double Ry);

public sealed class BeamSegmentModel
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string Mark { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string Axis { get; set; } = "";
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
    public double LengthMm { get; set; }
    public int YJustification { get; set; }
    public string YJustificationName { get; set; } = "Origin";
    public double YOffsetMm { get; set; }
    public int ZJustification { get; set; }
    public string ZJustificationName { get; set; } = "Top";
    public double ZOffsetMm { get; set; }
    public string LevelName { get; set; } = "Tầng 1";
    public double TopElevationMm { get; set; } = -100.0;
    public bool IsPerimeter { get; set; }
    public bool IsSecondary { get; set; }
}
