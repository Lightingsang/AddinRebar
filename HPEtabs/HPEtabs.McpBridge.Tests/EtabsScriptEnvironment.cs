using System.Reflection;
using HPEtabs.McpBridge.Service;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPEtabs.McpBridge.Tests;

/// <summary>
///     The bridge's exact script environment (references incl. the installed ETABSv1.dll, imports, globals) and the
///     embedded tier table, shared by the tests that classify scripts: what these tests bind is what the bridge binds.
/// </summary>
internal static class EtabsScriptEnvironment
{
    public static readonly Assembly Wrapper = Assembly.Load("ETABSv1");

    public static readonly ScriptCompiler Compiler = BridgeEntry.CreateScriptCompiler(new BridgeSettings(), Wrapper);

    public static readonly EtabsTierAnalyzer Analyzer = new(EtabsTierTable.Embedded);

    /// <summary>Compiles (the script must compile — the analyzer only sees compiled scripts) and classifies.</summary>
    public static TierVerdict Inspect(string code)
    {
        var compiled = Compiler.GetOrCompile(code);
        if (compiled.Script is null)
            throw new InvalidOperationException("test script does not compile: " + string.Join(" | ", compiled.Diagnostics.Select(d => $"{d.Id} {d.Message}")));
        return Analyzer.Inspect(compiled.Script);
    }
}
