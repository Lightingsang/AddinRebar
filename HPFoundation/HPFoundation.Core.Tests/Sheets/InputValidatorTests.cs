using HPFoundation.Core.Model;
using HPFoundation.Core.Sheets;
using Xunit;

namespace HPFoundation.Core.Tests.Sheets;

public sealed class InputValidatorTests
{
    private static readonly FoundationInput Valid = new(
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
        new SpringSupport(159.7, 79.9, 0.1, 0.05),
        "B20",
        "CB400-V");

    [Fact]
    public void Validate_ConsistentInput_HasNoIssues()
    {
        Assert.Empty(InputValidator.Validate(Valid));
    }

    [Fact]
    public void Validate_ThreeColumns_RefusesFourColumnProfile()
    {
        var input = Valid with { Columns = Valid.Columns.Take(3).ToArray() };

        Assert.Contains(InputValidator.Validate(input), issue => issue.Message.Contains("4"));
    }

    [Fact]
    public void Validate_BeamWiderThanFooting_ReportsSection()
    {
        var input = Valid with { Section = Valid.Section with { BeamWidthM = 3 } };

        Assert.Contains(InputValidator.Validate(input), issue => issue.Message.Contains("beam width"));
    }

    [Fact]
    public void Validate_FlangeDeeperThanBeam_ReportsSection()
    {
        var input = Valid with { Section = Valid.Section with { FlangeDepthM = 0.9 } };

        Assert.Contains(InputValidator.Validate(input), issue => issue.Message.Contains("flange"));
    }

    [Fact]
    public void Validate_ColumnBeyondFootingEnd_ReportsAxis()
    {
        var input = Valid with { FootingLengthM = 11 };

        Assert.Contains(InputValidator.Validate(input), issue => issue.Message.Contains("axis D"));
    }

    [Fact]
    public void Validate_ColumnOffTheSpringGrid_IsAccepted()
    {
        var input = Valid with
        {
            Columns = Valid.Columns.Select((column, index) => index == 1 ? column with { AxisPositionM = 4.15 } : column).ToArray(),
        };

        Assert.Empty(InputValidator.Validate(input));
    }

    [Fact]
    public void Validate_ZeroSpan_ReportsColumns()
    {
        var input = Valid with
        {
            Columns = Valid.Columns.Select((column, index) => index == 1 ? column with { AxisPositionM = 1.6 } : column).ToArray(),
        };

        Assert.Contains(InputValidator.Validate(input), issue => issue.Message.Contains("positive spans"));
    }

    [Fact]
    public void FailedCells_UnknownGradeAndExcelError_ReportsBothInReadingOrder()
    {
        var grid = new SheetGrid("MB1-4COT", new Dictionary<CellAddress, SheetCell>
        {
            [CellAddress.Parse("E110")] = SheetCell.FromText("!!!"),
            [CellAddress.Parse("H3")] = SheetCell.FromError("#DIV/0!"),
            [CellAddress.Parse("A1")] = SheetCell.FromText("8.Tính toán móng băng"),
        });

        var issues = InputValidator.FailedCells(grid);

        Assert.Equal(new[] { "H3", "E110" }, issues.Select(issue => issue.Cell));
    }

    [Fact]
    public void Validate_ZeroSpring_ReportsSprings()
    {
        var input = Valid with { Springs = Valid.Springs with { EndStiffnessTPerM = 0 } };

        Assert.Contains(InputValidator.Validate(input), issue => issue.Message.Contains("spring"));
    }

    [Fact]
    public void Validate_SpringSpacingDoesNotDivideLength_ReportsSprings()
    {
        var input = Valid with { Springs = Valid.Springs with { InteriorSpacingM = 0.25 } };

        Assert.Contains(InputValidator.Validate(input), issue => issue.Message.Contains("spacing"));
    }
}
