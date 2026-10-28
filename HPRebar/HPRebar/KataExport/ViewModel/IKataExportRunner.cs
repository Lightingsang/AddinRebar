using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.Model;
using HPRebar.KataRebar.Model;

namespace HPRebar.KataExport.ViewModel;

/// <summary>
/// Bridge interface between KataExportViewModel and Revit API thread.
/// </summary>
public interface IKataExportRunner
{
    Task HighlightAsync(IReadOnlyList<ElementId> beamIds);
    Task<KataExportSession?> RepickAsync();

    /// <summary>Measures the beams and plans the sheet on them, without changing the model.</summary>
    /// <param name="preferReversed">The direction the window writes the sheet in (settles a symmetric run).</param>
    /// <param name="addJointRebar">Reinforce the crossing beams and stub columns Revit measures on the spans.</param>
    Task<KataRebarPreview> PreviewRebarAsync(
        IReadOnlyList<ElementId> beamIds, KataBeamRebarSpec spec, KataSettings settings, bool preferReversed, bool addJointRebar);

    Task<KataRebarGenerationResult> GenerateRebarAsync(
        IReadOnlyList<ElementId> beamIds,
        KataBeamRebarSpec spec,
        KataSettings settings,
        bool preferReversed,
        bool addJointRebar,
        IReadOnlyDictionary<double, ElementId> barTypeIds,
        IReadOnlyCollection<string> removedKeys,
        string? plannedFingerprint);
}
