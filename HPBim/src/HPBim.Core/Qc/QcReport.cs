namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using System.Text;
using HPBim.Core.Geometry;

public enum QcSeverity
{
    Warning,
    Error,
    Critical
}

public readonly record struct QcViolation(
    QcSeverity Severity,
    string RuleId,
    string Message,
    string? ElementId = null,
    Point2D? Location = null);

public readonly record struct QcClash(
    string ElementIdA,
    string ElementIdB,
    string ClashType,
    string Description,
    Point2D Location,
    double PenetrationMm);

public sealed class QcReport
{
    public bool IsValid => Errors.Count == 0 && Clashes.Count == 0;
    public IReadOnlyList<QcViolation> Errors { get; }
    public IReadOnlyList<QcViolation> Warnings { get; }
    public IReadOnlyList<QcClash> Clashes { get; }
    public int TotalChecksRun { get; }
    public TimeSpan Duration { get; }

    public QcReport(
        IReadOnlyList<QcViolation> errors,
        IReadOnlyList<QcViolation> warnings,
        IReadOnlyList<QcClash> clashes,
        int totalChecksRun,
        TimeSpan duration)
    {
        Errors = errors ?? Array.Empty<QcViolation>();
        Warnings = warnings ?? Array.Empty<QcViolation>();
        Clashes = clashes ?? Array.Empty<QcClash>();
        TotalChecksRun = totalChecksRun;
        Duration = duration;
    }

    public string FormatSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== KẾT QUẢ SOÁT LỖI QC OFFLINE ({Duration.TotalMilliseconds:F1}ms) ===");
        sb.AppendLine($"Tổng số kiểm tra: {TotalChecksRun} | Lỗi: {Errors.Count} | Cảnh báo: {Warnings.Count} | Va chạm: {Clashes.Count}");

        if (IsValid)
        {
            sb.AppendLine("TRẠNG THÁI: SẠCH (Hợp lệ 100%, sẵn sàng dispatch tới Revit).");
            return sb.ToString();
        }

        sb.AppendLine("TRẠNG THÁI: KHÔNG HỢP LỆ (Phát hiện lỗi cần xử lý trước khi dựng hình).");
        if (Clashes.Count > 0)
        {
            sb.AppendLine("--- VA CHẠM CẤU KIỆN (CLASHES) ---");
            foreach (var clash in Clashes)
            {
                sb.AppendLine($"[CLASH] {clash.ClashType}: {clash.ElementIdA} vs {clash.ElementIdB} tại ({clash.Location.X:F0}, {clash.Location.Y:F0}) - {clash.Description}");
            }
        }

        if (Errors.Count > 0)
        {
            sb.AppendLine("--- LỖI QUY CHUẨN (ERRORS) ---");
            foreach (var err in Errors)
            {
                sb.AppendLine($"[{err.RuleId}] {err.Message}");
            }
        }

        if (Warnings.Count > 0)
        {
            sb.AppendLine("--- CẢNH BÁO (WARNINGS) ---");
            foreach (var warn in Warnings)
            {
                sb.AppendLine($"[{warn.RuleId}] {warn.Message}");
            }
        }

        return sb.ToString();
    }
}
