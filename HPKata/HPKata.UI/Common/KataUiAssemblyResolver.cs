using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

namespace HPKata.UI.Common
{
    /// <summary>
    /// Conflict-resilient assembly resolver for HPKata.UI and its dependencies (CommunityToolkit.Mvvm,
    /// MaterialDesignThemes.Wpf, MaterialDesignColors, ClosedXML, QRCoder).
    /// Handles side-by-side version conflicts when another CAD/Revit plugin has already loaded an older
    /// assembly version (e.g. CommunityToolkit.Mvvm 8.2.0.0) into AssemblyLoadContext.Default.
    /// </summary>
    public static class KataUiAssemblyResolver
    {
        private static readonly string[] HostPrefixes = ["acdbmgd", "acmgd", "accoremgd", "AcWindows", "AdWindows", "RevitAPI"];
        private static readonly string[] PreloadNames =
        [
            "HPKata.Core",
            "CommunityToolkit.Mvvm",
            "MaterialDesignColors",
            "MaterialDesignThemes.Wpf",
            "Microsoft.Xaml.Behaviors"
        ];

        private static readonly ConcurrentDictionary<string, Assembly> IsolatedCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly IsolatedDependencyContext IsolatedContext = new();
        private static int _initialized;

#pragma warning disable CA2255
        [ModuleInitializer]
        public static void Initialize()
#pragma warning restore CA2255
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
            {
                return;
            }

            RegisterHooks();
            PreloadCoreDependencies();
        }

        public static void EnsureRegistered() => Initialize();

        private static void RegisterHooks()
        {
            AssemblyLoadContext.Default.Resolving += OnDefaultResolving;

            MethodInfo? loadFromMethod = typeof(Assembly).GetMethod(
                "LoadFromResolveHandler",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (loadFromMethod != null)
            {
                var builtInHandler = (ResolveEventHandler)Delegate.CreateDelegate(
                    typeof(ResolveEventHandler), loadFromMethod);

                FieldInfo? handlerSetField = typeof(Assembly).GetField(
                    "s_loadFromHandlerSet",
                    BindingFlags.NonPublic | BindingFlags.Static);
                handlerSetField?.SetValue(null, true);

                AppDomain.CurrentDomain.AssemblyResolve -= builtInHandler;
                AppDomain.CurrentDomain.AssemblyResolve += OnDomainAssemblyResolve;
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    Assembly? resolvedFirst = OnDomainAssemblyResolve(sender, args);
                    if (resolvedFirst != null)
                    {
                        return resolvedFirst;
                    }

                    try
                    {
                        return builtInHandler(sender, args);
                    }
                    catch (FileLoadException)
                    {
                        // Annotated catch per Rule Q-B2: fallback to isolated ALC when Default ALC has older version
                        return OnDomainAssemblyResolve(sender, args);
                    }
                };
            }
            else
            {
                AppDomain.CurrentDomain.AssemblyResolve += OnDomainAssemblyResolve;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PreloadCoreDependencies()
        {
            foreach (string simpleName in PreloadNames)
            {
                try
                {
                    ResolveAssembly(new AssemblyName(simpleName));
                }
                catch (Exception)
                {
                    // Annotated catch per Rule Q-B2: non-fatal preload probe during module initialization
                }
            }
        }

        private static Assembly? OnDefaultResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            return ResolveAssembly(assemblyName);
        }

        private static Assembly? OnDomainAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                return ResolveAssembly(new AssemblyName(args.Name));
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: ignore malformed assembly names from third-party plugins
                return null;
            }
        }

        public static Assembly? ResolveAssembly(AssemblyName requestedName, IEnumerable<string>? extraSearchDirs = null)
        {
            string? simpleName = requestedName.Name;
            if (string.IsNullOrWhiteSpace(simpleName) ||
                simpleName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) ||
                IsHostAssembly(simpleName))
            {
                return null;
            }

            string? candidatePath = FindCandidateDllPath(simpleName, extraSearchDirs);
            Version? fileVersion = GetCandidateFileVersion(candidatePath);
            Version? targetVersion = requestedName.Version ?? fileVersion;

            Assembly? defaultMatch = AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));

            if (defaultMatch != null)
            {
                Version? loadedVersion = defaultMatch.GetName().Version;
                if (targetVersion == null || (loadedVersion != null && loadedVersion >= targetVersion))
                {
                    return defaultMatch;
                }

                // Default ALC already holds an older version (e.g. 8.2.0.0 < 8.4.0.0).
                // Load our version into IsolatedContext so Default ALC does not throw 0x80131621.
                return LoadIntoIsolatedContext(simpleName, targetVersion, candidatePath);
            }

            if (candidatePath == null)
            {
                return null;
            }

            try
            {
                if (requestedName.Version != null && fileVersion != null && fileVersion < requestedName.Version)
                {
                    return null;
                }

                return AssemblyLoadContext.Default.LoadFromAssemblyPath(candidatePath);
            }
            catch (FileLoadException)
            {
                // Annotated catch per Rule Q-B2: Default ALC rejected load due to name collision; isolate in custom ALC
                return LoadIntoIsolatedContext(simpleName, targetVersion, candidatePath);
            }
        }

        private static Assembly? LoadIntoIsolatedContext(string simpleName, Version? targetVersion, string? candidatePath)
        {
            if (IsolatedCache.TryGetValue(simpleName, out Assembly? cached))
            {
                if (targetVersion == null || cached.GetName().Version >= targetVersion)
                {
                    return cached;
                }
            }

            if (candidatePath != null && File.Exists(candidatePath))
            {
                Assembly loaded = IsolatedContext.LoadFromAssemblyPath(candidatePath);
                IsolatedCache[simpleName] = loaded;
                return loaded;
            }

            Assembly? existingInAnyAlc = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                    string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase) &&
                    (targetVersion == null || a.GetName().Version >= targetVersion));

            if (existingInAnyAlc != null)
            {
                IsolatedCache[simpleName] = existingInAnyAlc;
            }

            return existingInAnyAlc;
        }

        private static Version? GetCandidateFileVersion(string? candidatePath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath) || !File.Exists(candidatePath))
            {
                return null;
            }

            try
            {
                return AssemblyName.GetAssemblyName(candidatePath).Version;
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: ignore non-managed DLLs during version inspection
                return null;
            }
        }

        private static string? FindCandidateDllPath(string simpleName, IEnumerable<string>? extraSearchDirs = null)
        {
            string fileName = simpleName + ".dll";
            var dirs = extraSearchDirs != null
                ? extraSearchDirs.Concat(GetProbeDirectories())
                : GetProbeDirectories();

            foreach (string dir in dirs)
            {
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;
                string fullPath = Path.Combine(dir, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            return null;
        }

        private static IEnumerable<string> GetProbeDirectories()
        {
            string? ownDir = Path.GetDirectoryName(typeof(KataUiAssemblyResolver).Assembly.Location);
            if (!string.IsNullOrWhiteSpace(ownDir)) yield return ownDir;

            yield return @"C:\HPKata";

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(appData, @"Autodesk\ApplicationPlugins\HPKata.bundle\Contents");
            yield return Path.Combine(appData, @"Autodesk\Revit\Addins\2026\HPKata.Revit");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir)) yield return baseDir;
        }

        private static bool IsHostAssembly(string simpleName)
        {
            foreach (string prefix in HostPrefixes)
            {
                if (simpleName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private sealed class IsolatedDependencyContext : AssemblyLoadContext
        {
            public IsolatedDependencyContext() : base("HPKata.UI.Dependencies", isCollectible: false)
            {
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                string? simpleName = assemblyName.Name;
                if (string.IsNullOrWhiteSpace(simpleName) || IsHostAssembly(simpleName))
                {
                    return null;
                }

                Assembly? inDefault = Default.Assemblies
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
                if (inDefault != null && (assemblyName.Version == null || inDefault.GetName().Version >= assemblyName.Version))
                {
                    return inDefault;
                }

                if (IsolatedCache.TryGetValue(simpleName, out Assembly? cached))
                {
                    return cached;
                }

                string? path = FindCandidateDllPath(simpleName);
                if (path != null)
                {
                    Assembly loaded = LoadFromAssemblyPath(path);
                    IsolatedCache[simpleName] = loaded;
                    return loaded;
                }

                return null;
            }
        }
    }
}
