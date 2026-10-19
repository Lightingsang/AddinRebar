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
///     Queries all Line and Polyline entities on Plan A and reports their layers.
/// </summary>
public sealed class QueryPlanALinesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public QueryPlanALinesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_planA_lines_report.json"));

        _formatter.WriteBanner("Query Plan A Lines", "Quét toàn bộ đường nét trên Mặt Bằng Móng");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);

            string script = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                double originX = 470383.6;
                double originY = -37486.2;

                double minX = originX - 5000, maxX = originX + 52000;
                double minY = originY - 5000, maxY = originY + 32000;

                var lineLayers = new Dictionary<string, int>();
                var polyLayers = new Dictionary<string, int>();
                var sampleLines = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    if (ent is Line ln)
                    {
                        var p = ln.StartPoint;
                        if (p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY)
                        {
                            lineLayers[ln.Layer] = lineLayers.GetValueOrDefault(ln.Layer, 0) + 1;
                            if (sampleLines.Count < 20)
                            {
                                sampleLines.Add(new {
                                    lay = ln.Layer,
                                    len = Math.Round(ln.Length, 1),
                                    p1 = new { x = Math.Round(ln.StartPoint.X - originX, 1), y = Math.Round(ln.StartPoint.Y - originY, 1) },
                                    p2 = new { x = Math.Round(ln.EndPoint.X - originX, 1), y = Math.Round(ln.EndPoint.Y - originY, 1) }
                                });
                            }
                        }
                    }
                    else if (ent is Polyline pl)
                    {
                        var p = pl.StartPoint;
                        if (p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY)
                        {
                            polyLayers[pl.Layer] = polyLayers.GetValueOrDefault(pl.Layer, 0) + 1;
                        }
                    }
                    else if (ent is Polyline2d p2d)
                    {
                        polyLayers[p2d.Layer] = polyLayers.GetValueOrDefault(p2d.Layer, 0) + 1;
                    }
                }

                return new {
                    lineLayers = lineLayers,
                    polyLayers = polyLayers,
                    sampleLines = sampleLines
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Query Plan A Lines");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã quét xong: {outputPath}");
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
