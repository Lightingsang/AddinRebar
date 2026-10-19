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
///     Queries all true closed pile cap polylines from layers MONG, Bao Mong, BaoDM, DC-*.
/// </summary>
public sealed class QueryTrueFootingPolylinesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public QueryTrueFootingPolylinesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_true_footing_polylines.json"));

        _formatter.WriteBanner("True Footing Polylines", "Trích xuất chính xác đường bao đài móng từ layer MONG, BaoDM, DC");

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

                // Origin của Mặt Bằng Móng & Giằng Móng:
                // Trục 1 - Trục A trên mặt bằng cọc: X=302352.1, Y=-38575.9
                // Trục 1 - Trục A trên mặt bằng đài móng: cần xác định chính xác
                var targetLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "MONG", "Bao Mong", "BaoDM", "MONGC-S", "Bao-A", "Bao", "DC"
                };

                var footings = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    string lay = ent.Layer;
                    bool match = targetLayers.Contains(lay) || lay.StartsWith("DC-", StringComparison.OrdinalIgnoreCase);
                    if (!match) continue;

                    if (ent is Polyline pl && pl.Closed)
                    {
                        double areaM2 = Math.Round(pl.Area / 1e6, 3);
                        // Chỉ lấy các đài có diện tích hợp lý (từ 0.8m2 đến 30m2)
                        if (areaM2 >= 0.8 && areaM2 <= 40.0)
                        {
                            var pts = new List<object>();
                            double cx = 0, cy = 0;
                            for (int i = 0; i < pl.NumberOfVertices; i++)
                            {
                                var pt = pl.GetPoint2dAt(i);
                                pts.Add(new { x = Math.Round(pt.X, 1), y = Math.Round(pt.Y, 1) });
                                cx += pt.X;
                                cy += pt.Y;
                            }
                            cx = Math.Round(cx / pl.NumberOfVertices, 1);
                            cy = Math.Round(cy / pl.NumberOfVertices, 1);

                            footings.Add(new {
                                handle = pl.Handle.ToString(),
                                layer = lay,
                                areaM2 = areaM2,
                                verts = pl.NumberOfVertices,
                                cx = cx,
                                cy = cy,
                                points = pts
                            });
                        }
                    }
                }

                return new {
                    totalFootings = footings.Count,
                    footings = footings
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Query True Footings");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã trích xuất thành công {res.Value.Value.GetProperty("totalFootings").GetInt32()} đài móng vào: {outputPath}");
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
