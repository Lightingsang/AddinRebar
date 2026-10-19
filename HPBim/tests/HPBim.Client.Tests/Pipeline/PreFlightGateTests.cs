namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class PreFlightGateTests
{
    [Fact]
    public void BimPlanRunnerScriptTemplate_Preamble_ContainsPreFlightGateCode()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("var docVer = Document.GetDocumentVersion(doc);", script);
        Assert.Contains("[PRE_FLIGHT_FAILED]", script);
        Assert.Contains("targetDoc.Str(\"versionGuid\"", script);
    }

    [Fact]
    public async Task PreFlightGate_WhenTitleAndVersionGuidMatch_ExecutionProceeds()
    {
        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = req.DryRun,
            Value = JsonDocument.Parse("{\"total\": 1, \"created\": 1, \"records\": [{\"sourceKey\":\"[MyProject]#[Column]:C1\",\"mark\":\"C1\",\"category\":\"Column\",\"status\":\"Created\",\"targetUniqueId\":\"uid-1\"}]}").RootElement
        };

        var engine = new BimPipelineEngine(fakeClient);
        var expectedGuid = Guid.NewGuid();
        var targetDoc = new DocumentIdentitySnapshot
        {
            Title = "MyProject.rvt",
            VersionGuid = expectedGuid
        };

        var plan = new BimExecutionPlan
        {
            TargetDoc = targetDoc,
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("MyProject", CoordinationCategory.Column, "C1"),
                    Category = CoordinationCategory.Column,
                    Mark = "C1",
                    TargetLevelName = "Level 1"
                }
            }
        };

        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task PreFlightGate_WhenTitleMismatches_ThrowsPreFlightFailedAndRollsBack()
    {
        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = true,
            RolledBack = true,
            Message = "InvalidOperationException: [PRE_FLIGHT_FAILED] Target document mismatch: expected 'ExpectedModel.rvt' (VersionGUID 00000000-0000-0000-0000-000000000000), but active document is 'ActiveModel.rvt' (VersionGUID 00000000-0000-0000-0000-000000000000)"
        };

        var engine = new BimPipelineEngine(fakeClient);
        var plan = new BimExecutionPlan
        {
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "ExpectedModel.rvt",
                VersionGuid = Guid.Empty
            },
            Items = Array.Empty<ElementPlanItem>()
        };

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Contains("[PRE_FLIGHT_FAILED]", result.ErrorMessage);
        Assert.Contains("ExpectedModel.rvt", result.ErrorMessage);
    }

    [Fact]
    public async Task PreFlightGate_WhenVersionGuidMismatches_ThrowsPreFlightFailedAndRollsBack()
    {
        var expectedGuid = Guid.NewGuid();
        var actualGuid = Guid.NewGuid();

        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = true,
            RolledBack = true,
            Message = $"InvalidOperationException: [PRE_FLIGHT_FAILED] Target document mismatch: expected 'Model.rvt' (VersionGUID {expectedGuid:D}), but active document is 'Model.rvt' (VersionGUID {actualGuid:D})"
        };

        var engine = new BimPipelineEngine(fakeClient);
        var plan = new BimExecutionPlan
        {
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Model.rvt",
                VersionGuid = expectedGuid
            },
            Items = Array.Empty<ElementPlanItem>()
        };

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Contains("[PRE_FLIGHT_FAILED]", result.ErrorMessage);
        Assert.Contains(expectedGuid.ToString("D"), result.ErrorMessage);
    }

    [Fact]
    public async Task DocumentIdentityQuery_ParsesLiveSnapshotAccurately()
    {
        var vGuid = Guid.NewGuid();
        var cGuid = Guid.NewGuid();

        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            Value = JsonDocument.Parse($$"""
            {
                "title": "NhaPho_5x18m.rvt",
                "pathName": "C:\\Models\\NhaPho_5x18m.rvt",
                "versionGuid": "{{vGuid:D}}",
                "numberOfSaves": 42,
                "centralGuid": "{{cGuid:D}}",
                "activeViewName": "Level 1"
            }
            """).RootElement
        };

        var query = new DocumentIdentityQuery(fakeClient);
        var snapshot = await query.QueryActiveDocumentAsync();

        Assert.Equal("NhaPho_5x18m.rvt", snapshot.Title);
        Assert.Equal("C:\\Models\\NhaPho_5x18m.rvt", snapshot.PathName);
        Assert.Equal(vGuid, snapshot.VersionGuid);
        Assert.Equal(42, snapshot.NumberOfSaves);
        Assert.Equal(cGuid, snapshot.CentralGuid);
        Assert.Equal("Level 1", snapshot.ActiveViewName);
        Assert.True(snapshot.IsWorkshared);
        Assert.False(snapshot.IsEmpty);
    }

    [Fact]
    public async Task DocumentIdentityQuery_WhenBridgeFails_ThrowsRevitPipeException()
    {
        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = true,
            Message = "No active document open in Revit."
        };

        var query = new DocumentIdentityQuery(fakeClient);

        await Assert.ThrowsAsync<RevitPipeException>(() => query.QueryActiveDocumentAsync());
    }
}
