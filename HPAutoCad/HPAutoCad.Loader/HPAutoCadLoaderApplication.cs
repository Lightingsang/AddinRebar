using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
using HPAutoCad.Loader;
using HPAutoCad.Loader.Commands;
using HPAutoCad.Loader.Ribbon;

[assembly: ExtensionApplication(typeof(HPAutoCadLoaderApplication))]
[assembly: CommandClass(typeof(HPGeoCommands))]
[assembly: CommandClass(typeof(SmartPlotCommands))]

namespace HPAutoCad.Loader;

/// <summary>
/// AutoCAD extension entry point for HPAutoCad.
/// Instantiates AppLoadContext, loads Contents\App\HPAutoCad.dll, retrieves entry delegates via
/// reflection, and mounts the HPGeoLink panel onto the shared HPAutoCad Ribbon tab.
/// </summary>
public sealed class HPAutoCadLoaderApplication : IExtensionApplication
{
    private const string AppFolder = "App";
    private const string AppAssemblyFile = "HPAutoCad.dll";
    private const string EntryTypeName = "HPAutoCad.Entry";
    private const string EntryMethodName = "Start";

    /// <summary>The delegates returned by the add-in; null until Initialize succeeds.</summary>
    internal static IReadOnlyDictionary<string, Delegate>? App { get; private set; }

    /// <summary>Recorded failure reason if add-in startup throws an exception.</summary>
    internal static string? StartupError { get; private set; }

    public static string Version { get; } =
        typeof(HPAutoCadLoaderApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(HPAutoCadLoaderApplication).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";

    public void Initialize()
    {
        var loaderPath = Assembly.GetExecutingAssembly().Location;
        var loaderDir = Path.GetDirectoryName(loaderPath)!;
        var appPath = Path.Combine(loaderDir, AppFolder, AppAssemblyFile);
        var product = SystemVariable("PRODUCT");
        var acadVersion = SystemVariable("ACADVER");

        LoaderLog.Write($"HPAutoCad loader {Version} in {product} {acadVersion}: loader={loaderPath}; runtime={RuntimeInformation.FrameworkDescription}");

        try
        {
            if (!File.Exists(appPath))
            {
                throw new FileNotFoundException("Add-in assembly missing beside the loader", appPath);
            }

            var context = new AppLoadContext(appPath);
            var assembly = context.LoadFromAssemblyPath(appPath);
            var entry = assembly.GetType(EntryTypeName) 
                        ?? throw new TypeLoadException($"{EntryTypeName} not found in {AppAssemblyFile}");
            
            var start = entry.GetMethods(BindingFlags.Public | BindingFlags.Static)
                             .FirstOrDefault(m => m.Name == EntryMethodName && m.GetParameters().Length == 2)
                        ?? entry.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                .FirstOrDefault(m => m.Name == EntryMethodName)
                        ?? throw new MissingMethodException(EntryTypeName, EntryMethodName);

            // Invoke Entry.Start: supports both Start(appDir, Action<string>) and legacy Start(appDir, product, acadVersion)
            var parameters = start.GetParameters();
            object? handle = null;

            if (parameters.Length == 2 && parameters[1].ParameterType == typeof(Action<string>))
            {
                handle = start.Invoke(null, [Path.GetDirectoryName(appPath)!, new Action<string>(msg => LoaderLog.Write(msg))]);
            }
            else if (parameters.Length == 3)
            {
                handle = start.Invoke(null, [Path.GetDirectoryName(appPath)!, product, acadVersion]);
            }
            else
            {
                handle = start.Invoke(null, [Path.GetDirectoryName(appPath)!]);
            }

            App = handle as IReadOnlyDictionary<string, Delegate>
                  ?? throw new InvalidCastException($"{EntryTypeName}.{EntryMethodName} must return IReadOnlyDictionary<string, Delegate>");

            var alcName = AssemblyLoadContext.GetLoadContext(assembly)?.Name ?? "default";
            LoaderLog.Write($"HPAutoCad {Version} loaded in {product} {acadVersion}: add-in started in load context '{alcName}' with {App.Count} entry points");
        }
        catch (System.Exception exception)
        {
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            StartupError = cause.GetType().Name + ": " + cause.Message;
            LoaderLog.Write("HPAutoCad add-in failed to start", exception);
        }

        // Install shared Ribbon tab panels. Guarded independently so a Ribbon failure never crashes the add-in.
        try
        {
            HPGeoLinkRibbonTab.Install();
            SmartPlotRibbonPanel.Install();
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
            HPGeoLinkRibbonTab.Uninstall();
            SmartPlotRibbonPanel.Uninstall();
            HPGeoCommands.Invoke("stop", null);
            LoaderLog.Write($"HPAutoCad loader {Version} terminated");
        }
        catch (System.Exception exception)
        {
            LoaderLog.Write("terminate failed", exception);
        }
    }

    internal static string SystemVariable(string name)
    {
        try
        {
            return Application.GetSystemVariable(name)?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
