using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPCivil3d.Mcp.Server.Hosts;

/// <summary>
///     The Civil 3D profile as this exe registers it: pipe <c>hpcivil3d-mcp-{version}</c>, wire prefix
///     <c>civil3d.</c>, registry root <c>%AppData%\HPCivil3d\McpServer\</c>, and this assembly as the home of
///     the Civil 3D tool classes and the embedded seed library. Civil 3D is an AutoCAD vertical on the same
///     acad.exe, but it is its own host here: its own bundle, pipe, registry and seeds, so AutoCAD 2026 and
///     Civil 3D 2026 can be served side by side.
/// </summary>
public static class Civil3dHostProfile
{
    public const string ExecuteToolName = "execute_civil3d_code";
    public const string ContextToolName = "get_civil3d_context";

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.Civil3dHost,
        DisplayName = "Civil 3D",
        ServerName = "HPCivil3d MCP",
        ProductFolder = "HPCivil3d",
        EnvPrefix = "HPCIVIL3D_MCP_",
        DefaultVersion = 2026,
        // Civil 3D 2026 on AutoCAD 2026 base release only (R25.1 on .NET 8); 2025 (R25.0) and the .NET 10 updates are unverified.
        ValidVersions = new[] { 2026 },
        MethodPrefix = JsonRpcMethods.Civil3dPrefix,
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.Civil3dHost,
        Categories = new[] { "Document", "Alignment", "Profile", "Surface", "Corridor", "Pipe", "Parcel", "Point", "Data", "Generic" },
        CoreToolNames = new[] { ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.Civil3dImports,
        ScriptContractSummary =
            "Globals: doc (Document), db (Database), ed (Editor), app (DocumentCollection), tr (the bridge's Transaction — use tr.GetObject / tr.AddNewlyCreatedDBObject, never Commit/Abort/Dispose it, never start a transaction or lock of your own), " +
            "civil (CivilDocument — GetAlignmentIds/GetSurfaceIds/GetPipeNetworkIds/CorridorCollection/CogoPoints/Settings/Styles; null when the drawing has no Civil data), " +
            "units (mm ↔ the Civil drawing unit, Meters or Feet: units.ToDrawing(mm), units.ToMm(du), units.Label — plan geometry crosses the tool boundary in mm, stations and elevations stay in drawing units), ct, log(string), progress(cur,total,msg), args. " +
            "transaction: auto when the code changes the drawing, none when it only reads; manual is accepted but runs like auto. " +
            "Editor prompts, SendStringToExecute, modal dialogs, corridor/surface Rebuild, data shortcuts, the survey database and file import/export members are blocked. U in Civil 3D reverts the runs made since the user's last command.",
        HostAssembly = typeof(Civil3dHostProfile).Assembly,
        CliExecutable = "HPCivil3d.Mcp.Server.exe",
        BridgeNotConnectedHint =
            "Civil 3D 2026 bridge not connected. Open Civil 3D 2026 (acad.exe /product C3D — plain AutoCAD 2026 does not load this bundle), " +
            "wait for the HPCivil3d MCP Bridge to load, then open ribbon HPCivil3d > MCP > MCP Bridge (or command HPC3DMCPBRIDGE), start the listener and tick 'Allow AI code execution' (pipe hpcivil3d-mcp-2026).",
    };
}
