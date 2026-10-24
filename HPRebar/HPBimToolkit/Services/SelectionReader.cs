using Autodesk.Revit.UI;
using HPBimToolkit.Core.Contracts;

namespace HPBimToolkit.Services;

/// <summary>Reads the current selection as ids (capped by <see cref="SelectionSnapshot" />).</summary>
public static class SelectionReader
{
    public static SelectionSnapshot Read(UIApplication app)
    {
        var ids = ActiveDocument.Require(app).Selection.GetElementIds();
        return SelectionSnapshot.From(ids.Select(id => id.Value).ToArray());
    }
}
