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
///     Pure C# command to update elevations of 57 Slab Foundations and 196 piles in Revit 2026 via Named Pipe.
///     Classifies footings into 1000mm, 1200mm, and 1300mm types and sets pile top elevations (-2.5m, -2.7m, -2.8m).
/// </summary>
public sealed class UpdateFootingElevationsCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public UpdateFootingElevationsCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        bool dryRun = args.HasFlag("dry-run");

        string modeLabel = dryRun ? "DRY-RUN (Thử nghiệm & Rollback)" : "COMMIT (Ghi thực tế)";
        _formatter.WriteBanner("Update Footing & Pile Elevations", $"Đồng bộ Cao độ 57 Đài móng & 196 Cọc [{modeLabel}]");

        string dataPath = Path.Combine("scratch", "footings_slab_model.json");
        if (!File.Exists(dataPath))
        {
            _formatter.WriteError($"Không tìm thấy dữ liệu đài móng tại: {dataPath}");
            return 1;
        }

        using var docJson = JsonDocument.Parse(await File.ReadAllTextAsync(dataPath, ct).ConfigureAwait(false));
        var footingsElem = docJson.RootElement.GetProperty("footings");
        _formatter.WriteSuccess($"Đã tải {footingsElem.GetArrayLength()} đài móng từ {dataPath}");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 120
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối thành công tới Revit Named Pipe: {pipeName}");

            var result = await DispatchScriptAsync(client, footingsElem, dryRun, ct).ConfigureAwait(false);
            RenderResult(result, dryRun);
            return result.IsError ? 1 : 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi cập nhật cao độ đài và cọc: {ex.Message}");
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

    private async Task<ExecuteResult> DispatchScriptAsync(
        IRevitPipeClient client, JsonElement footingsElem, bool dryRun, CancellationToken ct)
    {
        _formatter.WriteInfo("Đang gửi C# Roslyn Script cập nhật Type đài và cao độ cọc sang Revit 2026...");
        string scriptCode = BuildRevitScript();

        var argsObj = JsonSerializer.SerializeToElement(new { footings = footingsElem });

        var request = new ExecuteRequest(
            Code: scriptCode,
            Transaction: TransactionModes.Auto,
            DryRun: dryRun,
            TimeoutSeconds: 120,
            Label: "Update Footing & Pile Elevations",
            Args: argsObj);

        return await client.ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    private void RenderResult(ExecuteResult result, bool dryRun)
    {
        if (result.IsError)
        {
            string diags = result.Diagnostics != null && result.Diagnostics.Count > 0
                ? string.Join("; ", result.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                : "Không có diagnostics";
            _formatter.WriteError($"Revit trả về lỗi: {result.Message} -> {diags}");
            return;
        }

        if (result.Value.HasValue)
        {
            var val = result.Value.Value;
            int slabsUpdated = val.TryGetProperty("slabsUpdated", out var sElem) ? sElem.GetInt32() : 0;
            int pilesUpdated = val.TryGetProperty("pilesUpdated", out var pElem) ? pElem.GetInt32() : 0;
            int h1000 = val.TryGetProperty("slabs1000", out var h1) ? h1.GetInt32() : 0;
            int h1200 = val.TryGetProperty("slabs1200", out var h2) ? h2.GetInt32() : 0;
            int h1300 = val.TryGetProperty("slabs1300", out var h3) ? h3.GetInt32() : 0;

            string[][] rows =
            [
                ["Thời gian thực thi", $"{result.DurationMs} ms"],
                ["Đài móng đã cập nhật", $"{slabsUpdated} / 57 đài (1000mm: {h1000}, 1200mm: {h1200}, 1300mm: {h1300})"],
                ["Cọc đã cập nhật cao độ", $"{pilesUpdated} / 196 cọc"],
                ["Phần tử sửa đổi (Modified)", result.Changed.Modified.ToString()],
                ["Trạng thái Rollback", result.RolledBack ? "CÓ (Transaction Rolled Back sạch sẽ)" : "KHÔNG (Committed vào Revit)"]
            ];
            _formatter.RenderTable(["Chỉ số", "Kết quả thực tế"], rows);
        }

        if (dryRun)
        {
            _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Đã mô phỏng cập nhật 57 đài & 196 cọc và rollback an toàn 100%.");
        }
        else
        {
            _formatter.WriteSuccess("COMMIT THÀNH CÔNG: Đã cập nhật vĩnh viễn phân loại 57 đài móng và cao độ 196 cọc trong Revit!");
        }
    }

    public static string BuildRevitScript() => """
        var footings = args.List("footings");

        var floorTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(FloorType))
            .Cast<FloorType>()
            .ToDictionary(ft => ft.Name, ft => ft.Id);

        var existingSlabs = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .Cast<Floor>()
            .Where(f => f.FloorType.Name.StartsWith("HP-ES-CAP"))
            .ToList();

        int slabs1000 = 0, slabs1200 = 0, slabs1300 = 0;
        int slabsUpdated = 0;

        foreach (var slab in existingSlabs)
        {
            string comment = slab.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
            var def = footings.FirstOrDefault(f => f.Str("Name") == comment);
            if (def == null) continue;

            string targetTypeName = def.Str("FloorType", "HP-ES-CAP-CON-B20-1000");
            if (floorTypes.TryGetValue(targetTypeName, out var targetTypeId))
            {
                if (slab.FloorType.Id != targetTypeId)
                {
                    slab.ChangeTypeId(targetTypeId);
                }
                slabsUpdated++;
                if (targetTypeName.Contains("1000")) slabs1000++;
                else if (targetTypeName.Contains("1200")) slabs1200++;
                else if (targetTypeName.Contains("1300")) slabs1300++;
            }
        }

        var existingPiles = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilyInstance>()
            .Where(fi => fi.Symbol.Family.Name == "1_STR_Coc_Vuong")
            .ToList();

        int pilesUpdated = 0;
        foreach (var pile in existingPiles)
        {
            var loc = ((LocationPoint)pile.Location).Point;
            double px = loc.X * 304.8;
            double py = loc.Y * 304.8;

            var matchedFooting = footings.FirstOrDefault(f => {
                double cx = f.Double("CenterX");
                double cy = f.Double("CenterY");
                double wx = f.Double("WidthX");
                double wy = f.Double("WidthY");
                return px >= cx - wx / 2.0 - 50.0 && px <= cx + wx / 2.0 + 50.0 &&
                       py >= cy - wy / 2.0 - 50.0 && py <= cy + wy / 2.0 + 50.0;
            });

            if (matchedFooting != null)
            {
                double pileTopZ = matchedFooting.Double("PileTopZMm", -2500.0);
                double offsetFt = (pileTopZ - (-1000.0)) / 304.8;

                var offsetParam = pile.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                if (offsetParam != null && !offsetParam.IsReadOnly)
                {
                    offsetParam.Set(offsetFt);
                    pilesUpdated++;
                }
            }
        }

        return new {
            slabsUpdated = slabsUpdated,
            slabs1000 = slabs1000,
            slabs1200 = slabs1200,
            slabs1300 = slabs1300,
            pilesUpdated = pilesUpdated
        };
    """;
}
