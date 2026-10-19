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
///     Queries and extracts grade beams (đà kiềng / giằng móng) from AutoCAD 2026.
/// </summary>
public sealed class ExtractGradeBeamsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public ExtractGradeBeamsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_grade_beams_raw.json"));

        _formatter.WriteBanner("Extract Grade Beams from CAD", "Quét hệ thống đà kiềng / giằng móng từ AutoCAD 2026");

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

                double originX = 218352.1;
                double originY = -99383.2;

                var layerStats = new Dictionary<string, int>();

                if (bt.Has("DK"))
                {
                    var btrDK = (BlockTableRecord)tr.GetObject(bt["DK"], OpenMode.ForRead);
                    foreach (ObjectId id in btrDK)
                    {
                        var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (e == null) continue;
                        string lyr = e.Layer ?? "None";
                        if (!layerStats.ContainsKey(lyr)) layerStats[lyr] = 0;
                        layerStats[lyr]++;
                    }
                }

                var msTexts = new List<object>();
                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is DBText dt)
                    {
                        double rx = Math.Round(dt.Position.X - originX, 1);
                        double ry = Math.Round(dt.Position.Y - originY, 1);
                        if (rx >= -5000 && rx <= 55000 && ry >= -5000 && ry <= 35000)
                        {
                            msTexts.Add(new {
                                text = dt.TextString.Trim(),
                                layer = dt.Layer,
                                rx = rx,
                                ry = ry,
                                h = Math.Round(dt.Height, 1)
                            });
                        }
                    }
                    else if (ent is MText mt)
                    {
                        double rx = Math.Round(mt.Location.X - originX, 1);
                        double ry = Math.Round(mt.Location.Y - originY, 1);
                        if (rx >= -5000 && rx <= 55000 && ry >= -5000 && ry <= 35000)
                        {
                            msTexts.Add(new {
                                text = mt.Contents.Trim(),
                                layer = mt.Layer,
                                rx = rx,
                                ry = ry,
                                h = Math.Round(mt.TextHeight, 1)
                            });
                        }
                    }
                }

                return new {
                    layersInBlockDK = layerStats,
                    totalMsTexts = msTexts.Count,
                    msTexts = msTexts
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Extract Grade Beams");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, JsonSerializer.Serialize(res.Value.Value, JsonOptions));
            _formatter.WriteSuccess($"Đã trích xuất thành công dữ liệu đà kiềng vào: {outputPath}");
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
