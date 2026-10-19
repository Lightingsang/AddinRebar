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
///     Pure C# command to query active Revit Grids and Levels via Named Pipe.
///     Target: Revit 2026 main-thread execution.
/// </summary>
public sealed class QueryRevitGridsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public QueryRevitGridsCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "revit_grids_and_levels.json"));

        _formatter.WriteBanner("Query Revit Grids & Levels", $"Truy vấn lưới trục & cao độ từ Revit 2026 (Pipe: {pipeName})");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối thành công tới Named Pipe: {pipeName}");

            var payload = await ExecuteQueryScriptAsync(client, ct).ConfigureAwait(false);
            if (payload == null)
            {
                _formatter.WriteError("Không nhận được dữ liệu từ Revit.");
                return 1;
            }

            await SaveOutputFileAsync(payload.Value, outputPath, ct).ConfigureAwait(false);
            RenderSummary(payload.Value, outputPath);
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi truy vấn Revit: {ex.Message}");
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

    private async Task<JsonElement?> ExecuteQueryScriptAsync(IRevitPipeClient client, CancellationToken ct)
    {
        _formatter.WriteInfo("Đang thực thi C# Roslyn Script trên Revit main-thread...");
        string scriptCode = BuildScript();

        var request = new ExecuteRequest(
            Code: scriptCode,
            Transaction: TransactionModes.None,
            DryRun: false,
            TimeoutSeconds: 45,
            Label: "Query Revit Grids and Levels");

        var result = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
        if (result.IsError || result.Value == null)
        {
            throw new InvalidOperationException($"Lỗi truy vấn Revit: {result.Message}");
        }

        return result.Value;
    }

    private static string BuildScript() => """
        var grids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .Select(g => {
                var line = g.Curve as Line;
                double x0 = line != null ? Math.Round(line.GetEndPoint(0).X * 304.8, 1) : 0.0;
                double y0 = line != null ? Math.Round(line.GetEndPoint(0).Y * 304.8, 1) : 0.0;
                double x1 = line != null ? Math.Round(line.GetEndPoint(1).X * 304.8, 1) : 0.0;
                double y1 = line != null ? Math.Round(line.GetEndPoint(1).Y * 304.8, 1) : 0.0;
                return new {
                    name = g.Name,
                    x0, y0, x1, y1
                };
            })
            .OrderBy(g => g.name)
            .ToList();

        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .Select(l => new {
                name = l.Name,
                elevationMm = Math.Round(l.Elevation * 304.8, 1)
            })
            .OrderBy(l => l.elevationMm)
            .ToList();

        var foundationSymbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilySymbol>()
            .Select(s => new { family = s.Family.Name, name = s.Name })
            .ToList();

        var columnSymbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_StructuralColumns)
            .Cast<FamilySymbol>()
            .Select(s => new { family = s.Family.Name, name = s.Name })
            .ToList();

        var pileCount = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilyInstance>()
            .Count(fi => fi.Symbol.Name == "P400" || fi.Symbol.Family.Name == "4_STR_Coc_Tron");

        return new {
            grids,
            levels,
            foundationSymbols,
            columnSymbols,
            pileCount
        };
        """;

    private static async Task SaveOutputFileAsync(JsonElement payload, string path, CancellationToken ct)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(payload, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
    }

    private void RenderSummary(JsonElement payload, string outputPath)
    {
        int gridCount = payload.TryGetProperty("grids", out var g) && g.ValueKind == JsonValueKind.Array ? g.GetArrayLength() : 0;
        int levelCount = payload.TryGetProperty("levels", out var l) && l.ValueKind == JsonValueKind.Array ? l.GetArrayLength() : 0;
        int pileCount = payload.TryGetProperty("pileCount", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : 0;

        string[][] summaryRows =
        [
            ["Lưới trục (Grids)", $"{gridCount} trục"],
            ["Cao trình (Levels)", $"{levelCount} tầng"],
            ["Cọc ly tâm D400 (Piles)", $"{pileCount} cọc (Đã cắm)"],
            ["Tập tin xuất", outputPath]
        ];

        _formatter.RenderTable(["Hạng mục", "Thông tin Revit"], summaryRows);
        _formatter.WriteSuccess($"Truy vấn hoàn tất! Đã lưu kết quả tại: {outputPath}");
    }
}
