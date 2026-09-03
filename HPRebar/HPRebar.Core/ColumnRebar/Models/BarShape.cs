namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>
///     Bending shape of a scheduled bar. Names match the detail-shop family types of the source tool
///     (DS00 = straight, DS07* = transition bend at the top), so the Revit layer can look up the family
///     symbol by enum name.
/// </summary>
public enum BarShape
{
    DS00,
    DS01,
    DS02,
    DS03,
    DS03A,
    DS04,
    DS05,
    DS06,
    DS06A,
    DS07,
    DS07A,
    DS07B
}
