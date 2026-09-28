using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace HPRebar.KataRebar;

/// <summary>
/// Restricts user pick operations to structural framing elements (concrete beams).
/// </summary>
public sealed class KataRebarSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem is FamilyInstance && elem.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming;
    }

    public bool AllowReference(Reference reference, XYZ position) => false;
}
