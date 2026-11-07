using System.Text.RegularExpressions;

namespace HPFoundation.Core.Sheets;

/// <summary>
///     Finds the input cells by the labels beside them, because the sheets of one workbook do not share row numbers.
///     An anchor that is missing or ambiguous is reported, never guessed.
/// </summary>
public static partial class SheetLayoutLocator
{
    private const int LabelColumn = 1;
    private const int AxisHeaderColumn = 2;
    private const int AxialHeaderColumn = 3;
    private const int ValueColumnD = 4;
    private const int ValueColumnC = 3;
    private const int ValueColumnB = 2;
    private const int SpringStiffnessColumn = 6;
    private const int UnitRowsBelowLoadHeader = 2;
    private const int UnitRowsBelowSpanHeader = 2;
    private const int HeaderRowsBelowSpringTitle = 2;
    private const char MomentXColumnLetter = 'D';
    private const char MomentYColumnLetter = 'F';

    private static readonly string[] LoadHeaders = ["Tên trục", "Ntto", "Mttox", "Qttox", "Mttoy", "Qttoy"];

    public static LayoutResult Locate(SheetGrid grid)
    {
        var missing = new List<string>();

        var loadHeaderRow = FindLoadHeaderRow(grid, missing);
        var firstLoadRow = loadHeaderRow + UnitRowsBelowLoadHeader;
        var loadRowCount = CountLoadRows(grid, firstLoadRow);
        var spanRow = FindSpanRow(grid, missing);

        var beamDepth = ValueBeside(grid, "Chọn chiều cao dầm móng h =", ValueColumnD, missing);
        var footingWidth = ValueBeside(grid, "Chọn bề rộng móng b =", ValueColumnD, missing);
        var columnWidth = ValueBeside(grid, "Chiều rộng tiết diện cột bc=", ValueColumnD, missing);
        var beamWidth = ValueBeside(grid, "chọn bd =", ValueColumnB, missing);
        var flangeDepth = ValueBeside(grid, "Chiều cao cánh móng h1 =", ValueColumnD, missing);
        var concreteGrade = ValueBeside(grid, "- Cấp độ bền bê tông", ValueColumnC, missing);
        var steelGrade = ValueBeside(grid, "- Nhóm thép", ValueColumnC, missing);
        var springRow = FindInteriorSpringRow(grid, missing);
        var longMoment = SingleFormula(grid, LongMomentFormula(), "long direction moment formula", missing);
        var shortMoment = SingleFormula(grid, ShortMomentFormula(), "short direction moment formula", missing);

        if (missing.Count == 0)
        {
            RequireLoadRows(grid, longMoment, MomentXColumnLetter, firstLoadRow, loadRowCount, missing);
            RequireLoadRows(grid, shortMoment, MomentYColumnLetter, firstLoadRow, loadRowCount, missing);
        }

        if (missing.Count > 0)
        {
            return new LayoutResult(null, missing);
        }

        var layout = new SheetLayout(
            firstLoadRow,
            loadRowCount,
            spanRow,
            beamDepth,
            footingWidth,
            columnWidth,
            beamWidth,
            flangeDepth,
            concreteGrade,
            steelGrade,
            springRow,
            longMoment,
            shortMoment);
        return new LayoutResult(layout, []);
    }

    private static int FindLoadHeaderRow(SheetGrid grid, List<string> missing)
    {
        var axialHeaders = grid
            .FindAddresses(cell => LabelMatches(cell, LoadHeaders[1]))
            .Where(address => address.Column == AxialHeaderColumn)
            .ToList();
        if (axialHeaders.Count != 1)
        {
            missing.Add(LoadHeaders[1]);
            return 0;
        }

        var row = axialHeaders[0].Row;
        for (var offset = 0; offset < LoadHeaders.Length; offset++)
        {
            var header = new CellAddress(row, AxisHeaderColumn + offset);
            if (!string.Equals(grid.TextAt(header), LoadHeaders[offset], StringComparison.OrdinalIgnoreCase))
            {
                missing.Add(LoadHeaders[offset]);
            }
        }

        return row;
    }

    private static int CountLoadRows(SheetGrid grid, int firstLoadRow)
    {
        var count = 0;
        while (grid.CellAt(new CellAddress(firstLoadRow + count, AxialHeaderColumn))?.Number is not null)
        {
            count++;
        }

        return count;
    }

    private static int FindSpanRow(SheetGrid grid, List<string> missing)
    {
        var headerRows = grid
            .FindAddresses(cell => LabelMatches(cell, "x1"))
            .Where(address => grid.TextAt(address with { Column = LabelColumn })
                .StartsWith("Console", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (headerRows.Count != 1)
        {
            missing.Add("Console + x1 span header");
            return 0;
        }

        return headerRows[0].Row + UnitRowsBelowSpanHeader;
    }

    private static CellAddress ValueBeside(SheetGrid grid, string label, int valueColumn, List<string> missing)
    {
        var labels = grid
            .FindAddresses(cell => LabelMatches(cell, label))
            .Where(address => address.Column == LabelColumn)
            .ToList();
        if (labels.Count != 1)
        {
            missing.Add(label);
            return default;
        }

        return labels[0] with { Column = valueColumn };
    }

    private static int FindInteriorSpringRow(SheetGrid grid, List<string> missing)
    {
        const string SpringTitle = "BẢNG TÍNH HỆ SỐ LÒ XO ki";
        var titles = grid.FindAddresses(cell => LabelMatches(cell, SpringTitle)).ToList();
        if (titles.Count != 1
            || !grid.TextAt(new CellAddress(titles[0].Row + 1, SpringStiffnessColumn)).StartsWith("ki", StringComparison.Ordinal))
        {
            missing.Add(SpringTitle);
            return 0;
        }

        return titles[0].Row + HeaderRowsBelowSpringTitle;
    }

    private static CellAddress SingleFormula(SheetGrid grid, Regex pattern, string description, List<string> missing)
    {
        var matches = grid.FindAddresses(cell => cell.Formula is { } formula && pattern.IsMatch(formula)).ToList();
        if (matches.Count != 1)
        {
            missing.Add($"{description} ({matches.Count} found)");
            return default;
        }

        return matches[0];
    }

    private static void RequireLoadRows(
        SheetGrid grid,
        CellAddress formulaCell,
        char column,
        int firstLoadRow,
        int loadRowCount,
        List<string> missing)
    {
        var loadSum = $"SUM({column}{firstLoadRow}:{column}{firstLoadRow + loadRowCount - 1})";
        if (grid.CellAt(formulaCell)?.Formula?.Contains(loadSum, StringComparison.Ordinal) != true)
        {
            missing.Add($"{loadSum} in the moment formula at {formulaCell}");
        }
    }

    private static bool LabelMatches(SheetCell cell, string label) =>
        string.Equals(cell.Text?.Trim(), label, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^=ROUND\(SUM\(D\d+:D\d+\)\+ABS\(")]
    private static partial Regex LongMomentFormula();

    [GeneratedRegex(@"^=ROUND\(SUM\(F\d+:F\d+\)\+SUM\(G\d+:G\d+\)\*")]
    private static partial Regex ShortMomentFormula();
}
