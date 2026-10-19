namespace HPBim.Cli.Commands;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Core.Qc;
using HPBim.Core.Serialization;
using HPBim.Core.Validation;

public sealed class ValidateCommand
{
    private readonly ConsoleFormatter _formatter;

    public ValidateCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string? inputPath = args.GetOption("input");
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            _formatter.WriteError("Tham số --input <path> là bắt buộc.");
            return 2;
        }

        if (!File.Exists(inputPath))
        {
            _formatter.WriteError($"Tập tin mô hình không tồn tại: {inputPath}");
            return 2;
        }

        _formatter.WriteBanner("Validate BIM Model", $"Kiểm tra hợp chuẩn và va chạm offline: {inputPath}");

        string json = await File.ReadAllTextAsync(inputPath, ct).ConfigureAwait(false);

        // Step 1: Strict Artifact & Schema Validation Gate
        var schemaResult = BimModelSchemaValidator.Validate(json, requireStrictDiscriminant: true);
        if (!schemaResult.IsValid)
        {
            _formatter.WriteError("THẤT BẠI: Dữ liệu không đạt chuẩn Schema BIM.");
            var errorRows = schemaResult.Errors.Select((err, idx) => new[] { (idx + 1).ToString(), err }).ToArray();
            _formatter.RenderTable(["STT", "Chi tiết lỗi Schema"], errorRows);
            _formatter.WriteError("TRẠNG THÁI: KHÔNG HỢP LỆ (Schema Rejected). Không thể dispatch sang Revit.");
            return 1;
        }

        // Step 2: Deserialization into Strongly-Typed Domain Model
        var model = BimModelSerializer.Deserialize(json);

        // Step 3: Domain & TCVN QC Engine
        var qcEngine = new QcEngine();
        var report = qcEngine.Validate(model);

        RenderValidationSummary(report);
        return report.IsValid ? 0 : 1;
    }

    private void RenderValidationSummary(QcReport report)
    {
        string[][] summaryRows =
        [
            ["Số lượt kiểm tra", report.TotalChecksRun.ToString()],
            ["Thời gian thực hiện", $"{report.Duration.TotalMilliseconds:F1} ms"],
            ["Lỗi quy chuẩn (Errors)", report.Errors.Count.ToString()],
            ["Cảnh báo (Warnings)", report.Warnings.Count.ToString()],
            ["Va chạm (Clashes)", report.Clashes.Count.ToString()]
        ];
        _formatter.RenderTable(["Chỉ số", "Giá trị"], summaryRows);

        RenderClashesAndErrors(report);

        if (report.IsValid)
        {
            _formatter.WriteSuccess("TRẠNG THÁI: SẠCH 100%. Mô hình sẵn sàng dispatch tới Revit.");
        }
        else
        {
            _formatter.WriteError("TRẠNG THÁI: KHÔNG HỢP LỆ. Vui lòng khắc phục các lỗi trước khi dựng hình.");
        }
    }

    private void RenderClashesAndErrors(QcReport report)
    {
        if (report.Clashes.Count > 0)
        {
            var clashRows = new string[report.Clashes.Count][];
            for (int i = 0; i < report.Clashes.Count; i++)
            {
                var c = report.Clashes[i];
                clashRows[i] = [c.ClashType, $"{c.ElementIdA} vs {c.ElementIdB}", $"({c.Location.X:F0}, {c.Location.Y:F0})", c.Description];
            }
            _formatter.RenderTable(["Loại va chạm", "Cặp cấu kiện", "Tọa độ", "Mô tả"], clashRows);
        }

        if (report.Errors.Count > 0)
        {
            var errorRows = new string[report.Errors.Count][];
            for (int i = 0; i < report.Errors.Count; i++)
            {
                var e = report.Errors[i];
                errorRows[i] = [e.RuleId, e.ElementId ?? "-", e.Message];
            }
            _formatter.RenderTable(["Mã luật", "Cấu kiện", "Chi tiết lỗi"], errorRows);
        }
    }
}
