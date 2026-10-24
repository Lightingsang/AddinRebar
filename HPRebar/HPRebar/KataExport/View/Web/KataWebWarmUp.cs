using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;

namespace HPRebar.KataExport.View.Web;

/// <summary>
///     Binds the WebView2 assemblies while Revit is still loading add-ins one by one. Bound lazily instead, the first
///     page message would ask Revit's add-in loader for them from the UI thread, and that loader blocks while another
///     add-in loads an assembly on another thread (the HP BIM Toolkit deadlock). Best effort: a failure is logged and
///     the web window reports it when opened.
/// </summary>
internal static class KataWebWarmUp
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var core = typeof(CoreWebView2Environment).Assembly;
            var wpf = typeof(WebView2).Assembly;
            RuntimeHelpers.RunClassConstructor(typeof(KataWebWindow).TypeHandle);
            Log.Information("Kata web: WebView2 {Version} from {Location} in load context {Context}, WPF control {Wpf}, warmed up in {Elapsed} ms",
                core.GetName().Version, core.Location, AssemblyLoadContext.GetLoadContext(core)?.Name, wpf.GetName().Version, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Kata web: WebView2 warm-up failed after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
