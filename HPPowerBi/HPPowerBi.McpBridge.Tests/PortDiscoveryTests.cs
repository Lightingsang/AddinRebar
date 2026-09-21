using System;
using System.IO;
using System.Text;
using HPPowerBi.McpBridge.Discovery;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class PortDiscoveryTests : IDisposable
{
    private readonly string _tempRoot;

    public PortDiscoveryTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "HPPowerBi_Test_Workspaces_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
        catch { }
    }

    [Fact]
    public void ReadPortFile_ValidUtf16Le_ReturnsPort()
    {
        var filePath = Path.Combine(_tempRoot, "msmdsrv.port.txt");
        File.WriteAllText(filePath, "51234\r\n", Encoding.Unicode);

        var port = AnalysisServicesPortFinder.ReadPortFile(filePath);

        Assert.Equal(51234, port);
    }

    [Fact]
    public void ReadPortFile_WithNullCharsAndWhitespace_ParsesCleanly()
    {
        var filePath = Path.Combine(_tempRoot, "msmdsrv.port.txt");
        File.WriteAllText(filePath, "\0\0 49152 \0\r\n", Encoding.Unicode);

        var port = AnalysisServicesPortFinder.ReadPortFile(filePath);

        Assert.Equal(49152, port);
    }

    [Fact]
    public void ReadPortFile_InvalidPortNumber_ThrowsFormatException()
    {
        var filePath = Path.Combine(_tempRoot, "msmdsrv.port.txt");
        File.WriteAllText(filePath, "NotAPort", Encoding.Unicode);

        Assert.Throws<FormatException>(() => AnalysisServicesPortFinder.ReadPortFile(filePath));
    }

    [Fact]
    public void ReadPortFile_OutOfRangePort_ThrowsInvalidDataException()
    {
        var filePath = Path.Combine(_tempRoot, "msmdsrv.port.txt");
        File.WriteAllText(filePath, "80", Encoding.Unicode); // Port < 1024

        Assert.Throws<InvalidDataException>(() => AnalysisServicesPortFinder.ReadPortFile(filePath));
    }

    [Fact]
    public void FindAllActivePorts_MultipleWorkspaces_FindsAllValid()
    {
        var ws1 = Path.Combine(_tempRoot, "AnalysisServicesWorkspace_abc1");
        var data1 = Path.Combine(ws1, "Data");
        Directory.CreateDirectory(data1);
        File.WriteAllText(Path.Combine(data1, "msmdsrv.port.txt"), "50001", Encoding.Unicode);

        var ws2 = Path.Combine(_tempRoot, "AnalysisServicesWorkspace_def2");
        var data2 = Path.Combine(ws2, "Data");
        Directory.CreateDirectory(data2);
        File.WriteAllText(Path.Combine(data2, "msmdsrv.port.txt"), "50002", Encoding.Unicode);

        var ports = AnalysisServicesPortFinder.FindAllActivePorts(_tempRoot);

        Assert.Equal(2, ports.Count);
        Assert.Contains(ports, p => p.port == 50001);
        Assert.Contains(ports, p => p.port == 50002);
    }

    [Fact]
    public void ExtractReportName_StripsPowerBiDesktopSuffix()
    {
        var name = PbiInstanceInfo.ExtractReportName("Sales Performance 2026 - Power BI Desktop");
        Assert.Equal("Sales Performance 2026", name);

        var untitled = PbiInstanceInfo.ExtractReportName(null);
        Assert.Equal("Untitled", untitled);
    }
}
