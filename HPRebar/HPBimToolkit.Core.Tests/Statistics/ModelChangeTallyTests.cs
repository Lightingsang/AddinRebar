using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Statistics;
using Xunit;

namespace HPBimToolkit.Core.Tests.Statistics;

public sealed class ModelChangeTallyTests
{
    [Fact]
    public void Record_SeveralBatches_SumsEachKind()
    {
        var tally = new ModelChangeTally();

        tally.Record(2, 0, 5);
        tally.Record(1, 3, 0);

        Assert.True(tally.HasChanges);
        Assert.Equal(new ModelStaleEvent(3, 3, 5), tally.ToEvent());
    }

    [Fact]
    public void Reset_AfterChanges_StartsFromZero()
    {
        var tally = new ModelChangeTally();
        tally.Record(4, 4, 4);

        tally.Reset();

        Assert.False(tally.HasChanges);
        Assert.Equal(new ModelStaleEvent(0, 0, 0), tally.ToEvent());
    }
}
