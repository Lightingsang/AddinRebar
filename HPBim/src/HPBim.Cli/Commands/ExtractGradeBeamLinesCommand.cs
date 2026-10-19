namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Comprehensive extraction of ALL structural grade beam lines, polylines, and skew beams
///     from AutoCAD Block DK and ModelSpace using compact array payloads to strictly prevent 64KB truncation.
/// </summary>
public sealed class ExtractGradeBeamLinesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public ExtractGradeBeamLinesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "cad_grade_beam_lines.json"));

        _formatter.WriteBanner("Extract Grade Beam Lines", "Trích xuất toàn diện 100% nét Dầm từ Block DK & ModelSpace (Compact Payload)");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 90
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối AutoCAD Named Pipe: {pipeName}");

            string script = BuildCompactExtractionScript();

            var req = new ExecuteRequest(script, TransactionModes.None, false, 90, "Extract All Beam Lines Compact");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            var val = res.Value.Value;
            var formattedPayload = ConvertCompactToStandardModel(val);

            File.WriteAllText(outputPath, JsonSerializer.Serialize(formattedPayload, JsonOptions));
            _formatter.WriteSuccess($"Đã trích xuất toàn diện và ghi vào: {outputPath}");

            _formatter.WriteInfo($"  - Nét dầm phương X: {formattedPayload.totalXLines} đoạn");
            _formatter.WriteInfo($"  - Nét dầm phương Y: {formattedPayload.totalYLines} đoạn");
            _formatter.WriteInfo($"  - Nét dầm xiên/chéo: {formattedPayload.totalDiagLines} đoạn");

            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi trích xuất: {ex.Message}");
            return 1;
        }
    }

    private static dynamic ConvertCompactToStandardModel(JsonElement val)
    {
        var xLines = new List<object>();
        var yLines = new List<object>();
        var diagLines = new List<object>();

        if (val.TryGetProperty("xLines", out var xArray))
        {
            int idx = 0;
            foreach (var item in xArray.EnumerateArray())
            {
                idx++;
                double y = item[0].GetDouble();
                double x1 = item[1].GetDouble();
                double x2 = item[2].GetDouble();
                xLines.Add(new { handle = $"XL_{idx:D3}", y = y, x1 = x1, x2 = x2, len = Math.Round(x2 - x1, 1) });
            }
        }

        if (val.TryGetProperty("yLines", out var yArray))
        {
            int idx = 0;
            foreach (var item in yArray.EnumerateArray())
            {
                idx++;
                double x = item[0].GetDouble();
                double y1 = item[1].GetDouble();
                double y2 = item[2].GetDouble();
                yLines.Add(new { handle = $"YL_{idx:D3}", x = x, y1 = y1, y2 = y2, len = Math.Round(y2 - y1, 1) });
            }
        }

        if (val.TryGetProperty("diagLines", out var dArray))
        {
            int idx = 0;
            foreach (var item in dArray.EnumerateArray())
            {
                idx++;
                double x1 = item[0].GetDouble();
                double y1 = item[1].GetDouble();
                double x2 = item[2].GetDouble();
                double y2 = item[3].GetDouble();
                double len = Math.Round(Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2)), 1);
                diagLines.Add(new { handle = $"DL_{idx:D3}", x1 = x1, y1 = y1, x2 = x2, y2 = y2, len = len });
            }
        }

        return new
        {
            totalXLines = xLines.Count,
            totalYLines = yLines.Count,
            totalDiagLines = diagLines.Count,
            xLines = xLines,
            yLines = yLines,
            diagLines = diagLines
        };
    }

    private static string BuildCompactExtractionScript() => """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        double originX = 218352.1;
        double originY = -99383.2;

        Matrix3d transform = Matrix3d.Identity;
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead);
            if (ent is BlockReference br)
            {
                var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                if (btr.Name.Equals("DK", StringComparison.OrdinalIgnoreCase))
                {
                    transform = br.BlockTransform;
                    break;
                }
            }
        }

        var xLines = new List<double[]>();
        var yLines = new List<double[]>();
        var diagLines = new List<double[]>();

        void ProcessSegment(Point3d p1W, Point3d p2W)
        {
            double rx1 = Math.Round(p1W.X - originX, 0);
            double ry1 = Math.Round(p1W.Y - originY, 0);
            double rx2 = Math.Round(p2W.X - originX, 0);
            double ry2 = Math.Round(p2W.Y - originY, 0);
            double len = Math.Round(p1W.DistanceTo(p2W), 0);
            if (len < 200.0) return;

            if (Math.Abs(ry1 - ry2) <= 25.0)
            {
                double ym = Math.Round((ry1 + ry2) / 2.0, 0);
                xLines.Add(new[] { ym, Math.Min(rx1, rx2), Math.Max(rx1, rx2) });
            }
            else if (Math.Abs(rx1 - rx2) <= 25.0)
            {
                double xm = Math.Round((rx1 + rx2) / 2.0, 0);
                yLines.Add(new[] { xm, Math.Min(ry1, ry2), Math.Max(ry1, ry2) });
            }
            else
            {
                diagLines.Add(new[] { rx1, ry1, rx2, ry2 });
            }
        }

        // 1. Block DK (Lines and Polylines)
        if (bt.Has("DK"))
        {
            var btrDK = (BlockTableRecord)tr.GetObject(bt["DK"], OpenMode.ForRead);
            foreach (ObjectId id in btrDK)
            {
                var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e is Line ln)
                {
                    ProcessSegment(ln.StartPoint.TransformBy(transform), ln.EndPoint.TransformBy(transform));
                }
                else if (e is Polyline pl)
                {
                    for (int i = 0; i < pl.NumberOfVertices - 1; i++)
                    {
                        var seg = pl.GetLineSegmentAt(i);
                        ProcessSegment(seg.StartPoint.TransformBy(transform), seg.EndPoint.TransformBy(transform));
                    }
                    if (pl.Closed && pl.NumberOfVertices > 2)
                    {
                        var seg = pl.GetLineSegmentAt(pl.NumberOfVertices - 1);
                        ProcessSegment(seg.StartPoint.TransformBy(transform), seg.EndPoint.TransformBy(transform));
                    }
                }
            }
        }

        // 2. ModelSpace within Da Kieng bounding box on beam layers
        double minX = 210000, maxX = 295000;
        double minY = -130000, maxY = -80000;

        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null || ent is BlockReference) continue;

            string lyr = ent.Layer ?? "";
            if (!lyr.Contains("DK") && !lyr.Contains("BEAM") && !lyr.Contains("Bao_DK")) continue;

            Extents3d ext;
            try { ext = ent.GeometricExtents; } catch { continue; }
            if (ext.MaxPoint.X < minX || ext.MinPoint.X > maxX || ext.MaxPoint.Y < minY || ext.MinPoint.Y > maxY) continue;

            if (ent is Line msLn)
            {
                ProcessSegment(msLn.StartPoint, msLn.EndPoint);
            }
            else if (ent is Polyline msPl)
            {
                for (int i = 0; i < msPl.NumberOfVertices - 1; i++)
                {
                    var seg = msPl.GetLineSegmentAt(i);
                    ProcessSegment(seg.StartPoint, seg.EndPoint);
                }
            }
        }

        return new {
            xLines = xLines,
            yLines = yLines,
            diagLines = diagLines
        };
    """;
}
