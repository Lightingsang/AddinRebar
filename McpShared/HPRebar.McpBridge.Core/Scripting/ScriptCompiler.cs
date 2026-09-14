using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>What the compiler hands back: a runnable script, or the errors that stopped it.</summary>
public sealed record CompileOutcome(Script<object>? Script, IReadOnlyList<ScriptDiagnostic> Diagnostics, bool CacheHit)
{
    public bool Succeeded => Script is not null;
}

/// <summary>
///     Turns source into a <see cref="Script{T}"/> once per distinct source, off the Revit thread. Knows
///     nothing about Revit: the add-in tells it which assemblies to reference and which globals type the
///     script sees. Every referenced assembly is registered with the loader so the script binds to the
///     instances already loaded in the add-in's load context — a second copy would make the globals
///     object fail to cast at run time.
/// </summary>
public sealed class ScriptCompiler
{
    private readonly ScriptOptions _options;
    private readonly Type _globalsType;
    private readonly InteractiveAssemblyLoader _loader;
    private readonly ScriptCache _cache;
    private readonly string _optionsFingerprint;
    private int _compiledCount;

    public ScriptCompiler(IReadOnlyCollection<Assembly> references, IReadOnlyCollection<string> imports, Type globalsType, int cacheSize)
    {
        _globalsType = globalsType;
        _loader = new InteractiveAssemblyLoader();

        foreach (var assembly in references.Append(globalsType.Assembly).Distinct())
        {
            _loader.RegisterDependency(assembly);
        }

        _options = ScriptOptions.Default
            .WithReferences(references)
            .WithImports(imports)
            .WithEmitDebugInformation(false)
            .WithOptimizationLevel(OptimizationLevel.Release);

        _optionsFingerprint = string.Join("|", references.Select(r => r.FullName).Concat(imports)) + "|" + globalsType.AssemblyQualifiedName;
        _cache = new ScriptCache(cacheSize);
    }

    /// <summary>How many real compilations happened; each one is a dynamic assembly that stays in memory.</summary>
    public int CompiledCount => Volatile.Read(ref _compiledCount);

    public int CachedCount => _cache.Count;

    public static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    public CompileOutcome GetOrCompile(string code)
    {
        var key = Hash(code + "\0" + _optionsFingerprint);

        if (_cache.TryGet(key, out var cached)) return new CompileOutcome(cached, [], CacheHit: true);

        var script = CSharpScript.Create<object>(code, _options, _globalsType, _loader);
        var errors = script.Compile()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(ToDiagnostic)
            .ToArray();

        if (errors.Length > 0) return new CompileOutcome(null, errors, CacheHit: false);

        Interlocked.Increment(ref _compiledCount);
        _cache.Add(key, script);

        return new CompileOutcome(script, [], CacheHit: false);
    }

    private static ScriptDiagnostic ToDiagnostic(Diagnostic diagnostic)
    {
        var position = diagnostic.Location.GetLineSpan().StartLinePosition;

        return new ScriptDiagnostic(position.Line + 1, position.Character + 1, diagnostic.Id, diagnostic.GetMessage());
    }
}
