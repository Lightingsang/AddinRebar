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
///     Queries all entities (polylines, lines, blocks) in the pile layout plan area (X: 300000..360000, Y: -45000..-5000).
/// </summary>
public sealed class InspectPileAreaFootingsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectPileAreaFootingsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_pile_area_footings.json"));

        _formatter.WriteBanner("Inspect Pile Area Footings", "Khảo sát đường bao đài móng tại khu vực tim cọc (X: 300k..360k)");

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

                double minX = 300000, maxX = 360000;
                double minY = -45000, maxY = -5000;

                var layerCounts = new Dictionary<string, int>();
                var closedPolys = new List<object>();
                var blockRefs = new List<object>();
                var texts = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    string lay = ent.Layer;

                    if (ent is Polyline pl)
                    {
                        var pt = pl.StartPoint;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            layerCounts[lay] = layerCounts.GetValueOrDefault(lay, 0) + 1;
                            if (pl.Closed && pl.Area > 0.5e6 && pl.Area < 50e6)
                            {
                                var pts = new List<object>();
                                for (int i = 0; i < pl.NumberOfVertices; i++)
                                {
                                    var p = pl.GetPoint2dAt(i);
                                    pts.Add(new { x = Math.Round(p.X, 1), y = Math.Round(p.Y, 1) });
                                }
                                closedPolys.Add(new {
                                    handle = pl.Handle.ToString(),
                                    layer = lay,
                                    areaM2 = Math.Round(pl.Area / 1e6, 2),
                                    verts = pl.NumberOfVertices,
                                    points = pts
                                });
                            }
                        }
                    }
                    else if (ent is BlockReference br)
                    {
                        var pt = br.Position;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            layerCounts[lay] = layerCounts.GetValueOrDefault(lay, 0) + 1;
                            blockRefs.Add(new {
                                handle = br.Handle.ToString(),
                                block = br.Name,
                                layer = lay,
                                x = Math.Round(pt.X, 1),
                                y = Math.Round(pt.Y, 1)
                            });
                        }
                    }
                    else if (ent is DBText dt)
                    {
                        var pt = dt.Position;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            string t = dt.TextString.Trim();
                            if (t.Contains("ĐC") || t.Contains("DC") || t.Contains("MÓNG") || t.Contains("ĐÀI"))
                            {
                                texts.Add(new {
                                    text = t,
                                    layer = lay,
                                    x = Math.Round(pt.X, 1),
                                    y = Math.Round(pt.Y, 1)
                                });
                            }
                        }
                    }
                }

                return new {
                    layerCounts = layerCounts,
                    totalClosedPolys = closedPolys.Count,
                    closedPolys = closedPolys,
                    totalBlockRefs = blockRefs.Count,
                    blockRefs = blockRefs.Take(50).ToList(),
                    totalTexts = texts.Count,
                    texts = texts
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Pile Area");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã khảo sát xong khu vực tim cọc: {outputPath}");
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
