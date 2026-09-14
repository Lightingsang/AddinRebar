using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The validator takes categories, reserved names and the host from the profile of the exe that runs
///     it: a Revit category is an error for the AutoCAD server, a record written for another host is
///     refused, and the four-argument overload keeps behaving exactly as the Revit server always did.
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
}
