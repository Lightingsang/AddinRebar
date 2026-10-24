using Autodesk.Revit.UI;
using HPBimToolkit.Core.Contracts;
using HPWebHost.Protocol;

namespace HPBimToolkit.Services;

/// <summary>The active document for commands that need one; none open is <c>NO_DOCUMENT</c>, which the page shows as its empty state.</summary>
public static class ActiveDocument
{
    public static UIDocument Require(UIApplication app) =>
        app.ActiveUIDocument ?? throw new BridgeException(BridgeErrorCodes.NoDocument, "No Revit document is open.");
}
