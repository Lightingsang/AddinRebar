using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPAutoCad.Mcp.Server.Hosts;

/// <summary>
///     The AutoCAD profile as this exe registers it: pipe <c>hpautocad-mcp-{version}</c>, wire prefix
///     <c>autocad.</c>, registry root <c>%AppData%\HPAutoCad\McpServer\</c>, and this assembly as the home
///     of the AutoCAD tool classes and the embedded seed library. One exe, one host, one pipe.
/// </summary>
public static class AutocadHostProfile
{
    public const string ExecuteToolName = "execute_autocad_code";
    public const string ContextToolName = "get_autocad_context";

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.AutocadHost,
        DisplayName = "AutoCAD",
        ServerName = "HPAutoCad MCP",
        ProductFolder = "HPAutoCad",
        EnvPrefix = "HPAUTOCAD_MCP_",
        DefaultVersion = 2026,
        // AutoCAD 2026 base release only (R25.1 on .NET 8); Update 1.2 / 2027 run .NET 10 and are unverified.
        ValidVersions = new[] { 2026 },
        MethodPrefix = JsonRpcMethods.AutocadPrefix,
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.AutocadHost,
        Categories = new[] { "Drawing", "Layer", "Block", "Annotation", "Layout", "Data", "Generic" },
        CoreToolNames = new[] { ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.AutocadImports,
        ScriptContractSummary =
            "Globals: doc (Document), db (Database), ed (Editor), app (DocumentCollection), tr (the bridge's Transaction — use tr.GetObject / tr.AddNewlyCreatedDBObject, never Commit/Abort/Dispose it, never start a transaction or lock of your own), " +
            "units (units.ToDrawing(mm), units.ToMm(du), units.Label — coordinates are drawing units), ct, log(string), progress(cur,total,msg), args. " +
            "transaction: auto when the code changes the drawing, none when it only reads; manual is accepted but runs like auto. " +
            "Editor prompts, SendStringToExecute and modal dialogs are blocked. U in AutoCAD reverts the runs made since the user's last command.",
        HostAssembly = typeof(AutocadHostProfile).Assembly,
        CliExecutable = "HPAutoCad.Mcp.Server.exe",
    };
}
