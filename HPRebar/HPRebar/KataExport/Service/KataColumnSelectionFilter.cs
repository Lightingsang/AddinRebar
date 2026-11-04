using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace HPRebar.KataExport.Service;

/// <summary>Structural columns only: the picks of Kata Column Export and Kata Column Rebar.</summary>
public sealed class KataColumnSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element element) =>
        element is FamilyInstance && element.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralColumns;

    public bool AllowReference(Reference reference, XYZ position) => false;
}
