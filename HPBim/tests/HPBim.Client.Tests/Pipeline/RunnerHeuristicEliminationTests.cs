namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Client.Pipeline;
using HPBim.Client.ScriptGenerators;
using HPBim.Client.TypeResolver;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using Xunit;

public sealed class RunnerHeuristicEliminationTests
{
    [Fact]
    public void ColumnGeneration_IncludesRotationAndTopLevelParameter()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("ElementTransformUtils.RotateElement", script);
        Assert.Contains("BuiltInParameter.FAMILY_TOP_LEVEL_PARAM", script);
        Assert.Contains("Math.Abs(rotDeg) >", script);
        Assert.Contains("Line.CreateBound(pt, pt + XYZ.BasisZ)", script);
    }

    [Fact]
    public void PipingSystemTypeAndPipeType_HaveZeroFirstOrDefaultInRunnerScript()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.DoesNotContain("pipingSysTypes.FirstOrDefault", script);
        Assert.DoesNotContain("pipeTypes.FirstOrDefault", script);
        Assert.Contains("pipingSysTypes.TryGetValue(reqSysType, out pipingSysType)", script);
        Assert.Contains("pipeTypes.TryGetValue(reqPipeType, out pipeType)", script);
        Assert.Contains("ERR_SYMBOL_NOT_FOUND", script);
    }

    [Fact]
    public void Stage7MepGenerator_HasZeroFirstOrDefaultForPipingTypes_AndThrowsErrSymbolNotFound()
    {
        string script = Stage7MepGenerator.GetScriptCode();

        Assert.DoesNotContain("pipingSysTypes.FirstOrDefault", script);
        Assert.DoesNotContain("pipeTypes.FirstOrDefault", script);
        Assert.Contains("[ERR_SYMBOL_NOT_FOUND]", script);
        Assert.Contains("PipingSystemType '{reqSysName}' or PipeType '{reqPipeName}' not found in document.", script);
    }

    [Fact]
    public void CompositeBimGenerator_HasZeroFirstOrDefaultForPipingTypes_AndThrowsErrSymbolNotFound()
    {
        string script = CompositeBimGenerator.GetScriptCode();

        Assert.DoesNotContain("pipingSysTypes.FirstOrDefault", script);
        Assert.DoesNotContain("pipeTypes.FirstOrDefault", script);
        Assert.Contains("[ERR_SYMBOL_NOT_FOUND]", script);
    }

    [Fact]
    public void ExecutionPlanBuilder_PopulatesPipeRunResourceAndGeometry()
    {
        var model = new BimModel
        {
            ProjectName = "TestProj",
            Levels = new List<LevelDef>
            {
                new() { Name = "Level 1", ElevationM = 0.0 }
            },
            Pipes = new List<PipeRunDef>
            {
                new()
                {
                    Id = "P-01",
                    SystemType = PipeSystemType.WaterSupply,
                    Material = "PPR",
                    DiameterMm = 32.0,
                    Level = "Level 1",
                    Points = new List<Point3D>
                    {
                        new(0, 0, 1000),
                        new(2000, 0, 1000)
                    }
                }
            }
        };

        var plan = ExecutionPlanBuilder.Build(model);
        var pipeItem = plan.Items.FirstOrDefault(i => i.Category == CoordinationCategory.PipeRun);

        Assert.NotNull(pipeItem);
        Assert.Equal("Domestic Cold Water", pipeItem.Resource.FamilyName);
        Assert.Equal("PPR", pipeItem.Resource.TypeName);
        Assert.IsType<LineGeometrySpec>(pipeItem.Geometry);

        var lineGeom = (LineGeometrySpec)pipeItem.Geometry;
        Assert.Equal(0, lineGeom.Start.X);
        Assert.Equal(2000, lineGeom.End.X);
        Assert.Equal("Domestic Cold Water", pipeItem.Parameters["SystemType"]);
        Assert.Equal("PPR", pipeItem.Parameters["PipeType"]);
        Assert.Equal(32.0, pipeItem.Parameters["DiameterMm"]);
    }
}
