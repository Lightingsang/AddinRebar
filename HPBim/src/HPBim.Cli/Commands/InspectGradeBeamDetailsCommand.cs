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
///     Queries detailed longitudinal sections and cross-sections of grade beams GM-1 to GM-22.
/// </summary>
public sealed class InspectGradeBeamDetailsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectGradeBeamDetailsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_beam_details_summary.json"));

        _formatter.WriteBanner("Inspect Grade Beam Details", "Khảo sát chi tiết dầm GM-1..GM-22 (cao độ, chiều cao, nhịp)");

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

                // Khảo sát vùng chi tiết giằng móng: X: 510000..540000, Y: -65000..-45000
                double minX = 505000, maxX = 540000;
                double minY = -65000, maxY = -45000;

                var cotes = new List<object>();
                var beamTitles = new List<object>();
                var dims = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    if (ent is BlockReference br && br.Name.Contains("Cote"))
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
                            cotes.Add(new {
                                x = Math.Round(pt.X, 1),
                                y = Math.Round(pt.Y, 1),
                                atts = atts
                            });
                        }
                    }
                    else if (ent is DBText dt)
                    {
                        var pt = dt.Position;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            string t = dt.TextString.Trim();
                            if (t.Contains("GM-") || t.Contains("BN-") || t.Contains("COTE") || t.Contains("TL:"))
                            {
                                beamTitles.Add(new {
                                    text = t,
                                    layer = dt.Layer,
                                    x = Math.Round(pt.X, 1),
                                    y = Math.Round(pt.Y, 1)
                                });
                            }
                        }
                    }
                    else if (ent is MText mt)
                    {
                        var pt = mt.Location;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            string t = mt.Contents.Trim();
                            if (t.Contains("GM-") || t.Contains("BN-") || t.Contains("COTE") || t.Contains("TL:"))
                            {
                                beamTitles.Add(new {
                                    text = t,
                                    layer = mt.Layer,
                                    x = Math.Round(pt.X, 1),
                                    y = Math.Round(pt.Y, 1)
                                });
                            }
                        }
                    }
                    else if (ent is RotatedDimension rd)
                    {
                        var pt = rd.TextPosition;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            double meas = Math.Round(rd.Measurement, 1);
                            if (meas == 300 || meas == 400 || meas == 500 || meas == 600 || meas == 1000)
                            {
                                dims.Add(new {
                                    meas = meas,
                                    x = Math.Round(pt.X, 1),
                                    y = Math.Round(pt.Y, 1),
                                    rot = Math.Round(rd.Rotation * 180.0 / Math.PI, 1)
                                });
                            }
                        }
                    }
                }

                return new {
                    totalCotes = cotes.Count,
                    totalBeamTitles = beamTitles.Count,
                    totalDims = dims.Count,
                    cotes = cotes,
                    beamTitles = beamTitles,
                    dims = dims.Take(50).ToList()
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Grade Beam Details");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã trích xuất chi tiết dầm: {outputPath}");
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
