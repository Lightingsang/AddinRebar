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
///     Scans the entire AutoCAD DWG to list all drawing title blocks, sheet names, and drawing frames.
/// </summary>
public sealed class InspectCadDrawingSheetsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectCadDrawingSheetsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_all_sheets_catalog.json"));

        _formatter.WriteBanner("Catalog Drawing Sheets", "Lập danh mục tất cả khung tên và tên bản vẽ trong file DWG");

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

                var titles = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    string text = "";
                    double x = 0, y = 0;

                    if (ent is DBText dt)
                    {
                        text = dt.TextString.Trim();
                        x = dt.Position.X;
                        y = dt.Position.Y;
                    }
                    else if (ent is MText mt)
                    {
                        text = mt.Contents.Trim();
                        x = mt.Location.X;
                        y = mt.Location.Y;
                    }

                    if (string.IsNullOrEmpty(text)) continue;

                    if (text.Contains("MẶT BẰNG") || text.Contains("MAËT BAÈNG") || text.Contains("CHI TIẾT") || text.Contains("CHI TIEÁT") || text.Contains("KC-") || text.Contains("TÊN BẢN VẼ") || text.Contains("TEÂN BA\u00DBN VE\u00C3"))
                    {
                        titles.Add(new {
                            text = text,
                            layer = ent.Layer,
                            x = Math.Round(x, 1),
                            y = Math.Round(y, 1)
                        });
                    }
                }

                return new {
                    total = titles.Count,
                    titles = titles
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Catalog Sheets");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã lập xong danh mục bản vẽ: {outputPath}");
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
