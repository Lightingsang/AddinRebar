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
///     Queries all dimension entities and elevation markers around footing sections ĐC-1 to ĐC-6.
/// </summary>
public sealed class InspectFootingDimensionsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectFootingDimensionsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_footing_dimensions.json"));

        _formatter.WriteBanner("Inspect Footing Dimensions", "Truy vấn Dim và Cao độ chi tiết đài móng ĐC-1..ĐC-6");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Named Pipe: {pipeName}");

            string script = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                var dims = new List<object>();
                var texts = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is Dimension d)
                    {
                        var pt = d.TextPosition;
                        if (pt.X >= 510000 && pt.X <= 630000 && pt.Y >= -70000 && pt.Y <= 0)
                        {
                            double meas = Math.Round(d.Measurement, 1);
                            dims.Add(new {
                                type = d.GetType().Name,
                                x = Math.Round(pt.X, 1),
                                y = Math.Round(pt.Y, 1),
                                measurement = meas,
                                text = d.DimensionText
                            });
                        }
                    }
                    else if (ent is DBText dt)
                    {
                        if (dt.Position.X >= 510000 && dt.Position.X <= 630000 && dt.Position.Y >= -70000 && dt.Position.Y <= 0)
                        {
                            string t = dt.TextString.Trim();
                            if (t.Contains("-") || t.Contains("+") || t.Contains("COTE") || t.Contains("h=") || t.Contains("H="))
                            {
                                texts.Add(new {
                                    text = t,
                                    layer = dt.Layer,
                                    x = Math.Round(dt.Position.X, 1),
                                    y = Math.Round(dt.Position.Y, 1)
                                });
                            }
                        }
                    }
                    else if (ent is MText mt)
                    {
                        if (mt.Location.X >= 510000 && mt.Location.X <= 630000 && mt.Location.Y >= -70000 && mt.Location.Y <= 0)
                        {
                            string t = mt.Contents.Trim();
                            if (t.Contains("-") || t.Contains("+") || t.Contains("COTE") || t.Contains("h=") || t.Contains("H="))
                            {
                                texts.Add(new {
                                    text = t,
                                    layer = mt.Layer,
                                    x = Math.Round(mt.Location.X, 1),
                                    y = Math.Round(mt.Location.Y, 1)
                                });
                            }
                        }
                    }
                }

                return new {
                    totalDims = dims.Count,
                    totalTexts = texts.Count,
                    dims = dims.Take(100).ToList(),
                    texts = texts
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Dimensions");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã lưu kết quả truy vấn Dim vào: {outputPath}");

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
