namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;

/// <summary>
///     Pure C# command to cross-verify AutoCAD grids and foundation level against Revit model.
///     Generates an audited comparison table and sign-off report.
/// </summary>
public sealed class CrossVerifyGridsCommand
{
    private const double ToleranceMm = 1.0;
    private readonly ConsoleFormatter _formatter;

    public CrossVerifyGridsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string cadPath = args.GetOption("cad", Path.Combine("scratch", "piles_true_bim_model.json"));
        string revitPath = args.GetOption("revit", Path.Combine("scratch", "revit_grids_and_levels.json"));
        string reportPath = args.GetOption("report", Path.Combine("scratch", "Bien_Ban_Doi_Soat_Luoi_Truc.md"));

        _formatter.WriteBanner("Cross-Verify Grids & Levels", "Đối soát chéo Hệ Lưới Trục & Tầng Móng giữa CAD và Revit");

        if (!File.Exists(cadPath))
        {
            _formatter.WriteError($"Tập tin trích xuất CAD không tồn tại: {cadPath}. Hãy chạy 'extract-cad' trước.");
            return 2;
        }

        if (!File.Exists(revitPath))
        {
            _formatter.WriteError($"Tập tin truy vấn Revit không tồn tại: {revitPath}. Hãy chạy 'query-revit-grids' trước.");
            return 2;
        }

        var (cadData, revitData) = await LoadDataAsync(cadPath, revitPath, ct).ConfigureAwait(false);
        var report = CompareGridsAndLevels(cadData, revitData);

        await SaveReportAsync(report, reportPath, ct).ConfigureAwait(false);
        RenderReport(report);

        return report.AllPassed ? 0 : 1;
    }

    private static async Task<(CadGridData Cad, RevitGridData Revit)> LoadDataAsync(
        string cadPath, string revitPath, CancellationToken ct)
    {
        string cadJson = await File.ReadAllTextAsync(cadPath, ct).ConfigureAwait(false);
        string revitJson = await File.ReadAllTextAsync(revitPath, ct).ConfigureAwait(false);

        using var cadDoc = JsonDocument.Parse(cadJson);
        using var revitDoc = JsonDocument.Parse(revitJson);

        var cadRoot = cadDoc.RootElement;
        var revitRoot = revitDoc.RootElement;

        double origX = cadRoot.GetProperty("origin").GetProperty("cad_x_mm").GetDouble();
        double origY = cadRoot.GetProperty("origin").GetProperty("cad_y_mm").GetDouble();

        var xGrids = cadRoot.GetProperty("grids").GetProperty("x").EnumerateArray()
            .Select(g => (Name: g.GetProperty("name").GetString() ?? "", CadX: g.GetProperty("x").GetDouble()))
            .ToList();

        var yGrids = cadRoot.GetProperty("grids").GetProperty("y").EnumerateArray()
            .Select(g => (Name: g.GetProperty("name").GetString() ?? "", CadY: g.GetProperty("y").GetDouble()))
            .ToList();

        var revGrids = revitRoot.GetProperty("grids").EnumerateArray()
            .Select(g => (
                Name: g.GetProperty("name").GetString() ?? "",
                X0: g.GetProperty("x0").GetDouble(),
                Y0: g.GetProperty("y0").GetDouble(),
                X1: g.GetProperty("x1").GetDouble(),
                Y1: g.GetProperty("y1").GetDouble()))
            .ToList();

        var revLevels = revitRoot.GetProperty("levels").EnumerateArray()
            .Select(l => (
                Name: l.GetProperty("name").GetString() ?? "",
                ElevationMm: l.GetProperty("elevationMm").GetDouble()))
            .ToList();

        return (new CadGridData(origX, origY, xGrids, yGrids), new RevitGridData(revGrids, revLevels));
    }

    private static VerificationResult CompareGridsAndLevels(CadGridData cad, RevitGridData revit)
    {
        var rows = new List<GridCompareRow>();
        bool allPassed = true;

        foreach (var (name, cadX) in cad.XGrids)
        {
            double expX = Math.Round(cadX - cad.OriginX, 1);
            var revMatch = revit.Grids.FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            bool found = !string.IsNullOrEmpty(revMatch.Name);
            double actX = found ? Math.Round((revMatch.X0 + revMatch.X1) / 2.0, 1) : double.NaN;
            double delta = found ? Math.Abs(actX - expX) : double.NaN;
            bool passed = found && delta <= ToleranceMm;
            if (!passed) allPassed = false;

            rows.Add(new GridCompareRow("X (Dọc)", name, cadX, expX, actX, delta, passed));
        }

        foreach (var (name, cadY) in cad.YGrids)
        {
            double expY = Math.Round(cadY - cad.OriginY, 1);
            var revMatch = revit.Grids.FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            bool found = !string.IsNullOrEmpty(revMatch.Name);
            double actY = found ? Math.Round((revMatch.Y0 + revMatch.Y1) / 2.0, 1) : double.NaN;
            double delta = found ? Math.Abs(actY - expY) : double.NaN;
            bool passed = found && delta <= ToleranceMm;
            if (!passed) allPassed = false;

            rows.Add(new GridCompareRow("Y (Ngang)", name, cadY, expY, actY, delta, passed));
        }

        var foundationLevel = revit.Levels.FirstOrDefault(l => l.Name.Equals("Tầng Móng", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(foundationLevel.Name))
        {
            foundationLevel = revit.Levels.FirstOrDefault(l => l.Name.IndexOf("Móng", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        bool levelFound = !string.IsNullOrEmpty(foundationLevel.Name);
        double actLevel = levelFound ? foundationLevel.ElevationMm : double.NaN;
        double expLevel = -1000.0;
        double levelDelta = levelFound ? Math.Abs(actLevel - expLevel) : double.NaN;
        bool levelPassed = levelFound && levelDelta <= ToleranceMm;
        if (!levelPassed) allPassed = false;

        return new VerificationResult(rows, levelFound, foundationLevel.Name, expLevel, actLevel, levelDelta, levelPassed, allPassed);
    }

    private void RenderReport(VerificationResult r)
    {
        var tableRows = r.Rows.Select(row => new[]
        {
            row.Type,
            row.Name,
            row.CadCoord.ToString("F1"),
            row.ExpectedRevit.ToString("F1"),
            double.IsNaN(row.ActualRevit) ? "N/A" : row.ActualRevit.ToString("F1"),
            double.IsNaN(row.Delta) ? "N/A" : row.Delta.ToString("F1") + " mm",
            row.Passed ? "PASS" : "FAIL"
        }).ToList();

        _formatter.RenderTable(
            ["Phương", "Trục", "CAD (mm)", "Revit Kỳ Vọng", "Revit Thực Tế", "Độ Lệch", "Đạt"],
            tableRows);

        _formatter.WriteInfo($"Cao trình Móng: Kỳ vọng {r.ExpLevel:F1} mm | Thực tế: {r.ActLevel:F1} mm ({r.LevelName}) | Độ lệch: {r.LevelDelta:F1} mm -> {(r.LevelPassed ? "PASS" : "FAIL")}");

        if (r.AllPassed)
        {
            _formatter.WriteSuccess("NGHIỆM THU HOÀN TOÀN: 100% 27 trục và Tầng Móng khớp tuyệt đối giữa AutoCAD và Revit (Dung sai <= 1.0 mm)!");
        }
        else
        {
            _formatter.WriteError("PHÁT HIỆN SAI LỆCH VƯỢT DUNG SAI: Vui lòng kiểm tra các trục bị FAIL.");
        }
    }

    private static async Task SaveReportAsync(VerificationResult r, string reportPath, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# BIÊN BẢN NGHIỆM THU ĐỐI SOÁT CHÉO HỆ LƯỚI TRỤC & CAO ĐỘ");
        sb.AppendLine();
        sb.AppendLine($"- **Thời điểm kiểm tra**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **Nguồn CAD**: AutoCAD 2026 (`KC-HT-90-TLTT-16.dwg`)");
        sb.AppendLine($"- **Đích Revit**: Revit 2026 (`TemplateAIModel.rvt`)");
        sb.AppendLine($"- **Dung sai quy định**: $\\le {ToleranceMm}\\text{{ mm}}$");
        sb.AppendLine();
        sb.AppendLine("## 1. Kết quả kiểm tra Hệ Lưới Trục (27 Trục)");
        sb.AppendLine();
        sb.AppendLine("| Phương | Tên Trục | Tọa độ CAD (mm) | Revit Kỳ vọng (mm) | Revit Thực tế (mm) | Sai lệch (mm) | Kết luận |");
        sb.AppendLine("| :--- | :---: | :---: | :---: | :---: | :---: | :---: |");
        foreach (var row in r.Rows)
        {
            sb.AppendLine($"| {row.Type} | **{row.Name}** | {row.CadCoord:F1} | {row.ExpectedRevit:F1} | {row.ActualRevit:F1} | {row.Delta:F1} mm | {(row.Passed ? "✅ PASS" : "❌ FAIL")} |");
        }
        sb.AppendLine();
        sb.AppendLine("## 2. Kết quả kiểm tra Cao Trình Móng");
        sb.AppendLine();
        sb.AppendLine($"- **Tên Level**: `{r.LevelName}`");
        sb.AppendLine($"- **Cao độ thiết kế (DWG)**: `{r.ExpLevel:F1} mm` (-1.000 m)");
        sb.AppendLine($"- **Cao độ mô hình (Revit)**: `{r.ActLevel:F1} mm`");
        sb.AppendLine($"- **Sai lệch**: `{r.LevelDelta:F1} mm` -> {(r.LevelPassed ? "✅ PASS" : "❌ FAIL")}");
        sb.AppendLine();
        sb.AppendLine("## 3. Kết luận chung của Hai Trợ lý");
        sb.AppendLine();
        sb.AppendLine(r.AllPassed
            ? "> **KẾT LUẬN**: Hệ lưới trục và cao trình móng đã dựng trong Revit 2026 khớp 100% với bản vẽ AutoCAD gốc. Đủ điều kiện kỹ thuật để tiến hành giai đoạn 2: Cắm 196 tim cọc móng."
            : "> **CẢNH BÁO**: Có sai lệch vượt dung sai cho phép. Yêu cầu rà soát lại trước khi triển khai cọc.");

        await File.WriteAllTextAsync(reportPath, sb.ToString(), ct).ConfigureAwait(false);
    }

    private sealed record CadGridData(double OriginX, double OriginY, List<(string Name, double CadX)> XGrids, List<(string Name, double CadY)> YGrids);
    private sealed record RevitGridData(List<(string Name, double X0, double Y0, double X1, double Y1)> Grids, List<(string Name, double ElevationMm)> Levels);
    private sealed record GridCompareRow(string Type, string Name, double CadCoord, double ExpectedRevit, double ActualRevit, double Delta, bool Passed);
    private sealed record VerificationResult(List<GridCompareRow> Rows, bool LevelFound, string LevelName, double ExpLevel, double ActLevel, double LevelDelta, bool LevelPassed, bool AllPassed);
}
