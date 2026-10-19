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
///     Inspects all lines, arcs, 2d polylines, and blocks on Plan A by layer.
/// </summary>
public sealed class InspectPlanAAllEntitiesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectPlanAAllEntitiesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_planA_all_entities_by_layer.json"));

        _formatter.WriteBanner("Inspect Plan A All Entities", "Thống kê tất cả thực thể theo layer trên Mặt Bằng Móng");

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

                double minX = originX - 5000, maxX = originX + 52000;
                double minY = originY - 5000, maxY = originY + 32000;

                var stats = new Dictionary<string, Dictionary<string, int>>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    Point3d? pt = null;
                    if (ent is Line ln) pt = ln.StartPoint;
                    else if (ent is Polyline pl) pt = pl.StartPoint;
                    else if (ent is BlockReference br) pt = br.Position;
                    else if (ent is DBText dt) pt = dt.Position;
                    else if (ent is MText mt) pt = mt.Location;
                    else if (ent is Circle cr) pt = cr.Center;
                    else if (ent is Arc ar) pt = ar.Center;

                    if (!pt.HasValue) continue;

                    if (pt.Value.X >= minX && pt.Value.X <= maxX && pt.Value.Y >= minY && pt.Value.Y <= maxY)
                    {
                        string lay = ent.Layer;
                        string type = ent.GetType().Name;

                        if (!stats.ContainsKey(lay))
                        {
                            stats[lay] = new Dictionary<string, int>();
                        }
                        stats[lay][type] = stats[lay].GetValueOrDefault(type, 0) + 1;
                    }
                }

                return stats;
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Plan A Entities");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, res.Value.Value.GetRawText());
            _formatter.WriteSuccess($"Đã thống kê thực thể Plan A: {outputPath}");
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
