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
using Xunit;

public sealed class EmpiricalChallengerM1Tests
{
    private static string ScriptArgs_Require(JsonElement elem, string key)
    {
        if (elem.TryGetProperty(key, out var prop))
        {
            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString() ?? string.Empty,
                JsonValueKind.Number => prop.GetRawText(),
                _ => prop.GetRawText()
            };
        }
        throw new ArgumentException($"args.{key} is required.");
    }

    [Fact]
    public void Challenge1_Category_WireFormat_CausesAllElementsToFallIntoElseNoOp()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var plan = ExecutionPlanBuilder.Build(model);

        var element = BridgeJson.ToElement(plan);
        Assert.NotNull(element);

        var itemsElem = element.Value.GetProperty("items");
        var items = itemsElem.EnumerateArray().ToList();
        Assert.NotEmpty(items);

        int levelMatches = 0;
        int gridMatches = 0;
        int columnMatches = 0;
        int beamMatches = 0;
        int wallMatches = 0;
        int slabMatches = 0;
        int openingMatches = 0;
        int roomMatches = 0;
        int mepMatches = 0;
        int rebarMatches = 0;
        int elseNoOpMatches = 0;

        foreach (var item in items)
        {
            string category = ScriptArgs_Require(item, "category");

            if (category == "Level") levelMatches++;
            else if (category == "Grid") gridMatches++;
            else if (category == "Column") columnMatches++;
            else if (category == "Beam" || category == "GradeBeam") beamMatches++;
            else if (category == "Wall") wallMatches++;
            else if (category == "Slab") slabMatches++;
            else if (category == "Opening" || category == "Door" || category == "Window") openingMatches++;
            else if (category == "Room") roomMatches++;
            else if (category == "MepFixture" || category == "PipeRun") mepMatches++;
            else if (category == "Rebar") rebarMatches++;
            else elseNoOpMatches++;
        }

        // VERIFIED RESOLUTION: Items match their categories as string enums on the wire
        Assert.Equal(0, elseNoOpMatches);
        Assert.True(levelMatches > 0, "Level elements correctly matched by string Category.");
        Assert.True(gridMatches > 0, "Grid elements correctly matched by string Category.");
        Assert.True(columnMatches > 0, "Column elements correctly matched by string Category.");
        Assert.True(beamMatches > 0, "Beam elements correctly matched by string Category.");
        Assert.True(wallMatches > 0, "Wall elements correctly matched by string Category.");
        Assert.True(slabMatches > 0, "Slab elements correctly matched by string Category.");
        Assert.True(roomMatches > 0, "Room elements correctly matched by string Category.");
        Assert.True(mepMatches > 0, "MEP elements correctly matched by string Category.");
        Assert.True(rebarMatches > 0, "Rebar elements correctly matched by string Category.");
    }

    [Fact]
    public void Verify_BridgeJson_Category_Serialization()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var plan = ExecutionPlanBuilder.Build(model);

        var element = BridgeJson.ToElement(plan);
        Assert.NotNull(element);

        var itemsElem = element.Value.GetProperty("items");
        var items = itemsElem.EnumerateArray().ToList();
        Assert.NotEmpty(items);

        foreach (var item in items)
        {
            var catProp = item.GetProperty("category");
            Assert.Equal(JsonValueKind.String, catProp.ValueKind);
            string catString = catProp.GetString()!;
            Assert.False(int.TryParse(catString, out _), $"Category should serialize as string name, but was: '{catString}'");
        }
    }

    [Fact]
    public void Challenge2_ScriptTemplate_ContainsFirstValueFallback_ViolatingRule0()
    {
        string scriptCode = BimPlanRunnerScriptTemplate.GetScriptCode();

        // Check for .FirstOrDefault() fallbacks on symbol/type dictionaries in the script template
        bool columnFallback = scriptCode.Contains("columnSymbols.Values.FirstOrDefault()");
        bool beamFallback = scriptCode.Contains("beamSymbols.Values.FirstOrDefault()");
        bool wallFallback = scriptCode.Contains("wallTypes.Values.FirstOrDefault()");

        // VERIFIED RESOLUTION: The script strictly fails fast without arbitrary .FirstOrDefault() fallbacks
        Assert.False(columnFallback, "Script must not contain columnSymbols.Values.FirstOrDefault() fallback.");
        Assert.False(beamFallback, "Script must not contain beamSymbols.Values.FirstOrDefault() fallback.");
        Assert.False(wallFallback, "Script must not contain wallTypes.Values.FirstOrDefault() fallback.");
    }

    [Fact]
    public async Task Challenge3_PipelineResult_ReportsSuccessTrue_WhenElementsFail()
    {
        // Response with a failed element record
        var fakeResponseJson = """
        {
            "planId": "1234",
            "total": 2,
            "created": 0,
            "updated": 0,
            "noOp": 0,
            "skipped": 0,
            "failed": 2,
            "records": [
                {
                    "sourceKey": "Doc#Column:C1",
                    "mark": "C1",
                    "category": "Column",
                    "status": "Failed",
                    "errorCode": "ERR_SYMBOL_NOT_FOUND",
                    "errorReason": "Symbol not found"
                },
                {
                    "sourceKey": "Doc#Beam:B1",
                    "mark": "B1",
                    "category": "Beam",
                    "status": "Failed",
                    "errorCode": "ERR_LEVEL_NOT_FOUND",
                    "errorReason": "Level not found"
                }
            ]
        }
        """;

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => new ExecuteResult
            {
                IsError = false, // Script finished normally and returned the failed records summary
                DurationMs = 50,
                RolledBack = req.DryRun,
                Value = JsonDocument.Parse(fakeResponseJson).RootElement
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-1234",
            Items = new[]
            {
                new ElementPlanItem { SourceKey = CompositeSourceKey.Parse("Doc#Column:C1"), Category = CoordinationCategory.Column, Mark = "C1", SequenceOrder = 1 },
                new ElementPlanItem { SourceKey = CompositeSourceKey.Parse("Doc#Beam:B1"), Category = CoordinationCategory.Beam, Mark = "B1", SequenceOrder = 2 }
            },
            Options = new ExecutionOptions { DryRun = true }
        };

        var result = await engine.ExecutePlanAsync(plan);

        // VERIFIED RESOLUTION: Pipeline correctly fails when elements fail
        Assert.False(result.Success, "Pipeline result must report Success = false when element execution fails.");
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("ERR_SYMBOL_NOT_FOUND", result.ErrorMessage);
        Assert.All(result.ElementRecords, r => Assert.Equal(ElementExecutionStatus.Failed, r.Status));
    }

    [Fact]
    public void Challenge4_ScriptTemplate_OmitsSlabsOpeningsRoomsMepRebar()
    {
        string scriptCode = BimPlanRunnerScriptTemplate.GetScriptCode();

        // VERIFIED RESOLUTION: All categories are now supported in BimPlanRunnerScriptTemplate
        Assert.Contains("category == \"Slab\"", scriptCode);
        Assert.Contains("category == \"Opening\"", scriptCode);
        Assert.Contains("category == \"Room\"", scriptCode);
        Assert.Contains("category == \"MepFixture\"", scriptCode);
        Assert.Contains("category == \"PipeRun\"", scriptCode);
        Assert.Contains("category == \"Rebar\"", scriptCode);
    }

    [Fact]
    public void Challenge5_PlanHash_IsNotDeterministicAcrossBuildsOfSameModel()
    {
        var model = NhaPho5x18mFixture.CreateModel();

        var plan1 = ExecutionPlanBuilder.Build(model);
        var plan2 = ExecutionPlanBuilder.Build(model);

        // VERIFIED RESOLUTION: PlanHash is deterministic across builds of identical model
        Assert.Equal(plan1.PlanHash, plan2.PlanHash);
    }

    [Fact]
    public void Challenge6_CompositeSourceKey_RoundTrip_FailsOnEmptyDocIdentifier()
    {
        var key = new CompositeSourceKey(string.Empty, CoordinationCategory.Column, "C1");
        string formatted = key.ToString();

        // formatted is "[]#[Column]:C1"
        Assert.Equal("[]#[Column]:C1", formatted);

        bool parsedSuccessfully = CompositeSourceKey.TryParse(formatted, out var parsedKey);

        // VERIFIED RESOLUTION: TryParse succeeds when DocIdentifier is empty
        Assert.True(parsedSuccessfully, "CompositeSourceKey.TryParse must succeed when DocIdentifier is empty.");
        Assert.Equal(key, parsedKey);
    }
}
