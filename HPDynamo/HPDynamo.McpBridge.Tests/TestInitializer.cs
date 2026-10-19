using System.Runtime.CompilerServices;
using HPDynamo.McpBridge.Entry;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace HPDynamo.McpBridge.Tests;

internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        DynamoAssemblyResolver.Install();
    }
}

