using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using HPAutoCad.HPGeoLink.Commands;
using HPAutoCad.SmartPlot.Commands;

namespace HPAutoCad.Commands;

/// <summary>
/// Direct AutoCAD commands exposed by HPAutoCad.dll.
/// This allows AutoCAD Add-In Manager (e.g. CadAddinManager) to discover and execute commands directly
/// from bin\Debug\net8.0-windows\HPAutoCad.dll without needing to reload or restart AutoCAD.
/// </summary>
public sealed class HPAutoCadCommands
{
    static HPAutoCadCommands()
    {
        EnsureAssemblyResolverInstalled();
    }

    private static bool _resolverInstalled;
    private static readonly object _lock = new();

    /// <summary>
    /// Installs an assembly resolver so that when CadAddinManager loads HPAutoCad.dll,
    /// sibling dependencies (HPAutoCad.Core.dll, CommunityToolkit.Mvvm.dll, WebView2, PdfSharp, etc.)
    /// in the same directory are resolved reliably.
    /// </summary>
    public static void EnsureAssemblyResolverInstalled()
    {
        if (_resolverInstalled) return;
        lock (_lock)
        {
            if (_resolverInstalled) return;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveSiblingAssembly;
            _resolverInstalled = true;
        }
    }

    private static Assembly? ResolveSiblingAssembly(object? sender, ResolveEventArgs args)
    {
        try
        {
            var requestedName = new AssemblyName(args.Name).Name;
            if (string.IsNullOrEmpty(requestedName)) return null;

            var baseDir = Path.GetDirectoryName(typeof(HPAutoCadCommands).Assembly.Location);
            if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) return null;

            var targetFile = Path.Combine(baseDir, requestedName + ".dll");
            if (File.Exists(targetFile))
            {
                return Assembly.LoadFrom(targetFile);
            }
        }
        catch
        {
            // Ignore resolution errors and allow standard fallback
        }
        return null;
    }

    // --- HPGeoLink Commands ---

    [CommandMethod("HPGEO", CommandFlags.Modal)]
    public void Dialog()
    {
        EnsureAssemblyResolverInstalled();
        HPGeoDialogCommand.Run();
    }

    [CommandMethod("HPGEODIALOG", CommandFlags.Modal)]
    public void DialogAlias() => Dialog();

    [CommandMethod("HPGEOIMPORT", CommandFlags.Modal)]
    public void Import()
    {
        EnsureAssemblyResolverInstalled();
        HPGeoImportCommand.Run();
    }

    [CommandMethod("-HPGEOKMZ", CommandFlags.Modal)]
    public void KmzScript()
    {
        EnsureAssemblyResolverInstalled();
        HPGeoKmzScriptCommand.Run();
    }

    [CommandMethod("HPGEOKMZ", CommandFlags.Modal)]
    public void KmzScriptAlias() => KmzScript();

    [CommandMethod("-HPGEOIMPORT", CommandFlags.Modal)]
    public void ImportScript()
    {
        EnsureAssemblyResolverInstalled();
        HPGeoImportScriptCommand.Run();
    }

    [CommandMethod("-HPGEOIMAGE", CommandFlags.Modal)]
    public void ImageScript()
    {
        EnsureAssemblyResolverInstalled();
        HPGeoImageScriptCommand.Run();
    }

    [CommandMethod("HPGEOINFO", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
    public void Info()
    {
        EnsureAssemblyResolverInstalled();
        HPGeoInfoCommand.Run();
    }

    // --- SmartPlot Commands ---

    [CommandMethod("HPSMARTPLOT", CommandFlags.Modal | CommandFlags.Session)]
    public void SmartPlot()
    {
        EnsureAssemblyResolverInstalled();
        SmartPlotCommand.Run();
    }

    [CommandMethod("HPLOT", CommandFlags.Modal | CommandFlags.Session)]
    public void SmartPlotAlias() => SmartPlot();
}
