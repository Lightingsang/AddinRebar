using System.Globalization;
using HPFoundation.Core.Model;

namespace HPFoundation.Core.Sheets;

/// <summary>An input, or the issues that prevented it.</summary>
public sealed record InputResult(FoundationInput? Input, IReadOnlyList<SheetIssue> Issues);

/// <summary>Assembles the foundation input from a calculated sheet, reporting every unusable cell at once.</summary>
public static class FoundationInputBuilder
{
    private const int AxisColumn = 2;
    private const int AxialColumn = 3;
    private const int MomentXColumn = 4;
    private const int ShearXColumn = 5;
    private const int MomentYColumn = 6;
    private const int ShearYColumn = 7;
    private const int FirstSpanColumn = 1;
    private const int SpringSpacingColumn = 2;
    private const int SpringStiffnessColumn = 6;

    public static InputResult Build(SheetGrid grid, SheetLayout layout)
    {
        var reader = new CellReader(grid);

        var spans = ReadSpans(reader, layout);
        var columns = ReadColumns(reader, layout, spans);
        var section = new BeamSection(
            reader.Number(layout.FootingWidth),
            reader.Number(layout.BeamWidth),
            reader.Number(layout.BeamDepth),
            reader.Number(layout.FlangeDepth),
            reader.Number(layout.ColumnWidth));
        var springs = ReadSprings(reader, layout);
        var concreteGrade = reader.Text(layout.ConcreteGrade);
        var steelGrade = reader.Text(layout.SteelGrade);

        if (reader.Issues.Count > 0)
        {
            return new InputResult(null, reader.Issues);
        }

        var input = new FoundationInput(
            grid.SheetName,
            columns,
            spans.Sum(),
            section,
            springs,
            concreteGrade,
            steelGrade);
        return new InputResult(input, []);
    }

    /// <summary>The console, the spans between columns and the far console: one more value than there are columns.</summary>
    private static double[] ReadSpans(CellReader reader, SheetLayout layout) =>
        Enumerable
            .Range(0, layout.LoadRowCount + 1)
            .Select(offset => reader.Number(new CellAddress(layout.SpanRow, FirstSpanColumn + offset)))
            .ToArray();

    private static ColumnLoad[] ReadColumns(CellReader reader, SheetLayout layout, double[] spans)
    {
        return Enumerable
            .Range(0, layout.LoadRowCount)
            .Select(index =>
            {
                var row = layout.FirstLoadRow + index;
                return new ColumnLoad(
                    reader.AxisLabel(new CellAddress(row, AxisColumn)),
                    spans.Take(index + 1).Sum(),
                    reader.Number(new CellAddress(row, AxialColumn)),
                    reader.Number(new CellAddress(row, MomentXColumn)),
                    reader.Number(new CellAddress(row, ShearXColumn)),
                    reader.Number(new CellAddress(row, MomentYColumn)),
                    reader.Number(new CellAddress(row, ShearYColumn)));
            })
            .ToArray();
    }

    private static SpringSupport ReadSprings(CellReader reader, SheetLayout layout)
    {
        var interiorRow = layout.InteriorSpringRow;
        var endRow = interiorRow + 1;
        return new SpringSupport(
            reader.Number(new CellAddress(interiorRow, SpringStiffnessColumn)),
            reader.Number(new CellAddress(endRow, SpringStiffnessColumn)),
            reader.Number(new CellAddress(interiorRow, SpringSpacingColumn)),
            reader.Number(new CellAddress(endRow, SpringSpacingColumn)));
    }

    /// <summary>Collects unusable cells instead of stopping at the first one.</summary>
    private sealed class CellReader(SheetGrid grid)
    {
        private readonly List<SheetIssue> _issues = [];

        public IReadOnlyList<SheetIssue> Issues => _issues;

        public double Number(CellAddress address)
        {
            var cell = grid.CellAt(address);
            if (cell?.Number is { } number)
            {
                return number;
            }

            Report(address, cell, "a number");
            return 0;
        }

        /// <summary>Some sheets letter their axes, others number them.</summary>
        public string AxisLabel(CellAddress address) =>
            grid.CellAt(address)?.Number is { } number
                ? number.ToString(CultureInfo.InvariantCulture)
                : Text(address);

        public string Text(CellAddress address)
        {
            var cell = grid.CellAt(address);
            if (!string.IsNullOrWhiteSpace(cell?.Text))
            {
                return cell.Text.Trim();
            }

            Report(address, cell, "text");
            return string.Empty;
        }

        private void Report(CellAddress address, SheetCell? cell, string expected)
        {
            var found = cell?.ErrorText ?? cell?.Text ?? "nothing";
            _issues.Add(new SheetIssue(address.ToString(), $"Expected {expected}, found {found}."));
        }
    }
}
