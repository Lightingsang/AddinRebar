using HPFoundation.Core.Model;
using HPFoundation.Core.Modeling;
using Xunit;

namespace HPFoundation.Core.Tests.Modeling;

public sealed class EquilibriumCheckTests
{
    private static readonly ModelSpec Spec = new(
        "MB1-4COT",
        new[]
        {
            new ModelNode(0, SpringKind.End, null),
            new ModelNode(1.6, SpringKind.Interior, new ColumnLoad("A", 1.6, 135, 0, 0, 0, 0)),
            new ModelNode(5.3, SpringKind.Interior, new ColumnLoad("B", 5.3, 116, 0, 0, 0, 0)),
        },
        new TeeSection(2.4, 0.5, 0.8, 0.2, 0.3, 0.3),
        2_800_000,
        156,
        78);

    [Fact]
    public void Holds_ReactionEqualsLoad_IsTrue()
    {
        Assert.True(EquilibriumCheck.Holds(Spec, new AnalysisResult([], 251.005)));
    }

    [Fact]
    public void Holds_ReactionShort_IsFalse()
    {
        Assert.False(EquilibriumCheck.Holds(Spec, new AnalysisResult([], 250.9)));
    }
}
