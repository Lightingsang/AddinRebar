namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

/// <summary>
///     Captures the state of a removed Grasshopper component for potential rollback.
/// </summary>
internal sealed record RemovedComponentSnapshot(
    IGH_DocumentObject Object,
    IReadOnlyList<(IGH_Param Target, IGH_Param Source)> SeveredWires,
    byte[]? ArchiveBytes)
{
    public Guid InstanceGuid => Object.InstanceGuid;
}
