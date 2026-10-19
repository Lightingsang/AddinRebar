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
///     Comprehensive deep inspection of AutoCAD sheet MẶT BẰNG BỐ TRÍ ĐÀ KIỀNG.
/// </summary>
public sealed class InspectStory1DaKiengSheetCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectStory1DaKiengSheetCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_da_kieng_sheet_deep_inspection.json"));

        _formatter.WriteBanner("Inspect Da Kieng Sheet", "Khảo sát toàn diện bản vẽ CAD Mặt Bằng Bố Trí Đà Kiềng");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối AutoCAD Named Pipe: {pipeName}");

            string script = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                // Da Kieng Sheet bounding box in CAD coordinates:
                // Title at (233956.1, -105791.4), (280484.8, -116649.2)
                double minX = 210000, maxX = 295000;
                double minY = -130000, maxY = -80000;

                var msEntities = new Dictionary<string, int>();
                var msBlocks = new List<object>();
                var dkBlockEntities = new Dictionary<string, int>();
                var dkBlockLayers = new Dictionary<string, int>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    Extents3d ext;
                    try { ext = ent.GeometricExtents; } catch { continue; }

                    if (ext.MaxPoint.X >= minX && ext.MinPoint.X <= maxX &&
                        ext.MaxPoint.Y >= minY && ext.MinPoint.Y <= maxY)
                    {
                        string type = ent.GetType().Name;
                        msEntities[type] = msEntities.TryGetValue(type, out int cnt) ? cnt + 1 : 1;

                        if (ent is BlockReference br)
                        {
                            var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                            msBlocks.Add(new {
                                handle = br.Handle.ToString(),
                                name = btr.Name,
                                layer = ent.Layer,
                                x = Math.Round(br.Position.X, 1),
                                y = Math.Round(br.Position.Y, 1)
                            });
                        }
                    }
                }

                if (bt.Has("DK"))
                {
                    var btrDK = (BlockTableRecord)tr.GetObject(bt["DK"], OpenMode.ForRead);
                    foreach (ObjectId id in btrDK)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;
                        string type = ent.GetType().Name;
                        dkBlockEntities[type] = dkBlockEntities.TryGetValue(type, out int c1) ? c1 + 1 : 1;

                        string lyr = ent.Layer ?? "";
                        dkBlockLayers[lyr] = dkBlockLayers.TryGetValue(lyr, out int c2) ? c2 + 1 : 1;
                    }
                }

                return new {
                    msEntityCounts = msEntities,
                    msBlockReferences = msBlocks,
                    dkBlockEntities = dkBlockEntities,
                    dkBlockLayers = dkBlockLayers
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Da Kieng Sheet");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi truy vấn AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã lưu kết quả khảo sát CAD vào: {outputPath}");

            Console.WriteLine(res.Value.Value.GetRawText());
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kết nối AutoCAD: {ex.Message}");
            return 1;
        }
    }
}
