using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Bootstrap;

/// <summary>
///     What every host exe's Program.cs does, parameterised by its <see cref="IHostProfile"/>: stderr-only
///     logging, configuration anchored to the exe folder, options validated against the profile, the
///     bridge client, the tool registry, and an MCP server whose tools come from two assemblies — the
///     engine (registry meta tools, inspect, cancel) and the host exe (execute, context, resources, prompts).
/// </summary>
public static class McpServerHost
{
    public const string RegistryCommand = "registry";

    public static HostApplicationBuilder CreateBuilder(string[] args, IHostProfile profile)
    {
        var engine = typeof(McpServerHost).Assembly;
        if (profile.HostAssembly == engine)
            throw new ArgumentException("profile.HostAssembly must be the host exe (it holds the host's tools and seed library), not the engine assembly.", nameof(profile));

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
        builder.Configuration.AddEnvironmentVariables(profile.EnvPrefix);

        ConfigureOptions(builder.Services, builder.Configuration, profile);

        // One pipe connection for the whole process; the SDK creates a DI scope per tool call, so the tools
        // themselves are transient and only borrow these.
        builder.Services.AddSingleton<IRevitBridgeClient, RevitBridgeClient>();
        builder.Services.AddSingleton<ResultFormatter>();
        builder.Services.AddSingleton<ExecuteCodeService>();
        builder.Services.AddSingleton<ContextService>();

        // Tool memory: files are the truth, SQLite indexes them, the manager runs them, the registrar exposes
        // published ones as MCP tools at runtime. The hosted service goes in before the MCP transport so the
        // library is loaded when the first tools/list arrives.
        builder.Services.AddSingleton(sp => new ToolLibraryStore(sp.GetRequiredService<IOptions<RegistryOptions>>().Value.LibraryPath, sp.GetRequiredService<ILogger<ToolLibraryStore>>()));
        builder.Services.AddSingleton(sp => new ToolRegistryDb(sp.GetRequiredService<IOptions<RegistryOptions>>().Value.DbPath, sp.GetRequiredService<ILogger<ToolRegistryDb>>()));
        builder.Services.AddSingleton<ToolManager>();
        builder.Services.AddSingleton<ToolLifecycleService>();
        builder.Services.AddSingleton<DynamicToolRegistrar>();
        builder.Services.AddHostedService<RegistryStartup>();

        var version = engine.GetName().Version?.ToString(3) ?? "0.0.0";

        builder.Services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation { Name = profile.ServerName, Version = version };
                // Registry tools come and go while the server runs; advertise list_changed so clients refresh.
                options.ToolCollection ??= [];
                options.Capabilities ??= new ServerCapabilities();
                options.Capabilities.Tools ??= new ToolsCapability();
                options.Capabilities.Tools.ListChanged = true;
            })
            .WithStdioServerTransport()
            .WithToolsFromAssembly(engine)
            .WithToolsFromAssembly(profile.HostAssembly)
            .WithResourcesFromAssembly(engine)
            .WithResourcesFromAssembly(profile.HostAssembly)
            .WithPromptsFromAssembly(engine)
            .WithPromptsFromAssembly(profile.HostAssembly);

        return builder;
    }

    /// <summary>
    ///     Registers <see cref="BridgeOptions"/> and <see cref="RegistryOptions"/> with the profile applied
    ///     both <em>before</em> binding and after it. Before, because the configuration binder reads every
    ///     property's current value and writes it back through the setter — a pipe name or library path
    ///     computed while the profile was still the Revit default would be pinned. After, so that no
    ///     configuration key can point this exe at another host's pipe or registry.
    /// </summary>
    public static void ConfigureOptions(IServiceCollection services, IConfiguration configuration, IHostProfile profile)
    {
        services.AddSingleton(profile);

        services
            .AddOptions<BridgeOptions>()
            .Configure(options => options.HostId = profile.HostId)
            .Bind(configuration.GetSection(BridgeOptions.SectionName))
            .PostConfigure(options => options.HostId = profile.HostId)
            .Validate(options => options.IsValid(profile.ValidVersions), "Bridge options out of range; see BridgeOptions.IsValid")
            .ValidateOnStart();

        services
            .AddOptions<RegistryOptions>()
            .Configure(options => options.ProductFolder = profile.ProductFolder)
            .Bind(configuration.GetSection(RegistryOptions.SectionName))
            .PostConfigure(options => options.ProductFolder = profile.ProductFolder)
            .Validate(options => options.IsValid(), "Registry options out of range; see RegistryOptions.IsValid")
            .ValidateOnStart();
    }

    /// <summary>
    ///     `{exe} registry <command>` runs the human side of the registry (approve, reject, list…) with the
    ///     same configuration and services, never starting the MCP transport; anything else serves MCP over stdio.
    /// </summary>
    public static async Task<int> RunAsync(string[] args, IHostProfile profile)
    {
        var builder = CreateBuilder(args, profile);

        if (args.Length > 0 && string.Equals(args[0], RegistryCommand, StringComparison.OrdinalIgnoreCase))
        {
            using var cliHost = builder.Build();
            return await RegistryCli.RunAsync(cliHost.Services, args[1..]).ConfigureAwait(false);
        }

        await builder.Build().RunAsync().ConfigureAwait(false);
        return 0;
    }
}
