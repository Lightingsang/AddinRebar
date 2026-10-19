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
///     Pure C# command to survey and deeply inspect the AutoCAD Da Kieng floor plan sheet.
///     Uses compact payloads to guarantee execution within Named Pipe 64KB limit.
/// </summary>
public sealed class SurveyDaKiengFloorPlanCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public SurveyDaKiengFloorPlanCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string outJson = args.GetOption("output", Path.Combine("scratch", "cad_da_kieng_survey_dossier.json"));
        string outMd = args.GetOption("report", Path.Combine("scratch", "cad_da_kieng_survey_dossier.md"));

        _formatter.WriteBanner("Survey Da Kieng Floor Plan", "Khảo sát chuyên sâu mặt bằng Đà Kiềng (AutoCAD 2026)");

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

            string script = BuildCadSurveyScript();
            var req = new ExecuteRequest(script, TransactionModes.None, false, 90, "Survey Da Kieng Floor Plan");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            var val = res.Value.Value;
            File.WriteAllText(outJson, JsonSerializer.Serialize(val, JsonOptions));
            _formatter.WriteSuccess($"Đã lưu hồ sơ khảo sát JSON: {outJson}");

            await GenerateMarkdownReportAsync(val, outMd, ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã xuất Báo cáo Khảo sát Mặt bằng Markdown: {outMd}");

            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi khảo sát: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    public static string BuildCadSurveyScript() => """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        double minX = 210000, maxX = 275000;
        double minY = -105000, maxY = -70000;

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

        var texts = new List<object>();
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null) continue;

            string txt = "";
            Point3d pos = Point3d.Origin;
            if (ent is DBText dt) { txt = dt.TextString; pos = dt.Position; }
            else if (ent is MText mt) { txt = mt.Contents; pos = mt.Location; }
            else continue;

            if (pos.X < minX || pos.X > maxX || pos.Y < minY || pos.Y > maxY) continue;
            if (string.IsNullOrWhiteSpace(txt)) continue;

            double rx = Math.Round(pos.X - originX, 0);
            double ry = Math.Round(pos.Y - originY, 0);
            texts.Add(new[] { txt, ent.Layer ?? "", rx.ToString(), ry.ToString() });
        }

        var lines = new List<double[]>();
        void AddSeg(Point3d p1, Point3d p2)
        {
            double rx1 = Math.Round(p1.X - originX, 0);
            double ry1 = Math.Round(p1.Y - originY, 0);
            double rx2 = Math.Round(p2.X - originX, 0);
            double ry2 = Math.Round(p2.Y - originY, 0);
            double len = Math.Round(p1.DistanceTo(p2), 0);
            if (len < 150.0) return;

            if (Math.Abs(ry1 - ry2) <= 20.0)
            {
                double ym = Math.Round((ry1 + ry2) / 2.0, 0);
                lines.Add(new[] { 0.0, ym, Math.Min(rx1, rx2), Math.Max(rx1, rx2), len });
            }
            else if (Math.Abs(rx1 - rx2) <= 20.0)
            {
                double xm = Math.Round((rx1 + rx2) / 2.0, 0);
                lines.Add(new[] { 1.0, xm, Math.Min(ry1, ry2), Math.Max(ry1, ry2), len });
            }
            else
            {
                lines.Add(new[] { 2.0, rx1, ry1, rx2, ry2, len });
            }
        }

        if (bt.Has("DK"))
        {
            var btrDK = (BlockTableRecord)tr.GetObject(bt["DK"], OpenMode.ForRead);
            foreach (ObjectId id in btrDK)
            {
                var e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e is Line ln) AddSeg(ln.StartPoint.TransformBy(transform), ln.EndPoint.TransformBy(transform));
                else if (e is Polyline pl)
                {
                    for (int i = 0; i < pl.NumberOfVertices - 1; i++)
                    {
                        var s = pl.GetLineSegmentAt(i);
                        AddSeg(s.StartPoint.TransformBy(transform), s.EndPoint.TransformBy(transform));
                    }
                    if (pl.Closed && pl.NumberOfVertices > 2)
                    {
                        var s = pl.GetLineSegmentAt(pl.NumberOfVertices - 1);
                        AddSeg(s.StartPoint.TransformBy(transform), s.EndPoint.TransformBy(transform));
                    }
                }
            }
        }

        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null || ent is BlockReference) continue;

            string lyr = ent.Layer ?? "";
            if (!lyr.Contains("DK") && !lyr.Contains("BEAM") && !lyr.Contains("Bao_DK")) continue;

            Extents3d ext;
            try { ext = ent.GeometricExtents; } catch { continue; }
            if (ext.MaxPoint.X < minX || ext.MinPoint.X > maxX || ext.MaxPoint.Y < minY || ext.MinPoint.Y > maxY) continue;

            if (ent is Line msLn) AddSeg(msLn.StartPoint, msLn.EndPoint);
            else if (ent is Polyline msPl)
            {
                for (int i = 0; i < msPl.NumberOfVertices - 1; i++)
                {
                    var s = msPl.GetLineSegmentAt(i);
                    AddSeg(s.StartPoint, s.EndPoint);
                }
            }
        }

        return new {
            origin = new { x = originX, y = originY },
            totalTexts = texts.Count,
            totalSegments = lines.Count,
            texts = texts,
            segments = lines
        };
    """;

    private async Task GenerateMarkdownReportAsync(JsonElement val, string outPath, CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# BÁO CÁO KHẢO SÁT & PHÂN TÍCH HÌNH HỌC MẶT BẰNG ĐÀ KIỀNG (TẦNG 1)");
        sb.AppendLine("## THỰC THI THUẦN C# .NET NATIVE TRỰC TIẾP QUA AUTOCAD MCP BRIDGE");
        sb.AppendLine();
        sb.AppendLine($"* **Thời gian khảo sát**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"* **Gốc tọa độ quy ước**: Giao Trục 1 - Trục A (CAD X=218352.1, Y=-99383.2)");
        sb.AppendLine($"* **Phạm vi khảo sát**: X: 0 đến +48900 mm, Y: 0 đến +27900 mm");

        int textCount = val.GetProperty("totalTexts").GetInt32();
        int segCount = val.GetProperty("totalSegments").GetInt32();
        sb.AppendLine($"* **Tổng số thực thể text thu thập**: {textCount}");
        sb.AppendLine($"* **Tổng số đoạn nét hình học**: {segCount}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        sb.AppendLine("### 1. BẢNG THỐNG KÊ CÁC NHÃN DẦM & TIẾT DIỆN TRÊN MẶT BẰNG");
        sb.AppendLine();
        sb.AppendLine("| Nội dung Text | Layer | Tọa độ Rx (mm) | Tọa độ Ry (mm) |");
        sb.AppendLine("|:---|:---|:---:|:---:|");

        var texts = val.GetProperty("texts").EnumerateArray();
        int shown = 0;
        foreach (var t in texts)
        {
            var arr = t.EnumerateArray().ToList();
            string txt = arr[0].GetString() ?? "";
            string lyr = arr[1].GetString() ?? "";
            string rx = arr[2].GetString() ?? "";
            string ry = arr[3].GetString() ?? "";

            if (txt.Contains("DK") || txt.Contains("x") || txt.Contains("DX"))
            {
                sb.AppendLine($"| `{txt}` | {lyr} | {rx} | {ry} |");
                shown++;
            }
        }

        sb.AppendLine();
        sb.AppendLine($"*(Tổng số nhãn đà kiềng/tiết diện tìm thấy: {shown})*");
        sb.AppendLine();

        await File.WriteAllTextAsync(outPath, sb.ToString(), System.Text.Encoding.UTF8, ct).ConfigureAwait(false);
    }
}
