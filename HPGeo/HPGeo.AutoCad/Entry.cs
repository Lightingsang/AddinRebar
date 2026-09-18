using System.Reflection;
using HPGeo.AutoCad.Commands;

namespace HPGeo.AutoCad;

/// <summary>
/// What the loader calls by reflection once it has loaded this assembly into its own load context:
/// <see cref="Start"/> returns the entry points behind the AutoCAD commands the loader registers. No AutoCAD
/// attribute lives here — command registration belongs to the loader, which AutoCAD loads directly.
/// </summary>
public static class Entry
{
    public static string Version { get; } =
        typeof(Entry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(Entry).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>
    /// Keys: <c>info</c> (HPGEOINFO), <c>kmz-script</c> (-HPGEOKMZ), <c>dialog</c> (HPGEO), <c>import</c> (HPGEOIMPORT),
    /// <c>import-script</c> (-HPGEOIMPORT), <c>stop</c> (Terminate).
    /// Every delegate is an <see cref="Action"/> that runs on AutoCAD's main thread inside the command context.
    /// </summary>
    public static IReadOnlyDictionary<string, Delegate> Start(string appDirectory, string product, string acadVersion)
    {
        HPGeoLog.Information($"HPGeo {Version} loaded in {product} {acadVersion} from {appDirectory}");
        return new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            ["info"] = new Action(HPGeoInfoCommand.Run),
            ["kmz-script"] = new Action(HPGeoKmzScriptCommand.Run),
            ["dialog"] = new Action(HPGeoDialogCommand.Run),
            ["import"] = new Action(HPGeoImportCommand.Run),
            ["import-script"] = new Action(HPGeoImportScriptCommand.Run),
            ["stop"] = new Action(Stop),
        };
    }

    private static void Stop() => HPGeoLog.Information($"HPGeo {Version} unloaded");
}
