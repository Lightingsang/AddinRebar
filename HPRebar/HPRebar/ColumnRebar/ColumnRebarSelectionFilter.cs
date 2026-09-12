using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace HPRebar.ColumnRebar;

/// <summary>Restricts picking to structural columns.</summary>
public sealed class ColumnRebarSelectionFilter : ISelectionFilter
{
    // Category.BuiltInCategory rather than the category name: the name is localised, and rather than
    // ElementId, whose numeric accessor changed type between Revit versions.
    public bool AllowElement(Element element) =>
        element.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralColumns;

    public bool AllowReference(Reference reference, XYZ position) => true;
}
