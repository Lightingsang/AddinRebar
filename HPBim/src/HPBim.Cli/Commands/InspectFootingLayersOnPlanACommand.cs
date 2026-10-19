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
///     Inspects all entity layers around each footing text on Plan A to find the exact outline geometry of pile caps.
/// </summary>
public sealed class InspectFootingLayersOnPlanACommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectFootingLayersOnPlanACommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_planA_footing_outlines.json"));

        _formatter.WriteBanner("Inspect Footing Outlines on Plan A", "Tìm layer và hình học đường bao đài móng quanh các nhãn ĐC");

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

                // Collect all entities in this bounding box
                var layers = new Dictionary<string, int>();
                var allPolys = new List<object>();
                var allLines = new List<object>();
                var allBlocks = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    if (ent is Polyline pl)
                    {
                        var pt = pl.StartPoint;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            layers[ent.Layer] = layers.GetValueOrDefault(ent.Layer, 0) + 1;
                            if (pl.Closed && pl.Area >= 0.5e6 && pl.Area <= 30e6)
                            {
                                allPolys.Add(new {
                                    handle = pl.Handle.ToString(),
                                    layer = ent.Layer,
                                    area = Math.Round(pl.Area / 1e6, 3),
                                    verts = pl.NumberOfVertices,
                                    cx = Math.Round(pl.Bounds.HasValue ? (pl.Bounds.Value.MinPoint.X + pl.Bounds.Value.MaxPoint.X)/2 - originX : 0, 1),
                                    cy = Math.Round(pl.Bounds.HasValue ? (pl.Bounds.Value.MinPoint.Y + pl.Bounds.Value.MaxPoint.Y)/2 - originY : 0, 1)
                                });
                            }
                        }
                    }
                    else if (ent is BlockReference br)
                    {
                        var pt = br.Position;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            layers[ent.Layer] = layers.GetValueOrDefault(ent.Layer, 0) + 1;
                            allBlocks.Add(new {
                                handle = br.Handle.ToString(),
                                name = br.Name,
                                layer = ent.Layer,
                                x = Math.Round(pt.X - originX, 1),
                                y = Math.Round(pt.Y - originY, 1)
                            });
                        }
                    }
                    else if (ent is Line ln)
                    {
                        var pt = ln.StartPoint;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            layers[ent.Layer] = layers.GetValueOrDefault(ent.Layer, 0) + 1;
                        }
                    }
                }

                return new {
                    layers = layers,
                    totalPolys = allPolys.Count,
                    polys = allPolys,
                    totalBlocks = allBlocks.Count,
                    blocks = allBlocks.Take(50).ToList()
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Plan A Footings");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã khảo sát xong: {outputPath}");
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
