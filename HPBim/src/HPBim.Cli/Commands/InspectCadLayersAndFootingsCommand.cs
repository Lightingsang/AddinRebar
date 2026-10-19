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
///     Inspects all AutoCAD layers and searches for pile cap outlines across all blocks and model space.
/// </summary>
public sealed class InspectCadLayersAndFootingsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectCadLayersAndFootingsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_layers_and_footings_search.json"));

        _formatter.WriteBanner("Inspect CAD Layers & Footings", "Tìm layer và đối tượng đài móng trong DWG");

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
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                var layers = new List<string>();
                foreach (ObjectId lid in lt)
                {
                    var ltr = (LayerTableRecord)tr.GetObject(lid, OpenMode.ForRead);
                    layers.Add(ltr.Name);
                }

                // Check entities in ModelSpace and Block DK with layer containing DC or ĐC or MONG or DAI
                var dcEntities = new List<object>();

                void ScanRecord(BlockTableRecord btr, string recName, Matrix3d xform)
                {
                    foreach (ObjectId id in btr)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        string l = ent.Layer.ToLowerInvariant();
                        if (l.Contains("dc") || l.Contains("đc") || l.Contains("mong") || l.Contains("đài") || l.Contains("dai"))
                        {
                            if (ent is Polyline pl)
                            {
                                var pts = new List<object>();
                                for (int i = 0; i < pl.NumberOfVertices; i++)
                                {
                                    var p = new Point3d(pl.GetPoint2dAt(i).X, pl.GetPoint2dAt(i).Y, 0).TransformBy(xform);
                                    pts.Add(new { x = Math.Round(p.X, 1), y = Math.Round(p.Y, 1) });
                                }
                                dcEntities.Add(new {
                                    container = recName,
                                    type = "Polyline",
                                    layer = ent.Layer,
                                    closed = pl.Closed,
                                    area = Math.Round(pl.Area / 1e6, 2),
                                    verts = pl.NumberOfVertices,
                                    points = pts
                                });
                            }
                            else if (ent is DBText dt)
                            {
                                var p = dt.Position.TransformBy(xform);
                                dcEntities.Add(new {
                                    container = recName,
                                    type = "DBText",
                                    layer = ent.Layer,
                                    text = dt.TextString.Trim(),
                                    x = Math.Round(p.X, 1),
                                    y = Math.Round(p.Y, 1)
                                });
                            }
                            else if (ent is Line ln)
                            {
                                var p1 = ln.StartPoint.TransformBy(xform);
                                var p2 = ln.EndPoint.TransformBy(xform);
                                dcEntities.Add(new {
                                    container = recName,
                                    type = "Line",
                                    layer = ent.Layer,
                                    len = Math.Round(ln.Length, 1),
                                    x1 = Math.Round(p1.X, 1),
                                    y1 = Math.Round(p1.Y, 1),
                                    x2 = Math.Round(p2.X, 1),
                                    y2 = Math.Round(p2.Y, 1)
                                });
                            }
                        }
                    }
                }

                ScanRecord(ms, "ModelSpace", Matrix3d.Identity);

                if (bt.Has("DK"))
                {
                    Matrix3d xf = Matrix3d.Identity;
                    foreach (ObjectId id in ms)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead);
                        if (ent is BlockReference br)
                        {
                            var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                            if (btr.Name.Equals("DK", StringComparison.OrdinalIgnoreCase))
                            {
                                xf = br.BlockTransform;
                                break;
                            }
                        }
                    }
                    var btrDK = (BlockTableRecord)tr.GetObject(bt["DK"], OpenMode.ForRead);
                    ScanRecord(btrDK, "Block_DK", xf);
                }

                return new {
                    totalLayers = layers.Count,
                    layers = layers,
                    totalDcEntities = dcEntities.Count,
                    dcEntities = dcEntities
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect CAD Layers");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã lưu kết quả khảo sát layer và đài móng vào: {outputPath}");
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
