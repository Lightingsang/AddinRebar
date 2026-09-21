using System.Runtime.CompilerServices;
using HPRobot.McpBridge.Com;

namespace HPRobot.McpBridge.Tests;

internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        RobotAssemblyResolver.Install();
    }
}
