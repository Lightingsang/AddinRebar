using System.Reflection;
using HPSap2000.McpBridge.Service;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPSap2000.McpBridge.Tests;

/// <summary>
///     The bridge's exact script environment (references incl. the installed SAP2000v1.dll, imports, globals) and the
///     embedded tier table, shared by the tests that classify scripts: what these tests bind is what the bridge binds.
/// </summary>
internal static class SapScriptEnvironment
{
    public static readonly Assembly Wrapper = Assembly.Load("SAP2000v1");

    public static readonly ScriptCompiler Compiler = BridgeEntry.CreateScriptCompiler(new BridgeSettings(), Wrapper);

    public static readonly SapTierAnalyzer Analyzer = new(SapTierTable.Embedded);

    /// <summary>Compiles (the script must compile — the analyzer only sees compiled scripts) and classifies.</summary>
    public static TierVerdict Inspect(string code)
    {
        var compiled = Compiler.GetOrCompile(code);
        if (compiled.Script is null)
            throw new InvalidOperationException("test script does not compile: " + string.Join(" | ", compiled.Diagnostics.Select(d => $"{d.Id} {d.Message}")));
        return Analyzer.Inspect(compiled.Script);
    }
}
