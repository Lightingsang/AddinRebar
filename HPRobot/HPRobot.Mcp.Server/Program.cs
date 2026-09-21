using HPRobot.Mcp.Server.Hosts.Robot;
using HPRebar.Mcp.Server.Bootstrap;

// The Robot Structural Analysis Professional 2026 MCP server:
// host-neutral bootstrap, pipe client, registry engine, and meta tools live in HPRebar.Mcp.Server.Core.
// This exe supplies the RobotHostProfile, core tools, prompts, resources, and 12 embedded seeds.
return await McpServerHost.RunAsync(args, new RobotHostProfile());
