using HPFoundation.Core.Model;

namespace HPFoundation.Core.Sheets;

/// <summary>Checks that an input describes a footing the four-column ETABS model can represent.</summary>
public static class InputValidator
{
    private const int SupportedColumnCount = 4;
    private const double LengthToleranceM = 1e-6;
    private const string FailedGradeMarker = "!!!";

    public static IReadOnlyList<SheetIssue> Validate(FoundationInput input)
    {
        return ColumnIssues(input)
            .Concat(SectionIssues(input.Section))
            .Concat(SpringIssues(input))
            .Select(message => new SheetIssue(string.Empty, message))
            .ToList();
    }

    /// <summary>
    ///     Finds cells Excel could not calculate. The sheet's grade tables answer "!!!" for a grade they do not know,
    ///     which a reader of the inputs alone would never see.
    /// </summary>
    public static IReadOnlyList<SheetIssue> FailedCells(SheetGrid grid)
    {
        return grid
            .FindAddresses(cell => cell.ErrorText is not null || cell.Text == FailedGradeMarker)
            .Select(address => new SheetIssue(
                address.ToString(),
                $"Excel could not calculate this cell: {grid.CellAt(address)!.ErrorText ?? FailedGradeMarker}."))
            .ToList();
    }

    private static IEnumerable<string> ColumnIssues(FoundationInput input)
    {
        if (input.Columns.Count != SupportedColumnCount)
        {
            yield return $"Expected {SupportedColumnCount} columns, found {input.Columns.Count}.";
        }

        var positions = input.Columns.Select(column => column.AxisPositionM).ToList();
        if (positions.Zip(positions.Skip(1), (left, right) => right <= left).Any(isDescending => isDescending))
        {
            yield return "The column axes must run in one direction with positive spans.";
        }

        foreach (var column in input.Columns)
        {
            if (column.AxisPositionM <= 0 || column.AxisPositionM >= input.FootingLengthM)
            {
                yield return $"axis {column.Axis} at {column.AxisPositionM} m lies outside the footing.";
            }

            if (column.AxialT <= 0)
            {
                yield return $"axis {column.Axis} has no compressive axial force.";
            }
        }
    }

    private static IEnumerable<string> SectionIssues(BeamSection section)
    {
        if (section.BeamWidthM <= 0 || section.BeamWidthM >= section.FootingWidthM)
        {
            yield return "The beam width must be positive and less than the footing width.";
        }

        if (section.FlangeDepthM <= 0 || section.FlangeDepthM >= section.BeamDepthM)
        {
            yield return "The flange depth must be positive and less than the beam depth.";
        }
    }

    private static bool IsOnGrid(double positionM, double spacingM)
    {
        var divisions = positionM / spacingM;
        return Math.Abs(divisions - Math.Round(divisions)) * spacingM <= LengthToleranceM;
    }

    private static IEnumerable<string> SpringIssues(FoundationInput input)
    {
        var springs = input.Springs;
        if (springs.InteriorStiffnessTPerM <= 0 || springs.EndStiffnessTPerM <= 0)
        {
            yield return "Every spring stiffness must be positive.";
        }

        if (springs.InteriorSpacingM <= 0)
        {
            yield return "The spring spacing must be positive.";
            yield break;
        }

        if (!IsOnGrid(input.FootingLengthM, springs.InteriorSpacingM))
        {
            yield return $"The spring spacing {springs.InteriorSpacingM} m does not divide the footing length.";
        }
    }
}
