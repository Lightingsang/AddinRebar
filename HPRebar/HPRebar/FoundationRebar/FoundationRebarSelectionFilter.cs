using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace HPRebar.FoundationRebar;

/// <summary>
/// Selection filter restricting interactive picking strictly to Floor (foundation slab) elements.
/// </summary>
public sealed class FoundationRebarSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element element)
    {
        if (element is null) return false;

        if (element is Floor) return true;

        // Multi-version: ElementId
#if REVIT2024_OR_GREATER
        if (element.Category?.Id.Value == (long)BuiltInCategory.OST_Floors) return true;
#else
        if (element.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_Floors) return true;
#endif

        return false;
    }

    public bool AllowReference(Reference reference, XYZ position) => true;
}
