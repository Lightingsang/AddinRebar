using System.Globalization;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     What a script sees as `units`: the host's drawing/model length unit and the two conversions a
///     script needs at its boundary, because every tool takes millimetres from the AI and the host API
///     works in its own unit (feet in Revit, whatever INSUNITS says in AutoCAD). Host-neutral: the bridge
///     builds it from the host's settings; tests build it from a number.
/// </summary>
public sealed class ScriptUnits
{
    /// <summary>Millimetres, as a fallback for a drawing that declares no unit.</summary>
    public static readonly ScriptUnits Millimeters = new ScriptUnits("Millimeters", 1.0);

    public ScriptUnits(string label, double mmPerUnit, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("label is required", nameof(label));
        if (!(mmPerUnit > 0) || double.IsInfinity(mmPerUnit)) throw new ArgumentOutOfRangeException(nameof(mmPerUnit), "must be a positive finite number");

        Label = label;
        MmPerUnit = mmPerUnit;
        Note = note;
    }

    /// <summary>Human label of the host unit, e.g. "Millimeters", "Inches", "Unitless".</summary>
    public string Label { get; }

    /// <summary>How many millimetres one host unit is: 1 for mm, 25.4 for inches, 304.8 for feet.</summary>
    public double MmPerUnit { get; }

    /// <summary>Set when the conversion is a guess (e.g. INSUNITS = Unitless treated as mm) so the script can log it.</summary>
    public string? Note { get; }

    /// <summary>Millimetres → host units.</summary>
    public double ToDrawing(double millimeters) => millimeters / MmPerUnit;

    /// <summary>Host units → millimetres.</summary>
    public double ToMm(double drawingUnits) => drawingUnits * MmPerUnit;

    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0} ({1} mm/unit)", Label, MmPerUnit);
}
