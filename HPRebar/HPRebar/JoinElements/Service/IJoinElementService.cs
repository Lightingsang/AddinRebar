using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.JoinElements.Model;

namespace HPRebar.JoinElements.Service;

/// <summary>
/// Service interface for detecting collisions and executing geometry joining between elements.
/// </summary>
public interface IJoinElementService
{
    /// <summary>
    /// Executes Auto Join according to specified settings inside a Revit Transaction.
    /// </summary>
    JoinElementResult ExecuteJoin(
        Document doc,
        UIDocument? uidoc,
        JoinElementSettings settings,
        IProgress<JoinProgressReport>? progress = null);
}
