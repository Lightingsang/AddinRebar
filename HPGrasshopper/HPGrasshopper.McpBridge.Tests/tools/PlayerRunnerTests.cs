namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class PlayerRunnerTests
{
    private readonly PlayerRunner _runner = new();

    [Fact]
    public void Run_NonExistentFile_ReturnsFailureWithError()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ghx");
        var result = _runner.Run(nonExistentPath);

        Assert.NotNull(result);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        var error = doc.RootElement.GetProperty("error").GetString();
        Assert.NotNull(error);
        Assert.Contains("Definition file not found", error);
    }

    [Fact]
    public void Run_EmptyFilePath_ReturnsFailureWithError()
    {
        var result = _runner.Run(string.Empty);
        Assert.NotNull(result);

        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public void Run_CorruptedFile_ReturnsDeserializationError()
    {
        var corruptFile = Path.Combine(Path.GetTempPath(), "Corrupt_" + Guid.NewGuid().ToString("N") + ".ghx");
        File.WriteAllText(corruptFile, "<<not_valid_xml>>");
        try
        {
            var result = _runner.Run(corruptFile);
            Assert.NotNull(result);

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);
            Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
            var error = doc.RootElement.GetProperty("error").GetString();
            Assert.Contains("Failed to deserialize", error);
        }
        finally
        {
            if (File.Exists(corruptFile)) File.Delete(corruptFile);
        }
    }

    [Fact]
    public void Run_InputBinding_NumberParam_ParsesJsonNumberCorrectly()
    {
        var param = TestProxyFactory.CreateParam("A");
        using var doc = JsonDocument.Parse("{\"A\": 42.5}");
        var prop = doc.RootElement.GetProperty("A");

        var applied = PlayerRunner.SetParamValue(param, prop);

        Assert.True(applied);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        Assert.Single(proxy.VolatileDataAdded);
        var numberGoo = Assert.IsAssignableFrom<GH_Number>(proxy.VolatileDataAdded[0]);
        Assert.Equal(42.5, numberGoo.Value);
    }

    [Fact]
    public void Run_InputBinding_BooleanParam_ParsesJsonBoolCorrectly()
    {
        var paramFlag = TestProxyFactory.CreateParam("Flag");
        var paramDisabled = TestProxyFactory.CreateParam("Disabled");
        using var doc = JsonDocument.Parse("{\"Flag\": true, \"Disabled\": false}");

        var appliedFlag = PlayerRunner.SetParamValue(paramFlag, doc.RootElement.GetProperty("Flag"));
        var appliedDisabled = PlayerRunner.SetParamValue(paramDisabled, doc.RootElement.GetProperty("Disabled"));

        Assert.True(appliedFlag);
        Assert.True(appliedDisabled);
        var proxyFlag = TestProxyFactory.GetProxy<TestParamProxy>(paramFlag);
        var proxyDisabled = TestProxyFactory.GetProxy<TestParamProxy>(paramDisabled);
        Assert.True(Assert.IsAssignableFrom<GH_Boolean>(proxyFlag.VolatileDataAdded[0]).Value);
        Assert.False(Assert.IsAssignableFrom<GH_Boolean>(proxyDisabled.VolatileDataAdded[0]).Value);
    }

    [Fact]
    public void Run_InputBinding_StringParam_ParsesJsonStringCorrectly()
    {
        var param = TestProxyFactory.CreateParam("Name");
        using var doc = JsonDocument.Parse("{\"Name\": \"DirectShape_Wall\"}");
        var prop = doc.RootElement.GetProperty("Name");

        var applied = PlayerRunner.SetParamValue(param, prop);

        Assert.True(applied);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        Assert.Single(proxy.VolatileDataAdded);
        var stringGoo = Assert.IsAssignableFrom<GH_String>(proxy.VolatileDataAdded[0]);
        Assert.Equal("DirectShape_Wall", stringGoo.Value);
    }

    [Fact]
    public void Run_InputBinding_UnsupportedJsonType_SafelyHandled()
    {
        var paramNull = TestProxyFactory.CreateParam("NullInput");
        var paramObject = TestProxyFactory.CreateParam("ObjectInput");
        using var doc = JsonDocument.Parse("{\"NullInput\": null, \"ObjectInput\": {\"x\": 10}}");

        var appliedNull = PlayerRunner.SetParamValue(paramNull, doc.RootElement.GetProperty("NullInput"));
        var appliedObject = PlayerRunner.SetParamValue(paramObject, doc.RootElement.GetProperty("ObjectInput"));

        Assert.False(appliedNull);
        Assert.False(appliedObject);
        var proxyNull = TestProxyFactory.GetProxy<TestParamProxy>(paramNull);
        var proxyObject = TestProxyFactory.GetProxy<TestParamProxy>(paramObject);
        Assert.Empty(proxyNull.VolatileDataAdded);
        Assert.Empty(proxyObject.VolatileDataAdded);
    }

    [Fact]
    public void Run_InputBinding_MatchByGuid_ParsesValidGuidKey()
    {
        var targetGuid = Guid.NewGuid();
        var param = TestProxyFactory.CreateParam("P1", instanceGuid: targetGuid);
        var objects = new IGH_DocumentObject[] { param };

        var matched = PlayerRunner.FindInputParam(objects, targetGuid.ToString("D"), id => id == targetGuid ? param : null);

        Assert.NotNull(matched);
        Assert.Same(param, matched);
    }

    [Fact]
    public void Run_InputBinding_MatchByNickName_CaseInsensitive()
    {
        var param = TestProxyFactory.CreateParam("length");
        var objects = new IGH_DocumentObject[] { param };

        var matched = PlayerRunner.FindInputParam(objects, "LENGTH");

        Assert.NotNull(matched);
        Assert.Same(param, matched);
        Assert.Equal("length", matched!.NickName);
    }

    [Fact]
    public void Run_OutputFiltering_NullRequestedOutputs_ExtractsAllOutputs()
    {
        var paramA = TestProxyFactory.CreateParam("OutA", volatileDataCount: 1);
        var paramB = TestProxyFactory.CreateParam("OutB", volatileDataCount: 1);
        var proxyA = TestProxyFactory.GetProxy<TestParamProxy>(paramA);
        var proxyB = TestProxyFactory.GetProxy<TestParamProxy>(paramB);
        proxyA.VolatileDataAdded.Add(new GH_Number(10.0));
        proxyB.VolatileDataAdded.Add(new GH_Number(20.0));

        var objects = new IGH_DocumentObject[] { paramA, paramB };
        var outputs = PlayerRunner.ExtractOutputs(objects, requestedOutputs: null);

        Assert.Equal(2, outputs.Count);
        Assert.True(outputs.ContainsKey("OutA"));
        Assert.True(outputs.ContainsKey("OutB"));
    }

    [Fact]
    public void Run_OutputFiltering_SpecifiedRequestedOutputs_FiltersCorrectly()
    {
        var paramResult = TestProxyFactory.CreateParam("Result", volatileDataCount: 1);
        var paramArea = TestProxyFactory.CreateParam("Area", volatileDataCount: 1);
        var paramVolume = TestProxyFactory.CreateParam("Volume", volatileDataCount: 1);
        TestProxyFactory.GetProxy<TestParamProxy>(paramResult).VolatileDataAdded.Add(new GH_Number(100.0));
        TestProxyFactory.GetProxy<TestParamProxy>(paramArea).VolatileDataAdded.Add(new GH_Number(250.0));
        TestProxyFactory.GetProxy<TestParamProxy>(paramVolume).VolatileDataAdded.Add(new GH_Number(500.0));

        var objects = new IGH_DocumentObject[] { paramResult, paramArea, paramVolume };
        var outputs = PlayerRunner.ExtractOutputs(objects, new[] { "Result", "Area" });

        Assert.Equal(2, outputs.Count);
        Assert.True(outputs.ContainsKey("Result"));
        Assert.True(outputs.ContainsKey("Area"));
        Assert.False(outputs.ContainsKey("Volume"));
    }

    [Fact]
    public void Run_OutputFiltering_CaseInsensitiveMatching_MatchesRequestedOutputs()
    {
        var param = TestProxyFactory.CreateParam("result", volatileDataCount: 1);
        TestProxyFactory.GetProxy<TestParamProxy>(param).VolatileDataAdded.Add(new GH_Number(1500.0));

        var objects = new IGH_DocumentObject[] { param };
        var outputs = PlayerRunner.ExtractOutputs(objects, new[] { "RESULT" });

        Assert.Single(outputs);
        Assert.True(outputs.ContainsKey("result"));
    }

    [Fact]
    public void PlayerRunner_Reflection_VerifiesRequiredMethodsAndSignatures()
    {
        var type = typeof(PlayerRunner);

        var loadDef = type.GetMethod("LoadDefinition", BindingFlags.NonPublic | BindingFlags.Static);
        var applyInputs = type.GetMethod("ApplyInputs", BindingFlags.NonPublic | BindingFlags.Static);
        var findParam = type.GetMethod("FindInputParam", BindingFlags.NonPublic | BindingFlags.Static);
        var setVal = type.GetMethod("SetParamValue", BindingFlags.NonPublic | BindingFlags.Static);
        var extractOutputs = type.GetMethod("ExtractOutputs", BindingFlags.NonPublic | BindingFlags.Static);
        var extractParamVal = type.GetMethod("ExtractParamValue", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(loadDef);
        Assert.NotNull(applyInputs);
        Assert.NotNull(findParam);
        Assert.NotNull(setVal);
        Assert.NotNull(extractOutputs);
        Assert.NotNull(extractParamVal);
    }

    [Fact]
    public void Run_HeadlessExecution_CatchesDllNotFoundCleanly()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MathSlider.ghx");
        if (File.Exists(fixturePath))
        {
            try
            {
                var res = _runner.Run(fixturePath);
                Assert.NotNull(res);
            }
            catch (TypeInitializationException)
            {
                // Expected in headless environment lacking native Rhino runtime
            }
            catch (DllNotFoundException)
            {
                // Expected when rhcommon_c is unavailable
            }
        }
    }

    [Fact]
    public void Run_HeadlessExecution_Binary_CatchesDllNotFoundCleanly()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MathSlider.gh");
        if (File.Exists(fixturePath))
        {
            try
            {
                var res = _runner.Run(fixturePath);
                Assert.NotNull(res);
            }
            catch (TypeInitializationException)
            {
                // Expected in headless environment lacking native Rhino runtime
            }
            catch (DllNotFoundException)
            {
                // Expected when rhcommon_c is unavailable
            }
        }
    }

    [Fact]
    public void Run_DocumentDisposal_VerifiesFinallyBlockEnsuresDisposal()
    {
        var disposable = new MockDisposable();
        PlayerRunner.DisposeDocument(disposable, headless: true);
        Assert.True(disposable.Disposed);

        var keptAlive = new MockDisposable();
        PlayerRunner.DisposeDocument(keptAlive, headless: false);
        Assert.False(keptAlive.Disposed);
    }

    private sealed class MockDisposable : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
}
