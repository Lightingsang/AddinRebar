using Autodesk.Navisworks.Api.Plugins;

namespace HPNavis.McpBridge;

/// <summary>
///     Ribbon tab "HPNavis" ▸ panel "MCP" ▸ button "MCP Bridge" — the same one-button surface the Revit MCP
///     bridge puts on its ribbon: it opens (or activates) the status window where the user starts the listener
///     and ticks the per-session opt-ins. Layout and strings come from <c>en-US\HPNavisRibbon.xaml</c> /
///     <c>.name</c> beside the DLL, the icons from <c>Images\</c> (rendered by
///     <c>tools/icons/render-ribbon-icons.ps1</c>). Navisworks creates this instance on the first click; the
///     assembly itself is already loaded at start-up through <see cref="HPNavisBridgePlugin"/>.
/// </summary>
[Plugin(PluginName, "HPNV", DisplayName = "HPNavis MCP Ribbon", ToolTip = "Ribbon tab of the HPNavis MCP bridge")]
[Strings("HPNavisRibbon.name")]
[RibbonLayout("HPNavisRibbon.xaml")]
[RibbonTab(TabId, DisplayName = "HPNavis")]
[Command(McpBridgeCommandId, DisplayName = "MCP Bridge",
    Icon = "McpBridge_16.png", LargeIcon = "McpBridge_32.png",
    ToolTip = "Open the MCP bridge window",
    ExtendedToolTip = "Start or stop the listener and allow AI code execution for this Navisworks session. Works with or without a model open.",
    // Explicit: the button stays usable on a clear document too (Navisworks itself greys every tab while its start page shows).
    CallCanExecute = CallCanExecute.Always)]
public sealed class HPNavisRibbonPlugin : CommandHandlerPlugin
{
    public const string PluginName = "HPNavis.McpBridge.Ribbon";
    public const string TabId = "ID_HPNAVIS";
    public const string McpBridgeCommandId = "ID_HPNAVIS_MCP_BRIDGE";

    public override CommandState CanExecuteCommand(string name) => new CommandState(true);

    public override int ExecuteCommand(string name, params string[] parameters)
    {
        if (name != McpBridgeCommandId) return 1;

        try
        {
            BridgeEntry.ShowWindow();
            return 0;
        }
        catch (Exception exception)
        {
            // A silent no-op click would look like a dead button: say why (typically the bridge failed to start — see the log).
            Serilog.Log.Error(exception, "HPNavis MCP bridge: the status window could not be opened from the Ribbon");
            System.Windows.MessageBox.Show(exception.Message, "HPNavis MCP Bridge", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return 1;
        }
    }
}
