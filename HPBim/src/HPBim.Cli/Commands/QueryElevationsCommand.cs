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
///     Pinpoint exact elevation marks and footing labels across KC-HT-90-TLTT-16.dwg.
/// </summary>
public sealed class QueryElevationsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ConsoleFormatter _formatter;

    public QueryElevationsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));

        _formatter.WriteBanner("Pinpoint Elevations", "Truy vấn chính xác các ký hiệu cao độ và đài móng");

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

                // Collect distinct elevation blocks
                var coteBlocks = new List<(string val, double x, double y)>();
                var footingTitles = new List<object>();
                var pileNotes = new List<string>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is not Entity e) continue;

                    if (e is BlockReference br && br.Name.Contains("Cote"))
                    {
                        foreach (ObjectId aid in br.AttributeCollection)
                        {
                            var att = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                            string txt = att.TextString.Trim();
                            if (txt.StartsWith("-") || txt.StartsWith("+") || txt.StartsWith("%%p") || txt.StartsWith("±"))
                            {
                                coteBlocks.Add((txt, Math.Round(br.Position.X, 1), Math.Round(br.Position.Y, 1)));
                            }
                        }
                    }
                    else if (e is DBText dt)
                    {
                        string txt = dt.TextString.Trim();
                        if (txt.Contains("ĐAØI") || txt.Contains("MÓNG") || txt.Contains("CHAÂN BAÄC") || txt.Contains("COTE -1") || txt.Contains("COTE -2"))
                        {
                            footingTitles.Add(new
                            {
                                txt = txt,
                                layer = dt.Layer,
                                x = Math.Round(dt.Position.X, 1),
                                y = Math.Round(dt.Position.Y, 1)
                            });
                        }
                        else if (dt.Layer.Equals("KC-2", StringComparison.OrdinalIgnoreCase) || dt.Layer.Equals("TEXT-S", StringComparison.OrdinalIgnoreCase))
                        {
                            if (txt.Contains("COÏC") || txt.Contains("CỌC") || txt.Contains("ĐẦU CỌC") || txt.Contains("NGÀM") || txt.Contains("BÊ TÔNG LÓT") || txt.Contains("BEÂ TOÂNG LOÙT"))
                            {
                                pileNotes.Add(txt);
                            }
                        }
                    }
                    else if (e is MText mt)
                    {
                        string txt = mt.Contents.Trim();
                        if (txt.Contains("ĐAØI") || txt.Contains("MÓNG") || txt.Contains("CHAÂN BAÄC") || txt.Contains("COTE -1") || txt.Contains("COTE -2"))
                        {
                            footingTitles.Add(new
                            {
                                txt = txt,
                                layer = mt.Layer,
                                x = Math.Round(mt.Location.X, 1),
                                y = Math.Round(mt.Location.Y, 1)
                            });
                        }
                    }
                }

                // Group Cote blocks by value
                var coteSummary = coteBlocks
                    .GroupBy(c => c.val)
                    .Select(g => new { cote = g.Key, count = g.Count(), sampleX = g.First().x, sampleY = g.First().y })
                    .OrderBy(g => g.cote)
                    .ToList();

                return new
                {
                    cotes = coteSummary,
                    footingTitles = footingTitles.Take(30).ToList(),
                    pileNotes = pileNotes.Distinct().Take(20).ToList()
                };
                """;

            var request = new ExecuteRequest(
                Code: scriptCode,
                Transaction: TransactionModes.None,
                DryRun: false,
                TimeoutSeconds: 60,
                Label: "Pinpoint Elevations Compact");

            var result = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            if (result.IsError || result.Value == null)
            {
                string diags = result.Diagnostics != null ? string.Join("\n", result.Diagnostics.Select(d => d.Message)) : "";
                _formatter.WriteError($"Lỗi: {result.Message} - {diags}");
                return 1;
            }

            string jsonText;
            JsonElement root = result.Value.Value;
            if (root.ValueKind == JsonValueKind.String)
            {
                jsonText = root.GetString() ?? "{}";
                using var parsedDoc = JsonDocument.Parse(jsonText);
                root = parsedDoc.RootElement.Clone();
            }
            else
            {
                jsonText = JsonSerializer.Serialize(root, JsonOptions);
            }

            string outPath = Path.Combine("scratch", "exact_elevations_summary.json");
            await File.WriteAllTextAsync(outPath, jsonText, ct).ConfigureAwait(false);

            _formatter.WriteSuccess($"Đã lưu kết quả tóm tắt vào: {outPath}");

            if (root.TryGetProperty("cotes", out var cArr))
            {
                _formatter.WriteInfo("--- TỔNG HỢP CÁC CAO ĐỘ (COTE) XUẤT HIỆN TRONG BẢN VẼ ---");
                foreach (var c in cArr.EnumerateArray())
                {
                    string cote = c.GetProperty("cote").GetString() ?? "";
                    int count = c.GetProperty("count").GetInt32();
                    double sx = c.GetProperty("sampleX").GetDouble();
                    double sy = c.GetProperty("sampleY").GetDouble();
                    Console.WriteLine($"  Cao độ '{cote}': xuất hiện {count} lần (Mẫu tại X={sx:F0}, Y={sy:F0})");
                }
            }

            if (root.TryGetProperty("footingTitles", out var ftArr))
            {
                _formatter.WriteInfo("--- CÁC TIÊU ĐỀ/GHI CHÚ ĐÀI MÓNG ---");
                foreach (var ft in ftArr.EnumerateArray())
                {
                    string t = ft.GetProperty("txt").GetString() ?? "";
                    double x = ft.GetProperty("x").GetDouble();
                    double y = ft.GetProperty("y").GetDouble();
                    Console.WriteLine($"  [X={x:F0}, Y={y:F0}]: {t}");
                }
            }

            if (root.TryGetProperty("pileNotes", out var pnArr))
            {
                _formatter.WriteInfo("--- GHI CHÚ KỸ THUẬT CỌC/LÓT ĐÀI ---");
                foreach (var pn in pnArr.EnumerateArray())
                {
                    Console.WriteLine($"  * {pn.GetString()}");
                }
            }

            return 0;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
