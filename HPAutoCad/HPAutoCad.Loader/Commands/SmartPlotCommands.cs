using Autodesk.AutoCAD.Runtime;

namespace HPAutoCad.Loader.Commands;

/// <summary>
/// AutoCAD commands registered in the Default ALC for Smart Plot Pro.
/// Delegates execution to the "smartplot" entry point in HPAutoCad.dll inside AppLoadContext.
/// </summary>
public sealed class SmartPlotCommands
{
    [CommandMethod("HPSMARTPLOT", CommandFlags.Modal | CommandFlags.Session)]
    public void SmartPlot() => HPGeoCommands.Invoke("smartplot", "HPSMARTPLOT");

    [CommandMethod("HPLOT", CommandFlags.Modal | CommandFlags.Session)]
    public void SmartPlotAlias() => HPGeoCommands.Invoke("smartplot", "HPLOT");
}
