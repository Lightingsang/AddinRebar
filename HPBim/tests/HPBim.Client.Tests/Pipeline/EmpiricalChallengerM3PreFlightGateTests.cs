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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

/// <summary>
/// Adversarial empirical tests challenging Feature F7 (Pre-Flight Gate):
/// Verifies title mismatch, version mismatch, matching identity, casing tolerance,
/// empty snapshot bypass, and unconditional transaction rollback with [PRE_FLIGHT_FAILED].
/// </summary>
public sealed class EmpiricalChallengerM3PreFlightGateTests
{
    [Fact]
    public void PreFlightScript_Syntax_CompilesWithZeroRoslynErrors()
    {
        string scriptCode = BimPlanRunnerScriptTemplate.GetScriptCode();
        Assert.False(string.IsNullOrWhiteSpace(scriptCode));

        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            kind: SourceCodeKind.Script);

        var tree = CSharpSyntaxTree.ParseText(scriptCode, parseOptions);
        var errors = tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Challenge_TitleMismatch_MustTriggerRollbackAndPreFlightFailed()
    {
        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req =>
        {
            var elem = req.Args ?? default;
            string expectedTitle = elem.TryGetProperty("targetDoc", out var td) && td.TryGetProperty("title", out var tp) ? tp.GetString() ?? "" : "";
            const string actualActiveTitle = "ActiveHospital.rvt";

            if (!string.Equals(actualActiveTitle, expectedTitle, StringComparison.OrdinalIgnoreCase))
            {
                return new ExecuteResult
                {
                    IsError = true,
                    RolledBack = true,
                    Message = $"InvalidOperationException: [PRE_FLIGHT_FAILED] Target document mismatch: expected '{expectedTitle}', but active document is '{actualActiveTitle}'"
                };
            }

            return new ExecuteResult { IsError = false, RolledBack = req.DryRun };
        };

        var engine = new BimPipelineEngine(fakeClient);
        var plan = CreatePlan("ResidentialTower.rvt", Guid.Empty);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("[PRE_FLIGHT_FAILED]", result.ErrorMessage);
        Assert.Contains("ResidentialTower.rvt", result.ErrorMessage);
        Assert.Equal(0, result.CreatedCount);
    }

    [Fact]
    public async Task Challenge_VersionGuidMismatch_MustTriggerRollbackAndPreFlightFailed()
    {
        var expectedGuid = Guid.NewGuid();
        var actualGuid = Guid.NewGuid();

        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req =>
        {
            var elem = req.Args ?? default;
            string expectedGuidStr = elem.TryGetProperty("targetDoc", out var td) && td.TryGetProperty("versionGuid", out var vg) ? vg.GetString() ?? "" : "";
            Guid.TryParse(expectedGuidStr, out var parsedGuid);

            if (parsedGuid != actualGuid)
            {
                return new ExecuteResult
                {
                    IsError = true,
                    RolledBack = true,
                    Message = $"InvalidOperationException: [PRE_FLIGHT_FAILED] Target document mismatch: expected VersionGUID {parsedGuid:D}, but active document is {actualGuid:D}"
                };
            }

            return new ExecuteResult { IsError = false, RolledBack = req.DryRun };
        };

        var engine = new BimPipelineEngine(fakeClient);
        var plan = CreatePlan("Model.rvt", expectedGuid);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("[PRE_FLIGHT_FAILED]", result.ErrorMessage);
        Assert.Contains(expectedGuid.ToString("D"), result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_MatchingIdentity_ProceedsNormallyWithoutRollbackInApply()
    {
        var guid = Guid.NewGuid();
        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req =>
        {
            var elem = req.Args ?? default;
            string expectedTitle = elem.TryGetProperty("targetDoc", out var td) && td.TryGetProperty("title", out var tp) ? tp.GetString() ?? "" : "";
            string expectedGuidStr = td.TryGetProperty("versionGuid", out var vg) ? vg.GetString() ?? "" : "";

            Assert.Equal("TargetModel.rvt", expectedTitle);
            Assert.Equal(guid.ToString("D"), expectedGuidStr);

            return new ExecuteResult
            {
                IsError = false,
                RolledBack = req.DryRun,
                Value = JsonDocument.Parse("{\"total\": 1, \"created\": 1, \"records\": [{\"sourceKey\":\"[Doc]#[Column]:C1\",\"mark\":\"C1\",\"category\":\"Column\",\"status\":\"Created\",\"targetUniqueId\":\"uid-1\"}]}").RootElement
            };
        };

        var engine = new BimPipelineEngine(fakeClient);
        var plan = CreatePlan("TargetModel.rvt", guid);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.False(result.RolledBack);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Challenge_TitleCasingDifference_TreatedAsMatchingIdentity()
    {
        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req =>
        {
            var elem = req.Args ?? default;
            string expectedTitle = elem.TryGetProperty("targetDoc", out var td) && td.TryGetProperty("title", out var tp) ? tp.GetString() ?? "" : "";
            const string activeTitle = "PROJECT_OFFICE.RVT";

            bool matches = string.Equals(activeTitle, expectedTitle, StringComparison.OrdinalIgnoreCase);
            if (!matches)
            {
                return new ExecuteResult { IsError = true, RolledBack = true, Message = "[PRE_FLIGHT_FAILED]" };
            }

            return new ExecuteResult
            {
                IsError = false,
                RolledBack = req.DryRun,
                Value = JsonDocument.Parse("{\"total\": 1, \"created\": 1, \"records\": [{\"sourceKey\":\"[Doc]#[Column]:C1\",\"mark\":\"C1\",\"category\":\"Column\",\"status\":\"Created\",\"targetUniqueId\":\"uid-1\"}]}").RootElement
            };
        };

        var engine = new BimPipelineEngine(fakeClient);
        var plan = CreatePlan("project_office.rvt", Guid.Empty);

        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("Model.rvt", "Different.rvt", false)]
    [InlineData("Model.rvt", "MODEL.RVT", true)]
    [InlineData("", "AnyModel.rvt", true)]
    public void Challenge_DocumentIdentitySnapshot_MatchesDomainLogic(
        string snapshotTitle,
        string actualTitle,
        bool shouldMatch)
    {
        var snapshot = new DocumentIdentitySnapshot
        {
            Title = snapshotTitle,
            VersionGuid = Guid.Empty
        };

        bool actualMatch = snapshot.Matches(actualTitle, Guid.NewGuid());
        Assert.Equal(shouldMatch, actualMatch);
    }

    [Fact]
    public void Challenge_DocumentIdentitySnapshot_VersionGuidMatchesDomainLogic()
    {
        var targetGuid = Guid.NewGuid();
        var snapshot = new DocumentIdentitySnapshot
        {
            Title = string.Empty,
            VersionGuid = targetGuid
        };

        Assert.True(snapshot.Matches("Any.rvt", targetGuid));
        Assert.False(snapshot.Matches("Any.rvt", Guid.NewGuid()));
        Assert.False(snapshot.Matches("Any.rvt", Guid.Empty));
    }

    [Fact]
    public async Task Challenge_EmptySnapshot_BypassesPreFlightCheck()
    {
        var fakeClient = new FakeRevitPipeClient();
        fakeClient.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            RolledBack = req.DryRun,
            Value = JsonDocument.Parse("{\"total\": 0, \"records\": []}").RootElement
        };

        var engine = new BimPipelineEngine(fakeClient);
        var emptySnapshot = new DocumentIdentitySnapshot();
        Assert.True(emptySnapshot.IsEmpty);

        var plan = new BimExecutionPlan
        {
            TargetDoc = emptySnapshot,
            Items = Array.Empty<ElementPlanItem>()
        };

        var result = await engine.ExecutePlanAsync(plan);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    private static BimExecutionPlan CreatePlan(string title, Guid versionGuid) =>
        new()
        {
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = title,
                VersionGuid = versionGuid
            },
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
                    Category = CoordinationCategory.Column,
                    Mark = "C1",
                    TargetLevelName = "Level 1"
                }
            },
            Options = new ExecutionOptions { DryRun = false }
        };
}
