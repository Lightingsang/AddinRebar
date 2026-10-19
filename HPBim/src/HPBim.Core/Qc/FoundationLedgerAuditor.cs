namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Coordination;

/// <summary>
/// Dedicated auditor for Step 8: Validates coordination ledger synchronization and acceptance dossier readiness.
/// Adheres strictly to Rule 0 and Clean Code C1/C2 (<= 120 lines, methods <= 40 lines).
/// </summary>
public sealed class FoundationLedgerAuditor
{
    public FoundationStepResult AuditLedgerAndDossier(
        IReadOnlyList<FoundationStepResult> priorSteps,
        BimCoordinationLedger? ledger,
        int expectedPiles = 196,
        int expectedFootings = 72,
        int minExpectedBeams = 188,
        int maxExpectedBeams = 192)
    {
        if (CheckPriorSteps(priorSteps, out var earlyResult))
        {
            return earlyResult!;
        }

        if (ledger == null)
        {
            return new FoundationStepResult(
                8, "Sổ Cái & Hồ Sơ Nghiệm Thu", FoundationStepStatus.Inconclusive,
                "Chưa đủ cơ sở kết luận: Chưa cung cấp Sổ cái điều phối (BimCoordinationLedger). Theo R1, cấm tự động nghiệm thu.",
                new[] { "Thiếu nguồn dữ liệu đối chiếu Sổ cái điều phối (BimCoordinationLedger is null)." });
        }

        var errors = new List<string>();
        ValidateLedgerContent(ledger, expectedPiles, expectedFootings, minExpectedBeams, maxExpectedBeams, errors);

        bool passed = errors.Count == 0;
        var status = passed ? FoundationStepStatus.Passed : FoundationStepStatus.Failed;
        string summary = passed
            ? $"Sổ cái đồng bộ 100% ({ledger.Elements.Count} cấu kiện, 0 RFI tồn đọng). Đủ điều kiện nghiệm thu và đóng băng phân đoạn."
            : $"Sổ cái chưa đạt điều kiện nghiệm thu: {string.Join("; ", errors)}";

        return new FoundationStepResult(8, "Sổ Cái & Hồ Sơ Nghiệm Thu", status, summary, errors);
    }

    private static bool CheckPriorSteps(IReadOnlyList<FoundationStepResult> priorSteps, out FoundationStepResult? failureResult)
    {
        failureResult = null;
        if (priorSteps.Any(s => s.Status == FoundationStepStatus.Failed))
        {
            var failedSteps = priorSteps.Where(s => s.Status == FoundationStepStatus.Failed).Select(s => s.StepNumber);
            failureResult = new FoundationStepResult(
                8, "Sổ Cái & Hồ Sơ Nghiệm Thu", FoundationStepStatus.Failed,
                "Không đủ điều kiện ghi nhận Sổ cái do các bước kiểm toán trước đó thất bại.",
                new[] { $"Các bước kiểm toán trước ({string.Join(", ", failedSteps)}) không đạt chuẩn. Không đủ điều kiện ghi nhận Sổ cái." });
            return true;
        }

        if (priorSteps.Any(s => s.Status == FoundationStepStatus.Inconclusive))
        {
            var inconSteps = priorSteps.Where(s => s.Status == FoundationStepStatus.Inconclusive).Select(s => s.StepNumber);
            failureResult = new FoundationStepResult(
                8, "Sổ Cái & Hồ Sơ Nghiệm Thu", FoundationStepStatus.Inconclusive,
                "Chưa đủ cơ sở kết luận: Một số bước kiểm toán hình học trước đó thiếu dữ liệu đối chiếu.",
                new[] { $"Các bước kiểm toán ({string.Join(", ", inconSteps)}) chưa đủ cơ sở kết luận. Cấm tự động nghiệm thu." });
            return true;
        }

        return false;
    }

    private static void ValidateLedgerContent(
        BimCoordinationLedger ledger,
        int expectedPiles,
        int expectedFootings,
        int minExpectedBeams,
        int maxExpectedBeams,
        List<string> errors)
    {
        if (!string.Equals(ledger.ActivePhase, "PHAN_MONG", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ledger.ActivePhase, "Foundation", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Sổ cái đang ở phân đoạn '{ledger.ActivePhase}' thay vì 'PHAN_MONG'.");
        }

        int ledgerPiles = ledger.Elements.Count(e => e.Category == CoordinationCategory.Pile);
        int ledgerFootings = ledger.Elements.Count(e => e.Category == CoordinationCategory.Footing);
        int ledgerBeams = ledger.Elements.Count(e => e.Category == CoordinationCategory.GradeBeam);

        if (ledgerPiles != expectedPiles) errors.Add($"Sổ cái ghi nhận {ledgerPiles} cọc != {expectedPiles} cọc.");
        if (ledgerFootings != expectedFootings) errors.Add($"Sổ cái ghi nhận {ledgerFootings} đài != {expectedFootings} đài.");
        if (ledgerBeams < minExpectedBeams || ledgerBeams > maxExpectedBeams)
        {
            errors.Add($"Sổ cái ghi nhận {ledgerBeams} giằng móng ngoài phạm vi {minExpectedBeams}-{maxExpectedBeams}.");
        }

        int pendingRfis = ledger.Rfis.Count(r => r.Status == RfiStatus.PendingChiefEngineer);
        if (pendingRfis > 0) errors.Add($"Còn {pendingRfis} RFI đang chờ Kỹ sư Chủ trì làm rõ, không được phép nghiệm thu.");

        int unready = ledger.Elements.Count(e => e.TechnicalGroup != ElementTechnicalGroup.Group1_ReadyToModel);
        if (unready > 0) errors.Add($"Còn {unready} cấu kiện trong Sổ cái có mâu thuẫn hoặc thiếu thông tin (Nhóm 2/Nhóm 3).");
    }
}
