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
///     Pure C# command to inspect Story 2 Framing plan (Sàn Tầng 2) from AutoCAD DWG.
/// </summary>
public sealed class InspectCadStory2FramingCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectCadStory2FramingCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_story2_framing_raw.json"));

        _formatter.WriteBanner("Inspect Story 2 Framing", "Khảo sát mặt bằng kết cấu dầm sàn Tầng 2 từ DWG (100% C#)");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Named Pipe AutoCAD: {pipeName}");

            var req = new ExecuteRequest(BuildCadInspectionScript(), TransactionModes.None, false, 60, "Inspect Story 2 Framing");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                string diags = res.Diagnostics != null && res.Diagnostics.Count > 0
                    ? string.Join("; ", res.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                    : "Không có diagnostics";
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message} -> {diags}");
                return 1;
            }

            string json = JsonSerializer.Serialize(res.Value.Value, JsonOptions);
            string dir = Path.GetDirectoryName(outputPath) ?? "scratch";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);

            _formatter.WriteSuccess($"Đã lưu kết quả khảo sát Sàn Tầng 2 tại: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi: {ex.Message}");
            return 2;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string BuildCadInspectionScript()
    {
        return """
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            // Bounds of Story 2 Framing plan: X in [200000..280000], Y in [-240000..-190000]
            double minX = 200000.0, maxX = 280000.0;
            double minY = -240000.0, maxY = -190000.0;

            var gridBubbles = new List<object>();
            var beamTexts = new List<object>();
            var slabTexts = new List<object>();
            var beamLines = new List<object>();

            foreach (ObjectId id in ms)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;

                if (ent is BlockReference br)
                {
                    var pt = br.Position;
                    if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                    {
                        var atts = new List<string>();
                        foreach (ObjectId aid in br.AttributeCollection)
                        {
                            var att = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                            atts.Add(att.TextString.Trim());
                        }
                        if (atts.Count > 0)
                        {
                            gridBubbles.Add(new {
                                name = br.Name,
                                layer = br.Layer,
                                x = Math.Round(pt.X, 1),
                                y = Math.Round(pt.Y, 1),
                                text = atts[0],
                                atts = atts
                            });
                        }
                    }
                }
                else if (ent is DBText dt)
                {
                    double x = dt.Position.X, y = dt.Position.Y;
                    if (x >= minX && x <= maxX && y >= minY && y <= maxY)
                    {
                        string txt = dt.TextString.Trim();
                        var obj = new { text = txt, layer = dt.Layer, x = Math.Round(x, 1), y = Math.Round(y, 1) };
                        string u = txt.ToUpper();
                        if (u.StartsWith("D") || u.Contains("200X") || u.Contains("300X") || u.Contains("400X"))
                            beamTexts.Add(obj);
                        else if (u.Contains("SÀN") || u.Contains("SAØN") || u.Contains("DÀY") || u.Contains("DAØY") || u.Contains("COTE") || u.Contains("S1") || u.Contains("S2"))
                            slabTexts.Add(obj);
                    }
                }
                else if (ent is MText mt)
                {
                    double x = mt.Location.X, y = mt.Location.Y;
                    if (x >= minX && x <= maxX && y >= minY && y <= maxY)
                    {
                        string txt = mt.Contents.Trim();
                        var obj = new { text = txt, layer = mt.Layer, x = Math.Round(x, 1), y = Math.Round(y, 1) };
                        string u = txt.ToUpper();
                        if (u.StartsWith("D") || u.Contains("200X") || u.Contains("300X") || u.Contains("400X"))
                            beamTexts.Add(obj);
                        else if (u.Contains("SÀN") || u.Contains("SAØN") || u.Contains("DÀY") || u.Contains("DAØY") || u.Contains("COTE") || u.Contains("S1") || u.Contains("S2"))
                            slabTexts.Add(obj);
                    }
                }
                else if (ent is Line ln)
                {
                    double sx = ln.StartPoint.X, sy = ln.StartPoint.Y;
                    double ex = ln.EndPoint.X, ey = ln.EndPoint.Y;
                    if ((sx >= minX && sx <= maxX && sy >= minY && sy <= maxY) ||
                        (ex >= minX && ex <= maxX && ey >= minY && ey <= maxY))
                    {
                        string l = ent.Layer;
                        if (l == "A_Bao_DK_S_0.2" || l == "DK-X" || l == "DK-Y" || l == "G1-T2" || l == "G2-T2")
                        {
                            beamLines.Add(new {
                                layer = l,
                                startX = Math.Round(sx, 1),
                                startY = Math.Round(sy, 1),
                                endX = Math.Round(ex, 1),
                                endY = Math.Round(ey, 1)
                            });
                        }
                    }
                }
                else if (ent is Polyline pl)
                {
                    try
                    {
                        var ext = pl.GeometricExtents;
                        if (ext.MaxPoint.X >= minX && ext.MinPoint.X <= maxX &&
                            ext.MaxPoint.Y >= minY && ext.MinPoint.Y <= maxY)
                        {
                            string l = pl.Layer;
                            if (l == "A_Bao_DK_S_0.2" || l == "DK-X" || l == "DK-Y" || l == "G1-T2" || l == "G2-T2")
                            {
                                var pts = new List<object>();
                                for (int i = 0; i < pl.NumberOfVertices; i++)
                                {
                                    var p = pl.GetPoint2dAt(i);
                                    pts.Add(new { x = Math.Round(p.X, 1), y = Math.Round(p.Y, 1) });
                                }
                                beamLines.Add(new {
                                    layer = l,
                                    isPolyline = true,
                                    closed = pl.Closed,
                                    points = pts
                                });
                            }
                        }
                    }
                    catch { }
                }
            }

            return new {
                gridBubblesCount = gridBubbles.Count,
                gridBubbles = gridBubbles,
                beamTextsCount = beamTexts.Count,
                beamTexts = beamTexts,
                slabTextsCount = slabTexts.Count,
                slabTexts = slabTexts,
                beamLinesCount = beamLines.Count,
                beamLines = beamLines
            };
        """;
    }
}
