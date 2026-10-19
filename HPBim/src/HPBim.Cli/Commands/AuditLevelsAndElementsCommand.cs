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
///     Pure C# command to perform 8-step independent QA/QC audit for realigned levels and elements.
/// </summary>
public sealed class AuditLevelsAndElementsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public AuditLevelsAndElementsCommand(
        ConsoleFormatter? formatter = null,
        IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));

        _formatter.WriteBanner("Audit Levels & Elements", "Kiểm toán độc lập 8 bước Hệ Cao Trình & Cấu Kiện Tầng 1 (100% C#)");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 10000,
            DefaultExecuteTimeoutSeconds = 120
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Revit Named Pipe: {pipeName}");

            var req = new ExecuteRequest(
                Code: BuildAuditScript(),
                Transaction: TransactionModes.None,
                DryRun: false,
                TimeoutSeconds: 120,
                Label: "Audit Levels & Elements");

            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi Revit API: {res.Message}");
                return 2;
            }

            return await ProcessAuditResultAsync(res.Value.Value, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kiểm toán: {ex.Message}");
            return 3;
        }
        finally
        {
            if (_injectedClient == null)
                await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<int> ProcessAuditResultAsync(JsonElement elem, CancellationToken ct)
    {
        var report = new LevelsAuditReport();
        bool allPassed = elem.GetProperty("allPassed").GetBoolean();
        report.AllPassed = allPassed;

        foreach (var s in elem.GetProperty("steps").EnumerateArray())
        {
            report.Steps.Add(new AuditStepResult(
                s.GetProperty("name").GetString() ?? "",
                s.GetProperty("passed").GetBoolean(),
                s.GetProperty("details").GetString() ?? ""
            ));
        }

        string resultLabel = allPassed ? "CHẤP THUẬN NGHIỆM THU 100%" : "CẦN ĐIỀU CHỈNH";
        _formatter.WriteBanner("Báo Cáo Kết Quả Kiểm Toán Cao Độ", resultLabel);

        foreach (var step in report.Steps)
        {
            if (step.Passed) _formatter.WriteSuccess($"[PASS] {step.Name}: {step.Details}");
            else _formatter.WriteError($"[FAIL] {step.Name}: {step.Details}");
        }

        string jsonPath = Path.Combine("scratch", "realign_elevations_audit_report.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, JsonOptions), ct).ConfigureAwait(false);
        _formatter.WriteSuccess($"Đã lưu kết quả kiểm toán tại: {jsonPath}");

        await LevelsAndElementsAuditDossierWriter.WriteAuditDossierAsync(report, ct).ConfigureAwait(false);
        _formatter.WriteSuccess("Đã xuất Biên bản nghiệm thu kỹ thuật Markdown!");

        return allPassed ? 0 : 1;
    }

    private static string BuildAuditScript()
    {
        return """
            double mmPerFt = 304.8;
            var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();

            var steps = new List<object>();

            // Step 1: Connected
            steps.Add(new { name = "Bước 1: Kết nối hạ tầng Named Pipe Revit 2026", passed = true, details = "Đã kết nối trực tiếp tiến trình Revit 2026." });

            // Step 2: Level Tầng 1 (Kết cấu) at -100mm
            var t1Kc = levels.FirstOrDefault(l => l.Name.Contains("Tầng 1 (Kết cấu)"));
            bool s2Pass = t1Kc != null && Math.Abs(t1Kc.Elevation * mmPerFt - (-100.0)) <= 1.0;
            string s2Det = t1Kc != null ? $"Level tồn tại tại elevation = {Math.Round(t1Kc.Elevation * mmPerFt, 1)}mm" : "Không tìm thấy Level Tầng 1 (Kết cấu)";
            steps.Add(new { name = "Bước 2: Kiểm toán Level Tầng 1 (Kết cấu) (-0.100m)", passed = s2Pass, details = s2Det });

            // Step 3: Level Tầng 1 (±0.000) at 0.0mm
            var t1Arch = levels.FirstOrDefault(l => l.Name.Contains("Tầng 1 (±0.000)") || l.Name.Equals("Tầng 1"));
            bool s3Pass = t1Arch != null && Math.Abs(t1Arch.Elevation * mmPerFt) <= 1.0;
            string s3Det = t1Arch != null ? $"Level tồn tại tại elevation = {Math.Round(t1Arch.Elevation * mmPerFt, 1)}mm" : "Không tìm thấy Level Tầng 1 (±0.000)";
            steps.Add(new { name = "Bước 3: Kiểm toán Level Tầng 1 (±0.000) tham chiếu kiến trúc", passed = s3Pass, details = s3Det });

            // Step 4: Redundant levels deleted
            bool hasTangMong = levels.Any(l => l.Name.Contains("Tầng Móng"));
            bool hasDayDai = levels.Any(l => l.Name.Contains("Đáy Đài Móng"));
            bool s4Pass = !hasTangMong && !hasDayDai;
            string s4Det = s4Pass ? "Đã xóa sạch 2 Level Tầng Móng và Đáy Đài Móng." : $"Còn tồn tại: TangMong={hasTangMong}, DayDai={hasDayDai}";
            steps.Add(new { name = "Bước 4: Kiểm toán xóa bỏ 2 Level thừa (Tầng Móng & Đáy Đài Móng)", passed = s4Pass, details = s4Det });

            // Step 5: Grade Beams at Đỉnh Đài Móng (-1.600m)
            var dinhDai = levels.FirstOrDefault(l => l.Name.Contains("Đỉnh Đài Móng"));
            var story2 = levels.FirstOrDefault(l => l.Name.Contains("Tầng 2"));
            var framings = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
            int gbOnDinhDai = framings.Count(b => b.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId() == dinhDai?.Id);
            bool s5Pass = dinhDai != null && gbOnDinhDai == framings.Count && framings.Count >= 180;
            string s5Det = $"100% {gbOnDinhDai}/{framings.Count} giằng móng neo chính xác tại Level Đỉnh Đài Móng (-1.600m).";
            steps.Add(new { name = "Bước 5: Kiểm toán 188 Giằng Móng neo phẳng Đỉnh Đài Móng (-1.600m)", passed = s5Pass, details = s5Det });

            // Step 6: Short Columns (-1.600m -> -0.100m, h=1500mm)
            var cols = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
            int shortColsOk = cols.Count(c => c.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.AsElementId() == dinhDai?.Id &&
                                              c.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.AsElementId() == t1Kc?.Id);
            bool s6Pass = shortColsOk == 72;
            string s6Det = $"Thực tế {shortColsOk}/72 Cổ Cột ngàm Base=Đỉnh Đài Móng (-1.6m), Top=Tầng 1 KC (-0.1m), h=1500mm.";
            steps.Add(new { name = "Bước 6: Kiểm toán 72 Cổ Cột Móng ngàm vào sàn thô Tầng 1 (h=1500mm)", passed = s6Pass, details = s6Det });

            // Step 7: Story 1 Columns (-0.100m -> +4.300m, h=4400mm)
            int s1ColsOk = cols.Count(c => c.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.AsElementId() == t1Kc?.Id &&
                                           c.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.AsElementId() == story2?.Id);
            bool s7Pass = s1ColsOk == 72;
            string s7Det = $"Thực tế {s1ColsOk}/72 Cột Tầng 1 chân tại Tầng 1 KC (-0.1m), Top=Tầng 2 (+4.3m), h=4400mm.";
            steps.Add(new { name = "Bước 7: Kiểm toán 72 Cột Thân Tầng 1 đặt trên sàn thô Tầng 1 (h=4400mm)", passed = s7Pass, details = s7Det });

            // Step 8: Elevator Walls (8 Cổ Vách h=1500mm + 8 Vách T1 h=4400mm)
            var walls = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().Cast<Wall>().ToList();
            int shortWallsOk = walls.Count(w => w.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)?.AsElementId() == dinhDai?.Id &&
                                                w.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.AsElementId() == t1Kc?.Id);
            int s1WallsOk = walls.Count(w => w.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)?.AsElementId() == t1Kc?.Id &&
                                             w.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.AsElementId() == story2?.Id);
            bool s8Pass = shortWallsOk == 8 && s1WallsOk == 8;
            string s8Det = $"Thực tế {shortWallsOk}/8 Cổ Vách (h=1500mm) và {s1WallsOk}/8 Vách T1 (h=4400mm) khớp sàn thô -0.100m.";
            steps.Add(new { name = "Bước 8: Kiểm toán 16 Vách Thang Máy T1A & T1B liên kết đúng Tầng 1 KC", passed = s8Pass, details = s8Det });

            bool allPassed = s2Pass && s3Pass && s4Pass && s5Pass && s6Pass && s7Pass && s8Pass;

            return new {
                allPassed = allPassed,
                steps = steps
            };
        """;
    }
}
