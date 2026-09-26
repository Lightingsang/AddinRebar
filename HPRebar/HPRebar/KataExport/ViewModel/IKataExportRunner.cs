using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPRebar.KataExport.Model;

namespace HPRebar.KataExport.ViewModel;

/// <summary>
/// Bridge interface between KataExportViewModel and Revit API thread.
/// </summary>
public interface IKataExportRunner
{
    Task HighlightAsync(IReadOnlyList<ElementId> beamIds);
    Task<KataExportSession?> RepickAsync();
}
