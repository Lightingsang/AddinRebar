using System;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;

namespace HPAutoCad.Loader;

/// <summary>
/// AutoCAD commands registered in the Default ALC. Each command forwards execution to the
/// appropriate delegate in HPAutoCad.dll loaded inside AppLoadContext.
/// </summary>
public sealed class HPGeoCommands
{
    [CommandMethod("HPGEO", CommandFlags.Modal)]
    public void Dialog() => Invoke("dialog", "HPGEO");

    [CommandMethod("HPGEODIALOG", CommandFlags.Modal)]
    public void DialogAlias() => Invoke("dialog", "HPGEODIALOG");

    [CommandMethod("-HPGEOKMZ", CommandFlags.Modal)]
    public void KmzScript() => Invoke("kmz-script", "-HPGEOKMZ");

    [CommandMethod("HPGEOKMZ", CommandFlags.Modal)]
    public void KmzScriptAlias() => Invoke("kmz-script", "HPGEOKMZ");

    [CommandMethod("HPGEOIMPORT", CommandFlags.Modal)]
    public void Import() => Invoke("import", "HPGEOIMPORT");

    [CommandMethod("-HPGEOIMPORT", CommandFlags.Modal)]
    public void ImportScript() => Invoke("import-script", "-HPGEOIMPORT");

    [CommandMethod("-HPGEOIMAGE", CommandFlags.Modal)]
    public void ImageScript() => Invoke("image-script", "-HPGEOIMAGE");

    [CommandMethod("HPGEOINFO", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
    public void Info() => Invoke("info", "HPGEOINFO");

    /// <summary>
    /// Invokes one add-in entry point delegate.
    /// If commandName is null, console messaging is suppressed (used for background tasks like Terminate).
    /// </summary>
    internal static void Invoke(string key, string? commandName)
    {
        var app = HPAutoCadLoaderApplication.App;
        var ed = Application.DocumentManager.MdiActiveDocument?.Editor;

        if (app is null)
        {
            var reason = HPAutoCadLoaderApplication.StartupError ?? "not started";
            if (commandName is not null)
            {
                ed?.WriteMessage($"\n{commandName}: HPAutoCad did not start ({reason}). See {LoaderLog.LogDirectory}\\loader.log\n");
            }
            return;
        }

        if (!app.TryGetValue(key, out var action))
        {
            if (commandName is not null)
            {
                ed?.WriteMessage($"\n{commandName}: entry point '{key}' missing in the add-in.\n");
            }
            return;
        }

        try
        {
            action.DynamicInvoke();
        }
        catch (System.Exception exception)
        {
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            LoaderLog.Write($"{commandName ?? key} failed", cause);
            if (commandName is not null)
            {
                ed?.WriteMessage($"\n{commandName} failed: {cause.Message}\n");
            }
        }
    }
}
