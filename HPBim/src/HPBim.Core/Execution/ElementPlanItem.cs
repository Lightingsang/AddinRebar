namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HPBim.Core.Coordination;

/// <summary>
/// Atomic element operation in the execution plan with pre-resolved coordinates,
/// level bindings, and family symbol specifications.
/// </summary>
public sealed record ElementPlanItem
{
    public CompositeSourceKey SourceKey { get; init; } = new();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CoordinationCategory Category { get; init; }
    public string Mark { get; init; } = string.Empty;
    public int SequenceOrder { get; init; }
    public string TargetLevelName { get; init; } = string.Empty;
    public string? TargetTopLevelName { get; init; }
    public ResourceResolutionSpec Resource { get; init; } = new();
    public ElementGeometrySpec Geometry { get; init; } = new PointGeometrySpec();
    public IReadOnlyList<string> ProvenanceHandles { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public bool IsStructural { get; init; }
    public bool CreateAssociatedView { get; init; }
    public string? HostElementKey { get; init; }
    public string? TargetUniqueId { get; init; }
}
