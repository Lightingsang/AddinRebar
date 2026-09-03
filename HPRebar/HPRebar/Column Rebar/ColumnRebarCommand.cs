using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;

namespace HPRebar.ColumnRebar;

/// <summary>
///     Entry point for the Column Rebar tool.
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public sealed class ColumnRebarCommand : ExternalCommand
{
    public override void Execute()
    {
        TaskDialog.Show("Column Rebar", "Scaffold OK");
    }
}
