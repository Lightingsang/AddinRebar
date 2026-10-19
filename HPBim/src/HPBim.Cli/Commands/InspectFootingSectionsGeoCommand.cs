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
///     Queries all Cote elevation blocks, vertical dimensions, and section titles in the footing detail sheets.
/// </summary>
public sealed class InspectFootingSectionsGeoCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectFootingSectionsGeoCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_sections_detailed_geo.json"));

        _formatter.WriteBanner("Inspect Footing Sections Geo", "Truy vấn Cote, Dim độ dày và cao trình mặt cắt đài móng");

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

                var cotes = new List<object>();
                var verticalDims = new List<object>();
                var sectionTitles = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is BlockReference br && br.Name.Contains("Cote"))
                    {
                        var pt = br.Position;
                        if (pt.X >= 540000 && pt.X <= 630000 && pt.Y >= -70000 && pt.Y <= -10000)
                        {
                            var attList = new List<string>();
                            foreach (ObjectId aid in br.AttributeCollection)
                            {
                                var att = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                                attList.Add(att.TextString.Trim());
                            }
                            cotes.Add(new {
                                block = br.Name,
                                x = Math.Round(pt.X, 1),
                                y = Math.Round(pt.Y, 1),
                                values = attList
                            });
                        }
                    }
                    else if (ent is RotatedDimension rd)
                    {
                        var pt = rd.TextPosition;
                        if (pt.X >= 540000 && pt.X <= 630000 && pt.Y >= -70000 && pt.Y <= -10000)
                        {
                            // Vertical or near vertical dimension (rotation ~ 90 deg or 270 deg)
                            double rot = Math.Round(rd.Rotation * 180.0 / Math.PI, 1);
                            double meas = Math.Round(rd.Measurement, 1);
                            if (Math.Abs(rot - 90) < 5 || Math.Abs(rot - 270) < 5 || Math.Abs(rot + 90) < 5)
                            {
                                verticalDims.Add(new {
                                    x = Math.Round(pt.X, 1),
                                    y = Math.Round(pt.Y, 1),
                                    measurement = meas,
                                    text = rd.DimensionText,
                                    rot = rot
                                });
                            }
                        }
                    }
                    else if (ent is DBText dt && dt.Layer.Equals("Text CK", StringComparison.OrdinalIgnoreCase))
                    {
                        sectionTitles.Add(new {
                            text = dt.TextString.Trim(),
                            x = Math.Round(dt.Position.X, 1),
                            y = Math.Round(dt.Position.Y, 1)
                        });
                    }
                }

                return new {
                    totalCotes = cotes.Count,
                    totalVerticalDims = verticalDims.Count,
                    cotes = cotes,
                    verticalDims = verticalDims,
                    sectionTitles = sectionTitles
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Sections Geo");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã lưu phân tích chi tiết vào: {outputPath}");
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
