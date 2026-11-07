using System.Text.Json;
using HPFoundation.Core.Sheets;
using Xunit;

namespace HPFoundation.Core.Tests.Sheets;

/// <summary>Runs the sheet rules over a grid captured from the real MB1-4COT sheet, before any rewrite.</summary>
public sealed class RealSheetTests
{
    private static readonly SheetGrid Grid = LoadFixture();

    [Fact]
    public void Locate_RealSheet_FindsEveryAnchorAtItsKnownCell()
    {
        var result = SheetLayoutLocator.Locate(Grid);

        Assert.Empty(result.MissingAnchors);
        var layout = result.Layout!;
        Assert.Equal(17, layout.FirstLoadRow);
        Assert.Equal(4, layout.LoadRowCount);
        Assert.Equal(29, layout.SpanRow);
        Assert.Equal(128, layout.InteriorSpringRow);
        Assert.Equal("F34", layout.LongDirectionMoment.ToString());
        Assert.Equal("D39", layout.ShortDirectionMoment.ToString());
    }

    [Fact]
    public void Build_RealSheet_MatchesTheSheetAsAnalysed()
    {
        var layout = SheetLayoutLocator.Locate(Grid).Layout!;

        var result = FoundationInputBuilder.Build(Grid, layout);

        Assert.Empty(result.Issues);
        var input = result.Input!;
        Assert.Empty(InputValidator.Validate(input));
        Assert.Equal(11.7, input.FootingLengthM, 6);
        Assert.Equal(403, input.Columns.Sum(column => column.AxialT), 6);
        Assert.Equal(159.72466, input.Springs.InteriorStiffnessTPerM, 4);
        Assert.Equal(79.862329, input.Springs.EndStiffnessTPerM, 4);
    }

    [Fact]
    public void Rewrite_RealFormulas_ProduceTheAgreedConvention()
    {
        var layout = SheetLayoutLocator.Locate(Grid).Layout!;

        var longFormula = MomentFormulaRewriter.RewriteLongDirection(Grid.CellAt(layout.LongDirectionMoment)!.Formula!);
        var shortFormula = MomentFormulaRewriter.RewriteShortDirection(Grid.CellAt(layout.ShortDirectionMoment)!.Formula!);

        Assert.StartsWith("=ABS(ROUND(SUM(F17:F20)+ABS(", longFormula);
        Assert.StartsWith("=ABS(ROUND(SUM(D17:D20)+SUM(G17:G20)*", shortFormula);
    }

    private static SheetGrid LoadFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "mb1-4cot-grid.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var cells = new Dictionary<CellAddress, SheetCell>();
        foreach (var cell in document.RootElement.GetProperty("cells").EnumerateArray())
        {
            cells[CellAddress.Parse(cell.GetProperty("address").GetString()!)] = new SheetCell(
                cell.GetProperty("Number").ValueKind == JsonValueKind.Number ? cell.GetProperty("Number").GetDouble() : null,
                cell.GetProperty("Text").GetString(),
                cell.GetProperty("ErrorText").GetString(),
                cell.GetProperty("Formula").GetString());
        }

        return new SheetGrid(document.RootElement.GetProperty("sheetName").GetString()!, cells);
    }
}
