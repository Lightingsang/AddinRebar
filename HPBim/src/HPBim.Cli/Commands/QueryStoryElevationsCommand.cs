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
///     Pure C# command to associate each Floor Plan Title in KC-HT-90-TLTT-16.dwg with its primary elevation tag.
/// </summary>
public sealed class QueryStoryElevationsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public QueryStoryElevationsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));

        _formatter.WriteBanner("Match Floor Plans & Elevations", "Khớp từng mặt bằng kết cấu với cao trình sàn chuẩn");

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

                var plans = new List<(string Name, double X, double Y)>
                {
                    ("MẶT BẰNG MÓNG & ĐÀ KIỀNG", 235766, -116832),
                    ("MẶT BẰNG SÀN SÂN KHẤU (TẦNG SK)", 238870, -168987),
                    ("MẶT BẰNG SÀN TẦNG 2 (KHÁN ĐÀI)", 215331, -291605),
                    ("MẶT BẰNG SÀN TẦNG 3", 233739, -282989),
                    ("MẶT BẰNG SÀN TẦNG 4", 233739, -342029),
                    ("MẶT BẰNG SÀN ÁP MÁI", 229364, -406188),
                    ("MẶT BẰNG MÁI", 407465, -408381)
                };

                var allElevs = new List<(string Val, double X, double Y)>();
                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is BlockReference br)
                    {
                        foreach (ObjectId aid in br.AttributeCollection)
                        {
                            var att = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                            string txt = att.TextString.Trim();
                            if ((txt.StartsWith("+") || txt.StartsWith("-") || txt.StartsWith("%%p") || txt.StartsWith("±") || txt.StartsWith("0.0")) && txt.Length <= 10)
                            {
                                allElevs.Add((txt, br.Position.X, br.Position.Y));
                            }
                        }
                    }
                    else if (ent is DBText dt)
                    {
                        string txt = dt.TextString.Trim();
                        if ((txt.StartsWith("+") || txt.StartsWith("-") || txt.StartsWith("%%p") || txt.StartsWith("±")) && txt.Length <= 10)
                        {
                            allElevs.Add((txt, dt.Position.X, dt.Position.Y));
                        }
                    }
                }

                var matched = new List<object>();
                foreach (var p in plans)
                {
                    // Find elevation markers within Delta Y <= 40000 and Delta X <= 60000
                    var nearby = allElevs
                        .Where(e => Math.Abs(e.X - p.X) <= 80000 && Math.Abs(e.Y - p.Y) <= 45000)
                        .GroupBy(e => e.Val)
                        .Select(g => new { val = g.Key, count = g.Count() })
                        .OrderByDescending(g => g.count)
                        .Take(5)
                        .ToList();

                    matched.Add(new
                    {
                        plan = p.Name,
                        x = p.X,
                        y = p.Y,
                        nearbyElevations = nearby
                    });
                }

                return matched;
            """;

            var request = new ExecuteRequest(
                Code: scriptCode,
                Transaction: TransactionModes.None,
                DryRun: false,
                TimeoutSeconds: 60,
                Label: "Match Floor Plans and Elevations");

            var result = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            if (result.IsError || result.Value == null)
            {
                _formatter.WriteError($"Lỗi truy vấn: {result.Message}");
                return 1;
            }

            string outPath = Path.Combine("scratch", "floor_plans_matched.json");
            string json = JsonSerializer.Serialize(result.Value.Value, JsonOptions);
            await File.WriteAllTextAsync(outPath, json, ct).ConfigureAwait(false);

            _formatter.WriteSuccess($"Đã trích xuất xong! Kết quả lưu tại: {outPath}");
            Console.WriteLine(json);
            return 0;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
