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
///     Queries detailed footing sections and pile notes in AutoCAD 2026.
/// </summary>
public sealed class QueryCadFootingsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ConsoleFormatter _formatter;

    public QueryCadFootingsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));

        _formatter.WriteBanner("Query Footing Details", "Phân tích chi tiết mặt cắt đài móng và cao độ cọc trong AutoCAD");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            if (!await client.PingAsync(ct).ConfigureAwait(false)) return 1;

            string scriptCode = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                var items = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is not Entity e) continue;

                    double x = 0, y = 0;
                    string type = e.GetType().Name;
                    string text = "";

                    if (e is DBText dt)
                    {
                        x = dt.Position.X;
                        y = dt.Position.Y;
                        text = dt.TextString.Trim();
                    }
                    else if (e is MText mt)
                    {
                        x = mt.Location.X;
                        y = mt.Location.Y;
                        text = mt.Contents.Trim();
                    }
                    else if (e is BlockReference br)
                    {
                        x = br.Position.X;
                        y = br.Position.Y;
                        var atts = new List<string>();
                        foreach (ObjectId aid in br.AttributeCollection)
                        {
                            var att = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                            atts.Add($"{att.Tag}={att.TextString}");
                        }
                        text = $"[Block {br.Name}] " + string.Join("; ", atts);
                    }
                    else
                    {
                        continue;
                    }

                    // Scan the footing details zone (X from 520,000 to 700,000, Y from -60,000 to 0)
                    if (x >= 520000 && x <= 700000 && y >= -65000 && y <= 5000)
                    {
                        items.Add(new
                        {
                            layer = e.Layer,
                            x = Math.Round(x, 1),
                            y = Math.Round(y, 1),
                            text = text
                        });
                    }
                    // Also scan near the foundation plan note zone (X from 300,000 to 360,000, Y from -65,000 to -40,000)
                    else if (x >= 300000 && x <= 360000 && y >= -65000 && y <= -40000)
                    {
                        if (text.Contains("CAO") || text.Contains("MÓNG") || text.Contains("ĐÀI") || text.Contains("COTE") || text.Contains("-1.") || text.Contains("CỌC") || text.Contains("COIC"))
                        {
                            items.Add(new
                            {
                                layer = e.Layer,
                                x = Math.Round(x, 1),
                                y = Math.Round(y, 1),
                                text = text
                            });
                        }
                    }
                }

                return new
                {
                    totalItems = items.Count,
                    items = items
                };
                """;

            var request = new ExecuteRequest(
                Code: scriptCode,
                Transaction: TransactionModes.None,
                DryRun: false,
                TimeoutSeconds: 60,
                Label: "Query Footing Sections");

            var result = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            if (result.IsError || result.Value == null)
            {
                string diags = result.Diagnostics != null ? string.Join("\n", result.Diagnostics.Select(d => d.Message)) : "";
                _formatter.WriteError($"Lỗi: {result.Message} - {diags}");
                return 1;
            }

            string outPath = Path.Combine("scratch", "footing_sections_raw.json");
            await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(result.Value.Value, JsonOptions), ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã trích xuất {result.Value.Value.GetProperty("totalItems").GetInt32()} phần tử chi tiết đài móng vào {outPath}");
            return 0;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
