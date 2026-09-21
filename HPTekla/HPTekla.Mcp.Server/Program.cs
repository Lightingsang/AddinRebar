using HPTekla.Mcp.Server.Hosts.Tekla;
using HPRebar.Mcp.Server.Bootstrap;

// The Tekla Structures 2025 MCP server:
// host-neutral bootstrap, pipe client, registry engine, and meta tools live in HPRebar.Mcp.Server.Core.
// This exe supplies TeklaHostProfile, core tools, prompts, resources, and 12 embedded seeds.
return await McpServerHost.RunAsync(args, TeklaHostProfile.Instance);
