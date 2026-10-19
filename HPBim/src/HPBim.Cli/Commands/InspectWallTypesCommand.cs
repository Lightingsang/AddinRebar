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
///     Queries available WallType symbols in Revit 2026.
/// </summary>
public sealed class InspectWallTypesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectWallTypesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "revit_wall_types.json"));

        _formatter.WriteBanner("Inspect Revit Wall Types", "Truy vấn các loại tường/vách trong Revit 2026");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Named Pipe Revit: {pipeName}");

            string script = """
                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType));

                var wallTypes = new List<object>();
                foreach (WallType wt in collector)
                {
                    double widthMm = Math.Round(wt.Width * 304.8, 1);
                    string kind = wt.Kind.ToString();

                    wallTypes.Add(new {
                        id = wt.Id.Value,
                        name = wt.Name,
                        kind = kind,
                        widthMm = widthMm
                    });
                }

                return new {
                    total = wallTypes.Count,
                    types = wallTypes
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Wall Types");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                string diags = res.Diagnostics != null && res.Diagnostics.Count > 0
                    ? string.Join("; ", res.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                    : "Không có diagnostics";
                _formatter.WriteError($"Lỗi Revit: {res.Message} -> {diags}");
                return 1;
            }

            string json = JsonSerializer.Serialize(res.Value.Value, JsonOptions);
            string dir = Path.GetDirectoryName(outputPath) ?? "scratch";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(outputPath, json);

            _formatter.WriteSuccess($"Đã trích xuất xong {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kết nối: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
