using Autodesk.Revit.DB;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
/// Conversion boundary between millimetres (HPRebar.Core) and Revit internal units (decimal feet).
/// </summary>
public static class RevitUnits
{
    /// <summary>Millimetres to Revit internal units (decimal feet).</summary>
    public static double MmToFt(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

    /// <summary>Revit internal units (decimal feet) to millimetres.</summary>
    public static double FtToMm(double ft) => UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);
}
