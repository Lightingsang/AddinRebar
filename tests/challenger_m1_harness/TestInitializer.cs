using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using HPGrasshopper.McpBridge.Host;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ChallengerM1Harness;

internal static class TestInitializer
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetErrorMode(uint uMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    private const uint SEM_FAILCRITICALERRORS = 0x0001;
    private const uint SEM_NOGPFAULTERRORBOX = 0x0002;
    private const uint SEM_NOOPENFILEERRORBOX = 0x8000;

    private static Assembly? _revitApiUiStub;
    private static readonly object _lock = new();

    [ModuleInitializer]
    internal static void Initialize()
    {
        var errorMode = SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX | SEM_NOOPENFILEERRORBOX;
        SetErrorMode(errorMode);
        SetThreadErrorMode(errorMode, out _);
        var revitDir = @"C:\Program Files\Autodesk\Revit 2026";
        if (Directory.Exists(revitDir))
        {
            SetDllDirectory(revitDir);
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (!currentPath.Contains(revitDir, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH", revitDir + ";" + currentPath);
            }
        }

        GrasshopperAssemblyResolver.Install();

        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            if (string.Equals(name.Name, "RevitAPIUI", StringComparison.OrdinalIgnoreCase))
            {
                lock (_lock)
                {
                    return _revitApiUiStub ??= CreateRevitApiUiStub(context);
                }
            }

            var path = Path.Combine(revitDir, name.Name + ".dll");
            if (File.Exists(path))
            {
                try
                {
                    return context.LoadFromAssemblyPath(path);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        };
    }

    private static Assembly? CreateRevitApiUiStub(AssemblyLoadContext context)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "HPGrasshopper_Challenger_Stubs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var stubDllPath = Path.Combine(tempDir, "RevitAPIUI.dll");

        var stubCode = @"
namespace Autodesk.Revit.UI
{
    public class UIApplication {}
    public class UIDocument {}
    public interface IExternalEventHandler { void Execute(UIApplication app); string GetName(); }
    public enum ExternalEventRequest { Accepted, Pending, Denied, TimedOut }
    public class ExternalEvent { public ExternalEventRequest Raise() => ExternalEventRequest.Accepted; }
    public interface IExternalApplication {}
    public interface IExternalCommand {}
    public enum Result { Succeeded, Failed, Cancelled }
    public class UIControlledApplication {}
    public static class UIThemeManager { public static UITheme CurrentTheme => UITheme.Light; }
    public enum UITheme { Light, Dark }
}";
        var syntaxTree = CSharpSyntaxTree.ParseText(stubCode);
        var compilation = CSharpCompilation.Create(
            "RevitAPIUI",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var emitResult = compilation.Emit(stubDllPath);
        if (!emitResult.Success) return null;

        return context.LoadFromAssemblyPath(stubDllPath);
    }
}
