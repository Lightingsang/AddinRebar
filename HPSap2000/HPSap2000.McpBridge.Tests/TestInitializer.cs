using System.Runtime.CompilerServices;
using HPSap2000.McpBridge.Service;

namespace HPSap2000.McpBridge.Tests;

/// <summary>Installs the SAP2000 assembly resolver before any tests that reference SAP2000v1 types are JIT-compiled.</summary>
internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        SapAssemblyResolver.Install();
    }
}
