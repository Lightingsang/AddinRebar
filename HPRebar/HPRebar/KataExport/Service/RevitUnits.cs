using Autodesk.Revit.DB;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Boundary between the millimetre domain of <c>HPRebar.Core.KataExport</c> and Revit's internal feet,
/// independent of the project's display units.
/// </summary>
public static class RevitUnits
{
    /// <summary>Millimetres to Revit internal units (decimal feet).</summary>
    public static double MmToFt(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

    /// <summary>Revit internal units (decimal feet) to millimetres.</summary>
    public static double FtToMm(double ft) => UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);
}
