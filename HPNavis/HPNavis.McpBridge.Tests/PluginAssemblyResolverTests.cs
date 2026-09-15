using System.IO;
using System.Reflection;
using Xunit;

namespace HPNavis.McpBridge.Tests;

/// <summary>The folder test the resolver uses to tell its own assemblies from a foreign plugin's.</summary>
public sealed class PluginAssemblyResolverTests
{
    [Fact]
    public void An_assembly_inside_the_folder_is_ours_and_one_outside_is_not()
    {
        var ours = typeof(PluginAssemblyResolverTests).Assembly;
        var folder = Path.GetDirectoryName(ours.Location)!;

        Assert.True(PluginAssemblyResolver.IsInFolder(ours, folder));
        Assert.True(PluginAssemblyResolver.IsInFolder(ours, folder + Path.DirectorySeparatorChar)); // trailing separator tolerated
        Assert.False(PluginAssemblyResolver.IsInFolder(typeof(object).Assembly, folder)); // mscorlib lives in the framework folder
    }

    [Fact]
    public void A_sibling_folder_with_the_same_prefix_does_not_count()
    {
        var ours = typeof(PluginAssemblyResolverTests).Assembly;
        var folder = Path.GetDirectoryName(ours.Location)!;

        // "…\HPNavis.McpBridge.Tests" must not match "…\HPNavis.McpBridge" by prefix
        Assert.False(PluginAssemblyResolver.IsInFolder(ours, folder.Substring(0, folder.Length - 1)));
    }

    [Fact]
    public void Dynamic_assemblies_count_as_ours()
    {
        var dynamic = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("HPNavis.Tests.Dynamic"), System.Reflection.Emit.AssemblyBuilderAccess.Run);

        Assert.True(PluginAssemblyResolver.IsInFolder(dynamic, @"C:\anywhere"));
    }
}
