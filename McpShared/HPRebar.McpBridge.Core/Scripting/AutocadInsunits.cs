namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     AutoCAD's INSUNITS codes (the numeric values of <c>Autodesk.AutoCAD.DatabaseServices.UnitsValue</c>,
///     AcDbMgd 25.1) mapped to millimetres. Kept as plain numbers so the table is testable without the
///     AutoCAD assemblies, which are mixed-mode and only load inside acad.exe. Every tool speaks
///     millimetres at its boundary; the drawing unit is whatever this table says.
/// </summary>
public static class AutocadInsunits
{
    /// <summary>INSUNITS = 0. Treated as millimetres with a note so the script can log the guess.</summary>
    public const int Unitless = 0;

    private static readonly IReadOnlyDictionary<int, (string Label, double MmPerUnit)> Table = new Dictionary<int, (string, double)>
    {
        [1] = ("Inches", 25.4),
        [2] = ("Feet", 304.8),
        [3] = ("Miles", 1_609_344),
        [4] = ("Millimeters", 1),
        [5] = ("Centimeters", 10),
        [6] = ("Meters", 1000),
        [7] = ("Kilometers", 1_000_000),
        [8] = ("Microinches", 0.0000254),
        [9] = ("Mils", 0.0254),
        [10] = ("Yards", 914.4),
        [11] = ("Angstroms", 1e-7),
        [12] = ("Nanometers", 1e-6),
        [13] = ("Microns", 0.001),
        [14] = ("Decimeters", 100),
        [15] = ("Dekameters", 10_000),
        [16] = ("Hectometers", 100_000),
        [17] = ("Gigameters", 1e12),
        [18] = ("Astronomical", 1.495978707e14),
        [19] = ("LightYears", 9.4607304725808e18),
        [20] = ("Parsecs", 3.0856775814913673e19),
        [21] = ("USSurveyFeet", 304.8006096012192),
        [22] = ("USSurveyInch", 25.4000508001016),
        [23] = ("USSurveyYard", 914.4018288036576),
        [24] = ("USSurveyMile", 1_609_347.218694437),
    };

    /// <summary>
    ///     Units for an INSUNITS code. Unknown or unitless drawings fall back to millimetres with a
    ///     <see cref="ScriptUnits.Note"/> explaining the guess — the script logs it, the AI sees it.
    /// </summary>
    public static ScriptUnits For(int insunits)
    {
        if (Table.TryGetValue(insunits, out var entry)) return new ScriptUnits(entry.Label, entry.MmPerUnit);

        return insunits == Unitless
            ? new ScriptUnits("Unitless", 1, "INSUNITS is 0 (unitless); the bridge treats one drawing unit as one millimetre.")
            : new ScriptUnits("Unknown", 1, $"INSUNITS {insunits} is not in the bridge's table; treated as millimetres.");
    }

    /// <summary>The label AutoCAD would show for a code, for the context snapshot.</summary>
    public static string LabelFor(int insunits) => For(insunits).Label;
}
