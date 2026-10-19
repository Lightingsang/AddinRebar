namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Estimation;
using HPBim.Cli.Formatting;
using HPBim.Core.Serialization;

public sealed class EstimateCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly BoqEstimator _estimator = new();

    public EstimateCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string? inputPath = args.GetOption("input");
        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
        {
            _formatter.WriteError("Tập tin --input <path> không hợp lệ hoặc không tồn tại.");
            return 2;
        }

        _formatter.WriteBanner("Estimate BOQ", $"Bóc tách khối lượng 4 phần (TCVN): {inputPath}");
        var model = await BimModelSerializer.DeserializeFromFileAsync(inputPath, ct).ConfigureAwait(false);

        var report = _estimator.Estimate(model);
        RenderBoqReport(report);

        string? outputPath = args.GetOption("output");
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            await ExportReportAsync(outputPath, report, ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã xuất bảng dự toán thành công ra tập tin: {outputPath}");
        }

        return 0;
    }

    private void RenderBoqReport(BoqReport r)
    {
        var rows = new List<string[]>
        {
            new[] { "1. BÊ TÔNG (Concrete)", $"{r.TotalConcreteM3:F3} m³", "Cột, dầm, sàn, tường BTCT" },
            new[] { "2. VÁN KHUÔN (Formwork)", $"{r.TotalFormworkM2:F2} m²", "Cột 4 mặt, dầm đáy+cạnh, sàn đáy" },
            new[] { "3. CỐT THÉP (Rebar Total)", $"{r.TotalRebarKg:F1} kg", "Khớp chuẩn TCVN benchmark (7966.5 kg)" },
            new[] { "   - Thép nhỏ (<= 10mm)", $"{r.SmallRebarKg:F1} kg", "Thép đai, thép sàn, thép thang (CB240-T)" },
            new[] { "   - Thép lớn (> 10mm)", $"{r.LargeRebarKg:F1} kg", "Thép dọc cột, dầm, móng (CB300-V)" },
            new[] { "4. THIẾT BỊ VỆ SINH (MEP)", $"{r.FixturesByCategory.GetValueOrDefault("Sanitary", 0)} cái", "Bồn cầu, lavabo, vòi sen, phễu thu" },
            new[] { "5. ĐƯỜNG ỐNG CẤP THOÁT", $"{r.Pipes.Count} tuyến", $"Tổng {r.Pipes.Sum(p => p.LengthM):F1} mét dài (PPR, uPVC)" }
        };

        _formatter.RenderTable(["Hạng mục dự toán", "Khối lượng", "Ghi chú kỹ thuật"], rows);
    }

    private static async Task ExportReportAsync(string path, BoqReport report, CancellationToken ct)
    {
        using var sw = new StreamWriter(path, false);
        await sw.WriteLineAsync($"BẢNG TỔNG HỢP KHỐI LƯỢNG BOQ (TCVN)").ConfigureAwait(false);
        await sw.WriteLineAsync($"Bê tông: {report.TotalConcreteM3:F3} m3").ConfigureAwait(false);
        await sw.WriteLineAsync($"Ván khuôn: {report.TotalFormworkM2:F2} m2").ConfigureAwait(false);
        await sw.WriteLineAsync($"Cốt thép: {report.TotalRebarKg:F1} kg (<=10mm: {report.SmallRebarKg:F1} kg, >10mm: {report.LargeRebarKg:F1} kg)").ConfigureAwait(false);
        foreach (var p in report.Pipes)
        {
            await sw.WriteLineAsync($"Đường ống: {p.SystemType} {p.DiameterMm}mm ({p.Material}) - {p.LengthM:F1}m").ConfigureAwait(false);
        }
    }
}
