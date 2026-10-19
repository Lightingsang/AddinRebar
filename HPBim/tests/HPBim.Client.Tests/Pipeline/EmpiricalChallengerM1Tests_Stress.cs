namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Tests.Fixtures;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public sealed class EmpiricalChallengerM1Tests_Stress
{
    [Fact]
    public void Stress1_BimPlanRunnerScriptTemplate_HasZeroCSharpSyntaxErrors()
    {
        string code = BimPlanRunnerScriptTemplate.GetScriptCode();
        Assert.False(string.IsNullOrWhiteSpace(code), "Script code must not be empty.");

        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            kind: SourceCodeKind.Script);

        var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);
        var errors = syntaxTree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Stress2_AllCoordinationCategories_SerializeAsStringEnums_OnWire()
    {
        var allCategories = Enum.GetValues<CoordinationCategory>();
        foreach (var cat in allCategories)
        {
            var item = new ElementPlanItem
            {
                SourceKey = CompositeSourceKey.Create("Doc", cat, "TestMark"),
                Category = cat,
                Mark = "TestMark"
            };

            var plan = new BimExecutionPlan
            {
                SourceModelName = "TestModel",
                Items = new[] { item }
            };

            var jsonElem = BridgeJson.ToElement(plan);
            Assert.NotNull(jsonElem);

            var itemsArr = jsonElem.Value.GetProperty("items").EnumerateArray().ToList();
            Assert.Single(itemsArr);

            string catWire = itemsArr[0].GetProperty("category").GetString()!;
            Assert.Equal(cat.ToString(), catWire);
        }
    }

    [Fact]
    public void Stress3_ScriptTemplate_EradicatesAllHeuristicFirstSymbolFallbacks()
    {
        string scriptCode = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.DoesNotContain(".Values.FirstOrDefault()", scriptCode);
        Assert.DoesNotContain(".Values.First()", scriptCode);
        Assert.DoesNotContain(".FirstOrDefault(s =>", scriptCode);
        Assert.Contains("ERR_SYMBOL_NOT_FOUND", scriptCode);
        Assert.Contains("ERR_UNSUPPORTED_CATEGORY", scriptCode);
    }

    [Fact]
    public async Task Stress4_MixedBatch_FailsFast_WhenSingleElementFails()
    {
        var fakeResponseJson = """
        {
            "planId": "stress-plan",
            "total": 3,
            "created": 2,
            "updated": 0,
            "noOp": 0,
            "skipped": 0,
            "failed": 1,
            "records": [
                { "sourceKey": "[D]#[Column]:C1", "mark": "C1", "category": "Column", "status": "Created", "targetUniqueId": "uid-c1" },
                { "sourceKey": "[D]#[Column]:C2", "mark": "C2", "category": "Column", "status": "Created", "targetUniqueId": "uid-c2" },
                { "sourceKey": "[D]#[Beam]:B1", "mark": "B1", "category": "Beam", "status": "Failed",
                  "errorCode": "ERR_SYMBOL_NOT_FOUND", "errorReason": "Beam symbol not found" }
            ]
        }
        """;

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => new ExecuteResult
            {
                IsError = false,
                DurationMs = 100,
                Value = JsonDocument.Parse(fakeResponseJson).RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "stress-plan",
            Items = new[]
            {
                new ElementPlanItem { SourceKey = CompositeSourceKey.Parse("[D]#[Column]:C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 1 },
                new ElementPlanItem { SourceKey = CompositeSourceKey.Parse("[D]#[Column]:C2"), Category = CoordinationCategory.Column, Mark = "C2", SequenceOrder = 2 },
                new ElementPlanItem { SourceKey = CompositeSourceKey.Parse("[D]#[Beam]:B1"), Category = CoordinationCategory.Beam, Mark = "B1", SequenceOrder = 3 }
            }
        };

        var result = await engine.ExecutePlanAsync(plan);

        Assert.False(result.Success, "Pipeline must report Success=false when any element fails.");
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Execution failed for 1 element(s)", result.ErrorMessage);
        Assert.Contains("ERR_SYMBOL_NOT_FOUND", result.ErrorMessage);
    }

    [Fact]
    public void Stress5_PlanHash_IsSensitiveToModelGeometryAndDocChanges()
    {
        var modelA = NhaPho5x18mFixture.CreateModel();
        var planA1 = ExecutionPlanBuilder.Build(modelA);
        var planA2 = ExecutionPlanBuilder.Build(modelA);

        Assert.Equal(planA1.PlanHash, planA2.PlanHash);

        var modifiedDoc = new DocumentIdentitySnapshot
        {
            Title = "DifferentProject.rvt",
            VersionGuid = Guid.NewGuid()
        };
        var planDocChanged = ExecutionPlanBuilder.Build(modelA, targetDoc: modifiedDoc);
        Assert.NotEqual(planA1.PlanHash, planDocChanged.PlanHash);

        var modifiedColumns = modelA.Columns.Select((c, i) => i == 0 ? c with { CenterLocation = new Point2D(9999.0, 9999.0) } : c).ToList();
        var modelB = modelA with { Columns = modifiedColumns };
        var planGeomChanged = ExecutionPlanBuilder.Build(modelB);
        Assert.NotEqual(planA1.PlanHash, planGeomChanged.PlanHash);
    }

    [Fact]
    public void Stress6_CompositeSourceKey_ExhaustiveFormatAndEdgeCases()
    {
        Assert.True(CompositeSourceKey.TryParse("[]#[Column]:C1", out var k1));
        Assert.Equal(string.Empty, k1!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, k1.Category);
        Assert.Equal("C1", k1.PrimaryHandle);

        Assert.True(CompositeSourceKey.TryParse("#Column:C2", out var k2));
        Assert.Equal(string.Empty, k2!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, k2.Category);
        Assert.Equal("C2", k2.PrimaryHandle);

        Assert.True(CompositeSourceKey.TryParse("[MyModel]#[Beam]:B10", out var k3));
        Assert.Equal("MyModel", k3!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Beam, k3.Category);
        Assert.Equal("B10", k3.PrimaryHandle);

        Assert.False(CompositeSourceKey.TryParse(string.Empty, out _));
        Assert.False(CompositeSourceKey.TryParse("   ", out _));
        Assert.False(CompositeSourceKey.TryParse("invalid_format", out _));
        Assert.False(CompositeSourceKey.TryParse("[Doc]#[NonExistentCategory]:Handle", out _));
    }
}
