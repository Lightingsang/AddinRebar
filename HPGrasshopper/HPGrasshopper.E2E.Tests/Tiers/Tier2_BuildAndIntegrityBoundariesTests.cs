namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Linq;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class Tier2_BuildAndIntegrityBoundariesTests
{
    [Fact]
    public void TC_T2_BLD_01_ZeroDisallowedNativeAssemblyReferences()
    {
        var profile = GuardProfile.Grasshopper;
        Assert.NotNull(profile);
        var badCode = "System.IO.File.Delete(\"test.txt\");";
        var diags = ScriptGuard.Check(badCode, profile);
        Assert.NotEmpty(diags);
        Assert.Contains(diags, d => d.Message.Contains("System.IO"));
    }

    [Fact]
    public void TC_T2_BLD_02_ZeroNativeDependenciesInE2eTestSuite()
    {
        var asm = typeof(Tier2_BuildAndIntegrityBoundariesTests).Assembly;
        var refNames = asm.GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.DoesNotContain(refNames, n => n.StartsWith("Autodesk", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refNames, n => n.StartsWith("Rhino", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refNames, n => n.StartsWith("Grasshopper", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refNames, n => n.StartsWith("HPRebar.Mcp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TC_T2_BLD_03_StrictRuleC2ComplianceBoundary()
    {
        var bridge = new GrasshopperE2EBridge();
        var code300 = string.Join("\n", Enumerable.Repeat("int x = 1;", 300));
        var result300 = bridge.ValidateScript(code300);
        Assert.True(result300.IsValid);

        var code301 = string.Join("\n", Enumerable.Repeat("int x = 1;", 301));
        var result301 = bridge.ValidateScript(code301);
        Assert.False(result301.IsValid);
        Assert.Contains(result301.Errors, e => e.Contains("300") || e.Contains("Q-B3"));
    }

    [Fact]
    public void TC_T2_BLD_04_StrictRuleM3ComplianceBoundary()
    {
        const int maxRuleM3Lines = 50;
        var bridge = new GrasshopperE2EBridge();
        var smallBlock = "int a = 1;\nint b = 2;\nreturn a + b;";
        var res = bridge.ValidateScript(smallBlock);
        Assert.True(res.IsValid);
        Assert.True(res.LineCount <= maxRuleM3Lines);
    }

    [Fact]
    public void TC_T2_BLD_05_HostProfileDefaultClampingInvariant()
    {
        var profile = HostProfile.Grasshopper;
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Equal(600, HostScriptContracts.GrasshopperHeavyMaxTimeoutSeconds);
        Assert.True(profile.MaxTimeoutSeconds <= 600);
    }
}
