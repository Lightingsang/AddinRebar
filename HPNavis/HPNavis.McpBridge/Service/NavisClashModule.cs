using System.Runtime.CompilerServices;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using Serilog;

namespace HPNavis.McpBridge.Service;

/// <summary>
///     Clash Detective ships with Navisworks Manage only. The bridge compiles against
///     <c>Autodesk.Navisworks.Clash.dll</c>, so every touch of a Clash type sits in its own non-inlined
///     method: on a Simulate install the JIT throws there, is caught once, and the bridge simply reports
///     <c>HasClashModule = false</c> instead of failing to load.
/// </summary>
public static class NavisClashModule
{
    private static bool? _available;

    public static bool IsAvailable
    {
        get
        {
            if (_available is { } known) return known;

            try
            {
                Touch();
                _available = true;
            }
            catch (Exception exception) when (exception is TypeLoadException or System.IO.FileNotFoundException or System.IO.FileLoadException or MissingMethodException)
            {
                Log.Information("Clash module not available: {Reason}", exception.GetType().Name);
                _available = false;
            }

            return _available.Value;
        }
    }

    /// <summary>Number of clash tests in the document, 0 when the module is absent or the read fails.</summary>
    public static int TestCount(Document doc)
    {
        if (!IsAvailable) return 0;

        try { return CountTests(doc); }
        catch { return 0; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Touch() => _ = typeof(DocumentClash).Assembly.GetName();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountTests(Document doc) => doc.GetClash().TestsData.Tests.Count;

    /// <summary>The Clash assembly for the script compiler's references, or null on Simulate.</summary>
    public static System.Reflection.Assembly? Assembly => IsAvailable ? AssemblyOf() : null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static System.Reflection.Assembly AssemblyOf() => typeof(DocumentClash).Assembly;
}
