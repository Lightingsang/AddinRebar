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
///     Locates Grids (1..13, A..N) on the Foundation Plan (_ MẶT BẰNG BỐ TRÍ ĐÀI CỌC + GIẰNG MÓNG + BÓ NỀN).
/// </summary>
public sealed class InspectFoundationPlanCoordinatesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectFoundationPlanCoordinatesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_foundation_plan_grids.json"));

        _formatter.WriteBanner("Foundation Plan Grids", "Định vị hệ trục trên Mặt Bằng Bố Trí Đài Cọc");

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

                // Khu vực quanh tiêu đề mặt bằng móng (X=532562, Y=-57038)
                // Khoảng tìm kiếm: X: 440000..550000, Y: -70000..0
                double minX = 440000, maxX = 550000;
                double minY = -75000, maxY = 5000;

                var gridBubbles = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent is BlockReference br)
                    {
                        var pt = br.Position;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            if (br.Name.Contains("TRUC") || br.Name.Contains("truc") || br.Layer.Contains("TRUC") || br.Layer.Contains("truc"))
                            {
                                var atts = new List<string>();
                                foreach (ObjectId aid in br.AttributeCollection)
                                {
                                    var att = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                                    atts.Add(att.TextString.Trim());
                                }
                                gridBubbles.Add(new {
                                    block = br.Name,
                                    layer = br.Layer,
                                    x = Math.Round(pt.X, 1),
                                    y = Math.Round(pt.Y, 1),
                                    atts = atts
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
                            if ((t.Length <= 2 && (char.IsLetter(t[0]) || char.IsDigit(t[0]))) && (dt.Layer.Contains("TRUC") || dt.Layer.Contains("truc")))
                            {
                                gridBubbles.Add(new {
                                    block = "DBText",
                                    layer = dt.Layer,
                                    x = Math.Round(pt.X, 1),
                                    y = Math.Round(pt.Y, 1),
                                    atts = new List<string> { t }
                                });
                            }
                        }
                    }
                }

                return new {
                    totalBubbles = gridBubbles.Count,
                    bubbles = gridBubbles
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Foundation Grids");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã định vị trục mặt bằng móng: {outputPath}");
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
