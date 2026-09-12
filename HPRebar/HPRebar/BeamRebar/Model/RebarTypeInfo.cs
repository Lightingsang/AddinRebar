using Autodesk.Revit.DB.Structure;

namespace HPRebar.BeamRebar.Model;

/// <summary>
/// A rebar bar type available in the document, with its bar diameter in millimetres.
/// </summary>
public sealed record RebarTypeInfo
{
    public string Name { get; init; } = string.Empty;

    public double DiameterMm { get; init; }

    public RebarBarType BarType { get; init; } = null!;
}
