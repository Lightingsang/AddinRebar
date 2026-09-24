using System.IO;
using System.Text.Json;
using HPEtabs.McpBridge.Service;
using HPRebar.Mcp.Contracts.JsonRpc;
using Xunit;

namespace HPEtabs.McpBridge.Tests;

public sealed class EtabsConnectionTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "hpetabs-conn-test-" + Guid.NewGuid().ToString("N"));

    public EtabsConnectionTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void Connection_config_defaults_have_autostart_and_prefer_existing_instance()
    {
        var config = new EtabsConnectionConfig();

        Assert.True(config.AutoStart);
        Assert.True(config.PreferExistingInstance);
        Assert.True(config.StartVisible);
        Assert.Equal(60, config.StartupTimeoutSeconds);
        Assert.Null(config.EtabsExePath);
        Assert.Null(config.DefaultModelPath);
    }

    [Fact]
    public void Connection_config_persists_and_reloads_cleanly()
    {
        var config = new EtabsConnectionConfig
        {
            AutoStart = false,
            PreferExistingInstance = true,
            StartVisible = false,
            StartupTimeoutSeconds = 45,
            EtabsExePath = @"C:\Test\ETABS.exe",
            DefaultModelPath = @"C:\Test\Template.edb"
        };

        var vendor = "TestVendor_" + Guid.NewGuid().ToString("N");
        var product = "TestProduct";

        try
        {
            config.Save(vendor, product);
            var loaded = EtabsConnectionConfig.Load(vendor, product);

            Assert.False(loaded.AutoStart);
            Assert.True(loaded.PreferExistingInstance);
            Assert.False(loaded.StartVisible);
            Assert.Equal(45, loaded.StartupTimeoutSeconds);
            Assert.Equal(@"C:\Test\ETABS.exe", loaded.EtabsExePath);
            Assert.Equal(@"C:\Test\Template.edb", loaded.DefaultModelPath);
        }
        finally
        {
            var configPath = EtabsConnectionConfig.GetConfigPath(vendor, product);
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }

    [Fact]
    public void Connection_result_serializes_state_as_expected_json_string()
    {
        var result = new EtabsConnectionResult(
            EtabsConnectionState.started_new,
            "22.7.0",
            22.7,
            "Model1.EDB",
            12345,
            null,
            null);

        var json = JsonSerializer.Serialize(result);

        Assert.Contains("\"State\":\"started_new\"", json);
        Assert.Contains("\"Version\":\"22.7.0\"", json);
        Assert.Contains("\"Pid\":12345", json);

        var deserialized = JsonSerializer.Deserialize<EtabsConnectionResult>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(EtabsConnectionState.started_new, deserialized.State);
        Assert.Equal(12345, deserialized.Pid);
    }

    [Fact]
    public void EnsureConnected_returns_failed_when_autostart_disabled_and_no_process()
    {
        using var attachment = new EtabsAttachment
        {
            Config = new EtabsConnectionConfig
            {
                AutoStart = false,
                PreferExistingInstance = false
            }
        };

        var result = attachment.EnsureConnected();

        Assert.Equal(EtabsConnectionState.failed, result.State);
        Assert.Contains("AutoStart is disabled", result.ErrorMessage);
    }

    [Fact]
    public void IsHealthy_returns_false_when_not_attached()
    {
        using var attachment = new EtabsAttachment();

        Assert.False(attachment.Attached);
        Assert.False(attachment.IsHealthy());
    }

    [Fact]
    public void NotAttached_exception_contains_specified_reason()
    {
        var ex = EtabsAttachment.NotAttached("Custom reason for test");

        Assert.Equal(BridgeErrorCode.NoActiveDocument, ex.Code);
        Assert.Contains("Custom reason for test", ex.Message);
        Assert.Contains("click Attach", ex.Message);
    }

    [Fact]
    public void IsVisible_and_SetVisible_return_false_when_not_attached()
    {
        using var attachment = new EtabsAttachment();

        Assert.False(attachment.IsVisible);
        Assert.False(attachment.SetVisible(true));
        Assert.False(attachment.SetVisible(false));
    }
}
