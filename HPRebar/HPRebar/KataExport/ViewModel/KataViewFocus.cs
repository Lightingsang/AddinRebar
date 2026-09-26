namespace HPRebar.KataExport.ViewModel;

/// <summary>
/// A request for the elevation to frame something: the span at <paramref name="ColumnIndex"/> with its two
/// supports, or the whole run when null. <paramref name="Serial"/> makes a repeated request a new value.
/// </summary>
public sealed record KataViewFocus(int? ColumnIndex, int Serial);
