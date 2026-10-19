namespace HPGrasshopper.E2E.Tests.Tiers;

using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Hosts;
using Xunit;

public sealed class Tier1_HostProfileAndSecurityTests
{
    [Fact]
    public void TC_T1_GRD_01_HostProfileMetadataInvariants()
    {
        Assert.Equal("grasshopper", PipeNaming.GrasshopperHost);
        Assert.Equal(2026, HostProfile.Grasshopper.DefaultVersion);
        Assert.Equal("hpgrasshopper-mcp-2026", PipeNaming.For(HostProfile.Grasshopper.HostId, HostProfile.Grasshopper.DefaultVersion));
    }

    [Fact]
    public void TC_T1_GRD_02_LegitimateRhinoGrasshopperApiAllowance()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "var pt = new Rhino.Geometry.Point3d(0, 0, 0); var docName = doc.DisplayName;";

        var quality = bridge.ValidateScript(code);
        Assert.True(quality.IsValid);
        Assert.Empty(quality.Errors);
    }

    [Fact]
    public void TC_T1_GRD_03_SystemIoNamespaceDenial()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.IO.File.Delete(\"test.txt\");";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.IO"));
    }

    [Fact]
    public void TC_T1_GRD_04_SystemDiagnosticsProcessDenial()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Diagnostics.Process.Start(\"calc.exe\");";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Diagnostics.Process"));
    }

    [Fact]
    public void TC_T1_GRD_05_UiPopupsDenial()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Windows.Forms.MessageBox.Show(\"Hello\");";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Windows.Forms"));
    }
}
