using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HPKata.AutoCad.Tests
{
    internal static class TestAssemblyResolver
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            const string acadDir = @"C:\Program Files\Autodesk\AutoCAD 2026";
            if (Directory.Exists(acadDir))
            {
                var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                if (!currentPath.Contains(acadDir, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.SetEnvironmentVariable("PATH", acadDir + ";" + currentPath);
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                var name = new AssemblyName(args.Name).Name;
                if (string.Equals(name, "accoremgd", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "acdbmgd", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "acmgd", StringComparison.OrdinalIgnoreCase))
                {
                    string installCandidate = Path.Combine(acadDir, name + ".dll");
                    if (File.Exists(installCandidate)) return Assembly.LoadFrom(installCandidate);

                    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var candidateModel = Path.Combine(userProfile, ".nuget", "packages", "autocad.net.model", "25.1.0", "lib", "net8.0", name + ".dll");
                    if (File.Exists(candidateModel)) return Assembly.LoadFrom(candidateModel);

                    var candidateCore = Path.Combine(userProfile, ".nuget", "packages", "autocad.net.core", "25.1.0", "lib", "net8.0", name + ".dll");
                    if (File.Exists(candidateCore)) return Assembly.LoadFrom(candidateCore);

                    var candidateNet = Path.Combine(userProfile, ".nuget", "packages", "autocad.net", "25.1.0", "lib", "net8.0", name + ".dll");
                    if (File.Exists(candidateNet)) return Assembly.LoadFrom(candidateNet);
                }
                return null;
            };
        }
    }
}
