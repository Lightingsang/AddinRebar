using HPEtabs.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;

// The ETABS MCP server: everything host-neutral (bootstrap, pipe client, registry engine, meta tools)
// lives in HPRebar.Mcp.Server.Core; this exe contributes the ETABS profile, its core tool/prompt/
// resource classes and the embedded seed library. `HPEtabs.Mcp.Server.exe registry <command>` runs the
// human side of the registry without starting the MCP transport.
return await McpServerHost.RunAsync(args, EtabsHostProfile.Instance);
