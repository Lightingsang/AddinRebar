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
using HPBim.Core.Coordination;

/// <summary>
///     Reconstructs the 3D grade beam network from CAD lines and text annotations.
///     Protected by FailFastGate to eliminate speculation and default dimensions.
/// </summary>
public sealed class BuildGradeBeamNetworkCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IFailFastGate _failFastGate;

    public BuildGradeBeamNetworkCommand(ConsoleFormatter? formatter = null, IFailFastGate? failFastGate = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _failFastGate = failFastGate ?? new FailFastGate();
    }

    private static readonly Dictionary<string, (int W, int H)> StandardSections = new(StringComparer.OrdinalIgnoreCase)
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
        string outputPath = args.GetOption("output", Path.Combine("scratch", "grade_beams_model.json"));

        _formatter.WriteBanner("Build Grade Beam Network", "Khôi phục mạng lưới đà kiềng từ đường bao và nhãn CAD");

        if (!File.Exists(linesPath) || !File.Exists(textsPath))
        {
            _formatter.WriteError("Không tìm thấy file dữ liệu đầu vào!");
            return Task.FromResult(1);
        }

        using var linesDoc = JsonDocument.Parse(File.ReadAllText(linesPath));
        var linesRoot = linesDoc.RootElement;
        var xLines = linesRoot.GetProperty("xLines").EnumerateArray()
            .Select(l => new CadLineDto(
                l.GetProperty("y").GetDouble(),
                l.GetProperty("x1").GetDouble(),
                l.GetProperty("x2").GetDouble(),
                l.GetProperty("len").GetDouble()))
            .ToList();

        var yLines = linesRoot.GetProperty("yLines").EnumerateArray()
            .Select(l => new CadLineDto(
                l.GetProperty("x").GetDouble(),
                l.GetProperty("y1").GetDouble(),
                l.GetProperty("y2").GetDouble(),
                l.GetProperty("len").GetDouble()))
            .ToList();

        using var textsDoc = JsonDocument.Parse(File.ReadAllText(textsPath));
        var dkTexts = textsDoc.RootElement.GetProperty("dkTexts").EnumerateArray()
            .Select(t => new CadTextDto(
                t.GetProperty("text").GetString() ?? "",
                t.GetProperty("layer").GetString() ?? "",
                t.GetProperty("rx").GetDouble(),
                t.GetProperty("ry").GetDouble()))
            .ToList();

        _formatter.WriteInfo($"Đã nạp {xLines.Count} đoạn thẳng X, {yLines.Count} đoạn thẳng Y, {dkTexts.Count} nhãn CAD.");

        var unverifiedList = new List<string>();
        var xBeams = ProcessAxisLines(xLines, dkTexts, isXAxis: true, _failFastGate, unverifiedList);
        var yBeams = ProcessAxisLines(yLines, dkTexts, isXAxis: false, _failFastGate, unverifiedList);

        if (unverifiedList.Count > 0)
        {
            _formatter.WriteWarning($"FailFastGate: Đã chặn {unverifiedList.Count} đoạn dầm suy đoán / thiếu thông tin.");
        }

        var uniqueXBeams = DeduplicateSegments(xBeams);
        var uniqueYBeams = DeduplicateSegments(yBeams);

        _formatter.WriteSuccess($"Xác thực: {uniqueXBeams.Count} dầm phương X, {uniqueYBeams.Count} dầm phương Y.");

        var allBeams = uniqueXBeams.Concat(uniqueYBeams).ToList();
        File.WriteAllText(outputPath, JsonSerializer.Serialize(new
        {
            totalBeams = allBeams.Count,
            totalX = uniqueXBeams.Count,
            totalY = uniqueYBeams.Count,
            unverifiedCount = unverifiedList.Count,
            beams = allBeams
        }, JsonOptions));

        _formatter.WriteSuccess($"Đã xuất mô hình đà kiềng vào: {outputPath}");

        foreach (var b in allBeams.Take(25))
        {
            Console.WriteLine($"  [{b.Axis}] {b.Name, -8} Type: {b.TypeName, -10} L={b.Length, 6:F0}mm từ ({b.StartX, 7:F0}, {b.StartY, 7:F0}) đến ({b.EndX, 7:F0}, {b.EndY, 7:F0})");
        }

        if (unverifiedList.Count > 0)
        {
            throw new FailFastCoordinationException("BeamNetwork", unverifiedList);
        }

        return Task.FromResult(0);
    }

    private static List<BeamSegment> ProcessAxisLines(
        List<CadLineDto> linesList, List<CadTextDto> dkTexts, bool isXAxis, IFailFastGate gate, List<string> unverifiedList)
    {
        var result = new List<BeamSegment>();
        var positions = linesList.Select(l => l.Pos).Distinct();
        var clusters = ClusterValues(positions, 25.0);

        for (int i = 0; i < clusters.Count; i++)
        {
            for (int j = i + 1; j < clusters.Count; j++)
            {
                double dist = Math.Abs(clusters[j] - clusters[i]);
                if (dist >= 180.0 && dist <= 320.0)
                {
                    double mid = (clusters[i] + clusters[j]) / 2.0;
                    double width = Math.Round(dist, 0);

                    var subset1 = linesList.Where(l => Math.Abs(l.Pos - clusters[i]) <= 15.0).ToList();
                    var subset2 = linesList.Where(l => Math.Abs(l.Pos - clusters[j]) <= 15.0).ToList();

                    foreach (var l1 in subset1)
                    {
                        var matchingL2 = subset2.FirstOrDefault(l2 =>
                            Math.Max(l1.Start, l2.Start) < Math.Min(l1.End, l2.End) - 100.0);

                        if (matchingL2 != null)
                        {
                            double s = Math.Min(l1.Start, matchingL2.Start);
                            double e = Math.Max(l1.End, matchingL2.End);

                            double centerX = isXAxis ? (s + e) / 2.0 : mid;
                            double centerY = isXAxis ? mid : (s + e) / 2.0;

                            var info = ResolveBeamInfo(dkTexts, centerX, centerY, width);

                            var spec = new CadSourceSpec
                            {
                                Handle = "", // Line pairing lacks genuine CAD element handle
                                Layer = "DK",
                                TopElevation = -1.600,
                                Width = info.W > 0 ? info.W : null,
                                Height = info.H > 0 ? info.H : null,
                                Supports = Array.Empty<string>()
                            };

                            var eval = gate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, info.Name);
                            if (!eval.IsPassed)
                            {
                                unverifiedList.Add($"[{info.Name}] L={e - s:F0}mm: {string.Join("; ", eval.Violations)}");
                                continue;
                            }

                            result.Add(new BeamSegment
                            {
                                Axis = isXAxis ? "X" : "Y",
                                Name = info.Name,
                                TypeName = $"B_{info.W}x{info.H}",
                                StartX = isXAxis ? s : mid,
                                StartY = isXAxis ? mid : s,
                                EndX = isXAxis ? e : mid,
                                EndY = isXAxis ? mid : e,
                                Width = info.W,
                                Height = info.H,
                                Length = Math.Round(e - s, 1)
                            });
                        }
                    }
                }
            }
        }
        return result;
    }

    private static BeamInfo ResolveBeamInfo(List<CadTextDto> dkTexts, double x, double y, double measuredW)
    {
        string bestName = "DK";
        int w = 0;
        int h = 0; // FailFast: Zero defaults, no speculative h=400mm

        double minDist = double.MaxValue;
        foreach (var t in dkTexts)
        {
            double dist = Math.Sqrt(Math.Pow(t.Rx - x, 2) + Math.Pow(t.Ry - y, 2));
            if (dist < minDist && dist <= 2000.0)
            {
                var matchDim = Regex.Match(t.Text, @"\(?(\d{3})x(\d{3})\)?");
                if (matchDim.Success)
                {
                    w = int.Parse(matchDim.Groups[1].Value);
                    h = int.Parse(matchDim.Groups[2].Value);
                    bestName = t.Text;
                    minDist = dist;
                }
                else if (StandardSections.TryGetValue(t.Text, out var sec))
                {
                    bestName = t.Text;
                    w = sec.W;
                    h = sec.H;
                    minDist = dist;
                }
            }
        }

        return new BeamInfo(bestName, w, h);
    }

    private static List<double> ClusterValues(IEnumerable<double> values, double tol)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var clusters = new List<double>();
        foreach (var v in sorted)
        {
            if (clusters.Count == 0 || Math.Abs(clusters[^1] - v) > tol)
            {
                clusters.Add(v);
            }
        }
        return clusters;
    }

    private static List<BeamSegment> DeduplicateSegments(List<BeamSegment> segments)
    {
        var result = new List<BeamSegment>();
        foreach (var seg in segments.OrderByDescending(s => s.Length))
        {
            bool exists = result.Any(r =>
                Math.Abs(r.StartX - seg.StartX) <= 100.0 &&
                Math.Abs(r.StartY - seg.StartY) <= 100.0 &&
                Math.Abs(r.EndX - seg.EndX) <= 100.0 &&
                Math.Abs(r.EndY - seg.EndY) <= 100.0);

            if (!exists) result.Add(seg);
        }
        return result;
    }

    public sealed record CadLineDto(double Pos, double Start, double End, double Length);
    public sealed record CadTextDto(string Text, string Layer, double Rx, double Ry);
    public sealed record BeamInfo(string Name, int W, int H);

    public sealed class BeamSegment
    {
        public string Axis { get; set; } = "";
        public string Name { get; set; } = "";
        public string TypeName { get; set; } = "";
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Length { get; set; }
    }
}
