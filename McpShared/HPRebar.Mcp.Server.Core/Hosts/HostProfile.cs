using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;

namespace HPRebar.Mcp.Server.Hosts;

/// <summary>
///     Data-only implementation. <see cref="Revit"/> carries the values the Revit server shipped with,
///     so every engine constructor can default to it and the 2026-09 tests keep passing unchanged;
///     an exe copies it and sets <see cref="HostAssembly"/>.
/// </summary>
public sealed class HostProfile : IHostProfile
{
    public static readonly HostProfile Revit = new HostProfile
    {
        HostId = PipeNaming.RevitHost,
        DisplayName = "Revit",
        ServerName = "HPRebar Revit MCP",
        ProductFolder = "HPRebar",
        EnvPrefix = "HPREBAR_MCP_",
        DefaultVersion = 2026,
        ValidVersions = new[] { 2025, 2026 },
        MethodPrefix = JsonRpcMethods.RevitPrefix,
        ExecuteToolName = "execute_revit_code",
        ContextToolName = "get_revit_context",
        ResourceScheme = "revit",
        Categories = new[] { "Architecture", "Structure", "MEP", "Annotation", "View", "Data", "Generic" },
        CoreToolNames = new[] { "execute_revit_code", "get_revit_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.RevitImports,
        ScriptContractSummary =
            "Globals: doc (Document), uidoc (UIDocument), app (Application), uiapp (UIApplication), ct, log(string), progress(cur,total,msg), args. " +
            "Revit API lengths are in feet; accept millimetres in args and convert with UnitUtils. " +
            "transaction: auto when the code modifies the model and opens no Transaction itself; none when it only reads; manual only if it opens its own Transaction.",
        HostAssembly = typeof(HostProfile).Assembly,
        CliExecutable = "HPRebar.Mcp.Server.exe",
    };

    public static readonly HostProfile Dynamo = new HostProfile
    {
        HostId = PipeNaming.DynamoHost,
        DisplayName = "Dynamo",
        ServerName = "HPDynamo MCP",
        ProductFolder = "HPDynamo",
        EnvPrefix = "HPDYNAMO_MCP_",
        DefaultVersion = 2026,
        ValidVersions = new[] { 2026 },
        MethodPrefix = JsonRpcMethods.DynamoPrefix,
        ExecuteToolName = "execute_dynamo_code",
        ContextToolName = "get_dynamo_context",
        ResourceScheme = "dynamo",
        Categories = new[] { "Nodes", "Wire", "Workspace", "Geometry", "Revit", "Python", "Package", "Generic" },
        CoreToolNames = new[] { "execute_dynamo_code", "execute_designscript", "get_dynamo_context", "cancel_execution" },
        ScriptImports = HostScriptContracts.DynamoImports,
        ScriptContractSummary =
            "Globals: dynamo (DynamoModel/DynamoViewModel), workspace (WorkspaceModel), doc (Document), ct, log(string), progress(cur,total,msg), args. " +
            "Dual-host execution: headless via RevitDynamoModel or live canvas inspection/authoring. " +
            "transaction: auto when the code modifies Revit elements or Dynamo workspace; none when reading; manual if managing its own Transaction.",
        HostAssembly = typeof(HostProfile).Assembly,
        CliExecutable = "HPDynamo.Mcp.Server.exe",
        MaxTimeoutSeconds = HostScriptContracts.DynamoHeavyMaxTimeoutSeconds,
        BridgeNotConnectedHint = "Open Revit 2026 with Dynamo and ensure HPDynamo MCP Bridge is running (pipe hpdynamo-mcp-2026).",
        TimeoutSemanticsHint = "The script or graph execution may still be running in Dynamo/Revit; check Dynamo status before retrying.",
    };

    public static readonly HostProfile Grasshopper = new HostProfile
    {
        HostId = PipeNaming.GrasshopperHost,
        DisplayName = "Grasshopper",
        ServerName = "HPGrasshopper MCP",
        ProductFolder = "HPGrasshopper",
        EnvPrefix = "HPGRASSHOPPER_MCP_",
        DefaultVersion = 2026,
        ValidVersions = new[] { 2026 },
        MethodPrefix = JsonRpcMethods.GrasshopperPrefix,
        ExecuteToolName = "execute_grasshopper_code",
        ContextToolName = "get_grasshopper_context",
        ResourceScheme = "grasshopper",
        Categories = new[] { "Canvas", "Component", "Parameter", "Geometry", "Revit", "Python", "Player", "Generic" },
        CoreToolNames = new[]
        {
            "execute_grasshopper_code",
            "get_grasshopper_context",
            "read_grasshopper_canvas",
            "run_grasshopper_player",
            "batch_edit_grasshopper_canvas",
            "export_grasshopper_canvas_image",
            "cancel_execution",
        },
        ScriptImports = HostScriptContracts.GrasshopperImports,
        ScriptContractSummary =
            "Globals: doc (GH_Document), canvas (GH_Canvas), rhinoDoc (RhinoDoc), revitDoc (Document), ct, log(string), progress(cur,total,msg), args. " +
            "Dual-host execution: Grasshopper canvas in Rhino.Inside.Revit 2026. " +
            "transaction: auto when modifying Revit elements or Grasshopper canvas; none when reading; manual if managing its own Transaction.",
        HostAssembly = typeof(HostProfile).Assembly,
        CliExecutable = "HPGrasshopper.Mcp.Server.exe",
        MaxTimeoutSeconds = HostScriptContracts.GrasshopperHeavyMaxTimeoutSeconds,
        BridgeNotConnectedHint = "Open Revit 2026 with Rhino.Inside.Revit and Grasshopper, ensure HPGrasshopper MCP Bridge is running (pipe hpgrasshopper-mcp-2026).",
        TimeoutSemanticsHint = "The script or definition solver may still be running in Grasshopper/Revit; check Grasshopper status before retrying.",
    };

    private readonly string? _cliExecutable;

    public required string HostId { get; init; }

    public required string DisplayName { get; init; }

    public required string ServerName { get; init; }

    public required string ProductFolder { get; init; }

    public required string EnvPrefix { get; init; }

    public required int DefaultVersion { get; init; }

    public required IReadOnlyCollection<int> ValidVersions { get; init; }

    public required string MethodPrefix { get; init; }

    public required string ExecuteToolName { get; init; }

    public required string ContextToolName { get; init; }

    public required string ResourceScheme { get; init; }

    public required IReadOnlyCollection<string> Categories { get; init; }

    public required IReadOnlyCollection<string> CoreToolNames { get; init; }

    public required IReadOnlyCollection<string> ScriptImports { get; init; }

    public required string ScriptContractSummary { get; init; }

    public required Assembly HostAssembly { get; init; }

    private readonly int _maxTimeoutSeconds = 120;

    /// <summary>120 unless a host raises it; see <see cref="IHostProfile.MaxTimeoutSeconds"/>. Never below the minimum a script may ask for.</summary>
    public int MaxTimeoutSeconds
    {
        get => _maxTimeoutSeconds;
        init => _maxTimeoutSeconds = value >= Services.ExecuteCodeService.MinTimeoutSeconds
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaxTimeoutSeconds), value, $"must be at least {Services.ExecuteCodeService.MinTimeoutSeconds} seconds");
    }

    /// <summary>Defaults to the host assembly's name + `.exe`, which is what the published server is called.</summary>
    public string CliExecutable
    {
        get => _cliExecutable ?? HostAssembly.GetName().Name + ".exe";
        init => _cliExecutable = value;
    }

    /// <summary>Null (the generic add-in sentence) unless a host sets it; see <see cref="IHostProfile.BridgeNotConnectedHint"/>.</summary>
    public string? BridgeNotConnectedHint { get; init; }

    /// <summary>Null (the generic rollback sentence) unless a host sets it; see <see cref="IHostProfile.TimeoutSemanticsHint"/>.</summary>
    public string? TimeoutSemanticsHint { get; init; }

    public string PipeName(int version) => PipeNaming.For(HostId, version);

    public string Method(string suffix) => JsonRpcMethods.For(MethodPrefix, suffix);

    /// <summary>Same data, different exe assembly — what a host exe registers in DI.</summary>
    public HostProfile WithHostAssembly(Assembly assembly) => new HostProfile
    {
        HostId = HostId, DisplayName = DisplayName, ServerName = ServerName, ProductFolder = ProductFolder, EnvPrefix = EnvPrefix,
        DefaultVersion = DefaultVersion, ValidVersions = ValidVersions, MethodPrefix = MethodPrefix, ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName, ResourceScheme = ResourceScheme, Categories = Categories, CoreToolNames = CoreToolNames,
        ScriptImports = ScriptImports, ScriptContractSummary = ScriptContractSummary, HostAssembly = assembly, CliExecutable = CliExecutable,
        MaxTimeoutSeconds = MaxTimeoutSeconds, BridgeNotConnectedHint = BridgeNotConnectedHint, TimeoutSemanticsHint = TimeoutSemanticsHint,
    };
}
