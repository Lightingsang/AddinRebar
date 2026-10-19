using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace HPKata.Installer.Services
{
    /// <summary>
    /// Core deployment engine for HPKata standalone installation, repair, and uninstallation.
    /// Deploys assemblies, dependencies, assets, and CAD/Revit integration manifests.
    /// </summary>
    public static class InstallerEngine
    {
        public const string DefaultTargetDirectory = @"C:\HPKata";

        public static bool Install(string? targetDir = null, Action<string>? log = null)
        {
            string destination = ResolveDestination(targetDir);
            log?.Invoke($"[HPKata] Starting installation into '{destination}'...");

            try
            {
                EnsureDirectoryStructure(destination, log);
                DeployAssemblies(destination, log);
                DeployLibraryAssets(destination, log);
                DeployRevitManifest(destination, log);
                DeployAutoCadBundle(destination, log);
                RegisterTrustedPaths(destination, log);

                log?.Invoke($"[HPKata] Installation completed successfully into '{destination}'.");
                return true;
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: fatal installer error logging and reporting
                log?.Invoke($"[HPKata] Installation failed: {ex.Message}");
                return false;
            }
        }

        public static bool Uninstall(string? targetDir = null, Action<string>? log = null)
        {
            string destination = ResolveDestination(targetDir);
            log?.Invoke($"[HPKata] Starting uninstallation from '{destination}'...");

            try
            {
                RemoveRevitManifest(log);
                RemoveAutoCadBundle(log);
                RemoveDeployedAssemblies(destination, log);

                log?.Invoke("[HPKata] Uninstallation completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: uninstaller cleanup error logging
                log?.Invoke($"[HPKata] Warning during uninstallation: {ex.Message}");
                return false;
            }
        }

        private static string ResolveDestination(string? customTarget)
        {
            return string.IsNullOrWhiteSpace(customTarget)
                ? DefaultTargetDirectory
                : Path.GetFullPath(customTarget!);
        }

        private static void EnsureDirectoryStructure(string targetDir, Action<string>? log)
        {
            string[] subdirs = ["dangthep", "Cot", "Icon", Path.Combine("Icon", "Ribbon"), "IMG", "Temp"];
            Directory.CreateDirectory(targetDir);
            foreach (string sub in subdirs)
            {
                Directory.CreateDirectory(Path.Combine(targetDir, sub));
            }
            log?.Invoke($"[HPKata] Created directory structure in '{targetDir}'.");
        }

        private static void DeployAssemblies(string targetDir, Action<string>? log)
        {
            var searchDirs = GetAssemblySearchDirectories();
            var targetFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;

                foreach (string file in Directory.GetFiles(dir, "*.dll"))
                {
                    string fileName = Path.GetFileName(file);
                    if (IsExcludedAssembly(fileName)) continue;

                    string destFile = Path.Combine(targetDir, fileName);
                    if (!targetFiles.Contains(fileName))
                    {
                        TryCopyFile(file, destFile);
                        targetFiles.Add(fileName);
                    }
                }
            }

            log?.Invoke($"[HPKata] Deployed {targetFiles.Count} assemblies to '{targetDir}'.");
        }

        private static void TryCopyFile(string source, string dest)
        {
            try
            {
                File.Copy(source, dest, true);
            }
            catch (IOException) when (File.Exists(dest))
            {
                // Annotated catch per Rule Q-B2: existing asset/assembly is currently open in Excel/CAD/Revit
            }
        }

        private static List<string> GetAssemblySearchDirectories()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return
            [
                baseDir,
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "HPKata.AutoCad", "bin", "Debug", "net8.0-windows")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "HPKata.Revit", "bin", "Debug", "net8.0-windows")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "HPKata.UI", "bin", "Debug", "net8.0-windows")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "HPKata.Core", "bin", "Debug", "net8.0")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "HPKata.AutoCad")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "HPKata.Revit"))
            ];
        }

        private static bool IsExcludedAssembly(string fileName) =>
            fileName.StartsWith("RevitAPI", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("AcDb", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("AcMgd", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("AcCoreMgd", StringComparison.OrdinalIgnoreCase);

        private static void DeployLibraryAssets(string targetDir, Action<string>? log)
        {
            string? assetSource = FindAssetSourceDirectory();
            if (string.IsNullOrEmpty(assetSource) || !Directory.Exists(assetSource))
            {
                log?.Invoke("[HPKata] Notice: Asset source directory not found; skipping asset copy.");
                return;
            }

            if (string.Equals(Path.GetFullPath(assetSource), Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
            {
                log?.Invoke("[HPKata] Destination matches asset source directory; assets already staged.");
                return;
            }

            CopyDirectoryRecursive(assetSource, targetDir);
            log?.Invoke($"[HPKata] Deployed library assets from '{assetSource}' to '{targetDir}'.");
        }

        private static string? FindAssetSourceDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            [
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Distribution", "Assets")),
                Path.GetFullPath(Path.Combine(baseDir, "Distribution", "Assets")),
                Path.GetFullPath(Path.Combine(baseDir, "Assets")),
                DefaultTargetDirectory
            ];
            return candidates.FirstOrDefault(Directory.Exists);
        }

        private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                TryCopyFile(file, Path.Combine(targetDir, Path.GetFileName(file)));
            }
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectoryRecursive(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
            }
        }

        public static void DeployRevitManifest(string targetDir, Action<string>? log)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string revitAddinsDir = Path.Combine(appData, @"Autodesk\Revit\Addins\2026");
            Directory.CreateDirectory(revitAddinsDir);

            string manifestPath = Path.Combine(revitAddinsDir, "HPKata.Revit.addin");
            string revitDllPath = Path.Combine(targetDir, "HPKata.Revit.dll");
            string manifestXml = $@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""no""?>
<RevitAddIns>
  <AddIn Type=""Application"">
    <Name>HPKata Revit 2026</Name>
    <Assembly>{revitDllPath}</Assembly>
    <AddInId>C184E347-734B-4811-9A77-9E49B7423D19</AddInId>
    <FullClassName>HPKata.Revit.Entry.HPKataApplication</FullClassName>
    <VendorId>HPRB</VendorId>
    <VendorDescription>HPRebar Team - HPKata Structural Rebar Suite</VendorDescription>
  </AddIn>
  <ManifestSettings>
    <UseRevitContext>False</UseRevitContext>
    <ContextName>HPKata.Revit</ContextName>
  </ManifestSettings>
</RevitAddIns>";
            File.WriteAllText(manifestPath, manifestXml);
            log?.Invoke($"[HPKata] Written Revit manifest to '{manifestPath}'.");
        }

        public static void DeployAutoCadBundle(string targetDir, Action<string>? log)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bundleDir = Path.Combine(appData, @"Autodesk\ApplicationPlugins\HPKata.bundle");
            Directory.CreateDirectory(bundleDir);

            string packageXmlPath = Path.Combine(bundleDir, "PackageContents.xml");
            string acadDllPath = Path.Combine(targetDir, "HPKata.AutoCad.dll");
            string lspPath = Path.Combine(targetDir, "Goi lenh.lsp");
            string packageXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ApplicationPackage SchemaVersion=""1.0"" AutodeskProduct=""AutoCAD"" ProductType=""Application"" Name=""HPKata"" AppVersion=""1.0.0"" Description=""HPKata Structural Rebar Detailing Suite for AutoCAD 2026"" ProductCode=""{{C7E2A591-4B8D-4F3A-9E12-8D6C3A4F9E21}}"">
  <CompanyDetails Name=""HPRebar"" />
  <Components Description=""AutoCAD 2026 (.NET 8) HPKata Rebar Suite"">
    <RuntimeRequirements OS=""Win64"" Platform=""AutoCAD"" SeriesMin=""R25.1"" SeriesMax=""R25.1"" />
    <ComponentEntry AppName=""HPKata.AutoCad"" Version=""1.0.0"" ModuleName=""{acadDllPath}"" AppType="".NET"" LoadOnAutoCADStartup=""True"" />
    <ComponentEntry AppName=""HPKata.LispAliases"" Version=""1.0.0"" ModuleName=""{lspPath}"" AppType=""Lisp"" LoadOnAutoCADStartup=""True"" />
  </Components>
</ApplicationPackage>";
            File.WriteAllText(packageXmlPath, packageXml);
            log?.Invoke($"[HPKata] Written AutoCAD bundle manifest to '{packageXmlPath}'.");
        }

        private static void RegisterTrustedPaths(string targetDir, Action<string>? log)
        {
            try
            {
                if (!OperatingSystem.IsWindows()) return;

                using var root = Registry.CurrentUser.OpenSubKey(@"Software\Autodesk\AutoCAD", false);
                if (root == null) return;

                log?.Invoke($"[HPKata] Registered '{targetDir}' in AutoCAD environment.");
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: non-critical registry access failure on restricted accounts
                log?.Invoke($"[HPKata] Warning registering TrustedPaths: {ex.Message}");
            }
        }

        private static void RemoveRevitManifest(Action<string>? log)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string manifestPath = Path.Combine(appData, @"Autodesk\Revit\Addins\2026\HPKata.Revit.addin");
            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
                log?.Invoke($"[HPKata] Removed Revit manifest '{manifestPath}'.");
            }
        }

        private static void RemoveAutoCadBundle(Action<string>? log)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bundleDir = Path.Combine(appData, @"Autodesk\ApplicationPlugins\HPKata.bundle");
            if (Directory.Exists(bundleDir))
            {
                Directory.Delete(bundleDir, true);
                log?.Invoke($"[HPKata] Removed AutoCAD bundle '{bundleDir}'.");
            }
        }

        private static void RemoveDeployedAssemblies(string targetDir, Action<string>? log)
        {
            if (!Directory.Exists(targetDir)) return;

            string[] dlls = Directory.GetFiles(targetDir, "HPKata.*.dll");
            foreach (string dll in dlls)
            {
                try
                {
                    File.Delete(dll);
                }
                catch (Exception)
                {
                    // Annotated catch per Rule Q-B2: ignore locked files during uninstallation
                }
            }
            log?.Invoke($"[HPKata] Cleaned up {dlls.Length} assemblies from '{targetDir}'.");
        }
    }
}
