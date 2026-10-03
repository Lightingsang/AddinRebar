using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Windows;

namespace HPAutoCad.Loader.Ribbon;

/// <summary>
///     Detects and automatically ensures the CadAddinManager Ribbon tab is created.
///     CadAddinManager declares a startup command 'InitAddinManager' in PackageContents.xml,
///     but fails to create its Ribbon tab when AutoCAD launches because ComponentManager.Ribbon
///     is null at startup and CadAddinManager does not hook ComponentManager.ItemInitialized.
///     This integrator invokes CadAddinManager's CreateRibbon() as soon as the Ribbon is ready
///     and restores it whenever the workspace or Ribbon resets.
/// </summary>
internal static class CadAddinManagerIntegrator
{
    private const string CadAddinTabId = "AddinManager";
    private static bool _triedDirectLoad;

    public static void EnsureRibbonTab(RibbonControl ribbon)
    {
        try
        {
            if (ribbon.FindTab(CadAddinTabId) is not null) return;

            // 1. Locate CadAddinManager.App in loaded assemblies
            var appType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a =>
                {
                    try { return a.GetType("CadAddinManager.App"); }
                    catch { return null; }
                })
                .FirstOrDefault(t => t is not null);

            // 2. If not loaded yet, attempt to load from standard bundle install path
            if (appType is null && !_triedDirectLoad)
            {
                _triedDirectLoad = true;
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var bundleDll = Path.Combine(appData, "Autodesk", "ApplicationPlugins", "CadAddinManager.bundle", "26", "CadAddinManager.dll");
                if (File.Exists(bundleDll))
                {
                    try
                    {
                        var asm = Assembly.LoadFrom(bundleDll);
                        appType = asm.GetType("CadAddinManager.App");
                    }
                    catch
                    {
                        // Ignore load failures
                    }
                }
            }

            if (appType is null) return;

            // 3. Instantiate and invoke CreateRibbon()
            var appInstance = Activator.CreateInstance(appType);
            var createRibbonMethod = appType.GetMethod("CreateRibbon", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (createRibbonMethod is not null)
            {
                createRibbonMethod.Invoke(appInstance, null);
                LoaderLog.Write("CadAddinManager ribbon tab automatically restored.");
            }
        }
        catch (Exception ex)
        {
            // Failsafe: never disrupt HPAutoCad if CadAddinManager encounters an issue
            LoaderLog.Write("CadAddinManager auto-ribbon invocation skipped: " + ex.Message);
        }
    }
}
