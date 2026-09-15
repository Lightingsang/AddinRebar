using HPNavis.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;

// The Navisworks MCP server: everything host-neutral (bootstrap, pipe client, registry engine, meta tools)
// lives in HPRebar.Mcp.Server.Core; this exe contributes the Navisworks profile, its core tool/prompt/
// resource classes and the embedded seed library. `HPNavis.Mcp.Server.exe registry <command>` runs the
// human side of the registry without starting the MCP transport.
return await McpServerHost.RunAsync(args, NavisHostProfile.Instance);
