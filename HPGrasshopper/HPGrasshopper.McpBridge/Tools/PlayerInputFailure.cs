namespace HPGrasshopper.McpBridge.Tools;

/// <summary>
///     Diagnostic record detailing a rejected or unmatched input parameter in Grasshopper Player.
/// </summary>
public sealed record PlayerInputFailure(string Name, string Reason);
