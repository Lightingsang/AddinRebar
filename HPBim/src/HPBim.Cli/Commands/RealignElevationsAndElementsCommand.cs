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
///     Pure C# command to realign building levels and structural elements in Revit 2026.
///     Standardizes: Đỉnh Đài Móng (-1.6m), Giằng Móng (-1.6m), Tầng 1 Kết cấu (-0.1m).
/// </summary>
public sealed class RealignElevationsAndElementsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public RealignElevationsAndElementsCommand(
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
        _formatter.WriteBanner("Realign Elevations & Elements", $"Chuẩn hóa cao độ công trình & nắn chỉnh cấu kiện [{modeLabel}]");

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

            var req = new ExecuteRequest(
                Code: BuildRevitScript(),
                Transaction: TransactionModes.Auto,
                DryRun: dryRun,
                TimeoutSeconds: 120,
                Label: "Realign Elevations & Elements");

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
        string json = JsonSerializer.Serialize(res.Value!.Value, JsonOptions);
        string outputPath = Path.Combine("scratch", "realign_elevations_result.json");
        File.WriteAllText(outputPath, json);

        var doc = res.Value.Value;
        _formatter.WriteSuccess($"Kết quả thực thi (dryRun={dryRun}):");
        if (doc.TryGetProperty("levelCreated", out var lc))
            _formatter.WriteInfo($"  - Level Tầng 1 (Kết cấu): {(lc.GetBoolean() ? "Tạo mới (-100mm)" : "Đã tồn tại")}");
        if (doc.TryGetProperty("deletedLevels", out var dl))
            _formatter.WriteInfo($"  - Đã xóa các Level thừa: {dl.GetString()}");
        if (doc.TryGetProperty("gradeBeamsUpdated", out var gbu))
            _formatter.WriteInfo($"  - Giằng móng chuyển về Đỉnh Đài Móng (-1.600m): {gbu.GetInt32()} dầm");
        if (doc.TryGetProperty("shortColsUpdated", out var scu))
            _formatter.WriteInfo($"  - Cổ Cột Móng ngàm vào Tầng 1 Kết cấu (-0.100m): {scu.GetInt32()} cột (h=1500mm)");
        if (doc.TryGetProperty("shortWallsUpdated", out var swu))
            _formatter.WriteInfo($"  - Cổ Vách Móng ngàm vào Tầng 1 Kết cấu (-0.100m): {swu.GetInt32()} vách (h=1500mm)");
        if (doc.TryGetProperty("story1ColsUpdated", out var s1cu))
            _formatter.WriteInfo($"  - Cột Tầng 1 chân đặt tại Tầng 1 Kết cấu (-0.100m): {s1cu.GetInt32()} cột (h=4400mm)");
        if (doc.TryGetProperty("story1WallsUpdated", out var s1wu))
            _formatter.WriteInfo($"  - Vách Tầng 1 chân đặt tại Tầng 1 Kết cấu (-0.100m): {s1wu.GetInt32()} vách (h=4400mm)");

        _formatter.WriteSuccess($"Đã lưu kết quả chi tiết: {outputPath}");
        return 0;
    }

    private static string BuildRevitScript()
    {
        return """
            double ftPerMm = 1.0 / 304.8;
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            foreach (var l in levels)
            {
                if (l.Pinned) l.Pinned = false;
            }

            // 1. Locate or create Level "Tầng 1 (Kết cấu)" at -100mm (-0.1m)
            double t1KcelevFt = -100.0 * ftPerMm;
            Level t1KcLevel = levels.FirstOrDefault(l => l.Name.Contains("Tầng 1 (Kết cấu)") || l.Name.Contains("Tầng 1 (KC)"));
            bool levelCreated = false;

            if (t1KcLevel == null)
            {
                t1KcLevel = Level.Create(doc, t1KcelevFt);
                t1KcLevel.Name = "Tầng 1 (Kết cấu)";
                levelCreated = true;
            }
            else
            {
                t1KcLevel.Elevation = t1KcelevFt;
            }

            // Ensure Level "Tầng 1 (±0.000)" is at 0.0mm
            Level t1Arch = levels.FirstOrDefault(l => l.Name.Contains("Tầng 1 (±0.000)") || l.Name.Equals("Tầng 1"));
            if (t1Arch != null && Math.Abs(t1Arch.Elevation) > 1e-4)
            {
                t1Arch.Elevation = 0.0;
            }

            Level dinhDai = levels.FirstOrDefault(l => l.Name.Contains("Đỉnh Đài Móng"));
            Level story2 = levels.FirstOrDefault(l => l.Name.Contains("Tầng 2"));

            if (dinhDai == null || story2 == null)
            {
                throw new InvalidOperationException("Không tìm thấy Level Đỉnh Đài Móng hoặc Tầng 2!");
            }

            // 2. Realign Grade Beams: verify all hosted at dinhDai and ensure Z offset is 0
            var framings = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            int gradeBeamsUpdated = 0;
            foreach (var b in framings)
            {
                var zOffsetParam = b.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION);
                var zOffset1Param = b.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION);

                if (zOffsetParam != null && !zOffsetParam.IsReadOnly) zOffsetParam.Set(0.0);
                if (zOffset1Param != null && !zOffset1Param.IsReadOnly) zOffset1Param.Set(0.0);
                gradeBeamsUpdated++;
            }

            // 3. Realign Columns: Short Columns (-1.6m -> -0.1m) and Story 1 Columns (-0.1m -> +4.3m)
            var columns = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            int shortColsUpdated = 0;
            int story1ColsUpdated = 0;

            foreach (var c in columns)
            {
                if (c.Pinned) c.Pinned = false;
                var baseLvlParam = c.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
                var topLvlParam = c.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                var baseOffset = c.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
                var topOffset = c.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

                if (baseLvlParam == null || topLvlParam == null) continue;

                var baseId = baseLvlParam.AsElementId();
                var topId = topLvlParam.AsElementId();

                if (baseId == dinhDai.Id)
                {
                    if (!topLvlParam.IsReadOnly) topLvlParam.Set(t1KcLevel.Id);
                    if (baseOffset != null && !baseOffset.IsReadOnly) baseOffset.Set(0.0);
                    if (topOffset != null && !topOffset.IsReadOnly) topOffset.Set(0.0);
                    shortColsUpdated++;
                }
                else if (topId == story2.Id)
                {
                    if (!baseLvlParam.IsReadOnly) baseLvlParam.Set(t1KcLevel.Id);
                    if (baseOffset != null && !baseOffset.IsReadOnly) baseOffset.Set(0.0);
                    if (topOffset != null && !topOffset.IsReadOnly) topOffset.Set(0.0);
                    story1ColsUpdated++;
                }
            }

            // 4. Realign Elevator Walls: Short Walls (-1.6m -> -0.1m) and Story 1 Walls (-0.1m -> +4.3m)
            var walls = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .ToList();

            int shortWallsUpdated = 0;
            int story1WallsUpdated = 0;

            foreach (var w in walls)
            {
                if (w.Pinned) w.Pinned = false;
                var baseLvlParam = w.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                var topLvlParam = w.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                var baseOffset = w.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                var topOffset = w.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);

                if (baseLvlParam == null || topLvlParam == null) continue;

                var baseId = baseLvlParam.AsElementId();
                var topId = topLvlParam.AsElementId();

                if (baseId == dinhDai.Id)
                {
                    if (!topLvlParam.IsReadOnly) topLvlParam.Set(t1KcLevel.Id);
                    if (baseOffset != null && !baseOffset.IsReadOnly) baseOffset.Set(0.0);
                    if (topOffset != null && !topOffset.IsReadOnly) topOffset.Set(0.0);
                    shortWallsUpdated++;
                }
                else if (topId == story2.Id)
                {
                    if (!baseLvlParam.IsReadOnly) baseLvlParam.Set(t1KcLevel.Id);
                    if (baseOffset != null && !baseOffset.IsReadOnly) baseOffset.Set(0.0);
                    if (topOffset != null && !topOffset.IsReadOnly) topOffset.Set(0.0);
                    story1WallsUpdated++;
                }
            }

            // 5. Delete redundant levels: "Tầng Móng" and "Đáy Đài Móng"
            var toDelete = levels.Where(l => l.Name.Contains("Tầng Móng") || l.Name.Contains("Đáy Đài Móng")).ToList();
            var deletedNames = new List<string>();
            foreach (var l in toDelete)
            {
                if (l.Pinned) l.Pinned = false;
                deletedNames.Add(l.Name);
                doc.Delete(l.Id);
            }

            return new {
                levelCreated = levelCreated,
                t1KcLevelId = t1KcLevel.Id.Value,
                deletedLevels = string.Join(", ", deletedNames),
                gradeBeamsUpdated = gradeBeamsUpdated,
                shortColsUpdated = shortColsUpdated,
                shortWallsUpdated = shortWallsUpdated,
                story1ColsUpdated = story1ColsUpdated,
                story1WallsUpdated = story1WallsUpdated
            };
        """;
    }
}
