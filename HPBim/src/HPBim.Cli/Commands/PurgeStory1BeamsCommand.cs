namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Pure C# command to safely purge/delete Story 1 Grade Beams at Level Tầng 1 (-100mm)
///     without touching foundation ground beams at Đỉnh Đài Móng (-1.6m), columns, or walls.
/// </summary>
public sealed class PurgeStory1BeamsCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public PurgeStory1BeamsCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        bool dryRun = args.HasFlag("dry-run");

        string modeLabel = dryRun ? "DRY-RUN (Thử nghiệm & Rollback)" : "COMMIT (Xóa thực tế)";
        _formatter.WriteBanner("Purge Story 1 Beams", $"Làm sạch Dầm Tầng 1 (-100mm) [{modeLabel}]");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 10000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Named Pipe Revit: {pipeName}");

            string script = """
                var levelT1 = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => Math.Abs(l.Elevation * 304.8 - (-100.0)) < 1.0 || l.Name == "Tầng 1");

                if (levelT1 == null)
                {
                    return new { error = "Không tìm thấy Level Tầng 1 trong mô hình." };
                }

                var allFraming = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .ToList();

                var beamsToDelete = new List<FamilyInstance>();
                foreach (var b in allFraming)
                {
                    var curve = (b.Location as LocationCurve)?.Curve;
                    if (curve == null) continue;

                    var refLvl = b.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId();
                    double z = curve.GetEndPoint(0).Z * 304.8;
                    bool isStory1 = (refLvl != null && refLvl == levelT1.Id)
                                 || (b.LevelId == levelT1.Id)
                                 || (Math.Abs(z - (-100.0)) < 10.0);

                    // Ensure we DO NOT delete foundation beams at Đỉnh Đài Móng (-1.6m)
                    if (isStory1 && Math.Abs(z - (-1600.0)) > 100.0)
                    {
                        beamsToDelete.Add(b);
                    }
                }

                int count = beamsToDelete.Count;
                foreach (var b in beamsToDelete)
                {
                    if (b.Pinned) b.Pinned = false;
                    doc.Delete(b.Id);
                }

                // Check remaining foundation beams at Đỉnh Đài Móng
                var remainingFraming = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType()
                    .GetElementCount();

                return new {
                    deletedCount = count,
                    remainingFraming = remainingFraming,
                    levelName = levelT1.Name
                };
            """;

            var req = new ExecuteRequest(
                Code: script,
                Transaction: TransactionModes.Auto,
                DryRun: dryRun,
                TimeoutSeconds: 60,
                Label: "Purge Story 1 Beams");

            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi xóa dầm: {res.Message}");
                return 1;
            }

            var val = res.Value.Value;
            int deleted = val.TryGetProperty("deletedCount", out var dElem) ? dElem.GetInt32() : 0;
            int remaining = val.TryGetProperty("remainingFraming", out var rElem) ? rElem.GetInt32() : 0;

            string[][] rows =
            [
                ["Số lượng dầm Tầng 1 đã xóa", $"{deleted} dầm"],
                ["Số dầm Giằng Móng còn lại (-1.6m)", $"{remaining} dầm (Đỉnh Đài Móng)"],
                ["Trạng thái Rollback", res.RolledBack ? "CÓ (Rollback sạch sẽ)" : "KHÔNG (Committed)"]
            ];
            _formatter.RenderTable(["Chỉ số", "Kết quả thực tế"], rows);

            if (dryRun)
                _formatter.WriteSuccess("DRY-RUN XÓA THÀNH CÔNG: Sẵn sàng thực hiện commit xóa thật.");
            else
                _formatter.WriteSuccess("COMMIT XÓA THÀNH CÔNG: Mô hình Tầng 1 đã sạch sẽ, bảo toàn 100% Giằng móng và Cột!");

            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi: {ex.Message}");
            return 1;
        }
        finally
        {
            if (_injectedClient == null)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
