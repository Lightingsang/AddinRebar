namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;

public sealed partial class ReconcileService : IReconcileService
{
    private readonly IIntentJournal _journal;
    private readonly IRevitPipeClient _pipeClient;
    private readonly BimCoordinationLedgerService _ledgerService;
    private readonly IExtensibleStorageReceiptReader _receiptReader;

    public string? LedgerFilePath { get; set; }

    public ReconcileService(
        IIntentJournal journal,
        IRevitPipeClient pipeClient,
        BimCoordinationLedgerService ledgerService,
        IExtensibleStorageReceiptReader? receiptReader = null,
        string? ledgerFilePath = null)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _receiptReader = receiptReader ?? new ExtensibleStorageReceiptReader(pipeClient);
        LedgerFilePath = ledgerFilePath;
    }

    public ReconcileService(
        IIntentJournal journal,
        IRevitPipeClient pipeClient,
        BimCoordinationLedgerService ledgerService,
        string? ledgerFilePath)
        : this(journal, pipeClient, ledgerService, null, ledgerFilePath)
    {
    }

    public async Task<IReadOnlyList<ReconcileResult>> ReconcileAllUnknownIntentsAsync(CancellationToken cancellationToken = default)
    {
        var unresolved = await _journal.GetIncompleteIntentsAsync(cancellationToken).ConfigureAwait(false);
        if (unresolved.Count == 0)
        {
            return new[] { ReconcileResult.NoOp() };
        }

        var results = new List<ReconcileResult>(unresolved.Count);
        foreach (var intent in unresolved)
        {
            var res = await ReconcileSingleIntentAsync(intent, cancellationToken).ConfigureAwait(false);
            results.Add(res);
        }

        return results;
    }

    public async Task<ReconcileResult> ReconcileIntentAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var intent = await _journal.GetIntentAsync(operationId, cancellationToken).ConfigureAwait(false);
        if (intent == null)
        {
            return ReconcileResult.StillUnknown(operationId, string.Empty, "Intent not found in journal.");
        }

        return await ReconcileSingleIntentAsync(intent, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ReconcileResult> ReconcileSingleIntentAsync(IntentJournalRecord intent, CancellationToken ct)
    {
        try
        {
            if (!_pipeClient.IsConnected)
            {
                await _pipeClient.ConnectAsync(ct).ConfigureAwait(false);
            }

            var receipt = await _receiptReader.FindReceiptAsync(intent.OperationId, intent.PlanHash, ct).ConfigureAwait(false);
            if (receipt != null && VerifyThreeFactors(intent, receipt))
            {
                return await ApplyCommittedReceiptAsync(intent, receipt, ct).ConfigureAwait(false);
            }

            string reason = receipt == null
                ? "Receipt absent in Revit Extensible Storage. Preserving OutcomeUnknown to prevent blind overwrite."
                : $"3-Factor verification mismatch: OperationId match={receipt.OperationId == intent.OperationId}, PlanHash match={string.Equals(receipt.PlanHash, intent.PlanHash, StringComparison.OrdinalIgnoreCase)}, TargetDocId match={MatchTargetDocId(intent, receipt)}. Preserving OutcomeUnknown.";

            await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
            return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
        }
        catch (Exception ex) when (ex is RevitPipeException or IOException or TimeoutException or JsonException or InvalidOperationException or ArgumentException)
        {
            string errorPrefix = ex is RevitPipeException or IOException or TimeoutException
                ? "Pipe communication error"
                : "Receipt payload parsing error";

            return ReconcileResult.StillUnknown(
                intent.OperationId,
                intent.PlanHash,
                $"{errorPrefix} during reconciliation: {ex.Message}");
        }
    }

    public static bool VerifyThreeFactors(IntentJournalRecord intent, CoordinationReceiptRecord receipt)
    {
        if (intent == null || receipt == null) return false;

        bool factor1OpId = intent.OperationId != Guid.Empty && receipt.OperationId == intent.OperationId;
        bool factor2PlanHash = !string.IsNullOrEmpty(intent.PlanHash) &&
                               !string.IsNullOrEmpty(receipt.PlanHash) &&
                               string.Equals(intent.PlanHash, receipt.PlanHash, StringComparison.OrdinalIgnoreCase);
        bool factor3TargetDocId = MatchTargetDocId(intent, receipt);

        return factor1OpId && factor2PlanHash && factor3TargetDocId;
    }

    private async Task<ReconcileResult> ApplyCommittedReceiptAsync(
        IntentJournalRecord intent,
        CoordinationReceiptRecord receipt,
        CancellationToken ct)
    {
        if (receipt.ElementMap == null)
        {
            string reason = "Receipt ElementMap is null or corrupt in Revit Extensible Storage. Preserving OutcomeUnknown.";
            await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
            return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
        }

        string? coverageError = ValidateKeysetCoverage(intent, receipt.ElementMap);
        if (coverageError != null)
        {
            await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, coverageError, ct).ConfigureAwait(false);
            return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, coverageError);
        }

        var parsedKeys = new List<(CompositeSourceKey Key, string UniqueId)>(receipt.ElementMap.Count);
        foreach (var (keyStr, uniqueId) in receipt.ElementMap)
        {
            if (!CompositeSourceKey.TryParse(keyStr, out var sourceKey) || sourceKey == null || string.IsNullOrWhiteSpace(uniqueId))
            {
                string reason = $"Receipt contains invalid or unparseable source key '{keyStr}' or empty uniqueId. Preserving OutcomeUnknown.";
                await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
                return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
            }

            parsedKeys.Add((sourceKey, uniqueId));
        }

        foreach (var (sourceKey, uniqueId) in parsedKeys)
        {
            _ledgerService.UpdateMapping(sourceKey, uniqueId, intent.PlanHash, ElementExecutionStatus.Created);
        }

        string? persistError = await PersistLedgerBeforeCommitAsync(ct).ConfigureAwait(false);
        if (persistError != null)
        {
            await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, persistError, ct).ConfigureAwait(false);
            return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, persistError);
        }

        await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.Committed, null, ct).ConfigureAwait(false);
        return ReconcileResult.Committed(intent.OperationId, intent.PlanHash, receipt.ElementMap);
    }

    private static string? ValidateKeysetCoverage(IntentJournalRecord intent, IReadOnlyDictionary<string, string> elementMap)
    {
        if (intent.SourceKeys != null && intent.SourceKeys.Count > 0)
        {
            var expectedKeys = new HashSet<string>(intent.SourceKeys, StringComparer.OrdinalIgnoreCase);
            var actualKeys = new HashSet<string>(elementMap.Keys, StringComparer.OrdinalIgnoreCase);

            if (!expectedKeys.SetEquals(actualKeys))
            {
                int missingCount = expectedKeys.Except(actualKeys).Count();
                int foreignCount = actualKeys.Except(expectedKeys).Count();
                return $"Receipt keyset coverage mismatch: expected {expectedKeys.Count} keys, but receipt contained {actualKeys.Count} entries (missing={missingCount}, foreign={foreignCount}). Preserving OutcomeUnknown.";
            }
        }
        else if (intent.ItemCount != elementMap.Count)
        {
            return $"Receipt completeness mismatch: expected ItemCount={intent.ItemCount}, but receipt contained {elementMap.Count} entries. Preserving OutcomeUnknown.";
        }

        return null;
    }

    private async Task<string?> PersistLedgerBeforeCommitAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(LedgerFilePath))
        {
            return null;
        }

        try
        {
            await _ledgerService.SaveToFileAsync(LedgerFilePath!, ct).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            return $"Failed to persist coordination ledger to disk at '{LedgerFilePath}': {ex.Message}. Preserving OutcomeUnknown.";
        }
    }
}
