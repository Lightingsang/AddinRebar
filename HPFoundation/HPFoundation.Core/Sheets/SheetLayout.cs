namespace HPFoundation.Core.Sheets;

/// <summary>Where the inputs of a strip footing sheet were found.</summary>
public sealed record SheetLayout(
    int FirstLoadRow,
    int LoadRowCount,
    int SpanRow,
    CellAddress BeamDepth,
    CellAddress FootingWidth,
    CellAddress ColumnWidth,
    CellAddress BeamWidth,
    CellAddress FlangeDepth,
    CellAddress ConcreteGrade,
    CellAddress SteelGrade,
    int InteriorSpringRow,
    CellAddress LongDirectionMoment,
    CellAddress ShortDirectionMoment);

/// <summary>A layout, or the anchors that could not be found. Exactly one of the two is filled.</summary>
public sealed record LayoutResult(SheetLayout? Layout, IReadOnlyList<string> MissingAnchors);
