using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;

namespace HPGeo.AutoCad.Loader;

/// <summary>
/// The commands AutoCAD registers, each forwarding to the add-in's delegate of the same key. When the add-in
/// failed to start, every command says why and where the log is instead of failing silently.
/// </summary>
public sealed class HPGeoCommands
{
    [CommandMethod("HPGEOINFO", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
    public void Info() => Invoke("info", "HPGEOINFO");

    // Every command but HPGEOINFO may write the drawing (the exports store the zone in the named object dictionary,
    // the imports draw entities), so each keeps its own undo marker: one U removes exactly what one command did.
    [CommandMethod("-HPGEOKMZ", CommandFlags.Modal)]
    public void KmzScript() => Invoke("kmz-script", "-HPGEOKMZ");

    [CommandMethod("HPGEO", CommandFlags.Modal)]
    public void Dialog() => Invoke("dialog", "HPGEO");

    [CommandMethod("HPGEOIMPORT", CommandFlags.Modal)]
    public void Import() => Invoke("import", "HPGEOIMPORT");

    [CommandMethod("-HPGEOIMPORT", CommandFlags.Modal)]
    public void ImportScript() => Invoke("import-script", "-HPGEOIMPORT");

    [CommandMethod("-HPGEOIMAGE", CommandFlags.Modal)]
    public void ImageScript() => Invoke("image-script", "-HPGEOIMAGE");

    /// <summary>Runs one add-in entry point; <paramref name="commandName"/> null = no console output (Terminate).</summary>
    internal static void Invoke(string key, string? commandName)
    {
        var app = HPGeoLoaderApplication.App;
        var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
        if (app is null)
        {
            var reason = HPGeoLoaderApplication.StartupError ?? "not started";
            if (commandName is not null)
                ed?.WriteMessage($"\n{commandName}: HPGeo did not start ({reason}). See {LoaderLog.LogDirectory}\\loader.log\n");
            return;
        }
        if (!app.TryGetValue(key, out var action))
        {
            if (commandName is not null) ed?.WriteMessage($"\n{commandName}: entry point '{key}' missing in the add-in.\n");
            return;
        }
        try
        {
            action.DynamicInvoke();
        }
        catch (System.Exception exception)
        {
            var cause = exception is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner : exception;
            LoaderLog.Write($"{commandName ?? key} failed", cause);
            if (commandName is not null) ed?.WriteMessage($"\n{commandName} failed: {cause.Message}\n");
        }
    }
}
