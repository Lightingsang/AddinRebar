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
///     Queries detailed footing drawings (Sheet 3/4) in AutoCAD 2026
///     to find exact dimensions, top/bottom cotes, and pile embedment notes for ĐC-1..ĐC-7.
/// </summary>
public sealed class QueryFootingDetailsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public QueryFootingDetailsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        _formatter.WriteBanner("Query Footing Details ĐC-1..ĐC-7", "Truy vấn cao độ đỉnh/đáy và chiều dày đài móng từ CAD");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối AutoCAD qua Named Pipe: {pipeName}");

            string script = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                // Quét toàn bộ text trong khu vực Chi tiết Đài móng: X từ 520,000 đến 650,000, Y từ -65,000 đến 0
                var items = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is not Entity e) continue;

                    double x = 0, y = 0;
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
                    else
                    {
                        continue;
                    }

                    if (x >= 520000 && x <= 650000 && y >= -65000 && y <= 0)
                    {
                        // Lọc các text có liên quan đến ĐC, COTE, KÍCH THƯỚC, BÊ TÔNG, CỌC
                        if (text.Contains("ĐC") || text.Contains("DC") || text.Contains("COTE") || text.Contains("COÏC") ||
                            text.Contains("CỌC") || text.Contains("MÓNG") || text.Contains("ÑOAÏN") || text.Contains("100") ||
                            text.Contains("NGÀM") || text.Contains("-1.") || text.Contains("-2.") || text.Contains("CHIEÀU") ||
                            text.Contains("DAØY") || text.Contains("H=") || text.Contains("h=") || text.Contains("TL") ||
                            text.Contains("CHI TIEÁT") || text.Contains("MAËT CAÉT"))
                        {
                            items.Add(new {
                                layer = e.Layer,
                                x = Math.Round(x, 1),
                                y = Math.Round(y, 1),
                                text = text
                            });
                        }
                    }
                }

                return new {
                    totalItems = items.Count,
                    items = items
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Query Footing Details");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                if (res.Diagnostics != null)
                {
                    foreach (var d in res.Diagnostics)
                    {
                        _formatter.WriteError($"  [{d.Id}] L{d.Line}:C{d.Column} - {d.Message}");
                    }
                }
                return 1;
            }

            string outPath = Path.Combine("scratch", "cad_footing_details_dc.json");
            File.WriteAllText(outPath, JsonSerializer.Serialize(res.Value.Value, JsonOptions));
            _formatter.WriteSuccess($"Đã trích xuất {res.Value.Value.GetProperty("totalItems").GetInt32()} ghi chú chi tiết đài móng vào: {outPath}");

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
