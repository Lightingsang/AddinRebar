namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for querying Revit Extensible Storage coordination receipts.
/// </summary>
public interface IExtensibleStorageReceiptReader
{
    Task<CoordinationReceiptRecord?> FindReceiptByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<CoordinationReceiptRecord?> FindReceiptByPlanHashAsync(
        string planHash,
        CancellationToken cancellationToken = default);

    Task<CoordinationReceiptRecord?> FindReceiptAsync(
        Guid operationId,
        string? planHash = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoordinationReceiptRecord>> GetAllReceiptsAsync(
        CancellationToken cancellationToken = default);
}
