using HPSap2000.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;

// The SAP2000 MCP server: everything host-neutral (bootstrap, pipe client, registry engine, meta tools)
// lives in HPRebar.Mcp.Server.Core; this exe contributes the SAP2000 profile, its core tool/prompt/
// resource classes and the embedded seed library. `HPSap2000.Mcp.Server.exe registry <command>` runs the
// human side of the registry without starting the MCP transport.
return await McpServerHost.RunAsync(args, Sap2000HostProfile.Instance);
