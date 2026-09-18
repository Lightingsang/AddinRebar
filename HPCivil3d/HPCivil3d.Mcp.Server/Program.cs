using HPCivil3d.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;

// The Civil 3D MCP server: everything host-neutral (bootstrap, pipe client, registry engine, meta tools)
// lives in HPRebar.Mcp.Server.Core; this exe contributes the Civil 3D profile, its four core tool/prompt/
// resource classes and the embedded seed library. `HPCivil3d.Mcp.Server.exe registry <command>` runs the
// human side of the registry without starting the MCP transport.
return await McpServerHost.RunAsync(args, Civil3dHostProfile.Instance);
