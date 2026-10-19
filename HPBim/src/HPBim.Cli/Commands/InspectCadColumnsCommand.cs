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
///     Pure C# command to inspect AutoCAD Block COL-HTRU and column layout for Story 1.
/// </summary>
public sealed class InspectCadColumnsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectCadColumnsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_columns_extracted.json"));

        _formatter.WriteBanner("Inspect CAD Columns", "Trích xuất 100% C# danh sách 72 Cột Tầng 1 từ block COL-HTRU");

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

                // Origin of Plan Story 1: intersection of Grid 1 and Grid A
                // In Plan A (Foundation): Grid 1-A is at X = 302352.1, Y = -38575.9
                // On Plan Story 1 (around X=739659.7, Y=-47064.2), find the BlockReference of COL-HTRU or polylines.
                
                class ColRaw
                {
                    public string layer;
                    public double width;
                    public double height;
                    public double centerX;
                    public double centerY;
                }

                var colDefEntities = new List<ColRaw>();
                if (bt.Has("COL-HTRU"))
                {
                    var colBtr = (BlockTableRecord)tr.GetObject(bt["COL-HTRU"], OpenMode.ForRead);
                    foreach (ObjectId id in colBtr)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent is Polyline pl)
                        {
                            double minX = double.MaxValue, maxX = double.MinValue;
                            double minY = double.MaxValue, maxY = double.MinValue;
                            for (int i = 0; i < pl.NumberOfVertices; i++)
                            {
                                var pt = pl.GetPoint2dAt(i);
                                if (pt.X < minX) minX = pt.X;
                                if (pt.X > maxX) maxX = pt.X;
                                if (pt.Y < minY) minY = pt.Y;
                                if (pt.Y > maxY) maxY = pt.Y;
                            }

                            var item = new ColRaw();
                            item.layer = pl.Layer;
                            item.width = Math.Round(maxX - minX, 1);
                            item.height = Math.Round(maxY - minY, 1);
                            item.centerX = Math.Round((minX + maxX) / 2.0, 1);
                            item.centerY = Math.Round((minY + maxY) / 2.0, 1);
                            colDefEntities.Add(item);
                        }
                    }
                }

                // On Story 1 plan (handle 131EB49, X=766328.5, Y=-38777.7)
                // Offset from Grid 1-A to Block Origin: dX = 43976.4, dY = 379.9
                double dX = 43976.4;
                double dY = 379.9;

                // Extract text labels on Story 1 plan to find column marks (C-1, C-2, etc.)
                double s1MinX = 720000, s1MaxX = 775000;
                double s1MinY = -45000, s1MaxY = -10000;
                var textLabels = new List<(string text, double x, double y, string layer)>();

                foreach (ObjectId id in ms)
                {
                    string dxf = id.ObjectClass.DxfName;
                    if (dxf != "TEXT" && dxf != "MTEXT") continue;

                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    if (ent is DBText dt)
                    {
                        if (dt.Position.X >= s1MinX && dt.Position.X <= s1MaxX &&
                            dt.Position.Y >= s1MinY && dt.Position.Y <= s1MaxY)
                        {
                            string t = dt.TextString.Trim();
                            if (!string.IsNullOrEmpty(t))
                                textLabels.Add((t, dt.Position.X, dt.Position.Y, dt.Layer));
                        }
                    }
                    else if (ent is MText mt)
                    {
                        if (mt.Location.X >= s1MinX && mt.Location.X <= s1MaxX &&
                            mt.Location.Y >= s1MinY && mt.Location.Y <= s1MaxY)
                        {
                            string t = mt.Contents.Trim();
                            if (!string.IsNullOrEmpty(t))
                                textLabels.Add((t, mt.Location.X, mt.Location.Y, mt.Layer));
                        }
                    }
                }

                // Map 72 columns to Revit coordinates and find nearest column mark text
                double planX = 766328.5;
                double planY = -38777.7;
                var mappedColumns = new List<object>();

                int colIndex = 0;
                foreach (var c in colDefEntities)
                {
                    colIndex++;
                    double cx = c.centerX;
                    double cy = c.centerY;
                    double w = c.width;
                    double h = c.height;

                    double revitX = Math.Round(cx + dX, 1);
                    double revitY = Math.Round(cy + dY, 1);

                    double cadX = Math.Round(planX + cx, 1);
                    double cadY = Math.Round(planY + cy, 1);

                    // Find nearest text that looks like a column mark (e.g. C-1, C1, C-2, C-3a, C-4, etc.)
                    string nearestMark = "";
                    double minDist = 3000.0; // search within 3m
                    foreach (var txt in textLabels)
                    {
                        double dist = Math.Sqrt(Math.Pow(txt.x - cadX, 2) + Math.Pow(txt.y - cadY, 2));
                        if (dist < minDist)
                        {
                            // Check if text looks like a column mark
                            string upper = txt.text.ToUpper();
                            if (upper.StartsWith("C-") || upper.StartsWith("C_") || upper.StartsWith("C") && upper.Length <= 4 && char.IsDigit(upper[1]))
                            {
                                minDist = dist;
                                nearestMark = txt.text;
                            }
                        }
                    }

                    mappedColumns.Add(new {
                        index = colIndex,
                        mark = nearestMark,
                        width = w,
                        height = h,
                        revitX = revitX,
                        revitY = revitY,
                        cadX = cadX,
                        cadY = cadY,
                        layer = c.layer
                    });
                }

                return new {
                    blockDefinitionName = "COL-HTRU",
                    totalColumns = mappedColumns.Count,
                    story1TextCount = textLabels.Count,
                    columns = mappedColumns
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect COL-HTRU");
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

            _formatter.WriteSuccess($"Đã trích xuất xong dữ liệu COL-HTRU vào: {outputPath}");
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
