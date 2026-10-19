namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;

/// <summary>
/// Strongly-typed record of a receipt read from Revit Extensible Storage on a DataStorage element.
/// </summary>
public sealed record CoordinationReceiptRecord
{
    public Guid OperationId { get; init; } = Guid.Empty;
    public string PlanHash { get; init; } = string.Empty;
    public string TargetDocId { get; init; } = string.Empty;
    public string TargetDocTitle { get; init; } = string.Empty;
    public string TargetDocPath { get; init; } = string.Empty;
    public string TargetVersionGuid { get; init; } = string.Empty;
    public string TargetCentralGuid { get; init; } = string.Empty;
    public string CommittedAtUtc { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string>? ElementMap { get; init; }
}
