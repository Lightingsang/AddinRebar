using System;
using System.IO;
using System.Text.Json;
using HPPowerBi.McpBridge.ExternalTools;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class ExternalToolsRegistrarTests : IDisposable
{
    private readonly string _tempDir;

    public ExternalToolsRegistrarTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_Test_ExtTools_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    [Fact]
    public void GenerateJson_ProducesValidExternalToolSchema()
    {
        var exePath = @"C:\Program Files\HPPowerBi\HPPowerBi.McpBridge.exe";
        var json = ExternalToolsRegistrar.GenerateJson(exePath);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("1.0", root.GetProperty("version").GetString());
        Assert.Equal(ExternalToolsRegistrar.ToolName, root.GetProperty("name").GetString());
        Assert.Equal("ExternalTool", root.GetProperty("operationType").GetString());
        Assert.Equal(exePath, root.GetProperty("executable").GetString());
        Assert.Equal(ExternalToolsRegistrar.ToolArguments, root.GetProperty("arguments").GetString());
    }

    [Fact]
    public void Register_InCustomDirectory_CreatesFileAndVerifiesRegistration()
    {
        var exePath = @"C:\TestPath\HPPowerBi.McpBridge.exe";
        var result = ExternalToolsRegistrar.Register(exePath, _tempDir);

        Assert.True(result.Success);
        Assert.True(File.Exists(result.FilePath));

        var isReg = ExternalToolsRegistrar.IsRegistered(exePath, _tempDir);
        Assert.True(isReg);

        var unreg = ExternalToolsRegistrar.Unregister(_tempDir);
        Assert.True(unreg);
        Assert.False(File.Exists(result.FilePath));
    }
}
