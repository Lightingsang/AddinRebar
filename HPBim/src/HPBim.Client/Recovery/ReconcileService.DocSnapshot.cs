namespace HPBim.Client.Recovery;

using System;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using HPBim.Core.Models;

public sealed partial class ReconcileService
{
    private static bool MatchTargetDocId(IntentJournalRecord intent, CoordinationReceiptRecord receipt)
    {
        var targetDoc = intent.TargetDoc;
        if (targetDoc != null && HasStaticIdentity(targetDoc))
        {
            return MatchTargetDocSnapshot(targetDoc, receipt);
        }

        string expectedDocId = intent.TargetDocId;
        string actualDocId = receipt.TargetDocId;

        if (string.IsNullOrWhiteSpace(expectedDocId) && string.IsNullOrWhiteSpace(actualDocId))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(expectedDocId) || string.IsNullOrWhiteSpace(actualDocId))
        {
            return false;
        }

        return string.Equals(expectedDocId, actualDocId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchTargetDocSnapshot(DocumentIdentitySnapshot targetDoc, CoordinationReceiptRecord receipt)
    {
        if (targetDoc == null || receipt == null)
        {
            return false;
        }

        string actualTitle = !string.IsNullOrEmpty(receipt.TargetDocTitle)
            ? receipt.TargetDocTitle
            : receipt.TargetDocId;

        if (!string.IsNullOrEmpty(targetDoc.Title))
        {
            if (string.IsNullOrEmpty(actualTitle) ||
                !string.Equals(targetDoc.Title, actualTitle, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(targetDoc.PathName))
        {
            if (string.IsNullOrEmpty(receipt.TargetDocPath) ||
                !string.Equals(targetDoc.PathName, receipt.TargetDocPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (targetDoc.CentralGuid.HasValue && targetDoc.CentralGuid.Value != Guid.Empty)
        {
            if (string.IsNullOrEmpty(receipt.TargetCentralGuid) ||
                !Guid.TryParse(receipt.TargetCentralGuid, out var centGuid) ||
                centGuid == Guid.Empty ||
                targetDoc.CentralGuid.Value != centGuid)
            {
                return false;
            }
        }

        return true;
    }

    public static bool MatchTargetDocSnapshot(DocSnapshot targetDoc, CoordinationReceiptRecord receipt)
    {
        if (targetDoc == null || receipt == null)
        {
            return false;
        }

        string actualTitle = !string.IsNullOrEmpty(receipt.TargetDocTitle)
            ? receipt.TargetDocTitle
            : receipt.TargetDocId;

        return targetDoc.MatchesStaticIdentity(
            actualTitle,
            receipt.TargetDocPath,
            receipt.TargetCentralGuid,
            receipt.TargetCentralGuid);
    }

    public static bool HasStaticIdentity(DocumentIdentitySnapshot targetDoc)
    {
        if (targetDoc == null)
        {
            return false;
        }

        return !string.IsNullOrEmpty(targetDoc.Title) ||
               !string.IsNullOrEmpty(targetDoc.PathName) ||
               (targetDoc.CentralGuid.HasValue && targetDoc.CentralGuid.Value != Guid.Empty);
    }
}
