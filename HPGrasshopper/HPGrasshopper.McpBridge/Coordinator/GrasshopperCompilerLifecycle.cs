namespace HPGrasshopper.McpBridge.Coordinator;

using System;
using System.Reflection;
using HPGrasshopper.McpBridge.Support;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

/// <summary>
///     Manages the lifecycle and caching of the Roslyn script compiler and type inspector,
///     automatically reacting to assembly loading and reference fingerprint drift.
/// </summary>
internal sealed class GrasshopperCompilerLifecycle : IDisposable
{
    private readonly object _lock = new();
    private ScriptCompiler? _compiler;
    private TypeInspector? _inspector;
    private string _fingerprint = string.Empty;

    public GrasshopperCompilerLifecycle()
    {
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    }

    public int CompiledScriptCount => _compiler?.CompiledCount ?? 0;

    public ScriptCompiler GetCompiler(BridgeSettings? settings)
    {
        lock (_lock)
        {
            var currentFingerprint = GrasshopperCompilerReferences.ComputeFingerprint();
            if (_compiler != null && !string.Equals(_fingerprint, currentFingerprint, StringComparison.Ordinal))
            {
                Log.Information("GrasshopperCompilerLifecycle: Reference fingerprint changed, invalidating compiler.");
                InvalidateCompilerCore();
            }

            if (_compiler == null)
            {
                var refs = GrasshopperCompilerReferences.All;
                _compiler = new ScriptCompiler(
                    refs,
                    GrasshopperCompilerReferences.ImportsFor(refs),
                    typeof(GrasshopperScriptGlobals),
                    settings?.ScriptCacheSize ?? 50);
                _fingerprint = currentFingerprint;
            }

            return _compiler;
        }
    }

    public TypeInspector GetInspector()
    {
        lock (_lock)
        {
            _inspector ??= new TypeInspector(GrasshopperCompilerReferences.All);
            return _inspector;
        }
    }

    public InspectResult Inspect(InspectRequest request) => GetInspector().Inspect(request);

    public void InvalidateCompiler()
    {
        lock (_lock)
        {
            InvalidateCompilerCore();
            Log.Information("GrasshopperCompilerLifecycle: Compiler cache invalidated.");
        }
    }

    private void InvalidateCompilerCore()
    {
        _compiler = null;
        _inspector = null;
        _fingerprint = string.Empty;
    }

    private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        var asmName = args.LoadedAssembly?.GetName().Name;
        if (string.IsNullOrEmpty(asmName)) return;

        if (asmName.StartsWith("Rhino", StringComparison.OrdinalIgnoreCase) ||
            asmName.StartsWith("Grasshopper", StringComparison.OrdinalIgnoreCase) ||
            asmName.StartsWith("GH_IO", StringComparison.OrdinalIgnoreCase) ||
            asmName.StartsWith("RevitAPI", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("GrasshopperCompilerLifecycle: Assembly {Name} loaded, invalidating compiler cache.", asmName);
            InvalidateCompiler();
        }
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
    }
}
