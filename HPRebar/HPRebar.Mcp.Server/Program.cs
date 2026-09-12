using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

// The host AI launches this process with an arbitrary working directory, so anchor configuration
// (appsettings.json) to the executable's folder rather than the current directory.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// stdout carries the JSON-RPC stream and nothing else: every log line must go to stderr,
// otherwise the MCP client sees a corrupt frame and drops the connection.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Configuration.AddEnvironmentVariables("HPREBAR_MCP_");

builder.Services
    .AddOptions<BridgeOptions>()
    .Bind(builder.Configuration.GetSection(BridgeOptions.SectionName))
    .Validate(options => options.IsValid(), "Bridge options out of range; see BridgeOptions.IsValid")
    .ValidateOnStart();

builder.Services
    .AddOptions<RegistryOptions>()
    .Bind(builder.Configuration.GetSection(RegistryOptions.SectionName))
    .Validate(options => options.IsValid(), "Registry options out of range; see RegistryOptions.IsValid")
    .ValidateOnStart();

// One pipe connection for the whole process; the SDK creates a DI scope per tool call, so the tools
// themselves are transient and only borrow these two.
builder.Services.AddSingleton<IRevitBridgeClient, RevitBridgeClient>();
builder.Services.AddSingleton<ResultFormatter>();

// Tool memory: files are the truth, SQLite indexes them, the manager runs them, the registrar exposes
// published ones as MCP tools at runtime. The hosted service goes in before the MCP transport so the
// library is loaded when the first tools/list arrives.
builder.Services.AddSingleton(sp => new ToolLibraryStore(sp.GetRequiredService<IOptions<RegistryOptions>>().Value.LibraryPath, sp.GetRequiredService<ILogger<ToolLibraryStore>>()));
builder.Services.AddSingleton(sp => new ToolRegistryDb(sp.GetRequiredService<IOptions<RegistryOptions>>().Value.DbPath, sp.GetRequiredService<ILogger<ToolRegistryDb>>()));
builder.Services.AddSingleton<ToolManager>();
builder.Services.AddSingleton<ToolLifecycleService>();
builder.Services.AddSingleton<DynamicToolRegistrar>();
builder.Services.AddHostedService<RegistryStartup>();

builder.Services
    .AddMcpServer(options =>
    {
        // Registry tools come and go while the server runs; advertise list_changed so clients refresh.
        options.ToolCollection ??= [];
        options.Capabilities ??= new ServerCapabilities();
        options.Capabilities.Tools ??= new ToolsCapability();
        options.Capabilities.Tools.ListChanged = true;
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

// `HPRebar.Mcp.Server.exe registry <command>`: the human side of the registry (approve, reject, list…).
// Same configuration and services as the server, but the MCP transport is never started.
if (args.Length > 0 && string.Equals(args[0], "registry", StringComparison.OrdinalIgnoreCase))
{
    using var cliHost = builder.Build();
    return await RegistryCli.RunAsync(cliHost.Services, args[1..]);
}

await builder.Build().RunAsync();
return 0;
