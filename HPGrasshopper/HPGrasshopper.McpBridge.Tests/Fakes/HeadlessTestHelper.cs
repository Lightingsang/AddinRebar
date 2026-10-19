namespace HPGrasshopper.McpBridge.Tests.Fakes;

using System.Runtime.CompilerServices;
using Grasshopper.Kernel;

/// <summary>
///     Headless test helper creating uninitialized Grasshopper document instances
///     without triggering unmanaged P/Invoke into native Rhino/rhcommon_c.dll.
/// </summary>
internal static class HeadlessTestHelper
{
    public static GH_Document CreateSafeHeadlessDocument() =>
        (GH_Document)RuntimeHelpers.GetUninitializedObject(typeof(GH_Document));
}
