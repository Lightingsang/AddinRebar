using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The registry takes categories, reserved names, the host and the exe name from the profile of the exe
///     that runs it: a Revit category is an error for the AutoCAD server, a record written for another host is
///     refused (validator and CLI import alike), a proposal is stamped with this host and reviewed with this
///     exe's approve command, and the four-argument overload keeps behaving exactly as the Revit server always did.
/// </summary>
public sealed class RegistryProfileTests
{
    private static readonly HostProfile Autocad = new HostProfile
    {
        HostId = PipeNaming.AutocadHost, DisplayName = "AutoCAD", ServerName = "test", ProductFolder = "HPAutoCadTest", EnvPrefix = "X_",
        DefaultVersion = 2026, ValidVersions = new[] { 2026 }, MethodPrefix = JsonRpcMethods.AutocadPrefix,
        ExecuteToolName = "execute_autocad_code", ContextToolName = "get_autocad_context", ResourceScheme = "autocad",
        Categories = new[] { "Drawing", "Layer", "Block", "Annotation", "Layout", "Data", "Generic" },
        CoreToolNames = new[] { "execute_autocad_code", "get_autocad_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.AutocadImports, ScriptContractSummary = "test", HostAssembly = typeof(RegistryProfileTests).Assembly,
        CliExecutable = "HPAutoCadTest.exe",
    };

    private static ToolRecord Candidate(string name = "list_layers", string category = "Layer", string? host = "autocad") => new ToolRecord
    {
        Name = name,
        Title = "List layers",
        Description = "Lists every layer of the drawing with its colour and state.",
        Category = category,
        Transaction = "none",
        TimeoutSeconds = 30,
        Host = host,
        Code = "return args.Bool(\"includeCounts\", false);",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{"includeCounts":{"type":"boolean"}}}"""),
        Examples =
        [
            new ToolExample { Title = "all", Args = JsonSerializer.Deserialize<JsonElement>("{}") },
            new ToolExample { Title = "counts", Args = JsonSerializer.Deserialize<JsonElement>("""{"includeCounts":true}""") },
        ],
    };

    [Fact]
    public void Autocad_profile_accepts_its_own_categories_and_host()
    {
        var report = ToolValidator.Validate(Candidate(), null, [], false, Autocad);

        Assert.True(report.IsValid, string.Join("; ", report.Errors));
    }

    [Fact]
    public void Autocad_profile_rejects_a_revit_category_a_foreign_host_and_its_own_core_tool_names()
    {
        Assert.Contains(ToolValidator.Validate(Candidate(category: "Architecture"), null, [], false, Autocad).Errors, e => e.Contains("Drawing, Layer, Block"));
        Assert.Contains(ToolValidator.Validate(Candidate(host: "revit"), null, [], false, Autocad).Errors, e => e.Contains("host must be 'autocad'"));
        Assert.Contains(ToolValidator.Validate(Candidate(name: "execute_autocad_code"), null, [], false, Autocad).Errors, e => e.Contains("reserved"));
        Assert.Contains(ToolValidator.Validate(Candidate(name: "search_tools"), null, [], false, Autocad).Errors, e => e.Contains("reserved"));
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(name: "execute_revit_code"), null, [], false, Autocad).Errors, e => e.Contains("reserved"));
    }

    [Fact]
    public void A_record_without_a_host_is_accepted_and_the_revit_overload_keeps_the_revit_rules()
    {
        Assert.True(ToolValidator.Validate(Candidate(host: null), null, [], false, Autocad).IsValid);

        var revit = ToolValidator.Validate(Candidate(name: "count_walls", category: "Architecture", host: "revit"), null, [], false);
        Assert.True(revit.IsValid, string.Join("; ", revit.Errors));
        Assert.Contains(ToolValidator.Validate(Candidate(name: "execute_revit_code", category: "Architecture", host: "revit"), null, [], false).Errors, e => e.Contains("reserved"));
        Assert.Contains(ToolValidator.Validate(Candidate(name: "x_layers", category: "Layer", host: "revit"), null, [], false).Errors, e => e.Contains("Architecture, Structure"));
    }

    [Fact]
    public void The_exe_name_defaults_to_the_host_assembly_and_the_revit_profile_keeps_its_historical_name()
    {
        Assert.Equal("HPRebar.Mcp.Server.exe", HostProfile.Revit.CliExecutable);
        Assert.Equal("HPRebar.Mcp.Server.exe", HostProfile.Revit.WithHostAssembly(typeof(RegistryProfileTests).Assembly).CliExecutable);
        var derived = new HostProfile
        {
            HostId = "x", DisplayName = "X", ServerName = "x", ProductFolder = "X", EnvPrefix = "X_", DefaultVersion = 1, ValidVersions = new[] { 1 },
            MethodPrefix = "x.", ExecuteToolName = "e", ContextToolName = "c", ResourceScheme = "x", Categories = new[] { "Generic" }, CoreToolNames = new[] { "e" },
            ScriptImports = new[] { "System" }, ScriptContractSummary = "x", HostAssembly = typeof(RegistryProfileTests).Assembly,
        };
        Assert.Equal(typeof(RegistryProfileTests).Assembly.GetName().Name + ".exe", derived.CliExecutable);
    }

    [Fact]
    public async Task A_proposal_on_the_autocad_profile_is_stamped_with_the_host_and_reviewed_with_this_exe()
    {
        await using var f = new AutocadRegistry();
        var lifecycle = new ToolLifecycleService(f.Manager, f.Client, NullLogger<ToolLifecycleService>.Instance);

        var input = new ProposeInput("count_layers", "Count layers", "Counts the layers of the drawing for the profile test", "layer", ["test"],
            JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{"includeCounts":{"type":"boolean"}}}"""),
            "return args.Bool(\"includeCounts\", false);",
            [new ToolExample { Title = "all", Args = JsonSerializer.Deserialize<JsonElement>("{}") }, new ToolExample { Title = "counts", Args = JsonSerializer.Deserialize<JsonElement>("""{"includeCounts":true}""") }],
            "none", 30, null, false);
        var proposed = await lifecycle.ProposeAsync(input, TestContext.Current.CancellationToken);

        Assert.True(proposed.Accepted, string.Join("; ", proposed.Report.Errors));
        Assert.Equal("autocad", proposed.Record!.Host);
        Assert.Equal("Layer", proposed.Record.Category);

        var review = File.ReadAllText(lifecycle.WriteReview(proposed.Record));
        Assert.Contains("**Host:** autocad", review);
        Assert.Contains("HPAutoCadTest.exe registry approve count_layers", review);
        Assert.DoesNotContain("HPRebar.Mcp.Server.exe", review);

        var rejected = await lifecycle.ProposeAsync(input with { Name = "count_walls", Category = "Architecture" }, TestContext.Current.CancellationToken);
        Assert.False(rejected.Accepted);
    }

    [Fact]
    public async Task The_cli_names_this_exe_and_import_skips_a_folder_written_for_another_host()
    {
        await using var f = new AutocadRegistry();
        var services = new ServiceCollection()
            .AddSingleton(f.Store).AddSingleton(f.Db).AddSingleton(f.Manager)
            .AddSingleton(new ToolLifecycleService(f.Manager, f.Client, NullLogger<ToolLifecycleService>.Instance))
            .BuildServiceProvider();

        var usage = new StringWriter();
        Assert.Equal(0, await RegistryCli.RunAsync(services, ["--help"], usage));
        Assert.StartsWith("HPAutoCadTest.exe registry", usage.ToString().TrimStart());

        var foreign = Path.Combine(f.Root, "foreign");
        using (var other = new ToolLibraryStore(foreign, NullLogger<ToolLibraryStore>.Instance))
        {
            other.Write(Candidate(name: "count_walls", category: "Architecture", host: "revit"));
            other.Write(Candidate(name: "count_layers", host: null));
        }

        // The store lays tools out as <Category>/<name>; `import` takes a folder of tool folders.
        var output = new StringWriter();
        Assert.Equal(0, await RegistryCli.RunAsync(services, ["import", Path.Combine(foreign, "Architecture")], output));
        Assert.Equal(0, await RegistryCli.RunAsync(services, ["import", Path.Combine(foreign, "Layer")], output));

        Assert.Contains("skipped", output.ToString());
        Assert.Contains("host 'revit' is not 'autocad'", output.ToString());
        Assert.False(f.Manager.TryGet("count_walls", out _));
        Assert.True(f.Manager.TryGet("count_layers", out var imported));
        Assert.Equal("autocad", imported.Host);
    }

    /// <summary>A registry on a temp root whose bridge client carries the AutoCAD profile, over a real pipe to the fake.</summary>
    private sealed class AutocadRegistry : IAsyncDisposable
    {
        public readonly string Root = Path.Combine(Path.GetTempPath(), "hpautocad-registry-" + Guid.NewGuid().ToString("N"));
        public readonly FakeRevitExecutor Executor = new();
        public readonly ToolLibraryStore Store;
        public readonly ToolRegistryDb Db;
        public readonly ToolManager Manager;
        public readonly RevitBridgeClient Client;
        private readonly PipeListener _listener;

        public AutocadRegistry()
        {
            var options = new RegistryOptions { LibraryPath = Path.Combine(Root, "lib"), DbPath = Path.Combine(Root, "registry.db"), WatchLibrary = false };
            var pipe = "hpautocad-mcp-test-" + Guid.NewGuid().ToString("N");
            _listener = new PipeListener(pipe, new RequestDispatcher(Executor, new BridgeSettings { ExecutionEnabled = true }, "2026"));
            _listener.Start();
            Client = new RevitBridgeClient(Options.Create(new BridgeOptions { PipeName = pipe, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 }), NullLogger<RevitBridgeClient>.Instance, Autocad);
            Store = new ToolLibraryStore(options.LibraryPath, NullLogger<ToolLibraryStore>.Instance);
            Store.EnsureRoot();
            Db = new ToolRegistryDb(options.DbPath, NullLogger<ToolRegistryDb>.Instance);
            Db.Initialize();
            Manager = new ToolManager(Options.Create(options), Options.Create(new BridgeOptions()), Store, Db, Client, NullLogger<ToolManager>.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await _listener.StopAsync();
            _listener.Dispose();
            Store.Dispose();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(Root, true); } catch (IOException) { /* best effort */ }
        }
    }
}
