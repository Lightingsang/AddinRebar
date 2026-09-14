using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;

namespace HPAutoCad.McpBridge.Loader;

/// <summary>
///     The AutoCAD commands of the bridge. They live here because AutoCAD only scans the assembly it
///     loaded for <see cref="CommandMethodAttribute"/>; each one forwards to a delegate the bridge
///     registered at start-up. None of them touches a document, so the default modal flags are fine.
/// </summary>
public static class BridgeLoaderCommands
{
    /// <summary>Opens the bridge status window (listener on/off, the "Allow AI code execution" opt-in, last run).</summary>
    [CommandMethod("HPMCPBRIDGE")]
    public static void ShowBridgeWindow() => Run("show");

    [CommandMethod("HPMCPSTART")]
    public static void StartListener() => Run("start");

    [CommandMethod("HPMCPSTOP")]
    public static void StopListener() => Run("stop");

    /// <summary>Prints the bridge state (pipe, listener, opt-in, last run) on the command line.</summary>
    [CommandMethod("HPMCPSTATUS")]
    public static void PrintStatus() => Run("status");

    /// <summary>
    ///     Phase-1 spike: proves Roslyn in the isolated load context, a modeless WPF window, and the two ways
    ///     of reaching AutoCAD's main thread from a background thread. Writes a report under
    ///     %LocalAppData%\HPAutoCad\McpBridge\logs. It draws into the active drawing, so the bridge refuses
    ///     it unless HPAUTOCAD_MCP_SPIKE=1 is set in acad.exe's environment. Removed once the bridge
    ///     runtime (phase 2) lands.
    /// </summary>
    [CommandMethod("HPMCPSPIKE")]
    public static void RunSpike() => Run("spike", false);

    /// <summary>Same spike, then discards the drawing and quits — for an unattended `acad.exe /b` run. Same gate.</summary>
    [CommandMethod("HPMCPSPIKEQUIT")]
    public static void RunSpikeAndQuit() => Run("spike", true);

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
