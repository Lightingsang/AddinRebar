using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Statistics;
using Xunit;

namespace HPBimToolkit.Core.Tests.Statistics;

public sealed class CategoryTallyTests
{
    [Fact]
    public void ToCounts_MixedCategories_SortsByCountThenName()
    {
        var tally = new CategoryTally();
        Add(tally, 1, "Walls", 2);
        Add(tally, 2, "doors", 3);
        Add(tally, 3, "Columns", 3);

        var counts = tally.ToCounts();

        Assert.Equal(["Columns", "doors", "Walls"], counts.Select(c => c.Name));
        Assert.Equal(8, tally.Total);
    }

    [Fact]
    public void Add_SameCategoryManyTimes_ReadsNameOnce()
    {
        var tally = new CategoryTally();
        var reads = 0;

        for (var i = 0; i < 5; i++)
        {
            tally.Add(7, () => { reads++; return "Floors"; });
        }

        Assert.Equal(1, reads);
        Assert.Equal(new CategoryCount(7, "Floors", 5), tally.ToCounts().Single());
    }

    [Fact]
    public void ToCounts_Empty_ReturnsNothing()
    {
        Assert.Empty(new CategoryTally().ToCounts());
    }

    private static void Add(CategoryTally tally, long id, string name, int times)
    {
        foreach (var _ in Enumerable.Range(0, times))
        {
            tally.Add(id, () => name);
        }
    }
}
