using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.KataRebar.Model;

/// <summary>A RebarBarType of the project as the window lists it; the element itself is looked up again by id when drawing.</summary>
public sealed record RebarTypeOption
{
    public string Name { get; init; } = "";

    public double DiameterMm { get; init; }

    public RebarDeformationType DeformationType { get; init; } = RebarDeformationType.Deformed;

    public ElementId Id { get; init; } = ElementId.InvalidElementId;

    public override string ToString() => $"{Name} (Ø{DiameterMm:0.#} mm)";
}
