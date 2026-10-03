using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>
/// Verifies the reflection contract between HPAutoCad.Loader (AppLoadContext) and HPAutoCad.Entry.
/// Ensures the entry point methods, signatures, and returned delegates match the loader's requirements.
/// </summary>
public sealed class LoaderContractTests
{
    private const string EntryTypeName = "HPAutoCad.Entry";
    private const string EntryMethodName = "Start";

    [Fact]
    public void Entry_type_exists_and_is_public_static()
    {
        var entryType = typeof(HPAutoCad.Entry);
        Assert.NotNull(entryType);
        Assert.True(entryType.IsAbstract && entryType.IsSealed, "Entry should be static (abstract and sealed)");
    }

    [Fact]
    public void Entry_has_Start_methods_with_expected_overloads()
    {
        var entryType = typeof(HPAutoCad.Entry);
        var startMethods = entryType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == EntryMethodName)
            .ToList();

        Assert.True(startMethods.Count >= 2, $"Expected at least 2 Start overloads, found {startMethods.Count}");

        var twoParam = startMethods.FirstOrDefault(m => m.GetParameters().Length == 2);
        Assert.NotNull(twoParam);
        Assert.Equal(typeof(string), twoParam.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(Action<string>), twoParam.GetParameters()[1].ParameterType);

        var threeParam = startMethods.FirstOrDefault(m => m.GetParameters().Length == 3);
        Assert.NotNull(threeParam);
        Assert.Equal(typeof(string), threeParam.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(string), threeParam.GetParameters()[1].ParameterType);
        Assert.Equal(typeof(string), threeParam.GetParameters()[2].ParameterType);
    }

    [Fact]
    public void Direct_GetMethod_by_name_only_throws_AmbiguousMatchException_due_to_overloads()
    {
        // This test documents the exact reason why HPAutoCadLoaderApplication.cs line 61 throws:
        // entry.GetMethod(EntryMethodName, BindingFlags.Public | BindingFlags.Static) throws AmbiguousMatchException
        // when overloaded methods exist on the type.
        var entryType = typeof(HPAutoCad.Entry);
        Assert.Throws<AmbiguousMatchException>(() =>
        {
            entryType.GetMethod(EntryMethodName, BindingFlags.Public | BindingFlags.Static);
        });
    }

    [Fact]
    public void Disambiguated_reflection_resolution_succeeds()
    {
        var entryType = typeof(HPAutoCad.Entry);
        // Correct way for loader to resolve the preferred Start method:
        var start = entryType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == EntryMethodName && m.GetParameters().Length == 2)
            ?? entryType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == EntryMethodName);

        Assert.NotNull(start);

        // Invoke with temp dir and log action
        var result = start.Invoke(null, new object[] { AppContext.BaseDirectory, new Action<string>(_ => { }) });
        var dict = Assert.IsAssignableFrom<IReadOnlyDictionary<string, Delegate>>(result);

        Assert.True(dict.ContainsKey("info"));
        Assert.True(dict.ContainsKey("kmz-script"));
        Assert.True(dict.ContainsKey("image-script"));
        Assert.True(dict.ContainsKey("import-script"));
        Assert.True(dict.ContainsKey("dialog"));
        Assert.True(dict.ContainsKey("import"));
        Assert.True(dict.ContainsKey("stop"));
    }

    [Fact]
    public void HPAutoCadCommands_declares_commands_for_CadAddinManager()
    {
        EnsureHostAssemblyResolver();
        var cmdType = typeof(HPAutoCad.Commands.HPAutoCadCommands);
        Assert.NotNull(cmdType);
        Assert.True(cmdType.IsPublic && cmdType.IsSealed);

        // Verify [CommandMethod] on methods using reflection data (discovered by CadAddinManager via Mono.Cecil)
        var commandMethods = cmdType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(m => m.GetCustomAttributesData())
            .Where(a => a.AttributeType.Name == "CommandMethodAttribute")
            .Select(a => a.ConstructorArguments.FirstOrDefault().Value?.ToString())
            .Where(name => !string.IsNullOrEmpty(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Core HPGeoLink commands
        Assert.Contains("HPGEO", commandMethods);
        Assert.Contains("HPGEODIALOG", commandMethods);
        Assert.Contains("HPGEOIMPORT", commandMethods);
        Assert.Contains("-HPGEOKMZ", commandMethods);
        Assert.Contains("HPGEOKMZ", commandMethods);
        Assert.Contains("-HPGEOIMPORT", commandMethods);
        Assert.Contains("-HPGEOIMAGE", commandMethods);
        Assert.Contains("HPGEOINFO", commandMethods);

        // Core SmartPlot commands
        Assert.Contains("HPSMARTPLOT", commandMethods);
        Assert.Contains("HPLOT", commandMethods);
    }

    private static bool _resolverRegistered;
    private static void EnsureHostAssemblyResolver()
    {
        if (_resolverRegistered) return;
        _resolverRegistered = true;
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var name = new AssemblyName(args.Name).Name;
            if (name is "accoremgd" or "acdbmgd" or "acmgd")
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var candidateCore = Path.Combine(userProfile, ".nuget", "packages", "autocad.net.core", "25.1.0", "lib", "net8.0", name + ".dll");
                if (File.Exists(candidateCore)) return Assembly.LoadFrom(candidateCore);

                var candidateModel = Path.Combine(userProfile, ".nuget", "packages", "autocad.net.model", "25.1.0", "lib", "net8.0", name + ".dll");
                if (File.Exists(candidateModel)) return Assembly.LoadFrom(candidateModel);

                var candidateNet = Path.Combine(userProfile, ".nuget", "packages", "autocad.net", "25.1.0", "lib", "net8.0", name + ".dll");
                if (File.Exists(candidateNet)) return Assembly.LoadFrom(candidateNet);
            }
            return null;
        };
    }
}

