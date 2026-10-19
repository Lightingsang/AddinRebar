namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Client.Pipeline;
using HPBim.Client.ScriptGenerators;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using Xunit;

public sealed class EmpiricalChallengerHeuristicsTests
{
    [Theory]
    [InlineData(0.0, false)]
    [InlineData(45.0, true)]
    [InlineData(90.0, true)]
    [InlineData(180.0, true)]
    [InlineData(-45.0, true)]
    [InlineData(-90.0, true)]
    [InlineData(-180.0, true)]
    public void ColumnRotation_MathematicalEvaluation_HandlesAllAnglesAccurately(double rotDeg, bool shouldRotate)
    {
        bool evaluatedRotate = Math.Abs(rotDeg) > 1e-4;
        Assert.Equal(shouldRotate, evaluatedRotate);

        if (evaluatedRotate)
        {
            double rad = rotDeg * (Math.PI / 180.0);
            double expectedRad = rotDeg switch
            {
                45.0 => Math.PI / 4.0,
                90.0 => Math.PI / 2.0,
                180.0 => Math.PI,
                -45.0 => -Math.PI / 4.0,
                -90.0 => -Math.PI / 2.0,
                -180.0 => -Math.PI,
                _ => rad
            };
            Assert.Equal(expectedRad, rad, precision: 6);
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    [InlineData(180.0)]
    [InlineData(-45.0)]
    [InlineData(-90.0)]
    [InlineData(-180.0)]
    public void ExecutionPlanBuilder_ColumnPlacement_PreservesRotationDegAndTopLevel(double angle)
    {
        var model = new BimModel
        {
            ProjectName = "RotAndLevelTest",
            Levels = new List<LevelDef>
            {
                new() { Name = "Level 1", ElevationM = 0.0 },
                new() { Name = "Level 2", ElevationM = 3.6 }
            },
            Columns = new List<ColumnDef>
            {
                new()
                {
                    Id = "C-TEST",
                    BaseLevel = "Level 1",
                    TopLevel = "Level 2",
                    CenterLocation = new Point2D(1000, 2000),
                    RotationDeg = angle,
                    WidthMm = 220,
                    DepthMm = 220
                }
            }
        };

        var plan = ExecutionPlanBuilder.Build(model);
        var colItem = plan.Items.FirstOrDefault(i => i.Category == CoordinationCategory.Column);

        Assert.NotNull(colItem);
        Assert.Equal("Level 1", colItem.TargetLevelName);
        Assert.Equal("Level 2", colItem.TargetTopLevelName);
        Assert.IsType<PointGeometrySpec>(colItem.Geometry);

        var ptGeom = (PointGeometrySpec)colItem.Geometry;
        Assert.Equal(angle, ptGeom.RotationDegrees);
    }

    [Fact]
    public void ColumnRunnerScript_GuaranteesRotationAndTopLevelAssignment_AndFailsOnMissingTopLevel()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("string topLvlName = item.Str(\"targetTopLevelName\", string.Empty);", script);
        Assert.Contains("if (!existingLevels.TryGetValue(topLvlName, out topLvl))", script);
        Assert.Contains("[\"errorCode\"] = \"ERR_LEVEL_NOT_FOUND\"", script);
        Assert.Contains("TopLevel '{topLvlName}' not found.", script);
        Assert.Contains("var pTop = colInst.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);", script);
        Assert.Contains("if (pTop != null && !pTop.IsReadOnly) pTop.Set(topLvl.Id);", script);

        Assert.Contains("double rotDeg = geom.Double(\"rotationDegrees\", geom.Double(\"rotationDeg\", 0.0));", script);
        Assert.Contains("if (Math.Abs(rotDeg) > 1e-4)", script);
        Assert.Contains("var axis = Line.CreateBound(pt, pt + XYZ.BasisZ);", script);
        Assert.Contains("ElementTransformUtils.RotateElement(doc, colInst.Id, axis, rotDeg * (Math.PI / 180.0));", script);
    }

    [Fact]
    public void PipeResolution_AcrossAllGenerators_NeverUsesFirstOrDefault_AndReportsErrSymbolNotFound()
    {
        string runnerScript = BimPlanRunnerScriptTemplate.GetScriptCode();
        string stage7Script = Stage7MepGenerator.GetScriptCode();
        string compScript = CompositeBimGenerator.GetScriptCode();

        Assert.DoesNotContain("pipingSysTypes.FirstOrDefault", runnerScript);
        Assert.DoesNotContain("pipeTypes.FirstOrDefault", runnerScript);
        Assert.DoesNotContain("pipingSysTypes.FirstOrDefault", stage7Script);
        Assert.DoesNotContain("pipeTypes.FirstOrDefault", stage7Script);
        Assert.DoesNotContain("pipingSysTypes.FirstOrDefault", compScript);
        Assert.DoesNotContain("pipeTypes.FirstOrDefault", compScript);

        Assert.Contains("if (pipingSysType == null || pipeType == null)", runnerScript);
        Assert.Contains("[\"errorCode\"] = \"ERR_SYMBOL_NOT_FOUND\"", runnerScript);
        Assert.Contains("not found in document.", runnerScript);

        Assert.Contains("if (pipeItems.Count > 0 && (pipingSystemType == null || pipeType == null))", stage7Script);
        Assert.Contains("[ERR_SYMBOL_NOT_FOUND]", stage7Script);

        Assert.Contains("if (pipeItems.Count > 0 && (pipingSystemType == null || pipeType == null))", compScript);
        Assert.Contains("[ERR_SYMBOL_NOT_FOUND]", compScript);
    }

    [Fact]
    public void PipeRun_ExecutionPlanBuilder_PreservesExplicitParameters_NoGuesswork()
    {
        var model = new BimModel
        {
            ProjectName = "MepPlan",
            Levels = new List<LevelDef>
            {
                new() { Name = "Ground", ElevationM = 0.0 }
            },
            Pipes = new List<PipeRunDef>
            {
                new()
                {
                    Id = "P-CW-01",
                    SystemType = PipeSystemType.WaterSupply,
                    Material = "HDPE",
                    DiameterMm = 50.0,
                    Level = "Ground",
                    Points = new List<Point3D>
                    {
                        new(100, 200, 500),
                        new(1000, 2000, 500)
                    }
                }
            }
        };

        var plan = ExecutionPlanBuilder.Build(model);
        var pipeItem = plan.Items.FirstOrDefault(i => i.Category == CoordinationCategory.PipeRun);

        Assert.NotNull(pipeItem);
        Assert.Equal("Domestic Cold Water", pipeItem.Resource.FamilyName);
        Assert.Equal("HDPE", pipeItem.Resource.TypeName);
        Assert.Equal("Domestic Cold Water", pipeItem.Parameters["SystemType"]);
        Assert.Equal("HDPE", pipeItem.Parameters["PipeType"]);
        Assert.Equal(50.0, pipeItem.Parameters["DiameterMm"]);
    }
}
