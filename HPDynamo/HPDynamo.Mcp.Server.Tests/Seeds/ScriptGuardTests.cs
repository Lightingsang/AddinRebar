namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.Collections.Generic;
using System.Linq;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class ScriptGuardTests
{
    private static IReadOnlyList<SeedInstaller.SeedContent> Seeds() =>
        SeedInstaller.LoadSeeds(typeof(DynamoHostProfile).Assembly);

    public static IEnumerable<object[]> SeedData() =>
        Seeds().Select(s => new object[] { s.Category + "/" + s.Name });

    [Theory]
    [MemberData(nameof(SeedData))]
    public void AllSeeds_PassScriptGuardWithZeroViolations(string seedKey)
    {
        var seed = Seeds().Single(s => s.Category + "/" + s.Name == seedKey);
        var diagnostics = ScriptGuard.Check(seed.Code, GuardProfile.Dynamo);

        Assert.True(diagnostics.Count == 0,
            $"Seed '{seedKey}' has guard violations: {string.Join("; ", diagnostics.Select(d => d.Message))}");
    }

    [Fact]
    public void ScriptGuard_DeniesModalDialogs()
    {
        const string scriptWithMsgBox = "System.Windows.Forms.MessageBox.Show(\"Test\"); return 1;";
        var diagMsgBox = ScriptGuard.Check(scriptWithMsgBox, GuardProfile.Dynamo);
        Assert.NotEmpty(diagMsgBox);

        const string scriptWithTaskDialog = "TaskDialog.Show(\"Revit\", \"Test\"); return 1;";
        var diagTaskDialog = ScriptGuard.Check(scriptWithTaskDialog, GuardProfile.Dynamo);
        Assert.NotEmpty(diagTaskDialog);
    }

    [Fact]
    public void ScriptGuard_DeniesForbiddenNamespacesAndProcess()
    {
        const string scriptWithIo = "System.IO.File.Delete(\"test.txt\"); return 1;";
        var diagIo = ScriptGuard.Check(scriptWithIo, GuardProfile.Dynamo);
        Assert.NotEmpty(diagIo);

        const string scriptWithProc = "System.Diagnostics.Process.Start(\"calc.exe\"); return 1;";
        var diagProc = ScriptGuard.Check(scriptWithProc, GuardProfile.Dynamo);
        Assert.NotEmpty(diagProc);

        const string scriptWithNet = "var client = new System.Net.Http.HttpClient(); return 1;";
        var diagNet = ScriptGuard.Check(scriptWithNet, GuardProfile.Dynamo);
        Assert.NotEmpty(diagNet);
    }

    [Fact]
    public void ScriptGuard_DeniesScriptTriviaAndDirectives()
    {
        const string scriptWithRef = "#r \"some.dll\"\nreturn 1;";
        var diagRef = ScriptGuard.Check(scriptWithRef, GuardProfile.Dynamo);
        Assert.NotEmpty(diagRef);

        const string scriptWithLoad = "#load \"other.csx\"\nreturn 1;";
        var diagLoad = ScriptGuard.Check(scriptWithLoad, GuardProfile.Dynamo);
        Assert.NotEmpty(diagLoad);
    }

    [Fact]
    public void ScriptGuard_DeniesGlobalNamespaceBypass()
    {
        const string scriptWithGlobal = "global::System.IO.File.Delete(\"test.txt\"); return 1;";
        var diagGlobal = ScriptGuard.Check(scriptWithGlobal, GuardProfile.Dynamo);
        Assert.NotEmpty(diagGlobal);
    }
}
