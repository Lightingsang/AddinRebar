using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts.Revit;

// The Revit MCP server: everything host-neutral (bootstrap, pipe client, registry engine, meta tools)
// lives in HPRebar.Mcp.Server.Core; this exe contributes the Revit profile, its four core tool/prompt/
// resource classes and the embedded seed library. `HPRebar.Mcp.Server.exe registry <command>` runs the
// human side of the registry without starting the MCP transport.
return await McpServerHost.RunAsync(args, RevitHostProfile.Instance);
