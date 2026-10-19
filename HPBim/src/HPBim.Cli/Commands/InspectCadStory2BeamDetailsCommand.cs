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
///     Pure C# command to inspect Story 2 Beam Detail sheets (Chi tiết đà sàn Tầng 2).
/// </summary>
public sealed class InspectCadStory2BeamDetailsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectCadStory2BeamDetailsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_story2_beam_details.json"));

        _formatter.WriteBanner("Inspect Story 2 Beam Details", "Khảo sát các bản vẽ Chi Tiết Đà Sàn Tầng 2 (100% C#)");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Named Pipe AutoCAD: {pipeName}");

            var req = new ExecuteRequest(BuildCadInspectionScript(), TransactionModes.None, false, 60, "Inspect Story 2 Beam Details");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                string diags = res.Diagnostics != null && res.Diagnostics.Count > 0
                    ? string.Join("; ", res.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                    : "Không có diagnostics";
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message} -> {diags}");
                return 1;
            }

            string json = JsonSerializer.Serialize(res.Value.Value, JsonOptions);
            string dir = Path.GetDirectoryName(outputPath) ?? "scratch";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);

            _formatter.WriteSuccess($"Đã lưu kết quả chi tiết đà sàn Tầng 2 tại: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi: {ex.Message}");
            return 2;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string BuildCadInspectionScript()
    {
        return """
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            // Bounds of Story 2 Beam Details: X in [380000..650000], Y in [-245000..-180000]
            double minX = 380000.0, maxX = 650000.0;
            double minY = -245000.0, maxY = -180000.0;

            var beamTitles = new List<object>();

            foreach (ObjectId id in ms)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;

                double x = 0, y = 0;
                string txt = "";

                if (ent is DBText dt)
                {
                    x = dt.Position.X;
                    y = dt.Position.Y;
                    txt = dt.TextString.Trim();
                }
                else if (ent is MText mt)
                {
                    x = mt.Location.X;
                    y = mt.Location.Y;
                    txt = mt.Contents.Trim();
                }

                if (!string.IsNullOrEmpty(txt) && x >= minX && x <= maxX && y >= minY && y <= maxY)
                {
                    string u = txt.ToUpper();
                    if (u.Contains("D-") || u.Contains("D1.") || u.Contains("D2.") || u.Contains("D3.") ||
                        u.Contains("D4.") || u.Contains("D5.") || u.Contains("D6.") || u.Contains("D7.") ||
                        u.Contains("D8.") || u.Contains("D9.") || u.Contains("D10.") || u.Contains("D20.") ||
                        u.Contains("200X") || u.Contains("300X") || u.Contains("400X") || u.Contains("DS"))
                    {
                        beamTitles.Add(new {
                            text = txt,
                            layer = ent.Layer,
                            x = Math.Round(x, 1),
                            y = Math.Round(y, 1)
                        });
                    }
                }
            }

            return new {
                totalTitles = beamTitles.Count,
                titles = beamTitles
            };
        """;
    }
}
