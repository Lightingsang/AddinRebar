namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Request For Information (RFI) record raised when information conflicts or is missing.
/// </summary>
public sealed record RfiRecord
{
    public string RfiId { get; init; } = string.Empty;
    public IReadOnlyList<string> ElementIds { get; init; } = Array.Empty<string>();
    public string Title { get; init; } = string.Empty;
    public string ConflictDescription { get; init; } = string.Empty;
    public string ProposedAction { get; init; } = string.Empty;
    public RfiStatus Status { get; init; } = RfiStatus.PendingChiefEngineer;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; init; }
}
