using Autodesk.Revit.DB.Structure;

namespace HPRebar.ColumnRebar.Model;

/// <summary>One rebar product available in the document, with its diameter already in millimetres.</summary>
public sealed record RebarTypeInfo
{
    public string Name { get; init; } = string.Empty;

    public double DiameterMm { get; init; }

    public RebarBarType BarType { get; init; } = null!;
}
