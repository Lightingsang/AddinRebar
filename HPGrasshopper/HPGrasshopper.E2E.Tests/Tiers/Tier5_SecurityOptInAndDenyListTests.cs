namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

public sealed class Tier5_SecurityOptInAndDenyListTests
{
    [Fact]
    public async Task TC_T5_SEC_01_ExecutionDisabled_ReturnsMinus32001()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = false };
        var req = new GrasshopperExecutionRequest("return 1;");

        var res = await bridge.ExecuteAsync(req);
        Assert.False(res.Success);
        Assert.Equal(-32001, res.ErrorCode);
        Assert.Contains(GrasshopperE2EContracts.OptInDisabledMessageFragment, res.ErrorMessage);
    }

    [Fact]
    public void TC_T5_SEC_02_NonExecutionMethods_PermittedWhenDisabled()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = false };
        var ctx = bridge.GetContext();

        Assert.True(ctx.IsConnected);
        Assert.False(ctx.IsExecutionEnabled);
    }

    [Fact]
    public void TC_T5_SEC_03_RapidOptInStateFlipping_ZeroLeakage()
    {
        var bridge = new GrasshopperE2EBridge();
        for (int i = 0; i < 100; i++)
        {
            bridge.IsExecutionEnabled = (i % 2 == 1);
            Assert.Equal(i % 2 == 1, bridge.IsExecutionEnabled);
        }
    }

    [Fact]
    public void TC_T5_SEC_04_ForbiddenNamespace_SystemIO_Blocked()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.IO.File.Exists(\"test\");";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.IO"));
    }

    [Fact]
    public void TC_T5_SEC_05_ForbiddenNamespace_SystemDiagnostics_Blocked()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Diagnostics.Process.GetProcesses();";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Diagnostics"));
    }

    [Fact]
    public void TC_T5_SEC_06_DenyListEvasion_GlobalAlias_Blocked()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "global::System.IO.Directory.GetFiles(\".\");";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.IO"));
    }

    [Fact]
    public void TC_T5_SEC_07_DenyListEvasion_ReflectionEmit_Blocked()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "System.Reflection.Assembly a = null;";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Reflection"));
    }

    [Fact]
    public async Task TC_T5_SEC_08_MalformedJsonRpc_SafetyRejection()
    {
        var bridge = new GrasshopperE2EBridge();
        await using var simulator = new GrasshopperPipeSimulator(bridge: bridge);
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(2000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        // Send invalid JSON line over named pipe to verify RequestDispatcher error response
        await writer.WriteLineAsync("{ invalid_json }");
        var responseLine = await reader.ReadLineAsync();

        Assert.NotNull(responseLine);
        var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(responseLine);
        Assert.NotNull(envelope);
        Assert.Equal(JsonRpcKind.Response, envelope.Kind);
        Assert.NotNull(envelope.Error);
        Assert.Equal(BridgeErrorCode.ParseError, envelope.Error.Code);
        Assert.Equal(-32700, envelope.Error.Code);
        Assert.Contains("JSON-RPC", envelope.Error.Message);
    }
}
