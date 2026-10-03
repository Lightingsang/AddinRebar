using Autodesk.Revit.DB;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Single boundary between the millimetre domain used by <c>HPRebar.Core</c> and Revit's internal feet.
///     Every conversion goes through here so a signature change lands in one place.
/// </summary>
internal static class RevitUnits
{
    /// <summary>Millimetres to Revit internal units (decimal feet).</summary>
    public static double MmToFt(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

    /// <summary>Revit internal units (decimal feet) to millimetres.</summary>
    public static double FtToMm(double ft) => UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);
}
