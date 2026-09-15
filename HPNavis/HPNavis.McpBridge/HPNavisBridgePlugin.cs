using System.IO;
using Autodesk.Navisworks.Api.Plugins;

namespace HPNavis.McpBridge;

/// <summary>
///     The entry point Navisworks creates for us. An <see cref="EventWatcherPlugin"/> is the one plugin
///     kind that is not delay-loaded, so the pipe listener can come up without a click — the same role
///     the extension application plays in AutoCAD. Everything real happens in <see cref="BridgeEntry"/>;
///     this class only wires the two lifecycle calls and, first of all, the assembly resolver, which must
///     be in place before any Roslyn type is touched (the static constructor runs before Navisworks
///     calls <see cref="OnLoaded"/>).
/// </summary>
[Plugin("HPNavis.McpBridge", "HPNV", DisplayName = "HPNavis MCP", ToolTip = "MCP bridge: lets an AI agent run reviewed C# against this Navisworks session")]
public sealed class HPNavisBridgePlugin : EventWatcherPlugin
{
    static HPNavisBridgePlugin()
    {
        PluginAssemblyResolver.Install(Path.GetDirectoryName(typeof(HPNavisBridgePlugin).Assembly.Location)!);
    }

    // Roamer calls these directly: an exception that escapes here has no handler of ours above it.
    public override void OnLoaded()
    {
        try { BridgeEntry.Start(PluginAssemblyResolver.Folder); }
        catch (Exception exception) { BridgeEntry.ReportStartupFailure(exception); }
    }

    public override void OnUnloading()
    {
        try { BridgeEntry.Dispose(); }
        catch (Exception exception) { Serilog.Log.Error(exception, "HPNavis MCP bridge failed to shut down cleanly"); }
    }
}
