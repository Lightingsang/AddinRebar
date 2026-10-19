namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;

public sealed partial class BimPipelineEngine
{
    private static readonly HashSet<string> ValidSkipReasonCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SKIP_FROZEN_ZONE",
        "SKIP_STAGE_DISABLED",
        "SKIP_PRECONDITION_FAILED",
        "SKIP_PREREQUISITE_FAILED",
        "SKIP_USER_EXCLUDED"
    };

    private static bool IsIntegrityDefect(ElementExecutionRecord rec) =>
        rec.Status == ElementExecutionStatus.Failed && rec.ErrorCode is "ERR_FOREIGN_RECORD"
            or "ERR_DUPLICATE_RECORD"
            or "ERR_INVALID_STATUS_ENUM"
            or "ERR_MISSING_TARGET_UNIQUE_ID"
            or "ERR_CATEGORY_MISMATCH"
            or "ERR_MARK_MISMATCH"
            or "ERR_INTEGRITY_DEFECT_SKIPPED"
            or "ERR_NOOP_TARGET_MISMATCH";

    private static ElementExecutionRecord ValidateReturnedRecord(
        ElementPlanItem item,
        ElementExecutionRecord rec,
        bool isFailedRollback)
    {
        var identityRec = ValidateIdentityAndTargets(item, rec);
        if (identityRec.Status == ElementExecutionStatus.Failed)
        {
            return identityRec;
        }

        return ValidateStatusAndRollback(item, identityRec, isFailedRollback);
    }

    private static ElementExecutionRecord ValidateIdentityAndTargets(
        ElementPlanItem item,
        ElementExecutionRecord rec)
    {
        if (rec.Category != item.Category)
        {
            return rec with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_CATEGORY_MISMATCH",
                ErrorReason = $"Category mismatch for '{item.SourceKey}': expected '{item.Category}', returned '{rec.Category}'."
            };
        }

        if (!string.Equals(rec.Mark, item.Mark, StringComparison.OrdinalIgnoreCase))
        {
            return rec with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_MARK_MISMATCH",
                ErrorReason = $"Mark mismatch for '{item.SourceKey}': expected '{item.Mark}', returned '{rec.Mark}'."
            };
        }

        if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated or ElementExecutionStatus.NoOp &&
            string.IsNullOrWhiteSpace(rec.TargetUniqueId))
        {
            return rec with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID",
                ErrorReason = $"Element '{item.Mark}' with status '{rec.Status}' MUST have a non-empty TargetUniqueId."
            };
        }

        if (rec.Status == ElementExecutionStatus.NoOp &&
            !string.IsNullOrEmpty(item.TargetUniqueId) &&
            !string.Equals(rec.TargetUniqueId, item.TargetUniqueId, StringComparison.OrdinalIgnoreCase))
        {
            return rec with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_NOOP_TARGET_MISMATCH",
                ErrorReason = $"NoOp element '{item.Mark}' returned TargetUniqueId '{rec.TargetUniqueId}' which does not match ledger TargetUniqueId '{item.TargetUniqueId}'."
            };
        }

        return rec;
    }

    private static ElementExecutionRecord ValidateStatusAndRollback(
        ElementPlanItem item,
        ElementExecutionRecord rec,
        bool isFailedRollback)
    {
        if (rec.Status == ElementExecutionStatus.Skipped && !IsSkipAllowed(item, rec))
        {
            return rec with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_INTEGRITY_DEFECT_SKIPPED",
                ErrorReason = $"Required plan item '{item.Mark}' was skipped without a valid defined reason: '{rec.ErrorReason ?? rec.ErrorCode}'."
            };
        }

        if (isFailedRollback && rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated or ElementExecutionStatus.NoOp)
        {
            return rec with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_BATCH_ROLLED_BACK",
                ErrorReason = "Transaction rolled back due to failure in batch."
            };
        }

        return rec;
    }

    private static ElementExecutionRecord CreateMissingItemRecord(ElementPlanItem item, bool isFailedRollback) =>
        isFailedRollback
            ? new ElementExecutionRecord
            {
                SourceKey = item.SourceKey,
                Category = item.Category,
                Mark = item.Mark,
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_BATCH_ROLLED_BACK",
                ErrorReason = $"Transaction rolled back before plan item '{item.Mark}' ({item.SourceKey}) could be committed."
            }
            : ElementExecutionRecord.OutcomeUnknown(
                item.SourceKey,
                item.Category,
                item.Mark,
                $"No execution record returned from Revit for plan item '{item.Mark}' ({item.SourceKey}).",
                "ERR_RECORD_DEFICIT");

    private static IReadOnlyList<ElementExecutionRecord> ApplyBatchDefectResolution(
        List<ElementExecutionRecord> reconciled,
        List<ElementExecutionRecord> foreignRecords,
        List<CompositeSourceKey> duplicateKeys,
        bool hostRolledBack)
    {
        var (batchCode, batchReason) = DetectBatchDefect(foreignRecords, duplicateKeys);
        var firstDefect = reconciled.FirstOrDefault(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode != "ERR_BATCH_ROLLED_BACK");

        return reconciled.Select(r => ResolveSingleItemOutcome(r, firstDefect, batchCode, batchReason, hostRolledBack)).ToList();
    }

    private static ElementExecutionRecord ResolveSingleItemOutcome(
        ElementExecutionRecord r,
        ElementExecutionRecord? firstDefect,
        string? batchCode,
        string? batchReason,
        bool hostRolledBack)
    {
        if (r.Status == ElementExecutionStatus.Failed && r.ErrorCode != "ERR_BATCH_ROLLED_BACK" && batchCode == null)
        {
            return r;
        }

        if (batchCode != null)
        {
            return r with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = batchCode,
                ErrorReason = batchReason
            };
        }

        if (hostRolledBack)
        {
            return r with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_BATCH_ROLLED_BACK",
                ErrorReason = firstDefect != null
                    ? $"Transaction rolled back by Revit bridge: [{firstDefect.ErrorCode}] {firstDefect.ErrorReason}"
                    : "Transaction rolled back by Revit bridge."
            };
        }

        return r with
        {
            Status = ElementExecutionStatus.OutcomeUnknown,
            ErrorCode = "ERR_POST_DISPATCH_INTEGRITY_DEFECT",
            ErrorReason = firstDefect != null
                ? $"Host transaction was NOT rolled back, but post-dispatch integrity defect occurred: [{firstDefect.ErrorCode}] {firstDefect.ErrorReason}. Model state is ambiguous; reconciliation required."
                : "Host transaction was NOT rolled back, but post-dispatch integrity defect occurred. Model state is ambiguous; reconciliation required."
        };
    }

    private static (string? Code, string? Reason) DetectBatchDefect(
        List<ElementExecutionRecord> foreign,
        List<CompositeSourceKey> duplicates)
    {
        if (foreign.Count > 0)
        {
            return ("ERR_FOREIGN_RECORD",
                $"Foreign record detected: SourceKey '{foreign[0].SourceKey}' was returned by Revit but does not exist in the execution plan.");
        }
        if (duplicates.Count > 0)
        {
            return ("ERR_DUPLICATE_RECORD",
                $"Duplicate records detected: Multiple execution records returned for SourceKey '{duplicates[0]}'.");
        }
        return (null, null);
    }

    private static bool IsSkipAllowed(ElementPlanItem item, ElementExecutionRecord rec) =>
        (item.Parameters.TryGetValue("SkipAllowed", out var sa) && sa is bool b && b) || IsValidSkipReason(rec.ErrorCode);

    private static bool IsValidSkipReason(string? code) =>
        !string.IsNullOrWhiteSpace(code) && ValidSkipReasonCodes.Contains(code!);
}
