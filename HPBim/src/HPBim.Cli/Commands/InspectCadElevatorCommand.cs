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
///     Pure C# command to inspect Elevator Core and Pit details in AutoCAD DWG.
/// </summary>
public sealed class InspectCadElevatorCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectCadElevatorCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_elevator_details.json"));

        _formatter.WriteBanner("Inspect CAD Elevator", "Khảo sát chi tiết buồng thang máy và hố Pit từ DWG");

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

            string script = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                // Query all dimensions in Pit section area (655000..668000, -48000..-40000)
                var pitSecDims = new List<object>();

                foreach (ObjectId id in ms)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent is Dimension dim)
                    {
                        var ext = ent.GeometricExtents;
                        double midX = (ext.MinPoint.X + ext.MaxPoint.X) / 2.0;
                        double midY = (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0;

                        if (midX >= 655000 && midX <= 670000 && midY >= -48000 && midY <= -40000)
                        {
                            pitSecDims.Add(new {
                                measurement = Math.Round(dim.Measurement, 1),
                                text = dim.DimensionText.Trim(),
                                layer = dim.Layer,
                                minX = Math.Round(ext.MinPoint.X, 1),
                                maxX = Math.Round(ext.MaxPoint.X, 1),
                                minY = Math.Round(ext.MinPoint.Y, 1),
                                maxY = Math.Round(ext.MaxPoint.Y, 1)
                            });
                        }
                    }
                }

                return new {
                    total = pitSecDims.Count,
                    dims = pitSecDims
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Elevator");
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
            File.WriteAllText(outputPath, json);

            _formatter.WriteSuccess($"Đã trích xuất xong dữ liệu Thang Máy: {outputPath}");
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
