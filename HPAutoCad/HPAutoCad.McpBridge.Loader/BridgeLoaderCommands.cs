using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;

namespace HPAutoCad.McpBridge.Loader;

/// <summary>
///     The AutoCAD commands of the bridge. They live here because AutoCAD only scans the assembly it
///     loaded for <see cref="CommandMethodAttribute"/>; each one forwards to a delegate the bridge
///     registered at start-up. None of them needs a document — Session flags keep them available while
///     no drawing is open, which is exactly when a user wants to see why the AI got "no drawing". There is
///     deliberately no command that runs a script: every run comes in over the pipe so the audit sees all of them.
/// </summary>
public static class BridgeLoaderCommands
{
    /// <summary>Opens the bridge status window (listener on/off, the "Allow AI code execution" opt-in, last run).</summary>
    [CommandMethod("HPMCPBRIDGE", CommandFlags.Session)]
    public static void ShowBridgeWindow() => Run("show");

    [CommandMethod("HPMCPSTART", CommandFlags.Session)]
    public static void StartListener() => Run("start");

    [CommandMethod("HPMCPSTOP", CommandFlags.Session)]
    public static void StopListener() => Run("stop");

    /// <summary>Prints the bridge state (pipe, listener, opt-in, last run) on the command line.</summary>
    [CommandMethod("HPMCPSTATUS", CommandFlags.Session)]
    public static void PrintStatus() => Run("status");

    private static void Run(string key, params object?[] args)
    {
        var editor = Application.DocumentManager.MdiActiveDocument?.Editor;

        try
        {
            var result = BridgeLoaderApplication.Invoke(key, args);
            if (result is string text && text.Length > 0) editor?.WriteMessage("\n" + text + "\n");
        }
        catch (System.Exception exception)
        {
            var message = exception is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner.Message : exception.Message;
            editor?.WriteMessage($"\n[HPAutoCad MCP] {key} failed: {message}\n");
            LoaderLog.Write($"command {key} failed", exception);
        }
    }
}
