namespace HPAutoCad;

using System;
using System.Collections.Generic;
using System.Reflection;
using HPAutoCad.HPGeoLink.Commands;
using HPAutoCad.HPGeoLink.Support;
using HPAutoCad.SmartPlot.Commands;

public static class Entry
{
    public static string Version { get; } =
        typeof(Entry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(Entry).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";

    /// <summary>
    /// Loader reflection entry point called by HPAutoCad.Loader (AppLoadContext).
    /// Returns dictionary of delegates invoked by AutoCAD commands registered in the loader.
    /// </summary>
    public static IReadOnlyDictionary<string, Delegate> Start(string appDirectory, Action<string>? log = null)
    {
        log?.Invoke($"HPAutoCad {Version} starting from {appDirectory}");
        HPGeoLog.Information($"HPAutoCad {Version} loaded from {appDirectory}");

        return new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            ["dialog"] = new Action(HPGeoDialogCommand.Run),
            ["kmz-script"] = new Action(HPGeoKmzScriptCommand.Run),
            ["import"] = new Action(HPGeoImportCommand.Run),
            ["import-script"] = new Action(HPGeoImportScriptCommand.Run),
            ["image-script"] = new Action(HPGeoImageScriptCommand.Run),
            ["info"] = new Action(HPGeoInfoCommand.Run),
            ["smartplot"] = new Action(SmartPlotCommand.Run),
            ["stop"] = new Action(Stop),
        };
    }

    /// <summary>Backward-compatible 3-argument overload for legacy loaders.</summary>
    public static IReadOnlyDictionary<string, Delegate> Start(string appDirectory, string product, string acadVersion)
    {
        HPGeoLog.Information($"HPAutoCad {Version} loaded in {product} {acadVersion} from {appDirectory}");
        return Start(appDirectory, null);
    }

    private static void Stop() => HPGeoLog.Information($"HPAutoCad {Version} unloaded");
}
