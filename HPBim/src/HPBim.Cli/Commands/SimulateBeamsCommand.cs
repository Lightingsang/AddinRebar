namespace HPBim.Cli.Commands;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;

/// <summary>
///     Pure C# command to simulate Story 1 Beams in an interactive 3D WebGL Digital Twin
///     directly inside Antigravity before committing to Revit.
/// </summary>
public sealed class SimulateBeamsCommand
{
    private readonly ConsoleFormatter _formatter;

    public SimulateBeamsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string beamsPath = args.GetOption("input", Path.Combine("scratch", "story1_grade_beams_model.json"));
        string cadPath = args.GetOption("cad", Path.Combine("scratch", "cad_grade_beam_lines.json"));
        string colsPath = args.GetOption("columns", Path.Combine("scratch", "columns_story1_model.json"));
        string wallsPath = args.GetOption("walls", Path.Combine("scratch", "elevator_walls_model.json"));
        string gridsPath = args.GetOption("grids", Path.Combine("scratch", "revit_grids_and_levels.json"));

        string artifactDir = Path.Combine("C:", "Users", "STR-HP03", ".gemini", "antigravity", "brain", "087c0df3-fff1-40c1-83ce-c0d4c6cb6acd");
        string defaultOutput = Path.Combine(artifactDir, "story1_beams_3d_simulation.html");
        string outputPath = args.GetOption("output", defaultOutput);

        _formatter.WriteBanner("3D Digital Twin Simulation", "Mô Phỏng Trực Quan 3D Dầm Tầng 1 (Đà Kiềng) Trước Khi Dựng Revit");

        if (!File.Exists(beamsPath) || !File.Exists(cadPath))
        {
            _formatter.WriteError($"Thiếu file dữ liệu đầu vào dầm hoặc CAD: {beamsPath}, {cadPath}");
            return 1;
        }

        string beamsJson = await File.ReadAllTextAsync(beamsPath, ct).ConfigureAwait(false);
        string cadJson = await File.ReadAllTextAsync(cadPath, ct).ConfigureAwait(false);
        string colsJson = File.Exists(colsPath) ? await File.ReadAllTextAsync(colsPath, ct).ConfigureAwait(false) : "[]";
        string wallsJson = File.Exists(wallsPath) ? await File.ReadAllTextAsync(wallsPath, ct).ConfigureAwait(false) : "[]";
        string gridsJson = File.Exists(gridsPath) ? await File.ReadAllTextAsync(gridsPath, ct).ConfigureAwait(false) : "[]";

        int beamCount = ParseCount(beamsJson, "beams");
        int colCount = ParseCount(colsJson, "columns");
        int wallCount = ParseCount(wallsJson, "walls");

        _formatter.WriteInfo($"Đã nạp {beamCount} dầm 3D, {colCount} cột, {wallCount} vách và nét vẽ CAD.");
        _formatter.WriteInfo("Đang khởi tạo mã nguồn 3D WebGL Digital Twin Viewer...");

        string htmlContent = await Story1WebGlSimulatorBuilder.BuildHtmlAsync(
            beamsJson, cadJson, colsJson, wallsJson, gridsJson, ct).ConfigureAwait(false);

        if (Directory.Exists(artifactDir))
        {
            await File.WriteAllTextAsync(outputPath, htmlContent, ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã tạo 3D Digital Twin Viewer Artifact tại: {outputPath}");
        }

        string localOutput = Path.Combine("scratch", "story1_beams_3d_simulation.html");
        await File.WriteAllTextAsync(localOutput, htmlContent, ct).ConfigureAwait(false);
        _formatter.WriteSuccess($"Đã sao lưu bản xem trước tại: {localOutput}");

        RenderSummaryTable(beamCount, colCount, wallCount, outputPath);
        return 0;
    }

    private static int ParseCount(string json, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array) return doc.RootElement.GetArrayLength();
            if (doc.RootElement.TryGetProperty(propertyName, out var p) && p.ValueKind == JsonValueKind.Array)
                return p.GetArrayLength();
            if (doc.RootElement.TryGetProperty("totalBeams", out var tb)) return tb.GetInt32();
            if (doc.RootElement.TryGetProperty("totalColumns", out var tc)) return tc.GetInt32();
            if (doc.RootElement.TryGetProperty("totalWalls", out var tw)) return tw.GetInt32();
        }
        catch (JsonException)
        {
            // Non-critical: property is optional or JSON structure differs
        }
        return 0;
    }

    private void RenderSummaryTable(int beams, int cols, int walls, string outputPath)
    {
        string[][] rows =
        [
            ["Công nghệ nhận diện", "Google AI Spatial Reasoning + AutoCAD .NET Vector Engine"],
            ["Động cơ đồ họa mô phỏng", "Interactive 3D WebGL Digital Twin (Embedded, 0 phụ thuộc mạng)"],
            ["Số lượng Dầm 3D", $"{beams} dầm (Tô màu lập thể theo tiết diện thực tế)"],
            ["Nét vẽ CAD 2D gốc", "Đã chiếu chồng khít lên dầm để kiểm tra mép dầm trùng nét"],
            ["Cột & Vách điểm tựa", $"{cols} cột + {walls} vách hiện hữu làm mốc neo không gian"],
            ["Thao tác hỗ trợ", "Xoay 360°, Phóng to/Thu nhỏ, Dời góc, Click xem thông số dầm"],
            ["Đường dẫn mở xem", outputPath]
        ];

        _formatter.RenderTable(["Thông số Mô Phỏng 3D", "Chi tiết kỹ thuật"], rows);
        _formatter.WriteSuccess("MÔ PHỎNG 3D HOÀN TẤT: Bạn có thể mở xem trực tiếp ngay bây giờ!");
    }
}
