namespace HPGrasshopper.McpBridge.Tools;

/// <summary>
///     Information about a component created during a canvas batch edit operation.
/// </summary>
public sealed record CreatedComponentInfo(
    int Index,
    string? TempId,
    string InstanceGuid,
    string Name,
    string NickName,
    string Type);
