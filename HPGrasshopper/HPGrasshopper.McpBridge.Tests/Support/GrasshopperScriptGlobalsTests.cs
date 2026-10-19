namespace HPGrasshopper.McpBridge.Tests.Support;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using HPGrasshopper.McpBridge.Support;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

public sealed class GrasshopperScriptGlobalsTests
{
    [Fact]
    public void Globals_Properties_ExposeAllRequiredContractMembers()
    {
        var type = typeof(GrasshopperScriptGlobals);
        var properties = type.GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Required by contract: doc, canvas, rhinoDoc, revitDoc, uidoc, uiapp, ct, log, progress, args
        Assert.Contains("doc", properties);
        Assert.Contains("canvas", properties);
        Assert.Contains("rhinoDoc", properties);
        Assert.Contains("revitDoc", properties);
        Assert.Contains("uidoc", properties);
        Assert.Contains("uiapp", properties);
        Assert.Contains("ct", properties);
        Assert.Contains("log", properties);
        Assert.Contains("progress", properties);
        Assert.Contains("args", properties);

        // Additional dual-safety properties
        Assert.Contains("dryRun", properties);
        Assert.Contains("globals", properties);
    }

    [Fact]
    public void Globals_NonHostPropertyTypes_MatchContractSignatures()
    {
        var type = typeof(GrasshopperScriptGlobals);

        var propCt = type.GetProperty("ct");
        var propLog = type.GetProperty("log");
        var propProgress = type.GetProperty("progress");
        var propArgs = type.GetProperty("args");
        var propDryRun = type.GetProperty("dryRun");
        var propGlobals = type.GetProperty("globals");

        Assert.NotNull(propCt);
        Assert.NotNull(propLog);
        Assert.NotNull(propProgress);
        Assert.NotNull(propArgs);
        Assert.NotNull(propDryRun);
        Assert.NotNull(propGlobals);

        Assert.Equal(typeof(CancellationToken), propCt.PropertyType);
        Assert.Equal(typeof(Action<string?>), propLog.PropertyType);
        Assert.Equal(typeof(Action<int, int?, string?>), propProgress.PropertyType);
        Assert.Equal(typeof(ScriptArgs), propArgs.PropertyType);
        Assert.Equal(typeof(bool), propDryRun.PropertyType);
        Assert.Equal(typeof(GrasshopperScriptGlobals), propGlobals.PropertyType);
    }

    [Fact]
    public void RoslynScriptCompilation_WithScriptGlobals_CompilesCleanly()
    {
        var options = ScriptOptions.Default
            .WithReferences(typeof(GrasshopperScriptGlobals).Assembly, typeof(ScriptArgs).Assembly)
            .WithImports("System", "HPRebar.McpBridge.Core.Scripting");

        const string code = @"
            log(""computing sum..."");
            int valA = args.Int(""a"");
            int valB = args.Int(""b"");
            bool isDry = dryRun;
            return $""Sum={valA + valB}, DryRun={isDry}"";
        ";

        var script = CSharpScript.Create<string>(code, options, typeof(GrasshopperScriptGlobals));
        var diagnostics = script.Compile();

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void RoslynScriptCompilation_AccessingAllGlobalsMembers_CompilesCleanly()
    {
        var hostRefs = ResolveHostMetadataReferences();
        Assert.NotNull(hostRefs);

        var options = ScriptOptions.Default
            .WithReferences(
                typeof(GrasshopperScriptGlobals).Assembly,
                typeof(ScriptArgs).Assembly,
                typeof(CancellationToken).Assembly)
            .AddReferences(hostRefs)
            .WithImports("System", "System.Threading", "HPRebar.McpBridge.Core.Scripting");

        const string code = @"
            if (doc != null) { }
            if (canvas != null) { }
            if (rhinoDoc != null) { }
            if (revitDoc != null) { }
            if (uidoc != null) { }
            if (uiapp != null) { }
            ct.ThrowIfCancellationRequested();
            log(""hello"");
            progress(1, 10, ""status"");
            var a = args.Str(""key"");
            bool d = dryRun;
            var g = globals;
            return true;
        ";

        var script = CSharpScript.Create<bool>(code, options, typeof(GrasshopperScriptGlobals));
        var diagnostics = script.Compile();

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    private static List<MetadataReference>? ResolveHostMetadataReferences()
    {
        var revitDir = Environment.GetEnvironmentVariable("HPREBAR_REVIT_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "Revit 2026");
        var revitApiDll = Path.Combine(revitDir, "RevitAPI.dll");
        var revitApiUiDll = Path.Combine(revitDir, "RevitAPIUI.dll");

        if (!File.Exists(revitApiDll) || !File.Exists(revitApiUiDll)) return null;

        var list = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(revitApiDll),
            MetadataReference.CreateFromFile(revitApiUiDll)
        };

        var rhinoDir = Environment.GetEnvironmentVariable("HPGRASSHOPPER_RHINO_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino 8");
        var rhinoCommon = Path.Combine(rhinoDir, "System", "netcore", "RhinoCommon.dll");
        if (File.Exists(rhinoCommon)) list.Add(MetadataReference.CreateFromFile(rhinoCommon));

        var ghDir = Path.Combine(rhinoDir, "Plug-ins", "Grasshopper");
        var ghDll = Path.Combine(ghDir, "Grasshopper.dll");
        if (File.Exists(ghDll)) list.Add(MetadataReference.CreateFromFile(ghDll));

        return list;
    }
}
