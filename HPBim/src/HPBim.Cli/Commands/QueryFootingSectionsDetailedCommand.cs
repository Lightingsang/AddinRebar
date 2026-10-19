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
///     Queries detailed text and elevation blocks around each footing section (1-1 to 6-6) in AutoCAD.
/// </summary>
public sealed class QueryFootingSectionsDetailedCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public QueryFootingSectionsDetailedCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        _formatter.WriteBanner("Detailed Footing Sections", "Phân tích cao độ từng mặt cắt đài móng 1-1 đến 6-6");

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

                var sections = new List<object>();

                // Định nghĩa tọa độ tâm của các mặt cắt đài móng
                var targets = new[] {
                    new { Name = "ĐC-1 (MC 1-1)", X = 548781.0, Y = -20425.0 },
                    new { Name = "ĐC-2 (MC 2-2)", X = 549023.0, Y = -50751.0 },
                    new { Name = "ĐC-2' (MC 2'-2')", X = 566808.0, Y = -20751.0 },
                    new { Name = "ĐC-3 (MC 3-3)", X = 567256.0, Y = -48780.0 },
                    new { Name = "ĐC-3' (MC 3'-3')", X = 605964.0, Y = -49180.0 },
                    new { Name = "ĐC-4 (MC 4-4)", X = 585346.0, Y = -20351.0 },
                    new { Name = "ĐC-5 (MC 5-5)", X = 585411.0, Y = -50171.0 },
                    new { Name = "ĐC-6 (MC 6-6)", X = 604814.0, Y = -19793.0 }
                };

                foreach (var tgt in targets)
                {
                    var foundTexts = new List<object>();
                    double r = 8000.0;

                    foreach (ObjectId id in ms)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead);
                        if (ent is not Entity e) continue;

                        double ex = 0, ey = 0;
                        string text = "";

                        if (e is DBText dt)
                        {
                            ex = dt.Position.X;
                            ey = dt.Position.Y;
                            text = dt.TextString.Trim();
                        }
                        else if (e is MText mt)
                        {
                            ex = mt.Location.X;
                            ey = mt.Location.Y;
                            text = mt.Contents.Trim();
                        }
                        else if (e is BlockReference br)
                        {
                            ex = br.Position.X;
                            ey = br.Position.Y;
                            if (br.Name.Contains("Cote") || br.Name.Contains("CT"))
                            {
                                var atts = new List<string>();
                                foreach (ObjectId aid in br.AttributeCollection)
                                {
                                    var att = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                                    atts.Add(att.TextString.Trim());
                                }
                                text = $"[COTE {br.Name}]: " + string.Join(" | ", atts);
                            }
                        }

                        if (string.IsNullOrEmpty(text)) continue;

                        if (Math.Abs(ex - tgt.X) <= r && Math.Abs(ey - tgt.Y) <= r)
                        {
                            foundTexts.Add(new {
                                text = text,
                                layer = e.Layer,
                                dx = Math.Round(ex - tgt.X, 1),
                                dy = Math.Round(ey - tgt.Y, 1)
                            });
                        }
                    }

                    sections.Add(new {
                        target = tgt.Name,
                        totalFound = foundTexts.Count,
                        items = foundTexts
                    });
                }

                return sections;
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Query Detailed Sections");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                if (res.Diagnostics != null)
                {
                    foreach (var d in res.Diagnostics) _formatter.WriteError($"  [{d.Id}] L{d.Line}: {d.Message}");
                }
                return 1;
            }

            string outPath = Path.Combine("scratch", "cad_detailed_sections_report.json");
            File.WriteAllText(outPath, JsonSerializer.Serialize(res.Value.Value, JsonOptions));
            _formatter.WriteSuccess($"Đã trích xuất báo cáo chi tiết từng mặt cắt đài móng vào: {outPath}");

            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
