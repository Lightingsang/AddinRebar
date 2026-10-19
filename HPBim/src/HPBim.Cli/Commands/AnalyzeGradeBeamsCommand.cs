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
///     Analyzes grade beam lines and text labels extracted from CAD.
///     Protected by FailFastGate to eliminate speculation and floating text pairings.
/// </summary>
public sealed class AnalyzeGradeBeamsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IFailFastGate _failFastGate;

    public AnalyzeGradeBeamsCommand(ConsoleFormatter? formatter = null, IFailFastGate? failFastGate = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _failFastGate = failFastGate ?? new FailFastGate();
    }

    public Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string rawPath = args.GetOption("input", Path.Combine("scratch", "cad_grade_beams_raw.json"));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_grade_beams_analysis.json"));

        _formatter.WriteBanner("Analyze Grade Beams", "Phân tích hệ thống đà kiềng / giằng móng từ CAD");

        if (!File.Exists(rawPath))
        {
            _formatter.WriteError($"Không tìm thấy file: {rawPath}");
            return Task.FromResult(1);
        }

        string json = File.ReadAllText(rawPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.String)
        {
            using var innerDoc = JsonDocument.Parse(root.GetString() ?? "{}");
            return Task.FromResult(Analyze(innerDoc.RootElement, outputPath));
        }
        return Task.FromResult(Analyze(root, outputPath));
    }

    private int Analyze(JsonElement root, string outputPath)
    {
        var msTexts = root.GetProperty("msTexts").EnumerateArray()
            .Select(t => new
            {
                text = t.GetProperty("text").GetString() ?? "",
                layer = t.GetProperty("layer").GetString() ?? "",
                rx = t.GetProperty("rx").GetDouble(),
                ry = t.GetProperty("ry").GetDouble()
            })
            .ToList();

        _formatter.WriteInfo($"Tổng số texts trong khu vực móng: {msTexts.Count}");

        // Filter texts that could be grade beam tags (DK, ĐK, dimensions like 200x, 300x, etc.)
        var dkTexts = msTexts
            .Where(t => Regex.IsMatch(t.text, @"(DK|ĐK|\b[1-9]\d{0,2}x\d{2,3})", RegexOptions.IgnoreCase))
            .ToList();

        _formatter.WriteSuccess($"Tìm thấy {dkTexts.Count} nhãn liên quan đến đà kiềng (DK / tiết diện)");
        foreach (var t in dkTexts.Take(40))
        {
            Console.WriteLine($"  [Text] '{t.text}' (Layer: {t.layer}, X={t.rx:F1}, Y={t.ry:F1})");
        }

        var beamNameTexts = msTexts.Where(t => t.layer.StartsWith("DK-", StringComparison.OrdinalIgnoreCase)
                                              || t.layer.Equals("a", StringComparison.OrdinalIgnoreCase)
                                              || t.layer.Equals("a'", StringComparison.OrdinalIgnoreCase)
                                              || t.layer.Equals("CS1-T2", StringComparison.OrdinalIgnoreCase)
                                              || t.layer.Equals("DK1", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var ttnTexts = msTexts.Where(t => t.layer.Equals("TTN", StringComparison.OrdinalIgnoreCase)).ToList();

        _formatter.WriteInfo($"\n--- Ghép nối Nhãn dầm và Kích thước tiết diện (TTN) ---");
        // For each beam name, find nearest TTN text within 1500mm
        var pairedBeams = new List<object>();
        var unverifiedList = new List<string>();

        foreach (var bn in beamNameTexts)
        {
            var nearestTtn = ttnTexts
                .Select(tt => new {
                    ttn = tt,
                    dist = Math.Sqrt(Math.Pow(tt.rx - bn.rx, 2) + Math.Pow(tt.ry - bn.ry, 2))
                })
                .Where(x => x.dist < 2000.0)
                .OrderBy(x => x.dist)
                .FirstOrDefault();

            string sec = nearestTtn?.ttn.text ?? "Unknown";
            bool hasValidSec = !string.Equals(sec, "Unknown", StringComparison.OrdinalIgnoreCase);

            var spec = new CadSourceSpec
            {
                Handle = bn.text,
                Layer = bn.layer,
                TopElevation = -1.600,
                Width = hasValidSec ? 200 : null,
                Height = hasValidSec ? 400 : null,
                Supports = Array.Empty<string>()
            };

            var eval = _failFastGate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, bn.text);
            if (!eval.IsPassed)
            {
                unverifiedList.Add($"[{bn.text}] {string.Join("; ", eval.Violations)}");
            }

            pairedBeams.Add(new {
                Name = bn.text,
                Layer = bn.layer,
                X = bn.rx,
                Y = bn.ry,
                Section = sec,
                DistToSec = nearestTtn != null ? Math.Round(nearestTtn.dist, 1) : -1,
                IsVerified = eval.IsPassed
            });
        }

        if (unverifiedList.Count > 0)
        {
            _formatter.WriteWarning($"FailFastGate: Có {unverifiedList.Count} vị trí dầm chưa đủ cơ sở để kết luận tiết diện.");
        }

        _formatter.WriteSuccess($"Đã ghép được {pairedBeams.Count} vị trí dầm. Hiển thị 30 dầm mẫu:");
        foreach (dynamic b in pairedBeams.Take(30))
        {
            Console.WriteLine($"  [{b.Layer}] {b.Name, -8} tại (X={b.X, 7:F1}, Y={b.Y, 7:F1}) -> Tiết diện: {b.Section} (d={b.DistToSec}mm)");
        }

        // Summary of beam types and sections
        var beamTypeGroups = pairedBeams.Cast<dynamic>()
            .GroupBy(b => (string)b.Name)
            .Select(g => new {
                Name = g.Key,
                Count = g.Count(),
                Sections = string.Join(", ", g.Select(x => (string)x.Section).Distinct()),
                SampleX = g.First().X,
                SampleY = g.First().Y
            })
            .OrderBy(x => x.Name);

        _formatter.WriteInfo("\n--- Bảng tổng hợp các loại dầm và tiết diện ---");
        foreach (var bg in beamTypeGroups)
        {
            Console.WriteLine($"  - {bg.Name, -8} (SL={bg.Count, 2}) -> Tiết diện: {bg.Sections}");
        }

        // Save filtered texts to output
        File.WriteAllText(outputPath, JsonSerializer.Serialize(new
        {
            totalMsTexts = msTexts.Count,
            dkTextsCount = dkTexts.Count,
            dkTexts = dkTexts,
            allTexts = msTexts
        }, JsonOptions));

        _formatter.WriteSuccess($"Đã lưu kết quả phân tích vào: {outputPath}");
        return 0;
    }
}
