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
///     Pure C# command to scan AutoCAD DWG for all building story levels, floor elevations and section marks.
/// </summary>
public sealed class QueryBuildingLevelsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public QueryBuildingLevelsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "building_levels_raw.json"));

        _formatter.WriteBanner("Query Building Floor Levels", "Quét toàn bộ tầng, cao trình sàn & mái từ AutoCAD DWG");

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

            string scriptCode = BuildScript();

            var request = new ExecuteRequest(
                Code: scriptCode,
                Transaction: TransactionModes.None,
                DryRun: false,
                TimeoutSeconds: 60,
                Label: "Query Building Levels and Sections");

            var result = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            if (result.IsError || result.Value == null)
            {
                _formatter.WriteError($"Lỗi truy vấn CAD: {result.Message}");
                return 1;
            }

            await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(result.Value.Value, JsonOptions), ct).ConfigureAwait(false);
            RenderResult(result.Value.Value);
            return 0;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string BuildScript() => """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        var floorTitles = new List<(string Text, double X, double Y, string Layer)>();
        var elevationMarkers = new List<(string Value, double X, double Y, string Source)>();

        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead);
            if (ent is not Entity e) continue;

            if (e is DBText dt)
            {
                string txt = dt.TextString.Trim();
                CheckAndAddText(txt, dt.Position.X, dt.Position.Y, dt.Layer, floorTitles, elevationMarkers);
            }
            else if (e is MText mt)
            {
                string txt = mt.Contents.Trim();
                CheckAndAddText(txt, mt.Location.X, mt.Location.Y, mt.Layer, floorTitles, elevationMarkers);
            }
            else if (e is BlockReference br)
            {
                foreach (ObjectId aid in br.AttributeCollection)
                {
                    var att = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                    string atxt = att.TextString.Trim();
                    if (atxt.StartsWith("+") || atxt.StartsWith("-") || atxt.StartsWith("%%p") || atxt.StartsWith("±") || atxt.StartsWith("0.0") || atxt.StartsWith("+0.0"))
                    {
                        elevationMarkers.Add((atxt, Math.Round(br.Position.X, 1), Math.Round(br.Position.Y, 1), br.Name));
                    }
                }
            }
        }

        void CheckAndAddText(string txt, double x, double y, string layer, 
            List<(string, double, double, string)> titles, 
            List<(string, double, double, string)> elevs)
        {
            if (string.IsNullOrEmpty(txt)) return;

            string upper = txt.ToUpperInvariant();
            if (upper.Contains("TAÀNG") || upper.Contains("TẦNG") || 
                upper.Contains("LAÀU") || upper.Contains("LẦU") || 
                upper.Contains("TREÄT") || upper.Contains("TRỆT") || 
                upper.Contains("MAÙI") || upper.Contains("MÁI") || 
                upper.Contains("SAØN") || upper.Contains("SÀN") ||
                upper.Contains("KHAÙN ÑAØI") || upper.Contains("KHÁN ĐÀI") ||
                upper.Contains("MOÙNG") || upper.Contains("MÓNG") ||
                upper.Contains("COTE") || upper.Contains("CAO ĐỘ") || upper.Contains("CAO ÑOÄ"))
            {
                titles.Add((txt, Math.Round(x, 1), Math.Round(y, 1), layer));
            }

            if ((txt.StartsWith("+") || txt.StartsWith("-") || txt.StartsWith("%%p") || txt.StartsWith("±")) && txt.Length <= 10)
            {
                elevs.Add((txt, Math.Round(x, 1), Math.Round(y, 1), "Text"));
            }
        }

        var distinctElevs = elevationMarkers
            .GroupBy(m => m.Value)
            .Select(g => new { value = g.Key, count = g.Count(), sampleX = g.First().X, sampleY = g.First().Y, source = g.First().Source })
            .OrderBy(g => g.value)
            .ToList();

        var importantTitles = floorTitles
            .Where(t => t.Text.Contains("MẶT BẰNG") || t.Text.Contains("MAËT BAÈNG") || 
                        t.Text.Contains("MẶT CẮT") || t.Text.Contains("MAËT CAÉT") ||
                        t.Text.Contains("TẦNG") || t.Text.Contains("TAÀNG") ||
                        t.Text.Contains("LẦU") || t.Text.Contains("LAÀU") ||
                        t.Text.Contains("MÁI") || t.Text.Contains("MAÙI"))
            .Select(t => new { text = t.Text, x = t.X, y = t.Y, layer = t.Layer })
            .Distinct()
            .Take(50)
            .ToList();

        return new
        {
            elevations = distinctElevs,
            titles = importantTitles
        };
    """;

    private void RenderResult(JsonElement root)
    {
        if (root.TryGetProperty("elevations", out var eArr))
        {
            _formatter.WriteInfo("--- TẤT CẢ KÝ HIỆU CAO ĐỘ (ELEVATIONS) PHÁT HIỆN TRONG CAD ---");
            foreach (var e in eArr.EnumerateArray())
            {
                string val = e.GetProperty("value").GetString() ?? "";
                int count = e.GetProperty("count").GetInt32();
                string src = e.GetProperty("source").GetString() ?? "";
                Console.WriteLine($"  Cao trình [{val}]: xuất hiện {count} lần (Nguồn: {src})");
            }
        }

        if (root.TryGetProperty("titles", out var tArr))
        {
            _formatter.WriteInfo("--- TÊN CÁC MẶT BẰNG / TẦNG PHÁT HIỆN TRONG CAD ---");
            foreach (var t in tArr.EnumerateArray())
            {
                string txt = t.GetProperty("text").GetString() ?? "";
                double x = t.GetProperty("x").GetDouble();
                double y = t.GetProperty("y").GetDouble();
                Console.WriteLine($"  [X={x:F0}, Y={y:F0}]: {txt}");
            }
        }
    }
}
