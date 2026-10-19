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
///     Scans all block definitions in the DWG to see which blocks contain foundation entities.
/// </summary>
public sealed class ScanAllBlockDefinitionsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public ScanAllBlockDefinitionsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_block_definitions_scan.json"));

        _formatter.WriteBanner("Scan Block Definitions", "Quét toàn bộ định nghĩa Block trong database");

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

                var blocks = new List<object>();

                foreach (ObjectId id in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (btr.IsLayout) continue;

                    int lineCount = 0;
                    int polyCount = 0;
                    int textCount = 0;
                    var btrLayers = new HashSet<string>();

                    foreach (ObjectId eid in btr)
                    {
                        var ent = tr.GetObject(eid, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        btrLayers.Add(ent.Layer);
                        if (ent is Line) lineCount++;
                        else if (ent is Polyline) polyCount++;
                        else if (ent is DBText || ent is MText) textCount++;
                    }

                    if (lineCount > 10 || polyCount > 5)
                    {
                        blocks.Add(new {
                            name = btr.Name,
                            lines = lineCount,
                            polys = polyCount,
                            texts = textCount,
                            layers = btrLayers.Take(10).ToList()
                        });
                    }
                }

                return blocks;
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Scan Blocks");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã quét xong định nghĩa Block: {outputPath}");
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
