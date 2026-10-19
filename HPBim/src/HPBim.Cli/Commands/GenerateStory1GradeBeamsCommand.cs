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
///     Pure C# command to construct Story 1 Grade Beams (Dầm Sàn Tầng 1 / Đà Kiềng)
///     in Revit 2026 at Level Tầng 1 (Elevation -100mm) with Left/Origin Justification.
/// </summary>
public sealed class GenerateStory1GradeBeamsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;
    private readonly IFailFastGate _failFastGate;

    public GenerateStory1GradeBeamsCommand(
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
        string inputPath = args.GetOption("input", Path.Combine("scratch", "story1_grade_beams_model.json"));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "story1_grade_beams_created.json"));

        string modeLabel = dryRun ? "DRY-RUN (Thử nghiệm & Rollback)" : "COMMIT (Ghi thực tế)";
        _formatter.WriteBanner("Generate Story 1 Grade Beams", $"Dựng Dầm Sàn Tầng 1 / Đà Kiềng [{modeLabel}]");

        if (!File.Exists(inputPath))
        {
            _formatter.WriteError($"Không tìm thấy file mô hình tại: {inputPath}");
            return 1;
        }

        using var inDoc = JsonDocument.Parse(await File.ReadAllTextAsync(inputPath, ct).ConfigureAwait(false));
        var root = inDoc.RootElement;
        var beamsElem = root.GetProperty("beams");
        _formatter.WriteSuccess($"Đã nạp {beamsElem.GetArrayLength()} dầm từ {inputPath}");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 10000,
            DefaultExecuteTimeoutSeconds = 180
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Named Pipe Revit: {pipeName}");

            var res = await DispatchCreationScriptAsync(client, beamsElem, dryRun, ct).ConfigureAwait(false);
            RenderResult(res, dryRun);

            if (!res.IsError && res.Value.HasValue)
            {
                File.WriteAllText(outputPath, JsonSerializer.Serialize(res.Value.Value, JsonOptions));
                _formatter.WriteSuccess($"Đã ghi nhận vết cấu kiện khởi tạo vào: {outputPath}");
            }

            return res.IsError ? 1 : 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thi công dầm trong Revit: {ex.Message}");
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

    private async Task<ExecuteResult> DispatchCreationScriptAsync(
        IRevitPipeClient client, JsonElement beamsElem, bool dryRun, CancellationToken ct)
    {
        _formatter.WriteInfo("Đang gửi C# Roslyn Script khởi tạo hệ Dầm Sàn Tầng 1 sang Revit 2026...");
        string scriptCode = BuildRevitScript();
        var argsObj = JsonSerializer.SerializeToElement(new { beams = beamsElem });

        var req = new ExecuteRequest(
            Code: scriptCode,
            Transaction: TransactionModes.Auto,
            DryRun: dryRun,
            TimeoutSeconds: 180,
            Label: "Generate Story 1 Grade Beams",
            Args: argsObj);

        return await client.ExecuteAsync(req, ct).ConfigureAwait(false);
    }

    private void RenderResult(ExecuteResult res, bool dryRun)
    {
        if (res.IsError)
        {
            string diags = res.Diagnostics != null && res.Diagnostics.Count > 0
                ? string.Join("; ", res.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                : "Không có diagnostics";
            _formatter.WriteError($"Revit trả về lỗi: {res.Message} -> {diags}");
            return;
        }

        if (res.Value.HasValue)
        {
            var val = res.Value.Value;
            int count = val.TryGetProperty("createdCount", out var cElem) ? cElem.GetInt32() : 0;
            string levelName = val.TryGetProperty("targetLevel", out var lElem) ? lElem.GetString() ?? "" : "";
            double topMm = val.TryGetProperty("topElevationMm", out var oElem) ? oElem.GetDouble() : -100.0;

            string[][] rows =
            [
                ["Thời gian thực thi", $"{res.DurationMs} ms"],
                ["Số lượng Dầm đã dựng", $"{count} dầm"],
                ["Level tham chiếu", $"{levelName} (Cao độ đỉnh: {topMm} mm)"],
                ["Phần tử tạo mới (Added)", res.Changed.Added.ToString()],
                ["Trạng thái Rollback", res.RolledBack ? "CÓ (Rollback sạch sẽ)" : "KHÔNG (Committed vào Revit)"]
            ];
            _formatter.RenderTable(["Chỉ số", "Kết quả thực tế"], rows);
        }

        if (dryRun)
            _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Đã kiểm tra tạo dầm và rollback an toàn 100%.");
        else
            _formatter.WriteSuccess("COMMIT THÀNH CÔNG: Hệ Dầm Sàn Tầng 1 đã được tạo vĩnh viễn trong Revit!");
    }

    public static string BuildRevitScript() => """
        var symbolCollector = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>();

        var symbolsDict = new Dictionary<string, FamilySymbol>(StringComparer.OrdinalIgnoreCase);
        bool symActivated = false;
        foreach (var s in symbolCollector)
        {
            symbolsDict[s.Name] = s;
            if (!s.IsActive)
            {
                s.Activate();
                symActivated = true;
            }
        }
        if (symActivated)
        {
            doc.Regenerate();
        }

        var levelT1 = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .FirstOrDefault(l => Math.Abs(l.Elevation * 304.8 - (-100.0)) < 1.0 || l.Name == "Tầng 1")
            ?? throw new InvalidOperationException("Không tìm thấy Level Tầng 1 (-100mm) trong Revit!");

        double zElevation = levelT1.Elevation;
        var beams = args.List("beams");
        var createdList = new List<object>();

        foreach (var item in beams)
        {
            string name = item.Str("Name", "DK") ?? "DK";
            string mark = item.Str("Mark", name) ?? name;
            string typeName = item.Str("TypeName", "B_200x400") ?? "B_200x400";
            double x1 = item.Double("StartX");
            double y1 = item.Double("StartY");
            double x2 = item.Double("EndX");
            double y2 = item.Double("EndY");
            int yJust = (int)item.Double("YJustification");
            double yOffsetMm = item.Double("YOffsetMm");

            if (!symbolsDict.TryGetValue(typeName, out var sym))
            {
                var baseSymbol = symbolsDict.Values.FirstOrDefault(s => s.FamilyName.Contains("ConcreteRectangular"))
                              ?? symbolsDict.Values.FirstOrDefault(s => s.FamilyName.Contains("Concrete"))
                              ?? symbolsDict.Values.First();
                var parts = typeName.Replace("B_", "").Split('x');
                if (parts.Length == 2 && double.TryParse(parts[0], out double bW) && double.TryParse(parts[1], out double bH))
                {
                    sym = baseSymbol.Duplicate(typeName) as FamilySymbol;
                    var pB = sym.LookupParameter("b") ?? sym.LookupParameter("Width") ?? sym.LookupParameter("B");
                    var pH = sym.LookupParameter("h") ?? sym.LookupParameter("Height") ?? sym.LookupParameter("H");
                    if (pB != null) pB.Set(bW / 304.8);
                    if (pH != null) pH.Set(bH / 304.8);
                    symbolsDict[typeName] = sym;
                    if (!sym.IsActive) sym.Activate();
                }
                else
                {
                    throw new InvalidOperationException($"[ĐIỀU 0] Không tìm thấy FamilySymbol '{typeName}' trong Revit.");
                }
            }

            var p1 = new XYZ(x1 / 304.8, y1 / 304.8, zElevation);
            var p2 = new XYZ(x2 / 304.8, y2 / 304.8, zElevation);

            if (p1.DistanceTo(p2) < 0.1) continue;

            var line = Line.CreateBound(p1, p2);
            var instance = doc.Create.NewFamilyInstance(line, sym, levelT1, Autodesk.Revit.DB.Structure.StructuralType.Beam);

            // Z Justification: Top (0) -> Top of beam is at Level elevation (-100mm)
            instance.get_Parameter(BuiltInParameter.Z_JUSTIFICATION)?.Set(0);
            instance.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE)?.Set(0.0);

            // Y Justification: 0 = Left, 2 = Origin
            instance.get_Parameter(BuiltInParameter.Y_JUSTIFICATION)?.Set(yJust);
            if (Math.Abs(yOffsetMm) > 0.01)
            {
                instance.get_Parameter(BuiltInParameter.Y_OFFSET_VALUE)?.Set(yOffsetMm / 304.8);
            }

            // Disallow join at both ends to prevent automatic miter cut artifacts
            Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(instance, 0);
            Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(instance, 1);

            // Set Mark and Comments
            instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(mark);
            instance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(name);

            createdList.Add(new {
                id = instance.Id.Value,
                mark = mark,
                name = name,
                type = typeName,
                lengthMm = Math.Round(line.Length * 304.8, 1),
                yJust = yJust,
                yOffsetMm = Math.Round(yOffsetMm, 1)
            });
        }

        return new {
            createdCount = createdList.Count,
            targetLevel = levelT1.Name,
            topElevationMm = Math.Round(zElevation * 304.8, 1),
            createdBeams = createdList
        };
    """;
}
