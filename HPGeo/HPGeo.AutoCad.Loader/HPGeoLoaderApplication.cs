using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
using HPGeo.AutoCad.Loader;
using HPGeo.AutoCad.Loader.Ribbon;

[assembly: ExtensionApplication(typeof(HPGeoLoaderApplication))]
[assembly: CommandClass(typeof(HPGeoCommands))]

namespace HPGeo.AutoCad.Loader;

/// <summary>
/// What AutoCAD sees of HPGeo: an extension that, on load, creates <see cref="GeoLoadContext"/>, loads
/// Contents\App\HPGeo.AutoCad.dll into it and calls its <c>Entry.Start</c> by reflection for the delegates the
/// commands invoke. There is no compile-time reference to the add-in on purpose — one would make the JIT load
/// it into the default context before this code runs, defeating the isolation.
/// </summary>
public sealed class HPGeoLoaderApplication : IExtensionApplication
{
    private const string AppFolder = "App";
    private const string AppAssemblyFile = "HPGeo.AutoCad.dll";
    private const string EntryTypeName = "HPGeo.AutoCad.Entry";
    private const string EntryMethodName = "Start";

    /// <summary>The delegates the add-in handed back; null until <see cref="Initialize"/> succeeded.</summary>
    internal static IReadOnlyDictionary<string, Delegate>? App { get; private set; }

    internal static string? StartupError { get; private set; }

    public static string Version { get; } =
        typeof(HPGeoLoaderApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(HPGeoLoaderApplication).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public void Initialize()
    {
        var loaderPath = Assembly.GetExecutingAssembly().Location;
        var appPath = Path.Combine(Path.GetDirectoryName(loaderPath)!, AppFolder, AppAssemblyFile);
        var product = SystemVariable("PRODUCT");
        var acadVersion = SystemVariable("ACADVER");
        LoaderLog.Write($"HPGeo loader {Version} in {product} {acadVersion}: loader={loaderPath}; runtime {RuntimeInformation.FrameworkDescription}");

        try
        {
            if (!File.Exists(appPath)) throw new FileNotFoundException("Add-in assembly missing beside the loader", appPath);

            var context = new GeoLoadContext(appPath);
            var assembly = context.LoadFromAssemblyPath(appPath);
            var entry = assembly.GetType(EntryTypeName) ?? throw new TypeLoadException($"{EntryTypeName} not found in {AppAssemblyFile}");
            var start = entry.GetMethod(EntryMethodName, BindingFlags.Public | BindingFlags.Static)
                        ?? throw new MissingMethodException(EntryTypeName, EntryMethodName);

            var handle = start.Invoke(null, [Path.GetDirectoryName(appPath)!, product, acadVersion]);
            App = handle as IReadOnlyDictionary<string, Delegate>
                  ?? throw new InvalidCastException($"{EntryTypeName}.{EntryMethodName} must return IReadOnlyDictionary<string, Delegate>");
            LoaderLog.Write($"HPGeo {Version} loaded in {product} {acadVersion}: add-in started in load context '{AssemblyLoadContext(assembly)}' with {App.Count} entry points");
        }
        catch (System.Exception exception)
        {
            // AutoCAD would swallow this into a one-line "failed to initialize" — keep the full story on disk.
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            StartupError = cause.GetType().Name + ": " + cause.Message;
            LoaderLog.Write("HPGeo add-in failed to start", exception);
        }

        // The ribbon is built even when the add-in failed (its button is disabled, the tooltip says why); a ribbon
        // failure must never take the commands down, so it gets its own guard.
        try
        {
            HPGeoRibbonTab.Install();
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
            HPGeoRibbonTab.Uninstall();
            HPGeoCommands.Invoke("stop", null);
            LoaderLog.Write($"HPGeo loader {Version} terminated");
        }
        catch (System.Exception exception)
        {
            LoaderLog.Write("terminate failed", exception);
        }
    }

    internal static string SystemVariable(string name)
    {
        try { return Application.GetSystemVariable(name)?.ToString() ?? ""; }
        catch (System.Exception) { return ""; }
    }

    private static string AssemblyLoadContext(Assembly assembly) =>
        System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly)?.Name ?? "default";
}
