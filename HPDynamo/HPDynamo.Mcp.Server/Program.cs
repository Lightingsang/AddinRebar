using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Registry;
using Microsoft.Extensions.Hosting;

var builder = McpServerHost.CreateBuilder(args, DynamoHostProfile.Instance);

// Enforce strict 24-tool surface: suppress inspect_type from ToolCollection & DI
DynamoHostProfile.EnforceToolSurface(builder);

if (args.Length > 0 && string.Equals(args[0], McpServerHost.RegistryCommand, StringComparison.OrdinalIgnoreCase))
{
    using var cliHost = builder.Build();
    return await RegistryCli.RunAsync(cliHost.Services, args[1..]).ConfigureAwait(false);
}

await builder.Build().RunAsync().ConfigureAwait(false);
return 0;
