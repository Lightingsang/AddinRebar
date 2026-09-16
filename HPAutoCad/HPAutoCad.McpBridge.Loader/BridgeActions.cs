using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices.Core;

namespace HPAutoCad.McpBridge.Loader;

/// <summary>
///     One place that runs a bridge entry point for a user gesture — a command on the command line or the
///     button on the Ribbon — and reports the outcome where the user is looking: the command line when a
///     drawing is open, an alert when none is (Session commands and Ribbon clicks both happen without one).
///     Nothing here touches a drawing, so it needs no document lock and is safe while a command is running.
/// </summary>
internal static class BridgeActions
{
    public const string Prefix = "[HPAutoCad MCP]";

    /// <summary>True once the bridge started; the Ribbon disables its button otherwise.</summary>
    public static bool BridgeAvailable => BridgeLoaderApplication.Bridge is not null;

    /// <summary>Runs an entry point and shows its message (if any). Never throws: failures go to the log and the user.</summary>
    public static void Run(string key, params object?[] args)
    {
        try
        {
            var result = BridgeLoaderApplication.Invoke(key, args);
            if (result is string text && text.Length > 0) Report(text, alertWithoutDocument: key == "status");
        }
        catch (System.Exception exception)
        {
            var message = exception is TargetInvocationException { InnerException: { } inner } ? inner.Message : exception.Message;
            Report($"{Prefix} {key} failed: {message}", alertWithoutDocument: true);
            LoaderLog.Write($"action {key} failed", exception);
        }
    }

    private static void Report(string text, bool alertWithoutDocument)
    {
        var editor = Application.DocumentManager.MdiActiveDocument?.Editor;
        if (editor is not null) editor.WriteMessage("\n" + text + "\n");
        else if (alertWithoutDocument) Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(text);
        else LoaderLog.Write(text);
    }
}
