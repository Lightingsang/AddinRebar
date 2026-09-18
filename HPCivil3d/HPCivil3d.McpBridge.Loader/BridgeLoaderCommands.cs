using Autodesk.AutoCAD.Runtime;

namespace HPCivil3d.McpBridge.Loader;

/// <summary>
///     The AutoCAD commands of the bridge. They live here because AutoCAD only scans the assembly it
///     loaded for <see cref="CommandMethodAttribute"/>; each one forwards to a delegate the bridge
///     registered at start-up through <see cref="BridgeActions"/>, the same path the Ribbon button takes.
///     None of them needs a document — Session flags keep them available while no drawing is open, which
///     is exactly when a user wants to see why the AI got "no drawing". There is deliberately no command
///     that runs a script: every run comes in over the pipe so the audit sees all of them.
/// </summary>
public static class BridgeLoaderCommands
{
    /// <summary>Opens the bridge status window (listener on/off, the "Allow AI code execution" opt-in, last run).</summary>
    [CommandMethod("HPC3DMCPBRIDGE", CommandFlags.Session)]
    public static void ShowBridgeWindow() => BridgeActions.Run("show");

    [CommandMethod("HPC3DMCPSTART", CommandFlags.Session)]
    public static void StartListener() => BridgeActions.Run("start");

    [CommandMethod("HPC3DMCPSTOP", CommandFlags.Session)]
    public static void StopListener() => BridgeActions.Run("stop");

    /// <summary>Prints the bridge state (pipe, listener, opt-in, last run) on the command line.</summary>
    [CommandMethod("HPC3DMCPSTATUS", CommandFlags.Session)]
    public static void PrintStatus() => BridgeActions.Run("status");
}
