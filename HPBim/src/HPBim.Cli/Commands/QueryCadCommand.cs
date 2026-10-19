namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Queries AutoCAD drawing texts, notes, dimensions, and blocks via Named Pipe to inspect foundation details.
///     Target: AutoCAD 2026 main-thread execution.
/// </summary>
public sealed class QueryCadCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ConsoleFormatter _formatter;

    public QueryCadCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "foundation_technical_notes.json"));

        _formatter.WriteBanner("Query Foundation Details", $"Truy vấn chi tiết đài móng & cao độ AutoCAD 2026 (Pipe: {pipeName})");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            bool pingOk = await client.PingAsync(ct).ConfigureAwait(false);
            if (!pingOk)
            {
                throw new InvalidOperationException($"Không nhận được phản hồi Ping từ Named Pipe '{pipeName}'.");
            }

            _formatter.WriteSuccess($"Đã kết nối thành công tới Named Pipe: {pipeName}");
            _formatter.WriteInfo("Đang trích xuất toàn bộ bản vẽ KC-HT-90-TLTT-16.dwg...");

            string scriptCode = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                var sheetTitles = new List<object>();
                var foundationNotes = new List<object>();
                var elevationNotes = new List<object>();
                var footingDetails = new List<object>();
                var allMatchedTexts = new List<object>();

                string[] keywords = new string[]
                {
                    "MÓNG", "MONG", "ĐÀI", "DAI", "CAO ĐỘ", "CAO DO", "COTE", "CỐT",
                    "ĐỈNH", "ĐÁY", "DINH", "DAY", "NGÀM", "NGAM", "-1.", "-2.", "H=", "h=",
                    "D400", "300x300", "CẮT CỌC", "CAT COC", "ĐẦU CỌC", "DAU COC", "LÓT", "LOT",
                    "Đ1", "Đ2", "Đ3", "Đ4", "Đ5", "Đ6", "Đ7", "Đ8", "Đ9", "Đ10",
                    "M1", "M2", "M3", "M4", "M5", "M6", "M7", "M8", "KC-", "TLTT"
                };

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is not Entity e) continue;
                    string layer = e.Layer;
                    string txt = "";
                    double x = 0, y = 0, h = 0;

                    if (ent is DBText dt)
                    {
                        txt = dt.TextString.Trim();
                        x = dt.Position.X;
                        y = dt.Position.Y;
                        h = dt.Height;
                    }
                    else if (ent is MText mt)
                    {
                        txt = mt.Contents.Trim();
                        x = mt.Location.X;
                        y = mt.Location.Y;
                        h = mt.TextHeight;
                    }
                    else if (ent is BlockReference br)
                    {
                        // Check attributes of blocks (e.g. elevation marks, sheet titles)
                        foreach (ObjectId attId in br.AttributeCollection)
                        {
                            var att = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                            string attVal = att.TextString.Trim();
                            if (attVal.Contains("-1.") || attVal.Contains("-2.") || attVal.Contains("±0") || attVal.Contains("+0") || attVal.Contains("MÓNG") || attVal.Contains("ĐÀI"))
                            {
                                elevationNotes.Add(new
                                {
                                    block = br.Name,
                                    tag = att.Tag,
                                    text = attVal,
                                    x = Math.Round(br.Position.X, 1),
                                    y = Math.Round(br.Position.Y, 1)
                                });
                            }
                        }
                        continue;
                    }
                    else
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(txt)) continue;

                    // Sheet titles (large text)
                    if (h >= 500 && (txt.IndexOf("MẶT BẰNG", StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("MAÏT BAÈNG", StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("CHI TIẾT", StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("CHI TIEÁT", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        sheetTitles.Add(new { layer, text = txt, x = Math.Round(x, 1), y = Math.Round(y, 1), height = Math.Round(h, 1) });
                    }

                    // Check keywords
                    bool match = false;
                    foreach (var kw in keywords)
                    {
                        if (txt.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            match = true;
                            break;
                        }
                    }

                    if (match)
                    {
                        allMatchedTexts.Add(new { layer, text = txt, x = Math.Round(x, 1), y = Math.Round(y, 1), height = Math.Round(h, 1) });
                    }
                }

                return new
                {
                    drawing = doc.Name,
                    sheetTitles = sheetTitles,
                    elevationNotes = elevationNotes,
                    matchedTexts = allMatchedTexts
                };
                """;

            var request = new ExecuteRequest(
                Code: scriptCode,
                Transaction: TransactionModes.None,
                DryRun: false,
                TimeoutSeconds: 60,
                Label: "Query Foundation Details");

            var result = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            if (result.IsError || result.Value == null)
            {
                string diags = result.Diagnostics != null && result.Diagnostics.Count > 0
                    ? string.Join("\n", result.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                    : "Không có diagnostics";
                _formatter.WriteError($"Lỗi truy vấn CAD: {result.Message}\n{diags}");
                return 1;
            }

            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string jsonText = JsonSerializer.Serialize(result.Value.Value, JsonOptions);
            await File.WriteAllTextAsync(outputPath, jsonText, ct).ConfigureAwait(false);

            _formatter.WriteSuccess($"Đã lưu phân tích kỹ thuật vào: {outputPath}");

            // In tóm tắt ra console
            var root = result.Value.Value;
            if (root.TryGetProperty("sheetTitles", out var stArr))
            {
                _formatter.WriteInfo($"--- CÁC TIÊU ĐỀ BẢN VẼ / MẶT BẰNG PHÁT HIỆN ({stArr.GetArrayLength()}) ---");
                foreach (var st in stArr.EnumerateArray())
                {
                    string t = st.GetProperty("text").GetString() ?? "";
                    double x = st.GetProperty("x").GetDouble();
                    double y = st.GetProperty("y").GetDouble();
                    Console.WriteLine($"  [X={x:F0}, Y={y:F0}]: {t}");
                }
            }

            if (root.TryGetProperty("elevationNotes", out var elArr))
            {
                _formatter.WriteInfo($"--- CÁC KÝ HIỆU CAO ĐỘ (COTE ELEVATION) PHÁT HIỆN ({elArr.GetArrayLength()}) ---");
                foreach (var el in elArr.EnumerateArray())
                {
                    string b = el.GetProperty("block").GetString() ?? "";
                    string tag = el.GetProperty("tag").GetString() ?? "";
                    string t = el.GetProperty("text").GetString() ?? "";
                    double x = el.GetProperty("x").GetDouble();
                    double y = el.GetProperty("y").GetDouble();
                    Console.WriteLine($"  [Block={b}, Tag={tag}, X={x:F0}, Y={y:F0}]: {t}");
                }
            }

            if (root.TryGetProperty("matchedTexts", out var mtArr))
            {
                _formatter.WriteInfo($"--- TỔNG SỐ TEXT LIÊN QUAN ĐẾN ĐÀI MÓNG/CAO TRÌNH: {mtArr.GetArrayLength()} ---");
            }

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
