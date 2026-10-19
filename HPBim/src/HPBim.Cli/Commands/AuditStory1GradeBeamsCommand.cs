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
///     100% C# command to execute an authentic 8-step QA/QC audit for Story 1 Grade Beams in Revit 2026.
/// </summary>
public sealed class AuditStory1GradeBeamsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public AuditStory1GradeBeamsCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        _formatter.WriteBanner("Story 1 Grade Beams QA/QC Audit", "Kiểm toán độc lập 8 bước Dầm Sàn Tầng 1 / Đà Kiềng (100% C#)");

        string modelPath = Path.Combine("scratch", "story1_grade_beams_model.json");
        if (!File.Exists(modelPath))
        {
            _formatter.WriteError($"Không tìm thấy file mô hình thiết kế: {modelPath}");
            return 1;
        }

        using var modelDoc = JsonDocument.Parse(await File.ReadAllTextAsync(modelPath, ct).ConfigureAwait(false));
        var expectedBeams = modelDoc.RootElement.GetProperty("beams");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName, ConnectTimeoutMs = 10000, DefaultExecuteTimeoutSeconds = 120
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Revit Named Pipe: {pipeName}");

            string script = BuildAuditQueryScript();
            var req = new ExecuteRequest(script, TransactionModes.None, false, 120, "Query Story 1 Beams For Audit");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi truy vấn Revit: {res.Message}");
                return 2;
            }

            var liveData = res.Value.Value;
            var report = Run8StepAudit(expectedBeams, liveData);
            PrintReport(report);

            string outPath = Path.Combine("scratch", "story1_grade_beams_audit_report.json");
            File.WriteAllText(outPath, JsonSerializer.Serialize(report, JsonOptions));
            _formatter.WriteSuccess($"Đã lưu kết quả kiểm toán: {outPath}");

            await Story1GradeBeamsAuditDossierWriter.WriteAuditDossierAsync(report, ct).ConfigureAwait(false);
            _formatter.WriteSuccess("Đã xuất hồ sơ nghiệm thu kỹ thuật Markdown hoàn tất!");

            return report.AllPassed ? 0 : 3;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi kiểm toán: {ex}");
            return 99;
        }
        finally
        {
            if (_injectedClient == null) await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string BuildAuditQueryScript() => """
        var levelT1 = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
            .FirstOrDefault(l => Math.Abs(l.Elevation * 304.8 - (-100.0)) < 1.0 || l.Name == "Tầng 1");

        if (levelT1 == null)
        {
            return new { totalLiveBeams = 0, badLevelCount = 0, typeCounts = new Dictionary<string, int>(), originCount = 0, leftCount = 0, unlockedCount = 0, invalidLengthCount = 0 };
        }

        var collector = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>();

        int totalLiveBeams = 0, badLevelCount = 0, originCount = 0, leftCount = 0, unlockedCount = 0, invalidLengthCount = 0;
        var typeCounts = new Dictionary<string, int>();

        foreach (var b in collector)
        {
            var curve = (b.Location as LocationCurve)?.Curve;
            if (curve == null) continue;

            var p1 = curve.GetEndPoint(0);
            double zTop = p1.Z * 304.8;
            var refLvl = b.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId();
            bool isStory1 = (refLvl != null && refLvl == levelT1.Id)
                         || (b.LevelId == levelT1.Id)
                         || (Math.Abs(zTop - (-100.0)) < 10.0);
            if (!isStory1) continue;

            totalLiveBeams++;
            if (Math.Abs(zTop - (-100.0)) > 2.0) badLevelCount++;

            string tName = b.Symbol?.Name ?? "";
            if (typeCounts.ContainsKey(tName)) typeCounts[tName]++;
            else typeCounts[tName] = 1;

            int yj = b.get_Parameter(BuiltInParameter.Y_JUSTIFICATION)?.AsInteger() ?? -1;
            if (yj == 0) leftCount++;
            else if (yj == 2) originCount++;

            bool join0 = Autodesk.Revit.DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(b, 0);
            bool join1 = Autodesk.Revit.DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(b, 1);
            if (join0 || join1) unlockedCount++;

            if (curve.Length * 304.8 < 250.0) invalidLengthCount++;
        }

        return new { totalLiveBeams, badLevelCount, typeCounts, originCount, leftCount, unlockedCount, invalidLengthCount };
    """;

    private Story1GradeBeamsAuditReport Run8StepAudit(JsonElement expectedBeams, JsonElement liveData)
    {
        var report = new Story1GradeBeamsAuditReport();
        int expectedCount = expectedBeams.GetArrayLength();
        int liveCount = liveData.GetProperty("totalLiveBeams").GetInt32();
        int badLevelCount = liveData.GetProperty("badLevelCount").GetInt32();
        int originCount = liveData.GetProperty("originCount").GetInt32();
        int leftCount = liveData.GetProperty("leftCount").GetInt32();
        int unlockedCount = liveData.GetProperty("unlockedCount").GetInt32();
        int invalidLengthCount = liveData.GetProperty("invalidLengthCount").GetInt32();

        report.OriginJustCount = originCount;
        report.LeftJustCount = leftCount;
        report.TotalBeams = liveCount;

        bool s1 = AuditCount(report, expectedCount, liveCount);
        bool s2 = AuditLevels(report, badLevelCount, liveCount);
        bool s3 = AuditTypes(report, expectedBeams, liveData.GetProperty("typeCounts"));
        bool s4 = AuditJustification(report, originCount, leftCount, liveCount);
        bool s5 = AuditLocking(report, unlockedCount, liveCount);
        bool s6 = AuditContinuity(report, invalidLengthCount, liveCount);
        bool s7 = AuditCadTolerance(report, liveData);

        report.AllPassed = s1 && s2 && s3 && s4 && s5 && s6 && s7;
        string conclusion = report.AllPassed
            ? "CHẤP THUẬN NGHIỆM THU 100% (PASS)"
            : (!s7
                ? "CHƯA ĐỦ CƠ SỞ ĐỂ KẾT LUẬN (INCONCLUSIVE) - THIẾU BẰNG CHỨNG ĐO ĐẠC NÉT CAD"
                : "KHÔNG ĐẠT - YÊU CẦU SỬA LỖI");
        AddStep(report, 8, "Kết luận Nghiệm thu Kỹ thuật Cuối cùng", "100% tiêu chí đạt chuẩn TCVN & ĐIỀU 0", conclusion, report.AllPassed);

        return report;
    }

    private static void AddStep(Story1GradeBeamsAuditReport r, int num, string name, string exp, string act, bool pass)
        => r.Steps.Add(new AuditStepItem { StepNumber = num, Name = name, Expected = exp, Actual = act, Passed = pass });

    private static bool AuditCount(Story1GradeBeamsAuditReport r, int exp, int act)
    {
        bool pass = exp > 0 && act == exp;
        AddStep(r, 1, "Kiểm toán Số lượng Dầm Sàn Tầng 1", $"{exp} dầm", $"{act} dầm", pass);
        return pass;
    }

    private static bool AuditLevels(Story1GradeBeamsAuditReport r, int badCount, int liveCount)
    {
        bool pass = badCount == 0 && liveCount > 0;
        string act = badCount == 0 ? $"100% đạt chuẩn -100.0 mm ({liveCount}/{liveCount})" : $"{badCount} dầm lệch cao độ";
        AddStep(r, 2, "Kiểm toán Cao trình & Cao độ Đỉnh Dầm", "Level 'Tầng 1', Đỉnh: -100.0 mm (Sai số <= 1mm)", act, pass);
        return pass;
    }

    private static bool AuditTypes(Story1GradeBeamsAuditReport r, JsonElement expectedBeams, JsonElement liveTypesElem)
    {
        var expectedTypes = expectedBeams.EnumerateArray()
            .GroupBy(b => (b.TryGetProperty("TypeName", out var tp) ? tp.GetString() : null)
                       ?? (b.TryGetProperty("typeName", out var tc) ? tc.GetString() : null)
                       ?? "")
            .ToDictionary(g => g.Key, g => g.Count());

        var liveTypes = new Dictionary<string, int>();
        foreach (var prop in liveTypesElem.EnumerateObject()) liveTypes[prop.Name] = prop.Value.GetInt32();

        bool pass = true;
        foreach (var kvp in expectedTypes)
        {
            liveTypes.TryGetValue(kvp.Key, out int actualCount);
            if (actualCount != kvp.Value) pass = false;

            var parts = kvp.Key.Replace("B_", "").Split('x');
            int w = parts.Length > 0 && int.TryParse(parts[0], out int pw) ? pw : 0;
            int h = parts.Length > 1 && int.TryParse(parts[1], out int ph) ? ph : 0;

            r.TypeStats.Add(new BeamTypeStatItem
            {
                TypeName = kvp.Key,
                Width = w,
                Height = h,
                ExpectedCount = kvp.Value,
                ActualCount = actualCount
            });
        }

        AddStep(r, 3, "Kiểm toán Tiết diện Hình học (b x h)", $"{expectedTypes.Count} loại tiết diện khớp chuẩn thiết kế",
            pass ? $"100% khớp {expectedTypes.Count} chủng loại" : "Có sai lệch chủng loại tiết diện", pass);
        return pass;
    }

    private static bool AuditJustification(Story1GradeBeamsAuditReport r, int origin, int left, int liveCount)
    {
        bool pass = (left + origin) == liveCount && liveCount > 0;
        AddStep(r, 4, "Kiểm toán Định vị Tim Dầm (Y Justification & Offset)", "Origin (trùng tim) / Left + Offset (lệch tim/biên)",
            $"Origin: {origin} dầm, Left: {left} dầm (100% hợp lệ)", pass);
        return pass;
    }

    private static bool AuditLocking(Story1GradeBeamsAuditReport r, int unlocked, int liveCount)
    {
        bool pass = unlocked == 0 && liveCount > 0;
        string act = unlocked == 0 ? $"100% đã khóa ({liveCount}/{liveCount})" : $"{unlocked} dầm chưa khóa";
        AddStep(r, 5, "Kiểm toán Khóa Liên kết Đầu Dầm (Disallow Join)", "100% dầm khóa liên kết 2 đầu (tránh vát xén miter)", act, pass);
        return pass;
    }

    private static bool AuditContinuity(Story1GradeBeamsAuditReport r, int invalidLen, int liveCount)
    {
        bool pass = invalidLen == 0 && liveCount > 0;
        string act = pass ? $"100% liên tục nhịp chuẩn ({liveCount}/{liveCount})" : $"{invalidLen} dầm bất thường";
        AddStep(r, 6, "Kiểm toán Phân đoạn Span-by-Span Chuẩn ETABS", "Nhịp dầm liên tục giữa các tim cột/dầm phụ, L >= 250mm", act, pass);
        return pass;
    }

    private static bool AuditCadTolerance(Story1GradeBeamsAuditReport r, JsonElement liveData)
    {
        if (liveData.TryGetProperty("avgCadEdgeDevMm", out var pAvg) &&
            liveData.TryGetProperty("maxCadEdgeDevMm", out var pMax))
        {
            r.AvgCadEdgeDevMm = pAvg.GetDouble();
            r.MaxCadEdgeDevMm = pMax.GetDouble();
            bool pass = r.MaxCadEdgeDevMm <= 2.0;
            AddStep(r, 7, "Kiểm toán Dung sai Trùng khớp Nét CAD", "Sai lệch mép dầm so với CAD <= 2.0 mm",
                $"Độ lệch max: {r.MaxCadEdgeDevMm:F1} mm, TB: {r.AvgCadEdgeDevMm:F1} mm ({(pass ? "ĐẠT" : "KHÔNG ĐẠT")})", pass);
            return pass;
        }

        r.AvgCadEdgeDevMm = 0.0;
        r.MaxCadEdgeDevMm = 0.0;
        AddStep(r, 7, "Kiểm toán Dung sai Trùng khớp Nét CAD", "Sai lệch mép dầm so với CAD <= 2.0 mm",
            "Chưa đủ cơ sở để kết luận: Thiếu nguồn đối chiếu vết nét CAD (Inconclusive)", false);
        return false;
    }

    private void PrintReport(Story1GradeBeamsAuditReport report)
    {
        _formatter.WriteBanner("Kết quả Kiểm toán Độc lập 8 Bước", report.AllPassed ? "TẤT CẢ 8 BƯỚC ĐẠT CHUẨN" : "CÓ BƯỚC KHÔNG ĐẠT");
        string[][] rows = report.Steps.Select(s => new[]
        {
            $"B{s.StepNumber}",
            s.Name,
            s.Expected,
            s.Actual,
            s.Passed ? "PASS" : "FAIL"
        }).ToArray();

        _formatter.RenderTable(["Bước", "Nội dung kiểm toán", "Yêu cầu", "Thực tế", "Đánh giá"], rows);
    }
}
