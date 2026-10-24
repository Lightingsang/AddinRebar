using HPBimToolkit.Core.Contracts;
using Xunit;

namespace HPBimToolkit.Core.Tests.Contracts;

public sealed class SelectionSnapshotTests
{
    [Fact]
    public void From_FewerIdsThanCap_KeepsAllAndIsNotTruncated()
    {
        var snapshot = SelectionSnapshot.From([10L, 20L]);

        Assert.Equal(2, snapshot.Count);
        Assert.Equal([10L, 20L], snapshot.Ids);
        Assert.False(snapshot.Truncated);
    }

    [Fact]
    public void From_MoreIdsThanCap_CapsIdsButCountsAll()
    {
        var ids = Enumerable.Range(1, SelectionSnapshot.MaxIds + 5).Select(i => (long)i).ToArray();

        var snapshot = SelectionSnapshot.From(ids);

        Assert.Equal(SelectionSnapshot.MaxIds + 5, snapshot.Count);
        Assert.Equal(SelectionSnapshot.MaxIds, snapshot.Ids.Count);
        Assert.True(snapshot.Truncated);
    }
}
