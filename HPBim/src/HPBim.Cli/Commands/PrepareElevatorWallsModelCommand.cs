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
///     Pure C# command to prepare the Elevator Walls model for both cores (T1A & T1B).
/// </summary>
public sealed class PrepareElevatorWallsModelCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public PrepareElevatorWallsModelCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string outputPath = args.GetOption("output", Path.Combine("scratch", "elevator_walls_model.json"));

        _formatter.WriteBanner("Prepare Elevator Walls Model", "Chuẩn hóa dữ liệu Vách Thang Máy T1A & T1B sang mô hình BIM C#");

        var wallPanels = new List<object>();

        // Core 1: South Core T1A (Span 5-6, Span A-B)
        AddCorePanels(wallPanels, "T1A", "Thang Máy T1A (Nam)", 15200.0, 1200.0, 2400.0, 2600.0, 0.0, 2400.0);

        // Core 2: North Core T1B (Span 5-6, Span M-N)
        AddCorePanels(wallPanels, "T1B", "Thang Máy T1B (Bắc)", 15200.0, 26700.0, 2400.0, 2600.0, 25500.0, 27900.0);

        var model = new
        {
            totalCores = 2,
            wallType = "2_Wall_BTCT_D200",
            structuralUsage = "Shear",
            thicknessMm = 200.0,
            totalWalls = wallPanels.Count,
            totalSegments = wallPanels.Count * 2,
            walls = wallPanels
        };

        string json = JsonSerializer.Serialize(model, JsonOptions);
        string dir = Path.GetDirectoryName(outputPath) ?? "scratch";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);

        _formatter.WriteSuccess($"Đã chuẩn hóa {wallPanels.Count} diện vách ({wallPanels.Count * 2} phân đoạn) vào: {outputPath}");
        _formatter.WriteInfo("  - Core T1A (Nam): 4 diện vách (Tây, Bắc, Đông, Nam) quanh tim (15200, 1200)");
        _formatter.WriteInfo("  - Core T1B (Bắc): 4 diện vách (Tây, Bắc, Đông, Nam) quanh tim (15200, 26700)");
        _formatter.WriteInfo("  - Phân đoạn 1: Cổ Vách Móng (Đỉnh Đài Móng -1.600m -> Tầng 1 ±0.000, h = 1.600m)");
        _formatter.WriteInfo("  - Phân đoạn 2: Vách Tầng 1 (Tầng 1 ±0.000 -> Tầng 2 +4.300m, h = 4.300m)");

        return 0;
    }

    private static void AddCorePanels(
        List<object> list,
        string coreCode,
        string coreName,
        double cx,
        double cy,
        double lengthY,
        double lengthX,
        double yMin,
        double yMax)
    {
        double xMin = 13900.0;
        double xMax = 16500.0;

        // Panel 1: West (Trục 5)
        list.Add(CreateWallPanel(coreCode, $"{coreCode}_W1", "Tây (Trục 5)", coreName, xMin, yMin, xMin, yMax, 200.0));

        // Panel 2: North
        list.Add(CreateWallPanel(coreCode, $"{coreCode}_W2", "Bắc", coreName, xMin, yMax, xMax, yMax, 200.0));

        // Panel 3: East (Trục 6)
        list.Add(CreateWallPanel(coreCode, $"{coreCode}_W3", "Đông (Trục 6)", coreName, xMax, yMax, xMax, yMin, 200.0));

        // Panel 4: South
        list.Add(CreateWallPanel(coreCode, $"{coreCode}_W4", "Nam", coreName, xMax, yMin, xMin, yMin, 200.0));
    }

    private static object CreateWallPanel(
        string coreCode,
        string panelCode,
        string side,
        string coreName,
        double x0,
        double y0,
        double x1,
        double y1,
        double thickness)
    {
        double length = Math.Round(Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1 - y0, 2)), 1);

        return new
        {
            core = coreCode,
            panel = panelCode,
            side = side,
            x0Mm = x0,
            y0Mm = y0,
            x1Mm = x1,
            y1Mm = y1,
            lengthMm = length,
            thicknessMm = thickness,
            coVach = new
            {
                mark = $"CV_{panelCode}",
                baseLevel = "Đỉnh Đài Móng",
                topLevel = "Tầng 1 (±0.000)",
                baseOffsetMm = 0.0,
                topOffsetMm = 0.0,
                comments = $"Cổ Vách {coreName} - {side}"
            },
            vachTang1 = new
            {
                mark = $"V1_{panelCode}",
                baseLevel = "Tầng 1 (±0.000)",
                topLevel = "Tầng 2",
                baseOffsetMm = 0.0,
                topOffsetMm = 0.0,
                comments = $"Vách Tầng 1 {coreName} - {side}"
            }
        };
    }
}
