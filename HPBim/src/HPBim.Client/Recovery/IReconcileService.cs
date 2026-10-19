namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service responsible for resolving ambiguous operation intents by cross-referencing
/// Revit Extensible Storage receipts and restoring coordination ledger mappings.
/// </summary>
public interface IReconcileService
{
    Task<IReadOnlyList<ReconcileResult>> ReconcileAllUnknownIntentsAsync(CancellationToken cancellationToken = default);

    Task<ReconcileResult> ReconcileIntentAsync(Guid operationId, CancellationToken cancellationToken = default);
}
