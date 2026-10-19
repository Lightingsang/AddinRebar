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
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Pure C# command to model Foundation Level and 27 Grids into Revit 2026 via Named Pipe.
///     Supports dry-run preview and permanent commit. Complies 100% with Clean Code Governance.
/// </summary>
public sealed class GenerateFoundationCommand
{
    private const double ExtensionMm = 3000.0;
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public GenerateFoundationCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string inputPath = args.GetOption("input", Path.Combine("scratch", "piles_true_bim_model.json"));
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        bool dryRun = args.HasFlag("dry-run");

        if (!File.Exists(inputPath))
        {
            _formatter.WriteError($"Tập tin mô hình không tồn tại: {inputPath}");
            return 2;
        }

        string modeLabel = dryRun ? "DRY-RUN (Thử nghiệm & Rollback)" : "COMMIT (Ghi thực tế)";
        _formatter.WriteBanner("Generate Foundation Grids", $"Tạo 27 Trục & Level Móng trong Revit 2026 [{modeLabel}]");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối thành công tới Revit Named Pipe: {pipeName}");

            var (gridPayload, xCount, yCount) = LoadGridPayload(inputPath);
            _formatter.WriteInfo($"Đã tải dữ liệu hình học: {xCount} trục X (1..13), {yCount} trục Y (A..N).");

            var result = await DispatchRevitScriptAsync(client, gridPayload, dryRun, ct).ConfigureAwait(false);
            RenderExecutionResult(result, dryRun, xCount, yCount);
            bool isOk = dryRun ? (!result.IsError && result.RolledBack) : !result.IsError;
            return isOk ? 0 : 1;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi Revit: {ex.Message}");
            return 1;
        }
        finally
        {
            if (_injectedClient == null)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static (object Payload, int XCount, int YCount) LoadGridPayload(string inputPath)
    {
        string jsonText = File.ReadAllText(inputPath);
        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        double originX = root.GetProperty("origin").GetProperty("cad_x_mm").GetDouble();
        double originY = root.GetProperty("origin").GetProperty("cad_y_mm").GetDouble();

        var xGrids = root.GetProperty("grids").GetProperty("x").EnumerateArray()
            .Select(g => new { Name = g.GetProperty("name").GetString() ?? "", CadX = g.GetProperty("x").GetDouble() })
            .ToList();

        var yGrids = root.GetProperty("grids").GetProperty("y").EnumerateArray()
            .Select(g => new { Name = g.GetProperty("name").GetString() ?? "", CadY = g.GetProperty("y").GetDouble() })
            .ToList();

        double minX = xGrids.Min(g => g.CadX) - originX;
        double maxX = xGrids.Max(g => g.CadX) - originX;
        double minY = yGrids.Min(g => g.CadY) - originY;
        double maxY = yGrids.Max(g => g.CadY) - originY;

        var gridLines = new List<object>();

        foreach (var gx in xGrids)
        {
            // Luôn đưa về số nguyên chẵn (loại bỏ hoàn toàn số lẻ thập phân từ CAD)
            double rx = Math.Round(gx.CadX - originX);
            gridLines.Add(new
            {
                name = gx.Name,
                x0 = rx,
                y0 = minY - ExtensionMm,
                x1 = rx,
                y1 = maxY + ExtensionMm
            });
        }

        foreach (var gy in yGrids)
        {
            // Luôn đưa về số nguyên chẵn (loại bỏ hoàn toàn số lẻ thập phân từ CAD)
            double ry = Math.Round(gy.CadY - originY);
            gridLines.Add(new
            {
                name = gy.Name,
                x0 = minX - ExtensionMm,
                y0 = ry,
                x1 = maxX + ExtensionMm,
                y1 = ry
            });
        }

        return (new
        {
            levelName = "Tầng Móng",
            levelElevationM = -1.0,
            grids = gridLines
        }, xGrids.Count, yGrids.Count);
    }

    private async Task<ExecuteResult> DispatchRevitScriptAsync(
        IRevitPipeClient client, object payload, bool dryRun, CancellationToken ct)
    {
        _formatter.WriteInfo("Đang gửi C# Roslyn Script sang Revit 2026...");
        string scriptCode = BuildRevitScript();

        string payloadJson = JsonSerializer.Serialize(payload);
        using var argsDoc = JsonDocument.Parse(payloadJson);

        var request = new ExecuteRequest(
            Code: scriptCode,
            Transaction: TransactionModes.Auto,
            DryRun: dryRun,
            TimeoutSeconds: 45,
            Label: "Create Foundation Grids and Level",
            Args: argsDoc.RootElement.Clone());

        return await client.ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    private void RenderExecutionResult(ExecuteResult result, bool dryRun, int xCount, int yCount)
    {
        if (result.IsError)
        {
            string diags = result.Diagnostics != null && result.Diagnostics.Count > 0
                ? string.Join("; ", result.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                : "Không có diagnostics";
            _formatter.WriteError($"Revit trả về lỗi: {result.Message} -> {diags}");
            return;
        }

        string[][] summaryRows =
        [
            ["Thời gian thực thi", $"{result.DurationMs} ms"],
            ["Trục X (1..13)", $"{xCount} trục"],
            ["Trục Y (A..N)", $"{yCount} trục"],
            ["Phần tử tạo mới (Added)", result.Changed.Added.ToString()],
            ["Trạng thái Rollback", result.RolledBack ? "CÓ (Transaction Rolled Back sạch sẽ)" : "KHÔNG (Committed vào Revit)"]
        ];
        _formatter.RenderTable(["Chỉ số", "Kết quả thực tế"], summaryRows);

        if (dryRun)
        {
            if (!result.IsError && result.RolledBack)
            {
                _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Mô hình đã được dựng thử nghiệm và rollback an toàn 100%!");
            }
            else if (!result.RolledBack)
            {
                _formatter.WriteError("CẢNH BÁO NGUY HIỂM: Dry-run thất bại! Revit bridge KHÔNG xác nhận rollback.");
            }
            else
            {
                _formatter.WriteError($"DRY-RUN THẤT BẠI: {result.Message}");
            }
        }
        else if (!result.IsError)
        {
            _formatter.WriteSuccess("COMMIT THÀNH CÔNG: 27 Lưới Trục và Level Móng đã được ghi nhận vĩnh viễn vào Revit 2026!");
        }
        else
        {
            _formatter.WriteError($"COMMIT THẤT BẠI: {result.Message}");
        }
    }

    private static string BuildRevitScript() => """
        double Ft(double mm) => mm / 304.8;
        XYZ ToXyz(double xMm, double yMm, double zMm = 0.0) => new XYZ(Ft(xMm), Ft(yMm), Ft(zMm));

        // 1. Tạo Level Móng
        string lvlName = args.Str("levelName", "Tầng Móng");
        double lvlElevM = args.Double("levelElevationM", -1.0);
        double lvlElevFt = lvlElevM * (1000.0 / 304.8);

        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        if (!levels.TryGetValue(lvlName, out var foundationLevel))
        {
            foundationLevel = Level.Create(doc, lvlElevFt);
            foundationLevel.Name = lvlName;
            levels[lvlName] = foundationLevel;
        }

        var pStory = foundationLevel.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY);
        if (pStory != null && !pStory.IsReadOnly) pStory.Set(1);

        // 2. Tạo 27 Lưới Trục
        var existingGrids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .ToDictionary(g => g.Name, g => g, StringComparer.OrdinalIgnoreCase);

        var gridItems = args.List("grids");
        int createdGrids = 0;

        foreach (var item in gridItems)
        {
            string name = item.Require("name");
            if (existingGrids.ContainsKey(name)) continue;

            var p0 = ToXyz(item.Double("x0"), item.Double("y0"), 0);
            var p1 = ToXyz(item.Double("x1"), item.Double("y1"), 0);

            if (p0.DistanceTo(p1) < 0.001) continue;

            var line = Line.CreateBound(p0, p1);
            var grid = Grid.Create(doc, line);
            grid.Name = name;
            existingGrids[name] = grid;
            createdGrids++;
        }

        return new
        {
            level = foundationLevel.Name,
            levelElevation = lvlElevM,
            totalGridsInPayload = gridItems.Count,
            createdGrids = createdGrids
        };
        """;
}
