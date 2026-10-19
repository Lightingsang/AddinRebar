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
///     Pure C# command to construct Elevator Shear Walls in Revit 2026.
///     Builds 8 Short Walls (Cổ Vách) and 8 Story 1 Walls for Cores T1A & T1B.
/// </summary>
public sealed class GenerateElevatorWallsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public GenerateElevatorWallsCommand(
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
        _formatter.WriteBanner("Generate Elevator Walls", $"Dựng Vách Thang Máy T1A & T1B [{modeLabel}]");

        string dataPath = args.GetOption("input", Path.Combine("scratch", "elevator_walls_model.json"));
        if (!File.Exists(dataPath))
        {
            _formatter.WriteError($"Không tìm thấy file mô hình vách: {dataPath}");
            return 1;
        }

        using var docJson = JsonDocument.Parse(await File.ReadAllTextAsync(dataPath, ct).ConfigureAwait(false));
        var wallsElem = docJson.RootElement.GetProperty("walls");
        int totalWalls = wallsElem.GetArrayLength();
        _formatter.WriteSuccess($"Đã tải {totalWalls} diện vách ({totalWalls * 2} phân đoạn) từ {dataPath}");

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

            var argsObj = JsonSerializer.SerializeToElement(new { walls = wallsElem });

            var req = new ExecuteRequest(
                Code: BuildRevitScript(),
                Transaction: TransactionModes.Auto,
                DryRun: dryRun,
                TimeoutSeconds: 120,
                Label: "Generate Elevator Walls",
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

            return ProcessExecutionResult(res, dryRun);
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi: {ex.Message}");
            return 4;
        }
        finally
        {
            if (_injectedClient == null)
                await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private int ProcessExecutionResult(ExecuteResult res, bool dryRun)
    {
        var val = res.Value!.Value;
        string json = JsonSerializer.Serialize(val, JsonOptions);
        string outputPath = Path.Combine("scratch", "elevator_walls_revit_created.json");
        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outputPath, json);

        int cvCount = val.TryGetProperty("coVachCreated", out var cv) ? cv.GetInt32() : 0;
        int v1Count = val.TryGetProperty("vachTang1Created", out var v1) ? v1.GetInt32() : 0;
        string wallTypeUsed = val.TryGetProperty("wallTypeUsed", out var wt) ? wt.GetString() ?? "" : "";

        _formatter.WriteInfo($"Kết quả thực thi (dryRun={dryRun}):");
        _formatter.WriteInfo($"  - Cổ Vách Móng tạo mới: {cvCount}/8");
        _formatter.WriteInfo($"  - Vách Tầng 1 tạo mới: {v1Count}/8");
        _formatter.WriteInfo($"  - Wall Type sử dụng: {wallTypeUsed}");
        _formatter.WriteInfo($"  - Added: {res.Changed.Added}, RolledBack: {res.RolledBack}");
        _formatter.WriteSuccess($"Đã lưu kết quả chi tiết: {outputPath}");

        bool countsMatch = cvCount == 8 && v1Count == 8;
        if (dryRun)
        {
            if (res.RolledBack && countsMatch)
            {
                _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Đã dựng thử nghiệm 16 vách thang máy và rollback 100% an toàn.");
                return 0;
            }
            if (!res.RolledBack)
            {
                _formatter.WriteError("CẢNH BÁO NGUY HIỂM: Dry-run thất bại! Revit bridge KHÔNG xác nhận rollback.");
                return 5;
            }
            _formatter.WriteError($"DRY-RUN THẤT BẠI: Số lượng vách không đạt (Cổ vách: {cvCount}/8, Vách T1: {v1Count}/8).");
            return 5;
        }

        if (countsMatch)
        {
            _formatter.WriteSuccess("COMMIT THÀNH CÔNG: Đã tạo 16 vách thang máy vào Revit.");
            return 0;
        }

        _formatter.WriteError($"COMMIT THẤT BẠI: Số lượng vách không đạt (Cổ vách: {cvCount}/8, Vách T1: {v1Count}/8).");
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

        // 2. Locate Structural Wall Type
        WallType wallType = null;
        var wallTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>();

        foreach (var wt in wallTypes)
        {
            if (wt.Name == "2_Wall_BTCT_D200") { wallType = wt; break; }
        }

        if (wallType == null)
        {
            wallType = wallTypes.FirstOrDefault(wt => wt.Name.Contains("BTCT") && Math.Abs(wt.Width * 304.8 - 200.0) < 5.0);
        }

        if (wallType == null)
        {
            throw new InvalidOperationException("Không tìm thấy WallType BTCT D200 phù hợp trong Revit. Cấm phỏng đoán wall type (ĐIỀU 0).");
        }

        // 3. Construct Walls
        var walls = args.List("walls");
        var cvIds = new List<long>();
        var v1Ids = new List<long>();

        foreach (var w in walls)
        {
            double x0 = w.Double("x0Mm") / 304.8;
            double y0 = w.Double("y0Mm") / 304.8;
            double x1 = w.Double("x1Mm") / 304.8;
            double y1 = w.Double("y1Mm") / 304.8;

            var line0 = Line.CreateBound(new XYZ(x0, y0, 0), new XYZ(x1, y1, 0));

            // Phase 1: Cổ Vách Móng (Đỉnh Đài Móng -> Tầng 1)
            double heightCvFt = (0.0 - (-1600.0)) / 304.8;
            var wallCv = Wall.Create(doc, line0, wallType.Id, levelDinhDai.Id, heightCvFt, 0.0, false, true);

            var pCvBase = wallCv.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            if (pCvBase != null) pCvBase.Set(levelDinhDai.Id);

            var pCvTop = wallCv.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
            if (pCvTop != null) pCvTop.Set(levelTang1.Id);

            var pCvUsage = wallCv.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM);
            if (pCvUsage != null) pCvUsage.Set(1); // StructuralWallUsage.Shear

            var cvData = w.Obj("coVach");
            var pCvMark = wallCv.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            if (pCvMark != null) pCvMark.Set(cvData.Str("mark"));

            var pCvComm = wallCv.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (pCvComm != null) pCvComm.Set(cvData.Str("comments"));

            cvIds.Add(wallCv.Id.Value);

            // Phase 2: Vách Tầng 1 (Tầng 1 -> Tầng 2)
            double heightV1Ft = (4300.0 - 0.0) / 304.8;
            var wallV1 = Wall.Create(doc, line0, wallType.Id, levelTang1.Id, heightV1Ft, 0.0, false, true);

            var pV1Base = wallV1.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            if (pV1Base != null) pV1Base.Set(levelTang1.Id);

            var pV1Top = wallV1.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
            if (pV1Top != null) pV1Top.Set(levelTang2.Id);

            var pV1Usage = wallV1.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM);
            if (pV1Usage != null) pV1Usage.Set(1); // StructuralWallUsage.Shear

            var v1Data = w.Obj("vachTang1");
            var pV1Mark = wallV1.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            if (pV1Mark != null) pV1Mark.Set(v1Data.Str("mark"));

            var pV1Comm = wallV1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (pV1Comm != null) pV1Comm.Set(v1Data.Str("comments"));

            v1Ids.Add(wallV1.Id.Value);
        }

        return new {
            coVachCreated = cvIds.Count,
            vachTang1Created = v1Ids.Count,
            coVachIds = cvIds,
            vachTang1Ids = v1Ids,
            wallTypeUsed = wallType.Name
        };
    """;
}
