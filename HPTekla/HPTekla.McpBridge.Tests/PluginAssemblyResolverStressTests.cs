using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace HPTekla.McpBridge.Tests;

public sealed class PluginAssemblyResolverStressTests
{
    private static readonly MethodInfo ResolveMethod = typeof(PluginAssemblyResolver).GetMethod(
        "Resolve", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static Assembly? InvokeResolve(ResolveEventArgs args)
    {
        return (Assembly?)ResolveMethod.Invoke(null, new object[] { args });
    }

    [Fact]
    public void IsInFolder_IdentifiesOwnAndExternalAssembliesAccurately()
    {
        var ours = typeof(PluginAssemblyResolverStressTests).Assembly;
        var folder = Path.GetDirectoryName(ours.Location)!;

        Assert.True(PluginAssemblyResolver.IsInFolder(ours, folder));
        Assert.True(PluginAssemblyResolver.IsInFolder(ours, folder + Path.DirectorySeparatorChar));
        Assert.False(PluginAssemblyResolver.IsInFolder(typeof(object).Assembly, folder));
    }

    [Fact]
    public void IsInFolder_DoesNotMatchSiblingFolderWithIdenticalPrefix()
    {
        var ours = typeof(PluginAssemblyResolverStressTests).Assembly;
        var folder = Path.GetDirectoryName(ours.Location)!;

        // E.g. "HPTekla.McpBridge.Tests" must not be considered inside "HPTekla.McpBridge"
        var chopped = folder.Substring(0, folder.Length - 1);
        Assert.False(PluginAssemblyResolver.IsInFolder(ours, chopped));
    }

    [Fact]
    public void IsInFolder_DynamicAssembliesCountAsOurs()
    {
        var dynamicAsm = AppDomain.CurrentDomain.DefineDynamicAssembly(
            new AssemblyName("HPTekla.Tests.DynamicAssembly"),
            AssemblyBuilderAccess.Run);

        Assert.True(PluginAssemblyResolver.IsInFolder(dynamicAsm, @"C:\anywhere\arbitrary"));
    }

    [Fact]
    public void NonAllowListedAssemblies_AreIgnoredAndReturnNull()
    {
        var binFolder = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location)!;
        PluginAssemblyResolver.Install(binFolder);

        // System.Data or Newtonsoft.Json are not in AllowList
        var args = new ResolveEventArgs("Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed");
        var resolved = InvokeResolve(args);

        Assert.Null(resolved);
    }

    [Fact]
    public void NonExistentCandidate_ReturnsNullGracefully()
    {
        var binFolder = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location)!;
        PluginAssemblyResolver.Install(binFolder);

        // Name is in AllowList ("System.IO.Pipelines"), but simulate if file didn't exist in a dummy folder
        var tempFolder = Path.Combine(Path.GetTempPath(), "HPTeklaResolverTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        try
        {
            // Set private _folder temporarily to test missing file
            var folderField = typeof(PluginAssemblyResolver).GetField("_folder", BindingFlags.NonPublic | BindingFlags.Static)!;
            var originalFolder = folderField.GetValue(null);
            try
            {
                folderField.SetValue(null, tempFolder);
                var args = new ResolveEventArgs("System.IO.Pipelines, Version=10.0.0.12, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
                var resolved = InvokeResolve(args);
                Assert.Null(resolved);
            }
            finally
            {
                folderField.SetValue(null, originalFolder);
            }
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void CorruptedAssemblyFile_CatchesAndLogsFailureWithoutThrowing()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "HPTeklaResolverCorrupt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        try
        {
            // Write a corrupt .dll in temp folder for an allowlisted assembly
            File.WriteAllText(Path.Combine(tempFolder, "System.Text.Json.dll"), "CORRUPTED NOT A REAL PE FILE");

            var folderField = typeof(PluginAssemblyResolver).GetField("_folder", BindingFlags.NonPublic | BindingFlags.Static)!;
            var onResolveMethod = typeof(PluginAssemblyResolver).GetMethod("OnResolve", BindingFlags.NonPublic | BindingFlags.Static)!;
            var originalFolder = folderField.GetValue(null);

            try
            {
                folderField.SetValue(null, tempFolder);
                var args = new ResolveEventArgs("System.Text.Json, Version=10.0.0.12, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
                
                // Calling OnResolve must NOT throw, must return null
                var result = onResolveMethod.Invoke(null, new object?[] { null, args });
                Assert.Null(result);

                // Verify it recorded the failure in ResolvedNames
                var history = PluginAssemblyResolver.ResolvedNames;
                Assert.Contains(history, entry => entry.Contains("System.Text.Json") && entry.Contains("failed:"));
            }
            finally
            {
                folderField.SetValue(null, originalFolder);
            }
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void VersionMismatch_HigherMajorVersionRequestedThanAvailable_ReturnsNull()
    {
        var binFolder = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location)!;
        PluginAssemblyResolver.Install(binFolder);

        // System.Collections.Immutable on disk is 10.0.0.1
        // If a request asks for 11.0.0.0 (higher major version), it must return null
        var args = new ResolveEventArgs("System.Collections.Immutable, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        var resolved = InvokeResolve(args);

        Assert.Null(resolved);
    }

    [Fact]
    public void VersionMismatch_HigherMinorVersionRequestedThanAvailable_ReturnsNull()
    {
        var binFolder = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location)!;
        PluginAssemblyResolver.Install(binFolder);

        // System.Collections.Immutable on disk is 10.0.0.1
        // If a request asks for 10.0.1.0 (same major, but higher revision/build), it must return null
        var args = new ResolveEventArgs("System.Collections.Immutable, Version=10.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        var resolved = InvokeResolve(args);

        Assert.Null(resolved);
    }

    [Fact]
    public void VersionMatching_SameMajorLowerOrEqualVersion_ResolvesSuccessfully()
    {
        var binFolder = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location)!;
        PluginAssemblyResolver.Install(binFolder);

        // Roslyn requests System.Collections.Immutable 10.0.0.0 while 10.0.0.1 is shipped
        var args = new ResolveEventArgs("System.Collections.Immutable, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        var resolved = InvokeResolve(args);

        Assert.NotNull(resolved);
        Assert.Equal("System.Collections.Immutable", resolved!.GetName().Name);
        Assert.Equal(new Version(10, 0, 0, 1), resolved.GetName().Version);
    }

    [Fact]
    public void MicrosoftBclAsyncInterfaces_Version8RequestedByCommunityToolkitMvvm_BindsForwardSuccessfully()
    {
        // CommunityToolkit.Mvvm v8.4.0 on net462 / netstandard2.0 references Microsoft.Bcl.AsyncInterfaces Version=8.0.0.0.
        // System.Text.Json 10.0.12 bundles Microsoft.Bcl.AsyncInterfaces Version=10.0.0.12.
        // When requested by our own plugin assembly, PluginAssemblyResolver allows binding forward across major versions.
        var binFolder = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location)!;
        PluginAssemblyResolver.Install(binFolder);

        var asyncInterfacesDll = Path.Combine(binFolder, "Microsoft.Bcl.AsyncInterfaces.dll");
        Assert.True(File.Exists(asyncInterfacesDll), "Microsoft.Bcl.AsyncInterfaces.dll must exist in bin");
        var diskVersion = AssemblyName.GetAssemblyName(asyncInterfacesDll).Version!;
        Assert.Equal(10, diskVersion.Major);

        // CommunityToolkit.Mvvm assembly from the same plugin folder requesting Version=8.0.0.0
        var mvvmAsm = typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject).Assembly;
        Assert.True(PluginAssemblyResolver.IsInFolder(mvvmAsm, binFolder), "CommunityToolkit.Mvvm is in our plugin folder");

        var args = new ResolveEventArgs(
            "Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
            mvvmAsm);

        var resolved = InvokeResolve(args);

        Assert.NotNull(resolved);
        Assert.Equal("Microsoft.Bcl.AsyncInterfaces", resolved!.GetName().Name);
        Assert.Equal(diskVersion, resolved.GetName().Version);
    }

    [Fact]
    public void ReflectiveRequest_DifferentMajorVersion_ReturnsNullForSafety()
    {
        var binFolder = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location)!;
        PluginAssemblyResolver.Install(binFolder);

        // Reflective request without requesting assembly (requester == null) for version 8 when 10 is available.
        // Foreign plugins or reflective callers asking for an older major must not receive our 10.0.
        var args = new ResolveEventArgs(
            "Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
            null);

        var resolved = InvokeResolve(args);

        Assert.Null(resolved);
    }

    [Fact]
    public void CultureInvariance_SatelliteResourceRequests_ReturnsNullSafely()
    {
        var binFolder = Path.GetDirectoryName(typeof(PluginAssemblyResolver).Assembly.Location)!;
        PluginAssemblyResolver.Install(binFolder);

        // Roslyn satellites or localized resources (e.g. Microsoft.CodeAnalysis.resources)
        var args = new ResolveEventArgs("Microsoft.CodeAnalysis.resources, Version=5.9.0.0, Culture=de, PublicKeyToken=31bf3856ad364e35");
        var resolved = InvokeResolve(args);

        Assert.Null(resolved);
    }
}
