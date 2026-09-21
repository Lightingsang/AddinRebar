using System;
using System.Collections.Generic;
using System.Reflection;
using Tekla.Structures.Plugins;

namespace HPTekla.McpBridge;

/// <summary>
///     Tekla Structures standard plugin entry point.
///     Enables activation via Ribbon buttons, Quick Launch (Ctrl+Q), catalog, or macros.
///     When invoked, ensures the MCP bridge pipe listener is started and displays the modeless status window.
/// </summary>
[Plugin("HPTeklaBridge")]
[PluginUserInterface("HPTekla.McpBridge.NullForm")]
public sealed class HPTeklaBridgePlugin : PluginBase
{
    static HPTeklaBridgePlugin()
    {
        var pluginFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(pluginFolder))
        {
            PluginAssemblyResolver.Install(pluginFolder);
        }
    }

    public override bool Run(List<InputDefinition> input)
    {
        BridgeEntry.ShowStatusWindow();
        return true;
    }

    public override List<InputDefinition> DefineInput()
    {
        // No interactive model picking required to launch the AI bridge
        return new List<InputDefinition>();
    }
}
