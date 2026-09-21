using System.IO;
using System.Reflection;

namespace HPTekla.McpBridge;

/// <summary>
///     .NET Framework binds assemblies by exact version and TeklaStructures.exe's configuration redirects
///     only Trimble's own assemblies. Roslyn scripting asks for System.Collections.Immutable 10.0.0.0 while the
///     package ships 10.0.0.1 (same for Reflection.Metadata, Unsafe, Memory) — inside an in-process plugin
///     nobody can add binding redirects that a console app would get, so this handler answers those requests
///     with the copy beside the plugin. Deliberately narrow: only names on the allow-list, and only for requests
///     that come from nowhere (Roslyn's reflective loads) or from this plugin's own folder. Another plugin in
///     Tekla asking for System.Text.Json 8.0 must never be handed our 10.0.
/// </summary>
public static class PluginAssemblyResolver
{
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.Scripting", "Microsoft.CodeAnalysis.CSharp.Scripting",
        "System.Collections.Immutable", "System.Reflection.Metadata", "System.Runtime.CompilerServices.Unsafe",
        "System.Memory", "System.Buffers", "System.Numerics.Vectors", "System.Threading.Tasks.Extensions",
        "System.Text.Encoding.CodePages", "System.Text.Json", "System.Text.Encodings.Web", "Microsoft.Bcl.AsyncInterfaces",
        "System.IO.Pipelines", "System.Threading.Channels", "System.Diagnostics.DiagnosticSource", "System.ComponentModel.Annotations",
        "HPRebar.Mcp.Contracts", "HPRebar.McpBridge.Core", "CommunityToolkit.Mvvm", "Serilog", "Serilog.Sinks.File",
        "HPTekla.McpBridge",
    };

    private static readonly List<string> Resolved = new();
    private static string? _folder;
    private static int _installed;

    /// <summary>What was resolved so far, for the log and the self-check.</summary>
    public static IReadOnlyList<string> ResolvedNames
    {
        get { lock (Resolved) return Resolved.ToArray(); }
    }

    public static string Folder => _folder ?? string.Empty;

    /// <summary>Idempotent; call before the first Roslyn type is touched (the plugin's entry point).</summary>
    public static void Install(string pluginFolder)
    {
        if (Interlocked.Exchange(ref _installed, 1) == 1) return;

        _folder = pluginFolder;
        AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
    }

    private static Assembly? OnResolve(object? sender, ResolveEventArgs args)
    {
        try { return Resolve(args); }
        catch (Exception exception)
        {
            // A resolver that throws fails the load in whichever plugin asked; never let it surface as our crash.
            lock (Resolved) Resolved.Add($"{args.Name} -> failed: {exception.GetType().Name}");
            return null;
        }
    }

    private static Assembly? Resolve(ResolveEventArgs args)
    {
        var folder = _folder;
        if (folder is null) return null;

        var name = new AssemblyName(args.Name);
        if (name.Name is null || !AllowList.Contains(name.Name)) return null;

        // Only requests that originate in this plugin (or in nobody — reflective loads) get our copies.
        var requester = args.RequestingAssembly;
        if (requester is not null && !IsInFolder(requester, folder)) return null;

        var candidate = Path.Combine(folder, name.Name + ".dll");
        if (!File.Exists(candidate)) return null;

        // In host applications every reflective request may arrive with RequestingAssembly == null, so the requester
        // test above cannot distinguish foreign plugins from us. The version family can: our copy answers only
        // a request for the same major version that is not newer than the file.
        // When requested by our own plugin (requester is not null && IsInFolder(requester, folder)), allow binding
        // forward to higher available major versions (e.g. CommunityToolkit.Mvvm requesting Microsoft.Bcl.AsyncInterfaces 8.0 while 10.0 is shipped).
        var available = AssemblyName.GetAssemblyName(candidate).Version;
        if (name.Version is { } requested && available is not null)
        {
            var isOwnPlugin = requester is not null && IsInFolder(requester, folder);
            if (isOwnPlugin)
            {
                if (requested > available) return null;
            }
            else
            {
                if (requested.Major != available.Major || requested > available) return null;
            }
        }

        var assembly = Assembly.LoadFrom(candidate);
        lock (Resolved) Resolved.Add($"{args.Name} -> {Path.GetFileName(candidate)} {assembly.GetName().Version} (requested by {requester?.GetName().Name ?? "<none>"})");
        return assembly;
    }

    /// <summary>True when the assembly file lives in the plugin folder (dynamic assemblies have no location and count as ours).</summary>
    public static bool IsInFolder(Assembly assembly, string folder)
    {
        string location;
        try { location = assembly.Location; }
        catch (NotSupportedException) { return true; }

        if (string.IsNullOrEmpty(location)) return true;

        var full = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(location).StartsWith(full, StringComparison.OrdinalIgnoreCase);
    }
}
