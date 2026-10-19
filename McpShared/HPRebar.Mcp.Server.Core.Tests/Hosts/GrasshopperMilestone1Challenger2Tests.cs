using System.Text.Json;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using Xunit;
using static HPRebar.Mcp.Server.Tests.GrasshopperTestProfile;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Challenger 2 suite: Empirical verification of Grasshopper ContextResult wire serialization,
///     null suppression, multi-host isolation, ToolValidator timeout clamping, and CoreToolNames shadowing prevention.
/// </summary>
public sealed class GrasshopperMilestone1Challenger2Tests
{
    [Fact]
    public void ContextResult_serializes_GrasshopperInfo_in_camelCase_with_exact_field_names()
    {
        var info = new GrasshopperInfo(
            IsEditorLoaded: true,
            GrasshopperVersion: "1.0.0007",
            RhinoVersion: "8.12.24282.7001",
            ActiveDefinitionName: "ParametricTruss.gh",
            ActiveDefinitionPath: @"C:\GrasshopperScripts\ParametricTruss.gh",
            ComponentCount: 150,
            WireCount: 220,
            SelectedComponentCount: 8,
            HasUnsavedChanges: true,
            IsRhinoInsideRevitActive: true);

        var context = new ContextResult { Host = "grasshopper", HostVersion = "2026", Grasshopper = info };
        var json = BridgeJson.Serialize(context);
        using var doc = JsonDocument.Parse(json);
        var gh = doc.RootElement.GetProperty("grasshopper");

        Assert.True(gh.GetProperty("isEditorLoaded").GetBoolean());
        Assert.Equal("1.0.0007", gh.GetProperty("grasshopperVersion").GetString());
        Assert.Equal("8.12.24282.7001", gh.GetProperty("rhinoVersion").GetString());
        Assert.Equal("ParametricTruss.gh", gh.GetProperty("activeDefinitionName").GetString());
        Assert.Equal(@"C:\GrasshopperScripts\ParametricTruss.gh", gh.GetProperty("activeDefinitionPath").GetString());
        Assert.Equal(150, gh.GetProperty("componentCount").GetInt32());
        Assert.Equal(220, gh.GetProperty("wireCount").GetInt32());
        Assert.Equal(8, gh.GetProperty("selectedComponentCount").GetInt32());
        Assert.True(gh.GetProperty("hasUnsavedChanges").GetBoolean());
        Assert.True(gh.GetProperty("isRhinoInsideRevitActive").GetBoolean());
        Assert.Equal(10, gh.EnumerateObject().Count());
    }

    [Fact]
    public void ContextResult_omits_null_fields_and_round_trips_unicode()
    {
        var info = new GrasshopperInfo(false, null, null, "Dàn_Thép.gh", @"D:\DựÁn\Dàn_Thép.gh", 0, 0, 0, false, false);
        var context = new ContextResult { Host = "grasshopper", HostVersion = "2026", Grasshopper = info };
        var json = BridgeJson.Serialize(context);

        Assert.DoesNotContain("\"grasshopperVersion\"", json);
        Assert.DoesNotContain("\"rhinoVersion\"", json);
        Assert.Contains("Dàn_Thép.gh", json);

        var back = BridgeJson.Deserialize<ContextResult>(json)!;
        Assert.NotNull(back.Grasshopper);
        Assert.Null(back.Grasshopper.GrasshopperVersion);
        Assert.Null(back.Grasshopper.RhinoVersion);
        Assert.Equal("Dàn_Thép.gh", back.Grasshopper.ActiveDefinitionName);
        Assert.Equal(@"D:\DựÁn\Dàn_Thép.gh", back.Grasshopper.ActiveDefinitionPath);
    }

    [Fact]
    public void ContextResult_for_all_eleven_sibling_hosts_never_contains_grasshopper_payload()
    {
        var siblingContexts = new[]
        {
            new ContextResult { Host = "revit", RevitVersion = "2026", DocTitle = "RevitModel.rvt" },
            new ContextResult { Host = "autocad", Autocad = new AutocadInfo("mm", "1", "Model", "0", true, true, true) },
            new ContextResult { Host = "navis", Navis = new NavisInfo("Meters", 0, [], 0, 0, 0, false, false, true, false, false) },
            new ContextResult { Host = "etabs", Etabs = new EtabsInfo(true, 1, "2.10", false, "kN, mm, C", "kN, mm, C", false, 1, 2, 3) },
            new ContextResult { Host = "civil3d", Civil3d = new Civil3dInfo("Civil3D", true, "Meters", null, false, 1, 1, 1, 1, 1, 1) },
            new ContextResult { Host = "sap2000", Sap2000 = new Sap2000Info(true, 1, "27.0", false, "kN, m, C", "kN, m, C", false, 1, 2, 3) },
            new ContextResult { Host = "powerbi", PowerBi = new PowerBiInfo(true, 1, 5000, "Model", "1600", false, 1, 1, 1) },
            new ContextResult { Host = "excel", Excel = new ExcelInfo(true, 1, "16.0", "Book.xlsx", "Sheet1", "A1", true, false, 1, 1, true) },
            new ContextResult { Host = "robot", Robot = new RobotInfo(true, 1, "2026", "Frame3D", true, false, 1, 1, 1, 1) },
            new ContextResult { Host = "tekla", Tekla = new TeklaInfo(true, "Model", @"C:\Tekla", "Project", "2025.0", false, 1, 1, 1) },
            new ContextResult { Host = "dynamo", Dynamo = new DynamoInfo(true, "3.7.0", "Graph.dyn", @"C:\Graph.dyn", "Automatic", 1, 1, 1, false) },
        };

        foreach (var ctx in siblingContexts)
        {
            var json = BridgeJson.Serialize(ctx);
            Assert.DoesNotContain("\"grasshopper\"", json);
        }
    }

    [Fact]
    public void ContextResult_for_grasshopper_contains_zero_sibling_payloads()
    {
        var context = new ContextResult
        {
            Host = "grasshopper",
            HostVersion = "2026",
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "Test.gh", @"C:\Test.gh", 10, 10, 0, false, true),
        };

        var json = BridgeJson.Serialize(context);
        Assert.Contains("\"grasshopper\"", json);
        Assert.DoesNotContain("\"autocad\"", json);
        Assert.DoesNotContain("\"navis\"", json);
        Assert.DoesNotContain("\"etabs\"", json);
        Assert.DoesNotContain("\"civil3d\"", json);
        Assert.DoesNotContain("\"sap2000\"", json);
        Assert.DoesNotContain("\"powerbi\"", json);
        Assert.DoesNotContain("\"excel\"", json);
        Assert.DoesNotContain("\"robot\"", json);
        Assert.DoesNotContain("\"tekla\"", json);
        Assert.DoesNotContain("\"dynamo\"", json);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(120)]
    [InlineData(300)]
    [InlineData(599)]
    [InlineData(600)]
    public void ToolValidator_accepts_timeouts_within_grasshopper_ceiling_of_600(int timeoutSeconds)
    {
        var candidate = Candidate(timeoutSeconds);
        var report = ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper);
        Assert.DoesNotContain(report.Errors, e => e.Contains("timeoutSeconds"));
    }

    [Theory]
    [InlineData(601)]
    [InlineData(602)]
    [InlineData(900)]
    [InlineData(1200)]
    public void ToolValidator_rejects_timeouts_exceeding_grasshopper_ceiling_of_600(int timeoutSeconds)
    {
        var candidate = Candidate(timeoutSeconds);
        var report = ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper);
        Assert.Contains(report.Errors, e => e == "timeoutSeconds must be between 5 and 600.");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-600)]
    public void ToolValidator_rejects_timeouts_below_minimum_of_5(int timeoutSeconds)
    {
        var candidate = Candidate(timeoutSeconds);
        var report = ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper);
        Assert.Contains(report.Errors, e => e == "timeoutSeconds must be between 5 and 600.");
    }

    [Fact]
    public void ToolValidator_honors_custom_maxTimeout_on_grasshopper_profile()
    {
        var customProfile = Grasshopper(maxTimeout: 120);
        Assert.DoesNotContain(ToolValidator.Validate(Candidate(120), null, [], false, customProfile).Errors, e => e.Contains("timeoutSeconds"));
        Assert.Contains(ToolValidator.Validate(Candidate(121), null, [], false, customProfile).Errors, e => e == "timeoutSeconds must be between 5 and 120.");
    }

    [Fact]
    public void HostProfile_Grasshopper_declares_exactly_seven_distinct_core_tools()
    {
        var expectedCoreTools = new[]
        {
            "execute_grasshopper_code",
            "get_grasshopper_context",
            "read_grasshopper_canvas",
            "run_grasshopper_player",
            "batch_edit_grasshopper_canvas",
            "export_grasshopper_canvas_image",
            "cancel_execution",
        };

        Assert.Equal(7, HostProfile.Grasshopper.CoreToolNames.Count);
        Assert.Equal(7, HostProfile.Grasshopper.CoreToolNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(expectedCoreTools, HostProfile.Grasshopper.CoreToolNames);
        Assert.Equal("execute_grasshopper_code", HostProfile.Grasshopper.ExecuteToolName);
        Assert.Equal("get_grasshopper_context", HostProfile.Grasshopper.ContextToolName);
    }

    [Theory]
    [InlineData("execute_grasshopper_code")]
    [InlineData("get_grasshopper_context")]
    [InlineData("read_grasshopper_canvas")]
    [InlineData("run_grasshopper_player")]
    [InlineData("batch_edit_grasshopper_canvas")]
    [InlineData("export_grasshopper_canvas_image")]
    [InlineData("cancel_execution")]
    public void ToolValidator_rejects_shadowing_of_all_seven_grasshopper_core_tools(string coreToolName)
    {
        Assert.True(ToolValidator.IsReserved(coreToolName, HostProfile.Grasshopper));
        var candidate = CandidateNamed(coreToolName);
        var report = ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper);
        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e == $"name '{coreToolName}' is reserved by the server.");
    }

    [Theory]
    [InlineData("search_tools")]
    [InlineData("get_tool")]
    [InlineData("run_tool")]
    [InlineData("get_run")]
    [InlineData("propose_tool")]
    [InlineData("test_tool")]
    [InlineData("publish_tool")]
    [InlineData("manage_tool")]
    public void ToolValidator_rejects_shadowing_of_all_registry_tools(string registryToolName)
    {
        Assert.True(ToolValidator.IsReserved(registryToolName, HostProfile.Grasshopper));
        var candidate = CandidateNamed(registryToolName);
        var report = ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper);
        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e == $"name '{registryToolName}' is reserved by the server.");
    }

    [Theory]
    [InlineData("create_mesh_rebar")]
    [InlineData("extract_curve_points")]
    [InlineData("solve_truss_deflection")]
    public void ToolValidator_allows_unreserved_grasshopper_tools(string toolName)
    {
        Assert.False(ToolValidator.IsReserved(toolName, HostProfile.Grasshopper));
        var candidate = CandidateNamed(toolName);
        var report = ToolValidator.Validate(candidate, null, [], false, HostProfile.Grasshopper);
        Assert.DoesNotContain(report.Errors, e => e.Contains("is reserved by the server"));
    }

    private static ToolRecord CandidateNamed(string name) => new ToolRecord
    {
        Name = name,
        Title = "Sample candidate tool",
        Description = "Valid sample tool description with >= 20 characters.",
        Category = "Canvas",
        Transaction = "none",
        TimeoutSeconds = 60,
        Host = "grasshopper",
        Code = "return doc.FilePath;",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
        Examples = [new ToolExample { Title = "default", Args = JsonSerializer.Deserialize<JsonElement>("{}") }],
    };
}
