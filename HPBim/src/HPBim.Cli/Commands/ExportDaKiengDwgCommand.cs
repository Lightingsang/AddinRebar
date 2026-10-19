namespace HPBim.Cli.Commands;

using System;
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
///     Pure C# command to export a clean, normalized DWG of Da Kieng floor plan from AutoCAD 2026.
///     Sets origin at Grid 1-A (0,0,0) and units to Millimeters.
/// </summary>
public sealed class ExportDaKiengDwgCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public ExportDaKiengDwgCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.AutocadHost, 2026));
        string defaultOut = Path.GetFullPath(Path.Combine("scratch", "MatBang_DaKieng.dwg"));
        string targetDwg = args.GetOption("output", defaultOut);

        _formatter.WriteBanner("Export Da Kieng DWG", "Trích xuất Mặt bằng Đà Kiềng sang DWG chuẩn gốc tọa độ Trục 1-A (0,0)");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 10000,
            DefaultExecuteTimeoutSeconds = 120
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối AutoCAD Named Pipe: {pipeName}");

            string script = BuildCadExportScript(targetDwg);
            var req = new ExecuteRequest(script, TransactionModes.Auto, true, 120, "Export Da Kieng DWG");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return 1;
            }

            var val = res.Value.Value;
            RenderExportSummary(val, targetDwg);
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi trích xuất DWG: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string BuildCadExportScript(string targetDwgPath)
    {
        string? dir = Path.GetDirectoryName(targetDwgPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string escapedPath = targetDwgPath.Replace("\\", "\\\\");
        return $$"""
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            // Bounding box of Sheet KC-09 Da Kieng
            double minX = 208000, maxX = 278000;
            double minY = -106000, maxY = -68000;
            double originX = 218352.1;
            double originY = -99383.2;

            var ids = new ObjectIdCollection();
            int candidateCount = 0;
            var blocksToExplode = new List<BlockReference>();

            foreach (ObjectId id in ms)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;

                bool inRange = false;
                if (ent is BlockReference br)
                {
                    if (br.Position.X >= minX && br.Position.X <= maxX &&
                        br.Position.Y >= minY && br.Position.Y <= maxY)
                    {
                        inRange = true;
                    }
                }

                if (!inRange)
                {
                    try
                    {
                        var ext = ent.GeometricExtents;
                        if (ext.MaxPoint.X >= minX && ext.MinPoint.X <= maxX &&
                            ext.MaxPoint.Y >= minY && ext.MinPoint.Y <= maxY)
                        {
                            inRange = true;
                        }
                    }
                    catch { }
                }

                if (inRange)
                {
                    if (ent is BlockReference brTarget)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(brTarget.BlockTableRecord, OpenMode.ForRead);
                        if (btr.Name.Equals("DK", StringComparison.OrdinalIgnoreCase))
                        {
                            blocksToExplode.Add(brTarget);
                            continue;
                        }
                    }

                    ids.Add(id);
                    candidateCount++;
                }
            }

            var disp = Matrix3d.Displacement(new Vector3d(-originX, -originY, 0.0));
            var exportIds = new ObjectIdCollection();

            foreach (ObjectId id in ids)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;
                var clone = ent.Clone() as Entity;
                if (clone == null) continue;
                clone.TransformBy(disp);
                ms.AppendEntity(clone);
                tr.AddNewlyCreatedDBObject(clone, true);
                exportIds.Add(clone.ObjectId);
            }

            int explodedCount = 0;
            foreach (var br in blocksToExplode)
            {
                var exploded = new DBObjectCollection();
                br.Explode(exploded);
                foreach (DBObject obj in exploded)
                {
                    if (obj is Entity e)
                    {
                        e.TransformBy(disp);
                        ms.AppendEntity(e);
                        tr.AddNewlyCreatedDBObject(e, true);
                        exportIds.Add(e.ObjectId);
                        explodedCount++;
                    }
                }
            }

            if (exportIds.Count == 0)
            {
                return new { success = false, error = "Không tìm thấy thực thể trong vùng mặt bằng Đà Kiềng." };
            }

            using (var outDb = db.Wblock(exportIds, Point3d.Origin))
            {
                outDb.Insunits = UnitsValue.Millimeters;
                outDb.Measurement = MeasurementValue.Metric;
                outDb.SaveAs("{{escapedPath}}", DwgVersion.Current);
            }

            return new {
                success = true,
                exportedEntities = exportIds.Count,
                explodedBeamLines = explodedCount,
                originX = originX,
                originY = originY
            };
        """;
    }

    private void RenderExportSummary(JsonElement val, string targetDwg)
    {
        bool success = val.TryGetProperty("success", out var sElem) && sElem.GetBoolean();
        if (!success)
        {
            string err = val.TryGetProperty("error", out var eElem) ? eElem.GetString() ?? "Lỗi không xác định" : "Lỗi";
            _formatter.WriteError($"Xuất DWG thất bại: {err}");
            return;
        }

        int count = val.TryGetProperty("exportedEntities", out var cElem) ? cElem.GetInt32() : 0;
        var fi = new FileInfo(targetDwg);
        long bytes = fi.Exists ? fi.Length : 0;

        string[][] rows =
        [
            ["Đường dẫn file DWG", targetDwg],
            ["Số lượng thực thể xuất", $"{count} thực thể"],
            ["Dung lượng file", $"{bytes / 1024.0:F1} KB"],
            ["Gốc tọa độ quy ước (Trục 1-A)", "CAD: (218352.1, -99383.2) -> Mới: (0.0, 0.0)"],
            ["Đơn vị bản vẽ", "Millimeters (INSUNITS = 4)"]
        ];

        _formatter.RenderTable(["Thông số", "Giá trị"], rows);
        _formatter.WriteSuccess("XUẤT DWG THÀNH CÔNG: File đã sẵn sàng để Link vào Revit!");
    }
}
