using Autodesk.Navisworks.Api.Plugins;

namespace HPNavis.McpBridge;

/// <summary>
///     Opens (or activates) the status window programmatically:
///     <c>Application.Plugins.ExecuteAddInPlugin("HPNavis.McpBridge.Window.HPNV")</c>. Hidden from the
///     Add-ins menu (<see cref="AddInLocation.None"/>) since the Ribbon button of
///     <see cref="HPNavisRibbonPlugin"/> took over that role — one visible entry point, not two.
/// </summary>
[Plugin("HPNavis.McpBridge.Window", "HPNV", DisplayName = "HPNavis MCP", ToolTip = "Open the HPNavis MCP bridge window")]
[AddInPlugin(AddInLocation.None)]
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
