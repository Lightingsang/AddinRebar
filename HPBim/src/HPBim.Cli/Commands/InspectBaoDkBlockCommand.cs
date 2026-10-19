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
///     Inspects the block reference on layer A_Bao_DK_S_0.2 and lists all entities in its definition.
/// </summary>
public sealed class InspectBaoDkBlockCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectBaoDkBlockCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_bao_dk_block_content.json"));

        _formatter.WriteBanner("Inspect Bao DK Block", "Kiểm tra BlockReference trên layer A_Bao_DK_S_0.2");

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

                double originX = 470383.6;
                double originY = -37486.2;

                double minX = 460000, maxX = 530000;
                double minY = -45000, maxY = -5000;

                var blockInfo = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent is BlockReference br)
                    {
                        var pt = br.Position;
                        if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        {
                            var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                            if (btr.Name != "Rec-TT" && !btr.Name.Contains("TRUC"))
                            {
                                blockInfo.Add(new {
                                    handle = br.Handle.ToString(),
                                    name = btr.Name,
                                    layer = ent.Layer,
                                    x = Math.Round(pt.X - originX, 1),
                                    y = Math.Round(pt.Y - originY, 1)
                                });
                            }
                        }
                    }
                }

                return blockInfo;
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Bao DK Block");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã trích xuất nội dung block: {outputPath}");
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
