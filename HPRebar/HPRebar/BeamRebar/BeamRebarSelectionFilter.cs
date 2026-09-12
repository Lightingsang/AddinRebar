using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace HPRebar.BeamRebar;

/// <summary>
/// Restricts selection strictly to structural framing family instances (beams).
/// </summary>
public sealed class BeamRebarSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element element)
    {
        if (element is null) return false;

        // Ensure category matches BuiltInCategory.OST_StructuralFraming
        if (element.Category?.BuiltInCategory != BuiltInCategory.OST_StructuralFraming)
            return false;

        // Ensure element is a placed instance (not a FamilySymbol or ElementType)
        return element is FamilyInstance;
    }

    public bool AllowReference(Reference reference, XYZ position) => true;
}
