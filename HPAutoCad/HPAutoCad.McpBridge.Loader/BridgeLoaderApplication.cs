using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using HPAutoCad.McpBridge.Loader;
using HPAutoCad.McpBridge.Loader.Ribbon;

[assembly: ExtensionApplication(typeof(BridgeLoaderApplication))]
[assembly: CommandClass(typeof(BridgeLoaderCommands))]

namespace HPAutoCad.McpBridge.Loader;

/// <summary>
///     What AutoCAD sees of the bridge: an extension that, on load, creates <see cref="BridgeLoadContext"/>,
///     loads Contents\Bridge\HPAutoCad.McpBridge.dll into it and starts it by reflection. There is no
///     compile-time reference to the bridge on purpose — one would make the JIT load it into the default
///     context before this code runs, defeating the isolation.
/// </summary>
public sealed class BridgeLoaderApplication : IExtensionApplication
{
    private const string BridgeAssemblyFile = "HPAutoCad.McpBridge.dll";
    private const string EntryTypeName = "HPAutoCad.McpBridge.BridgeEntry";
    private const string EntryMethodName = "Start";

    /// <summary>The delegates the bridge handed back; null until <see cref="Initialize"/> succeeded.</summary>
    internal static IReadOnlyDictionary<string, Delegate>? Bridge { get; private set; }

    internal static string? StartupError { get; private set; }

    public void Initialize()
    {
        var loaderPath = Assembly.GetExecutingAssembly().Location;
        var bridgePath = Path.Combine(Path.GetDirectoryName(loaderPath)!, "Bridge", BridgeAssemblyFile);
        LoaderLog.Write($"initialize: loader={loaderPath}; runtime {RuntimeInformation.FrameworkDescription}");

        try
        {
            if (!File.Exists(bridgePath)) throw new FileNotFoundException("Bridge assembly missing beside the loader", bridgePath);

            var context = new BridgeLoadContext(bridgePath);
            var assembly = context.LoadFromAssemblyPath(bridgePath);
            var entry = assembly.GetType(EntryTypeName) ?? throw new TypeLoadException($"{EntryTypeName} not found in {BridgeAssemblyFile}");
            var start = entry.GetMethod(EntryMethodName, BindingFlags.Public | BindingFlags.Static)
                        ?? throw new MissingMethodException(EntryTypeName, EntryMethodName);

            // The bridge logs through Serilog once it is up; until then, and for anything it wants beside its
            // own log, it can call back into this file.
            Action<string> log = LoaderLog.Write;
            var handle = start.Invoke(null, [Path.GetDirectoryName(bridgePath)!, log]);

            Bridge = handle as IReadOnlyDictionary<string, Delegate>
                     ?? throw new InvalidCastException($"{EntryTypeName}.{EntryMethodName} must return IReadOnlyDictionary<string, Delegate>");

            LoaderLog.Write($"bridge started in load context '{LoadContextNameOf(assembly)}' with {Bridge.Count} entry points");
        }
        catch (System.Exception exception)
        {
            // AutoCAD would swallow this into a one-line "failed to initialize" — keep the full story on disk.
            // Invoke wraps whatever Start threw; the commands print StartupError, so unwrap it for them.
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            StartupError = cause.GetType().Name + ": " + cause.Message;
            LoaderLog.Write("bridge failed to start", exception);
        }

        // The Ribbon tab is built even when the bridge failed (its button is disabled, the tooltip says why); a Ribbon failure
        // must never take the bridge or the commands down, so it gets its own guard.
        try
        {
            McpRibbonTab.Install();
        }
        catch (System.Exception exception)
        {
            LoaderLog.Write("ribbon install failed", exception);
        }
    }

    public void Terminate()
    {
        try
        {
            McpRibbonTab.Uninstall();
        }
        catch (System.Exception exception)
        {
            LoaderLog.Write("ribbon uninstall failed", exception);
        }

        if (Bridge is null) return;

        try
        {
            Invoke("dispose");
        }
        catch (System.Exception exception)
        {
            LoaderLog.Write("bridge dispose failed", exception);
        }
    }

    /// <summary>Runs one of the bridge's entry points; the return value is whatever the delegate returned (null for Action).</summary>
    internal static object? Invoke(string key, params object?[] args)
    {
        if (Bridge is null) throw new InvalidOperationException("The MCP bridge did not start: " + (StartupError ?? "see " + LoaderLog.FilePath));
        if (!Bridge.TryGetValue(key, out var entry)) throw new KeyNotFoundException($"The bridge has no '{key}' entry point");

        return entry.DynamicInvoke(args);
    }

    private static string LoadContextNameOf(Assembly assembly) =>
        System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly)?.Name ?? "<unknown>";
}
