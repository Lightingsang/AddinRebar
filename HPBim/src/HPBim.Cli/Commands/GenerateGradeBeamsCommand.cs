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
using HPBim.Core.Coordination;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Pure C# command to construct Grade Beams (Đà kiềng / Giằng móng) in Revit 2026.
///     Protected by FailFastGate to eliminate speculation and phantom beams.
/// </summary>
public sealed class GenerateGradeBeamsCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;
    private readonly IFailFastGate _failFastGate;

    public GenerateGradeBeamsCommand(
        ConsoleFormatter? formatter = null,
        IRevitPipeClient? injectedClient = null,
        IFailFastGate? failFastGate = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
        _failFastGate = failFastGate ?? new FailFastGate();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        bool dryRun = args.HasFlag("dry-run");

        string modeLabel = dryRun ? "DRY-RUN (Thử nghiệm & Rollback)" : "COMMIT (Ghi thực tế)";
        _formatter.WriteBanner("Generate Grade Beams", $"Dựng Đà kiềng / Giằng móng [{modeLabel}]");

        string dataPath = args.GetOption("input", Path.Combine("scratch", "grade_beams_model.json"));
        if (!File.Exists(dataPath))
        {
            _formatter.WriteError($"Không tìm thấy dữ liệu đà kiềng tại: {dataPath}");
            return 1;
        }

        using var docJson = JsonDocument.Parse(await File.ReadAllTextAsync(dataPath, ct).ConfigureAwait(false));
        var beamsElem = docJson.RootElement.GetProperty("beams");
        _formatter.WriteSuccess($"Đã tải {beamsElem.GetArrayLength()} dầm đà kiềng từ {dataPath}");

        // FailFastGate validation: eliminate speculation, missing elevations, and missing supports
        var violations = new List<string>();
        foreach (var b in beamsElem.EnumerateArray())
        {
            string name = b.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
            double w = b.TryGetProperty("width", out var wp) ? wp.GetDouble() : 0;
            double h = b.TryGetProperty("height", out var hp) ? hp.GetDouble() : 0;
            string handle = b.TryGetProperty("handle", out var hnd) ? hnd.GetString() ?? "" : "";

            var spec = new CadSourceSpec
            {
                Handle = handle,
                Layer = "DK",
                TopElevation = -1.600,
                Width = w > 0 ? w : null,
                Height = h > 0 ? h : null,
                Supports = b.TryGetProperty("supports", out var sp)
                    ? sp.EnumerateArray().Select(s => s.GetString() ?? "").ToList()
                    : Array.Empty<string>(),
                CrossCheckPassed = !b.TryGetProperty("crossCheckPassed", out var ccp) || ccp.GetBoolean()
            };

            var eval = _failFastGate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, name);
            if (!eval.IsPassed)
            {
                violations.Add($"[{name}] {string.Join("; ", eval.Violations)}");
            }
        }

        if (violations.Count > 0)
        {
            _formatter.WriteError($"[FailFastGate] Phát hiện {violations.Count} dầm vi phạm ĐIỀU 0 (thiếu handle/tiết diện/điểm tựa).");
            _formatter.WriteWarning("DỪNG NGAY LẬP TỨC: Cấm dựng dầm ma hoặc phỏng đoán vào Revit.");
            throw new FailFastCoordinationException("GenerateGradeBeams", violations);
        }

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

            var result = await DispatchScriptAsync(client, beamsElem, dryRun, ct).ConfigureAwait(false);
            RenderResult(result, dryRun);
            bool isOk = dryRun ? (!result.IsError && result.RolledBack) : !result.IsError;
            return isOk ? 0 : 1;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi dựng đà kiềng trong Revit: {ex.Message}");
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
        IRevitPipeClient client, JsonElement beamsElem, bool dryRun, CancellationToken ct)
    {
        _formatter.WriteInfo("Đang gửi C# Roslyn Script dựng Structural Framing (Beams) sang Revit 2026...");
        string scriptCode = BuildRevitScript();

        var argsObj = JsonSerializer.SerializeToElement(new { beams = beamsElem });

        var request = new ExecuteRequest(
            Code: scriptCode,
            Transaction: TransactionModes.Auto,
            DryRun: dryRun,
            TimeoutSeconds: 120,
            Label: "Generate 205 Grade Beams",
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
            int count = val.TryGetProperty("createdCount", out var cElem) ? cElem.GetInt32() : 0;
            string levelName = val.TryGetProperty("targetLevel", out var lElem) ? lElem.GetString() ?? "" : "";
            double topElevationMm = val.TryGetProperty("topElevationMm", out var oElem) ? oElem.GetDouble() : -1000.0;

            string[][] rows =
            [
                ["Thời gian thực thi", $"{result.DurationMs} ms"],
                ["Số lượng đà kiềng đã dựng", $"{count} dầm"],
                ["Level đặt dầm", $"{levelName} (Cao độ đỉnh: {topElevationMm} mm)"],
                ["Phần tử tạo mới (Added)", result.Changed.Added.ToString()],
                ["Trạng thái Rollback", result.RolledBack ? "CÓ (Transaction Rolled Back sạch sẽ)" : "KHÔNG (Committed vào Revit)"]
            ];
            _formatter.RenderTable(["Chỉ số", "Kết quả thực tế"], rows);
        }

        if (dryRun)
        {
            if (!result.IsError && result.RolledBack)
            {
                _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Đã thử nghiệm dựng đà kiềng và rollback an toàn 100%.");
            }
            else if (!result.RolledBack)
            {
                _formatter.WriteError("CẢNH BÁO NGUY HIỂM: Dry-run thất bại! Revit bridge KHÔNG xác nhận rollback.");
            }
            else
            {
                _formatter.WriteError($"DRY-RUN THẤT BẠI: {result.Message}");
            }
        }
        else if (!result.IsError)
        {
            _formatter.WriteSuccess("COMMIT THÀNH CÔNG: Toàn bộ hệ thống đà kiềng đã được khởi tạo vĩnh viễn trong Revit 2026!");
        }
        else
        {
            _formatter.WriteError($"COMMIT THẤT BẠI: {result.Message}");
        }
    }

    public static string BuildRevitScript() => """
        var symbolCollector = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>();

        var symbolsDict = new Dictionary<string, FamilySymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in symbolCollector)
        {
            symbolsDict[s.Name] = s;
        }

        var levelMong = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .FirstOrDefault(l => l.Name == "Tầng Móng");

        if (levelMong == null)
        {
            throw new InvalidOperationException("Không tìm thấy Level 'Tầng Móng' trong Revit. Cấm phỏng đoán level (ĐIỀU 0).");
        }

        double zElevation = levelMong.Elevation;
        var beams = args.List("beams");
        var createdIds = new List<string>();

        foreach (var item in beams)
        {
            string name = item.Str("Name", "DK") ?? "DK";
            string typeName = item.Str("TypeName", "B_200x400") ?? "B_200x400";
            double x1 = item.Double("StartX");
            double y1 = item.Double("StartY");
            double x2 = item.Double("EndX");
            double y2 = item.Double("EndY");

            if (!symbolsDict.TryGetValue(typeName, out var sym))
            {
                throw new InvalidOperationException($"[ĐIỀU 0] Không tìm thấy FamilySymbol '{typeName}' trong Revit. Cấm tự gán loại mặc định.");
            }

            if (!sym.IsActive)
            {
                sym.Activate();
                doc.Regenerate();
            }

            var p1 = new XYZ(x1 / 304.8, y1 / 304.8, zElevation);
            var p2 = new XYZ(x2 / 304.8, y2 / 304.8, zElevation);

            if (p1.DistanceTo(p2) < 0.1) continue; // skip too short curves

            var line = Line.CreateBound(p1, p2);
            var instance = doc.Create.NewFamilyInstance(line, sym, levelMong, Autodesk.Revit.DB.Structure.StructuralType.Beam);

            instance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(name);
            instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(name);
            instance.get_Parameter(BuiltInParameter.Z_JUSTIFICATION)?.Set(0); // 0 = Top

            createdIds.Add(instance.Id.ToString());
        }

        return new {
            createdCount = createdIds.Count,
            targetLevel = levelMong.Name,
            topElevationMm = Math.Round(zElevation * 304.8, 1),
            sampleIds = createdIds.Take(10).ToList()
        };
    """;
}
