using HPKata.Revit.Abstractions;
using HPKata.Revit.Runners;
using HPKata.Revit.Tests.Mocks;
using Xunit;

namespace HPKata.Revit.Tests.Runners;

public sealed class RevitCuttingStockRunnerTests
{
    private static MockRevitDocumentContext CreateCuttingContext()
    {
        var context = new MockRevitDocumentContext();
        // D20 main bars: 8 bars of 4500 mm
        context.AllRebars.Add(new RevitRebarDto
        {
            ElementId = 1,
            Partition = "D1",
            Mark = "1",
            Quantity = 8,
            DiameterMm = 20,
            SingleBarLengthMm = 4500.0,
            ShapeName = "M_00"
        });
        // D16 bars: 10 bars of 3200 mm
        context.AllRebars.Add(new RevitRebarDto
        {
            ElementId = 2,
            Partition = "D1",
            Mark = "2",
            Quantity = 10,
            DiameterMm = 16,
            SingleBarLengthMm = 3200.0,
            ShapeName = "M_00"
        });
        // D8 stirrups: 20 bars of 1200 mm (below 10mm -> must be filtered out!)
        context.AllRebars.Add(new RevitRebarDto
        {
            ElementId = 3,
            Partition = "D1",
            Mark = "3",
            Quantity = 20,
            DiameterMm = 8,
            SingleBarLengthMm = 1200.0,
            ShapeName = "M_11"
        });
        return context;
    }

    [Fact]
    public void Execute_FiltersBarsBelow10mm()
    {
        var context = CreateCuttingContext();
        var results = RevitCuttingStockRunner.Execute(context);

        // Should only optimize D16 and D20, NOT D8!
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.DiameterMm == 16);
        Assert.Contains(results, r => r.DiameterMm == 20);
        Assert.DoesNotContain(results, r => r.DiameterMm == 8);
    }

    [Fact]
    public void Execute_Standard11700mm_ProducesValidCuttingPatterns()
    {
        var context = CreateCuttingContext();
        var results = RevitCuttingStockRunner.Execute(context, 11700.0);

        foreach (var res in results)
        {
            Assert.True(res.NewStandardBarsCount > 0, "Should use at least 1 standard stock bar.");
            foreach (var pat in res.Patterns)
            {
                Assert.Equal(11700.0, pat.StockLengthMm);
                Assert.True(pat.UsedLengthMm <= pat.StockLengthMm, "Cuts must not exceed stock length.");
                Assert.True(pat.RemnantLengthMm >= 0.0, "Remnant must be non-negative.");
            }
        }
    }

    [Fact]
    public void Execute_GrossAndNetWaste_AreNonNegative()
    {
        var context = CreateCuttingContext();
        var results = RevitCuttingStockRunner.Execute(context);

        foreach (var res in results)
        {
            Assert.True(res.GrossWastePercent >= 0.0, "Gross waste must be >= 0%");
            Assert.True(res.NetWastePercent >= 0.0, "Net waste must be >= 0%");
            Assert.True(res.GrossWastePercent <= 100.0, "Gross waste must be <= 100%");
        }
    }

    [Fact]
    public void Execute_SummaryTable_BuildsCorrectRowCounts()
    {
        var context = CreateCuttingContext();
        var results = RevitCuttingStockRunner.Execute(context);

        Assert.Single(context.CreatedTables);
        var table = context.CreatedTables[0];
        Assert.Equal(results.Count, table.Rows.Count);
        Assert.Equal(7, table.Headers.Count);
    }

    [Fact]
    public void Execute_EmptyDemands_ReturnsEmptyResults()
    {
        var context = new MockRevitDocumentContext();
        // Only D6 and D8 bars (all below 10mm threshold)
        context.AllRebars.Add(new RevitRebarDto { DiameterMm = 6, Quantity = 5, SingleBarLengthMm = 1000 });
        context.AllRebars.Add(new RevitRebarDto { DiameterMm = 8, Quantity = 5, SingleBarLengthMm = 1000 });

        var results = RevitCuttingStockRunner.Execute(context);
        Assert.Empty(results);
    }
}
