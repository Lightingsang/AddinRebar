using System.Runtime.CompilerServices;
using HPEtabs.McpBridge.Service;

namespace HPEtabs.McpBridge.Tests;

/// <summary>Installs the ETABS assembly resolver before any tests that reference ETABSv1 types are JIT-compiled.</summary>
internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EtabsAssemblyResolver.Install();
    }
}
