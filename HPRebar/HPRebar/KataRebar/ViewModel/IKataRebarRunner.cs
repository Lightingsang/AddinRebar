using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;

namespace HPRebar.KataRebar.ViewModel;

/// <summary>What the modeless window asks of Revit; each call runs on Revit's API thread.</summary>
public interface IKataRebarRunner
{
    /// <summary>Lets the user pick beams in the model and measures them; null when the pick is cancelled.</summary>
    Task<KataBeamMatchResult?> PickBeamsAsync();

    Task<KataBeamMatchResult> MeasureBeamsAsync(IReadOnlyList<ElementId> beamIds);

    /// <summary>Measures the beams again, plans the sheet on them and draws the bars.</summary>
    Task<KataRebarGenerationResult> GenerateAsync(
        IReadOnlyList<ElementId> beamIds,
        KataBeamRebarSpec spec,
        IReadOnlyDictionary<double, ElementId> barTypeIds);
}
