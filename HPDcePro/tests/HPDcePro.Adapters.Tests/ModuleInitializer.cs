using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using HPDcePro.Adapters.Tests.Revit;

namespace HPDcePro.Adapters.Tests
{
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            string acadDir = @"C:\Program Files\Autodesk\AutoCAD 2026";
            if (Directory.Exists(acadDir))
            {
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                if (!currentPath.Contains(acadDir, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.SetEnvironmentVariable("PATH", $"{acadDir};{currentPath}");
                }
            }

            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                return RevitStubGenerator.ResolveRevitAssembly(name.Name ?? string.Empty, context);
            };

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string asmName = new AssemblyName(args.Name).Name ?? string.Empty;
                var stub = RevitStubGenerator.ResolveRevitAssembly(asmName, AssemblyLoadContext.Default);
                if (stub != null) return stub;

                string nugetBase = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".nuget", "packages");

                string[] candidates =
                {
                    Path.Combine(acadDir, $"{asmName}.dll"),
                    Path.Combine(nugetBase, "autocad.net.core", "25.1.0", "lib", "net8.0", $"{asmName}.dll"),
                    Path.Combine(nugetBase, "autocad.net.model", "25.1.0", "lib", "net8.0", $"{asmName}.dll"),
                    Path.Combine(nugetBase, "autocad.net", "25.1.0", "lib", "net8.0", $"{asmName}.dll")
                };

                foreach (var path in candidates)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            return Assembly.LoadFrom(path);
                        }
                        catch (Exception)
                        {
                            // Reason: Continue fallback to next candidate if assembly fails to load.
                        }
                    }
                }
                return null;
            };
        }
    }
}
