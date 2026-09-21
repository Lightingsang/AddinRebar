using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Win32;
using Serilog;

namespace HPRobot.McpBridge.Com;

/// <summary>
///     Dynamically resolves and loads <c>Interop.RobotOM.dll</c> from the Robot Structural Analysis
///     Professional 2026 installation directory at runtime.
///     Avoids redistributing vendor interop binaries and ensures runtime compatibility with the installed host.
/// </summary>
public static class RobotAssemblyResolver
{
    public const string InteropFileName = "Interop.RobotOM.dll";
    public const string AssemblyName = "Interop.RobotOM";
    public const string EnvironmentVariable = "HPROBOT_ROBOT_DIR";
    public const string RobotObjectClsid = "{F7870790-CDE5-11D1-8FF1-00A02447BAAE}";

    private static string? _installDir;
    private static string? _resolvedPath;
    private static bool _installed;

    public static string? InstallDirectory => _installDir;

    public static string? ResolvedPath => _resolvedPath;

    public static string? WrapperFileVersion =>
        _installDir is null ? null : FileVersionInfo.GetVersionInfo(Path.Combine(_installDir, InteropFileName)).FileVersion;

    /// <summary>
    ///     Installs the dynamic assembly resolver into the Default AssemblyLoadContext.
    /// </summary>
    public static bool Install()
    {
        if (_installed) return _installDir is not null;
        _installed = true;

        _installDir = FindInstallDirectory();
        if (_installDir is null)
        {
            Log.Error("Interop.RobotOM.dll not found. Set {Env} or install Robot Structural Analysis Professional 2026", EnvironmentVariable);
            return false;
        }

        AssemblyLoadContext.Default.Resolving += Resolve;
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var requested = new AssemblyName(args.Name);
            return Resolve(AssemblyLoadContext.Default, requested);
        };

        Log.Information("Interop.RobotOM.dll will resolve from '{Directory}' (file version: {Version})",
            _installDir, WrapperFileVersion ?? "unknown");
        return true;
    }

    public static string? FindInstallDirectory()
    {
        // 1. Environment variable
        var envPath = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(Path.Combine(envPath, InteropFileName)))
        {
            return envPath;
        }

        // 2. Windows Registry COM LocalServer32
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey($@"CLSID\{RobotObjectClsid}\LocalServer32")
                ?? Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Classes\CLSID\{RobotObjectClsid}\LocalServer32");

            if (key?.GetValue(null) is string exePath)
            {
                var dir = Path.GetDirectoryName(exePath.Trim('\"'));
                if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, InteropFileName)))
                {
                    return dir;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to read Robot COM LocalServer32 registry key");
        }

        // 3. Default Program Files directory
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var defaultDir = Path.Combine(programFiles, "Autodesk", "Robot Structural Analysis Professional 2026", "Exe");
        if (File.Exists(Path.Combine(defaultDir, InteropFileName)))
        {
            return defaultDir;
        }

        return null;
    }

    private static Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
    {
        if (!string.Equals(name.Name, AssemblyName, StringComparison.OrdinalIgnoreCase) || _installDir is null)
            return null;

        var path = Path.Combine(_installDir, InteropFileName);
        if (!File.Exists(path)) return null;

        var assembly = context.LoadFromAssemblyPath(path);
        _resolvedPath = assembly.Location;
        Log.Information("Interop.RobotOM.dll successfully loaded from '{Path}'", path);
        return assembly;
    }
}
