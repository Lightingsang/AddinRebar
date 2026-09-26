using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace HPRebar.KataExport;

/// <summary>
/// Filters user selection to only allow Structural Framing elements.
/// </summary>
public sealed class KataExportSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem is FamilyInstance fi &&
               elem.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming;
    }

    public bool AllowReference(Reference reference, XYZ position) => false;
}
