namespace HPGrasshopper.E2E.Tests.Contracts;

using System.Collections.Generic;
using System.Linq;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;

/// <summary>
/// Authoritative wire and interface contracts for HPGrasshopper E2E tests,
/// mapped 1:1 to canonical production sources in McpShared and HPGrasshopper.Mcp.Server.
/// </summary>
public static class GrasshopperE2EContracts
{
    public static string HostKey => PipeNaming.GrasshopperHost;
    public static int DefaultHostVersion => GrasshopperHostProfile.DefaultVersion;
    public static string ExpectedPipeName => PipeNaming.For(PipeNaming.GrasshopperHost, GrasshopperHostProfile.DefaultVersion);
    public static string MethodPrefix => JsonRpcMethods.GrasshopperPrefix;

    public const int OptInDisabledErrorCode = BridgeErrorCode.ExecutionDisabled;
    public static string OptInDisabledMessageFragment => "Allow AI Grasshopper execution";
    public const int ExpectedTotalToolsCount = 24;
    public static string SuppressedEngineTool => GrasshopperHostProfile.SuppressedEngineToolName;

    public static IReadOnlyList<string> RequiredImports => HostScriptContracts.GrasshopperImports;
    public static IReadOnlyList<string> RequiredGlobals => HostScriptContracts.GrasshopperGlobals;
    public static IReadOnlyList<string> CoreTools => GrasshopperHostProfile.Instance.CoreToolNames.ToList();
    public static IReadOnlyList<string> RegistryMetaTools => ToolValidator.RegistryToolNames;
    public static IReadOnlyList<string> EmbeddedSeeds => SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly).Select(s => s.Name).ToList();
    public static IReadOnlyList<string> DeniedNamespaces => GuardProfile.Grasshopper.DeniedNamespaces.ToList();

    public static string ResolvePipeName(string host, int version) => PipeNaming.For(host, version);
}
