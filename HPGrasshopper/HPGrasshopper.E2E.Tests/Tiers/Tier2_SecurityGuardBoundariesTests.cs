namespace HPGrasshopper.E2E.Tests.Tiers;

using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using Xunit;

public sealed class Tier2_SecurityGuardBoundariesTests
{
    [Fact]
    public void TC_T2_GRD_01_GlobalAliasNamespaceBypassNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "global::System.IO.File.WriteAllText(\"a.txt\", \"b\");";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.IO"));
    }

    [Fact]
    public void TC_T2_GRD_02_HashRHashLoadPreprocessorDirectivesNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        var code1 = "#r \"RevitAPI.dll\"";
        var code2 = "#load \"helper.csx\"";

        var quality1 = bridge.ValidateScript(code1);
        var quality2 = bridge.ValidateScript(code2);

        Assert.False(quality1.IsValid);
        Assert.False(quality2.IsValid);
        Assert.Contains(quality1.Errors, e => e.Contains("#r"));
        Assert.Contains(quality2.Errors, e => e.Contains("#load"));
    }

    [Fact]
    public void TC_T2_GRD_03_HostProcessTerminationCallsDenial()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Diagnostics.Process.GetCurrentProcess().Kill();";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Diagnostics"));
    }

    [Fact]
    public void TC_T2_GRD_04_DocumentDisposeDenial()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Reflection.Assembly.GetExecutingAssembly();";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Reflection"));
    }

    [Fact]
    public void TC_T2_GRD_05_BridgeInternalNamespaceTamperingDenial()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Net.Http.HttpClient client = null;";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Net"));
    }

    [Fact]
    public void TC_T2_GRD_06_ReflectionEmitCodeGenerationDenial()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Reflection.Emit.DynamicMethod dyn = null;";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Reflection"));
    }

    [Fact]
    public void TC_T2_GRD_07_NetworkSocketsNamespaceDenial()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Net.Sockets.Socket socket = null;";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Net"));
    }

    [Fact]
    public void TC_T2_GRD_08_UnannotatedEmptyCatchBlockRejection()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "try { int x = 1; } catch { }";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("empty catch", StringComparison.OrdinalIgnoreCase) || e.Contains("Q-B2"));
    }

    [Fact]
    public void TC_T2_GRD_09_CommentedOutCodeRejection()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "// var x = doc.Objects;";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("commented-out", StringComparison.OrdinalIgnoreCase) || e.Contains("Q-B1"));
    }

    [Fact]
    public void TC_T2_GRD_10_LegitimateProseCommentsAllowance()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "// calculate beam reinforcement parameters\nint span = 6000;";

        var quality = bridge.ValidateScript(code);
        Assert.True(quality.IsValid);
        Assert.Empty(quality.Errors);
    }
}
