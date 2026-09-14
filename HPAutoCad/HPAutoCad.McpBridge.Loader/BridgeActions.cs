using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices.Core;

namespace HPAutoCad.McpBridge.Loader;

/// <summary>
///     One place that runs a bridge entry point for a user gesture — a command on the command line or a
///     button on the Ribbon — and reports the outcome where the user is looking: the command line when a
///     drawing is open, an alert when none is (Session commands and Ribbon clicks both happen without one).
///     Nothing here touches a drawing, so it needs no document lock and is safe while a command is running.
/// </summary>
internal static class BridgeActions
{
    public const string Prefix = "[HPAutoCad MCP]";

    /// <summary>True once the bridge started; the Ribbon disables bridge-backed buttons otherwise.</summary>
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

    /// <summary>Runs an entry point that returns a value; null (plus a log line) when the bridge refuses.</summary>
    public static T? Query<T>(string key, params object?[] args)
    {
        try
        {
            return BridgeLoaderApplication.Invoke(key, args) is T value ? value : default;
        }
        catch (System.Exception exception)
        {
            LoaderLog.Write($"query {key} failed", exception);
            return default;
        }
    }

    /// <summary>Opens a folder or file with the shell; a missing path is reported, not created.</summary>
    public static void OpenPath(string? path, string what)
    {
        if (path is null || path.Length == 0 || !(Directory.Exists(path) || File.Exists(path)))
        {
            Report($"{Prefix} {what} chưa tồn tại: {path ?? "(không rõ)"}", alertWithoutDocument: true);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (System.Exception exception)
        {
            Report($"{Prefix} không mở được {what}: {exception.Message}", alertWithoutDocument: true);
            LoaderLog.Write($"open {what} failed", exception);
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
