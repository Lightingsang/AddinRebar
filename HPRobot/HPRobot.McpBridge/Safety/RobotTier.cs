namespace HPRobot.McpBridge.Safety;

/// <summary>
///     3-Tier safety classification for Autodesk Robot Structural Analysis operations:
///     - Read: Safe queries of geometry, properties, materials, loads, and results (get_structural_objects, get_bar_forces).
///     - Write: Modifying model entities (draw_bar_by_coords, assign_node_support, assign_bar_load). Auto-snapshot taken.
///     - DeleteHeavy: Structural element deletions, model clear, or FEA solver execution (run_calculations, Delete).
///       Requires explicit UI opt-in checkbox 'AllowHeavyOperations'.
/// </summary>
public enum RobotTier
{
    Read = 0,
    Write = 1,
    DeleteHeavy = 2
}
