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
///     Queries and extracts pile cap (đài móng) polylines and text annotations from AutoCAD 2026.
/// </summary>
public sealed class ExtractFootingsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public ExtractFootingsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_footings_raw.json"));

        _formatter.WriteBanner("Extract Footings from CAD", "Quét đường bao và nhãn đài móng từ AutoCAD 2026");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối thành công tới Named Pipe: {pipeName}");

            string script = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                // Mat Bang Mong & Da Kieng:
                // Goc truc 1 - A: X0 = 218352.1, Y0 = -99383.2
                double originX = 218352.1;
                double originY = -99383.2;

                var lines = new List<object>();
                var polylines = new List<object>();

                if (bt.Has("DK"))
                {
                    // Tim vi tri block DK trong model space
                    Matrix3d transform = Matrix3d.Identity;
                    foreach (ObjectId id in ms)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead);
                        if (ent is BlockReference br)
                        {
                            var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                            if (btr.Name.Equals("DK", StringComparison.OrdinalIgnoreCase))
                            {
                                transform = br.BlockTransform;
                                break;
                            }
                        }
                    }

                    var btrDK = (BlockTableRecord)tr.GetObject(bt["DK"], OpenMode.ForRead);
                    foreach (ObjectId id in btrDK)
                    {
                        var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (e == null || !e.Layer.Equals("A_Bao_DK_S_0.2", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (e is Line ln)
                        {
                            var p1 = ln.StartPoint.TransformBy(transform);
                            var p2 = ln.EndPoint.TransformBy(transform);

                            lines.Add(new {
                                handle = ln.Handle.ToString(),
                                rx1 = Math.Round(p1.X - originX, 1),
                                ry1 = Math.Round(p1.Y - originY, 1),
                                rx2 = Math.Round(p2.X - originX, 1),
                                ry2 = Math.Round(p2.Y - originY, 1),
                                len = Math.Round(ln.Length, 1)
                            });
                        }
                        else if (e is Polyline pl)
                        {
                            var pts = new List<object>();
                            for (int i = 0; i < pl.NumberOfVertices; i++)
                            {
                                var pt3d = new Point3d(pl.GetPoint2dAt(i).X, pl.GetPoint2dAt(i).Y, 0).TransformBy(transform);
                                pts.Add(new {
                                    rx = Math.Round(pt3d.X - originX, 1),
                                    ry = Math.Round(pt3d.Y - originY, 1)
                                });
                            }
                            polylines.Add(new {
                                handle = pl.Handle.ToString(),
                                closed = pl.Closed,
                                areaM2 = Math.Round(pl.Area / 1000000.0, 2),
                                points = pts
                            });
                        }
                    }
                }

                var texts = new List<object>();
                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is DBText dt)
                    {
                        double rx = Math.Round(dt.Position.X - originX, 1);
                        double ry = Math.Round(dt.Position.Y - originY, 1);
                        if (rx >= -5000 && rx <= 55000 && ry >= -5000 && ry <= 35000)
                        {
                            texts.Add(new {
                                text = dt.TextString.Trim(),
                                layer = dt.Layer,
                                rx = rx,
                                ry = ry
                            });
                        }
                    }
                    else if (ent is MText mt)
                    {
                        double rx = Math.Round(mt.Location.X - originX, 1);
                        double ry = Math.Round(mt.Location.Y - originY, 1);
                        if (rx >= -5000 && rx <= 55000 && ry >= -5000 && ry <= 35000)
                        {
                            texts.Add(new {
                                text = mt.Contents.Trim(),
                                layer = mt.Layer,
                                rx = rx,
                                ry = ry
                            });
                        }
                    }
                }

                return new {
                    totalLines = lines.Count,
                    totalPolylines = polylines.Count,
                    totalTexts = texts.Count,
                    allPolylines = polylines,
                    texts = texts,
                    lines = lines
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Extract Footings");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, JsonSerializer.Serialize(res.Value.Value, JsonOptions));
            _formatter.WriteSuccess($"Đã trích xuất thành công dữ liệu đài móng vào: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kết nối AutoCAD: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
