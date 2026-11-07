using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Win32;

namespace HPFoundation.App.Etabs;

/// <summary>
///     Finds the installed ETABS and resolves its wrapper from there, so no CSI binary ships with the tool.
///     The resolver must be in place before any method naming an ETABSv1 type is compiled.
/// </summary>
internal static class EtabsInstallation
{
    private const string ComRegistration = @"SOFTWARE\Classes\CLSID\{e4f6d00f-51a5-4d65-a09a-ba00fcbf1f82}\LocalServer32";
    private const string DefaultFolder = @"Computers and Structures\ETABS 22";
    private const string WrapperName = "ETABSv1";

    public static string ProgramPath { get; } = FindProgram();

    public static void InstallResolver() => AssemblyLoadContext.Default.Resolving += ResolveWrapper;

    private static Assembly? ResolveWrapper(AssemblyLoadContext context, AssemblyName name)
    {
        if (!string.Equals(name.Name, WrapperName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var folder = Path.GetDirectoryName(ProgramPath)!;
        return context.LoadFromAssemblyPath(Path.Combine(folder, $"{WrapperName}.dll"));
    }

    private static string FindProgram()
    {
        using var key = Registry.LocalMachine.OpenSubKey(ComRegistration);
        var registered = (key?.GetValue(null) as string)?.Trim('"');
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return !string.IsNullOrEmpty(registered) && File.Exists(registered)
            ? registered
            : Path.Combine(programFiles, DefaultFolder, "ETABS.exe");
    }
}
