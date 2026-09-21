using HPRobot.McpBridge.Safety;
using Xunit;

namespace HPRobot.McpBridge.Tests;

public sealed class RobotTierAnalyzerTests
{
    // =========================================================================
    // 1. Tier R (Read-Only Queries)
    // =========================================================================

    [Theory]
    [InlineData("var count = structure.Nodes.GetAll().Count;")]
    [InlineData("var bar = structure.Bars.Get(1); double l = bar.Length;")]
    [InlineData("var reaction = structure.Results.Nodes.Reactions.Value(1, 1);")]
    [InlineData("var forces = structure.Results.Bars.Forces.Value(1, 0.5);")]
    [InlineData("var mat = structure.Labels.Get(RobotLabelType.I_LT_MATERIAL, \"C25\");")]
    [InlineData("var v = robot.Application.ProgramVersion;")]
    [InlineData("var active = robot.Project.IsActive;")]
    [InlineData("var name = robot.Project.FileName;")]
    [InlineData("var exists = structure.Nodes.Exist(10);")]
    [InlineData("var free = structure.Bars.FreeNumber;")]
    [InlineData("var units = robot.Project.Preferences.Units;")]
    public void Analyze_ReadQueries_ClassifiesAsTierRead(string script)
    {
        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.Read, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
        Assert.Empty(result.WriteMembers);
        Assert.NotEmpty(result.ReadMembers);
    }

    [Fact]
    public void Analyze_EmptyOrWhitespaceScript_ClassifiesAsTierRead()
    {
        var resultNull = RobotTierAnalyzer.Analyze(null!);
        var resultEmpty = RobotTierAnalyzer.Analyze("");
        var resultWhitespace = RobotTierAnalyzer.Analyze("   \t\r\n  ");

        Assert.Equal(RobotTier.Read, resultNull.HighestTier);
        Assert.Equal(RobotTier.Read, resultEmpty.HighestTier);
        Assert.Equal(RobotTier.Read, resultWhitespace.HighestTier);
    }

    [Fact]
    public void Analyze_PureLocalCalculations_ClassifiesAsTierRead()
    {
        var script = @"
            int a = 10;
            int b = 20;
            int c = a + b;
            var list = new System.Collections.Generic.List<int> { a, b, c };
            return list.Count;
        ";

        var result = RobotTierAnalyzer.Analyze(script);
        Assert.Equal(RobotTier.Read, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
        Assert.Empty(result.WriteMembers);
    }

    [Fact]
    public void Analyze_ComplexLinqQueries_ClassifiesAsTierRead()
    {
        var script = @"
            var bars = Enumerable.Range(1, 10)
                .Where(i => structure.Bars.Exist(i) != 0)
                .Select(i => structure.Bars.Get(i))
                .Select(b => new { b.Number, b.Length, b.StartNode, b.EndNode })
                .ToList();
            return bars;
        ";

        var result = RobotTierAnalyzer.Analyze(script);
        Assert.Equal(RobotTier.Read, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
        Assert.Empty(result.WriteMembers);
    }

    // =========================================================================
    // 2. Tier W (Structural Mutations & Modifications)
    // =========================================================================

    [Theory]
    [InlineData("structure.Nodes.Create(1, 0.0, 0.0, 0.0);")]
    [InlineData("structure.Bars.Create(1, 1, 2);")]
    [InlineData("bar.SetLabel(RobotLabelType.I_LT_BAR_SECTION, \"IPE 300\");")]
    [InlineData("node.SetLabel(RobotLabelType.I_LT_SUPPORT, \"Pinned\");")]
    [InlineData("structure.Cases.CreateSimple(1, \"DL\", 0, 0);")]
    [InlineData("structure.Cases.CreateCombination(101, \"ULS\", 0, 0, 0);")]
    [InlineData("structure.Labels.Store(label);")]
    [InlineData("simpleCase.NewRecord(RobotLoadRecordType.I_LRT_UNIFORM);")]
    [InlineData("loadRecord.SetValue(0, -15.0);")]
    [InlineData("Nodes.Create(1, 0.0, 0.0, 0.0);")]
    [InlineData("structure.Nodes.Modify(1, 1.0, 2.0, 3.0);")]
    [InlineData("structure.Objects.CreateContour(1, contour);")]
    [InlineData("robot.Project.Save();")]
    [InlineData("robot.Project.SaveAs(\"backup.rtd\");")]
    public void Analyze_StructuralMutations_ClassifiesAsTierWrite(string script)
    {
        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.Write, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
        Assert.NotEmpty(result.WriteMembers);
    }

    [Fact]
    public void Analyze_MemberPropertyAssignment_ClassifiesAsTierWrite()
    {
        var script = @"
            var node = structure.Nodes.Get(1);
            node.X = 15.0;
        ";

        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.Write, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
        Assert.Contains(result.WriteMembers, m => m.Contains("node.X"));
    }

    [Fact]
    public void Analyze_BarGammaAssignment_ClassifiesAsTierWrite()
    {
        var script = @"
            var bar = structure.Bars.Get(1);
            bar.Gamma = 90.0;
        ";

        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.Write, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
        Assert.NotEmpty(result.WriteMembers);
    }

    // =========================================================================
    // 3. Tier D (Delete & Heavy FEA Operations)
    // =========================================================================

    [Theory]
    [InlineData("structure.CalcEngine.Calculate();")]
    [InlineData("calcEngine.Calculate();")]
    [InlineData("structure.CalcEngine.GenerateModel();")]
    [InlineData("structure.CalcEngine.AutoGenerateModel();")]
    [InlineData("structure.Bars.Delete(1);")]
    [InlineData("structure.Bars.DeleteMany(\"1 2 3\");")]
    [InlineData("structure.Bars.DeleteAll();")]
    [InlineData("structure.Nodes.Delete(5);")]
    [InlineData("structure.Nodes.DeleteMany(\"5 6\");")]
    [InlineData("structure.Nodes.DeleteAll();")]
    [InlineData("structure.Objects.Delete(1);")]
    [InlineData("structure.Cases.Delete(2);")]
    [InlineData("structure.Labels.Delete(RobotLabelType.I_LT_BAR_SECTION, \"Old\");")]
    [InlineData("structure.Clear();")]
    [InlineData("robot.Project.New(RobotProjectType.I_PT_FRAME_3D);")]
    [InlineData("robot.Project.Close();")]
    [InlineData("robot.Application.Quit();")]
    public void Analyze_HeavyOrDestructiveOperations_ClassifiesAsTierDeleteHeavy(string script)
    {
        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.DeleteHeavy, result.HighestTier);
        Assert.NotEmpty(result.DeleteHeavyMembers);
    }

    // =========================================================================
    // 4. Adversarial & Edge Cases
    // =========================================================================

    [Fact]
    public void Analyze_StringLiteralsContainingKeywords_DoesNotEscalateToHeavyOrWrite()
    {
        var script = @"
            log(""Running Calculate analysis on the selected frame..."");
            log(""Will Delete obsolete records if needed"");
            var count = structure.Nodes.GetAll().Count;
            return count;
        ";

        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.Read, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
        Assert.Empty(result.WriteMembers);
    }

    [Fact]
    public void Analyze_CommentsContainingKeywords_DoesNotEscalateToHeavy()
    {
        var script = @"
            // structure.CalcEngine.Calculate();
            // structure.Bars.Delete(1);
            /*
               structure.Clear();
            */
            var count = structure.Bars.Count;
            return count;
        ";

        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.Read, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
    }

    [Theory]
    [InlineData("structure.calcengine.calculate();")]
    [InlineData("STRUCTURE.CALCENGINE.CALCULATE();")]
    [InlineData("structure.bars.delete(1);")]
    [InlineData("structure.nodes.create(1, 0, 0, 0);")]
    public void Analyze_CaseInsensitiveMembers_ClassifiesCorrectly(string script)
    {
        var result = RobotTierAnalyzer.Analyze(script);

        if (script.Contains("delete", System.StringComparison.OrdinalIgnoreCase) ||
            script.Contains("calculate", System.StringComparison.OrdinalIgnoreCase))
        {
            Assert.Equal(RobotTier.DeleteHeavy, result.HighestTier);
        }
        else
        {
            Assert.Equal(RobotTier.Write, result.HighestTier);
        }
    }

    [Fact]
    public void Analyze_MixedReadAndWrite_EscalatesToTierWrite()
    {
        var script = @"
            var count = structure.Nodes.GetAll().Count;
            var node1 = structure.Nodes.Get(1);
            structure.Nodes.Create(count + 1, node1.X + 5.0, node1.Y, node1.Z);
            return count + 1;
        ";

        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.Write, result.HighestTier);
        Assert.Empty(result.DeleteHeavyMembers);
        Assert.NotEmpty(result.WriteMembers);
        Assert.NotEmpty(result.ReadMembers);
    }

    [Fact]
    public void Analyze_MixedReadWriteAndHeavy_EscalatesToTierDeleteHeavy()
    {
        var script = @"
            var count = structure.Nodes.Count;
            structure.Nodes.Create(count + 1, 0, 0, 10);
            structure.CalcEngine.Calculate();
            var reaction = structure.Results.Nodes.Reactions.Value(1, 1);
            return reaction;
        ";

        var result = RobotTierAnalyzer.Analyze(script);

        Assert.Equal(RobotTier.DeleteHeavy, result.HighestTier);
        Assert.NotEmpty(result.DeleteHeavyMembers);
        Assert.NotEmpty(result.WriteMembers);
        Assert.NotEmpty(result.ReadMembers);
    }

    [Fact]
    public void Analyze_ChainedMethodInvocations_DetectsTerminalOperation()
    {
        var script = @"
            structure.Bars.Get(1).StartNode = 2;
        ";

        var result = RobotTierAnalyzer.Analyze(script);
        Assert.Equal(RobotTier.Write, result.HighestTier);
    }

    [Fact]
    public void Analyze_DirectVsChainedReceiver_DemonstratesKnownMembersQualificationLimitation()
    {
        // When invoked directly as Nodes.FindXYZ, KnownMembers matches
        var directResult = RobotTierAnalyzer.Analyze("Nodes.FindXYZ(1.0, 2.0, 3.0);");
        Assert.Equal(RobotTier.Write, directResult.HighestTier);

        // When invoked with prefix structure.Nodes.FindXYZ, KnownMembers lookup fails
        // and simpleName "FindXYZ" is not in fallback list, falling through to Tier Read
        var chainedResult = RobotTierAnalyzer.Analyze("structure.Nodes.FindXYZ(1.0, 2.0, 3.0);");
        Assert.Equal(RobotTier.Read, chainedResult.HighestTier);
    }
}
