using Autodesk.Navisworks.Api.Plugins;

namespace HPNavis.McpBridge;

/// <summary>
///     The one button the MVP puts in Navisworks: Add-ins ▸ "HPNavis MCP" opens (or activates) the status
///     window where the user starts the listener and ticks the per-session opt-ins. An
///     <see cref="AddInPlugin"/> costs no XAML, no strings file and no icons — the Ribbon tab is a later
///     step, as it was for AutoCAD.
/// </summary>
[Plugin("HPNavis.McpBridge.Window", "HPNV", DisplayName = "HPNavis MCP", ToolTip = "Open the HPNavis MCP bridge window")]
[AddInPlugin(AddInLocation.AddIn)]
public sealed class HPNavisWindowPlugin : AddInPlugin
{
    public override int Execute(params string[] parameters)
    {
        try
        {
            BridgeEntry.ShowWindow();
            return 0;
        }
        catch (Exception exception)
        {
            Serilog.Log.Error(exception, "HPNavis MCP bridge: the status window could not be opened");
            return 1;
        }
    }
}
