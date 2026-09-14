using HPAutoCad.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;

// The AutoCAD MCP server: everything host-neutral (bootstrap, pipe client, registry engine, meta tools)
// lives in HPRebar.Mcp.Server.Core; this exe contributes the AutoCAD profile, its four core tool/prompt/
// resource classes and the embedded seed library. `HPAutoCad.Mcp.Server.exe registry <command>` runs the
// human side of the registry without starting the MCP transport.
return await McpServerHost.RunAsync(args, AutocadHostProfile.Instance);
