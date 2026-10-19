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
///     Pure C# command to construct 72 Short Columns (Cổ cột móng) and 72 Story 1 Columns in Revit 2026.
///     Protected by FailFastGate to eliminate speculation.
/// </summary>
public sealed class GenerateColumnsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public GenerateColumnsCommand(
        ConsoleFormatter? formatter = null,
        IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        bool dryRun = args.HasFlag("dry-run");

        string modeLabel = dryRun ? "DRY-RUN (Thử nghiệm & Rollback)" : "COMMIT (Ghi thực tế)";
        _formatter.WriteBanner("Generate Columns", $"Dựng Cổ Cột Móng & Cột Tầng 1 [{modeLabel}]");

        string dataPath = args.GetOption("input", Path.Combine("scratch", "columns_story1_model.json"));
        if (!File.Exists(dataPath))
        {
            _formatter.WriteError($"Không tìm thấy file mô hình cột: {dataPath}");
            return 1;
        }

        using var docJson = JsonDocument.Parse(await File.ReadAllTextAsync(dataPath, ct).ConfigureAwait(false));
        var colsElem = docJson.RootElement.GetProperty("columns");
        var typesElem = docJson.RootElement.GetProperty("typesNeeded");
        int totalCols = colsElem.GetArrayLength();
        _formatter.WriteSuccess($"Đã tải {totalCols} vị trí cột ({totalCols * 2} phân đoạn) từ {dataPath}");

        var violations = ValidateColumnData(colsElem);
        if (violations.Count > 0)
        {
            _formatter.WriteError($"[FailFastGate] Phát hiện {violations.Count} vi phạm dữ liệu cột:");
            foreach (var v in violations) _formatter.WriteError($"  - {v}");
            return 2;
        }

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 10000,
            DefaultExecuteTimeoutSeconds = 120
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Named Pipe Revit: {pipeName}");

            var argsObj = JsonSerializer.SerializeToElement(new
            {
                typesNeeded = typesElem,
                columns = colsElem
            });

            var req = new ExecuteRequest(
                Code: BuildRevitScript(),
                Transaction: TransactionModes.Auto,
                DryRun: dryRun,
                TimeoutSeconds: 120,
                Label: "Generate Story 1 Columns",
                Args: argsObj);

            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                string diags = res.Diagnostics != null && res.Diagnostics.Count > 0
                    ? string.Join("; ", res.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                    : "Không có diagnostics";
                _formatter.WriteError($"Lỗi Revit API: {res.Message} -> {diags}");
                return 3;
            }

            return ProcessScriptResult(res, dryRun);
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kết nối Revit: {ex.Message}");
            return 4;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static List<string> ValidateColumnData(JsonElement colsElem)
    {
        var violations = new List<string>();
        int i = 0;
        foreach (var c in colsElem.EnumerateArray())
        {
            i++;
            double w = c.GetProperty("widthMm").GetDouble();
            double h = c.GetProperty("heightMm").GetDouble();
            if (w < 200 || h < 200)
                violations.Add($"Cột #{i} có kích thước không hợp lệ: {w}x{h} mm");
        }
        return violations;
    }

    private int ProcessScriptResult(ExecuteResult res, bool dryRun)
    {
        var val = res.Value!.Value;
        string json = JsonSerializer.Serialize(val, JsonOptions);
        string outputPath = Path.Combine("scratch", "columns_revit_created.json");
        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outputPath, json);

        int ccCount = val.TryGetProperty("coCotCreated", out var cc) ? cc.GetInt32() : 0;
        int c1Count = val.TryGetProperty("cotTang1Created", out var c1) ? c1.GetInt32() : 0;
        int typesCreated = val.TryGetProperty("typesCreated", out var tc) ? tc.GetInt32() : 0;

        _formatter.WriteInfo($"Kết quả thực thi (dryRun={dryRun}):");
        _formatter.WriteInfo($"  - Cổ Cột Móng tạo mới: {ccCount}/72");
        _formatter.WriteInfo($"  - Cột Tầng 1 tạo mới: {c1Count}/72");
        _formatter.WriteInfo($"  - Types sinh mới: {typesCreated}");
        _formatter.WriteInfo($"  - Added: {res.Changed.Added}, RolledBack: {res.RolledBack}");
        _formatter.WriteSuccess($"Đã lưu kết quả chi tiết: {outputPath}");

        bool countsMatch = ccCount == 72 && c1Count == 72;
        if (dryRun)
        {
            if (res.RolledBack && countsMatch)
            {
                _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Đã dựng thử nghiệm 144 cột và rollback 100% an toàn.");
                return 0;
            }
            if (!res.RolledBack)
            {
                _formatter.WriteError("CẢNH BÁO NGUY HIỂM: Dry-run thất bại! Revit bridge KHÔNG xác nhận rollback.");
                return 5;
            }
            _formatter.WriteError($"DRY-RUN THẤT BẠI: Số lượng cột không đủ yêu cầu (Cổ cột: {ccCount}/72, Cột T1: {c1Count}/72).");
            return 5;
        }

        if (countsMatch)
        {
            _formatter.WriteSuccess("COMMIT THÀNH CÔNG: Đã tạo 144 cột vào Revit.");
            return 0;
        }

        _formatter.WriteError($"COMMIT THẤT BẠI: Số lượng cột không đủ yêu cầu (Cổ cột: {ccCount}/72, Cột T1: {c1Count}/72).");
        return 5;
    }

    private static string BuildRevitScript() => """
        // 1. Locate Levels
        Level levelDinhDai = null;
        Level levelTang1 = null;
        Level levelTang2 = null;

        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>();

        foreach (var lvl in levels)
        {
            if (lvl.Name == "Đỉnh Đài Móng") levelDinhDai = lvl;
            else if (lvl.Name == "Tầng 1 (±0.000)") levelTang1 = lvl;
            else if (lvl.Name == "Tầng 2") levelTang2 = lvl;
        }

        if (levelDinhDai == null || levelTang1 == null || levelTang2 == null)
        {
            throw new InvalidOperationException("Thiếu Level cần thiết (Đỉnh Đài Móng, Tầng 1 (±0.000), Tầng 2)");
        }

        // 2. Locate Concrete Column Family & Types
        var symbols = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralColumns)
            .WhereElementIsElementType()
            .Cast<FamilySymbol>()
            .ToList();

        FamilySymbol baseSymbol = symbols.FirstOrDefault(s => s.FamilyName.Contains("1_STR_Column_ConcreteRectangular_DungTam"))
                               ?? symbols.FirstOrDefault(s => s.FamilyName.Contains("Column_ConcreteRectangular"));

        if (baseSymbol == null)
        {
            throw new InvalidOperationException("Không tìm thấy FamilySymbol cột bê tông hình chữ nhật trong mô hình. Cấm phỏng đoán symbol (ĐIỀU 0).");
        }

        var symbolDict = new Dictionary<string, FamilySymbol>();
        foreach (var s in symbols)
        {
            if (s.Family.Id == baseSymbol.Family.Id && !symbolDict.ContainsKey(s.Name))
                symbolDict[s.Name] = s;
        }

        int typesCreated = 0;
        var typesNeeded = args.List("typesNeeded");
        foreach (var tn in typesNeeded)
        {
            string tName = tn.Str("name");
            double wMm = tn.Double("widthMm");
            double hMm = tn.Double("heightMm");

            if (!symbolDict.ContainsKey(tName))
            {
                var newSym = baseSymbol.Duplicate(tName) as FamilySymbol;
                var pB = newSym.LookupParameter("b") ?? newSym.LookupParameter("Width") ?? newSym.LookupParameter("B");
                var pH = newSym.LookupParameter("h") ?? newSym.LookupParameter("Height") ?? newSym.LookupParameter("H");
                if (pB != null) pB.Set(wMm / 304.8);
                if (pH != null) pH.Set(hMm / 304.8);

                symbolDict[tName] = newSym;
                typesCreated++;
            }

            if (!symbolDict[tName].IsActive)
                symbolDict[tName].Activate();
        }

        // 3. Create Column Instances
        var cols = args.List("columns");
        var ccIds = new List<long>();
        var c1Ids = new List<long>();

        foreach (var c in cols)
        {
            int idx = c.Int("index");
            string tName = c.Str("typeName");
            double xMm = c.Double("xMm");
            double yMm = c.Double("yMm");

            var sym = symbolDict[tName];
            var pt = new XYZ(xMm / 304.8, yMm / 304.8, 0);

            // A. Cổ Cột Móng (-1600mm to 0mm)
            var instCC = doc.Create.NewFamilyInstance(pt, sym, levelTang1, Autodesk.Revit.DB.Structure.StructuralType.Column);
            instCC.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.Set(levelDinhDai.Id);
            instCC.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(levelTang1.Id);
            instCC.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM)?.Set(0.0);
            instCC.get_Parameter(BuiltInParameter.SCHEDULE_TOP_LEVEL_OFFSET_PARAM)?.Set(0.0);
            instCC.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("Cổ Cột Móng");
            instCC.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set($"CC_{idx:D2}");
            ccIds.Add(instCC.Id.Value);

            // B. Cột Tầng 1 (0mm to +4300mm)
            var instC1 = doc.Create.NewFamilyInstance(pt, sym, levelTang1, Autodesk.Revit.DB.Structure.StructuralType.Column);
            instC1.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(levelTang2.Id);
            instC1.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.Set(levelTang1.Id);
            instC1.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM)?.Set(0.0);
            instC1.get_Parameter(BuiltInParameter.SCHEDULE_TOP_LEVEL_OFFSET_PARAM)?.Set(0.0);
            instC1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("Cột Tầng 1");
            instC1.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set($"C1_{idx:D2}");
            c1Ids.Add(instC1.Id.Value);
        }

        return new {
            typesCreated = typesCreated,
            coCotCreated = ccIds.Count,
            cotTang1Created = c1Ids.Count,
            totalCreated = ccIds.Count + c1Ids.Count,
            coCotIds = ccIds,
            cotTang1Ids = c1Ids
        };
        """;
}
