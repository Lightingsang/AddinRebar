using HPPowerBi.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;

// The Power BI MCP server: everything host-neutral (bootstrap, pipe client, registry engine, meta tools)
// lives in HPRebar.Mcp.Server.Core; this exe contributes the Power BI profile, its core tool/prompt/
// resource classes and the embedded seed library. `HPPowerBi.Mcp.Server.exe registry <command>` runs the
// human side of the registry without starting the MCP transport.
return await McpServerHost.RunAsync(args, PowerBiHostProfile.Instance);
