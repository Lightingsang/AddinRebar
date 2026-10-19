namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Cli.Scripts;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Pure C# command to extract pile stakeout and grid system directly from AutoCAD via Named Pipe.
///     Complies 100% with HP Clean Code Governance.
/// </summary>
public sealed class ExtractCadCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public ExtractCadCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "piles_true_bim_model.json"));
        string csvPath = args.GetOption("csv", Path.Combine("scratch", "Bang_Toa_Do_Tim_Coc_Chuan_Ky_Thuat.csv"));

        _formatter.WriteBanner("Extract CAD Piles & Grids", $"Trích xuất tim cọc 100% C# từ AutoCAD (Pipe: {pipeName})");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await ConnectAndVerifyPipeAsync(client, pipeName, ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối thành công tới Named Pipe: {pipeName}");

            var payload = await ExecuteExtractionScriptAsync(client, ct).ConfigureAwait(false);
            if (payload == null)
            {
                _formatter.WriteError("Không thể trích xuất dữ liệu từ AutoCAD (kết quả rỗng).");
                return 1;
            }

            await SaveOutputFileAsync(payload.Value, outputPath, ct).ConfigureAwait(false);
            await SaveCsvScheduleAsync(payload.Value, csvPath, ct).ConfigureAwait(false);
            RenderExtractionSummary(payload.Value, outputPath, csvPath);
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi trích xuất CAD: {ex.Message}");
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

    private static async Task ConnectAndVerifyPipeAsync(IRevitPipeClient client, string pipeName, CancellationToken ct)
    {
        await client.ConnectAsync(ct).ConfigureAwait(false);
        bool pingOk = await client.PingAsync(ct).ConfigureAwait(false);
        if (!pingOk)
        {
            throw new InvalidOperationException($"Không nhận được phản hồi Ping từ Named Pipe '{pipeName}'.");
        }
    }

    private async Task<JsonElement?> ExecuteExtractionScriptAsync(IRevitPipeClient client, CancellationToken ct)
    {
        _formatter.WriteInfo("Đang thực thi C# Roslyn Script trên AutoCAD main-thread...");
        string scriptCode = CadExtractionScript.BuildScript();

        var request = new ExecuteRequest(
            Code: scriptCode,
            Transaction: TransactionModes.None,
            DryRun: false,
            TimeoutSeconds: 60,
            Label: "Extract Piles and Grids");

        var result = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
        if (result.IsError)
        {
            string diags = result.Diagnostics != null && result.Diagnostics.Count > 0
                ? string.Join("; ", result.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                : "Không có diagnostics";
            throw new InvalidOperationException($"Lỗi thực thi AutoCAD script: {result.Message} -> {diags}");
        }

        return result.Value;
    }

    private static async Task SaveOutputFileAsync(JsonElement payload, string outputPath, CancellationToken ct)
    {
        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string jsonText = JsonSerializer.Serialize(payload, JsonOptions);
        await File.WriteAllTextAsync(outputPath, jsonText, ct).ConfigureAwait(false);
    }

    private static async Task SaveCsvScheduleAsync(JsonElement payload, string csvPath, CancellationToken ct)
    {
        if (!payload.TryGetProperty("piles", out var pilesArr)) return;

        var sb = new StringBuilder();
        sb.AppendLine("STT,Số Hiệu Cọc,CAD Handle,Tọa độ CAD X (mm),Tọa độ CAD Y (mm),Tọa độ Revit X (mm),Tọa độ Revit Y (mm),Độ lệch Text CAD (mm)");

        foreach (var p in pilesArr.EnumerateArray())
        {
            int stt = p.GetProperty("stt").GetInt32();
            string name = p.GetProperty("name").GetString() ?? "";
            string handle = p.GetProperty("handle").GetString() ?? "";
            double cadX = p.GetProperty("cad_x").GetDouble();
            double cadY = p.GetProperty("cad_y").GetDouble();
            double revitX = p.GetProperty("revit_x").GetDouble();
            double revitY = p.GetProperty("revit_y").GetDouble();
            double dist = p.GetProperty("dist_to_text").GetDouble();

            sb.AppendLine($"{stt},{name},{handle},{cadX:F1},{cadY:F1},{revitX:F1},{revitY:F1},{dist:F1}");
        }

        string? dir = Path.GetDirectoryName(csvPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8, ct).ConfigureAwait(false);
    }

    private void RenderExtractionSummary(JsonElement root, string outputPath, string csvPath)
    {
        int totalPiles = root.TryGetProperty("totalPiles", out var tp) ? tp.GetInt32() : 0;
        int xCount = 0;
        int yCount = 0;

        if (root.TryGetProperty("grids", out var grids))
        {
            if (grids.TryGetProperty("x", out var xArr)) xCount = xArr.GetArrayLength();
            if (grids.TryGetProperty("y", out var yArr)) yCount = yArr.GetArrayLength();
        }

        string[][] summaryRows =
        [
            ["Số lượng cọc trích xuất", totalPiles.ToString()],
            ["Hệ trục X (Trục số)", xCount.ToString()],
            ["Hệ trục Y (Trục chữ)", yCount.ToString()],
            ["Tập tin xuất JSON", outputPath],
            ["Tập tin xuất CSV", csvPath]
        ];
        _formatter.RenderTable(["Hạng mục", "Thông tin"], summaryRows);
        _formatter.WriteSuccess($"Trích xuất hoàn tất! File JSON: {outputPath} | CSV: {csvPath}");
    }
}
