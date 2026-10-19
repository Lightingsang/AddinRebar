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
///     Extracts all pile caps and grade beams from the True Foundation Plan frame (Origin: Grid 1-A at X=470383.6, Y=-37486.2).
/// </summary>
public sealed class ExtractTrueFoundationElementsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public ExtractTrueFoundationElementsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_true_foundation_elements.json"));

        _formatter.WriteBanner("Extract True Foundation Elements", "Trích xuất đài móng và giằng móng từ Mặt Bằng Móng chuẩn");

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

                // Origin chuẩn của Mặt Bằng Móng (Giao điểm Trục 1 - Trục A):
                double originX = 470383.6;
                double originY = -37486.2;

                // Phạm vi mặt bằng móng:
                double minX = originX - 15000, maxX = originX + 55000;
                double minY = originY - 15000, maxY = originY + 35000;

                var footings = new List<object>();
                var beamLines = new List<object>();
                var beamTexts = new List<object>();
                var footingTexts = new List<object>();

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
                            if (pl.Closed && pl.Area >= 0.8e6 && pl.Area <= 30e6)
                            {
                                var pts = new List<object>();
                                double cx = 0, cy = 0;
                                for (int i = 0; i < pl.NumberOfVertices; i++)
                                {
                                    var p = pl.GetPoint2dAt(i);
                                    double rx = Math.Round(p.X - originX, 1);
                                    double ry = Math.Round(p.Y - originY, 1);
                                    pts.Add(new { x = rx, y = ry });
                                    cx += rx;
                                    cy += ry;
                                }
                                cx = Math.Round(cx / pl.NumberOfVertices, 1);
                                cy = Math.Round(cy / pl.NumberOfVertices, 1);

                                footings.Add(new {
                                    handle = pl.Handle.ToString(),
                                    layer = lay,
                                    areaM2 = Math.Round(pl.Area / 1e6, 3),
                                    verts = pl.NumberOfVertices,
                                    cx = cx,
                                    cy = cy,
                                    points = pts
                                });
                            }
                        }
                    }
                    else if (ent is Line ln)
                    {
                        var p1 = ln.StartPoint;
                        var p2 = ln.EndPoint;
                        if (p1.X >= minX && p1.X <= maxX && p1.Y >= minY && p1.Y <= maxY)
                        {
                            if (lay.Contains("DK") || lay.Contains("DAM") || lay.Contains("GIANG") || lay.Equals("A_Bao_DK_S_0.2", StringComparison.OrdinalIgnoreCase))
                            {
                                beamLines.Add(new {
                                    handle = ln.Handle.ToString(),
                                    layer = lay,
                                    len = Math.Round(ln.Length, 1),
                                    x1 = Math.Round(p1.X - originX, 1),
                                    y1 = Math.Round(p1.Y - originY, 1),
                                    x2 = Math.Round(p2.X - originX, 1),
                                    y2 = Math.Round(p2.Y - originY, 1)
                                });
                            }
                        }
                    }
                    else if (ent is DBText dt)
                    {
                        var pt = dt.Position;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            string t = dt.TextString.Trim();
                            double rx = Math.Round(pt.X - originX, 1);
                            double ry = Math.Round(pt.Y - originY, 1);

                            if (t.StartsWith("ĐC") || t.StartsWith("DC") || t.StartsWith("ÑC"))
                            {
                                footingTexts.Add(new { text = t, layer = lay, x = rx, y = ry });
                            }
                            else if (t.Contains("DK") || t.Contains("GM") || t.Contains("DX") || t.Contains("(") && t.Contains("x") && t.Contains(")"))
                            {
                                beamTexts.Add(new { text = t, layer = lay, x = rx, y = ry });
                            }
                        }
                    }
                    else if (ent is MText mt)
                    {
                        var pt = mt.Location;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            string t = mt.Contents.Trim();
                            double rx = Math.Round(pt.X - originX, 1);
                            double ry = Math.Round(pt.Y - originY, 1);

                            if (t.StartsWith("ĐC") || t.StartsWith("DC") || t.StartsWith("ÑC"))
                            {
                                footingTexts.Add(new { text = t, layer = lay, x = rx, y = ry });
                            }
                            else if (t.Contains("DK") || t.Contains("GM") || t.Contains("DX") || t.Contains("(") && t.Contains("x") && t.Contains(")"))
                            {
                                beamTexts.Add(new { text = t, layer = lay, x = rx, y = ry });
                            }
                        }
                    }
                }

                return new {
                    totalFootings = footings.Count,
                    totalFootingTexts = footingTexts.Count,
                    totalBeamLines = beamLines.Count,
                    totalBeamTexts = beamTexts.Count,
                    footings = footings,
                    footingTexts = footingTexts,
                    beamTexts = beamTexts,
                    beamLines = beamLines.Take(100).ToList()
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Extract True Foundation");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã trích xuất thành công toàn bộ phần tử móng chuẩn: {outputPath}");
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
