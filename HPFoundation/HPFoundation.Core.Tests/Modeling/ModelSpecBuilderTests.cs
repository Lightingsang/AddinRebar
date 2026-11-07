using HPFoundation.Core.Model;
using HPFoundation.Core.Modeling;
using Xunit;

namespace HPFoundation.Core.Tests.Modeling;

public sealed class ModelSpecBuilderTests
{
    private static readonly FoundationInput Mb1 = new(
        "MB1-4COT",
        new[]
        {
            new ColumnLoad("A", 1.6, 135, 1, 1, 2.7, 1.7),
            new ColumnLoad("B", 5.3, 116, 1, 1, 3.4, 1.9),
            new ColumnLoad("C", 9.0, 100.5, 1.1, 1.5, 3.1, 2.4),
            new ColumnLoad("D", 11.4, 51.5, 1, 1, 2.6, 2.3),
        },
        11.7,
        new BeamSection(2.4, 0.5, 0.8, 0.6, 0.35),
        new SpringSupport(156.264, 78.132, 0.1, 0.05),
        "B20",
        "CB400-V");

    [Fact]
    public void Build_ColumnsOnTheGrid_MakesOneNodePerSpacing()
    {
        var spec = ModelSpecBuilder.Build(Mb1, FlangeProfile.DefaultFor(0.6));

        Assert.Equal(118, spec.Nodes.Count);
        Assert.Equal(0, spec.Nodes[0].XM, 9);
        Assert.Equal(11.7, spec.Nodes[^1].XM, 9);
    }

    [Fact]
    public void Build_BothFootingEnds_GetTheEndSpring()
    {
        var spec = ModelSpecBuilder.Build(Mb1, FlangeProfile.DefaultFor(0.6));

        Assert.Equal(SpringKind.End, spec.Nodes[0].Spring);
        Assert.Equal(SpringKind.End, spec.Nodes[^1].Spring);
        Assert.Equal(116, spec.Nodes.Count(node => node.Spring == SpringKind.Interior));
    }

    [Fact]
    public void Build_Stiffness_IsRoundedToWholeTonsPerMetre()
    {
        var spec = ModelSpecBuilder.Build(Mb1, FlangeProfile.DefaultFor(0.6));

        Assert.Equal(156, spec.InteriorStiffnessTPerM);
        Assert.Equal(78, spec.EndStiffnessTPerM);
    }

    [Fact]
    public void Build_ColumnOnTheGrid_PutsItsLoadOnThatNode()
    {
        var spec = ModelSpecBuilder.Build(Mb1, FlangeProfile.DefaultFor(0.6));

        var loaded = spec.Nodes.Where(node => node.Load is not null).ToList();
        Assert.Equal(4, loaded.Count);
        Assert.Equal(new[] { 1.6, 5.3, 9.0, 11.4 }, loaded.Select(node => Math.Round(node.XM, 6)));
        Assert.Equal(SpringKind.Interior, loaded[0].Spring);
    }

    [Fact]
    public void Build_ColumnOffTheGrid_AddsAnExtraNodeWithoutSpring()
    {
        var input = Mb1 with
        {
            Columns = Mb1.Columns.Select((column, index) => index == 1 ? column with { AxisPositionM = 4.15 } : column).ToArray(),
        };

        var spec = ModelSpecBuilder.Build(input, FlangeProfile.DefaultFor(0.6));

        Assert.Equal(119, spec.Nodes.Count);
        var extra = spec.Nodes.Single(node => Math.Abs(node.XM - 4.15) < 1e-9);
        Assert.Equal(SpringKind.None, extra.Spring);
        Assert.Equal("B", extra.Load!.Axis);
        Assert.Equal(spec.Nodes.OrderBy(node => node.XM).Select(node => node.XM), spec.Nodes.Select(node => node.XM));
    }

    [Fact]
    public void Build_ConcreteGrade_SetsTheElasticModulus()
    {
        var spec = ModelSpecBuilder.Build(Mb1, FlangeProfile.DefaultFor(0.6));

        Assert.Equal(2_804_219.6, spec.ConcreteModulusTPerM2, 1);
    }

    [Fact]
    public void SectionArea_Mb1Tee_MatchesTheHandModel()
    {
        var spec = ModelSpecBuilder.Build(Mb1, FlangeProfile.DefaultFor(0.6));

        Assert.Equal(1.255, spec.Section.AreaM2, 9);
    }

    [Fact]
    public void DefaultFlange_ShallowerFlange_KeepsTheEdgeAndShortensTheHaunch()
    {
        var profile = FlangeProfile.DefaultFor(0.5);

        Assert.Equal(0.3, profile.EdgeThicknessM, 9);
        Assert.Equal(0.2, profile.HaunchHeightM, 9);
    }

    [Fact]
    public void Build_FlangeProfileDisagreesWithSheet_Throws()
    {
        var profile = new FlangeProfile(0.3, 0.4);

        Assert.Throws<ArgumentException>(() => ModelSpecBuilder.Build(Mb1, profile));
    }
}
